namespace Engine.Benchmark;

/// <summary>
/// The complete, serialisable record of one benchmark execution.
/// Stored as JSON so users can save, share, and reload results.
/// </summary>
public sealed class BenchmarkSession
{
    // ------------------------------------------------------------------
    // Metadata
    // ------------------------------------------------------------------

    /// <summary>UTC timestamp when the session was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ------------------------------------------------------------------
    // Source configuration
    // ------------------------------------------------------------------

    /// <summary>
    /// The <see cref="BenchmarkConfig"/> that was used to generate this session.
    /// Embedded so the session JSON is self-contained: configuration and results
    /// travel together and the run can be fully reproduced.
    /// </summary>
    public BenchmarkConfig Config { get; set; } = new();

    // ------------------------------------------------------------------
    // Per-router results
    // ------------------------------------------------------------------

    /// <summary>
    /// One <see cref="BenchmarkResult"/> per router in
    /// <see cref="BenchmarkConfig.RouterNames"/>, in the same order.
    /// </summary>
    public List<BenchmarkResult> Results { get; set; } = [];
}
