using System;
using System.Collections.Generic;
using Lexvor.API.Objects;

namespace Lexvor.Models.ProfileViewModels {
	public class PickPlanViewModel {
		public Profile Profile { get; set; }
		public ApplicationUser User { get; set; }

		public List<PlanType> Plans { get; set; }
		public bool DeviceOrderingEnabled { get; set; }
		public bool HasPendingPurchase { get; set; }
		public bool FirstTimeCustomer { get; set; }
		public string AffirmPublicKey { get; set; }
	}

	public class PlanSelection {
		public string DeviceId { get; set; }
	}

	public class PlanPurchaseModel {
		public int NumberOfLines { get; set; }
		public List<string> SelectedDeviceIds { get; set; }
		public Guid SelectedPlanId { get; set; }
		public List<string> MobileNumbers { get; set; }
		public List<string> IMEIs { get; set; }
		public bool BYOD { get; set; }
		public bool Porting { get; set; }
		public bool IsAffirmConfirmed { get; set; }
		public List<OptionModel> SelectedOptions { get; set; }
	}

	public class PlanPurchaseFirstStepModel {
		public int NumberOfLines { get; set; }
		public Guid SelectedPlanId { get; set; }
		public List<string> LineHolderNames { get; set; }
		public List<string> MobileNumbers { get; set; }
		public bool Porting { get; set; }
	}

	public class OptionModel {
		public Guid DeviceId { get; set; }
		public List<OptionGroupModel> Options { get; set; }
	}

	public class OptionGroupModel {
		public Guid Id { get; set; }
	}
}
