using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Payment;
using Lexvor.API.Objects.Enums;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Lexvor.Controllers.API {
	[ApiController]
	[Route("api/[controller]")]
	public class PaymentController : Controller {
		private ApplicationDbContext _context;
		private readonly OtherSettings _other;

		public PaymentController(ApplicationDbContext context,
				IOptions<OtherSettings> other) {
			_context = context;
			_other = other.Value;
		}

		[HttpPost, AllowAnonymous]
		public async Task<HttpResponseMessage> WebHookExecutor() {
			using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
				var json = await reader.ReadToEndAsync();

				// Capture payment events and add charges to the user account

				return new HttpResponseMessage(HttpStatusCode.OK);
			}
		}

		[HttpPost, AllowAnonymous]
		[HttpGet("PaymentSuccess/{id}")]
		public async Task<HttpResponseMessage> PaymentSuccess(Guid id) {
			// PayAccountId

			using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
				var json = await reader.ReadToEndAsync();

				// Capture payment events and add charges to the user account
				_context.Add(new WebhookResponseObject() {
					ObjectId = id.ToString(),
					ObjectType = "PayAccountId",
					Received = DateTime.UtcNow,
					ReceivedAction = "PaymentMade",
					Text = json
				});

				return new HttpResponseMessage(HttpStatusCode.OK);
			}
		}

		[HttpGet, AllowAnonymous]
		[HttpGet("GetSubUpdates/{start ?}/{end ?}")]
		public async Task<HttpResponseMessage> GetSubUpdates(DateTime start = default(DateTime), DateTime end = default(DateTime)) {

			var status = new JobStatus() {
				Job = "GetSubUpdates",
				RunTime = DateTime.Now,
				Status = (byte)Lexvor.API.Objects.Status.Running,
				Message = "GetSubUpdates started"
			};

			EntityEntry<JobStatus> statusEntity = null;
			try {
				statusEntity = await _context.JobStatus.AddAsync(status);

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			var service = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass,_other.EpicPayReportingUrl);

			if (start == DateTime.MinValue) {
				start = DateTime.UtcNow.AddDays(-10);
			}
			if (end == DateTime.MinValue) {
				end = DateTime.UtcNow;
			}

			var result = await service.GetSuccessfulSubscriptionCharges(start, end);

			try {
				// Map all Charges with no ProfileID to their profile using name
				foreach (var charge in result) {
					if (charge.ProfileId == Guid.Empty) {
						var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.FirstName.ToLower() == charge.FirstName.ToLower() && x.LastName.ToLower() == charge.LastName.ToLower());
						if (profile != null) {
							charge.ProfileId = profile.Id;
						}
					}

					if (charge.ProfileId != Guid.Empty) {
						var plans = await _context.Plans.Where(x => x.ProfileId == charge.ProfileId && x.Status == PlanStatus.PaymentHold).ToListAsync();
						if (plans.Count > 0) {
							plans.ForEach(x => x.Status = PlanStatus.Active);

							_context.UpdateRange(plans);
							// _context.SaveChangesAsync();
						}
						if(charge.InvoiceId != null) {
							var existingCharge = await _context.Charges.FirstOrDefaultAsync(x => x.InvoiceId == charge.InvoiceId);
							if (existingCharge == null) {
								_context.Charges.Add(charge);
							}
						}
					}

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception ex) {
				ErrorHandler.Capture(_other.SentryDSN, ex, "GetSubUpdates");
			}

			try {
				if (statusEntity != null) {
					statusEntity.Entity.RunTime = DateTime.Now;
					statusEntity.Entity.Status = (byte)Lexvor.API.Objects.Status.Complete;
					statusEntity.Entity.Message = "GetSubUpdates completed";

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		[HttpGet, AllowAnonymous]
		[HttpGet("GetRejectedACH")]

		public async Task<HttpResponseMessage> GetRejectedACH(DateTime start = default(DateTime), DateTime end = default(DateTime)) {

			var service = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass, _other.EpicPayReportingUrl);

			if (start == DateTime.MinValue) {
				start = DateTime.UtcNow.AddDays(-14);
			}

			if (end == DateTime.MinValue) {
				end = DateTime.UtcNow;
			}
			var response = await service.GetRejectedACH(start, end);
			foreach (var res in response) {
				string accountHolder = res.accountholder;
				var payAccounts = new List<Lexvor.API.Objects.User.BankAccount>();
				if (accountHolder.Contains(" ")) {
					string[] accountHolderName = accountHolder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					payAccounts = _context.PayAccounts.Where(x => x.AccountNumber == res.account_no.ToString() && x.AccountFirstName == accountHolderName[0] && x.AccountLastName == accountHolderName[1]).ToList();
				}
				else {
					payAccounts = _context.PayAccounts.Where(x => x.AccountNumber == res.account_no.ToString() && x.AccountFirstName == accountHolder).ToList();
				}
					foreach (var account in payAccounts) {
					var charge = _context.Charges.Where(x => x.ProfileId == account.ProfileId).ToList();
					charge.ForEach(x => {
						x.Status = Lexvor.API.Objects.User.ChargeStatus.Rejected;
						x.NeedsAttention = true;
					});
					charge.ForEach(x => x.Status = Lexvor.API.Objects.User.ChargeStatus.Rejected);
					_context.UpdateRange(charge);
					await _context.SaveChangesAsync();

					var plan = _context.Plans.Where(x => x.ProfileId == account.ProfileId).ToList();
					plan.ForEach(x => x.Status = PlanStatus.PaymentHold);
					_context.UpdateRange(plan);
					await _context.SaveChangesAsync();
				}
			}
      return new HttpResponseMessage(HttpStatusCode.OK);
			
		} 
		[HttpPost, AllowAnonymous]
		public async Task<HttpResponseMessage> RefreshAccounts() {
			// Refresh plaid account credentials and mark accounts needing update.

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		//Confirm a profile and their active bank account based on transaction history
		[HttpGet, AllowAnonymous]
		[HttpGet("ConfirmAccounts")]
		public async Task<HttpResponseMessage> ConfirmAccounts() {

			var status = new JobStatus() {
				Job = "ConfirmAccounts",
				RunTime = DateTime.Now,
				Status = (byte)Lexvor.API.Objects.Status.Running,
				Message = "Confirm Accounts started"
			};

			EntityEntry<JobStatus> statusEntity = null;
			try {
				statusEntity = await _context.JobStatus.AddAsync(status);

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			var service = new EpicPay(_other.EpicPayUrl, _other.EpicPayKey, _other.EpicPayPass, _other.EpicPayReportingUrl);

			var result = await service.GetTransactionActivity(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);

			foreach(var trans in result) {
				var profile = await _context.Profiles.FirstOrDefaultAsync(profile => profile.Id.ToString() == trans.client_customer_id);

				// If the profile exists based on a transaction
				if(profile != null) {

					// Mark profile status as confirmed if the current transaction is a sale
					if((trans.reason_code == "000" && trans.trxn_type == "SALE") || (trans.trxn_type == "DEBIT")) {
						profile.ProfileStatus = ProfileStatus.Confirmed;
					}

					var chargesExist = await _context.Charges.AnyAsync(charges => charges.InvoiceId == trans.transaction_id);

					// If any charges exist, mark their active bank account as confirmed
					if(chargesExist) {
						var bankAccount = await _context.PayAccounts.FirstOrDefaultAsync(account => account.ProfileId == profile.Id && account.Active);

						if(bankAccount != null) {
							bankAccount.Confirmed = true;
						}
					}
				}
			}

			await _context.SaveChangesAsync();

			try {
				
				if (statusEntity != null) {
					statusEntity.Entity.RunTime = DateTime.Now;
					statusEntity.Entity.Status = (byte)Lexvor.API.Objects.Status.Complete;
					statusEntity.Entity.Message = "Confirm Accounts complete";

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

	}
}
