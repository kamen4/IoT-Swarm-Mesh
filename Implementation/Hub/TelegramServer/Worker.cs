using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramServer.Services;

namespace TelegramServer;

/// <summary>
/// Background service that starts and maintains the Telegram Bot long-polling loop.
/// Delegates all update processing to <see cref="BotUpdateHandler"/>.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotUpdateHandler _handler;
    private readonly ILogger<Worker> _logger;

    /// <summary>Initialises the worker with the required Telegram client, handler, and logger.</summary>
    public Worker(ITelegramBotClient botClient, BotUpdateHandler handler, ILogger<Worker> logger)
    {
        _botClient = botClient;
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// Starts Telegram long polling. Runs until the host signals cancellation.
    /// Listens for Message and CallbackQuery update types.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot polling...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        await _botClient.ReceiveAsync(
            updateHandler: (client, update, ct) => _handler.HandleUpdateAsync(update, ct),
            errorHandler: (client, ex, ct) => _handler.HandleErrorAsync(ex, ct),
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}
