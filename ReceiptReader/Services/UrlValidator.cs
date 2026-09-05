using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal sealed class UrlValidator
{
    /// <summary>
    /// Whether <paramref name="url"/> is a fully parseable InvoiceCheck link.
    /// Delegates to <see cref="TaxReceiptLink.TryParse"/> so routing and persistence agree.
    /// </summary>
    public static bool IsUrlValid(string? url) => TaxReceiptLink.TryParse(url, out _);
}
