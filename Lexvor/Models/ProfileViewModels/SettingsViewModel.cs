using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lexvor.Models.HomeViewModels {
    public class SettingsViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
        public BankAccount PayAccount { get; set; }

        [Required]
        [DataType(DataType.Upload)]
        [DisplayName("Image of your government issued ID")]
        public IFormFile Upload1 { get; set; }

        [DataType(DataType.Upload)]
        [DisplayName("Image of a recent utility bill or bank statement")]
        public IFormFile Upload2 { get; set; }
    }

    public class ChangePasswordViewModel {
        [Required]
        [DisplayName("Your Current Password")]
        public string OldPassword { get; set; }
        [Required]
        [DisplayName("New Password")]
        public string NewPassword { get; set; }
        [Compare("NewPassword")]
        [DisplayName("Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }
}
