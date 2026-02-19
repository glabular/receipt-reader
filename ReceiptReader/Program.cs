using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        // HtmlAgilityPack parses static HTML only;
        // use a headless browser if JavaScript-rendered content must load first.
        var options = new ChromeOptions();

        options.AddArgument("--headless");

        using var driver = new ChromeDriver(options);
        var url = Console.ReadLine()?.Trim();

        driver.Navigate().GoToUrl(url);
        
        // Wait for the page to load completely
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1000));

        wait.Until(d => d.FindElement(By.TagName("body")));

        var pageSource = driver.PageSource;
        var htmlDoc = new HtmlDocument();

        htmlDoc.LoadHtml(pageSource);

        // Locate the main invoice items list
        var invoiceList = htmlDoc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'invoice-items-list') and contains(@class,'list-unstyled')]"
        );

        if (invoiceList != null)
        {
            // Loop through each top-level <li class="invoice-item"> inside the list
            var items = invoiceList.SelectNodes("./li[contains(@class,'invoice-item')]");

            if (items != null)
            {
                foreach (var item in items)
                {
                    // Only look inside the heading <li> for the title, unit price, and price
                    var heading = item.SelectSingleNode(".//li[contains(@class,'invoice-item--heading')]");
                    if (heading != null)
                    {
                        var title = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--title')]")?.InnerText.Trim();
                        var unitPrice = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--unit-price')]")?.InnerText.Trim();
                        var price = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--price')]")?.InnerText.Trim();

                        Console.WriteLine($"Title: {title}, Unit Price: {unitPrice}, Total Price: {price}");
                    }
                }
            }
        }
    }
}