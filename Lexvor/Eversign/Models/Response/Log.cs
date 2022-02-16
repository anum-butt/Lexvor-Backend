namespace Lexvor.Eversign.Models.Response {
	public class Log {
		public string Event { get; set; }
		public object Signer { get; set; }
		public int? Timestamp { get; set; }
	}
}
