using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.API.Objects;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Web;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Data;
using System.Text;

namespace Lexvor.Areas.Admin.Controllers {

	[Area("Admin")]
	public class StockedDeviceController : BaseAdminController {
		private readonly ApplicationDbContext _context;
		private readonly OtherSettings _other;
		public StockedDeviceController(ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IOptions<ConnectionStrings> connStrings,
			IOptions<OtherSettings> other) : base(context, userManager, connStrings) {
			_context = context;
			_other = other.Value;
		}
		
		[Authorize(Roles = Roles.Level1Support)]
		public async Task<IActionResult> Index(string stock, string imei, Guid deviceid, Guid userid) {
			var list = await _context.StockedDevice.Include(x => x.Device).ToListAsync();
			var deviceLi = await _context.Devices.Where(x => !x.Archived).OrderBy(s => s.Name).ToListAsync();

			var stockdata = from e in _context.StockedDevice
							join d in _context.Devices on e.DeviceId equals d.Id
							join u in _context.UserDevices on e.Id equals u.StockedDevice.Id into ud
							from userdev in ud.DefaultIfEmpty()
							join p in _context.Plans on userdev.PlanId equals p.Id into pl
							from plan in pl.DefaultIfEmpty()
							join pr in _context.Profiles on plan.ProfileId equals pr.Id into pro
							from profile in pro.DefaultIfEmpty()
							join acc in _context.Users on profile.AccountId equals acc.Id into us
							from user in us.DefaultIfEmpty()
							select new StockDeviceListVM() {
								Id = e.Id,
								IMEI = e.IMEI,
								Device = d,
								User = user,
								StockedDevice = e
							};
			var users = stockdata.Select(o => o.User).Where(x => x.Email != null).Distinct();
			var dev = stockdata.Select(o => o.Device).Distinct();

			var filter = stockdata;
			if (stock == "1") {
				filter = stockdata.Where(x => x.StockedDevice.Available == true);
			}
			if (stock == "2") {
				filter = stockdata.Where(x => x.StockedDevice.Available == false && x.User == null);
			}
			if (stock == "3") {
				filter = stockdata.Where(x => x.User != null);
			}
			if (imei != null && imei != "") {
				filter = stockdata.Where(x => x.StockedDevice.IMEI.Contains(imei));
			}
			if (deviceid != null && deviceid != Guid.Empty) {
				filter = stockdata.Where(x => x.StockedDevice.Device.Id == deviceid);
			}
			if (userid != null && userid != Guid.Empty) {
				filter = stockdata.Where(x => x.User.Id == userid.ToString());
			}
			StockFilter sf = new StockFilter();
			sf.stock = stock;
			sf.deviceid = deviceid;
			sf.imei = imei;
			sf.userid = userid;
			return View(new StockDeviceListAllVM() {
				StockDeviceListVM = filter.ToList(),
				Users = users.ToList(),
				Dev = dev.ToList(),
				stockfilter = sf
			});
		}
		
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create() {

			var devices = await _context.Devices.Where(x => !x.Archived).OrderBy(s => s.Name).ToListAsync();
			var model = new StockDeviceCreateViewModels() {
				Device = devices
			};
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Create(StockDeviceCreateViewModels model) {
			int val = 0;
			StockedDevice item = new StockedDevice();
			if (ModelState.IsValid) {
				if (_context.StockedDevice.ToList().Any(o => o.IMEI == model.StockedDevice.IMEI)) {
					ModelState.AddModelError("IMEIError", "IMEI Already Exists");
				}
				else {
					val = 1;
				}
			}

			if (val == 1) {
				item.Id = Guid.NewGuid();
				item.DeviceId = model.StockedDevice.DeviceId;
				item.IMEI = model.StockedDevice.IMEI;
				item.Available = true;
				_context.Add(item);
				await _context.SaveChangesAsync();
				return RedirectToAction("Index", new { stock = "1" });
			}
			else {
				var device = await _context.Devices.OrderBy(s => s.Name).ToListAsync();
				return View(new StockDeviceCreateViewModels() {
					StockedDevice = model.StockedDevice,
					Device = device
				});
			}

			
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> createBulk(IFormFile upload) {
			var dbDevices = await _context.Devices.Where(x => !x.Archived).OrderBy(s => s.Name).ToListAsync();
			var dbImei = await _context.StockedDevice.ToListAsync();
			string errmsg = "";
			List<CSVDevList> dList = new List<CSVDevList>();
			if (upload != null) {
				if (upload.FileName.EndsWith(".csv")) {
					var result = new StringBuilder();
					using (var reader = new StreamReader(upload.OpenReadStream())) {
						int i = 0;
						while (reader.Peek() >= 0) {
							string txt = await reader.ReadLineAsync();
							i++;
							if (i > 1) {
								string[] str = txt.Split(',');
								CSVDevList dl = new CSVDevList();
								dl.Name = str[0];
								dl.imei = str[1];

								List<DeviceOption> dopt = new List<DeviceOption>();

								int strcnt = str.Length;
								if (strcnt % 2 == 0) {
									for (int k = 2; k < strcnt; k++) {
										if (k % 2 == 0) {
											{
												DeviceOption doptObj = new DeviceOption();
												string gp = str[k];
												string vl = str[k + 1];
												if (gp != "" && vl != "") {
													doptObj.OptionGroup = str[k];
													doptObj.OptionValue = str[k + 1];
													dopt.Add(doptObj);
												}
												else {
													if (gp != "" && vl == "") {
														errmsg += str[1] + ",";
													}
												}
											}
										}
									}
								}
								else {
									errmsg += str[1] + ",";

								}
								dl.DevOpt = dopt;
								dList.Add(dl);
							}
						}

						var duplicateIMEI = (dList.GroupBy(x => x.imei)
						.Where(group => group.Count() > 1)
						.Select(group => group.Key)).ToList();


						var devMis = (from item in dList
									  where !dbDevices.Any(x => x.Name.Trim().Replace(" ", string.Empty) == item.Name.Trim().Replace(" ", string.Empty))
									  select item).ToList();

						var imeiMis = (from item in dList
									   where dbImei.Any(x => x.IMEI.Trim().Replace(" ", string.Empty) == item.imei.Trim().Replace(" ", string.Empty))
									   select item).ToList();

						if (errmsg != "") {
							errmsg = "Column count not matched for " + errmsg.Remove(errmsg.LastIndexOf(","));
						}

						if (devMis.Count != 0 || imeiMis.Count != 0 || duplicateIMEI.Count != 0 || errmsg != "") {
							return View("Create", new StockDeviceCreateViewModels() {
								csvdevnamelist = devMis,
								csvdevimeilist = imeiMis,
								csvduplicateimeilist = duplicateIMEI,
								Device = dbDevices,
								createMode = "Bulk",
								ErrorMessage = errmsg
							});
						}
						else {

							foreach (CSVDevList li in dList) {
								var dev = _context.Devices.Where(e => e.Name.Trim().Replace(" ", string.Empty) == li.Name.Trim().Replace(" ", string.Empty)).FirstOrDefault();
								StockedDevice sd = new StockedDevice();
								sd.Device = dev;
								sd.IMEI = li.imei;
								sd.Available = true;
								sd.Options = li.DevOpt;
								_context.Add(sd);
							}

							await _context.SaveChangesAsync();
						}
					}
				}
				else {
					ModelState.AddModelError("File", "This file format is not supported");
					return View();
				}
			}
			else {
				ModelState.AddModelError("File", "Please Upload Your file");
			}
			return RedirectToAction("Index", new { stock = "1" });
		}
		
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id) {

			var devices = await _context.Devices.Where(x => !x.Archived).OrderBy(s => s.Name).ToListAsync();
			var stockedddevice = await _context.StockedDevice.Include(x => x.Device).SingleOrDefaultAsync(m => m.Id == id);
			var model = new StockDeviceEditViewModels() {
				Device = devices,
				StockedDevice = stockedddevice
			};
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Edit(Guid id, StockDeviceEditViewModels model) {
			int val = 0;
			StockedDevice item = new StockedDevice();
			if (ModelState.IsValid) {
				var dbST = await _context.StockedDevice.Where(o => o.Id == id).FirstAsync();
				if (dbST.IMEI == model.StockedDevice.IMEI) {
					val = 1;
				}
				else if (_context.StockedDevice.ToList().Any(o => o.IMEI == model.StockedDevice.IMEI)) {
					ModelState.AddModelError("IMEIError", "IMEI Already Exists");
				}
				else {
					val = 1;
				}
			}

			if (val == 1) {
				//_context.Update(model.StockedDevice);
				var stDev = await _context.StockedDevice.SingleOrDefaultAsync(m => m.Id == id);
				stDev.Available = true;
				stDev.IMEI = model.StockedDevice.IMEI;
				stDev.DeviceId = model.StockedDevice.DeviceId;
				await _context.SaveChangesAsync();
				return RedirectToAction("Index", new { stock = "1" });
			}
			else {
				var device = await _context.Devices.OrderBy(s => s.Name).ToListAsync();
				return View(new StockDeviceEditViewModels() {
					StockedDevice = model.StockedDevice,
					Device = device
				});
			}

		}
		
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> NewAssign(Guid id, string assigntype) {
			var item = await _context.StockedDevice.Include(x => x.Device).SingleOrDefaultAsync(m => m.Id == id);
			var deviceId = item.DeviceId;
			var plans = await _context.Plans.Include(x => x.UserDevice).Include(x => x.PlanType).Include(x => x.Profile).ThenInclude(x => x.Account)
							.Where(s => s.DeviceId == deviceId && s.UserDevice.StockedDevice == null && s.UserDevice.Shipped != null).ToListAsync();

			var CurrPlan = await _context.Plans.Include(x => x.UserDevice).Include(x => x.PlanType).Include(x => x.Profile).ThenInclude(x => x.Account)
							.Where(s => s.UserDevice.StockedDevice.Id == id).FirstOrDefaultAsync();
			return View(new NewAssignVM() {
				UserPlan = plans,
				StockedDevice = item,
				AssignType = assigntype,
				CurrUserPlan = CurrPlan
			});
		}

		[HttpGet]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> NewAssignSave(Guid id, Guid StockDeviceId, string IMEI, string AssignType) {
			if (AssignType == "Re Assign") {
				var CurrSt = await _context.UserDevices.Include(x => x.StockedDevice)
							.Where(s => s.StockedDevice.Id == StockDeviceId).FirstOrDefaultAsync();
				CurrSt.IMEI = null;
				StockedDevice st = new StockedDevice();
				st = null;
				CurrSt.StockedDevice = st;
				await _context.SaveChangesAsync();
			}
			var item = await _context.UserDevices.SingleOrDefaultAsync(m => m.PlanId == id);
			var stDev = await _context.StockedDevice.SingleOrDefaultAsync(m => m.Id == StockDeviceId);
			stDev.Available = false;
			item.IMEI = IMEI;
			item.StockedDevice = stDev;
			await _context.SaveChangesAsync();

			return RedirectToAction("Index", new { stock = "3" });
		}

		[HttpGet]
		[Authorize(Roles = Roles.TrustedManager)]
		public async Task<IActionResult> Archive(Guid Id, string ArchiveStatus) {
			var CurrSt = await _context.UserDevices.Include(x => x.StockedDevice)
							.Where(s => s.StockedDevice.Id == Id).FirstOrDefaultAsync();
			if (CurrSt != null) {
				CurrSt.IMEI = null;
				CurrSt.StockedDevice.Available = false;
				StockedDevice st = new StockedDevice();
				st = null;
				CurrSt.StockedDevice = st;
				await _context.SaveChangesAsync();
			}
			else if (ArchiveStatus == "Archive") {
				var St = await _context.StockedDevice.SingleOrDefaultAsync(m => m.Id == Id);
				St.Available = false;
				await _context.SaveChangesAsync();
			}
			else if (ArchiveStatus == "UnArchive") {
				var St = await _context.StockedDevice.SingleOrDefaultAsync(m => m.Id == Id);
				St.Available = true;
				await _context.SaveChangesAsync();
			}

			return RedirectToAction("Index", new { stock = "2" });
		}
	}


}
