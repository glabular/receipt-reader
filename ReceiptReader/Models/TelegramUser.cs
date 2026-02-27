namespace ReceiptReader.Models;

internal sealed class TelegramUser
{
    public int Id { get; set; }

    public long TelegramUserId { get; set; }

    public List<Invoice> Invoices { get; set; } = [];

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
