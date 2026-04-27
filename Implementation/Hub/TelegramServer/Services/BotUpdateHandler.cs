using Common.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramServer.Services;

public class BotUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBusinessServerClient _businessClient;
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(
        ITelegramBotClient botClient,
        IBusinessServerClient businessClient,
        ILogger<BotUpdateHandler> logger)
    {
        _botClient = botClient;
        _businessClient = businessClient;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message) return;

        _logger.LogInformation("Received message from {UserId}: {Text}", message.From?.Id, text);

        var echoResult = await _businessClient.EchoAsync(new EchoRequest(text), ct);

        var reply = echoResult is not null
            ? $"BusinessServer echo: {echoResult.Echo} (at {echoResult.ReceivedAt:HH:mm:ss})"
            : "BusinessServer is unavailable.";

        await _botClient.SendMessage(message.Chat.Id, reply, cancellationToken: ct);
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram polling error");
        return Task.CompletedTask;
    }
}
