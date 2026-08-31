using ReceiptReader.Services.Invoices;
using ReceiptReader.Services.Photos;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class PhotoMessageHandler : ITelegramMessageHandler
{
    private readonly QrExtractionService _qrExtractionService;
    private readonly InvoiceUrlSubmissionService _invoiceUrlSubmissionService;
    private readonly ILogger<PhotoMessageHandler> _logger;

    public PhotoMessageHandler(
        QrExtractionService qrExtractionService,
        InvoiceUrlSubmissionService invoiceUrlSubmissionService,
        ILogger<PhotoMessageHandler> logger)
    {
        _qrExtractionService = qrExtractionService;
        _invoiceUrlSubmissionService = invoiceUrlSubmissionService;
        _logger = logger;
    }

    public bool CanHandle(TelegramMessageContext context) => context.MessageType == Enums.MessageType.Photo;

    public async Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        var dbUser = context.DbUser;

        if (dbUser is null)
        {
            _logger.LogError("Photo handler received message without database user context.");
            return;
        }

        if (context.Message.Photo is null || context.Message.Photo.Length == 0)
        {
            _logger.LogWarning(
                "Received a photo message without photo content from user {TelegramUserId}",
                dbUser.TelegramUserId);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "I couldn't detect a valid photo. Please resend the receipt image with the QR code clearly visible.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var largestPhoto = context.Message.Photo.Last();
            var qrText = await _qrExtractionService.ExtractQrTextAsync(context.Bot, largestPhoto, dbUser);

            if (string.IsNullOrWhiteSpace(qrText))
            {
                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    "🔍 No QR code detected. Please make sure the photo is clear.",
                    cancellationToken: cancellationToken);

                return;
            }

            if (!UrlValidator.IsUrlValid(qrText))
            {
                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    "⚠️ This QR code is not a valid Montenegro tax receipt.",
                    cancellationToken: cancellationToken);
                _logger.LogWarning("User {TelegramUserId} scanned an invalid QR code: {QrText}", dbUser.TelegramUserId, qrText);
                
                return;
            }

            _logger.LogInformation("User {TelegramUserId} scanned a valid QR code: {QrText}", dbUser.TelegramUserId, qrText);
            await _invoiceUrlSubmissionService.SubmitAsync(
                context.Bot,
                context.Message.Chat.Id,
                qrText,
                dbUser,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing photo message from user {TelegramUserId}", dbUser.TelegramUserId);
            
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "⚠️ Unexpected error occurred.",
                cancellationToken: cancellationToken);
        }
    }
}
