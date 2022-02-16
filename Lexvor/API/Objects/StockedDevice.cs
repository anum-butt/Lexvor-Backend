using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class StockedDevice
    {
        public Guid Id { get; set; }
       
        public Device Device { get; set; }

        //[Required, Range(0, int.MaxValue, ErrorMessage = "Select Program")]
        //[RegularExpression(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$")]
        //[NotEmpty]

       
        [DisplayName("Device")]
        public Guid DeviceId { get; set; }
        [Required (ErrorMessage = "Please Enter IMEI No")]
        [DisplayName("IMEI No")]
	    public string IMEI {
		    get => _imei;
		    set => _imei = StaticUtils.NumericStrip(value);
	    }
	    private string _imei;

        public bool Available { get; set; }

        public List<DeviceOption> Options { get; set; }
    }

    public enum StockedDeviceStatus
    {
        Available = 1,
        PendingRepair = 2,
        NeedsRepair = 3,
        Subscribed = 10,
        InTransit = 88,
        InProcess = 99
    }
}
