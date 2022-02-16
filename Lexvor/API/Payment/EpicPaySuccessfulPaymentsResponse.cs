using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {
	public class EpicPaySuccessfulPaymentsResponse {
		public Result result { get; set; } 
		public Status status { get; set; } 
	}

	public class Filters {
		public string start_date { get; set; }
		public string end_date { get; set; }
	}

	public class SuccessfulPayment {
		public int schedule_id { get; set; }
		public DateTime scheduled_date { get; set; }
		public double amount { get; set; }
		public double secondary_amount { get; set; }
		public bool alert_after_sent { get; set; }
		public bool alert_before_sent { get; set; }
		public string wallet_id { get; set; }
		public string customer_first_name { get; set; }
		public string customer_last_name { get; set; }
		public string accountholder { get; set; }
		public string account_type { get; set; }
		public string account_suffix { get; set; }
		public string account_expiration_date { get; set; }
		public int terminal_id { get; set; }
		public string terminal_name { get; set; }
		public string subscription_id { get; set; }
		public string client_order_id { get; set; }
		public string status { get; set; }
		public string transaction_id { get; set; }
		public DateTime transaction_date { get; set; }
		public string transaction_status { get; set; }
		public string reason_code { get; set; }
		public string response_message { get; set; }
	}

	public class Result {
		public Filters filters { get; set; }
		public int record_count { get; set; }
		public List<SuccessfulPayment> data { get; set; }
	}

	public class Status {
		public string response_code { get; set; }
		public string reason_code { get; set; }
		public string reason_text { get; set; }
	}
}
