using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Payment;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Lexvor.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Lexvor.API.Payment {
	public class EpicPay {
		private static class Urls {
			public static string AddSubscription = nameof(AddSubscription);
			public static string AddWalletItem = nameof(AddWalletItem);
			public static string EditSubscription = nameof(EditSubscription);
			public static string Charge = "authorize";
			public static string SuspendSubscription = "Subscription/Suspend";
			public static string DeleteSubscription = "deletesubscription";
			public static string GetReport = nameof(GetReport);
		}

		private HttpClient Client => new HttpClient() {
			DefaultRequestHeaders = {
				Authorization = new AuthenticationHeaderValue("Basic", AuthValue)
			}
		};

		private string BaseUrl { get; set; }
		private string ReportingBaseUrl { get; set; }
		private string Key { get; set; }
		private string Pass { get; set; }
		private string AuthValue => System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Key + ":" + Pass));

		private Uri GetUrl(string relative) => new Uri(new Uri(BaseUrl), relative);
		private Uri GetReportUrl(string relative) => new Uri(new Uri(ReportingBaseUrl), relative);

		public EpicPay(string baseUrl, string apikey, string apiPassword, string reportingUrl = "") {
			BaseUrl = baseUrl;
			Key = apikey;
			Pass = apiPassword;
			ReportingBaseUrl = reportingUrl;
		}

		/// <summary>
		/// This will only EVER be called once, when a user first purchases their FIRST ever plan.
		/// Returns: CustomerId, SubscriptionId, SubscriptionStartDate, Error
		/// </summary>
		/// <param name="profile"></param>
		/// <param name="plan"></param>
		/// <param name="userBillDate"></param>
		/// <returns></returns>
		public async Task<(string, string, DateTime, string)> CreateSubscription(string userEmail, Profile profile, DateTime nextBillDate, int monthly, BankAccount pay, string decryptKey, string callbackUrl) {
			// DO NOT user EpicPay's Wallet feature.
			if (profile.BillingDay == -1) {
				throw new MissingFieldException("The profile does not have a BillingDay set");
			}

			if (!string.IsNullOrWhiteSpace(profile.ExternalCustomerId)) {
				throw new Exception("You cannot create a new subscription for a customer that has an existing subscription.");
			}

			// Add subscription
			var subscription = new EpicPaySubscription(userEmail, profile, monthly, nextBillDate, pay, decryptKey, callbackUrl);

			var response = await Client.PostAsync(GetUrl(Urls.AddSubscription), new StringContent(JsonConvert.SerializeObject(subscription), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				var epResp =
					JsonConvert.DeserializeObject<EpicPaySubscriptionResponse>(respString);
				if (epResp.status.response_code.ToLower() == "received" || epResp.status.response_code.ToLower() == "approved") {
					var sub = epResp.result.subscription;
					// Wallet id is used to update the payment details on a subscription
					return (sub.wallet_id, sub.subscription_id, profile.NextBillDate, "");
				} else {
					// Return error from processor, bubble to user.
					return ("", "", default(DateTime), epResp.status.reason_text);
				}
			} else {
				ErrorHandler.StaticCapture(new Exception($"EpicPay Create Subscription call failed for profile {profile.Id}"), null, "EpicPay-Create", new Dictionary<string, string>() {
					{"Response", respString}
				});
				return ("", "", default(DateTime), "There was an unexpected error. Please try again.");
			}
		}

		/// <summary>
		/// This will be called whenever a customer adds a plan to their Profile.
		/// Returns: CustomerId, SubscriptionId, SubscriptionStartDate, Error
		/// </summary>
		/// <param name="profile"></param>
		/// <param name="plan"></param>
		/// <param name="userBillDate"></param>
		/// <returns></returns>
		public async Task<(string, string, DateTime, string)> EditSubscription(string subId, string userEmail, Profile profile, DateTime nextBillDate, int monthly, BankAccount pay, string decryptKey, string callbackUrl) {
			// DO NOT user EpicPay's Wallet feature.
			if (profile.BillingDay == -1) {
				throw new MissingFieldException("The profile does not have a BillingDay set");
			}

			if (string.IsNullOrWhiteSpace(subId)) {
				throw new Exception("The Existing Subscription ID was not provided.");
			}

			// Start date will be the date that the subscription first started.
			var subscription = new EpicPaySubscription(userEmail, profile, monthly, nextBillDate, pay, decryptKey, callbackUrl);

			var response = await Client.PostAsync(GetUrl($"{Urls.EditSubscription}/{subId}"), new StringContent(JsonConvert.SerializeObject(subscription), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				var epResp =
					JsonConvert.DeserializeObject<EpicPaySubscriptionResponse>(respString);
				if (epResp.status.response_code.ToLower() == "received" || epResp.status.response_code.ToLower() == "approved") {
					var sub = epResp.result.subscription;
					// Wallet id is used to update the payment details on a subscription
					return (sub.wallet_id, sub.subscription_id, profile.NextBillDate, "");
				} else {
					// Return error from processor, bubble to user.
					return ("", "", default(DateTime), epResp.status.reason_text);
				}
			} else {
				ErrorHandler.StaticCapture(new Exception($"EpicPay Edit Subscription call failed for profile {profile.Id}"), null, "EpicPay-Edit", new Dictionary<string, string>() {
					{"Response", respString}
				});
				return ("", "", default(DateTime), "There was an unexpected error. Please try again.");
			}
		}

		public async Task<(string, string)> Charge(Profile profile, int amount, BankAccount pay, string decryptKey) {
			// Start date will be the date that the subscription first started.
			var charge = new EpicPayCharge(profile, amount, pay, decryptKey);

			if (BaseUrl.Contains("sandbox")) {
				charge.is_test = true;
			}

			var response = await Client.PostAsync(GetUrl(Urls.Charge), new StringContent(JsonConvert.SerializeObject(charge), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				var epResp =
					JsonConvert.DeserializeObject<EpicPaySubscriptionResponse>(respString);
				if (epResp.status.response_code.ToLower() == "received" || epResp.status.response_code.ToLower() == "approved") {
					var payment = epResp.result.payment;
					return (payment.transaction_id, "");
				} else {
					// Return error from processor, bubble to user.
					return ("", epResp.status.reason_text);
				}
			} else {
				ErrorHandler.StaticCapture(new Exception($"EpicPay Charge call failed for profile {profile.Id}"), null, "EpicPay-Charge", new Dictionary<string, string>() {
					{"Response", respString}
				});
				return ("", "There was an unexpected error. Please try again.");
			}
		}

		public async Task<bool> DeleteSubscription(string subId) {
			// Set subscription to suspended
			var response = await Client.PostAsync(GetUrl(Path.Combine(Urls.DeleteSubscription, subId)), new StringContent("", Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				var epResp =
					JsonConvert.DeserializeObject<EpicPaySubscriptionDeleteResponse>(respString);
				return epResp.result.status.response_code == "Received";
			}
			else {
				ErrorHandler.StaticCapture(new Exception($"EpicPay Sub delete call failed for subId {subId}"), null, "EpicPay-Delete", new Dictionary<string, string>() {
					{"Response", respString}
				});
				return false;
			}
		}

		public async Task<bool> SuspendSubscription(string subId) {
			// Set subscription to suspended
			var response = await Client.PostAsync(GetUrl(Path.Combine(Urls.SuspendSubscription, subId)), new StringContent("", Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				var epResp =
					JsonConvert.DeserializeObject<EpicPaySubscriptionSuspendResponse>(respString);
				return epResp.result.subscription.status == "Suspended";
			} else {
				ErrorHandler.StaticCapture(new Exception($"EpicPay Sub Suspend call failed for subId {subId}"), null, "EpicPay-Suspend", new Dictionary<string, string>() {
					{"Response", respString}
				});
				return false;
			}
		}

		public async Task<string> ChangeSubscriptionBillDate() {
			// Figure prorate amount to charge now

			// Build the subscription object, BE CAREFUL to include all fields. Empty fields will be nulled on EpicPay's side.

			// Edit the subscription and change bill date

			return "";
		}

		public async Task<string> UpdatePaymentMethod() {
			// Create new wallet item

			// Update all user subscriptions to use the new bill date

			// Charge prorate amounts for bill date change for existing subscriptions.

			return "";
		}

		/// <summary>
		/// Get all successful ACH subscription payments
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public async Task<List<Charge>> GetSuccessfulSubscriptionCharges(DateTime start, DateTime end) {
			if (string.IsNullOrWhiteSpace(ReportingBaseUrl)) {
				throw new Exception("Reporting URL was empty");
			}

			var response = await Client.PostAsync(GetReportUrl(Path.Combine(Urls.GetReport, "successfulpayments")), new StringContent(JsonConvert.SerializeObject(new {
				start_date = start.ToString("yyyy-MM-dd"),
				end_date = end.ToString("yyyy-MM-dd")
			}), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			try {
				var payments = JsonConvert.DeserializeObject<EpicPaySuccessfulPaymentsResponse>(respString);

				if (payments.status.response_code != "Received") {
					throw new Exception($"Report returns failed status. {payments.status.response_code}:{payments.status.reason_code}:{payments.status.reason_text}");
				}

				return payments.result.data.Select(x => new Charge() {
					Status = ChargeStatus.Charged,
					Amount = Convert.ToInt32(x.amount * 100),
					Date = x.transaction_date,
					InvoiceId = x.transaction_id,
					ProfileId = x.client_order_id.IsNull() ? Guid.Empty : Guid.Parse(x.client_order_id),
					Description = "Monthly Subscription Charge",
					FirstName = x.customer_first_name,
					LastName =  x.customer_last_name
				}).ToList();
			} catch (Exception e) {
				throw new Exception("Could not deserialize report data", e);
			}

			return null;
		}
		/// <summary>
		/// Get the Last 14 days Rejected ACH Rejects
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public async Task<List<ACHRejects>> GetRejectedACH(DateTime start, DateTime end) {
			if (string.IsNullOrWhiteSpace(ReportingBaseUrl)) {
				throw new Exception("Reporting URL was empty");
			}

			var response = await Client.PostAsync(GetReportUrl(Path.Combine(Urls.GetReport, "achrejects")), new StringContent(JsonConvert.SerializeObject(new {
				start_date = start.ToString("yyyy-MM-dd"),
				end_date = end.ToString("yyyy-MM-dd"),

			}), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();
			try {
				var achRejects = JsonConvert.DeserializeObject<ACHRejectedResponse>(respString, new JsonSerializerSettings {
					DateFormatString = "yyyy-MM-dd"
				});

				if (achRejects.status.response_code != "Received") {
					throw new Exception($"Report returns failed status. {achRejects.status.response_code}:{achRejects.status.reason_code}:{achRejects.status.reason_text}");
				}
				return 	achRejects.result.data.Select(res=>new ACHRejects() {
							accountholder = res.accountholder,
					       account_no = res.account_no,
							addenda = res.addenda,
							Amount = res.Amount,
							effective_date = res.effective_date,
					      	entry_class = res.entry_class,
							entry_desc = res.entry_desc,
							reason_code = res.reason_code,
							reason_text = res.reason_text,
							reported_date = res.reported_date,
							routing_number = res.routing_number,
							settled_date = res.settled_date,
							trxn_id = res.trxn_id
						}).ToList();
					
				
			}
			catch (Exception e) {
				throw new Exception("Could not deserialize report data", e);

			}
			return null;
			}
		/// <summary>
		/// Get all transaction activity between two dates
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public async Task<List<TransactionActivity>> GetTransactionActivity(DateTime start, DateTime end) {
			if (string.IsNullOrWhiteSpace(ReportingBaseUrl)) {
				throw new Exception("Reporting URL was empty");
			}

			var response = await Client.PostAsync(GetReportUrl(Path.Combine(Urls.GetReport, "trxnactivity")), new StringContent(JsonConvert.SerializeObject(new {
				start_date = start.ToString("yyyy-MM-dd"),
				end_date = end.ToString("yyyy-MM-dd")
			}), Encoding.UTF8, "application/json"));

			var respString = await response.Content.ReadAsStringAsync();

			try {
				var payments = JsonConvert.DeserializeObject<EpicPayTransactionActivityResponse>(respString);

				if (payments.status.response_code != "Received") {
					throw new Exception($"Report returns failed status. {payments.status.response_code}:{payments.status.reason_code}:{payments.status.reason_text}");
				}

				return payments.result.data;
			}
			catch (Exception e) {
				throw new Exception("Could not deserialize transaction report data", e);
			}
		}
	}
}
