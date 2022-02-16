using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.API.Services {
	public static class ProrateService {
		public static int GetProrateCharge(LinePricing pricing, DateTime cycleStartDate) {
			var today = DateTime.UtcNow;

			if (today.Date != cycleStartDate.Date) {
				// Get the daily charge for this month
				var dailyCharge = pricing.MonthlyCost / DateTime.DaysInMonth(today.Year, today.Month);

				var numOfDaysToProrate = (today.Day - DateTime.DaysInMonth(today.Year, today.Month)) - cycleStartDate.Day;

				var totalProrate = dailyCharge * numOfDaysToProrate;
				return totalProrate;
			}

			return 0;
		}
	}
}
