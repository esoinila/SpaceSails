using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #246/#249 · THE DEPARTURE SOLVE — the offer's basis, and the reason the mode is reachable from a
/// berth at all. A pure departure-only Lambert scan over coarse time-of-flight cells: the bus
/// INCLUDES the departure burn, so the jump rides the POST-BURN conic, which reaches by construction.
///
/// <para>The three <c>const</c>s that size the scan travel with it — they are compile-time folded, so
/// no file order can initialise one late, which is the whole of the #1163 hazard. Split out of
/// <c>LongHaul.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class LongHaul
{
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
    /// <param name="ArrivalRelativeSpeed">Speed relative to the planet at arrival — the speed the arrival
    /// insertion brake (#262) must shed to reach the clamp/capture window.</param>
    /// <param name="ArrivalSpeed">The ship's WORLD (heliocentric) speed at arrival — the "current speed" the
    /// insertion brake is priced against (<see cref="OrbitRule.PulsesFor"/>), the same kernel the live
    /// approach/insert burns spend with. Zero on a failed solve.</param>
    public readonly record struct Departure(
        bool Ok,
        string? Failure,
        Vector2d PostBurnVelocity,
        double DepartureDeltaV,
        int DeparturePulses,
        double ArrivalCenterTime,
        double ArrivalRelativeSpeed,
        double ArrivalSpeed);

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
            return new Departure(false, $"{targetPlanet.Name} has no usable heliocentric frame to haul across", default, 0, 0, 0, 0, 0);
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
            return new Departure(false, "the ship has no heliocentric radius to depart from", default, 0, 0, 0, 0, 0);
        }

        double shipWorldSpeed = ship.Velocity.Length;
        double hohmannTof = TransferMath.Hohmann(r1Len, targetPlanet.OrbitRadius, sunMu).TransferSeconds;

        bool found = false;
        double bestDv = double.PositiveInfinity;
        Vector2d bestV1 = default;
        double bestArrival = 0, bestArrivalRel = 0, bestArrivalSpeed = 0;

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
                Vector2d sunVelArrive = TransferMath.BodyVelocity(ephemeris, sun.Id, tArrive);
                Vector2d planetVel = TransferMath.BodyVelocity(ephemeris, targetPlanet.Id, tArrive) - sunVelArrive;
                bestDv = dv;
                bestV1 = lam.V1 + sunVel0;                     // fold the sun frame back into world velocity
                bestArrival = tArrive;
                bestArrivalRel = (lam.V2 - planetVel).Length;
                bestArrivalSpeed = (lam.V2 + sunVelArrive).Length; // world (heliocentric) speed at arrival — the insertion's pricing basis
                found = true;
            }
        }

        if (!found)
        {
            return new Departure(false, $"no departure arc to {targetPlanet.Name} from here — the geometry won't close", default, 0, 0, 0, 0, 0);
        }

        int pulses = OrbitRule.PulsesFor(bestDv, shipWorldSpeed);
        return new Departure(true, null, bestV1, bestDv, pulses, bestArrival, bestArrivalRel, bestArrivalSpeed);
    }
}
