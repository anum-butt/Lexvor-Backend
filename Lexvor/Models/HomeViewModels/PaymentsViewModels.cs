using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using System.ComponentModel.DataAnnotations;

namespace Lexvor.Models.HomeViewModels {
    public class NetworkPurchaseViewModel {
        public int NumberOfLines { get; set; }
        public int PurchaseAmount { get; set; }
        public string StripeKey { get; set; }
        public ApplicationUser User { get; set; }
    }

    public class BillingProfileViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
		public string GoogleMapsKey { get; set; }
    }

    public class BankInformationViewModel {
        [DisplayName("Account Number")]
        [Required(ErrorMessage = "Account Number is required")]
        [RegularExpression(@"^[0-9]{2,20}$", ErrorMessage = "Please enter between 2 and 20 digits for a valid bank account number")]
        public string BankAccountNum { get; set; }

        [DisplayName("Routing Number")]
        [Required(ErrorMessage = "Routing Number is required")]
        public string BankAccountRoutingNum { get; set; }

        [DisplayName("First Name on Account")]
        [Required(ErrorMessage = "First Name on the account is required")]
        public string FirstNameOnAccount { get; set; }

        [DisplayName("Last Name on Account")]
        [Required(ErrorMessage = "Last Name on the account is required")]
        public string LastNameOnAccount { get; set; }

        [DisplayName("Name of Bank")]
        [Required(ErrorMessage = "Name of the Bank is required")]
        public string BankName { get; set; }
        public string PublicToken { get; set; }
        public string PlaidAccountId { get; set; }


        public string PlaidEnv { get; set; }
        public string PlaidPublicKey { get; set; }

        public Profile Profile { get; set; }
    }

    public class AffirmCheckoutViewModel {
	    public ApplicationUser User { get; set; }
	    public Profile Profile { get; set; }
	    public List<UserPlan> Plans { get; set; }
	    public string Token { get; set; }
	    public string OrderId { get; set; }
	    public string AffirmPublicKey { get; set; }
	    public string AffirmJsUrl { get; set; }
    }
}
