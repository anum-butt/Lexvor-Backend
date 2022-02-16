using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
	public class ACHAuthorizationAgreement {
		public Guid Id { get; set; }

		[ForeignKey("ProfileId")]
		public Profile Profile { get; set; }
		public Guid ProfileId { get; set; }

		public UserPlan Plan { get; set; }

		public DateTime Timestamp { get; set; }
		public string IPAddress { get; set; }
		public string UserAgent { get; set; }
		public string Browser { get; set; }
		public string Device { get; set; }
	}
}
