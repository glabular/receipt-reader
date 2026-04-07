namespace ReceiptReader.Services.Invoices;

internal enum InvoiceProcessingStatus
{
    Success,
    Duplicate,
    InvoiceNull,
    Failure
}
