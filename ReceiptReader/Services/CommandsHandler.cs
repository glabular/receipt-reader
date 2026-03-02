using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services;

internal class CommandsHandler
{
    private readonly InvoiceService _invoiceService;

    public CommandsHandler(InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    // TODO: Add user to the signature for future logging.
    public async Task HandleAsync(TelegramBotClient bot, Message msg)
    {
        var command = msg.Text?.Trim().Split(' ')[0].ToLower();

        Console.WriteLine($"Received command: {command}");

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
        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this month..."
        );

        var totalSpent = await _invoiceService.GetMonthlyTotalAsync(msg.From.Id, DateTime.UtcNow.Month, DateTime.UtcNow.Year);

        if (totalSpent is null)
        {
            await bot.SendMessage(msg.Chat.Id,
                "No receipts found for this month.");
        }
        else
        {
            await bot.SendMessage(msg.Chat.Id,
                $"Your total spending this month is: {totalSpent.Value:F2}");
        }
    }

    private async Task HandleSpentYearAsync(TelegramBotClient bot, Message msg)
    {
        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this year..."
        );

        var totalSpent = await _invoiceService.GetYearlyTotal(msg.From.Id, DateTime.UtcNow.Year);

        if (totalSpent is null)
        {
            await bot.SendMessage(msg.Chat.Id,
                "No receipts found for this year.");
        }
        else
        {
            await bot.SendMessage(msg.Chat.Id,
                $"Your total spending this year is: {totalSpent.Value:F2}");
        }
    }
}
