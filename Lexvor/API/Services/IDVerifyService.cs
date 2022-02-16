using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Acklann.Plaid.Entity;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.API.Vouched;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Identity = Lexvor.API.Objects.User.Identity;

namespace Lexvor.API.Services
{
    public static class IDVerifyService
    {
	    public static async Task<string> UploadIDDocument(OtherSettings other, IFormFile upload, string path) {
			var data = new byte[upload.Length];
		    var stream = new MemoryStream();
		    await upload.CopyToAsync(stream);
		    data = stream.ToArray();
		    var ext = System.IO.Path.GetExtension(upload.FileName);

		    var fullpath = $"{path}{ext}";

			await BlobService.UploadBlob(data, fullpath, other, true);

		    return fullpath;
	    }

	    public static async Task<Identity> RunVerification(DbContext context, OtherSettings other, IdentityDocument document, byte[] fileData, Profile profile) {
		    var vouched = new VouchedApi(other.VouchedPublicKey, other.VouchedPrivateKey);
		    VouchedResponse vouchedResponse = null;
		    try {
			    vouchedResponse = await vouched.SubmitVerification("", fileData);
		    }
		    catch (Exception e) {
			    ErrorHandler.Capture(other.SentryDSN, e, area: "Vouched-API");
				throw;
		    }

			// Save the raw response
			try {
				context.Add(new WebhookResponseObject() {
					ObjectType = "IdentityDocument",
					ObjectId = document.Id.ToString(),
					Received = DateTime.UtcNow,
					ReceivedAction = "Id Authenticity",
					Text = JsonConvert.SerializeObject(vouchedResponse)
				});
				await context.SaveChangesAsync();
			}
			catch (Exception e) {
				ErrorHandler.Capture(other.SentryDSN, e, area: "Id-Authenticity-Check");
			}

		    if (vouchedResponse.completed == "true" && vouchedResponse.result.success) {
			    var result = vouchedResponse.result;
				var firstName = result.firstName.Split(" ").ElementAtOrDefault(0);
				var middleName = result.firstName.Split(" ").ElementAtOrDefault(1);
				// TODO add errors from ID verify to the Identity object and surface them to the UI.
				return new Identity() {
					Profile = profile,
					IdentityDocument = document,
					FirstName = firstName,
					MiddleName = String.IsNullOrEmpty(middleName) ? null : middleName,
					LastName = result.lastName,
					AuthenticityConfidence = result.confidences.idQuality ?? 0,
					BirthDate = result.birthDate.Value,
					ExpiryDate = result.expireDate.Value,
					DocumentType = result.type,
					Address = result.idAddress != null ? new Objects.User.Address() {
						Line1 = $"{result.idAddress.streetNumber} {result.idAddress.street}",
						City = result.idAddress.city,
						Provence = result.idAddress.state,
						PostalCode = result.idAddress.postalCode,
						Source = AddressSource.Vouched
					} : null,
					Source = IndentitySource.Vouched,
					LastUpdated = DateTime.Now
				};
		    }
		    else {
				throw new Exception($"ID Verify failed. {vouchedResponse.errors?.FirstOrDefault()?.message ?? "See inner exception"}.", new Exception(JsonConvert.SerializeObject(vouchedResponse)));
		    }
	    }

		public static async Task RunVerificationAsync(DbContext context, OtherSettings other, IdentityDocument document, byte[] fileData, Profile profile, string callbackUrl = "") {
			var vouched = new VouchedApi(other.VouchedPublicKey, other.VouchedPrivateKey);
			var vouchedResponse = await vouched.SubmitVerification(callbackUrl, fileData);
			// Save the raw response
			try {
				context.Add(new WebhookResponseObject() {
					ObjectType = "IdentityDocument",
					ObjectId = document.Id.ToString(),
					Received = DateTime.UtcNow,
					ReceivedAction = "Id Authenticity",
					Text = JsonConvert.SerializeObject(vouchedResponse)
				});
				await context.SaveChangesAsync();
			} catch (Exception e) {
				ErrorHandler.Capture(other.SentryDSN, e, area: "Id Authenticity Check");
			}
		}

		public static async Task<Identity> ParseCallbackResponse(OtherSettings other, IdentityDocument document, Profile profile, string body) {
			var vouched = new VouchedApi(other.VouchedPublicKey, other.VouchedPrivateKey);
			var vouchedResponse = vouched.ParseResponse(body);

			if (vouchedResponse.completed == "true") {
				var result = vouchedResponse.result;
				var firstName = result.firstName.Split(" ").ElementAtOrDefault(0);
				var middleName = result.firstName.Split(" ").ElementAtOrDefault(1);
				// TODO add errors from ID verify to the Identity object and surface them to the UI.
				return new Identity() {
					Profile = profile,
					IdentityDocument = document,
					FirstName = firstName,
					MiddleName = String.IsNullOrEmpty(middleName) ? null : middleName,
					LastName = result.lastName,
					AuthenticityConfidence = result.confidences.idQuality ?? 0,
					BirthDate = result.birthDate.Value,
					ExpiryDate = result.expireDate.Value,
					DocumentType = result.type,
					Address = result.idAddress != null ? new Objects.User.Address() {
						Line1 = $"{result.idAddress.streetNumber} {result.idAddress.street}",
						City = result.idAddress.city,
						Provence = result.idAddress.state,
						PostalCode = result.idAddress.postalCode,
						Source = AddressSource.Vouched
					} : null,
					LastUpdated = DateTime.Now,
					Source = IndentitySource.Vouched
				};
			} else {
				throw new Exception("ID Verify failed. See response in inner exception.", new Exception(JsonConvert.SerializeObject(vouchedResponse)));
			}
		}
	}
}
