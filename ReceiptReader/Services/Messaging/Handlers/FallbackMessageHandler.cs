using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ReceiptReader.Services.Messaging.Handlers;

internal sealed class FallbackMessageHandler : ITelegramMessageHandler
{
    private readonly UserService _userService;
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<FallbackMessageHandler> _logger;

    public FallbackMessageHandler(
        UserService userService,
        InvoiceService invoiceService,
        ILogger<FallbackMessageHandler> logger)
    {
        _userService = userService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public bool CanHandle(TelegramMessageContext context) => true;

    public async Task HandleAsync(TelegramMessageContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.DbUser?.TelegramUserId;

        if (userId is not null && context.MessageType == Enums.MessageType.Text)
        {
            var pendingCommand = await _userService.GetPendingCommandAsync(userId.Value);
            if (string.Equals(
                pendingCommand,
                UserService.SpentMonthSelectPendingCommand,
                StringComparison.Ordinal))
            {
                await HandleSpentMonthSelectInputAsync(context, userId.Value, cancellationToken);
                return;
            }

            if (string.Equals(
                pendingCommand,
                UserService.SpentYearSelectPendingCommand,
                StringComparison.Ordinal))
            {
                await HandleSpentYearSelectInputAsync(context, userId.Value, cancellationToken);
                return;
            }
        }

        _logger.LogWarning(
            "Received unsupported message type {MessageType} from user {TelegramUserId}",
            context.MessageType,
            userId);

        await context.Bot.SendMessage(
            context.Message.Chat.Id,
            "Please send a receipt photo with a visible QR code.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleSpentMonthSelectInputAsync(
        TelegramMessageContext context,
        long userId,
        CancellationToken cancellationToken)
    {
        var rawText = context.Message.Text?.Trim() ?? string.Empty;
        if (!TryParseMonthYear(rawText, out var month, out var year))
        {
            await _userService.ClearPendingCommandAsync(userId);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "Invalid date input. Command aborted.\n" +
                "Run /spent_month_select again and use:\n" +
                "- MM\n" +
                "- MM YYYY",
                cancellationToken: cancellationToken);
            return;
        }

        var totalSpent = await _invoiceService.GetMonthlyTotalAsync(userId, month, year);
        await _userService.ClearPendingCommandAsync(userId);

        if (totalSpent is null)
        {
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                $"No receipts found for {month:D2}/{year}.",
                cancellationToken: cancellationToken);
            return;
        }

        await context.Bot.SendMessage(
            context.Message.Chat.Id,
            $"Your total spending for {month:D2}/{year} is: {totalSpent.Value:F2}",
            cancellationToken: cancellationToken);
    }

    private static bool TryParseMonthYear(string input, out int month, out int year)
    {
        month = default;
        year = default;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out month) || month is < 1 or > 12)
        {
            return false;
        }

        if (parts.Length == 1)
        {
            year = DateTime.UtcNow.Year;
            return true;
        }

        if (parts[1].Length != 4 || !int.TryParse(parts[1], out year))
        {
            return false;
        }

        return true;
    }

    private async Task HandleSpentYearSelectInputAsync(
        TelegramMessageContext context,
        long userId,
        CancellationToken cancellationToken)
    {
        var rawText = context.Message.Text?.Trim() ?? string.Empty;
        if (!TryParseYear(rawText, out var year))
        {
            await _userService.ClearPendingCommandAsync(userId);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                "Invalid year input. Command aborted.\n" +
                "Run /spent_year_select again and use:\n" +
                "- YYYY",
                cancellationToken: cancellationToken);
            return;
        }

        var totalSpent = await _invoiceService.GetYearlyTotalAsync(userId, year);
        await _userService.ClearPendingCommandAsync(userId);

        if (totalSpent is null)
        {
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                $"No receipts found for {year}.",
                cancellationToken: cancellationToken);
            return;
        }

        await context.Bot.SendMessage(
            context.Message.Chat.Id,
            $"Your total spending for {year} is: {totalSpent.Value:F2}",
            cancellationToken: cancellationToken);
    }

    private static bool TryParseYear(string input, out int year)
    {
        year = default;
        if (input.Length != 4 || !int.TryParse(input, out year))
        {
            return false;
        }

        return year is >= 2000 and <= 2100;
    }
}
