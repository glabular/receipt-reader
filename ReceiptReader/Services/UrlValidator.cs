using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal sealed class UrlValidator
{
    /// <summary>
    /// Whether trimmed <paramref name="url"/> is an InvoiceCheck link we will open in the browser.
    /// Same gate as <see cref="TaxReceiptLink.TryParse"/>: mapr.tax.gov.me prefix + iic + tin.
    /// </summary>
    public static bool IsUrlValid(string? url) => TaxReceiptLink.TryParse(url, out _);
}
