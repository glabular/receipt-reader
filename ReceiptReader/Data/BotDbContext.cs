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
            .HasForeignKey(i => i.UserId);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.BoughtItems)
            .HasForeignKey(p => p.InvoiceId);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalSum)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.TotalPrice)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Product>()
            .Property(p => p.UnitPrice)
            .HasPrecision(18, 4);

        // Safer for items sold by weight (e.g., 0.455 kg)
        modelBuilder.Entity<Product>()
            .Property(p => p.Quantity)
            .HasPrecision(18, 3);
    }
}
