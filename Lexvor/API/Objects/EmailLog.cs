using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
	public class EmailLog {
		public int Id { get; set; }
		public Profile Profile { get; set; }
		public EmailType EmailType { get; set; }
		public string Subject { get; set; }
		public DateTime Sent { get; set; }
		public bool Success { get; set; }
		public string Error { get; set; }
	}

	public enum EmailType {
		PingEmail = 0
	}
}
