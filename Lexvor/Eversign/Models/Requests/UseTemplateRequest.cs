using System.Collections.Generic;

namespace Lexvor.Eversign.Models.Requests {
	public class UseTemplateRequest {
		public int Sandbox { get; set; }
		public string TemplateId { get; set; }
		public string Title { get; set; }
		public string Message { get; set; }
		public string CustomRequesterName { get; set; }
		public string CustomRequesterEmail { get; set; }
		public string Redirect { get; set; }
		public string RedirectDecline { get; set; }
		public string Client { get; set; }
		public int Expires { get; set; }
		public int EmbeddedSigningEnabled { get; set; }
		public List<Signer> Signers { get; set; }
		public List<Recipient> Recipients { get; set; }
		public List<Field> Fields { get; set; }
	}
}
