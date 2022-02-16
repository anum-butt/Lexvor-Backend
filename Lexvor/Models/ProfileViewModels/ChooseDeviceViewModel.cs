using Lexvor.API.Objects;
using System;
using System.Collections.Generic;

namespace Lexvor.Models.ProfileViewModels {
	public class ChooseDeviceViewModel {
		public Profile Profile { get; set; }
		public ApplicationUser User { get; set; }
		public bool DeviceOrderingEnabled { get; set; }
		public bool HasPendingPurchase { get; set; }
		public bool FirstTimeCustomer { get; set; }
		public string AffirmPublicKey { get; set; }
		public int LinesNumbers { get; set; }
		public PlanType SelectedPlanType { get; internal set; }
		public List<AccessoryLineViewModel> PlanModels { get; set; }
	}

	public class ChooseDevicePurchasesModel {
		public int NumberOfLines { get; set; }
		public List<string> SelectedDeviceIds { get; set; }
		public Guid SelectedPlanId { get; set; }
		public List<string> IMEIs { get; set; }
		public bool BYOD { get; set; }
		public bool Porting { get; set; }
		public bool IsAffirmConfirmed { get; set; }
		public List<OptionModel> SelectedOptions { get; set; }
		public List<AccessoryLineViewModel> Accessories { get; set; }
	}
}
