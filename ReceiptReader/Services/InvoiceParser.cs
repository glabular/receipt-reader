using HtmlAgilityPack;
using ReceiptReader.Models;
using System.Globalization;

namespace ReceiptReader.Services;

internal sealed class InvoiceParser
{
    public static Invoice? ParseInvoicePage(string pageUrl, string pageSource)
    {
        var result = new Invoice();
        var htmlDoc = new HtmlDocument();
        var totalNode = htmlDoc.DocumentNode.SelectSingleNode("//p[contains(@class,'card-amount')]");

        htmlDoc.LoadHtml(pageSource);
        result.TotalSum = ParseDecimal(totalNode);
        result.ShoppingDate = ParseDate(htmlDoc);
        result.ShopName = ParseShopName(htmlDoc);
        result.BoughtItems = ParseInvoiceItems(htmlDoc);  
        result.URL = pageUrl;

        return result;
    }

    private static List<Product>? ParseInvoiceItems(HtmlDocument htmlDoc)
    {
        var invoiceList = htmlDoc.DocumentNode.SelectSingleNode(
            "//ul[contains(@class,'invoice-items-list') and contains(@class,'list-unstyled')]"
        );

        var products = new List<Product>();

        if (invoiceList != null)
        {
            var items = invoiceList.SelectNodes("./li[contains(@class,'invoice-item')]");
            if (items == null)
            {
                return products;
            }

            foreach (var item in items)
            {
                var heading = item.SelectSingleNode(".//li[contains(@class,'invoice-item--heading')]");
                var details = item.SelectSingleNode(".//li[contains(@class,'invoice-item--details')]");

                if (heading == null)
                {
                    continue;
                }

                var unitPriceNode = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--unit-price')]");
                var totalPriceNode = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--price')]");
                var quantityNode = details.SelectSingleNode(".//span[contains(@class,'invoice-item--quantity')]");

                var product = new Product
                {
                    Name = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--title')]")?.InnerText.Trim() ?? string.Empty,
                    UnitPrice = ParseDecimal(unitPriceNode) ?? 0m,
                    TotalPrice = ParseDecimal(totalPriceNode) ?? 0m,
                    Quantity = ParseDecimal(quantityNode) ?? 0m
                };

                products.Add(product);
            }
        }

        return products;
    }

    private static string? ParseShopName(HtmlDocument htmlDoc)
    {
        var nameNode = htmlDoc.DocumentNode.SelectSingleNode("//li[contains(@class,'invoice-basic-info--business-name')]");
        string businessName = nameNode?.InnerText.Trim() ?? string.Empty;

        return businessName;
    }

    private static DateTime? ParseDate(HtmlDocument htmlDoc)
    {
        var dateNode = htmlDoc.DocumentNode.SelectSingleNode("//li[contains(@class,'invoice-basic-info--date')]");
        string dateText = dateNode?.InnerText.Trim() ?? string.Empty;

        if (DateTime.TryParseExact(
            dateText,
            "dd/MM/yyyy HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime shoppingDate))
        {
            return shoppingDate;
        }

        return null;
    }

    private static decimal? ParseDecimal(HtmlNode? node)
    {
        if (node == null)
        {
            return null;
        }

        var text = node.InnerText.Replace("EUR", string.Empty).Trim();

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("sr-Latn-ME"), out var value))
        {
            return value;
        }

        return null;
    }
}
