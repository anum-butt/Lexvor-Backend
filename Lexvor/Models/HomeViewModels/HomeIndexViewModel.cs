using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lexvor.API.Objects;

namespace Lexvor.Models.HomeViewModels {
    public class HomeIndexViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }

        public List<UserPlan> Plans { get; set; }
		public bool UserHasRequest { get; set; }
	}

    public class SpecialUpgradeViewModel {
		public Profile Profile { get; set; }
		public ApplicationUser User { get; set; }
		public string StripeKey { get; set; }
		public Device UpgradeDevice { get; set; }
		public List<UserPlan> UserPlans { get; set; }
		public Guid PlanId { get; set; }
		public string Color { get; set; }
	}

    public class GenericPaymentViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
        public string StripeKey { get; set; }
    }
}
