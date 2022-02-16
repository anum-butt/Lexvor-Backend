using Lexvor.API.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models.AdminViewModels {
	public class PlanTypeViewModels {
	}
	public class PlanTypeCreateVM {
		public List<PlanTypeDeviceSel> PlanTypeDeviceSel { get; set; }
		public PlanType PlanType { get; set; }
	}


	public class PlanTypeDeviceSel {
		public string DeviceName { get; set; }
		public Guid DeviceId { get; set; }
		public bool DevSelect { get; set; }
		public bool DevSelectPre { get; set; }
	}
	public class PlanTypeLinePricing {
		public PlanType PlanType { get; set; }
		public List<LinePricing> LinePricing { get; set;}
	}
	

}
