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
///
/// <para>#251 · THIS FILE IS THE MODE'S OWN VOCABULARY OF STATE — the thresholds, the
/// <see cref="Blocker"/> enum, the <see cref="Reach"/> a projection returns, the three GATES that
/// decide whether a jump may be offered at all, and the ephemeris lookup every part of the family
/// uses. The rest is four siblings: <c>.Project</c> (the closed-form coast and the conic it is
/// sampled from), <c>.Departure</c> (the Lambert scan that makes the mode reachable from a berth),
/// <c>.Insertion</c> (#262's quoted arrival brake and the round bill), and <c>.Words</c> (every
/// sentence the mode speaks, including #255's void overlay).</para>
///
/// <para><b>The cut is where the initializers allow, which here is everywhere:</b> this class holds
/// no <c>static readonly</c> field at all — every number in it is a <c>const</c>, folded into its
/// uses at compile time, so no file order can initialise one late. That is the hazard #1163 paid for
/// and <c>NoPartialClassSpreadsItsStaticFieldsTests</c> now watches; it is absent here by
/// construction rather than by luck. No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public static partial class LongHaul
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
