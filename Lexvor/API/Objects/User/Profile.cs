using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.Extensions;
using Lexvor.Models;
using Microsoft.AspNetCore.Http;

namespace Lexvor.API.Objects {
    [Dapper.Table("Profiles")]
    public class Profile {
        [Dapper.Key]
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("AccountId")]
        public ApplicationUser Account { get; set; }
        public string AccountId { get; set; }

        public CustomerType CustomerType { get; set; }
        public ProfileType ProfileType { get; set; }
		public ProfileStatus ProfileStatus { get; set; }

		/// <summary>
		/// ID to map this customer in external systems. For EpicPay this is walletId.
		/// </summary>
        public string ExternalCustomerId { get; set; }
		/// <summary>
		/// ID to map this customer in external wireless provisioning systems. For Telispire this is AccountNumber.
		/// </summary>
		public string ExternalWirelessCustomerId { get; set; }

        // Demographic Info
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        public string LastName { get; set; }

        [DisplayName("Primary Phone")]
        public string Phone { get; set; }

		public bool? PhoneVerified { get; set; }

		public string PhoneVerificationCode { get; set; }

		[Editable(false)]
	    [NotMapped]
	    public bool InProbation => DateJoined.AddMonths(12) > DateTime.Now;

		[Editable(false)]
        [NotMapped]
		[DisplayName("Full Name")]
        public string FullName {
            get {
                if (string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(FirstName)) {
                    return "Lexvor User";
                }
                else {
                    return $"{FirstName} {LastName}";
                }
            }
        }

        // Account Info
        public IDVerifyStatus IDVerifyStatus { get; set; }
        /// <summary>
        /// Used to display to the user what is missing or wrong with their documents
        /// </summary>
        public string IDVerifyStatusDescription { get; set; }

        [ForeignKey("BillingAddressId")]
        public Address BillingAddress { get; set; }
        public Guid? BillingAddressId { get; set; }

        [ForeignKey("ShippingAddressId")]
        public Address ShippingAddress { get; set; }
        public Guid? ShippingAddressId { get; set; }

        public DateTime LastModified { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DateJoined { get; set; }
        
		/// <summary>
		/// Force reaffirmation of the agreement
		/// </summary>
	    public bool ForceReaffirmation { get; set; }

        public List<IdentityDocument> IdentityDocuments { get; set; }

		/// <summary>
		/// This value is set when the user purchases their FIRST plan ever.
		/// </summary>
        public DateTime? BillingCycleStart { get; set; }

        /// <summary>
        /// If this is the last day of the month then all future days will be the last day of the month. Marked with 99.
        /// This is set on FIRST subscription creation. Returns -1 if no day is set.
        /// </summary>
        [Editable(false)]
        [NotMapped]
        public int BillingDay => 1;


        [Editable(false)]
        [NotMapped]
        public DateTime NextBillDate => DateTime.Now.AddMonths(1).GetFirst();

		[NotMapped]
		public bool HasAnyPlans { get; set; }

		public string Referral { get; set; }

		public bool? IsArchive { get; set; }
    }
}
