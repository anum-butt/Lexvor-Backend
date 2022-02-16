using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
    [Obsolete]
    public class ProfileCreditCardAccount {
        public Guid Id { get; set; }

        [ForeignKey("ProfileId")]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }
        
        [DisplayName("Credit Card Number")]
        public string CreditCardNumber { get; set; }

        public string ExternalId { get; set; }

        public bool Active { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
