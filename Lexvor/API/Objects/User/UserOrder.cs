using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lexvor.API.Objects.User;

namespace Lexvor.API.Objects
{
    /// <summary>
    /// The object that ties the device and subscription to the user. A user can have multiple plans.
    /// </summary>
    public class UserOrder
    {
        public Guid Id { get; set; }

		public Guid ProfileId { get; set; }
		[ForeignKey("ProfileId")]
		public Profile Profile { get; set; }

		/// <summary>
		/// User plan that this order is for
		/// </summary>
		public Guid UserPlanId { get; set; }
		[ForeignKey("UserPlanId")]
		public UserPlan UserPlan { get; set; }

		public Guid OrderId { get; set; }

		public DateTime OrderDate { get; set; }

		/// <summary>
		/// Total in cents
		/// </summary>
		public int Total { get; set; }

		// TODO THIS IS SOOOO GROSS!!!!!!

		public virtual UserAccessory Accessory1 { get; set; }

		public virtual UserAccessory Accessory2 { get; set; }

		public virtual UserAccessory Accessory3 { get; set; }
	}
}
