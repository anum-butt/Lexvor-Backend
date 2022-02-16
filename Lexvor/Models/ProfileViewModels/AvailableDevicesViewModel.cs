using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lexvor.API.Objects;
using Microsoft.AspNetCore.Http;

namespace Lexvor.Models.ProfileViewModels
{
    public class AvailableDevicesViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }

        public UserPlan CurrentUserPlan { get; set; }
        public List<Device> Devices { get; set; }

        public Guid SelectedDeviceId { get; set; }
        public Guid CurrentUserPlanId { get; set; }
    }
}
