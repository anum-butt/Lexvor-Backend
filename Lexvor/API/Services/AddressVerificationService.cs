using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects.User;
using MAX.USPS;
using Address = Lexvor.API.Objects.User.Address;

namespace Lexvor.API.Services {
    public static class AddressVerificationService {

		public static async Task<Address> Verify(Address address, string USPSWebtoolUserID) {
            var manager = new USPSManager(USPSWebtoolUserID, false);
            //var verifier =
            //    new AddressVerification.Proxy.AddressVerifier("http://production.shippingapis.com/ShippingAPI.dll",
            //        "338WESTR1674", "940SA56FE024");

            var addVer = new MAX.USPS.Address() {
                // Yes this is on purpose. Address1 is for suite or apartment number
                Address2 = address.Line1,
                City = address.City,
                State = address.Provence,
                Zip = address.PostalCode
            };

            if (!string.IsNullOrWhiteSpace(address.Line2)) {
                addVer.Address1 = address.Line2;
            }

			try {
				var verified = manager.ValidateAddress(addVer);
				return new Address() {
					// Yes this is on purpose. Address1 is for suite or apartment number
					Line1 = verified.Address2,
					Line2 = verified.Address1,
					City = verified.City,
					Provence = verified.State,
					PostalCode = verified.Zip,
					Source = AddressSource.USPS
				};
			} catch (Exception e) {
				throw e;
			}
        }
    }
}
