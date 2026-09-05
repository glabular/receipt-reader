using ReceiptReader.Models;
using Microsoft.Extensions.Logging;

namespace ReceiptReader.Services.Invoices;

internal sealed class InvoiceProcessingService
{
    private readonly InvoiceService _invoiceService;
    private readonly ReceiptClient _receiptClient;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        InvoiceService invoiceService,
        ReceiptClient receiptClient,
        ILogger<InvoiceProcessingService> logger)
    {
        _invoiceService = invoiceService;
        _receiptClient = receiptClient;
        _logger = logger;
    }

    public async Task<InvoiceProcessingResult> ProcessAsync(
        string url,
        TelegramUser telegramUser,
        CancellationToken cancellationToken = default)
    {
        if (!TaxReceiptLink.TryParse(url, out var link))
        {
            _logger.LogWarning("Could not parse receipt URL: {Url}", url);
            return new InvoiceProcessingResult
            {
                Status = InvoiceProcessingStatus.InvoiceNull
            };
        }

        if (_invoiceService.InvoiceExistsByIic(link.InvoiceIdentificationCode))
        {
            return new InvoiceProcessingResult
            {
                Status = InvoiceProcessingStatus.Duplicate
            };
        }

        try
        {
            var invoice = await _receiptClient.GetInvoiceAsync(url, cancellationToken);

            if (invoice is null)
            {
                _logger.LogWarning(
                    "Invoice was null for URL: {Url} (IIC: {Iic})",
                    url,
                    link.InvoiceIdentificationCode);
                return new InvoiceProcessingResult
                {
                    Status = InvoiceProcessingStatus.InvoiceNull
                };
            }

            link.ApplyTo(invoice);
            invoice.URL = url;
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UserId = telegramUser.Id;
            invoice.TelegramUser = null;
            await _invoiceService.AddInvoiceAsync(invoice);

            _logger.LogInformation(
                "Invoice added successfully for user {TelegramUserId} with IIC: {Iic} URL: {Url}",
                telegramUser.TelegramUserId,
                link.InvoiceIdentificationCode,
                url);

            return new InvoiceProcessingResult
            {
                Status = InvoiceProcessingStatus.Success,
                Invoice = invoice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error adding invoice for user {TelegramUserId} with IIC: {Iic} URL: {Url}",
                telegramUser.TelegramUserId,
                link.InvoiceIdentificationCode,
                url);

            return new InvoiceProcessingResult
            {
                Status = InvoiceProcessingStatus.Failure,
                ErrorMessage = ex.Message
            };
        }
    }
}
