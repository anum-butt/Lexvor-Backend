using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lexvor.Common {
	public class HttpClientBase {
		private HttpClient _httpClient { get; }

		private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
			ContractResolver = new DefaultContractResolver {
				NamingStrategy = new SnakeCaseNamingStrategy { ProcessDictionaryKeys = true }
			},
			Formatting = Formatting.Indented
		};

		public HttpClientBase() {
			_httpClient = new HttpClient();
		}

		public async Task<TResponse> GetAsync<TResponse>(string url) {
			try {
				var response = await _httpClient.GetAsync(url);
				var responseString = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode) {
					var responseObject = JsonConvert.DeserializeObject<TResponse>(responseString, _jsonSerializerSettings);
					return responseObject;
				}
				else {
					throw new Exception(responseString);
				}
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request) {
			try {
				var stringPayload = JsonConvert.SerializeObject(request, _jsonSerializerSettings);
				var requestBody = new StringContent(stringPayload, Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync(url, requestBody);
				var responseString = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode) {
					var responseObject = JsonConvert.DeserializeObject<TResponse>(responseString, _jsonSerializerSettings);
					return responseObject;
				}
				else {
					throw new Exception(responseString);
				}
			}
			catch (Exception ex) {
				throw ex;
			}
		}
	}
}
