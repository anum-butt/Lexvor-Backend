using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.Controllers.API {
    public class BaseApiController : ControllerBase {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _context;

        public BaseApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) {
            _userManager = userManager;
            _context = context;
        }

        public string CurrentUserEmail => User.Identity.IsAuthenticated ? User.Identity.Name : null;
        protected Profile CurrentProfile => CurrentUserEmail != null ? _context.Profiles.Include(x => x.BillingAddress).Include(x => x.ShippingAddress).First(p => p.Account.Email == CurrentUserEmail) : null;

        protected async Task<ApplicationUser> GetCurrentAccount() {
            return await _userManager.FindByEmailAsync(CurrentUserEmail);
        }

        private static Random random = new Random();
        public static string RandomString(int length) {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
