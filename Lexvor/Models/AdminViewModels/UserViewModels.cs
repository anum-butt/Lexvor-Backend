using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Dapper;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Microsoft.AspNetCore.Http;

namespace Lexvor.Models.AdminViewModels
{
    public class UserEditViewModel
    {
        public Profile Profile { get; set; }
        public SelectListModel SelectListModel { get; set; }
		
	    [DataType(DataType.Upload)]
	    public IFormFile ImageUpload1 { get; set; }
		
	    [DataType(DataType.Upload)]
	    public IFormFile ImageUpload2 { get; set; }
		
	    [DataType(DataType.Upload)]
	    public IFormFile ImageUpload3 { get; set; }
	}
	
    public class ProfileAccount {
	    public Profile Profile { get; set; }
	    public ApplicationUser User { get; set; }
    }
    public class UserProfileData {
	    public Guid Id { get; set; }
	    public string FirstName { get; set; }
	    public string LastName { get; set; }
	    public string Email { get; set; }
	    public IDVerifyStatus IDVerifyStatus { get; set; }
	    public DateTime DateJoined { get; set; }
	    public bool EmailConfirmed { get; set; }
	    public int ActivePlanCount { get; set; }
		public bool? IsArchive { get; set; }
    }

	public class UserListViewModel {
		public List<UserProfileData> Users { get; set; }
    }

    public class ChangePlanTypeViewModel {
        public UserPlan UserPlan { get; set; }
        public Guid NewPlanTypeId { get; set; }
    }

    public class AssignDeviceViewModel {
		public UserPlan UserPlan { get; set; }
		public Guid DeviceId { get; set; }
		public string Carrier { get; set; }
		public string Color { get; set; }
		public string IMEI { get; set; }

		[DisplayName("If user is BYOD, all we need is the IMEI")]
		public bool BYOD { get; set; }
		public DateTime Requested { get; set; }
	}

	public class ChangeBillDateViewModel {
		public UserPlan UserPlan { get; set; }
		public Profile Profile { get; set; }
		public DateTime NewBillDate { get; set; }
	}

	public class UserDetailsViewModel {
		public List<UserPlan> Plans { get; set; }
	    public List<DeviceIntake> TradeIns { get; set; }
		public Profile Profile { get; set; }
		public ApplicationUser User { get; set; }
		public int ReferralPosition { get; set; }
		public List<BankAccount> PayAccounts { get; set; }
		public bool ShowArchivedPlans { get; set; }
	}

	public class UpgradeViewModel {
		public UserPlan UserPlan { get; set; }
		public Guid UpgradePlanId { get; set; }
        /// <summary>
        /// Amount in cents
        /// </summary>
		public int Monthly { get; set; }
		public bool DisableAds { get; set; }
	}

	public class VerifyViewModel {
		public Profile Profile { get; set; }
		public Identity VouchedIdentity { get; set; }
		public Identity PlaidIdentity { get; set; }
		public Identity TwilioIdentity { get; set; }
		public string Notes { get; set; }
	}
}
