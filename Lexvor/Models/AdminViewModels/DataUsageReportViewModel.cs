using Lexvor.API.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models.AdminViewModels {
	public class DataUsageReportViewModel {
		public Guid Id { get; set; }
		public string Name { get; set; }

		public string Email { get; set; }
		public string Phone { get; set; }

		public string PlanName { get; set; }
		public int DataUsage { get; set; }

		public Double DataCost { get; set; }
       
		public Double Revenue { get; set; }
		public PlanStatus PlanStatus { get; set; }

	}
}
