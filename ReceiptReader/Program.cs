using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReceiptReader.Data;
using ReceiptReader.Services.Invoices;
using ReceiptReader.Services.Messaging;
using ReceiptReader.Services.Messaging.Handlers;
using ReceiptReader.Services.Photos;
using ReceiptReader.Logging;
using ReceiptReader.Services;
using ReceiptReader.UI;
using Serilog;
using Serilog.Events;
using System.Drawing;
using System.Windows.Forms;

namespace ReceiptReader;

internal class Program
{
    private const string LogOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private const int LogRingBufferCapacity = 5000;

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
        var logRingBuffer = new LogRingBuffer(LogRingBufferCapacity);
        var winFormsLogSink = new WinFormsLogSink(logRingBuffer, LogOutputTemplate);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(logsFilePath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                outputTemplate: LogOutputTemplate)
            .WriteTo.Sink(winFormsLogSink)
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
            Application.Run(new TrayApplicationContext(serviceProvider, logRingBuffer, "ReceiptReader"));
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
        private readonly NotifyIcon _notifyIcon;
        private readonly LogViewerForm _logViewerForm;

        public TrayApplicationContext(IServiceProvider serviceProvider, LogRingBuffer logRingBuffer, string appName)
        {
            _ = serviceProvider;

            _logViewerForm = new LogViewerForm(logRingBuffer, appName);
            MainForm = _logViewerForm;

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

            _notifyIcon.DoubleClick += (_, _) => ShowLogViewer();
        }

        private void ShowLogViewer()
        {
            _logViewerForm.Show();
            _logViewerForm.WindowState = FormWindowState.Normal;
            _logViewerForm.ShowInTaskbar = true;
            _logViewerForm.Activate();
        }

        protected override void ExitThreadCore()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _logViewerForm.PrepareForApplicationExit();
            _logViewerForm.Close();
            base.ExitThreadCore();
        }
    }
}