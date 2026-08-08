using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #733 — THE SHIP YOU LEFT PARKED IS STILL IN THE SOLAR SYSTEM WHILE YOU ARE DOWN THE LIFT.
///
/// <para>Found by the 2026-08-06 smoke run: <c>/map?kaamos=hq&amp;land=1&amp;floor=23</c> killed the
/// captain within seconds of the ground card, two boots out of two, with a death line quoting a
/// periapsis "under the surface". The floor arrival was perfect; it was the MOTHERSHIP, alone in the
/// background sim, that flew into Enceladus while the captain stood 23 storeys under the ice.</para>
///
/// <para><b>Why the diff the issue asked for is the whole answer.</b> <c>?secretlab=deep&amp;land=1</c>
/// loiters safely for half an hour because that cheat leaves the ship CLAMPED at a berth, and a clamped
/// ship's position is owned by the berth. <c>?kaamos=hq</c> undocks her and lets her go co-moving beside
/// the ice — and then the descent's clock cost re-stamped her at the new epoch without moving her. See
/// <see cref="LoiterClock"/> for the argument; the arithmetic is that the crossing costs
/// <c>gap ÷ 8 km/s</c> while the ice moon's own absolute speed is ~7.6 km/s, so the moon covers very
/// nearly the standoff in very nearly the time the freeze lasts. The miss distance scales WITH the gap,
/// which is why <see cref="TheFrozenHull_IsTheOneTheMoonRunsDown"/> fails at BOTH standoffs the game uses
/// and why parking further out was never going to be the cure.</para>
///
/// <para>The suspicion in the issue — "a MOON constant governing a SHIP" — was the right shape and the
/// wrong constant. Nothing about the facility's depth leaked anywhere. What reached the ship's parking
/// solution was the moon's own MOTION, admitted into her state by a clock that moved while she did
/// not.</para>
///
/// <para><b>#742 closed the other half.</b> These tests were written against
/// <see cref="BerthState.CoMoving"/> because that was the construction the arc parked you with. It no
/// longer is: the free parks now build through <see cref="BerthState.CoOrbital"/>, which lays the standoff
/// along the moon's own track instead of the Sun's radius, so everything here that claims to fly "the real
/// construction path" now flies that one. The two tests that deliberately reproduce the OLD, broken states
/// — the frozen hull and the diving coast — stay on <c>CoMoving</c> on purpose and say so: they are #733's
/// kept evidence, and evidence you have quietly re-based is not evidence.</para>
/// </summary>
public class TheParkedShipIsNotRunDownByTheMoonTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    /// <summary>Every standoff this game actually parks a free-flying ship at beside a moon, named. Both
    /// are quoted from the constants the live code quotes, so retuning either one re-runs this sweep for
    /// free — and neither may be special-cased, because the failure mode scales with the gap.</summary>
    public static TheoryData<string, double> MoonParks => new()
    {
        { "the ?start=enceladus bench start (#136)", 5.0e6 },
        { "the cycler arrival · ?kaamos=hq (#411)", CyclerWindow.ArrivalOffsetMeters },
    };

    /// <summary>How long the parked hull must keep its track after the captain has gone down. Three sim
    /// days is more than two full Enceladus orbits (P = 32.9 h) — long past the beat the smoke run died
    /// on (t ≈ 21 min) and long enough that a slow resonant close would show.</summary>
    private const double WatchSeconds = 3.0 * 86_400.0;

    /// <summary>How long a captain can plausibly be OFF the ship — the window the parked hull must hold
    /// at every arrival phase, not just the lucky ones. Two hours is generous against the thing that
    /// actually bounds an excursion (the tank: the B23 card quotes 20 min 16 s) and against the walk back
    /// up the lift shaft on top of it. It is deliberately not the horizon of the whole voyage: an unkept
    /// orbit is SUPPOSED to erode eventually, announced (#327), and the game charges to re-park.</summary>
    private const double ExcursionSeconds = 2.0 * 3600.0;

    /// <summary>
    /// Fly a hull and watch what she meets — with the RAW integrator and the RAW crossing test, the pair
    /// <c>OnTick</c> itself runs, and deliberately NOT through <see cref="LoiterClock"/>.
    ///
    /// <para>That separation is the point and it was earned the hard way: the first draft of this guard
    /// flew the watch through <c>LoiterClock</c> too, so reverting the fix froze the observer along with
    /// the ship and the test went happily GREEN on the broken behaviour. A guard whose world cannot move
    /// cannot tell pass from fail. The clock cost under test is the only thing that goes through the new
    /// law; everything that judges it is independent of it.</para>
    /// </summary>
    private static (SurfaceImpact.Crossing? Struck, double Closest, double ClosestAt) FlyWatching(
        Simulator simulator, ICelestialEphemeris ephemeris, ShipState ship, double seconds, string watchBodyId)
    {
        double end = ship.SimTime + seconds;
        double closest = double.MaxValue;
        double closestAt = ship.SimTime;

        while (ship.SimTime < end)
        {
            Vector2d from = ship.Position;
            double fromTime = ship.SimTime;
            ship = simulator.RunAdaptive(ship, Math.Min(LoiterClock.QuantumSeconds, end - ship.SimTime));

            if (SurfaceImpact.FirstCrossing(from, fromTime, ship.Position, ship.SimTime, ephemeris) is { } hit)
            {
                return (hit, 0.0, hit.SimTime); // a strike IS zero clearance; the caller reports the hit
            }

            double range = (ship.Position - ephemeris.Position(watchBodyId, ship.SimTime)).Length;
            if (range < closest)
            {
                closest = range;
                closestAt = ship.SimTime;
            }
        }

        return (null, closest, closestAt);
    }

    // ── The guard ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE LAW: pay the shuttle's clock cost off a real parked ship, then fly her — and nothing she owns
    /// may reach a surface. Built through the real construction path at every step: the ship is
    /// <see cref="BerthState.CoMoving"/> at the ice moon exactly as <c>SeedKaamosHeadOfficeCheat</c> and
    /// <c>RideTheCyclerWindow</c> build her, the crossing is <see cref="ShuttleRange.TravelSeconds"/> of
    /// the real gap exactly as <c>ShuttleExcursion</c> prices it, and the cost is paid through
    /// <see cref="LoiterClock"/> — the one law <c>AdvanceShuttleClock</c> now calls. No orbit is written
    /// out by hand here, because a hand-built orbit is a world that cannot tell pass from fail.
    /// </summary>
    [Theory]
    [MemberData(nameof(MoonParks))]
    public void TheShuttlesClockCost_LeavesTheMothershipOnATrackThatClearsTheIce(string what, double standoff)
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        string moon = KaamosLore.IceMoonBodyId;
        CelestialBody ice = eph.Bodies.First(b => b.Id == moon);

        ShipState parked = BerthState.CoOrbital(eph, moon, 0, standoff); // the live free park (#742)
        double gap = (parked.Position - eph.Position(moon, 0)).Length;
        double crossing = ShuttleRange.TravelSeconds(gap);

        // 1 · The descent's clock cost — THE STEP UNDER TEST, and the step that killed her: the clock
        //     jumped by the crossing and the hull did not move, so the moon arrived where she was standing.
        LoiterClock.Coast down = LoiterClock.Advance(simulator, eph, parked, crossing);
        Assert.True(down.Struck is null,
            $"{what}: the {crossing:F0} s shuttle crossing itself ended on " +
            $"{down.Struck?.BodyName} — the hull was run down before the boots were on the ground.");

        // 2 · …and then the whole excursion, flown by the raw integrator that judges it (see FlyWatching).
        (SurfaceImpact.Crossing? struck, double closest, double closestAt) =
            FlyWatching(simulator, eph, down.Ship, WatchSeconds, moon);

        Assert.True(struck is null,
            $"{what}: the parked hull met {struck?.BodyName} at t = {struck?.SimTime / 60:F1} min — " +
            $"{(struck?.SimTime ?? 0) - crossing:F0} s after the boots landed. The captain is 23 floors " +
            "down; nothing up there is flying.");

        // The issue's own words: assert the hull's track CLEARS the surface, not merely that it missed.
        Assert.True(closest > ice.BodyRadius,
            $"{what}: closest approach {closest:e3} m at t = {closestAt / 3600:F2} h is inside " +
            $"{ice.Name}'s {ice.BodyRadius:e3} m surface radius.");
    }

    // ── Checkpoint 2 of the issue: and what about the captain who EARNED the door? ───────────────────

    /// <summary>
    /// THE LEGITIMATE ARRIVAL, ANSWERED RATHER THAN ASSUMED. The issue's second checkpoint: if the cheat
    /// parks on a bad periapsis, is a player who flies the five shards, the capstone and the forty-day
    /// wait presumably fine? The tempting answer is yes, because the cheat is "just" a shortcut — and it
    /// is wrong. <c>SeedKaamosHeadOfficeCheat</c> and <c>RideTheCyclerWindow</c> build the parked ship
    /// with the SAME <see cref="BerthState.CoMoving"/> call at the same standoff, and the honest route
    /// then walks the captain to the same shuttle bay and pays the same crossing. The cheat was never
    /// parking her badly; the crossing was running her down, and it would have run down the earned
    /// arrival exactly as hard.
    ///
    /// <para>So the sweep asks it where the honest route actually lands: at an ARBITRARY epoch. The ride
    /// arrives at <c>SimTime + CrossingSeconds</c> off a window that opens every 40 days and stands open
    /// for two, so the arrival phase is effectively free — and the standoff is laid along the SUN-outward
    /// direction while the moon's own phase turns every 32.9 h, which is precisely the geometry that
    /// decided whether the freeze killed you. One epoch proving safe would prove nothing about the next
    /// one. Every phase of the moon's orbit is swept, at three points around Saturn's own year.</para>
    /// </summary>
    [Fact]
    public void TheEarnedArrival_ParksClearOfTheIceAtEveryEpochTheWindowCanLandOn()
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        string moon = KaamosLore.IceMoonBodyId;
        CelestialBody ice = eph.Bodies.First(b => b.Id == moon);

        double worstClearance = double.MaxValue;
        double worstAt = 0;
        int swept = 0;

        foreach (double epoch in ArrivalEpochs())
        {
            ShipState parked = BerthState.CoOrbital(eph, moon, epoch, CyclerWindow.ArrivalOffsetMeters);
            double crossing = ShuttleRange.TravelSeconds(
                (parked.Position - eph.Position(moon, epoch)).Length);

            LoiterClock.Coast down = LoiterClock.Advance(simulator, eph, parked, crossing);
            Assert.True(down.Struck is null,
                $"epoch {epoch:F0} s: the shuttle crossing itself ended on {down.Struck?.BodyName}.");

            (SurfaceImpact.Crossing? struck, double closest, double closestAt) =
                FlyWatching(simulator, eph, down.Ship, ExcursionSeconds, moon);

            Assert.True(struck is null,
                $"epoch {epoch:F0} s: an EARNED arrival met {struck?.BodyName} " +
                $"{((struck?.SimTime ?? 0) - epoch) / 60:F1} min after the ride in.");
            Assert.True(closest > ice.BodyRadius,
                $"epoch {epoch:F0} s: closest approach {closest:e3} m at t = {closestAt:F0} s is inside " +
                $"{ice.Name}'s {ice.BodyRadius:e3} m surface radius.");

            if (closest < worstClearance)
            {
                worstClearance = closest;
                worstAt = epoch;
            }
            swept++;
        }

        Assert.Equal(72, swept);
        Assert.True(worstClearance > ice.BodyRadius,
            $"the tightest earned arrival clears {ice.Name} by {worstClearance:e3} m (epoch {worstAt:F0} s).");
    }

    /// <summary>
    /// THE PART #733 COULD NOT REACH, AND #742 CLOSED — kept here, at this file's own 72-epoch resolution,
    /// as the before-and-after of one number.
    ///
    /// <para>When this guard was written the park was <see cref="BerthState.CoMoving"/>: the standoff laid
    /// along the SUN-outward direction with the MOON's velocity handed over, which in Saturn's frame is a
    /// different ellipse whose shape depends entirely on the arrival phase. Of the 24 phases of one
    /// Enceladus orbit, phase 2/24 closed on the moon and struck it at <b>+9.54 h</b>, and 3/24 and 14/24
    /// passed inside 3.157e5 and 5.816e5 m — one and two surface radii. All this guard could hold then was
    /// that nothing struck inside the two hours a captain can plausibly be off the ship, and it said so.</para>
    ///
    /// <para>#742 laid the standoff along the moon's own track instead (<see cref="BerthState.CoOrbital"/>),
    /// so the bar moved from "not inside an excursion" to <b>nothing strikes at all, at any phase, for five
    /// sim days</b> — and the assertion moved with it. A guard left asserting the old, weaker line would
    /// have passed for free on the new construction and told nobody anything, which is this repo's fifth
    /// named bug class. The tightest clearance is asserted too, so "no strike" can never be satisfied by a
    /// hull that merely grazed.</para>
    /// </summary>
    [Fact]
    public void TheCoOrbitalPark_NowHoldsAtEveryArrivalPhase_NotMerelyForAnExcursion()
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        string moon = KaamosLore.IceMoonBodyId;

        double earliest = double.PositiveInfinity;
        double earliestAt = 0;
        double tightest = double.MaxValue;
        double tightestAt = 0;
        int swept = 0;

        foreach (double epoch in ArrivalEpochs())
        {
            ShipState parked = BerthState.CoOrbital(eph, moon, epoch, CyclerWindow.ArrivalOffsetMeters);
            double crossing = ShuttleRange.TravelSeconds(
                (parked.Position - eph.Position(moon, epoch)).Length);
            ShipState afloat = LoiterClock.Advance(simulator, eph, parked, crossing).Ship;

            (SurfaceImpact.Crossing? struck, double closest, _) =
                FlyWatching(simulator, eph, afloat, 5.0 * 86_400.0, moon);

            if (struck is { } hit && hit.SimTime - epoch < earliest)
            {
                earliest = hit.SimTime - epoch;
                earliestAt = epoch;
            }
            else if (struck is null && closest < tightest)
            {
                tightest = closest;
                tightestAt = epoch;
            }

            swept++;
        }

        Assert.Equal(72, swept);

        Assert.True(double.IsPositiveInfinity(earliest),
            $"the parked hull strikes {moon} {earliest / 3600:F2} h after an arrival at epoch " +
            $"{earliestAt:F0} s. Since #742 the answer at every one of these {swept} epochs is NEVER, over " +
            "five sim days — the last measured strike was phase 2/24 at +9.54 h, on the old Sun-outward park.");

        // …and it did not merely miss: the tightest of the 72 keeps most of the 1e7 m it was let go with.
        Assert.True(tightest > 0.5 * CyclerWindow.ArrivalOffsetMeters,
            $"the tightest of {swept} arrivals closes to {tightest:e3} m (epoch {tightestAt:F0} s) — under " +
            $"half the {CyclerWindow.ArrivalOffsetMeters:e2} m standoff it was let go at.");
    }

    /// <summary>Every arrival phase the honest route can land on: the window's 40-day grid makes the
    /// moon's phase free, so all 24 of one Enceladus orbit are swept, at three points around Saturn's own
    /// year. Saturn's own phase mattered enormously to the OLD park (the standoff was laid along the
    /// SUN-outward direction, so where Saturn stood in its orbit turned the offset relative to the moon's
    /// track) and should now matter to nothing at all — which is exactly why all three are still flown.
    /// A sweep narrowed to the axis the fix removed is a sweep that can no longer catch the fix coming
    /// undone.</summary>
    private static IEnumerable<double> ArrivalEpochs()
    {
        const double MoonPeriod = 118_386.8;      // Enceladus, from the scenario
        const double SaturnYear = 929_596_000.0;  // …and its parent's

        foreach (double saturnPhase in new[] { 0.0, SaturnYear / 3.0, 2.0 * SaturnYear / 3.0 })
        {
            for (int step = 0; step < 24; step++)
            {
                yield return saturnPhase + step * (MoonPeriod / 24.0);
            }
        }
    }

    // ── The evidence, kept ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WHAT ACTUALLY KILLED HER, pinned so nobody re-derives the freeze from first principles and calls
    /// it obvious. Re-stamping the parked hull at the new epoch with her position and velocity untouched
    /// — the old <c>AdvanceShuttleClock</c> line, reproduced here verbatim — puts her inside Enceladus
    /// within the hour, at BOTH standoffs. It is not a near miss and it is not luck: the freeze lasts
    /// <c>gap ÷ 8 km/s</c> and the moon travels ~7.6 km/s, so the moon covers the gap in almost exactly
    /// the time the hull stands still.
    /// </summary>
    [Theory]
    [MemberData(nameof(MoonParks))]
    public void TheFrozenHull_IsTheOneTheMoonRunsDown(string what, double standoff)
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        string moon = KaamosLore.IceMoonBodyId;

        // #733's EVIDENCE, so it is built the way #733 found it: CoMoving, the Sun-outward park that was
        // live that day. Deliberately not re-based onto #742's CoOrbital — evidence you have quietly
        // rebuilt is not evidence, and what is under test here is the FREEZE, not the placement.
        ShipState parked = BerthState.CoMoving(eph, moon, 0, standoff);
        double gap = (parked.Position - eph.Position(moon, 0)).Length;
        double crossing = ShuttleRange.TravelSeconds(gap);

        // The old idiom, exactly: "frozen in place — the shuttle moved, not the mothership".
        var frozen = new ShipState(parked.Position, parked.Velocity, parked.SimTime + crossing, parked.Charge);

        (SurfaceImpact.Crossing? struck, _, _) = FlyWatching(simulator, eph, frozen, 3600.0, moon);

        Assert.True(struck is not null,
            $"{what}: the freeze no longer kills — if the world has moved under this, re-derive #733 " +
            "before deleting the fix it justifies.");
        Assert.Equal(moon, struck!.Value.BodyId);
        Assert.True(struck.Value.SimTime < crossing + 600.0,
            $"{what}: the freeze struck at t = {struck.Value.SimTime:F0} s — #733 was 'within seconds' of " +
            $"the ground card (the crossing ends at {crossing:F0} s).");
    }

    // ── The safety valve ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A coast that really does reach a surface must HAND BACK the crossing rather than sail
    /// through it. The frozen state above is a genuinely diving track, so it doubles as the fixture for
    /// this: <see cref="LoiterClock"/> is asked to swallow the whole hour in one call and must still
    /// report the strike — the per-quantum look is what stops a small moon slipping between two coarse
    /// endpoints (#264's tunnelling, paid for once already).</summary>
    [Fact]
    public void ALoiterThatReachesASurface_HandsBackTheCrossingInsteadOfTunnelling()
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        string moon = KaamosLore.IceMoonBodyId;

        ShipState parked = BerthState.CoMoving(eph, moon, 0, CyclerWindow.ArrivalOffsetMeters);
        double crossing = ShuttleRange.TravelSeconds(
            (parked.Position - eph.Position(moon, 0)).Length);
        var frozen = new ShipState(parked.Position, parked.Velocity, parked.SimTime + crossing, parked.Charge);

        LoiterClock.Coast coast = LoiterClock.Advance(simulator, eph, frozen, 3600.0);

        Assert.NotNull(coast.Struck);
        Assert.Equal(moon, coast.Struck!.Value.BodyId);

        // …and it stops AT the strike: the returned state is the end of the quantum that hit, not an hour
        // of flying done on the far side of a moon the ship never came out of.
        Assert.True(coast.Ship.SimTime <= coast.Struck.Value.SimTime + LoiterClock.QuantumSeconds,
            $"the coast ran on to t = {coast.Ship.SimTime:F0} s past a strike at " +
            $"{coast.Struck.Value.SimTime:F0} s.");
    }

    /// <summary>A non-positive cost is a no-op, and a clean coast lands exactly on the epoch it was
    /// asked for — the two boundaries every caller of the clock relies on.</summary>
    [Fact]
    public void ACleanCoast_LandsExactlyOnTheEpochAsked()
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        ShipState parked = BerthState.CoMoving(eph, KaamosLore.IceMoonBodyId, 0, CyclerWindow.ArrivalOffsetMeters);

        Assert.Equal(parked, LoiterClock.Advance(simulator, eph, parked, 0).Ship);
        Assert.Equal(parked, LoiterClock.Advance(simulator, eph, parked, -5).Ship);

        LoiterClock.Coast coast = LoiterClock.Advance(simulator, eph, parked, 1234.5);
        Assert.Null(coast.Struck);
        Assert.Equal(parked.SimTime + 1234.5, coast.Ship.SimTime, 6);
        Assert.NotEqual(parked.Position, coast.Ship.Position); // she FLEW it — that is the whole fix
    }
}
