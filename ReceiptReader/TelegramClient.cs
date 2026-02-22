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
        try
        {
            var me = await _bot.GetMe();
            Console.WriteLine($"{me.Username}: authentication successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to authenticate bot: {ex.Message}");
            throw;
        }

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
                await HandleStartCommandAsync(msg);
                return;
            case Enums.MessageType.ValidUrl:
                Console.WriteLine("Received a valid URL");
                await HandleUrlAsync(msg);
                return;
            case Enums.MessageType.Photo:
                Console.WriteLine("Received a photo");
                await HandleSinglePhotoAsync(msg);
                return;
            // TODO: Come up with better way to handle albums without the hashset.
            case Enums.MessageType.Album:
                // Skip if already processed
                if (msg.MediaGroupId != null)
                {
                    if (!_processedGroups.Add(msg.MediaGroupId))
                    {
                        return; // Already processed this album
                    }
                }

                Console.WriteLine("Received photo album");

                // Take only one photo from the album and pass to single photo handler
                await HandleSinglePhotoAsync(msg);   

                return;
            case Enums.MessageType.Text:
                Console.WriteLine("Received some text");
                Console.WriteLine($"TODO: Remove the echo functionality. Echo: {msg.Text}");
                return;
            case Enums.MessageType.Other:
                default:
                return;
        }
    }

    private async Task HandleUrlAsync(Message msg)
    {
        throw new NotImplementedException();
    }

    private async Task HandleSinglePhotoAsync(Message msg)
    {
        var largestPhoto = msg.Photo.Last();
        using var photoStream = await DownloadPhotoToMemoryAsync(largestPhoto.FileId);
        var qrText = QrCodeReader.ReadQrCode(photoStream);
        Console.WriteLine($"QR Code content: {qrText}");
    }

    private async Task HandleStartCommandAsync(Message msg)
    {
        await _bot.SendMessage(msg.Chat.Id, "Welcome!");
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
