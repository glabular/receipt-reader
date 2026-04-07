using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReceiptReader.Data;
using ReceiptReader.Services.Invoices;
using ReceiptReader.Services.Messaging;
using ReceiptReader.Services.Messaging.Handlers;
using ReceiptReader.Services.Photos;
using ReceiptReader.Services;
using Serilog;
using Serilog.Events;

namespace ReceiptReader;

internal class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<Program>()
            .Build();

        var logsDirectory = LogsDirectoryResolver.Resolve(config);
        Directory.CreateDirectory(logsDirectory);
        var logsFilePath = Path.Combine(logsDirectory, "log_.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File(logsFilePath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

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
        services.AddScoped<SpendingSummaryFormatter>();
        services.AddScoped<UserService>();
        services.AddScoped<ReceiptClient>();
        services.AddSingleton<WeChatQrReader>();
        services.AddScoped<CommandsHandler>();
        services.AddScoped<TelegramUpdateProcessor>();
        services.AddScoped<TelegramMessageClassifier>();
        services.AddScoped<IMessageRouter, MessageRouter>();
        services.AddScoped<CommandMessageHandler>();
        services.AddScoped<PhotoMessageHandler>();
        services.AddScoped<FallbackMessageHandler>();
        services.AddScoped<PhotoStorageService>(sp =>
            new PhotoStorageService(
                logsDirectory,
                sp.GetRequiredService<ILogger<PhotoStorageService>>()));
        services.AddScoped<QrExtractionService>();
        services.AddScoped<InvoiceProcessingService>();
        services.AddScoped<InvoiceMessageFormatter>();
        services.AddSingleton<TelegramClient>(sp =>
            new TelegramClient(
                telegramToken,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<TelegramClient>>()
            ));

        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var isDevelopment = string.Equals(dotnetEnvironment, "Development", StringComparison.OrdinalIgnoreCase);

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = isDevelopment,
            ValidateOnBuild = isDevelopment
        });

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