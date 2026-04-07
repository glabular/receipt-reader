using ReceiptReader.Models;

namespace ReceiptReader.Services.Invoices;

internal sealed class InvoiceProcessingResult
{
    public InvoiceProcessingStatus Status { get; init; }

    public Invoice? Invoice { get; init; }

    public string? ErrorMessage { get; init; }
}
