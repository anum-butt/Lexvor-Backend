namespace Lexvor.Eversign.Models.Response {
	public class Recipient {
		public string Name { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string Message { get; set; }
		public int? Required { get; set; }
		public string Language { get; set; }
	}
}
