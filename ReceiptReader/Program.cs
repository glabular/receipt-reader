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
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(telegramToken))
        {
            Console.WriteLine("ERROR: 'TelegramBotToken' is missing from secrets.json.");

            return;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: The DB connection string is missing from secrets.json.");

            return;
        }

        var services = new ServiceCollection();

        services.AddDbContext<BotDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<InvoiceService>();
        services.AddScoped<UserService>();
        services.AddScoped<ReceiptClient>();
        services.AddScoped<WeChatQrReader>();
        services.AddScoped<CommandsHandler>();
        services.AddScoped<TelegramClient>(sp =>
            new TelegramClient(
                telegramToken,
                sp.GetRequiredService<InvoiceService>(),
                sp.GetRequiredService<ReceiptClient>(),
                sp.GetRequiredService<WeChatQrReader>(),
                sp.GetRequiredService<UserService>(),
                sp.GetRequiredService<CommandsHandler>()

        ));

        await using var serviceProvider = services.BuildServiceProvider();
        var invoiceService = serviceProvider.GetRequiredService<InvoiceService>();
        var telegramClient = serviceProvider.GetRequiredService<TelegramClient>();

        await telegramClient.StartAsync();

        Console.ReadLine();
    }
}
