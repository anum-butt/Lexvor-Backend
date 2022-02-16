using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Twilio.AspNet.Common;

namespace Lexvor.Controllers.API {
	[Route("api/[controller]")]
	[ApiController]
	public class CommunicationsController : BaseApiController {

		public CommunicationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) {

		}

		// SMSMessageCallback is called when a SMS message changes status 
		[HttpPost("SMSMessageCallback")]
		public async Task<IActionResult> SMSMessageCallback() {
			try {
				var smsId = Request.Form["SmsSid"].ToString();
				var message = await _context.UserComms.FirstOrDefaultAsync(x => x.ExternalId == smsId);

				if (message == null) {
					await _context.UserComms.AddAsync(new UserComm {
						ExternalId = Request.Form["SmsSid"],
						Status = Request.Form["MessageStatus"]
					});
				} else {
					message.Status = Request.Form["MessageStatus"];
				}

				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				Console.WriteLine(e);
				throw;
			}

			return Ok();
		}

	}
}
