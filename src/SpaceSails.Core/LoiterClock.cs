namespace SpaceSails.Core;

/// <summary>
/// #733 — THE CLOCK COST A CAPTAIN PAYS SOMEWHERE ELSE, and what it does to the ship they left behind.
///
/// <para>Two things move the sim clock without anybody flying anything: riding the shuttle down to a
/// surface (the door that is a flight, <see cref="ShuttleRange"/>) and turning in at the bunk
/// (<see cref="CabinComforts.SleepSimSeconds"/>). Both used to re-stamp the mothership at the new epoch
/// with her position and velocity untouched — the old comment's words, "frozen in place; the shuttle
/// moved, not the mothership".</para>
///
/// <para><b>That sentence is only true in the ship's own frame.</b> A mothership loitering alongside
/// Enceladus is doing ~7.6 km/s in the Sun's frame, which is the frame every position in this game is
/// written in. A clock that moves while she does not is therefore a teleport BACKWARDS along her own
/// rail — and the moon she is parked beside keeps going, and runs her down. That is the whole of #733:
/// <c>/map?kaamos=hq&amp;land=1&amp;floor=23</c> killed the captain seconds after the ground card, with a
/// death line quoting a periapsis "under the surface" that the parked orbit never actually had.</para>
///
/// <para><b>And it was never bad luck.</b> The crossing costs <c>gap ÷ CruiseSpeedMps</c> (8 km/s) while
/// the ice moon's own absolute speed is ~7.6 km/s, so the moon covers very nearly the standoff in very
/// nearly the time the freeze lasts. The miss distance scales WITH the gap, which is why parking further
/// out could never have been the cure: BOTH standoffs the game actually uses — 5e6 m for the
/// <c>?start=enceladus</c> bench start and 1e7 m for <see cref="CyclerWindow.ArrivalOffsetMeters"/>, the
/// cycler arrival the head office is reached on — end up inside Enceladus's 252 km surface radius.</para>
///
/// <para>A DOCKED ship never had the bug, and that is the tell the issue asked for: the clamp re-pins her
/// onto the berth's rail at the new epoch, which is exactly why <c>?secretlab=deep&amp;land=1</c> loiters
/// safely for half an hour while the head office lithobrakes. This is the same law from the other end —
/// a free-flying ship FLIES the crossing. She coasts her conic ballistically (nobody is at the helm; an
/// armed burn does not fire itself while the captain is down a lift shaft), and if that conic really does
/// reach a surface the strike is handed BACK to the caller rather than tunnelled through, so an orbit that
/// genuinely was diving still ends the way #264 says it must.</para>
/// </summary>
public static class LoiterClock
{
    /// <summary>The coarse quantum the coast is flown in — the live loop's own warp quantum, so a loiter
    /// and a fast-forward integrate the same way, and <see cref="SurfaceImpact"/> gets a look once per
    /// quantum rather than once per crossing (a whole small moon can pass between two coarse endpoints,
    /// which is the tunnelling #264 already paid for once).</summary>
    public const double QuantumSeconds = 60.0;

    /// <summary>Where she ended up, and what she met on the way. <see cref="Struck"/> is null on a clean
    /// coast; when it is set, <see cref="Ship"/> is the state at the end of the quantum that struck and
    /// the caller owns the consequence.</summary>
    public readonly record struct Coast(ShipState Ship, SurfaceImpact.Crossing? Struck);

    /// <summary>
    /// Fly <paramref name="ship"/> forward by <paramref name="seconds"/> of clock she is not steering
    /// through, watching for a surface crossing every <see cref="QuantumSeconds"/>. A non-positive cost
    /// returns her untouched. No <see cref="ManeuverPlan"/> is applied on purpose — see the type remarks.
    /// </summary>
    public static Coast Advance(
        Simulator simulator, ICelestialEphemeris ephemeris, ShipState ship, double seconds)
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ArgumentNullException.ThrowIfNull(ephemeris);

        if (!(seconds > 0.0))
        {
            return new Coast(ship, null);
        }

        // Each quantum's end is measured off the ONE end epoch rather than accumulated, so a long coast
        // cannot drift its own clock a quantum at a time.
        double end = ship.SimTime + seconds;
        const double Settled = 1e-9; // closer than this to the end is the end

        while (end - ship.SimTime > Settled)
        {
            double quantum = Math.Min(QuantumSeconds, end - ship.SimTime);
            Vector2d from = ship.Position;
            double fromTime = ship.SimTime;
            ship = simulator.RunAdaptive(ship, quantum);

            if (SurfaceImpact.FirstCrossing(from, fromTime, ship.Position, ship.SimTime, ephemeris) is { } hit)
            {
                return new Coast(ship, hit);
            }
        }

        return new Coast(ship, null);
    }
}
