using ReceiptReader.Services;
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

        var typeOfMessage = GetMessageType(msg);

        switch (typeOfMessage)
        {
            case Enums.MessageType.StartCommand:
                Console.WriteLine("Received /start command");
                return;
            case Enums.MessageType.ValidUrl:
                Console.WriteLine("Received a valid URL");
                return;
            case Enums.MessageType.Photo:
                Console.WriteLine("Received a photo");
                return;
            case Enums.MessageType.Album:
                Console.WriteLine("Received photo album");
                return;
            case Enums.MessageType.Text:
                Console.WriteLine("Received some text");
                return;
            case Enums.MessageType.Other:
                default:
                return;
        }

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
            var largestPhoto = msg.Photo.Last();
            using var photoStream = await DownloadPhotoToMemoryAsync(largestPhoto.FileId);
            var qrText = QrCodeReader.ReadQrCode(photoStream);
            Console.WriteLine($"QR Code content: {qrText}");
        }
    }

    private async Task<MemoryStream> DownloadPhotoToMemoryAsync(string fileId)
    {
        var file = await _bot.GetFile(fileId);
        if (file.FilePath == null)
            throw new InvalidOperationException("File path is null");

        var memoryStream = new MemoryStream();
        await _bot.DownloadFile(file.FilePath, memoryStream);
        memoryStream.Position = 0; // reset stream for reading
        return memoryStream;
    }

    private static Enums.MessageType GetMessageType(Message msg)
    {
        if (msg.MediaGroupId != null)
        {
            return Enums.MessageType.Album;
        }
        if (msg.Photo != null && msg.Photo.Length > 0)
        {
            return Enums.MessageType.Photo;
        }
        if (msg.Text != null)
        {
            var text = msg.Text.Trim();

            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                return Enums.MessageType.StartCommand;
            }
            else if (UrlValidator.IsUrlValid(text))
            {
                return Enums.MessageType.ValidUrl;
            }

            return Enums.MessageType.Text;
        }

        return Enums.MessageType.Other;
    }
}
