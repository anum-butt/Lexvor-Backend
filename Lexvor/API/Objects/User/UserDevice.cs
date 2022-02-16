using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexvor.Data;
using Lexvor.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.API.Objects.User
{
    /// <summary>
    /// This object will always be retrieved through a Plan
    /// </summary>
    public class UserDevice {
	    public Guid Id { get; set; }
		// This is not a foreign key to prevent a circular reference with EF. Plus Plan to UserDevice is one->many.
	    public Guid PlanId { get; set; }

	    public Profile Profile { get; set; }

	    private string _imei;
	    public string IMEI {
		    get => _imei;
		    set => _imei = value != null ? Regex.Replace(value, @"[^\d]", "") : null;
	    }

	    public bool IMEIValid { get; set; }
		public DateTime? Requested { get; set; }
        public DateTime? Shipped { get; set; }

        /// <summary>
        /// Is the device currently in the users possession?
        /// </summary>
        public bool IsActive { get; set; }

        public bool BYOD { get; set; }

        public DateTime? ReturnRequested { get; set; }
        public DateTime? ReturnApproved { get; set; }

		public StockedDevice StockedDevice { get; set; }
        //public Guid StockedDeviceId { get; set; }

		// Shitty way to handle the many-to-many. This is a tack-on feature that I'm not convinced will be that large ever.
		// TODO this is SOO shitty.
        public string ChosenOptions { get; set; }
		
        [Editable(false)]
        [NotMapped]
		public List<DeviceOption> Options { get; set; }

		public async Task<List<DeviceOption>> GetOptions(ApplicationDbContext context) {
			var ids = ChosenOptions.Split(',').ToList();
			return await context.DeviceOptions.Where(x => ids.Contains(x.Id.ToString())).ToListAsync();
		}

        public static string SetOptions(List<DeviceOption> options) {
	        return string.Join(',', options.Select(x => x.Id.ToString()).ToList());
        }
		
        public bool PurchasedWithAffirm { get; set; }
        public string AffirmChargeId { get; set; }
        public bool UpgradeAvailable { get; set; }
	}
}
