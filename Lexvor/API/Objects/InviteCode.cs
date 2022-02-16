using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class InviteCode
    {
	    public Guid Id { get; set; }
	    public string Code { get; set; }
	    public bool  Used { get; set; }
    }
}
