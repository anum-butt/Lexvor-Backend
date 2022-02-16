using Lexvor.API.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Lexvor.Models.AdminViewModels {

	public class StockDeviceListVM {
		public Guid Id { get; set; }
		public Device Device { get; set; }
		public List<Device> DeviceLi { get; set; }

		//public List<StockedDevice> StockedDevice { get; set; }
		public StockedDevice StockedDevice { get; set; }
		public ApplicationUser User { get; set; }
		public string AssignTo { get; set; }
		public string IMEI { get; set; }

	}
	public class StockDeviceListAllVM {
		public List<StockDeviceListVM> StockDeviceListVM { get; set; }
		public List<ApplicationUser> Users { get; set; }
		public List<Device> Dev { get; set; }
		public StockFilter stockfilter { get; set; }
	}

	public class StockFilter {
		public string stock { get; set; }
		public Guid deviceid { get; set; }
		public string imei { get; set; }
		public Guid userid{ get; set; }
	}
		public class StockDeviceCreateViewModels {

		public StockedDevice StockedDevice { get; set; }
		public List<Device> Device { get; set; }
		public IEnumerable<SelectListItem> DeviceList { get; set; }
		public string createMode { get; set; }

		public List<CSVDevList> csvdevnamelist { get; set; }

		public List<CSVDevList> csvdevimeilist { get; set; }
		public List<string> csvduplicateimeilist { get; set; }
		public string ErrorMessage { get; set; }

	}
	public class CSVDevList {
		public string Name { get; set; }
		public string imei { get; set; }
		public List<DeviceOption> DevOpt { get; set; }
	}
	public class StockDeviceEditViewModels {
		public Guid Id { get; set; }

		public StockedDevice StockedDevice { get; set; }
		public List<Device> Device { get; set; }

	}

	public class NewAssignVM {
		public StockedDevice StockedDevice { get; set; }

		public List<UserPlan> UserPlan { get; set; }

		public UserPlan CurrUserPlan { get; set; }

		public List<Profile> Profile { get; set; }
		public List<PlanType> PlanType { get; set; }

		public Guid profileId { get; set; }

		public Guid PlanTypeId { get; set; }

		public string AssignType { get; set; }


	}

}
