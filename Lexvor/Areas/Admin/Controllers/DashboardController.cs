using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using Lexvor.API.Services;
using Lexvor.Controllers;
using Lexvor.Controllers.API;
using Lexvor.Data;
using Lexvor.Extensions;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Lexvor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lexvor.Areas.Admin.Controllers {
	[Area("Admin")]
	[Route("admin/[controller]")]
	[Authorize(Roles = Roles.Level1Support)]
	public class DashboardController : BaseAdminController {
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OtherSettings _other;

		public static string Name = "Dashboard";

		public DashboardController(
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other,
			ApplicationDbContext context) : base(context, userManager, connStrings) {
			_userManager = userManager;
			_other = other.Value;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var allUsers = await _context.Profiles.Where(x => x.Account.EmailConfirmed && x.IsArchive != true).ToListAsync();
			var allPlans = await _context.Plans.Where(x => x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid).ToListAsync();
			var logins = await _context.LoginAttempts.Where(l => l.AttemptDate > DateTime.UtcNow.AddHours(-24)).ToListAsync();
			var uniqueLogins = logins.Select(l => l.Email).Distinct().Count();
			var NoDevicesCnt =  _context.Plans.Include(x => x.UserDevice).Include(x => x.PlanType).Include(x => x.Profile)
				.ThenInclude(x => x.Account).Count(s => s.UserDevice.StockedDevice == null && s.UserDevice.Shipped != null);

			var usersByMonth = DbRaw.Query<ChartItem>(@"select
			  datejoined as Label,
			  SUM(num) OVER (PARTITION BY dateadd(year, datediff(year, 0, datejoined), 0) ORDER BY datejoined) as Value
			from (
			  SELECT
				dateadd(day, datediff(day, 0, datejoined), 0) as datejoined,
				count(*) as num
			  FROM Profiles
			  WHERE datejoined != '0001-01-01 00:00:00.0000000' and firstname is not null and firstname != '' and datejoined > dateadd(day,-180,getdate())
			  GROUP BY datejoined
			) as data", null, _connStrings.DefaultConnection).ToList();

			var mrr = DbRaw.Query<ChartItem>(@"
			select
				datejoined as Label,
				SUM(num / 100) OVER (PARTITION BY dateadd(year, datediff(year, 0, datejoined), 0) ORDER BY datejoined) as Value
			from (
				SELECT
				dateadd(month, datediff(month, 0, datejoined), 0) as datejoined,
				sum(p.Monthly) as num
				FROM Profiles
				join plans p on p.ProfileId = Profiles.Id and (p.Status = 1 or p.Status = 5)
				WHERE datejoined != '0001-01-01 00:00:00.0000000'
				group by dateadd(month, datediff(month, 0, datejoined), 0)
			) as data", null, _connStrings.DefaultConnection).ToList();

			usersByMonth.ForEach(u => {
				u.Label = DateTime.Parse(u.Label).ToString("yyyy-MM-dd");
			});

			mrr.ForEach(u => {
				u.Label = DateTime.Parse(u.Label).ToString("yyyy-MM-dd");
			});

			var identityVerify = await _context.Profiles.CountAsync(p => p.IDVerifyStatus == IDVerifyStatus.Pending);
			var planActivations = await _context.Plans.CountAsync(x => x.AgreementSigned && (x.Status == PlanStatus.DevicePending || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid) && x.WirelessStatus == WirelessStatus.NoPlan);
			var tradeinsPending = await _context.DeviceIntakes.CountAsync(x => x.IntakeType == IntakeType.NewCustomerTradeIn && !x.Approved.HasValue);
			var mdnsPending = await _context.Plans.CountAsync(x => string.IsNullOrWhiteSpace(x.MDN) && !PlanService.ActivePendingStatuses.Contains(x.Status) && !string.IsNullOrEmpty(x.AssignedSIMICC));
			var portsPending = await _context.Plans.CountAsync(x => x.AgreementSigned 
			                                                        && (x.Status == PlanStatus.DevicePending || x.Status == PlanStatus.Active || x.Status == PlanStatus.Paid)
			                                                        && (x.PortRequest != null && (x.PortRequest.Status == PortStatus.Ready || x.PortRequest.Status == PortStatus.Pending)));

			var model = new AdminDashboardViewModel() {
				TotalUsers = allUsers.Count,
				LastWeekUsers = allUsers.Count(p => p.DateJoined > DateTime.UtcNow.AddDays(-7)),
				LastMonthUsers = allUsers.Count(p => p.DateJoined > DateTime.UtcNow.AddDays(-30)),
				NoDevices= NoDevicesCnt,
				TotalPlans = allPlans.Count,
				UsersByMonth = usersByMonth,
				MonthlyRecurringRevenue = mrr,
				IdsPending = identityVerify,
				ActivationsPending = planActivations,
				TradeInsPending = tradeinsPending,
				MdnsPending = mdnsPending,
				PortsPending = portsPending
			};

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Index(AdminDashboardViewModel model) {
	        return RedirectToAction(nameof(Index));
		}

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> TaxRatingData() {

	        var firstOfLastMonth = DateTime.Now.AddMonths(-1).GetFirst();
	        var taxData = DbRaw.Query<TaxData>(@"select
			  'S' as record_type,
			  C.Id as unique_id,
			  0 as customer_type,
			  a.PostalCode as location_a,
			  c.Date as invoice_date,
			  case when C.InternalObjectId = '00000000-0000-0000-0000-000000000000' then 'W001' else 'W001' end as product_code,
			  case when C.InternalObjectId = '00000000-0000-0000-0000-000000000000' then '2' else '1' end as service_code,
			  '5' as provider,
			  C.Amount / 100 as charge_amount,
			  '1' as units
			from Charges C
			join Profiles p on p.id = c.ProfileId
			join Addresses a on a.Id = p.BillingAddressId
			where p.ExternalCustomerId is not null and c.status = 5
			  and p.id not in (
			  'D25F2EC8-E27B-45BF-93C5-F79A313841F2',
			  'ADB461A2-3BD2-451A-9DED-08D7CB8499BD'
			)
			and C.amount != 1000
			and c.Date >= @start and c.Date <= @end
			order by invoice_date desc", new { start = firstOfLastMonth, end = firstOfLastMonth.GetLast() }, _connStrings.DefaultConnection).ToList();

			
	        var memStream = new MemoryStream();

	        using (StreamWriter writer = new StreamWriter(memStream)) {
		        writer.WriteLine("record_type,unique_id,customer_type,location_a,invoice_date,product_code,service_code,provider,charge_amount,units");
		        foreach (var data in taxData) {
			        writer.WriteLine($"{data.record_type},{data.unique_id},{data.customer_type},{data.location_a},{data.invoice_date.ToString("d")},{data.product_code},{data.service_code},{data.provider},{data.charge_amount},{data.units}");
		        }
		        writer.Flush();
	        }

	        return File(memStream.ToArray(), "text/csv", firstOfLastMonth.ToString("MMMM") + "_lexvor_tax_data.csv");
        }

        public class TaxData {
	        public string record_type { get; set; }
	        public Guid unique_id { get; set; }
	        public string customer_type { get; set; }
	        public string location_a { get; set; }
	        public DateTime invoice_date { get; set; }
	        public string product_code { get; set; }
	        public string service_code { get; set; }
	        public int provider { get; set; }
	        public int charge_amount { get; set; }
	        public int units { get; set; }
        }

		// Use to retrieve non-public blob assets
		[Route("[action]")]
		public async Task<IActionResult> RetreiveSensitiveBlobAsset(string blobName) {
			var url = await BlobService.GetPrivateBlobUrl(blobName, _other);
			return Json(new {
				imageUrl = url
			});
		}

		[Route("[action]")]
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> HeaderAlert() {
			var model = new HeaderAlertsViewModel() {
				Alerts = new Dictionary<string, string>()
			};
			var msg = Message;
			if (!string.IsNullOrEmpty(msg)) {
				model.Alerts.Add(msg, "success");
			}

			var err = ErrorMessage;
			if (!string.IsNullOrEmpty(err)) {
				model.Alerts.Add(err, "warning");
			}

			return PartialView("_HeaderAlert", model);
		}

		[Route("[action]")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> Config() {
			return Json(_other);
		}
	}
}
