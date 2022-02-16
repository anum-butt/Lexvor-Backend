using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	public class UserDevicesController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;
		private readonly IEmailSender _emailSender;

		public static string Name => "UserDevices";

		public UserDevicesController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			IEmailSender emailSender,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_emailSender = emailSender;
			_other = other.Value;
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> ReturnsRequested() {
			var userDevices = await _context.UserDevices.Where(u => u.ReturnRequested.HasValue).Select(u => u.Id).ToListAsync();

			var plans = await _context.Plans.Include(p => p.UserDevice).Include(p => p.PlanType).Include(p => p.Profile)
				.Include(p => p.Device).Where(p => p.UserDeviceId.HasValue && userDevices.Contains(p.UserDeviceId.Value))
				.ToListAsync();


			return View(plans.OrderBy(p => p.UserDevice.ReturnRequested));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> TradeIns() {
			var pending = await _context.DeviceIntakes.Include(x => x.Profile).Where(x => !x.Approved.HasValue && x.IntakeType == IntakeType.NewCustomerTradeIn).ToListAsync();
			var complete = await _context.DeviceIntakes.Include(x => x.Profile).Where(x => x.Approved.HasValue && x.IntakeType == IntakeType.NewCustomerTradeIn).OrderByDescending(t => t.Requested).ToListAsync();

			return View(new TradeInViewModel() {
				Pending = pending,
				Complete = complete
			});
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> ApproveTradeIn(Guid id) {
			var tradein = await _context.DeviceIntakes.Include(x => x.Profile).ThenInclude(x => x.Account).FirstOrDefaultAsync(t => t.Id == id);

			if (tradein == null) {
				ErrorMessage = "Trade In ID was incorrect. Trade In not approved.";
			}
			else {
				tradein.Approved = DateTime.UtcNow;

				// Send user email
				_emailSender.SendEmailAsync(tradein.Profile.Account.Email, "Your trade-in has been approved.",
					"Thank you for choosing Lexvor Wireless. Your trade-in has been approved. Please contact your account manager to discuss the trade-in offer. customerservice@lexvor.com");

				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(TradeIns));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> DeviceRequests() {
			var userDeviceUpgrades = await _context.UserDevices.Where(u => !u.Shipped.HasValue).ToListAsync();
			var userDevices = _context.UserDevices.Where(u => u.Shipped.HasValue).ToList();
			var planIds = userDeviceUpgrades.Select(u => u.PlanId).ToList();

			var plans = _context.Plans.Include(p => p.PlanType).Include(p => p.Profile)
				.Include(p => p.Device).Where(p => p.UserDeviceId.HasValue && planIds.Contains(p.Id))
				.ToList();

			var model = new DeviceRequestsViewModel() {
				Upgrades = new List<DeviceRequestViewModel>(),
				New = new List<DeviceRequestViewModel>()
			};
			foreach (var userDevice in userDeviceUpgrades) {

				// TODO
				//if (userDevice.Upgrade) {
				//	var plan = plans.FirstOrDefault(p => p.UserDeviceId.HasValue && p.UserDeviceId.Value == userDevice.Id);
				//	if (plan != null) {
				//		var single = new DeviceRequestViewModel() {
				//			UserDevice = userDevice,
				//			UserPlan = plan,
				//			PastUserDevice = userDevices.Where(u => u.PlanId == plan.Id && u.Shipped.HasValue)
				//				.OrderByDescending(u => u.Requested).FirstOrDefault()
				//		};
				//		model.Upgrades.Add(single);
				//	}
				//} else {
				//	var plan = plans.FirstOrDefault(p => p.UserDeviceId.HasValue && p.UserDeviceId.Value == userDevice.Id);
				//	if (plan != null) {
				//		var single = new DeviceRequestViewModel() {
				//			UserDevice = userDevice,
				//			UserPlan = plan,
				//			PastUserDevice = userDevices.Where(u => u.PlanId == plan.Id && u.Shipped.HasValue).OrderByDescending(u => u.Requested).FirstOrDefault()
				//		};
				//		model.New.Add(single);
				//	}
				//}
			}

			return View(model);
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> CompleteRequest(Guid id) {
			// Userdeviceid
			var userDevice = await _context.UserDevices.FirstAsync(t => t.Id == id);
			userDevice.Shipped = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(DeviceRequests));
		}

		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> DeviceAccessories() {
			// Get accessory orders from the last 180 days
			var orders = await _context.UserOrders.Include(x => x.Accessory1).Include(x => x.Accessory2).Include(x => x.Accessory3).Where(x => x.OrderDate < DateTime.UtcNow && x.OrderDate > DateTime.UtcNow.AddDays(-180))
				.Where(x => x.Accessory1 != null || x.Accessory2 != null || x.Accessory3 != null).ToListAsync();
			var model = new UserOrdersViewModel() {
				Orders = new List<UserOrderViewModel>()
			};

			foreach (var order in orders) {
				var profile = await _context.Profiles.FirstOrDefaultAsync(x => x.Id == order.ProfileId);
				var orderModel = new UserOrderViewModel() {
					CustomerName = "",
					Accessory1 = null,
					Accessory2 = null,
					Accessory3 = null,
					AccessoryTotal = 0
				};

				orderModel.CustomerName = profile.FullName;

				if (order.Accessory1 != null) {
					orderModel.Accessory1 = order.Accessory1;
					orderModel.AccessoryTotal += order.Accessory1.Price;
					if (order.Accessory1.LifetimeWarranty) {
						orderModel.AccessoryTotal += order.Accessory1.LifetimeWarrantyPrice;
					}
				}
				if (order.Accessory2 != null) {
					orderModel.Accessory2 = order.Accessory2;
					orderModel.AccessoryTotal += order.Accessory2.Price;
					if (order.Accessory2.LifetimeWarranty) {
						orderModel.AccessoryTotal += order.Accessory2.LifetimeWarrantyPrice;
					}
				}
				if (order.Accessory3 != null) {
					orderModel.Accessory3 = order.Accessory3;
					orderModel.AccessoryTotal += order.Accessory3.Price;
					if (order.Accessory3.LifetimeWarranty) {
						orderModel.AccessoryTotal += order.Accessory3.LifetimeWarrantyPrice;
					}
				}

				model.Orders.Add(orderModel);
			}

			return View(model);
		}



		[HttpGet]
		[Route("[action]")]
		[Authorize(Roles = Roles.Manager)]
		public async Task<IActionResult> ToggleUpgrade(Guid userDeviceId, string returnUrl) {
			var userDevice = await _context.UserDevices.FirstOrDefaultAsync(u => u.Id == userDeviceId);

			userDevice.UpgradeAvailable = !userDevice.UpgradeAvailable;

			await _context.SaveChangesAsync();

			return RedirectToLocal(returnUrl);
		}

	}

	public class DeviceRequestsViewModel {
		public List<DeviceRequestViewModel> Upgrades { get; set; }
		public List<DeviceRequestViewModel> New { get; set; }
	}

	public class DeviceRequestViewModel {
		public UserDevice UserDevice { get; set; }
		public UserDevice PastUserDevice { get; set; }
		public UserPlan UserPlan { get; set; }
	}

	public class UserOrdersViewModel {
		public List<UserOrderViewModel> Orders { get; set; }
	}

	public class UserOrderViewModel {
		public string CustomerName { get; set; }
		public UserAccessory Accessory1 { get; set; }
		public UserAccessory Accessory2 { get; set; }
		public UserAccessory Accessory3 { get; set; }
		public int AccessoryTotal { get; set; }
	}
}
