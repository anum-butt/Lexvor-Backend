using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects.User;

namespace Lexvor.Models.AdminViewModels {
    public class TradeInViewModel {
        public List<DeviceIntake> Pending { get; set; }
        public List<DeviceIntake> Complete { get; set; }
    }
}
