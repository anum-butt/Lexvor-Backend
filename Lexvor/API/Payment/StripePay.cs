using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stripe;

namespace Lexvor.API.Payment {
	public class StripePay {

		public string stripeApiKey { get; set; }
		public StripePay(string apiKey) {
			stripeApiKey = apiKey;
		}
		public Stripe.Charge StripePayAttempt(string stripeToken, string stripeEmail,int amount) {
			var customerService = new Stripe.CustomerService();
			var existing = customerService.List(new CustomerListOptions() {
				Email = stripeEmail
			});
			Stripe.Customer customer;
			if (existing == null) {
				customer = customerService.Create(new CustomerCreateOptions() {
					Email = stripeEmail,
					Source = stripeToken
				});
			}
			else {
				customer = existing.First();
			}
			
			Stripe.StripeConfiguration.ApiKey = stripeApiKey;

			var myCharge = new Stripe.ChargeCreateOptions {
				Currency = "USD",
				ReceiptEmail = stripeEmail,
				Description = "Lexvor Wireless Plan",
				Source = stripeToken,
				Capture = true,
				Amount = amount
			};
			var chargeService = new Stripe.ChargeService();
			var stripeCharge = chargeService.Create(myCharge);

			// Make the plan
			var planService = new PlanService();
			var plan = planService.Create(new PlanCreateOptions() {
				Amount = amount,
				BillingScheme = "per_unit",
				Interval = "month",
				Nickname = $"Plan for {stripeEmail}",
				Product = "prod_JXt1KE9RPyxr59",
			});

			var subscriptionService = new SubscriptionService();
			var sub = subscriptionService.Create(new SubscriptionCreateOptions() {
				Customer = customer.Id,
				Items = new List<SubscriptionItemOptions>() {
					new SubscriptionItemOptions() {
						Plan = plan.Id,
						Quantity = 1,
					}
				},
			});

			return stripeCharge;
		}
	}
}
