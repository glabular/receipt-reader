namespace ReceiptReader.Services;

internal sealed class UrlValidatior
{
    private const string BaseUrl = "https://mapr.tax.gov.me/";
    private const string VerifyEndpoint = "ic/#/verify?iic=";
    private static readonly string VerificationUrlTemplate = $"{BaseUrl}{VerifyEndpoint}";

    public static bool IsUrlValid(string? url)
    {
        return !string.IsNullOrEmpty(url) &&
               Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
               url.StartsWith(VerificationUrlTemplate);
    }
}
