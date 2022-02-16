using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.API.Payment {
	public class EpicPayCharge {
		/// <summary>
		///  In cents
		/// </summary>
		public int amount { get; set; }

		public string currency { get; set; }
		public string method { get; set; }
		public string transaction_type { get; set; }
		public string sec_code { get; set; }
		public string client_customer_id { get; set; }
		public string entry_description { get; set; }
		public bool is_test { get; set; }

		/// <summary>
		/// Will only be filled on a response
		/// </summary>
		public string transaction_id { get; set; }


		public EpicPayBankAccount bank_account { get; set; }

		public EpicPayCharge() {

		}

		/// <summary>
		/// Charge an account once.
		/// </summary>
		/// <param name="amountInCents"></param>
		/// <param name="nextPayment"></param>
		public EpicPayCharge(Profile profile, int amountInCents, BankAccount bank, string decryptKey) {
			amount = amountInCents;
			currency = "usd";
			method = "echeck";
			transaction_type = "Sale";
			sec_code = "WEB";
			bank_account = new EpicPayBankAccount() {
				account_type = "personal_checking",
				account_holder_name = $"{bank.AccountFirstName} {bank.AccountLastName}",
				account_number = StringCipher.DecryptString(bank.AccountNumber, decryptKey),
				routing_number = bank.RoutingNumber
			};
			client_customer_id = profile.Id.ToString();
			entry_description = "LEXVORWRLS";
			is_test = false;
		}
	}
}
