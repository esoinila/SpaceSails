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
/// A scheduled control input: at <paramref name="SimTime"/>, fire <paramref name="Pulses"/>
/// pulses of <paramref name="Action"/>. <paramref name="Fine"/> pulses are ±1% trims instead
/// of the full ±10% — one integer 10% pulse is a ~2.5 km/s hammer, and plotted approaches
/// need a scalpel (owner playtest, M16).
/// </summary>
public readonly record struct ManeuverNode(double SimTime, ManeuverAction Action, int Pulses = 1, bool Fine = false);

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
    /// Combined velocity scale factor of all nodes scheduled in [fromInclusive, toExclusive).
    /// Returns 1.0 when no node fires in the window.
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

            if (node.SimTime >= fromInclusive)
            {
                double pulse = node.Action == ManeuverAction.Accelerate
                    ? (node.Fine ? FineAccelerateFactor : AccelerateFactor)
                    : (node.Fine ? FineDecelerateFactor : DecelerateFactor);
                for (int i = 0; i < node.Pulses; i++)
                {
                    factor *= pulse;
                }
            }
        }

        return factor;
    }
}
