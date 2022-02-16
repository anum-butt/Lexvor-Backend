using Lexvor.API;
using Lexvor.Data;
using Lexvor.Eversign;
using Lexvor.Eversign.Models.Requests;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.Extensions;

namespace Lexvor.Controllers {
	public class EversignController : BaseUserController {
		private readonly OtherSettings _other;

		public static string Name => "Eversign";

		public EversignController(
			UserManager<ApplicationUser> userManager,
			IOptions<OtherSettings> settings,
			ApplicationDbContext context) : base(context, userManager) {
			_other = settings.Value;
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> SignPlanAgreements(Guid planId) {
			try {
				var everSignClient = new EversignClient(_other.EversignBaseUrl, _other.EversignAccessKey, _other.EversignBusinessId);
				string embeddedSigningUrl = string.Empty;
				var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
				var plan = await _context.Plans.Include(p => p.Profile)
					.ThenInclude(p => p.Account)
					.Include(p => p.PlanType)
					.FirstOrDefaultAsync(p => p.Id == planId && p.ProfileId == CurrentProfile.Id);

				if (plan == null || (plan.AgreementSigned && !string.IsNullOrEmpty(plan.AgreementUrl))) {
					ErrorHandler.Capture(_other.SentryDSN, new Exception($"Invalid plan {plan?.AgreementSigned} {plan?.AgreementUrl}"), HttpContext, "Eversign");
					ErrorMessage = "There was an error when trying to sign your documents. Contact support.";
					return RedirectToAction(nameof(HomeController.Index), HomeController.Name);

				}

				if (!string.IsNullOrEmpty(plan.AgreementUrl)) {
					var document = await everSignClient.GetDocument(plan.AgreementUrl);
					embeddedSigningUrl = document.Signers.FirstOrDefault(s => s.Email == CurrentUserEmail)?.EmbeddedSigningUrl;
				}
				else {
					var getDocumentResponse = await everSignClient.GetDocument(_other.EversignTestTemplateHash);

					var useTemplateRequest = new UseTemplateRequest {
						EmbeddedSigningEnabled = 1,
						TemplateId = getDocumentResponse.DocumentHash,
						Signers = new List<Signer> {
							new Signer {
								Email = CurrentUserEmail,
								DeliverEmail = 1,
								Language = getDocumentResponse.Signers.FirstOrDefault().Language,
								Message = getDocumentResponse.Signers.FirstOrDefault().Message,
								Role = getDocumentResponse.Signers.FirstOrDefault().Role,
								Name = $"{plan.Profile.FirstName} {plan.Profile.LastName}"
							}
						},
						Sandbox = 0,
						Expires = int.MaxValue,
						Fields = new List<Field>() {
							new Field() {
								Identifier = "monthly",
								Value = $"${(plan.Monthly / 100).ToString("D")}"
							},
							new Field() {
								Identifier = "monthly_2",
								Value = $"${(plan.Monthly / 100).ToString("D")}"
							},
							new Field() {
								Identifier = "address1",
								Value = $"{CurrentProfile.BillingAddress.Line1} {CurrentProfile.BillingAddress.Line2}"
							},
							new Field() {
								Identifier = "address2",
								Value = $"{CurrentProfile.BillingAddress.City}, {CurrentProfile.BillingAddress.Provence} {CurrentProfile.BillingAddress.PostalCode}"
							},
							new Field() {
								Identifier = "phone",
								Value = CurrentProfile.Phone
							},
							new Field() {
								Identifier = "name",
								Value = $"{plan.Profile.FirstName} {plan.Profile.LastName}"
							},
							new Field() {
								Identifier = "transaction",
								Value = plan.Id.ToString()
							},
							new Field() {
								Identifier = "date",
								Value = DateTime.Now.ToString("d")
							},
							new Field() {
								Identifier = "due",
								Value = DateTime.Now.GetNextFirst().ToString("d")
							},
							new Field() {
								Identifier = "totalpayments",
								Value = plan.PlanType.TermLength.ToString()
							},
						}
					};

					var useTemplateResponse = await everSignClient.UseTemplate(useTemplateRequest);
					embeddedSigningUrl = useTemplateResponse.Signers.FirstOrDefault(s => s.Email == user.Email)?.EmbeddedSigningUrl;
					plan.AgreementUrl = useTemplateResponse.DocumentHash;
					await _context.SaveChangesAsync();
				}

				return View("SignEversignDocument", new SignEversignDocumentModel {
					EmbeddedSigningUrl = embeddedSigningUrl,
					PlanId = plan.Id
				});
			}
			catch(Exception ex) {
				ErrorHandler.Capture(_other.SentryDSN, ex, HttpContext, "Eversign");
				ErrorMessage = "There was an error when trying to sign your documents. Contact support.";
				return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> SigningComplete(Guid planId) {
			try {
				var everSignClient = new EversignClient(_other.EversignBaseUrl, _other.EversignAccessKey, _other.EversignBusinessId);
				var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
				var plan = await _context.Plans.Include(p => p.Profile)
					.ThenInclude(p => p.Account)
					.FirstOrDefaultAsync(p => p.Id == planId && p.Profile.Account.Id == user.Id);

				if (plan == null || string.IsNullOrEmpty(plan.AgreementUrl)) {
					throw new Exception("Invalid plan.");
				}

				var document = await everSignClient.GetDocument(plan.AgreementUrl);
				bool isSuccessfullySigned = plan.AgreementSigned;

				if (!isSuccessfullySigned && document.IsCompleted == 1) {
					plan.AgreementSigned = true;
					await _context.SaveChangesAsync();
					isSuccessfullySigned = true;
				}

				return new JsonResult(new { IsSuccessfullySigned = isSuccessfullySigned });
			}
			catch (Exception) {
				return new JsonResult(new { IsSuccessfullySigned = false });
			}
		}
	}
}
