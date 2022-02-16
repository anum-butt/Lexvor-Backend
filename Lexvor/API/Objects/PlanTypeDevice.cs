using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
    public class PlanTypeDevice {
        public Device Device { get; set; }
        public Guid DeviceId { get; set; }

        public PlanType PlanType { get; set; }
        public Guid PlanTypeId { get; set; }
    }
}
