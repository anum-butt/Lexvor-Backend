using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
    public class BankAccount {
        public Guid Id { get; set; }

        [ForeignKey("ProfileId")]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }

        [DisplayName("Account Number")]
        public string AccountNumber { get; set; }

        [DisplayName("Routing Number")]
        public string RoutingNumber { get; set; }

        [DisplayName("Account First Name")]
        public string AccountFirstName { get; set; }

        [DisplayName("Account Last Name")]
        public string AccountLastName { get; set; }
        public string Bank { get; set; }

        [DisplayName("Account Number")]
        public string MaskedAccountNumber { get; set; }

        public string AccountMask => MaskedAccountNumber.Substring(MaskedAccountNumber.Length - 4);

        /// <summary>
        /// Active or not specifies pending entries
        /// </summary>
        public bool Active { get; set; }
        /// <summary>
        /// A Bank account that has had a successful transaction in the past
        /// </summary>
        public bool Confirmed { get; set; }
		/// <summary>
		/// Archived is an entry that was active at one point but has been removed from an account or otherwise superseded.
		/// </summary>
		public bool Archived { get; set; }
		/// <summary>
		/// Plaid ID
		/// </summary>
        public string ExternalReferenceNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }

		public double? LastBalance { get; set; }
		public DateTime LastBalanceCheck { get; set; }
}
}
