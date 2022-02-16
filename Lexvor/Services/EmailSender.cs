using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace Lexvor.Services {
	// This class is used by the application to send email for account confirmation and password reset.
	// For more details see https://go.microsoft.com/fwlink/?LinkID=532713
	public class EmailSender : IEmailSender {
		private static HttpClient Client = new HttpClient();

		public EmailSender(IOptions<MessageSenderOptions> optionsAccessor, IOptions<OtherSettings> otherSettings) {
			Options = optionsAccessor.Value;
			OtherSettings = otherSettings.Value;

			var byteArray = Encoding.ASCII.GetBytes($"api:{Options.ApiKey}");
			Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
		}

		public MessageSenderOptions Options { get; } //set only via Secret Manager
		public OtherSettings OtherSettings { get; } //set only via Secret Manager

		public async Task SendEmailAsync(string[] emails, string subject, string plainMessage, string htmlMessage = "", string replyTo = "") {
			replyTo = string.IsNullOrWhiteSpace(replyTo) ? "noreply@mail.lexvorwireless.com" : replyTo;
			var body = "";

			await Task.Run(async () => {
				try {
					var request =
						new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/mail.lexvorwireless.com/messages");

					var content = new List<KeyValuePair<string, string>> {
						new KeyValuePair<string, string>("from", replyTo),
						new KeyValuePair<string, string>("to", string.Join(',', emails)),
						new KeyValuePair<string, string>("subject", subject),
						new KeyValuePair<string, string>("text", plainMessage),
						new KeyValuePair<string, string>("h:Reply-To", replyTo)
					};

					if (!string.IsNullOrEmpty(htmlMessage)) {
						content.Add(new KeyValuePair<string, string>("html", htmlMessage));

					}

					request.Content = new FormUrlEncodedContent(content);

				    var response = await Client.SendAsync(request);
					body = await response.Content.ReadAsStringAsync();

					if (!response.IsSuccessStatusCode) {
						ErrorHandler.Capture(OtherSettings.SentryDSN, new Exception($"Email failed to send. {response.ReasonPhrase}"), "Email", new Dictionary<string, string>() {
							{ "ResponseBody", body }
						});
					}
				}
				catch (Exception ex) {
					ErrorHandler.Capture(OtherSettings.SentryDSN, ex, "Email", new Dictionary<string, string>() {
						{ "ResponseBody", body }
					});
				}
			});
		
		}

		public Task SendEmailAsync(string email, string subject, string plainMessage, string htmlMessage = "", string replyTo = "") {
			return SendEmailAsync(new[] { email }, subject, plainMessage, htmlMessage, replyTo);
		}

		public async Task SendEmailAsync(string email, string subject, string plainMessage, string attachmentURL, bool isAttachment,byte[] blob, string htmlMessage = "", string replyTo = "") {
			replyTo = string.IsNullOrWhiteSpace(replyTo) ? "noreply@mail.lexvorwireless.com" : replyTo;
			await Task.Run(async () => {

				var body = "";
				try {
					var request =
						new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/mail.lexvorwireless.com/messages");

					var values = new[] {
						new KeyValuePair<string, string>("from", replyTo),
						new KeyValuePair<string, string>("to","rajwindersinghhusnar@gmail.com"),
						new KeyValuePair<string, string>("subject", subject),
						new KeyValuePair<string, string>("text", plainMessage),
						new KeyValuePair<string, string>("h:Reply-To", replyTo)
					};
					MultipartFormDataContent multiPartContent = new MultipartFormDataContent("----MyGreatBoundary");
					ByteArrayContent byteArrayContent = new ByteArrayContent(blob);
					byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");
					multiPartContent.Add(byteArrayContent, "this is the name of the content", "Invoice.pdf");

					if (!string.IsNullOrEmpty(htmlMessage)) {
						values.Append(new KeyValuePair<string, string>("html", htmlMessage));

					}
					foreach (var keyValuePair in values) {
						multiPartContent.Add(new StringContent(keyValuePair.Value),
							String.Format("\"{0}\"", keyValuePair.Key));
					}


					request.Content = multiPartContent;

					var response = await Client.SendAsync(request);
					body = await response.Content.ReadAsStringAsync();

					if (!response.IsSuccessStatusCode) {
						ErrorHandler.Capture(OtherSettings.SentryDSN, new Exception($"Email failed to send. {response.ReasonPhrase}"), "Email", new Dictionary<string, string>() {
							{ "ResponseBody", body }
						});
					}
				}
				catch (Exception ex) {
					ErrorHandler.Capture(OtherSettings.SentryDSN, ex, "Email", new Dictionary<string, string>() {
						{ "ResponseBody", body }
					});
				}
			});
			

					}
	}

	public class MessageSenderOptions {
		public string ApiKey { get; set; }
		public string FromEmail { get; set; }
	}
}
