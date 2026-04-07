using Microsoft.Extensions.Logging;
using ReceiptReader.Services.Messaging.Commands;

namespace ReceiptReader.Services;

internal sealed class CommandsHandler
{
    private readonly InvoiceService _invoiceService;
    private readonly UserService _userService;
    private readonly SpendingSummaryFormatter _spendingSummaryFormatter;
    private readonly ILogger<CommandsHandler> _logger;

    public CommandsHandler(
        InvoiceService invoiceService,
        UserService userService,
        SpendingSummaryFormatter spendingSummaryFormatter,
        ILogger<CommandsHandler> logger)
    {
        _invoiceService = invoiceService;
        _userService = userService;
        _spendingSummaryFormatter = spendingSummaryFormatter;
        _logger = logger;
    }

    public async Task<CommandResult> HandleAsync(CommandRequest request, CancellationToken cancellationToken = default)
    {
        var command = request.Text?.Trim().Split(' ')[0].ToLowerInvariant();
        _logger.LogInformation("Received command {Command} from user {TelegramUserId}", command, request.TelegramUserId);
        await _userService.ClearPendingCommandAsync(request.TelegramUserId);

        switch (command)
        {
            case "/start":
                return BuildSingleMessageResult(GetStartText());

            case "/help":
                return BuildSingleMessageResult(GetHelpText());

            case "/spent_month":
                return await HandleSpentMonthAsync(request, cancellationToken);

            case "/spent_month_select":
                return await HandleSpentMonthSelectAsync(request, cancellationToken);

            case "/spent_year":
                return await HandleSpentYearAsync(request, cancellationToken);

            case "/spent_year_select":
                return await HandleSpentYearSelectAsync(request, cancellationToken);

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
               "/spent_month – View total spending for the current month\n" +
               "/spent_month_select – Select month interactively (MM or MM YYYY)\n" +
               "/spent_year – View total spending for the current year\n" +
               "/spent_year_select – Select year interactively (YYYY)\n" +
               "Or use the Menu button\n\n" +
               "To analyze a receipt, send a clear photo with a visible QR code.";
    }

    private static string GetStartText()
    {
        return "Welcome.\n\n" +
               "Send a receipt photo with a clearly visible QR code.\n" +
               "I will extract the items and calculate the total amount.";
    }

    private async Task<CommandResult> HandleSpentMonthAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        var userId = request.TelegramUserId;
        var month = DateTime.UtcNow.Month;
        var year = DateTime.UtcNow.Year;
        var messages = new List<string>();
        var totalSpent = await _invoiceService.GetMonthlyTotalAsync(request.TelegramUserId, month, year);

        if (totalSpent is null)
        {
            _logger.LogInformation("No monthly data found for user {UserId} (Month: {Month})", userId, month);
            messages.Add("📭 No receipts found for this month yet.");
        }
        else
        {
            try
            {
                var topItems = await _invoiceService.GetTopSpentItemsByMonthAsync(userId, month, year, topCount: 5);
                _logger.LogInformation("Successfully calculated monthly total for user {UserId}: {Total}", userId, totalSpent);
                messages.Add(_spendingSummaryFormatter.Build(
                    periodLabel: $"month ({month:D2}/{year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 5 most bought items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load monthly top item stats for user {UserId}", userId);
                messages.Add($"💸 Your total spending for this month ({month:D2}/{year}) is: {totalSpent.Value:F2} EUR");
            }
        }

        return new CommandResult { Messages = messages };
    }

    private async Task<CommandResult> HandleSpentYearAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        var userId = request.TelegramUserId;
        var year = DateTime.UtcNow.Year;
        var messages = new List<string>();
        var totalSpent = await _invoiceService.GetYearlyTotalAsync(request.TelegramUserId, year);

        if (totalSpent is null)
        {
            _logger.LogInformation("No yearly data found for user {UserId} (Year: {Year})", userId, year);
            messages.Add("📭 No receipts found for this year yet.");
        }
        else
        {
            try
            {
                var topItems = await _invoiceService.GetTopSpentItemsByYearAsync(userId, year, topCount: 10);
                _logger.LogInformation("Successfully calculated yearly total for user {UserId}: {Total}", userId, totalSpent);
                messages.Add(_spendingSummaryFormatter.Build(
                    periodLabel: $"year ({year})",
                    totalSpent: totalSpent.Value,
                    topItems: topItems,
                    topItemsTitle: "Top 10 most bought items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load yearly top item stats for user {UserId}", userId);
                messages.Add($"💸 Your total spending for this year ({year}) is: {totalSpent.Value:F2} EUR");
            }
        }

        return new CommandResult { Messages = messages };
    }

    private async Task<CommandResult> HandleSpentMonthSelectAsync(
        CommandRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.SetPendingCommandAsync(request.TelegramUserId, UserService.SpentMonthSelectPendingCommand);
        _logger.LogInformation(
            "User {UserId} started conversational /spent_month_select flow",
            request.TelegramUserId);

        return BuildSingleMessageResult(
            "Enter month to check spending:\n" +
            "- MM (uses current year)\n" +
            "- MM YYYY (specific year)\n\n" +
            "Examples: 03 or 03 2026");
    }

    private async Task<CommandResult> HandleSpentYearSelectAsync(
        CommandRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.SetPendingCommandAsync(request.TelegramUserId, UserService.SpentYearSelectPendingCommand);
        _logger.LogInformation(
            "User {UserId} started conversational /spent_year_select flow",
            request.TelegramUserId);

        return BuildSingleMessageResult(
            "Enter year to check spending:\n" +
            "- YYYY\n\n" +
            "Example: 2026");
    }

    private static CommandResult BuildSingleMessageResult(string message)
    {
        return new CommandResult
        {
            Messages = [message]
        };
    }
}
