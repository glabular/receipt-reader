namespace ReceiptReader.Models;

internal sealed class InvoiceItem
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string UnitPrice { get; set; }

    public required string InvoiceItemPrice { get; set; }

    public required string Quantity { get; set; }
}
