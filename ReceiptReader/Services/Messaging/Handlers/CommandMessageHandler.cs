using ReceiptReader.Services;
using Microsoft.Extensions.Logging;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class CommandMessageHandler : ITelegramMessageHandler
{
    private readonly CommandsHandler _commandsHandler;
    private readonly ILogger<CommandMessageHandler> _logger;

    public CommandMessageHandler(CommandsHandler commandsHandler, ILogger<CommandMessageHandler> logger)
    {
        _commandsHandler = commandsHandler;
        _logger = logger;
    }

    public bool CanHandle(TelegramMessageContext context) => context.MessageType == Enums.MessageType.Command;

    public async Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Routing command message {MessageId} from user {TelegramUserId}",
            context.Message.MessageId,
            context.DbUser?.TelegramUserId);

        await _commandsHandler.HandleAsync(context.Bot, context.Message);
    }
}
