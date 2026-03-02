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

    public async Task<decimal?> GetMonthlyTotalAsync(long telegramUserId, int month, int year)
    {
        return await GetUserInvoicesByDate(telegramUserId, year, month)
            .Select(i => (decimal?)i.TotalSum)
            .SumAsync();
    }

    public async Task<decimal?> GetYearlyTotal(long telegramUserId, int year)
    {
        return await GetUserInvoicesByDate(telegramUserId, year)
            .Select(i => (decimal?)i.TotalSum)
            .SumAsync();
    }

    private IQueryable<Invoice> GetUserInvoicesByDate(long telegramUserId, int year, int? month = null)
    {
        var userIdQuery = _dbContext.TelegramUsers
            .Where(u => u.TelegramUserId == telegramUserId)
            .Select(u => u.Id);

        var query = _dbContext.Invoices
            .Where(i => userIdQuery.Contains(i.UserId) &&
                        i.ShoppingDate.HasValue &&
                        i.ShoppingDate.Value.Year == year);

        if (month.HasValue)
        {
            query = query.Where(
                i => i.ShoppingDate.HasValue &&
                i.ShoppingDate.Value.Month == month.Value);
        }

        return query;
    }
}
