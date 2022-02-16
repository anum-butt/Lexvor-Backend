using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Payment;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Controllers;
using Lexvor.Controllers.API;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	public class UsersController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name => "Users";

		public UsersController(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			IEmailSender emailSender,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
			_emailSender = emailSender;
			_signInManager = signInManager;

		}

		[HttpGet]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index() {
			var profiles = DbRaw.Query<UserProfileData>(@"
				select
				  R.Id,
				  R.FirstName,
				  R.LastName,
				  ANU.Email,
				  R.DateJoined,
				  ANU.EmailConfirmed,
				  R.IDVerifyStatus,
				  R.IsArchive,
				  (select count(*)
				  from Plans
				  where Plans.ProfileId = R.Id
				    and Status in (1,4,5)) ActivePlanCount
				from Profiles R
				join AspNetUsers ANU on R.AccountId = ANU.Id			
				", null, _connStrings.DefaultConnection);

			var model = new UserListViewModel() {
				Users = profiles.Where(x => x.IsArchive != true).ToList()
			};

			return View(model);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> PendingIDs() {
			var profiles = await _context.Profiles.Include(x => x.Account).Where(x => x.IDVerifyStatus == IDVerifyStatus.Pending).ToListAsync();

			return View(profiles);
		}

		[HttpGet]
		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Impersonate(Guid id) {
			var profile = await _context.Profiles.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id);
			HttpContext.Session.Set("IMPERSONATE_ID", Encoding.UTF8.GetBytes(id.ToString()));
			HttpContext.Session.Set("IMPERSONATE_EMAIL", Encoding.UTF8.GetBytes(profile.Account.Email));

			return RedirectToAction(nameof(HomeController.Index), HomeController.Name, new { area = "" });
		}
		[HttpGet]
		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpgradePlan(Guid id) {
			var profile = await _context.Profiles.Where(x => x.Id == id).FirstOrDefaultAsync();
			var user = await _context.Users.Where(x => x.Id == profile.AccountId).FirstOrDefaultAsync();

			var userPlans = await _context.Plans.Include(x => x.Profile).Include(x => x.PlanType).Where(x => x.ProfileId == profile.Id).ToListAsync();

			var telispirePlans = new List<UserUpgradePlanDetailsViewModel>();

			foreach (var plan in userPlans) {
				if (!string.IsNullOrEmpty(plan.MDN)) {
					try {
						var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
						var telispirePlan = await service.GetActiveWirelessPlan(plan.MDN);
						var usage = await service.GetUsageForCycle(plan.MDN, DateTime.Now.Year, DateTime.Now.Month);

						var model = new UserUpgradePlanDetailsViewModel() {
							UserName = user.UserName,
							TelispirePlan = telispirePlan.MasterCategory,
							TelispireUsage = usage,
							UserPlan = plan,
							NextBillingDate = telispirePlan.InstallDate
						};

						telispirePlans.Add(model);
					}
					catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "telispire error");
					}
				}
			}
			return View(telispirePlans);
		}
		[HttpGet]
		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangePlan(Guid id, Guid planid) {
			var profile = await _context.Profiles.Include(x => x.Account).Where(x => x.Id == id).FirstOrDefaultAsync();
			var plan = await _context.Plans.Include(x => x.PlanType).Where(x => x.Id == planid).FirstOrDefaultAsync();

			UpgradePlanDetailsViewModel model = null;

			if (!string.IsNullOrEmpty(plan.MDN)) {

				try {
					var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
					var telispirePlan = await service.GetActiveWirelessPlan(plan.MDN);

					model = new UpgradePlanDetailsViewModel() {
						Profile = profile,
						TelispirePlan = telispirePlan.MasterCategory,
						UserPlan = plan
					};
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "telispire error");
				}
			}

			return View(model);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ChangePlan(UpgradePlanDetailsViewModel model) {
			try {
				var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
				await service.ChangePackageOnMDN(model.UserPlan.MDN, model.TelispirePlan, model.NewPlan);

				return RedirectToAction("UpgradePlan", new { id = model.Profile.Id });
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "telispire error");
			}

			return View(model);
		}


		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> StopImpersonate() {
			HttpContext.Session.Remove("IMPERSONATE_ID");
			HttpContext.Session.Remove("IMPERSONATE_EMAIL");

			return RedirectToAction(nameof(HomeController.Index), HomeController.Name, new { area = "" });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> InactiveUsers() {
			var plans = await _context.Plans.Select(p => p.ProfileId).ToListAsync();
			var profiles = DbRaw.Query<Profile>(@"
				select *
				from Profiles
				left outer join plans p on p.ProfileId = profiles.Id
				where p.Id is null
				and IDVerifyStatus != 10
				and DATEDIFF(day, DateJoined, GETDATE()) > 14
				and IDVerifyStatus = 0", null, _connStrings.DefaultConnection);
			var users = await _userManager.Users.ToListAsync();

			var model = profiles.Join(users, profile => profile.AccountId, user => user.Id,
				(profile, user) => new ProfileAccount() { Profile = profile, User = user });

			var archivedUser = await _context.Profiles.Include(profile => profile.Account).Where(x => x.IsArchive == true).ToListAsync();
			ViewData["ArchivedUsers"] = archivedUser;

			return View(model);
		}


		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> DeleteInactiveUsers() {
			DbRaw.Execute(@"
				delete from profiles where id in (
				  SELECT Profiles.Id
				  FROM Profiles
					LEFT OUTER JOIN plans p ON p.ProfileId = profiles.Id
				  WHERE p.Id IS NULL
						and IDVerifyStatus != 10
						AND DATEDIFF(DAY, DateJoined, GETDATE()) > 14
				)", null, _connStrings.DefaultConnection);
			DbRaw.Execute("delete from AspNetUsers where id not in (select accountid from profiles)", null,
				_connStrings.DefaultConnection);


			return RedirectToAction(nameof(InactiveUsers));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ResendConfirmEmail(string email) {
			var user = await _userManager.FindByEmailAsync(email);

			string code = "";
			string callBack = "";
			try {
				code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmCode");
			}
			try {
				callBack = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmCallBack");
			}
			if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(callBack)) {
				try {
					await EmailService.SendEmailConfirmation(_emailSender, _other, user.Email, callBack);
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmSend");
				}
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ResendPhoneVerify(Guid id) {
			var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == id);

			try {
				var callback = string.Format("{0}://{1}{2}", Request.Scheme,
					Request.Host, Url.Action(nameof(CommunicationsController.SMSMessageCallback), "Communications"));

				var message = "Thank you for signing up for Lexvor. Please verify your phone when you login. Verification Code: " + profile.PhoneVerificationCode + ". https://lexvorwireless.com/";
				await TwilioService.Send(_other, _context, callback, profile.Id, profile.Phone,
					message);
			}
			catch (Exception e) {
				// Don't fail on error
				ErrorHandler.Capture(_other.SentryDSN, new Exception("Failed to send user phone validation", e), HttpContext);
				ErrorMessage = "Could not send.";
			}

			Message = "Resent Phone Verification";

			return RedirectToAction(nameof(UserDetails), new { id = id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Lock(string email) {
			var user = await _userManager.FindByEmailAsync(email);
			await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(DateTime.UtcNow.AddYears(100)));
			var profile = await _context.Profiles.FirstAsync(p => p.AccountId == user.Id);
			return RedirectToAction(nameof(UserDetails), new { id = profile.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> TrustUser(Guid id) {
			// ProfileId
			var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == id);
			if (profile.ProfileStatus != ProfileStatus.Trusted) {
				profile.ProfileStatus = ProfileStatus.Trusted;
			}
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(UserDetails), new { id = profile.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Unlock(string email) {
			var user = await _userManager.FindByEmailAsync(email);
			await _userManager.SetLockoutEndDateAsync(user, null);
			await _userManager.SetLockoutEnabledAsync(user, false);
			await _userManager.ResetAccessFailedCountAsync(user);
			var profile = await _context.Profiles.FirstAsync(p => p.AccountId == user.Id);
			return RedirectToAction(nameof(UserDetails), new { id = profile.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id) {
			var profile = await _context.Profiles.FirstAsync(p => p.Id == id);
			var model = new UserEditViewModel() {
				Profile = profile,
				SelectListModel = new SelectListModel()
			};
			return View(model);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(UserEditViewModel model) {
			_context.Attach(model.Profile);
			_context.Entry(model.Profile).Property(x => x.FirstName).IsModified = true;
			_context.Entry(model.Profile).Property(x => x.LastName).IsModified = true;
			_context.Entry(model.Profile).Property(x => x.Phone).IsModified = true;
			// TODO Address Updating
			_context.Entry(model.Profile).Property(x => x.IDVerifyStatus).IsModified = true;

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateExternalWirelessId(Guid id, string returnUrl = "") {
			// ProfileId
			if (id == Guid.Empty) {
				return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
			}

			var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == id);

			return View(profile);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateExternalWirelessId(Profile profile, string returnUrl = "") {
			var userProf = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == profile.Id);

			userProf.ExternalWirelessCustomerId = profile.ExternalWirelessCustomerId ?? "";

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(UserDetails), UsersController.Name, new { id = profile.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> VerifyID(Guid id) {
			if (id == Guid.Empty) {
				ErrorMessage = "There was no user id provided.";
				return RedirectToAction(nameof(Index));
			}

			// ProfileId
			var profile = await _context.Profiles.Include(p => p.Account).Include(x => x.IdentityDocuments).FirstOrDefaultAsync(p => p.Id == id);
			if (profile == null) {
				ErrorMessage = $"There was no profile for the ID you selected ({id})";
				ErrorHandler.Capture(_other.SentryDSN, new Exception($"ProfileId passed did not find a profile {id}"), HttpContext, "VerifyID");
			}

			var vouched = await _context.Identities
				.Include(x => x.Address)
				.Where(x => x.Profile.Id == id && x.Source == IndentitySource.Vouched)
				.OrderByDescending(x => x.LastUpdated).FirstOrDefaultAsync();

			var plaid = await _context.Identities
				.Include(x => x.Address)
				.Where(x => x.Profile.Id == id && x.Source == IndentitySource.Plaid)
				.OrderByDescending(x => x.LastUpdated).FirstOrDefaultAsync();

			var twilio = await _context.Identities
				.Include(x => x.Address)
				.Where(x => x.Profile.Id == id && x.Source == IndentitySource.Twilio)
				.OrderByDescending(x => x.LastUpdated).FirstOrDefaultAsync();

			return View(new VerifyViewModel() {
				Profile = profile,
				VouchedIdentity = vouched,
				PlaidIdentity = plaid,
				TwilioIdentity = twilio
			});
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> VerifyID(VerifyViewModel model) {
			var profile = await _context.Profiles.Include(p => p.Account).FirstAsync(p => p.Id == model.Profile.Id);
			profile.IDVerifyStatus = IDVerifyStatus.Verified;

			if (!await _userManager.IsInRoleAsync(profile.Account, Roles.User)) {
				await _userManager.AddToRoleAsync(profile.Account, Roles.User);
				await _userManager.UpdateSecurityStampAsync(profile.Account);
			}

			await _context.SaveChangesAsync();

			// Mark all paid plans as active
			var plans = await _context.Plans.Where(x => x.ProfileId == profile.Id && x.Status == PlanStatus.Paid)
				.ToListAsync();

			plans.ForEach(x => x.Status = PlanStatus.Active);

			await _context.SaveChangesAsync();

			// Send user email
			await EmailService.SendIdVerifiedEmail(_emailSender, _other, profile.Account.Email, profile.FullName);

			return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> RequestIDUpdate(VerifyViewModel model) {
			var profile = await _context.Profiles.Include(p => p.Account).FirstAsync(p => p.Id == model.Profile.Id);
			profile.IDVerifyStatus = IDVerifyStatus.ReverificationRequired;
			profile.IDVerifyStatusDescription = model.Notes;

			if (await _userManager.IsInRoleAsync(profile.Account, Roles.User)) {
				await _userManager.RemoveFromRoleAsync(profile.Account, Roles.User);
			}

			await _context.SaveChangesAsync();

			// Send user email
			await EmailService.SendIdUpdatesEmail(_emailSender, _other, profile.Account.Email, profile.FullName);

			return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> UserDetails(Guid id, bool showArchived = false, bool showCancelledPlans = false) {
			// ProfileId
			var profile = await _context.Profiles.Include(p => p.Account).Include(x => x.BillingAddress).FirstAsync(p => p.Id == id);
			var plans = _context.Plans
				.Include(p => p.PlanType)
				.Include(p => p.Profile)
				.Include(p => p.Device)
				.Include(p => p.UserDevice)
				.Where(p => p.ProfileId == id);
			if (!showCancelledPlans) {
				plans = plans.Where(x => PlanService.ActivePendingStatuses.Contains(x.Status));
			}

			var planList = await plans.ToListAsync();
			var payQuery = _context.PayAccounts.Where(x => x.ProfileId == id);
			if (!showArchived) {
				payQuery = payQuery.Where(x => !x.Archived).OrderBy(x => x.Active);
			}
			var pay = await payQuery.ToListAsync();

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
			// For each plan, hydrate the options. TODO Wow this sucks ass.
			foreach (var plan in planList) {
				if (plan.UserDevice?.ChosenOptions != null) {
					var ids = plan.UserDevice.ChosenOptions.Split(',').ToList();
					plan.UserDevice.Options = await _context.DeviceOptions.Where(x => ids.Contains(x.Id.ToString())).ToListAsync();
				}

				plan.ThrottleLevel = "None";
				if (!plan.MDN.IsNull() && PlanService.ActiveStatuses.Contains(plan.Status)) {
					double gbs = 0;
					try {
						var usage = await UsageService.GetUsageAggregateForCycle(_context, plan.MDN, DateTime.UtcNow.GetFirst());
						if (usage.KBData > plan.PlanType.FirstThrottle) {
							plan.ThrottleLevel = "3G";
						}
						if (usage.KBData > plan.PlanType.SecondThrottle) {
							plan.ThrottleLevel = "2G";
						}
						// Calculate cost
						plan.MDNCostToDate = WirelessService.CalculatePlanCostToDate(usage, plan.IsPorting);
					}
					catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, "GB Usage");
						plan.ThrottleLevel = "No Data";
					}
				}
			}

			var tradeins = await _context.DeviceIntakes.Where(x => x.ProfileId == profile.Id)
				.OrderByDescending(x => x.Requested).ToListAsync();

			var model = new UserDetailsViewModel() {
				Plans = planList,
				Profile = profile,
				User = profile.Account,
				TradeIns = tradeins,
				PayAccounts = pay
			};

			return View(model);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> AssignDevice(Guid id) {
			// PlanId
			var plan = await _context.Plans.FirstAsync(p => p.Id == id);

			var model = new AssignDeviceViewModel() {
				UserPlan = plan
			};

			ViewBag.Devices = await _context.Devices
				.Where(x => x.PlanTypes.Select(y => y.PlanTypeId).Contains(plan.PlanTypeId)).ToListAsync();

			return View(model);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> AssignDevice(AssignDeviceViewModel model) {
			// PlanId
			// TODO this is terrible we need to fix this
			var plan = await _context.Plans.FirstAsync(p => p.Id == model.UserPlan.Id);
			var profile = await _context.Profiles.Include(p => p.Account).FirstAsync(p => p.Id == plan.ProfileId);
			try {
				var device = model.BYOD ? null : await _context.Devices.FirstAsync(d => d.Id == model.DeviceId);

				await DeviceService.AssignDeviceToUserPlan(_context, _emailSender, new AssignContext() {
					User = profile.Account,
					UserPlan = plan,
					Device = device,
					Carrier = model.Carrier,
					Color = model.Color,
					Requested = model.Requested,
					IMEI = model.IMEI,
					BYOD = model.BYOD
				}, false);
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, new Exception("Could not assign device to user plan.", e), HttpContext, "UserPlan-Device-Assign");
				ErrorMessage = "Could not assign device.";
			}

			return RedirectToAction(nameof(UserDetails), new { id = plan.ProfileId });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> MarkShipped(Guid id, string returnUrl = "") {
			// UserDeviceId
			var userdevice = await _context.UserDevices.FirstOrDefaultAsync(p => p.Id == id);

			// Even if the user is BYOD, we still save the ship date

			// If the user has a device or the user is bringing their own device
			if (userdevice != null || userdevice.BYOD) {
				userdevice.Shipped = DateTime.UtcNow;
				await _context.SaveChangesAsync();
				return LocalDefaultRedirect(returnUrl, nameof(UserDetails), UsersController.Name, new { Id = userdevice.Profile.Id });
			}
			else {
				ErrorMessage = $"Could not mark device {id} as shipped.";
				return LocalDefaultRedirect(returnUrl, nameof(Index), UsersController.Name);
			}
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangeBillDate(Guid id) {
			// PlanId
			var plan = await _context.Plans.Include(p => p.Profile).FirstAsync(p => p.Id == id);

			var model = new ChangeBillDateViewModel() {
				UserPlan = plan,
				Profile = plan.Profile
			};

			return View(model);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Upgrade(Guid id) {
			// PlanId
			var plan = await _context.Plans.Include(p => p.Profile).FirstAsync(p => p.Id == id);

			var model = new UpgradeViewModel() {
				UserPlan = plan
			};

			ViewBag.PlanTypes = await _context.PlanTypes.ToListAsync();

			return View(model);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Upgrade(UpgradeViewModel model) {
			// PlanId
			var plan = await _context.Plans.Include(p => p.Profile).FirstAsync(p => p.Id == model.UserPlan.Id);
			var upgradePlan = await _context.PlanTypes.FirstAsync(p => p.Id == model.UpgradePlanId);

			// Add ads fee if opted out
			var cost = model.Monthly;
			if (model.DisableAds) {
				cost += 500;
			}

			//var planId = Payments.CreatePlan(CurrentProfile.ExternalCustomerId, Convert.ToInt32(cost), upgradePlan.Name);

			try {
				//var subId = Payments.UpgradeSubscription(plan.Profile.ExternalCustomerId, planId, plan.ExternalSubscriptionId);
				//plan.ExternalSubscriptionId = subId;
				plan.PlanTypeId = upgradePlan.Id;
				plan.Monthly = cost;
				plan.LastModified = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "UpgradeSub");
				throw new Exception(
					$"There was an error upgrading the subscription. Profile Id: {plan.ProfileId}. Plan Id: {plan.Id}");
			}

			return RedirectToAction(nameof(UserDetails), new { id = plan.ProfileId });
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangeBillDate(ChangeBillDateViewModel model) {
			// PlanId
			var plan = await _context.Plans.Include(p => p.Profile).FirstAsync(p => p.Id == model.UserPlan.Id);

			//if (plan.ExternalSubscriptionId != null) {
			//	//Payments.ChangeBillingDate(plan.ExternalSubscriptionId, model.NewBillDate);
			//	CurrentProfile.BillingCycleStart = model.NewBillDate;
			//	plan.LastModified = DateTime.UtcNow;
			//	await _context.SaveChangesAsync();
			//}

			return RedirectToAction(nameof(UserDetails), new { id = plan.ProfileId });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Archive(Guid id) {

			bool canArchived = true;

			var profile = await _context.Profiles.FirstAsync(p => p.Id == id);
			var plans = await _context.Plans.Where(p => p.ProfileId == id && (p.Status != PlanStatus.Active || p.Status != PlanStatus.Pending)).ToListAsync();

			if (plans.Count() != 0) {
				var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

				foreach (var plan in plans) {
					var activePlan = await service.GetActiveWirelessPlan(plan.MDN);
					if (activePlan != null) {
						canArchived = false;
					}
				}
			}

			if (canArchived) {
				profile.IsArchive = true;
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Delete(Guid id) {
			var profile = await _context.Profiles.FirstAsync(p => p.Id == id);
			var user = await _userManager.FindByIdAsync(profile.AccountId);
			var plans = await _context.Plans.Where(p => p.ProfileId == id).ToListAsync();
			var pay = await _context.PayAccounts.Where(x => x.ProfileId == profile.Id).ToListAsync();
			var identities = await _context.Identities.Where(x => x.Profile.Id == profile.Id).ToListAsync();

			// If they have active plans, or have agreed to the agreement, we cannot delete.
			if (plans.Any(x => x.AgreementSigned || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid)) {
				ErrorMessage = "You cannot delete a user with active plans";
				return RedirectToAction(nameof(Index));
			}

			// If they have active pay accounts, we cannot delete.
			if (pay.Any(x => x.Active)) {
				ErrorMessage = "You cannot delete a user with active bank accounts.";
				return RedirectToAction(nameof(Index));
			}

			if (profile.IDVerifyStatus == IDVerifyStatus.Verified) {
				ErrorMessage = "You cannot delete a user with a verified identity.";
				return RedirectToAction(nameof(Index));
			}

			// Delete user plans
			_context.RemoveRange(plans);

			// Delete user profile
			_context.Remove(profile);

			await _context.SaveChangesAsync();

			// Delete user
			await _userManager.DeleteAsync(user);

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> VerifyAddress(Guid id) {
			var profile = await _context.Profiles.Include(p => p.Account).Include(x => x.BillingAddress).FirstAsync(p => p.Id == id);

			try {
				var verfiedAddress = await AddressVerificationService.Verify(profile.BillingAddress, _other.USPSWebtoolUserID);

				if (profile.BillingAddress.Line1 != verfiedAddress.Line1) {
					Message = $"User's address is {profile.BillingAddress.GetAddressBlock()} but verifier found {verfiedAddress.GetAddressBlock()}. Consider updating user's address. <a href={Url.Action(nameof(UsersController.UpdateAddress), "Users", new { id })}>Update it by searching on google maps</a>";
				}
				else {
					Message = "User's address is verified.";
				}
			}
			catch (Exception e) {
				ErrorMessage = $"An exception occured while verifying the user's address: {e.Message} <a href={Url.Action(nameof(UsersController.UpdateAddress), "Users", new { id })}>Update it by searching on google maps</a>";
			}

			return RedirectToAction(nameof(UserDetails), new { id });
		}
		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> PastAddresses(Guid id) {

			var addresses = await _context.Addresses.Where(x => x.ProfileId == id).OrderByDescending(x => x.LastUpdated).ToListAsync();
			return View(addresses);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateAddress(Guid id) {
			var profile = await _context.Profiles.Include(p => p.Account).Include(x => x.BillingAddress).FirstAsync(p => p.Id == id);

			try {
				var foundAddress = await GoogleMapsService.AddressLookup(profile.BillingAddress, _other.GoogleApiKey);

				if (foundAddress != null) {
					profile.BillingAddress.Line1 = foundAddress.Line1.ToUpper();
					profile.BillingAddress.City = foundAddress.City.ToUpper();
					profile.BillingAddress.Provence = foundAddress.Provence;
					profile.BillingAddress.PostalCode = foundAddress.PostalCode;
					profile.BillingAddress.LastUpdated = DateTime.UtcNow;
					profile.BillingAddress.Source = AddressSource.GoogleMaps;

					await _context.SaveChangesAsync();

					Message = $"User's address was updated to {foundAddress.GetAddressBlock()}.";
				}
				else {
					ErrorMessage = $"Google maps did not find an address. User's address was not updated.";
				}

			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "UpdateAddress");
				ErrorMessage = $"An exception occured during Google maps address lookup: {e.Message}";
			}

			return RedirectToAction(nameof(UserDetails), new { id });
		}

		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Charges(Guid id) {
			var charges = _context.Charges.Where(x => x.ProfileId == id).OrderByDescending(x => x.Date);
			var list = charges.Count() > 0 ? await charges.ToListAsync() : new List<Charge>();
			return View(list);
		}

		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ConfirmBankAccount(Guid id) {
			var pay = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == id);
			pay.Confirmed = !pay.Confirmed;
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(UserDetails), Name, new { id = pay.ProfileId });
		}

		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ArchiveBankAccount(Guid id) {
			var pay = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == id);
			pay.Archived = true;
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(UserDetails), Name, new { id = pay.ProfileId });
		}

		[HttpGet]
		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> GetNameFromPrimaryPhone(Guid id) {
			var profile = await _context.Profiles.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id);

			var identity = await TwilioService.GetNameFromNumber(_other, profile.Phone);

			if (identity == null) {
				Message = "Could not get a name from the carrier, they will not provide it for this number. Do not attempt this verification again for this customer";
			}
			else {
				identity.Profile = profile;
				await _context.AddAsync(identity);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(UsersController.VerifyID), UsersController.Name, new { id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChargePayAccount(Guid profileId, Guid payAccountId) {

			var model = new ChargePayAccountViewModel {
				ProfileId = profileId,
				PayAccountId = payAccountId
			};

			return View(model);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChargePayAccount(ChargePayAccountViewModel model) {
			if (!ModelState.IsValid) {
				return View(model);
			}

			var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == model.ProfileId);
			var payAcc = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == model.PayAccountId);

			var chargeInCents = (int)(model.ChargeAmount * 100);
			var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);

			var (transactionId, chargeError) = await epic.Charge(CurrentProfile, chargeInCents, payAcc, _other.EncryptionKey);

			if (!string.IsNullOrWhiteSpace(chargeError)) {
				ErrorMessage = $"There was an issue when charging the user. {chargeError}";
				return RedirectToAction(nameof(UserDetails), Name, new { id = model.ProfileId });
			}
			else {
				Message = $"Successfully charged {profile.FullName} ${model.ChargeAmount}";
			}

			var charge = new Charge {
				Profile = profile,
				ProfileId = profile.Id,
				InvoiceId = transactionId,
				Amount = chargeInCents,
				Description = model.ChargeDescription,
				Date = DateTime.UtcNow,
				Status = ChargeStatus.Charged,
			};

			_context.Add(charge);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(UserDetails), Name, new { id = model.ProfileId });
		}


		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> HistoricalDataUsageReport() {

			var plans = await _context.Plans
					.Include(p => p.PlanType)
					.Include(p => p.Profile)
					.Include(p => p.Device)
					.Include(p => p.Profile.Account)
					.Include(p => p.UserDevice).Where(x => x.Status != PlanStatus.Cancelled).ToListAsync();

			var report = new List<DataUsageReportViewModel>();

			foreach (var plan in plans) {
				var usage = await UsageService.GetUsageAggregateForCycle(_context, plan.MDN, DateTime.UtcNow.GetFirst());
				var cost = WirelessService.CalculatePlanCostToDate(usage, plan.IsPorting);

				var email = plan.Profile.Account.Email;
				if (plan.Profile.Account.Email.Length > 65) {
					email = plan.Profile.Account.Email.Substring(0, 65);
				}

				var repo = new DataUsageReportViewModel {
					Id = plan.ProfileId,
					Name = plan.Profile.FullName,
					Email = email,
					Phone = plan.Profile.Phone,
					PlanName = plan.PlanType.Name,
					PlanStatus = plan.Status,
					Revenue = Math.Round((plan.Monthly / 100) - plan.MDNCostToDate, 2),
					DataUsage = usage.KBData,
					DataCost = Math.Round(cost, 2),

				};
				report.Add(repo);
			}
			return View(report);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ACHRejectsResolution() {

			var rejectedAchList = new List<ACHRejectsResolution>();

			try {

				var service = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass, _other.EpicPayReportingUrl);

				var start = DateTime.UtcNow.AddDays(-30);

				var end = DateTime.UtcNow;
				var response = await service.GetRejectedACH(start, end);
				var dbPayAccounts = _context.PayAccounts.Include(x => x.Profile).ThenInclude(x => x.Account).Where(x => x.Active == true).ToList();

				foreach (var res in response) {
					string accountHolder = res.accountholder;
					var filteredPayAccount = dbPayAccounts.Where(x => x.AccountMask.Substring(x.AccountMask.Length - 4, 4) == res.account_no).FirstOrDefault();
					// We have a matching account for the ACH reject
					if (filteredPayAccount != null) {
						var charge = _context.Charges.Where(x => x.ProfileId == filteredPayAccount.ProfileId && x.Status == ChargeStatus.Rejected).FirstOrDefault();
						// We have a matching charge
						if (charge != null) {
							rejectedAchList.Add(new ACHRejectsResolution {
								Balance = filteredPayAccount.LastBalance,
								LastBalanceCheck = filteredPayAccount.LastBalanceCheck,
								MaskedAccount = filteredPayAccount.MaskedAccountNumber,
								ProfileId = filteredPayAccount.ProfileId,
								Amount = charge.Amount,
								ChargeDate = res.reported_date,
								UserEmail = filteredPayAccount.Profile.Account.Email,
								Charge = charge,
								PayAccountId = filteredPayAccount.Id,
								ChargeType = charge.ChargeType
							});
						} else {
							// No matching charge, create one before saving the ach reject
							var newCharge = _context.Add(new Charge() { 
								Amount = Convert.ToInt32(res.Amount * 100),
								ChargeType = ChargeType.AnyOther,
								Date = res.effective_date ?? new DateTime(),
								Description = "Rejected Payment",
								FirstName = filteredPayAccount.AccountFirstName,
								LastName = filteredPayAccount.AccountLastName,
								InvoiceId = res.trxn_id,
								NeedsAttention = true,
								ProfileId = filteredPayAccount.ProfileId,
								Status = ChargeStatus.Rejected
							});
							await _context.SaveChangesAsync();
							rejectedAchList.Add(new ACHRejectsResolution {
								Balance = filteredPayAccount.LastBalance,
								LastBalanceCheck = filteredPayAccount.LastBalanceCheck,
								MaskedAccount = filteredPayAccount.MaskedAccountNumber,
								ProfileId = filteredPayAccount.ProfileId,
								Amount = newCharge.Entity.Amount,
								ChargeDate = res.reported_date,
								UserEmail = filteredPayAccount.Profile.Account.Email,
								Charge = newCharge.Entity,
								PayAccountId = filteredPayAccount.Id,
								ChargeType = newCharge.Entity.ChargeType
							});
						}
					}
				}

				return View(rejectedAchList);
			}
			catch(Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, "ACH Rejects Resloution Page");
				return View(rejectedAchList);
			}
			}


		[HttpPost]
			public async Task<bool> SetNeedsAttention(Guid chargeId) {
				try {
					var charge = _context.Charges.Where(x => x.Id == chargeId).FirstOrDefault();
					charge.NeedsAttention = false;
					_context.Update(charge);
					await _context.SaveChangesAsync();
					return true;
				}
				catch (Exception ex) {
					ErrorHandler.Capture(_other.SentryDSN, ex, HttpContext, "Setting Needs Attention");
					return false;
				}
			}

			[HttpPost]
			[Route("[action]")]
			[Authorize(Roles = Roles.Admin)]

			public async Task<bool> ChargeRejectedACH(ChargePayAccountViewModel model, Guid chargeId) {
				var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == model.ProfileId);
				var payAcc = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == model.PayAccountId);

				var chargeInCents = (int)(model.ChargeAmount * 100);
				try {
					var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);

					var (transactionId, chargeError) = await epic.Charge(CurrentProfile, chargeInCents, payAcc, _other.EncryptionKey);

					if (!string.IsNullOrWhiteSpace(chargeError)) {
						ErrorMessage = $"There was an issue when charging the user. {chargeError}";

						return false;
					}

					var charge = await _context.Charges.FirstOrDefaultAsync(x => x.Id == chargeId);

					charge.InvoiceId = transactionId;
					charge.Status = ChargeStatus.Charged;
					charge.ChargeType = ChargeType.Bill;
					charge.NeedsAttention = false;
					_context.Update(charge);
					await _context.SaveChangesAsync();
					return true;
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, "Charging Rejected ACH");
					return false;
				}
			}
			[HttpPost]
			[Route("[action]")]
			[Authorize(Roles = Roles.Admin)]
			public async Task<bool> UpdateBalance(Guid paymentId) {
				var plaid = new Plaid(_other.PlaidSecret, _other.PlaidClientId, _other.PlaidEnv);
				var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == paymentId);


				if (payAccount == null) {
					return false;
				}

				var accessToken = await _context.ProfileSettings.FirstOrDefaultAsync(x => x.ProfileId == payAccount.ProfileId && x.SettingName == "plaid_access");

				if (accessToken != null) {
					try {
						var bal = await plaid.GetLastBalance(accessToken.SettingValue, payAccount.ExternalReferenceNumber);
						payAccount.LastBalance = (double)bal;
						payAccount.LastBalanceCheck = DateTime.Now;
						await _context.SaveChangesAsync();
						return true;
					}
					catch (Exception e) {
						// Balance check errors should be logged, but not stop the user.
						ErrorHandler.Capture(_other.SentryDSN, e, "Could not gather balance info.Contact the customer and have them update the connection");
						return false;
					}
				}

				return false;

			}
		}
	}
