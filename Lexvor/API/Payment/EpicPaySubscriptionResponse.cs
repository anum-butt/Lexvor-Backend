namespace Lexvor.API.Payment {
	public class EpicPaySubscriptionResponse {
		public EpicPayStatus status { get; set; }
		public EpicPayResult result { get; set; }
	}

	public class EpicPayResult {
		public EpicPaySubscription subscription { get; set; }
		public EpicPayCharge payment { get; set; }
	}

	public class EpicPayStatus {
		public string response_code { get; set; }
		public string reason_code { get; set; }
		public string reason_text { get; set; }
	}
}
