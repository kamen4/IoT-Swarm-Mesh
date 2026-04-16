using Engine.Core;
using Engine.Devices;
using Engine.Statistics;
using System.Diagnostics;
using System.Numerics;

namespace WebApp.Client.Services;

/// <summary>
/// Blazor client-side service that drives the simulation from the UI layer.
/// <para>
/// Owns the tick loop (<see cref="RunLoopAsync"/>), wraps <see cref="Engine.Core.SimulationEngine"/>
/// and <see cref="Engine.Statistics.SimulationStatistics"/>, and provides high-level
/// operations (start, stop, reset, add/remove devices, load presets, generate random
/// layouts) consumed by the <c>Home.razor</c> page.
/// </para>
/// <para>
/// A <see cref="PacketLimitError"/> string is set when a
/// <see cref="Engine.Core.PacketLimitExceededException"/> is caught during a tick;
/// the Blazor page renders this as a dismissible error banner.
/// </para>
/// </summary>
public class SimulationService : IDisposable
{
    /// <summary>The singleton simulation engine shared across the application.</summary>
    public SimulationEngine Engine { get; } = SimulationEngine.Instance;

    /// <summary>The singleton statistics collector; updated reactively with every tick.</summary>
    public SimulationStatistics Statistics { get; } = SimulationStatistics.Instance;

    /// <summary>User-configurable simulation settings; apply via <see cref="ApplyConfig"/>.</summary>
    public SimulationConfig Config { get; } = new();

    /// <summary><c>true</c> while the tick loop is running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Wall-clock time in milliseconds spent on the last <see cref="Engine"/> tick.</summary>
    public double LastTickMs { get; private set; }

    /// <summary>
    /// Set when the simulation was stopped by a <see cref="PacketLimitExceededException"/>.
    /// Cleared on the next <see cref="Start"/> or <see cref="Reset"/> call.
    /// </summary>
    public string? PacketLimitError { get; private set; }

    /// <summary>
    /// Raised after every engine tick or after any state-affecting operation
    /// (start, stop, reset, device change). UI components subscribe to this
    /// event and call <c>StateHasChanged()</c> to trigger a re-render.
    /// </summary>
    public event Action? StateChanged;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// The preset that was last loaded with <see cref="LoadPreset"/>, or
    /// <c>null</c> if the current layout was built by <see cref="SeedDefaultDevices"/>
    /// or <see cref="GenerateRandom"/>.
    /// Used by <see cref="Reset"/> so it restores the same topology instead of
    /// always falling back to the hard-coded default.
    /// </summary>
    private SimulationPreset? _activePreset;

    /// <summary>
    /// The name of the preset that was last loaded with <see cref="LoadPreset"/>,
    /// or <c>"Default"</c> if no preset has been loaded yet.
    /// Shown in the UI so the user knows what <see cref="Reset"/> will restore.
    /// </summary>
    public string ActivePresetName => _activePreset?.Name ?? "Default";

    public SimulationService()
    {
        // Apply the default router from config before seeding devices.
        Engine.DefaultPacketTtl = Config.DefaultTTL;
        Engine.DefaultPacketTicksToTravel = Config.TicksToTravel;
        Engine.Router = Config.SelectedRouter;
        Engine.NetworkBuilder = Config.SelectedNetworkBuilder;
        Engine.SetSwarmVector(Config.SwarmVector);
        SeedDefaultDevices();
    }

    /// <summary>Starts the tick loop if it is not already running.</summary>
    public void Start()
    {
        if (IsRunning) return;
        PacketLimitError = null;
        IsRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    /// <summary>Stops the tick loop and cancels the periodic timer.</summary>
    public void Stop()
    {
        IsRunning = false;
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Config.TickIntervalMs));
        var sw = new Stopwatch();

        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                sw.Restart();
                Engine.Tick();
                LastTickMs = sw.Elapsed.TotalMilliseconds;
                StateChanged?.Invoke();
            }
        }
        catch (PacketLimitExceededException ex)
        {
            // Stop the simulation and surface the error to the UI.
            IsRunning = false;
            _cts = null;
            PacketLimitError = ex.Message;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Pushes all settings from <see cref="Config"/> to the engine.
    /// If the simulation is running it is stopped and restarted so the new
    /// tick interval takes effect immediately.
    /// </summary>
    public void ApplyConfig()
    {
        Engine.VisibilityDistance = Config.VisibilityDistance;
        Engine.MaxActivePackets = Config.MaxActivePackets;
        Engine.DefaultPacketTtl = Config.DefaultTTL;
        Engine.DefaultPacketTicksToTravel = Config.TicksToTravel;
        Engine.Router = Config.SelectedRouter;
        Engine.NetworkBuilder = Config.SelectedNetworkBuilder;
        Engine.SetSwarmVector(Config.SwarmVector);
        Engine.RebuildTopology();

        if (IsRunning)
        {
            Stop();
            Start();
        }
    }

    /// <summary>Creates a new device from <paramref name="form"/> and registers it with the engine.</summary>
    public void AddDevice(DeviceFormModel form)
    {
        var device = CreateDevice(form);
        Engine.RegisterDevice(device);
    }

    /// <summary>
    /// Applies the fields in <paramref name="form"/> to the existing device
    /// identified by <paramref name="id"/>. No-op if the device is not found.
    /// </summary>
    public void UpdateDevice(Guid id, DeviceFormModel form)
    {
        var device = Engine.Devices.FirstOrDefault(d => d.Id == id);
        if (device is null) return;

        device.Name = form.Name;
        device.Position = new Vector2(form.X, form.Y);

        if (device is GeneratorDevice gen)
            gen.GenFrequencyTicks = form.GenFrequencyTicks;

        if (device is EmitterDevice emitter)
            emitter.ControlFrequencyTicks = form.ControlFrequencyTicks;

        Engine.RebuildTopology();
    }

    /// <summary>Removes the device with the given <paramref name="id"/> from the engine.</summary>
    public void RemoveDevice(Guid id) => Engine.RemoveDevice(id);

    private void SeedDefaultDevices()
    {
        Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = new Vector2(0, 0) });
        Engine.RegisterDevice(new GeneratorDevice(40) { Name = "Sensor-A", Position = new Vector2(150, 0) });
        Engine.RegisterDevice(new GeneratorDevice(50) { Name = "Sensor-B", Position = new Vector2(-150, 0) });
        Engine.RegisterDevice(new GeneratorDevice(45) { Name = "Sensor-C", Position = new Vector2(0, 150) });
        Engine.RegisterDevice(new EmitterDevice { Name = "Lamp-1", Position = new Vector2(80, 120) });
        Engine.RegisterDevice(new EmitterDevice { Name = "Lamp-2", Position = new Vector2(-80, -120) });
    }

    private static Device CreateDevice(DeviceFormModel form) => form.DeviceType switch
    {
        DeviceType.Hub => new HubDevice { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        DeviceType.Generator => new GeneratorDevice(form.GenFrequencyTicks) { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        DeviceType.Emitter => new EmitterDevice(form.ControlFrequencyTicks) { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        _ => throw new ArgumentOutOfRangeException()
    };

    public void Reset()
    {
        Stop();
        Engine.Reset();
        Statistics.Reset();
        PacketLimitError = null;

        // Restore the last-loaded preset, or fall back to the built-in default.
        if (_activePreset is not null)
            _activePreset.Build(this);
        else
            SeedDefaultDevices();

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Stops the simulation, clears all devices and in-flight packets, then
    /// applies the device layout defined by <paramref name="preset"/>.
    /// The preset is remembered and will be restored by subsequent <see cref="Reset"/> calls.
    /// </summary>
    public void LoadPreset(SimulationPreset preset)
    {
        _activePreset = preset;
        Stop();
        Engine.Reset();
        Statistics.Reset();
        PacketLimitError = null;
        preset.Build(this);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Places a random network of <paramref name="count"/> devices around the
    /// hub, reset to an empty engine first.
    /// <c>_activePreset</c> is cleared so <see cref="Reset"/> will fall back
    /// to the built-in default rather than re-generating the random layout.
    /// </summary>
    public void GenerateRandom(int count)
        => GenerateRandom(new RandomGenerationOptions { DeviceCount = count });

    /// <summary>
    /// Places a random network based on <paramref name="options"/>.
    /// When <see cref="RandomGenerationOptions.EnsureConnected"/> is enabled,
    /// each generated device is guaranteed to be within visibility range of an
    /// existing node, producing a connected graph.
    /// </summary>
    /// <param name="options">Random-generation options.</param>
    public void GenerateRandom(RandomGenerationOptions options)
    {
        _activePreset = null;   // random layout has no preset to restore
        Stop();
        Engine.Reset();
        Statistics.Reset();
        PacketLimitError = null;

        var rng = Random.Shared;
        var hub = new HubDevice { Name = "Hub", Position = new Vector2(0, 0) };
        Engine.RegisterDevice(hub);

        var placedPositions = new List<Vector2> { hub.Position };
        var requestedCount = Math.Clamp(options.DeviceCount, 1, 500);

        var generatorMin = Math.Clamp(Math.Min(options.GeneratorMinTicks, options.GeneratorMaxTicks), 1, 10_000);
        var generatorMax = Math.Clamp(Math.Max(options.GeneratorMinTicks, options.GeneratorMaxTicks), generatorMin, 10_000);

        var emitterMin = Math.Clamp(Math.Min(options.EmitterMinTicks, options.EmitterMaxTicks), 1, 10_000);
        var emitterMax = Math.Clamp(Math.Max(options.EmitterMinTicks, options.EmitterMaxTicks), emitterMin, 10_000);

        var emitterShare = Math.Clamp(options.EmitterSharePercent, 0, 100) / 100.0;

        var sensorIndex = 1;
        var lampIndex = 1;

        for (var i = 0; i < requestedCount; i++)
        {
            var position = options.EnsureConnected
                ? GenerateConnectedPosition(placedPositions, Engine.VisibilityDistance, options, rng)
                : GenerateUnconstrainedPosition(Engine.VisibilityDistance, options, rng);

            placedPositions.Add(position);

            var useEmitter = rng.NextDouble() < emitterShare;
            Device device = useEmitter
                ? new EmitterDevice(rng.Next(emitterMin, emitterMax + 1))
                {
                    Name = $"Lamp-{lampIndex++}",
                    Position = position,
                }
                : new GeneratorDevice(rng.Next(generatorMin, generatorMax + 1))
                {
                    Name = $"Sensor-{sensorIndex++}",
                    Position = position,
                };

            Engine.RegisterDevice(device);
        }

        StateChanged?.Invoke();
    }

    private static Vector2 GenerateConnectedPosition(
        IReadOnlyList<Vector2> anchors,
        int visibilityDistance,
        RandomGenerationOptions options,
        Random rng)
    {
        var minFactor = Math.Clamp(options.ConnectedMinRadiusFactor, 0.05, 0.95);
        var maxFactor = Math.Clamp(options.ConnectedMaxRadiusFactor, minFactor, 0.98);

        var minSpacing = visibilityDistance * 0.12f;
        var fallback = Vector2.Zero;

        for (var attempt = 0; attempt < 48; attempt++)
        {
            var anchor = anchors[rng.Next(anchors.Count)];
            var angle = rng.NextDouble() * Math.PI * 2.0;
            var radiusFactor = minFactor + rng.NextDouble() * (maxFactor - minFactor);
            var radius = (float)(visibilityDistance * radiusFactor);

            var candidate = anchor + new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));

            if (attempt == 0)
                fallback = candidate;

            if (!IsTooClose(candidate, anchors, minSpacing))
                return candidate;
        }

        return fallback;
    }

    private static Vector2 GenerateUnconstrainedPosition(
        int visibilityDistance,
        RandomGenerationOptions options,
        Random rng)
    {
        var radiusFactor = Math.Max(1.0, options.FreeRadiusFactor);
        var angle = rng.NextDouble() * Math.PI * 2.0;
        var radius = rng.NextDouble() * visibilityDistance * radiusFactor;

        return new Vector2(
            (float)(Math.Cos(angle) * radius),
            (float)(Math.Sin(angle) * radius));
    }

    private static bool IsTooClose(Vector2 candidate, IReadOnlyList<Vector2> anchors, float minSpacing)
    {
        foreach (var point in anchors)
            if (Vector2.Distance(candidate, point) < minSpacing)
                return true;

        return false;
    }

    /// <summary>Stops the tick loop and releases the cancellation-token source.</summary>
    public void Dispose()
    {
        Stop();
    }
}
