using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {
	public class EpicPayBankAccount {
		public string account_type { get; set; }
		public string routing_number { get; set; }
		public string account_number { get; set; }
		public string account_holder_name { get; set; }
	}
}
