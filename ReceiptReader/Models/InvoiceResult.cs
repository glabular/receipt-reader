namespace ReceiptReader.Models;

internal sealed class InvoiceResult
{
    public int Id { get; set; }

    public string? ShopName { get; set; }

    public DateTime? ShoppingDate { get; set; }

    public List<InvoiceItem>? BoughtItems { get; set; }

    public decimal? TotalSum { get; set; }

    public string? URL { get; set; }
}
