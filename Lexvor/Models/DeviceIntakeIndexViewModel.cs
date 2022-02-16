using System.Collections.Generic;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models
{
    public class DeviceIntakeIndexViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
	    public DeviceIntake Intake { get; set; }
	    public List<DeviceIntake> IntakeRequests { get; set; }
	}
}
