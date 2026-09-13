using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #246/#251 · THE VOID, COMPUTED RATHER THAN FLOWN. <c>Project</c> marches the ship's own
/// heliocentric conic forward at a fixed step, bisects the capture-range crossing, and reports the
/// <see cref="LongHaul.Reach"/>; <c>PropagateHeliocentricTo</c> is the placement that makes the jump
/// sound; <c>SampleHeliocentricPath</c> is the same conic handed to the map.
///
/// <para>Pure and deterministic: fixed march step, fixed bisection budget, no wall clock, no
/// randomness. Split out of <c>LongHaul.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered, and no field of any kind moved — the family holds only <c>const</c>s, and they stayed
/// where the one file declared them.</para>
/// </summary>
public static partial class LongHaul
{
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
    /// #261 — advance the ship's heliocentric coast along its closed-form conic to a target epoch WITHOUT
    /// integrating. The jump-scale warp-skip reuses this so a multi-year ballistic coast becomes a clock
    /// advance, not the ~62M tick steps that froze the tab. Marches <see cref="TransferMath.PropagateKepler"/>
    /// about the sun in fixed strides, re-seeding each stride so the universal anomaly never grows into the
    /// Stumpff cosh/sinh overflow regime (the same incremental reasoning as <see cref="Project"/>). Pure and
    /// deterministic: a fixed stride schedule, no clock, no randomness — client and any replay agree.
    ///
    /// <para>HONEST ONLY IN OPEN HELIOCENTRIC CRUISE. The live integrator (<see cref="Simulator"/>) is
    /// n-body; the sun-relative conic is the true model of the ship's motion only clear of every planet's
    /// well. The caller MUST gate on <see cref="InsideAnyWell"/> — deep in a well the coast is a conic
    /// around THAT body, and this heliocentric advance would lie (correctness beats elegance: fall back to
    /// chunked integration there). Conserves the sun-relative conic invariants by construction.</para>
    /// </summary>
    /// <param name="ship">The ship's state at the departure instant (already coasting in open space).</param>
    /// <param name="ephemeris">The same rails the live sim flies.</param>
    /// <param name="sun">The root heliocentric attractor the coast is a conic about.</param>
    /// <param name="targetEpoch">The sim clock to advance the coast to. At/​before now is a no-op clock set.</param>
    public static ShipState PropagateHeliocentricTo(
        ShipState ship, ICelestialEphemeris ephemeris, CelestialBody sun, double targetEpoch)
    {
        double t0 = ship.SimTime;
        if (sun is not { Mu: > 0 } || targetEpoch <= t0)
        {
            return ship with { SimTime = Math.Max(t0, targetEpoch) };
        }

        double sunMu = sun.Mu;
        Vector2d relPos = ship.Position - ephemeris.Position(sun.Id, t0);
        Vector2d relVel = ship.Velocity - TransferMath.BodyVelocity(ephemeris, sun.Id, t0);

        // Fixed stride: the #246 6 h cadence, but let a multi-year haul stretch the stride so the march
        // stays a few thousand closed-form hops (cheap) instead of tens of thousands. Deterministic — the
        // schedule is a pure function of the span, no clock.
        double span = targetEpoch - t0;
        double step = Math.Max(ProjectStepSeconds, span / 3000.0);

        double t = t0;
        while (t < targetEpoch)
        {
            double dt = Math.Min(step, targetEpoch - t);
            if (TransferMath.PropagateKepler(relPos, relVel, dt, sunMu) is not { } k
                || !double.IsFinite(k.Position.X) || !double.IsFinite(k.Position.Y))
            {
                break; // degenerate conic — stop at the last good state; fold at the reached clock below.
            }

            relPos = k.Position;
            relVel = k.Velocity;
            t += dt;
        }

        // Fold the sun-relative state back into world coordinates through the sun's own rail at the reached
        // clock, so position and SimTime stay consistent even if a degenerate stride broke the march early.
        Vector2d worldPos = ephemeris.Position(sun.Id, t) + relPos;
        Vector2d worldVel = TransferMath.BodyVelocity(ephemeris, sun.Id, t) + relVel;
        return new ShipState(worldPos, worldVel, t, ship.Charge);
    }

    /// <summary>
    /// #267 — sample the ship's heliocentric coast as a drawn path of world-space samples, for the
    /// surface-clearance gate (<see cref="SurfaceClearance.Check"/>). Marches the SAME closed-form conic
    /// <see cref="Project"/> and <see cref="PropagateHeliocentricTo"/> fly — re-seeding each stride so the
    /// universal anomaly never grows into the Stumpff overflow regime — and records one world-space sample
    /// per stride up to <paramref name="untilEpoch"/>. This is NOT a second integrator: it is the one
    /// heliocentric kernel, sampled, so the clearance gate judges the very arc the jump will ride. Pure and
    /// deterministic (a fixed stride schedule, no clock, no randomness).
    /// </summary>
    /// <param name="ship">The (post-departure-burn) ship state whose conic is being verified.</param>
    /// <param name="ephemeris">The same rails the sim flies.</param>
    /// <param name="sun">The root heliocentric attractor the coast is a conic about.</param>
    /// <param name="untilEpoch">Stop sampling at this sim clock (the arrival epoch).</param>
    /// <param name="maxSamples">Sample cap — the stride widens to fit a multi-year haul under it.</param>
    public static IReadOnlyList<TrajectorySample> SampleHeliocentricPath(
        ShipState ship, ICelestialEphemeris ephemeris, CelestialBody sun, double untilEpoch, int maxSamples = 512)
    {
        var path = new List<TrajectorySample> { new(ship.SimTime, ship.Position) };
        double t0 = ship.SimTime;
        if (sun is not { Mu: > 0 } || untilEpoch <= t0)
        {
            return path;
        }

        double sunMu = sun.Mu;
        Vector2d relPos = ship.Position - ephemeris.Position(sun.Id, t0);
        Vector2d relVel = ship.Velocity - TransferMath.BodyVelocity(ephemeris, sun.Id, t0);

        // Fixed stride, the #246 6 h cadence, widened so a multi-year haul stays under the sample cap.
        double span = untilEpoch - t0;
        double step = Math.Max(ProjectStepSeconds, span / Math.Max(1, maxSamples - 1));

        double t = t0;
        while (t < untilEpoch && path.Count < maxSamples)
        {
            double dt = Math.Min(step, untilEpoch - t);
            if (TransferMath.PropagateKepler(relPos, relVel, dt, sunMu) is not { } k
                || !double.IsFinite(k.Position.X) || !double.IsFinite(k.Position.Y))
            {
                break; // degenerate conic — stop at the last good state
            }

            relPos = k.Position;
            relVel = k.Velocity;
            t += dt;
            path.Add(new TrajectorySample(t, ephemeris.Position(sun.Id, t) + relPos));
        }

        return path;
    }
}
