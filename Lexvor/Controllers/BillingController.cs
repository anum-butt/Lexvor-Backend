using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acklann.Plaid;
using Acklann.Plaid.Balance;
using Acklann.Plaid.Management;
using iTextSharp.text.pdf;
using Lexvor.API;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.HomeViewModels;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Controllers {
	public class BillingController : BaseUserController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ConnectionStrings _connStrings;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;
		private readonly string _defaultConn;

		public static string Name => "Billing";
		private readonly IWebHostEnvironment _webHostEnvironment;

		public BillingController(
			IWebHostEnvironment webHostEnvironment,
			UserManager<ApplicationUser> userManager,
			IEmailSender emailSender,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager) {
			_userManager = userManager;
			_emailSender = emailSender;
			_connStrings = connStrings.Value;
			_other = other.Value;
			_webHostEnvironment = webHostEnvironment;
			_defaultConn = _connStrings.DefaultConnection;
		}

		[Authorize(Roles = Roles.User)]
		public async Task<ActionResult> UpdateConnection() {
			var payAccount =
			  await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);

			if (payAccount == null) {
				ErrorMessage = "You do not have a connected bank account and so cannot update the connection";
				return RedirectToAction(nameof(Index));
			}

			Enum.TryParse(_other.PlaidEnv, out Acklann.Plaid.Environment env);

			var client = new PlaidClient(env);
			var settings = await _context.ProfileSettings.Where(x => x.ProfileId == CurrentProfile.Id).ToListAsync();
			var token = settings.Count() > 0 ? settings.Where(x => x.SettingName == "plaid_access").FirstOrDefault() : null;

			CreateLinkTokenResponse accessToken = null;
			if (token != null) {
				accessToken = await client.CreateLinkToken(new CreateLinkTokenRequest() {
					ClientId = _other.PlaidClientId,
					AccessToken = token.SettingValue,
					Secret = _other.PlaidSecret,
					User = new CreateLinkTokenRequest.UserInfo() { ClientUserId = payAccount.ExternalReferenceNumber },
					ClientName = "Lexvor",
					CountryCodes = new string[] { "US" },
					Language = "en"
				});
			}
			return RedirectToAction("Index", new { accesstoken = accessToken.LinkToken });
		}


		[Authorize(Roles = Roles.User)]
		public async Task<IActionResult> Index(string returnUrl = "", string accesstoken = null) {
			// TODO: This page will also allow refreshing of plaid connections.
			var payAccount =
				await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);
			var address = CurrentProfile.BillingAddress;
			var pastCharges = await _context.Charges.Where(x => x.ProfileId == CurrentProfile.Id && x.Status == ChargeStatus.Charged).ToListAsync();

			var token = await _context.ProfileSettings.Where(x => x.ProfileId == CurrentProfile.Id && x.SettingName == "plaid_access").FirstOrDefaultAsync();

			GetAccountResponse accInfo = null;
			if (token != null) {
				Enum.TryParse(_other.PlaidEnv, out Acklann.Plaid.Environment env);
				var client = new PlaidClient(env);
				accInfo = await client.FetchAccountAsync(new GetAccountRequest() {
					AccessToken = token.SettingValue,
					ClientId = _other.PlaidClientId,
					Secret = _other.PlaidSecret
				});
			}

			ViewData["ReturnUrl"] = returnUrl;

			if (!string.IsNullOrEmpty(returnUrl)) {
				Message = "Please complete your billing address before continuing your purchase.";
			}

			return View(new BillingIndexViewModel() {
				CurrentBankAccount = payAccount,
				CurrentBillingAddress = address,
				PastCharges = pastCharges,
				NextBillingDate = CurrentProfile.NextBillDate
			});
		}

		[Authorize(Roles = Roles.User)]
		public async Task<IActionResult> Edit() {
			var payAccount =
				await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);
			if (payAccount != null && payAccount.Confirmed) {
				ErrorMessage = "You need to contact support to edit a confirmed bank account";
				return RedirectToAction(nameof(Index));
			}

			if (payAccount != null) {
				// Archive the current pay account.
				payAccount.Active = false;
				payAccount.LastUpdated = DateTime.UtcNow;
				payAccount.Archived = true;

				await _context.SaveChangesAsync();
			}

			// Go through the payments flow with redirect back here.
			//return RedirectToAction(nameof(PurchaseController.ActionNavigator), PaymentsController.Name, new { updateAcc = true, returnUrl = Url.Action(nameof(EditComplete)) });
			return RedirectToAction(nameof(PaymentsController.BankAccountInformation), PaymentsController.Name, new { returnUrl = Url.Action(nameof(EditComplete)) });
		}

		[Authorize(Roles = Roles.User)]
		public async Task<IActionResult> EditComplete() {
			var payAccount =
				await _context.PayAccounts.FirstOrDefaultAsync(x => x.ProfileId == CurrentProfile.Id && x.Active);
			// If the new account was activated, do nothing. If not, reset last archived account to the active one.
			if (payAccount == null) {
				var archived = await _context.PayAccounts.Where(x => x.ProfileId == CurrentProfile.Id && x.Archived).OrderByDescending(x => x.LastUpdated).FirstOrDefaultAsync();
				if (archived != null) {
					archived.Archived = false;
					archived.LastUpdated = DateTime.UtcNow;
					archived.Active = true;
					await _context.SaveChangesAsync();
				}
				else {
					ErrorHandler.Capture(_other.SentryDSN, new Exception($"User had no previous bank account and the current bank account save failed. {CurrentProfile.Id}"), HttpContext, "Billing-Edit-Complete");
				}
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		public async Task<IActionResult> AddressUpdate(BillingIndexViewModel model, string returnUrl = "") {
			try {
				// TODO this is terrible shit. Fix it later.
				try {
					var address = await AddressVerificationService.Verify(model.CurrentBillingAddress, _other.USPSWebtoolUserID);
					address.ProfileId = CurrentProfile.Id;
					// TODO move to db default value
					address.LastUpdated = DateTime.UtcNow;
					if (string.IsNullOrEmpty(returnUrl)) {
						// Only reset status if returnurl is empty. If it is not, then we were redirects from the purchase flow
						// and want to ignore the ID verify condition.
						CurrentProfile.IDVerifyStatus = IDVerifyStatus.ReverificationRequired;
					}
					_context.Addresses.Add(address);
					await _context.SaveChangesAsync();

					CurrentProfile.BillingAddressId = address.Id;
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "AddressUpdate");
					// Set address to user input
					var entity = _context.Addresses.Add(new Address() {
						ProfileId = CurrentProfile.Id,
						Line1 = model.CurrentBillingAddress.Line1,
						Line2 = model.CurrentBillingAddress.Line2,
						City = model.CurrentBillingAddress.City,
						Provence = model.CurrentBillingAddress.Provence,
						PostalCode = model.CurrentBillingAddress.PostalCode,
						Source = AddressSource.UserInput
					});
					await _context.SaveChangesAsync();
					CurrentProfile.BillingAddressId = entity.Entity.Id;
					CurrentProfile.IDVerifyStatus = IDVerifyStatus.ReverificationRequired;
				}

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "AddressUpdate");
				ErrorMessage = "Could not verify your billing address. Please confirm that you typed it in correctly.";
				return RedirectToAction(nameof(Index), new { returnUrl });
			}

			if (string.IsNullOrEmpty(returnUrl)) {
				return RedirectToAction(nameof(Index), Name);
			}
			else {
				return RedirectToLocal(returnUrl);
			}
		}

		[Authorize(Roles = Roles.Trial)]
		public async Task<IActionResult> BillingProfile(string returnUrl = "") {
			var user = await GetCurrentAccount();
			ViewData["ReturnUrl"] = returnUrl;

			// If billing profile is filled out, do the redirect
			if (CurrentProfile.BillingAddressId.HasValue) {
				return Redirect(returnUrl);
			}

			return View(new BillingProfileViewModel() {
				Profile = CurrentProfile,
				User = user,
				GoogleMapsKey = _other.GoogleApiKey
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.Trial)]
		public async Task<IActionResult> BillingProfile(BillingProfileViewModel model, string returnUrl = "") {
			ViewData["ReturnUrl"] = returnUrl;

			if (!ModelState.IsValid) return View(model);

			// TODO: This is all terrible EF shit. Fix it.
			var address = model.Profile.BillingAddress;
			try {
				address = await AddressVerificationService.Verify(model.Profile.BillingAddress, _other.USPSWebtoolUserID);
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "BillingProfile-AddressVerify");
				address = model.Profile.BillingAddress;
				address.Source = AddressSource.UserInput;
			}

			address.ProfileId = CurrentProfile.Id;
			// TODO move to db default value
			address.LastUpdated = DateTime.UtcNow;
			address.PostalCode = model.Profile.BillingAddress.PostalCode;
			_context.Addresses.Add(address);

			CurrentProfile.BillingAddress = address;
			CurrentProfile.Phone = StaticUtils.NumericStrip(model.Profile.Phone);
			CurrentProfile.FirstName = model.Profile.FirstName;
			CurrentProfile.LastName = model.Profile.LastName;

			var userIdentity = await _context.Identities.FirstOrDefaultAsync(x => x.Profile.Id == CurrentProfile.Id);

			if (userIdentity != null) {
				if (userIdentity.FirstName.Equals(CurrentProfile.FirstName, StringComparison.OrdinalIgnoreCase) && userIdentity.LastName.Equals(CurrentProfile.LastName, StringComparison.OrdinalIgnoreCase)) {
					userIdentity.Profile.IDVerifyStatus = IDVerifyStatus.Verified;
				}
				else if (!userIdentity.FirstName.Equals(CurrentProfile.FirstName, StringComparison.OrdinalIgnoreCase)) {
					ErrorMessage = "The First Name provided does not match the First Name on your uploaded Identity document.";
				}
				else if (!userIdentity.LastName.Equals(CurrentProfile.LastName, StringComparison.OrdinalIgnoreCase)) {
					ErrorMessage = "The Last Name provided does not match the Last Name on your uploaded Identity document.";
				}
			}

			await _context.SaveChangesAsync();

			return Redirect(returnUrl);
		}

		public async Task<IActionResult> GenrateBillingInvoice() {
			var id = CurrentProfile.Id;
			var payAccount = _context.PayAccounts.FirstOrDefault(x => x.ProfileId == id);
			var activePlans = await _context.Plans.Include(x => x.UserDevice).Include(x => x.Device).Where(x => x.ProfileId == id && PlanService.ActiveStatuses.Contains(x.Status)).ToListAsync();

			if (activePlans.Count == 0) {
				ErrorMessage = "You have no active plans, so we cannot generate an invoice.";
				return RedirectToAction(nameof(Index));
			}

			var billingInvoice = activePlans.Select(x => new BillingInvoice() {
					Profile = CurrentProfile,
					BillingInvoiceInfos = new BillingInvoiceInfo {
						AccountNumber = payAccount.MaskedAccountNumber,
						ActiveDevice = x.Device?.Name ?? "BYOD",
						IMEI = x.UserDevice.IMEI,
						MRC = x.Monthly,
						Address = CurrentProfile.BillingAddress.Line1 + " " + "City" + CurrentProfile.BillingAddress.City + " " + CurrentProfile.BillingAddress.PostalCode
					}
				}
			);
			string contentRootPath = _webHostEnvironment.ContentRootPath;
			string pdfTemplate = "";
			string InvoicePdf = "";

			pdfTemplate = Path.Combine(contentRootPath, "Template", "LexvorInvoice.pdf");
			InvoicePdf = Path.Combine(contentRootPath, "Template", "LexvorInvoices.pdf");

			PdfReader pdfReader = new PdfReader(pdfTemplate);
			PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(InvoicePdf, FileMode.Create));
			pdfStamper.FormFlattening = true;
			AcroFields pdfFormFields = pdfStamper.AcroFields;

			var counter = 1;
			var total = 0;
			foreach (var invoice in billingInvoice) {
				pdfFormFields.SetField($"ACCOUNT_NUMBERRow{counter}", invoice.BillingInvoiceInfos.AccountNumber);
				pdfFormFields.SetField($"IMEIRow{counter}", invoice.BillingInvoiceInfos.IMEI);
				pdfFormFields.SetField($"ACTIVE_DEVICERow{counter}", invoice.BillingInvoiceInfos.ActiveDevice);
				pdfFormFields.SetField($"MRCRow{counter}", (invoice.BillingInvoiceInfos.MRC / 100).ToString("C"));
				counter++;
				total += invoice.BillingInvoiceInfos.MRC;
			}
			pdfFormFields.SetField("Total", (total / 100).ToString("C"));

			pdfFormFields.SetField("InvoiceDate", DateTime.Now.ToShortDateString());
			pdfFormFields.SetField("CustomerName", billingInvoice.First().Profile.FullName);
			pdfFormFields.SetField("CustomerPhone", billingInvoice.First().Profile.Phone);
			pdfFormFields.SetField("CustomerAddress", billingInvoice.First().BillingInvoiceInfos.Address);
			pdfStamper.Close();
			var stream = new FileStream(InvoicePdf, FileMode.Open);
			return new FileStreamResult(stream, "application/pdf");
		}
	}
}
