using Engine.Core;
using Engine.Devices;
using Engine.Packets;
using Engine.Routers;
using Engine.Statistics;
using System.Numerics;
using System.Threading;

namespace Engine.Benchmark;

/// <summary>
/// Executes a <see cref="BenchmarkConfig"/> headlessly  -  without any UI  -  and
/// produces a <see cref="BenchmarkSession"/> containing one
/// <see cref="BenchmarkResult"/> per configured router.
/// <para>
/// <b>Threading:</b> supports both synchronous and asynchronous execution.
/// The async entry point periodically yields to keep UI render loops responsive
/// on single-threaded environments such as Blazor WebAssembly.
/// Each router run uses an isolated engine/statistics context so independent
/// runs can execute in parallel when requested.
/// </para>
/// <para>
/// <b>Progress reporting:</b> the optional <c>onProgress</c>
/// callback in <see cref="Run"/> receives detailed
/// <see cref="BenchmarkRunProgress"/> snapshots while each router is running,
/// including per-router tick progress and overall progress.
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
    /// Optional callback invoked during execution with detailed
    /// <see cref="BenchmarkRunProgress"/> snapshots. Called on the thread-pool
    /// thread, not the Blazor synchronisation context.
    /// </param>
    /// <param name="networkBuilder">
    /// Optional topology builder override for this benchmark run.
    /// When null, the current <see cref="SimulationEngine.Instance"/>
    /// builder is used as a template.
    /// </param>
    /// <param name="swarmVector">
    /// Optional swarm-vector override for this benchmark run.
    /// When null, the current <see cref="SimulationEngine.Instance"/>
    /// vector is used as a template.
    /// </param>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum number of router runs executed in parallel.
    /// Use 1 for fully sequential execution.
    /// </param>
    /// <returns>
    /// A <see cref="BenchmarkSession"/> with results for every router that was
    /// found and executed.
    /// </returns>
    public static BenchmarkSession Run(
        BenchmarkConfig              config,
        IReadOnlyDictionary<string, IPacketRouter> availableRouters,
        Action<BenchmarkRunProgress>? onProgress = null,
        INetworkBuilder? networkBuilder = null,
        SwarmProtocolVector? swarmVector = null,
        int maxDegreeOfParallelism = 1)
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

        var totalRouters = routerNames.Count;
        if (totalRouters == 0)
            return session;

        var effectiveNetworkBuilder = networkBuilder ?? SimulationEngine.Instance.NetworkBuilder;
        var effectiveVector = (swarmVector ?? SimulationEngine.Instance.SwarmVector).Normalized();
        var effectiveParallelism = Math.Clamp(maxDegreeOfParallelism, 1, totalRouters);

        if (effectiveParallelism > 1)
        {
            var progressLock = new object();
            var completedRouters = 0;
            var results = new BenchmarkResult?[totalRouters];

            Parallel.For(
                0,
                totalRouters,
                new ParallelOptions { MaxDegreeOfParallelism = effectiveParallelism },
                ri =>
                {
                    var router = availableRouters[routerNames[ri]];

                    var result = RunSingle(
                        config,
                        router,
                        sortedEvents,
                        ri + 1,
                        totalRouters,
                        onProgress: null,
                        effectiveNetworkBuilder,
                        effectiveVector);

                    results[ri] = result;

                    if (onProgress is null)
                        return;

                    var completed = Interlocked.Increment(ref completedRouters);
                    var snapshot = new BenchmarkRunProgress
                    {
                        CompletedRouters = completed,
                        TotalRouters = totalRouters,
                        CurrentRouterIndex = ri + 1,
                        CurrentRouterName = router.Name,
                        CurrentTick = config.DurationTicks,
                        DurationTicks = config.DurationTicks,
                        RouterProgress = 1,
                        OverallProgress = (double)completed / totalRouters,
                    };

                    lock (progressLock)
                        onProgress(snapshot);
                });

            for (var i = 0; i < results.Length; i++)
            {
                if (results[i] is not null)
                    session.Results.Add(results[i]!);
            }

            return session;
        }

        for (var ri = 0; ri < totalRouters; ri++)
        {
            var router = availableRouters[routerNames[ri]];

            onProgress?.Invoke(new BenchmarkRunProgress
            {
                CompletedRouters = ri,
                TotalRouters = totalRouters,
                CurrentRouterIndex = ri + 1,
                CurrentRouterName = router.Name,
                CurrentTick = 0,
                DurationTicks = config.DurationTicks,
                RouterProgress = 0,
                OverallProgress = (double)ri / totalRouters,
            });

            var result = RunSingle(
                config,
                router,
                sortedEvents,
                ri + 1,
                totalRouters,
                onProgress,
                effectiveNetworkBuilder,
                effectiveVector);
            session.Results.Add(result);

            onProgress?.Invoke(new BenchmarkRunProgress
            {
                CompletedRouters = ri + 1,
                TotalRouters = totalRouters,
                CurrentRouterIndex = ri + 1,
                CurrentRouterName = router.Name,
                CurrentTick = config.DurationTicks,
                DurationTicks = config.DurationTicks,
                RouterProgress = 1,
                OverallProgress = (double)(ri + 1) / totalRouters,
            });
        }

        return session;
    }

    /// <summary>
    /// Runs the benchmark asynchronously while periodically yielding control so
    /// UI frameworks can process paint/input between tick batches.
    /// </summary>
    /// <param name="config">The benchmark configuration to execute.</param>
    /// <param name="availableRouters">Available router registry keyed by name.</param>
    /// <param name="onProgress">Optional detailed progress callback.</param>
    /// <param name="networkBuilder">
    /// Optional topology builder override for this benchmark run.
    /// When null, the current <see cref="SimulationEngine.Instance"/>
    /// builder is used as a template.
    /// </param>
    /// <param name="swarmVector">
    /// Optional swarm-vector override for this benchmark run.
    /// When null, the current <see cref="SimulationEngine.Instance"/>
    /// vector is used as a template.
    /// </param>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum number of router runs executed in parallel.
    /// Use 1 for fully sequential execution.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A completed <see cref="BenchmarkSession"/>.</returns>
    public static async Task<BenchmarkSession> RunAsync(
        BenchmarkConfig config,
        IReadOnlyDictionary<string, IPacketRouter> availableRouters,
        Action<BenchmarkRunProgress>? onProgress = null,
        INetworkBuilder? networkBuilder = null,
        SwarmProtocolVector? swarmVector = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default)
    {
        var session = new BenchmarkSession
        {
            Config = config,
            CreatedAt = DateTime.UtcNow,
        };

        var sortedEvents = config.Events
            .OrderBy(e => e.AtTick)
            .ToArray();

        var routerNames = config.RouterNames
            .Where(n => availableRouters.ContainsKey(n))
            .ToList();

        var totalRouters = routerNames.Count;
        if (totalRouters == 0)
            return session;

        var effectiveNetworkBuilder = networkBuilder ?? SimulationEngine.Instance.NetworkBuilder;
        var effectiveVector = (swarmVector ?? SimulationEngine.Instance.SwarmVector).Normalized();
        var effectiveParallelism = Math.Clamp(maxDegreeOfParallelism, 1, totalRouters);

        if (effectiveParallelism > 1)
        {
            var progressLock = new object();
            var completedRouters = 0;
            var results = new BenchmarkResult?[totalRouters];
            using var gate = new SemaphoreSlim(effectiveParallelism);

            var tasks = new List<Task>(totalRouters);
            for (var ri = 0; ri < totalRouters; ri++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await gate.WaitAsync(cancellationToken);

                var routerIndex = ri;
                var router = availableRouters[routerNames[routerIndex]];

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var result = RunSingle(
                            config,
                            router,
                            sortedEvents,
                            routerIndex + 1,
                            totalRouters,
                            onProgress: null,
                            effectiveNetworkBuilder,
                            effectiveVector);

                        results[routerIndex] = result;

                        if (onProgress is not null)
                        {
                            var completed = Interlocked.Increment(ref completedRouters);
                            var snapshot = new BenchmarkRunProgress
                            {
                                CompletedRouters = completed,
                                TotalRouters = totalRouters,
                                CurrentRouterIndex = routerIndex + 1,
                                CurrentRouterName = router.Name,
                                CurrentTick = config.DurationTicks,
                                DurationTicks = config.DurationTicks,
                                RouterProgress = 1,
                                OverallProgress = (double)completed / totalRouters,
                            };

                            lock (progressLock)
                                onProgress(snapshot);
                        }
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            for (var i = 0; i < results.Length; i++)
            {
                if (results[i] is not null)
                    session.Results.Add(results[i]!);
            }

            return session;
        }

        for (var ri = 0; ri < totalRouters; ri++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var router = availableRouters[routerNames[ri]];

            onProgress?.Invoke(new BenchmarkRunProgress
            {
                CompletedRouters = ri,
                TotalRouters = totalRouters,
                CurrentRouterIndex = ri + 1,
                CurrentRouterName = router.Name,
                CurrentTick = 0,
                DurationTicks = config.DurationTicks,
                RouterProgress = 0,
                OverallProgress = (double)ri / totalRouters,
            });

            var result = await RunSingleAsync(
                    config,
                    router,
                    sortedEvents,
                    ri + 1,
                    totalRouters,
                    onProgress,
                    cancellationToken,
                    effectiveNetworkBuilder,
                    effectiveVector);

            session.Results.Add(result);

            onProgress?.Invoke(new BenchmarkRunProgress
            {
                CompletedRouters = ri + 1,
                TotalRouters = totalRouters,
                CurrentRouterIndex = ri + 1,
                CurrentRouterName = router.Name,
                CurrentTick = config.DurationTicks,
                DurationTicks = config.DurationTicks,
                RouterProgress = 1,
                OverallProgress = (double)(ri + 1) / totalRouters,
            });

            await Task.Yield();
        }

        return session;
    }

    // ------------------------------------------------------------------
    // Single-router headless run
    // ------------------------------------------------------------------

    /// <summary>
    /// Executes one full simulation run for <paramref name="router"/>.
    /// Resets an isolated engine context, builds the initial network, then ticks
    /// until <see cref="BenchmarkConfig.DurationTicks"/> is reached while
    /// firing any scheduled events at the correct tick.
    /// </summary>
    private static BenchmarkResult RunSingle(
        BenchmarkConfig        config,
        IPacketRouter          router,
        BenchmarkEventEntry[]  sortedEvents,
        int                    currentRouterIndex,
        int                    totalRouters,
        Action<BenchmarkRunProgress>? onProgress,
        INetworkBuilder networkBuilder,
        SwarmProtocolVector swarmVector)
    {
        var engine = SimulationEngine.CreateIsolated();
        var stats  = SimulationStatistics.CreateIsolated(engine);

        using var engineScope = SimulationEngine.PushScopedInstance(engine);
        using var statsScope = SimulationStatistics.PushScopedInstance(stats);

        // Full reset: clears devices, packets, topology, and all statistics.
        engine.Reset();
        stats.Reset();
        engine.NetworkBuilder = networkBuilder;
        engine.SetSwarmVector(swarmVector);

        // Apply engine settings from the configuration.
        engine.VisibilityDistance = config.VisibilityDistance;
        engine.MaxActivePackets   = config.MaxActivePackets;
        engine.DefaultPacketTtl = config.DefaultTtl;
        engine.DefaultPacketTicksToTravel = config.TicksToTravel;
        engine.Router             = router;

        // Build initial device layout.
        foreach (var dto in config.Devices)
            engine.RegisterDevice(CreateDevice(dto));

        // Process events in tick order; track index for efficient advance.
        int eventIdx = 0;

        var durationTicks = Math.Max(1L, config.DurationTicks);
        var reportStep = Math.Max(1L, durationTicks / 250L);
        var lastReportedTick = 0L;
        var lastTick = 0L;

        // Run the tick loop without any timer delay  -  as fast as possible.
        for (var tick = 1L; tick <= durationTicks; tick++)
        {
            lastTick = tick;

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

            if (onProgress is not null &&
                (tick == 1 || tick == durationTicks || tick - lastReportedTick >= reportStep))
            {
                lastReportedTick = tick;
                ReportProgress(
                    onProgress,
                    currentRouterIndex,
                    totalRouters,
                    router.Name,
                    tick,
                    durationTicks);
            }
        }

        if (onProgress is not null && lastTick > 0)
        {
            ReportProgress(
                onProgress,
                currentRouterIndex,
                totalRouters,
                router.Name,
                lastTick,
                durationTicks);
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

    private static async Task<BenchmarkResult> RunSingleAsync(
        BenchmarkConfig config,
        IPacketRouter router,
        BenchmarkEventEntry[] sortedEvents,
        int currentRouterIndex,
        int totalRouters,
        Action<BenchmarkRunProgress>? onProgress,
        CancellationToken cancellationToken,
        INetworkBuilder networkBuilder,
        SwarmProtocolVector swarmVector)
    {
        var engine = SimulationEngine.CreateIsolated();
        var stats = SimulationStatistics.CreateIsolated(engine);

        using var engineScope = SimulationEngine.PushScopedInstance(engine);
        using var statsScope = SimulationStatistics.PushScopedInstance(stats);

        engine.Reset();
        stats.Reset();
        engine.NetworkBuilder = networkBuilder;
        engine.SetSwarmVector(swarmVector);

        engine.VisibilityDistance = config.VisibilityDistance;
        engine.MaxActivePackets = config.MaxActivePackets;
        engine.DefaultPacketTtl = config.DefaultTtl;
        engine.DefaultPacketTicksToTravel = config.TicksToTravel;
        engine.Router = router;

        foreach (var dto in config.Devices)
            engine.RegisterDevice(CreateDevice(dto));

        var eventIdx = 0;

        var durationTicks = Math.Max(1L, config.DurationTicks);
        var reportStep = Math.Max(1L, durationTicks / 100L);
        var yieldStep = Math.Max(1L, Math.Min(128L, durationTicks / 200L));

        var lastReportedTick = 0L;
        var lastYieldTick = 0L;
        var lastTick = 0L;

        for (var tick = 1L; tick <= durationTicks; tick++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastTick = tick;

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
                break;
            }

            if (onProgress is not null &&
                (tick == 1 || tick == durationTicks || tick - lastReportedTick >= reportStep))
            {
                lastReportedTick = tick;
                ReportProgress(
                    onProgress,
                    currentRouterIndex,
                    totalRouters,
                    router.Name,
                    tick,
                    durationTicks);
            }

            if (tick == durationTicks || tick - lastYieldTick >= yieldStep)
            {
                lastYieldTick = tick;
                await Task.Yield();
            }
        }

        if (onProgress is not null && lastTick > 0)
        {
            ReportProgress(
                onProgress,
                currentRouterIndex,
                totalRouters,
                router.Name,
                lastTick,
                durationTicks);
        }

        return new BenchmarkResult
        {
            RouterName = router.Name,
            TotalPacketsRegistered = stats.TotalPacketsRegistered.Value,
            TotalPacketsDelivered = stats.TotalPacketsDelivered.Value,
            TotalPacketsExpired = stats.TotalPacketsExpired.Value,
            DuplicateDeliveries = stats.DuplicateDeliveries.Value,
            AvgHopCount = stats.AvgHopCount.Value,
            DeliveryRate = stats.DeliveryRate.Value,
            AvgTickMs = stats.AvgTickMs.Value,
            History = stats.History.ToArray(),
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

    private static void ReportProgress(
        Action<BenchmarkRunProgress> onProgress,
        int currentRouterIndex,
        int totalRouters,
        string routerName,
        long tick,
        long durationTicks)
    {
        var routerProgress = Math.Clamp((double)tick / durationTicks, 0.0, 1.0);
        var overallProgress = Math.Clamp(
            ((currentRouterIndex - 1) + routerProgress) / totalRouters,
            0.0,
            1.0);

        onProgress(new BenchmarkRunProgress
        {
            CompletedRouters = Math.Max(0, currentRouterIndex - 1),
            TotalRouters = totalRouters,
            CurrentRouterIndex = currentRouterIndex,
            CurrentRouterName = routerName,
            CurrentTick = tick,
            DurationTicks = durationTicks,
            RouterProgress = routerProgress,
            OverallProgress = overallProgress,
        });
    }
}
