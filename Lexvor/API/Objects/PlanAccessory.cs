using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
	public class PlanAccessory {
		public Guid Id { get; set; }

		[Required]
		public string Name { get; set; }
		public string Description { get; set; }
		/// <summary>
		/// Price in Cents
		/// </summary>
		public int Price { get; set; }

		public string Grouping { get; set; }

		/// <summary>
		/// Comma separated list of available options for the user to pick
		/// </summary>
		public string Options { get; set; }

		public string ImageUrl { get; set; }

		[NotMapped]
		[DataType(DataType.Upload)]
		public IFormFile ImageUpload { get; set; }

		[Display(Name="Accessory has available LT warranty")]
		public bool LifetimeWarranty { get; set; }
		public int LifetimeWarrantyPrice { get; set; }

		public int? CurrentStockLevel { get; set; }
	}
}
