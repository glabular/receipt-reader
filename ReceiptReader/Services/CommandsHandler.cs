using Microsoft.Extensions.Logging;
using ReceiptReader.Services.Messaging.Commands;

namespace ReceiptReader.Services;

internal sealed class CommandsHandler
{
    private readonly InvoiceService _invoiceService;
    private readonly SpendingSummaryFormatter _spendingSummaryFormatter;
    private readonly SpendingAccessResolver _spendingAccessResolver;
    private readonly ILogger<CommandsHandler> _logger;

    public CommandsHandler(
        InvoiceService invoiceService,
        SpendingSummaryFormatter spendingSummaryFormatter,
        SpendingAccessResolver spendingAccessResolver,
        ILogger<CommandsHandler> logger)
    {
        _invoiceService = invoiceService;
        _spendingSummaryFormatter = spendingSummaryFormatter;
        _spendingAccessResolver = spendingAccessResolver;
        _logger = logger;
    }

    public async Task<CommandResult> HandleAsync(CommandRequest request, CancellationToken cancellationToken = default)
    {
        var (command, args) = ParseCommandAndArguments(request.Text);
        _logger.LogInformation("Received command {Command} from user {TelegramUserId}", command, request.TelegramUserId);

        switch (command)
        {
            case "/start":
                return BuildSingleMessageResult(GetStartText());

            case "/help":
                return BuildSingleMessageResult(GetHelpText());

            case "/spent_month":
                return await HandleSpentMonthAsync(request, args, cancellationToken);

            case "/spent_year":
                return await HandleSpentYearAsync(request, args, cancellationToken);

            default:
                _logger.LogWarning("Unknown command received: {Command} from {UserId}", command, request.TelegramUserId);
                return BuildSingleMessageResult("Unknown command.");
        }
    }

    private static string GetHelpText()
    {
        return "Available commands:\n\n" +
               "/start – Introduction\n" +
               "/help – Show available commands\n" +
               "/spent_month – View spending for current month or use M[ M] YY|YYYY\n" +
               "/spent_year – View spending for current year or use YY|YYYY\n" +
               "Or use the Menu button\n\n" +
               "To analyze a receipt, send a clear photo with a visible QR code.";
    }

    private static string GetStartText()
    {
        return "Welcome.\n\n" +
               "Send a receipt photo with a clearly visible QR code.\n" +
               "I will extract the items and calculate the total amount.";
    }

    private async Task<CommandResult> HandleSpentMonthAsync(
        CommandRequest request,
        string arguments,
        CancellationToken cancellationToken)
    {
        int month;
        int year;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            month = DateTime.UtcNow.Month;
            year = DateTime.UtcNow.Year;
        }
        else if (!TryParseMonthArguments(arguments, out month, out year))
        {
            return BuildSingleMessageResult(
                "Invalid /spent_month arguments.\n" +
                "Use:\n" +
                "- /spent_month\n" +
                "- /spent_month M\n" +
                "- /spent_month MM\n" +
                "- /spent_month M YY\n" +
                "- /spent_month MM YYYY");
        }
        var requesterUserId = request.TelegramUserId;
        var ownerUserId = _spendingAccessResolver.ResolveOwnerTelegramUserId(requesterUserId);
        var messages = new List<string>();
        var totalSpent = await _invoiceService.GetMonthlyTotalAsync(ownerUserId, month, year);

        if (totalSpent is null)
        {
            _logger.LogInformation(
                "No monthly data found for requester {RequesterUserId} using owner {OwnerUserId} (Month: {Month})",
                requesterUserId,
                ownerUserId,
                month);
            messages.Add($"📭 No receipts found for {month:D2}/{year}.");
        }
        else
        {
            try
            {
                var topItems = await _invoiceService.GetTopSpentItemsByMonthAsync(ownerUserId, month, year, topCount: 5);
                _logger.LogInformation(
                    "Successfully calculated monthly total for requester {RequesterUserId} using owner {OwnerUserId}: {Total}",
                    requesterUserId,
                    ownerUserId,
                    totalSpent);
                messages.Add(_spendingSummaryFormatter.Build(
                    periodLabel: $"month ({month:D2}/{year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 5 most bought items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load monthly top item stats for requester {RequesterUserId} using owner {OwnerUserId}",
                    requesterUserId,
                    ownerUserId);
                messages.Add($"💸 Your total spending for month ({month:D2}/{year}) is: {totalSpent.Value:F2} EUR");
            }
        }

        return new CommandResult { Messages = messages };
    }

    private async Task<CommandResult> HandleSpentYearAsync(
        CommandRequest request,
        string arguments,
        CancellationToken cancellationToken)
    {
        int year;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            year = DateTime.UtcNow.Year;
        }
        else if (!TryParseYearArguments(arguments, out year))
        {
            return BuildSingleMessageResult(
                "Invalid /spent_year arguments.\n" +
                "Use:\n" +
                "- /spent_year\n" +
                "- /spent_year YY\n" +
                "- /spent_year YYYY");
        }

        var requesterUserId = request.TelegramUserId;
        var ownerUserId = _spendingAccessResolver.ResolveOwnerTelegramUserId(requesterUserId);
        var messages = new List<string>();
        var totalSpent = await _invoiceService.GetYearlyTotalAsync(ownerUserId, year);

        if (totalSpent is null)
        {
            _logger.LogInformation(
                "No yearly data found for requester {RequesterUserId} using owner {OwnerUserId} (Year: {Year})",
                requesterUserId,
                ownerUserId,
                year);
            messages.Add($"📭 No receipts found for {year}.");
        }
        else
        {
            try
            {
                var topItems = await _invoiceService.GetTopSpentItemsByYearAsync(ownerUserId, year, topCount: 10);
                _logger.LogInformation(
                    "Successfully calculated yearly total for requester {RequesterUserId} using owner {OwnerUserId}: {Total}",
                    requesterUserId,
                    ownerUserId,
                    totalSpent);
                messages.Add(_spendingSummaryFormatter.Build(
                    periodLabel: $"year ({year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 10 most bought items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load yearly top item stats for requester {RequesterUserId} using owner {OwnerUserId}",
                    requesterUserId,
                    ownerUserId);
                messages.Add($"💸 Your total spending for year ({year}) is: {totalSpent.Value:F2} EUR");
            }
        }

        return new CommandResult { Messages = messages };
    }

    private static (string command, string arguments) ParseCommandAndArguments(string? text)
    {
        var trimmedText = text?.Trim() ?? string.Empty;
        if (trimmedText.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        var splitIndex = trimmedText.IndexOf(' ');
        if (splitIndex < 0)
        {
            return (NormalizeCommandToken(trimmedText), string.Empty);
        }

        var command = NormalizeCommandToken(trimmedText[..splitIndex]);
        var arguments = trimmedText[(splitIndex + 1)..].Trim();
        return (command, arguments);
    }

    private static string NormalizeCommandToken(string token)
    {
        var commandOnly = token.Split('@')[0];
        return commandOnly.ToLowerInvariant();
    }

    private static bool TryParseMonthArguments(string arguments, out int month, out int year)
    {
        month = default;
        year = default;

        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

        return TryParseYearToken(parts[1], out year);
    }

    private static bool TryParseYearArguments(string arguments, out int year)
    {
        year = default;
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 && TryParseYearToken(parts[0], out year);
    }

    private static bool TryParseYearToken(string token, out int year)
    {
        year = default;
        if (!int.TryParse(token, out var parsedYear))
        {
            return false;
        }

        if (token.Length == 2)
        {
            parsedYear += 2000;
        }
        else if (token.Length != 4)
        {
            return false;
        }

        if (parsedYear is < 2000 or > 2100)
        {
            return false;
        }

        year = parsedYear;
        return true;
    }

    private static CommandResult BuildSingleMessageResult(string message)
    {
        return new CommandResult
        {
            Messages = [message]
        };
    }
}
