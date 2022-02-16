using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Payment;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;

namespace Lexvor.Controllers {
	[Authorize(Roles = Roles.Trial)]
	public class PlanController : BaseUserController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name => "Plan";

		public PlanController(
			UserManager<ApplicationUser> userManager,
			IOptionsMonitor<OtherSettings> other,
			IEmailSender emailSender,
			ApplicationDbContext context) : base(context, userManager) {
			_userManager = userManager;
			_other = other.CurrentValue;
			_emailSender = emailSender;
		}

		/// <summary>
		/// All plans available to the user
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> Index() {
			var activePlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && PlanService.ActivePendingStatuses.Contains(x.Status)).ToListAsync();
			if (activePlans.Count(x => x.Status == PlanStatus.Pending) >= 4) {
				Message =
					"You have a purchase pending for the maximum allowed number of plans. Please cancel those purchases or continue with your existing purchase.";
			}
			//else if (activePlans.Count() >= 4) {
			//	ErrorMessage =
			//		"You already have 4 plans. You cannot purchase more at this time. Please contact support for an exclusion.";
			//	return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			//}

			// If this is first load after payment info (new account) the all plans are available.
			var plans = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.Where(x => x.DisplayOnPublicPages && !x.Archived && x.Flag == PlanTypeSpecialFlags.NoFlag).OrderBy(x => x.SortOrder).ToListAsync();

			// All existing, non-cancelled plans.
			var existingUserPlan =
				await _context.Plans.FirstOrDefaultAsync(x =>
					x.ProfileId == CurrentProfile.Id && x.Status != PlanStatus.Cancelled);

			var model = new PickPlanViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				Plans = plans,
				HasPendingPurchase = await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending),
				FirstTimeCustomer = !(await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id)),
				AffirmPublicKey = _other.AffirmPublicKey
			};
			return View(model);
		}

		public async Task<IActionResult> Preorder() {
			var activePlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && PlanService.ActivePendingStatuses.Contains(x.Status)).ToListAsync();
			if (activePlans.Count(x => x.Status == PlanStatus.Pending) >= 4) {
				Message =
					"You have a purchase pending for the maximum allowed number of plans. Please cancel those purchases or continue with your existing purchase.";
			}
			//else if (activePlans.Count() >= 4) {
			//	ErrorMessage =
			//		"You already have 4 plans. You cannot purchase more at this time. Please contact support for an exclusion.";
			//	return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			//}

			var plans = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.Where(x => !x.Archived && x.Flag == PlanTypeSpecialFlags.PreOrder).OrderBy(x => x.SortOrder).ToListAsync();

			var model = new PickPlanViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				Plans = plans,
				HasPendingPurchase = activePlans.Any(x => x.Status == PlanStatus.Pending)
			};

			return View(nameof(Index), model);
		}

		public async Task<IActionResult> FutureVIP() {
			var activePlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && PlanService.ActivePendingStatuses.Contains(x.Status)).ToListAsync();
			if (activePlans.Count(x => x.Status == PlanStatus.Pending) >= 4) {
				Message =
					"You have a purchase pending for the maximum allowed number of plans. Please cancel those purchases or continue with your existing purchase.";
			}
			else if (activePlans.Count() >= 4) {
				ErrorMessage =
					"You already have 4 plans. You cannot purchase more at this time. Please contact support for an exclusion.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}

			var plans = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.Where(x => !x.Archived && x.Flag == PlanTypeSpecialFlags.FutureVIP).OrderBy(x => x.SortOrder).ToListAsync();

			var model = new PickPlanViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				Plans = plans,
				HasPendingPurchase = activePlans.Any(x => x.Status == PlanStatus.Pending)
			};

			Message = "We are offering these special, limited time plans for VIP customers.";

			return View(nameof(Index), model);
		}

		public async Task<IActionResult> FuturePromo() {
			var activePlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && PlanService.ActivePendingStatuses.Contains(x.Status)).ToListAsync();
			if (activePlans.Count(x => x.Status == PlanStatus.Pending) >= 4) {
				Message =
					"You have a purchase pending for the maximum allowed number of plans. Please cancel those purchases or continue with your existing purchase.";
			}
			//else if (activePlans.Count() >= 4) {
			//	ErrorMessage =
			//		"You already have 4 plans. You cannot purchase more at this time. Please contact support for an exclusion.";
			//	return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			//}

			var plans = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.Where(x => !x.Archived && x.Flag == PlanTypeSpecialFlags.FuturePromo).OrderBy(x => x.SortOrder).ToListAsync();

			var model = new PickPlanViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				Plans = plans,
				HasPendingPurchase = activePlans.Any(x => x.Status == PlanStatus.Pending)
			};

			Message = "We are offering these special, limited time plans to our best customers.";

			return View(nameof(Index), model);
		}

		public async Task<IActionResult> PrePaid() {
			var activePlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && PlanService.ActivePendingStatuses.Contains(x.Status)).ToListAsync();
			if (activePlans.Count(x => x.Status == PlanStatus.Pending) >= 4) {
				Message =
					"You have a purchase pending for the maximum allowed number of plans. Please cancel those purchases or continue with your existing purchase.";
			}
			//else if (activePlans.Count() >= 4) {
			//	ErrorMessage =
			//		"You already have 4 plans. You cannot purchase more at this time. Please contact support for an exclusion.";
			//	return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			//}

			var plans = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.Where(x => !x.Archived && x.Flag == PlanTypeSpecialFlags.PrePaid).OrderBy(x => x.SortOrder).ToListAsync();

			var model = new PickPlanViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				Plans = plans,
				HasPendingPurchase = activePlans.Any(x => x.Status == PlanStatus.Pending)
			};

			Message = "We are offering these special, limited time plans for customer willing to pre-paid.";

			return View(nameof(Index), model);
		}

		public async Task<IActionResult> ActivatePlans() {
			var pendingPlanTypeIds = _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending)
												 .Select(p => p.PlanTypeId)
												 .Distinct().ToList();

			foreach (var planTypeId in pendingPlanTypeIds) {
				var pendingPlans = await _context.Plans
					.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
					.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
					.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
					.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
					.Include(x => x.UserDevice)
					.Include(x => x.Device)
					.Include(x => x.PortRequest)
					.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending && x.PlanTypeId == planTypeId)
					.ToListAsync();

				// We need to get a number of active plans on this plan type so we can get the correct Line pricing.
				var activePlansOnThisPlanType = await _context.Plans.CountAsync(x => x.ProfileId == CurrentProfile.Id && PlanService.ActiveStatuses.Contains(x.Status) && x.PlanTypeId == pendingPlans.First().PlanTypeId);

				if (CurrentProfile.BillingAddress == null || string.IsNullOrEmpty(CurrentProfile.BillingAddress.Line1)) {
					ErrorMessage = "You do not have a billing address set. Please go to the Billing page and fill out your billing address.";
					return RedirectToAction(nameof(Index));
				}

				// Re confirm that everything is set
				var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active && !x.Archived);
				var allSigned = pendingPlans.All(x => x.AgreementSigned);
				var bypassPlaid = CurrentProfile.ProfileStatus == ProfileStatus.Trusted;

				// If the user has all agreements signed and a valid bank account attached; then process the payment.
				if (allSigned && payAccount != null && payAccount.Active == true || bypassPlaid) {
					// Run auto verify on IDs
					try {
						var identity = await _context.Identities.FirstOrDefaultAsync(x => x.Profile.Id == CurrentProfile.Id);
						if (identity.AuthenticityConfidence > .8) {
							// Check for auto conditions
							var autoVerify = true;
							// First name from ACH is contained in ID name
							if (!identity.FirstName.ToLower().Contains(CurrentProfile.FirstName.ToLower())) {
								autoVerify = false;
							}
							// If last name does not match exactly.
							if (identity.LastName.ToLower() != CurrentProfile.LastName.ToLower()) {
								autoVerify = false;
							}

							if (autoVerify) {
								var user = await GetCurrentAccount();
								CurrentProfile.IDVerifyStatus = IDVerifyStatus.Verified;
								if (!await _userManager.IsInRoleAsync(user, Roles.User)) {
									await _userManager.AddToRoleAsync(user, Roles.User);
									await _userManager.UpdateSecurityStampAsync(user);
								}
								await _context.SaveChangesAsync();
							}
						}
					}
					catch (Exception e) {
						// Don't kill the process on failure.
						ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Auto-ID-Verify");
					}

					// Charge all pending plans for initiation
					// First we will get the appropriate initiation charge from line pricing, then subtract any already paid initiation fee, that will be the fee the customer pays.
					var newTotalActive = activePlansOnThisPlanType + pendingPlans.Count();
					//if (newTotalActive >= 4) {
					//	ErrorMessage = "You already have 4 plans. You cannot purchase more than 4 at this time. Please contact support for an exclusion.";
					//	return RedirectToAction(nameof(Index));
					//}
					var linePricing = PlanService.GetLinePricing(newTotalActive, pendingPlans.First().PlanType);
					if (linePricing == null) {
						ErrorMessage = "There was an error determining pricing for your number of lines. Please contact support.";
						ErrorHandler.Capture(_other.SentryDSN, new Exception($"Could not determine per line pricing for plan: {pendingPlans.First().Id}") {
							Data = {
								{"PlanType", pendingPlans.First().PlanType},
								{"LineCount", newTotalActive}
							}
						}, "Plan-Activate");
						return RedirectToAction(nameof(Index));
					}
					var totalCharge = linePricing.InitiationFee;

					var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);
					// Charge initiation first, we might not be creating the sub so Initiation will always be a single charge.
					// Do we have an existing initiation charge for this customer
					//var pastInitiationForPendingPlans = await _context.Charges.Where(x => x.Description.Contains("Initiation") &&
					//															  x.ProfileId == CurrentProfile.Id &&
					//															  pendingPlans.Select(x => x.Id).Contains(x.InternalObjectId)).ToListAsync();
					//var pastProrateForPendingPlans = await _context.Charges.Where(x => x.Description.Contains("Prorate") &&
					//																   x.ProfileId == CurrentProfile.Id &&
					//																   pendingPlans.Select(x => x.Id).Contains(x.InternalObjectId)).ToListAsync();
					// TODO re-enable Initiation
					// Only charge the difference this time around
					//totalCharge = Math.Max(0, totalCharge - pastInitiationForPendingPlans.Sum(x => x.Amount));
					//var prorateAmount = ProrateService.GetProrateCharge(linePricing,
					//	new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1));
					//var totalProrate = Math.Max(0, prorateAmount - pastProrateForPendingPlans.Sum(x => x.Amount));
					totalCharge = 10_00;
					int totalProrate = 0, prorateAmount = 0;

					if (CurrentProfile.ProfileStatus == ProfileStatus.Provisional) {
						ErrorHandler.Capture(_other.SentryDSN, new Exception($"Provisional account attempted purchase. {CurrentProfile.Id}"), HttpContext);
						ErrorMessage = $"Your account is currently in a Provisional status because you did not or could not link your bank account with our system. Please email support. customerservice@lexvor.com";
						return RedirectToAction(nameof(Index));
					}

					// Check bank account for balance
					if (payAccount.LastBalanceCheck != DateTime.MinValue && payAccount.LastBalanceCheck > DateTime.Now.AddDays(-7)) {
						// TODO LastBalance needs to be normalized as cents
						if ((payAccount.LastBalance * 1000) < totalCharge && _other.EnableBalanceCheckFailures) {
							// No money in account for transaction
							ErrorHandler.Capture(_other.SentryDSN, new Exception($"Bank balance for account was less than charge amount. {CurrentProfile.Id} | Amount: {totalCharge} | Balance: {payAccount.LastBalance}"), HttpContext);
							ErrorMessage = $"Your payment was rejected because our system indicated that you do not have the funds in your account to complete the transaction.";
							return RedirectToAction(nameof(Index));
						}
					}

					if (totalCharge > 0 || totalProrate > 0) {
						// Multiple the prorate for one plan to all the pending plans
						var prorateMessage = totalProrate > 0 ? $"Prorate for partial month (${totalProrate / 100})." : "";
						// TODO re-enable
						// var initiationMessage = totalCharge > 0 ? $"Initiation for {pendingPlans.First().PlanType.Name} plan. " : "";
						var initiationMessage = "Authentication Charge";

						try {
							// TODO
							//await _emailSender.SendEmailAsync(new [] {"itadmin@lexvor.com", "customerservice@lexvor.com", "lexvor@lexvor.com"}, 
							//	$"New Initiation Charge", $"Email: {CurrentUserEmail}. Plan {pendingPlans.First().PlanType.Name}. Initiation: ${(totalCharge + totalProrate) / 100}");
							await _emailSender.SendEmailAsync(new[] { "itadmin@lexvor.com", "customerservice@lexvor.com", "lexvor@lexvor.com" },
								$"New Authentication Charge", $"Email: {CurrentUserEmail}. Plan {pendingPlans.First().PlanType.Name}. Charge: ${(totalCharge + totalProrate) / 100}");
						}
						catch (Exception e) {
							// Don't fail on error
							ErrorHandler.Capture(_other.SentryDSN, new Exception("Failed to send payment notification email", e), HttpContext);
						}

						if (totalCharge + totalProrate > 1500_00) {
							// Charge is over $1500, STOP
							ErrorHandler.Capture(_other.SentryDSN, new Exception($"Attempted EpicPay charge for over $1500 on initiation. {CurrentProfile.Id} | Amount: {totalCharge} | Prorate: {totalProrate}"), HttpContext);
							// TODO
							//ErrorMessage = $"There was an issue when charging your bank account for Initiation. Please email support. customerservice@lexvor.com";
							ErrorMessage = $"There was an issue when charging your bank account for the Authentication Charge. Please email support. customerservice@lexvor.com";
							return RedirectToAction(nameof(Index));
						}

						// Figure any user orders here and add that price.
						var orders = await UserOrderService.GetOrdersForPlans(_context, pendingPlans);
						var orderTotal = orders.Sum(x => x.Total);

						// TODO
						//var (transactionId, chargeError) = await epic.Charge(CurrentProfile, totalCharge + totalProrate + orderTotal, payAccount, _other.EncryptionKey);
						var (transactionId, chargeError) = await epic.Charge(CurrentProfile, totalCharge, payAccount, _other.EncryptionKey);

						if (!string.IsNullOrWhiteSpace(chargeError)) {
							// TODO
							//ErrorMessage = $"There was an issue when charging your bank account for Initiation. {chargeError}";
							ErrorMessage = $"There was an issue when charging your bank account for the Authentication charge. {chargeError}";
							return RedirectToAction(nameof(Index));
						}

						// Send invoice email to user
						await EmailService.SendMembershipEmailAsync(_emailSender, CurrentProfile.FullName, CurrentUserEmail, pendingPlans.First().PlanType.Name, pendingPlans.Count);
						await EmailService.SendNewActivationInvoice(_emailSender, _other, CurrentProfile, CurrentUserEmail, pendingPlans, orders, linePricing, totalCharge, prorateAmount);

						foreach (var pendingPlan in pendingPlans) {
							_context.Add(new Charge() {
								Amount = totalCharge + totalProrate,
								Date = DateTime.UtcNow,
								Description = $"{initiationMessage}{prorateMessage}",
								InvoiceId = transactionId,
								Profile = CurrentProfile,
								Status = ChargeStatus.Charged,
								InternalObjectId = pendingPlan.Id
							});
						}

						// TODO
						//foreach (var order in orders) {
						//	_context.Add(new Charge() {
						//		Amount = order.Total,
						//		Date = DateTime.UtcNow,
						//		Description = $"Accessories Purchase for order id: {order.OrderId}",
						//		InvoiceId = order.OrderId.ToString(),
						//		Profile = CurrentProfile,
						//		Status = ChargeStatus.Charged,
						//		InternalObjectId = order.Id
						//	});
						//}
						// Save our progress, so we don't over charge customers if the initiation goes through but there is another error.
						await _context.SaveChangesAsync();
					}

					// NOTE: One sub id per customer. Plans are all grouped together to get one charge per month per customer.
					// We may not be creating the subscription here. Only create sub when:
					// - If user is wireless only
					// - They did NOT pick a backordered device
					var plansToCreateSubFor = new List<UserPlan>();
					var plansNotReady = new List<UserPlan>();
					foreach (var pendingPlan in pendingPlans) {
						if (pendingPlan.IsWirelessOnly()) {
							plansToCreateSubFor.Add(pendingPlan);
						}

						// And device not backordered
						if (!pendingPlan.IsWirelessOnly()) {
							var hasStock =
								await _context.StockedDevice.AnyAsync(x =>
									x.DeviceId == pendingPlan.Device.Id && x.Available);
							if (hasStock) {
								plansToCreateSubFor.Add(pendingPlan);
							}
						}
					}
					plansNotReady = pendingPlans.Except(plansToCreateSubFor).ToList();
					// -----------------------------------------
					// DO NOT USE pendingPlans PAST THIS POINT
					// -----------------------------------------
					// plansToCreateSubFor and plansNotReady will contain all plans from pendingPlans.

					if (CurrentProfile.BillingDay == -1) {
						// Set billing date, has not been set before. All cycles are on the first.
						CurrentProfile.BillingCycleStart = DateTime.UtcNow.GetNextFirst();
					}

					// If we have plans to create subscriptions for.
					if (plansToCreateSubFor.Any()) {
						var existingPlans = await _context.Plans.Where(x =>
							x.ProfileId == CurrentProfile.Id && !string.IsNullOrWhiteSpace(x.ExternalSubscriptionId)).ToListAsync();

						// Create the customer if not exists 
						if (CurrentProfile.ExternalWirelessCustomerId.IsNull()) {
							try {
								var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
								var accountNumber = await service.CreateCustomer(CurrentProfile.FullName, CurrentProfile.BillingAddress.Line1,
									CurrentProfile.BillingAddress.City, CurrentProfile.BillingAddress.Provence,
									CurrentProfile.BillingAddress.PostalCode);
								if (accountNumber.IsNull()) {
									// Account creation failed, allow the user to continue anyway.
									ErrorHandler.Capture(_other.SentryDSN, new Exception($"There should be a more recent error that describes why Account Number was empty for customer {CurrentProfile.Id}."), area: "Wireless-Customer-Create");
								}

								CurrentProfile.ExternalWirelessCustomerId = accountNumber;
								await _context.SaveChangesAsync();
							}
							catch (Exception e) {
								// If there was a problem creating the customer, log an error but continue. We can create it later, but want the customer to finish purchasing.
								ErrorHandler.Capture(_other.SentryDSN, new Exception($"Could not create the Wireless Customer for {CurrentProfile.Id}", e), HttpContext);
							}
						}

						if (CurrentProfile.IDVerifyStatus == IDVerifyStatus.Verified) {
							plansToCreateSubFor.ForEach(x => {
								x.Status = PlanStatus.Active;
							});
						}
						else {
							plansToCreateSubFor.ForEach(x => {
								x.Status = PlanStatus.Paid;
							});
						}

						//var methodReturn = await PlanService.GetSubscriptionMethod(existingPlans, CurrentProfile.Id);
						//if (!methodReturn.IsSuccess) {
						//	// If there is more than one subid, there is a serious problem. Let the customer finish though.
						//	ErrorHandler.Capture(_other.SentryDSN, new Exception(methodReturn.Error), area: "Existing-Sub-Retreival");
						//}

						//// Get new monthly cost for all current plans
						//var newMonthlyTotal = linePricing.MonthlyCost;
						//// Add any surcharges from the Options
						//// Get all chosen options from the plans
						//var options = plansToCreateSubFor.Select(x => x.UserDevice.ChosenOptions).Where(x => !x.IsNull());
						//// Select all the option ids (include duplicates for multiple lines with same option)
						//var optionIds = options.SelectMany(x => x.Split(',')).ToList();
						//if (optionIds.Any()) {
						//	// get the options from db to find the surcharge and multiply by the occurances in the id list
						//	var dbOptions = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id.ToString())).ToListAsync();
						//	newMonthlyTotal = newMonthlyTotal + dbOptions.Sum(x => x.Surcharge * optionIds.Count(y => y == x.Id.ToString()));
						//}

						//if (newMonthlyTotal > 1_000_000) {
						//	// Charge is over $1000, STOP
						//	ErrorHandler.Capture(_other.SentryDSN, new Exception($"Attempted EpicPay charge for over $1000 on monthly. {CurrentProfile.Id} | Amount: {newMonthlyTotal}"), HttpContext);
						//	ErrorMessage = $"There was an issue when charging your bank account. Please email support. customerservice@lexvor.com";
						//	return RedirectToAction(nameof(Index));
						//}

						//try {
						//	var callbackUrl = Url.Action(nameof(API.PaymentController.PaymentSuccess), "Payment", new {id = payAccount.Id});
						//	var status = await PlanService.CreateSubscriptionForPlan(_other, CurrentProfile, CurrentUserEmail, newMonthlyTotal, epic, payAccount, methodReturn.Method, methodReturn.SubscriptionId, callbackUrl);

						//	// Activate all pending plans. Paid if not ID verified, Active if verified.
						//	// If the payment call failed then the plans are still pending. Everything should be set already from the action navigator so the user can just retry payment.
						//	if (status.IsSuccess) {
						//		PlanService.UpdatePlansAfterSubscriptionCreate(status, CurrentProfile, plansToCreateSubFor);
						//		// NEVER activate the Wireless plan here. Plan activation will always require admin intervention.
						//	} else {
						//		// TODO Track what errors are returned and set for retry or not.
						//		ErrorHandler.Capture(_other.SentryDSN, new Exception(status.Error), HttpContext, "Plan-Activation");
						//		ErrorMessage = "There was an issue when charging your bank account. Please try again later.";
						//		return RedirectToAction(nameof(Index));
						//	}
						//}
						//catch (Exception e) {
						//	ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Activation");
						//	ErrorMessage = "There was an issue when charging your bank account and you may have been charged. Please contact support.";
						//	return RedirectToAction(nameof(Index));
						//}
					}
					// not sub ID here because plans that are not ready did not have a sub created.
					plansNotReady.ForEach(x => x.Status = PlanStatus.DevicePending);

					await _context.SaveChangesAsync();

					// WE MADE IT. PLANS ARE PURCHASED!
				}
				else {
					// If we have an issue, we need to go through the payments flow again
					return RedirectToAction(nameof(PurchaseController.ActionNavigator), PurchaseController.Name,
						new { returnUrl = Url.Action(nameof(HomeController.Index), HomeController.Name) });
				}
			}

			// If we have an issue, we need to go through the payments flow again
			return RedirectToAction(nameof(PurchaseController.ActionNavigator), PurchaseController.Name,
				new { returnUrl = Url.Action(nameof(HomeController.Index), HomeController.Name) });
		}

		public async Task<IActionResult> CancelPendingPlan(string returnUrl = "") {
			var pendings =
				await _context.Plans.Include(x => x.PlanType).Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();
			pendings.ForEach(x => x.Status = PlanStatus.Cancelled);
			await _context.SaveChangesAsync();
			return RedirectToLocal(returnUrl);
		}

		public async Task<IActionResult> Cancel(Guid id) {
			// UserPlanId
			// Cancel the user plan
			// Cancel Wireless
			// Update EpicPay with new amounts/cancel sub

			return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
		}

		public async Task<IActionResult> RenamePlan(Guid planId, string name) {
			// Get current user plan
			var plans = await _context.Plans.FirstAsync(p => p.Id == planId);
			plans.UserGivenName = name;
			await _context.SaveChangesAsync();

			return Json(new { Status = "success" });
		}

		public async Task<IActionResult> UpdateIMEI(Guid userDeviceId, string imei) {
			var userdevice = await _context.UserDevices.FirstAsync(p => p.Id == userDeviceId);
			userdevice.IMEI = imei;
			await _context.SaveChangesAsync();

			return Json(new { Status = "success" });
		}
	}
}
