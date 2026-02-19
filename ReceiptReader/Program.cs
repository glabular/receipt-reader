using ReceiptReader.Services;
using System;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Please enter receipt URL:");
        string? url;
        while (!UrlValidatior.IsUrlValid(url = Console.ReadLine()?.Trim()))
        {
            Console.Error.WriteLine("Invalid URL provided. Please try again:\n");
        }

        var pageSource = BrowserEngine.GetPageSource(url!); 

        var invoiceItems = InvoiceParser.ParseInvoicePage(pageSource);

        foreach (var item in invoiceItems)
        {
            Console.WriteLine($"Title: {item.Title}, Unit Price: {item.UnitPrice}, Total Price: {item.InvoiceItemPrice}, Quantity: {item.Quantity}");
        }
    }
}
