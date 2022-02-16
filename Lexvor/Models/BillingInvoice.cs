using Lexvor.API.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models {
	public class BillingInvoice {
		
	   public BillingInvoiceInfo BillingInvoiceInfos { get; set; }
       
		public Profile Profile { get; set; }
	}

	public class BillingInvoiceInfo {
		public string IMEI { get; set; }

		public string AccountNumber { get; set; }
		public string ActiveDevice { get; set; }
	    public int MRC { get; set; }
	    public string Address { get; set; }

	}
}
