using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lexvor.API.Objects;
using Microsoft.AspNetCore.Http;

namespace Lexvor.Models.AdminViewModels
{
    public class AdminDashboardViewModel
    {
        public int LastMonthUsers { get; set; }
        public int LastWeekUsers { get; set; }
		public int NoDevices { get; set; }
		public int TotalUsers { get; set; }
        public int TotalPlans { get; set; }
	    public List<ChartItem> UsersByMonth { get; set; }
	    public List<ChartItem> MonthlyRecurringRevenue { get; set; }

	    public int IdsPending { get; set; }
	    public int TradeInsPending { get; set; }
	    public int ActivationsPending { get; set; }
	    public int MdnsPending { get; set; }
	    public int PortsPending { get; set; }

		[DataType(DataType.Upload)]
	    public IFormFile upload { get; set; }

	    public string waitlistAmount { get; set; }
	}

	public class ChartItem {
		public string Label { get; set; }
		public int Value { get; set; }
	}
}
