using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.Models.AdminViewModels
{
    public class AccountCreditListModel
    {
	    public Profile Profile { get; set; }
	    public List<AccountCredit> Credits { get; set; }
	}

	public class AccountCreditApplyModel {
		public AccountCredit Credit { get; set; }
		public List<UserPlan> Plans { get; set; }
		public Guid PlanId { get; set; }
	}
}
