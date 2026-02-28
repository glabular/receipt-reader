using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services;

internal class CommandsHandler
{
    // TODO: Add user to the signature for future logging.
    public static async Task HandleAsync(TelegramBotClient bot, Message msg)
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

    private static async Task HandleSpentMonthAsync(TelegramBotClient bot, Message msg)
    {
        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this month..."
        );


        await bot.SendMessage(msg.Chat.Id,
                    "Your total spending this month is: X...");
    }

    private static async Task HandleSpentYearAsync(TelegramBotClient bot, Message msg)
    {
        await bot.SendMessage(
            msg.Chat.Id,
            "Calculating your total spending for this year..."
        );


        await bot.SendMessage(msg.Chat.Id,
                    "Your total spending this year is: X...");
    }
}
