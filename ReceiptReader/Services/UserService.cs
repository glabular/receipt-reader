using ReceiptReader.Data;

namespace ReceiptReader.Services;

internal class UserService
{
    private readonly BotDbContext _dbContext;

    public UserService(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
