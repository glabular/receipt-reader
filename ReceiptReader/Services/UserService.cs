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
        Console.WriteLine();
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
            await _dbContext.SaveChangesAsync();

            return user;
        }

        var changed = false;

        if (!string.Equals(user.Username, tgUser.Username, StringComparison.Ordinal))
        {
            user.Username = tgUser.Username;
            changed = true;
        }

        if (!string.Equals(user.FirstName, tgUser.FirstName, StringComparison.Ordinal))
        {
            user.FirstName = tgUser.FirstName;
            changed = true;
        }

        if (!string.Equals(user.LastName, tgUser.LastName, StringComparison.Ordinal))
        {
            user.LastName = tgUser.LastName;
            changed = true;
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }
}
