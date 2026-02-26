using Engine.Core;
using Engine.Devices;
using Engine.Statistics;
using System.Diagnostics;
using System.Numerics;

namespace WebApp.Client.Services;

public class SimulationService : IDisposable
{
    public SimulationEngine     Engine     { get; } = SimulationEngine.Instance;
    public SimulationStatistics Statistics { get; } = SimulationStatistics.Instance;
    public SimulationConfig     Config     { get; } = new();

    public bool   IsRunning  { get; private set; }
    public double LastTickMs { get; private set; }

    public event Action? StateChanged;

    private CancellationTokenSource? _cts;

    public SimulationService()
    {
        SeedDefaultDevices();
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

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

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            sw.Restart();
            Engine.Tick();
            LastTickMs = sw.Elapsed.TotalMilliseconds;
            StateChanged?.Invoke();
        }
    }

    public void ApplyConfig()
    {
        Engine.VisibilityDistance = Config.VisibilityDistance;
        if (IsRunning)
        {
            Stop();
            Start();
        }
    }

    public void AddDevice(DeviceFormModel form)
    {
        var device = CreateDevice(form);
        Engine.RegisterDevice(device);
    }

    public void UpdateDevice(Guid id, DeviceFormModel form)
    {
        var device = Engine.Devices.FirstOrDefault(d => d.Id == id);
        if (device is null) return;

        device.Name     = form.Name;
        device.Position = new Vector2(form.X, form.Y);

        if (device is GeneratorDevice gen)
            gen.GenFrequencyTicks = form.GenFrequencyTicks;
    }

    public void RemoveDevice(Guid id) => Engine.RemoveDevice(id);

    private void SeedDefaultDevices()
    {
        Engine.RegisterDevice(new HubDevice            { Name = "Hub",      Position = new Vector2(  0,   0) });
        Engine.RegisterDevice(new GeneratorDevice(40)  { Name = "Sensor-A", Position = new Vector2(150,   0) });
        Engine.RegisterDevice(new GeneratorDevice(50)  { Name = "Sensor-B", Position = new Vector2(-150,  0) });
        Engine.RegisterDevice(new GeneratorDevice(45)  { Name = "Sensor-C", Position = new Vector2(  0, 150) });
        Engine.RegisterDevice(new EmitterDevice        { Name = "Lamp-1",   Position = new Vector2( 80, 120) });
        Engine.RegisterDevice(new EmitterDevice        { Name = "Lamp-2",   Position = new Vector2(-80,-120) });
    }

    private static Device CreateDevice(DeviceFormModel form) => form.DeviceType switch
    {
        DeviceType.Hub       => new HubDevice           { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        DeviceType.Generator => new GeneratorDevice(form.GenFrequencyTicks) { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        DeviceType.Emitter   => new EmitterDevice       { Name = form.Name, Position = new Vector2(form.X, form.Y) },
        _                    => throw new ArgumentOutOfRangeException()
    };

    public void Reset()
    {
        Stop();
        Engine.Reset();
        Statistics.Reset();
        SeedDefaultDevices();
        StateChanged?.Invoke();
    }

    public void GenerateRandom(int count)
    {
        Stop();
        Engine.Reset();
        Statistics.Reset();

        var rng = Random.Shared;
        Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = new Vector2(0, 0) });

        for (int i = 0; i < count; i++)
        {
            double angle  = rng.NextDouble() * Math.PI * 2;
            double radius = rng.NextDouble() * Engine.VisibilityDistance * 2.5;
            var pos = new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));

            Device device = i % 2 == 0
                ? new GeneratorDevice(rng.Next(20, 80)) { Name = $"Sensor-{i + 1}", Position = pos }
                : new EmitterDevice                     { Name = $"Lamp-{i + 1}",   Position = pos };

            Engine.RegisterDevice(device);
        }

        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        Stop();
    }
}
