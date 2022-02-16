using System;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	[Authorize(Roles = Roles.Admin)]
	public class AccountCreditController : BaseAdminController {

		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public AccountCreditController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			IEmailSender emailSender,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
			_emailSender = emailSender;
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> Index(Guid id) {
			// ProfileId
			var profile = await _context.Profiles.Include(p => p.Account).FirstAsync(p => p.Id == id);
			var credits = await _context.AccountCredits.Where(c => c.ProfileId == profile.Id).ToListAsync();

			var model = new AccountCreditListModel() {
				Profile = profile,
				Credits = credits
			};
			return View(model);
		}


		[Route("[action]")]
		public async Task<IActionResult> ApplytoPlan(Guid id) {
			var credit = await _context.AccountCredits.FirstAsync(c => c.Id == id);
			var plans = await _context.Plans.Include(p => p.PlanType).Where(p => p.ProfileId == credit.ProfileId).ToListAsync();

			var model = new AccountCreditApplyModel() {
				Credit = credit,
				Plans = plans
			};

			return View(model);
		}
		[HttpPost]
		[Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ApplytoPlan(AccountCreditApplyModel model) {
			var credit = await _context.AccountCredits.FirstAsync(c => c.Id == model.Credit.Id);
			if (ModelState.IsValid && credit.ApplicableToMonthlyFee) {
				var plans = await _context.Plans.FirstAsync(p => p.Id == model.PlanId);
				// The duration for the coupon is total amount divided by monthly applicable
				var duration = credit.Amount / credit.Amount;

				// Create the discount
				

				// Apply the discount

				return RedirectToAction(nameof(Index), new { id = credit.ProfileId });
			}
			return View();
		}

		[Route("[action]")]
		public async Task<IActionResult> Create(Guid id) {
			return View(new AccountCredit() {
				ProfileId = id
			});
		}

		// POST: Admin/Offers/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AccountCredit item) {
			if (ModelState.IsValid) {
				if (item.ApplicableToMonthlyFee && Math.Abs(item.Amount) <= 0 &&
					(Math.Abs(item.Amount % item.Amount) <= 0)) {
					ModelState.AddModelError("", "Amount and Monthly Apply amount must be divisors of each other. No remainder.");
				}

				if (ModelState.IsValid) {
					item.Id = Guid.NewGuid();
					_context.Add(item);
					await _context.SaveChangesAsync();
					return RedirectToAction(nameof(Index), new { id = item.ProfileId });
				}
			}
			return View(item);
		}

		[Route("[action]")]
		public async Task<IActionResult> Edit(Guid id) {
			var item = await _context.AccountCredits.SingleOrDefaultAsync(m => m.Id == id);
			if (item == null) {
				return NotFound();
			}
			return View(item);
		}

		// POST: Admin/Offers/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, AccountCredit item) {
			if (id != item.Id) {
				return NotFound();
			}

			if (ModelState.IsValid) {
				try {
					_context.Update(item);
					await _context.SaveChangesAsync();
				} catch (DbUpdateConcurrencyException) {
					if (!Exists(item.Id)) {
						return NotFound();
					} else {
						throw;
					}
				}
				return RedirectToAction(nameof(Index), new { id = item.ProfileId });
			}
			return View(item);
		}

		// TODO
		//[Route("[action]")]
		//public async Task<IActionResult> Delete(Guid id) {
		//	var item = await _context.AccountCredits.SingleOrDefaultAsync(m => m.Id == id);
		//	if (item == null) {
		//		return NotFound();
		//	}

		//	return View(item);
		//}

		// POST: Admin/Offers/Delete/5
		[HttpPost, Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id) {
			var item = await _context.AccountCredits.SingleOrDefaultAsync(m => m.Id == id);
			_context.AccountCredits.Remove(item);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index), new { id = item.ProfileId });
		}

		private bool Exists(Guid id) {
			return _context.AccountCredits.Any(e => e.Id == id);
		}
	}
}