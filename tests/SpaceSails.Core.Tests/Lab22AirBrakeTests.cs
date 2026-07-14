namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 22 — The air brake (atmospheric drag in Core: the skim/skip flight assists).
/// See labs/22-the-air-brake/README.md and Probe.cs for the lesson. Every gate flies real
/// trajectories through <see cref="Simulator"/> in a planet-centered (body-at-origin) frame, so the
/// two-body energy is clean and the drag physics is isolated from any solar perturbation.
///
/// The sacred one is <see cref="RegressionGate_NoAtmosphere_ByteIdenticalToVacuum"/>: with no ship
/// inside any shell, an atmosphere-bearing ephemeris must fly bit-identically to a vacuum one.
/// </summary>
public class Lab22AirBrakeTests
{
    // Jupiter/Earth constants + shells MIRROR scenarios/sol.json and the probe.
    private const double JupiterMu = 1.26686534e17;
    private const double JupiterR = 6.9911e7;
    private const double EarthMu = 3.986004418e14;
    private const double EarthR = 6.371e6;

    private static Atmosphere JupiterAtm => new(RefDensity: 4.0e-6, ScaleHeight: 3.0e4, TopAltitude: 4.0e5);
    private static Atmosphere EarthAtm => new(RefDensity: 1.2, ScaleHeight: 8.0e3, TopAltitude: 1.4e5);

    private static Simulator MakeSim(double mu, double radius, Atmosphere? atm) =>
        new(new CircularOrbitEphemeris([new CelestialBody("b", "b", null, mu, radius, 0, 0, 0, Atmosphere: atm)]),
            timeStepSeconds: 1.0);

    // Incoming hyperbolic state on the +x axis at rStart whose vacuum periapsis is rPeri (probe's Arrival).
    private static ShipState Arrival(double mu, double rStart, double rPeri, double vInf)
    {
        double vPeri = Math.Sqrt(vInf * vInf + 2 * mu / rPeri);
        double h = rPeri * vPeri;
        double v = Math.Sqrt(vInf * vInf + 2 * mu / rStart);
        double vt = h / rStart;
        double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
        return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
    }

    private static double Energy(ShipState s, double mu) => s.Velocity.LengthSquared / 2.0 - mu / s.Position.Length;

    // Fly one atmosphere pass (entry above the shell, out the far side), returning the post state and
    // the drag report — the probe's FlyPass, condensed.
    private static (ShipState Post, Simulator.DragReport Report) FlyPass(
        Simulator sim, ShipState entry, double bodyRadius, double topAltitude)
    {
        double shellTop = bodyRadius + topAltitude;
        double peak = 0, shed = 0, minAlt = double.PositiveInfinity;
        string? body = null;
        ShipState s = entry;
        bool entered = false;
        while (s.SimTime - entry.SimTime < 6 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) =
                sim.RunAdaptiveWithDrag(s, 30.0, null, minTimeStep: 0.1, maxTimeStep: 2.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            shed += rep.DeltaVShedMetersPerSecond;
            if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
            if (rep.DominantBodyId is not null) body = rep.DominantBodyId;
            s = next;
            double r = s.Position.Length;
            if (r < bodyRadius) break;
            if (r < shellTop) entered = true;
            else if (entered) break;
        }
        return (s, new Simulator.DragReport(peak, shed, 0, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt, s.Velocity.Length, body));
    }

    [Fact]
    public void RegressionGate_NoAtmosphere_ByteIdenticalToVacuum()
    {
        // A flyby whose periapsis (2000 km) stays FAR above Jupiter's 400 km shell: the atmosphere is
        // present but never entered, so the drag term must contribute exactly nothing and the whole
        // flight must be bit-identical to the same body with no atmosphere at all.
        var vacuum = MakeSim(JupiterMu, JupiterR, atm: null);
        var withAir = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        ShipState entry = Arrival(JupiterMu, JupiterR + 6.0e6, JupiterR + 2.0e6, 6000);

        ShipState a = vacuum.RunAdaptive(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        ShipState b = withAir.RunAdaptive(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        Assert.Equal(a, b); // record-struct equality: bit-identical, not approximately equal

        // And the drag report of that untouched pass is all-zero with no dominant body.
        (_, Simulator.DragReport rep) = withAir.RunAdaptiveWithDrag(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        Assert.Equal(0.0, rep.PeakDecelMetersPerSecondSquared);
        Assert.Equal(0.0, rep.DeltaVShedMetersPerSecond);
        Assert.Null(rep.DominantBodyId);
    }

    [Fact]
    public void RunAdaptiveWithDrag_TrajectoryMatchesRunAdaptive_AndIsDeterministic()
    {
        // The reporting overload must fly the exact same trajectory as RunAdaptive (it only observes),
        // and two runs must be bit-identical in both state and report.
        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        ShipState entry = Arrival(JupiterMu, JupiterR + 7.0e5, JupiterR + 80e3, 5500);

        ShipState plain = sim.RunAdaptive(entry, 2 * 3600, null, minTimeStep: 0.1, maxTimeStep: 2.0);
        (ShipState reported, Simulator.DragReport r1) = sim.RunAdaptiveWithDrag(entry, 2 * 3600, null, minTimeStep: 0.1, maxTimeStep: 2.0);
        (ShipState reported2, Simulator.DragReport r2) = sim.RunAdaptiveWithDrag(entry, 2 * 3600, null, minTimeStep: 0.1, maxTimeStep: 2.0);

        Assert.Equal(plain, reported);     // observing drag never perturbs the flight
        Assert.Equal(reported, reported2);
        Assert.Equal(r1, r2);              // the DragReport (PR-I's API) is deterministic
        Assert.True(r1.DeltaVShedMetersPerSecond > 0, "an 80 km Jupiter dip must shed speed");
        Assert.Equal("b", r1.DominantBodyId);
    }

    [Fact]
    public void DeeperPeriapsis_ShedsMoreSpeed_Monotonic()
    {
        // The corridor's core sanity: a deeper dip always sheds more Δv (denser air, longer column).
        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        double bStart = JupiterR + JupiterAtm.TopAltitude + 3.0e5;
        double last = -1;
        foreach (double altKm in new[] { 200.0, 130.0, 80.0, 40.0 }) // shallow -> deep
        {
            (_, Simulator.DragReport rep) = FlyPass(sim, Arrival(JupiterMu, bStart, JupiterR + altKm * 1000, 5500), JupiterR, JupiterAtm.TopAltitude);
            Assert.True(rep.DeltaVShedMetersPerSecond > last,
                $"depth {altKm} km sheds {rep.DeltaVShedMetersPerSecond:F0} m/s, not more than the shallower pass ({last:F0})");
            last = rep.DeltaVShedMetersPerSecond;
        }
    }

    [Fact]
    public void DragPass_StrictlyDecreasesOrbitalEnergy()
    {
        // A drag pass can only remove energy — never add it.
        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        double bStart = JupiterR + JupiterAtm.TopAltitude + 3.0e5;
        ShipState entry = Arrival(JupiterMu, bStart, JupiterR + 60e3, 5500);
        (ShipState post, _) = FlyPass(sim, entry, JupiterR, JupiterAtm.TopAltitude);
        Assert.True(Energy(post, JupiterMu) < Energy(entry, JupiterMu),
            "orbital energy must strictly decrease across a drag pass");
    }

    [Fact]
    public void ShallowHyperbolicPass_ExitsSlowerButStillEscapes()
    {
        // The skip: a shallow, fast pass sheds a little and is thrown back out — energy stays > 0
        // (still hyperbolic) but the exit speed at the entry radius is lower than the entry speed.
        var sim = MakeSim(EarthMu, EarthR, EarthAtm);
        double eStart = EarthR + EarthAtm.TopAltitude + 6.0e4;
        ShipState entry = Arrival(EarthMu, eStart, EarthR + 110e3, 1500); // shallow (110 km) at Apollo-return speed
        (ShipState post, Simulator.DragReport rep) = FlyPass(sim, entry, EarthR, EarthAtm.TopAltitude);

        Assert.True(rep.DeltaVShedMetersPerSecond > 0, "a shallow pass still sheds some speed");
        Assert.True(Energy(post, EarthMu) > 0, "the shallow skip must remain hyperbolic (skips back out)");
        // Compare speeds at a common radius (post state, near the shell top on the way out).
        double vEntryAtR = Math.Sqrt(2 * (Energy(entry, EarthMu) + EarthMu / post.Position.Length));
        Assert.True(post.Velocity.Length < vEntryAtR, "exit speed must be lower than the pre-drag speed at the same radius");
    }

    [Fact]
    public void DeepPass_CrossesTheDamageLine()
    {
        // The damage-line proxy PR-I consumes: a deep dip drives peak deceleration well past a
        // few g, and shallow dips do not — the DragReport.PeakDecelG the gauge reads.
        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        double bStart = JupiterR + JupiterAtm.TopAltitude + 3.0e5;

        (_, Simulator.DragReport shallow) = FlyPass(sim, Arrival(JupiterMu, bStart, JupiterR + 130e3, 5500), JupiterR, JupiterAtm.TopAltitude);
        (_, Simulator.DragReport deep) = FlyPass(sim, Arrival(JupiterMu, bStart, JupiterR + 5e3, 5500), JupiterR, JupiterAtm.TopAltitude);

        Assert.True(shallow.PeakDecelG < 1.0, $"a shallow 130 km dip stays gentle ({shallow.PeakDecelG:F2} g)");
        Assert.True(deep.PeakDecelG > 3.0, $"a 5 km graze crosses the damage line ({deep.PeakDecelG:F2} g)");
        // PeakDecelG is just the m/s^2 peak in standard gravities.
        Assert.Equal(deep.PeakDecelMetersPerSecondSquared / 9.80665, deep.PeakDecelG, precision: 9);
    }

    [Fact]
    public void SailHoleDamageLine_IsThePromotedCoreConstant_ThreeGravities()
    {
        // The 3 g damage line used to live in the lab probe; it is now a single Core constant on
        // Atmosphere that the lab, the game's corridor gauge, and the live consequence all share. Pin
        // its value AND that it brackets the corridor the DragReport measures: a deep 5 km Jupiter graze
        // crosses it (holes the sail), a shallow 130 km dip stays well under it (safe braking).
        Assert.Equal(3.0, Atmosphere.SailHoleDecelG);

        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        double bStart = JupiterR + JupiterAtm.TopAltitude + 3.0e5;
        (_, Simulator.DragReport shallow) = FlyPass(sim, Arrival(JupiterMu, bStart, JupiterR + 130e3, 5500), JupiterR, JupiterAtm.TopAltitude);
        (_, Simulator.DragReport deep) = FlyPass(sim, Arrival(JupiterMu, bStart, JupiterR + 5e3, 5500), JupiterR, JupiterAtm.TopAltitude);

        Assert.True(shallow.PeakDecelG < Atmosphere.SailHoleDecelG, "a gentle corridor dip does not hole the sail");
        Assert.True(deep.PeakDecelG >= Atmosphere.SailHoleDecelG, "a deep graze holes the sail");
    }

    [Fact]
    public void FuelOutCapture_TurnsHyperbolicArrivalBound_WithNoBurns()
    {
        // The SGU move: a hyperbolic arrival (E > 0) becomes bound (E < 0) after one deep-enough
        // skim, spending zero propellant. (Single pass; the full ledger lives in the probe.)
        var sim = MakeSim(JupiterMu, JupiterR, JupiterAtm);
        double bStart = JupiterR + JupiterAtm.TopAltitude + 3.0e5;
        ShipState entry = Arrival(JupiterMu, bStart, JupiterR + 72e3, 6000);
        Assert.True(Energy(entry, JupiterMu) > 0, "arrival is hyperbolic");
        (ShipState post, _) = FlyPass(sim, entry, JupiterR, JupiterAtm.TopAltitude);
        Assert.True(Energy(post, JupiterMu) < 0, "one skim captures the arrival into a bound orbit");
    }

    [Fact]
    public void Density_IsExponential_AndZeroOutsideTheShell()
    {
        Atmosphere atm = JupiterAtm;
        Assert.Equal(0.0, atm.DensityAt(-10));                 // below the surface: nothing
        Assert.Equal(0.0, atm.DensityAt(0));                   // the surface itself is the closed lower bound: zero
        Assert.True(atm.DensityAt(1) > 0 && atm.DensityAt(1) < atm.RefDensity); // just inside: near reference
        Assert.Equal(0.0, atm.DensityAt(atm.TopAltitude));     // at the shell top: exactly zero
        Assert.Equal(0.0, atm.DensityAt(atm.TopAltitude + 1)); // above: zero
        // One scale height up is a factor of 1/e.
        double near = atm.DensityAt(1);
        Assert.Equal(near / Math.E, atm.DensityAt(1 + atm.ScaleHeight), precision: 12);
    }
}
