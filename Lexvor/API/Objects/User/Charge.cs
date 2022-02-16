using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
	public class Charge {
		public Guid Id { get; set; }

		[ForeignKey("ProfileId")]
		public Profile Profile { get; set; }

		public Guid ProfileId { get; set; }

		public string InvoiceId { get; set; }

		/// <summary>
		/// Amount in cents. Eg. $10 = 1000
		/// </summary>
		public int Amount { get; set; }

		public string Description { get; set; }

		public DateTime Date { get; set; }
		public bool NeedsAttention { get; set; }

		public ChargeStatus Status { get; set; }

		/// <summary>
		/// Object in our system that this charge applies to
		/// </summary>
		public Guid InternalObjectId { get; set; }

		public ChargeType ChargeType { get; set; }
	
		/// <summary>
		/// Only used for Epic Pay transaction reports where client_order_id is empty
		/// </summary>
		[NotMapped]
		public string FirstName { get; set; }
		
		/// <summary>
		/// Only used for Epic Pay transaction reports where client_order_id is empty
		/// </summary>
		[NotMapped]	
		public string LastName { get; set; }
	}	

	public enum ChargeStatus {
		Pending = 1,
		Charged = 5,
		Cancelled = 99,
        Rejected=0
	}	

	public enum ChargeType {
		Bill=0,
		OneTime=1,
		AnyOther=2

	}
}
