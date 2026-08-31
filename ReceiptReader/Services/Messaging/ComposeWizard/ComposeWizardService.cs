using System.Globalization;
using Microsoft.Extensions.Logging;
using ReceiptReader.Models;
using ReceiptReader.Services.Invoices;
using Telegram.Bot;

namespace ReceiptReader.Services.Messaging.ComposeWizard;

/// <summary>
/// Multi-step Telegram dialog that collects InvoiceCheck parameters, composes the URL, and submits it.
/// </summary>
internal sealed class ComposeWizardService
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "d.M.yyyy",
        "d/M/yyyy"
    ];

    private readonly ComposeWizardSessionStore _sessionStore;
    private readonly InvoiceUrlSubmissionService _invoiceUrlSubmissionService;
    private readonly ILogger<ComposeWizardService> _logger;

    public ComposeWizardService(
        ComposeWizardSessionStore sessionStore,
        InvoiceUrlSubmissionService invoiceUrlSubmissionService,
        ILogger<ComposeWizardService> logger)
    {
        _sessionStore = sessionStore;
        _invoiceUrlSubmissionService = invoiceUrlSubmissionService;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new session for the user (replacing any existing one) and returns the first prompt.
    /// </summary>
    public string Start(long telegramUserId)
    {
        var session = new ComposeWizardSession();
        _sessionStore.Set(telegramUserId, session);
        return BuildPrompt(ComposeWizardStep.AwaitingIic);
    }

    /// <summary>
    /// Clears the user's session.
    /// </summary>
    /// <returns>A confirmation message, or <see langword="null"/> if no session was active.</returns>
    public string? Cancel(long telegramUserId)
    {
        if (!_sessionStore.Clear(telegramUserId))
        {
            return null;
        }

        return "Compose cancelled. You can send a receipt photo, a tax link, or use /compose to start again.";
    }

    /// <summary>
    /// Drops the user's session without a user-facing message.
    /// </summary>
    public void ClearSession(long telegramUserId) => _sessionStore.Clear(telegramUserId);

    /// <summary>
    /// Whether the user has a non-expired compose session.
    /// </summary>
    public bool HasActiveSession(long telegramUserId) => _sessionStore.HasActiveSession(telegramUserId);

    /// <summary>
    /// Prompt text for the user's current step, or <see langword="null"/> if no session is active.
    /// </summary>
    public string? GetCurrentPrompt(long telegramUserId)
    {
        if (!_sessionStore.TryGet(telegramUserId, out var session) || session is null)
        {
            return null;
        }

        return BuildPrompt(session.Step);
    }

    /// <summary>
    /// Applies <paramref name="answer"/> to the active session, advances or completes the dialog, and replies in chat.
    /// </summary>
    /// <returns><see langword="false"/> if the user has no active session; otherwise <see langword="true"/>.</returns>
    public async Task<bool> TryHandleAnswerAsync(
        ITelegramBotClient bot,
        long chatId,
        long telegramUserId,
        string answer,
        TelegramUser dbUser,
        CancellationToken cancellationToken = default)
    {
        if (!_sessionStore.TryGet(telegramUserId, out var session) || session is null)
        {
            return false;
        }

        var trimmed = answer.Trim();
        if (trimmed.Length == 0)
        {
            await bot.SendMessage(
                chatId,
                "Please send a non-empty value.\n\n" + BuildPrompt(session.Step),
                cancellationToken: cancellationToken);
            return true;
        }

        switch (session.Step)
        {
            case ComposeWizardStep.AwaitingIic:
                session.InvoiceIdentificationCode = trimmed;
                session.Step = ComposeWizardStep.AwaitingTin;
                _sessionStore.Set(telegramUserId, session);
                await bot.SendMessage(chatId, BuildPrompt(session.Step), cancellationToken: cancellationToken);
                return true;

            case ComposeWizardStep.AwaitingTin:
                session.TaxpayerIdentificationNumber = trimmed;
                session.Step = ComposeWizardStep.AwaitingDate;
                _sessionStore.Set(telegramUserId, session);
                await bot.SendMessage(chatId, BuildPrompt(session.Step), cancellationToken: cancellationToken);
                return true;

            case ComposeWizardStep.AwaitingDate:
                if (!TryParseDate(trimmed, out var created))
                {
                    await bot.SendMessage(
                        chatId,
                        "Could not parse that date. Use yyyy-MM-dd, dd.MM.yyyy, or dd/MM/yyyy.\n\n" +
                        BuildPrompt(session.Step),
                        cancellationToken: cancellationToken);
                    return true;
                }

                session.Created = created;
                session.Step = ComposeWizardStep.AwaitingPrice;
                _sessionStore.Set(telegramUserId, session);
                await bot.SendMessage(chatId, BuildPrompt(session.Step), cancellationToken: cancellationToken);
                return true;

            case ComposeWizardStep.AwaitingPrice:
                if (!TryParsePrice(trimmed, out var price))
                {
                    await bot.SendMessage(
                        chatId,
                        "Could not parse that price. Send a non-negative number, e.g. 12.50.\n\n" +
                        BuildPrompt(session.Step),
                        cancellationToken: cancellationToken);
                    return true;
                }

                session.TotalPrice = price;
                await CompleteAndSubmitAsync(bot, chatId, telegramUserId, session, dbUser, cancellationToken);
                return true;

            default:
                _logger.LogWarning(
                    "Compose wizard for user {TelegramUserId} had unexpected step {Step}",
                    telegramUserId,
                    session.Step);
                _sessionStore.Clear(telegramUserId);
                await bot.SendMessage(
                    chatId,
                    "Compose session was reset. Use /compose to start again.",
                    cancellationToken: cancellationToken);
                return true;
        }
    }

    private async Task CompleteAndSubmitAsync(
        ITelegramBotClient bot,
        long chatId,
        long telegramUserId,
        ComposeWizardSession session,
        TelegramUser dbUser,
        CancellationToken cancellationToken)
    {
        if (session.InvoiceIdentificationCode is null
            || session.TaxpayerIdentificationNumber is null
            || session.Created is null
            || session.TotalPrice is null)
        {
            _logger.LogError(
                "Compose wizard for user {TelegramUserId} completed with missing fields",
                telegramUserId);
            _sessionStore.Clear(telegramUserId);
            await bot.SendMessage(
                chatId,
                "Compose failed due to missing data. Use /compose to start again.",
                cancellationToken: cancellationToken);
            return;
        }

        var link = new TaxReceiptLink
        {
            InvoiceIdentificationCode = session.InvoiceIdentificationCode,
            TaxpayerIdentificationNumber = session.TaxpayerIdentificationNumber,
            Created = session.Created.Value,
            TotalPrice = session.TotalPrice.Value
        };

        var url = link.ComposeUrl();
        _sessionStore.Clear(telegramUserId);

        _logger.LogInformation(
            "User {TelegramUserId} composed receipt URL via wizard: {Url}",
            telegramUserId,
            url);

        await _invoiceUrlSubmissionService.SubmitAsync(bot, chatId, url, dbUser, cancellationToken);
    }

    private static bool TryParseDate(string input, out DateTimeOffset created)
    {
        created = default;

        if (!DateTime.TryParseExact(
                input,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return false;
        }

        var offset = DateTimeOffset.Now.Offset;
        created = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, offset);
        return true;
    }

    private static bool TryParsePrice(string input, out decimal price)
    {
        price = default;
        var normalized = input.Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
        {
            return false;
        }

        return price >= 0;
    }

    private static string BuildPrompt(ComposeWizardStep step) => step switch
    {
        ComposeWizardStep.AwaitingIic =>
            "Compose receipt URL — step 1/4\n\nSend the IIC / IKOF (invoice identification code).",
        ComposeWizardStep.AwaitingTin =>
            "Compose receipt URL — step 2/4\n\nSend the TIN / PIB (taxpayer identification number).",
        ComposeWizardStep.AwaitingDate =>
            "Compose receipt URL — step 3/4\n\nSend the issue date only (time will be set to 00:00:00).\n" +
            "Formats: yyyy-MM-dd, dd.MM.yyyy, or dd/MM/yyyy.",
        ComposeWizardStep.AwaitingPrice =>
            "Compose receipt URL — step 4/4\n\nSend the total price (e.g. 12.50).",
        _ => "Compose session error. Use /compose to start again."
    };
}
