namespace ReceiptReader.Models;

internal sealed class TelegramUser
{
    public int Id { get; set; }

    public long TelegramUserId { get; set; }

    public List<Invoice> Invoices { get; set; } = [];
}
