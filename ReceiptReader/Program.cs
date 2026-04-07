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
using System.Drawing;
using System.Windows.Forms;

namespace ReceiptReader;

internal class Program
{
    [STAThread]
    static void Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<Program>()
            .Build();

        var logsDirectory = LogsDirectoryResolver.Resolve(config);
        Directory.CreateDirectory(logsDirectory);
        var logsFilePath = Path.Combine(logsDirectory, "log_.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
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
        services.AddSingleton<IConfiguration>(config);

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        services.AddDbContext<BotDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<InvoiceService>();
        services.AddScoped<SpendingSummaryFormatter>();
        services.AddSingleton<SpendingAccessResolver>();
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

        ServiceProvider? serviceProvider = null;
        try
        {
            serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = isDevelopment,
                ValidateOnBuild = isDevelopment
            });

            var telegramClient = serviceProvider.GetRequiredService<TelegramClient>();
            telegramClient.StartAsync().GetAwaiter().GetResult();

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext(serviceProvider, "ReceiptReader"));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            if (serviceProvider is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            else if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Log.CloseAndFlush();
        }
    }

    private sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NotifyIcon _notifyIcon;

        public TrayApplicationContext(IServiceProvider serviceProvider, string appName)
        {
            _serviceProvider = serviceProvider;

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(new ToolStripMenuItem(appName) { Enabled = false });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, (_, _) => ExitThread());

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = appName,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        protected override void ExitThreadCore()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.ExitThreadCore();
        }
    }
}