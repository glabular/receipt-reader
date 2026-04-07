using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ReceiptReader.Services.Messaging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;

namespace ReceiptReader;

internal sealed class TelegramClient : IAsyncDisposable
{
    private readonly TelegramBotClient _bot;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TelegramClient> _logger;

    public TelegramClient(
        string token,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TelegramClient> logger)
    {
        _bot = new TelegramBotClient(token);
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        try
        {
            var me = await _bot.GetMe();
            _logger.LogInformation("Bot authenticated successfully as {Username}", me.Username);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to authenticate bot");
            throw;
        }

        _bot.OnMessage += OnMessage;
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        _logger.LogInformation("OnMessage: scope created in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

        var updateProcessor = scope.ServiceProvider.GetRequiredService<TelegramUpdateProcessor>();
        _logger.LogInformation("OnMessage: update processor resolved in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

        await updateProcessor.ProcessAsync(_bot, msg, type);
        _logger.LogInformation("OnMessage: processing completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }
}
