using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acklann.Plaid.Balance;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models {
	public class BillingIndexViewModel {
		public Address CurrentBillingAddress { get; set; }
		public BankAccount CurrentBankAccount { get; set; }
		public IList<Charge> PastCharges { get; set; }
		public Profile Profile { get; set; }
		public DateTime? LastBillingDate { get; internal set; }
		public DateTime NextBillingDate { get; internal set; }
		public GetAccountResponse PlaidAccount { get; set; }

	}
}
