using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio.AspNet.Common;

namespace Lexvor.Controllers.API {
	[Route("api/[controller]")]
	[ApiController]
	[AllowAnonymous]
	public class ChatBotController : BaseApiController {
		private readonly OtherSettings _other;

		public ChatBotController(ApplicationDbContext context,
			IOptions<OtherSettings> other, 
			UserManager<ApplicationUser> userManager) : base(context, userManager) {
			_other = other.Value;
		}
		
		[HttpGet("Lines/{id}")]
		public async Task<IActionResult> Lines([FromHeader]string Authorization, Guid id) {
			if(Authorization == _other.ChatBotToken) {
				var lines = await _context.Plans.Where(x => x.ProfileId == id && PlanService.ActiveStatuses.Contains(x.Status)).Select(x => StaticUtils.FormatPhone(x.MDN)).ToListAsync();
				if(!lines.Any()) {
					return NoContent();
				}
				return Ok(new { 
					lines
				});
			} else {
				return Unauthorized();
			}
		}

		[HttpGet("usage/{id}/{mdn}")]
		public async Task<IActionResult> Lines([FromHeader]string Authorization, Guid id, string mdn) {
			mdn = StaticUtils.NumericStrip(mdn);
			if (Authorization == _other.ChatBotToken) {
				var lines = await _context.Plans.Where(x => x.ProfileId == id && PlanService.ActiveStatuses.Contains(x.Status)).Select(x => StaticUtils.NumericStrip(x.MDN)).ToListAsync();
				if (!lines.Any()) {
					return NoContent();
				}
				if (lines.Contains(mdn)) {
					var usage = await UsageService.GetUsageAggregateForCycle(_context, mdn, DateTime.UtcNow.GetFirst());
					return Ok($"You have used {StaticUtils.ConvertKBToGB(usage.KBData)} GB of data this cycle. Mins: {usage.Minutes}. SMS: {usage.SMS}.");
				} else {
					return Ok("Sorry, I can't gather usage information for that line");
				}
			} else {
				return Unauthorized();
			}
		}

		[HttpGet("throttles/{id}")]
		public async Task<IActionResult> Throttles([FromHeader]string Authorization, Guid id) {
			if (Authorization == _other.ChatBotToken) {
				var plans = await _context.Plans.Include(x => x.PlanType).Where(x => x.ProfileId == id && PlanService.ActiveStatuses.Contains(x.Status)).ToListAsync();
				var throttleMessages = new List<string>();

				if (!plans.Any()) {
					return NoContent();
				}
				foreach (var plan in plans) {
					try {
						var message = await UsageService.CalculateThrottleForLine(_context, plan.PlanType, plan.MDN, DateTime.UtcNow.GetFirst());
						throttleMessages.Add($"{StaticUtils.FormatPhone(plan.MDN)}: {message}");
					} catch (Exception e) {
						ErrorHandler.Capture(_other.SentryDSN, e, "Throttle Chat");
					}
				}

				return Ok(string.Join(Environment.NewLine, throttleMessages));
			} else {
				return Unauthorized();
			}
		}
	}
}
