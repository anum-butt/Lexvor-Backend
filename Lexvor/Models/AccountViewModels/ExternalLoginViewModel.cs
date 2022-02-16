using System.ComponentModel.DataAnnotations;

namespace Lexvor.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
