using System.ComponentModel.DataAnnotations.Schema;

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
    /// URL key <c>iic</c>. Filled from <see cref="TaxReceiptLink"/>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public string InvoiceIdentificationCode { get; set; } = null!;

    /// <summary>
    /// URL key <c>tin</c>. Filled from <see cref="TaxReceiptLink"/>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public string TaxpayerIdentificationNumber { get; set; } = null!;

    /// <summary>
    /// URL key <c>crtd</c>. Receipt issue time from the link — not <see cref="CreatedAt"/>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// URL key <c>ord</c>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public int? OrdinalNumber { get; set; }

    /// <summary>
    /// URL key <c>bu</c>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public string? BusinessUnitCode { get; set; }

    /// <summary>
    /// URL key <c>cr</c>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public string? CashRegisterCode { get; set; }

    /// <summary>
    /// URL key <c>sw</c>.
    /// NotMapped until the IIC migration adds the column.
    /// </summary>
    [NotMapped]
    public string? SoftwareCode { get; set; }

    public TelegramUser? TelegramUser { get; set; }

    public int UserId { get; set; }
}
