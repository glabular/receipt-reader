using ReceiptReader.Data;
using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal class InvoiceService
{
    private readonly BotDbContext _dbContext;

    public InvoiceService(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddInvoiceAsync(Invoice invoice)
    {
        // TODO: Decide whether Add or AddAsync to use here.
        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
    }

    public bool IsInvoiceExist(string url)
    {
        return _dbContext.Invoices.Any(i => i.URL == url);
    }
}
