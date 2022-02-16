using System;
using System.Collections.Generic;

namespace Lexvor.API {
    public class ConnectionStrings {
        public string DefaultConnection { get; set; }
    }

    public class OtherSettings {
        public string SentryDSN { get; set; }
	    public string StripePublishableKey { get; set; }

		public string StripeSecretKey { get; set; }
		public string BlobUri { get; set; }
        public string BlobKey { get; set; }
        public string BlobContainer { get; set; }
        public string BlobContainerSensitive { get; set; }

	    public bool InviteOnly { get; set; }

        public string AcceptedEmailEndings { get; set; }
        public string[] EmailEndings => AcceptedEmailEndings?.Split(',');

        public string ChargeOverEndpoint { get; set; }
        public string ChargeOverUser { get; set; }
        public string ChargeOverPassword { get; set; }

        public string PlaidEnv { get; set; }
        public string PlaidClientId { get; set; }
        public string PlaidPublicKey { get; set; }
        public string PlaidSecret { get; set; }

        public string TelispireUser { get; set; }
        public string TelispirePassword { get; set; }

        public string EncryptionKey { get; set; }
		
        public string EpicPayUrl { get; set; }
        public string EpicPayReportingUrl { get; set; }
        public string EpicPayKey { get; set; }
        public string EpicPayPass { get; set; }

		public bool DeviceOrderingEnabled { get; set; }

		public string VouchedPublicKey { get; set; }
		public string VouchedPrivateKey { get; set; }
		public string USPSWebtoolUserID { get; set; }
		public string TwilioAccountSid { get; set; }
		public string TwilioAuthToken { get; set; }
		public string TwilioNumber { get; set; }
		public string GoogleApiKey { get; set; }
		public string ChatBotToken { get; set; }
		public bool EnableBalanceCheckFailures { get; set; }
		
		public string AffirmApiUrl { get; set; }
		public string AffirmJsUrl { get; set; }
		public string AffirmPrivateKey { get; set; }
		public string AffirmPublicKey { get; set; }
		public bool EnablePhoneVerifications { get; set; }
		public bool ThrottleNotifyEnabled { get; set; }

		public string EversignBaseUrl { get; set; }
		public string EversignAccessKey { get; set; }
		public int EversignBusinessId { get; set; }
		public string EversignTestTemplateHash { get; set; }
	}
}
