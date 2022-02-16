using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Lexvor.Helpers {
	public static class ViewHelpers {
		public static object PermissionedView(HttpContext context, object value, string role)
		{
			if (context.User.IsInRole(role)) {
				return value;
			}
			else {
				return "****";
			}
		}
		public static bool IsPermissioned(HttpContext context, string role) {
			return context.User.IsInRole(role);
		}
	}
}
