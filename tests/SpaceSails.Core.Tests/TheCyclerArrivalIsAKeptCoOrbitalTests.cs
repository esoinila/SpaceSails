using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #742 — THE ARRIVAL PHASE THE CYCLER NEVER LETS YOU CHOOSE.
///
/// <para>The window opens every 40 days and stands open for two, so the phase of Enceladus's 32.9 h orbit
/// that the ride lets you go on is effectively free. The park it let you go on was
/// <see cref="BerthState.CoMoving"/>: the standoff laid along the SUN-outward direction, the moon's
/// velocity handed over. In Saturn's frame that is not a park — it is a different ellipse, and WHICH
/// ellipse depends entirely on where the Sun-outward direction happened to be pointing relative to the
/// moon's track. Sweeping one whole moon orbit at the 1e7 m cycler standoff: semi-major axis 2.193e8 …
/// 2.592e8 m against the moon's 2.38037e8, period 104,768 … 134,641 s against the moon's 118,387 — up to
/// 13.7 % off — at e ≈ 0.041 throughout, so periapsis and apoapsis bracketed the rail the moon is on and
/// the period mismatch walked the ship round it until the crossings met. Phase 2/24 struck the ice at
/// +9.54 h; 3/24 and 14/24 passed at 3.157e5 and 5.816e5 m, one and two surface radii.</para>
///
/// <para><b>The law these hold.</b> An arrival the captain cannot choose must deposit a KEPT orbit —
/// the owner's ruling, "AUTOPILOT HOLDS THE ORBIT" — at EVERY phase the generator admits, not at 23 of
/// 24. <see cref="BerthState.CoOrbital"/> lays the standoff along the body's own track instead, which is
/// the body's own conic phase-shifted, so there is no phase dependence left to be unlucky in.</para>
///
/// <para><b>Proven able to fail, not assumed to be.</b> Making <see cref="BerthState.CoOrbital"/> hand back
/// <see cref="BerthState.CoMoving"/> — one line, the old geometry — takes TWELVE tests red across this file
/// and <c>TheParkedShipIsNotRunDownByTheMoonTests</c>: phase 2/24 (epoch 9,866 s) meets Enceladus at
/// +9.54 h, phase 5/24 meets Europa at +12.00 h, and
/// <see cref="EveryArrivalPhase_DepositsAnOrbitThatDoesNotCrossTheMoonsRail"/> goes red at the FIRST phase
/// of every park, because the mechanism was never one unlucky number. The anti-vacuity block below then
/// refuses to let any of it pass on a world that is not moving.</para>
/// </summary>
[SlowGate] // #251 · 28 s over 13 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public class TheCyclerArrivalIsAKeptCoOrbitalTests
{
    /// <summary>Enceladus's rail period, from <c>sol.json</c> — quoted ONCE, only to pin the issue's own
    /// epoch (phase 2/24 = 9,866 s) to a number a reader can check against the report. Every sweep below
    /// reads the period off the body it is sweeping instead, because a moon's number copied into a test
    /// file is the same bug class as a moon's number copied into a client file — and because a sweep that
    /// paced Europa's phases by Enceladus's clock would cover 38 % of Europa's orbit while claiming
    /// "all 24".</summary>
    private const double EnceladusRailPeriodSeconds = 118_386.8;

    /// <summary>How many arrival phases the window admits — the sweep's resolution, and the number the
    /// issue counted the bad one out of. Quoted from Core so the guard and the cheat can never disagree
    /// about what "phase 2 of 24" means.</summary>
    private const int ArrivalPhases = CyclerWindow.ArrivalPhases;

    /// <summary>How far past the ten hours the drift used to take. Five sim days is 3.6 Enceladus orbits
    /// and 12× the strike time #742 reported, long enough that a slow resonant close cannot hide inside
    /// it.</summary>
    private const double HorizonSeconds = 5.0 * 86_400.0;

    /// <summary>Every free (unclamped) park beside a massive body this game deposits a ship at, named and
    /// quoted from the live constants. All of them are the same construction and the same disease.</summary>
    public static TheoryData<string, string, double> FreeParks => new()
    {
        { "the cycler arrival · ?kaamos=hq (#411)", KaamosLore.IceMoonBodyId, CyclerWindow.ArrivalOffsetMeters },
        { "the ?start=enceladus bench start (#136)", KaamosLore.IceMoonBodyId, 5.0e6 },
        { "the ?start=jupiter spawn, beside Europa", "europa", 2.0e7 },
    };

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    /// <summary>The epoch of an arrival phase, paced by the body's OWN orbit — the live function the
    /// <c>?arrivalphase=</c> cheat calls, so the sweep and the demo door walk the same grid.</summary>
    private static double PhaseEpoch(CelestialBody body, int phase) =>
        CyclerWindow.ArrivalPhaseEpoch(body.OrbitPeriod, phase);

    /// <summary>Fly her ballistically, watching a body — the raw integrator and the raw crossing test, so
    /// nothing that judges the flight goes through the construction under test.</summary>
    private static (SurfaceImpact.Crossing? Struck, double Closest, double Widest, double ClosestAt) FlyWatching(
        Simulator simulator, ICelestialEphemeris ephemeris, ShipState ship, double seconds, string watchBodyId)
    {
        double end = ship.SimTime + seconds;
        double closest = double.MaxValue;
        double widest = 0.0;
        double closestAt = ship.SimTime;

        while (ship.SimTime < end)
        {
            Vector2d from = ship.Position;
            double fromTime = ship.SimTime;
            ship = simulator.RunAdaptive(ship, Math.Min(LoiterClock.QuantumSeconds, end - ship.SimTime));

            if (SurfaceImpact.FirstCrossing(from, fromTime, ship.Position, ship.SimTime, ephemeris) is { } hit)
            {
                return (hit, 0.0, widest, hit.SimTime);
            }

            double range = (ship.Position - ephemeris.Position(watchBodyId, ship.SimTime)).Length;
            if (range < closest)
            {
                closest = range;
                closestAt = ship.SimTime;
            }

            if (range > widest)
            {
                widest = range;
            }
        }

        return (null, closest, widest, closestAt);
    }

    // ── The sweep ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE GUARD: every one of the 24 arrival phases, flown for five days off the real deposit, must hold
    /// most of its standoff. Not merely "misses the surface" — #742's phase 3/24 missed the surface by
    /// 1.25 radii and that is a captain watching the ice fill the window. The bar is HALF THE STANDOFF,
    /// which is a bar the Sun-outward park fails at four separate phases and the along-track park clears
    /// with 96 % of the gap still in hand.
    /// </summary>
    [Theory]
    [MemberData(nameof(FreeParks))]
    public void EveryArrivalPhase_KeepsMostOfItsStandoffForFiveDays(string what, string bodyId, double standoff)
    {
        CircularOrbitEphemeris eph = Sol();
        var simulator = new Simulator(eph, timeStepSeconds: 1.0);
        CelestialBody moon = eph.Bodies.First(b => b.Id == bodyId);

        double bar = standoff / 2.0;
        double worst = double.MaxValue;
        int worstPhase = -1;
        double worstAt = 0;
        var phasesSwept = new HashSet<int>();
        double totalSpread = 0;

        for (int phase = 0; phase < ArrivalPhases; phase++)
        {
            double epoch = PhaseEpoch(moon, phase);
            ShipState parked = BerthState.CoOrbital(eph, bodyId, epoch, standoff);

            // The shuttle crossing the arc's own passengers pay first, through the one law that prices it.
            double crossing = ShuttleRange.TravelSeconds((parked.Position - eph.Position(bodyId, epoch)).Length);
            LoiterClock.Coast down = LoiterClock.Advance(simulator, eph, parked, crossing);
            Assert.True(down.Struck is null,
                $"{what}: phase {phase}/{ArrivalPhases} — the {crossing:F0} s crossing itself ended on " +
                $"{down.Struck?.BodyName}.");

            (SurfaceImpact.Crossing? struck, double closest, double widest, double closestAt) =
                FlyWatching(simulator, eph, down.Ship, HorizonSeconds, bodyId);

            Assert.True(struck is null,
                $"{what}: phase {phase}/{ArrivalPhases} (epoch {epoch:F0} s) met {struck?.BodyName} at " +
                $"+{((struck?.SimTime ?? 0) - epoch) / 3600:F2} h — inside the {HorizonSeconds / 3600:F0} h " +
                "this arrival must hold. #742's strike was at +9.54 h.");

            phasesSwept.Add(phase);
            totalSpread += widest - closest;

            if (closest < worst)
            {
                worst = closest;
                worstPhase = phase;
                worstAt = closestAt - epoch;
            }
        }

        Assert.True(worst > bar,
            $"{what}: the tightest of {ArrivalPhases} arrival phases closes to {worst:e3} m at " +
            $"+{worstAt / 3600:F2} h (phase {worstPhase}/{ArrivalPhases}) — under half the {standoff:e2} m " +
            $"standoff, and {worst / moon.BodyRadius:F2} surface radii of {moon.Name}.");

        // ── Anti-vacuity: the sweep must have swept, and the world must have moved. ──────────────────
        Assert.Equal(ArrivalPhases, phasesSwept.Count);
        Assert.Equal(ArrivalPhases, phasesSwept.Select(p => PhaseEpoch(moon, p)).Distinct().Count());

        // The phases must span THIS body's whole orbit, or "all 24" is 24 samples of the same geometry.
        // Paced by the body under test, never by Enceladus: Europa's orbit is 2.6× longer, and a grid on
        // Enceladus's clock would sweep 38 % of it while reporting a full revolution.
        double span = PhaseEpoch(moon, ArrivalPhases - 1) - PhaseEpoch(moon, 0);
        Assert.True(span > 0.9 * Math.Abs(moon.OrbitPeriod) * (ArrivalPhases - 1) / ArrivalPhases
                    && span < Math.Abs(moon.OrbitPeriod),
            $"{what}: the swept phases span {span:F0} s of {moon.Name}'s {Math.Abs(moon.OrbitPeriod):F0} s " +
            "orbit — they must walk once round it, not sample one corner of it 24 times.");

        // …and each flight must actually have been perturbed: a range that never varies is a ship that
        // never flew, and a guard that cannot tell a moving world from a frozen one proves nothing.
        Assert.True(totalSpread > ArrivalPhases * 1_000.0,
            $"{what}: the swept flights varied their range by only {totalSpread:e3} m in total — " +
            "nothing moved, so nothing was tested.");
    }

    // ── The mechanism itself, guarded ────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MECHANISM, held directly rather than only its symptom. #742's strike was the end of a chain:
    /// the deposited state was a Saturn-centric ellipse whose periapsis and apoapsis <b>bracketed the
    /// moon's own rail radius</b>, and a period up to 13.7 % off then walked the ship round that rail
    /// until the two crossings coincided. So this asserts the two facts that together make the strike
    /// impossible: the deposited conic's period matches the moon's to well under a percent, and its radial
    /// wander about the rail is a small fraction of the very standoff the park is promising to hold.
    ///
    /// <para>This is the assertion that goes red at EVERY phase on the old construction, not one in 24 —
    /// which is what makes it a law rather than a fence around one unlucky number.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(FreeParks))]
    public void EveryArrivalPhase_DepositsAnOrbitThatDoesNotCrossTheMoonsRail(
        string what, string bodyId, double standoff)
    {
        CircularOrbitEphemeris eph = Sol();
        CelestialBody moon = eph.Bodies.First(b => b.Id == bodyId);
        CelestialBody parent = eph.Bodies.First(b => b.Id == moon.ParentId);

        int swept = 0;
        for (int phase = 0; phase < ArrivalPhases; phase++)
        {
            double epoch = PhaseEpoch(moon, phase);
            ShipState parked = BerthState.CoOrbital(eph, bodyId, epoch, standoff);

            Vector2d parentPos = eph.Position(parent.Id, epoch);
            Vector2d parentVel = (eph.Position(parent.Id, epoch + 1) - eph.Position(parent.Id, epoch - 1)) / 2.0;
            Vector2d r = parked.Position - parentPos;
            Vector2d v = parked.Velocity - parentVel;

            double semiMajor = 1.0 / (2.0 / r.Length - v.LengthSquared / parent.Mu);
            double angular = r.X * v.Y - r.Y * v.X;
            double ecc = Math.Sqrt(Math.Max(0.0, 1.0 - angular * angular / (parent.Mu * semiMajor)));
            double period = Math.Tau * Math.Sqrt(semiMajor * semiMajor * semiMajor / parent.Mu);

            double rail = eph.InstantaneousOrbitRadius(bodyId, epoch);
            double periapsis = semiMajor * (1.0 - ecc);
            double apoapsis = semiMajor * (1.0 + ecc);
            double excursion = Math.Max(rail - periapsis, apoapsis - rail);

            // The bar is a fifth of the standoff, because the standoff is what the park PROMISES: a
            // conic whose radial wander about the rail is a small fraction of the gap it is holding can
            // never bring the hull onto the moon, however long the phases take to line up. The old
            // construction wandered 2.4e7 m about a 1e7 m standoff — twelve times this bar.
            Assert.True(excursion < 0.2 * standoff,
                $"{what}: phase {phase}/{ArrivalPhases} deposits a conic spanning {periapsis:e4} … " +
                $"{apoapsis:e4} m about {moon.Name}'s {rail:e4} m rail (e = {ecc:F5}) — a radial wander of " +
                $"{excursion:e3} m against a {standoff:e2} m standoff. An orbit that crosses the rail the " +
                "moon is on, by more than the gap it is meant to keep, will be met on it sooner or later.");

            // Math.Abs on the moon's period too: sol.json writes a retrograde orbit as a NEGATIVE period,
            // and a tolerance built from one of those is a negative bar that nothing can clear.
            double railPeriod = Math.Abs(moon.OrbitPeriod);
            Assert.True(Math.Abs(period - railPeriod) < 0.01 * railPeriod,
                $"{what}: phase {phase}/{ArrivalPhases} deposits a period of {period:F1} s against " +
                $"{moon.Name}'s {railPeriod:F1} s — {(period - railPeriod) / railPeriod:P2} " +
                "off. #742 ranged to 13.7 % and that mismatch is what walked the hull onto the moon.");

            swept++;
        }

        Assert.Equal(ArrivalPhases, swept);
    }

    // ── The deposit's own contract ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// The park must still BE a park, and this is where the fix's one honest cost is written down rather
    /// than hidden. Two points on the same circular track, an arc apart, do NOT have the same velocity
    /// VECTOR — theirs differ by ω·d, which at the cycler standoff is 531 m/s. What they do have is the
    /// same SPEED about the parent (a rotation is an isometry, to the metre per second) and a relative
    /// velocity that is exactly perpendicular to the gap between them: a rigid rotation, so the RANGE
    /// RATE is zero and the standoff does not open or close. That is the correct reading of "co-moving
    /// alongside", and it is the one the old construction failed — it matched the velocity vector exactly
    /// and then flew off on a different ellipse.
    /// </summary>
    [Theory]
    [MemberData(nameof(FreeParks))]
    public void TheFreePark_HoldsItsStandoff_AtZeroRangeRate(string what, string bodyId, double standoff)
    {
        CircularOrbitEphemeris eph = Sol();
        CelestialBody body = eph.Bodies.First(b => b.Id == bodyId);

        for (int phase = 0; phase < ArrivalPhases; phase++)
        {
            double epoch = PhaseEpoch(body, phase);
            ShipState parked = BerthState.CoOrbital(eph, bodyId, epoch, standoff);

            Vector2d bodyPos = eph.Position(bodyId, epoch);
            Vector2d bodyVel = (eph.Position(bodyId, epoch + 1) - eph.Position(bodyId, epoch - 1)) / 2.0;
            Vector2d parentPos = eph.Position(body.ParentId!, epoch);
            Vector2d parentVel = (eph.Position(body.ParentId!, epoch + 1) - eph.Position(body.ParentId!, epoch - 1)) / 2.0;

            double gap = (parked.Position - bodyPos).Length;
            Assert.True(Math.Abs(gap - standoff) < 1.0,
                $"{what}: phase {phase} parked at {gap:e6} m, not the {standoff:e6} m standoff.");

            double shipSpeed = (parked.Velocity - parentVel).Length;
            double bodySpeed = (bodyVel - parentVel).Length;
            Assert.True(Math.Abs(shipSpeed - bodySpeed) < 1e-3,
                $"{what}: phase {phase} was handed {shipSpeed:F6} m/s about {body.ParentId} against the " +
                $"body's {bodySpeed:F6} m/s — a rotation must preserve the speed exactly.");

            Vector2d separation = parked.Position - bodyPos;
            Vector2d closing = parked.Velocity - bodyVel;
            double rangeRate = separation.Dot(closing) / separation.Length;
            Assert.True(Math.Abs(rangeRate) < 1.0,
                $"{what}: phase {phase} is closing on {body.Name} at {rangeRate:F3} m/s at the moment of " +
                "the deposit — a rigid rotation must open and close the gap at nothing at all.");

            // …and the relative velocity is real, not zero: 531 m/s at the cycler standoff. If this ever
            // reads zero the ship has been handed the body's own vector again, which is #742 exactly.
            Assert.True(closing.Length > 0.5 * standoff * Math.Tau / Math.Abs(body.OrbitPeriod),
                $"{what}: phase {phase} shows a relative velocity of {closing.Length:F1} m/s where a rigid " +
                $"co-rotation at {standoff:e2} m needs ω·d.");
        }
    }

    /// <summary>BEHIND, and read off the body rather than assumed. A ship handed a rail state and then
    /// integrated flies a hair slower round than the rail does, so the park must sit where that lag OPENS
    /// the gap. <c>sol.json</c>'s Triton runs retrograde (a negative period), so the sign has to come from
    /// the body's own angular momentum — a hard-coded "counter-clockwise" would park the ship in front of
    /// it.</summary>
    [Theory]
    [InlineData("enceladus")]
    [InlineData("triton")]
    public void TheFreePark_SitsBehindTheBody_EvenWhenTheBodyRunsBackwards(string bodyId)
    {
        CircularOrbitEphemeris eph = Sol();
        CelestialBody body = eph.Bodies.First(b => b.Id == bodyId);
        const double epoch = 12_345.0;
        const double standoff = 5.0e6;

        Vector2d parent = eph.Position(body.ParentId!, epoch);
        Vector2d rel = eph.Position(bodyId, epoch) - parent;
        Vector2d relVel = (eph.Position(bodyId, epoch + 1) - eph.Position(bodyId, epoch - 1)) / 2.0
            - (eph.Position(body.ParentId!, epoch + 1) - eph.Position(body.ParentId!, epoch - 1)) / 2.0;

        Vector2d shipRel = BerthState.CoOrbital(eph, bodyId, epoch, standoff).Position - parent;

        // "Behind" = the ship is on the side the body has already left: the cross product from the ship to
        // the body has the same sign as the body's own angular momentum.
        double bodyAngular = rel.X * relVel.Y - rel.Y * relVel.X;
        double shipToBody = shipRel.X * rel.Y - shipRel.Y * rel.X;
        Assert.True(Math.Sign(shipToBody) == Math.Sign(bodyAngular),
            $"{bodyId} (angular momentum {bodyAngular:e3}) was given a park on the wrong side of itself.");
    }

    /// <summary>
    /// THE DEMO ROW MUST OPEN THE DOOR IT NAMES. <c>/map?kaamos=hq&amp;arrivalphase=2</c> is promised, in
    /// <c>DevStarts</c> and in <c>docs/testing-guide.md</c>, to boot into the placement that used to strike
    /// the ice at +9.54 h. That promise is arithmetic — phase 2 of 24 is epoch 9,866 s — and this holds the
    /// cheat's own function to it. A testing-guide row that quietly points at a different phase would be
    /// this project's own bug class committed in its own documentation, which has happened here before.
    ///
    /// <para>The phase grid is held to the MOON, too: <see cref="CyclerWindow.ArrivalPhaseEpoch"/> takes
    /// the period as an argument precisely so that no copy of Enceladus's number lives anywhere but
    /// Enceladus, and the grid it builds spans exactly one revolution and then repeats.</para>
    /// </summary>
    [Fact]
    public void TheArrivalPhaseCheat_LandsOnThePhaseThatUsedToStrike()
    {
        CircularOrbitEphemeris eph = Sol();
        CelestialBody ice = eph.Bodies.First(b => b.Id == KaamosLore.IceMoonBodyId);

        Assert.Equal(ArrivalPhases, CyclerWindow.ArrivalPhases);

        // Phase 2/24 — the epoch the sweep struck at, to the second the issue reported it.
        double bad = CyclerWindow.ArrivalPhaseEpoch(ice.OrbitPeriod, 2);
        Assert.Equal(9866.0, bad, 0);

        // …and the one hand-quoted moon constant in this file is the same moon (see the const's remarks).
        Assert.Equal(EnceladusRailPeriodSeconds, Math.Abs(ice.OrbitPeriod), 3);

        // The grid is the moon's own orbit, once round and no further: phase 24 is phase 0 again.
        Assert.Equal(0.0, CyclerWindow.ArrivalPhaseEpoch(ice.OrbitPeriod, 0));
        Assert.Equal(0.0, CyclerWindow.ArrivalPhaseEpoch(ice.OrbitPeriod, ArrivalPhases));
        Assert.Equal(
            Math.Abs(ice.OrbitPeriod) * (ArrivalPhases - 1) / ArrivalPhases,
            CyclerWindow.ArrivalPhaseEpoch(ice.OrbitPeriod, ArrivalPhases - 1), 6);

        // A retrograde moon's negative period still yields a forward-running grid, because a phase index
        // is a place on a circle and not a direction of travel (sol.json's Triton has one).
        CelestialBody triton = eph.Bodies.First(b => b.Id == "triton");
        Assert.True(triton.OrbitPeriod < 0, "sol.json's Triton is the retrograde case this guards.");
        Assert.True(CyclerWindow.ArrivalPhaseEpoch(triton.OrbitPeriod, 5) > 0);
    }

    /// <summary>The fallbacks, stated: no parent, no motion, or a standoff too wide to be an arc on the
    /// rail — there is no track to lay the offset along, so the construction hands back the old one
    /// rather than inventing a track.</summary>
    [Fact]
    public void WithNoTrackToLayItAlong_TheFreeParkIsTheClampedBerth()
    {
        CircularOrbitEphemeris eph = Sol();

        // The Sun has no parent.
        Assert.Equal(
            BerthState.CoMoving(eph, "sun", 0, 1.0e7),
            BerthState.CoOrbital(eph, "sun", 0, 1.0e7));

        // A standoff wider than the rail's diameter is not an arc on it.
        double diameter = 2.0 * eph.InstantaneousOrbitRadius("enceladus", 0);
        Assert.Equal(
            BerthState.CoMoving(eph, "enceladus", 0, diameter + 1),
            BerthState.CoOrbital(eph, "enceladus", 0, diameter + 1));

        // A zero standoff is the body itself either way.
        Assert.Equal(
            BerthState.CoMoving(eph, "enceladus", 0, 0),
            BerthState.CoOrbital(eph, "enceladus", 0, 0));
    }
}
