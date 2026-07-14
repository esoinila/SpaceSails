// Lab 01 — Falling is orbiting
//
// Teaching voice: an orbit is not a magic circle a spaceship glues itself to. It is a fall
// that keeps missing the ground because it has sideways velocity. Newton's cannonball is not
// a metaphor — it is literally what this probe computes. Drop a ship with zero velocity at
// 1 AU and it falls straight into the Sun (a degenerate, zero-angular-momentum "ellipse").
// Give it exactly circular speed sideways and the same free-fall acceleration curves the
// path into a closed circle. Same physics, same integrator, different initial condition.
//
// Curtis, "Orbital Mechanics for Engineering Students," ch. 2 derives vis-viva
//     v^2 = mu * (2/r - 1/a)
// from the two-body equation of motion and energy conservation. Curtis does this with
// calculus on paper. We do it by actually running SpaceSails.Core's own fixed-step
// integrator — the same code the game flies ships with — and checking whether the numbers
// it produces agree with the closed form. They should, to the integrator's truncation error,
// and by how much is the whole point of this lesson.
//
// IRONCLAD RULE: every number below was produced by running this probe. If you change the
// code, the printed numbers in labs/01-falling-is-orbiting/README.md go stale — rerun and
// re-paste, don't hand-edit them.

using SpaceSails.Core;
using SpaceSails.LabViz;

// Sol's own numbers (scenarios/sol.json): the Sun's mu and Earth's orbit radius define "1 AU"
// and "circular speed" for this whole probe, so results transfer straight into the game.
const double SunMu = 1.32712440018e20;   // m^3/s^2
const double AU = 1.496e11;              // m, Earth's orbit radius in sol.json
const double SunBodyRadius = 6.9634e8;   // m — inside this, Simulator zeroes the force (collision stub)

// The live game's own timestep (docs/m4-spec.md: "real Simulator ship, dt = 1 s behind an
// accumulator"). This is the dt every "computed" number below uses unless a section says
// otherwise.
const double GameDt = 1.0;

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var ephemeris = new CircularOrbitEphemeris([sun]);

double VisVivaSpeed(double r, double a) => Math.Sqrt(SunMu * (2.0 / r - 1.0 / a));

Console.WriteLine("=== Section A: radial free-fall from 1 AU (v0 = 0) ===");
Console.WriteLine();
Console.WriteLine("A ship at rest has zero angular momentum, so its 'orbit' is the limiting");
Console.WriteLine("case of an ellipse squashed to a line: semi-major axis a = r0/2 exactly,");
Console.WriteLine("because apoapsis = r0 and periapsis = 0 by construction (Curtis ch. 2, the");
Console.WriteLine("radial/rectilinear trajectory). Vis-viva with that fixed a predicts speed");
Console.WriteLine("at any radius reached along the way; we integrate the actual fall and check.");
Console.WriteLine();
RunFreefallCheck(dt: GameDt, label: "dt = 1 s (the game's live dt)");
Console.WriteLine();
RunFreefallCheck(dt: GameDt * 2, label: "dt = 2 s (BREAK IT: doubled dt)");

Console.WriteLine();
Console.WriteLine("=== Section B: circular orbit at 1 AU ===");
Console.WriteLine();
Console.WriteLine("Now give the ship sideways speed v_circ = sqrt(mu/r0) instead of zero. The");
Console.WriteLine("same fall now perpetually misses the Sun: a = r0 exactly (a circle IS an");
Console.WriteLine("ellipse with e = 0). We sample speed a quarter, half, and full orbit later");
Console.WriteLine("and compare against vis-viva using the *initial* a = r0 — any gap is the");
Console.WriteLine("integrator's own energy drift, not a modeling error.");
Console.WriteLine();
RunCircularCheck();

Console.WriteLine();
Console.WriteLine("=== Break-it: the +/-10% pulse, before lesson 4 even gets there ===");
Console.WriteLine();
Console.WriteLine("The game only ever changes ship speed by +/-10% multiplicative pulses. Do");
Console.WriteLine("that to an exactly-circular ship and watch the circle become an ellipse.");
Console.WriteLine("We print both the closed-form (vis-viva + angular momentum) prediction of");
Console.WriteLine("periapsis/apoapsis AND the number the integrator itself finds by scanning");
Console.WriteLine("radius over one full orbit — they should agree.");
Console.WriteLine();
RunPulseBreakIt(+0.10, "speed x1.10 (accelerate pulse)");
Console.WriteLine();
RunPulseBreakIt(-0.10, "speed x0.90 (decelerate pulse)");

// ---- Seeing it: `-- --viz` draws the computed orbit in the game's visual language --------
// Every line below is gated behind LabViz.Wants(args), so without the flag stdout stays
// byte-identical. We re-run the Section B circular orbit through the same integrator purely to
// observe it — one full revolution of the fall that keeps missing the Sun — and hand the
// samples to the viewer as a ghost path. This ~6-line block is the pattern other labs copy.
if (LabViz.Wants(args))
{
    var viz = new VizScene("lab01-falling-is-orbiting", "Lab 01 — Falling is orbiting",
        "Circular speed at 1 AU: a fall that perpetually misses the Sun");
    viz.AddBodies(ephemeris.Bodies);

    // Circular speed straight from Core's OrbitRule (sqrt(mu/r)), so the picture uses the same helper
    // the game's orbit-insertion code does rather than re-spelling the formula.
    var release = new ShipState(new Vector2d(AU, 0), new Vector2d(0, OrbitRule.CircularSpeed(sun, AU)), 0);
    double period = 2 * Math.PI * Math.Sqrt(AU * AU * AU / SunMu);
    // One full revolution at the adaptive step's 3600 s clamp is ~8767 samples — past the default
    // 8192 cap, which would truncate the drawn circle ~24° short of closing. Lift the cap so the last
    // sample reaches the period and the ring closes.
    var orbit = new Simulator(ephemeris, GameDt).ProjectAdaptive(release, null, period, maxSamples: 16_384);
    viz.AddPath("circular orbit @ 1 AU", orbit, VizColors.Trajectory, ghost: true);
    viz.AddMarker(0, release.Position, "release @ 1 AU (v = v_circ)", MarkerKinds.Event);

    LabViz.Show(viz, args);
}

// ---- Section A implementation --------------------------------------------------------

void RunFreefallCheck(double dt, string label)
{
    double r0 = AU;
    double aFall = r0 / 2.0; // degenerate ellipse: apoapsis r0, periapsis 0
    double[] checkpoints = [0.75 * AU, 0.50 * AU, 0.25 * AU];

    var simulator = new Simulator(ephemeris, dt);
    var state = new ShipState(new Vector2d(r0, 0), Vector2d.Zero, 0);

    Console.WriteLine(label);
    Console.WriteLine($"{"radius (AU)",-14}{"v_computed (m/s)",-20}{"v_vis-viva (m/s)",-20}{"rel. error",-14}{"sim days",-10}");

    int checkpointIndex = 0;
    double prevRadius = state.Position.Length;
    ShipState prevState = state;
    while (checkpointIndex < checkpoints.Length)
    {
        state = simulator.Step(state);
        double radius = state.Position.Length;
        double target = checkpoints[checkpointIndex];

        if (prevRadius > target && radius <= target)
        {
            // Linearly interpolate to the exact checkpoint radius between the two straddling
            // steps, so the comparison isn't polluted by "missed the radius by one dt".
            double f = (prevRadius - target) / (prevRadius - radius);
            double vPrev = prevState.Velocity.Length;
            double vNow = state.Velocity.Length;
            double vComputed = vPrev + f * (vNow - vPrev);
            double simTime = prevState.SimTime + f * dt;

            double vVisViva = VisVivaSpeed(target, aFall);
            double relError = Math.Abs(vComputed - vVisViva) / vVisViva;

            Console.WriteLine(
                $"{target / AU,-14:F2}{vComputed,-20:F6}{vVisViva,-20:F6}{relError,-14:E3}{simTime / 86400.0,-10:F2}");

            checkpointIndex++;
        }

        prevRadius = radius;
        prevState = state;
    }
}

// ---- Section B implementation --------------------------------------------------------

void RunCircularCheck()
{
    double r0 = AU;
    double vCirc = Math.Sqrt(SunMu / r0);
    double aCirc = r0; // exact by construction

    var simulator = new Simulator(ephemeris, GameDt);
    var state = new ShipState(new Vector2d(r0, 0), new Vector2d(0, vCirc), 0);

    // Period from the same fixed a (Kepler's third law falls straight out of vis-viva plus
    // the vis-viva-consistent orbit equation); used only to pick sample times.
    double period = 2 * Math.PI * Math.Sqrt(r0 * r0 * r0 / SunMu);
    double[] fractions = [0.25, 0.50, 1.00];

    Console.WriteLine($"{"orbit fraction",-16}{"v_computed (m/s)",-20}{"v_vis-viva (m/s)",-20}{"rel. error",-14}{"radius (AU)",-12}");

    double elapsed = 0;
    foreach (double fraction in fractions)
    {
        double targetTime = fraction * period;
        state = simulator.Run(state, targetTime - elapsed);
        elapsed = targetTime;

        double radius = state.Position.Length;
        double vComputed = state.Velocity.Length;
        double vVisViva = VisVivaSpeed(radius, aCirc);
        double relError = Math.Abs(vComputed - vVisViva) / vVisViva;

        Console.WriteLine(
            $"{fraction,-16:F2}{vComputed,-20:F6}{vVisViva,-20:F6}{relError,-14:E3}{radius / AU,-12:F6}");
    }
}

// ---- Break-it implementation ----------------------------------------------------------

void RunPulseBreakIt(double pulseFraction, string label)
{
    double r0 = AU;
    double vCirc = Math.Sqrt(SunMu / r0);
    double v = vCirc * (1.0 + pulseFraction);

    // Closed form: specific energy and angular momentum from the pulsed initial state give a
    // and e directly (Curtis ch. 2).
    double energy = v * v / 2.0 - SunMu / r0;
    double a = -SunMu / (2.0 * energy);
    double h = r0 * v; // purely tangential velocity: h = r x v = r0 * v
    double e = Math.Sqrt(Math.Max(0.0, 1.0 - (h * h) / (SunMu * a)));
    double rPeriClosedForm = a * (1 - e);
    double rApoClosedForm = a * (1 + e);

    var simulator = new Simulator(ephemeris, GameDt);
    var state = new ShipState(new Vector2d(r0, 0), new Vector2d(0, v), 0);
    double period = 2 * Math.PI * Math.Sqrt(a * a * a / SunMu);

    double rMin = r0, rMax = r0;
    int steps = (int)Math.Ceiling(period / GameDt);
    for (int i = 0; i < steps; i++)
    {
        state = simulator.Step(state);
        double r = state.Position.Length;
        rMin = Math.Min(rMin, r);
        rMax = Math.Max(rMax, r);
    }

    Console.WriteLine(label);
    Console.WriteLine($"  eccentricity e = {e:F6}, semi-major axis a = {a / AU:F6} AU");
    Console.WriteLine($"  {"",-22}{"periapsis (AU)",-18}{"apoapsis (AU)",-18}");
    Console.WriteLine($"  {"closed form",-22}{rPeriClosedForm / AU,-18:F6}{rApoClosedForm / AU,-18:F6}");
    Console.WriteLine($"  {"integrator scan",-22}{rMin / AU,-18:F6}{rMax / AU,-18:F6}");
}
