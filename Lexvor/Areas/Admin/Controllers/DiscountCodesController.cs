using System;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	[Authorize(Roles = Roles.TrustedManager)]
    public class DiscountCodesController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;

		public DiscountCodesController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> Index() {
			var codes = await _context.DiscountCodes.Include(d => d.PlanType).ToListAsync();

			return View(codes);
		}

		[Route("[action]")]
		public async Task<IActionResult> Create() {
			ViewBag.PlanTypes = await _context.PlanTypes.ToListAsync();
			return View();
		}

		// POST: Admin/Offers/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(DiscountCode item) {
			if (ModelState.IsValid) {
				item.Id = Guid.NewGuid();
				item.Code = item.Code.ToUpper();
				_context.Add(item);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			ViewBag.PlanTypes = await _context.PlanTypes.ToListAsync();
			return View(item);
		}

		[Route("[action]")]
		public async Task<IActionResult> Edit(Guid id) {
			var item = await _context.DiscountCodes.SingleOrDefaultAsync(m => m.Id == id);
			if (item == null) {
				return NotFound();
			}
			ViewBag.PlanTypes = await _context.PlanTypes.ToListAsync();
			return View(item);
		}

		// POST: Admin/Offers/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, DiscountCode item) {
			if (id != item.Id) {
				return NotFound();
			}

			if (ModelState.IsValid) {
				try {
					item.Code = item.Code.ToUpper();
					_context.Update(item);
					await _context.SaveChangesAsync();
				} catch (DbUpdateConcurrencyException) {
					if (!Exists(item.Id)) {
						return NotFound();
					} else {
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			ViewBag.PlanTypes = await _context.PlanTypes.ToListAsync();
			return View(item);
		}

		[Route("[action]")]
		public async Task<IActionResult> Delete(Guid id) {
			var item = await _context.DiscountCodes.SingleOrDefaultAsync(m => m.Id == id);
			if (item == null) {
				return NotFound();
			}

			return View(item);
		}

		// POST: Admin/Offers/Delete/5
		[HttpPost, Route("[action]")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id) {
			var item = await _context.DiscountCodes.SingleOrDefaultAsync(m => m.Id == id);
			_context.DiscountCodes.Remove(item);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool Exists(Guid id) {
			return _context.DiscountCodes.Any(e => e.Id == id);
		}
	}
}
