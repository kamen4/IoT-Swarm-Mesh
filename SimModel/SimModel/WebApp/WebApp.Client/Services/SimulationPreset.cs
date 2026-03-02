using Engine.Devices;
using System.Numerics;

namespace WebApp.Client.Services;

/// <summary>
/// Describes a named, ready-to-use network layout.
/// A preset is a pure data record: it carries a display name, an optional
/// description, and a factory delegate that populates a fresh engine with
/// the desired devices.
/// </summary>
/// <param name="Name">Short label shown in the UI dropdown.</param>
/// <param name="Description">One-line description shown as a tooltip / sub-text.</param>
/// <param name="Build">
/// Delegate invoked with a <see cref="SimulationService"/> to register all
/// devices. The engine is already reset before <see cref="Build"/> is called.
/// </param>
public sealed record SimulationPreset(
    string Name,
    string Description,
    Action<SimulationService> Build
);
