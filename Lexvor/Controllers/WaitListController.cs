//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Lexvor.API;
//using Lexvor.API.Objects;
//using Lexvor.API.Services;
//using Lexvor.Data;
//using Microsoft.AspNetCore.Mvc;
//using Lexvor.Models;
//using Lexvor.Models.AccountViewModels;
//using Lexvor.Models.HomeViewModels;
//using Lexvor.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Lexvor.Controllers {

//    [AllowAnonymous]
//    //[ServiceFilter(typeof(ExceptionCatcher))]
//    public class WaitListController : BaseUserController {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly ConnectionStrings _connStrings;
//        private readonly OtherSettings _other;
//        private readonly IEmailSender _emailSender;
//        private readonly string _defaultConn;

//        public WaitListController(
//            UserManager<ApplicationUser> userManager,
//            IEmailSender emailSender,
//            IOptions<ConnectionStrings> connStrings,
//            IOptions<OtherSettings> other,
//            ApplicationDbContext context) : base(context, userManager) {
//            _userManager = userManager;
//            _emailSender = emailSender;
//            _connStrings = connStrings.Value;
//            _other = other.Value;
//            _defaultConn = _connStrings.DefaultConnection;
//        }

//        [TempData]
//        public string WaitListUserId { get; set; }

//        public async Task<IActionResult> Index() {
//            ViewBag.Plans = await PlanTypeService.GetPlanTypes(_context);
//            ViewBag.Phones = await _context.Devices.Where(x => !x.Archived && x.Available).Select(x => new { x.Name }).Distinct().ToListAsync();
//            return View(new Profile());
//        }

//	    [HttpPost]
//	    public async Task<IActionResult> Index(Profile model) {
//		    // If we have an existing user use that one.
//		    var user = await _context.Profiles.FirstOrDefaultAsync(x => x.Email == model.Email && !x.Processed);

//		    if (user == null) {
//				// Only validate model state if this is a new user.
//			    if (ModelState.IsValid) {
//				    model.LastModified = DateTime.UtcNow;
//				    _context.WaitListUsers.Add(model);
//				    await _context.SaveChangesAsync();
//				    WaitListUserId = model.Id.ToString();

//				    // Send email
//				    await EmailService.SendWaitListEmail(_emailSender, _other, model.Email, model.FullName, await GetRank(model.Email));
//			    }
//			    else {
//					ModelState.AddModelError("", "You have not signed up for the waitlist.");
//				    ViewBag.Plans = await _context.PlanTypes.ToListAsync();
//			        ViewBag.Phones = await _context.Devices.Where(x => !x.Archived && x.Available).Select(x => new { Name = x.Name }).Distinct().ToListAsync();
//                    return View(model);
//			    }
//		    }
//		    else {
//			    WaitListUserId = user.Id.ToString();
//				// If the user has already deposited, go straight to the success screen
//				if (user.Deposited) {
//				    return RedirectToAction(nameof(DepositSuccess));
//			    }
//		    }

//		    return RedirectToAction(nameof(Deposit));
//	    }

//	    public async Task<IActionResult> Deposit() {
//		    if (string.IsNullOrEmpty(WaitListUserId)) {
//			    return RedirectToAction(nameof(Index));
//		    }
//			var id = Guid.Parse(WaitListUserId);
//		    if (id == Guid.Empty) {
//			    return RedirectToAction(nameof(Index));
//		    }

//			var user = await _context.WaitListUsers.FirstAsync(w => w.Id == id);
			
//			return View(new WaitListDepositViewModel() {
//                StripeKey = _other.StripePublishableKey,
//                WaitListUser = user,
//				Rank = await GetRank(user.Email),
//				Spots = 14,
//                DepositAmount = Convert.ToInt32((await _context.Settings.FirstAsync(x => x.Key == "WaitListAmount")).Value)
//			});
//        }

//        [HttpPost]
//        public async Task<IActionResult> Deposit(WaitListDepositViewModel model) {
//            try {
//                var user = await _context.WaitListUsers.FirstAsync(w => w.Id == model.WaitListUser.Id);
//	            WaitListUserId = user.Id.ToString();

//				var token = Request.Form.FirstOrDefault(b => b.Key == "stripeToken").Value.ToString() ?? "";

//                var customerId = Payments.GetCustomer(user.Email);
//                user.CustomerId = customerId;

//                var depositAmt =
//                    Convert.ToInt32((await _context.Settings.FirstAsync(x => x.Key == "WaitListAmount")).Value);
//                var orderId = Payments.ChargeCustomer(depositAmt, model.WaitListUser.Email, "Wait List Deposit", token);

//                user.Deposited = true;
//                user.TransactionId = orderId;
//                await _context.SaveChangesAsync();

//	            return RedirectToAction(nameof(DepositSuccess));
//			}
//            catch (Exception e) {
//                ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Waitlist", new Dictionary<string, string>() {
//                    { "Email", model.WaitListUser.Email }
//                });

//                ModelState.AddModelError("", "There was an error processing your charge, please try again. If this has happened more than once, please contact support. fyinanceteam@fyinance.net");
//                return View(model);
//            }
//        }

//        public async Task<IActionResult> DepositSuccess() {
//	        var id = Guid.Parse(WaitListUserId);
//	        if (id == Guid.Empty) {
//		        return RedirectToAction(nameof(Index));
//	        }

//	        var user = await _context.WaitListUsers.FirstAsync(w => w.Id == id);

//			return View(new WaitListDepositSuccessViewModel() {
//				Rank = await GetRank(user.Email),
//				WaitListUser = user
//			});
//        }

//	    private async Task<int> GetRank(string userEmail) {
//			var ranking = await _context.WaitListUsers.Where(w => !w.Processed).OrderByDescending(w => w.Deposited)
//			    .ThenBy(w => w.LastModified).ToListAsync();

//		    var index = ranking.FindIndex(w => w.Email == userEmail);
//		    if (index != -1) {
//			    index = index + 5;
//		    } else {
//			    index = 150;
//		    }

//		    return index;
//	    }
//    }
//}
