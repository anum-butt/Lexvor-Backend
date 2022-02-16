using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.API.Payment {
	public class EpicPaySubscription {
		/// <summary>
		///  In cents
		/// </summary>
		public int amount { get; set; }

		public string currency { get; set; }
		public string method { get; set; }
		public string next_payment_date { get; set; }
		public string frequency { get; set; }
		public int period { get; set; }
		public string sec_code { get; set; }
		public string client_customer_id { get; set; }

		// Our ProfileId in order to map this sub back to a customer in reports.
		public string client_order_id { get; set; }
		public string entry_description { get; set; }

		/// <summary>
		/// Will only be filled on a response
		/// </summary>
		public string customer_id { get; set; }
		/// <summary>
		/// Will only be filled on a response
		/// </summary>
		public string subscription_id { get; set; }
		/// <summary>
		/// Will only be filled on a response
		/// </summary>
		public string wallet_id { get; set; }

		public string callback_url { get; set; }


		public EpicPayBankAccount bank_account { get; set; }

		public EpicPayCustomerAddress customer_address { get; set; }

		public EpicPaySubscription() {
			
		}

		/// <summary>
		/// Monthly Subscription with ECheck method/ Only use this constructor for a new customer.
		/// </summary>
		/// <param name="amountInCents"></param>
		/// <param name="nextPayment"></param>
		public EpicPaySubscription(string userEmail, Profile profile, int amountInCents, DateTime nextPayment, BankAccount bank, string decryptKey, string callbackUrl) {
			if (profile.BillingAddress == null) {
				throw new MissingFieldException("Profile was missing Billing Address");
			}

			amount = amountInCents;
			currency = "usd";
			method = "echeck";
			next_payment_date = nextPayment.ToString("yyyy-MM-dd");
			frequency = "every_n_months";
			period = 1;
			sec_code = "WEB";
			bank_account = new EpicPayBankAccount() {
				account_type = "personal_checking",
				account_holder_name = $"{bank.AccountFirstName} {bank.AccountLastName}",
				account_number = StringCipher.DecryptString(bank.AccountNumber, decryptKey),
				routing_number = bank.RoutingNumber
			};
			entry_description = "LEXVORWRLS";
			customer_address = new EpicPayCustomerAddress() {
				first_name = profile.FirstName,
				last_name = profile.LastName,
				email = userEmail
			};
			client_order_id = profile.Id.ToString();
			client_customer_id = profile.Id.ToString();
			callback_url = callbackUrl;
		}
	}
}
