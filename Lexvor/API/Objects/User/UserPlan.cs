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
    public class UserPlan
    {
        public Guid Id { get; set; }
        public string UserGivenName { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("ProfileId")]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }

		/// <summary>
		/// If DeviceId is null, then this is either not assigned yet, or the user is BYOD.
		/// Device is the specific device that the user has. Ex. iPhone X with IMEI 1234
		/// </summary>
        [ForeignKey("DeviceId")]
        public Device Device { get; set; }
		/// <summary>
		/// If DeviceId is null, then this is either not assigned yet, or the user is BYOD.
		/// Device is the specific device that the user has. Ex. iPhone X with IMEI 1234
		/// </summary>
        public Guid? DeviceId { get; set; }

		/// <summary>
		/// UpgradeDeviceId should only have a value if the user is eligible for upgrade
		/// and they are currently in the process of upgrading.
		/// </summary>
		[ForeignKey("UpgradeDeviceId")]
		public Device UpgradeDevice { get; set; }
		/// <summary>
		/// UpgradeDeviceId should only have a value if the user is eligible for upgrade
		/// and they are currently in the process of upgrading.
		/// </summary>
		public Guid? UpgradeDeviceId { get; set; }

		/// <summary>
		/// This is the current User Device, upgrade request or not.
		/// UserDevice is the class of device that the user has or picked. Ex. iPhone X
		/// </summary>
		[ForeignKey("UserDeviceId")]
        public UserDevice UserDevice { get; set; }
        public Guid? UserDeviceId { get; set; }

		/// <summary>
		/// External ID from the Payment Processor
		/// </summary>
		public string ExternalSubscriptionId { get; set; }
		public DateTime ExternalSubscriptionStartDate { get; set; }

		// Cached values of the plan, since the numbers can change at any time.
		public int Initiation { get; set; }
	    public int Monthly { get; set; }

        [ForeignKey("PlanTypeId")]
        public PlanType PlanType { get; set; }
        public Guid PlanTypeId { get; set; }

        public PlanStatus Status { get; set; }

        public string PromoApplied { get; set; }
        public bool AgreementSigned { get; set; }

		/// <summary>
		///  Mobile Number
		/// </summary>
		public string MDN { get; set; }
		public bool? MDNPortable { get; set; }
		public string AssignedSIMICC { get; set; }
		public DateTime? SIMShipped { get; set; }
        /// <summary>
        /// External ID for the wireless plan from the MVNO.
        /// </summary>
        [Obsolete("This field is not currently used for anything important. Telispire does not have an ID for the actual wireless line, they use MDN.")]
        public string ExternalWirelessPlanId { get; set; }

        public WirelessStatus WirelessStatus { get; set; }
        
		[Editable(false)]
		[NotMapped]
		public bool PortComplete { get; set; }

		public PortRequest? PortRequest { get; set; }

		public bool IsPorting { get; set; }

		public bool IsWirelessOnly() {
			return Device == null && UserDevice != null && UserDevice.BYOD;
		}

		[NotMapped]
		public string ThrottleLevel { get; set; }
        
		[Editable(false)]
		[NotMapped]
		[DisplayName("Cost")]
		public double MDNCostToDate { get; set; }

		[NotMapped]
		[DisplayName("Revenue Cost Delta")]
		public double RevenueCostDelta { get; set; }

		public string WirelessPlanName { get; set; }

		public string AgreementUrl { get; set; }
	}

    public enum WirelessStatus {
        NoPlan = 0,
        Active = 1,
        Hotlined = 9,
        Deactivated = 99
    }
}
