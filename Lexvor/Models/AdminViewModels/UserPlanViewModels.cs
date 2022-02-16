using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AdminViewModels {
	public class UserPlanDetailsViewModel {
		public UserPlan UserPlan { get; set; }
		public bool ConfirmedPayAccount { get; set; }
		public UsageDay UsageDay { get; set; }
		public UserOrder Order { get; set; }
		public string ErrorMessage { get; set; }

	}
}
