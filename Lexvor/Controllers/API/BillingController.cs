using iTextSharp.text.pdf;
using Lexvor.API;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lexvor.Controllers.API {
	[Route("api/[controller]")]
	[ApiController]
	public class BillingController : BaseApiController {

		private ApplicationDbContext _context;
		private readonly OtherSettings _otherSettings;
		private readonly IEmailSender _emailSender;
		private readonly UserManager<ApplicationUser> userManager;
		private readonly IWebHostEnvironment _webhostenvironment;

		public BillingController(ApplicationDbContext context,
			IOptions<OtherSettings> otherSettings,
			IEmailSender emailSender,
			UserManager<ApplicationUser> userManager,
			IWebHostEnvironment webhostenvironment
			) : base(context, userManager) {
			_context = context;
			_otherSettings = otherSettings.Value;
			_emailSender = emailSender;
			_webhostenvironment = webhostenvironment;
		}

		[HttpGet("GenerateBillingInvoice")]
		public async Task<IActionResult> GenerateBillIngInvoice() {
			var activeUserPlan = await _context.Plans
				.Include(x => x.Profile)
				.Where(x => PlanService.ActiveStatuses.Contains(x.Status)).ToListAsync();


			foreach (var userPlan in activeUserPlan) {
				var userDocuments = await _context.UserDocuments.Where(x => x.GeneratedOn.Month - 1 == DateTime.Now.Month - 1 && x.ProfileId == userPlan.ProfileId && x.DocumentType == DocumentType.Bill).ToListAsync();
				if (userDocuments.Count <= 0) {
					var plans = _context.Plans
							.Include(x => x.UserDevice)
							.ThenInclude(x => x.StockedDevice)
							.ThenInclude(x => x.Device)
							.Where(x => x.Id == userPlan.Id).FirstOrDefault();

					var payaccount = _context.PayAccounts.Where(x => x.ProfileId == CurrentProfile.Id).FirstOrDefault();

					var billingInvoice = new BillingInvoice() {
						Profile = CurrentProfile,

						BillingInvoiceInfos = new BillingInvoiceInfo {
							AccountNumber = payaccount.MaskedAccountNumber,
							ActiveDevice = plans.UserDevice.StockedDevice.Device.Name,
							IMEI = plans.UserDevice.IMEI,
							MRC = userPlan.Monthly,
							Address = CurrentProfile.BillingAddress.Line1 + " " + "city" + CurrentProfile.BillingAddress.City + " " + CurrentProfile.BillingAddress.PostalCode
						}
					};
					string contentRootPath = _webhostenvironment.ContentRootPath;
					string pdfTemplate = "";

					pdfTemplate = Path.Combine(contentRootPath, "Template", "LexvorInvoice.pdf");

					var memoryStream = new MemoryStream();
					PdfReader pdfReader = new PdfReader(pdfTemplate);
					PdfStamper pdfStamper = new PdfStamper(pdfReader, memoryStream);
					AcroFields pdfFormFields = pdfStamper.AcroFields;

					pdfFormFields.SetField("ACCOUNT_NUMBERRow1", billingInvoice.BillingInvoiceInfos.AccountNumber);
					pdfFormFields.SetField("IMEIRow1", billingInvoice.BillingInvoiceInfos.IMEI);
					pdfFormFields.SetField("ACTIVE_DEVICERow1", billingInvoice.BillingInvoiceInfos.ActiveDevice);
					pdfFormFields.SetField("MRCRow1", billingInvoice.BillingInvoiceInfos.MRC.ToString());
					pdfFormFields.SetField("InvoiceDate", DateTime.Now.ToShortDateString());
					pdfFormFields.SetField("CustomerName", billingInvoice.Profile.FullName);
					pdfFormFields.SetField("CustomerPhone", billingInvoice.Profile.Phone);
					pdfFormFields.SetField("CustomerAddress", billingInvoice.BillingInvoiceInfos.Address);
					pdfStamper.FormFlattening = true;
					pdfStamper.Close();
					byte[] bytes = memoryStream.ToArray();
					string filePath = $"device-images/{"Invoice/" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + userPlan.Id + ".pdf"}";
					try {
						await BlobService.UploadBlob(bytes, filePath, _otherSettings);
						var userDoc = new UserDocuments() {
							Name = "test",
							DocumentType = DocumentType.Bill,
							URL = filePath,
							GeneratedOn = DateTime.Now,
							UserPlanId = userPlan.Id,
							ProfileId = userPlan.ProfileId
						};
						_context.UserDocuments.Add(userDoc);

						await _context.SaveChangesAsync();
						var url = await BlobService.GetPrivateBlobUrl(filePath, _otherSettings);
						var blob = await BlobService.DownloadBlobBytes(filePath, _otherSettings);
						await _emailSender.SendEmailAsync(CurrentUserEmail, "BillingInvoice", "Download the invoice to check your billing details", url, true, blob);
					}

					catch (Exception e) {
						ErrorHandler.Capture(_otherSettings.SentryDSN, e, "Billing-invoice-Pdf-upload");

					}

				};

			}

			return Ok();

		}
	}
}



