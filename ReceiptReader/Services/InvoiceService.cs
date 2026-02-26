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
        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
    }
}
