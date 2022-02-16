using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Controllers.API;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Stripe;

namespace Lexvor.Controllers {
    [Authorize(Roles = Roles.Trial)]
    //[ServiceFilter(typeof(ExceptionCatcher))]
    public class HomeController : BaseUserController {
        private readonly ConnectionStrings _connStrings;
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;
        private readonly string _defaultConn;
        
        public static string Name => "Home";

        public HomeController(
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IOptions<ConnectionStrings> connStrings,
            IOptions<OtherSettings> other,
            ApplicationDbContext context) : base(context, userManager) {
            _emailSender = emailSender;
            _connStrings = connStrings.Value;
            _other = other.Value;
            _defaultConn = _connStrings.DefaultConnection;
        }

        public async Task<IActionResult> Index() {
	        var user = await GetCurrentAccount();
	        var plans = await _context.Plans
		        .Include(p => p.PlanType)
				.Include(p => p.UserDevice)
				.Include(p => p.Device)
				.Include(p => p.PortRequest)
		        .Where(x => x.ProfileId == CurrentProfile.Id && (x.Status != PlanStatus.Cancelled)).ToListAsync();
			
	        if (!plans.Any()) {
		        return RedirectToAction(nameof(PlanController.Index), PlanController.Name);
	        }
			
            var hasrequest = await _context.Plans.AnyAsync(p => p.ProfileId == CurrentProfile.Id && p.UserDeviceId.HasValue);
			
            var model = new HomeIndexViewModel() {
                Profile = CurrentProfile,
                User = user,
                Plans = plans,
                UserHasRequest = hasrequest
            };
			
            return View(model);
		}

        [AllowAnonymous]
		public async Task<IActionResult> OurPlans() {
		    var planTypes = await PlanTypeService.GetPublicPlanTypes(_context);
            return View(planTypes);
		}
		
		[AllowAnonymous]
	    public async Task<IActionResult> OurDevices(Guid id) {
		    var planType = await PlanTypeService.GetPlanTypeByIdWithDevices(_context, id);
		    return View(new OurDevicesViewModel() {
			    Devices = planType.Devices.Select(x => x.Device).ToList(),
				Plan = planType
		    });
        }

	    [HttpGet]
        public async Task<IActionResult> ResendEmail() {
		    var user = await GetCurrentAccount();
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
		    await EmailService.SendEmailConfirmation(_emailSender, _other, user.Email, callbackUrl);

			Message = "The confirmation email has been re-sent to the email you signed up with. Don't forget to check spam.";

		    return RedirectToAction(nameof(HomeController.Index));
		}

	    [HttpGet]
        public async Task<IActionResult> Support() {
		    var model = new SupportViewModel() {
			    Profile = CurrentProfile,
			    Request = new SupportRequest(),
				User = await GetCurrentAccount()
		    };
		    return View(model);
	    }

		[HttpPost]
		[ValidateAntiForgeryToken]
        public async Task<IActionResult> Support(SupportViewModel model) {
            var user = await GetCurrentAccount();

            var plans = await _context.Plans.AnyAsync(x => x.Status == PlanStatus.Active && x.ProfileId == CurrentProfile.Id);

            var message = $@"New support request from {(string.IsNullOrWhiteSpace(CurrentProfile.FullName) ? user.Email : CurrentProfile.FullName)}
				Contact info: {CurrentUserEmail} ({CurrentProfile.Phone})
				{(plans ? "Existing Customer" : "New Customer")}
				Method of Contact: {model.Request.PreferredMethodOfContact}
				Time to Contact: {model.Request.PreferredTime}
				Message: {model.Request.Message}
			";

            await _emailSender.SendEmailAsync("customerservice@lexvor.com", $"New support request from {(string.IsNullOrWhiteSpace(CurrentProfile.FullName) ? user.Email : CurrentProfile.FullName)}",
	            message, replyTo: user.Email);
            await _emailSender.SendEmailAsync("lexvor@lexvor.com", $"New support request from {(string.IsNullOrWhiteSpace(CurrentProfile.FullName) ? user.Email : CurrentProfile.FullName)}",
	            message, replyTo: user.Email);

            Message = "We will get back to you within 2 business days.";

            return View(new SupportViewModel() {
	            Profile = CurrentProfile,
	            Request = new SupportRequest(),
	            User = await GetCurrentAccount()
            });
        }

		[AllowAnonymous]
	    public IActionResult Error() {
		    // Get the details of the exception that occurred
		    var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

			if (exceptionFeature != null) {
				// Get which route the exception occurred at
				string routeWhereExceptionOccurred = exceptionFeature.Path;

				// Get the exception that occurred
				Exception exceptionThatOccurred = exceptionFeature.Error;

				var errorcode = ErrorHandler.Capture(_other.SentryDSN, exceptionThatOccurred, HttpContext, customData: new Dictionary<string, string>() {
					{ "Route", routeWhereExceptionOccurred }
				});

				if (exceptionThatOccurred is SqlException) {
					ViewBag.message = "There was a db mismatch error.";
				}
				else {
					ViewBag.message = exceptionThatOccurred.Message;
					ViewBag.code = errorcode;
				}
			}
			return View();
	    }

        /// <summary>
        /// This is here because we do not want the usermanager in the BaseUserController.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> HeaderAlert(bool hidePersistentMessages = false) {
            var model = hidePersistentMessages ? new HeaderAlertsViewModel() { Alerts = new Dictionary<string, string>() } : await GenerateAlertModel(_userManager, CurrentProfile);

            var msg = Message;
            if (!string.IsNullOrEmpty(msg)) {
                model.Alerts.Add(msg, "success");
            }

            var err = ErrorMessage;
            if (!string.IsNullOrEmpty(err)) {
                model.Alerts.Add(err, "warning");
            }

            // Get DB user messages
            var messages = await UserMessageService.GetLatestUserMessages(_context, CurrentProfile.AccountId);
            messages.ForEach((m) => {
                model.Alerts.Add(m.Title, m.Color);
            });

            return PartialView("_HeaderAlert", model);
        }

		public async Task<IActionResult> VerifyPhone(string profileid, string code) {

			var profile = await _context.Profiles.Where(x => x.PhoneVerificationCode == code 
							&& x.Id == Guid.Parse(profileid)).FirstOrDefaultAsync();

			if (profile != null) {
				profile.PhoneVerified = true;

				await _context.SaveChangesAsync();

				return new JsonResult(new { result = "verified" });
			}

			return new JsonResult(new { result = "not match" });
		}

		public async Task<IActionResult> ResendVerificationCode() {
			if (!CurrentProfile?.PhoneVerified ?? false) {

				var callback = string.Format("{0}://{1}{2}", Request.Scheme,
					Request.Host, Url.Action(nameof(CommunicationsController.SMSMessageCallback), "Communications"));

				var message = "Thank you for signing up for Lexvor. Please verify your phone when you login. Verification Code: " + CurrentProfile.PhoneVerificationCode + ". https://lexvorwireless.com/";
				await TwilioService.Send(_other, _context, callback, CurrentProfile.Id, CurrentProfile.Phone,
					message);

				return Ok();
			}

			return Ok();
		}

		protected async Task<HeaderAlertsViewModel> GenerateAlertModel(UserManager<ApplicationUser> userManager, Profile profile) {
            var alerts = new Dictionary<string, string>();
            var anyPlans = _context.Plans.Any(x => x.ProfileId == profile.Id);

            var user = await userManager.FindByEmailAsync(CurrentUserEmail);
            var confirmEmail = await userManager.IsEmailConfirmedAsync(user);
            if (!confirmEmail) {
	            alerts.Add(
		            $"Please confirm your email address. We cannot activate your plan until your email is verified. Don't forget to check your spam. <a href=\"{Url.Action(nameof(AccountController.ResendEmailConfirm), AccountController.Name)}\">Send email again</a>",
		            "info");
            }
			// Only show if the user has plans. no plans = new user, don't show this message.
            if (profile.IDVerifyStatus == IDVerifyStatus.NotStarted && anyPlans) {
	            alerts.Add($"Please <a href=\"{Url.Action(nameof(ProfileController.Settings), ProfileController.Name)}\">upload your identity documents</a>. We cannot activate your plan until your ID is verified.", "warning");
            }
            if (profile.IDVerifyStatus == IDVerifyStatus.Pending) {
	            alerts.Add("Your ID is currently pending verification. We will update you when things change (usually within 24 hours).", "info");
            }
            if (profile.IDVerifyStatus == IDVerifyStatus.ReverificationRequired) {
	            alerts.Add($"There was something wrong with your identity documents. Please check the <a href=\"{Url.Action(nameof(DocumentsController.Index), DocumentsController.Name)}\">Settings page</a> for more details.", "danger");
			}
			if (_other.EnablePhoneVerifications) {
				if (profile.PhoneVerified == false || profile.PhoneVerified == null) {
					alerts.Add($"Your phone number is not verified, <a href=\"#\" onclick=\"openForm();\">Verify</a> it now.", "danger");

				}
			}

			var model = new HeaderAlertsViewModel() {
                Alerts = alerts
            };

            return model;
        }

		[Authorize]
		public async Task<IActionResult> LineChooser(string baseUrl, EnumUrlSectionToReplaceType queryParameterType, string queryParameterName) {
			// UserId
			var user = await _userManager.FindByEmailAsync(CurrentUserEmail);

			// Get current user plan
			var plans = await _context.Plans
				.Include(p => p.Device)
				.Include(p => p.PlanType)
				.Include(x => x.PortRequest)
				.Include(p => p.UserDevice)
				.ThenInclude(ud => ud.Profile)
				.Where(p => p.UserDevice.Profile.AccountId == user.Id)
				.ToListAsync();

			return View(new LineChooserViewModel { 
				BaseUrl = baseUrl,
				QueryParameterType = queryParameterType,
				QueryParameterName = queryParameterName,
				Plans = plans
			});
		}
    }
}
