using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace Lexvor.Controllers
{
    //[ServiceFilter(typeof(ExceptionCatcher))]
    public abstract class BaseController : Controller {
        protected static HttpClient Http = new HttpClient();
        private const string MessageKey = "Message";
        private const string ErrorMessageKey = "ErrorMessage";

        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;

        public string CurrentUserEmail {
	        get {
		        var imp = GetSessionValue("IMPERSONATE_EMAIL");
		        if (imp.IsNull()) {
			        return User.Identity.IsAuthenticated ? User.Identity.Name : null;
		        }
		        else {
			        return imp;
		        }
	        }
        }

        protected async Task<ApplicationUser> GetCurrentAccount() {
            return await _userManager.FindByEmailAsync(CurrentUserEmail);
        }

        protected BaseController(
			ApplicationDbContext context,
            UserManager<ApplicationUser> userManager) {
		    _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// WARNING: Accessing this property is destructive. You must save the value after access if you need to use it more than once.
        /// </summary>
        public string Message {
	        get {
		        var key = GetSessionValue(MessageKey);
				HttpContext.Session.Remove(MessageKey);
		        return key;
	        }
	        set => HttpContext.Session.Set(MessageKey, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// WARNING: Accessing this property is destructive. You must save the value after access if you need to use it more than once.
        /// </summary>
        public string ErrorMessage {
			get {
				var key = GetSessionValue(ErrorMessageKey);
				HttpContext.Session.Remove(ErrorMessageKey);
				return key;
			}
			set => HttpContext.Session.Set(ErrorMessageKey, Encoding.UTF8.GetBytes(value));
        }

        public bool HasMessage => HttpContext.Session.Keys.Contains(MessageKey);
        public bool HasErrorMessage => HttpContext.Session.Keys.Contains(ErrorMessageKey);

        private string GetSessionValue(string key) {
            byte[] val;
            var exists = HttpContext.Session.TryGetValue(key, out val);
            return exists ? Encoding.UTF8.GetString(val) : "";
        }

        public override void OnActionExecuted(ActionExecutedContext context) {
            if (context.Exception != null) {
                var ex = context.Exception;
                // TODO Display Error?
            }

            if (!GetSessionValue("IMPERSONATE_ID").IsNull()) {
	            ViewData["IsImpersonating"] = true;
            }

            base.OnActionExecuted(context);
        }
        
        private static Random random = new Random();
        public static string RandomString(int length) {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        protected IActionResult RedirectToLocal(string returnUrl) {
            if (Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            } else {
                return RedirectToAction(nameof(HomeController.Index), HomeController.Name);
            }
        }
    }

    public class ExceptionCatcher : ExceptionFilterAttribute {
        public override void OnException(ExceptionContext context) {
            var ex = context.Exception;

            var features = context.HttpContext.Features;
            var service = features[typeof(IServiceProvidersFeature)] as RequestServicesFeature;
            var settingsManager = service?.RequestServices.GetService(typeof(IOptions<OtherSettings>));
            var settings = (settingsManager as OptionsManager<OtherSettings>)?.Value;
            if (settings != null) {
                ErrorHandler.Capture(settings.SentryDSN, ex);
            }

            base.OnException(context);
        }
    }

    public class SetTempDataModelStateAttribute : ActionFilterAttribute {
        public override void OnActionExecuted(ActionExecutedContext filterContext) {
            base.OnActionExecuted(filterContext);
            var controller = filterContext.Controller as Controller;
            controller.TempData["ModelState"] = JsonConvert.SerializeObject(controller.ViewData.ModelState);
        }
    }

    public class RestoreModelStateFromTempDataAttribute : ActionFilterAttribute {
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            base.OnActionExecuting(filterContext);
            var controller = filterContext.Controller as Controller;
            if (controller.TempData.ContainsKey("ModelState")) {
                controller.ViewData.ModelState.Merge(
                    JsonConvert.DeserializeObject<ModelStateDictionary>(controller.TempData["ModelState"].ToString()));
            }
        }
    }

    public class EnableReturnUrlAttribute : ActionFilterAttribute {
	    public override void OnActionExecuting(ActionExecutingContext filterContext) {
		    base.OnActionExecuting(filterContext);
		    var controller = filterContext.Controller as Controller;

		    var queryStringKeys = filterContext.HttpContext.Request.Query.Keys.Select(x => (x, x.ToLower()));
			var key = queryStringKeys.FirstOrDefault(x => x.Item2 == "returnurl").x;
		    if (key != null) {
			    filterContext.HttpContext.Request.Query.TryGetValue(key, out StringValues redirect);
			    controller.ViewData["returnUrl"] = redirect.ToString();

		    }
	    }
    }
}
