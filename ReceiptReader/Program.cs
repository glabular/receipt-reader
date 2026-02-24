using ReceiptReader.Services;
using Microsoft.Extensions.Configuration;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
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

        using var client = new ReceiptClient();

        while (true)
        {
            Console.WriteLine("Please enter receipt URL:");
            var url = Console.ReadLine()?.Trim();
            if (!UrlValidator.IsUrlValid(url))
            {
                Console.Error.WriteLine("Invalid URL provided. Please try again.\n");
                continue;
            }

            var invoiceResult = client.GetInvoice(url!);

            Console.WriteLine($"You spent {invoiceResult.TotalSum} in '{invoiceResult.ShopName}' at {invoiceResult.ShoppingDate}");

            if (invoiceResult?.BoughtItems == null || invoiceResult.BoughtItems.Count == 0)
            {
                Console.WriteLine("No items found in the receipt.");
                continue;
            }

            foreach (var item in invoiceResult.BoughtItems)
            {
                Console.WriteLine($"Title: {item.Name}, Unit Price: {item.UnitPrice}, Total Price: {item.TotalPrice}, Quantity: {item.Quantity}");
            }

            Console.WriteLine("-----\n\n");
        }
    }
}
