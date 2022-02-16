using Lexvor.API;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.HomeViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Lexvor.Controllers {
	[Authorize(Roles = Roles.Trial)]
	public class DocumentsController : BaseUserController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name => "Documents";

		public DocumentsController(ApplicationDbContext context,
						UserManager<ApplicationUser> userManager,
									IOptions<OtherSettings> other,
									IEmailSender emailSender

			) : base(context, userManager) {
			_other = other.Value;
			_emailSender = emailSender;
			_userManager = userManager;

		}

		public async Task<IActionResult> Index() {
			var user = GetCurrentAccount();
			var documents = _context.UserDocuments.Where(x => x.ProfileId == CurrentProfile.Id).OrderByDescending(x => x.GeneratedOn).ToList();
			var UserDocumentView = new List<UserDocumentView>();
			foreach (var doc in documents) {
				UserDocumentView.Add(new UserDocumentView {
					Id = doc.Id,
					DocumentName = doc.Name,
					DocumentType = doc.DocumentType,
					GeneratedOn = doc.GeneratedOn,
					URL = doc.URL,
					ViewedOn = doc.ViewedOn

				});

			}

			var userDocumentViewModel = new UserDocumentViewModel {
				Profile = CurrentProfile,
				UserDocumentsView = UserDocumentView,
				SettingViewModel = new Models.HomeViewModels.SettingsViewModel {
					User = GetCurrentAccount().Result,
					Profile = CurrentProfile,
					PayAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active)
				}
			};
			return View(userDocumentViewModel);
		}

		public async Task<IActionResult> DownloadUserDocument(string documentUrl) {

			var docBytes = await BlobService.DownloadBlobBytes(documentUrl, _other, true);
			Response.Headers.Add("Content-Disposition", "attachment; filename=" + documentUrl);
			return new FileStreamResult(new MemoryStream(docBytes), "application/pdf");


		}

		public async Task<IActionResult> ViewUserDocument(int? documentId) {

			var document = _context.UserDocuments.Where(x => x.Id == documentId).FirstOrDefault();
			document.ViewedOn = DateTime.Now;
			await _context.SaveChangesAsync();
			var url = await BlobService.GetPrivateBlobUrl(document.URL, _other);
			var docBytes = await BlobService.DownloadBlobBytes(document.URL, _other, true);
			return Json(new {
				pdfUrl = url
			});
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(SettingsViewModel model) {
			var user = await GetCurrentAccount();

			// Model for rerender on error
			var reModel = new SettingsViewModel() {
				Profile = CurrentProfile,
				User = user,
				PayAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active)
			};

			var acceptedExtensions = new[] { ".pdf", ".jpeg", ".jpg", ".png" };
			if (model.Upload1 != null && !acceptedExtensions.Contains(Path.GetExtension(model.Upload1.FileName).ToLower())) {
				ModelState.AddModelError("", "Your upload was not in the correct format (jpeg, png, pdf).");
			}

			if (ModelState.IsValid) {
				try {
					var blobPath = $"id-images/{CurrentProfile.Id}";

					// Upload the image to blob
					if (model.Upload1 != null) {
						var url = await IDVerifyService.UploadIDDocument(_other, model.Upload1, $"{blobPath}/{Guid.NewGuid()}");
						var doc = await _context.IdentityDocuments.AddAsync(new IdentityDocument() {
							DocumentUrl = url,
							Profile = CurrentProfile
						});

						try {
							var data = new byte[model.Upload1.Length];
							var stream = new MemoryStream();
							await model.Upload1.CopyToAsync(stream);
							data = stream.ToArray();

							var identity = await IDVerifyService.RunVerification(_context, _other, doc.Entity, data, CurrentProfile);
							_context.Add(identity);
							await _context.SaveChangesAsync();
						} catch (Exception e) {
							ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Settings-ID-Upload");
						}
					}

					CurrentProfile.IDVerifyStatus = IDVerifyStatus.Pending;
					await _userManager.AddToRoleAsync(user, Roles.User);

					await _context.SaveChangesAsync();

					// Send admin email
					await EmailService.SendIDVerifyAdminEmail(_emailSender, "customerservice@lexvor.com", user.Email);
				} catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Upload");

					ModelState.AddModelError("", "There was a problem uploading your ID documents please email them to support instead.");
					return View(reModel);
				}

				return RedirectToAction(nameof(DocumentsController.Index));
			}
			var documents = _context.UserDocuments.Where(x => x.ProfileId == CurrentProfile.Id).OrderByDescending(x => x.GeneratedOn).ToList();
			var UserDocumentView = new List<UserDocumentView>();
			foreach (var doc in documents) {
				UserDocumentView.Add(new UserDocumentView {
					Id = doc.Id,
					DocumentName = doc.Name,
					DocumentType = doc.DocumentType,
					GeneratedOn = doc.GeneratedOn,
					URL = doc.URL,
					ViewedOn = doc.ViewedOn

				});
			}
			var userDocumentModel = new UserDocumentViewModel() {
				Profile = CurrentProfile,
				UserDocumentsView = UserDocumentView,
				SettingViewModel = reModel
			};
			return View(userDocumentModel);

		}

		public async Task<IActionResult> Settings(SettingsViewModel model) {
			var user = await GetCurrentAccount();

			// Model for rerender on error
			var reModel = new SettingsViewModel() {
				Profile = CurrentProfile,
				User = user,
				PayAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active)
			};

			var acceptedExtensions = new[] { ".pdf", ".jpeg", ".jpg", ".png" };
			if (model.Upload1 != null && !acceptedExtensions.Contains(Path.GetExtension(model.Upload1.FileName).ToLower())) {
				ModelState.AddModelError("", "Your upload was not in the correct format (jpeg, png, pdf).");
			}

			if (ModelState.IsValid) {
				try {
					var blobPath = $"id-images/{CurrentProfile.Id}";

					// Upload the image to blob
					if (model.Upload1 != null) {
						var url = await IDVerifyService.UploadIDDocument(_other, model.Upload1, $"{blobPath}/{Guid.NewGuid()}");
						var doc = await _context.IdentityDocuments.AddAsync(new IdentityDocument() {
							DocumentUrl = url,
							Profile = CurrentProfile
						});

						try {
							var data = new byte[model.Upload1.Length];
							var stream = new MemoryStream();
							await model.Upload1.CopyToAsync(stream);
							data = stream.ToArray();

							var identity = await IDVerifyService.RunVerification(_context, _other, doc.Entity, data, CurrentProfile);
							_context.Add(identity);
							await _context.SaveChangesAsync();
						} catch (Exception e) {
							ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Settings-ID-Upload");
						}
					}

					CurrentProfile.IDVerifyStatus = IDVerifyStatus.Pending;
					await _userManager.AddToRoleAsync(user, Roles.User);

					await _context.SaveChangesAsync();

					// Send admin email
					await EmailService.SendIDVerifyAdminEmail(_emailSender, "customerservice@lexvor.com", user.Email);
				} catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "ID-Upload");

					ModelState.AddModelError("", "There was a problem uploading your ID documents please email them to support instead.");
					return View(reModel);
				}

				return RedirectToAction(nameof(DocumentsController.Index));
			}

			return View(reModel);
		}
	}
}


