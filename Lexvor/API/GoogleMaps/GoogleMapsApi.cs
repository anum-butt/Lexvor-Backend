using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Lexvor.API.GoogleMaps {
	public class GoogleMapsApi {
		private string ApiKey { get; set; }

		private string BaseApi = "https://maps.googleapis.com/maps/api";

		public HttpClient Client { get; set; }

		public GoogleMapsApi(string apiKey) {
			ApiKey = apiKey;

			Client = new HttpClient();
		}

		public async Task<GeocodingResponse> Geocode(string input) {
			var builder = new UriBuilder(BaseApi + "/geocode/json");
			var query = HttpUtility.ParseQueryString(builder.Query);
			query["address"] = input.ToLower();
			query["key"] = ApiKey;
			builder.Query = query.ToString();

			try {
				var response = await Client.GetAsync(builder.ToString());
				var respContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode) {
					try {
						return JsonConvert.DeserializeObject<GeocodingResponse>(respContent);
					}
					catch (Exception e) {
						throw new Exception("Could not deserialize the response from the Google Maps Place API.", e);
					}
				}
				else {
					throw new Exception($"Google Maps Place API returned an error code. {respContent}");
				}
			} catch (Exception e) {
				throw new Exception($"Google Maps Place API returned an error. {e}");
			}
		}
	}
}
