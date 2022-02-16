using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lexvor.API.Payment {
	public class AffirmAuthorizeResponse {
		public string id { get; set; }
		public DateTime created { get; set; }
		public string currency { get; set; }
		public int amount { get; set; }
		public int auth_hold { get; set; }
		public string order_id { get; set; }
	}

	public class Event {
		public DateTime created { get; set; }
		public string currency { get; set; }
		public string id { get; set; }
		public string transaction_id { get; set; }
		public string type { get; set; }
	}

	public class Details {
		public string order_id { get; set; }
		public int shipping_amount { get; set; }
		public int tax_amount { get; set; }
	}

}
