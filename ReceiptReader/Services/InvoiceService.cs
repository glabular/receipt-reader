using Microsoft.EntityFrameworkCore;
using ReceiptReader.Data;
using ReceiptReader.Models;

namespace ReceiptReader.Services;

internal class InvoiceService
{
    internal sealed record ItemSpendingStat(string ProductName, decimal TotalSpent);

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

    public async Task<decimal?> GetYearlyTotalAsync(long telegramUserId, int year)
    {
        return await GetUserInvoicesByDate(telegramUserId, year)
            .Select(i => (decimal?)i.TotalSum)
            .SumAsync();
    }

    public async Task<IReadOnlyList<ItemSpendingStat>> GetTopSpentItemsByMonthAsync(
        long telegramUserId,
        int month,
        int year,
        int topCount)
    {
        return await GetTopSpentItemsAsync(telegramUserId, year, topCount, month);
    }

    public async Task<IReadOnlyList<ItemSpendingStat>> GetTopSpentItemsByYearAsync(
        long telegramUserId,
        int year,
        int topCount)
    {
        return await GetTopSpentItemsAsync(telegramUserId, year, topCount);
    }

    private async Task<IReadOnlyList<ItemSpendingStat>> GetTopSpentItemsAsync(
        long telegramUserId,
        int year,
        int topCount,
        int? month = null)
    {
        var invoiceIdsQuery = GetUserInvoicesByDate(telegramUserId, year, month)
            .Select(i => i.Id);

        var rawStats = await _dbContext.Products
            .Where(p => invoiceIdsQuery.Contains(p.InvoiceId))
            .GroupBy(p => p.Name)
            .Select(g => new
            {
                ProductName = g.Key,
                TotalSpent = g.Sum(p => p.TotalPrice)
            })
            .OrderByDescending(s => s.TotalSpent)
            .Take(topCount)
            .ToListAsync();

        return rawStats
            .Select(s => new ItemSpendingStat(s.ProductName, s.TotalSpent))
            .ToList();
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
