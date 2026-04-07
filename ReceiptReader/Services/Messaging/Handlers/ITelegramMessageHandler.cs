namespace ReceiptReader.Services.Messaging.Handlers;

internal interface ITelegramMessageHandler
{
    bool CanHandle(TelegramMessageContext context);

    Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default);
}
