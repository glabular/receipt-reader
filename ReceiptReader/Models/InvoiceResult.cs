namespace ReceiptReader.Models;

internal sealed class InvoiceResult
{
    public string? ShopName { get; set; }

    public DateTime? ShoppingDate { get; set; }

    public List<InvoiceItem>? BoughtItems { get; set; }

    public decimal? TotalSum { get; set; }
}
