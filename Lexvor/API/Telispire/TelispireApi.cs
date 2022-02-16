﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Lexvor.Extensions;
using Newtonsoft.Json;
using TelispirePostPaidAPI;

namespace Lexvor.API.Telispire {
	public class TelispireApi {
		public string ApiUser { get; set; }
		public string ApiPassword { get; set; }

		public MdnServicesSoapClient Client { get; set; }

		private InspectorBehavior _requestInterceptor = new InspectorBehavior();

		/// <summary>
		/// IMPORTANT: Api Password cannot have & in it
		/// </summary>
		/// <param name="user"></param>
		/// <param name="password"></param>
		public TelispireApi(string user, string password) {
			ApiUser = user;
			ApiPassword = password;
			Client = new MdnServicesSoapClient(MdnServicesSoapClient.EndpointConfiguration.MdnServicesSoap12, "https://wirelessprovisioning.com/desktopmodules/telispire.webservices/mdnservices.asmx");
			Client.Endpoint.EndpointBehaviors.Add(_requestInterceptor);
		}

		public async Task<bool> SuspendMDN(string mdn) {

			var request = new SuspendMDNRequest() {
				password = ApiPassword,
				username = ApiUser,
				MDN = mdn
			};

			SuspendMDNResponse response = null;
			try {
				response = await Client.SuspendMDNAsync(request);
				if (response.SuspendMDNResult != null) {
					return true;
				}
				else {
					throw new Exception("Error while disconnecting mdn");
				}
			}
			catch (Exception e) {
				throw;
			}
		}
		public async Task<string> CreateCustomer(string name, string addressline1, string city, string state, string zip) {
			var req = new CreateCustomerRequest() {
				Customer = new Info() {
					FullName = name,
					HomeAddress1 = addressline1,
					HomeCity = city,
					HomeState = state,
					HomeZip = zip,
					ShipAddress1 = addressline1,
					ShipCity = city,
					ShipState = state,
					ShipZip = zip,
					BillingCycle = "2"
				},
				password = ApiPassword,
				username = ApiUser
			};

			CreateCustomerResponse resp = null;
			try {
				resp = await Client.CreateCustomerAsync(req);
				if (resp.Customer != null) {
					return resp.Customer.AccountNumber;
				}
				else {
					throw new Exception("Error retrieving Customer Info from CreateCustomer call");
				}
			}
			catch (Exception e) {
				throw;
			}
		}

		/// <summary>
		/// All service lines end on the 16th of each month, so all billing needs to prorate and move to the first of the month.
		/// This ensures that we get paid well before we get charged by Telispire.
		/// </summary>
		/// <param name="callBackUrl"></param>
		/// <param name="zip"></param>
		/// <param name="imei"></param>
		/// <param name="icc"></param>
		/// <param name="internalPlanId"></param>
		/// <returns></returns>
		public async Task<Wireless> StartActivateServiceLine(string accountNumber, string callBackUrl, string zip, string imei, string simIcc, Guid internalPlanId, string externalPlanId) {
			//var req = $@"<?xml version=""1.0"" encoding=""utf-16""?>
			//    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
			//      <soap:Body>
			//        <VerizonPrepaid_Activate_Async xmlns=""urn:telispire:MdnServices"">
			//			<username>{ApiUser}</username>
			//			<password>{ApiPassword}</password>
			//			<npanxx>{zip}</npanxx>
			//			<esn>{imei}</esn>
			//			<icc>{simIcc}</icc>
			//			<activationPlanId>{14720}</activationPlanId>
			//			<autopayPlanId>{14720}</autopayPlanId>
			//			<cycleDay>{DateTime.UtcNow.Day}</cycleDay>
			//			<verizonAccount>1166</verizonAccount>
			//			<referenceNumber>{internalPlanId}</referenceNumber>
			//			<callbackURL>{callBackUrl}</callbackURL>
			//		</VerizonPrepaid_Activate_Async> 
			//      </soap:Body>
			//    </soap:Envelope>
			//";

			//var client = new HttpClient();
			//var request = new HttpRequestMessage(HttpMethod.Post, "https://wirelessprovisioning.com/desktopmodules/telispire.webservices/mdnservices.asmx?op=VerizonPrepaid_Activate_Async") {
			//	Content = new StringContent(req, Encoding.UTF8, "text/xml")
			//};
			//request.Headers.Add("SOAPAction", "\"urn:telispire:MdnServices/VerizonPrepaid_Activate_Async\"");
			////var httpResponse = await client.SendAsync(request);

			//var content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><VerizonPrepaid_Activate_AsyncResponse xmlns=\"urn:telispire:MdnServices\"><VerizonPrepaid_Activate_AsyncResult><ICC>89148000005248511511</ICC><ActivatedByMDNSwap>false</ActivatedByMDNSwap><DisconnectedByMDNSwap>false</DisconnectedByMDNSwap><EarlyTerminationFee>0</EarlyTerminationFee><Deposit>0</Deposit><ActivatedBy>TSP_AGENTS_FYINANCE_API</ActivatedBy><IsPrepaid>true</IsPrepaid><Id>4342184</Id><MDN>4342184</MDN><RatePlan>FLEX_VZ</RatePlan><ActivationDate>0001-01-01T00:00:00</ActivationDate><DeactivationDate>0001-01-01T00:00:00</DeactivationDate><Status>1</Status><ESN>357271091232906</ESN><AccountNumber>16085984</AccountNumber><InventoryId>0</InventoryId><Orderid>0</Orderid><Pin>TXN7C5F787B3</Pin><Carrier>Verizon</Carrier><WarrantyExpires>0001-01-01T00:00:00</WarrantyExpires><ContractExpires>0001-01-01T00:00:00</ContractExpires><WholesalePlan>TLSPR_4G_UC_TALKTEXT</WholesalePlan></VerizonPrepaid_Activate_AsyncResult></VerizonPrepaid_Activate_AsyncResponse>";//await httpResponse.Content.ReadAsStringAsync();

			//var doc = new XmlDocument();
			//doc.LoadXml(content);
			//var serializer = new XmlSerializer(typeof(VerizonPrepaid_Activate_AsyncResponse));
			//var response = (VerizonPrepaid_Activate_AsyncResponse)serializer.Deserialize(new StringReader(content));

			// PREPAID
			//var request = new VerizonPrepaid_Activate_AsyncRequest(ApiUser, ApiPassword, zip, imei,
			//	simIcc, 14720, 14720, DateTime.UtcNow.Day, "1166", internalPlanId.ToString(),
			//	callBackUrl);

			//VerizonPrepaid_Activate_AsyncResponse response = null;
			//try {
			//	response = await Client.VerizonPrepaid_Activate_AsyncAsync(request);
			//	return response.VerizonPrepaid_Activate_AsyncResult;
			//}
			//catch (Exception e) {
			//	ErrorHandler.StaticCapture(e, null, "Telispire-Activate-NoPort", new Dictionary<string, string>() {
			//		{"ActivationResponse", JsonConvert.SerializeObject(response) }
			//	});
			//	throw;
			//}

			// POSTPAID
			var request = new VerizonPostPaid_ActivateRequest() {
				username = ApiUser,
				password = ApiPassword,
				accountNumber = accountNumber,
				callbackURL = callBackUrl,
				npanxx = zip,
				esn = imei,
				icc = simIcc,
				referenceNumber = internalPlanId.ToString(),
				vbosRatePlan = "LEXVOR_VZ",
				vpRateplan = new VbosPackage() {
					PackageName = externalPlanId,
					MRC = 0,
					InstallDate = DateTime.Now
				},
				contractExpires = DateTime.Now.AddYears(10),
				warrantyExpires = DateTime.Now.AddYears(10)
			};

			VerizonPostPaid_ActivateResponse response = null;
			try {
				response = await Client.VerizonPostPaid_ActivateAsync(request);
				return response.VerizonPostPaid_ActivateResult;
			}
			catch (Exception e) {
				ErrorHandler.StaticCapture(e, null, "Wireless-Activate-NoPort", new Dictionary<string, string>() {
					{"ActivationResponse", JsonConvert.SerializeObject(response) }
				});
				throw;
			}
		}

		public async Task<(bool, string)> StartActivateServiceLineWithPort(string accountNumber, string callBackUrl, string imei, string mobileNumber, string simIcc, PortRequest portDetails, Guid internalPlanId, string externalPlanId) {
			if (portDetails.Zip.IsNull() || portDetails.Zip.Length != 5) {
				return (false, "Zip code is required to port.");
			}
			if (mobileNumber.IsNull() || mobileNumber.Length < 9) {
				return (false, "A vaild mobile number is required to port.");
			}
			if (portDetails.AccountNumber.IsNull() || portDetails.AccountNumber.Length < 9) {
				return (false, "A valid Account Number is required to port.");
			}
			//var req = $@"<?xml version=""1.0"" encoding=""utf-16""?>
			//    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
			//      <soap:Body>
			//        <VerizonPort_ActivatePrepaid_Async3 xmlns=""urn:telispire:MdnServices"">
			//		   <username>{ApiUser}</username>
			//		   <password>{ApiPassword}</password>
			//		   <activationPlanId>{14720}</activationPlanId>
			//		   <autopayPlanId>{14720}</autopayPlanId>
			//		   <mdn>{mobileNumber}</mdn>
			//		   <esn>{imei}</esn>
			//		   <icc>{simIcc}</icc>
			//		   <port>
			//		      <Id>{0}</Id>
			//		      <OrderId>{0}</OrderId>
			//		      <OldCarrier>{"Boost"}</OldCarrier>
			//		      <OldCarrierAccountNumber>{Regex.Replace(portDetails.AccountNumber, "([^A-Z0-9a-z])+", "")}</OldCarrierAccountNumber>
			//		      <FirstName>{portDetails.FirstName}</FirstName>
			//		      <MiddleName>{portDetails.MiddleInitial}</MiddleName>
			//		      <LastName>{portDetails.LastName}</LastName>
			//			  <BusinessName />
			//		      <Password>{portDetails.Password}</Password>
			//		      <Address1>{portDetails.AddressLine1}</Address1>
			//		      <Address2>{portDetails.AddressLine2}</Address2>
			//		      <City>{portDetails.City}</City>
			//		      <State>{portDetails.State}</State>
			//		      <Zip>{portDetails.Zip}</Zip>
			//		      <MDN>{mobileNumber}</MDN>
			//		   </port>
			//		   <cycleDay>{DateTime.UtcNow.Day}</cycleDay>
			//		   <verizonAccount>{1166}</verizonAccount>
			//		   <referenceNumber>{internalPlanId}</referenceNumber>
			//		   <callbackURL>{callBackUrl}</callbackURL>
			//		</VerizonPort_ActivatePrepaid_Async3>
			//      </soap:Body>
			//    </soap:Envelope>
			//";

			//var client = new HttpClient();
			//var request = new HttpRequestMessage(HttpMethod.Post, "https://wirelessprovisioning.com/desktopmodules/telispire.webservices/mdnservices.asmx?op=VerizonPort_ActivatePrepaid_Async3") {
			//	Content = new StringContent(req, Encoding.UTF8, "text/xml")
			//};
			//request.Headers.Add("SOAPAction", "urn:telispire:MdnServices/VerizonPort_ActivatePrepaid_Async3");
			//var httpResponse = await client.SendAsync(request);
			//var content = await httpResponse.Content.ReadAsStringAsync();

			// PREPAID
			//var request = new TelispireSOAPService.VerizonPort_ActivatePrepaid_Async3Request(ApiUser, ApiPassword, 14720, 14720,
			//	mobileNumber, imei, simIcc, new Port() {
			//		OldCarrierAccountNumber = Regex.Replace(portDetails.AccountNumber, "([^A-Z0-9a-z])+", ""),
			//		FirstName = portDetails.FirstName,
			//		LastName = portDetails.LastName,
			//		MiddleName = portDetails.MiddleInitial?.Substring(0, 1),
			//		Password = portDetails.Password,
			//		MDN = mobileNumber,
			//		Address1 = portDetails.AddressLine1,
			//		Address2 = portDetails.AddressLine2,
			//		City = portDetails.City,
			//		State = portDetails.State,
			//		Zip = portDetails.Zip,
			//		BusinessName = "",
			//		OldCarrier = portDetails.OSPName,
			//		OrderId = 0,
			//		Id = 0
			//	}, DateTime.UtcNow.Day, "1166", internalPlanId.ToString(), callBackUrl);

			//VerizonPort_ActivatePrepaid_Async3Response response = null;
			//try {
			//	response = await Client.VerizonPort_ActivatePrepaid_Async3Async(request);
			//	return response?.Customer;
			//}
			//catch (Exception e) {
			//	ErrorHandler.StaticCapture(e, null, "Telispire-Activate-Port", new Dictionary<string, string>() {
			//		{"ActivationResponse", JsonConvert.SerializeObject(response) }
			//	});
			//	throw;
			//}

			// POSTPAID
			var request = new Verizon_ActivatePort2Request() {
				username = ApiUser,
				password = ApiPassword,
				accountNumber = accountNumber,
				MDN = Regex.Replace(mobileNumber, "[^0-9.]", ""),
				esn = imei,
				icc = simIcc,
				vbosPlan = "LEXVOR_VZ",
				vpRateplan = new VbosPackage() {
					PackageName = externalPlanId,
					MRC = 0,
					InstallDate = DateTime.Now
				},
				port = new Port() {
					OldCarrierAccountNumber = Regex.Replace(portDetails.AccountNumber, "([^A-Z0-9a-z])+", ""),
					Notes = "",
					FirstName = portDetails.FirstName,
					LastName = portDetails.LastName,
					MiddleName = (portDetails.MiddleInitial?.Substring(0, 1) ?? ""),
					SSN = "",
					Password = portDetails.Password,
					MDN = Regex.Replace(mobileNumber, "[^0-9.]", ""),
					AccountNumber = accountNumber,
					StreetNumber = "",
					Address1 = portDetails.AddressLine1,
					Address2 = portDetails.AddressLine2,
					City = portDetails.City,
					State = portDetails.State,
					Zip = portDetails.Zip,
					BusinessName = "",
					OldCarrier = portDetails.OSPName,
					OrderId = 0,
					Id = 0
				}
			};

			Verizon_ActivatePort2Response response = null;
			try {
				response = await Client.Verizon_ActivatePort2Async(request);
				return (!response.hasErrors, response.errorMessage);
			}
			catch (Exception e) {
				ErrorHandler.StaticCapture(e, null, "Wireless-Activate-Port", new Dictionary<string, string>() {
					{"ActivationResponse", JsonConvert.SerializeObject(response) }
				});
				throw;
			}
		}

		public async Task<bool> DisconnectMDN(string mdn) {

			var request = new DisconnectMDNRequest() {
				password = ApiPassword,
				username = ApiUser,
				MDN = mdn
			};

			DisconnectMDNResponse response = null;
			try {
				response = await Client.DisconnectMDNAsync(request);
				if (response.DisconnectMDNResult != null) {
					return true;
				}
				else {
					throw new Exception("Error while disconnecting mdn");
				}
			}
			catch (Exception e) {
				throw;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mobileNumber"></param>
		/// <returns>Reference Number for async call</returns>
		public async Task<string> SubmitPortValidate(string mobileNumber) {
			var request = new Verizon_SubmitPortValidationRequest(ApiUser, ApiPassword, mobileNumber);
			var response = await Client.Verizon_SubmitPortValidationAsync(request);
			if (response.message == "SUCCESSFULLY PROCESSED THE REQUEST") {
				return response.Verizon_SubmitPortValidationResult;
			}
			else {
				return null;
			}
		}

		public async Task<(bool, string)> CheckPortStatus(string mobileNumber) {
			var request = new Verizon_PortInInquiryRequest(ApiUser, ApiPassword, mobileNumber);
			var response = await Client.Verizon_PortInInquiryAsync(request);
			if (!response.errorMessage.IsNull()) {
				return (false, response.errorMessage);
			}
			else {
				if (response.Verizon_PortInInquiryResult.Status != VerizonPortInStatus.Completed) {
					return (false, response.Verizon_PortInInquiryResult.ReasonDescription ?? "No error code given. Check Wireless Provider.");
				}
				else {
					return (true, "Port Complete");
				}
			}
		}

		public async Task<string> GetMDNFromICC(string simIcc) {
			var request = new GetWirelessByICCRequest(ApiUser, ApiPassword, simIcc);
			var response = await Client.GetWirelessByICCAsync(request);
			if (response.GetWirelessByICCResult != null && response.GetWirelessByICCResult.Status != "7" && response.GetWirelessByICCResult.Status != "8") {
				return response.GetWirelessByICCResult.MDN;
			}
			else {
				return null;
			}
		}

		public async Task<(bool, string)> CheckPortValidate(string referenceNumber) {
			var req = $@"<?xml version=""1.0"" encoding=""utf-16""?>
			    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
			      <soap:Body>
			        <Verizon_CheckPortValidationResults xmlns=""urn:telispire:MdnServices"">
			          <username>{ApiUser}</username>
			          <password>{ApiPassword}</password>
			          <refNumber>{referenceNumber}</refNumber>
			        </Verizon_CheckPortValidationResults>
			      </soap:Body>
			    </soap:Envelope>
			";

			var client = new HttpClient();
			var response = await client.PostAsync("https://wirelessprovisioning.com/desktopmodules/telispire.webservices/mdnservices.asmx?op=Verizon_CheckPortValidationResults", new StringContent(req, Encoding.UTF8, "text/xml"));

			var content = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode) {
				if (content.ToLower().Contains("is portable")) {
					return (true, "Your number is portable");
				}
				else {
					return (false, content);
				}
			}
			else {
				return (false, content);
			}
		}

		public async Task<string> IMEIValidate(string imei) {
			// Strip spaces
			imei = Regex.Replace(imei, @"\s+", "");

			if (imei.Length == 14) {
				imei = $"{imei}0";
			}

			if (imei.Length != 15 && imei.Length != 11 && imei.Length != 8) {
				return $"The IMEI provided ({imei}) is invalid.";
			}

			var request = new Verizon_ValidateDeviceRequest(ApiUser, ApiPassword, imei, true);
			var response = await Client.Verizon_ValidateDeviceAsync(request);
			if (response.Verizon_ValidateDeviceResult == null) {
				return $"There was an error trying to validate the IMEI {imei}";
			}
			else if (response.Verizon_ValidateDeviceResult.deviceResponse.isPIBLock.ToUpper() == "Y") {
				return "This device is currently locked to Verizon, we cannot activate it.";
			}
			else if (response.Verizon_ValidateDeviceResult.deviceResponse.returnMessage == "DISCOUNT_1_ACCESS" || response.Verizon_ValidateDeviceResult.deviceResponse.returnMessage == "DISCOUNT_2_ACCESS") {
				return "We cannot activate this device because it already has a Verizon plan attached to it.";
			}
			else {
				var resp = response.Verizon_ValidateDeviceResult.deviceResponse.isInDMD.ToUpper();
				return resp == "Y" ? resp : response.Verizon_ValidateDeviceResult.deviceResponse.returnMessage;
			}
		}

		public async Task<double> GetGBUsage(string mobileNumber, DateTime start, DateTime end) {
			var mdn = StaticUtils.NumericStrip(mobileNumber);
			var request = new GetUsageSummaryRequest(ApiUser, ApiPassword, mdn, start, end);
			var response = await Client.GetUsageSummaryAsync(request);
			try {
				var kb = response.GetUsageSummaryResult.UsageSummaryData.Sum(x => x.Kilobytes);
				return Math.Round((double)kb / (double)1000000, 2);
			}
			catch (Exception e) {
				throw new Exception($"Failed to retrieve usage data for {mdn}", e);
			}
		}

		public async Task<UsageDay> GetUsageForDay(string mobileNumber, DateTime date) {
			var mdn = StaticUtils.NumericStrip(mobileNumber);
			var request = new GetUsageSummaryRequest(ApiUser, ApiPassword, mdn, date.Date, date.Date.AddDays(1).AddMinutes(-1));
			var response = await Client.GetUsageSummaryAsync(request);
			try {
				return new UsageDay() {
					MDN = mdn,
					Date = date.Date,
					Minutes = response.GetUsageSummaryResult.UsageSummaryVoice.Sum(x => x.Minutes),
					SMS = response.GetUsageSummaryResult.UsageSummaryText.Sum(x => x.Count),
					KBData = response.GetUsageSummaryResult.UsageSummaryData.Sum(x => x.Kilobytes),
				};
			}
			catch (Exception e) {
				throw new Exception($"Failed to retrieve usage data for {mobileNumber}", e);
			}
		}

		public async Task<UsageDay> GetUsageForCycle(string mobileNumber, int year, int month) {
			mobileNumber = StaticUtils.NumericStrip(mobileNumber);
			var date = new DateTime(year, month, 1);
			var request = new GetUsageSummaryRequest(ApiUser, ApiPassword, mobileNumber, date, date.GetLast());


			var response = new GetUsageSummaryResponse();
			try {
				response = await Client.GetUsageSummaryAsync(request);

				// string requestXML = _requestInterceptor.LastRequestXML;
				// string responseXML = _requestInterceptor.LastResponseXML;

				return new UsageDay() {
					MDN = mobileNumber,
					Date = date.Date,
					Minutes = response.GetUsageSummaryResult.UsageSummaryVoice.Sum(x => x.Minutes),
					SMS = response.GetUsageSummaryResult.UsageSummaryText.Sum(x => x.Count),
					KBData = response.GetUsageSummaryResult.UsageSummaryData.Sum(x => x.Kilobytes),
				};
			}
			catch (Exception e) {
				// string requestXML = _requestInterceptor.LastRequestXML;
				// string responseXML = _requestInterceptor.LastResponseXML;

				Console.WriteLine(e);
				throw new Exception($"Failed to retrieve usage data for {mobileNumber}", e);
			}
		}

		public async Task<ActiveWirelessPackage> GetActiveWirelessPlan(string mdn) {
			try {
				var telispireResponce = await Client.GetActivePackagesAsync(new GetActivePackagesRequest {
					MDN = mdn,
					password = ApiPassword,
					username = ApiUser
				});

				return telispireResponce.GetActivePackagesResult.OrderByDescending(p => p.InstallDate)
																.FirstOrDefault();

			}
			catch (Exception e) {
				throw new Exception($"Failed to retrieve wireless plan from Telispire by mdn: {mdn}", e);
			}
		}

		public async Task ChangePackageOnMDN(string mdn, string oldPackageName, string newPackageName) {
			try {
				var telispireResponce = await Client.ChangePackageOnMDNAsync(new ChangePackageOnMDNRequest {
					MDN = mdn,
					password = ApiPassword,
					username = ApiUser,
					oldPackage = new VbosPackage {
						InstallDate = DateTime.Now,
						PackageName = oldPackageName,
						MRC = 0
					},
					newPackage = new VbosPackage {
						InstallDate = DateTime.Now,
						PackageName = newPackageName,
						MRC = 0
					}
				});
			}
			catch (Exception e) {
				throw new Exception($"Failed to change plan on MDN in Telispire by mdn: {mdn}", e);
			}
		}
	}
}
