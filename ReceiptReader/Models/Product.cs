namespace ReceiptReader.Models;

internal sealed class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required decimal UnitPrice { get; set; }

    public required decimal TotalPrice { get; set; }

    public required decimal Quantity { get; set; }
}
