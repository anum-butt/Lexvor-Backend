using Lexvor.API.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models {
	public class AccessoryPurchaseViewModel {
		public Profile Profile { get; set; }
		public List<AccessoryLineViewModel> PlanModels { get; set; }
	}

	public class AccessoryLineViewModel {
		public UserPlan UserPlan { get; set; }
		public double Total { get; set; }
		public List<AccessoryGroupViewModel> AccessoryGroups { get; set; }
	}

	public class AccessoryGroupViewModel {
		public string GroupName { get; set; }
		public List<AccessoryItemViewModel> Accessories { get; set; }
	}

	public class AccessoryItemViewModel {
		public string Name { get; set; }
		public int Price { get; set; }
		public bool LifetimeWarranty { get; set; }
		public int LifetimeWarrantyPrice { get; set; }
		public bool Selected { get; set; }
		public bool SelectedWarranty { get; set; }
		public string ImageUrl { get; set; }
		public string Description { get; set; }
		public Guid Id { get; set; }
	}
}
