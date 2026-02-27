using ReceiptReader.Data;
using ReceiptReader.Models;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;

namespace ReceiptReader.Services;

internal class UserService
{
    private readonly BotDbContext _dbContext;

    public UserService(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TelegramUser?> EnsureUserExistsAsync(User tgUser)
    {
        var user = await _dbContext.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == tgUser.Id);

        if (user is null)
        {
            user = new TelegramUser
            {
                TelegramUserId = tgUser.Id,
                Username = tgUser.Username,
                FirstName = tgUser.FirstName,
                LastName = tgUser.LastName
            };

            _dbContext.TelegramUsers.Add(user);            
        }
        else
        {
            user.Username = tgUser.Username;
            user.FirstName = tgUser.FirstName;
            user.LastName = tgUser.LastName;            
        }

        await _dbContext.SaveChangesAsync();

        return user;
    }
}
