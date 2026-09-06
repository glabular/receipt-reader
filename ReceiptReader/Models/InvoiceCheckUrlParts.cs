namespace ReceiptReader.Models;

/// <summary>
/// Montenegro Tax Administration InvoiceCheck URL shell (host, path, fragment).
/// </summary>
internal static class InvoiceCheckUrlParts
{
    public const string Host = "mapr.tax.gov.me";

    public const string AbsolutePath = "/ic/";

    public const string FragmentPrefix = "#/verify?";

    public const string FragmentIicPrefix = FragmentPrefix + "iic=";

    /// <summary>
    /// Full InvoiceCheck URL prefix including scheme, host, path, and fragment query start.
    /// </summary>
    public const string ComposeBase = "https://" + Host + AbsolutePath + FragmentPrefix;
}
