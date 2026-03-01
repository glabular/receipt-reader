using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
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
    private readonly InvoiceService _invoiceService;
    private readonly ReceiptClient _receiptClient;
    private readonly WeChatQrReader _qrReader;
    private readonly UserService _userService;
    private readonly CommandsHandler _commandsHandler;

    public TelegramClient(
        string token, 
        InvoiceService invoiceService,
        ReceiptClient receiptClient,
        WeChatQrReader qrReader,
        UserService userService,
        CommandsHandler commandsHandler)
    {
        _bot = new TelegramBotClient(token);
        _invoiceService = invoiceService;
        _qrReader = qrReader;
        _userService = userService;
        _commandsHandler = commandsHandler;
        _receiptClient = receiptClient;
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

        if (msg.From is null)
        {
            return;
        }

        var dbUser = await _userService.EnsureUserExistsAsync(msg.From);

        if (dbUser is null)
        {
            Console.WriteLine("Error: Could not create or fetch the user from the database.");

            return;
        }

        var typeOfMessage = GetMessageType(msg);

        await HandleMessage(typeOfMessage, msg, dbUser);
    }

    private async Task HandleValidUrlAsync(Message msg, TelegramUser user)
    {
        var url = msg.Text?.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        await ProcessInvoiceUrlAsync(msg.Chat.Id, url, user);
    }

    private async Task HandleSinglePhotoAsync(Message msg, TelegramUser user)
    {
        if (msg.Photo is null)
        {
            Console.WriteLine("Photo was null");
            await _bot.SendMessage(
                msg.Chat.Id, 
                "I couldn't detect a valid photo. Please resend the receipt image with the QR code clearly visible.");

            return;
        }

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

                await ProcessInvoiceUrlAsync(msg.Chat.Id, qrText, user);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing image: {ex.Message}");
        }
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
        if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.Trim().StartsWith('/'))
        {
            return Enums.MessageType.Command;
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
        sb.AppendLine($"💰 <b>Total Sum:</b> {invoice.TotalSum.ToString("N2") ?? "0.00"} EUR");

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

    private async Task ProcessInvoiceUrlAsync(long chatId, string url, TelegramUser telegramUser)
    {
        var exists = _invoiceService.InvoiceExists(url);

        if (exists)
        {
            await _bot.SendMessage(chatId, "⚠️ This receipt already exists.");

            return;
        }

        await _bot.SendMessage(chatId, "📄 Your receipt is being processed. This may take a few moments.");

        var invoice = _receiptClient.GetInvoice(url);

        if (invoice is null)
        {
            await _bot.SendMessage(chatId, "❌ Could not process invoice.");
            Console.WriteLine($"Invoice was null for URL: {url}");

            return;
        }

        invoice.CreatedAt = DateTime.UtcNow;
        invoice.TelegramUser = telegramUser;

        try
        {
            await _invoiceService.AddInvoiceAsync(invoice);

            Console.WriteLine("Invoice saved to database.");

            await _bot.SendMessage(
                chatId: chatId,
                text: BuildInvoiceMessage(invoice),
                parseMode: ParseMode.Html
            );
        }        
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            await _bot.SendMessage(chatId, "⚠️ Unexpected error occurred.");
        }
    }

    private async Task HandleMessage(Enums.MessageType type, Message msg, TelegramUser user)
    {
        switch (type)
        {
            case Enums.MessageType.Command:
                await _commandsHandler.HandleAsync(_bot, msg);
                break;

            case Enums.MessageType.Photo:
                await HandleSinglePhotoAsync(msg, user);
                break;

            case Enums.MessageType.Text:
                Console.WriteLine($"Received some text: {msg.Text}");
                break;

            default:
                Console.WriteLine("Received unsupported message");
                await _bot.SendMessage(msg.Chat.Id, "Please send a receipt photo with a visible QR code.");
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _qrReader.Dispose();
        _receiptClient.Dispose();
    }
}
