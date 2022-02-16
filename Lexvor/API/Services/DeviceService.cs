using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Lexvor.API.Services
{
    public static class DeviceService
    {
		/// <summary>
		/// Will assign the device and send the admin email for new device request. This is only used for upgrade/replacement requests
		/// </summary>
		/// <param name="db"></param>
		/// <param name="email"></param>
		/// <param name="user"></param>
		/// <param name="userPlan"></param>
		/// <param name="device"></param>
		/// <param name="carrier"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public static async Task AssignDeviceToUserPlan(ApplicationDbContext db, IEmailSender email, AssignContext context, bool sendAdminEmail = true) {
			// If user is not BYOD then assign a device. If they are then we don't know what device they have.
			if (!context.BYOD) {
				context.UserPlan.DeviceId = context.Device.Id;
			}
			// only assign a device for the plans where user completed the purchase or payment is successful

			var plans = db.Plans.Where(p => p.ProfileId == context.UserPlan.ProfileId && PlanService.ActiveStatuses.Contains(p.Status)).ToList();

			foreach(var plan in plans){
				// Assign the user device
		        var stockedDevice=db.StockedDevice.Include(d=>d.Device).Where(x => x.Available == true && x.DeviceId==plan.DeviceId).FirstOrDefault();
				var userDevice = new UserDevice() {
					Requested = context.Requested == DateTime.MinValue ? DateTime.UtcNow : context.Requested,
					PlanId = plan.Id,
					IMEI = stockedDevice.IMEI
				};
				db.UserDevices.Add(userDevice);
				await db.SaveChangesAsync();
				stockedDevice.Available = false;
				plan.UserDeviceId = userDevice.Id;
				db.Update(plan);
				await db.SaveChangesAsync();

				if (sendAdminEmail) {
					await EmailService.SendNewDeviceRequestEmail(email, "customerservice@lexvor.com", context.User.Email, stockedDevice.Device.Name, context.Carrier);
				}
			}
		    
				
			// If the requested date is set then this was called from an admin page. 
			// Set the shipped date accordingly. Assuming that the customer has the phone already.
			//if (context.Requested != DateTime.MinValue) {
			//	userDevice.Shipped = context.Requested;
			//}

		   

			// Set the more recent request
			

			
		}

		public static async Task<DateTime> GetUpgradeDateForDevice(UserDevice device, PlanType planType) {
			if (!device.Requested.HasValue) {
				return DateTime.MinValue;
			}
			return device.Requested.Value.AddMonths(planType.TermLength);
		}

		public static async Task PopulateDeviceOptions(List<UserDevice> devices, ApplicationDbContext context) {
			foreach (var userDevice in devices) {
				userDevice.Options = await userDevice.GetOptions(context);
			}
		}
    }

	public class AssignContext {
		public ApplicationUser User { get; set; }
		public UserPlan UserPlan { get; set; }
		public Device Device { get; set; }
		public string Carrier { get; set; }
		public string Color { get; set; }
		public DateTime Requested { get; set; }
		public bool BYOD { get; set; }
		public string IMEI { get; set; }
	}
}
