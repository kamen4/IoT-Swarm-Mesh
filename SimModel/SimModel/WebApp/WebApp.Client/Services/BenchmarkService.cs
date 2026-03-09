using Engine.Benchmark;
using Engine.Routers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Client.Services;

/// <summary>
/// Blazor-side service that wraps <see cref="BenchmarkRunner"/> and adds:
/// <list type="bullet">
///   <item>Async execution via <c>Task.Run</c> so the WASM UI stays responsive.</item>
///   <item>Progress reporting as a 0–1 fraction for a loading-wheel overlay.</item>
///   <item>JSON serialisation / deserialisation of <see cref="BenchmarkSession"/>
///         objects so users can save and reload benchmark results.</item>
///   <item>An in-memory list of saved sessions shown in the benchmark library.</item>
/// </list>
/// </summary>
public sealed class BenchmarkService
{
    // ------------------------------------------------------------------
    // Router registry — all routers the runner can use.
    // Matches the same set exposed in SimulationConfig.AvailableRouters.
    // ------------------------------------------------------------------

    /// <summary>
    /// All routing strategies available for selection in a benchmark config.
    /// Keyed by <see cref="IPacketRouter.Name"/> for O(1) lookup by the runner.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IPacketRouter> AvailableRouters =
        SimulationConfig.AvailableRouters.ToDictionary(r => r.Name);

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------

    /// <summary>Whether a benchmark is currently running in the background.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Progress fraction in [0, 1].
    /// Updated after each router completes; 0 before start, 1 when done.
    /// </summary>
    public double Progress { get; private set; }

    /// <summary>The session produced by the most recent run, or <c>null</c> if none yet.</summary>
    public BenchmarkSession? LastSession { get; private set; }

    /// <summary>
    /// All sessions that have been saved to the in-memory library this browser
    /// session.  Persisted only until page refresh (WASM has no server storage).
    /// </summary>
    public List<BenchmarkSession> SavedSessions { get; } = [];

    /// <summary>Raised when <see cref="IsRunning"/>, <see cref="Progress"/>, or
    /// <see cref="LastSession"/> changes, so UI components can re-render.</summary>
    public event Action? StateChanged;

    // ------------------------------------------------------------------
    // JSON options — shared instance for serialise / deserialise
    // ------------------------------------------------------------------

    /// <summary>
    /// JSON serialiser options used for save/load.
    /// <para>
    /// <see cref="JsonPolymorphic"/> on <see cref="BenchmarkEvent"/> requires
    /// the standard System.Text.Json polymorphism support enabled here.
    /// </para>
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented          = true,
        // Preserve polymorphic $type discriminators on BenchmarkEvent variants.
        // ReferenceHandler not needed since the object graph is a simple tree.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ------------------------------------------------------------------
    // Run
    // ------------------------------------------------------------------

    /// <summary>
    /// Starts the benchmark for <paramref name="config"/> asynchronously.
    /// Returns immediately; the caller should watch <see cref="StateChanged"/>
    /// to know when the run finishes.
    /// </summary>
    /// <remarks>
    /// The actual tick loop runs inside <c>Task.Run</c> on a thread-pool thread
    /// so the Blazor render loop is never blocked.  Progress callbacks marshal
    /// back to <see cref="StateChanged"/> without switching to the UI thread
    /// (since this is WASM, all code runs on the same thread anyway, but the
    /// pattern is correct for future server-side use).
    /// </remarks>
    public async Task RunAsync(BenchmarkConfig config)
    {
        if (IsRunning) return;

        IsRunning = true;
        Progress  = 0;
        LastSession = null;
        Notify();

        int total = config.RouterNames
            .Count(n => AvailableRouters.ContainsKey(n));
        if (total == 0) total = 1; // avoid div-by-zero

        try
        {
            // Run headless on the thread pool so WASM can still process JS
            // events (e.g. the browser doesn't freeze).
            var session = await Task.Run(() =>
                BenchmarkRunner.Run(
                    config,
                    AvailableRouters,
                    (done, _) =>
                    {
                        Progress = (double)done / total;
                        Notify();
                    }));

            LastSession = session;
        }
        finally
        {
            IsRunning = false;
            Progress  = 1;
            Notify();
        }
    }

    // ------------------------------------------------------------------
    // Library management
    // ------------------------------------------------------------------

    /// <summary>Saves <paramref name="session"/> to the in-memory library.</summary>
    public void SaveToLibrary(BenchmarkSession session)
    {
        // Avoid duplicates: replace if already present (same object reference).
        var idx = SavedSessions.IndexOf(session);
        if (idx >= 0)
            SavedSessions[idx] = session;
        else
            SavedSessions.Add(session);
        Notify();
    }

    /// <summary>Removes <paramref name="session"/> from the in-memory library.</summary>
    public void DeleteFromLibrary(BenchmarkSession session)
    {
        SavedSessions.Remove(session);
        Notify();
    }

    // ------------------------------------------------------------------
    // JSON serialisation (browser download / upload)
    // ------------------------------------------------------------------

    /// <summary>
    /// Serialises <paramref name="session"/> to a JSON string suitable for
    /// writing to a browser-downloaded file.
    /// </summary>
    public string Serialize(BenchmarkSession session) =>
        JsonSerializer.Serialize(session, JsonOptions);

    /// <summary>
    /// Deserialises a <see cref="BenchmarkSession"/> from a JSON string that
    /// was previously produced by <see cref="Serialize"/>.
    /// Returns <c>null</c> if the JSON is malformed.
    /// </summary>
    public BenchmarkSession? Deserialize(string json)
    {
        try   { return JsonSerializer.Deserialize<BenchmarkSession>(json, JsonOptions); }
        catch { return null; }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void Notify() => StateChanged?.Invoke();
}
