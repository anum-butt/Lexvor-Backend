using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Models {
    public class UserPaymentContextViewModel {
        public BankAccount PayAccount { get; set; }
        public UserPlan UserPlan { get; set; }
        public Guid PlanTypeId { get; set; }
        public PlanType PlanType { get; set; }
        public string PromoCode { get; set; }
        public  DiscountCode AppliedPromo { get; set; }

        // Promo modified amounts
        public int InitiationAdjusted { get; set; }
        public int MonthlyAdjusted { get; set; }

        // Subtotal after promo is applied
    }
}
