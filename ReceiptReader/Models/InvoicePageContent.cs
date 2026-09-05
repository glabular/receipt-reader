namespace ReceiptReader.Models;

/// <summary>
/// Receipt content parsed from tax page HTML.
/// </summary>
internal sealed class InvoicePageContent
{
    public required decimal TotalSum { get; init; }

    public DateTime? ShoppingDate { get; init; }

    public string? ShopName { get; init; }

    public required List<Product> BoughtItems { get; init; }
}
