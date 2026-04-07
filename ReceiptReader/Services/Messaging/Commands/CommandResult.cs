namespace ReceiptReader.Services.Messaging.Commands;

internal sealed class CommandResult
{
    public required IReadOnlyList<string> Messages { get; init; }
}
