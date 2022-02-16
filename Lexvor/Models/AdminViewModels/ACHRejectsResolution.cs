using Lexvor.API.Objects.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models.AdminViewModels {
	public class ACHRejectsResolution {

		public Guid ProfileId { get; set; }
		public int Amount { get; set; }
	    public ChargeType ChargeType { get; set; }
		public string UserEmail { get; set; }
		public string MaskedAccount { get; set; }
        public double? Balance { get; set; }
		public DateTime LastBalanceCheck { get; set; }
		public DateTime ?ChargeDate { get; set; }
		public Charge Charge { get; set; }

		public Guid PayAccountId { get; set; }
	}
}
