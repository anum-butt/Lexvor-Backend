using System;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class SIMController : BaseAdminController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OtherSettings _other;

        public static string Name => "SIM";

        public SIMController(
            UserManager<ApplicationUser> userManager,
            IOptions<ConnectionStrings> connStrings,
            IOptions<OtherSettings> other,
            ApplicationDbContext context): base(context, userManager, connStrings) {
            _userManager = userManager;
            _other = other.Value;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Level1Support)]
        public async Task<IActionResult> Index() {
            var items = await _context.StockedSims.OrderBy(x => x.Available).ThenByDescending(x => x.DateAdded).ToListAsync();
            
            return View(items);
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = Roles.TrustedManager)]
        public async Task<IActionResult> Assignment(Guid planId, string returnUrl = "") {
	        ViewData["returnUrl"] = returnUrl;
	        
	        var items = await _context.StockedSims.Where(x => x.Available).OrderBy(x => x.DateAdded).ToListAsync();
            
	        return View(new SIMAssignmentViewModel() {
				AvailableSIMs = items,
				PlanId = planId
	        });
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = Roles.TrustedManager)]
        public async Task<IActionResult> Assign(Guid id, Guid planId, string returnUrl = "") {
	        var sim = await _context.StockedSims.FirstAsync(x => x.Id == id);
	        sim.Available = false;

	        var plan = await _context.Plans.FirstAsync(x => x.Id == planId);
	        plan.AssignedSIMICC = sim.ICCNumber;

	        await _context.SaveChangesAsync();

	        if (string.IsNullOrWhiteSpace(returnUrl)) {
		        return RedirectToAction(nameof(Index));
	        }
	        else {
		        return RedirectToLocal(returnUrl);
	        }
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = Roles.TrustedManager)]
        public async Task<IActionResult> MarkShipped(Guid id, string returnUrl = "") {
			// UserPlanId
			var plan = await _context.Plans.Include(x => x.PortRequest).FirstAsync(x => x.Id == id);

			if (string.IsNullOrWhiteSpace(plan.AssignedSIMICC)) {
				ErrorMessage = "No SIM Assigned";
			} else{
				plan.SIMShipped = DateTime.UtcNow;
				if (plan.PortRequest != null) {
					plan.PortRequest.CanBeSubmitted = true;
				}
				await _context.SaveChangesAsync();
			}

	        if (string.IsNullOrWhiteSpace(returnUrl)) {
		        return RedirectToAction(nameof(Index));
	        }
	        else {
		        return RedirectToLocal(returnUrl);
	        }
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = Roles.TrustedManager)]
        public async Task<IActionResult> Deactivate(Guid id) {
	        var sim = await _context.StockedSims.FirstAsync(x => x.Id == id);
	        sim.Available = false;
	        await _context.SaveChangesAsync();

	        return RedirectToAction(nameof(Index));
        }

        [HttpPost]
	    [Route("[action]")]
        [Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> UploadSims(string sims) {
			var stocked = sims.Split(",").Select(x => new StockedSim() {
				Available = true,
				DateAdded = DateTime.UtcNow,
				ICCNumber = x
			});
			//To check the system for existing SIMS with the same ICC
			if (stocked.Any()) {
				foreach (var itm in stocked) {
					var existingSims = _context.StockedSims.Where(x => x.ICCNumber == itm.ICCNumber).FirstOrDefault();
					if (existingSims == null) {
						_context.Add(itm);
						await _context.SaveChangesAsync();
					}
				}
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		 public async Task<JsonResult> UnAssignSim(Guid id) {
			var sim = await _context.StockedSims.FirstAsync(x => x.Id == id);
			sim.Available = true;

			var plan =  _context.Plans.Where(x => x.AssignedSIMICC == sim.ICCNumber).FirstOrDefault();
			plan.AssignedSIMICC = null;	

			await _context.SaveChangesAsync();
			return Json(true);
		}

	}
}
