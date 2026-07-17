namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 27 — The getaway (escape-from-pursuit computed honestly, PR-BUSTED's physics).
/// See labs/27-the-getaway/README.md and Probe.cs for the lesson. Every gate exercises the SAME Core
/// the game flies with: EncounterRule.AdvanceHunter (the thrust-only pursuit law), SlingPlanner.Solve
/// (the crank), the Simulator's atmosphere drag, TransferMath.PhasingOrbit, and PursuitOdds (this
/// lab's seam). The gates assert the envelope boundary converges deterministically, each escape's
/// headline number reproduces within a physical band, and the Core table matches the printed run.
/// </summary>
public class Lab27GetawayTests
{
    private const double Day = 86400.0;
    private const double Year = 365.25 * Day;
    private const double SunMu = 1.32712440018e20;
    private const double JupiterMu = 1.26686534e17;
    private const double RJ = 6.9911e7;
    private const double SaturnMu = 3.7931187e16;
    private const double G0 = 9.80665;

    // ---- the pursuit driver (Probe.cs' Chase + FleeChase, verbatim in spirit) -----------------
    private static (bool Caught, double MinSep, double RelAtMin) FleeChase(double headStartMeters, double fleeSpeedMps, double horizonDays)
    {
        const double sceneX = 1e12; // player flees radially outward -> never sun-blinded (Section B)
        var playerPos0 = new Vector2d(sceneX, 0);
        var playerVel = new Vector2d(fleeSpeedMps, 0);
        var wolf = new HunterState("wolf", "WOLF", "policed", 0, 0,
            new ShipState(new Vector2d(sceneX - headStartMeters, 0), Vector2d.Zero, 0), false, false);
        double horizon = horizonDays * Day;
        double minSep = double.MaxValue, relAtMin = 0;
        for (double t = EncounterRule.HunterStepSeconds; t <= horizon; t += EncounterRule.HunterStepSeconds)
        {
            var p = new ShipState(playerPos0 + playerVel * t, playerVel, t);
            wolf = EncounterRule.AdvanceHunter(wolf, p, t);
            double sep = (wolf.State.Position - p.Position).Length;
            if (sep < minSep) { minSep = sep; relAtMin = (wolf.State.Velocity - playerVel).Length; }
            if (wolf.CaughtPlayer) break;
        }
        return (wolf.CaughtPlayer, minSep, relAtMin);
    }

    [Fact]
    public void G1_Envelope_CatchCliffIsTheCatchRadius()
    {
        // Inside the catch radius at a modest flee speed: grabbed. Past it: the wolf always overshoots
        // and never earns a <=cap grab within the horizon. This is the wolves' honesty contract.
        Assert.True(FleeChase(1e8, 500, 120).Caught, "100,000 km head start, 0.5 km/s flee must be caught (in the jaws)");
        Assert.True(FleeChase(2e8, 2000, 120).Caught, "200,000 km head start, 2 km/s flee must be caught (inside R)");
        Assert.False(FleeChase(4e8, 0, 120).Caught, "400,000 km head start (past R) must RUN even against a drifter");
        Assert.False(FleeChase(6e8, 1000, 120).Caught, "600,000 km head start, 1 km/s flee must RUN");
        Assert.False(FleeChase(3e9, 0, 120).Caught, "3,000,000 km head start must RUN");

        // The overshoot tell: past R the closest pass is hot (over the cap) — a fast pass, not a grab.
        var overshoot = FleeChase(6e8, 1000, 120);
        Assert.True(overshoot.RelAtMin > EncounterRule.CatchRelativeSpeedMetersPerSecond,
            $"a 'runs' cell's closest pass must be over the {EncounterRule.CatchRelativeSpeedMetersPerSecond:N0} m/s cap; got {overshoot.RelAtMin:F0} m/s");
    }

    [Fact]
    public void G2_Envelope_IsDeterministic()
    {
        // The pursuit law is a pure function of its inputs — the honesty contract must be reproducible.
        var a = FleeChase(6e8, 1000, 120);
        var b = FleeChase(6e8, 1000, 120);
        Assert.Equal(a.Caught, b.Caught);
        Assert.Equal(a.MinSep, b.MinSep, precision: 6);
        Assert.Equal(a.RelAtMin, b.RelAtMin, precision: 6);
    }

    [Fact]
    public void G3_KillerIdentity_ReachIsUSquaredOverTwoA()
    {
        double reach = EncounterRule.CatchRelativeSpeedMetersPerSecond * EncounterRule.CatchRelativeSpeedMetersPerSecond
            / (2 * EncounterRule.HunterAccelMps2);
        Assert.Equal(9.0e6, reach, precision: 0); // 9,000 km — the reason the getaway is earnable at all
    }

    // ---- the sling (Probe.cs' Section C setup, verbatim) --------------------------------------
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeJupiterSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("earth", "earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
            new CelestialBody("jupiter", "jupiter", "sun", JupiterMu, RJ, 7.7857e11, 3.74336e8, 3.6),
            new CelestialBody("saturn", "saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    private static SlingPlanner.Result SolveSling(ICelestialEphemeris eph, Simulator sim, out double passEpoch)
    {
        double dep = 100 * Day, tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = TransferMath.Lambert(pad.Position, jAt, tof, SunMu);
        var burn = sim.RunAdaptive(new ShipState(pad.Position, lam!.Value.V1, dep), tof - 40 * Day);
        passEpoch = dep + tof;
        return SlingPlanner.Solve(sim, eph, new SlingPlanner.Request(burn, "jupiter", passEpoch, 12 * RJ, SlingPlanner.PassSide.Lead));
    }

    [Fact]
    public void G4_Sling_DonatesKmsTheThrustOnlyWolfCannotCheaplyMatch()
    {
        var (eph, sim) = MakeJupiterSystem();
        var sling = SolveSling(eph, sim, out _);

        Assert.True(sling.Ok, $"the sling must converge; got: {sling.Failure}");
        // The flyby donates a large heliocentric speed change for free (~12 km/s on this case).
        Assert.InRange(sling.SpeedGain, 10_000.0, 14_000.0);
        // Matching it by thrust alone is hours of continuous burn — far beyond a tail-shake.
        double wolfBurnHours = sling.SpeedGain / EncounterRule.HunterAccelMps2 / 3600;
        Assert.True(wolfBurnHours > 5.0, $"matching the donated speed must cost the wolf hours of thrust; got {wolfBurnHours:F1} h");
        // Post-sling the player recedes far over the catch cap -> the flown envelope's Clear region.
        Assert.Equal(PursuitOdds.GeometryClass.Clear,
            PursuitOdds.Classify(3 * EncounterRule.CatchRadiusMeters, sling.SpeedGain));
    }

    [Fact]
    public void G5_Sling_IsDeterministic()
    {
        var (eph1, sim1) = MakeJupiterSystem();
        var (eph2, sim2) = MakeJupiterSystem();
        var a = SolveSling(eph1, sim1, out _);
        var b = SolveSling(eph2, sim2, out _);
        Assert.True(a.Ok && b.Ok);
        Assert.Equal(a.SpeedGain, b.SpeedGain, precision: 3);
        Assert.Equal(a.DeltaV.X, b.DeltaV.X, precision: 6);
    }

    // ---- the skim (Probe.cs' Section D SkimPass, verbatim) ------------------------------------
    private static readonly Atmosphere JupiterAtm = new(RefDensity: 4.0e-6, ScaleHeight: 3.0e4, TopAltitude: 4.0e5);

    private static (double DvShed, double PeakG, double MinAltKm) SkimPass(double vInf, double periAltKm)
    {
        var eph = new CircularOrbitEphemeris([new CelestialBody("jupiter", "jupiter", null, JupiterMu, RJ, 0, 0, 0, Atmosphere: JupiterAtm)]);
        var sim = new Simulator(eph, timeStepSeconds: 1.0);
        double shellTop = RJ + JupiterAtm.TopAltitude;
        double rStart = shellTop + 3.0e5;
        double rPeri = RJ + periAltKm * 1000;
        double vPeri = Math.Sqrt(vInf * vInf + 2 * JupiterMu / rPeri);
        double h = rPeri * vPeri;
        double v = Math.Sqrt(vInf * vInf + 2 * JupiterMu / rStart);
        double vt = h / rStart;
        double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
        var s = new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
        double vEntry = s.Velocity.Length;
        double peak = 0, minAlt = double.PositiveInfinity;
        bool entered = false;
        while (s.SimTime < 12 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) = sim.RunAdaptiveWithDrag(s, 30.0, null, minTimeStep: 0.1, maxTimeStep: 2.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
            s = next;
            double r = s.Position.Length;
            if (r < RJ) break;
            if (r < shellTop) entered = true;
            else if (entered) break;
        }
        return (vEntry - s.Velocity.Length, peak / G0, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt / 1000);
    }

    [Fact]
    public void G6_Skim_DeepestCleanSkimShedsRealSpeedUnderTheGLine()
    {
        // The 40 km pass is the deepest CLEAN skim: sheds ~1,567 m/s under the 3 g sail-hole line.
        var clean = SkimPass(5500, 40);
        Assert.InRange(clean.DvShed, 1400.0, 1750.0);
        Assert.True(clean.PeakG <= Atmosphere.SailHoleDecelG, $"the 40 km skim must stay under the {Atmosphere.SailHoleDecelG:F0} g line; got {clean.PeakG:F2} g");
        Assert.True(clean.MinAltKm > 0, "the clean skim must not impact");

        // One notch deeper (20 km) holes the sail — free braking has a floor.
        var tooDeep = SkimPass(5500, 20);
        Assert.True(tooDeep.PeakG > Atmosphere.SailHoleDecelG, $"the 20 km skim must cross the g line; got {tooDeep.PeakG:F2} g");

        // The overshoot margin: the shed speed is a re-acquire window measured in tens of minutes.
        double reNullMinutes = clean.DvShed / EncounterRule.HunterAccelMps2 / 60;
        Assert.True(reNullMinutes > 40, $"the shed speed must cost the wolf tens of minutes to null; got {reNullMinutes:F0} min");
    }

    // ---- the phasing juke (Probe.cs' Section E setup, verbatim) -------------------------------
    private static (ICelestialEphemeris Eph, Simulator Sim, ShipState AtDep, double RDep, double Gap, Vector2d Prograde, Vector2d ShipRelVel, double WorldSpeed) MakeLaneCase()
    {
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("saturn", "saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
            new CelestialBody("titan", "titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
            new CelestialBody("ringside-exchange", "ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        var sim = new Simulator(eph, timeStepSeconds: 60);
        const double arcBehind = 92.64e6, prep = 600.0;
        double t0 = 0.0;
        Vector2d satPos0 = eph.Position("saturn", t0);
        Vector2d satVel0 = TransferMath.BodyVelocity(eph, "saturn", t0);
        Vector2d ringRel0 = eph.Position("ringside-exchange", t0) - satPos0;
        double ringAngle0 = Math.Atan2(ringRel0.Y, ringRel0.X);
        double railRadius = 1.35e9;
        double shipAngle0 = ringAngle0 - arcBehind / railRadius;
        Vector2d shipRelPos0 = new Vector2d(Math.Cos(shipAngle0), Math.Sin(shipAngle0)) * railRadius;
        Vector2d tangent0 = new Vector2d(-Math.Sin(shipAngle0), Math.Cos(shipAngle0));
        var laneShip = new ShipState(satPos0 + shipRelPos0, satVel0 + tangent0 * Math.Sqrt(SaturnMu / railRadius), t0);
        double tDep = t0 + prep;
        ShipState atDep = sim.RunAdaptive(laneShip, prep);
        Vector2d satPosDep = eph.Position("saturn", tDep);
        Vector2d satVelDep = TransferMath.BodyVelocity(eph, "saturn", tDep);
        Vector2d shipRelPosDep = atDep.Position - satPosDep;
        Vector2d shipRelVelDep = atDep.Velocity - satVelDep;
        Vector2d targetRelPosDep = eph.Position("ringside-exchange", tDep) - satPosDep;
        double rDep = shipRelPosDep.Length;
        double gap = TransferMath.PhaseGap(shipRelPosDep, targetRelPosDep);
        Vector2d prograde = new Vector2d(-shipRelPosDep.Y, shipRelPosDep.X) / rDep;
        return (eph, sim, atDep, rDep, gap, prograde, shipRelVelDep, atDep.Velocity.Length);
    }

    private static Vector2d PosAt(IReadOnlyList<TrajectorySample> path, double t)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (t >= path[i].SimTime && t <= path[i + 1].SimTime)
            {
                double span = path[i + 1].SimTime - path[i].SimTime;
                double f = span > 0 ? (t - path[i].SimTime) / span : 0;
                return path[i].Position + (path[i + 1].Position - path[i].Position) * f;
            }
        }
        return path[^1].Position;
    }

    [Fact]
    public void G7_Juke_StalenessIsMonotoneInK_AndVoidsTheShotSoonerForTheLoudJuke()
    {
        var c = MakeLaneCase();
        double tDep = c.AtDep.SimTime;
        var coast = c.Sim.ProjectAdaptive(c.AtDep, null, 8 * Day, maxTimeStep: 1800, maxSamples: 20_000);
        double hitR = OrdnanceRule.HitRadiusMeters;

        double? prevEnter = null, prevStale1 = null; double voidK1 = double.NaN, voidK6 = double.NaN;
        for (int k = 1; k <= 6; k++)
        {
            var plan = TransferMath.PhasingOrbit(c.RDep, c.Gap, SaturnMu, k, dipInside: true);
            Assert.NotNull(plan);
            double phasingSpeed = Math.Sqrt(SaturnMu * (2 / c.RDep - 1 / plan.Value.SemiMajorAxis));
            Vector2d dv1 = c.Prograde * phasingSpeed - c.ShipRelVel;
            var jukeStart = c.AtDep with { Velocity = c.AtDep.Velocity + dv1 };
            var jukePath = c.Sim.ProjectAdaptive(jukeStart, null, 4 * Day, maxTimeStep: 1800, maxSamples: 20_000);
            double stale1 = (PosAt(jukePath, tDep + 1 * Day) - PosAt(coast, tDep + 1 * Day)).Length;
            double voidH = double.NaN;
            for (double h = 0.5; h <= 96; h += 0.5)
                if ((PosAt(jukePath, tDep + h * 3600) - PosAt(coast, tDep + h * 3600)).Length > hitR) { voidH = h; break; }

            // Enter burn and staleness both shrink as k grows (more laps, gentler dip).
            if (prevEnter is double pe) Assert.True(plan.Value.EnterDeltaV < pe, $"enter Δv must shrink with k at k={k}");
            if (prevStale1 is double ps) Assert.True(stale1 < ps, $"staleness@1d must shrink with k at k={k}");
            prevEnter = plan.Value.EnterDeltaV;
            prevStale1 = stale1;
            if (k == 1) voidK1 = voidH;
            if (k == 6) voidK6 = voidH;
        }

        // The dear one-lap juke voids the 0.5 Mm firing solution soonest; the cheap six-lap juke latest.
        Assert.True(voidK1 < voidK6, $"k=1 must void the shot sooner ({voidK1:F1} h) than k=6 ({voidK6:F1} h)");
        Assert.InRange(voidK1, 5.0, 10.0); // ~7.5 h on this case
    }

    [Fact]
    public void G8_PursuitOdds_TableMatchesThePrintedRun()
    {
        // The boundary constants ARE EncounterRule's own catch numbers (the honesty contract).
        Assert.Equal(EncounterRule.CatchRadiusMeters, PursuitOdds.JawsHeadStartMeters);
        Assert.Equal(EncounterRule.CatchRelativeSpeedMetersPerSecond, PursuitOdds.RunnerCatchCapMps);

        // The geometry classifier, at the flown envelope's landmarks.
        Assert.Equal(PursuitOdds.GeometryClass.InItsJaws, PursuitOdds.Classify(2e8, 500));
        Assert.Equal(PursuitOdds.GeometryClass.EvenChase, PursuitOdds.Classify(3e8, 4000));
        Assert.Equal(PursuitOdds.GeometryClass.SternChase, PursuitOdds.Classify(5e8, 1000));
        Assert.Equal(PursuitOdds.GeometryClass.Clear, PursuitOdds.Classify(7e8, 1000));

        // The full odds table, pinned to Section F's printout (row = trick, col = geometry).
        var expected = new (PursuitOdds.Trick T, PursuitOdds.GeometryClass G, PursuitOdds.EscapeOdds O)[]
        {
            (PursuitOdds.Trick.Run, PursuitOdds.GeometryClass.InItsJaws, PursuitOdds.EscapeOdds.Forlorn),
            (PursuitOdds.Trick.Run, PursuitOdds.GeometryClass.EvenChase, PursuitOdds.EscapeOdds.EvenMoney),
            (PursuitOdds.Trick.Run, PursuitOdds.GeometryClass.SternChase, PursuitOdds.EscapeOdds.Likely),
            (PursuitOdds.Trick.Run, PursuitOdds.GeometryClass.Clear, PursuitOdds.EscapeOdds.Certain),
            (PursuitOdds.Trick.Sling, PursuitOdds.GeometryClass.InItsJaws, PursuitOdds.EscapeOdds.Slim),
            (PursuitOdds.Trick.Sling, PursuitOdds.GeometryClass.SternChase, PursuitOdds.EscapeOdds.Certain),
            (PursuitOdds.Trick.Skim, PursuitOdds.GeometryClass.InItsJaws, PursuitOdds.EscapeOdds.EvenMoney),
            (PursuitOdds.Trick.PhasingJuke, PursuitOdds.GeometryClass.InItsJaws, PursuitOdds.EscapeOdds.Slim),
            (PursuitOdds.Trick.PhasingJuke, PursuitOdds.GeometryClass.Clear, PursuitOdds.EscapeOdds.Certain),
        };
        foreach (var (t, g, o) in expected)
            Assert.Equal(o, PursuitOdds.OddsFor(t, g));

        // The dice ladder the RESIST/RUN roll reads.
        Assert.Equal(-3, PursuitOdds.ModifierFor(PursuitOdds.EscapeOdds.Forlorn));
        Assert.Equal(0, PursuitOdds.ModifierFor(PursuitOdds.EscapeOdds.EvenMoney));
        Assert.Equal(+4, PursuitOdds.ModifierFor(PursuitOdds.EscapeOdds.Certain));
        Assert.Equal(PursuitOdds.ModifierFor(PursuitOdds.OddsFor(PursuitOdds.Trick.Sling, PursuitOdds.GeometryClass.SternChase)),
            PursuitOdds.DiceModifier(PursuitOdds.Trick.Sling, PursuitOdds.GeometryClass.SternChase));
    }
}
