using Microsoft.EntityFrameworkCore;
using ReceiptReader.Data;
using ReceiptReader.Models;
using ReceiptReader.Services;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader;

internal sealed class TelegramClient : IAsyncDisposable
{
    private readonly TelegramBotClient _bot;
    private readonly HashSet<string> _processedGroups = [];
    private readonly InvoicesDbContext _invoicesDbContext = new();
    private readonly ReceiptClient _receiptClient = new();
    private readonly WeChatQrReader _qrReader = new();

    public TelegramClient(string token)
    {
        _bot = new TelegramBotClient(token);
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
        var url = msg.Text!;
        var exists = await _invoicesDbContext.Invoices.AnyAsync(i => i.URL == url);

        if (exists)
        {
            await _bot.SendMessage(msg.Chat.Id, "⚠️ This receipt already exists.");

            return;
        }

        var invoice = _receiptClient.GetInvoice(url);

        if (invoice == null)
        {
            await _bot.SendMessage(msg.Chat.Id, "❌ Could not process invoice.");
            Console.WriteLine("Invoice was null.");
            return;
        }

        try
        {
            var saved = await _invoicesDbContext.AddInvoiceAsync(invoice);

            if (!saved)
            {
                // TODO: Logging
                await _bot.SendMessage(msg.Chat.Id, "⚠️ This receipt already exists.");
                return;
            }

            Console.WriteLine("Invoice saved to database.");            

            await _bot.SendMessage(
                chatId: msg.Chat.Id,
                text: BuildInvoiceMessage(invoice),
                parseMode: ParseMode.Html // Enables the bold/bullet formatting
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            await _bot.SendMessage(msg.Chat.Id, "⚠️ Unexpected error occurred.");
        }        
    }

    private async Task HandleSinglePhotoAsync(Message msg)
    {
        try
        {
            var largestPhoto = msg.Photo.Last();
            var qrText = await ExtractQrTextAsync(largestPhoto);

            if (string.IsNullOrWhiteSpace(qrText))
            {
                await _bot.SendMessage(msg.Chat.Id, "🔍 No QR code detected. Please make sure the photo is clear.");
            }
            else if (!UrlValidator.IsUrlValid(qrText))
            {
                await _bot.SendMessage(msg.Chat.Id, "⚠️ This QR code is not a valid Montenegro tax receipt.");
                Console.WriteLine($"User scanned wrong QR: {qrText}");
            }
            else
            {
                // Success.
                Console.WriteLine($"Processing valid receipt: {qrText}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing image: {ex.Message}");
        }
    }

    private async Task HandleStartCommandAsync(Message msg)
    {
        await _bot.SendMessage(msg.Chat.Id, "Welcome!");
    }

    private async Task<MemoryStream> DownloadPhotoToMemoryAsync(string fileId)
    {
        var file = await _bot.GetFile(fileId);
        if (file.FilePath == null)
        {
            throw new InvalidOperationException("File path is null");
        }

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

    private static string BuildInvoiceMessage(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("✅ <b>Invoice Added!</b>");
        sb.AppendLine(new string('_', 35));
        sb.AppendLine($"🛒 <b>Shop:</b> {invoice.ShopName ?? "Unknown"}");
        sb.AppendLine($"📅 <b>Date:</b> {invoice.ShoppingDate?.ToString("dd.MM.yyyy HH:mm") ?? "N/A"}");
        sb.AppendLine();

        if (invoice.BoughtItems != null && invoice.BoughtItems.Count != 0)
        {
            sb.AppendLine("📦 <b>Items:</b>");
            foreach (var item in invoice.BoughtItems)
            {
                var qty = item.Quantity.ToString("G29") ?? "0";
                var unit = item.UnitPrice.ToString("N2") ?? "0.00";
                var total = item.TotalPrice.ToString("N2") ?? "0.00";
                var prettyName = char.ToUpper(item.Name[0]) + item.Name[1..].ToLower();

                sb.AppendLine($"- {prettyName} | {qty}x{unit} = <b>{total}</b>");
            }
        }

        sb.AppendLine(new string('_', 35));
        sb.AppendLine($"💰 <b>Total Sum:</b> {invoice.TotalSum?.ToString("N2") ?? "0.00"} EUR");

        return sb.ToString();
    }

    private async Task<string?> ExtractQrTextAsync(PhotoSize photo)
    {
        using var photoStream = await DownloadPhotoToMemoryAsync(photo.FileId);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

        try
        {
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                await photoStream.CopyToAsync(fileStream);
            }

            return _qrReader.ReadQr(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _qrReader.Dispose();
        _receiptClient.Dispose();
        await _invoicesDbContext.DisposeAsync();
    }
}
