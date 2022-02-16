using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class WebHookObject {
	    public Guid Id { get; set; }
	    public DateTime DateReceived { get; set; }
	    public string Payload { get; set; }
	    public string Type { get; set; }

	}
}
