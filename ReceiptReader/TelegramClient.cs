using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader;

internal sealed class TelegramClient
{
    private readonly TelegramBotClient _bot;
    private readonly HashSet<string> _processedGroups = [];

    public TelegramClient()
    {
        _bot = new TelegramBotClient("");
    }

    public async Task StartAsync()
    {        
        var me = await _bot.GetMe();
        _bot.OnMessage += OnMessage;
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        Console.WriteLine($"Received {type} in {msg.Chat}");

        if (msg.Text is not null && msg.Text == "/start")
        {
            await _bot.SendMessage(msg.Chat.Id, "Welcome!");
        }

        if (msg.MediaGroupId != null)
        {
            // If this message is part of a new album, track it and skip duplicates; otherwise ignore already processed 
            if (!_processedGroups.Contains(msg.MediaGroupId))
            {
                _processedGroups.Clear(); // clear old group
                _processedGroups.Add(msg.MediaGroupId);
            }
            else
            {
                return; // already processed
            }
        }

        // Handle single photo
        if (msg.Photo != null && msg.Photo.Length > 0)
        {
            var largestPhoto = msg.Photo.OrderByDescending(p => p.FileSize).First();
            await SavePhotoAsync(largestPhoto.FileId);
        }
    }

    private async Task SavePhotoAsync(string fileId)
    {
        var file = await _bot.GetFile(fileId);

        if (file.FilePath == null)
        {
            return;
        }

        var fileName = $"{Guid.NewGuid()}.jpg";
        var savePath = Path.Combine("downloads", fileName);

        Directory.CreateDirectory("downloads");

        await using var stream = File.Create(savePath);
        await _bot.DownloadFile(file.FilePath, stream);

        Console.WriteLine($"Image saved: {savePath}");
    }
}
