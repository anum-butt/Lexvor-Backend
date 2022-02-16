using System;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.Data;
using Lexvor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	[Authorize(Roles = Roles.Admin)]
	public class UserPermissionsController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;

		public UserPermissionsController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var users = DbRaw.Query<UserRole>(@"
				select u.id as UserId, u.username, r.id as RoleId, r.name as RoleName
				from AspNetUsers u
				join AspNetUserRoles ur on ur.UserId = u.Id
				join AspNetRoles r on r.Id = ur.RoleId
				where r.Name != 'user' and
					  r.Name != 'level1' AND
					  r.Name != 'level2' AND
					  r.Name != 'trial'", null, _connStrings.DefaultConnection);

			return View(users);
		}


		//     [HttpGet]
		//     [Route("[action]")]
		//     public async Task<IActionResult> Edit(Guid id) {
		//         var profile = await _context.Profiles.FirstAsync(p => p.Id == id);
		//         var model = new UserEditViewModel() {
		//             Profile = profile,
		//             SelectListModel = new SelectListModel()
		//         };
		//         return View(model);
		//     }

		//     [HttpPost]
		//     [Route("[action]")]
		//     public async Task<IActionResult> Edit(UserEditViewModel model) {
		//      var blobPath = $"id-images/{model.Profile.Id}";
		//      var front = $"{blobPath}-front";
		//      var back = $"{blobPath}-back";

		//      // Upload the image to blob
		//      if (model.FrontImageUpload != null) {
		//       var data = new byte[model.FrontImageUpload.Length];
		//       var stream = new MemoryStream();
		//       await model.FrontImageUpload.CopyToAsync(stream);
		//       data = stream.ToArray();
		//       await BlobService.UploadBlob(data, front, _other, true);
		//       model.Profile.IDFrontUrl = front;
		//}
		//      if (model.BackImageUpload != null) {
		//       var data = new byte[model.BackImageUpload.Length];
		//       var stream = new MemoryStream();
		//       await model.BackImageUpload.CopyToAsync(stream);
		//       data = stream.ToArray();
		//       await BlobService.UploadBlob(data, back, _other, true);
		//	model.Profile.IDBackUrl = back;
		//}

		//_context.Update(model.Profile);
		//         await _context.SaveChangesAsync();
		//         return RedirectToAction(nameof(Index));
		//     }
	}

	public class UserRole {
		public Guid UserId { get; set; }
		public Guid RoleId { get; set; }
		public string Email { get; set; }
		public string RoleName { get; set; }
	}
}
