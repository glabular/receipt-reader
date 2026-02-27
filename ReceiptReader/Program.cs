using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReceiptReader.Data;
using ReceiptReader.Services;

namespace ReceiptReader;

internal class Program
{
    static async Task Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var telegramToken = config["TelegramBotToken"];
        if (string.IsNullOrEmpty(telegramToken))
        {
            Console.WriteLine("ERROR: 'TelegramBotToken' is missing from secrets.json.");

            return;
        }

        var services = new ServiceCollection();

        services.AddDbContext<BotDbContext>(options =>
            options.UseSqlServer(
                "Server=localhost;Database=InvoicesDb;Trusted_Connection=True;TrustServerCertificate=True"));

        services.AddScoped<InvoiceService>();
        services.AddScoped<ReceiptClient>();
        services.AddScoped<WeChatQrReader>();
        services.AddScoped<TelegramClient>(sp =>
            new TelegramClient(
                telegramToken,
                sp.GetRequiredService<InvoiceService>(),
                sp.GetRequiredService<ReceiptClient>(),
                sp.GetRequiredService<WeChatQrReader>()
        ));

        await using var serviceProvider = services.BuildServiceProvider();
        var invoiceService = serviceProvider.GetRequiredService<InvoiceService>();
        var telegramClient = serviceProvider.GetRequiredService<TelegramClient>();

        await telegramClient.StartAsync();

        Console.ReadLine();
    }
}
