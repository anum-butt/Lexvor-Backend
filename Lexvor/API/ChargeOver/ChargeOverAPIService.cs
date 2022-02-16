using System;
using System.Collections.Generic;
using System.Linq;
using ChargeOver.Wrapper.Models;
using ChargeOver.Wrapper.Services;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;

namespace Lexvor.API.ChargeOver {
    public class ChargeOverAPIService {
        private ChargeOverApiConfiguration Config;
        private readonly OtherSettings _other;

        public ChargeOverAPIService(string username, string password, OtherSettings other) {
            Config = new ChargeOverApiConfiguration(other.ChargeOverEndpoint, username, password, "Basic");
            _other = other;
        }

        private int CreateCustomer(Profile profile, string email) {
            var customerService = new CustomersService(Config);
            var customer = customerService.CreateCustomer(new Customer() {
                Company = profile.FullName,
                SuperUserFirstName = profile.FirstName,
                SuperUserLastName = profile.LastName,
                SuperuserPhone = profile.Phone,
                SuperUserEmail = email
            });

            return customer.Id;
        }

        public int GetCustomer(Profile profile, string email) {
            var customerService = new CustomersService(Config);
            int customerId = new int();

            try {
                var response = customerService.QueryCustomers(new[] { String.Format("superuser_email:EQUALS:{0}", email) });

                if (response.Status == "OK") {
                    var customer = response.Response.FirstOrDefault(c => c.SuperUserEmail == email);

                    if (customer != null) {
                        customerId = customer.CustomerId.Value;
                    } else {
                        customerId = CreateCustomer(profile, email);
                    }
                }
            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, "ChargeOver-GetCustomer");
            }

            return customerId;
        }

        public int CreateSubscription(Profile profile, PlanType plan, int adjustedInitiation, int adjustedMonthly, int customerId) {
            var subscriptionService = new SubscriptionsService(Config);
            int subId = new int();

            try {
                var planTypes = GetPlanTypes();

                var subscription = new Subscription {
                    CustomerId = customerId,
                    BillAddr1 = profile.BillingAddress.Line1,
                    BillCity = profile.BillingAddress.City,
                    BillPostalCode = profile.BillingAddress.PostalCode,
                    BillCountry = "United States",
                    PayCycle = "mon",
                    PayMethod = "ach"
                };

                var line1 = new SubscriptionLineItem {
                    ItemId = planTypes[plan.Name],
                    ItemName = plan.Name,
                    SubscribeDatetime = DateTime.UtcNow,
                    Tierset = new Tierset {
                        Base = adjustedMonthly,
                        Setup = adjustedInitiation,
                    }
                };

                subscription.LineItems = new[] { line1 };

                var response = subscriptionService.CreateSubscription(subscription);

                if (response.Status != "OK") {
                    throw new Exception($"Create Subscription Failed {customerId}. {response.Message}");
                }

                // Suspend indefintely until we have confirmation and a payment method
                subscriptionService.SuspendSubscription(response.Id);

                subId = response.Id;
            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, "ChargeOver-CreateSubscription");
            }

            return subId;
        }

        public int ConfirmSubscription(int subId) {
            var subscriptionService = new SubscriptionsService(Config);
            var success = subscriptionService.UnsuspendSubscription(subId);

            if (!success.Response) {
                ErrorHandler.Capture(_other.SentryDSN, new Exception($"Confirm Subscription Failed {subId}"));
                return 0;
            }

            var invoice = subscriptionService.InvoiceSubscriptionNow(subId);

            if (invoice.Code != 200) {
                ErrorHandler.Capture(_other.SentryDSN, new Exception($"Invoice Subscription Failed {subId}"));
                return 0;
            }

            return invoice.Id;
        }

        public bool CreateACHAccount(int customerId, BankAccount pay) {
            var achService = new ACHeCheckAccountsService(Config);

            try {
                var request = new StoreACHAccount {
                    CustomerId = customerId,
                    Number = pay.AccountNumber,
                    Routing = pay.RoutingNumber,
                    //Name = pay.AccountName,
                    Bank = pay.Bank
                };

                var response = achService.StoreACHAccount(request);

                if (response.Status != "OK") {
                    ErrorHandler.Capture(_other.SentryDSN, new Exception($"ACH Save failed. {response.Message}"), "ChargeOver-CreateACHAccount");
                    return false;
                }

                return true;
            } catch (Exception e) {
                ErrorHandler.Capture(_other.SentryDSN, e, "ChargeOver-CreateACHAccount");
                return false;
            }
        }

        public Dictionary<string, int> GetPlanTypes() {
            var plans = new Dictionary<string, int>();

            var planService = new ItemsService(Config);
            try {
                var response = planService.QueryItems(new[] { "where=item_type:EQUALS:service" });
                if (response.Status == "OK") {
                    response.Response.Where(x => x.Custom1 == "Subscription").ToList().ForEach(x => {
                        if (x.ItemId.HasValue) {
                            plans.Add(x.Name, x.ItemId.Value);
                        }
                    });
                }
            } catch (Exception e) {
                ErrorHandler.StaticCapture(e, area: "ChargeOver-Plans");
            }

            return plans;
        }

        public Dictionary<string, int> GetPlanAddons() {
            var plans = new Dictionary<string, int>();

            var planService = new ItemsService(Config);
            var response = planService.QueryItems(new[] { "where=item_type:EQUALS:service" });

            if (response.Status == "OK") {
                response.Response.Where(x => x.Custom1 == "Addon").ToList().ForEach(x => {
                    if (x.ItemId.HasValue) {
                        plans.Add(x.Name, x.ItemId.Value);
                    }
                });
            }

            return plans;
        }
    }
}
