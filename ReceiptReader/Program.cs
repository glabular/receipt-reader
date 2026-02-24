using Microsoft.Extensions.Configuration;

namespace ReceiptReader;

internal class Program
{
    static async Task Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var telegramToken = config["TelegramBotToken"];

        if (string.IsNullOrEmpty(telegramToken))
        {
            Console.WriteLine("ERROR: 'TelegramBotToken' is missing from secrets.json.");

            return;
        }

        var telegramClient = new TelegramClient(telegramToken);

        await telegramClient.StartAsync();

        Console.ReadLine();
    }
}
