using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;

namespace Lexvor.Models.HomeViewModels
{
    public class OurDevicesViewModel
    {
	    public PlanType Plan { get; set; }
	    public List<Device> Devices { get; set; }
    }
}
