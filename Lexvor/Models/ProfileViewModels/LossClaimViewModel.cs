using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.Helpers;
using Microsoft.AspNetCore.Http;

namespace Lexvor.Models.ProfileViewModels
{
    public class LossClaimViewModel
    {
		public LossClaim LossClaim { get; set; }
        public Guid Id { get; set; }
        [DisplayName("Type of claim")]
        public LossType LossType { get; set; }
        [DisplayName("Image of the whole front of the phone")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile ImageWholeFront { get; set; }
        [DisplayName("Image of the whole back of the phone")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile ImageWholeBack { get; set; }
        [DisplayName("Image of the phone screen while on")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile ImagePhoneScreenOn { get; set; }
        [DisplayName("Image of the phone screen while off")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile ImagePhoneScreenOff { get; set; }
        [DisplayName("Close up of the damage from one angle")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile DamageCloseUpAngle1 { get; set; }
        [DisplayName("Close up of the damage from second angle")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile DamageCloseUpAngle2 { get; set; }
        [DisplayName("Close up of the damage from third angle")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" })]
		public IFormFile DamageCloseUpAngle3 { get; set; }
        [DisplayName("Police report picture")]
		[MaxFileSize(8)]
		[AllowedExtensions(new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp", ".pdf" })]
		public IFormFile PoliceReport { get; set; }
        public string Message { get; set; }
        public Profile Profile { get; set; }
    }
}
