using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AdminViewModels {
	public class SIMAssignmentViewModel {
		public List<StockedSim> AvailableSIMs { get; set; }
		public Guid PlanId { get; set; }
	}
}
