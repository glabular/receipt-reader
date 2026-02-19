using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ReceiptReader.Models;
using System;

namespace ReceiptReader;

internal class Program
{
    private const string BaseUrl = "https://mapr.tax.gov.me/";
    private const string VerifyEndpoint = "ic/#/verify?iic=";
    private static readonly string VerificationUrlTemplate = $"{BaseUrl}{VerifyEndpoint}";

    static async Task Main(string[] args)
    {
        string? url;
        while (!IsUrlValid(url = Console.ReadLine()?.Trim()))
        {
            Console.Error.WriteLine("Invalid URL provided. Please try again:\n");
        }

        // HtmlAgilityPack parses static HTML only;
        // use a headless browser if JavaScript-rendered content must load first.
        var options = new ChromeOptions();

        options.AddArgument("--headless");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--blink-settings=imagesEnabled=false");

        using var driver = new ChromeDriver(options);
        
        driver.Navigate().GoToUrl(url);

        // Wait for the page to load completely
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        wait.Until(d => {
            var elements = d.FindElements(By.ClassName("invoice-items-list"));

            return elements.Count > 0 && elements[0].Displayed;
        });

        var pageSource = driver.PageSource;
        var htmlDoc = new HtmlDocument();

        htmlDoc.LoadHtml(pageSource);

        // Locate the main invoice items list
        var invoiceList = htmlDoc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'invoice-items-list') and contains(@class,'list-unstyled')]"
        );

        var invoiceItems = new List<InvoiceItem>();

        if (invoiceList != null)
        {
            var items = invoiceList.SelectNodes("./li[contains(@class,'invoice-item')]");
            if (items == null)
            {
                Console.Error.WriteLine("No invoice items found.");
                return;
            }

            foreach (var item in items)
            {
                var heading = item.SelectSingleNode(".//li[contains(@class,'invoice-item--heading')]");
                var details = item.SelectSingleNode(".//li[contains(@class,'invoice-item--details')]");

                if (heading == null)
                {
                    continue;
                }

                var invoiceItem = new InvoiceItem
                {
                    Title = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--title')]")?.InnerText.Trim() ?? string.Empty,
                    UnitPrice = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--unit-price')]")?.InnerText.Trim() ?? string.Empty,
                    InvoiceItemPrice = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--price')]")?.InnerText.Trim() ?? string.Empty,
                    Quantity = details.SelectSingleNode(".//span[contains(@class,'invoice-item--quantity')]")?.InnerText.Trim() ?? string.Empty
                };

                invoiceItems.Add(invoiceItem);                
            }
            
        }

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
