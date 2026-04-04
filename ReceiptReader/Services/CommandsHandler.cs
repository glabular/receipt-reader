using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services;

internal class CommandsHandler
{
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<CommandsHandler> _logger;

    public CommandsHandler(InvoiceService invoiceService, ILogger<CommandsHandler> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    // TODO: Add user to the signature for future logging.
    public async Task HandleAsync(TelegramBotClient bot, Message msg)
    {
        var command = msg.Text?.Trim().Split(' ')[0].ToLower();

        if (msg.From is null)
        {
            _logger.LogWarning("Received message with no sender info: {MessageId}", msg.MessageId);
            return;
        }

        _logger.LogInformation("Received command {Command} from user {TelegramUserId}", command, msg.From.Id);

        switch (command)
        {
            case "/start":
                await HandleStartAsync(bot, msg);
                break;

            case "/help":
                await HandleHelpAsync(bot, msg);
                break;

            case "/spent_month":
                await HandleSpentMonthAsync(bot, msg);
                break;

            case "/spent_year":
                await HandleSpentYearAsync(bot, msg);
                break;

            default:
                _logger.LogWarning("Unknown command received: {Command} from {UserId}", command, msg.From.Id);
                await bot.SendMessage(msg.Chat.Id, "Unknown command.");
                break;
        }
    }

    private static async Task HandleHelpAsync(TelegramBotClient bot, Message msg)
    {
        await bot.SendMessage(
            msg.Chat.Id,
            "Available commands:\n\n" +
            "/start – Introduction\n" +
            "/help – Show available commands\n" +
            "/spent_month – View total spending for the current month\n" +
            "/spent_year – View total spending for the current year\n" +
            "Or use the Menu button\n\n" +
            "To analyze a receipt, send a clear photo with a visible QR code."
        );
    }

    private static async Task HandleStartAsync(TelegramBotClient bot, Message msg)
    {
        await bot.SendMessage(
            msg.Chat.Id,
            "Welcome.\n\n" +
            "Send a receipt photo with a clearly visible QR code.\n" +
            "I will extract the items and calculate the total amount."
        );
    }

    private async Task HandleSpentMonthAsync(TelegramBotClient bot, Message msg)
    {
        var userId = msg.From!.Id; // ! msg.From is checked for null in the caller method.
        var month = DateTime.UtcNow.Month;
        var year = DateTime.UtcNow.Year;

        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this month..."
        );

        var totalSpent = await _invoiceService.GetMonthlyTotalAsync(msg.From.Id, month, year);

        if (totalSpent is null)
        {
            _logger.LogInformation("No monthly data found for user {UserId} (Month: {Month})", userId, month);
            await bot.SendMessage(msg.Chat.Id,
                "No receipts found for this month.");
        }
        else
        {
            _logger.LogInformation("Successfully calculated monthly total for user {UserId}: {Total}", userId, totalSpent);
            await bot.SendMessage(msg.Chat.Id,
                $"Your total spending this month is: {totalSpent.Value:F2}");
        }
    }

    private async Task HandleSpentYearAsync(TelegramBotClient bot, Message msg)
    {
        var userId = msg.From!.Id; // ! msg.From is checked for null in the caller method.
        var year = DateTime.UtcNow.Year;

        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this year..."
        );

        var totalSpent = await _invoiceService.GetYearlyTotalAsync(msg.From.Id, year);

        if (totalSpent is null)
        {
            _logger.LogInformation("No yearly data found for user {UserId} (Year: {Year})", userId, year);
            await bot.SendMessage(msg.Chat.Id,
                "No receipts found for this year.");
        }
        else
        {
            _logger.LogInformation("Successfully calculated yearly total for user {UserId}: {Total}", userId, totalSpent);
            await bot.SendMessage(msg.Chat.Id,
                $"Your total spending this year is: {totalSpent.Value:F2}");
        }
    }
}
