using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class UserMessage
    {
	    public Guid Id { get; set; }
	    public string Title { get; set; }
	    public string Description { get; set; }
	    public string AccountId { get; set; }
	    public bool ShowOnce { get; set; }
	    public DateTime ExpirationDate { get; set; }
	    public bool Shown { get; set; }
	    public DateTime Created { get; set; }
	    public string Color { get; set; }
    }
}
