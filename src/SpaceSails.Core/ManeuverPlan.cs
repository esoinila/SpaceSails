namespace SpaceSails.Core;

/// <summary>The only propulsion in the game: scale the velocity vector up or down 10%.</summary>
public enum ManeuverAction
{
    /// <summary>Multiply velocity by 1.1.</summary>
    Accelerate,

    /// <summary>Multiply velocity by 0.9.</summary>
    Decelerate,
}

/// <summary>
/// How a node spends its pulses. The two modes coexist in one plan (MondayPonder: an X-Pilot
/// point-and-burn is needed to chase pods that climb out of the ecliptic without a gravity sling).
/// </summary>
public enum BurnMode
{
    /// <summary>The classic burn: scale the velocity vector's magnitude along its own direction
    /// (±<c>Percent</c> per pulse). Cheap, predictable, and always prograde/retrograde.</summary>
    Factor,

    /// <summary>The X-Pilot burn: add Δv along a fixed heading (<c>HeadingDegrees</c>) regardless of
    /// where the ship is already pointed. Per-pulse Δv magnitude is <c>Percent%</c> of the speed the
    /// ship carries entering the node — so a Vector burn aimed straight down the velocity vector is
    /// identical to a Factor Accelerate, and any other heading turns the course.</summary>
    Vector,
}

/// <summary>
/// A scheduled control input: at <paramref name="SimTime"/>, fire <paramref name="Pulses"/>
/// pulses of <paramref name="Action"/>. <paramref name="Percent"/> is the per-pulse strength
/// as a free double (owner request, M16c: "make the thrust amount float instead of 0.1 or 1");
/// 0 keeps the legacy quanta — ±10%, or ±1% when <paramref name="Fine"/> is set.
/// <paramref name="Mode"/> selects Factor (magnitude-only, <paramref name="Action"/> decides the
/// sign) or Vector (point-and-burn along <paramref name="HeadingDegrees"/>, a world-space heading
/// where 0° is +X and angle increases counter-clockwise; <paramref name="Action"/> is ignored).
/// </summary>
public readonly record struct ManeuverNode(
    double SimTime,
    ManeuverAction Action,
    int Pulses = 1,
    bool Fine = false,
    double Percent = 0,
    BurnMode Mode = BurnMode.Factor,
    double HeadingDegrees = 0)
{
    /// <summary>Effective per-pulse percentage, resolving the legacy Fine flag.</summary>
    public double EffectivePercent => Percent > 0 ? Percent : (Fine ? 1.0 : 10.0);
}

/// <summary>
/// A ship's flight plan: an ordered maneuver schedule. The same type serves the player's plotted
/// course and NPC hidden plans — one simulation engine drives both.
/// </summary>
public sealed class ManeuverPlan
{
    public const double AccelerateFactor = 1.1;
    public const double DecelerateFactor = 0.9;
    public const double FineAccelerateFactor = 1.01;
    public const double FineDecelerateFactor = 0.99;

    public static readonly ManeuverPlan Empty = new([]);

    private readonly ManeuverNode[] _nodes;

    public ManeuverPlan(IEnumerable<ManeuverNode> nodes)
    {
        _nodes = [.. nodes.OrderBy(n => n.SimTime)];
    }

    public IReadOnlyList<ManeuverNode> Nodes => _nodes;

    /// <summary>
    /// Combined velocity scale factor of the Factor-mode nodes scheduled in
    /// [fromInclusive, toExclusive). Returns 1.0 when no such node fires in the window. Vector-mode
    /// nodes cannot be reduced to a scalar (they turn the velocity), so they are ignored here — use
    /// <see cref="ApplyBurnsInWindow"/> to fly a plan that may contain either kind.
    /// </summary>
    public double ScaleFactorInWindow(double fromInclusive, double toExclusive)
    {
        double factor = 1.0;
        foreach (ManeuverNode node in _nodes)
        {
            if (node.SimTime >= toExclusive)
            {
                break;
            }

            if (node.SimTime >= fromInclusive && node.Mode == BurnMode.Factor)
            {
                double pulse = node.Action == ManeuverAction.Accelerate
                    ? 1.0 + node.EffectivePercent / 100.0
                    : 1.0 - node.EffectivePercent / 100.0;
                for (int i = 0; i < node.Pulses; i++)
                {
                    factor *= pulse;
                }
            }
        }

        return factor;
    }

    /// <summary>
    /// Apply every node scheduled in [fromInclusive, toExclusive) to <paramref name="velocity"/>,
    /// in time order, and return the resulting velocity. This is the general path the integrator
    /// uses — it handles both burn modes and is exact for a plan of pure Factor nodes (the product
    /// of their scale factors equals <see cref="ScaleFactorInWindow"/>).
    ///
    /// A Factor node scales the magnitude along the current velocity. A Vector node adds
    /// <c>Percent%</c> of the entering speed as Δv along its fixed heading per pulse — so pointing
    /// the heading down the velocity vector reproduces a Factor Accelerate, and any other heading
    /// bends the course (the X-Pilot burn: climb out of the plane, chase a pod, turn without a sling).
    /// </summary>
    public Vector2d ApplyBurnsInWindow(Vector2d velocity, double fromInclusive, double toExclusive)
    {
        foreach (ManeuverNode node in _nodes)
        {
            if (node.SimTime >= toExclusive)
            {
                break;
            }

            if (node.SimTime < fromInclusive)
            {
                continue;
            }

            if (node.Mode == BurnMode.Vector)
            {
                // Each pulse adds Percent% of the *current* speed as Δv along the heading — recomputed
                // per pulse, so straight ahead it compounds exactly like a Factor Accelerate (1.1×
                // per pulse) and one Vector pulse always costs the same reaction mass as one Factor
                // pulse. Heading is world-space (0° = +X, CCW positive), matching the map's arrow.
                double radians = node.HeadingDegrees * Math.PI / 180.0;
                var direction = new Vector2d(Math.Cos(radians), Math.Sin(radians));
                double fraction = node.EffectivePercent / 100.0;
                for (int i = 0; i < node.Pulses; i++)
                {
                    velocity += direction * (velocity.Length * fraction);
                }
            }
            else
            {
                double pulse = node.Action == ManeuverAction.Accelerate
                    ? 1.0 + node.EffectivePercent / 100.0
                    : 1.0 - node.EffectivePercent / 100.0;
                for (int i = 0; i < node.Pulses; i++)
                {
                    velocity *= pulse;
                }
            }
        }

        return velocity;
    }
}
