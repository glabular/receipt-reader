namespace ReceiptReader.Services.Messaging;

/// <summary>
/// Shared user-facing copy for how to submit Montenegro tax receipts.
/// </summary>
internal static class ReceiptIntakeCopy
{
    public const string HowToSubmit =
        "You can submit a receipt in any of these ways:\n" +
        "• Photo with a clearly visible QR code\n" +
        "• Paste an InvoiceCheck link from mapr.tax.gov.me\n" +
        "• /compose — enter IIC, TIN, and date if you only have the printed fields";

    public const string InvalidLink =
        "⚠️ That is not a valid Montenegro tax receipt link.\n" +
        "Only https://mapr.tax.gov.me/ic/#/verify?... links are accepted.";

    public const string InvalidQr =
        "⚠️ This QR code is not a valid Montenegro tax receipt link (mapr.tax.gov.me).";
}
