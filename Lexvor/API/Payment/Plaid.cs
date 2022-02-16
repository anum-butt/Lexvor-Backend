using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Acklann.Plaid;
using Acklann.Plaid.Balance;
using Acklann.Plaid.Identity;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Newtonsoft.Json;
using Environment = Acklann.Plaid.Environment;

namespace Lexvor.API.Payment {
	public class Plaid {
		private PlaidClient Client { get; set; }

		public string PlaidSecret { get; set; }
		public string PlaidClientId { get; set; }
		public string Env { get; set; }

		public Plaid(string secret, string clientId, string env) {
			PlaidSecret = secret;
			PlaidClientId = clientId;
			Env = env;
			Client = new PlaidClient(Enum.Parse<Environment>(env));
		}

		public async Task<List<Identity>> GetIdentity(string accessToken) {
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Plaid-Version", "2019-05-29");

			// SDK broken for Identity call
			var response = await client.PostAsync($"https://{Env}.plaid.com/identity/get", new StringContent(JsonConvert.SerializeObject(new {
				client_id = PlaidClientId,
				secret = PlaidSecret,
				access_token = accessToken
			}), Encoding.UTF8, "application/json"));

			var content = await response.Content.ReadAsStringAsync();
			var identityResponse = JsonConvert.DeserializeObject<PlaidIdentityResponse>(content);

			// If we have a name, fill it. If profile has a name already, do nothing.
			var identities = new List<Identity>();
			var owners = identityResponse.accounts.SelectMany(x => x.owners).ToList();

			foreach (var owner in owners) {
				var names = owner.names.Select(x => x.Replace(" or", "")).ToList();
				foreach (var idenName in names) {
					var identity = new Identity() {
						FirstName = idenName.Split(" ")[0],
						// Get name remainder and add back
						LastName = string.Join(" ", idenName.Split("").Skip(1)),
						AuthenticityConfidence = 1,
						// TODO make this an enum
						DocumentType = "bank-account",
						LastUpdated = DateTime.Now,
						Source = IndentitySource.Plaid
					};

					// Optional data
					try {
						var address = owner.addresses.FirstOrDefault();
						if (address != null) {
							identity.Address = new Objects.User.Address() {
								City = address.data.city,
								Provence = address.data.region,
								Line1 = address.data.street,
								PostalCode = address.data.postal_code
							};

						}
					} catch (Exception e) {
						Console.WriteLine(e);
						throw;
					}

					identities.Add(identity);
				}
			}

			return identities;
		}

		public async Task<decimal?> GetLastBalance(string accessToken, string plaidAccountId) {
			var balance = await Client.FetchAccountBalanceAsync(new GetBalanceRequest() {
				AccessToken = accessToken,
				ClientId = PlaidClientId,
				Secret = PlaidSecret
			});

			if (balance?.Accounts == null) {
				throw new Exception("Balance Check call returned no data.", new Exception(JsonConvert.SerializeObject(balance)));
			}
			if (balance.Accounts.Length == 0) {
				throw new Exception("Balance Check call returned no accounts.", new Exception(JsonConvert.SerializeObject(balance)));
			}

			var account = balance.Accounts.FirstOrDefault(x => x.Id == plaidAccountId);
			if (account != null) {
				return Math.Round(account.Balance.Available ?? account.Balance.Current, 2);
			} else {
				account = balance.Accounts.First();
				if (account.Balance.Available == null) {
					throw new Exception("Balance Check call returned no balance for account (Bank Redaction).", new Exception(JsonConvert.SerializeObject(balance)));
				}
				return Math.Round(account.Balance.Available ?? account.Balance.Current, 2);
			}
		}

		public async Task<List<string>> GetAccountInfo(string accessToken) {
			var accInfo = await Client.FetchAccountAsync(new GetAccountRequest() {
				AccessToken = accessToken,
				ClientId = PlaidClientId,
				Secret = PlaidSecret
			});

			if (accInfo.IsSuccessStatusCode) {
				return accInfo.Accounts.Where(x => x.SubType == "checking").Select(x => x.Mask).ToList();
			} else {
				return null;
			}
		}
	}
}
