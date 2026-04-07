using System.Text;

namespace ReceiptReader.Services;

internal sealed class SpendingSummaryFormatter
{
    public string Build(
        string periodLabel,
        decimal totalSpent,
        IReadOnlyList<InvoiceService.ItemSpendingStat> topItems,
        string topItemsTitle)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"💸 Your total spending for this {periodLabel} is: <b>{totalSpent:F2} EUR</b>");

        if (topItems.Count == 0)
        {
            sb.AppendLine("🧾 No item statistics available for this period.");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();
        sb.AppendLine($"📊 <b>{topItemsTitle}:</b>");

        for (var i = 0; i < topItems.Count; i++)
        {
            var item = topItems[i];
            sb.AppendLine($"{i + 1}. {FormatProductName(item.ProductName)} — <b>{item.TotalSpent:F2} EUR</b>");
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
