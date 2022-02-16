using Lexvor.API.Objects;
using Lexvor.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Services {
	public static class UserOrderService {
		public static async Task<List<UserOrder>> GetOrdersForPlans(ApplicationDbContext context, List<UserPlan> plans) {
			var orders = await context.UserOrders.Include(x => x.Accessory1).Include(x => x.Accessory2).Include(x => x.Accessory3).Where(x => plans.Select(y => y.Id).Contains(x.UserPlan.Id)).ToListAsync();
			return orders;
		}
	}
}
