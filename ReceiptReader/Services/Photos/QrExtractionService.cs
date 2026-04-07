using ReceiptReader.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services.Photos;

internal sealed class QrExtractionService
{
    private readonly PhotoStorageService _photoStorageService;
    private readonly WeChatQrReader _qrReader;
    private readonly ILogger<QrExtractionService> _logger;

    public QrExtractionService(
        PhotoStorageService photoStorageService,
        WeChatQrReader qrReader,
        ILogger<QrExtractionService> logger)
    {
        _photoStorageService = photoStorageService;
        _qrReader = qrReader;
        _logger = logger;
    }

    public async Task<string?> ExtractQrTextAsync(TelegramBotClient bot, PhotoSize photo, TelegramUser user)
    {
        var filePath = await _photoStorageService.SavePhotoToLogsAsync(bot, photo, user);

        if (filePath is null)
        {
            return null;
        }

        var qrText = _qrReader.ReadQr(filePath);

        if (string.IsNullOrWhiteSpace(qrText))
        {
            _logger.LogInformation("No QR code detected for user {TelegramUserId}", user.TelegramUserId);
        }

        return qrText;
    }
}
