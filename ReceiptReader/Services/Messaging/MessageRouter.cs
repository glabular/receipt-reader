using ReceiptReader.Services.Messaging.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ReceiptReader.Services.Messaging;

internal sealed class MessageRouter : IMessageRouter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(IServiceProvider serviceProvider, ILogger<MessageRouter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task RouteAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ITelegramMessageHandler handler = context.MessageType switch
        {
            Enums.MessageType.Command => _serviceProvider.GetRequiredService<CommandMessageHandler>(),
            Enums.MessageType.Photo => _serviceProvider.GetRequiredService<PhotoMessageHandler>(),
            _ => ResolveFallbackWithWarning(context)
        };

        await handler.HandleAsync(context, cancellationToken);
    }

    private FallbackMessageHandler ResolveFallbackWithWarning(TelegramMessageContext context)
    {
        if (context.MessageType is Enums.MessageType.Album)
        {
            _logger.LogWarning(
                "No dedicated handler for message type {MessageType}, using fallback. Message ID {MessageId}",
                context.MessageType,
                context.Message.MessageId);
        }

        return _serviceProvider.GetRequiredService<FallbackMessageHandler>();
    }
}
