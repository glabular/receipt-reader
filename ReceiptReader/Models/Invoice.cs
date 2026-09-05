namespace ReceiptReader.Models;

internal sealed class Invoice
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ShopName { get; set; }

    public DateTime? ShoppingDate { get; set; }

    public List<Product> BoughtItems { get; set; } = [];

    public decimal TotalSum { get; set; }

    public required string URL { get; set; }

    /// <summary>
    /// URL key <c>iic</c>. Receipt labels: IIC, IKOF.
    /// </summary>
    public required string InvoiceIdentificationCode { get; set; }

    /// <summary>
    /// URL key <c>tin</c>. Receipt labels: TIN, PIB.
    /// </summary>
    public required string TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    /// URL key <c>crtd</c>. Receipt issue time from the link — not <see cref="CreatedAt"/>.
    /// </summary>
    public required DateTimeOffset Created { get; set; }

    /// <summary>
    /// URL key <c>ord</c>.
    /// </summary>
    public int? OrdinalNumber { get; set; }

    /// <summary>
    /// URL key <c>bu</c>.
    /// </summary>
    public string? BusinessUnitCode { get; set; }

    /// <summary>
    /// URL key <c>cr</c>.
    /// </summary>
    public string? CashRegisterCode { get; set; }

    /// <summary>
    /// URL key <c>sw</c>.
    /// </summary>
    public string? SoftwareCode { get; set; }

    public TelegramUser? TelegramUser { get; set; }

    public int UserId { get; set; }
}
