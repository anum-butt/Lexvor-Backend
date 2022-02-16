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
using Acklann.Plaid;
using Acklann.Plaid.Management;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Payment;
using Lexvor.Extensions;
using UAParser;
using BankAccount = Lexvor.API.Objects.User.BankAccount;
using Environment = Acklann.Plaid.Environment;
using Microsoft.Extensions.Options;
using Lexvor.API.ChargeOver;
using Lexvor.Models.ProfileViewModels;
using Newtonsoft.Json;

namespace Lexvor.Controllers {
	[Authorize(Roles = Roles.Trial)]
	public class PurchaseController : BaseUserController {
		private readonly ConnectionStrings _connStrings;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;
		private readonly string _defaultConn;

		public static string Name => "Purchase";

		public PurchaseController(
			UserManager<ApplicationUser> userManager,
			IEmailSender emailSender,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager) {
			_emailSender = emailSender;
			_connStrings = connStrings.Value;
			_other = other.Value;
			_defaultConn = _connStrings.DefaultConnection;
		}

		/// <summary>
		/// This method will navigate the user to the appropriate page of the payment flow depending on what data exists in their account.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> ActionNavigator(string returnUrl, bool updateAcc = false) {
			if (string.IsNullOrWhiteSpace(returnUrl)) {
				ErrorHandler.Capture(_other.SentryDSN, new Exception($"Return Url is empty and is required to be set in {nameof(PurchaseController.ActionNavigator)}. Referrer: {Request.GetTypedHeaders().Referer}"), HttpContext);
				RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}

			// If the user has a at least one pending plan purchase, redirect to take ach info.
			var pendingPlans = await _context.Plans
				.Include(x => x.PlanType)
				.Include(x => x.UserDevice)
				.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			if (pendingPlans.Count > 0 && !updateAcc) {
				if (pendingPlans.Any(p => p.UserDeviceId.IsNull())) {
					return RedirectToAction(nameof(ChooseDevice), new { returnUrl });
				}

				var pendingOrders = await _context.UserOrders.AnyAsync(x => pendingPlans.Select(y => y.Id).Contains(x.UserPlan.Id));
				if (pendingPlans.First().PlanType.AllowAccessoryPurchase && !pendingOrders) {
					return RedirectToAction(nameof(Accessories), new { returnUrl });
				}

				if (CurrentProfile.BillingAddress == null || string.IsNullOrEmpty(CurrentProfile.BillingAddress.Line1)) {
					return RedirectToAction(nameof(BillingController.BillingProfile), BillingController.Name, new { returnUrl });
				}

				// If the user has not authorized the plans, do so
				if (!pendingPlans.All(x => x.AgreementSigned)) {
					return RedirectToAction(nameof(ACHAuthorization), new { returnUrl });
				}

				// If the user is using Affirm, redirect to the affirm checkout
				if (pendingPlans.Any(x => x.UserDevice.PurchasedWithAffirm)) {
					// If all the affirm plans to purchase do not have a charge id, run Affirm
					if (pendingPlans.Where(x => x.UserDevice.PurchasedWithAffirm).All(x => string.IsNullOrWhiteSpace(x.UserDevice.AffirmChargeId))) {
						return RedirectToAction(nameof(Checkout), new { returnUrl });
					}
					else {
						// Else we have charged for the devices and need to collect bank details.
						var activeBankAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);

						// If the user has no ACH data on file, collect it
						if (activeBankAccount == null) {
							return RedirectToAction(nameof(CollectBankingInformation), new { returnUrl });
						}
					}
				}

				//if chosen Plan Type does not allow striped purchasing
				if (pendingPlans.First().PlanType.AllowStripePurchases == false) {
					// If the user has not authorized the plans, do so
					if (!pendingPlans.All(x => x.AgreementSigned)) {
					}

					var activeBankAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);

					// If the user has no ACH data on file, collect it
					if (activeBankAccount == null) {
						return RedirectToAction(nameof(CollectBankingInformation), new { returnUrl });
					}
				}
				else {
					// check if user alredy paid or not
					if (pendingPlans.First().Status != PlanStatus.Paid) {
						return RedirectToAction(nameof(PaymentsController.StripePayment), PaymentsController.Name, new { returnUrl });
					}

				}

				return RedirectToLocal(returnUrl);

			}
			else if (updateAcc) {
				// If we are updating an account
				return RedirectToAction(nameof(CollectBankingInformation), new { returnUrl });
			}

			// If we got here someone redirected us incorrectly.
			ErrorHandler.Capture(_other.SentryDSN, new Exception($"Incorrect Payments ActionNavigator redirect. Return Url: {returnUrl}. Referrer: {Request.GetTypedHeaders().Referer}"), HttpContext);

			return RedirectToLocal(returnUrl);
		}

		public async Task<IActionResult> Accessories(string returnUrl = "") {
			ViewData["ReturnUrl"] = returnUrl;
			var availableAccessories = _context.Accessories.ToList().GroupBy(x => x.Grouping).ToList();
			var pendingPlans = await _context.Plans
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
				.Include(x => x.UserDevice)
				.Include(x => x.Device)
				.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			return View(new AccessoryPurchaseViewModel() {
				PlanModels = pendingPlans.Select(x => new AccessoryLineViewModel() {
					UserPlan = x,
					AccessoryGroups = availableAccessories.Select(x => new AccessoryGroupViewModel {
						GroupName = x.Key == "screen" ? "Screen Protector" : x.Key == "case" ? "Device Case" : x.Key == "earphones" ? "AirPods" : x.Key,
						Accessories = x.Select(y => new AccessoryItemViewModel() {
							LifetimeWarranty = y.LifetimeWarranty,
							LifetimeWarrantyPrice = y.LifetimeWarrantyPrice,
							Name = y.Name,
							Price = y.Price,
							Selected = false,
							SelectedWarranty = false,
							ImageUrl = y.ImageUrl,
							Id = y.Id,
							Description = y.Description
						}).ToList()
					}).ToList(),
				}).ToList(),
				Profile = CurrentProfile
			});
		}

		/// <summary>
		/// DO NOT DIRECT THE USER TO THIS PAGE. USE THE ACTIONNAVIGATOR.
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> CollectBankingInformation(string returnUrl = "") {
			// NOTE: Here we collect the bank account details for charging and connect the account with Plaid for Identity.
			ViewData["ReturnUrl"] = returnUrl;
			var pendingPlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			// Check requisite conditions and quit of we do not have necessary data.
			if (pendingPlans.Count == 0) {
				ErrorMessage = "There was a problem with starting the purchase process. Please try again.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}

			// Don't filter on active so we can get partials and fill the model
			var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && !x.Archived);
			if (payAccount != null && payAccount.Active) {
				return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
			}

			return View(new BankInformationViewModel() {
				Profile = CurrentProfile,
				BankAccountRoutingNum = payAccount?.RoutingNumber,
				BankName = payAccount?.Bank,
				FirstNameOnAccount = payAccount?.AccountFirstName,
				LastNameOnAccount = payAccount?.AccountLastName,
				PlaidEnv = _other.PlaidEnv,
				PlaidPublicKey = _other.PlaidPublicKey
			});
		}

		[HttpPost]
		public async Task<IActionResult> CollectBankingInformation(BankInformationViewModel model, string returnUrl = "", bool bankAccountMissing = false) {
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

					if (CurrentProfile.ProfileStatus == ProfileStatus.Trusted) {
						payAccount.Active = true;
						payAccount.LastUpdated = DateTime.UtcNow;
						await _context.SaveChangesAsync();
						return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
					}

					// If the customer's bank account is not supported by Plaid, mark the profile status as Provisional
					if (bankAccountMissing) {
						payAccount.Profile.ProfileStatus = ProfileStatus.Provisional;
						payAccount.Active = true;
						payAccount.LastUpdated = DateTime.UtcNow;
						await _context.SaveChangesAsync();
						return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
					}

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

						// Attempt ID Verify
						//try {
						//	var idDoc = await _context.IdentityDocuments.FirstOrDefaultAsync(x => x.Profile == CurrentProfile);
						//	var docBytes = await BlobService.DownloadBlobBytes(idDoc.DocumentUrl, _other, true);

						//	var callback = string.Format("{0}://{1}{2}", Request.Scheme,
						//		Request.Host, Url.Action(nameof(API.ProfileController.IDCallback), "Profile", new { id = idDoc.Id }));

						//	await IDVerifyService.RunVerificationAsync(_context, _other, idDoc, docBytes, CurrentProfile, callback);
						//}
						//catch (Exception e) {
						//	ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Check-After-Plaid");
						//	// Just log the error and continue. We have all we need to re-trigger if needed.
						//}

						await _context.SaveChangesAsync();

						return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
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


		/// <summary>
		/// DO NOT DIRECT THE USER TO THIS PAGE. USE THE ACTIONNAVIGATOR.
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> Checkout(string returnUrl) {
			// this is the Affirm checkout page.
			ViewData["ReturnUrl"] = returnUrl;
			var pendingPlans = await _context.Plans
				.Include(x => x.UserDevice)
				.Include(x => x.Device)
				.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			// Check requisite conditions and quit of we do not have necessary data.
			if (pendingPlans.Count == 0) {
				ErrorMessage = "There was a problem with starting the purchase process. Please try again.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}

			await DeviceService.PopulateDeviceOptions(pendingPlans.Select(x => x.UserDevice).ToList(), _context);

			var user = await GetCurrentAccount();

			return View(new AffirmCheckoutViewModel() {
				User = user,
				Profile = CurrentProfile,
				Plans = pendingPlans,
				AffirmPublicKey = _other.AffirmPublicKey,
				AffirmJsUrl = _other.AffirmJsUrl
			});
		}

		[HttpPost]
		public async Task<IActionResult> Checkout(AffirmCheckoutViewModel model, string returnUrl) {
			try {
				// this is the Affirm checkout page.
				ViewData["ReturnUrl"] = returnUrl;

				// Save the token
				await _context.ProfileSettings.AddAsync(new ProfileSetting() {
					Profile = CurrentProfile,
					DateAdded = DateTime.Now,
					SettingName = "AffirmToken",
					SettingValue = model.Token
				});
				await _context.SaveChangesAsync();

				var pendingPlans = await _context.Plans
					.Include(x => x.UserDevice)
					.Include(x => x.Device)
					.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

				if (_other.AffirmPublicKey.IsNull() || _other.AffirmPrivateKey.IsNull() || _other.AffirmApiUrl.IsNull()) {
					var user = await GetCurrentAccount();
					ErrorHandler.Capture(_other.SentryDSN, new Exception($"Missing Affirm Config. {_other.AffirmPublicKey} {_other.AffirmPrivateKey} {_other.AffirmApiUrl}"), HttpContext, "Affirm");
					ErrorMessage = "There was an error when trying to complete the Affirm purchase. Contact support.";
					return View(new AffirmCheckoutViewModel() {
						User = user,
						Profile = CurrentProfile,
						Plans = pendingPlans,
						AffirmPublicKey = _other.AffirmPublicKey,
						AffirmJsUrl = _other.AffirmJsUrl
					});
				}

				await DeviceService.PopulateDeviceOptions(pendingPlans.Select(x => x.UserDevice).ToList(), _context);

				var orderTotal = pendingPlans.Sum(x => x.Device.Price);
				orderTotal += pendingPlans.Sum(x => x.UserDevice.Options?.Sum(y => y.Surcharge) ?? 0);

				// If we have a token, complete affirm checkout
				if (!model.Token.IsNull() && !model.OrderId.IsNull()) {
					var service = new Affirm(_other.AffirmPublicKey, _other.AffirmPrivateKey, _other.AffirmApiUrl);
					string chargeId = "";
					try {
						chargeId = await service.AuthorizeCharge(model.Token, model.OrderId, orderTotal);
						await _context.ProfileSettings.AddAsync(new ProfileSetting() {
							Profile = CurrentProfile,
							DateAdded = DateTime.Now,
							SettingName = "AffirmChargeId",
							SettingValue = chargeId
						});
						await _context.SaveChangesAsync();
					}
					catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Affirm1");
						ErrorMessage = e.Message;
						var user = await GetCurrentAccount();

						return View(new AffirmCheckoutViewModel() {
							User = user,
							Profile = CurrentProfile,
							Plans = pendingPlans,
							AffirmPublicKey = _other.AffirmPublicKey,
							AffirmJsUrl = _other.AffirmJsUrl
						});
					}

					// We have an authorized charge
					try {
						var charge = await service.CaptureCharge(chargeId, model.OrderId);
						if (!charge.transaction_id.IsNull()) {
							await _context.Charges.AddAsync(new Charge() {
								Profile = CurrentProfile,
								Amount = charge.amount,
								ChargeType = ChargeType.AnyOther,
								Date = DateTime.Now,
								Description = "Affirm Purchase",
								InvoiceId = charge.transaction_id,
							});
							// Apply the charge id to the device to track it.
							pendingPlans.ForEach(x => x.UserDevice.AffirmChargeId = charge.transaction_id);

							await _context.SaveChangesAsync();

							return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
						}
						else {
							ErrorHandler.Capture(_other.SentryDSN, new Exception($"Error capturing affirm {JsonConvert.SerializeObject(charge)}"), HttpContext, "Affirm2");
							ErrorMessage = "Capturing your Affirm charge was not successful. Please contact support.";

							var user = await GetCurrentAccount();

							return View(new AffirmCheckoutViewModel() {
								User = user,
								Profile = CurrentProfile,
								Plans = pendingPlans,
								AffirmPublicKey = _other.AffirmPublicKey,
								AffirmJsUrl = _other.AffirmJsUrl
							});
						}
					}
					catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Affirm3");
						ErrorMessage = e.Message;
						var user = await GetCurrentAccount();

						return View(new AffirmCheckoutViewModel() {
							User = user,
							Profile = CurrentProfile,
							Plans = pendingPlans,
							AffirmPublicKey = _other.AffirmPublicKey,
							AffirmJsUrl = _other.AffirmJsUrl
						});
					}
				}
				else {
					ErrorMessage = "There was a problem with starting the purchase process. Please try again.";

					var user = await GetCurrentAccount();

					return View(new AffirmCheckoutViewModel() {
						User = user,
						Profile = CurrentProfile,
						Plans = pendingPlans,
						AffirmPublicKey = _other.AffirmPublicKey,
						AffirmJsUrl = _other.AffirmJsUrl
					});
				}
			}
			catch (Exception ex) {
				ErrorHandler.Capture(_other.SentryDSN, ex, HttpContext, "AffirmCapture");
				ErrorMessage = "There was an error when trying to start the Affirm purchase. Contact support.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}
		}

		[HttpGet]
		public async Task<IActionResult> ChooseDevice(string returnUrl) {

			ViewData["ReturnUrl"] = returnUrl;

			var availableAccessories = _context.Accessories.ToList().GroupBy(x => x.Grouping).ToList();

			var pendingPlans = await _context.Plans
				.Where(x => x.ProfileId == CurrentProfile.Id
							&& x.Status == PlanStatus.Pending
							&& x.UserDeviceId == null).ToListAsync();

			var pendingPlanTypeIds = pendingPlans.Select(pp => pp.PlanTypeId).ToList();

			var pendingPlanType = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.FirstOrDefaultAsync(x =>
					x.DisplayOnPublicPages
					&& !x.Archived
					&& pendingPlanTypeIds.Contains(x.Id));

			var model = new ChooseDeviceViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				SelectedPlanType = pendingPlanType,
				HasPendingPurchase = await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending),
				FirstTimeCustomer = !(await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id)),
				AffirmPublicKey = _other.AffirmPublicKey,
				LinesNumbers = pendingPlans.Count(pp => pp.PlanTypeId == pendingPlanType.Id),
				PlanModels = pendingPlans.Select(x => new AccessoryLineViewModel() {
					UserPlan = x,
					AccessoryGroups = availableAccessories.Select(x => new AccessoryGroupViewModel {
						GroupName = x.Key == "screen" ? "Screen Protector" : x.Key == "case" ? "Device Case" : x.Key == "earphones" ? "AirPods" : x.Key,
						Accessories = x.Select(y => new AccessoryItemViewModel() {
							LifetimeWarranty = y.LifetimeWarranty,
							LifetimeWarrantyPrice = y.LifetimeWarrantyPrice,
							Name = y.Name,
							Price = y.Price,
							Selected = false,
							SelectedWarranty = false,
							ImageUrl = y.ImageUrl,
							Id = y.Id,
							Description = y.Description
						}).ToList()
					}).ToList(),
				}).ToList(),
			};

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> ChooseUpgradeDevice(string returnUrl) {

			ViewData["ReturnUrl"] = returnUrl;

			var availableAccessories = _context.Accessories.ToList().GroupBy(x => x.Grouping).ToList();

			var upgradingPlans = await _context.Plans
				.Where(x => x.ProfileId == CurrentProfile.Id
							&& x.UserDevice.UpgradeAvailable).ToListAsync();

			var pendingPlanTypeIds = upgradingPlans.Select(pp => pp.PlanTypeId).ToList();

			var pendingPlanType = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.FirstOrDefaultAsync(x =>
					x.DisplayOnPublicPages
					&& !x.Archived
					&& pendingPlanTypeIds.Contains(x.Id));

			var model = new ChooseDeviceViewModel() {
				Profile = CurrentProfile,
				DeviceOrderingEnabled = _other.DeviceOrderingEnabled,
				SelectedPlanType = pendingPlanType,
				HasPendingPurchase = await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending),
				FirstTimeCustomer = !(await _context.Plans.AnyAsync(x => x.ProfileId == CurrentProfile.Id)),
				AffirmPublicKey = _other.AffirmPublicKey,
				LinesNumbers = upgradingPlans.Count(pp => pp.PlanTypeId == pendingPlanType.Id),
				PlanModels = upgradingPlans.Select(x => new AccessoryLineViewModel() {
					UserPlan = x,
					AccessoryGroups = availableAccessories.Select(x => new AccessoryGroupViewModel {
						GroupName = x.Key == "screen" ? "Screen Protector" : x.Key == "case" ? "Device Case" : x.Key == "earphones" ? "AirPods" : x.Key,
						Accessories = x.Select(y => new AccessoryItemViewModel() {
							LifetimeWarranty = y.LifetimeWarranty,
							LifetimeWarrantyPrice = y.LifetimeWarrantyPrice,
							Name = y.Name,
							Price = y.Price,
							Selected = false,
							SelectedWarranty = false,
							ImageUrl = y.ImageUrl,
							Id = y.Id,
							Description = y.Description
						}).ToList()
					}).ToList(),
				}).ToList(),
			};

			return View(model);
		}

		/// <summary>
		/// DO NOT DIRECT THE USER TO THIS PAGE. USE THE ACTIONNAVIGATOR.
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> ACHAuthorization(string returnUrl = "") {
			ViewData["ReturnUrl"] = returnUrl;
			var pendingPlans = await _context.Plans
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
				.Include(x => x.UserDevice)
				.Include(x => x.Device)
				.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			// Check requisite conditions and quit of we do not have necessary data.
			if (pendingPlans.Count == 0) {
				ErrorMessage = "You do not have any plans pending purchase. Please try again.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}
			// Check condition that got us here in the first place and continue down the line if it is met.
			if (pendingPlans.All(x => x.AgreementSigned)) {
				return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
			}

			var statuses = new[] { PlanStatus.Active, PlanStatus.Paid, PlanStatus.PaymentHold, PlanStatus.OnHold, PlanStatus.Pending };
			var currentPlanCount = await _context.Plans
				.CountAsync(x => x.ProfileId == CurrentProfile.Id && statuses.Contains(x.Status));

			var planType = pendingPlans.First().PlanType;
			var monthly = 0;
			var totalInitiation = 0;
			// Hard limit on four plans at once
			var linePricing = Lexvor.API.Services.PlanService.GetLinePricing(currentPlanCount > 4 ? 4 : currentPlanCount, planType);
			if (linePricing != null) {
				monthly = linePricing.MonthlyCost;
				totalInitiation = linePricing.InitiationFee;
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

			if (monthly == 0) {
				ErrorMessage =
					"There was an error calculating the potential monthly charge. Please cancel this purchase (link at bottom of page), and start over.";
				ErrorHandler.Capture(_other.SentryDSN, new Exception($"One of the line pricing objects was empty for the line count selected for plan {pendingPlans.First().Id}") {
					Data = {
					{"PlanType", planType},
					{"LineCount",currentPlanCount}
				}
				}, "ACH-Agreement");
			}

			var usingAffirm = pendingPlans.Any(x => x.UserDevice.PurchasedWithAffirm);
			var totalDevice = pendingPlans.Sum(x => x.Device?.Price);
			await DeviceService.PopulateDeviceOptions(pendingPlans.Select(x => x.UserDevice).ToList(), _context);
			totalDevice += pendingPlans.Select(x => x.UserDevice).Sum(x => x.Options.Sum(y => y.Surcharge));

			return View(new ACHAuthorizationModel() {
				Profile = CurrentProfile,
				User = await GetCurrentAccount(),
				UserPlans = pendingPlans,
				ReturnUrl = returnUrl,
				TotalMonthly = monthly,
				TotalInitiation = totalInitiation,
				UsingAffirm = usingAffirm,
				TotalDeviceCost = totalDevice ?? 0
			});
		}

		[HttpPost]
		public async Task<IActionResult> ACHAuthorization(ACHAuthorizationModel model, string returnUrl = "") {
			var pendingPlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending && !x.AgreementSigned).ToListAsync();

			// Save the authorization
			var parser = Parser.GetDefault();
			var ua = parser.Parse(Request.Headers["User-Agent"].ToString());

			var agreements = pendingPlans.Select(pendingPlan => new ACHAuthorizationAgreement() {
				Id = Guid.NewGuid(),
				ProfileId = CurrentProfile.Id,
				IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
				Timestamp = DateTime.UtcNow,
				Browser = ua.UA.Family,
				UserAgent = Request.Headers["User-Agent"].ToString(),
				Device = ua.Device.Family == "Other" ? ua.OS.Family : ua.Device.Family,
				Profile = CurrentProfile,
				Plan = pendingPlan
			}).ToList();

			await _context.ACHAuthorizationAgreements.AddRangeAsync(agreements);

			pendingPlans.ForEach(pendingPlan => pendingPlan.AgreementSigned = true);

			// Update name from the authorization
			CurrentProfile.FirstName = model.FirstName;
			CurrentProfile.LastName = model.LastName;

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
		}


		[HttpGet]
		public async Task<IActionResult> PlanPurchaseConfirm(string planTypeId, string promo = "") {
			var planId = Guid.Parse(planTypeId);
			var planType = await _context.PlanTypes.FirstOrDefaultAsync(x => x.Id == planId && !x.Archived);

			if (planType == null) {
				ErrorMessage = "The plan type that you selected is no longer available.";
				return RedirectToAction(nameof(HomeController.Index), "Home");
			}

			var model = new UserPaymentContextViewModel() {
				PlanTypeId = planId,
				PlanType = planType,
				PromoCode = promo
			};

			if (!string.IsNullOrWhiteSpace(promo)) {
				var discount = await _context.DiscountCodes.FirstOrDefaultAsync(d =>
					d.Code.ToUpper().Trim() == promo.ToUpper().Trim() && d.PlanTypeId == planId && d.StartDate <= DateTime.UtcNow &&
					d.EndDate >= DateTime.UtcNow);

				if (discount != null) {
					model.InitiationAdjusted = Convert.ToInt32(discount.NewInitiationFee);
					model.MonthlyAdjusted = Convert.ToInt32(discount.NewMonthlyCost);
					model.AppliedPromo = discount;
				}
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> PlanPurchaseConfirm(UserPaymentContextViewModel model) {
			//if (CurrentProfile.CustomerType) {
			//    ErrorMessage = "You do not have access to that page.";
			//    return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			//}

			// Cancel any in progress plans for user
			var plan = await _context.Plans.Include(x => x.PlanType).FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending);
			if (plan != null) {
				// TODO cancel the chargeover plan with this
				plan.Status = PlanStatus.Cancelled;
				await _context.SaveChangesAsync();
			}

			// Configure ChargeOver API service
			var service = new ChargeOverAPIService(_other.ChargeOverUser, _other.ChargeOverPassword, _other);
			// TODO this is shit
			if (string.IsNullOrEmpty(CurrentProfile.ExternalCustomerId) || CurrentProfile.ExternalCustomerId == "0") {
				try {
					var customerId = service.GetCustomer(CurrentProfile, CurrentUserEmail);
					CurrentProfile.ExternalCustomerId = customerId.ToString();
					// Save changes so far
					await _context.SaveChangesAsync();
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Charge");
					return Json(new { error = true, message = $"There was an error the customer could not be created. You have NOT been charged. Please contact support." });
				}
			}

			var planType = await _context.PlanTypes.FirstAsync(p => p.Id == model.PlanTypeId);

			// No promo, default cost
			model.InitiationAdjusted = Convert.ToInt32(planType.InitiationFee);
			model.MonthlyAdjusted = Convert.ToInt32(planType.MonthlyCost);

			if (!string.IsNullOrWhiteSpace(model.PromoCode)) {
				var discount = _context.DiscountCodes.FirstOrDefault(d =>
					d.Code.ToUpper().Trim() == model.PromoCode.ToUpper().Trim() && d.PlanTypeId == model.PlanTypeId && d.StartDate <= DateTime.UtcNow &&
					d.EndDate >= DateTime.UtcNow);

				if (discount != null) {
					model.InitiationAdjusted = Convert.ToInt32(discount.NewInitiationFee);
					model.MonthlyAdjusted = Convert.ToInt32(discount.NewMonthlyCost);
					model.AppliedPromo = discount;
				}
			}

			// Make the subscription
			try {
				var subId = service.CreateSubscription(CurrentProfile, planType, model.InitiationAdjusted, model.MonthlyAdjusted, Convert.ToInt32(CurrentProfile.ExternalCustomerId));
				// Make the User Plan
				var userPlan = new UserPlan() {
					PlanTypeId = planType.Id,
					LastModified = DateTime.UtcNow,
					ProfileId = CurrentProfile.Id,
					ExternalSubscriptionId = subId.ToString(),
					UserGivenName = $"{planType.Name} Plan {RandomString(2)}",
					Initiation = model.InitiationAdjusted,
					Monthly = model.MonthlyAdjusted,
					Status = PlanStatus.Pending,
					PromoApplied = model.AppliedPromo?.Code
				};

				_context.Plans.Add(userPlan);
				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "PurchaseController");
				throw e;
			}

			return RedirectToAction(nameof(CollectBankingInformation));
		}

		[HttpGet]
		public async Task<IActionResult> SwitchFinanceToPurchase(string returnUrl = "") {
			var pendingPlans = await _context.Plans.Include(x => x.UserDevice).Where(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending).ToListAsync();

			pendingPlans.ForEach(x => {
				if (x.UserDevice != null) {
					x.UserDevice.PurchasedWithAffirm = false;
					x.AgreementSigned = false;
				}
			});

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(ActionNavigator), new { returnUrl });
		}

		[HttpGet]
		public async Task<IActionResult> PurchaseComplete() {
			var plan = await _context.Plans.Include(x => x.PlanType).FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending);

			if (!plan.AgreementSigned) {
				return RedirectToAction(nameof(ProfileController.Agreement), "Profile", new { planId = plan.Id, returnUrl = Url.Action(nameof(PurchaseComplete)) });
			}

			var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);

			return View(new UserPaymentContextViewModel() {
				PayAccount = payAccount,
				UserPlan = plan
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PurchaseComplete(UserPaymentContextViewModel model) {
			var plan = await _context.Plans.Include(x => x.PlanType).FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Status == PlanStatus.Pending);
			plan.Status = PlanStatus.Active;

			var service = new ChargeOverAPIService(_other.ChargeOverUser, _other.ChargeOverPassword, _other);
			var s = service.ConfirmSubscription(Convert.ToInt32(plan.ExternalSubscriptionId));

			if (s != 0) {
				_context.Add(new Charge() {
					Amount = Convert.ToInt32(plan.Initiation),
					Description = $"Initiation for {plan.PlanType.Name} (ACH)",
					Date = DateTime.UtcNow,
					InvoiceId = s.ToString()
				});
				await _context.SaveChangesAsync();

				try {
					await EmailService.SendMembershipEmailAsync(_emailSender, CurrentProfile.FullName, CurrentUserEmail, plan.PlanType.Name,
						1);
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "WelcomeEmailSend");
				}
			}

			return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
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
