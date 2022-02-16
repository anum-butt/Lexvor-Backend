using System;
using System.Linq;
using System.Text;
using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.Controllers {
    //[ServiceFilter(typeof(ExceptionCatcher))]
    public abstract class BaseUserController : BaseController {
	    private Profile currentProfile { get; set; }
        protected Profile CurrentProfile {
	        get {
		        var imp = GetSessionValue("IMPERSONATE_ID");
		        if (!imp.IsNull()) {
			        currentProfile = _context.Profiles.Include(x => x.BillingAddress).Include(x => x.ShippingAddress).First(p => p.Id == Guid.Parse(imp));
		        }
		        else {
			        if (currentProfile == null) {
				        currentProfile = CurrentUserEmail != null ? _context.Profiles.Include(x => x.BillingAddress).Include(x => x.ShippingAddress).First(p => p.Account.Email == CurrentUserEmail) : null;
					}
		        }

		        return currentProfile;
	        }
        }
        private string GetSessionValue(string key) {
	        byte[] val;
	        var exists = HttpContext.Session.TryGetValue(key, out val);
	        return exists ? Encoding.UTF8.GetString(val) : "";
        }

        protected BaseUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) {
        }
        
        public override void OnActionExecuting(ActionExecutingContext context) {
            // If user is not authenticated or CurrentUserEmail is null redirect to login
            //if (!User.Identity.IsAuthenticated || CurrentUserEmail == null) {
	           // context.Result = RedirectToAction(nameof(AccountController.Login), AccountController.Name);
            //}

            base.OnActionExecuting(context);

            //var businessRole = context.HttpContext.User.IsInRole("business");
            //var currentController = context.RouteData.Values["controller"].ToString();

            //if (businessRole && currentController != "Business" && currentController != "Account") {
            //    context.Result = RedirectToAction("Details", "Business");
            //}
        }
    }
}
