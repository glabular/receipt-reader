namespace ReceiptReader.Services.Messaging.ComposeWizard;

/// <summary>
/// Prompt the compose wizard is waiting for from the user.
/// </summary>
internal enum ComposeWizardStep
{
    /// <summary>Invoice identification code (<c>iic</c>).</summary>
    AwaitingIic,

    /// <summary>Taxpayer identification number (<c>tin</c>).</summary>
    AwaitingTin,

    /// <summary>Issue date only; time is fixed at midnight with the process local offset.</summary>
    AwaitingDate,

    /// <summary>Total price (<c>prc</c>).</summary>
    AwaitingPrice
}
