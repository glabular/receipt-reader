using ReceiptReader.Models;
using ReceiptReader.Services;
using System;

namespace ReceiptReader;

internal class Program
{
    private const string BaseUrl = "https://mapr.tax.gov.me/";
    private const string VerifyEndpoint = "ic/#/verify?iic=";
    private static readonly string VerificationUrlTemplate = $"{BaseUrl}{VerifyEndpoint}";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Please enter receipt URL:");
        string? url;
        while (!IsUrlValid(url = Console.ReadLine()?.Trim()))
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

    private static bool IsUrlValid(string? url)
    {
        return !string.IsNullOrEmpty(url) &&
               Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
               url.StartsWith(VerificationUrlTemplate);
    }
}
