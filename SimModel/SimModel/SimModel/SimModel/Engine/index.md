# IoT Swarm Mesh — Engine Documentation

Welcome to the **IoT Swarm Mesh** simulation engine documentation.

This site is generated automatically from source XML comments and the
hand-written [conceptual guide](Documentation.md) using
[DocFX](https://dotnet.github.io/docfx/).

## Quick links

- [Conceptual overview & architecture](Documentation.md)
- [API Reference](api/index.md)

## About the project

The **Engine** library is the self-contained simulation core of the IoT Swarm Mesh
project. It models a wireless IoT network on a 2D plane: devices are placed at
arbitrary positions, data packets flow between them tick-by-tick, and a flooding
broadcast algorithm propagates data toward the central hub.

The library has **no UI dependencies** and can be driven by any host (console,
Blazor WebAssembly, test harness) that calls `SimulationEngine.Instance.Tick()`.

> Source code: [github.com/kamen4/IoT-Swarm-Mesh](https://github.com/kamen4/IoT-Swarm-Mesh)
