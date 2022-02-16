using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Services;
using SmartFormat;

namespace Lexvor.Services {
	public class NewActivationEmailModel {
		public string Email { get; set; }
		public string FullName { get; set; }
		public string Plan1Name { get; set; }
		public string Plan2Name { get; set; }
		public string Plan3Name { get; set; }
		public string Plan4Name { get; set; }
		public string Plan1Port { get; set; }
		public string Plan2Port { get; set; }
		public string Plan3Port { get; set; }
		public string Plan4Port { get; set; }
		public string Plan1Accessories { get; set; }
		public string Plan2Accessories { get; set; }
		public string Plan3Accessories { get; set; }
		public string Plan4Accessories { get; set; }
		public string Initiation { get; set; }
		public string Monthly { get; set; }
		public string Total { get; set; }
		public string Prorate { get; set; }
		public string ProrateMessage { get; set; }
		public string OrderTotal { get; set; }
		public string OrderId { get; set; }
		public DateTime Date { get; set; }
	}

	public static class EmailService {
		public static async Task SendEmailConfirmation(IEmailSender emailSender, OtherSettings other, string email, string link) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');

			var blob = await GetProcessedBlobText($"emails/confirm_email.html", other);

			await emailSender.SendEmailAsync(email, "Confirm your email with Lexvor",
				$"Welcome to Lexvor! Please confirm your account by clicking this link: {link}", Smart.Format(blob, link));
		}

		public static async Task SendResetPassword(IEmailSender emailSender, OtherSettings other, string email, string resetPLink) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var blob = await GetProcessedBlobText($"emails/reset_password.html", other);
			await emailSender.SendEmailAsync(email, "Reset Password",
				$"Please reset your password by visiting this link: {resetPLink}.", Smart.Format(blob, resetPLink));
		}

		public static async Task SendIdVerifiedEmail(IEmailSender emailSender, OtherSettings other, string email, string userFullName) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var blob = await GetProcessedBlobText($"emails/idverified.html", other);
			await emailSender.SendEmailAsync(email, "Your identity has been verified!.",
				$"Hello {userFullName}," +
				$"<br />" +
				"Congratulations! Your I.D. has been verified for your service plan here at Lexvor. " +
				"If you selected a device, it will be shipped out soon. If not, your plan will be activated shortly. Full details about " +
				"your order will be available on <a href=\"https://lexvorwireless.com\">our website</a> " +
				"Thank you again for choosing Lexvor Wireless! We hope to continue working with you! " +
				$"<br />" +
				"Lexvor Inc." +
				$"<br />" +
				"customerservice@lexvor.com", blob);
		}

		public static async Task SendIdUpdatesEmail(IEmailSender emailSender, OtherSettings other, string email, string userFullName) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var blob = await GetProcessedBlobText($"emails/idverifyproblem.html", other);
			await emailSender.SendEmailAsync(email, "There was a problem verifying your identity.",
				$"Hello {userFullName}," +
				$"<br />" +
				"Unfortunately we were unable to verify your identity. Please make sure that you uploaded all the required documents " +
				"and that those images contain all the information that we asked for. Please see the Settings page on our website for more specific " +
				"information about what when wrong. <a href=\"https://lexvorwireless.com/Profile/Settings\">More Info.</a> " +
				$"<br />" +
				"Lexvor Inc." +
				$"<br />" +
				"customerservice@lexvor.com", Smart.Format(blob, "https://lexvorwireless.com/profile/settings"));
		}



		public static async Task SendTemplatedEmail(IEmailSender emailSender, OtherSettings other, string templateName, string email, string subject, params object[] mergeFields) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var blob = await GetProcessedBlobText($"emails/{templateName}.html", other);
			await emailSender.SendEmailAsync(email, subject, "", Smart.Format(blob, mergeFields));
		}

		public static Task SendMembershipEmailAsync(IEmailSender emailSender, string name, string email, string level, int numOfLines) {
			return emailSender.SendEmailAsync(email, "Your membership is active. Welcome to Lexvor!", MembershipEmail(name, email, level, numOfLines));
		}

		public static async Task SendNewActivationInvoice(IEmailSender emailSender, OtherSettings other, Profile profile, string userEmail, List<UserPlan> plans, List<UserOrder> orders, LinePricing linePricing, int total, int prorate) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');
			var blob = await GetProcessedBlobText($"emails/new_activation.html", other);
			// Accessory Messages
			var line1order = plans.Count >= 1 ? orders.FirstOrDefault(x => x.UserPlanId == plans.First().Id) : null;
			var line2order = plans.Count >= 2 ? orders.FirstOrDefault(x => x.UserPlanId == plans[1].Id) : null;
			var line3order = plans.Count >= 3 ? orders.FirstOrDefault(x => x.UserPlanId == plans[2].Id) : null;
			var line4order = plans.Count >= 4 ? orders.FirstOrDefault(x => x.UserPlanId == plans[3].Id) : null;

			var model = new NewActivationEmailModel() {
				Email = userEmail,
				FullName = profile.FullName,
				OrderId = orders.Count != 0 ? orders.First().OrderId.ToString() : "",
				Date = orders.Count != 0 ? orders.First().OrderDate : DateTime.UtcNow,
				Plan1Name = $"Line 1: {plans.First().PlanType.Name}",
				Plan1Port = plans.First().PortRequest != null ? $"Porting: {plans.First().PortRequest.MDN}" : "",
				Initiation = (linePricing.InitiationFee / 100).ToString("F"),
				Monthly = (linePricing.MonthlyCost / 100).ToString("F"),
				Total = ((total + orders.Sum(x => x.Total)) / 100).ToString("F")
			};

			model.Plan2Name = plans.Count >= 2 ? $"Line 2: {plans[1].PlanType.Name}" : "";
			model.Plan3Name = plans.Count >= 3 ? $"Line 3: {plans[2].PlanType.Name}" : "";
			model.Plan4Name = plans.Count >= 4 ? $"Line 4: {plans[3].PlanType.Name}" : "";

			model.Plan2Port = plans.Count >= 2 ? plans[1].PortRequest != null ? $"Porting: {plans[1].PortRequest.MDN}" : "" : "";
			model.Plan3Port = plans.Count >= 3 ? plans[2].PortRequest != null ? $"Porting: {plans[2].PortRequest.MDN}" : "" : "";
			model.Plan4Port = plans.Count >= 4 ? plans[3].PortRequest != null ? $"Porting: {plans[3].PortRequest.MDN}" : "" : "";
			model.Plan1Accessories = line1order != null ? $"Accessories: {line1order.Accessory1?.Accessory ?? ""}, {line1order.Accessory2?.Accessory ?? ""}, {line1order.Accessory3?.Accessory ?? ""}" : "";
			model.Plan2Accessories = line2order != null ? $"Accessories: {line2order.Accessory1?.Accessory ?? ""}, {line2order.Accessory2?.Accessory ?? ""}, {line2order.Accessory3?.Accessory ?? ""}" : "";
			model.Plan3Accessories = line3order != null ? $"Accessories: {line3order.Accessory1?.Accessory ?? ""}, {line3order.Accessory2?.Accessory ?? ""}, {line3order.Accessory3?.Accessory ?? ""}" : "";
			model.Plan4Accessories = line4order != null ? $"Accessories: {line4order.Accessory1?.Accessory ?? ""}, {line4order.Accessory2?.Accessory ?? ""}, {line4order.Accessory3?.Accessory ?? ""}" : "";
			model.Prorate = prorate > 0 ? $"${(prorate / 100).ToString("F")}" : "";
			model.ProrateMessage = prorate > 0 ? "Prorate:" : "";
			model.OrderTotal = (orders.Sum(x => x.Total) / 100).ToString("F");

			await emailSender.SendEmailAsync(userEmail, "Receipt for your Lexvor Plan Activation",
				"Please enable HTML viewing to see this message or login to your account at https://lexvorwireless.com", Smart.Format(blob, model));
		}

		private static string MembershipEmail(string name, string email, string level, int numOfLines) => $@"
            Hi {name},
            
            Thank you for signing up for Lexvor Wireless. You will be notified as soon as your identity and other information provided has been verified.

            Below are details about your membership account. A receipt for your order will be coming shortly.

            Account: {name} ({email})
            Membership Level: {level}
			Number of Lines: {numOfLines}

            Log in to your membership account here: https://lexvorwireless.com/

            If you have any questions regarding your plan, please feel free to email us at customerservice@lexvor.com

            Lexvor Inc.
            (866) 996-2281
            customerservice@lexvor.com";

		public static async Task SendUserPushEmail(IEmailSender emailSender, OtherSettings other, string email) {
			Smart.Default.Parser.UseAlternativeEscapeChar('~');

			var blob = await GetProcessedBlobText($"emails/newuserpush.html", other);

			await emailSender.SendEmailAsync(email, "Lexvor here! We noticed you dropped off the map...",
				$"We recently redesigned our on-boarding flow to be clearer and easier to complete. Let's see if we can get you signed up. https://lexvorwireless.com", blob);
		}

		// Admin emails
		public static Task SendIDVerifyAdminEmail(IEmailSender emailSender, string email, string userEmail) {
			return emailSender.SendEmailAsync(email, "New user identity verification documents.",
				$"There is a new user ({userEmail}) that uploaded identity verification documents. Log in to view the documents.<a href=\"http://lexvorwireless.com/admin/admin\">Log In</a>");
		}
		public static Task SendNewDeviceRequestEmail(IEmailSender emailSender, string email, string userEmail, string deviceName, string carrier) {
			return emailSender.SendEmailAsync(email, "New device request.",
				$"There is a new device request for {userEmail}. Device: {deviceName}. Carrier: {carrier} <a href=\"http://lexvorwireless.com/admin/admin\">Log In</a>");
		}
		public static Task SendDeviceReturnEmail(IEmailSender emailSender, string email, string userEmail, string deviceName) {
			return emailSender.SendEmailAsync(email, "New device return request.",
				$"There is a new device return for {userEmail}. Device: {deviceName}. <a href=\"http://lexvorwireless.com/admin/admin\">Log In</a>");
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
}
