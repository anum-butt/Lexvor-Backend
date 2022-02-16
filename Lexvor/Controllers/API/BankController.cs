using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Payment;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Lexvor.Controllers.API {
	[ApiController]
	[Route("api/[controller]")]
	public class BankController : BaseApiController {
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name = "Bank";

		public BankController(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IOptions<OtherSettings> other,
			IEmailSender emailSender) : base(context, userManager) {
			_other = other.Value;
			_emailSender = emailSender;
		}

		[HttpGet]
		[Authorize(Roles = Roles.Admin)]
		[Authorize(Roles = Roles.TrustedManager)]
		[Route("GetBankBalance/{id}")]
		public async Task<IActionResult> GetBankBalance(Guid id, string returnUrl = "") {
			// Payaccount id
			var plaid = new Plaid(_other.PlaidSecret, _other.PlaidClientId, _other.PlaidEnv);
			var payAccount = await _context.PayAccounts.FirstOrDefaultAsync(x => x.Id == id);

			if (payAccount == null) {
				return LocalRedirect(returnUrl);
			}

			var accessToken = await _context.ProfileSettings.FirstOrDefaultAsync(x => x.ProfileId == payAccount.ProfileId && x.SettingName == "plaid_access");

			if (accessToken != null) {
				try {
					var bal = await plaid.GetLastBalance(accessToken.SettingValue, payAccount.ExternalReferenceNumber);
					payAccount.LastBalance = (double)bal;
					payAccount.LastBalanceCheck = DateTime.Now;
					await _context.SaveChangesAsync();
				} catch (Exception e) {
					// Balance check errors should be logged, but not stop the user.
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, area: "Plaid-Last-Balance-Admin");
				}
			}

			return LocalRedirect(returnUrl);
		}
	}
}
