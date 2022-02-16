using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	public class DevicesController : BaseAdminController {
		private readonly ApplicationDbContext _context;
		private readonly OtherSettings _other;

		public DevicesController(ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other) : base(context, userManager, connStrings) {
			_context = context;
			_other = other.Value;
		}

		// GET: Admin/Offers
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index() {
			var list = await _context.Devices.Include(x => x.PlanTypes).ThenInclude(x => x.PlanType).OrderBy(x => x.Name).ToListAsync();
			return View(list);
		}

		// GET: Admin/Offers/Create
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create() {
			var plantype = await _context.PlanTypes.Where(s => s.Archived == false).OrderBy(s => s.SortOrder).ToListAsync();
			List<DevicePlanTypeSel> selLi = new List<DevicePlanTypeSel>();
			foreach (PlanType pt in plantype) {
				DevicePlanTypeSel sel = new DevicePlanTypeSel();
				sel.PlanType = pt;
				sel.PlanTypeSelect = false;
				selLi.Add(sel);
			}
			var model = new DeviceCreateVM() {
				DevicePlanTypeSel = selLi
			};
			return View(model);
		}

		// POST: Admin/Offers/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create(DeviceCreateVM model) {
			int val = 0;
			var item = model.Device;
			if (ModelState.IsValid) {
				if (_context.Devices.ToList().Any(o => o.Name == item.Name)) {
					ModelState.AddModelError("DEVICENAMEERR", "Device Name Already Exists");
				}
				else {
					val = 1;
				}

				if (val == 1) {
					item.Id = Guid.NewGuid();
					item.DeviceType = DeviceType.Phone;
					item.Available = true;
					_context.Add(item);
					await _context.SaveChangesAsync();

					if (model.DevicePlanTypeSel != null) {
						PlanTypeDevice TD = new PlanTypeDevice();
						foreach (DevicePlanTypeSel planI in model.DevicePlanTypeSel) {
							if (planI.PlanTypeSelect == true) {
								TD.DeviceId = item.Id;
								TD.PlanTypeId = planI.PlanType.Id;
								_context.Add(TD);
								await _context.SaveChangesAsync();
							}
						}
					}

					var blobPath = $"device-images/{item.Id}";

					// Upload the image to blob
					if (item.ImageUpload != null) {
						var data = new byte[item.ImageUpload.Length];
						var stream = new MemoryStream();
						await item.ImageUpload.CopyToAsync(stream);
						data = stream.ToArray();
						await BlobService.UploadBlob(data, blobPath, _other);
					}

					item.ImageUrl = blobPath;
					_context.Update(item);
					await _context.SaveChangesAsync();
					return RedirectToAction(nameof(Index));
				}
			}
			if (!ModelState.IsValid || val == 0) {
				var plantype = await _context.PlanTypes.Where(s => s.Archived == false).OrderBy(s => s.SortOrder).ToListAsync();

				List<DevicePlanTypeSel> selLi = new List<DevicePlanTypeSel>();
				foreach (PlanType pt in plantype) {
					DevicePlanTypeSel sel = new DevicePlanTypeSel();
					sel.PlanType = pt;
					sel.PlanTypeSelect = false;
					selLi.Add(sel);
				}
				var modelF = new DeviceCreateVM() {
					DevicePlanTypeSel = selLi
				};
				return View(modelF);
			}
			return RedirectToAction(nameof(Index));
		}

		// GET: Admin/Offers/Edit/5
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id) {


			var item = await _context.Devices.Include(o => o.PlanTypes).SingleOrDefaultAsync(m => m.Id == id);
			var plantypedev = await _context.PlanTypeDevices.Where(m => m.DeviceId == id).ToListAsync();
			var plantype = await _context.PlanTypes.Where(s => s.Archived == false).OrderBy(s => s.SortOrder).ToListAsync();
			var devOpt = await _context.DeviceOptions.Where(s => s.Device.Id == id).OrderBy(s => s.OptionGroup).ToListAsync();
			List<DevicePlanTypeSel> selLi = new List<DevicePlanTypeSel>();
			foreach (PlanType PT in plantype) {
				bool bDSel = plantypedev.Where(x => x.PlanTypeId == PT.Id).Any();
				DevicePlanTypeSel sel = new DevicePlanTypeSel();
				sel.PlanType = PT;
				sel.PlanTypeSelect = bDSel;
				sel.PlanTypeSelectPre = bDSel;
				selLi.Add(sel);
			}
			var model = new DeviceEditVM() {
				DevicePlanTypeSel = selLi,
				Device = item,
				ShowDeviceOptions = devOpt.Any(),
				DeviceOption = devOpt
			};
			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<JsonResult> SaveDeviceOpt(Guid deviceId, Guid optionId, string optionValue, int surcharge, string optionGroup) {
			if (optionId == Guid.Empty) {
				var device = _context.Devices.FirstOrDefault(x => x.Id == deviceId);
				var item = new DeviceOption() {
					Device = device,
					OptionGroup = optionGroup,
					OptionValue = optionValue, 
					Surcharge = surcharge
				};
				_context.Add(item);
			} else {
				var deviceOpt = _context.DeviceOptions.FirstOrDefault(x => x.Id == optionId);
				deviceOpt.OptionGroup = optionGroup;
				deviceOpt.OptionValue = optionValue;
				deviceOpt.Surcharge = surcharge;
				_context.Update(deviceOpt);
			}
			await _context.SaveChangesAsync();

			var list = await _context.DeviceOptions.Where(x => x.Device.Id == deviceId).Select(x => new {
				x.Id, 
				x.OptionGroup, 
				x.OptionValue,
				x.Surcharge
			}).OrderBy(x => x.OptionGroup).ToListAsync();

			return Json(list);
		}

		[HttpDelete]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<JsonResult> DeleteDeviceOpt(Guid deviceId, Guid optionId) {

			var deviceOpt = _context.DeviceOptions.FirstOrDefault(x => x.Id == optionId);
			_context.Remove(deviceOpt);
			await _context.SaveChangesAsync();
			
			var list = await _context.DeviceOptions.Where(x => x.Device.Id == deviceId).Select(x => new {
				x.Id, 
				x.OptionGroup, 
				x.OptionValue,
				x.Surcharge
			}).OrderBy(x => x.OptionGroup).ToListAsync();

			return Json(list);
		}

		// POST: Admin/Offers/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(DeviceEditVM model) {
			var item = model.Device;
			if (_context.Devices.Any(o => o.Name == item.Name && o.Id != item.Id)) {
				ModelState.AddModelError("DEVICENAMEERR", "Device Name Already Exists");
			}

			if (ModelState.IsValid) {
				var blobPath = $"device-images/{item.Id}";
				// Upload the image to blob
				if (item.ImageUpload != null) {
					var data = new byte[item.ImageUpload.Length];
					var stream = new MemoryStream();
					await item.ImageUpload.CopyToAsync(stream);
					data = stream.ToArray();
					await BlobService.UploadBlob(data, blobPath, _other);
				}
				item.ImageUrl = blobPath;
				item.DeviceType = DeviceType.Phone;

				_context.Update(item);
				await _context.SaveChangesAsync();

				if (model.DevicePlanTypeSel != null) {
					foreach (DevicePlanTypeSel planI in model.DevicePlanTypeSel) {
						if (planI.PlanTypeSelect != planI.PlanTypeSelectPre) {
							PlanTypeDevice TD = new PlanTypeDevice();
							TD.DeviceId = item.Id;
							TD.PlanTypeId = planI.PlanType.Id;
							if (planI.PlanTypeSelect == true) {
								_context.Add(TD);
							}
							else {
								_context.Remove(TD);
							}
							await _context.SaveChangesAsync();
						}
					}
				}

				return RedirectToAction(nameof(Index));
			} else {
				var plantype = await _context.PlanTypes.Where(s => s.Archived == false).OrderBy(s => s.SortOrder).ToListAsync();
				var devOpt = await _context.DeviceOptions.Where(s => s.Device.Id == item.Id).OrderBy(s => s.OptionGroup).ToListAsync();

				List<DevicePlanTypeSel> selLi = new List<DevicePlanTypeSel>();
				foreach (PlanType pt in plantype) {
					DevicePlanTypeSel sel = new DevicePlanTypeSel();
					sel.PlanType = pt;
					sel.PlanTypeSelect = false;
					selLi.Add(sel);
				}
				var modelF = new DeviceEditVM() {
					Device = item,
					DevicePlanTypeSel = selLi,
					ShowDeviceOptions = devOpt.Any(),
					DeviceOption = devOpt
				};
				return View(modelF);
			}
		}

		// GET: Admin/Offers/Delete/5
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Delete(Guid id) {
			var item = await _context.Devices
				.SingleOrDefaultAsync(m => m.Id == id);
			if (item == null) {
				return NotFound();
			}

			return View(item);
		}

		// POST: Admin/Offers/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> DeleteConfirmed(Guid id) {
			var item = await _context.Devices.SingleOrDefaultAsync(m => m.Id == id);
			_context.Devices.Remove(item);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool Exists(Guid id) {
			return _context.Devices.Any(e => e.Id == id);
		}

		[HttpGet]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> NoDevices() {

			var plans = await _context.Plans.Include(x => x.UserDevice).Include(x => x.PlanType).Include(x => x.Profile).ThenInclude(x => x.Account)
							.Where(s => s.UserDevice.StockedDevice == null && s.UserDevice.Shipped != null).ToListAsync();

			return View(new NoDevicesVM() {
				UserPlan = plans
			});
		}
	}
}
