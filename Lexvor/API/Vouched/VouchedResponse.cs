using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Vouched {
	public class Parameters {
	}

	public class Request {
		public string type { get; set; }
		public string callbackURL { get; set; }
		public RequestInfo requestInfo { get; set; }
		public Parameters parameters { get; set; }
		public string properties { get; set; }
	}

	public class RequestInfo {
		public string referer { get; set; }
		public string useragent { get; set; }
		public string ipaddress { get; set; }
	}

	public class IdFieldsItem {
		public string name { get; set; }
	}

	public class Error {
		public string type { get; set; }
		public string message { get; set; }
		public string suggestion { get; set; }
	}

	public class IdAddress {
		public string unit { get; set; }
		public string streetNumber { get; set; }
		public string street { get; set; }
		public string city { get; set; }
		public string state { get; set; }
		public string country { get; set; }
		public string postalCode { get; set; }
		public string postalCodeSuffix { get; set; }
	}
	
	public class IpAddress    {
		public string city { get; set; } 
		public string country { get; set; } 
		public string state { get; set; } 
		public string postalCode { get; set; } 
		public Location location { get; set; } 
		public string userType { get; set; } 
		public string isp { get; set; } 
		public string organization { get; set; } 
		public bool?isAnonymous { get; set; } 
		public bool? isAnonymousVpn { get; set; } 
		public bool? isAnonymousHosting { get; set; } 
	}
	
	public class Location    {
		public double latitude { get; set; } 
		public double longitude { get; set; } 
	}

	public class Confidences {
		public double id { get; set; }
		public double? idQuality { get; set; }
		public double? idGlareQuality { get; set; }
		public string birthDateMatch { get; set; }
		public string nameMatch { get; set; }
		public string selfie { get; set; }
		public string selfieSunglasses { get; set; }
		public string selfieEyeglasses { get; set; }
		public string idMatch { get; set; }
		public string faceMatch { get; set; }
	}

	public class Result {
		public string id { get; set; }
		public string firstName { get; set; }
		public string lastName { get; set; }
		public DateTime? dob { get; set; }
		public DateTime? expireDate { get; set; }
		public DateTime? issueDate { get; set; }
		public DateTime? birthDate { get; set; }
		public string @class { get; set; }
		public string endorsements { get; set; }
		public string motorcycle { get; set; }
		public List<IdFieldsItem> idFields { get; set; }
		public IpAddress ipAddress { get; set; }
		public IdAddress idAddress { get; set; }
		public string type { get; set; }
		public string country { get; set; }
		public string state { get; set; }
		public Confidences confidences { get; set; }
		public bool success { get; set; }
		public string successWithSuggestion { get; set; }
		public bool? warnings { get; set; }
	}

	public class VouchedResponse {
		public string id { get; set; }
		public string status { get; set; }
		public string completed { get; set; }
		public string accountReviewed { get; set; }
		public DateTime submitted { get; set; }
		public DateTime updatedAt { get; set; }
		public DateTime? reviewedAt { get; set; }
		public string review { get; set; }
		public Request request { get; set; }
		public string surveyPoll { get; set; }
		public string surveyMessage { get; set; }
		public DateTime? surveyAt { get; set; }
		public Result result { get; set; }
		public List<Error> errors { get; set; }
	}

}
