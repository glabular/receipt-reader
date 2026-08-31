using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal sealed class UrlValidator
{
    public static bool IsUrlValid(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!string.Equals(uri.Host, InvoiceCheckUrlParts.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uri.AbsolutePath.Equals(InvoiceCheckUrlParts.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uri.Fragment.StartsWith(InvoiceCheckUrlParts.FragmentIicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
