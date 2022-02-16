using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models.HomeViewModels
{
    public class SupportViewModel : LoggedInPageViewModel
    {
        public SupportRequest Request { get; set; }
    }

    public class SupportRequest {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }

		[DisplayName("Preferred Method Of Contact")]
        public string PreferredMethodOfContact { get; set; }

        [DisplayName("Preferred Time")]
        public string PreferredTime { get; set; }
    }
}
