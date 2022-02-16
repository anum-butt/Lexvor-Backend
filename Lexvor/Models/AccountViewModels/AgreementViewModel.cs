using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.AccountViewModels {
	public class AgreementViewModel {
		[DisplayName("Please type your full legal name")]
		public string ProvidedName { get; set; }
		
		public bool Accepted { get; set; }
	    public string AgreementName { get; set; }
	    public UserPlan Plan { get; set; }
	}
}
