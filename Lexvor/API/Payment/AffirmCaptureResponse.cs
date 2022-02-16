using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {
	public class AffirmCaptureResponse {
		
		public int fee { get; set; } 
		public DateTime created { get; set; } 
		public string order_id { get; set; } 
		public string currency { get; set; } 
		public int amount { get; set; } 
		public string type { get; set; } 
		public string id { get; set; } 
		public string transaction_id { get; set; } 
	}
}
