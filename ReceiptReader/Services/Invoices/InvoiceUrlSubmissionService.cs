using ReceiptReader.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader.Services.Invoices;

internal sealed class InvoiceUrlSubmissionService
{
    private readonly InvoiceProcessingService _invoiceProcessingService;
    private readonly InvoiceMessageFormatter _invoiceMessageFormatter;
    private readonly ILogger<InvoiceUrlSubmissionService> _logger;

    public InvoiceUrlSubmissionService(
        InvoiceProcessingService invoiceProcessingService,
        InvoiceMessageFormatter invoiceMessageFormatter,
        ILogger<InvoiceUrlSubmissionService> logger)
    {
        _invoiceProcessingService = invoiceProcessingService;
        _invoiceMessageFormatter = invoiceMessageFormatter;
        _logger = logger;
    }

    public async Task SubmitAsync(
        ITelegramBotClient bot,
        long chatId,
        string url,
        TelegramUser dbUser,
        CancellationToken cancellationToken = default)
    {
        await bot.SendMessage(
            chatId,
            "📄 Your receipt is being processed. This may take a few moments.",
            cancellationToken: cancellationToken);

        var result = await _invoiceProcessingService.ProcessAsync(url, dbUser, cancellationToken);

        switch (result.Status)
        {
            case InvoiceProcessingStatus.Duplicate:
                await bot.SendMessage(
                    chatId,
                    "⚠️ This receipt already exists.",
                    cancellationToken: cancellationToken);
                break;

            case InvoiceProcessingStatus.InvoiceNull:
                await bot.SendMessage(
                    chatId,
                    "❌ Could not process invoice.",
                    cancellationToken: cancellationToken);
                break;

            case InvoiceProcessingStatus.Success:
                if (result.Invoice is null)
                {
                    _logger.LogError(
                        "Invoice processing reported success without invoice for user {TelegramUserId}",
                        dbUser.TelegramUserId);
                    await bot.SendMessage(
                        chatId,
                        "⚠️ Unexpected error occurred.",
                        cancellationToken: cancellationToken);
                    break;
                }

                await bot.SendMessage(
                    chatId: chatId,
                    text: _invoiceMessageFormatter.BuildInvoiceMessage(result.Invoice),
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;

            default:
                await bot.SendMessage(
                    chatId,
                    "⚠️ Unexpected error occurred.",
                    cancellationToken: cancellationToken);
                break;
        }
    }
}
