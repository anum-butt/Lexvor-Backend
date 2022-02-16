using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {
	

	public class EpicPaySubscriptionSuspendResponse {
		public SubSuspendResult result { get; set; }
	}

	public class EpicPaySubscriptionDeleteResponse {
		public SubDeleteResult result { get; set; }
	}

	public class SubDeleteResult {
		public EpicPayDeleted status { get; set; }
	}

	public class SubSuspendResult {
		public EpicPaySubSuspend subscription { get; set; }
	}

	public class EpicPayDeleted {
		public string response_code { get; set; }
	}

	public class EpicPaySubSuspend {
		public string id { get; set; }
		public string status { get; set; }
	}
}
