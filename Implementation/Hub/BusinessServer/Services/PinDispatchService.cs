using System.Collections.Concurrent;
using System.Text.Json;
using Common.Dto;
using Common.Messages;
using StackExchange.Redis;

namespace BusinessServer.Services;

/// <summary>
/// Implements <see cref="IPinDispatchService"/> by serializing commands to JSON, publishing them on the
/// hub:cmd Redis channel, and matching incoming hub:evt messages back to waiting callers via correlation IDs.
/// </summary>
public sealed class PinDispatchService : IPinDispatchService
{
    private readonly IConnectionMultiplexer _redis;

    /// <summary>Map of in-flight correlation IDs to their corresponding task completion sources.</summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<(int pin, int state)>> _pending = new();

    /// <summary>Initializes a new instance of <see cref="PinDispatchService"/> with a Redis connection.</summary>
    /// <param name="redis">The Redis connection used to publish commands and subscribe to event responses.</param>
    public PinDispatchService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    public async Task<PinToggleResponse> TogglePinAsync(int pin, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<(int pin, int state)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;

        try
        {
            var message = JsonSerializer.Serialize(new PinCommandMessage(correlationId, pin));
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync(RedisChannel.Literal("hub:cmd"), message);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000, ct));
            if (completed != tcs.Task)
                throw new TimeoutException($"No response for pin {pin} within 5 seconds.");

            var result = await tcs.Task;
            return new PinToggleResponse(result.pin, result.state);
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }

    /// <inheritdoc/>
    public void ResolveEvent(string correlationId, int pin, int state)
    {
        if (_pending.TryGetValue(correlationId, out var tcs))
            tcs.TrySetResult((pin, state));
    }
}
