using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models {
    public class ValidateViewModel {
        public string MobileNumber { get; set; }
        public string IMEI { get; set; }
    }
    public class ValidateIndexViewModel {
        public Profile Profile { get; set; }
        public bool ShowIMEI { get; set; }
    }
    public class UsageViewModel {
	    public Profile Profile { get; set; }
	    public List<UsageDay> Usage { get; set; }
    }
}
