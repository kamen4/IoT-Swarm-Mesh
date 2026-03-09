using Engine.Core;
using Engine.Devices;
using Engine.Packets;
using Engine.Routers;
using Engine.Statistics;
using System.Numerics;

namespace Engine.Benchmark;

/// <summary>
/// Executes a <see cref="BenchmarkConfig"/> headlessly  -  without any UI  -  and
/// produces a <see cref="BenchmarkSession"/> containing one
/// <see cref="BenchmarkResult"/> per configured router.
/// <para>
/// <b>Threading:</b> designed to be called from <c>Task.Run</c> so that the
/// Blazor UI thread stays responsive during the (potentially long) run.
/// All engine interaction happens through the singleton
/// <see cref="SimulationEngine.Instance"/> which is fully reset between runs.
/// </para>
/// <para>
/// <b>Progress reporting:</b> the optional <paramref name="onProgress"/>
/// callback in <see cref="Run"/> is invoked after each completed router run
/// with <c>(routerIndex, totalRouters)</c> so callers can display a spinner.
/// </para>
/// </summary>
public static class BenchmarkRunner
{
    // ------------------------------------------------------------------
    // Public entry point
    // ------------------------------------------------------------------

    /// <summary>
    /// Runs the benchmark described by <paramref name="config"/> synchronously.
    /// Call this inside <c>Task.Run()</c> to avoid blocking the UI thread.
    /// </summary>
    /// <param name="config">The benchmark configuration to execute.</param>
    /// <param name="availableRouters">
    /// All known <see cref="IPacketRouter"/> implementations, keyed by
    /// <see cref="IPacketRouter.Name"/>.  Routers listed in
    /// <see cref="BenchmarkConfig.RouterNames"/> that are not found here are
    /// silently skipped.
    /// </param>
    /// <param name="onProgress">
    /// Optional callback invoked after each router completes:
    /// <c>(completedCount, totalCount)</c>.  Called on the thread-pool thread,
    /// not the Blazor synchronisation context.
    /// </param>
    /// <returns>
    /// A <see cref="BenchmarkSession"/> with results for every router that was
    /// found and executed.
    /// </returns>
    public static BenchmarkSession Run(
        BenchmarkConfig              config,
        IReadOnlyDictionary<string, IPacketRouter> availableRouters,
        Action<int, int>?            onProgress = null)
    {
        var session = new BenchmarkSession
        {
            Config    = config,
            CreatedAt = DateTime.UtcNow,
        };

        // Sort events once so the per-router loop can process them in order.
        var sortedEvents = config.Events
            .OrderBy(e => e.AtTick)
            .ToArray();

        var routerNames = config.RouterNames
            .Where(n => availableRouters.ContainsKey(n))
            .ToList();

        for (int ri = 0; ri < routerNames.Count; ri++)
        {
            var router = availableRouters[routerNames[ri]];
            var result = RunSingle(config, router, sortedEvents);
            session.Results.Add(result);
            onProgress?.Invoke(ri + 1, routerNames.Count);
        }

        return session;
    }

    // ------------------------------------------------------------------
    // Single-router headless run
    // ------------------------------------------------------------------

    /// <summary>
    /// Executes one full simulation run for <paramref name="router"/>.
    /// Resets the engine singleton, builds the initial network, then ticks
    /// until <see cref="BenchmarkConfig.DurationTicks"/> is reached while
    /// firing any scheduled events at the correct tick.
    /// </summary>
    private static BenchmarkResult RunSingle(
        BenchmarkConfig        config,
        IPacketRouter          router,
        BenchmarkEventEntry[]  sortedEvents)
    {
        var engine = SimulationEngine.Instance;
        var stats  = SimulationStatistics.Instance;

        // Full reset: clears devices, packets, topology, and all statistics.
        engine.Reset();
        stats.Reset();

        // Apply engine settings from the configuration.
        engine.VisibilityDistance = config.VisibilityDistance;
        engine.MaxActivePackets   = config.MaxActivePackets;
        engine.Router             = router;

        // Build initial device layout.
        foreach (var dto in config.Devices)
            engine.RegisterDevice(CreateDevice(dto));

        // Process events in tick order; track index for efficient advance.
        int eventIdx = 0;

        // Run the tick loop without any timer delay  -  as fast as possible.
        for (long tick = 1; tick <= config.DurationTicks; tick++)
        {
            // Fire all events scheduled exactly at this tick before ticking.
            while (eventIdx < sortedEvents.Length &&
                   sortedEvents[eventIdx].AtTick == tick)
            {
                ApplyEvent(sortedEvents[eventIdx].Event, engine);
                eventIdx++;
            }

            try
            {
                engine.Tick();
            }
            catch (PacketLimitExceededException)
            {
                // Treat a packet storm as end-of-run: record what we have and stop.
                break;
            }
        }

        // Capture final metrics and full history.
        return new BenchmarkResult
        {
            RouterName             = router.Name,
            TotalPacketsRegistered = stats.TotalPacketsRegistered.Value,
            TotalPacketsDelivered  = stats.TotalPacketsDelivered.Value,
            TotalPacketsExpired    = stats.TotalPacketsExpired.Value,
            DuplicateDeliveries    = stats.DuplicateDeliveries.Value,
            AvgHopCount            = stats.AvgHopCount.Value,
            DeliveryRate           = stats.DeliveryRate.Value,
            AvgTickMs              = stats.AvgTickMs.Value,
            History                = stats.History.ToArray(),
        };
    }

    // ------------------------------------------------------------------
    // Event application
    // ------------------------------------------------------------------

    /// <summary>
    /// Applies a single <see cref="BenchmarkEvent"/> to the live engine.
    /// Unknown event types are silently ignored for forward-compatibility.
    /// </summary>
    private static void ApplyEvent(BenchmarkEvent evt, SimulationEngine engine)
    {
        switch (evt)
        {
            case ToggleBenchmarkEvent toggle:
            {
                // Find the named emitter and send a toggle control packet from the hub.
                var target = engine.Devices
                    .OfType<EmitterDevice>()
                    .FirstOrDefault(d => d.Name == toggle.DeviceName);
                var hub = engine.Hub;

                if (target is null || hub is null) break;

                // Toggle: send the opposite of current state.
                var packet = new ControlPacket(hub, target, !target.State)
                {
                    TTL = EmitterDevice.CONTROL_PACKET_TTL
                };
                engine.RoutePacket(packet, hub);
                break;
            }

            case RemoveDeviceBenchmarkEvent remove:
            {
                var device = engine.Devices
                    .FirstOrDefault(d => d.Name == remove.DeviceName);
                if (device is not null)
                    engine.RemoveDevice(device.Id);
                break;
            }

            case AddDeviceBenchmarkEvent add:
            {
                engine.RegisterDevice(CreateDevice(add.Device));
                break;
            }
        }
    }

    // ------------------------------------------------------------------
    // Device factory
    // ------------------------------------------------------------------

    /// <summary>Converts a serialisable <see cref="DeviceBenchmarkDto"/> to a live device.</summary>
    private static Device CreateDevice(DeviceBenchmarkDto dto)
    {
        var pos = new Vector2(dto.X, dto.Y);
        return dto.Kind switch
        {
            BenchmarkDeviceKind.Hub =>
                new HubDevice { Name = dto.Name, Position = pos },

            BenchmarkDeviceKind.Generator =>
                new GeneratorDevice(dto.GenFrequencyTicks) { Name = dto.Name, Position = pos },

            BenchmarkDeviceKind.Emitter =>
                new EmitterDevice(dto.ControlFrequencyTicks) { Name = dto.Name, Position = pos },

            _ => throw new ArgumentOutOfRangeException(nameof(dto.Kind), dto.Kind, null)
        };
    }
}
