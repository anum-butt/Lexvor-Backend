using System;

namespace Lexvor.Models {
	public class SignEversignDocumentModel {
		public string EmbeddedSigningUrl { get; set; }
		public string Profile { get; set; }
		public Guid PlanId { get; set; }
	}
}
