using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lexvor.Models;
using Lexvor.Models.AccountViewModels;
using Lexvor.Services;
using Microsoft.EntityFrameworkCore;
using SmartFormat;
using Lexvor.API.Objects.User;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using UAParser;
using Lexvor.API.Services;
using Lexvor.Helpers;
using Lexvor.Controllers.API;

namespace Lexvor.Controllers {
    /// <summary>
    /// THIS CONTROLLER SHALL ONLY CONTAIN ROUTES THAT DEAL DIRECTLY WITH USER MANAGEMENT AND AUTHORIZATION
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : BaseUserController {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ConnectionStrings _connStrings;
        private readonly OtherSettings _other;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public static string Name => "Account";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IOptions<ConnectionStrings> connStrings,
            IOptions<OtherSettings> other,
            ApplicationDbContext context) : base(context, userManager) {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _connStrings = connStrings.Value;
            _other = other.Value;
        }

        #region Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null) {
            // Redirect if user already logged in
            if (User.Identity.IsAuthenticated) {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            await _signInManager.SignOutAsync();

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null) {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null) {
                ModelState.AddModelError(string.Empty, "Either the password or user name you entered are incorrect. If you have forgotten your password please use the link below.");

                _context.LoginAttempts.Add(new LoginAttempt() {
                    AttemptDate = DateTime.UtcNow,
                    Email = model.Email,
                    IP = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    Success = false
                });
            }
            if (!ModelState.IsValid) return View(model);

            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);
            if (!result.Succeeded) {
                ModelState.AddModelError(string.Empty,
                    "Password or Email was incorrect. Or your account is locked.");

                // Log the failed attempt
                _context.LoginAttempts.Add(new LoginAttempt() {
                    AttemptDate = DateTime.UtcNow,
                    Email = model.Email,
                    IP = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    Success = false
                });
            }
            if (!ModelState.IsValid) return View(model);

            // Cannot use CurrentProfile on the login because the session is not yet established
            var profile = _context.Profiles.FirstOrDefault(p => p.AccountId == user.Id);

            if (profile == null) {
                ModelState.AddModelError("",
                    "Your account is in an invalid state. Please contact support.");
                ErrorHandler.Capture(_other.SentryDSN,
                    new Exception(
                        "The user login does not have an associated profile."),
                    HttpContext,
                    customData: new Dictionary<string, string>() { { "AttemptedLoginUsername", model.Email } });
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                await _signInManager.SignOutAsync();
                _context.LoginAttempts.Add(new LoginAttempt() {
                    AccountId = profile.AccountId,
                    AttemptDate = DateTime.UtcNow,
                    Email = model.Email,
                    IP = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    Success = false,
                });

                if (!ModelState.IsValid) return View(model);

                // Profile is not null, log the attempt
                _context.LoginAttempts.Add(new LoginAttempt() {
                    AccountId = profile.AccountId,
                    AttemptDate = DateTime.UtcNow,
                    Email = model.Email,
                    IP = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    Success = true,
                });
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(returnUrl)) return RedirectToLocal(returnUrl);
            }

			if (!string.IsNullOrEmpty(returnUrl)) return RedirectToLocal(returnUrl);

			// If the user doesn't have any pending or active plans, redirect to the start of plan page
			var plans = await _context.Plans.Where(x => x.ProfileId == profile.Id && (x.Status == PlanStatus.Pending || x.Status == PlanStatus.Active)).ToListAsync();

			if(plans.Count == 0) {
				return RedirectToAction(nameof(PlanController.Index), PlanController.Name);
			} else {
				return RedirectToAction(nameof(HomeController.Index), "Home");
			}

            //if (result.RequiresTwoFactor)
            //{
            //    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
            //}
        }
        #endregion

        #region 2fa
        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        //{
        //    // Ensure the user has gone through the username & password screen first
        //    var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        //    if (user == null)
        //    {
        //        throw new ApplicationException($"Unable to load two-factor authentication user.");
        //    }

        //    var model = new LoginWith2faViewModel { RememberMe = rememberMe };
        //    ViewData["ReturnUrl"] = returnUrl;

        //    return View(model);
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        //    if (user == null)
        //    {
        //        throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        //    }

        //    var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        //    var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

        //    if (result.Succeeded)
        //    {
        //        _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
        //        return RedirectToLocal(returnUrl);
        //    }
        //    else if (result.IsLockedOut)
        //    {
        //        _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        //        return RedirectToAction(nameof(Lockout));
        //    }
        //    else
        //    {
        //        _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
        //        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        //        return View();
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        //{
        //    // Ensure the user has gone through the username & password screen first
        //    var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        //    if (user == null)
        //    {
        //        throw new ApplicationException($"Unable to load two-factor authentication user.");
        //    }

        //    ViewData["ReturnUrl"] = returnUrl;

        //    return View();
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        //    if (user == null)
        //    {
        //        throw new ApplicationException($"Unable to load two-factor authentication user.");
        //    }

        //    var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

        //    var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        //    if (result.Succeeded)
        //    {
        //        _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
        //        return RedirectToLocal(returnUrl);
        //    }
        //    if (result.IsLockedOut)
        //    {
        //        _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        //        return RedirectToAction(nameof(Lockout));
        //    }
        //    else
        //    {
        //        _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
        //        ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
        //        return View();
        //    }
        //}
        #endregion

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout() {
            return View();
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string r = "", string returnUrl = null) {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["InviteOn"] = _other.InviteOnly;
            ViewData["r"] = r;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model,string returnUrl = null) {
            // Check email 
            await UserRegistrationEmailValidate(model.Email);

			var strippedPhone = StaticUtils.NumericStrip(model.Phone);
			if (strippedPhone.StartsWith("0") || strippedPhone.StartsWith("1") || strippedPhone.Length < 10) {
				ModelState.AddModelError("", "Please enter a valid US phone number.");
			}

            // If invite codes are on
            if (_other.InviteOnly) {
                var valid = await _context.InviteCodes.AnyAsync(i => i.Code == model.InviteCode);
                if (string.IsNullOrEmpty(model.InviteCode) || !valid) {
                    ModelState.AddModelError("", "The invite code you provided is not valid. An invite code is required for signup.");
                }
			}
			// Check if existing account with phone number
			//var existingUser = await _context.Profiles.FirstOrDefaultAsync(x => x.Phone == StaticUtils.NumericStrip(model.Phone) && !(x.IsArchive ?? false));
			//if (existingUser != null) {
			//	ModelState.AddModelError("", "There is already a user using that phone number. Please login with that account.");
			//}

			if (ModelState.IsValid) {
	            try {
		            var user = new ApplicationUser {UserName = model.Email, Email = model.Email};
		            var result = await _userManager.CreateAsync(user, model.Password);
		            if (result.Succeeded) {
			            // All users default to trial permissions
			            await _userManager.AddToRoleAsync(user, Roles.Trial);

						var phoneVerificationCode = RandomCode.RandomString(6);

						var profile = new Profile {
							AccountId = user.Id,
							IDVerifyStatus = IDVerifyStatus.NotStarted,
							Phone = StaticUtils.NumericStrip(model.Phone),
							LastModified = DateTime.UtcNow,
							CustomerType = CustomerType.EpicPay,
							ProfileType = ProfileType.Normal,
							Referral = model.ReferralCode,
							PhoneVerified = false,
							PhoneVerificationCode = phoneVerificationCode,
						};

						_context.Add(profile);
			            try {
				            await _context.SaveChangesAsync();
			            }
			            catch (Exception e) {
				            ModelState.AddModelError("", $"{e.Message} {e.InnerException?.Message}");
				            await _userManager.DeleteAsync(user);
				            await _context.SaveChangesAsync();
				            ViewData["ReturnUrl"] = returnUrl;
				            ViewData["InviteOn"] = _other.InviteOnly;
				            ViewData["r"] = model.ReferralCode;
				            return View(model);
			            }

						if (_other.EnablePhoneVerifications) {
							var userProfile = await _context.Profiles.Where(x => x.AccountId == user.Id).FirstOrDefaultAsync();

							if (userProfile != null) {
								try {
									var callback = string.Format("{0}://{1}{2}", Request.Scheme,
										Request.Host, Url.Action(nameof(CommunicationsController.SMSMessageCallback), "Communications"));

									var message = "Thank you for signing up for Lexvor. Please verify your phone when you login. Verification Code: " + phoneVerificationCode + ". https://lexvorwireless.com/";
									await TwilioService.Send(_other, _context, callback, userProfile.Id, userProfile.Phone,
										message);
								}
								catch (Exception e) {
									// Don't fail on error
									ErrorHandler.Capture(_other.SentryDSN, new Exception("Failed to send user phone validation", e), HttpContext);
								}
							}
						}

						await UserRegistrationConfirmation(user, model.Email);

			            // Send admin email
			            try {
				            await _emailSender.SendEmailAsync(new[] {"itadmin@lexvor.com", "customerservice@lexvor.com", "lexvor@lexvor.com"},
					            $"New User Signup", $"Email: {model.Email}. Phone: {model.Phone}");
			            }
			            catch (Exception e) {
				            // Don't fail on error
				            ErrorHandler.Capture(_other.SentryDSN, new Exception("Failed to send new user notification email", e), HttpContext);
			            }

			            if (!string.IsNullOrWhiteSpace(returnUrl)) {
							return RedirectToLocal(returnUrl);
			            }
			            else {
							return RedirectToAction(nameof(PlanController.Index), PlanController.Name);
			            }
		            }

		            AddErrors(result);
	            }
	            catch (Exception e) {
		            ErrorHandler.Capture(_other.SentryDSN, e, area: "Signup");
					ModelState.AddModelError("", e.InnerException?.Message ?? e.Message);
	            }
            }

            // If we got this far, something failed, redisplay form
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["InviteOn"] = _other.InviteOnly;
            ViewData["r"] = model.ReferralCode;
            return View(model);
        }

        private async Task UserRegistrationEmailValidate(string email) {
            var validEmail = Regex.IsMatch(email, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
            if (!validEmail) {
                ModelState.AddModelError("", "The email address you entered does not appear to be valid. Please double-check it.");
            } else {
                try {
                    var result = await ValidateEmail(email);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                    if (dict.ContainsKey("is_disposable_address") && bool.Parse(dict["is_disposable_address"].ToString())) {
                        ModelState.AddModelError("", "You cannot sign up with a disposable address.");
                    }
                    if (dict.ContainsKey("result") && dict["result"].ToString() != "deliverable") {
                        ModelState.AddModelError("", "Your email address does not appear to be valid. Please double-check it.");
                    }
                } catch (Exception) {

                }
            }

            if (!_other.EmailEndings.Any(x => email.EndsWith(x))) {
                ModelState.AddModelError("", "You cannot sign up with that email, please choose a different one.");
            }
        }

        private async Task UserRegistrationConfirmation(ApplicationUser user, string email) {
            string code = "";
            string callBack = "";
            try {
                code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmCode");
            }
            try {
                callBack = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmCallBack");
            }
            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(callBack)) {
                try {
                    await EmailService.SendEmailConfirmation(_emailSender, _other, email, callBack);
                } catch (Exception e) {
                    ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "EmailConfirmSend");
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        [HttpGet]
        public async Task<IActionResult> Logout() {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login), "Account");
        }

        #region ExternalLogin
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public IActionResult ExternalLogin(string provider, string returnUrl = null) {
        //    // Request a redirect to the external login provider.
        //    var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        //    var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        //    return Challenge(properties, provider);
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null) {
        //    if (remoteError != null) {
        //        ErrorMessage = $"Error from external provider: {remoteError}";
        //        return RedirectToAction(nameof(Login));
        //    }
        //    var info = await _signInManager.GetExternalLoginInfoAsync();
        //    if (info == null) {
        //        return RedirectToAction(nameof(Login));
        //    }

        //    // Sign in the user with this external login provider if the user already has a login.
        //    var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        //    if (result.Succeeded) {
        //        _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
        //        return RedirectToLocal(returnUrl);
        //    }
        //    if (result.IsLockedOut) {
        //        return RedirectToAction(nameof(Lockout));
        //    } else {
        //        // If the user does not have an account, then ask the user to create an account.
        //        ViewData["ReturnUrl"] = returnUrl;
        //        ViewData["LoginProvider"] = info.LoginProvider;
        //        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        //        return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
        //    }
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null) {
        //    if (ModelState.IsValid) {
        //        // Get the information about the user from the external login provider
        //        var info = await _signInManager.GetExternalLoginInfoAsync();
        //        if (info == null) {
        //            throw new ApplicationException("Error loading external login information during confirmation.");
        //        }
        //        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        //        var result = await _userManager.CreateAsync(user);
        //        if (result.Succeeded) {
        //            result = await _userManager.AddLoginAsync(user, info);
        //            if (result.Succeeded) {
        //                await _signInManager.SignInAsync(user, isPersistent: false);
        //                _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
        //                return RedirectToLocal(returnUrl);
        //            }
        //        }
        //        AddErrors(result);
        //    }

        //    ViewData["ReturnUrl"] = returnUrl;
        //    return View(nameof(ExternalLogin), model);
        //}
        #endregion

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code) {
            if (userId == null || code == null) {
                ErrorHandler.Capture(_other.SentryDSN, new Exception($"Code or user id was null. Query String: {Request.QueryString.Value}"), HttpContext, "EmailConfirm");
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                ErrorHandler.Capture(_other.SentryDSN, new Exception($"Unable to load user with ID {userId}. This may be because this userid is in staging and not in production."), HttpContext, "EmailConfirm");
                return RedirectToAction("Index", "Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded) {
				// TODO This should not be here. Only activate a user once the ID documents are uploaded.
	            await _userManager.AddToRoleAsync(user, Roles.User);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            } else {
	            ErrorHandler.Capture(_other.SentryDSN, new Exception($"Confirm email for user {userId} had the following error: {result.Errors.First().Description}"), HttpContext, "EmailConfirm");
                throw new Exception($"Your email could not be verified. Reason: {result.Errors.First().Description}");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) {
            if (ModelState.IsValid) {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null) {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
                if (!(await _userManager.IsEmailConfirmedAsync(user))) {
                    var emailCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callback = Url.EmailConfirmationLink(user.Id, emailCode, Request.Scheme);
                    await EmailService.SendEmailConfirmation(_emailSender, _other, model.Email, callback);

                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await EmailService.SendResetPassword(_emailSender, _other, model.Email, callbackUrl);

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ResendEmailConfirm() {
	        var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
	        if (!(await _userManager.IsEmailConfirmedAsync(user))) {
		        var emailCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		        var callback = Url.EmailConfirmationLink(user.Id, emailCode, Request.Scheme);
		        await EmailService.SendEmailConfirmation(_emailSender, _other, CurrentUserEmail, callback);
	        }
	        return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null) {
            if (code == null) {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded) {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() {
            return View();
        }

        public async Task<string> ValidateEmail(string email) {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mailgun.net/v4/address/validate?address={email}");
            var byteArray = Encoding.ASCII.GetBytes("api:key-1f6957f5ab5550bb9fbcd248d9d527a8");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var response = await Http.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }

        #region Helpers

        private void AddErrors(IdentityResult result) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}
