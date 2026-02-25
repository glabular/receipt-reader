using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReceiptReader.Services;

internal class CommandsHandler
{
    public static async Task HandleAsync(TelegramBotClient bot, Message msg)
    {
        var command = msg.Text?.Trim().Split(' ')[0].ToLower();

        Console.WriteLine($"Received command: {command}");

        switch (command)
        {
            case "/start":
            case "/help":
                await bot.SendMessage(msg.Chat.Id,
                    "Send a receipt photo with a visible QR code to get items and total price.");
                break;

            case "/spent_month":
                await bot.SendMessage(msg.Chat.Id,
                    "Your total spending this month is: X...");
                break;

            case "/spent_year":
                await bot.SendMessage(msg.Chat.Id,
                    "Your total spending this year is: X...");
                break;

            default:
                await bot.SendMessage(msg.Chat.Id, "Unknown command.");
                break;
        }
    }
}
