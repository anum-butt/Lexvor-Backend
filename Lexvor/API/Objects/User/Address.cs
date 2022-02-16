using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
    public class Address {
        public Guid Id { get; set; }
        
        [DisplayName("Address")]
        public string Line1 { get; set; }

        [DisplayName("Address Line 2")]
        public string Line2 { get; set; }
        
        public string City { get; set; }
        
        [DisplayName("State")]
        public string Provence { get; set; }
        
        [Required]
        [DisplayName("Zip")]
        public string PostalCode { get; set; }
        
        public Guid ProfileId { get; set; }
        public DateTime LastUpdated { get; set; }

        public AddressSource Source { get; set; }

        public string GetAddressBlock() {
	        return $"{Line1} {Line2}{Environment.NewLine}{City}, {Provence} {PostalCode}";
        }
    }

    public enum AddressSource {
        UserInput,
        Bank,
        USPS,
		Vouched,
		GoogleMaps
    }
}
