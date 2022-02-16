using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AdminViewModels {
	public class UserUpgradePlanDetailsViewModel {

		public string UserName { get; set; }
		public UserPlan UserPlan { get; set; }
		public string TelispirePlan { get; set; }

		public UsageDay TelispireUsage { get; set; }

		public DateTime NextBillingDate { get; set; }
	}
}
