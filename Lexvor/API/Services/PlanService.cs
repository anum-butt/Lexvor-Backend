using Lexvor.API.Objects;
using Lexvor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lexvor.API.Objects.User;
using Lexvor.API.Payment;
using Microsoft.EntityFrameworkCore;
using Lexvor.API.Objects.Enums;
using Lexvor.Extensions;

namespace Lexvor.API.Services {
	public static class PlanService {
		public static async Task<List<UserPlan>> GetReturnRequests(ApplicationDbContext db, Profile userProfile = null) {
			var userDevices = await db.UserDevices.Where(u => u.ReturnRequested.HasValue).Select(u => u.Id).ToListAsync();

			var plans = await db.Plans.Include(p => p.UserDevice).Include(p => p.PlanType).Include(p => p.Profile)
				.Include(p => p.Device)
				.Where(p => (userProfile == null || p.ProfileId == userProfile.Id) && p.UserDeviceId.HasValue && userDevices.Contains(p.UserDeviceId.Value) && !p.UserDevice.ReturnApproved.HasValue)
				.OrderBy(p => p.UserDevice.ReturnRequested)
				.ToListAsync();

			return plans;
		}

		public static async Task ApproveReturn(ApplicationDbContext db, UserDevice device) {
			device.ReturnApproved = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public static LinePricing GetLinePricing(int planCount, PlanType planType) {
			if (planCount == 1 && planType.LinePricing1 != null) {
				return planType.LinePricing1;
			}
			if (planCount == 2 && planType.LinePricing2 != null) {
				return planType.LinePricing2;
			}
			if (planCount == 3 && planType.LinePricing3 != null) {
				return planType.LinePricing3;
			}
			if (planCount >= 4 && planType.LinePricing4 != null) {
				return planType.LinePricing4;
			}

			return null;
		}

		/// <summary>
		/// Returns how to call Epic (add/update) and the SubscriptionId of the existing Sub or an Error 
		/// </summary>
		/// <param name="existingPlans"></param>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static async Task<GetSubscriptionMethodReturn> GetSubscriptionMethod(List<UserPlan> existingPlans, Guid profileId) {
			// Create or Edit subscription. Get the existing subs to check if this is a new customer or not.
			var existingSubId = existingPlans.Select(x => x.ExternalSubscriptionId).Distinct().Where(x => !x.IsNull()).ToList();
			// If there is more than one subid, there is a serious problem.
			if (existingSubId.Count() > 1) {
				return new GetSubscriptionMethodReturn() {
					Error = $"There is more than one subscription id for customer {profileId}",
					// Return sub id anyway because we are choosing in the caller to continue even with this error.
					SubscriptionId = existingSubId.First(),
					IsSuccess = false
				};
			}

			return new GetSubscriptionMethodReturn() {
				Method = existingSubId.Any() ? PlanSubscriptionMethod.Update : PlanSubscriptionMethod.Create,
				SubscriptionId = existingSubId.FirstOrDefault(),
				IsSuccess = true
			};
		}

		public static async Task<SubscriptionCreateReturn> CreateSubscriptionForPlan(OtherSettings other, Profile profile, string email, int amount, EpicPay epic, BankAccount activePayAccount, PlanSubscriptionMethod method, string subscriptionId, string callbackUrl) {
			var newSubId = "";
			var customerId = "";
			var epicError = "";
			var subStartDate = new DateTime();
			
			// Get new monthly cost for all current plans
			if (method == PlanSubscriptionMethod.Update) {
				(customerId, newSubId, subStartDate, epicError) = await epic.EditSubscription(subscriptionId, email, profile, DateTime.UtcNow.GetNextFirst(), amount, activePayAccount, other.EncryptionKey, callbackUrl);
			} else {
				(customerId, newSubId, subStartDate, epicError) = await epic.CreateSubscription(email, profile, DateTime.UtcNow.GetNextFirst(), amount, activePayAccount, other.EncryptionKey, callbackUrl);
			}

			// Short circuit if we had an EpicPay error
			if (!epicError.IsNull()) {
				return new SubscriptionCreateReturn() {
					Error = $"There was an issue when charging your bank account. {epicError}",
					IsSuccess = false
				};
			}

			// If sub id returned is not the same as the one we have, then we have a problem.
			if (!subscriptionId.IsNull() && newSubId != subscriptionId) {
				return new SubscriptionCreateReturn() {
					Error = $"The subscription id returned did not match the one we have for customer {profile.Id}",
					IsSuccess = false
				};
			}
			//if (profile.NextBillDate != subStartDate) {
			//	return new SubscriptionCreateReturn() {
			//		Error = $"The subscription start date changed for customer {profile.Id}",
			//		IsSuccess = false
			//	};
			//}

			return new SubscriptionCreateReturn() {
				IsSuccess = true,
				SubscriptionId = newSubId,
				CustomerId = customerId,
				StartDate = subStartDate
			};
		}

		public static void UpdatePlansAfterSubscriptionCreate(SubscriptionCreateReturn createReturn, Profile profile, List<UserPlan> plans) {
			if (profile.IDVerifyStatus == IDVerifyStatus.Verified) {
				plans.ForEach(x => {
					x.Status = PlanStatus.Active;
					// Set the sub ID from Epic
					x.ExternalSubscriptionId = createReturn.SubscriptionId;
					x.ExternalSubscriptionStartDate = createReturn.StartDate;
				});
			} else {
				plans.ForEach(x => {
					x.Status = PlanStatus.Paid;
					x.ExternalSubscriptionId = createReturn.SubscriptionId;
					x.ExternalSubscriptionStartDate = createReturn.StartDate;
				});
			}
			// Update the profile with ID from EpicPay for Edit operations later. This is EpicPay's WalletId.
			profile.ExternalCustomerId = createReturn.CustomerId;
		}

		public static PlanStatus[] ActivePendingStatuses => new[] { PlanStatus.Active, PlanStatus.Paid, PlanStatus.DevicePending, PlanStatus.PaymentHold, PlanStatus.OnHold, PlanStatus.Pending };
		public static PlanStatus[] ActiveStatuses => new[] { PlanStatus.Active, PlanStatus.Paid, PlanStatus.DevicePending };

		public enum PlanSubscriptionMethod {
			Update,
			Create
		}
	}

	public class GetSubscriptionMethodReturn {
		public string SubscriptionId { get; set; }
		public PlanService.PlanSubscriptionMethod Method { get; set; }
		public string Error { get; set; }
		public bool IsSuccess { get; set; }
	}

	public class SubscriptionCreateReturn {
		public string SubscriptionId { get; set; }
		public string CustomerId { get; set; }
		public DateTime StartDate { get; set; }
		public string Error { get; set; }
		public bool IsSuccess { get; set; }
	}
}
