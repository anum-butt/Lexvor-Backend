using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lexvor.API.Objects
{
    public class DiscountCode {
        public Guid Id { get; set; }
		[Required]
        public string Code { get; set; }

        /// <summary>
        /// Amount in cents
        /// </summary>
        [Required]
        [DisplayName("New Monthly Cost (in cents)")]
        public int NewMonthlyCost { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
	    [Required]
        [DisplayName("New Initiation Fee (in cents)")]
		public int NewInitiationFee { get; set; }

	    [Required]
		[DisplayName("Is this a one time use discount code for everyone?")]
        public bool OneTimeUse { get; set; }

	    [Required]
		[DisplayName("Is this a one time use discount code per user?")]
        public bool OneTimeUsePerUser { get; set; }

	    [Required]
		[DisplayName("Date that the discount code will start to work")]
        public DateTime StartDate { get; set; }

	    [Required]
		[DisplayName("Expiration date for the discount code")]
        public DateTime EndDate { get; set; }
        
        [ForeignKey("PlanTypeId")]
        public PlanType PlanType { get; set; }
	    [Required]
		public Guid PlanTypeId { get; set; }
    }
}
