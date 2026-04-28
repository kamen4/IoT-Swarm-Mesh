using System.Text.Json;
using Common.Messages;
using StackExchange.Redis;

namespace BusinessServer.Services;

/// <summary>
/// Background service that subscribes to the hub:evt Redis channel and routes incoming hardware
/// acknowledgement messages to the matching pending operation in <see cref="IPinDispatchService"/>.
/// </summary>
public sealed class PinEventListener : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IPinDispatchService _dispatch;
    private readonly ILogger<PinEventListener> _logger;

    /// <summary>Initializes a new instance of <see cref="PinEventListener"/> with its required dependencies.</summary>
    /// <param name="redis">Redis connection used to subscribe to hub:evt.</param>
    /// <param name="dispatch">Dispatch service to notify when a hardware event arrives.</param>
    /// <param name="logger">Logger for recording subscription errors.</param>
    public PinEventListener(IConnectionMultiplexer redis, IPinDispatchService dispatch, ILogger<PinEventListener> logger)
    {
        _redis = redis;
        _dispatch = dispatch;
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to the hub:evt Redis channel and forwards each deserialized <see cref="PinEventMessage"/>
    /// to <see cref="IPinDispatchService.ResolveEvent"/>. Runs until the host requests cancellation.
    /// </summary>
    /// <param name="stoppingToken">Token signalled when the host is shutting down.</param>
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
