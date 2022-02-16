using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lexvor.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Your Email Address")]
        public string Email { get; set; }

	    [Required]
	    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
	    [DataType(DataType.Password)]
	    [Display(Name = "Password")]
	    public string Password { get; set; }

	    [Required]
	    [Display(Name = "Your Current Phone Number")]
	    public string Phone { get; set; }

		//[DataType(DataType.Password)]
		//[Display(Name = "Confirm password")]
		//[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		//public string ConfirmPassword { get; set; }

	    [Display(Name = "Referral Code (optional)")]
	    public string ReferralCode { get; set; }

	    [Display(Name = "Invite Code (required)")]
	    public string InviteCode { get; set; }
	}

	public class NetworkRegisterViewModel {
		[Required]
		[EmailAddress]
		[Display(Name = "Your Email Address")]
		public string Email { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Required]
		[Display(Name = "Your Current Phone Number")]
		public string Phone { get; set; }

		//[DataType(DataType.Password)]
		//[Display(Name = "Confirm password")]
		//[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		//public string ConfirmPassword { get; set; }

		[Display(Name = "Referral Code (optional)")]
		public string ReferralCode { get; set; }

		[Display(Name = "Number of Accounts")]
		public string NumberOfLines { get; set; }
	}

	public class NetworkDetailsViewModel {
		public int NumberOfLines { get; set; }
		public List<string> PhoneNumbersToPort { get; set; }
	}

	public class ReferredRegisterViewModel {
		[Required]
		[EmailAddress]
		[Display(Name = "Your Email Address")]
		public string Email { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Required]
		[Display(Name = "Your Current Phone Number")]
		public string Phone { get; set; }

		[Required]
		[Display(Name = "Your Zip Code")]
		public string PostalCode { get; set; }

		public string ReferralCode { get; set; }
	}
}
