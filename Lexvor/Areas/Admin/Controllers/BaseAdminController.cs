using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.Controllers;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers
{
    public class BaseAdminController : BaseUserController {
	    protected readonly ConnectionStrings _connStrings;

		public BaseAdminController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IOptions<ConnectionStrings> connStrings) : base(context, userManager) {
			_connStrings = connStrings.Value;
		}

	    public override void OnActionExecuting(ActionExecutingContext context) {
			// Set the staging flag in ViewBag
		    var controller = context.Controller as BaseUserController;
		    controller.ViewBag.IsStaging = _connStrings.DefaultConnection.Contains("stage");

			base.OnActionExecuting(context);
	    }

	    protected IActionResult LocalDefaultRedirect(string returnUrl, string action, string controller, object routeValues = null) {
		    if (string.IsNullOrWhiteSpace(returnUrl)) {
			    return RedirectToAction(action, controller, routeValues);
		    }
		    else {
			    return RedirectToLocal(returnUrl);
		    }
	    }
    }
}
