using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Services {
	public static class UsageService {
		public static async Task<UsageDay> GetUsageAggregateForCycle(ApplicationDbContext context, string MDN, DateTime cycleStart) {
			var last = cycleStart.GetLast();
			var usages = await context.UsageDays.Where(x => x.MDN == MDN && x.Date >= cycleStart && x.Date <= last).ToListAsync();
			if (!usages.Any()) return new UsageDay() { Date = DateTime.UtcNow };
			return new UsageDay {
				Date = usages.Max(x => x.Date),
				Minutes = usages.Sum(x => x.Minutes),
				SMS = usages.Sum(x => x.SMS),
				KBData = usages.Sum(x => x.KBData)
			};
		}
		public static async Task<string> CalculateThrottleForLine(ApplicationDbContext context, PlanType planType, string MDN, DateTime cycleStart) {
			var last = cycleStart.GetLast();
			var usages = await context.UsageDays.Where(x => x.MDN == MDN && x.Date >= cycleStart && x.Date <= last).ToListAsync();
			var totalData = usages.Sum(x => x.KBData);
			return totalData > planType.FirstThrottle ? totalData > planType.SecondThrottle ? "2G Speeds" : "3G Speeds" : "No Throttling";
		}
		public static async Task<List<UsageDay>> GetUsageByMDN(ApplicationDbContext context, string MDN) {
			return await context.UsageDays.Where(x => x.MDN == MDN).OrderByDescending(dt => dt.Date).ToListAsync();
		}
	}
}
