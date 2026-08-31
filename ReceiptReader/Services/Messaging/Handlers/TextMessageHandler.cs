using ReceiptReader.Services.Invoices;
using ReceiptReader.Services.Messaging.ComposeWizard;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class TextMessageHandler : ITelegramMessageHandler
{
    private readonly InvoiceUrlSubmissionService _invoiceUrlSubmissionService;
    private readonly ComposeWizardService _composeWizardService;
    private readonly ILogger<TextMessageHandler> _logger;

    public TextMessageHandler(
        InvoiceUrlSubmissionService invoiceUrlSubmissionService,
        ComposeWizardService composeWizardService,
        ILogger<TextMessageHandler> logger)
    {
        _invoiceUrlSubmissionService = invoiceUrlSubmissionService;
        _composeWizardService = composeWizardService;
        _logger = logger;
    }

    public bool CanHandle(TelegramMessageContext context) => context.MessageType == Enums.MessageType.Text;

    public async Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        var dbUser = context.DbUser;

        if (dbUser is null)
        {
            _logger.LogError("Text handler received message without database user context.");
            return;
        }

        var candidate = context.Message.Text?.Trim();

        if (UrlValidator.IsUrlValid(candidate))
        {
            _composeWizardService.ClearSession(dbUser.TelegramUserId);

            try
            {
                _logger.LogInformation(
                    "User {TelegramUserId} submitted a valid receipt link: {Url}",
                    dbUser.TelegramUserId,
                    candidate);

                await _invoiceUrlSubmissionService.SubmitAsync(
                    context.Bot,
                    context.Message.Chat.Id,
                    candidate!,
                    dbUser,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text message from user {TelegramUserId}", dbUser.TelegramUserId);

                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    "⚠️ Unexpected error occurred.",
                    cancellationToken: cancellationToken);
            }

            return;
        }

        if (_composeWizardService.HasActiveSession(dbUser.TelegramUserId))
        {
            try
            {
                await _composeWizardService.TryHandleAnswerAsync(
                    context.Bot,
                    context.Message.Chat.Id,
                    dbUser.TelegramUserId,
                    candidate ?? string.Empty,
                    dbUser,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling compose wizard answer from user {TelegramUserId}",
                    dbUser.TelegramUserId);

                await context.Bot.SendMessage(
                    context.Message.Chat.Id,
                    "⚠️ Unexpected error occurred.",
                    cancellationToken: cancellationToken);
            }

            return;
        }

        _logger.LogInformation(
            "Ignoring non-link text from user {TelegramUserId}",
            dbUser.TelegramUserId);

        await context.Bot.SendMessage(
            context.Message.Chat.Id,
            UnsupportedMessageCopy.Guidance,
            cancellationToken: cancellationToken);
    }
}
