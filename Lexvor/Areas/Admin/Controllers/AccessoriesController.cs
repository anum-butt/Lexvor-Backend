using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Rest.Trunking.V1;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	public class AccessoriesController : BaseAdminController {

		private readonly ApplicationDbContext _context;
		private readonly OtherSettings _other;

		public AccessoriesController(ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other) : base(context, userManager, connStrings) {
			_context = context;
			_other = other.Value;
		}

		// GET: Admin/Offers
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index() {

			var list = await _context.Accessories.ToListAsync();
			return View(list);
		}

		// GET: Admin/Offers/Create
		[Authorize(Roles = Roles.TrustedManager)]
		public IActionResult Create() {

			return View(new PlanAccessory());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create(PlanAccessory model) {

			if (ModelState.IsValid) {

				await _context.AddAsync(model);
				await _context.SaveChangesAsync();

				var blobPath = $"accessory-images/{model.Id}";

				// Upload the image to blob
				if (model.ImageUpload != null) {
					var data = new byte[model.ImageUpload.Length];
					var stream = new MemoryStream();
					await model.ImageUpload.CopyToAsync(stream);
					data = stream.ToArray();
					await BlobService.UploadBlob(data, blobPath, _other);
				}

				model.ImageUrl = blobPath;
				_context.Update(model);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			return View(model);
		}

		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id) {

			var accessory = await _context.Accessories.Where(x => x.Id == id).FirstOrDefaultAsync();

			return View(accessory);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(PlanAccessory model) {

			if (ModelState.IsValid) {

				var accessory = await _context.Accessories.Where(x => x.Id == model.Id).FirstOrDefaultAsync();

				var blobPath = $"accessory-images/{model.Id}";

				// Upload the image to blob
				if (model.ImageUpload != null) {
					var data = new byte[model.ImageUpload.Length];
					var stream = new MemoryStream();
					await model.ImageUpload.CopyToAsync(stream);
					data = stream.ToArray();
					await BlobService.UploadBlob(data, blobPath, _other);
				}

				accessory.Name = model.Name;
				accessory.Description = model.Description;
				accessory.Options = model.Options;
				accessory.Grouping = model.Grouping;
				accessory.Price = model.Price;
				accessory.LifetimeWarranty = model.LifetimeWarranty;
				accessory.LifetimeWarrantyPrice = model.LifetimeWarrantyPrice;
				accessory.CurrentStockLevel = model.CurrentStockLevel;
				accessory.ImageUrl = blobPath;

				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			return View(model);
		}

		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Delete(Guid id) {

			var accessory = await _context.Accessories.Where(x => x.Id == id).FirstOrDefaultAsync();

			var accessoryPlans = await _context.UserAccessories.Where(x => x.Accessory == accessory.Name).ToListAsync();

			if (accessoryPlans.Count() == 0) {
				_context.Remove(accessory);
				await _context.SaveChangesAsync();
				return Json(new { delete = true });
			}

			return Json(new { delete = false });
		}
	}
}
