using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
    public class StockedSim {
        public Guid Id { get; set; }
        public DateTime DateAdded { get; set; }
        public string ICCNumber { get; set; }
        public bool Available { get; set; }
    }
}
