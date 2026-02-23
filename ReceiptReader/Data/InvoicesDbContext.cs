using Microsoft.EntityFrameworkCore;
using ReceiptReader.Models;

namespace ReceiptReader.Data;

internal class InvoicesDbContext : DbContext
{
    internal DbSet<Invoice> Invoices { get; set; }

    internal DbSet<Product> Products { get; set; }

    public async Task AddInvoiceAsync(Invoice invoice)
    {
        Invoices.Add(invoice);
        SaveChanges();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost;Database=InvoicesDb;Trusted_Connection=True;TrustServerCertificate=True");
    }
}
