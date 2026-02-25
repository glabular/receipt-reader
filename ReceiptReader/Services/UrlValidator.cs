using System.Text.RegularExpressions;

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

        // Enforce HTTPS only
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // Enforce exact host match
        if (!string.Equals(uri.Host, "mapr.tax.gov.me", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Enforce exact path
        if (!uri.AbsolutePath.Equals("/ic/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Ensure fragment contains expected verify pattern
        if (!uri.Fragment.StartsWith("#/verify?iic=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
