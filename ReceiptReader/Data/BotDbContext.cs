using Microsoft.EntityFrameworkCore;
using ReceiptReader.Models;

namespace ReceiptReader.Data;

internal class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

    internal DbSet<Invoice> Invoices { get; set; }

    internal DbSet<Product> Products { get; set; }

    internal DbSet<TelegramUser> TelegramUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.URL)
            .IsUnique();

        modelBuilder.Entity<TelegramUser>()
            .HasIndex(u => u.TelegramUserId)
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.TelegramUser)
            .WithMany(u => u.Invoices)
            .HasForeignKey(i => i.TelegramUserId);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.BoughtItems)
            .HasForeignKey(p => p.InvoiceId);
    }
}
