namespace ReceiptReader.Services.Messaging;

internal interface IMessageRouter
{
    Task RouteAsync(TelegramMessageContext context, CancellationToken cancellationToken = default);
}
