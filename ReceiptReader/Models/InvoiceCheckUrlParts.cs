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

    public const string ComposeBase = "https://" + Host + AbsolutePath + FragmentPrefix;
}
