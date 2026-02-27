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

    public async Task EnsureUserExistsAsync(User tgUser)
    {
        var existingUser = await _dbContext.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == tgUser.Id);

        if (existingUser is null)
        {
            var newUser = new TelegramUser
            {
                TelegramUserId = tgUser.Id,
                Username = tgUser.Username,
                FirstName = tgUser.FirstName,
                LastName = tgUser.LastName
            };

            _dbContext.TelegramUsers.Add(newUser);
        }
        else
        {
            existingUser.Username = tgUser.Username;
            existingUser.FirstName = tgUser.FirstName;
            existingUser.LastName = tgUser.LastName;
        }

        await _dbContext.SaveChangesAsync();
    }
}
