using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReceiptReader.Data;
using ReceiptReader.Services;
using Serilog;
using Serilog.Events;

namespace ReceiptReader;

internal class Program
{
    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("serilog_logs67.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
        
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var telegramToken = config["TelegramBotToken"];
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(telegramToken))
        {
            Log.Error("'TelegramBotToken' is missing from secrets.json.");

            return;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Log.Error("The DB connection string is missing from secrets.json.");

            return;
        }

        var services = new ServiceCollection();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

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
                sp.GetRequiredService<CommandsHandler>(),
                sp.GetRequiredService<ILogger<TelegramClient>>()
        ));

        await using var serviceProvider = services.BuildServiceProvider();
        var invoiceService = serviceProvider.GetRequiredService<InvoiceService>();
        var telegramClient = serviceProvider.GetRequiredService<TelegramClient>();

        try
        {
            await telegramClient.StartAsync();
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
