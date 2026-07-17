namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 32 — Aerocapture: the ice giant's haze is the free brake. See
/// labs/32-aerocapture-at-the-ice-giant/README.md and Probe.cs. Every gate flies real trajectories
/// through <see cref="Simulator"/> in a planet-centered (body-at-origin) frame, reusing lab 22's Core
/// drag unchanged, so the two-body energy is clean and the drag physics is isolated. The lab's
/// headline findings are pinned here: the corridor CLOSES above a critical speed, one pass cannot
/// capture the 29.8 km/s stranding, the thick shells (Titan/Venus) grab only over-g, and the
/// aerocapture bill is one pulse inside the owner's tank while the pure-propulsive bill is not.
/// </summary>
public class Lab32AerocaptureTests
{
    // Uranus/Neptune constants MIRROR scenarios/sol.json; the shells are the lab's PROPOSAL (Probe.cs).
    private const double UranusMu = 5.793939e15;
    private const double UranusR = 2.5362e7;
    private const double TitanMu = 8.9781e12;
    private const double TitanR = 2.575e6;

    private static Atmosphere UranusAtm => new(RefDensity: 1.4e-5, ScaleHeight: 1.2e5, TopAltitude: 1.0e6);
    private static Atmosphere TitanAtm => new(RefDensity: 5.3, ScaleHeight: 4.0e4, TopAltitude: 3.0e5);

    private const double G0 = 9.80665;
    private const double GBudget = Atmosphere.SailHoleDecelG; // 3 g

    private static Simulator MakeSim(double mu, double radius, Atmosphere? atm) =>
        new(new CircularOrbitEphemeris([new CelestialBody("b", "b", null, mu, radius, 0, 0, 0, Atmosphere: atm)]),
            timeStepSeconds: 1.0);

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

    private static double Apoapsis(ShipState s, double mu)
    {
        double e0 = Energy(s, mu);
        if (e0 >= 0) return double.PositiveInfinity;
        double a = -mu / (2 * e0);
        double h = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
        double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * h * h / (mu * mu)));
        return a * (1 + ecc);
    }

    // Fly one pass (entry above the shell, out the far side or into the surface), with the probe's
    // trapped-orbit guard: a bound orbit whose apoapsis is below the shell top can never climb clear —
    // it is a landing, not a capture, so the pass ends there rather than integrating the decay.
    private static (ShipState Post, double PeakG, double DvShed, bool Crashed) FlyPass(
        Simulator sim, ShipState entry, double bodyRadius, double topAltitude, double mu)
    {
        double shellTop = bodyRadius + topAltitude;
        double peak = 0, shed = 0;
        ShipState s = entry;
        bool entered = false, crashed = false;
        while (s.SimTime - entry.SimTime < 3 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) = sim.RunAdaptiveWithDrag(s, 20.0, null, minTimeStep: 0.05, maxTimeStep: 2.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            shed += rep.DeltaVShedMetersPerSecond;
            s = next;
            double r = s.Position.Length;
            if (r < bodyRadius) { crashed = true; break; }
            if (r < shellTop)
            {
                entered = true;
                if (Energy(s, mu) < 0 && Apoapsis(s, mu) < shellTop) { crashed = true; break; }
            }
            else if (entered) break;
        }
        return (s, peak / G0, shed, crashed);
    }

    [Fact]
    public void RegressionGate_UranusProposalShell_ByteIdenticalToVacuum_WhenNeverEntered()
    {
        // A flyby whose periapsis (5000 km) stays FAR above the 1000 km shell: the proposed atmosphere is
        // present but never entered, so the flight must be bit-identical to the same body with no air.
        var vacuum = MakeSim(UranusMu, UranusR, atm: null);
        var withAir = MakeSim(UranusMu, UranusR, UranusAtm);
        ShipState entry = Arrival(UranusMu, UranusR + 1.5e7, UranusR + 5.0e6, 6000);

        ShipState a = vacuum.RunAdaptive(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        ShipState b = withAir.RunAdaptive(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        Assert.Equal(a, b); // record-struct equality: bit-identical

        (_, Simulator.DragReport rep) = withAir.RunAdaptiveWithDrag(entry, 4 * 3600, null, minTimeStep: 0.1, maxTimeStep: 60);
        Assert.Equal(0.0, rep.DeltaVShedMetersPerSecond);
        Assert.Null(rep.DominantBodyId);
    }

    [Fact]
    public void TheStranding_HasNoSafeSinglePassCapture_TheCorridorIsClosed()
    {
        // Section B's punchline: at v_inf = 29.8 km/s, EVERY depth either skips out (still hyperbolic,
        // survivable) or is over the 3 g line — none both captures AND survives. The corridor is closed.
        var uranus = MakeSim(UranusMu, UranusR, UranusAtm);
        double start = UranusR + UranusAtm.TopAltitude + 3.0e5;
        bool anySafeCapture = false;
        for (double altKm = 20; altKm <= 900; altKm += 20)
        {
            var pass = FlyPass(uranus, Arrival(UranusMu, start, UranusR + altKm * 1000, 29800), UranusR, UranusAtm.TopAltitude, UranusMu);
            bool captured = !pass.Crashed && Energy(pass.Post, UranusMu) < 0;
            if (captured && pass.PeakG <= GBudget) anySafeCapture = true;
        }
        Assert.False(anySafeCapture, "at 29.8 km/s no single pass both captures and stays under 3 g");
    }

    [Fact]
    public void ASlowArrival_ReopensASafeCorridor_SpeedIsTheGate()
    {
        // The other half of Section B: halve-ish the speed and a safe single-pass corridor exists again.
        // Speed, not aim depth, is the gate — the corridor closes with arrival speed.
        var uranus = MakeSim(UranusMu, UranusR, UranusAtm);
        double start = UranusR + UranusAtm.TopAltitude + 3.0e5;
        bool anySafeCapture = false;
        for (double altKm = 20; altKm <= 900; altKm += 20)
        {
            var pass = FlyPass(uranus, Arrival(UranusMu, start, UranusR + altKm * 1000, 6000), UranusR, UranusAtm.TopAltitude, UranusMu);
            if (!pass.Crashed && Energy(pass.Post, UranusMu) < 0 && pass.PeakG <= GBudget) anySafeCapture = true;
        }
        Assert.True(anySafeCapture, "a 6 km/s arrival has a safe single-pass corridor");
    }

    [Fact]
    public void OneFreePass_ShedsFarLessThanCaptureNeeds_AtTheStranding()
    {
        // Section C1: the deepest pass under 3 g sheds only a few km/s, far short of the ~15 km/s needed
        // to drop the periapsis speed below local escape. One pass is not enough — a bridge is mandatory.
        var uranus = MakeSim(UranusMu, UranusR, UranusAtm);
        double start = UranusR + UranusAtm.TopAltitude + 3.0e5;
        double bestFree = 0, rPeriBest = 0;
        for (double altKm = 40; altKm <= 900; altKm += 5)
        {
            double rPeri = UranusR + altKm * 1000;
            var pass = FlyPass(uranus, Arrival(UranusMu, start, rPeri, 29800), UranusR, UranusAtm.TopAltitude, UranusMu);
            if (!pass.Crashed && pass.PeakG <= GBudget && pass.DvShed > bestFree) { bestFree = pass.DvShed; rPeriBest = rPeri; }
        }
        double vPeri = Math.Sqrt(29800.0 * 29800.0 + 2 * UranusMu / rPeriBest);
        double vEscPeri = Math.Sqrt(2 * UranusMu / rPeriBest);
        double needed = vPeri - vEscPeri;
        Assert.True(bestFree > 0, "some free pass sheds a positive amount");
        Assert.True(bestFree < needed, $"the free shed ({bestFree:F0}) is far short of the capture need ({needed:F0})");
        Assert.True(needed - bestFree > 5000, "the propellant bridge is multiple km/s — not a rounding gap");
    }

    [Fact]
    public void PropulsiveCaptureIsImpossible_ButAerocaptureAssistedFitsTheTank()
    {
        // Section D in game units: a pure-propulsive capture (shed the whole ~15.3 km/s) costs more than
        // the owner's 32 pulses; the air pays a chunk so the remaining bridge fits. PulsesFor is the
        // same kernel the autopilot spends with — one pulse = 1% of world speed as Delta-v.
        double vPeri = Math.Sqrt(29800.0 * 29800.0 + 2 * UranusMu / (UranusR + 110e3));
        double vEsc = Math.Sqrt(2 * UranusMu / (UranusR + 110e3));
        double dvToCapture = vPeri - vEsc;

        int propulsive = OrbitRule.PulsesFor(dvToCapture, vPeri);
        Assert.True(propulsive > 32, $"pure-propulsive capture ({propulsive} pulses) exceeds the 32-pulse tank");

        // Give the air a free 4 km/s (the measured 3 g pass) and the bridge must both drop AND fit.
        int bridge = OrbitRule.PulsesFor(dvToCapture - 4000, vPeri);
        Assert.True(bridge < propulsive, "the air-assisted bridge costs fewer pulses than the full burn");
        Assert.True(bridge <= 32, $"the aerocapture-assisted bridge ({bridge} pulses) fits the tank");
    }

    [Fact]
    public void BoundPass_DropsApoapsis_AndStrictlyRemovesEnergy()
    {
        // Section C2: once bound, a free gentle pass strictly lowers energy and apoapsis (never adds).
        var uranus = MakeSim(UranusMu, UranusR, UranusAtm);
        double start = UranusR + UranusAtm.TopAltitude + 3.0e5;
        // seed a wide bound orbit with a gentle 300 km periapsis (probe's C2 seed shape)
        double rPeri = UranusR + 300e3, rApo = 60 * UranusR, a = (rPeri + rApo) / 2;
        double vPeri = Math.Sqrt(UranusMu * (2 / rPeri - 1 / a)), h = rPeri * vPeri;
        double v = Math.Sqrt(UranusMu * (2 / start - 1 / a)), vt = h / start, vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
        ShipState entry = new(new Vector2d(start, 0), new Vector2d(vr, vt), 0);

        Assert.True(Energy(entry, UranusMu) < 0, "seed is bound");
        var pass = FlyPass(uranus, entry, UranusR, UranusAtm.TopAltitude, UranusMu);
        Assert.False(pass.Crashed, "a gentle 300 km pass does not auger in");
        Assert.True(Energy(pass.Post, UranusMu) < Energy(entry, UranusMu), "energy strictly decreases");
        Assert.True(Apoapsis(pass.Post, UranusMu) < rApo, "apoapsis drops on the free pass");
        Assert.True(pass.PeakG < GBudget, "the gentle tightening pass stays under the g line");
    }

    [Fact]
    public void DeeperPeriapsis_ShedsMoreSpeed_Monotonic_AtUranus()
    {
        var uranus = MakeSim(UranusMu, UranusR, UranusAtm);
        double start = UranusR + UranusAtm.TopAltitude + 3.0e5;
        double last = -1;
        foreach (double altKm in new[] { 400.0, 300.0, 220.0, 160.0 }) // shallow -> deep
        {
            var pass = FlyPass(uranus, Arrival(UranusMu, start, UranusR + altKm * 1000, 29800), UranusR, UranusAtm.TopAltitude, UranusMu);
            Assert.True(pass.DvShed > last, $"depth {altKm} km sheds {pass.DvShed:F0}, not more than the shallower pass ({last:F0})");
            last = pass.DvShed;
        }
    }

    [Fact]
    public void TitanThickShell_GrabsOnlyOverG_AeroLandsNotGentle()
    {
        // Section E: Titan's sol.json shell (5.3 kg/m^3) is so thick that any entering pass grabs the
        // ship — but at brutal g (the probe measures ~12.8 g at the shallowest grab). There is no gentle
        // capture-to-orbit corridor: it is a landing atmosphere, the anti-training-wheels.
        var titan = MakeSim(TitanMu, TitanR, TitanAtm);
        double start = TitanR + TitanAtm.TopAltitude + 3.0e5;
        bool anySafeOrbit = false;
        double shallowestGrabG = double.NaN;
        for (double altKm = TitanAtm.TopAltitude / 1000 - 10; altKm >= 5; altKm -= 5)
        {
            var pass = FlyPass(titan, Arrival(TitanMu, start, TitanR + altKm * 1000, 2000), TitanR, TitanAtm.TopAltitude, TitanMu);
            bool bound = pass.Crashed || Energy(pass.Post, TitanMu) < 0;
            double shellTop = TitanR + TitanAtm.TopAltitude;
            bool orbit = !pass.Crashed && Energy(pass.Post, TitanMu) < 0 && Apoapsis(pass.Post, TitanMu) > shellTop;
            if (bound && double.IsNaN(shallowestGrabG)) shallowestGrabG = pass.PeakG;
            if (orbit && pass.PeakG <= GBudget) anySafeOrbit = true;
        }
        Assert.False(anySafeOrbit, "Titan's thick shell offers no gentle sub-3g capture-to-orbit corridor");
        Assert.True(shallowestGrabG > GBudget, $"even the shallowest Titan grab is over the g line ({shallowestGrabG:F1} g)");
    }

    [Fact]
    public void RunAdaptiveWithDrag_IsDeterministic_AtUranus()
    {
        var sim = MakeSim(UranusMu, UranusR, UranusAtm);
        ShipState entry = Arrival(UranusMu, UranusR + UranusAtm.TopAltitude + 3.0e5, UranusR + 110e3, 29800);
        (ShipState s1, Simulator.DragReport r1) = sim.RunAdaptiveWithDrag(entry, 2 * 3600, null, minTimeStep: 0.05, maxTimeStep: 2.0);
        (ShipState s2, Simulator.DragReport r2) = sim.RunAdaptiveWithDrag(entry, 2 * 3600, null, minTimeStep: 0.05, maxTimeStep: 2.0);
        Assert.Equal(s1, s2);
        Assert.Equal(r1, r2);
        Assert.True(r1.DeltaVShedMetersPerSecond > 0, "a deep Uranus dip sheds speed");
    }
}
