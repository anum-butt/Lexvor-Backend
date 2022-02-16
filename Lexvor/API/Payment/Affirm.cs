using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Lexvor.Extensions;
using Newtonsoft.Json;

namespace Lexvor.API.Payment {
	public class Affirm {
		public string ApiUrl { get; set; }
		public HttpClient client { get; set; }

		public Affirm(string publicKey, string privateKey, string apiUrl) {
			client = new HttpClient();
			var credentials = Encoding.ASCII.GetBytes($"{publicKey}:{privateKey}");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
			ApiUrl = apiUrl;
		}

		public async Task<string> AuthorizeCharge(string checkoutToken, string orderId, int orderTotal) {
			var response = await client.PostAsync($"{ApiUrl}charges", new StringContent(JsonConvert.SerializeObject(new { checkout_token = checkoutToken, order_id = orderId }), Encoding.UTF8, "application/json"));
			var resString = await response.Content.ReadAsStringAsync();

			try {
				var authorize = JsonConvert.DeserializeObject<AffirmAuthorizeResponse>(resString);

				if (authorize.auth_hold < orderTotal) {
					throw new Exception($"The approval amount from Affirm was less than the order total. You cannot continue using Affirm if you were not authorized for the order total. Affirm: {authorize.auth_hold}. Order Total: {orderTotal}");
				}

				return authorize.id;
			}
			catch (Exception e) {
				throw new Exception("Could not deserialize response from Affirm.", new Exception(resString));
			}
		}

		public async Task<AffirmCaptureResponse> CaptureCharge(string chargeId, string orderId) {
			var response = await client.PostAsync($"{ApiUrl}charges/{chargeId}/capture", new StringContent(JsonConvert.SerializeObject(new { order_id = orderId }), Encoding.UTF8, "application/json"));
			var resString = await response.Content.ReadAsStringAsync();

			try {
				var capture = JsonConvert.DeserializeObject<AffirmCaptureResponse>(resString);

				return capture;
			}
			catch (Exception e) {
				throw new Exception($"Could not deserialize response from Affirm. {e.Message} | {resString}");
			}
		}
	}
}
