using Microsoft.Extensions.Logging;
using Telegram.Bot;

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
            "Please send a receipt photo with a visible QR code.",
            cancellationToken: cancellationToken);
    }
}
