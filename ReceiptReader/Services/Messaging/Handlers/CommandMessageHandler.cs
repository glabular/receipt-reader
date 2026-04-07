using Microsoft.Extensions.Logging;
using ReceiptReader.Services.Messaging.Commands;
using Telegram.Bot;

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

        if (context.Message.From is null)
        {
            _logger.LogWarning("Command message without sender. MessageId={MessageId}", context.Message.MessageId);
            
            return;
        }

        var request = new CommandRequest
        {
            ChatId = context.Message.Chat.Id,
            TelegramUserId = context.Message.From.Id,
            Text = context.Message.Text
        };

        var command = request.Text?.Trim().Split(' ')[0].ToLowerInvariant();
        if (command == "/spent_month")
        {
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "Calculating your total spending for this month...",
                cancellationToken: cancellationToken);
        }
        else if (command == "/spent_year")
        {
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "Calculating your total spending for this year...",
                cancellationToken: cancellationToken);
        }

        var result = await _commandsHandler.HandleAsync(request, cancellationToken);

        foreach (var message in result.Messages)
        {
            await context.Bot.SendMessage(context.Message.Chat.Id, message, cancellationToken: cancellationToken);
        }
    }
}
