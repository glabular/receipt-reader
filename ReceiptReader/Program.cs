using ReceiptReader.Services;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        var telegramClient = new TelegramClient();

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
