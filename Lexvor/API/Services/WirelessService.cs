using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Telispire;
using Lexvor.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.API.Services {
	public static class WirelessService {
		public static double CalculatePlanCostToDate(UsageDay usage, bool didPort) {
			double verizonMRCPerLine = 2.0;
			double telispireMRCPerLine = 3.0;
			double usageControlPlatform = 0.15;
			double FSUFFee = 0.02;
			double nonVerizonPort = -0.5;
			double perMin = 0.011;
			double perSMS = 0.011;

			double totalCost = verizonMRCPerLine + telispireMRCPerLine + usageControlPlatform + FSUFFee + (didPort ? nonVerizonPort : 0);

			// Now for usage
			double totalMin = usage.Minutes * perMin;
			double totalSMS = usage.SMS * perSMS;

			// Determine data tier
			double teir1Ceiling = 500 * 1024;
			double teir2Ceiling = 1024 * 1024;
			double teir3Ceiling = 4096 * 1024;
			double teir4Ceiling = 5120 * 1024;
			double teir1CostPerKB = 0.0000077148;
			double teir2CostPerKB = 0.0000073242;
			double teir3CostPerKB = 0.0000058496;
			double teir4CostPerKB = 0.0000053613;
			double teir5CostPerKB = 0.0000051270;

			double totalData = 0.0;

			if (usage.KBData < teir1Ceiling) {
				totalData = usage.KBData * teir1CostPerKB;
			} else if (usage.KBData < teir2Ceiling) {
				totalData = usage.KBData * teir2CostPerKB;
			} else if (usage.KBData < teir3Ceiling) {
				totalData = usage.KBData * teir3CostPerKB;
			} else if (usage.KBData < teir4Ceiling) {
				totalData = usage.KBData * teir4CostPerKB;
			} else {
				totalData = usage.KBData * teir5CostPerKB;
			}

			totalCost += totalMin + totalSMS + totalData;

			return totalCost;
		}
		
		/// <summary>
		/// Defaults to 3 days of usage iif no start date provided.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="context"></param>
		/// <param name="MDN"></param>
		/// <param name="startDate"></param>
		/// <returns></returns>
		public static async Task UpdateUsageDataFromWirelessProvider(TelispireApi service, ApplicationDbContext context, string MDN, DateTime? startDate = null) {
			var current = startDate.HasValue ? startDate.Value : DateTime.UtcNow.AddDays(-3);
			while (current <= DateTime.UtcNow.Date) {
				var status = await service.GetUsageForDay(MDN, current);

				var todays = await context.UsageDays.FirstOrDefaultAsync(x => x.MDN == MDN && x.Date == current.Date);
				if (todays != null) {
					todays.KBData = status.KBData;
					todays.Minutes = status.Minutes;
					todays.SMS = status.SMS;
				} else {
					context.Add(status);
				}

				await context.SaveChangesAsync();

				current = current.AddDays(1);
			}
		}

		public static async Task UpdatePortStatus(ApplicationDbContext context, TelispireApi api, UserPlan plan) {
			var (status, description) = await api.CheckPortStatus(plan.PortRequest.MDN);

			plan.PortRequest.StatusDescription = description;
			plan.PortRequest.Status = !status ? PortStatus.Error : PortStatus.Completed;
			await context.SaveChangesAsync();
		}
	}
}
