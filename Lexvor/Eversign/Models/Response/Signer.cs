namespace Lexvor.Eversign.Models.Response {
	public class Signer {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public int? Order { get; set; }
		public int? Pin { get; set; }
		public string Message { get; set; }
		public int? Signed { get; set; }
		public string SignedTimestamp { get; set; }
		public int? Required { get; set; }
		public int? DeliverEmail { get; set; }
		public string Language { get; set; }
		public int? Declined { get; set; }
		public int? Removed { get; set; }
		public int? Bounced { get; set; }
		public int? Sent { get; set; }
		public int? Viewed { get; set; }
		public string Status { get; set; }
		public string EmbeddedSigningUrl { get; set; }
	}
}
