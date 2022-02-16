using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartFormat;

namespace Lexvor.Tests {
	[TestClass]
	public class EmailSenderTests : BaseTest {
		[TestMethod]
		public async Task SmartFormatTest() {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');

			var blob = await GetProcessedBlobText($"emails/confirm_email.html", other);

			var result = Smart.Format(blob, "TESTTESTETEST");
		}

		[TestMethod]
		public async Task IDVeryErrorEmail() {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var link = "https://lexvor.com/profile/settings";
			var blob = await GetProcessedBlobText($"emails/idverifiyproblem.html", other);
			var result = Smart.Format(blob, link);

			Assert.IsTrue(result.Contains(link), "Email did not contain");
		}

		[TestMethod]
		public async Task NewActivationEmailTest() {
			var planId1 = Guid.NewGuid();
			var planId2 = Guid.NewGuid();
			var orderId = Guid.NewGuid();

			await EmailService.SendNewActivationInvoice(
				new FakeSender(),
				other,
				new Profile() {
					FirstName = "John",
					LastName = "Doe"
				},
				"john@example.com",
				new List<UserPlan>() {
					new UserPlan() {
						Id = planId1,
						PlanType = new PlanType() {
							Name = "My Plan"
						},
						PortRequest = new PortRequest() {
							MDN = "1239995555"
						}
					},
					new UserPlan() {
						Id = planId2,
						PlanType = new PlanType() {
							Name = "My Plan"
						}
					}
				},
				new List<UserOrder>() {
					new UserOrder() {
						UserPlanId = planId1,
						Accessory1 = new UserAccessory() {
							Accessory = "Phone case"
						},
						Accessory2 = new UserAccessory() {
							Accessory = "Air Pods"
						},
						OrderDate = DateTime.Now,
						OrderId = orderId
					},
					new UserOrder() {
						UserPlanId = planId2,
						Accessory2 = new UserAccessory() {
							Accessory = "Air Pods"
						},
						Accessory3 = new UserAccessory() {
							Accessory = "Protector"
						},
						OrderDate = DateTime.Now,
						OrderId = orderId
					}
				}, 
				new LinePricing() {
					MonthlyCost = 9900,
					InitiationFee = 29900
				},
				1400,
				0);
		}

		
		private static async Task<string> GetProcessedBlobText(string blobName, OtherSettings other) {
			var blob = await BlobService.DownloadBlobAsText(blobName, other);
			// Run replace. Take out tokens, replace all braces, replace tokens
			blob = blob.Replace("{{", "*~").Replace("}}", "~*");
			blob = blob.Replace("{", "~{").Replace("}", "~}");
			blob = blob.Replace("*~", "{").Replace("~*", "}");

			return blob;
		}
	}

	public class FakeSender : IEmailSender {
		public Task SendEmailAsync(string email, string subject, string plainMessage, string htmlMessage = "", string replyTo = "") {
			Debug.WriteLine(htmlMessage);
			return null;
		}

		public Task SendEmailAsync(string[] emails, string subject, string plainMessage, string htmlMessage = "", string replyTo = "") {
			Debug.WriteLine(htmlMessage);
			return null;
		}

		public Task SendEmailAsync(string emails, string subject, string plainMessage, string attachmentURL, bool isAttachment,byte[]blob, string htmlMessage = "", string replyTo = "") {
			Debug.WriteLine(htmlMessage);
			return null;
		}
	}
}
