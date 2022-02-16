using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChargeOver.Wrapper.Services;
using Lexvor.API;
using Lexvor.API.ChargeOver;
using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	public class PlanTypesController : BaseAdminController {
		private readonly ApplicationDbContext _context;
		private readonly OtherSettings _other;

		public PlanTypesController(ApplicationDbContext context,
			IOptions<ConnectionStrings> connStrings,
			UserManager<ApplicationUser> userManager,
			IOptions<OtherSettings> other) : base(context, userManager, connStrings) {
			_context = context;
			_other = other.Value;
		}

		// GET: Admin/Offers
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index() {
			IQueryable<PlanType> list;

			list = _context.PlanTypes;

			var temp = list.OrderBy(x => x.Archived).ThenBy(x => x.Name);

			return View(await temp.ToListAsync());
		}

		// GET: Admin/Offers/Create
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create() {
			var device = await _context.Devices.Where(s => s.Available).OrderBy(s => s.Name).ToListAsync();
			List<PlanTypeDeviceSel> selLi = new List<PlanTypeDeviceSel>();
			foreach (Device dev in device) {
				PlanTypeDeviceSel sel = new PlanTypeDeviceSel {
					DeviceName = dev.Name,
					DeviceId = dev.Id,
					DevSelect = false
				};
				selLi.Add(sel);
			}
			var model = new PlanTypeCreateVM() {
				PlanTypeDeviceSel = selLi
			};
			return View(model);
		}

		// POST: Admin/Offers/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create(PlanTypeCreateVM model) {
			if (_context.PlanTypes.ToList().Any(o => o.Name == model.PlanType.Name)) {
				ModelState.AddModelError("PLANNAMEERR", "Plan Name Already Exists");
			}

			if (model.PlanType.AllowAffirmPurchases && model.PlanType.AllowAccessoryPurchase) {
				ModelState.AddModelError("", "You cannot use accessory purchases with Affirm");
			}

			if (ModelState.IsValid) {
				model.PlanType.DisplayOnPublicPages = true;
				_context.Add(model.PlanType);
				await _context.SaveChangesAsync();

				PlanTypeDevice TD = new PlanTypeDevice();
				foreach (PlanTypeDeviceSel devI in model.PlanTypeDeviceSel) {
					if (devI.DevSelect) {
						try {
							TD.PlanTypeId = model.PlanType.Id;
							TD.DeviceId = devI.DeviceId;
							_context.Add(TD);
							await _context.SaveChangesAsync();
						}
						catch (Exception e) {
							ErrorHandler.Capture(_other.SentryDSN, new Exception("Could not add device to PlanType", e), HttpContext, "CreatePlanType");
						}
					}
				}
				return RedirectToAction(nameof(Index));
			}
			else {
				var device = await _context.Devices.Where(s => s.Available).OrderBy(s => s.Name).ToListAsync();
				List<PlanTypeDeviceSel> selLi = new List<PlanTypeDeviceSel>();
				foreach (Device dev in device) {
					PlanTypeDeviceSel sel = new PlanTypeDeviceSel {
						DeviceName = dev.Name,
						DeviceId = dev.Id,
						DevSelect = false
					};
					selLi.Add(sel);
				}

				model.PlanTypeDeviceSel = selLi;
				return View(model);
			}
		}

		// GET: Admin/Offers/Edit/5
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id) {
			var item = await _context.PlanTypes.SingleOrDefaultAsync(m => m.Id == id);
			var plantypedev = await _context.PlanTypeDevices.Where(m => m.PlanTypeId == id).ToListAsync();
			var device = await _context.Devices.Where(s => !s.Archived).OrderBy(s => s.Name).ToListAsync();

			List<PlanTypeDeviceSel> selLi = new List<PlanTypeDeviceSel>();
			foreach (Device dev in device) {
				bool bDSel = plantypedev.Any(x => x.DeviceId == dev.Id);
				PlanTypeDeviceSel sel = new PlanTypeDeviceSel {
					DeviceName = dev.Name,
					DeviceId = dev.Id,
					DevSelect = bDSel,
					DevSelectPre = bDSel
				};
				selLi.Add(sel);
			}
			var model = new PlanTypeCreateVM() {
				PlanTypeDeviceSel = selLi,
				PlanType = item
			};
			return View(model);
		}

		// POST: Admin/Offers/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(PlanTypeCreateVM model) {
			var item = model.PlanType;

			if (model.PlanType.AllowAffirmPurchases && model.PlanType.AllowAccessoryPurchase) {
				ModelState.AddModelError("", "You cannot use accessory purchases with Affirm");
			}

			if (ModelState.IsValid) {
				try {
					foreach (PlanTypeDeviceSel devI in model.PlanTypeDeviceSel) {
						if (devI.DevSelect != devI.DevSelectPre) {
							PlanTypeDevice pt = new PlanTypeDevice { DeviceId = devI.DeviceId, PlanTypeId = item.Id };
							if (devI.DevSelect) {
								_context.Add(pt);
							}
							else {
								_context.Remove(pt);
							}
							await _context.SaveChangesAsync();
						}
					}

					item.LastModified = DateTime.UtcNow;
					_context.Update(item);
					await _context.SaveChangesAsync();

				}
				catch (DbUpdateConcurrencyException) {
					if (!Exists(item.Id)) {
						return NotFound();
					}
					else {
						throw;
					}
				}
				return RedirectToAction(nameof(Index));

			}

			var plantypedev = await _context.PlanTypeDevices.Where(m => m.PlanTypeId == item.Id).ToListAsync();
			var device = await _context.Devices.Where(s => s.Available).OrderBy(s => s.Name).ToListAsync();
			List<PlanTypeDeviceSel> selLi = new List<PlanTypeDeviceSel>();
			foreach (Device dev in device) {
				bool bDSel = plantypedev.Any(x => x.DeviceId == dev.Id);
				PlanTypeDeviceSel sel = new PlanTypeDeviceSel();
				sel.DeviceName = dev.Name;
				sel.DeviceId = dev.Id;
				sel.DevSelect = bDSel;
				sel.DevSelectPre = bDSel;
				selLi.Add(sel);
			}
			var modelR = new PlanTypeCreateVM() {
				PlanTypeDeviceSel = selLi,
				PlanType = item
			};
			return View(modelR);
		}

		private bool Exists(Guid id) {
			return _context.PlanTypes.Any(e => e.Id == id);
		}
		// GET: Line pricing for plantypes
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> LinePricing(Guid Id) {
			var plantype = await _context.PlanTypes.Where(s => s.Id == Id).Include(x => x.LinePricing1).Include(x => x.LinePricing2).Include(x => x.LinePricing3).Include(x => x.LinePricing4).FirstOrDefaultAsync();
			List<int> lp = new List<int>();
			if (plantype.LinePricing1 != null)
				lp.Add(plantype.LinePricing1.Id);
			if (plantype.LinePricing2 != null)
				lp.Add(plantype.LinePricing2.Id);
			if (plantype.LinePricing3 != null)
				lp.Add(plantype.LinePricing3.Id);
			if (plantype.LinePricing4 != null)
				lp.Add(plantype.LinePricing4.Id);
			var LinePricing = await _context.LinePricing.Where(x => lp.Contains(x.Id)).ToListAsync();
			List<LinePricing> LP = new List<LinePricing>();
			for (int i = 1; i < 5; i++) {
				var LineItem = LinePricing.Where(s => s.RequiredNumOfLines == i).FirstOrDefault();
				LinePricing it = new LinePricing();
				it.RequiredNumOfLines = i;
				if (LineItem != null) {
					it.Id = LineItem.Id;
					it.InitiationFee = LineItem.InitiationFee;
					it.MonthlyCost = LineItem.MonthlyCost;
				}
				LP.Add(it);
			}

			var model = new PlanTypeLinePricing() {
				LinePricing = LP,
				PlanType = plantype
			};
			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> LinePricingInsert(int id, int lineno, int initationcost, int monthlycost, Guid plantypeid) {
			var linepricing = await _context.LinePricing.Where(x => x.Id == id).FirstOrDefaultAsync();
			if (linepricing == null) {
				LinePricing lp = new LinePricing();
				lp.Id = id;
				lp.RequiredNumOfLines = lineno;
				lp.InitiationFee = initationcost;
				lp.MonthlyCost = monthlycost;
				_context.Add(lp);
				await _context.SaveChangesAsync();
				var plantype = await _context.PlanTypes.Where(x => x.Id == plantypeid).FirstOrDefaultAsync();
				if (lineno == 1) {
					plantype.LinePricing1 = lp;
				}
				else if (lineno == 2) {
					plantype.LinePricing2 = lp;
				}
				else if (lineno == 3) {
					plantype.LinePricing3 = lp;
				}
				else if (lineno == 4) {
					plantype.LinePricing4 = lp;
				}
				_context.Update(lp);
				await _context.SaveChangesAsync();
			}
			else {
				linepricing.InitiationFee = initationcost;
				linepricing.MonthlyCost = monthlycost;
				_context.Update(linepricing);
				await _context.SaveChangesAsync();
			}
			return Json("success");
		}
	}
}
