using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexvor.API.Objects.Enums;
using Lexvor.Extensions;
using Microsoft.AspNetCore.Http;

namespace Lexvor.API.Objects.User
{
    public class DeviceIntake {
	    public Guid Id { get; set; }

	    [ForeignKey("ProfileId")]
	    public Profile Profile { get; set; }
	    public Guid ProfileId { get; set; }
		
		[DisplayName("Front of the Device. All edges visible.")]
		public string FrontImageUrl { get; set; }
	    [NotMapped]
	    [Required]
        [DataType(DataType.Upload)]
	    [DisplayName("Front of the Device. All edges visible.")]
		public IFormFile FrontImageUpload { get; set; }
		
		[DisplayName("Back of the Device. All edges visible.")]
		public string BackImageUrl { get; set; }
	    [NotMapped]
	    [Required]
        [DataType(DataType.Upload)]
	    [DisplayName("Back of the Device. All edges visible.")]
		public IFormFile BackImageUpload { get; set; }
		
		[DisplayName("Picture with the Device turned on")]
		public string OnImageUrl { get; set; }
	    [NotMapped]
	    [Required]
        [DataType(DataType.Upload)]
	    [DisplayName("Picture with the Device on")]
		public IFormFile OnImageUpload { get; set; }

		[Required]
	    public string Make { get; set; }
	    [Required]
		public string Model { get; set; }

	    [Required]
	    public string IMEI {
		    get => _imei;
		    set => _imei = Regex.Replace(value, @"[^\d]", "");
	    }
	    private string _imei;

	    [Required]
	    [DisplayName("Has this device been repaired by a third party repair shop?")]
	    public bool Repaired { get; set; }

	    [Required]
	    [DisplayName("Does this device have a balance left on it owed to a provider?")]
	    public bool Balance { get; set; }

	    [Required]
	    [DisplayName("Does this device turn on and charge properly?")]
	    public bool Charges { get; set; }

	    [Required]
	    [DisplayName("Are you the original owner of this device?")]
	    public bool OriginalOwner { get; set; }

		public DateTime Requested{ get; set; }
	    public DateTime? Received { get; set; }
		public DateTime? Approved { get; set; }
	    public IntakeType IntakeType { get; set; }
	}
}
