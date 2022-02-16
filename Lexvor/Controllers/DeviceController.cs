using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.HomeViewModels;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelispirePostPaidAPI;

namespace Lexvor.Controllers {
    public class DeviceController : BaseUserController {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;

        public static string Name => "device";

        public DeviceController(
            UserManager<ApplicationUser> userManager,
            IOptions<OtherSettings> other,
            IEmailSender emailSender,
            ApplicationDbContext context) : base(context, userManager) {
            _userManager = userManager;
            _emailSender = emailSender;
            _other = other.Value;
        }

        public async Task<IActionResult> AvailableDevices(Guid id) {
            // UserPlanId
            if (CurrentProfile.InProbation) {
                Message = "You are currently in your probationary period. You will not be able to select an upgrade device for the next 12 months. Contact Support if you wish to apply for an exemption.";
            }

            var userPlan = await _context.Plans.Include(x => x.Device)
                .Include(x => x.PlanType).ThenInclude(x => x.Devices).ThenInclude(x => x.Device).ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == id);

            // If the plan has a device that is not ready for upgrade.
			// TODO DeviceService
            //if (userPlan != null && userPlan.UserDevice?.NextUpgrade > DateTime.Now) {
            //    ErrorMessage = $"The plan you selected is not eligible for an upgrade at this time. Your upgrade date is {userPlan.UserDevice.NextUpgrade?.ToString("d")}";
            //    return RedirectToAction(nameof(HomeController.Index), "Home");
            //}

            if (userPlan == null) {
                ErrorMessage = $"Could not find a plan for that ID.";
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            var model = new AvailableDevicesViewModel() {
                Profile = CurrentProfile,
                Devices = userPlan.PlanType.Devices.Select(x => x.Device).ToList(),
                CurrentUserPlan = userPlan
            };
            return View(model);
        }
        
        [HttpPost]
        public async Task<IActionResult> AvailableDevices(AvailableDevicesViewModel model) {
            if (ModelState.IsValid) {
                var plan = await _context.Plans.FirstAsync(p => p.Id == model.CurrentUserPlanId);

                // Reset agreement flag because of new device.
                plan.AgreementSigned = false;
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
                // TODO refactor to capture device options
                var device = await _context.Devices.FirstAsync(x => x.Id == model.SelectedDeviceId);
                await DeviceService.AssignDeviceToUserPlan(_context, _emailSender, new AssignContext() {
                    User = user,
                    UserPlan = plan,
                    Device = device,
                });

                return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
            } else {
                var userPlan = await _context.Plans.Include(x => x.Device)
                    .Include(x => x.PlanType).ThenInclude(x => x.Devices).ThenInclude(x => x.Device).ThenInclude(x => x.Options)
                    .FirstOrDefaultAsync(x => x.Id == model.CurrentUserPlanId);

                // If the plan has a device that is not ready for upgrade.
				// TODO DeviceService
                //if (userPlan != null && userPlan.UserDevice?.NextUpgrade > DateTime.Now) {
                //    ErrorMessage = $"The plan you selected is not eligible for an upgrade at this time. Your upgrade date is {userPlan.UserDevice.NextUpgrade?.ToString("d")}";
                //    return RedirectToAction(nameof(HomeController.Index), "Home");
                //}
                if (userPlan == null) {
                    ErrorMessage = $"Could not find a plan for that ID.";
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }

                var reModel = new AvailableDevicesViewModel() {
                    Profile = CurrentProfile,
                    Devices = userPlan.PlanType.Devices.Select(x => x.Device).ToList(),
                    CurrentUserPlan = userPlan
                };

                return View(reModel);
            }
        }

        
        public async Task<IActionResult> Administer(Guid id) {
            // UserPlanId
            if (CurrentUserEmail.IsNull()) {
				// Reauth needed
				return RedirectToAction(nameof(AccountController.Login), AccountController.Name);
            }
            var user = await _userManager.FindByEmailAsync(CurrentUserEmail);

            // Get current user plan
            var plan = await _context.Plans
                .Include(p => p.Device)
                .Include(p => p.UserDevice)
                .Include(p => p.PlanType)
                .Include(x => x.PortRequest)
                .Include(p => p.UserDevice)
                .ThenInclude(ud => ud.Profile)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserDevice.Profile.AccountId == user.Id);

			if( plan == null) {
				var routeData = new {
					baseUrl = HttpContext.Request.Path,
					queryParameterType = EnumUrlSectionToReplaceType.PlanId,
					queryParameterName = "id"
				};
				return RedirectToAction(nameof(HomeController.LineChooser), HomeController.Name, routeData);
			}

            // Only require agreement if this is a device plan
            if (!plan.IsWirelessOnly() && !plan.AgreementSigned) {
                return RedirectToAction(nameof(ProfileController.Agreement), ProfileController.Name, new { planId = id, returnUrl = Url.Action(nameof(Administer), new { id = plan.Id }) });
            }

            var usage = new UsageDay();
            if (!plan.MDN.IsNull()) {
				usage = await UsageService.GetUsageAggregateForCycle(_context, plan.MDN, DateTime.UtcNow.GetFirst());
			}

            var model = new AdministerDevicesViewModel() {
                Profile = CurrentProfile,
                CurrentUserPlan = plan,
                User = user,
                PortRequest = new PortRequest() {
                    MDN = plan.PortRequest?.MDN ?? plan.MDN,
                    AccountNumber = plan.PortRequest?.AccountNumber,
                    Password = plan.PortRequest?.Password,
					Status = plan.PortRequest?.Status ?? PortStatus.Pending
				},
                UsageDay = usage
			};

            return View(model);
        }
        public async Task<IActionResult> ReturnDevice(Guid id) {
            // Get current user plan
            var plan = await _context.Plans.Include(p => p.Device).Include(p => p.UserDevice).FirstAsync(p => p.Id == id);
            return View(plan);
        }

        [HttpPost]

        public async Task<IActionResult> ReturnDevice(UserPlan model) {
            var plan = await _context.Plans.Include(p => p.Device).FirstAsync(p => p.Id == model.Id);

            var userDevice = await _context.UserDevices.FirstAsync(u => u.Id == plan.UserDeviceId);
            userDevice.ReturnRequested = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
            await EmailService.SendDeviceReturnEmail(_emailSender, "customerservice@lexvor.com", user.Email, plan.Device.Name);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
