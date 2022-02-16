//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Lexvor.API;
//using Lexvor.API.Objects;
//using Lexvor.Areas.Admin.Controllers;
//using Lexvor.Data;
//using Lexvor.Models;
//using Lexvor.Models.AdminViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Lexvor.Controllers.Admin
//{
//    [Area("Admin")]
//    [Route("admin/[controller]")]
//    [Authorize(Roles = Roles.Manager)]
//    public class WaitListController : BaseAdminController
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly OtherSettings _other;

//        public WaitListController(
//            UserManager<ApplicationUser> userManager,
//            IOptions<ConnectionStrings> connStrings,
//            IOptions<OtherSettings> other,
//            ApplicationDbContext context): base(context, userManager, connStrings) {
//            _userManager = userManager;
//            _other = other.Value;
//        }

//        [HttpGet]
//        public async Task<IActionResult> Index() {
//            var waitList = await _context.WaitListUsers.ToListAsync();
            
//            return View(waitList);
//		}

//	    [HttpGet]
//	    [Route("[action]")]
//		public async Task<IActionResult> MarkUserProcessed(Guid id) {
//		    var user = _context.WaitListUsers.First(g => g.Id == id);
//		    user.Processed = true;
//		    await _context.SaveChangesAsync();

//		    return RedirectToAction(nameof(Index));
//	    }
//	}
//}
