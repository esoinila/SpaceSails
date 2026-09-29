namespace SpaceSails.Core;

// #251 · Split from SlingPlanner.cs, moved verbatim: the summary of a flown Δv and every helper the
// solve leans on — the bracket, the pass, the apoapsis, the primary. The constants stay in SlingPlanner.cs.
public static partial class SlingPlanner
{
    /// <summary>
    /// Fly a GIVEN Δv at the burn node and report the same summary fields <see cref="Solve"/>
    /// produces — the pass, the ±window heliocentric speeds, the apoapsis/escape verdict, and the
    /// lever. The desk calls this at the QUANTIZED Δv (after rounding to whole Vector-burn pulses) so
    /// the numbers it shows are the ones the plan will actually fly. Ok is true when the flown Δv
    /// still yields a real pass by the target.
    /// </summary>
    public static Result Summarize(
        Simulator simulator, ICelestialEphemeris ephemeris, Request request, Vector2d deltaV, double? passEpoch = null, int iterations = 0)
    {
        ShipState burn = request.BurnState;
        double t0 = burn.SimTime;
        string target = request.TargetBodyId;
        (Vector2d sunPos, Vector2d sunVel, double sunMu) = Primary(ephemeris, t0);
        double postWindow = request.PostPassWindowSeconds;

        double tCA = passEpoch ?? RefinePass(simulator, ephemeris, burn, target, request.PassEpochEstimate);
        if (double.IsNaN(tCA))
        {
            return Failed("no pass by that body on the plotted course");
        }

        Evaluation nom = Evaluate(simulator, ephemeris, burn, deltaV, target, tCA, postWindow, sunPos, sunVel, sunMu);

        // Lever: perturb the aim by one pulse-quantum along the Δv direction, re-fly, and report the
        // downstream shift at the far end of the pass — the physics of the flyby-as-lever.
        double burnSpeed = (burn.Velocity - sunVel).Length;
        double pulse = request.PulseDeltaV > 0 ? request.PulseDeltaV : Math.Max(1.0, 0.01 * burnSpeed);
        Vector2d dir = deltaV.Length > 1e-6 ? deltaV.Normalized() : (burn.Velocity - sunVel).Normalized();
        Evaluation pert = Evaluate(simulator, ephemeris, burn, deltaV + dir * pulse, target, tCA, postWindow, sunPos, sunVel, sunMu);
        double leverGm = (pert.Downstream - nom.Downstream).Length / 1e9;

        return new Result(
            Ok: true,
            Failure: null,
            DeltaV: deltaV,
            DeltaVMagnitude: deltaV.Length,
            AchievedPassDistance: nom.PassDistance,
            PassEpoch: nom.PassEpoch,
            SpeedBefore: nom.SpeedBefore,
            SpeedAfter: nom.SpeedAfter,
            SpeedGain: nom.SpeedAfter - nom.SpeedBefore,
            Escapes: nom.Escapes,
            ApoapsisAU: nom.ApoapsisAU,
            LeverGm: leverGm,
            Iterations: iterations);
    }

    /// <summary>The last scan index <c>i</c> on the flank (walking from <paramref name="from"/> to
    /// <paramref name="to"/> in <paramref name="dir"/> steps) whose distance brackets
    /// <paramref name="target"/> with its neighbor. Returns (-1,-1) when the flank never reaches it.</summary>
    private static (int Lo, int Hi) FindBracket(double[] dists, double target, int from, int to, int dir)
    {
        for (int i = from; dir > 0 ? i < to : i > to; i += dir)
        {
            int j = i + dir;
            double a = dists[i], b = dists[j];
            if ((a - target) * (b - target) <= 0)
            {
                return dir > 0 ? (i, j) : (j, i);
            }
        }

        return (-1, -1);
    }

    private static Result Failed(string reason) => new(
        Ok: false, Failure: reason, DeltaV: Vector2d.Zero, DeltaVMagnitude: 0,
        AchievedPassDistance: 0, PassEpoch: 0, SpeedBefore: 0, SpeedAfter: 0, SpeedGain: 0,
        Escapes: false, ApoapsisAU: 0, LeverGm: 0, Iterations: 0);

    private readonly record struct Evaluation(
        double PassDistance, double PassEpoch, double SpeedBefore, double SpeedAfter,
        bool Escapes, double ApoapsisAU, Vector2d Downstream);

    /// <summary>Fly Δv at the burn node; measure the pass, the ±window heliocentric speeds (clamped
    /// inside the flown leg — lab 20's lesson), the apoapsis/escape verdict, and the far-end position
    /// used for the lever.</summary>
    private static Evaluation Evaluate(
        Simulator sim, ICelestialEphemeris eph, ShipState burn, Vector2d deltaV,
        string target, double tCA, double postWindow, Vector2d sunPos, Vector2d sunVel, double sunMu)
    {
        double t0 = burn.SimTime;
        var start = new ShipState(burn.Position, burn.Velocity + deltaV, t0);
        double horizon = (tCA - t0) + postWindow;
        // The adaptive stepper auto-refines through the encounter regardless of the max step; the
        // coarse cap only speeds the long deep-space coast. ClosestTo's parabolic refine recovers the
        // sub-sample periapsis, so the pass stays accurate.
        IReadOnlyList<TrajectorySample> path = sim.ProjectAdaptive(
            start, null, horizon, minTimeStep: 30, maxTimeStep: CoarseStep, dynamicalTimeFraction: 1.0 / 48, maxSamples: 8_000);
        (double passDist, double passT) = ClosestTo(eph, target, path, t0);

        double wBefore = Math.Min(90 * Day, (passT - t0) * 0.9);
        double wAfter = Math.Min(90 * Day, ((t0 + horizon) - passT) * 0.9);
        ShipState before = sim.RunAdaptive(start, Math.Max(1.0, (passT - wBefore) - t0), maxTimeStep: CoarseStep);
        ShipState after = sim.RunAdaptive(start, Math.Max(1.0, (passT + wAfter) - t0), maxTimeStep: CoarseStep);

        double speedBefore = (before.Velocity - sunVel).Length;
        double speedAfter = (after.Velocity - sunVel).Length;
        double apo = ApoapsisAU(after, sunPos, sunVel, sunMu, out bool escapes);
        return new Evaluation(passDist, passT, speedBefore, speedAfter, escapes, apo, after.Position);
    }

    private static (double Distance, double PassT) ProjectPass(
        Simulator sim, ICelestialEphemeris eph, ShipState burn, Vector2d deltaV, string target, double tCA, double marginWindow)
    {
        double t0 = burn.SimTime;
        var start = new ShipState(burn.Position, burn.Velocity + deltaV, t0);
        double horizon = (tCA - t0) + marginWindow;
        // Cost bound (the solve runs this ~20× in the scan + Newton, on IL-interpreted WASM over a
        // 22-body ephemeris): a coarser adaptive fraction than the game's 1/64 keeps the near-planet
        // step count down — bracketing/convergence only needs the pass distance to ~0.1 R, which the
        // parabolic refine recovers between the coarse samples. The final summary re-flies fine.
        IReadOnlyList<TrajectorySample> path = sim.ProjectAdaptive(
            start, null, horizon, minTimeStep: 120, maxTimeStep: CoarseStep, dynamicalTimeFraction: 1.0 / 24, maxSamples: 1_500);
        return ClosestTo(eph, target, path, t0);
    }

    private static double PostPassSpeed(
        Simulator sim, ICelestialEphemeris eph, ShipState burn, Vector2d deltaV, string target, double tCA, double postWindow)
    {
        double t0 = burn.SimTime;
        (_, Vector2d sunVel, _) = Primary(eph, t0);
        (double _, double passT) = ProjectPass(sim, eph, burn, deltaV, target, tCA, 4 * Day);
        double wAfter = Math.Min(90 * Day, postWindow * 0.9);
        ShipState after = sim.RunAdaptive(
            new ShipState(burn.Position, burn.Velocity + deltaV, t0), Math.Max(1.0, (passT + wAfter) - t0), maxTimeStep: CoarseStep);
        return (after.Velocity - sunVel).Length;
    }

    private static double RefinePass(Simulator sim, ICelestialEphemeris eph, ShipState burn, string target, double passEpochEstimate)
    {
        double horizon = (passEpochEstimate - burn.SimTime) + 60 * Day;
        if (horizon <= 0)
        {
            return double.NaN;
        }

        IReadOnlyList<TrajectorySample> path = sim.ProjectAdaptive(
            burn, null, horizon, minTimeStep: 120, maxTimeStep: CoarseStep, dynamicalTimeFraction: 1.0 / 24, maxSamples: 2_500);
        (double dist, double t) = ClosestTo(eph, target, path, burn.SimTime);
        return double.IsInfinity(dist) ? double.NaN : t;
    }

    private static (double Distance, double SimTime) ClosestTo(
        ICelestialEphemeris eph, string bodyId, IReadOnlyList<TrajectorySample> samples, double fromTime)
    {
        double best = double.MaxValue;
        int minIdx = -1;
        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].SimTime < fromTime)
            {
                continue;
            }

            double d = (eph.Position(bodyId, samples[i].SimTime) - samples[i].Position).Length;
            if (d < best)
            {
                (best, minIdx) = (d, i);
            }
        }

        if (minIdx < 0)
        {
            return (double.PositiveInfinity, fromTime);
        }

        // Sub-sample refine: fit a parabola to d² over the bracketing samples and evaluate the true
        // periapsis between them (as ClosestApproach does), so a coarse step still reports an accurate
        // pass distance — the solve can run cheap projections without the periapsis quantizing.
        if (minIdx > 0 && minIdx < samples.Count - 1)
        {
            double d0 = DistTo(eph, bodyId, samples[minIdx - 1]);
            double d2 = DistTo(eph, bodyId, samples[minIdx + 1]);
            double a = d0 * d0, b = best * best, c = d2 * d2;
            double denom = a - 2 * b + c;
            if (denom > 0)
            {
                double offset = Math.Clamp(0.5 * (a - c) / denom, -1, 1);
                TrajectorySample from = offset < 0 ? samples[minIdx - 1] : samples[minIdx];
                TrajectorySample to = offset < 0 ? samples[minIdx] : samples[minIdx + 1];
                double f = offset < 0 ? offset + 1 : offset;
                double t = from.SimTime + (to.SimTime - from.SimTime) * f;
                Vector2d pos = from.Position + (to.Position - from.Position) * f;
                double d = (pos - eph.Position(bodyId, t)).Length;
                if (d < best)
                {
                    return (d, t);
                }
            }
        }

        return (best, samples[minIdx].SimTime);
    }

    private static double DistTo(ICelestialEphemeris eph, string bodyId, TrajectorySample s) =>
        (eph.Position(bodyId, s.SimTime) - s.Position).Length;

    private static Vector2d BodyVelocity(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    private static double BodyRadius(ICelestialEphemeris eph, string id)
    {
        foreach (CelestialBody b in eph.Bodies)
        {
            if (b.Id == id)
            {
                return b.BodyRadius;
            }
        }

        return 1.0;
    }

    private static double ApoapsisAU(ShipState s, Vector2d sunPos, Vector2d sunVel, double sunMu, out bool escapes)
    {
        Vector2d r = s.Position - sunPos;
        Vector2d v = s.Velocity - sunVel;
        double rr = r.Length, vv = v.Length;
        double energy = vv * vv / 2 - sunMu / rr;
        if (energy >= 0)
        {
            escapes = true;
            return double.PositiveInfinity;
        }

        escapes = false;
        double a = -sunMu / (2 * energy);
        double h = Math.Abs(r.X * v.Y - r.Y * v.X);
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (sunMu * sunMu)));
        return a * (1 + e) / AU;
    }

    /// <summary>The primary attractor (the sun): the max-Mu body. Its position and velocity anchor the
    /// heliocentric speed and apoapsis readouts (it sits at the origin in the circular ephemeris, but
    /// reading it from the ephemeris keeps the math honest for any body table).</summary>
    private static (Vector2d Pos, Vector2d Vel, double Mu) Primary(ICelestialEphemeris eph, double t)
    {
        string id = "sun";
        double bestMu = -1;
        foreach (CelestialBody b in eph.Bodies)
        {
            if (b.Mu > bestMu)
            {
                (bestMu, id) = (b.Mu, b.Id);
            }
        }

        return (eph.Position(id, t), BodyVelocity(eph, id, t), bestMu);
    }
}
