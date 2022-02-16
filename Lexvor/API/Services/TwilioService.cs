using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V1;

namespace Lexvor.Services {
	public static class TwilioService {

		public static async Task<UserComm> Send(OtherSettings other, ApplicationDbContext context, string callbackUrl, Guid profileId, string MDN, string body) {
			TwilioClient.Init(other.TwilioAccountSid, other.TwilioAuthToken);

			try {
				var newMessage = await MessageResource.CreateAsync(
					body: body,
					from: new Twilio.Types.PhoneNumber(other.TwilioNumber),
					statusCallback: new Uri(callbackUrl),
					to: new Twilio.Types.PhoneNumber(MDN)
				);

				var storedMessage = await context.UserComms.FirstOrDefaultAsync(x => x.ExternalId == newMessage.Sid);

				if (storedMessage != null) {
					storedMessage.ProfileId = profileId;
					storedMessage.Sent = DateTime.UtcNow;
					storedMessage.Recipient = MDN;
					storedMessage.MessageType = MessageType.SMS;
					storedMessage.Subject = "";
					storedMessage.Content = body;
				} else {
					await context.UserComms.AddAsync(new UserComm() {
						ProfileId = profileId,
						Sent = DateTime.UtcNow,
						Recipient = MDN,
						MessageType = MessageType.SMS,
						Subject = "",
						Content = body,
						ExternalId = newMessage.Sid
					});
				}

				await context.SaveChangesAsync();

				return storedMessage;
			} catch (ApiException e) {
				ErrorHandler.Capture(other.SentryDSN, e, $"Twilio-Send");

				return null;
			}
		}

		public static async Task<string> GetMessageStatus(OtherSettings other, ApplicationDbContext context, Guid messageId) {

			var message = await context.UserComms.FirstOrDefaultAsync(x => x.Id == messageId);

			return message.Status;
		}


		public static async Task<Identity> GetNameFromNumber(OtherSettings other, string MDN) {
			TwilioClient.Init(other.TwilioAccountSid, other.TwilioAuthToken);

			try {
				var type = new List<string> {
					"caller-name"
				};

				var phoneNumber = PhoneNumberResource.Fetch(
					type: type,
					pathPhoneNumber: new Twilio.Types.PhoneNumber($"+1{StaticUtils.NumericStrip(MDN)}")
				);

				if(phoneNumber.CallerName != null && phoneNumber.CallerName.ContainsKey("caller_name")) {
					var name = phoneNumber.CallerName["caller_name"];
					if(name != null) {
						return new Identity() {
							FirstName = name.Split(" ").First(),
							LastName = string.Join(" ", name.Split(" ").Skip(1)),
							LastUpdated = DateTime.Now,
							Source = IndentitySource.Twilio,
							DocumentType = "name-on-carrier-account"
						};
					} else {
						return null;
					}
				}
			} catch (ApiException e) {
				ErrorHandler.Capture(other.SentryDSN, e, $"Twilio-Caller-Name");
			}

			return null;
		}
	}
}
