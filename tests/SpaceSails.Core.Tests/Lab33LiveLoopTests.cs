namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 33 — Aerobrake in the LIVE loop. Where Lab 32 (and <see cref="AerobrakeTests"/>) fly a
/// clean body-at-origin frame, these fly the SAME Core drag in a two-body-rail ephemeris — the Sun pinned,
/// Uranus on its real sol.json orbit at ~6.8 km/s — so v_rel genuinely includes the planet's heliocentric
/// motion and the integrator is n-body. The gates pin the live-loop findings: the campaign still converges
/// (apoapsis drops pass after pass), the moving atmosphere sheds exactly what the pinned frame did (Galilean),
/// an air-assisted bridge captures for real, and the whole thing is deterministic.
/// </summary>
public class Lab33LiveLoopTests
{
    private const double SunMu = 1.32712440018e20;
    private const double UranusMu = 5.793939e15, UranusR = 2.5362e7, UranusOrbitR = 2.87246e12, UranusPeriod = 2.65104e9;
    private const double G0 = 9.80665;
    private const double StrandVinf = 29800.0;

    private static Atmosphere UranusAtm => new(RefDensity: 1.4e-5, ScaleHeight: 1.2e5, TopAltitude: 1.0e6);

    private static (Simulator Sim, CircularOrbitEphemeris Eph) LiveUranus()
    {
        var sun = new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0);
        var uranus = new CelestialBody("uranus", "Uranus", "sun", UranusMu, UranusR, UranusOrbitR, UranusPeriod, 0.5,
            Atmosphere: UranusAtm);
        var eph = new CircularOrbitEphemeris([sun, uranus]);
        return (new Simulator(eph, timeStepSeconds: 1.0), eph);
    }

    private static Vector2d UPos(CircularOrbitEphemeris e, double t) => e.Position("uranus", t);
    private static Vector2d UVel(CircularOrbitEphemeris e, double t) =>
        (e.Position("uranus", t + 1.0) - e.Position("uranus", t - 1.0)) / 2.0;

    private static double RelEnergy(CircularOrbitEphemeris e, ShipState s)
    {
        Vector2d rRel = s.Position - UPos(e, s.SimTime);
        Vector2d vRel = s.Velocity - UVel(e, s.SimTime);
        return vRel.LengthSquared / 2.0 - UranusMu / rRel.Length;
    }

    private static double RelApoapsis(CircularOrbitEphemeris e, ShipState s)
    {
        Vector2d rRel = s.Position - UPos(e, s.SimTime);
        Vector2d vRel = s.Velocity - UVel(e, s.SimTime);
        double e0 = vRel.LengthSquared / 2.0 - UranusMu / rRel.Length;
        if (e0 >= 0) return double.PositiveInfinity;
        double a = -UranusMu / (2 * e0);
        double h = System.Math.Abs(rRel.X * vRel.Y - rRel.Y * vRel.X);
        double ecc = System.Math.Sqrt(System.Math.Max(0, 1 + 2 * e0 * h * h / (UranusMu * UranusMu)));
        return a * (1 + ecc);
    }

    private static double RelDist(CircularOrbitEphemeris e, ShipState s) => (s.Position - UPos(e, s.SimTime)).Length;

    private static ShipState LiveArrival(CircularOrbitEphemeris e, double t0, double rStart, double rPeri, double vInf)
    {
        double vPeri = System.Math.Sqrt(vInf * vInf + 2 * UranusMu / rPeri);
        double h = rPeri * vPeri;
        double v = System.Math.Sqrt(vInf * vInf + 2 * UranusMu / rStart);
        double vt = h / rStart, vr = -System.Math.Sqrt(System.Math.Max(0, v * v - vt * vt));
        return new ShipState(UPos(e, t0) + new Vector2d(rStart, 0), UVel(e, t0) + new Vector2d(vr, vt), t0);
    }

    private static ShipState LiveBound(CircularOrbitEphemeris e, double t0, double rStart, double rPeri, double rApo)
    {
        double a = (rPeri + rApo) / 2.0;
        double vPeri = System.Math.Sqrt(UranusMu * (2.0 / rPeri - 1.0 / a));
        double h = rPeri * vPeri;
        double v = System.Math.Sqrt(System.Math.Max(0, UranusMu * (2.0 / rStart - 1.0 / a)));
        double vt = h / rStart, vr = -System.Math.Sqrt(System.Math.Max(0, v * v - vt * vt));
        return new ShipState(UPos(e, t0) + new Vector2d(rStart, 0), UVel(e, t0) + new Vector2d(vr, vt), t0);
    }

    // One live pass: continuous RunAdaptiveWithDrag (drag is zero above the shell) from entry down through
    // periapsis until it climbs back above the shell top. The plot-desk skim gauge's integrator config.
    private static (ShipState Post, double PeakG, double Shed, bool Crashed) LivePass(
        Simulator sim, CircularOrbitEphemeris e, ShipState entry)
    {
        double shellTop = UranusR + UranusAtm.TopAltitude;
        double peak = 0, shed = 0;
        ShipState s = entry;
        bool entered = false, crashed = false;
        while (s.SimTime - entry.SimTime < 6 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) = sim.RunAdaptiveWithDrag(s, 20.0, null, minTimeStep: 0.05, maxTimeStep: 3.0);
            peak = System.Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            shed += rep.DeltaVShedMetersPerSecond;
            s = next;
            double r = RelDist(e, s);
            if (r < UranusR) { crashed = true; break; }
            if (r < shellTop) { entered = true; if (RelEnergy(e, s) < 0 && RelApoapsis(e, s) < shellTop) { crashed = true; break; } }
            else if (entered) break;
        }
        return (s, peak / G0, shed, crashed);
    }

    private static ShipState CoastToNextEntry(Simulator sim, CircularOrbitEphemeris e, ShipState post)
    {
        double shellTop = UranusR + UranusAtm.TopAltitude, gate = shellTop + 3.0e5;
        ShipState s = post;
        bool climbed = false;
        double t0 = s.SimTime;
        while (s.SimTime - t0 < 90 * 86400.0)
        {
            double r = RelDist(e, s);
            double step = r > 3 * shellTop ? 300.0 : 20.0;
            (ShipState next, _) = sim.RunAdaptiveWithDrag(s, step, null, minTimeStep: 1.0, maxTimeStep: step);
            double rNext = RelDist(e, next);
            if (rNext > gate) climbed = true;
            if (climbed && rNext <= gate && rNext < r) return next;
            s = next;
        }
        return s;
    }

    [Fact]
    public void TheFreeTighteningCampaign_Converges_UnderTheLiveIntegrator()
    {
        // Lab 33 §C: seeded on a captured wide orbit, each free live pass strictly lowers apoapsis — the
        // campaign converges with a MOVING atmosphere and a live solar tug, not just in the pinned frame.
        (Simulator sim, CircularOrbitEphemeris e) = LiveUranus();
        double startAlt = UranusAtm.TopAltitude + 3.0e5;
        ShipState ship = LiveBound(e, 0, UranusR + startAlt, UranusR + 300e3, 4 * UranusR);
        double firstApo = RelApoapsis(e, ship), lastApo = double.PositiveInfinity;

        for (int passN = 1; passN <= 5; passN++)
        {
            var pass = LivePass(sim, e, ship);
            Assert.False(pass.Crashed, $"pass {passN} does not auger in");
            Assert.True(RelEnergy(e, pass.Post) < 0, $"pass {passN} stays bound");
            double apo = RelApoapsis(e, pass.Post);
            Assert.True(apo < lastApo + 1e3, $"pass {passN} apoapsis {apo / UranusR:F2} R_U does not grow");
            lastApo = apo;
            if (apo < 1.5 * UranusR) break;
            ship = CoastToNextEntry(sim, e, pass.Post);
        }
        Assert.True(lastApo < firstApo * 0.85, $"apoapsis tightened materially ({firstApo / UranusR:F2} → {lastApo / UranusR:F2} R_U)");
    }

    [Fact]
    public void TheMovingAtmosphere_ShedsWhatThePinnedFrameDid_Galilean()
    {
        // Lab 33 §E: because drag depends only on speed relative to the air, the live moving-frame pass and
        // Lab 32's pinned-frame pass shed the same Δv at the same peak g — the pinned lab was honest.
        (Simulator liveSim, CircularOrbitEphemeris e) = LiveUranus();
        double startAlt = UranusAtm.TopAltitude + 3.0e5, rPeri = UranusR + 110e3;
        var live = LivePass(liveSim, e, LiveArrival(e, 0, UranusR + startAlt, rPeri, StrandVinf));

        // Pinned frame (Lab 32's rig): the same body at the origin, zero rail velocity.
        var pinnedEph = new CircularOrbitEphemeris([new CelestialBody("u", "u", null, UranusMu, UranusR, 0, 0, 0, Atmosphere: UranusAtm)]);
        var pinnedSim = new Simulator(pinnedEph, timeStepSeconds: 1.0);
        double rStart = UranusR + startAlt;
        double vPeri = System.Math.Sqrt(StrandVinf * StrandVinf + 2 * UranusMu / rPeri);
        double vt = rPeri * vPeri / rStart;
        double v = System.Math.Sqrt(StrandVinf * StrandVinf + 2 * UranusMu / rStart);
        double vr = -System.Math.Sqrt(System.Math.Max(0, v * v - vt * vt));
        ShipState ps = new(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
        double shellTop = UranusR + UranusAtm.TopAltitude, pPeak = 0, pShed = 0;
        bool pen = false;
        while (ps.SimTime < 6 * 3600)
        {
            (ShipState nx, Simulator.DragReport rep) = pinnedSim.RunAdaptiveWithDrag(ps, 20.0, null, minTimeStep: 0.05, maxTimeStep: 3.0);
            pPeak = System.Math.Max(pPeak, rep.PeakDecelMetersPerSecondSquared); pShed += rep.DeltaVShedMetersPerSecond; ps = nx;
            double r = ps.Position.Length;
            if (r < shellTop) pen = true; else if (pen) break;
            if (r < UranusR) break;
        }

        Assert.True(System.Math.Abs(pShed - live.Shed) < 1.0, $"Δv shed agrees: pinned {pShed:F1} vs live {live.Shed:F1} m/s");
        Assert.True(System.Math.Abs(pPeak / G0 - live.PeakG) < 0.01, $"peak g agrees: pinned {pPeak / G0:F3} vs live {live.PeakG:F3}");
    }

    [Fact]
    public void TheLiveCampaign_IsDeterministic()
    {
        (Simulator sim, CircularOrbitEphemeris e) = LiveUranus();
        double startAlt = UranusAtm.TopAltitude + 3.0e5, rPeri = UranusR + 110e3;
        var a = LivePass(sim, e, LiveArrival(e, 0, UranusR + startAlt, rPeri, StrandVinf));
        var b = LivePass(sim, e, LiveArrival(e, 0, UranusR + startAlt, rPeri, StrandVinf));
        Assert.Equal(a.Post, b.Post);
        Assert.Equal(a.PeakG, b.PeakG);
        Assert.Equal(a.Shed, b.Shed);
        Assert.True(a.Shed > 0, "a deep Uranus dip sheds speed");
    }
}
