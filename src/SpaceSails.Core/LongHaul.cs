using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #246 — 🚀 LONG HAUL: the autopilot MODE that crosses the void by COMPUTING it, not animating it.
/// The owner's live pain was the 172-day Mars→Uranus manual coast — "quite heavy and slow for the long
/// trip still". #172's warp-skip is for legs you WANT to watch; the long haul is for the void you don't.
///
/// <para>The deterministic core makes the jump sound. Every other thing in the world is a pure function
/// of sim time — rails (<see cref="ICelestialEphemeris.Position"/>), heat decay
/// (<see cref="EncounterRule.DecayHeat"/>), bank interest (<see cref="FavorBank.AccrueInterest"/>), pod
/// timetables (<see cref="MassDriverSchedule"/>), cache timers (<see cref="DiscoveryRule"/>) — so if we
/// (1) place the ship at the closed-form conic's arrival state (<see cref="TransferMath.PropagateKepler"/>
/// for the heliocentric coast) and (2) advance the sim clock to that arrival epoch, the whole world is
/// consistent BY CONSTRUCTION, not teleport-hacked. One frame of real time; the void is never integrated.</para>
///
/// <para>The bus model stands: the haul ends at the destination planet's <see cref="OrbitRule.CaptureRange"/>
/// handover — the premium last mile (insertion, dock, or a shuttle to a moon) is the existing machinery,
/// arm-or-manual. And the promise is legible before commit: "course reaches Uranus capture (2.34 AU) on
/// &lt;date&gt;", or for a coast that misses, "does NOT reach Uranus — closest pass X AU".</para>
///
/// <para>Guard rules (owner comment 2): the mode REFUSES while a hunter is actively pursuing — a hunter
/// mid-chase is NOT a pure function of time, so jumping past it would be a lie ("the long haul waits until
/// the sky is clear"). Active station-keeping is disarmed-with-confirm first (the existing flow). And the
/// ship must already be in open heliocentric cruise: the conic is only honest once clear of the planetary
/// well it departed.</para>
///
/// <para>Pure and deterministic throughout: fixed march step, fixed bisection budget, no wall clock, no
/// randomness — client WASM and any server replay agree to the bit.</para>
/// </summary>
public static class LongHaul
{
    /// <summary>A transfer whose arrival is beyond this (5 sim-days) is LONG — worth offering the haul
    /// rather than the watch-it warp-skip. Sits well above #172's one-day long-coast advert threshold
    /// (<see cref="WarpSkip.LongCoastThresholdSeconds"/>): a coast you'd skip is not automatically a void
    /// you'd jump.</summary>
    public const double LongThresholdSeconds = 5.0 * 86_400.0;

    /// <summary>One astronomical unit in metres (IAU 2012 exact) — the unit the arrival promise speaks
    /// the capture radius in ("capture (2.34 AU)"), the scale a captain reads outer-system distances at.</summary>
    public const double AstronomicalUnitMeters = 1.495978707e11;

    /// <summary>Coarse march step (s) for the void projection — 6 h. The heliocentric capture radius of
    /// even an inner planet (floor <see cref="OrbitRule.CaptureRangeFloorMeters"/> = 3e9 m) dwarfs the
    /// ship's per-step displacement, so no capture crossing is ever stepped over; the step is auto-tightened
    /// below for tiny capture zones anyway (see <see cref="Project"/>).</summary>
    public const double ProjectStepSeconds = 21_600.0;

    /// <summary>Hard search horizon: 400 sim-days covers a Neptune-scale haul with room to spare. A coast
    /// that has not reached capture within it is reported as un-reaching (the promise says "closest pass").</summary>
    public const double DefaultHorizonSeconds = 400.0 * 86_400.0;

    /// <summary>Bisection budget for pinning the capture-range crossing between two march samples — 48
    /// halvings takes a 6 h bracket to sub-millisecond, far finer than any downstream reads.</summary>
    private const int CrossingBisectionSteps = 48;

    /// <summary>Why the long haul is unavailable this instant, or <see cref="None"/> when it is armed to go.</summary>
    public enum Blocker
    {
        /// <summary>Clear to jump.</summary>
        None,

        /// <summary>A hunter is mid-chase — a pursuit is not a pure function of time, so the jump would lie.</summary>
        HunterActive,

        /// <summary>The autopilot is holding a kept orbit — disarm it (with the confirm) before jumping.</summary>
        Keeping,

        /// <summary>The ship is still inside a planet's Hill sphere — the heliocentric conic is only honest
        /// once clear of the well it is departing.</summary>
        InsideWell,

        /// <summary>The coast to the destination is shorter than <see cref="LongThresholdSeconds"/> — just
        /// watch it (warp-skip), the void mode is for the long dark.</summary>
        ShortHop,

        /// <summary>The plotted course never reaches the destination's capture range within the horizon.</summary>
        DoesNotReach,
    }

    /// <summary>The projected reach of the ship's current heliocentric coast toward a destination planet:
    /// whether (and where and when) the conic enters the planet's capture range, plus the closest approach
    /// for the honest "does NOT reach — closest pass X AU" verdict.</summary>
    /// <param name="Reaches">The conic enters the planet's <see cref="OrbitRule.CaptureRange"/> within the horizon.</param>
    /// <param name="ArrivalSimTime">Sim clock at the capture-range crossing (the haul's arrival epoch).</param>
    /// <param name="ArrivalState">The ship's closed-form state AT the capture-range handover — the frame the
    /// jump places the ship in. Only meaningful when <see cref="Reaches"/>.</param>
    /// <param name="CaptureRangeMeters">The destination planet's capture radius (the bus stop).</param>
    /// <param name="ClosestApproachMeters">The tightest ship↔planet separation on the coast — equals the
    /// capture radius on a reaching course; the true minimum on a missing one (the promise's "closest pass").</param>
    /// <param name="ClosestApproachSimTime">When that closest approach occurs.</param>
    public readonly record struct Reach(
        bool Reaches,
        double ArrivalSimTime,
        ShipState ArrivalState,
        double CaptureRangeMeters,
        double ClosestApproachMeters,
        double ClosestApproachSimTime)
    {
        /// <summary>Void-crossing duration from a given departure clock.</summary>
        public double ElapsedSecondsFrom(double fromSimTime) => ArrivalSimTime - fromSimTime;
    }

    /// <summary>The sun-orbiting planet a destination belongs to — the body the void mode actually hauls to
    /// and stops at the capture range of. A destination that already orbits the root (a planet, or a
    /// heliocentric derelict) is its own target; a moon or station is resolved up its parent chain to the
    /// planet that orbits the root. Null when no such ancestor exists (e.g. the root itself).</summary>
    public static CelestialBody? JumpTargetPlanet(ICelestialEphemeris ephemeris, string destinationId)
    {
        CelestialBody? body = Find(ephemeris, destinationId);
        while (body is { ParentId: not null })
        {
            CelestialBody? parent = Find(ephemeris, body.ParentId);
            if (parent is null)
            {
                return null;
            }

            if (parent.ParentId is null)
            {
                return body; // body orbits the root — it IS the sun-orbiting planet to haul to.
            }

            body = parent;
        }

        return null; // the destination is the root, or parentless — nothing to haul to.
    }

    /// <summary>
    /// Project the ship's current heliocentric coast forward on its closed-form conic and find the first
    /// instant it enters <paramref name="targetPlanet"/>'s capture range — the void mode's arrival gate.
    /// Marches <see cref="TransferMath.PropagateKepler"/> about the planet's parent (the sun) in coarse
    /// strides, then bisects the capture-range crossing to sub-millisecond. Pure: a function of the ship
    /// state, the rails, and the target — no clock, no randomness.
    /// </summary>
    /// <param name="ship">The ship's state at the departure instant (already coasting in open space).</param>
    /// <param name="ephemeris">The same rails the live sim flies.</param>
    /// <param name="targetPlanet">The sun-orbiting planet to haul to (see <see cref="JumpTargetPlanet"/>).</param>
    /// <param name="horizonSeconds">How far ahead to look for the capture crossing.</param>
    public static Reach Project(
        ShipState ship,
        ICelestialEphemeris ephemeris,
        CelestialBody targetPlanet,
        double horizonSeconds = DefaultHorizonSeconds)
    {
        CelestialBody? sun = targetPlanet.ParentId is null ? null : Find(ephemeris, targetPlanet.ParentId);
        double captureRange = sun is null
            ? OrbitRule.CaptureRangeFloorMeters
            : OrbitRule.CaptureRange(OrbitRule.HillRadius(targetPlanet, sun.Mu));

        if (sun is not { Mu: > 0 })
        {
            // No usable heliocentric attractor — cannot compute a conic. Report un-reaching honestly.
            double d0 = (ship.Position - ephemeris.Position(targetPlanet.Id, ship.SimTime)).Length;
            return new Reach(false, ship.SimTime, ship, captureRange, d0, ship.SimTime);
        }

        double t0 = ship.SimTime;
        double sunMu = sun.Mu;

        // The sun-relative running state. We march the conic INCREMENTALLY — each stride re-seeds
        // PropagateKepler from the previous state over a modest dt — so the universal-variable anomaly
        // never grows into the regime where the Stumpff cosh/sinh overflow (a single 170 km/s hyperbolic
        // arc propagated over 200 days in one shot does exactly that). Incremental stepping is still a
        // pure, deterministic function of the inputs: a fixed step schedule, no clock, no randomness.
        Vector2d relPos = ship.Position - ephemeris.Position(sun.Id, t0);
        Vector2d relVel = ship.Velocity - TransferMath.BodyVelocity(ephemeris, sun.Id, t0);

        // Advance the running sun-relative state by dt (re-seeding keeps every hop's anomaly small); fold
        // back into world coordinates through the sun's own rail (general even if the root ever drifts).
        ShipState Absolute(Vector2d rp, Vector2d rv, double t) =>
            new(ephemeris.Position(sun.Id, t) + rp, TransferMath.BodyVelocity(ephemeris, sun.Id, t) + rv, t, ship.Charge);

        double DistanceOf(ShipState s) => (s.Position - ephemeris.Position(targetPlanet.Id, s.SimTime)).Length;

        // Tighten the step so no capture zone is ever stepped clean over (never carry the ship more than a
        // quarter of the capture radius in one stride), but for a MULTI-YEAR heliocentric haul let the
        // stride grow so the march stays ~a few thousand iterations instead of tens of thousands — the wide
        // outer capture zones have the room. Short-horizon calls keep the #246 6 h cadence exactly.
        double shipSpeed = Math.Max(relVel.Length, 1.0);
        double capZoneCap = Math.Max(60.0, 0.25 * captureRange / shipSpeed);
        double step = Math.Min(capZoneCap, Math.Max(ProjectStepSeconds, horizonSeconds / 3000.0));

        ShipState prevAbs = Absolute(relPos, relVel, t0);
        double closest = DistanceOf(prevAbs);
        double closestT = t0;
        if (closest <= captureRange)
        {
            // Already inside the capture range — the haul is a no-op (the caller treats this as ShortHop/at-gate).
            return new Reach(true, t0, prevAbs, captureRange, closest, t0);
        }

        Vector2d prevRelPos = relPos, prevRelVel = relVel;
        for (double t = t0 + step; ; t += step)
        {
            if (t > t0 + horizonSeconds)
            {
                break;
            }

            if (TransferMath.PropagateKepler(prevRelPos, prevRelVel, step, sunMu) is not { } k
                || !double.IsFinite(k.Position.X) || !double.IsFinite(k.Position.Y))
            {
                break; // degenerate conic — stop honestly at the last good state (reported un-reaching)
            }

            ShipState here = Absolute(k.Position, k.Velocity, t);
            double dist = DistanceOf(here);
            if (dist < closest)
            {
                closest = dist;
                closestT = t;
            }

            if (dist <= captureRange)
            {
                // Crossing bracketed in the last stride: bisect a sub-dt in [0, step] from prev's state to
                // pin the entry (distance − captureRange goes + → ≤0), re-seeding each probe from prev.
                double lo = 0, hi = step;
                for (int i = 0; i < CrossingBisectionSteps; i++)
                {
                    double mid = 0.5 * (lo + hi);
                    TransferMath.KeplerState p = TransferMath.PropagateKepler(prevRelPos, prevRelVel, mid, sunMu)
                        ?? new TransferMath.KeplerState(prevRelPos, prevRelVel);
                    if (DistanceOf(Absolute(p.Position, p.Velocity, t - step + mid)) > captureRange)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                TransferMath.KeplerState entry = TransferMath.PropagateKepler(prevRelPos, prevRelVel, hi, sunMu)
                    ?? new TransferMath.KeplerState(prevRelPos, prevRelVel);
                ShipState arrival = Absolute(entry.Position, entry.Velocity, t - step + hi);
                double arrivalDist = DistanceOf(arrival);
                return new Reach(true, arrival.SimTime, arrival, captureRange, arrivalDist, arrival.SimTime);
            }

            prevRelPos = k.Position;
            prevRelVel = k.Velocity;
            prevAbs = here;
        }

        return new Reach(false, prevAbs.SimTime, prevAbs, captureRange, closest, closestT);
    }

    /// <summary>
    /// The one gate the whole mode keys off: is the long haul clear to jump the ship to
    /// <paramref name="reach"/>'s arrival? Ordered so the loudest, most actionable reason wins — a hunter
    /// in the sky, then a kept orbit to disarm, then still-in-the-well, then a hop too short to bother, then
    /// a course that plain misses. <see cref="Blocker.None"/> means engage.
    /// </summary>
    public static Blocker Evaluate(Reach reach, bool anyHunterActive, bool keepingOrbit, bool insideWell, double fromSimTime)
    {
        if (anyHunterActive)
        {
            return Blocker.HunterActive;
        }

        if (keepingOrbit)
        {
            return Blocker.Keeping;
        }

        if (insideWell)
        {
            return Blocker.InsideWell;
        }

        if (!reach.Reaches)
        {
            return Blocker.DoesNotReach;
        }

        return reach.ElapsedSecondsFrom(fromSimTime) < LongThresholdSeconds ? Blocker.ShortHop : Blocker.None;
    }

    /// <summary>Any hunter out there and closing — activated, still on the hunt (not broken off, not
    /// caught). A pursuit is not a pure function of sim time, so the void mode refuses while one is up
    /// (owner comment 2). Ships fitting out (before <see cref="HunterState.ActivationSimTime"/>) do not
    /// count — they aren't flying yet.</summary>
    public static bool AnyHunterActive(IEnumerable<HunterState> hunters, double simTime)
    {
        foreach (HunterState h in hunters)
        {
            if (!h.BrokenOff && !h.CaughtPlayer && simTime >= h.ActivationSimTime)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the ship still sits inside some planet's Hill sphere — the heliocentric conic is
    /// not yet the honest model of its motion, so the haul must wait until it is clear of the well. The
    /// destination's own target planet is exempt (arriving there is the whole point).</summary>
    public static bool InsideAnyWell(ShipState ship, ICelestialEphemeris ephemeris, string? exemptPlanetId = null)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Kind != BodyKind.Planet || body.ParentId is null || body.Id == exemptPlanetId)
            {
                continue;
            }

            CelestialBody? parent = Find(ephemeris, body.ParentId);
            if (parent is not { Mu: > 0 })
            {
                continue;
            }

            double hill = OrbitRule.HillRadius(body, parent.Mu);
            double distance = (ship.Position - ephemeris.Position(body.Id, ship.SimTime)).Length;
            if (distance < hill)
            {
                return true;
            }
        }

        return false;
    }

    // ===== The DEPARTURE solve — the offer's basis, so the mode is reachable from a berth (#246/#249 fix) =====
    // The #249 offer gated on Project of the CURRENT coast reaching the planet — which never happens from a
    // berth or an arbitrary coast, so the button was unreachable in exactly the situation it was built for.
    // The bus INCLUDES the departure burn: solve the cheap heliocentric arc from the ship's own state here
    // and now, quote its departure pulses, and the jump then rides the POST-BURN conic — which reaches by
    // construction. We do NOT reuse TransferPlanner: its arrival-matching cost and MaxRelativeSpeed gate are
    // for ORBITAL CAPTURE, and the long haul defers capture to the premium last mile (it only has to REACH
    // the capture range, arriving hot is fine). So this is a pure departure-only Lambert scan.

    /// <summary>Departure-time-of-flight cells scanned from here-and-now — coarse is plenty for the cheap
    /// row (the winner is refined only by picking the min over the scan).</summary>
    private const int DepartureTofCells = 28;

    private const double DepartureTofLowFraction = 0.25;
    private const double DepartureTofHighFraction = 1.6;

    /// <summary>A solved long-haul departure: the immediate burn that puts the ship on a heliocentric arc
    /// reaching the destination planet, priced honestly, with the arrival epoch and the last-mile relative
    /// speed the premium capture will have to kill. <see cref="Ok"/> false carries the verbatim reason.</summary>
    /// <param name="PostBurnVelocity">The ship's WORLD velocity after the departure burn (the Lambert
    /// departure velocity) — apply it to the current state and the coast rides the solved conic.</param>
    /// <param name="DepartureDeltaV">|burn| in m/s.</param>
    /// <param name="DeparturePulses">Priced with the same <see cref="OrbitRule.PulsesFor"/> kernel the live
    /// burns spend with — the number the offer quotes and the tank budget is checked against.</param>
    /// <param name="ArrivalCenterTime">Sim clock when the arc reaches the planet's centre; the jump stops a
    /// touch earlier, at the capture range (Project on the post-burn state finds the exact gate).</param>
    /// <param name="ArrivalRelativeSpeed">Speed relative to the planet at arrival — the premium last mile.</param>
    public readonly record struct Departure(
        bool Ok,
        string? Failure,
        Vector2d PostBurnVelocity,
        double DepartureDeltaV,
        int DeparturePulses,
        double ArrivalCenterTime,
        double ArrivalRelativeSpeed);

    /// <summary>
    /// Solve the cheap immediate departure that reaches <paramref name="targetPlanet"/> from the ship's
    /// current heliocentric state. Scans time-of-flight (departing now) around the Hohmann scale, Lambert-
    /// solving each and keeping the one with the least departure Δv (the cheap bus). Pure/deterministic.
    /// </summary>
    public static Departure SolveDeparture(ShipState ship, ICelestialEphemeris ephemeris, CelestialBody targetPlanet)
    {
        CelestialBody? sun = targetPlanet.ParentId is null ? null : Find(ephemeris, targetPlanet.ParentId);
        if (sun is not { Mu: > 0 })
        {
            return new Departure(false, $"{targetPlanet.Name} has no usable heliocentric frame to haul across", default, 0, 0, 0, 0);
        }

        double t0 = ship.SimTime;
        double sunMu = sun.Mu;
        Vector2d sunPos0 = ephemeris.Position(sun.Id, t0);
        Vector2d sunVel0 = TransferMath.BodyVelocity(ephemeris, sun.Id, t0);
        Vector2d r1 = ship.Position - sunPos0;
        Vector2d vShip = ship.Velocity - sunVel0;
        double r1Len = r1.Length;
        if (!(r1Len > 0))
        {
            return new Departure(false, "the ship has no heliocentric radius to depart from", default, 0, 0, 0, 0);
        }

        double shipWorldSpeed = ship.Velocity.Length;
        double hohmannTof = TransferMath.Hohmann(r1Len, targetPlanet.OrbitRadius, sunMu).TransferSeconds;

        bool found = false;
        double bestDv = double.PositiveInfinity;
        Vector2d bestV1 = default;
        double bestArrival = 0, bestArrivalRel = 0;

        for (int i = 0; i < DepartureTofCells; i++)
        {
            double frac = DepartureTofLowFraction
                + (DepartureTofHighFraction - DepartureTofLowFraction) * i / (DepartureTofCells - 1);
            double tof = hohmannTof * frac;
            if (!(tof > 0))
            {
                continue;
            }

            double tArrive = t0 + tof;
            Vector2d r2 = ephemeris.Position(targetPlanet.Id, tArrive) - ephemeris.Position(sun.Id, tArrive);
            if (TransferMath.Lambert(r1, r2, tof, sunMu) is not { } lam)
            {
                continue;
            }

            double dv = (lam.V1 - vShip).Length;
            if (dv < bestDv)
            {
                Vector2d planetVel = TransferMath.BodyVelocity(ephemeris, targetPlanet.Id, tArrive)
                                     - TransferMath.BodyVelocity(ephemeris, sun.Id, tArrive);
                bestDv = dv;
                bestV1 = lam.V1 + sunVel0;                     // fold the sun frame back into world velocity
                bestArrival = tArrive;
                bestArrivalRel = (lam.V2 - planetVel).Length;
                found = true;
            }
        }

        if (!found)
        {
            return new Departure(false, $"no departure arc to {targetPlanet.Name} from here — the geometry won't close", default, 0, 0, 0, 0);
        }

        int pulses = OrbitRule.PulsesFor(bestDv, shipWorldSpeed);
        return new Departure(true, null, bestV1, bestDv, pulses, bestArrival, bestArrivalRel);
    }

    // ===== The one voice for the long haul's words (HarborVocabulary-style; pure text, unit-tested) =====

    /// <summary>A metric distance spoken in AU, the outer-system unit ("2.34 AU").</summary>
    public static string FormatAu(double meters) =>
        (meters / AstronomicalUnitMeters).ToString("0.##", CultureInfo.InvariantCulture) + " AU";

    /// <summary>The banner's verbatim NOW row while the haul is (briefly) engaged (owner comment 1): the
    /// autopilot owns the leg, so the honest "YOU HAVE THE SHIP" of a manual coast must not stand.</summary>
    public static string BannerNow(string destName) =>
        $"🚀 AUTOPILOT HAS THE SHIP — NOW: long haul to {destName}";

    /// <summary>The arm-surface OFFER beside the normal options (#246 item 1): what the button says.</summary>
    public static string Offer(string destName, int pulsesNow, string arriveDateText) =>
        $"🚀 Long haul to {destName} — ≈{pulsesNow} p, arrive {arriveDateText}";

    /// <summary>The MAP CONTEXT-MENU action (owner refinement: the primary entry). One click from the map
    /// sets the destination AND engages — "autopilot to &lt;planet&gt; vicinity" (the owner's wording; it
    /// lands at the capture range, the last mile stays premium).</summary>
    public static string MenuAction(string planetName, int pulsesNow, string arriveDateText) =>
        $"🚀 Long haul — autopilot to {planetName} vicinity (≈{pulsesNow} p, arrive {arriveDateText})";

    /// <summary>The visible-but-disabled refusal when the solved departure outruns the tank (owner: never a
    /// hidden button — the affordance explains, #212). Speaks the number.</summary>
    public static string RefusalBudget(int neededPulses, int tankPulses) =>
        $"🚀 long haul needs ≈{neededPulses} p; tank has {tankPulses} — top up or find a cheaper window";

    /// <summary>The pre-commit PROMISE, stated plainly (#246 item 3 / owner "the UI does not say I will
    /// get to Uranus"): the destination verdict, the capture radius in AU, the arrival date, the cost now
    /// and the last-mile quote.</summary>
    public static string Promise(
        string planetName, double captureRangeMeters, string arriveDateText, int pulsesNow, int lastMilePulses) =>
        $"course reaches {planetName} capture ({FormatAu(captureRangeMeters)}) on {arriveDateText} — " +
        $"≈{pulsesNow} p now, ≈{lastMilePulses} p quoted for the last mile";

    /// <summary>The honest destination verdict line for a MANUAL coast's nav-target card (#246 item 3):
    /// reaches in N d, or misses with the closest pass named. <paramref name="fromSimTime"/> is the clock
    /// the ETA counts from.</summary>
    public static string ReachVerdict(string planetName, Reach reach, double fromSimTime)
    {
        if (reach.Reaches)
        {
            int days = (int)Math.Round(reach.ElapsedSecondsFrom(fromSimTime) / 86_400.0);
            return $"this course reaches {planetName} capture in {days} d";
        }

        return $"this coast does NOT reach {planetName} — closest pass {FormatAu(reach.ClosestApproachMeters)}";
    }

    /// <summary>The arrival announcement (#246 item 1e): the void is behind you.</summary>
    public static string Completed(string destName, int daysPassed) =>
        $"🚀 long haul complete — {daysPassed} d passed; arrived at {destName} capture range";

    /// <summary>The passbook / ledger line the jump books.</summary>
    public static string LedgerLine(string destName, int daysPassed, int pulsesSpent) =>
        $"🚀 long haul to {destName}: {daysPassed} d crossed, {pulsesSpent} p";

    /// <summary>The verbatim refusal — the reason, spoken (owner: "refusal with the reason").</summary>
    public static string RefusalText(Blocker blocker, string destName) => blocker switch
    {
        Blocker.HunterActive => "🚀 the long haul waits until the sky is clear — a hunter is still on us",
        Blocker.Keeping => $"🚀 disarm the kept orbit first, then the long haul to {destName} is yours",
        Blocker.InsideWell => $"🚀 leave the well before the long haul to {destName} — the void starts in open space",
        Blocker.ShortHop => $"🚀 {destName} is close enough to just watch — skip the coast, don't jump the void",
        Blocker.DoesNotReach => $"🚀 this course does not reach {destName} — trim it green before the long haul",
        _ => string.Empty,
    };

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
