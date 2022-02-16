using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.API.Services {
	public static class PlanTypeService {
		public static async Task<List<PlanType>> GetPlanTypes(ApplicationDbContext db) {
			return await db.PlanTypes.Where(p => !p.Archived).ToListAsync();
		}

	    public static async Task<List<PlanType>> GetPublicPlanTypes(ApplicationDbContext db) {
	        return await db.PlanTypes.Where(p => !p.Archived && p.DisplayOnPublicPages).ToListAsync();
		}

	    public static async Task<PlanType> GetPlanTypeByIdWithDevices(ApplicationDbContext db, Guid planTypeId) {
		    var planType = await db.PlanTypes.Include(x => x.Devices).ThenInclude(x => x.Device).FirstAsync(p => p.Id == planTypeId);
		    return planType;
	    }
	}
}
