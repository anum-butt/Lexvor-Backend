using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.ProfileViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Controllers {
    public class DeviceIntakeController : BaseUserController {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ConnectionStrings _connStrings;
        private readonly OtherSettings _other;
        private readonly IEmailSender _emailSender;
        private readonly string _defaultConn;

        public static string Name => "DeviceIntake";

        public DeviceIntakeController(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IOptions<ConnectionStrings> connStrings,
            IOptions<OtherSettings> other,
            ApplicationDbContext context) : base(context, userManager) {
            _userManager = userManager;
            _emailSender = emailSender;
            _connStrings = connStrings.Value;
            _other = other.Value;
            _defaultConn = _connStrings.DefaultConnection;
        }

        [Authorize(Roles = Roles.Trial)]
        public async Task<IActionResult> Index() {
            var user = await _userManager.FindByEmailAsync(CurrentUserEmail);
            var pastIntakes = await _context.DeviceIntakes.Where(t => t.ProfileId == CurrentProfile.Id).OrderBy(x => x.Received).ToListAsync();

            return View(new DeviceIntakeIndexViewModel() {
                Profile = CurrentProfile,
                User = user,
                Intake = new DeviceIntake(),
                IntakeRequests = pastIntakes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DeviceIntakeIndexViewModel model) {
            if (ModelState.IsValid) {
                model.Intake.Id = Guid.NewGuid();
                var blobPath = $"trade-in-images/{model.Intake.Id}";

                model.Intake.ProfileId = CurrentProfile.Id;
                model.Intake.Requested = DateTime.UtcNow;
                model.Intake.IntakeType = IntakeType.NewCustomerTradeIn;

				// Upload the image to blob
				if (model.Intake.FrontImageUpload != null) {
                    var front = $"{blobPath}-front{Path.GetExtension(model.Intake.FrontImageUpload.FileName)}";
                    var data = new byte[model.Intake.FrontImageUpload.Length];
                    var stream = new MemoryStream();
                    await model.Intake.FrontImageUpload.CopyToAsync(stream);
                    data = stream.ToArray();
                    await BlobService.UploadBlob(data, front, _other);
                    model.Intake.FrontImageUrl = front;
                }

                if (model.Intake.BackImageUpload != null) {
                    var back = $"{blobPath}-back{Path.GetExtension(model.Intake.BackImageUpload.FileName)}";
                    var data = new byte[model.Intake.BackImageUpload.Length];
                    var stream = new MemoryStream();
                    await model.Intake.BackImageUpload.CopyToAsync(stream);
                    data = stream.ToArray();
                    await BlobService.UploadBlob(data, back, _other);
                    model.Intake.BackImageUrl = back;
                }

                if (model.Intake.OnImageUpload != null) {
                    var on = $"{blobPath}-on{Path.GetExtension(model.Intake.OnImageUpload.FileName)}";
                    var data = new byte[model.Intake.OnImageUpload.Length];
                    var stream = new MemoryStream();
                    await model.Intake.OnImageUpload.CopyToAsync(stream);
                    data = stream.ToArray();
                    await BlobService.UploadBlob(data, on, _other);
                    model.Intake.OnImageUrl = on;
                }

                await _context.DeviceIntakes.AddAsync(model.Intake);
                await _context.SaveChangesAsync();

                Message = "Your request has been received successfully.";

                return RedirectToAction(nameof(Index), Name);
            } else {
                model.User = await _userManager.FindByEmailAsync(CurrentUserEmail);
                model.IntakeRequests = await _context.DeviceIntakes.Where(t => t.ProfileId == CurrentProfile.Id).ToListAsync();
                return View(model);
            }
        }
    }
}
