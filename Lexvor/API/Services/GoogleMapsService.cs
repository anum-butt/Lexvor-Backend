using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.GoogleMaps;
using Lexvor.API.Objects.User;

namespace Lexvor.API.Services {
	public class GoogleMapsService {
		public static async Task<Address> AddressLookup(Address address, string apiKey) {
			var service = new GoogleMapsApi(apiKey);

			try {
				var response = await service.Geocode(address.Line1 + " " + address.City + " " + address.Provence);

				if (response != null) {
					var addrComponents = response.results[0].formatted_address.Split(",");

					return new Address {
						Line1 = addrComponents[0].Trim(),
						City = addrComponents[1].Trim(),
						Provence = addrComponents[2].Split(" ")[1].Trim(),
						PostalCode = addrComponents[2].Split(" ")[2].Trim()
					};

				} else {
					throw new Exception("Google Maps returned a null response.");
				}
			}
			catch (Exception e) {
				throw e;
			}
		}
	}
}
