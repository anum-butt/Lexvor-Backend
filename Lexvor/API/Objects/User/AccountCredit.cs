using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.Models;

namespace Lexvor.API.Objects.User
{
    public class AccountCredit
    {
        public Guid Id { get; set; }

        [ForeignKey("ProfileId")]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }

        /// <summary>
        ///  Amount in cents
        /// </summary>
        [DisplayName("Amount (in cents)")]
        public int Amount { get; set; }

        [DisplayName("Number of times you want to apply the credit")]
        public int TimesToApply { get; set; }

        [DisplayName("Why does this user get this credit?")]
		public string Reason { get; set; }

	    [DisplayName("Can this credit be applied to Initiation?")]
		public bool ApplicableToInitiation { get; set; }

	    [DisplayName("Can this credit be applied to Monthly Fee?")]
		public bool ApplicableToMonthlyFee { get; set; }

		public int AppliedAmount { get; set; }

		public DateTime? LastApplied { get; set; }
		public int TimesApplied { get; set; }
        public DateTime Created { get; set; }

        // Object that this account credit is associated with (if necessary)
        public Guid? ObjectId { get; set; }
        public string ObjectType { get; set; }
    }
}
