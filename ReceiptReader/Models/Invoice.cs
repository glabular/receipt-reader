namespace ReceiptReader.Models;

internal sealed class Invoice
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ShopName { get; set; }

    /// <summary>
    /// Issue date/time from the tax page HTML — source of truth for when the receipt was issued.
    /// </summary>
    public DateTime? ShoppingDate { get; set; }

    public List<Product> BoughtItems { get; set; } = [];

    /// <summary>
    /// Total from the tax page HTML — source of truth (not URL <c>prc</c>).
    /// </summary>
    public decimal TotalSum { get; set; }

    public required string URL { get; set; }

    /// <summary>
    /// URL key <c>iic</c>. Used as the global unique receipt identity.
    /// </summary>
    public required string InvoiceIdentificationCode { get; set; }

    /// <summary>
    /// URL key <c>tin</c>. Link addressing metadata from the submitted URL.
    /// </summary>
    public required string TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    /// URL key <c>ord</c>. Link addressing metadata from the submitted URL.
    /// </summary>
    public int? OrdinalNumber { get; set; }

    /// <summary>
    /// URL key <c>bu</c>. Link addressing metadata from the submitted URL.
    /// </summary>
    public string? BusinessUnitCode { get; set; }

    /// <summary>
    /// URL key <c>cr</c>. Link addressing metadata from the submitted URL.
    /// </summary>
    public string? CashRegisterCode { get; set; }

    /// <summary>
    /// URL key <c>sw</c>. Link addressing metadata from the submitted URL.
    /// </summary>
    public string? SoftwareCode { get; set; }

    public TelegramUser? TelegramUser { get; set; }

    public int UserId { get; set; }
}
