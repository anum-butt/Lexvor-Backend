using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Payment {

	public class PlaidIdentityResponse {
		public List<Account> accounts { get; set; }
		public Item item { get; set; }
		public string request_id { get; set; }
	}

	public class Data {
		public string city { get; set; }
		public object country { get; set; }
		public string postal_code { get; set; }
		public string region { get; set; }
		public string street { get; set; }
	}

	public class Address {
		public Data data { get; set; }
		public bool primary { get; set; }
	}

	public class Email {
		public string data { get; set; }
		public bool primary { get; set; }
		public string type { get; set; }
	}

	public class PhoneNumber {
		public string data { get; set; }
		public bool primary { get; set; }
		public string type { get; set; }
	}

	public class Owner {
		public List<Address> addresses { get; set; }
		public List<Email> emails { get; set; }
		public List<string> names { get; set; }
		public List<PhoneNumber> phone_numbers { get; set; }
	}

	public class Account {
		public string account_id { get; set; }
		public string mask { get; set; }
		public string name { get; set; }
		public object official_name { get; set; }
		public List<Owner> owners { get; set; }
		public string subtype { get; set; }
		public string type { get; set; }
	}

	public class Item {
		public List<string> available_products { get; set; }
		public List<string> billed_products { get; set; }
		public object consent_expiration_time { get; set; }
		public object error { get; set; }
		public string institution_id { get; set; }
		public string item_id { get; set; }
		public string webhook { get; set; }
	}
}
