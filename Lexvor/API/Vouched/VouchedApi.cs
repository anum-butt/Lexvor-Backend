using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lexvor.API.Vouched {
	public class VouchedApi {
		public string PublicKey { get; set; }
		public string PrivateKey { get; set; }

		public string BaseApi = "https://verify.vouched.id/api";

		public HttpClient Client { get; set; }

		public VouchedApi(string publicKey, string privateKey) {
			PublicKey = publicKey;
			PrivateKey = privateKey;

			Client = new HttpClient();
			Client.DefaultRequestHeaders.Add("X-Api-Key", PrivateKey);
		}

		public async Task<VouchedResponse> SubmitVerification(string callBackUrl, byte[] imageData) {
			var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseApi}/jobs");
			var content = JsonConvert.SerializeObject(new IDJob() {
				type = "id-verification",
				properties = new List<Property>(),
				@params = new Params() {
					idPhoto = "data:image/png;base64," + Convert.ToBase64String(imageData)
				},
				callbackURL = callBackUrl
			});
			request.Content = new StringContent(content, Encoding.UTF8, "application/json");

			try {
				var response = await Client.SendAsync(request);
				var respContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode) {
					try {
						return JsonConvert.DeserializeObject<VouchedResponse>(respContent);
					}
					catch (Exception e) {
						throw new Exception("Could not deserialize the response from the ID service.", new Exception(respContent, e));
					}
				}
				else {
					throw new Exception($"ID Service returned an error code. {respContent}");
				}
			}
			catch (Exception e) {
				throw new Exception("There was an error calling the ID Service.", e);
			}
		}

		public VouchedResponse ParseResponse(string body) {
			try {
				return JsonConvert.DeserializeObject<VouchedResponse>(body);
			} catch (Exception e) {
				throw new Exception("Could not deserialize the response from the ID service.", new Exception(body, e));
			}
		}
	}

	public class Property
	{
		public string name { get; set; }
		public string value { get; set; }
	}

	public class Params
	{
		public string userPhoto { get; set; }
		public string idPhoto { get; set; }
	}

	public class IDJob
	{
		public string type { get; set; }
		public string callbackURL { get; set; }
		public IList<Property> properties { get; set; }
		public Params @params { get; set; }
	}

}
