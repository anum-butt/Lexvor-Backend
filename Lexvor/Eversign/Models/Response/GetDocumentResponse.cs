using System.Collections.Generic;

namespace Lexvor.Eversign.Models.Response {
	public class GetDocumentResponse {
		public string DocumentHash { get; set; }
		public string RequesterEmail { get; set; }
		public string CustomRequesterName { get; set; }
		public string CustomRequesterRmail { get; set; }
		public int? IsDraft { get; set; }
		public int? IsTemplate { get; set; }
		public int? IsCompleted { get; set; }
		public int? IsArchived { get; set; }
		public int? IsDeleted { get; set; }
		public int? IsTrashed { get; set; }
		public int? IsCancelled { get; set; }
		public int? Embedded { get; set; }
		public int? InPerson { get; set; }
		public string Permission { get; set; }
		public string TemplateId { get; set; }
		public string Title { get; set; }
		public string Message { get; set; }
		public int? UseSignerOrder { get; set; }
		public int? Reminders { get; set; }
		public int? RequireAllSigners { get; set; }
		public string Redirect { get; set; }
		public string RedirectDecline { get; set; }
		public string Client { get; set; }
		public int? Created { get; set; }
		public int? Completed { get; set; }
		public int? Expires { get; set; }
		public int? EmbeddedSigningEnabled { get; set; }
		public int? FlexibleSigning { get; set; }
		public List<File> Files { get; set; }
		public List<Signer> Signers { get; set; }
		public List<Recipient> Recipients { get; set; }
		public List<List<Field>> Fields { get; set; }
		public List<Log> Log { get; set; }
		public dynamic Meta { get; set; }
	}
}
