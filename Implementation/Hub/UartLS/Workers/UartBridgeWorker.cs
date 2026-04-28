using System.IO.Ports;
using System.Text.Json;
using System.Threading.Channels;
using Common.Messages;
using StackExchange.Redis;

namespace UartLS.Workers;

/// <summary>
/// Background service that bridges the Redis <c>hub:cmd</c> channel to a physical serial port
/// connected to GatewayDevice. Subscribes to Redis first, then opens the serial port with a
/// retry loop. Translates <see cref="PinCommandMessage"/> into the UART PIN_TOGGLE/PIN_STATE
/// wire protocol and publishes <see cref="PinEventMessage"/> results to <c>hub:evt</c>.
/// </summary>
public sealed class UartBridgeWorker : BackgroundService
{
    /// <summary>Redis channel name for inbound pin-toggle commands.</summary>
    private const string CmdChannel = "hub:cmd";
    /// <summary>Redis channel name for outbound pin-state events.</summary>
    private const string EvtChannel = "hub:evt";
    /// <summary>Maximum milliseconds to wait for a PIN_STATE response from the device.</summary>
    private const int ResponseTimeoutMs = 2000;
    /// <summary>Milliseconds to wait between serial-port open retries.</summary>
    private const int RetryDelayMs = 3000;

    /// <summary>Redis connection used to subscribe to commands and publish events.</summary>
    private readonly IConnectionMultiplexer _redis;
    /// <summary>Application configuration; provides <c>SerialPort:PortName</c> and <c>SerialPort:BaudRate</c>.</summary>
    private readonly IConfiguration _config;
    /// <summary>Logger for diagnostic and error output.</summary>
    private readonly ILogger<UartBridgeWorker> _logger;

    /// <summary>
    /// The open serial port. Declared <c>volatile</c> so the Redis callback thread always reads
    /// the latest reference written by <see cref="ExecuteAsync"/>. Null while the port is not yet open.
    /// </summary>
    private volatile SerialPort? _port;
    /// <summary>
    /// Unbounded channel that passes raw lines from <see cref="SerialReadLoop"/> to
    /// <see cref="HandleCommandAsync"/>. Created before the port-open retry loop.
    /// </summary>
    private Channel<string>? _lineChannel;

    /// <summary>
    /// Initialises a new instance of <see cref="UartBridgeWorker"/> with its required dependencies.
    /// </summary>
    /// <param name="redis">Redis connection multiplexer provided by DI.</param>
    /// <param name="config">Application configuration provided by DI.</param>
    /// <param name="logger">Logger instance provided by DI.</param>
    public UartBridgeWorker(
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<UartBridgeWorker> logger)
    {
        _redis = redis;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Main service lifecycle. Subscribes to Redis <c>hub:cmd</c>, opens the serial port with a
    /// retry loop, waits for the stop signal, then tears down the read loop, Redis subscription,
    /// and serial port in order.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signalled when the host is stopping.</param>
    /// <returns>A task that completes when the worker has fully shut down.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var portName = _config["SerialPort:PortName"] ?? "/dev/ttyACM0";
        var baudRate = _config.GetValue<int?>("SerialPort:BaudRate") ?? 115200;

        _logger.LogInformation("UartBridgeWorker starting. Port={Port} Baud={Baud}", portName, baudRate);

        // Subscribe to Redis immediately so BusinessServer is not silently blocked.
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(CmdChannel),
            (_channel, message) =>
            {
                if (!message.HasValue) return;

                PinCommandMessage? cmd;
                try
                {
                    cmd = JsonSerializer.Deserialize<PinCommandMessage>(message.ToString());
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialise hub:cmd message.");
                    return;
                }

                if (cmd is null) return;

                _logger.LogInformation("Received hub:cmd: CorrelationId={Id} Pin={Pin}", cmd.CorrelationId, cmd.Pin);

                // Fire-and-forget -- use Task.Run to avoid async void pitfalls.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleCommandAsync(cmd, subscriber, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled error in HandleCommandAsync for Pin={Pin}.", cmd.Pin);
                    }
                }, CancellationToken.None);
            });

        _logger.LogInformation("Subscribed to Redis channel '{Channel}'. Opening serial port...", CmdChannel);

        // Open serial port with retry loop.
        _lineChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true,
        });

        using var readLoopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        Task? readLoopTask = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var port = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 1000,
                    NewLine = "\n",
                };
                port.Open();

                // Opening /dev/ttyACM* on Linux causes the kernel CDC-ACM driver to assert
                // DTR=1 (SET_CONTROL_LINE_STATE), which resets ESP32-C3 USB-JTAG hardware.
                // Clearing DTR/RTS immediately ensures they stay low during all subsequent I/O
                // and prevents a second write-triggered reset.
                port.DtrEnable = false;
                port.RtsEnable = false;

                _logger.LogInformation("Serial port {Port} opened. Waiting for device boot...", portName);

                readLoopTask = Task.Run(
                    () => SerialReadLoop(port, _lineChannel.Writer, readLoopCts.Token),
                    CancellationToken.None);

                // Give the ESP32 time to finish booting after the port-open-triggered reset.
                // The ESP32-C3 boot sequence (ROM + app init) takes roughly 250-400 ms,
                // but we wait longer to be safe against slow flash operations at startup.
                await Task.Delay(3000, stoppingToken);

                _port = port;
                _logger.LogInformation("Gateway device ready. Accepting commands.");

                break; // Port is open -- exit retry loop.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Cannot open serial port {Port}. Check UART_PORT env var and device connection. " +
                    "Retrying in {Delay} ms...", portName, RetryDelayMs);
                await Task.Delay(RetryDelayMs, stoppingToken);
            }
        }

        // Wait until the service is stopped.
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        // Teardown.
        readLoopCts.Cancel();
        _lineChannel.Writer.TryComplete();

        if (readLoopTask is not null)
        {
            try { await readLoopTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing); }
            catch { /* best effort */ }
        }

        try { await subscriber.UnsubscribeAllAsync(); }
        catch { /* best effort */ }

        _port?.Close();
        _port?.Dispose();
        _port = null;

        _logger.LogInformation("UartBridgeWorker stopped.");
    }

    /// <summary>
    /// Handles a single <see cref="PinCommandMessage"/> received from Redis. Writes
    /// <c>PIN_TOGGLE:{pin}\n</c> to the serial port, waits up to <see cref="ResponseTimeoutMs"/>
    /// milliseconds for a matching <c>PIN_STATE:{pin}:{state}\n</c> response line, then
    /// publishes a <see cref="PinEventMessage"/> to Redis <c>hub:evt</c>.
    /// </summary>
    /// <param name="cmd">Deserialized command received from <c>hub:cmd</c>.</param>
    /// <param name="subscriber">Redis subscriber used to publish the result event.</param>
    /// <param name="cancellationToken">Token linked to the service lifetime.</param>
    /// <returns>A task that completes when the command has been handled or timed out.</returns>
    private async Task HandleCommandAsync(
        PinCommandMessage cmd,
        ISubscriber subscriber,
        CancellationToken cancellationToken)
    {
        var port = _port;
        if (port is null || !port.IsOpen)
        {
            _logger.LogWarning(
                "Serial port not open. Cannot handle command for Pin={Pin}. " +
                "Is the device connected and UART_PORT set correctly?", cmd.Pin);
            return;
        }

        if (_lineChannel is null) return;

        string uartLine = $"PIN_TOGGLE:{cmd.Pin}";
        _logger.LogInformation("Writing to UART: {Line}", uartLine);

        lock (port)
        {
            port.WriteLine(uartLine);
        }

        // Wait for the matching response line.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ResponseTimeoutMs);

        string? responseLine = null;
        string expectedPrefix = $"PIN_STATE:{cmd.Pin}:";

        try
        {
            await foreach (var line in _lineChannel.Reader.ReadAllAsync(timeoutCts.Token))
            {
                if (line.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    responseLine = line;
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Timeout ({Ms} ms) waiting for UART response to Pin={Pin}. " +
                "Is the GatewayDevice firmware running?", ResponseTimeoutMs, cmd.Pin);
            return;
        }

        if (responseLine is null) return;

        // Parse PIN_STATE:{pin}:{state}
        var parts = responseLine.Split(':');
        if (parts.Length != 3
            || !int.TryParse(parts[1], out int pin)
            || !int.TryParse(parts[2], out int state))
        {
            _logger.LogWarning("Unexpected UART response format: '{Line}'", responseLine);
            return;
        }

        _logger.LogInformation("UART response: Pin={Pin} State={State}", pin, state);

        var evt = new PinEventMessage(cmd.CorrelationId, pin, state);
        var json = JsonSerializer.Serialize(evt);
        await subscriber.PublishAsync(RedisChannel.Literal(EvtChannel), json);

        _logger.LogInformation("Published to {Channel}: {Json}", EvtChannel, json);
    }

    /// <summary>
    /// Runs on a dedicated <see cref="System.Threading.Tasks.Task.Run(System.Action)"/> thread.
    /// Continuously reads lines from <paramref name="port"/> and forwards non-empty lines to
    /// <paramref name="writer"/>. A short <see cref="SerialPort.ReadTimeout"/> causes
    /// <see cref="TimeoutException"/> on each poll interval so the loop can check
    /// <paramref name="cancellationToken"/> promptly without blocking indefinitely.
    /// </summary>
    /// <param name="port">The open serial port to read from.</param>
    /// <param name="writer">Channel writer that delivers lines to <see cref="HandleCommandAsync"/>.</param>
    /// <param name="cancellationToken">Token that signals the loop to stop.</param>
    private void SerialReadLoop(SerialPort port, ChannelWriter<string> writer, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Serial read loop started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string line = port.ReadLine().TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogInformation("UART raw line: '{Line}'", line);
                    writer.TryWrite(line);
                }
            }
            catch (TimeoutException)
            {
                // Expected -- ReadTimeout is short to allow cancellation checks.
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error reading from serial port.");
                break;
            }
        }

        _logger.LogInformation("Serial read loop stopped.");
    }
}
