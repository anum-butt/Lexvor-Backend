using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AdminViewModels {
	public class DeviceViewModels {
	}
	public class DeviceCreateVM {
		public List<DevicePlanTypeSel> DevicePlanTypeSel { get; set; }
		public Device Device { get; set; }
	}

	public class DeviceEditVM {
		public List<DevicePlanTypeSel> DevicePlanTypeSel { get; set; }
		public Device Device { get; set; }
		public bool ShowDeviceOptions { get; set; }
		public List<DeviceOption> DeviceOption { get; set; }
	}

	public class DevicePlanTypeSel {
		public PlanType PlanType { get; set; }
		public bool PlanTypeSelect { get; set; }
		public bool PlanTypeSelectPre { get; set; }
	}
	public class DeviceEditViewModel {
		public Device Device { get; set; }
		public ICollection<AssignedPlanTypesModel> PlanTypes { get; set; }
	}

	public class AssignedPlanTypesModel {
		public Guid PlanTypeId { get; set; }
		public bool Assigned { get; set; }
	}

	public class NoDevicesVM {
		public List<UserPlan> UserPlan { get; set; }
	}
}
