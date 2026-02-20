using Telegram.Bot;

namespace ReceiptReader;

internal sealed class TelegramClient
{
    public TelegramClient()
    {
        
    }

    public async Task Test()
    {
        var bot = new TelegramBotClient("");
        var me = await bot.GetMe();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
    }
}
