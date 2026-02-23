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

        htmlDoc.LoadHtml(pageSource);
        result.TotalSum = ParseTotal(htmlDoc);
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

                var unitPriceRaw = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--unit-price')]")?.InnerText.Trim() ?? string.Empty;

                if (!decimal.TryParse(unitPriceRaw, out var unitPrice))
                {
                    unitPrice = 0m;
                }

                var totalPriceRaw = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--price')]")?.InnerText.Trim() ?? string.Empty;

                if (!decimal.TryParse(totalPriceRaw, out var totalPrice))
                {
                    totalPrice = 0m;
                }

                var quantityRaw = details.SelectSingleNode(".//span[contains(@class,'invoice-item--quantity')]")?.InnerText.Trim() ?? string.Empty;

                if (!int.TryParse(quantityRaw, out var quantity))
                {
                    quantity = 0;
                }

                var product = new Product
                {
                    Name = heading.SelectSingleNode(".//span[contains(@class,'invoice-item--title')]")?.InnerText.Trim() ?? string.Empty,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Quantity = quantity
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

    private static decimal? ParseTotal(HtmlDocument htmlDoc)
    {
        var totalNode = htmlDoc.DocumentNode.SelectSingleNode("//p[contains(@class,'card-amount')]");

        if (totalNode == null)
        {
            return null;
        }

        var totalText = totalNode.InnerText
            .Replace("EUR", string.Empty)
            .Trim();

        if (decimal.TryParse(
            totalText,
            NumberStyles.Any,
            CultureInfo.GetCultureInfo("sr-Latn-ME"),
            out var totalAmount))
        {
            return totalAmount;
        }

        return null;
    }
}
