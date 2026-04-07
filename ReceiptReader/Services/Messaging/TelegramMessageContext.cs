using ReceiptReader.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramMessage = Telegram.Bot.Types.Message;

namespace ReceiptReader.Services.Messaging;

internal sealed class TelegramMessageContext
{
    public required TelegramBotClient Bot { get; init; }

    public required TelegramMessage Message { get; init; }

    public required UpdateType UpdateType { get; init; }

    public TelegramUser? DbUser { get; set; }

    public Enums.MessageType MessageType { get; set; }
}
