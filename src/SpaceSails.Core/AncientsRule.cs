namespace SpaceSails.Core;

/// <summary>
/// M28 · The Ancients' pilot (Sunday Plan F4; worldbuilding §2). Pyramid satellites are
/// off-board hardware: no departures entry, no NpcShip, and sensor behavior that does NOT
/// match <see cref="SensorModel"/> — they are simply invisible until you are close, sun
/// glare and hull glow be damned, and their orbits are deliberately un-Keplerian (a period
/// no natural object at that radius could have; the attentive player is meant to notice).
/// Approach one and it grants a few AUTO-PLOT CHARGES: spend a charge and the same
/// Simulator-evaluated search that plans NPC routes lays a course to your destination.
/// Design rule: charges are rare — manual flight stays the skill the game teaches.
/// </summary>
public static class AncientsRule
{
    public readonly record struct Pyramid(int Index, double OrbitRadius, double PeriodSeconds, double PhaseRad);

    /// <summary>They reveal themselves inside this range (0.1 AU) — a sensor rule of their
    /// own: no glare, no glow, just a hard bubble. Crossing their ring on an ordinary
    /// interplanetary sail is how they get found.</summary>
    public const double RevealRangeMeters = 1.5e10;

    /// <summary>Close enough for the grant — a deliberate detour inside the reveal bubble.</summary>
    public const double GrantRangeMeters = 2e9;

    /// <summary>Auto-plot charges granted per visit.</summary>
    public const int ChargesPerVisit = 3;

    /// <summary>A pyramid ignores you this long after granting (per pyramid).</summary>
    public const double GrantCooldownSeconds = 30 * 86400;

    // Two pyramids on deliberately impossible orbits: 2.3 AU circling in 200 days (Kepler
    // says ~3.5 years) and 4.6 AU RETROGRADE in 300 days (Kepler says ~10 years). Numbers
    // are constants, not scenario data — the ancients don't read our JSON.
    private static readonly Pyramid[] Pyramids =
    [
        new(0, 3.44e11, 200 * 86400.0, 0.9),
        new(1, 6.88e11, -300 * 86400.0, 3.7),
    ];

    public static int PyramidCount => Pyramids.Length;

    public static Vector2d PyramidPosition(int index, double simTime)
    {
        Pyramid p = Pyramids[index];
        double angle = p.PhaseRad + Math.Tau * simTime / p.PeriodSeconds;
        return new Vector2d(p.OrbitRadius * Math.Cos(angle), p.OrbitRadius * Math.Sin(angle));
    }

    public static bool Revealed(int index, Vector2d shipPosition, double simTime) =>
        (PyramidPosition(index, simTime) - shipPosition).LengthSquared <= RevealRangeMeters * RevealRangeMeters;

    public static bool InGrantRange(int index, Vector2d shipPosition, double simTime) =>
        (PyramidPosition(index, simTime) - shipPosition).LengthSquared <= GrantRangeMeters * GrantRangeMeters;

    /// <summary>The result of an auto-plot: the burn plan the ancient pilot lays, where it
    /// gets you, and when. <see cref="MissDistance"/> is the closest approach to the
    /// destination body — inside its capture range the M25 autopilot takes over from there.</summary>
    public readonly record struct AutoPlotResult(ManeuverPlan Plan, double MissDistance, double ClosestApproachSimTime);

    /// <summary>
    /// Lays a course from the CURRENT ship state to the destination body: a deterministic
    /// grid search over burn direction, pulse count and departure day, each candidate flown
    /// through the real Simulator at a coarse day-scale step (the RoutePlanner pattern,
    /// unhooked from origin bodies). Returns the best plan found; null when the ephemeris
    /// doesn't know the body.
    /// </summary>
    public static AutoPlotResult? AutoPlot(
        ICelestialEphemeris ephemeris,
        ShipState ship,
        string destinationId,
        double horizonSeconds = 730 * 86400.0)
    {
        bool known = false;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == destinationId)
            {
                known = true;
                break;
            }
        }

        if (!known)
        {
            return null;
        }

        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        double[] leadDays = [0.5, 1, 2, 4, 8, 16, 32];
        int[] pulseCounts = [1, 2, 3, 4, 6, 8, 10, 12, 16, 20];

        ManeuverPlan? bestPlan = null;
        double bestMiss = double.MaxValue;
        double bestTime = ship.SimTime;

        foreach (ManeuverAction action in new[] { ManeuverAction.Decelerate, ManeuverAction.Accelerate })
        {
            foreach (double lead in leadDays)
            {
                foreach (int pulses in pulseCounts)
                {
                    var plan = new ManeuverPlan([new ManeuverNode(ship.SimTime + lead * 86400.0, action, pulses)]);
                    IReadOnlyList<TrajectorySample> samples = simulator.ProjectAdaptive(
                        ship, plan, horizonSeconds, maxTimeStep: 86400, maxSamples: 800);

                    foreach (TrajectorySample sample in samples)
                    {
                        double d = (ephemeris.Position(destinationId, sample.SimTime) - sample.Position).Length;
                        if (d < bestMiss)
                        {
                            (bestMiss, bestPlan, bestTime) = (d, plan, sample.SimTime);
                        }
                    }
                }
            }
        }

        return bestPlan is null ? null : new AutoPlotResult(bestPlan, bestMiss, bestTime);
    }
}
