namespace ReceiptReader.Services.Messaging.ComposeWizard;

/// <summary>
/// Per-user state while <c>/compose</c> is collecting IIC, TIN, and date.
/// </summary>
internal sealed class ComposeWizardSession
{
    /// <summary>
    /// Which value is being asked for right now.
    /// </summary>
    public ComposeWizardStep Step { get; set; } = ComposeWizardStep.AwaitingIic;

    /// <summary>
    /// URL key <c>iic</c>. Receipt labels: IIC, IKOF. Set after the IIC step; otherwise <see langword="null"/>.
    /// </summary>
    public string? InvoiceIdentificationCode { get; set; }

    /// <summary>
    /// URL key <c>tin</c>. Receipt labels: TIN, PIB. Set after the TIN step; otherwise <see langword="null"/>.
    /// </summary>
    public string? TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    /// Receipt date as midnight (<c>00:00:00</c>) using the server's time-zone offset. Set after the date step; otherwise <see langword="null"/>.
    /// </summary>
    public DateTimeOffset? Created { get; set; }

    /// <summary>
    /// When this session was created (UTC).
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this session was last updated (UTC). Idle sessions expire after 30 minutes.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
}
