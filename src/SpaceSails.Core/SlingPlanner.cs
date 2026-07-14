namespace SpaceSails.Core;

/// <summary>
/// PR-G · The sling. Lesson 19/20's flyby-aiming pattern (labs/19-the-grand-tour/Probe.cs — the
/// <c>side</c> vector built from v_inf, the crank measured against the live integrator, the lever
/// warnings) productized as a tested Core library so the plotting desk can bend a track off a close
/// planetary pass without the player hand-tuning a burn.
///
/// The honesty rules are the labs' rules, kept here on purpose:
/// <list type="bullet">
/// <item>every residual is one flight of the real deterministic <see cref="Simulator"/> — no
/// patched conics;</item>
/// <item>the solver runs Newton to CONVERGENCE and reports <see cref="Result.Ok"/> = false (with a
/// reason) when the requested pass is out of reach under the Δv cap — it never dresses a best-effort
/// miss as a solution;</item>
/// <item>a flyby is a LEVER: <see cref="Result.LeverGm"/> re-flies the solution with the aim
/// perturbed by one pulse-quantum and reports how far the far end of the pass moves, so the UI can
/// print the same warning the labs print ("re-trim after the pass").</item>
/// </list>
///
/// The control variable is a signed perpendicular Δv <c>α</c> at the burn node (Δv = perp·α, where
/// <c>perp ⟂ v_inf</c> exactly as the lab builds <c>side</c>). Threading nearer or farther is a
/// smooth, monotonic function of α on each side of the encounter — unlike aiming at a fixed-clock
/// b-plane point, which folds into a second (whip-around) root next to a close pass. The achieved
/// closest approach <c>d(α)</c> is a well: it falls to ~0 at the impact α, rising on either side —
/// the two rising flanks are the two sides of the planet (lead vs trail). The solver scans α once to
/// locate the well and label the sides by measured gain, then runs a bisection-safeguarded Newton on
/// the chosen flank to hit the requested pass distance. Deterministic: no clocks, no randomness.
/// </summary>
public static class SlingPlanner
{
    private const double AU = 1.496e11;
    private const double Day = 86400.0;

    // The solve runs dozens of projections; the client's WASM is IL-interpreted (no AOT), so step
    // count is the whole cost. A generous max step lets the adaptive stepper coarsen to ~half-day
    // strides far from the target while still auto-refining to seconds through the encounter — the
    // parabolic refine in ClosestTo then recovers the true periapsis between the coarse samples.
    private const double CoarseStep = 43200.0;

    /// <summary>Which side of the encounter to thread. Lead = the crank donates heliocentric speed
    /// (boost); Trail = it donates less / sheds speed (brake). Mapped to the α flank empirically —
    /// the geometry decides which side gains — so the label stays honest.</summary>
    public enum PassSide
    {
        Lead,
        Trail,
    }

    /// <summary>
    /// A sling request. The caller has already integrated the existing plan to the chosen burn node
    /// and passes the ship state there as <paramref name="BurnState"/>. Aim is specified EITHER by
    /// <paramref name="RequestedPassDistance"/> + <paramref name="Side"/> (the common path — a slider
    /// in planet radii), OR by an explicit <paramref name="SignedOffset"/> (meters; the sign selects
    /// the flank), which overrides the first two.
    /// </summary>
    public readonly record struct Request(
        ShipState BurnState,
        string TargetBodyId,
        double PassEpochEstimate,
        double RequestedPassDistance,
        PassSide Side = PassSide.Lead,
        double? SignedOffset = null,
        double MaxDeltaV = 2500.0,
        double PulseDeltaV = 0.0,
        double PostPassWindowSeconds = 180 * Day);

    /// <summary>
    /// The solved (or refused) sling. On success, <see cref="DeltaV"/> is the burn to add at the
    /// burn node; the summary fields are read off the flown solution. On failure, <see cref="Ok"/>
    /// is false and <see cref="Failure"/> says why — the caller shows it verbatim.
    /// </summary>
    public readonly record struct Result(
        bool Ok,
        string? Failure,
        Vector2d DeltaV,
        double DeltaVMagnitude,
        double AchievedPassDistance,
        double PassEpoch,
        double SpeedBefore,
        double SpeedAfter,
        double SpeedGain,
        bool Escapes,
        double ApoapsisAU,
        double LeverGm,
        int Iterations);

    /// <summary>
    /// Solve for the Δv at the burn node that threads <paramref name="request"/>'s target at the
    /// requested pass distance and side. A coarse α scan locates the impact well and labels the two
    /// flanks by measured gain; a bisection-safeguarded Newton (finite-difference derivative, trust
    /// clamp to the bracket) refines the chosen flank to convergence within
    /// <paramref name="maxIterations"/>.
    /// </summary>
    public static Result Solve(Simulator simulator, ICelestialEphemeris ephemeris, Request request, int maxIterations = 60)
    {
        ShipState burn = request.BurnState;
        double t0 = burn.SimTime;
        string target = request.TargetBodyId;

        // 1. Refine the pass epoch from the current (unburned) arc and read the approach velocity a
        //    little before it to build the b-plane frame (perp ⟂ v_inf) — the lab's `side`.
        double roughHorizon = (request.PassEpochEstimate - t0) + 60 * Day;
        if (roughHorizon <= 0)
        {
            return Failed("the pass is behind the burn node — pick an earlier burn or a later pass");
        }

        double tCA = RefinePass(simulator, ephemeris, burn, target, request.PassEpochEstimate);
        if (double.IsNaN(tCA))
        {
            return Failed("no pass by that body on the plotted course");
        }

        double leadBack = Math.Min(2 * Day, (tCA - t0) * 0.5);
        ShipState approach = simulator.RunAdaptive(burn, Math.Max(1.0, (tCA - leadBack) - t0), maxTimeStep: CoarseStep);
        Vector2d vInf = approach.Velocity - BodyVelocity(ephemeris, target, approach.SimTime);
        Vector2d vInfHat = vInf.Normalized();
        var perp = new Vector2d(-vInfHat.Y, vInfHat.X);

        double targetDist = request.SignedOffset is double signed ? Math.Abs(signed) : request.RequestedPassDistance;
        if (targetDist <= 0)
        {
            return Failed("requested pass distance must be positive");
        }

        double cap = request.MaxDeltaV;
        // Only need to reach a little past the encounter to read the pass — a short margin keeps the
        // dozens of scan/Newton flights cheap (the long post-pass coast is the final summary's job) and
        // stops an impact-grade scan point from looping in a captured orbit before the sample cap.
        double margin = 1.5 * Day;

        // The achieved pass distance for a perpendicular Δv of α at the burn node (one flight each).
        double Pass(double alpha) => ProjectPass(simulator, ephemeris, burn, perp * alpha, target, tCA, margin).Distance;

        // 2. Scan α across ±cap to locate the impact well (min d) and its two rising flanks.
        const int scanPoints = 11;
        var alphas = new double[scanPoints];
        var dists = new double[scanPoints];
        int star = 0;
        for (int i = 0; i < scanPoints; i++)
        {
            alphas[i] = -cap + 2 * cap * i / (scanPoints - 1);
            dists[i] = Pass(alphas[i]);
            if (dists[i] < dists[star])
            {
                star = i;
            }
        }

        // 3. Pick which flank of the well to thread the requested distance on.
        //    - Well INTERIOR to the scan (both flanks real): the two flanks are the two sides of the
        //      planet — label them by measured heliocentric gain and honour the Lead/Trail toggle.
        //    - Well at/outside a scan end (one flank degenerates to a point): only ONE side is
        //      reachable within the Δv budget, so serve the request from that single usable flank
        //      regardless of the toggle (the other side is genuinely out of reach — see the failure).
        bool interior = star > 0 && star < scanPoints - 1;
        int lo = -1, hi = -1;
        if (interior)
        {
            double gainLo = PostPassSpeed(simulator, ephemeris, burn, perp * alphas[0], target, tCA, request.PostPassWindowSeconds);
            double gainHi = PostPassSpeed(simulator, ephemeris, burn, perp * alphas[scanPoints - 1], target, tCA, request.PostPassWindowSeconds);
            bool highFlankGains = gainHi >= gainLo;
            bool wantHighFlank = request.SignedOffset is double so
                ? so >= 0
                : (request.Side == PassSide.Lead ? highFlankGains : !highFlankGains);

            // Try the requested side; fall back to the other flank if it alone reaches the distance.
            (lo, hi) = wantHighFlank
                ? FindBracket(dists, targetDist, star, scanPoints - 1, +1)
                : FindBracket(dists, targetDist, star, 0, -1);
            if (lo < 0)
            {
                (lo, hi) = wantHighFlank
                    ? FindBracket(dists, targetDist, star, 0, -1)
                    : FindBracket(dists, targetDist, star, scanPoints - 1, +1);
            }
        }
        else
        {
            // The whole scan is one monotonic flank; walk it away from the endpoint well.
            (lo, hi) = star == 0
                ? FindBracket(dists, targetDist, 0, scanPoints - 1, +1)
                : FindBracket(dists, targetDist, star, 0, -1);
        }

        if (lo < 0)
        {
            double reachMax = 0, reachMin = double.MaxValue;
            foreach (double d in dists)
            {
                reachMax = Math.Max(reachMax, d);
                reachMin = Math.Min(reachMin, d);
            }

            double bodyR = BodyRadius(ephemeris, target);
            return Failed(
                $"no pass this cheap threads {targetDist / bodyR:F0} R — within the {cap:F0} m/s budget the flyby " +
                $"only reaches {reachMin / bodyR:F0}–{reachMax / bodyR:F0} R; arrive faster or ease the distance");
        }

        // 5. Bisection-safeguarded Newton on α (FD derivative, clamped to the bracket).
        double aL = alphas[lo], aR = alphas[hi];
        double rL = dists[lo] - targetDist;
        double tol = Math.Max(1e6, 0.03 * targetDist);
        double a = 0.5 * (aL + aR);
        double bestAlpha = a;
        bool converged = false;
        int iterations = 0;
        for (; iterations < maxIterations; iterations++)
        {
            double d = Pass(a);
            double r = d - targetDist;
            bestAlpha = a;
            if (Math.Abs(r) <= tol)
            {
                converged = true;
                break;
            }

            // Keep the sign-change bracket around the root.
            if (Math.Sign(r) == Math.Sign(rL))
            {
                aL = a;
                rL = r;
            }
            else
            {
                aR = a;
            }

            double h = Math.Max(1.0, Math.Abs(a) * 0.02 + 0.5);
            double deriv = (Pass(a + h) - d) / h;
            double next = Math.Abs(deriv) > 1e-12 ? a - r / deriv : 0.5 * (aL + aR);
            if (next <= Math.Min(aL, aR) || next >= Math.Max(aL, aR) || double.IsNaN(next))
            {
                next = 0.5 * (aL + aR);
            }

            a = next;
        }

        if (!converged)
        {
            return Failed("no pass this cheap bends you there — arrive faster or ask for a wider pass");
        }

        return Summarize(simulator, ephemeris, request, perp * bestAlpha, tCA, iterations);
    }

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
