using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Controllers.API {

	[ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ProfileController : BaseApiController {
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IOptions<OtherSettings> other,
            IEmailSender emailSender) : base(context, userManager) {
            _other = other.Value;
            _emailSender = emailSender;
        }


        [HttpPost("UploadDocument")]
        public async Task<IActionResult> UploadDocument(List<IFormFile> files) {
            var foundFiles = Request.Form.Files;
            if (foundFiles.Count == 0) {
                return BadRequest("No files uploaded");
            }

            var acceptedExtensions = new[] { ".pdf", ".jpeg", ".jpg", ".png" };
            if (!acceptedExtensions.Contains(Path.GetExtension(foundFiles.First().FileName).ToLower())) {
                return BadRequest("Your upload was not in the correct format (jpeg, png, pdf).");
            }
			try {
				var user = await GetCurrentAccount();
				var blobPath = $"id-images/{CurrentProfile.Id}";

				// Upload the image to blob
				var url = await IDVerifyService.UploadIDDocument(_other, foundFiles.First(), $"{blobPath}/{Guid.NewGuid()}");
				var idDoc = await _context.IdentityDocuments.AddAsync(new IdentityDocument() {
					DocumentUrl = url,
					Profile = CurrentProfile
				});
				await _context.SaveChangesAsync();

				await _userManager.AddToRoleAsync(user, Roles.User);
				
				CurrentProfile.IDVerifyStatus = IDVerifyStatus.Pending;
				await _context.SaveChangesAsync();

				// Send admin email
				await EmailService.SendIDVerifyAdminEmail(_emailSender, "customerservice@lexvor.com", user.Email);

				// Run id authenticity check
				// DO NOT RUN THIS NOW. Run it after the user connects Plaid.
				//try {
				//	var data = new byte[foundFiles.First().Length];
				//	var stream = new MemoryStream();
				//	await foundFiles.First().CopyToAsync(stream);
				//	data = stream.ToArray();

				//	var callback = string.Format("{0}://{1}{2}", Request.Scheme,
				//		Request.Host, Url.Action(nameof(ProfileController.IDCallback), "Profile", new { id = idDoc.Entity.Id }));

				//	await IDVerifyService.RunVerificationAsync(_context, _other, idDoc.Entity, data, CurrentProfile, callback);
				//}
				//catch (Exception e) {
				//	ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Check");
				//	// Just log the error and continue. We have all we need to re-trigger if needed.
				//}

            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Upload");

                ModelState.AddModelError("", "There was a problem uploading your ID documents please email them to support instead.");
            }

            return Ok();
		}

		[HttpPost("IDCallback/{id}")]
		[AllowAnonymous]
		public async Task<IActionResult> IDCallback(string id) {
			using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
				var json = await reader.ReadToEndAsync();

				// Save the raw response
				try {
					_context.Add(new WebhookResponseObject() {
						ObjectType = "IdentityDocumentCB",
						ObjectId = id,
						Received = DateTime.UtcNow,
						ReceivedAction = "Id Callback",
						Text = json
					});
					await _context.SaveChangesAsync();
				} catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, area: "Id Authenticity Check");
				}

				var identityDocument = await _context.IdentityDocuments.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

				var identity = await IDVerifyService.ParseCallbackResponse(_other, identityDocument, identityDocument.Profile, json);

				_context.Add(identity);
				await _context.SaveChangesAsync();

				// Run profile filling
				var profile = await _context.Profiles.Include(x => x.BillingAddress).FirstAsync(x => x.Id == identityDocument.Profile.Id);
				if (profile.FirstName.IsNull() && !identity.FirstName.IsNull()) {
					profile.FirstName = identity.FirstName;
				}
				if (profile.LastName.IsNull() && !identity.LastName.IsNull()) {
					profile.LastName = identity.LastName;
				}
				if (profile.BillingAddress == null && identity.Address != null) {
					profile.BillingAddress = identity.Address;
				}
				await _context.SaveChangesAsync();

				return Ok();
			}
		}

		[HttpGet("RunIDCheck/{id}")]
        public async Task<IActionResult> RunIDCheck(Guid id, string returnUrl = "") {
			// Identity Document
			var idDoc = await _context.IdentityDocuments.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == id);

			if (idDoc != null) {
				// Run id authenticity check
				try {
					var data = await BlobService.DownloadBlobBytes(idDoc.DocumentUrl, _other, true);

					var identity = await IDVerifyService.RunVerification(_context, _other, idDoc, data, idDoc.Profile);
					_context.Add(identity);
					await _context.SaveChangesAsync();
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Check");
					// Just log the error and continue. We have all we need to re-trigger if needed.
				}
			}

			if (returnUrl.IsNull()) {
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}
			else {
				return LocalRedirect(returnUrl);
			}
        }
		
		[AllowAnonymous]
        [HttpGet("SendPushEmails/{token}")]
        public async Task<IActionResult> SendPushEmails(string token) {
	        // Get every user that signed up between 3 and 5 days ago
	        if (token == "4398fvnvf78ngrkjco8r") {
		        var users = await _context.Profiles.Include(x => x.Account).Where(x => x.DateJoined > DateTime.UtcNow.AddDays(-5) && x.DateJoined > DateTime.UtcNow.AddDays(-3)).ToListAsync();
		        var profileIds = users.Select(x => x.Id).ToList();
		        var plans = await _context.Plans.Where(x => profileIds.Contains(x.Profile.Id)).ToListAsync();

		        foreach (var user in users) {
					// User has no plans
			        if (!plans.Any(x => x.Profile.Id == user.Id)) {
						// Check email log for an email
						var emailSent = await _context.EmailLogs.AnyAsync(x => x.Profile.Id == user.Id && x.EmailType == EmailType.PingEmail);
						if (!emailSent) {
							// Send email
							await EmailService.SendUserPushEmail(_emailSender, _other, user.Account.Email);
							_context.EmailLogs.Add(new EmailLog() {
								Profile = user,
								EmailType = EmailType.PingEmail,
								Sent = DateTime.UtcNow,
								// Not used right now
								Success = false
							});
							await _context.SaveChangesAsync();
						}
			        }
		        }
	        }

	        return new JsonResult("Success");
        }
    }
}
