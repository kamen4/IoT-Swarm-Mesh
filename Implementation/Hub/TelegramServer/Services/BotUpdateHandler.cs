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

        var trimmed = text.Trim();

        if (trimmed.Equals("/toggle", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("/pin", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient.SendMessage(message.Chat.Id, "Toggling pin 8\u2026", cancellationToken: ct);
            var pinResult = await _businessClient.TogglePinAsync(8, ct);
            var pinReply = pinResult is not null
                ? $"\u2705 Gateway pin 8 is now {(pinResult.State == 1 ? "HIGH" : "LOW")}"
                : "\u274c Gateway did not respond in time.";
            await _botClient.SendMessage(message.Chat.Id, pinReply, cancellationToken: ct);
            return;
        }

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
