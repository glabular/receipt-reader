using Microsoft.Extensions.Logging;
using ReceiptReader.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReceiptReader.Services.Messaging;

internal sealed class TelegramUpdateProcessor
{
    private readonly UserService _userService;
    private readonly TelegramMessageClassifier _classifier;
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<TelegramUpdateProcessor> _logger;

    public TelegramUpdateProcessor(
        UserService userService,
        TelegramMessageClassifier classifier,
        IMessageRouter messageRouter,
        ILogger<TelegramUpdateProcessor> logger)
    {
        _userService = userService;
        _classifier = classifier;
        _messageRouter = messageRouter;
        _logger = logger;
    }

    public async Task ProcessAsync(TelegramBotClient bot, Message message, UpdateType updateType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received {UpdateType} in {ChatId}", updateType, message.Chat);

        if (message.From is null)
        {
            _logger.LogWarning("Received message with no sender info: {MessageId}", message.MessageId);
            return;
        }

        var dbUser = await _userService.EnsureUserExistsAsync(message.From);

        if (dbUser is null)
        {
            _logger.LogError("Failed to create or fetch user with Telegram ID {TelegramUserId}", message.From.Id);
            return;
        }

        var context = new TelegramMessageContext
        {
            Bot = bot,
            Message = message,
            UpdateType = updateType,
            DbUser = dbUser,
            MessageType = _classifier.Classify(message)
        };

        await _messageRouter.RouteAsync(context, cancellationToken);
    }
}
