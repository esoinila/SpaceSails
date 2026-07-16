namespace SpaceSails.Core;

/// <summary>
/// #146/#157/#166 · The pump crawl. Answers one question the ship never asked before it starved:
/// <b>from where I am, with the pulses I have, can I still reach a fuel pump?</b> A depot rides the
/// rails at every planet, every station and every pirate haven (<see cref="TrafficSchedule.GenerateDepots"/>),
/// but <b>never at an ordinary moon</b> — so Titan and Luna are dry, and a captain parked there is a
/// crawl away from fuel. This service prices that crawl with the game's own planner
/// (<see cref="TransferPlanner"/> — the moon-run + last-mile machinery) and its own pulse kernel
/// (<see cref="OrbitRule.PulsesFor"/>), so the alarm's red line and the flight software's bill cannot
/// drift apart.
///
/// <para><b>The three verdicts</b> map straight onto the #166 fuel alarm:
/// <list type="bullet">
/// <item><see cref="Verdict.Comfortable"/> — the tank clears the well-aware reserve floor (or the ship
/// is already alongside a pump);</item>
/// <item><see cref="Verdict.Thin"/> — a pump is still reachable, but the tank has dipped below the
/// reserve it should hold (the amber squawk: refuel now);</item>
/// <item><see cref="Verdict.CannotReachAPump"/> — the cheapest reachable pump costs more pulses than
/// remain: the ship is one burn short of everywhere (the red squawk, the #146 starve).</item>
/// </list></para>
///
/// <para><b>The reserve floor is well-aware, not flat.</b> The #146 lesson is that a flat 18% of the
/// tank (<see cref="AutopilotRehearsal.ReserveFraction"/>, ~45 p on the base tank) does not track the
/// crawl: from a dry moon the nearest pump can be farther than the flat reserve (Lab 28 measures the
/// reach to Enceladus from a parked-at-Titan doorstep at ~77 p, above the 45 p flat floor). So the
/// amber floor RIDES the reach: <c>SafeReserve = nearest-reach + flat 18% cushion</c>. When the ship
/// is alongside a pump the reach is 0 and this collapses to the plain 18% amber of #166; out in the
/// well it rises to include the fare home. The reach is priced live by the same
/// <see cref="TransferPlanner"/> kernel the flight software spends with, so the alarm and the bill
/// cannot drift; the reference red lines are documented in <c>labs/28-the-pump-crawl</c>.</para>
///
/// <para><b>Parked at a dry moon.</b> The planner rightly refuses a departure from inside a moon's
/// Hill sphere (a documented follow-up), so when the given state sits inside a non-pump moon's Hill,
/// this service prices from that moon's <em>doorstep</em> — just outside the Hill, riding the moon's
/// velocity — which is the honest "parked, ready to leave" state the arm-click would hand the planner.
/// The small lift back to the doorstep is absorbed by the reserve pad. <see cref="Assessment.Reason"/>
/// records when this happened. Deterministic: a pure function of the inputs.</para>
/// </summary>
public static class FuelReachability
{
    /// <summary>The fuel alarm's three states, in order of increasing trouble.</summary>
    public enum Verdict
    {
        /// <summary>Clear of the well-aware reserve floor, or already alongside a pump.</summary>
        Comfortable,

        /// <summary>A pump is still reachable, but the tank is below the reserve it should hold — the
        /// amber crossing (#166): refuel at the next opportunity.</summary>
        Thin,

        /// <summary>The cheapest reachable pump costs more pulses than remain (or no pump is reachable
        /// at all) — the red crossing (#166/#146): stranded a burn short of a refuel.</summary>
        CannotReachAPump,
    }

    /// <summary>One priced route to a pump: the depot's host body and the planner's pulse estimate to
    /// ride the well onto it (0 when the ship is already alongside it).</summary>
    public readonly record struct DepotReach(string DepotBodyId, int Pulses);

    /// <summary>The fuel-reachability verdict for a ship state. <see cref="NearestDepotPulses"/> is
    /// <see cref="int.MaxValue"/> when no pump is reachable; <see cref="MarginPulses"/> is
    /// <c>Remaining − nearest reach</c> (how many pulses of slack over just reaching the cheapest pump);
    /// <see cref="SafeReservePulses"/> is the floor below which the verdict is <see cref="Verdict.Thin"/>
    /// or worse. <see cref="Reachable"/> lists every pump the planner found, cheapest first.</summary>
    public readonly record struct Assessment(
        Verdict Verdict,
        string? NearestDepotBodyId,
        int NearestDepotPulses,
        int RemainingPulses,
        int MarginPulses,
        int SafeReservePulses,
        IReadOnlyList<DepotReach> Reachable,
        string Reason);

    /// <summary>
    /// Assess whether <paramref name="ship"/> can still reach a fuel pump in the <paramref name="wellBodyId"/>
    /// well with <paramref name="remainingPulses"/> in a tank of <paramref name="tankCapacity"/>. Pumps are
    /// the depot-bearing children of the well (stations and haven moons — the same rule
    /// <see cref="TrafficSchedule.GenerateDepots"/> spawns them by). Each is priced with
    /// <see cref="TransferPlanner"/>; a pump the ship is already alongside costs 0. Deterministic.
    /// </summary>
    public static Assessment Assess(
        Simulator simulator,
        ICelestialEphemeris ephemeris,
        ShipState ship,
        int remainingPulses,
        int tankCapacity,
        string wellBodyId,
        double maxDeltaV = 25_000)
    {
        int flatReserve = AutopilotRehearsal.ReservePulses(tankCapacity);

        CelestialBody? well = Find(ephemeris, wellBodyId);
        if (well is null)
        {
            return new Assessment(
                Verdict.CannotReachAPump, null, int.MaxValue, remainingPulses, 0, flatReserve, [],
                $"no well named '{wellBodyId}'");
        }

        // The pumps: children of this well that carry a depot (station or haven), same as GenerateDepots.
        var pumps = new List<CelestialBody>();
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId == wellBodyId && (body.Kind == BodyKind.Station || body.IsHaven))
            {
                pumps.Add(body);
            }
        }

        if (pumps.Count == 0)
        {
            return new Assessment(
                Verdict.CannotReachAPump, null, int.MaxValue, remainingPulses, 0, flatReserve, [],
                $"no fuel pump orbits {well.Name}");
        }

        // If the ship is already alongside a pump, refuel is a click, not a trip — Comfortable.
        foreach (CelestialBody pump in pumps)
        {
            if (AtPump(ephemeris, ship, pump))
            {
                return new Assessment(
                    Verdict.Comfortable, pump.Id, 0, remainingPulses, remainingPulses, flatReserve,
                    [new DepotReach(pump.Id, 0)],
                    $"alongside {pump.Name} — refuel is a click");
            }
        }

        // Parked inside a (non-pump) moon's Hill sphere? Price from that moon's doorstep — the honest
        // "ready to leave" state, since the planner refuses a departure from inside a moon's well.
        string reasonPrefix = string.Empty;
        ShipState departure = ship;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId != wellBodyId || body.Kind != BodyKind.Moon)
            {
                continue;
            }

            double hill = OrbitRule.HillRadius(body, well.Mu);
            if ((ship.Position - ephemeris.Position(body.Id, ship.SimTime)).Length < hill)
            {
                departure = Doorstep(ephemeris, body, well, ship.SimTime, hill);
                reasonPrefix = $"priced from {body.Name}'s doorstep (parked-at-moon departure); ";
                break;
            }
        }

        // Price the reach to every pump; keep the reachable ones, cheapest first.
        var reachable = new List<DepotReach>();
        foreach (CelestialBody pump in pumps)
        {
            var plan = TransferPlanner.Solve(
                simulator, ephemeris, new TransferPlanner.Request(departure, wellBodyId, pump.Id, MaxWaitSeconds: 0, MaxDeltaV: maxDeltaV));
            if (plan.Ok)
            {
                reachable.Add(new DepotReach(pump.Id, plan.EstimatedPulses));
            }
        }

        reachable.Sort((a, b) => a.Pulses != b.Pulses
            ? a.Pulses.CompareTo(b.Pulses)
            : string.CompareOrdinal(a.DepotBodyId, b.DepotBodyId));

        if (reachable.Count == 0)
        {
            return new Assessment(
                Verdict.CannotReachAPump, null, int.MaxValue, remainingPulses, 0, flatReserve, reachable,
                $"{reasonPrefix}no pump has a feasible transfer window — every route threads the planet, " +
                "arrives too fast to capture, or beats the Δv ceiling");
        }

        DepotReach nearest = reachable[0];
        int reach = nearest.Pulses;
        int margin = remainingPulses - reach;

        // Well-aware amber floor: the fare to the nearest pump PLUS the normal 18% cushion on top, so
        // the amber crossing tracks the crawl instead of a flat fraction the well outgrows (#146).
        int safeReserve = reach + flatReserve;

        Verdict verdict;
        string reason;
        if (remainingPulses < reach)
        {
            verdict = Verdict.CannotReachAPump;
            reason = $"{reasonPrefix}nearest pump {NameOf(ephemeris, nearest.DepotBodyId)} is {reach} p away, " +
                $"only {remainingPulses} in the tank — stranded by {reach - remainingPulses} p";
        }
        else if (remainingPulses < safeReserve)
        {
            verdict = Verdict.Thin;
            reason = $"{reasonPrefix}nearest pump {NameOf(ephemeris, nearest.DepotBodyId)} is {reach} p away; " +
                $"tank {remainingPulses} p is below the {safeReserve} p well-aware reserve — refuel now";
        }
        else
        {
            verdict = Verdict.Comfortable;
            reason = $"{reasonPrefix}nearest pump {NameOf(ephemeris, nearest.DepotBodyId)} is {reach} p away, " +
                $"{margin} p of slack over the {safeReserve} p reserve";
        }

        return new Assessment(
            verdict, nearest.DepotBodyId, reach, remainingPulses, margin, safeReserve, reachable, reason);
    }

    /// <summary>
    /// #157 · The refuel gate the Trade desk reads: is the ship genuinely alongside a fuel pump right now,
    /// so "⛽ FILL HER UP" is a berth-side click rather than a trip? Scans every depot-bearing body in the
    /// system (stations + havens — the same hosts <see cref="TrafficSchedule.GenerateDepots"/> spawns
    /// pumps by) and applies the SAME <see cref="AtPump"/> truth <see cref="Assess"/> uses for its
    /// zero-reach Comfortable verdict, so the button and the alarm can never disagree about "you are
    /// docked". Well-independent (it does not need to know which planet's well you are in) and cheap —
    /// pure distance / dock-envelope tests, no transfer solves. Returns the pump you are alongside (the
    /// first found), or null. Deterministic.
    /// </summary>
    public static CelestialBody? AlongsidePump(ICelestialEphemeris ephemeris, ShipState ship)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if ((body.Kind == BodyKind.Station || body.IsHaven) && AtPump(ephemeris, ship, body))
            {
                return body;
            }
        }

        return null;
    }

    /// <summary>True when the ship is genuinely parked at a pump — refuelling is a berth-side click,
    /// not a transfer: inside a haven moon's Hill sphere (orbiting it), or inside a station's dock
    /// envelope. Deliberately NOT the capture range (whose 3e9 m floor reaches millions of km — that
    /// is "the autopilot could take over from here", not "you are docked").</summary>
    private static bool AtPump(ICelestialEphemeris ephemeris, ShipState ship, CelestialBody pump)
    {
        if (pump.Kind == BodyKind.Station || pump.Mu <= 0)
        {
            // A station berth demands BOTH proximity AND a matched rail velocity (DockRule) — not mere
            // distance: Highport is a LEO station and Luna drifts within 500,000 km of it while riding a
            // wholly different velocity, so a distance-only test would falsely read "docked at Highport".
            Vector2d stationPos = ephemeris.Position(pump.Id, ship.SimTime);
            Vector2d stationVel = TransferMath.BodyVelocity(ephemeris, pump.Id, ship.SimTime);
            return DockRule.InEnvelope(ship, stationPos, stationVel, pump.BodyRadius);
        }

        double distance = (ship.Position - ephemeris.Position(pump.Id, ship.SimTime)).Length;
        CelestialBody? parent = pump.ParentId is null ? null : Find(ephemeris, pump.ParentId);
        double hill = parent is null ? 0 : OrbitRule.HillRadius(pump, parent.Mu);
        return hill > 0 && distance <= hill;
    }

    /// <summary>A moon's doorstep: outside its Hill sphere on the outward radial, riding its velocity —
    /// the free-flying, ready-to-leave state the planner accepts.</summary>
    private static ShipState Doorstep(ICelestialEphemeris ephemeris, CelestialBody moon, CelestialBody well, double simTime, double hill)
    {
        Vector2d moonPos = ephemeris.Position(moon.Id, simTime);
        Vector2d wellPos = ephemeris.Position(well.Id, simTime);
        Vector2d outward = (moonPos - wellPos).Normalized();
        Vector2d vel = TransferMath.BodyVelocity(ephemeris, moon.Id, simTime);
        return new ShipState(moonPos + outward * Math.Max(3e6, hill * 2.0), vel, simTime);
    }

    private static string NameOf(ICelestialEphemeris ephemeris, string id) => Find(ephemeris, id)?.Name ?? id;

    private static CelestialBody? Find(ICelestialEphemeris ephemeris, string id)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == id)
            {
                return body;
            }
        }

        return null;
    }
}
