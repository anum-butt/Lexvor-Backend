using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API.Objects;
using Lexvor.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.API.Services
{
    public static class UserMessageService {
	    public static async Task<List<UserMessage>> GetLatestUserMessages(ApplicationDbContext ctx, string accountId) {
		    var messages = await ctx.UserMessages
			    .Where(m =>
				    (!string.IsNullOrEmpty(m.AccountId) && m.AccountId.ToLower() == accountId.ToLower())
				    && string.IsNullOrEmpty(m.AccountId)
				    && ((m.ShowOnce && !m.Shown) || !m.ShowOnce))
			    .OrderByDescending(m => m.Created).ToListAsync();
			
		    messages.ForEach(async (m) => {
				m.Shown = true;
			    await ctx.SaveChangesAsync();
			});

		    return messages;
	    }

	    public static async Task SaveUserMessage(ApplicationDbContext ctx, string title, string description, string accountId, string color) {
		    var message = new UserMessage() {
			    Created = DateTime.UtcNow,
			    AccountId = accountId,
			    Description = description,
			    Title = title,
			    ShowOnce = true,
				ExpirationDate = DateTime.UtcNow.AddDays(30),
				Color = color
			};

		    await ctx.UserMessages.AddAsync(message);
		    await ctx.SaveChangesAsync();
	    }
    }
}
