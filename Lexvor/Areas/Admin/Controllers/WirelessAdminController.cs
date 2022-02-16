using System;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Controllers.API;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	public class WirelessAdminController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;

		public static string Name => "WirelessAdmin";

		public WirelessAdminController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> CreateCustomer(Guid profileId, string returnUrl = "") {
			// UserPlanId
			var user = await _context.Profiles.Include(x => x.BillingAddress).FirstAsync(x => x.Id == profileId);

			if (!string.IsNullOrWhiteSpace(user.ExternalWirelessCustomerId)) {
				Message = "Customer already has a wireless account";
				return RedirectToLocal(returnUrl);
			}

			try {
				var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
				var accountNumber = await service.CreateCustomer(user.FullName, user.BillingAddress.Line1,
					user.BillingAddress.City, user.BillingAddress.Provence,
					user.BillingAddress.PostalCode);
				if (string.IsNullOrWhiteSpace(accountNumber)) {
					// Account creation failed, allow the user to continue anyway.
					ErrorHandler.Capture(_other.SentryDSN, new Exception("There should be a previous error that describes why Account Number was empty."), area: "Wireless-Customer-Create");
					ErrorMessage = "Account number returned was null. Could not create customer";
				}

				user.ExternalWirelessCustomerId = accountNumber;
				await _context.SaveChangesAsync();
			} catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, area: "Wireless-Customer-Create");
				ErrorMessage = e.Message;
				return RedirectToLocal(returnUrl);
			}

			// Allow admin to confirm
			ViewData["ReturnUrl"] = returnUrl;
			return RedirectToLocal(returnUrl);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ActivateLine(Guid userPlanId, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans
				.Include(x => x.Device)
				.Include(x => x.UserDevice)
				.Include(x => x.Profile)
				.Include(x => x.PlanType)
				.FirstOrDefaultAsync(x => x.Id == userPlanId);

			if (plan == null) {
				Message = "There was no plan by that ID";
				return RedirectToLocal(returnUrl);
			}

			// Allow admin to confirm
			ViewData["ReturnUrl"] = returnUrl;
			return View(plan);
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ActivateLine(UserPlan model, string returnUrl = "") {
			if (model.Id == Guid.Empty) {
				Message = "There was no plan by that ID";
				return RedirectToLocal(returnUrl);
			}

			var plan = await _context.Plans
				.Include(x => x.Device)
				.Include(x => x.UserDevice)
				.Include(x => x.Profile).ThenInclude(x => x.BillingAddress)
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
					}
				} catch (Exception e) {
					ErrorMessage = e.Message;
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Wireless-Activate-Line-Port");
					return RedirectToAction(nameof(ActivateLine), new { userPlanId = model.Id, returnUrl });
				}
			} else {
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
				} catch (Exception e) {
					ErrorMessage = e.Message;
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Wireless-Activate-Line-No-Port");
					return RedirectToAction(nameof(ActivateLine), new { userPlanId = model.Id, returnUrl });
				}
			}

			if (!string.IsNullOrWhiteSpace(returnUrl)) {
				return RedirectToLocal(returnUrl);
			}

			return RedirectToAction(nameof(UserPlansController.Activations), UserPlansController.Name);
		}

		[Route("[action]/{id}")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> CheckPortStatus(Guid id, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans.Include(x => x.PortRequest).FirstOrDefaultAsync(x => x.Id == id);

			if (plan.PortRequest != null) {
				try {
					var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
					await WirelessService.UpdatePortStatus(_context, service, plan);
				} catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, area: "Wireless-Port-Status");
					ErrorMessage = e.Message;
					return RedirectToLocal(returnUrl);
				}
			}

			// Allow admin to confirm
			ViewData["ReturnUrl"] = returnUrl;
			return RedirectToLocal(returnUrl);
		}
	}
}
