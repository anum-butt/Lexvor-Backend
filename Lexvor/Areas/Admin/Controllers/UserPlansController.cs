using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Payment;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Controllers;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Newtonsoft.Json;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	public class UserPlansController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name => "UserPlans";

		public UserPlansController(
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
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Activations() {
			var plans = await _context.Plans
				.Include(x => x.Profile)
				.Include(x => x.PlanType)
				.Include(x => x.Device)
				.Where(x => x.AgreementSigned && (x.Status == PlanStatus.DevicePending || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid) && x.WirelessStatus == WirelessStatus.NoPlan).ToListAsync();

			return View(plans);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> PendingPorts() {
			var plans = await _context.Plans
				.Include(x => x.Profile)
				.Include(x => x.PlanType)
				.Include(x => x.Device)
				.Where(x => x.AgreementSigned && (x.Status == PlanStatus.DevicePending || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid)
											  && (x.PortRequest != null && (x.PortRequest.Status == PortStatus.Ready || x.PortRequest.Status == PortStatus.Pending))).ToListAsync();

			return View(plans);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> MdnsPending() {
			var plans = await _context.Plans
				.Include(x => x.Profile)
				.Include(x => x.PlanType)
				.Include(x => x.Device)
				.Where(x => string.IsNullOrWhiteSpace(x.MDN) && !string.IsNullOrEmpty(x.AssignedSIMICC)).ToListAsync();

			return View(plans);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Communications(Guid id) {
			var comms = await _context.UserComms.Where(x => x.ProfileId == id).ToListAsync();

			return View(comms);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> CancelPlan(Guid userPlanId) {

			var plan = await _context.Plans.Where(x => x.Id == userPlanId).FirstAsync();
			plan.LastModified = DateTime.Now;
			plan.Status = PlanStatus.Suspend;

			await _context.SaveChangesAsync();

			try {

				if (!string.IsNullOrEmpty(plan.ExternalSubscriptionId)) {
					var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);
					//suspend subscription
					bool deleted = await epic.DeleteSubscription(plan.ExternalSubscriptionId);
				}

				if (!string.IsNullOrEmpty(plan.MDN)) {
					var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
					bool disconected = await service.DisconnectMDN(plan.MDN);
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Cancel plan");
			}

			return RedirectToAction(nameof(UsersController.UserDetails), UsersController.Name,
				new { id = plan.ProfileId });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> SuspendPlan(Guid userPlanId) {

			var plan = await _context.Plans.Where(x => x.Id == userPlanId).FirstAsync();
			plan.LastModified = DateTime.Now;
			plan.Status = PlanStatus.Suspend;

			await _context.SaveChangesAsync();

			try {

				if (!string.IsNullOrEmpty(plan.ExternalSubscriptionId)) {
					var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);
					//suspend subscription
					bool deleted = await epic.SuspendSubscription(plan.ExternalSubscriptionId);
				}

				if (!string.IsNullOrEmpty(plan.MDN)) {
					var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
					bool disconected = await service.SuspendMDN(plan.MDN);
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "suspend plan");
			}

			return RedirectToAction(nameof(UsersController.UserDetails), UsersController.Name,
				new { id = plan.ProfileId });
		}

		[HttpGet]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index(Guid userPlanId, bool bypass = false) {
			if (userPlanId == Guid.Empty) {
				return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
			}

			ViewBag.PortStatusList = PortStatusList();
			var userPlan = _context.Plans
				.Include(x => x.Profile).ThenInclude(x => x.BillingAddress)
				.Include(x => x.PlanType)
				.Include(x => x.Profile.Account)
				.Include(x => x.Device)
				.Include(x => x.PortRequest)
				.Include(x => x.UserDevice).FirstOrDefault(x => x.Id == userPlanId);

			var confirmed = await _context.PayAccounts.AnyAsync(x => x.ProfileId == userPlan.ProfileId && x.Confirmed);

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
			var usage = new UsageDay();
			if (!userPlan.MDN.IsNull()) {
				usage = await UsageService.GetUsageAggregateForCycle(_context, userPlan.MDN, DateTime.UtcNow.GetFirst());
			}

			var order = await _context.UserOrders
				.Include(x => x.Accessory1)
				.Include(x => x.Accessory2)
				.Include(x => x.Accessory3)
				.FirstOrDefaultAsync(x => x.UserPlanId == userPlanId);
			
			return View(new UserPlanDetailsViewModel() {
				UserPlan = userPlan,
				ConfirmedPayAccount = confirmed,
				UsageDay = usage,
				Order = order
			});
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangePlanType(Guid userPlanId) {
			if (userPlanId == Guid.Empty) {
				return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
			}

			var userPlan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == userPlanId);

			ViewBag.PlanTypes = await _context.PlanTypes.Where(x => !x.Archived).ToListAsync();

			return View(new ChangePlanTypeViewModel() {
				UserPlan = userPlan
			});
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangePlanType(ChangePlanTypeViewModel model) {
			var plan = _context.Plans.First(x => x.Id == model.UserPlan.Id);

			plan.PlanTypeId = model.NewPlanTypeId;

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index), new { userPlanId = plan.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateIMEI(Guid userDeviceId, string returnUrl = "") {
			if (userDeviceId == Guid.Empty) {
				return RedirectToAction(nameof(DashboardController.Index), DashboardController.Name);
			}

			var device = await _context.UserDevices.FirstOrDefaultAsync(x => x.Id == userDeviceId);

			return View(device);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateIMEI(UserDevice device, string returnUrl = "") {
			var userDevice = await _context.UserDevices.FirstOrDefaultAsync(x => x.Id == device.Id);

			userDevice.IMEI = device.IMEI ?? "";

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index), UserPlansController.Name, new { userPlanId = userDevice.PlanId });
		}

		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> GetMDN(Guid id, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == id);

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			if (plan != null && !plan.AssignedSIMICC.IsNull()) {
				try {
					var mdn = await service.GetMDNFromICC(plan.AssignedSIMICC);
					plan.MDN = StaticUtils.NumericStrip(mdn);
					await _context.SaveChangesAsync();
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Get-MDN-From-ICC");
				}
			}

			return LocalDefaultRedirect(returnUrl, nameof(Index), UserPlansController.Name, new { userPlanId = plan.Id });
		}

		[HttpGet]
		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> GenerateCSVDataReport(Guid id, string name, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == id);
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			if (plan != null && !plan.MDN.IsNull()) {
				try {
					var memStream = new MemoryStream();
					using (StreamWriter writer = new StreamWriter(memStream)) {
						writer.WriteLine("Day,Data Usage in GB");
						var futureDate = DateTime.UtcNow;
						for (DateTime currentDate = DateTime.UtcNow.GetFirst(); currentDate < futureDate; currentDate = currentDate.AddDays(1.0)) {
							double gbs = 0;
							gbs = await service.GetGBUsage(plan.MDN, currentDate, currentDate);
							writer.WriteLine(currentDate + "," + gbs);
						}
						writer.Flush();

						return File(memStream.ToArray(), "text/csv", name + "_DataReport.csv");
					}
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, $"Failed to get GB Usage for {plan.MDN}");
				}
			}

			return LocalDefaultRedirect(returnUrl, nameof(Index), UserPlansController.Name, new { userPlanId = plan.Id });
		}

		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> ChangePortStatus(Guid id, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans
				.Include(x => x.PortRequest)
				.Include(x => x.Profile)
					.ThenInclude(x => x.BillingAddress)
				.FirstOrDefaultAsync(x => x.Id == id);

			plan.IsPorting = !plan.IsPorting;

			if (plan.PortRequest == null && plan.IsPorting) {
				plan.PortRequest = new PortRequest {
					Plan = plan,
					MDN = plan.MDN,
					FirstName = plan.Profile.FirstName,
					LastName = plan.Profile.LastName,
					AddressLine1 = plan.Profile.BillingAddress.Line1,
					AddressLine2 = plan.Profile.BillingAddress.Line2,
					City = plan.Profile.BillingAddress.City,
					Zip = plan.Profile.BillingAddress.PostalCode,
					CanBeSubmitted = !string.IsNullOrWhiteSpace(plan.AssignedSIMICC),
					Status = PortStatus.Ready
				};
			}
			else {
				_context.PortRequests.Remove(plan.PortRequest);
			}

			await _context.SaveChangesAsync();

			return LocalDefaultRedirect(returnUrl, nameof(Index), UserPlansController.Name, new { userPlanId = plan.Id });
		}

		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> CreatePaySubscription(Guid id) {
			var plan = await _context.Plans
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing1)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing2)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing3)
				.Include(x => x.PlanType).ThenInclude(x => x.LinePricing4)
				.Include(x => x.UserDevice)
				.Include(x => x.Profile).ThenInclude(x => x.BillingAddress)
				.FirstOrDefaultAsync(x => x.Id == id);
			var existingPlans = await _context.Plans.Where(x => x.ProfileId == plan.ProfileId).ToListAsync();
			var activePlansOnThisPlanType = await _context.Plans.CountAsync(x => x.ProfileId == plan.ProfileId && PlanService.ActivePendingStatuses.Contains(x.Status) && x.PlanTypeId == plan.PlanTypeId);

			var methodReturn = await PlanService.GetSubscriptionMethod(existingPlans, plan.ProfileId);
			if (!methodReturn.IsSuccess) {
				// If there is more than one subid, there is a serious problem. Let the customer finish though.
				ErrorHandler.Capture(_other.SentryDSN, new Exception(methodReturn.Error), area: "Existing-Sub-Retrieval");
			}

			var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == plan.ProfileId && x.Active && !x.Archived);
			if (payAccount == null) {
				ErrorMessage = "There was no Active Pay Account for this customer.";
				return RedirectToAction(nameof(Index));
			}

			var epic = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass);

			// Get new monthly cost for all current plans
			var linePricing = PlanService.GetLinePricing(activePlansOnThisPlanType, plan.PlanType);
			var newMonthlyTotal = linePricing.MonthlyCost;
			// Add any surcharges from the Options
			// Get all chosen options from the plans
			// Select all the option ids (include duplicates for multiple lines with same option)
			var optionIds = plan.UserDevice.ChosenOptions?.Split(',').ToList();
			if (optionIds != null && optionIds.Any()) {
				// get the options from db to find the surcharge and multiply by the occurances in the id list
				var dbOptions = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id.ToString())).ToListAsync();
				newMonthlyTotal = newMonthlyTotal + dbOptions.Sum(x => x.Surcharge * optionIds.Count(y => y == x.Id.ToString()));
			}

			// TODO fix this. SOON.
			var callbackUrl = "https://lexvorwireless.com" + Url.Action(nameof(Lexvor.Controllers.API.PaymentController.PaymentSuccess), "Payment", new { id = payAccount.Id });
			var status = await PlanService.CreateSubscriptionForPlan(_other, plan.Profile, CurrentUserEmail, newMonthlyTotal, epic, payAccount, methodReturn.Method, methodReturn.SubscriptionId, callbackUrl);

			// Activate all pending plans. Paid if not ID verified, Active if verified.
			// If the payment call failed then the plans are still pending. Everything should be set already from the action navigator so the user can just retry payment.
			if (status.IsSuccess) {
				PlanService.UpdatePlansAfterSubscriptionCreate(status, plan.Profile, new List<UserPlan>() { plan });
				await _context.SaveChangesAsync();
				// NEVER activate the wireless plan here. Plan activation will always require admin intervention.
			}
			else {
				// TODO Track what errors are returned and set for retry or not.
				ErrorHandler.Capture(_other.SentryDSN, new Exception(status.Error), HttpContext, "Plan-Activation");
				ErrorMessage = "There was an issue when charging the users bank account. Please try again later.";
				return RedirectToAction(nameof(Index));
			}

			return RedirectToAction(nameof(Index), new { userPlanId = plan.Id });
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> RevenueVsCost() {
			var activePlans = await _context.Plans
				.Include(x => x.Profile).ThenInclude(x => x.Account)
				.Include(x => x.PlanType)
				.Where(x => PlanService.ActiveStatuses.Contains(x.Status)).ToListAsync();

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			foreach (var plan in activePlans) {
				plan.ThrottleLevel = "None";
				if (!plan.MDN.IsNull()) {
					try {
						var usage = await service.GetUsageForCycle(plan.MDN, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
						if (usage.KBData > plan.PlanType.FirstThrottle) {
							plan.ThrottleLevel = "3G";
						}
						if (usage.KBData > plan.PlanType.SecondThrottle) {
							plan.ThrottleLevel = "2G";
						}
						// Calculate cost
						plan.MDNCostToDate = WirelessService.CalculatePlanCostToDate(usage, plan.IsPorting);

						// Revenue minus cost 
						plan.RevenueCostDelta = Math.Round((plan.Monthly / 100) - plan.MDNCostToDate, 2);
					}
					catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, "GB Usage");
						plan.ThrottleLevel = "No Data";
					}
				}
			}

			return View(activePlans);
		}

		[HttpGet]
		[Route("[action]/{id}/{startDate ?}")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> UpdateUsage(Guid id, DateTime? startDate = null) {
			var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == id);

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			if (!plan.MDN.IsNull()) {
				try {
					await WirelessService.UpdateUsageDataFromWirelessProvider(service, _context, plan.MDN, startDate);
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, "Usage Update");
				}
			}

			return RedirectToAction(nameof(UserPlansController.Index), UserPlansController.Name, new { userPlanId = id });
		}


		public async Task<string> ActivateLine(UserPlan model) {
			if (model.Id == Guid.Empty) {
				Message = "There was no plan by that ID";
				return Message;
			}

			var plan = await _context.Plans
				.Include(x => x.Device)
				.Include(x => x.UserDevice)
				.Include(x => x.Profile).ThenInclude(x => x.BillingAddress)
				.Include(x => x.Profile.Account)
				.Include(x => x.PlanType)
				.Include(x => x.PortRequest)
				.FirstOrDefaultAsync(x => x.Id == model.Id);

			// Do the activation
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			var callback = string.Format("{0}://{1}{2}", Request.Scheme,
				Request.Host, Url.Action(nameof(WirelessController.ActivationCallBack), "Wireless",
					new { userPlanId = plan.Id }));

			if (plan.IsPorting) {
				try {
					var (success, message) = await service.StartActivateServiceLineWithPort(plan.Profile.ExternalWirelessCustomerId, callback, plan.UserDevice.IMEI,
					plan.PortRequest.MDN, plan.AssignedSIMICC, plan.PortRequest, plan.Id, plan.PlanType.ExternalId);
					_context.Add(new WebhookResponseObject() {
						ObjectId = model.Id.ToString(),
						ObjectType = nameof(UserPlan),
						Received = DateTime.UtcNow,
						ReceivedAction = "WirelessActivate",
						Text = message
					});
					await _context.SaveChangesAsync();

					if (success) {
						plan.ExternalWirelessPlanId = "-1";
						plan.WirelessStatus = WirelessStatus.Active;
						await _context.SaveChangesAsync();
					}
					else {
						ErrorMessage = message;
						var portRequest = _context.PortRequests.Where(x => x.Id == plan.PortRequest.Id).FirstOrDefault();
						portRequest.Status = PortStatus.Error;
						portRequest.StatusDescription = message;
						await _context.SaveChangesAsync();
						return ErrorMessage;
					}
				}
				catch (Exception e) {
					ErrorMessage = e.Message;
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Wireless-Activate-Line-Port");
					//return RedirectToAction(nameof(ActivateLine), new { userPlanId = model.Id, returnUrl });
					return ErrorMessage;
				}
			}
			else {
				try {
					var result = await service.StartActivateServiceLine(plan.Profile.ExternalWirelessCustomerId, callback, plan.Profile.BillingAddress.PostalCode, plan.UserDevice.IMEI,
					plan.AssignedSIMICC, plan.Id, plan.PlanType.ExternalId);

					_context.Add(new WebhookResponseObject() {
						ObjectId = model.Id.ToString(),
						ObjectType = nameof(UserPlan),
						Received = DateTime.UtcNow,
						ReceivedAction = "TelispireActivate",
						Text = JsonConvert.SerializeObject(result)
					});
					await _context.SaveChangesAsync();

					if (result.Status == "1" || result.Status == "2") {
						// Success?
						plan.ExternalWirelessPlanId = result.AccountNumber.ToString();
						plan.WirelessStatus = WirelessStatus.Active;
						await _context.SaveChangesAsync();
					}
					else {
						ErrorMessage = $"Activate error status: {result.Status}";
					}
				}
				catch (Exception e) {
					ErrorMessage = e.Message;
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Wireless-Activate-Line-No-Port");
					//			return RedirectToAction(nameof(ActivateLine), new { userPlanId = model.Id, returnUrl });
					return e.Message;
				}
			}
			return ErrorMessage;

		}


		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<JsonResult> UpdatePortRequest(PortRequest portRequest) {
			try {
				int portStatus = 0;
				if (!string.IsNullOrEmpty(portRequest.Status.ToString()))
					portStatus = (int)(PortStatus)Enum.Parse(typeof(PortStatus), portRequest.Status.ToString());

				portRequest = new PortRequest {
					Id = portRequest.Id,
					MDN = portRequest.MDN,
					AccountNumber = portRequest.AccountNumber,
					Password = portRequest.Password,
					Status = (PortStatus)portStatus,
					CanBeSubmitted = portRequest.CanBeSubmitted,
					LastUpdate = DateTime.Now,
					DateSubmitted = DateTime.Now,
					AddressLine1 = portRequest.AddressLine1,
					AddressLine2 = portRequest.AddressLine2,
					City = portRequest.City,
					FirstName = portRequest.FirstName,
					LastName = portRequest.LastName,
					MiddleInitial = portRequest.MiddleInitial,
					OSPName = portRequest.OSPName,
					State = portRequest.State,
					Zip = portRequest.Zip,
					StatusDescription = portRequest.StatusDescription
				};
				_context.Update(portRequest);
				await _context.SaveChangesAsync();
				return Json(true);
			}
			catch (Exception ex) {
				return Json(false);
			}
		}

		public List<SelectListItem> PortStatusList() {
			return new List<SelectListItem> {
			new SelectListItem {Text = PortStatus.Ready.ToString(), Value = ((int)PortStatus.Ready).ToString()},
			new SelectListItem {Text = PortStatus.Pending.ToString(), Value = ((int)PortStatus.Pending).ToString()},
			new SelectListItem {Text = PortStatus.Completed.ToString(), Value = ((int)PortStatus.Completed).ToString()},
			new SelectListItem {Text = PortStatus.Error.ToString(), Value = ((int)PortStatus.Error).ToString()},
			new SelectListItem {Text = PortStatus.Cancelled.ToString(), Value = ((int)PortStatus.Cancelled).ToString()},
			};
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<JsonResult> GetAvailableSIMs(string icc) {
			var items = await _context.StockedSims.Where(x => x.Available).OrderBy(x => x.DateAdded).Select(x => new { label = x.ICCNumber }).ToListAsync();
			if (!string.IsNullOrEmpty(icc)) {
				items = items.Where(x => x.label.Contains(icc)).ToList();
			}
			return Json(items);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<JsonResult> ReAssignSim(string iccNumber, string previousIccNumber, Guid planId) {
			var previousAssignedSIMICC = await _context.StockedSims.FirstAsync(x => x.ICCNumber == previousIccNumber);
			previousAssignedSIMICC.Available = true;

			var sim = await _context.StockedSims.FirstAsync(x => x.ICCNumber == iccNumber);
			sim.Available = false;

			var plan = await _context.Plans.FirstAsync(x => x.Id == planId);
			plan.AssignedSIMICC = sim.ICCNumber;

			await _context.SaveChangesAsync();
			return Json(true);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult> HistoricalDataUsage(Guid id) {
			List<UsageDay> usageDays = new List<UsageDay>();
			var userPlan = _context.Plans.Include(x => x.Profile)
							.FirstOrDefault(x => x.Id == id);
			ViewData["FullName"] = userPlan.Profile.FullName;
			if (!string.IsNullOrEmpty(userPlan.MDN)) {
				var usagesByMDN = await UsageService.GetUsageByMDN(_context, userPlan.MDN);
				if (usagesByMDN.Any()) {
					var groupedUsages = usagesByMDN.GroupBy(x => new { x.Date.Month, x.Date.Year }).Select(x => x.Key).ToList();
					if (groupedUsages.Any()) {
						foreach (var usages in groupedUsages) {
							var startDate = new DateTime(usages.Year, usages.Month, 1);
							var usage = usagesByMDN.Where(x => x.Date >= startDate && x.Date <= startDate.GetLast()).ToList();
							if (usage.Any()) {
								var usageDay = new UsageDay {
									Date = usage.Max(x => x.Date),
									Minutes = usage.Sum(x => x.Minutes),
									SMS = usage.Sum(x => x.SMS),
									KBData = usage.Sum(x => x.KBData)
								};
								usageDay.MDN = userPlan.MDN;
								usageDays.Add(usageDay);
							}
						}
					}
				}
			}
			return View(usageDays);
		}

	}
}



