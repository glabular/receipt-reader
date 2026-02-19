using ReceiptReader.Services;
using System;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Please enter receipt URL:");
        string? url;
        while (!UrlValidator.IsUrlValid(url = Console.ReadLine()?.Trim()))
        {
            Console.Error.WriteLine("Invalid URL provided. Please try again:\n");
        }

        var pageSource = BrowserEngine.GetPageSource(url!); 

        var invoiceResult = InvoiceParser.ParseInvoicePage(pageSource);
        var invoiceItems = invoiceResult?.BoughtItems;
        Console.WriteLine($"You spent {invoiceResult.TotalSum} in '{invoiceResult.ShopName}' at {invoiceResult.ShoppingDate}");

        foreach (var item in invoiceItems)
        {
            Console.WriteLine($"Title: {item.Title}, Unit Price: {item.UnitPrice}, Total Price: {item.InvoiceItemPrice}, Quantity: {item.Quantity}");
        }
    }
}
