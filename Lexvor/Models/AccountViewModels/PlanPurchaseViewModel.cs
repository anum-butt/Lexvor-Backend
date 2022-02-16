using System;
using System.Collections.Generic;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models.AccountViewModels
{
    public class PlanPurchaseViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }

        public List<PlanData> Plans{ get; set; }
    }

    public class PlanPurchaseConfirmViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
        public string StripeKey { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
        public int CreditAmount { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
        public int StripeCreditAmount { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
        public int SubTotal { get; set; }

        public DiscountCode Promo { get; set; }
        public PlanData Plan { get; set; }

	    public bool CustomerHasSource { get; set; }

		// Purchase context
		public bool DisableAds { get; set; }
		public string PromoCode { get; set; }
		public List<Guid> SelectedAddOns { get; set; }
		public Guid PlanTypeId { get; set; }
	    public int GroupPlanCount { get; set; }
	}

    public class PlanData {
        /// <summary>
        /// Amount in cents
        /// </summary>
        public int OriginalInitiationFee { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
        public int OriginalMonthlyFee { get; set; }

        public Guid PlanTypeId { get; set; }
        public string PlanDetails { get; set; }
        public string PlanName { get; set; }
	    public int GroupPlanCount { get; set; }
    }

    public class ConnectAccountViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
        public string PlaidEnv { get; set; }
        public string PlaidPublicKey { get; set; }

        public UserPlan UserPlan { get; set; }
        public DiscountCode AppliedPromo { get; set; }
        public string PromoCode { get; set; }

    }

    public class ACHAuthorizationModel {
        public Profile Profile { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ApplicationUser User { get; set; }
        public List<UserPlan> UserPlans { get; set; }
        public string ReturnUrl { get; set; }
		/// <summary>
		/// Amount in cents
		/// </summary>
		public int TotalMonthly { get; set; }
		/// <summary>
		/// Amount in cents
		/// </summary>
		public int TotalInitiation { get; set; }

		public bool UsingAffirm { get; set; }
		public int TotalDeviceCost { get; set; }
    }
}
