using HtmlAgilityPack;
using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal sealed class InvoiceParser
{
    public static List<InvoiceItem> ParseInvoicePage(string pageSource)
    {
        var htmlDoc = new HtmlDocument();

        htmlDoc.LoadHtml(pageSource);

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
                return invoiceItems;
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

        return invoiceItems;
    }
}