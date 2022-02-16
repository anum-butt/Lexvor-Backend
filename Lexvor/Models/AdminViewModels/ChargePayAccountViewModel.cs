using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models.AdminViewModels {
	public class ChargePayAccountViewModel {
		public Guid ProfileId { get; set; }

		public Guid PayAccountId { get; set; }

		[Required(ErrorMessage = "Please enter a charge amount.")]
		[Range(0.01, 1500.00, ErrorMessage = "Charge amount must be between 0.01 and 1500.00.")]
		public decimal ChargeAmount { get; set; }

		[Required(ErrorMessage = "Please enter a charge description.")]
		public string ChargeDescription { get; set; }
	}
}
