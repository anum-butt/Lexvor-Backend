using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Lexvor.Controllers.API {
	[ApiController]
	[Route("api/[controller]")]
	public class WirelessController : BaseApiController {
		private readonly OtherSettings _other;

		public WirelessController(
			ApplicationDbContext context,
			IOptions<OtherSettings> other,
			UserManager<ApplicationUser> userManager) : base(context, userManager) {
			_other = other.Value;
		}

		[HttpPost, AllowAnonymous]
		public async Task<HttpResponseMessage> RunDowngrades() {
			// Check all customers close to bill date for usage that will put them in the next lowest bucket and downgrade them.

			return new HttpResponseMessage(HttpStatusCode.OK);
		}


		[HttpGet, AllowAnonymous]
		[HttpGet("UpdatePorts/{token ?}")]
		public async Task<HttpResponseMessage> UpdatePorts(string token) {
			if (token != "fk398jnf784") {
				return new HttpResponseMessage(HttpStatusCode.Unauthorized);
			}

			var status = new JobStatus() {
				Job = "UpdatePorts",
				RunTime = DateTime.Now,
				Status = (byte)Lexvor.API.Objects.Status.Running,
				Message = "UpdatePorts Running"
			};

			try {
				await _context.JobStatus.AddAsync(status);

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			if (token != "fk398jnf784") {
				return new HttpResponseMessage(HttpStatusCode.Unauthorized);
			}

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			// Get all active plans
			var plans = await _context.Plans
				.Include(x => x.Profile)
				.Include(x => x.PlanType)
				.Include(x => x.Device)
				.Include(x => x.PortRequest)
				.Where(x => x.AgreementSigned && (x.Status == PlanStatus.DevicePending || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid)
											  && (x.PortRequest != null && (x.PortRequest.Status == PortStatus.Ready || x.PortRequest.Status == PortStatus.Pending))).ToListAsync();

			foreach (var plan in plans) {
				try {
					await WirelessService.UpdatePortStatus(_context, service, plan);
				}
				catch (Exception e) {
					// Usage fails, keep going and log
					ErrorHandler.Capture(_other.SentryDSN, e);
				}
			}

			try {

				status = await _context.JobStatus.Where(x => x.Id == status.Id).FirstOrDefaultAsync();

				if (status != null) {
					status.RunTime = DateTime.Now;
					status.Status = (byte)Lexvor.API.Objects.Status.Complete;
					status.Message = "UpdatePorts complete";

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}


		[HttpGet, AllowAnonymous]
		[HttpGet("GetUsages/{token ?}")]
		public async Task<HttpResponseMessage> GetUsages(string token) {
			if (token != "fk398jnf784") {
				return new HttpResponseMessage(HttpStatusCode.Unauthorized);
			}

			var status = new JobStatus() {
				Job = "GetUsages",
				RunTime = DateTime.Now,
				Status = (byte)Lexvor.API.Objects.Status.Running,
				Message = "GetUsages started"
			};

			EntityEntry<JobStatus> statusEntity = null;
			try {
				statusEntity = await _context.JobStatus.AddAsync(status);

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			if (token != "fk398jnf784") {
				return new HttpResponseMessage(HttpStatusCode.Unauthorized);
			}

			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			// Get all active plans
			var plans = await _context.Plans.Where(x => PlanService.ActiveStatuses.Contains(x.Status) && x.WirelessStatus == WirelessStatus.Active).ToListAsync();

			foreach (var plan in plans) {
				if (plan.MDN.IsNull()) {
					continue;
				}

				try {
					await WirelessService.UpdateUsageDataFromWirelessProvider(service, _context, plan.MDN);
				}
				catch (Exception e) when (!e.Message.Contains("User does not have access to MDN")) {
					// Usage fails, keep going and log
					ErrorHandler.Capture(_other.SentryDSN, e);
				}
			}

			try {
				if (statusEntity != null) {
					statusEntity.Entity.RunTime = DateTime.Now;
					statusEntity.Entity.Status = (byte)Lexvor.API.Objects.Status.Complete;
					statusEntity.Entity.Message = "GetUsages complete";

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		[HttpGet("validateimei/{imei}/{userDeviceId ?}")]
		public async Task<IActionResult> ValidateIMEI(string imei, Guid? userDeviceId) {
			// Validate MDN/Number and IMEI/ESN
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
			var result = await service.IMEIValidate(imei.Trim());

			// Update plan with IMEI validated, if provided.
			if (userDeviceId.HasValue) {
				var device = await _context.UserDevices.FirstOrDefaultAsync(x => x.Id == userDeviceId.Value);
				if (device != null) {
					device.IMEI = imei;
					device.IMEIValid = result == "Y";
					await _context.SaveChangesAsync();
				}
			}

			return Ok(new {
				success = true,
				imeiResult = result == "Y" ? result : null,
				error = result != "Y" ? result : null
			});
		}

		[AllowAnonymous]
		[HttpGet("validatemdn/{mdn}")]
		public async Task<IActionResult> ValidateMDN(string mdn) {
			// Validate MDN/Number and IMEI/ESN
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
			var refNumber = await service.SubmitPortValidate(Regex.Replace(mdn, @"[^\d]", ""));

			return Ok(new {
				success = true,
				message = refNumber
			});
		}

		[AllowAnonymous]
		[HttpGet("UpdateMDNStatus/{refNumber}/{planId ?}")]
		public async Task<IActionResult> UpdateMDNStatus(string refNumber, Guid? planId) {
			// Validate MDN/Number and IMEI/ESN
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			try {
				var (portable, message) = await service.CheckPortValidate(refNumber);

				if (planId.HasValue && !string.IsNullOrWhiteSpace(message)) {
					var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == planId.Value);
					if (plan != null) {
						plan.MDNPortable = portable;
						await _context.SaveChangesAsync();
					}
				}

				return Ok(new {
					success = !string.IsNullOrWhiteSpace(message),
					portable = portable,
					message = message
				});
			}
			catch (Exception e) {
				return Ok(new {
					success = false,
					portable = false,
					message = e.Message
				});
			}
		}


		[AllowAnonymous]
		[HttpGet("ActivationCallBack/{id}")]
		public async Task<IActionResult> ActivationCallBack(Guid id) {
			// UserPlanId
			var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == id);

			// If this was a no port activation, the users new number will be in this response.
			using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8)) {
				var xml = await reader.ReadToEndAsync();

			}

			return Ok(new {
				success = true
			});
		}

		[HttpGet("NotifyUserThrottle")]
		public async Task<IActionResult> NotifyUserThrottle() {

			if (!_other.ThrottleNotifyEnabled) { 
				return Ok(new {
					success = false
				});
			}
			
			var status = new JobStatus() {
				Job = "NotifyUserThrottle",
				RunTime = DateTime.Now,
				Status = (byte)Lexvor.API.Objects.Status.Running,
				Message = "NotifyUserThrottle Running"
			};

			EntityEntry<JobStatus> statusEntity = null;
			try {
				statusEntity = await _context.JobStatus.AddAsync(status);

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}


			var activePlans = await _context.Plans
				.Include(x => x.PlanType)
				.Where(x => PlanService.ActiveStatuses.Contains(x.Status) && x.WirelessStatus == WirelessStatus.Active && !string.IsNullOrWhiteSpace(x.MDN)).ToListAsync();
			var service = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

			// Do nothing if this is within the first three days of the cycle or in the last two days of the cycle
			if (DateTime.UtcNow < DateTime.UtcNow.GetFirst().AddDays(3) || DateTime.UtcNow > DateTime.UtcNow.GetLast().AddDays(-2)) {
				return Ok(new {
					success = true
				});
			}

			var callback = string.Format("{0}://{1}{2}", Request.Scheme,
				Request.Host, Url.Action(nameof(CommunicationsController.SMSMessageCallback), "Communications"));

			foreach (UserPlan plan in activePlans) {
				UsageDay usageDay = null;

				// Skip if this is a known bad number
				if (StaticUtils.NumericStrip(plan.MDN).StartsWith("100") || StaticUtils.NumericStrip(plan.MDN).StartsWith("000")) {
					continue;
				}

				try {
					usageDay = await service.GetUsageForCycle(plan.MDN, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Data-Usage-Notifications");
				}

				var first = DateTime.Now.GetFirst();

				if (usageDay != null) {
					if (usageDay.KBData >= plan.PlanType.SecondThrottle) {
						var message = "You've used all of your high speed data and you will begin to experience slower speeds. Call us for more information at (866) 996-2281. -Lexvor";
						// Only send message if they have not received one this pay cycle
						var sentThisCycle = await _context.UserComms.AnyAsync(x => x.ProfileId == plan.ProfileId && x.Recipient == plan.MDN && x.Sent > first && x.Content.Contains("all of your high speed data"));
						if (!sentThisCycle) {
							await TwilioService.Send(_other, _context, callback, plan.ProfileId, plan.MDN, message);
						}
					}
					else if (usageDay.KBData < plan.PlanType.FirstThrottle && (double)usageDay.KBData / plan.PlanType.FirstThrottle > 0.25) {
						var message = "You have used a considerable amount of data in a short period of time. We recommend using WiFi where you can. To check your data usage see our website: https://lexvorwireless.com -Lexvor";
						var sentThisCycle = await _context.UserComms.AnyAsync(x => x.ProfileId == plan.ProfileId && x.Recipient == plan.MDN && x.Sent > first && x.Content.Contains("considerable amount"));
						if (!sentThisCycle) {
							await TwilioService.Send(_other, _context, callback, plan.ProfileId, plan.MDN, message);
						}
					}
					else if (usageDay.KBData < plan.PlanType.SecondThrottle && (double)usageDay.KBData / plan.PlanType.SecondThrottle > 0.25) {
						var message = "You have used an excessive amount of data in a short period of time. We highly recommend using WiFi. To check your data usage see our website: https://lexvorwireless.com -Lexvor";
						var sentThisCycle = await _context.UserComms.AnyAsync(x => x.ProfileId == plan.ProfileId && x.Recipient == plan.MDN && x.Sent > first && x.Content.Contains("excessive amount"));
						if (!sentThisCycle) {
							await TwilioService.Send(_other, _context, callback, plan.ProfileId, plan.MDN, message);
						}
					}
				}
			}

			try {

				if (statusEntity != null) {
					statusEntity.Entity.RunTime = DateTime.Now;
					statusEntity.Entity.Status = (byte)Lexvor.API.Objects.Status.Complete;
					statusEntity.Entity.Message = "NotifyUserThrottle complete";

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Job Status exception");
			}

			return Ok(new {
				success = true
			});
		}

		[HttpGet("CheckWirelessPlan/{id}")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> CheckWirelessPlan(Guid id, string returnUrl = "") {
			var plan = await _context.Plans
				.Include(p => p.Profile)
				.FirstOrDefaultAsync(x => x.Id == id);

			var isValidPlanToCheck = plan != null
				&& !string.IsNullOrEmpty(plan.MDN)
				&& !string.IsNullOrEmpty(plan.Profile.ExternalWirelessCustomerId);

			var wirelessPlanName = string.Empty;

			if (isValidPlanToCheck) {
				try {
					var telispireApi = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);
					var wirelessPlan = await telispireApi.GetActiveWirelessPlan(plan.MDN);
					wirelessPlanName = wirelessPlan.MasterCategory;
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, area: "Get-Wireless-By-MDN");
				}
			}

			return new JsonResult(new { WirelessPlanName = wirelessPlanName });
		}

		[HttpGet("GetExternalPlanIds")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> GetExternalPlanIds() {
			var externalPlanIds = await _context.PlanTypes.Where(pt => !string.IsNullOrEmpty(pt.ExternalId))                                         
				                                          .Select(pt => new { pt.ExternalId, pt.Id })
				                                          .ToListAsync();

			return new JsonResult(new { ExternalPlanIds = externalPlanIds });
		}

		[HttpPost("ChangePackageOnMDN")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> ChangePackageOnMDN(
			[FromBody]ChangePackageOnMDNRequestModel request) {

			string wirelessPlanName = string.Empty;
			var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == request.ExistingPlanId);
			if (plan != null) {
				try {
					var telispireApi = new TelispireApi(_other.TelispireUser, _other.TelispirePassword);

					var existingWirelessPlan = await telispireApi.GetActiveWirelessPlan(plan.MDN);
					wirelessPlanName = existingWirelessPlan.MasterCategory;

					await telispireApi.ChangePackageOnMDN(plan.MDN, existingWirelessPlan.MasterCategory, request.NewPlanName);
					
					plan.WirelessPlanName = request.NewPlanName;
					await _context.SaveChangesAsync();
					wirelessPlanName = request.NewPlanName;
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, area: "Change-Package-On-MDN");
				}
			}

			return new JsonResult(new { wirelessPlanName });
		}
	}

	public class ChangePackageOnMDNRequestModel {
		public Guid ExistingPlanId { get; set; }
		public string NewPlanName { get; set; }
	}
}
