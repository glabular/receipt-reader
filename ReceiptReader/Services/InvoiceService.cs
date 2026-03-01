using Microsoft.EntityFrameworkCore;
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

    public bool InvoiceExists(string url)
    {
        return _dbContext.Invoices.Any(i => i.URL == url);
    }

    public decimal GetMonthlyTotalAsync(long telegramUserId, int month, int year)
    {
        throw new NotImplementedException();
    }

    public async Task<decimal?> GetYearlyTotal(long telegramUserId, int year)
    {
        var userId = await _dbContext.TelegramUsers
            .Where(u => u.TelegramUserId == telegramUserId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        // User not found
        if (userId == 0)
        {
            return null;
        }

        var invoicesQuery = _dbContext.Invoices
            .Where(i =>
                i.UserId == userId &&
                i.ShoppingDate.HasValue &&
                i.ShoppingDate.Value.Year == year);

        var hasInvoices = await invoicesQuery.AnyAsync();

        // No data for this year
        if (!hasInvoices)
        {
            return null;
        }

        return await invoicesQuery.SumAsync(i => i.TotalSum);
    }
}
