namespace SpaceSails.Core;

/// <summary>What kind of thing an area scan resolved.</summary>
public enum DiscoveryKind
{
    Debris,
    Rock,
    ColdPod,
    Derelict,
}

/// <summary>One thing the telescope resolved in a scanned patch of sky.</summary>
public readonly record struct Discovery(
    string Id, DiscoveryKind Kind, Vector2d Position, Vector2d Velocity, string Description);

/// <summary>
/// The populated sky (SundaySecondPlan F3): "We point an onboard Hubble class telescope at an
/// area, we will find something new, even debris, something not just nothing." Deterministic —
/// the sky is a fixed function of (scenario seed, sky cell, sim day), so two captains scanning
/// the same patch on the same day resolve the same objects, and re-scanning tomorrow finds the
/// sky's next page. Never randomness at call time, and never an empty result.
/// </summary>
public static class ScanDiscoveries
{
    /// <summary>Sky quantization: discoveries live in fixed square cells of this size.</summary>
    public const double CellSizeMeters = 2.0e10;

    /// <summary>The sky's page turns once per sim day — yesterday's debris has drifted on.</summary>
    public const double RefreshSeconds = 86400;

    /// <summary>Safety cap on the cell walk for huge requested radii.</summary>
    private const int MaxCellSpan = 24;

    private static readonly string[] DebrisFlavors =
    [
        "hull plating shard, tumbling",
        "spent pulse casing cluster",
        "shredded sail film, slowly folding",
        "insulation foam swarm",
    ];

    private static readonly string[] RockFlavors =
    [
        "carbonaceous rock, a few meters across",
        "metal-rich boulder, bright albedo",
        "loose gravel pack on a slow drift",
    ];

    /// <summary>
    /// Everything the telescope resolves in the disc (<paramref name="center"/>,
    /// <paramref name="radiusMeters"/>) at <paramref name="simTime"/>. Walks every sky cell the
    /// disc touches; each cell's contents are a pure function of the seed, cell, and sim day.
    /// If the disc happens to cover only empty cells, one guaranteed find is synthesized inside
    /// it — a Hubble pointed anywhere resolves *something*.
    /// </summary>
    public static IReadOnlyList<Discovery> FindAt(
        ulong scenarioSeed, Vector2d center, double radiusMeters, double simTime)
    {
        long day = (long)Math.Floor(simTime / RefreshSeconds);
        var found = new List<Discovery>();

        long cellX0 = CellOf(center.X - radiusMeters), cellX1 = CellOf(center.X + radiusMeters);
        long cellY0 = CellOf(center.Y - radiusMeters), cellY1 = CellOf(center.Y + radiusMeters);
        if (cellX1 - cellX0 >= MaxCellSpan)
        {
            (cellX0, cellX1) = (CellOf(center.X) - MaxCellSpan / 2, CellOf(center.X) + MaxCellSpan / 2);
        }

        if (cellY1 - cellY0 >= MaxCellSpan)
        {
            (cellY0, cellY1) = (CellOf(center.Y) - MaxCellSpan / 2, CellOf(center.Y) + MaxCellSpan / 2);
        }

        double radiusSquared = radiusMeters * radiusMeters;
        for (long cellY = cellY0; cellY <= cellY1; cellY++)
        {
            for (long cellX = cellX0; cellX <= cellX1; cellX++)
            {
                AppendCellFinds(scenarioSeed, cellX, cellY, day, center, radiusSquared, found);
            }
        }

        if (found.Count == 0)
        {
            found.Add(GuaranteedFind(scenarioSeed, center, radiusMeters, day));
        }

        return found;
    }

    private static long CellOf(double coordinate) => (long)Math.Floor(coordinate / CellSizeMeters);

    private static void AppendCellFinds(
        ulong seed, long cellX, long cellY, long day,
        Vector2d scanCenter, double radiusSquared, List<Discovery> found)
    {
        var rng = new DeterministicRandom(Mix(seed, cellX, cellY, day));
        int count = rng.NextInt(0, 3); // most cells are quiet; some hold one or two things
        for (int i = 0; i < count; i++)
        {
            var position = new Vector2d(
                (cellX + rng.NextDouble()) * CellSizeMeters,
                (cellY + rng.NextDouble()) * CellSizeMeters);
            DiscoveryKind kind = PickKind(rng.NextDouble());
            var velocity = new Vector2d(rng.NextDouble(-800, 800), rng.NextDouble(-800, 800));
            string description = Describe(kind, rng);
            if ((position - scanCenter).LengthSquared <= radiusSquared)
            {
                found.Add(new Discovery($"find:{cellX}:{cellY}:{day}:{i}", kind, position, velocity, description));
            }
        }
    }

    private static Discovery GuaranteedFind(ulong seed, Vector2d center, double radiusMeters, long day)
    {
        var rng = new DeterministicRandom(Mix(seed ^ 0xA5A5A5A5A5A5A5A5UL, CellOf(center.X), CellOf(center.Y), day));
        double angle = rng.NextDouble(0, Math.Tau);
        double offset = rng.NextDouble(0.1, 0.7) * radiusMeters;
        var position = center + new Vector2d(Math.Cos(angle), Math.Sin(angle)) * offset;
        var velocity = new Vector2d(rng.NextDouble(-400, 400), rng.NextDouble(-400, 400));
        DiscoveryKind kind = rng.NextDouble() < 0.7 ? DiscoveryKind.Debris : DiscoveryKind.Rock;
        return new Discovery(
            $"find:{CellOf(center.X)}:{CellOf(center.Y)}:{day}:faint", kind, position, velocity,
            $"faint return — {Describe(kind, rng)}");
    }

    private static DiscoveryKind PickKind(double roll) => roll switch
    {
        < 0.45 => DiscoveryKind.Debris,
        < 0.80 => DiscoveryKind.Rock,
        < 0.95 => DiscoveryKind.ColdPod,
        _ => DiscoveryKind.Derelict,
    };

    private static string Describe(DiscoveryKind kind, DeterministicRandom rng) => kind switch
    {
        DiscoveryKind.Debris => DebrisFlavors[rng.NextInt(0, DebrisFlavors.Length)],
        DiscoveryKind.Rock => RockFlavors[rng.NextInt(0, RockFlavors.Length)],
        DiscoveryKind.ColdPod => "cold cargo pod — transponder silent, ballistic",
        _ => "derelict hull, no thermal signature",
    };

    private static ulong Mix(ulong seed, long a, long b, long c)
    {
        unchecked
        {
            ulong z = seed
                + (ulong)a * 0x9E3779B97F4A7C15UL
                + (ulong)b * 0xC2B2AE3D27D4EB4FUL
                + (ulong)c * 0x165667B19E3779F9UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
