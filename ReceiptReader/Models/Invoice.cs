namespace ReceiptReader.Models;

internal sealed class Invoice
{
    public int Id { get; set; }

    public string? ShopName { get; set; }

    public DateTime? ShoppingDate { get; set; }

    public List<Product> BoughtItems { get; set; } = [];

    public decimal? TotalSum { get; set; }

    public required string URL { get; set; }
}
