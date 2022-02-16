using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {
	public class DeviceOption {
		public Guid Id { get; set; }

		public Device Device { get; set; }

		/// <summary>
		/// What group this option belongs to (ex. Color, Size)
		/// </summary>
		/// 
		[Required(ErrorMessage = "Please Enter Option")]
		public string OptionGroup { get; set; }
		[Required(ErrorMessage = "Please Enter Value")]
		public string OptionValue { get; set; }

		public StockedDevice StockedDevice { get; set; }

		/// <summary>
		/// Amount in cents to charge extra month for this option
		/// </summary>
		public int Surcharge { get; set; }
	}
}
