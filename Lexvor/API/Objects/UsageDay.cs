using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
	public class UsageDay {
		public Guid Id { get; set; }
		public string MDN { get; set; }
		public DateTime Date { get; set; }
		public int SMS { get; set; }
		public int Minutes { get; set; }
		public int KBData { get; set; }
	}
}
