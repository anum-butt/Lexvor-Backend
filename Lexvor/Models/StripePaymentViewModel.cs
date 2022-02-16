using Lexvor.API.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models {
	public class StripePaymentViewModel {

		public Profile Profile { get; set; }
		public string Email { get; set; }
		public long Amount { get; set; }
	}
}
