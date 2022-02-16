using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.Data;
using Microsoft.AspNetCore.Mvc;
using Lexvor.Models;
using Lexvor.Models.AccountViewModels;
using Lexvor.Models.HomeViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Acklann.Plaid;
using Acklann.Plaid.Auth;
using Acklann.Plaid.Management;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Lexvor.API.ChargeOver;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Payment;
using Lexvor.Extensions;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Stripe;
using UAParser;
using Address = Lexvor.API.Objects.User.Address;
using BankAccount = Lexvor.API.Objects.User.BankAccount;
using Charge = Lexvor.API.Objects.User.Charge;
using Environment = Acklann.Plaid.Environment;

namespace Lexvor.Controllers {
	[Authorize(Roles = Roles.Trial)]
	//[ServiceFilter(typeof(ExceptionCatcher))]
	public class PaymentsController : BaseUserController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ConnectionStrings _connStrings;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;
		private readonly string _defaultConn;

		public static string Name => "Payments";

		public PaymentsController(
			UserManager<ApplicationUser> userManager,
			IEmailSender emailSender,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager) {
			_userManager = userManager;
			_emailSender = emailSender;
			_connStrings = connStrings.Value;
			_other = other.Value;
			_defaultConn = _connStrings.DefaultConnection;
		}

		public override void OnActionExecuting(ActionExecutingContext context) {
			base.OnActionExecuting(context);
		}	

		public async Task<IActionResult> CancelPendingPlan() {
			var pending =
				await _context.Plans.Include(x => x.PlanType).Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();
			pending.ForEach(x => x.Status = PlanStatus.Cancelled);

			// Delete user orders for these plans
			var orders = await _context.UserOrders.Where(x => pending.Select(y => y.Id).Contains(x.UserPlanId)).ToListAsync();
			_context.RemoveRange(orders);

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
		}

		public IActionResult BankAccountInformation(string returnUrl = "") {		
			ViewData["ReturnUrl"] = returnUrl;
			// Don't filter on active so we can get partials and fill the model	

			return View(new BankInformationViewModel() {
				Profile = CurrentProfile,
				PlaidEnv = _other.PlaidEnv,
				PlaidPublicKey = _other.PlaidPublicKey
			});
		}

		[HttpPost]
		public async Task<IActionResult> BankAccountInformation(BankInformationViewModel model, string returnUrl = "", bool bankAccountMissing = false) {
			ViewData["ReturnUrl"] = returnUrl;

			model.PlaidEnv = _other.PlaidEnv;
			model.PlaidPublicKey = _other.PlaidPublicKey;
			model.Profile = CurrentProfile;

			if (ModelState.IsValid) {
				if (!validateRoutingNumber(model.BankAccountRoutingNum)) {
					ModelState.AddModelError("", "Routing number is not valid");
					return View(model);
				}

				if (model.BankName == "Chime") {
					ModelState.AddModelError("", "Chime is not a supported bank.");
					return View(model);
					}

				try {
					var existingPayAccounts = await _context.PayAccounts.Where(x => x.ProfileId == CurrentProfile.Id && !x.Archived).ToListAsync();
					if (existingPayAccounts.Any()) {
						// Archive existing incomplete accounts
						existingPayAccounts.ForEach(x => {
							x.Active = false;
							x.Archived = true;
						});
					}

					// Save the payment information for later charging
					var payAccount = new BankAccount() {
						Profile = CurrentProfile,
						AccountFirstName = model.FirstNameOnAccount,
						AccountLastName = model.LastNameOnAccount,
						AccountNumber = StringCipher.EncryptString(model.BankAccountNum, _other.EncryptionKey),
						RoutingNumber = model.BankAccountRoutingNum,
						MaskedAccountNumber = $"****{model.BankAccountNum.Substring(model.BankAccountNum.Length - 4, 4)}",
						Active = false,
						CreatedAt = DateTime.UtcNow,
						LastUpdated = DateTime.UtcNow,
						Bank = model.BankName,
						ExternalReferenceNumber = model.PlaidAccountId
					};

					// If profile data for name is empty, fill it in.
					if (CurrentProfile.FirstName.IsNull()) {
						CurrentProfile.FirstName = model.FirstNameOnAccount;
					}
					if (CurrentProfile.LastName.IsNull()) {
						CurrentProfile.LastName = model.LastNameOnAccount;
					}

					_context.Add(payAccount);
					await _context.SaveChangesAsync();

					Enum.TryParse(_other.PlaidEnv, out Environment env);

					try {
						ExchangeTokenResponse access;
						try {
							var client = new PlaidClient(env);
							access = await client.ExchangeTokenAsync(new ExchangeTokenRequest() {
								ClientId = _other.PlaidClientId,
								PublicToken = model.PublicToken,
								Secret = _other.PlaidSecret
							});
						}
						catch (Exception e) {
							var code = ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Link-token-Exchange");
							ModelState.AddModelError("", $"There was an error saving your bank details ({code}). Contact Support.");
							return View(model);
						}

						_context.ProfileSettings.Add(new ProfileSetting() {
							ProfileId = CurrentProfile.Id,
							SettingName = "plaid_access",
							SettingValue = access.AccessToken,
							DateAdded = DateTime.UtcNow
						});
						_context.ProfileSettings.Add(new ProfileSetting() {
							ProfileId = CurrentProfile.Id,
							SettingName = "plaid_itemid",
							SettingValue = access.ItemId,
							DateAdded = DateTime.UtcNow
						});

						// Activate the pay account
						payAccount.Active = true;
						payAccount.LastUpdated = DateTime.UtcNow;

						var plaid = new Plaid(_other.PlaidSecret, _other.PlaidClientId, _other.PlaidEnv);
						try {
							var accountMasks = await plaid.GetAccountInfo(access.AccessToken);

							if (accountMasks.Count == 0) {
								ModelState.AddModelError("", $"The account you linked did not contain any checking accounts. Please try linking again with a bank account that contains the checking account you are using for payments.");
								return View(model);
							}

							var isAdmin = await _userManager.IsInRoleAsync(await GetCurrentAccount(), Roles.Admin);
							if (!accountMasks.Contains(payAccount.AccountMask) && !isAdmin) {
								ModelState.AddModelError("", $"The account you linked did not contain the checking account you are using for payments.");
								return View(model);
							}
						}
						catch (Exception e) {
							// Account details error should be logged, but not stop the user.
							ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Account-Details");
						}

						// Gather identity
						try {
							var ids = await plaid.GetIdentity(access.AccessToken);
							foreach (var identity in ids) {
								try {
									// Save each ID separately because we want as much data as possible
									identity.Profile = CurrentProfile;
									await _context.AddAsync(identity);
									await _context.SaveChangesAsync();
								}
								catch (Exception e) {
									ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Id-Save");
								}
							}
						}
						catch (Exception e) {
							ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Id-Check");
						}

						// Last Balance
						try {
							var bal = await plaid.GetLastBalance(access.AccessToken, model.PlaidAccountId);
							payAccount.LastBalance = (double)bal;
							payAccount.LastBalanceCheck = DateTime.Now;
							await _context.SaveChangesAsync();
						}
						catch (Exception e) {
							// Balance check errors should be logged, but not stop the user.
							ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Last-Balance");
						}
						var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
						var plan = await _context.Plans.Include(x => x.Device).Where(x => x.ProfileId == CurrentProfile.Id).FirstOrDefaultAsync();
						var device = _context.Devices.Where(x => x.Id == plan.DeviceId).FirstOrDefault();
						await DeviceService.AssignDeviceToUserPlan(_context, _emailSender, new AssignContext() {
							User = user,
							UserPlan = plan,
							Device = device,
						});
						//return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
						return RedirectToAction(nameof(BillingController.EditComplete), BillingController.Name);
					}
					catch (Exception e) {
						var code = ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Link");
						ModelState.AddModelError("", $"There was an error saving your bank details ({code}). Contact Support.");
					}
				}
				catch (Exception e) {
					var code = ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "PayAccountSave");
					ModelState.AddModelError("", $"There was an error saving your bank details ({code}). Contact Support.");
				}
			}
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Charge(string body) {
			if (Request.Form == null || Request.Form.Count < 1 || !Request.Form.ContainsKey("token[id]")) {
				var e = new Exception("Request Form was empty for charging.");
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Charge");
				throw e;
			}
			var user = await _userManager.FindByEmailAsync(CurrentUserEmail);

			var token = Request.Form.FirstOrDefault(b => b.Key == "token[id]").Value.ToString() ?? "";

			// Update the profile with the values from the charge
			try {
				var name = Request.Form.FirstOrDefault(b => b.Key == "token[card][name]").Value.ToString() ?? "";
				var lastFour = Request.Form.FirstOrDefault(b => b.Key == "token[card][last4]").Value.ToString() ?? "";
				CurrentProfile.FirstName = name.Contains(" ") ? name.Split(" ")[0] : name;
				CurrentProfile.LastName = name.Contains(" ") ? string.Join(" ", name.Split(" ").Skip(1)) : name;

				// TODO make this a service and only allow one active at a time.
				var ccAccount = new ProfileCreditCardAccount() {
					Active = true,
					CreatedAt = DateTime.UtcNow,
					CreditCardNumber = lastFour,
					LastUpdated = DateTime.UtcNow,
					ProfileId = CurrentProfile.Id
				};

				_context.Add(ccAccount);

				// Save changes so far
				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Charge-ProfileUpdate");
			}

			if (string.IsNullOrEmpty(CurrentProfile.ExternalCustomerId)) {
				try {
					//var customerId = Payments.GetCustomer(user.Email);
					//CurrentProfile.ExternalCustomerId = customerId;
					// Save changes so far
					await _context.SaveChangesAsync();
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Charge");
					return Json(new { error = true, message = $"There was an error when (the customer could not be created). You have NOT been charged. Please contact support." });
				}
			}

			var planTypeId = Request.Form.FirstOrDefault(b => b.Key == "planTypeId").Value.ToString() ?? "";
			var groupCount = Request.Form.FirstOrDefault(b => b.Key == "count").Value.ToString() ?? "";
			var planType = await _context.PlanTypes.FirstAsync(p => p.Id == Guid.Parse(planTypeId));
			var initiation = planType.InitiationFee;
			var monthly = planType.MonthlyCost;
			// Get any account credits
			var credits = await _context.AccountCredits.Where(a =>
				a.ProfileId == CurrentProfile.Id && a.AppliedAmount < a.Amount && a.ApplicableToInitiation).ToListAsync();

			// Apply the promo
			var promoCode = Request.Form.FirstOrDefault(b => b.Key == "promo").Value.ToString() ?? "";
			if (!string.IsNullOrEmpty(promoCode)) {
				var promo = _context.DiscountCodes.FirstOrDefault(d =>
					d.Code.ToUpper().Trim() == promoCode.ToUpper().Trim() && d.PlanTypeId == Guid.Parse(planTypeId) && d.StartDate <= DateTime.UtcNow &&
					d.EndDate >= DateTime.UtcNow);

				if (promo != null) {
					initiation = promo.NewInitiationFee;
					monthly = promo.NewMonthlyCost;
				}
			}

			// Apply the disable ads surcharge
			var disableAds = Convert.ToBoolean(Request.Form.FirstOrDefault(b => b.Key == "disableAds").Value.ToString() ?? "");
			if (disableAds) {
				monthly += 5;
			}

			// Apply and invalidate credits
			if (credits != null) {
				foreach (var credit in credits) {
					if (initiation > 0.0D) {
						// Get the credit remaining on this credit object
						var remainingCredit = credit.Amount - credit.AppliedAmount;
						// If remaining credit is more than the remaining initiation, then do a partial apply
						if (remainingCredit > initiation) {
							remainingCredit = initiation;
							initiation = 0;
						}
						else {
							initiation = initiation - remainingCredit;
						}

						// After applying set the new applied amount to invalidate the credit.
						credit.AppliedAmount = credit.AppliedAmount + remainingCredit;
						await _context.SaveChangesAsync();
					}
				}
			}

			// Attach the source, so we always have their card
			try {
				//var sourceId = Payments.AttachOrUpdateSource(CurrentProfile.ExternalCustomerId, token);
				// We cannot use the token again, null it out.
				token = null;
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "SourceCreation");
				return Json(new { error = true, message = $"There was an error (source could not be attached). You have NOT been charged. Please contact support." });
			}

			string invoiceId = "Free Initiation";
			string description = $"Initiation Fee for Lexvor {planType.Name}";
			try {
				// First we charge the customer for the first month initiation
				if (Math.Abs(initiation) > 0.0D) {
					//invoiceId = Payments.ChargeCustomerNoToken(Convert.ToInt32(initiation), CurrentUserEmail, description, CurrentProfile.ExternalCustomerId);
				}
			}
			catch (StripeException se) when (se.Message == "Your card was declined.") {
				se.Data.Add("StripeError", JsonConvert.SerializeObject(se.StripeError));
				se.Data.Add("StripeResponse", JsonConvert.SerializeObject(se.StripeResponse));
				ErrorHandler.Capture(_other.SentryDSN, se, HttpContext, "ChargeCreation");
				var useDifferentCard = new[] {
					"card_not_supported",
					"do_not_try_again",
					"generic_decline",
					"insufficient_funds"
				};
				if (useDifferentCard.Contains(se.StripeError.DeclineCode)) {
					return Json(new { error = true, message = $"There was an error charging your card ({se.StripeError.DeclineCode}). Please use a different card." });
				}
				return Json(new { error = true, message = $"There was an error charging your card ({se.StripeError.DeclineCode}). Please try again." });
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ChargeCreation");
				return Json(new { error = true, message = $"There was an error charging your card. You may have been charged for Initiation. Please contact support." });
			}

			var charge = new Charge() {
				ProfileId = CurrentProfile.Id,
				InvoiceId = invoiceId,
				Amount = Convert.ToInt32(initiation),
				Description = description,
				Date = DateTime.UtcNow
			};
			_context.Charges.Add(charge);
			await _context.SaveChangesAsync();

			var userPlanId = Guid.NewGuid();
			try {
				// Then we make the plan (one per customer since the plans can differ so widely based on current promotions)
				//var planId = Payments.CreatePlan(CurrentProfile.ExternalCustomerId, Convert.ToInt32(monthly), planType.Name);
				// Then we make the subscription and apply the plan
				//string subId = Payments.CreateSubscription(CurrentProfile.ExternalCustomerId, planId);

				// Make the User Plan
				var userPlan = new UserPlan() {
					Id = userPlanId,
					PlanTypeId = planType.Id,
					LastModified = DateTime.UtcNow,
					ProfileId = CurrentProfile.Id,
					// ExternalSubscriptionId = subId,
					UserGivenName = $"{planType.Name} Plan {RandomString(2)}",
					Initiation = initiation,
					Monthly = monthly
				};
				_context.Plans.Add(userPlan);

				try {
					await EmailService.SendMembershipEmailAsync(_emailSender, CurrentProfile.FullName, CurrentUserEmail, planType.Name,
						1);
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "WelcomeEmailSend");
				}

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Payments");
				return Json(new { error = true, message = "There was an error assigning your plan. Please contact support." });
			}

			return Json(new { planId = userPlanId, error = false, message = "Purchase Successful." });
		}


		[HttpGet]
		public async Task<IActionResult> StripePayment(string returnUrl = "") {
			ViewData["returnUrl"] = "";
			ViewBag.StripePublishKey = _other.StripePublishableKey;
			var pendingPlans = await _context.Plans
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
			.Include(x => x.UserDevice)
			.Include(x => x.Device)
			.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();
			var statuses = new[] { PlanStatus.Active, PlanStatus.Paid, PlanStatus.PaymentHold, PlanStatus.OnHold, PlanStatus.Pending };
			var currentPlanCount = await _context.Plans
				.CountAsync(x => x.ProfileId == CurrentProfile.Id && statuses.Contains(x.Status));

			var planType = pendingPlans.First().PlanType;
			var monthly = 0;

			// Hard limit on four plans at once
			var linePricing = Lexvor.API.Services.PlanService.GetLinePricing(currentPlanCount > 4 ? 4 : currentPlanCount, planType);
			if (linePricing != null) {
				monthly = linePricing.MonthlyCost;
				// Add any surcharges from the Options
				// Get all chosen options from the plans
				var options = pendingPlans.Select(x => x.UserDevice.ChosenOptions).Where(x => !x.IsNull());
				// Select all the option ids (include duplicates for multiple lines with same option)
				var optionIds = options.SelectMany(x => x.Split(',')).ToList();
				if (optionIds.Any()) {
					// get the options from db to find the surcharge and multiply by the occurances in the id list
					var dbOptions = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id.ToString())).ToListAsync();
					monthly = monthly + dbOptions.Sum(x => x.Surcharge * optionIds.Count(y => y == x.Id.ToString()));
				}

			}
			var model = new StripePaymentViewModel() {
				Profile = CurrentProfile,
				Amount = monthly,
				Email = CurrentUserEmail
			};

			return View(model);

		}

		[HttpPost]
		public async Task<IActionResult> StripePayment(string stripeToken, string stripeEmail, string returnUrl = "") {

			var apiKey = _other.StripeSecretKey;
			var pendingPlans = await _context.Plans
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
			.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
			.Include(x => x.UserDevice)
			.Include(x => x.Device)
			.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			var statuses = new[] { PlanStatus.Active, PlanStatus.Paid, PlanStatus.PaymentHold, PlanStatus.OnHold, PlanStatus.Pending };
			var currentPlanCount = await _context.Plans
				.CountAsync(x => x.ProfileId == CurrentProfile.Id && statuses.Contains(x.Status));

			var planType = pendingPlans.First().PlanType;
			var monthly = 0;

			// Hard limit on four plans at once
			var linePricing = Lexvor.API.Services.PlanService.GetLinePricing(currentPlanCount > 4 ? 4 : currentPlanCount, planType);
			if (linePricing != null) {
				monthly = linePricing.MonthlyCost;
				// Add any surcharges from the Options
				// Get all chosen options from the plans
				var options = pendingPlans.Select(x => x.UserDevice.ChosenOptions).Where(x => !x.IsNull());
				// Select all the option ids (include duplicates for multiple lines with same option)
				var optionIds = options.SelectMany(x => x.Split(',')).ToList();
				if (optionIds.Any()) {
					// get the options from db to find the surcharge and multiply by the occurances in the id list
					var dbOptions = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id.ToString())).ToListAsync();
					monthly = monthly + dbOptions.Sum(x => x.Surcharge * optionIds.Count(y => y == x.Id.ToString()));
				}
				try {
					var stripePay = new StripePay(apiKey).StripePayAttempt(stripeToken, stripeEmail, monthly);
					if (stripePay.Status == "succeeded") {
						var charge = new Charge() {
							Amount = monthly,
							ProfileId = CurrentProfile.Id,
							Date = DateTime.Now,
							NeedsAttention = false,
							Status = ChargeStatus.Charged,
							Description = "Monthly Charge for Go Plan (4 lines)"

						};

						//updating charge table with new entry
						_context.Charges.Add(charge);

						//update plan status to PAID
						var plan = pendingPlans.First();
						plan.Status = PlanStatus.Paid;
						_context.Update(plan);
						await _context.SaveChangesAsync();

					}

				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, "Stripe payment");
				}

			}

			return RedirectToAction(nameof(PurchaseController.ActionNavigator), PurchaseController.Name, new { returnUrl });
		}

		bool validateRoutingNumber(string routingNum) {
			int sum = 0;

			if (routingNum.Length != 9) {
				return false;
			}

			for (int i = 0; i < routingNum.Length; i += 3) {
				sum += Convert.ToInt32((char.GetNumericValue(routingNum[i]) * 3) + (char.GetNumericValue(routingNum[i + 1]) * 7) +
					   char.GetNumericValue(routingNum[i + 2]));
			}

			bool valid = sum != 0 && (sum % 10 == 0); // If the sum is not zero and the sum is an exact multiple of 10, the the routing num is valid

			return valid;
		}
	}
}

