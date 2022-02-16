using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models.ProfileViewModels
{
    public class RequestDeviceViewModel
    {
        public Profile Profile { get; set; }
        public Device Device { get; set; }
	    public UserPlan UserPlan { get; set; }
	    // TODO capture device options
	}
}
