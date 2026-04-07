using ReceiptReader.Models;
using System.Text;

namespace ReceiptReader.Services.Invoices;

internal sealed class InvoiceMessageFormatter
{
    public string BuildInvoiceMessage(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("✅ <b>Invoice Added!</b>");
        sb.AppendLine(new string('_', 35));
        sb.AppendLine($"🛒 <b>Shop:</b> {invoice.ShopName ?? "Unknown"}");
        sb.AppendLine($"📅 <b>Date:</b> {invoice.ShoppingDate?.ToString("dd.MM.yyyy HH:mm") ?? "N/A"}");
        sb.AppendLine();

        if (invoice.BoughtItems is not null && invoice.BoughtItems.Count != 0)
        {
            sb.AppendLine("📦 <b>Items:</b>");
            foreach (var item in invoice.BoughtItems)
            {
                var qty = item.Quantity.ToString("G29");
                var unit = item.UnitPrice.ToString("N2");
                var total = item.TotalPrice.ToString("N4");
                var prettyName = char.ToUpper(item.Name[0]) + item.Name[1..].ToLower();

                sb.AppendLine($"- {prettyName} | {qty}x{unit} = <b>{total}</b>");
            }
        }

        sb.AppendLine(new string('_', 35));
        sb.AppendLine($"💰 <b>Total Sum:</b> {invoice.TotalSum.ToString("N2")} EUR");

        return sb.ToString();
    }
}
