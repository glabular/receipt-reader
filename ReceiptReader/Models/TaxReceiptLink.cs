namespace ReceiptReader.Models;

/// <summary>
/// Parameters of a Montenegro tax InvoiceCheck (<c>mapr.tax.gov.me/ic</c>) link.
/// </summary>
internal sealed class TaxReceiptLink
{
    /// <summary>
    /// URL key <c>iic</c>. Receipt labels: IIC, IKOF (Issuer Identification Code).
    /// </summary>
    public required string InvoiceIdentificationCode { get; init; }

    /// <summary>
    /// URL key <c>tin</c>. Receipt labels: TIN, PIB (poreski identifikacioni broj / poreski broj izdavaoca).
    /// </summary>
    public required string TaxpayerIdentificationNumber { get; init; }

    /// <summary>
    /// URL key <c>crtd</c>. Receipt labels: Date and time of issue; datum i vrijeme kreiranja.
    /// </summary>
    public required DateTimeOffset Created { get; init; }

    /// <summary>
    /// URL key <c>prc</c>. Receipt labels: Total price; ukupna cijena.
    /// </summary>
    public required decimal TotalPrice { get; init; }

    /// <summary>
    /// URL key <c>ord</c>. Receipt labels: Invoice number; broj naloga za račun.
    /// </summary>
    public int? OrdinalNumber { get; init; }

    /// <summary>
    /// URL key <c>bu</c>. Receipt labels: Business unit code; poslovna jedinica.
    /// </summary>
    public string? BusinessUnitCode { get; init; }

    /// <summary>
    /// URL key <c>cr</c>. Receipt labels: Cash register code; ENU (elektronski naplatni uređaj).
    /// </summary>
    public string? CashRegisterCode { get; init; }

    /// <summary>
    /// URL key <c>sw</c>. Receipt labels: Software code; kod softvera.
    /// </summary>
    public string? SoftwareCode { get; init; }

    /// <summary>
    /// Builds the InvoiceCheck verify URL for these parameters.
    /// </summary>
    public string ComposeUrl()
    {
        throw new NotImplementedException();
    }
}
