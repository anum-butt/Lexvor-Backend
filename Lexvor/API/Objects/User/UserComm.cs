using System;
using Lexvor.API.Objects.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
	public class UserComm {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid Id { get; set; }
		public string ExternalId { get; set; }
		public Guid ProfileId { get; set; }
		public DateTime? Sent { get; set; }
		public string Status { get; set; }
		public string Recipient { get; set; }
		public MessageType MessageType { get; set; }
		public string Subject { get; set; }
		public string Content { get; set; }
		public string? ErrorMessage { get; set; }
	}
}
