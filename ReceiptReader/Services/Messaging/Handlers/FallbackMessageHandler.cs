using Microsoft.Extensions.Logging;
using System.Text;
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

        try
        {
            var topItems = await _invoiceService.GetTopSpentItemsByMonthAsync(userId, month, year, topCount: 5);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                BuildSpendingSummaryMessage(
                    periodLabel: $"month ({month:D2}/{year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 5 most bought items"),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load monthly top item stats for user {UserId}", userId);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                $"💸 Your total spending for this month ({month:D2}/{year}) is: {totalSpent.Value:F2} EUR",
                cancellationToken: cancellationToken);
        }
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

        try
        {
            var topItems = await _invoiceService.GetTopSpentItemsByYearAsync(userId, year, topCount: 10);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                BuildSpendingSummaryMessage(
                    periodLabel: $"year ({year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 10 most bought items"),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load yearly top item stats for user {UserId}", userId);
            await context.Bot.SendMessage(
                context.Message.Chat.Id,
                $"💸 Your total spending for this year ({year}) is: {totalSpent.Value:F2} EUR",
                cancellationToken: cancellationToken);
        }
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

    private static string BuildSpendingSummaryMessage(
        string periodLabel,
        decimal totalSpent,
        IReadOnlyList<InvoiceService.ItemSpendingStat> topItems,
        string topItemsTitle)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"💸 Your total spending for this {periodLabel} is: {totalSpent:F2} EUR");

        if (topItems.Count == 0)
        {
            sb.AppendLine("🧾 No item statistics available for this period.");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();
        sb.AppendLine($"📊 {topItemsTitle}:");
        for (var i = 0; i < topItems.Count; i++)
        {
            var item = topItems[i];
            sb.AppendLine($"{i + 1}. {FormatProductName(item.ProductName)} — {item.TotalSpent:F2} EUR");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Unknown";
        }

        return name.Length == 1
            ? char.ToUpper(name[0]).ToString()
            : char.ToUpper(name[0]) + name[1..].ToLower();
    }
}
