namespace ReceiptReader.Services.Messaging.Commands;

internal sealed class CommandRequest
{
    public required long ChatId { get; init; }

    public required long TelegramUserId { get; init; }

    public string? Text { get; init; }
}
