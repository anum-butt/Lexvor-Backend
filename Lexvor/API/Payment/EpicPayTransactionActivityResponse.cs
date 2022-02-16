using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.Payment {
	public class EpicPayTransactionActivityResponse {
		public Result result { get; set; }
		public Status status { get; set; }
	}

	public class TransactionActivity {
		public string bin { get; set; }
		public string account_suffix { get; set; }
		public string client_trxn_id { get; set; }
		public string client_order_id { get; set; }
		public string client_customer_id { get; set; }
		public string mid { get; set; }
		public DateTime? deposited_date { get; set; }
		public string batch_id { get; set; }
		public string accountholder { get; set; }
		public double amount { get; set; }
		public double secondary_amount { get; set; }
		public string account_number { get; set; }
		public DateTime? settled_date { get; set; }
		public string trxn_type { get; set; }
		public string payment_type { get; set; }
		public DateTime? reported_date { get; set; }
		public string transaction_id { get; set; }
		public DateTime? trxn_utc_date { get; set; }
		public string reason_code { get; set; }
		public string reason_text { get; set; }
	}

	public class Result {
		public Filters filters { get; set; }
		public int record_count { get; set; }
		public List<TransactionActivity> data { get; set; }
	}

	public class Status {
		public string response_code { get; set; }
		public string reason_code { get; set; }
		public string reason_text { get; set; }
	}

	public class Filters {
		public string start_date { get; set; }
		public string end_date { get; set; }
	}
}
