using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.ProfileViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sentry.Protocol;
using Device = Lexvor.API.Objects.Device;

namespace Lexvor.Controllers.API {

	[ApiController]
	[Route("api/[controller]")]
	[Produces(MediaTypeNames.Application.Json)]
	public class PlanController : BaseApiController {
		private readonly OtherSettings _other;

		public PlanController(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IOptionsMonitor<OtherSettings> other
			) : base(context, userManager) {
			_other = other.CurrentValue;
		}

		[HttpGet("GetDevices/{id}")]
		public async Task<IActionResult> GetDevices(Guid id) {
			// PlanTypeId
			var plan = await _context.PlanTypes.Include(x => x.Devices).ThenInclude(x => x.Device).ThenInclude(x => x.Options).FirstAsync(x => x.Id == id);
			var devices = plan.Devices.OrderBy(x => x.Device.SortOrder).Select(x => new Device() {
				Id = x.Device.Id,
				Archived = x.Device.Archived,
				Available = x.Device.Available,
				Description = x.Device.Description,
				DeviceType = x.Device.DeviceType,
				ImageUrl = x.Device.ImageUrl,
				Name = x.Device.Name,
				Price = x.Device.Price,
				Options = x.Device.Options.Select(y => new DeviceOption() {
					OptionGroup = y.OptionGroup,
					OptionValue = y.OptionValue,
					Surcharge = y.Surcharge,
					Id = y.Id
				}).ToList()
			});

			return Ok(devices);
		}

		[HttpGet("IsDeviceInStock")]
		public async Task<IActionResult> IsDeviceInStock(Guid deviceId) {
			var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId);
			var isDeviceInStock = await _context.StockedDevice.Include(x => x.Device).AnyAsync(x => x.Device.Id == device.Id);
			return Ok(isDeviceInStock);
		}

		private async Task SetAccessories(List<AccessoryLineViewModel> model) {
			var availableAccessories = await _context.Accessories.ToListAsync();
			var orderTime = DateTime.UtcNow;
			var orderId = Guid.NewGuid();
			foreach (var plan in model) {
				var total = 0;
				foreach (var accGroup in plan.AccessoryGroups) {
					var selected = accGroup.Accessories.FirstOrDefault(x => x.Selected);
					if (selected != null) {
						total += selected.Price;
						if (selected.SelectedWarranty) {
							total += selected.LifetimeWarrantyPrice;
						}
					}
				}

				var order = new UserOrder() {
					ProfileId = CurrentProfile.Id,
					UserPlanId = plan.UserPlan.Id,
					OrderDate = orderTime,
					Total = total,
					OrderId = orderId
				};
				// Add lines to order
				if (plan.AccessoryGroups.Count >= 1) {
					var selectedAcc = plan.AccessoryGroups[0].Accessories.Where(y => y.Selected);
					if (selectedAcc.Count() != 0) {
						var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
						order.Accessory1 = new UserAccessory() {
							Accessory = accessory.Name,
							LifetimeWarranty = accessory.LifetimeWarranty,
							LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
							Price = accessory.Price,
							OrderId = orderId
						};
					}
				}
				if (plan.AccessoryGroups.Count >= 2) {
					var selectedAcc = plan.AccessoryGroups[1].Accessories.Where(y => y.Selected);
					if (selectedAcc.Count() != 0) {
						var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
						order.Accessory2 = new UserAccessory() {
							Accessory = accessory.Name,
							LifetimeWarranty = accessory.LifetimeWarranty,
							LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
							Price = accessory.Price,
							OrderId = orderId
						};
					}
				}
				if (plan.AccessoryGroups.Count >= 3) {
					var selectedAcc = plan.AccessoryGroups[2].Accessories.Where(y => y.Selected);
					if (selectedAcc.Count() != 0) {
						var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
						order.Accessory3 = new UserAccessory() {
							Accessory = accessory.Name,
							LifetimeWarranty = accessory.LifetimeWarranty,
							LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
							Price = accessory.Price,
							OrderId = orderId
						};
					}
				}
				_context.UserOrders.Add(order);
			}
		}

		private async Task SetDevices(ChooseDevicePurchasesModel model) {
			var pendingPlans = await _context.Plans.Where(x => x.ProfileId == CurrentProfile.Id
															   && x.Status == PlanStatus.Pending
															   && x.UserDeviceId == null
															   && x.PlanTypeId == model.SelectedPlanId)
												   .ToListAsync();

			// Validate that we have enough IMEI's for the lines on BYOD
			if (model.BYOD && model.IMEIs.Count(x => !string.IsNullOrWhiteSpace(x)) != model.NumberOfLines) {
				throw new Exception("Invalid count of IMEIS.");
			}

			if (pendingPlans.Count != model.NumberOfLines) {
				var ex = new Exception(
					"Your number of pending plans did not match the number of lines you selected. " +
					"Please cancel your pending purchases and try again.");
				ErrorHandler.Capture(_other.SentryDSN, ex, "Set-Devices", new Dictionary<string, string>() {
					{"Model", JsonConvert.SerializeObject(model)}
				});
				throw ex;
			}

			for (int i = 1; i <= model.NumberOfLines; i++) {
				var zero = i - 1;

				Guid? device = null;
				if (!model.BYOD) {
					var selected = model.SelectedDeviceIds.ElementAtOrDefault(zero);
					if (!selected.IsNull()) {
						device = Guid.Parse(selected);
					}
					else {
						throw new Exception("Device for plan is not selected.");
					}
				}

				var imei = "";
				if (model.BYOD) {
					var selected = model.IMEIs.ElementAtOrDefault(zero)?.Trim();
					if (!selected.IsNull()) {
						imei = selected;
					}
					else {
						throw new Exception("You did not provide an IMEI for each device that you are bringing.");
					}
				}

				var options = new List<DeviceOption>();
				if (model.SelectedOptions.Count >= i) {
					var optionIds = model.SelectedOptions[zero].Options.Select(x => x.Id);
					// Even though we are only using Guids below, do this to make sure that ids passed are in the DB
					options = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id)).ToListAsync();
				}

				pendingPlans[zero].IsPorting = model.Porting;
				// Only attach the device and user device if this is a wireless + device plan.
				pendingPlans[zero].DeviceId = device;
				pendingPlans[zero].UserDevice = new UserDevice() {
					PlanId = pendingPlans[zero].Id,
					IsActive = false,
					Requested = DateTime.UtcNow,
					Profile = CurrentProfile,
					BYOD = model.BYOD,
					IMEI = imei,
					ChosenOptions = UserDevice.SetOptions(options),
					PurchasedWithAffirm = model.IsAffirmConfirmed
				};
			}
		}

		[HttpPost("SetAccessoryPurchase")]
		public async Task<IActionResult> SetAccessoryPurchase(List<AccessoryLineViewModel> model) {
			// Convert to user order for each line
			var availableAccessories = _context.Accessories.ToList();
			var orderTime = DateTime.UtcNow;
			var orderId = Guid.NewGuid();
			try {
				foreach (var plan in model) {
					var total = 0;
					foreach (var accGroup in plan.AccessoryGroups) {
						var selected = accGroup.Accessories.FirstOrDefault(x => x.Selected);
						if (selected != null) {
							total += selected.Price;
							if (selected.SelectedWarranty) {
								total += selected.LifetimeWarrantyPrice;
							}
						}
					}

					var order = new UserOrder() {
						ProfileId = CurrentProfile.Id,
						UserPlanId = plan.UserPlan.Id,
						OrderDate = orderTime,
						Total = total,
						OrderId = orderId
					};
					// Add lines to order
					if (plan.AccessoryGroups.Count >= 1) {
						var selectedAcc = plan.AccessoryGroups[0].Accessories.Where(y => y.Selected);
						if (selectedAcc.Count() != 0) {
							var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
							order.Accessory1 = new UserAccessory() {
								Accessory = accessory.Name,
								LifetimeWarranty = accessory.LifetimeWarranty,
								LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
								Price = accessory.Price,
								OrderId = orderId
							};
						}
					}
					if (plan.AccessoryGroups.Count >= 2) {
						var selectedAcc = plan.AccessoryGroups[1].Accessories.Where(y => y.Selected);
						if (selectedAcc.Count() != 0) {
							var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
							order.Accessory2 = new UserAccessory() {
								Accessory = accessory.Name,
								LifetimeWarranty = accessory.LifetimeWarranty,
								LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
								Price = accessory.Price,
								OrderId = orderId
							};
						}
					}
					if (plan.AccessoryGroups.Count >= 3) {
						var selectedAcc = plan.AccessoryGroups[2].Accessories.Where(y => y.Selected);
						if (selectedAcc.Count() != 0) {
							var accessory = availableAccessories.First(x => selectedAcc.Select(x => x.Name).Contains(x.Name));
							order.Accessory3 = new UserAccessory() {
								Accessory = accessory.Name,
								LifetimeWarranty = accessory.LifetimeWarranty,
								LifetimeWarrantyPrice = accessory.LifetimeWarrantyPrice,
								Price = accessory.Price,
								OrderId = orderId
							};
						}
					}
					_context.UserOrders.Add(order);
				}
			} catch	(Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, "UserOrderLoop");
				return Ok(new {
					success = false,
					error = "There was an error processing your accessory order. Please try again or continue without accessories."
				});
			}

			try {
				await _context.SaveChangesAsync();
			} catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, "UserOrders");
				return Ok(new {
					success = false,
					error = "There was an error processing your accessory order. Please try again or continue without accessories."
				});
			}

			return Ok(new {
				success = true,
				error = ""
			});
		}

		public async Task<IActionResult> SetPlanPurchases(PlanPurchaseModel model) {
			if (!_other.DeviceOrderingEnabled) {
				return Forbid();
			}
			var planType = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.FirstAsync(x => x.Id == model.SelectedPlanId);

			// Validate that we have enough IMEI's for the lines on BYOD
			if (model.BYOD && model.IMEIs.Count(x => !string.IsNullOrWhiteSpace(x)) != model.NumberOfLines) {
				return Ok(new {
					success = false,
					redirectUrl = "",
					error = "You did not provide an IMEI for each line you are purchasing."
				});
			}
			// Validate that we have enough IMEI's for the lines on BYOD
			if (model.Porting && model.MobileNumbers.Count(x => !string.IsNullOrWhiteSpace(x)) != model.NumberOfLines) {
				return Ok(new {
					success = false,
					redirectUrl = "",
					error = "You did not provide a mobile number to port for each line you are purchasing."
				});
			}

			var pricing = PlanService.GetLinePricing(model.NumberOfLines, planType);

			for (int i = 1; i <= model.NumberOfLines; i++) {
				try {
					var zero = i - 1;
					// Use number of lines instead of device list to make sure that only lines the user selected are added
					var givenName = model.MobileNumbers.ElementAtOrDefault(zero) != null ? model.MobileNumbers.ElementAtOrDefault(zero)?.Trim() : "Plan " + RandomString(5);

					Guid? device = null;
					if (!model.BYOD) {
						var selected = model.SelectedDeviceIds.ElementAtOrDefault(zero);
						if (!selected.IsNull()) {
							device = Guid.Parse(selected);
						} else {
							return Ok(new {
								success = false,
								error = "You did not select a device for all selected plans"
							});
						}
					}

					var imei = "";
					if (model.BYOD) {
						var selected = model.IMEIs.ElementAtOrDefault(zero)?.Trim();
						if (!selected.IsNull()) {
							imei = selected;
						} else {
							return Ok(new {
								success = false,
								error = "You did not provide an IMEI for each device that you are bringing"
							});
						}
					}

					var options = new List<DeviceOption>();
					if (model.SelectedOptions.Count >= i) {
						var optionIds = model.SelectedOptions[zero].Options.Select(x => x.Id);
						// Even though we are only using Guids below, do this to make sure that ids passed are in the DB
						options = await _context.DeviceOptions.Where(x => optionIds.Contains(x.Id)).ToListAsync();
					}

					_context.Add(new UserPlan() {
						AgreementSigned = false,
						Initiation = pricing.InitiationFee / model.NumberOfLines,
						Monthly = pricing.MonthlyCost / model.NumberOfLines,
						PlanTypeId = model.SelectedPlanId,
						PortComplete = false,
						Profile = CurrentProfile,
						Status = PlanStatus.Pending,
						IsPorting = model.Porting,
						// Use MDN if we have it
						UserGivenName = givenName,
						MDN = StaticUtils.NumericStrip(model.MobileNumbers.ElementAtOrDefault(zero)),
						// Only attach the device and user device if this is a wireless + device plan.
						DeviceId = device,
						UserDevice = new UserDevice() {
							IsActive = false,
							Requested = DateTime.UtcNow,
							Profile = CurrentProfile,
							BYOD = model.BYOD,
							// If this is BYOD capture the IMEI
							IMEI = imei,
							// Assign the options
							ChosenOptions = UserDevice.SetOptions(options),
							// TODO assign stocked device
							PurchasedWithAffirm = model.IsAffirmConfirmed
						},
						PortRequest = model.Porting ? new PortRequest() {
							MDN = StaticUtils.NumericStrip(model.MobileNumbers.ElementAtOrDefault(zero)),
							CanBeSubmitted = false,
							Status = PortStatus.Ready
						} : null
					});
				} catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Create");
					return Ok(new {
						success = false,
						error = e.Message
					});
				}
			}

			try {
				await _context.SaveChangesAsync();
				// Go back and reset the planid on the devices
				var devices = _context.Plans.Where(x => x.Status == PlanStatus.Pending).Include(x => x.UserDevice).ForEachAsync(x => x.UserDevice.PlanId = x.Id);
				await _context.SaveChangesAsync();
			} catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Purchase");
				return Ok(new {
					success = false,
					error = e.Message
				});
			}

			// Redirect to payment with return of the ActivatePlans page.
			return Ok(new {
				success = true,
				redirectUrl = Url.Action(nameof(PurchaseController.ActionNavigator), PurchaseController.Name,
					new { returnUrl = Url.Action(nameof(Controllers.PlanController.ActivatePlans), Controllers.PlanController.Name) }),
				error = ""
			});
		}

		[HttpPost("setplanpurchases")]
		public async Task<IActionResult> SetPlanPurchases(PlanPurchaseFirstStepModel model) {
			if (!_other.DeviceOrderingEnabled) {
				return Forbid();
			}
			var planType = await _context.PlanTypes
				.Include(x => x.LinePricing1)
				.Include(x => x.LinePricing2)
				.Include(x => x.LinePricing3)
				.Include(x => x.LinePricing4)
				.FirstAsync(x => x.Id == model.SelectedPlanId);

			// Validate that we have enough IMEI's for the lines on BYOD
			if (model.Porting && model.MobileNumbers.Count(x => !string.IsNullOrWhiteSpace(x)) != model.NumberOfLines) {
				return Ok(new {
					success = false,
					redirectUrl = "",
					error = "You did not provide a mobile number to port for each line you are purchasing."
				});
			}

			var pricing = PlanService.GetLinePricing(model.NumberOfLines, planType);

			for (int i = 1; i <= model.NumberOfLines; i++) {
				try {
					var zero = i - 1;

					var givenName = model.LineHolderNames.ElementAtOrDefault(zero);

					_context.Add(new UserPlan() {
						AgreementSigned = false,
						MDN = StaticUtils.NumericStrip(model.MobileNumbers.ElementAtOrDefault(zero)),
						Initiation = pricing.InitiationFee / model.NumberOfLines,
						Monthly = pricing.MonthlyCost / model.NumberOfLines,
						PlanTypeId = model.SelectedPlanId,
						IsPorting = model.Porting,
						PortComplete = false,
						Profile = CurrentProfile,
						Status = PlanStatus.Pending,
						// Use MDN if we have it
						UserGivenName = givenName,
						PortRequest = model.Porting ? new PortRequest() {
							MDN = StaticUtils.NumericStrip(model.MobileNumbers.ElementAtOrDefault(zero)),
							CanBeSubmitted = false,
							Status = PortStatus.Ready
						} : null
					});
				}
				catch (Exception e) {
					ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Create");
					return Ok(new {
						success = false,
						error = e.Message
					});
				}
			}

			try {
				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Purchase");
				return Ok(new {
					success = false,
					error = e.Message
				});
			}

			return Ok(new {
				success = true,
				redirectUrl = Url.Action(nameof(PurchaseController.ActionNavigator), PurchaseController.Name,
					new { returnUrl = Url.Action(nameof(Controllers.PlanController.ActivatePlans), Controllers.PlanController.Name) }),
				error = ""
			});
		}

		[HttpPost("choosedevices")]
		public async Task<IActionResult> ChooseDevicePurchases(ChooseDevicePurchasesModel model) {
			if (!_other.DeviceOrderingEnabled) {
				return Forbid();
			}

			try {
				await SetDevices(model);
				await SetAccessories(model.Accessories);
				await _context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(_other.SentryDSN, e, HttpContext, "Plan-Purchase");
				return Ok(new {
					success = false,
					error = e.Message
				});
			}

			// Redirect to payment with return of the ActivatePlans page.
			return Ok(new {
				success = true,
				redirectUrl = Url.Action(nameof(PurchaseController.ActionNavigator), PurchaseController.Name,
					new { returnUrl = Url.Action(nameof(Controllers.PlanController.ActivatePlans), Controllers.PlanController.Name) }),
				error = ""
			});
		}
	}
}
