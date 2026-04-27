using System.Text.Json;
using Common.Messages;
using StackExchange.Redis;

namespace BusinessServer.Services;

public sealed class PinEventListener : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IPinDispatchService _dispatch;
    private readonly ILogger<PinEventListener> _logger;

    public PinEventListener(IConnectionMultiplexer redis, IPinDispatchService dispatch, ILogger<PinEventListener> logger)
    {
        _redis = redis;
        _dispatch = dispatch;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = _redis.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal("hub:evt"), (_, value) =>
        {
            if (value.IsNullOrEmpty)
                return;

            try
            {
                var msg = JsonSerializer.Deserialize<PinEventMessage>((string)value!);
                if (msg is null)
                    return;

                _dispatch.ResolveEvent(msg.CorrelationId, msg.Pin, msg.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process hub:evt message");
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await sub.UnsubscribeAsync(RedisChannel.Literal("hub:evt"));
    }
}
