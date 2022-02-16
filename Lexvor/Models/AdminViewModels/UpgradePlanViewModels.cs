using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AdminViewModels {
	public class UpgradePlanDetailsViewModel {

		public Profile Profile { get; set; }
		public UserPlan UserPlan { get; set; }
		public string TelispirePlan { get; set; }
		public string NewPlan { get; set; }
	}
}
