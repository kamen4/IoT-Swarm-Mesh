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
        Engine.Router = Config.SelectedRouter;
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
        Engine.Router = Config.SelectedRouter;

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
    {
        _activePreset = null;   // random layout has no preset to restore
        Stop();
        Engine.Reset();
        Statistics.Reset();
        PacketLimitError = null;

        var rng = Random.Shared;
        Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = new Vector2(0, 0) });

        for (int i = 0; i < count; i++)
        {
            double angle = rng.NextDouble() * Math.PI * 2;
            double radius = rng.NextDouble() * Engine.VisibilityDistance * 2.5;
            var pos = new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));

            Device device = i % 2 == 0
                ? new GeneratorDevice(rng.Next(20, 80)) { Name = $"Sensor-{i + 1}", Position = pos }
                : new EmitterDevice { Name = $"Lamp-{i + 1}", Position = pos };

            Engine.RegisterDevice(device);
        }

        StateChanged?.Invoke();
    }

    /// <summary>Stops the tick loop and releases the cancellation-token source.</summary>
    public void Dispose()
    {
        Stop();
    }
}
