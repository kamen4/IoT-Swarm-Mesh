namespace Engine.Statistics;

/// <summary>
/// A single observable metric tracked by <see cref="SimulationStatistics"/>.
/// UI layers bind to <see cref="DisplayValue"/> and listen to
/// <see cref="SimulationStatistics.Updated"/> for re-renders.
/// </summary>
public sealed class StatMetric
{
    /// <summary>Short label shown in the statistics panel header.</summary>
    public string Label { get; }

    /// <summary>Longer description shown as a tooltip or sub-text.</summary>
    public string Description { get; }

    /// <summary>Whether the value should be displayed with decimal places.</summary>
    public bool IsDecimal { get; }

    /// <summary>
    /// When <c>true</c> this metric can be selected as a chart series in the
    /// detailed statistics view.
    /// </summary>
    public bool IsPlottable { get; }

    /// <summary>Current raw value.</summary>
    public double Value { get; private set; }

    /// <summary>Formatted string ready for display.</summary>
    public string DisplayValue => IsDecimal ? Value.ToString("F2") : ((long)Value).ToString();

    public StatMetric(string label, string description, bool isDecimal = false, bool isPlottable = false)
    {
        Label       = label;
        Description = description;
        IsDecimal   = isDecimal;
        IsPlottable = isPlottable;
    }

    internal void Increment(double by = 1) => Value += by;
    internal void Set(double value)        => Value  = value;
    internal void Reset()                  => Value  = 0;
}
