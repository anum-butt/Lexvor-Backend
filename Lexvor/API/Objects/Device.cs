using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Lexvor.API.Objects
{
    public class Device {
        public Guid Id { get; set; }
		[Required (ErrorMessage = "Please Enter Device Name")]
        public string Name { get; set; }
        /// <summary>
        /// Phone, Tablet, or Other
        /// </summary>
        public DeviceType DeviceType { get; set; }

        public string ImageUrl { get; set; }
        [NotMapped]
        [DataType(DataType.Upload)]
        public IFormFile ImageUpload { get; set; }

        public string Description { get; set; }

        public bool Available { get; set; }
        public bool Archived { get; set; }

        public List<PlanTypeDevice> PlanTypes { get; set; }

        public List<DeviceOption> Options { get; set; }

        public int SortOrder { get; set; }

		[Display(Name="Price (for Affirm) in cents")]
		public int Price { get; set; }
    }

    public enum DeviceType {
        Phone = 1,
        Tablet = 2,
        Other = 3
    }
}
