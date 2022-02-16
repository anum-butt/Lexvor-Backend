namespace Lexvor.Eversign.Models.Requests {
	public class Signer {
		public string Role { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Pin { get; set; }
		public string Message { get; set; }
		public int DeliverEmail { get; set; }
		public string Language { get; set; }
	}
}
