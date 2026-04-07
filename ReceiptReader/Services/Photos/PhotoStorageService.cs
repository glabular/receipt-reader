using ReceiptReader.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services.Photos;

internal sealed class PhotoStorageService
{
    private readonly string _logsDirectory;
    private readonly ILogger<PhotoStorageService> _logger;

    public PhotoStorageService(string logsDirectory, ILogger<PhotoStorageService> logger)
    {
        _logsDirectory = logsDirectory;
        _logger = logger;
    }

    public async Task<MemoryStream> DownloadPhotoToMemoryAsync(TelegramBotClient bot, string fileId)
    {
        var file = await bot.GetFile(fileId);

        if (file.FilePath is null)
        {
            throw new InvalidOperationException("File path is null");
        }

        var memoryStream = new MemoryStream();
        await bot.DownloadFile(file.FilePath, memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<string?> SavePhotoToLogsAsync(TelegramBotClient bot, PhotoSize photo, TelegramUser user)
    {
        using var photoStream = await DownloadPhotoToMemoryAsync(bot, photo.FileId);
        var now = DateTime.UtcNow;
        var fileName = $"{now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.jpg";
        var yearFolder = now.ToString("yyyy");
        var monthFolder = now.ToString("MM");
        var userFolder = Path.Combine(_logsDirectory, user.TelegramUserId.ToString(), yearFolder, monthFolder);
        var filePath = Path.Combine(userFolder, fileName);

        Directory.CreateDirectory(userFolder);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await photoStream.CopyToAsync(fileStream);

        _logger.LogInformation("Saved photo to {PhotoPath}", filePath);
        return filePath;
    }
}
