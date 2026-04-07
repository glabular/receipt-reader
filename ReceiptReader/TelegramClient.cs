using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ReceiptReader.Services.Messaging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var updateProcessor = scope.ServiceProvider.GetRequiredService<TelegramUpdateProcessor>();

        await updateProcessor.ProcessAsync(_bot, msg, type);
    }

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }
}
