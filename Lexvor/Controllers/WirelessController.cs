using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Lexvor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Lexvor.Models;
using Lexvor.Models.AccountViewModels;
using Lexvor.Models.HomeViewModels;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Lexvor.API.Objects.Enums;

namespace Lexvor.Controllers {

    [AllowAnonymous]
    public class WirelessController : BaseUserController {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ConnectionStrings _connStrings;
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;
        private readonly string _defaultConn;

        public static string Name => "Wireless";

        public WirelessController(
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

	    public async Task<IActionResult> Index() {
            var user = await GetCurrentAccount();
            return View(new LoggedInPageViewModel() {
                Profile = CurrentProfile,
                User = user
            });
        }

        public async Task<IActionResult> StartPort(Guid id) {
            // UserPlanId
            // Create a port request for a user with Wireless that did not get created for whatever reason
            var plan = await _context.Plans.Include(x => x.PortRequest).FirstOrDefaultAsync(x => x.Id == id);
            // We have a plan and it does not have a PR
            if (plan != null && plan.PortRequest == null) {
                plan.PortRequest = new PortRequest() {
                    // If sim is assigned, the port can be submitted
                    CanBeSubmitted = !string.IsNullOrWhiteSpace(plan.AssignedSIMICC),
                    Status = Lexvor.API.Objects.User.PortStatus.Ready
                };
				plan.IsPorting = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(DeviceController.Administer), DeviceController.Name, new {id});
        }

        public async Task<IActionResult> SubmitPort(AdministerDevicesViewModel model) {
            // Status of number ports for account
            var plan = await _context.Plans.Include(x => x.PortRequest).FirstOrDefaultAsync(x => x.Id == model.CurrentUserPlan.Id);
            if (plan.PortRequest == null) plan.PortRequest = new PortRequest();
            if (model.CurrentUserPlan.PortRequest != null) {
	            plan.PortRequest.MDN = StaticUtils.NumericStrip(model.CurrentUserPlan.PortRequest.MDN);
	            plan.PortRequest.OSPName = model.CurrentUserPlan.PortRequest.OSPName;
	            plan.PortRequest.AccountNumber = model.CurrentUserPlan.PortRequest.AccountNumber;
	            plan.PortRequest.Password = model.CurrentUserPlan.PortRequest.Password;
	            plan.PortRequest.FirstName = model.CurrentUserPlan.PortRequest.FirstName;
	            plan.PortRequest.LastName = model.CurrentUserPlan.PortRequest.LastName;
	            plan.PortRequest.MiddleInitial = model.CurrentUserPlan.PortRequest.MiddleInitial;
	            plan.PortRequest.AddressLine1 = model.CurrentUserPlan.PortRequest.AddressLine1;
	            plan.PortRequest.AddressLine2 = model.CurrentUserPlan.PortRequest.AddressLine2;
	            plan.PortRequest.City = model.CurrentUserPlan.PortRequest.City;
	            plan.PortRequest.State = model.CurrentUserPlan.PortRequest.State;
	            plan.PortRequest.Zip = model.CurrentUserPlan.PortRequest.Zip;
	            plan.PortRequest.DateSubmitted = DateTime.Now;
	            plan.PortRequest.LastUpdate = DateTime.Now;
	            plan.PortRequest.Status = PortStatus.Pending;
            }

            await _context.SaveChangesAsync();

            Message = "Thank you for updating your Port information. We will get started activating your account";
            return RedirectToAction(nameof(DeviceController.Administer), DeviceController.Name, new { id = plan.Id });
        }

        public async Task<IActionResult> Validate(bool validateImei = false) {
            // Are you porting a number? Save that info so that we can modify the Activate view with the number port info
            // TODO Save the number for porting and the IMEI for bringing your own device to the DB or session somewhere so that we don't require the user to input it twice.
            return View(new ValidateIndexViewModel() {
                Profile = CurrentProfile,
                ShowIMEI = validateImei
            });
        }

        public async Task<JsonResult> ValidateMDN(ValidateViewModel model) {
            // Validate MDN/Number and IMEI/ESN
            var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
            var referenceNumber = await service.SubmitPortValidate(model.MobileNumber);

            return Json(new {
                referenceNumber = referenceNumber
            });
        }

        public async Task<JsonResult> ValidateIMEI(ValidateViewModel model) {
            // Validate MDN/Number and IMEI/ESN
            var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
            var referenceNumber = await service.SubmitPortValidate(model.MobileNumber);
            var result = await service.IMEIValidate(model.IMEI);

            return Json(new {
                referenceNumber = referenceNumber,
                imeiResult = result
            });
        }

        public async Task<JsonResult> ValidateCheck(string refNumber) {
            // Validate MDN/Number and IMEI/ESN
            var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
            var status = await service.CheckPortValidate(refNumber);

            return Json(new {
                success = status.Item1,
                message = status.Item2
            });
        }

        public async Task<IActionResult> Usage(string mdn, bool isFromLineChooserPageRequest = false) {
			
			if (string.IsNullOrEmpty(mdn) && isFromLineChooserPageRequest) {
				ErrorMessage = "You do not have a number assigned yet, so there is no usage data available.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}

			if (string.IsNullOrEmpty(mdn)) {
				var routeData = new {
					baseUrl = HttpContext.Request.Path,
					queryParameterType = EnumUrlSectionToReplaceType.MDN,
					queryParameterName = "mdn"
				};
				return RedirectToAction(nameof(HomeController.LineChooser), HomeController.Name, routeData);
			}

			mdn = mdn.Trim();

	        var permissionedPlan = await _context.Plans.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.MDN == mdn);
	        if (permissionedPlan == null) {
		        ErrorMessage = "You do not have permission to access usage data for that number.";
		        return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
	        }
	        
	        // Summary of usage for account
            var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
            
			// Update today
			var status = await service.GetUsageForDay(mdn, DateTime.UtcNow.Date);

			var todays = await _context.UsageDays.FirstOrDefaultAsync(x => x.MDN == mdn && x.Date == DateTime.UtcNow.Date);
			if (todays != null) {
				todays.KBData = status.KBData;
				todays.Minutes = status.Minutes;
				todays.SMS = status.SMS;
			}
			else {
				_context.Add(status);
			}

			await _context.SaveChangesAsync();

			// Get usage for the current bill cycle
			var usage = new List<UsageDay>();
			try {
				// Cannot use this in LINQ
				var first = DateTime.UtcNow.GetFirst();
				usage = await _context.UsageDays.Where(x => x.MDN == mdn && x.Date >= first).ToListAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, "Usage-List");
			}

            return View(new UsageViewModel() {
				Profile = CurrentProfile,
				Usage = usage.OrderBy(x => x.Date).ToList()
            });
        }

        public async Task<IActionResult> Records() {
            // Call and Text records for account
            return View();
        }

        public async Task<IActionResult> ActivationCallBack(TelispireSOAPService.GetWirelessResponse response) {
            // No sure what we use this for yet. Many confirming the Activation?
            return View();
        }
    }
}
