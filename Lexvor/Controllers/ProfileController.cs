using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AccountViewModels;
using Lexvor.Models.HomeViewModels;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Stripe;
using UAParser;

namespace Lexvor.Controllers {
    /// <summary>
    /// THIS CONTROLLER SHALL ONLY CONTAIN ROUTES THAT DEAL DIRECTLY WITH THE USER PROFILE TABLE.
    /// </summary>
    public class ProfileController : BaseUserController {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ConnectionStrings _connStrings;
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _defaultConn;

        public static string Name => "Profile";

        private readonly string[] validImageExtensions = new[] { ".bmp", ".gif", ".jpeg", ".jpg", ".png" };
		private readonly string BlobPath = "loss-claim-images";

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            SignInManager<ApplicationUser> signInManager,
            IOptions<ConnectionStrings> connStrings,
            IOptions<OtherSettings> other,
            ApplicationDbContext context) : base(context, userManager) {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _connStrings = connStrings.Value;
            _other = other.Value;
            _defaultConn = _connStrings.DefaultConnection;
        }
		
        [Authorize(Roles = Roles.Trial)]
        public async Task<IActionResult> Settings() {
            var user = await GetCurrentAccount();
            // Get stripe data for changing billing info and card.
            var model = new SettingsViewModel() {
                Profile = CurrentProfile,
                User = user,
                PayAccount =
                    await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active)
            };
			
            return View(model);
        }

     

		[Authorize(Roles = Roles.User)]
		public async Task<IActionResult> LossClaim(Guid id) {
			var user = await _userManager.FindByEmailAsync(CurrentUserEmail);

			var plan = await _context.Plans.SingleAsync(x => x.Id == id);
			var lossClaim = await _context.LossClaims.FirstOrDefaultAsync(x => x.UserDeviceId == plan.UserDeviceId);
			var model = new LossClaimViewModel() {
				Id = id,
				LossClaim = lossClaim,
				Profile = CurrentProfile
			};

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = Roles.User)]
		public async Task<IActionResult> LossClaim(LossClaimViewModel model) {
			if (IsLossClaimViewModelValid(model) && ModelState.IsValid) {
				var plan = await _context.Plans.Include(x=>x.UserDevice).ThenInclude(x=>x.StockedDevice).SingleAsync(x=> x.Id == model.Id);
				var newLossClaim = new LossClaim() {
					DateReported = DateTime.Now,
					UserDeviceId = plan.UserDeviceId.Value,
					Profile = CurrentProfile,
					LossType = model.LossType					
				};
				var entry = _context.LossClaims.Add(newLossClaim);

				await _context.SaveChangesAsync();
				await UploadLossClaimImagesAsync(model, entry.Entity.Id);

				await _emailSender.SendEmailAsync("customerservice@lexvor.com", CurrentUserEmail, $"New loss claim from {CurrentProfile.FullName}",
					model.Message);

				Message = "We will get back to you within 2 business days.";

				return RedirectToAction(nameof(HomeController.Index), "Home");
			}
			else {
				model.Profile = CurrentProfile;
				return View(model);
			}

		}

		private bool IsLossClaimViewModelValid(LossClaimViewModel model) {

			if (string.IsNullOrEmpty(model.Message)) {
				ModelState.AddModelError(string.Empty, "Please provide a message.");
				return false;
			}
			if (model.LossType == LossType.Damaged) {
				if (model.ImageWholeFront == null || model.ImageWholeBack == null || model.ImagePhoneScreenOn == null || model.ImagePhoneScreenOn == null
					|| model.ImagePhoneScreenOff == null || model.DamageCloseUpAngle1 == null || model.DamageCloseUpAngle2 == null || model.DamageCloseUpAngle3 == null) {
					ModelState.AddModelError(string.Empty, "Please provide all of the images.");
					return false;
				}
			}
			else if (model.LossType == LossType.Stolen && model.PoliceReport == null) {
				ModelState.AddModelError(string.Empty, "Please provide all of the images.");
				return false;
			}
			
			return true;
		}

		private async Task UploadLossClaimImagesAsync(LossClaimViewModel model, Guid lossClaimId) {
			if (model.LossType == LossType.Stolen) {
				await UploadPicture(model.PoliceReport, "PoliceReport", lossClaimId);
			}
			else if (model.LossType == LossType.Damaged) {
				await UploadPicture(model.ImageWholeFront, "ImageWholeFront", lossClaimId);
				await UploadPicture(model.ImageWholeBack, "ImageWholeBack", lossClaimId);
				await UploadPicture(model.ImagePhoneScreenOn, "ImagePhoneScreenOn", lossClaimId);
				await UploadPicture(model.ImagePhoneScreenOff, "ImagePhoneScreenOff", lossClaimId);
				await UploadPicture(model.DamageCloseUpAngle1, "DamageCloseUpAngle1", lossClaimId);
				await UploadPicture(model.DamageCloseUpAngle2, "DamageCloseUpAngle2", lossClaimId);
				await UploadPicture(model.DamageCloseUpAngle3, "DamageCloseUpAngle3", lossClaimId);
			}

			async Task UploadPicture(IFormFile picture, string picName, Guid lossClaimId) {
				var path = $"{BlobPath}-{picName}-{lossClaimId}{Path.GetExtension(picture.FileName)}";
				var stream = new MemoryStream();
				await picture.CopyToAsync(stream);
				await BlobService.UploadBlob(stream.ToArray(), path, _other);

				_context.LossClaimUploads.Add(new LossClaimUpload() {
					Id = Guid.NewGuid(),
					ImageUrl = path,
					LossClaimId = lossClaimId
				});

				await _context.SaveChangesAsync();
			}
		}

        [HttpGet]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> Agreement(Guid planId, string returnUrl = null) {
            if (!string.IsNullOrWhiteSpace(returnUrl)) {
                ViewData["ReturnUrl"] = returnUrl;
            }

            var plan = await _context.Plans
				.Include(x => x.PlanType)
				.Include(x => x.UserDevice)
				.Include(x => x.Device)
				.Include(x => x.Profile.BillingAddress)
				.FirstOrDefaultAsync(x => x.Id == planId);

            // If use does not have devices, do not sign agreement
            if ((plan != null && !plan.AgreementSigned) || CurrentProfile.ForceReaffirmation || !plan.IsWirelessOnly()) {
				// Default term length to 12 to make sure UI doesnt display negatives.
				plan.PlanType.TermLength = plan.PlanType.TermLength == 0 ? 12 : plan.PlanType.TermLength;
				return View(new AgreementViewModel() {
                    AgreementName = $"LeaseAgreement-{CurrentProfile.FirstName}.{CurrentProfile.LastName}.{CurrentUserEmail}-{plan.Id}-{DateTime.UtcNow:O}",
                    Plan = plan
                });
            } else {
                if (!string.IsNullOrWhiteSpace(returnUrl)) {
                    return RedirectToLocal(returnUrl);
                } else {
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> Agreement(AgreementViewModel model, string returnUrl = null) {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(model.ProvidedName) || !model.ProvidedName.Contains(' ')) {
                ModelState.AddModelError("", "Please provide your full legal name.");
            }
            // TODO check name against account name?
            if (!model.Accepted) {
                ModelState.AddModelError("", "You must accept our agreement before continuing.");
            }
            if (!ModelState.IsValid) {
				var p = await _context.Plans
					.Include(x => x.PlanType)
					.Include(x => x.UserDevice)
					.Include(x => x.Device)
					.FirstOrDefaultAsync(x => x.Id == model.Plan.Id);
				model.Plan = p;
				return View(model);
            }

            var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == model.Plan.Id);
            plan.AgreementSigned = true;
            CurrentProfile.ForceReaffirmation = false;

            var parser = Parser.GetDefault();
            var ua = parser.Parse(Request.Headers["User-Agent"].ToString());
            await _context.AgreementAffirmations.AddAsync(new AgreementAffirmation() {
                Id = Guid.NewGuid(),
                ProfileId = CurrentProfile.Id,
                IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                ProvidedName = model.ProvidedName,
                Timestamp = DateTime.UtcNow,
                Browser = ua.UserAgent.Family,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                Device = ua.Device.Family == "Other" ? ua.OS.Family : ua.Device.Family,
                AgreementName = model.AgreementName
            });

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl)) {
                return RedirectToLocal(returnUrl);
            } else {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }


        [HttpPost]
        [Authorize(Roles = Roles.Trial)]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
            var user = await GetCurrentAccount();

            if (!ModelState.IsValid) {
                ErrorMessage = string.Join("|", ModelState.Values.Select(x => string.Join(" ", x.Errors.Select(x => x.ErrorMessage))));
                return RedirectToAction(nameof(Settings));
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded) {
                ErrorMessage = string.Join("|", changePasswordResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Settings));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            Message = "Your password has been changed.";
            return RedirectToAction(nameof(Settings), ProfileController.Name);
        }

        private void AddErrors(IdentityResult result) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }


}
