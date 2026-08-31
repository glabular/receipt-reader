using Microsoft.Extensions.Logging;
using ReceiptReader.Services.Messaging.Commands;
using ReceiptReader.Services.Messaging.ComposeWizard;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class CommandMessageHandler : ITelegramMessageHandler
{
    private readonly CommandsHandler _commandsHandler;
    private readonly ComposeWizardService _composeWizardService;
    private readonly ILogger<CommandMessageHandler> _logger;

    public CommandMessageHandler(
        CommandsHandler commandsHandler,
        ComposeWizardService composeWizardService,
        ILogger<CommandMessageHandler> logger)
    {
        _commandsHandler = commandsHandler;
        _composeWizardService = composeWizardService;
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

        var commandToken = request.Text?.Trim().Split(' ')[0] ?? string.Empty;
        var command = commandToken.Split('@')[0].ToLowerInvariant();

        // Leaving the wizard for any command other than /compose or /cancel.
        // /compose restarts; /cancel clears explicitly.
        if (command is not "/compose" and not "/cancel")
        {
            _composeWizardService.ClearSession(request.TelegramUserId);
        }

        if (command == "/spent_month" || command == "/spent_year")
        {
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "Calculating your spending summary...",
                cancellationToken: cancellationToken);
        }

        var result = await _commandsHandler.HandleAsync(request, cancellationToken);

        foreach (var message in result.Messages)
        {
            if (message.Contains("<b>", StringComparison.Ordinal))
            {
                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    message,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
