using Engine.Devices;
using System.Numerics;

namespace WebApp.Client.Services;

/// <summary>
/// Static catalog of all built-in network-topology presets.
/// <para>
/// Each <see cref="SimulationPreset"/> carries a name, a one-line description,
/// and a build delegate that registers devices into a freshly-reset engine via
/// the supplied <see cref="SimulationService"/>.
/// </para>
/// <para>
/// To add a new preset: append a new entry to <see cref="All"/> following the
/// same pattern — create a static method that accepts a
/// <see cref="SimulationService"/> and registers devices, then reference it as
/// the <c>Build</c> delegate.
/// </para>
/// </summary>
public static class SimulationPresets
{
    /// <summary>All available presets in display order.</summary>
    public static readonly IReadOnlyList<SimulationPreset> All =
    [
        new("Default",          "Hub in the centre with 3 sensors and 2 lamps",                         BuildDefault),
        new("Line",             "Hub + 5 devices in a centred chain; each node reaches only neighbours", BuildLine),
        new("Star",             "Hub at the centre, 8 nodes equally spaced on a ring",                  BuildStar),
        new("Dense Cluster",    "Hub + 10 devices in a tight cluster — every node sees every other",    BuildDenseCluster),
        new("Two Clusters",     "Two groups bridged by a single relay node",                            BuildTwoClusters),
        new("Three Clusters",   "Three groups in a triangle bridged by relay nodes on each edge",       BuildThreeClusters),
        new("Grid 3?3",         "3?3 grid — each node reaches its 4 cross-neighbours only",             BuildGrid3x3),
        new("Long Chain",       "Hub + 8 nodes in a single line — hardest path for flooding",           BuildLongChain),
        new("Random (small)",   "8 randomly placed devices — reproducible seed 1",                      s => BuildRandom(s,  8, seed: 1)),
        new("Random (large)",   "20 randomly placed devices — reproducible seed 2",                     s => BuildRandom(s, 20, seed: 2)),
    ];

    // -------------------------------------------------------------------------
    // Preset builders
    // -------------------------------------------------------------------------

    /// <summary>Default starter layout: hub centre, 3 sensors, 2 lamps.</summary>
    private static void BuildDefault(SimulationService s)
    {
        s.Engine.RegisterDevice(new HubDevice           { Name = "Hub",      Position = new Vector2(  0,   0) });
        s.Engine.RegisterDevice(new GeneratorDevice(40) { Name = "Sensor-A", Position = new Vector2(150,   0) });
        s.Engine.RegisterDevice(new GeneratorDevice(50) { Name = "Sensor-B", Position = new Vector2(-150,  0) });
        s.Engine.RegisterDevice(new GeneratorDevice(45) { Name = "Sensor-C", Position = new Vector2(  0, 150) });
        s.Engine.RegisterDevice(new EmitterDevice       { Name = "Lamp-1",   Position = new Vector2( 80, 120) });
        s.Engine.RegisterDevice(new EmitterDevice       { Name = "Lamp-2",   Position = new Vector2(-80,-120) });
    }

    /// <summary>
    /// Six devices in a straight horizontal chain centred on the origin.
    /// Step = 80 % of visibility so adjacent nodes are within range but
    /// nodes two hops away are not.
    /// </summary>
    private static void BuildLine(SimulationService s)
    {
        float step  = s.Engine.VisibilityDistance * 0.8f;
        // 6 nodes: indices 0..5, centred so that the midpoint of the chain is at x=0
        float startX = -step * 2.5f;

        s.Engine.RegisterDevice(new HubDevice           { Name = "Hub",    Position = new Vector2(startX,          0) });
        s.Engine.RegisterDevice(new GeneratorDevice(30) { Name = "Node-1", Position = new Vector2(startX + step,   0) });
        s.Engine.RegisterDevice(new GeneratorDevice(40) { Name = "Node-2", Position = new Vector2(startX + step*2, 0) });
        s.Engine.RegisterDevice(new EmitterDevice       { Name = "Lamp-3", Position = new Vector2(startX + step*3, 0) });
        s.Engine.RegisterDevice(new GeneratorDevice(35) { Name = "Node-4", Position = new Vector2(startX + step*4, 0) });
        s.Engine.RegisterDevice(new EmitterDevice       { Name = "Lamp-5", Position = new Vector2(startX + step*5, 0) });
    }

    /// <summary>
    /// Hub in the centre with 8 nodes placed at equal angles on a circle
    /// whose radius is 70 % of visibility.  All nodes see the hub and their
    /// two immediate ring-neighbours.
    /// </summary>
    private static void BuildStar(SimulationService s)
    {
        float r = s.Engine.VisibilityDistance * 0.7f;

        s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = Vector2.Zero });

        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI * 2 / 8;
            var pos      = new Vector2((float)(Math.Cos(angle) * r), (float)(Math.Sin(angle) * r));
            if (i % 3 == 2)
                s.Engine.RegisterDevice(new EmitterDevice       { Name = $"Lamp-{i + 1}",   Position = pos });
            else
                s.Engine.RegisterDevice(new GeneratorDevice(30 + i * 5) { Name = $"Sensor-{i + 1}", Position = pos });
        }
    }

    /// <summary>
    /// Hub and 10 devices packed in a tight cluster so that every node can
    /// see every other node — maximum flooding load.
    /// Nodes are placed on two concentric rings so labels stay readable.
    /// </summary>
    private static void BuildDenseCluster(SimulationService s)
    {
        // Use two rings so nodes don't stack on top of each other.
        // Inner ring: 4 nodes at r1; outer ring: 6 nodes at r2.
        // Both radii well inside visibility distance.
        float r1 = s.Engine.VisibilityDistance * 0.18f;
        float r2 = s.Engine.VisibilityDistance * 0.38f;

        s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = Vector2.Zero });

        for (int i = 0; i < 4; i++)
        {
            double a   = i * Math.PI * 2 / 4;
            var pos    = new Vector2((float)(Math.Cos(a) * r1), (float)(Math.Sin(a) * r1));
            s.Engine.RegisterDevice(new GeneratorDevice(25 + i * 10) { Name = $"S-In{i + 1}", Position = pos });
        }

        for (int i = 0; i < 6; i++)
        {
            double a   = i * Math.PI * 2 / 6;
            var pos    = new Vector2((float)(Math.Cos(a) * r2), (float)(Math.Sin(a) * r2));
            if (i % 3 == 2)
                s.Engine.RegisterDevice(new EmitterDevice       { Name = $"Lamp-{i + 1}",  Position = pos });
            else
                s.Engine.RegisterDevice(new GeneratorDevice(30 + i * 8) { Name = $"S-Out{i + 1}", Position = pos });
        }
    }

    /// <summary>
    /// Two clusters of devices placed symmetrically around the origin.
    /// A single bridge node sits exactly halfway between them.
    /// <para>
    /// Connectivity guarantee: cluster spread ? 0.45 ? vis, bridge distance
    /// from each cluster centre = 0.9 ? vis, so every cluster member is at
    /// most 0.45 + 0.9 = 1.35 ? vis from the bridge... but that's still too
    /// far. So we set spread = 0.8 ? vis and place bridge members deterministically
    /// on a tight ring so their outermost point is within vis of the bridge.
    /// </para>
    /// </summary>
    private static void BuildTwoClusters(SimulationService s)
    {
        float vis    = s.Engine.VisibilityDistance;
        // Each cluster centre is 'half' from the origin;
        // bridge at origin must be within vis of every cluster member.
        // cluster-centre distance from bridge = half;  members are ? clrR from centre.
        // Constraint: half + clrR ? vis  ?  use half=0.65*vis, clrR=0.28*vis (sum=0.93)
        float half   = vis * 0.65f;
        float clrR   = vis * 0.28f;

        var leftCentre  = new Vector2(-half, 0);
        var rightCentre = new Vector2( half, 0);

        // Left cluster — hub + 3 sensors on a small ring
        s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = leftCentre });
        for (int i = 0; i < 3; i++)
        {
            double a   = i * Math.PI * 2 / 3 + Math.PI / 6;
            var pos    = leftCentre + new Vector2((float)(Math.Cos(a) * clrR), (float)(Math.Sin(a) * clrR));
            s.Engine.RegisterDevice(new GeneratorDevice(30 + i * 10) { Name = $"S-L{i + 1}", Position = pos });
        }

        // Bridge node at the origin
        s.Engine.RegisterDevice(new GeneratorDevice(60) { Name = "Bridge", Position = Vector2.Zero });

        // Right cluster — 3 sensors + 1 lamp on a small ring
        for (int i = 0; i < 3; i++)
        {
            double a   = i * Math.PI * 2 / 3 + Math.PI / 6;
            var pos    = rightCentre + new Vector2((float)(Math.Cos(a) * clrR), (float)(Math.Sin(a) * clrR));
            s.Engine.RegisterDevice(new GeneratorDevice(35 + i * 10) { Name = $"S-R{i + 1}", Position = pos });
        }
        s.Engine.RegisterDevice(new EmitterDevice { Name = "Lamp-R", Position = rightCentre });
    }

    /// <summary>
    /// Three clusters arranged in an equilateral triangle.
    /// Hub is in the first cluster; a bridge node sits on each edge midpoint.
    /// <para>
    /// Connectivity guarantee: triangle side = 1.2 ? vis; bridge is at the
    /// midpoint (0.6 ? vis from each centre); cluster members are on a ring
    /// of radius 0.28 ? vis, so max distance from bridge = 0.6 + 0.28 = 0.88 ? vis.
    /// </para>
    /// </summary>
    private static void BuildThreeClusters(SimulationService s)
    {
        float vis  = s.Engine.VisibilityDistance;
        float side = vis * 1.2f;   // triangle side — bridge midpoint is 0.6*vis from each centre
        float cr   = vis * 0.28f;  // intra-cluster ring radius

        // Equilateral triangle, centred at origin, flat-bottom
        float h = side * (float)(Math.Sqrt(3) / 2);
        var centres = new Vector2[]
        {
            new(     0,  h * 2f / 3f),          // top  (hub cluster)
            new(-side / 2, -h / 3f),            // bottom-left
            new( side / 2, -h / 3f),            // bottom-right
        };

        string[] labels = ["A", "B", "C"];

        for (int c = 0; c < 3; c++)
        {
            var centre = centres[c];
            if (c == 0)
                s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = centre });
            else
                s.Engine.RegisterDevice(new EmitterDevice { Name = $"Lamp-{labels[c]}", Position = centre });

            // 3 sensors evenly spaced on a ring inside the cluster
            for (int i = 0; i < 3; i++)
            {
                double a   = i * Math.PI * 2 / 3 + c * Math.PI / 4;
                var pos    = centre + new Vector2((float)(Math.Cos(a) * cr), (float)(Math.Sin(a) * cr));
                s.Engine.RegisterDevice(new GeneratorDevice(30 + c * 10 + i * 5)
                {
                    Name     = $"S-{labels[c]}{i + 1}",
                    Position = pos
                });
            }
        }

        // Bridge nodes at midpoints of each edge of the triangle
        for (int i = 0; i < 3; i++)
        {
            var mid = (centres[i] + centres[(i + 1) % 3]) / 2f;
            s.Engine.RegisterDevice(new GeneratorDevice(50) { Name = $"Br-{i + 1}", Position = mid });
        }
    }

    /// <summary>
    /// Nine nodes in a regular 3?3 grid centred at the origin.
    /// Spacing = 72 % of visibility so cross-neighbours are within range
    /// (0.72 ? vis) but diagonal neighbours are not (0.72 ? ?2 ? 1.02 ? vis).
    /// </summary>
    private static void BuildGrid3x3(SimulationService s)
    {
        float spacing = s.Engine.VisibilityDistance * 0.72f;

        string[,] names =
        {
            { "S-TL", "S-TM", "S-TR" },
            { "S-ML", "Hub",  "S-MR" },
            { "Lamp-BL", "S-BM", "Lamp-BR" },
        };

        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 3; col++)
        {
            var pos  = new Vector2((col - 1) * spacing, (row - 1) * spacing);
            string n = names[row, col];

            Device device = n switch
            {
                "Hub"                              => new HubDevice     { Name = n, Position = pos },
                var x when x.StartsWith("Lamp")    => new EmitterDevice { Name = n, Position = pos },
                _                                  => new GeneratorDevice(30 + row * 10 + col * 5) { Name = n, Position = pos },
            };
            s.Engine.RegisterDevice(device);
        }
    }

    /// <summary>
    /// Hub followed by 8 devices in a single horizontal chain centred at origin.
    /// Step = 75 % of visibility so only immediate neighbours can see each other.
    /// </summary>
    private static void BuildLongChain(SimulationService s)
    {
        float step   = s.Engine.VisibilityDistance * 0.75f;
        // 9 nodes total: indices 0..8, centred
        float startX = -step * 4f;

        s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = new Vector2(startX, 0) });

        for (int i = 1; i <= 8; i++)
        {
            Device device = i % 3 == 0
                ? new EmitterDevice       { Name = $"Lamp-{i}", Position = new Vector2(startX + step * i, 0) }
                : new GeneratorDevice(20 + i * 5) { Name = $"Node-{i}", Position = new Vector2(startX + step * i, 0) };
            s.Engine.RegisterDevice(device);
        }
    }

    /// <summary>
    /// Randomly places <paramref name="count"/> devices around the hub using a
    /// fixed <paramref name="seed"/> so the layout is reproducible across runs.
    /// </summary>
    private static void BuildRandom(SimulationService s, int count, int seed)
    {
        var rng = new Random(seed);
        s.Engine.RegisterDevice(new HubDevice { Name = "Hub", Position = Vector2.Zero });

        for (int i = 0; i < count; i++)
        {
            double angle  = rng.NextDouble() * Math.PI * 2;
            double radius = rng.NextDouble() * s.Engine.VisibilityDistance * 2.2;
            var pos = new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));

            Device device = i % 2 == 0
                ? new GeneratorDevice(rng.Next(20, 80)) { Name = $"Sensor-{i + 1}", Position = pos }
                : new EmitterDevice                     { Name = $"Lamp-{i + 1}",   Position = pos };

            s.Engine.RegisterDevice(device);
        }
    }
}
