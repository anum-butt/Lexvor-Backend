using System;
using System.Collections.Generic;
using System.ComponentModel;
using Dapper;

namespace Lexvor.API.Objects
{
    /// <summary>
    /// Plan Type is used to separate devices into different groups based on how much they pay
    /// </summary>
    public class PlanType
    {
        public Guid Id { get; set; }
		
        [Required]
        public string Name { get; set; }
		
		[Required]
        public string ShortName { get; set; }

        [DisplayName("HTML details to display on the payments page")]
        public string PlanDetails { get; set; }

        /// <summary>
        /// Amount in cents
        /// </summary>
        [DisplayName("Monthly Cost (in cents)")]
        public int MonthlyCost { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
        [DisplayName("Initiation Fee (in cents)")]
        public int InitiationFee { get; set; }

        [DisplayName("Sort order to display the plan on the plans purchase page")]
        public int SortOrder { get; set; }

        public bool Archived { get; set; }
        public DateTime LastModified { get; set; }

        /// <summary>
        /// ID to the mapped object in the subscription software.
        /// </summary>
        [DisplayName("Plan Name for Wireless Provider")]
        public string ExternalId { get; set; }
        
        public bool DisplayOnPublicPages { get; set; }
        
        public List<PlanTypeDevice> Devices { get; set; }

        /// <summary>
        /// Allow the user to select options on devices
        /// </summary>
        [DisplayName("Enable the creation of options for devices on this plan.")]
        public bool EnableDeviceOptions { get; set; }
		
        [DisplayName("Allow the customer to choose devices for this plan.")]
        public bool EnableDeviceOrdering { get; set; }

		[DisplayName("Force customer to choose devices for this plan. No BYOD.")]
		public bool ForceDeviceOrdering { get; set; }

		[DisplayName("Allow customers to purchase accessories on this plan.")]
		public bool AllowAccessoryPurchase { get; set; }

		[DisplayName("Allow Affirm purchases.")]
		public bool AllowAffirmPurchases { get; set; }

		public PlanTypeSpecialFlags Flag { get; set; }

		[DisplayName("The amount of data the user will be throttled at in KB, first level. (3mbps speeds)")]
		public int FirstThrottle { get; set; }

		[DisplayName("The amount of data the user will be throttled at in KB, second level. (256kbps speeds)")]
		public int SecondThrottle { get; set; }

		/// <summary>
		/// Special Pricing for one line
		/// </summary>
		public LinePricing LinePricing1 { get; set; }
		/// <summary>
		/// Special Pricing for two lines
		/// </summary>
		public LinePricing LinePricing2 { get; set; }
		/// <summary>
		/// Special Pricing for three lines
		/// </summary>
		public LinePricing LinePricing3 { get; set; }
		/// <summary>
		/// Special Pricing for four lines
		/// </summary>
		public LinePricing LinePricing4 { get; set; }

		/// <summary>
		/// Term length in months. Used for agreement population and determining upgrade date
		/// </summary>
		[DisplayName("Term Length (in months)")]
		public int TermLength { get; set; } = 24;
		/// <summary>
		/// set whether a Plan allows Stripe Purchasing
		/// </summary>
		public bool AllowStripePurchases { get; set; } = false;
	}

    public class LinePricing {
	    public int Id { get; set; }
		/// <summary>
		/// Number of lines required for purchase to get this price.
		/// </summary>
	    public int RequiredNumOfLines { get; set; }
		[DisplayName("Initiation Fee (in cents)")]
	    public int InitiationFee { get; set; }
	    [DisplayName("Monthly Cost (in cents)")]
	    public int MonthlyCost { get; set; }
    }

    public enum PlanTypeSpecialFlags {
		NoFlag = 0,
	    PrePaid = 1,
		FutureVIP = 2,
		PreOrder = 3,
		FuturePromo = 4
    }
}
