using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Microsoft.AspNetCore.Http;

namespace Lexvor.Models.ProfileViewModels {
	public class AdministerDevicesViewModel {
		public Profile Profile { get; set; }
		public ApplicationUser User { get; set; }

		public UserPlan CurrentUserPlan { get; set; }

		public PortRequest PortRequest { get; set; }
		public UsageDay UsageDay { get; set; }
	}
}
