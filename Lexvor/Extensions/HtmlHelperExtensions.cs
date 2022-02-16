using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lexvor.Extensions
{
    public static class HtmlHelpers {

        public static string ActiveLink(ViewContext context, string actionName, string controllerName) {
            string currentAction = context.RouteData.Values["action"].ToString();
            string currentController = context.RouteData.Values["controller"].ToString();

            if (actionName == currentAction && controllerName == currentController) {
                return "active";
            }

            return "";
        }
    }
}
