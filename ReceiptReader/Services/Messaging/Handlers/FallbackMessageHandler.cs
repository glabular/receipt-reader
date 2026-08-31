using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class FallbackMessageHandler : ITelegramMessageHandler
{
    private readonly ILogger<FallbackMessageHandler> _logger;

    public FallbackMessageHandler(ILogger<FallbackMessageHandler> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(TelegramMessageContext context) => true;

    public async Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.DbUser?.TelegramUserId;
        _logger.LogWarning(
            "Received unsupported message type {MessageType} from user {TelegramUserId}",
            context.MessageType,
            userId);

        await context.Bot.SendMessage(
            context.Message.Chat.Id,
            UnsupportedMessageCopy.Guidance,
            cancellationToken: cancellationToken);
    }
}
