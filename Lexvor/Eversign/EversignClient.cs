using Lexvor.Common;
using Lexvor.Eversign.Models;
using Lexvor.Eversign.Models.Requests;
using Lexvor.Eversign.Models.Response;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lexvor.Eversign {
	public class EversignClient {
		private string BaseUrl { get; }
		private string AccessKey { get; }
		private int BusinessId { get; }
		private string AccessKeyQuery => $"access_key={AccessKey}";
		private string BusinessIdQuery => $"business_id={BusinessId}";

		public EversignClient(string baseUrl, string accessKey, int businessId) {
			BaseUrl = baseUrl;
			AccessKey = accessKey;
			BusinessId = businessId;
		}

		public async Task<GetDocumentResponse> UseTemplate(UseTemplateRequest request) {
			HttpClientBase client = new HttpClientBase();
			var useTanplateResponse = await client.PostAsync<UseTemplateRequest, GetDocumentResponse>(
				$"{BaseUrl}/document?{AccessKeyQuery}&{BusinessIdQuery}", 
				request);

			return useTanplateResponse;
		}

		public async Task<GetDocumentResponse> GetDocument(string documentHash) {
			string documentHashQuery = $"document_hash={documentHash}";
			HttpClientBase client = new HttpClientBase();
			var getEmbeddedSigningURLsResponse = await client.GetAsync<GetDocumentResponse>(
				$"{BaseUrl}/document?{AccessKeyQuery}&{BusinessIdQuery}&{documentHashQuery}");

			return getEmbeddedSigningURLsResponse;
		}
	}
}
