using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {
	public class ACHRejectedResponse {
		public ACHResult result { get; set; }
		public Status status { get; set; }

	}

	public class ACHResult {
		public Filters filters { get; set; }
		public int record_count { get; set; }
		public List<ACHRejects> data { get; set; }
	}
	public class ACHRejects {
		public DateTime ? reported_date { get; set; }
		public DateTime? effective_date { get; set; }
		public DateTime? settled_date { get; set; }

		public string trxn_id { get; set; }

		public string account_no { get; set; }

		public decimal Amount { get; set; }
		public string accountholder { get; set; }

		public string reason_code { get; set; }
		public string addenda { get; set; }
		public string entry_class { get; set; }
		public string entry_desc { get; set; }

		public string routing_number { get; set; }

		public string reason_text { get; set; }
	}

    
}
