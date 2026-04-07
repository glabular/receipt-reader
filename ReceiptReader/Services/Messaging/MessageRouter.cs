using ReceiptReader.Services.Messaging.Handlers;
using Microsoft.Extensions.Logging;

namespace ReceiptReader.Services.Messaging;

internal sealed class MessageRouter : IMessageRouter
{
    private readonly IReadOnlyList<ITelegramMessageHandler> _handlers;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(IEnumerable<ITelegramMessageHandler> handlers, ILogger<MessageRouter> logger)
    {
        _handlers = handlers.ToList();
        _logger = logger;
    }

    public async Task RouteAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(context));

        if (handler is null)
        {
            _logger.LogError(
                "No handler found for message type {MessageType}, message ID {MessageId}",
                context.MessageType,
                context.Message.MessageId);

            throw new InvalidOperationException($"No handler registered for message type '{context.MessageType}'.");
        }

        await handler.HandleAsync(context, cancellationToken);
    }
}
