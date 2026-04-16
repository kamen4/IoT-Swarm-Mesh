namespace Engine.Benchmark;

/// <summary>
/// Progress snapshot emitted while a benchmark is running.
/// </summary>
public sealed record BenchmarkRunProgress
{
    /// <summary>
    /// Number of routers fully completed.
    /// </summary>
    public int CompletedRouters { get; init; }

    /// <summary>
    /// Total number of routers scheduled for this run.
    /// </summary>
    public int TotalRouters { get; init; }

    /// <summary>
    /// One-based index of the currently running router.
    /// </summary>
    public int CurrentRouterIndex { get; init; }

    /// <summary>
    /// Name of the currently running router.
    /// </summary>
    public string CurrentRouterName { get; init; } = "";

    /// <summary>
    /// Current tick in the active router run.
    /// </summary>
    public long CurrentTick { get; init; }

    /// <summary>
    /// Target number of ticks for the active router run.
    /// </summary>
    public long DurationTicks { get; init; }

    /// <summary>
    /// Active-router progress in [0, 1].
    /// </summary>
    public double RouterProgress { get; init; }

    /// <summary>
    /// Overall progress in [0, 1] across all routers.
    /// </summary>
    public double OverallProgress { get; init; }
}
