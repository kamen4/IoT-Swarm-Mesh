using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramServer.Services;

namespace TelegramServer;

public class Worker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotUpdateHandler _handler;
    private readonly ILogger<Worker> _logger;

    public Worker(ITelegramBotClient botClient, BotUpdateHandler handler, ILogger<Worker> logger)
    {
        _botClient = botClient;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot polling...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        await _botClient.ReceiveAsync(
            updateHandler: (client, update, ct) => _handler.HandleUpdateAsync(update, ct),
            errorHandler: (client, ex, ct) => _handler.HandleErrorAsync(ex, ct),
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}
