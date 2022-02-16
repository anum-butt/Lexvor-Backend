using System.Threading.Tasks;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lexvor.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller {
        [HttpGet]
        public async Task<IActionResult> Index() {
            var model = new ErrorViewModel();

            return View(model);
        }

        [HttpGet]
        public new async Task<IActionResult> NotFound() {
            var model = new ErrorViewModel();

            return View(model);
        }
    }
}
