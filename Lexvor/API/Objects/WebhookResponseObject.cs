using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
	public class WebhookResponseObject {
		public int Id { get; set; }
		public string ObjectType { get; set; }
		public string ObjectId { get; set; }
		public DateTime Received { get; set; }
		public string ReceivedAction { get; set; }
		public string Text { get; set; }
	}
}
