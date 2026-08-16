using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — PR-I's skim & skip: the corridor gauge, the vacuum-periapsis bisection and the flown drag pass, the aim burn, the dev cheat — and the sail it holes when the dip goes too deep.
public partial class Map
{
    // ---- PR-I · the skim & skip (the corridor gauge behind a plot-desk panel) ----

    // A close pass we can dip the cloud tops on: a planet/moon with an ATMOSPHERE, the pass ahead of us,
    // above the surface, and inside the Hill sphere where the aim can bend the periapsis into the shell.
    private bool PassIsSkimmable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null || cp.SimTime <= SimTime + 60)
        {
            return false;
        }

        CelestialBody? body = SlingBody(cp.BodyId, out CelestialBody? parent);
        if (body is null || parent is null || body.Kind == BodyKind.Station || body.Atmosphere is null)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        return cp.Distance > body.BodyRadius && cp.Distance < hill;
    }

    private Atmosphere? SkimAtmosphere() =>
        _skimmablePass is { } cp && SlingBody(cp.BodyId, out _) is { } b ? b.Atmosphere : null;

    private double SkimShellTopKm() => (SkimAtmosphere()?.TopAltitude ?? 4.0e5) / 1000.0;

    // Open/close the panel. Opening seeds a mid-corridor default depth (a fraction of the shell top that
    // lands in the useful-braking band for the tuned gas giants — the gauge shows the truth per body).
    // PR-D2: open/close the skim compose editor through the accordion (closes any other open step).
    private void ToggleSkimPanel()
    {
        bool opening = _openEditor != FlightEditorKind.Skim;
        _openEditor = opening ? FlightEditorKind.Skim : FlightEditorKind.None;
        _selectedPlanNode = null;
        _skimResult = null;
        _skimFailure = null;
        if (opening && SkimAtmosphere() is { } atm)
        {
            _skimAltKm = Math.Round(0.4 * atm.TopAltitude / 1000.0); // mid-corridor default
        }
    }

    private void OnSkimAltInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double km))
        {
            _skimAltKm = Math.Clamp(km, 1, SkimShellTopKm());
            _skimResult = null;
            _skimFailure = null;
        }
    }

    // SOLVE: aim the periapsis to the requested altitude by a VACUUM-periapsis bisection on a signed
    // perp-⟂-v_rel trim (the sling's aim frame, but the oracle is a cheap gravity-only pass because a
    // sub-shell periapsis is far under SlingPlanner's 0.1 R measurement floor), quantize to whole fine
    // pulses, then fly the quantized plan ONCE through the pass with RunAdaptiveWithDrag — the gauge
    // numbers ARE that flight. Runs on a reduced ephemeris (sun + the target's well + its moons), the
    // same WASM-affordability trick the sling uses; the in-shell pass is entirely the target's to shape.
    private async Task RunSkimSolveAsync()
    {
        if (_ephemeris is null || _simulator is null || _skimmablePass is not { } cp
            || SlingBody(cp.BodyId, out _) is not { Atmosphere: { } atm } body)
        {
            return;
        }

        _skimSolving = true;
        _skimResult = null;
        _skimFailure = null;
        StateHasChanged();
        await Task.Yield(); // let "flying the pass…" paint before the synchronous solve

        double R = body.BodyRadius, mu = body.Mu;
        double shellTop = atm.TopAltitude;

        // The aiming-burn node: a modest lever close to the pass (12 h before), else 10 min from now.
        double tBurn = cp.SimTime - SkimBurnLeadSeconds;
        if (tBurn <= _ship.SimTime + 300)
        {
            tBurn = Math.Floor(_ship.SimTime) + 600;
        }
        _skimBurnTime = tBurn;

        ShipState burnState = _simulator.RunAdaptive(_ship, Math.Max(1.0, tBurn - _ship.SimTime), _plan);
        double burnSpeed = burnState.Velocity.Length;
        double perPulseDv = Math.Max(0.3, burnSpeed * SkimBurnPercent / 100.0);
        int availablePulses = Math.Max(1, _reactionMassPulses - PlannedPulseTotal());
        double cap = Math.Min(SkimMaxAimDeltaV, Math.Max(perPulseDv, perPulseDv * availablePulses));

        // Reduced ephemerides: one gravity-only (the bisection oracle), one carrying the target's air (the gauge).
        (Simulator vacSim, Simulator airSim, ICelestialEphemeris redEph) = BuildSkimContext(cp.BodyId);

        Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
            (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

        Vector2d vRel = burnState.Velocity - BodyVel(redEph, body.Id, tBurn);
        Vector2d vHat = vRel.Normalized();
        var perp = new Vector2d(-vHat.Y, vHat.X);

        // Vacuum periapsis (m) of the aim burnState + perp*alpha, measured against the reduced grav field.
        double horizon = (cp.SimTime - tBurn) + 4 * 86400.0;
        double VacPeri(double alpha)
        {
            var start = new ShipState(burnState.Position, burnState.Velocity + perp * alpha, tBurn);
            IReadOnlyList<TrajectorySample> path = vacSim.ProjectAdaptive(
                start, null, horizon, minTimeStep: 5, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 30_000);
            double best = double.MaxValue;
            foreach (TrajectorySample s in path)
            {
                if (s.SimTime < tBurn)
                {
                    continue;
                }
                double d = (redEph.Position(body.Id, s.SimTime) - s.Position).Length;
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        double target = R + _skimAltKm * 1000.0;
        double fLo = VacPeri(-cap) - target;
        double fHi = VacPeri(+cap) - target;
        if (double.IsNaN(fLo) || double.IsNaN(fHi) || fLo * fHi > 0)
        {
            _skimFailure = "no aim this cheap threads that depth — widen the budget or ease the dip";
            _skimSolving = false;
            StateHasChanged();
            return;
        }

        // Bisect the signed perp trim to the requested periapsis (monotonic in alpha; ~24 cheap flights).
        double aLo = -cap, aHi = +cap;
        for (int i = 0; i < 24; i++)
        {
            double aMid = 0.5 * (aLo + aHi);
            double fMid = VacPeri(aMid) - target;
            if (fMid * fLo <= 0)
            {
                aHi = aMid;
            }
            else
            {
                aLo = aMid;
                fLo = fMid;
            }
        }

        double alphaStar = 0.5 * (aLo + aHi);
        int pulses = Math.Max(0, (int)Math.Round(Math.Abs(alphaStar) / perPulseDv));
        double signedMag = Math.Sign(alphaStar) * pulses * perPulseDv;
        Vector2d quantizedDv = perp * signedMag;
        _skimPulses = pulses;
        _skimHeadingDeg = pulses > 0
            ? Math.Atan2(quantizedDv.Y, quantizedDv.X) * 180.0 / Math.PI
            : 0.0;

        // Fly the QUANTIZED aim through the pass with drag — the gauge is this flight, not the request.
        _skimResult = FlySkimGauge(airSim, redEph, body, burnState, quantizedDv, mu, R, shellTop, burnSpeed);
        _skimSolving = false;
        StateHasChanged();
    }

    // One RunAdaptiveWithDrag pass of the quantized aim: peak g, Δv shed, min altitude, exit verdict —
    // every gauge number measured off the real drag flight. SINGLE-PASS numbers (fine-step accurate);
    // multi-pass planning is out of scope (see the panel's fine print).
    private SkimGauge FlySkimGauge(
        Simulator airSim, ICelestialEphemeris eph, CelestialBody body, ShipState burnState, Vector2d aimDv,
        double mu, double R, double shellTop, double burnSpeed)
    {
        Vector2d BodyPos(double t) => eph.Position(body.Id, t);
        Vector2d BodyVel(double t) => (eph.Position(body.Id, t + 1.0) - eph.Position(body.Id, t - 1.0)) / 2.0;

        var start = new ShipState(burnState.Position, burnState.Velocity + aimDv, burnState.SimTime);

        // Arrival hyperbolic about the body? (sets whether "too shallow" reads as a skip).
        Vector2d vRel0 = start.Velocity - BodyVel(start.SimTime);
        double r0 = (start.Position - BodyPos(start.SimTime)).Length;
        bool arrivalHyperbolic = vRel0.LengthSquared / 2.0 - mu / r0 > 0;

        // Find the pass epoch under the aim, then fly the shell crossing at fine resolution.
        double horizon = 8 * 86400.0;
        IReadOnlyList<TrajectorySample> path = airSim.ProjectAdaptive(
            start, null, horizon, minTimeStep: 5, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 40_000);
        double bestD = double.MaxValue, tPass = start.SimTime;
        foreach (TrajectorySample s in path)
        {
            if (s.SimTime < start.SimTime)
            {
                continue;
            }
            double d = (BodyPos(s.SimTime) - s.Position).Length;
            if (d < bestD)
            {
                (bestD, tPass) = (d, s.SimTime);
            }
        }

        double shellR = R + shellTop;
        ShipState s2 = airSim.RunAdaptive(start, Math.Max(1.0, (tPass - 2 * 3600) - start.SimTime));
        double peak = 0, shed = 0, minAlt = double.PositiveInfinity, t0 = s2.SimTime;
        bool entered = false;
        while (s2.SimTime - t0 < 12 * 3600)
        {
            (ShipState next, Simulator.DragReport rep) =
                airSim.RunAdaptiveWithDrag(s2, 20.0, null, minTimeStep: 0.1, maxTimeStep: 1.0);
            peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
            shed += rep.DeltaVShedMetersPerSecond;
            if (!double.IsNaN(rep.MinAltitudeMeters))
            {
                minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
            }
            s2 = next;
            double r = (BodyPos(s2.SimTime) - s2.Position).Length;
            if (r < shellR)
            {
                entered = true;
            }
            else if (entered)
            {
                break;
            }
        }

        // Clean post-pass energy about the body (propagate clear of the shell), for capture / exit v∞.
        ShipState post = airSim.RunAdaptive(s2, 12 * 3600);
        double rr = (BodyPos(post.SimTime) - post.Position).Length;
        double relv = (post.Velocity - BodyVel(post.SimTime)).Length;
        double e = relv * relv / 2.0 - mu / rr;
        bool captured = e < 0;
        double exitVinf = captured ? 0 : Math.Sqrt(2 * e);

        double achievedAlt = double.IsPositiveInfinity(minAlt) ? shellTop : minAlt;
        double pulsesSaved = shed / Math.Max(1.0, 0.10 * burnSpeed); // vs a −10% drive pulse at the pass entry speed

        return new SkimGauge(
            _skimPulses, _skimHeadingDeg, _skimBurnTime,
            achievedAlt, shed, peak / 9.80665, pulsesSaved,
            captured, exitVinf, arrivalHyperbolic,
            _skimAltKm, achievedAlt / 1000.0);
    }

    // A reduced ephemeris for the skim: the primary (sun), the target, its parent chain, and the
    // target's moons — the only bodies that shape an in-shell pass. Returns a gravity-only sim (the
    // bisection oracle), an atmosphere-carrying sim (the gauge flight), and the shared ephemeris. Same
    // reduction rationale as BuildSlingSolveContext: drop the negligible bodies that dominate WASM cost.
    private (Simulator VacSim, Simulator AirSim, ICelestialEphemeris Eph) BuildSkimContext(string targetId)
    {
        var ids = new HashSet<string>();
        string? cur = targetId;
        while (cur is not null && ids.Add(cur))
        {
            cur = _ephemeris!.Bodies.FirstOrDefault(b => b.Id == cur)?.ParentId;
        }
        foreach (CelestialBody b in _ephemeris!.Bodies)
        {
            if (b.ParentId == targetId)
            {
                ids.Add(b.Id);
            }
        }
        CelestialBody? root = _ephemeris.Bodies.Where(b => b.ParentId is null).OrderByDescending(b => b.Mu).FirstOrDefault();
        if (root is not null)
        {
            ids.Add(root.Id);
        }

        var airBodies = _ephemeris.Bodies.Where(b => ids.Contains(b.Id)).ToList();
        var vacBodies = airBodies.Select(b => b with { Atmosphere = null }).ToList();
        var airEph = new CircularOrbitEphemeris(airBodies);
        var vacEph = new CircularOrbitEphemeris(vacBodies);
        return (new Simulator(vacEph, 1.0), new Simulator(airEph, 1.0), airEph);
    }

    // Add the solved skim as a fine Vector-burn node (like the sling). Allowed even for a too-deep plan —
    // a captain may fly into the red; the gauge warned honestly.
    private void AddSkimBurn()
    {
        if (_skimResult is not { } g)
        {
            return;
        }
        if (g.Pulses < 1)
        {
            ShowPulseMessage("No aim burn needed — this depth is the natural pass; add a burn only to change it");
            return;
        }
        if (PlannedPulseTotal() + g.Pulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass for the aiming burn");
            return;
        }

        _planNodes.Add(new PlanNode
        {
            SimTime = _skimBurnTime,
            Action = ManeuverAction.Accelerate,
            Pulses = g.Pulses,
            Percent = SkimBurnPercent,
            Mode = BurnMode.Vector,
            HeadingDegrees = g.HeadingDeg,
        });
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage(g.TooDeep
            ? $"Skim burn laid in — into the RED at {g.HeadingDeg:F0}° 🔥 mind the sail"
            : $"Skim burn laid in — {g.Pulses} pulse{(g.Pulses == 1 ? "" : "s")} at {g.HeadingDeg:F0}° 🔥");
        _openEditor = FlightEditorKind.None; // PR-D2: committed — collapse the scratchpad; the step now lives in the list
        _skimResult = null;
    }

    private string SkimDepthLine(SkimGauge g)
    {
        bool onTarget = Math.Abs(g.RequestedAltKm - g.AchievedAltKm) <= 1.0;
        string aim = onTarget ? "on target" : $"asked {g.RequestedAltKm:F0}";
        string pulses = g.Pulses == 1 ? "" : "s";
        return $"Periapsis: {g.AchievedAltKm:F0} km ({aim}) · aim {g.Pulses} pulse{pulses}";
    }

    private string SkimShedLine(SkimGauge g) =>
        g.TooDeep
            ? $"Δv shed {g.ShedMps:F0} m/s · peak {g.PeakG:F1} g — WOULD HOLE THE SAIL"
            : $"Δv shed {g.ShedMps:F0} m/s (≈{g.PulsesSaved:F1} pulses saved) · peak {g.PeakG:F2} g";

    private string SkimOutcomeLine(SkimGauge g)
    {
        if (g.TooShallow && g.ArrivalHyperbolic)
        {
            return $"Skip — she bounces back out at v∞ {g.ExitVinfMps / 1000:F1} km/s";
        }
        if (g.TooShallow)
        {
            return "Too shallow — barely touches the air";
        }
        return g.Captured ? "Captured — the air bound her into orbit" : $"Exits at v∞ {g.ExitVinfMps / 1000:F1} km/s";
    }

    private string SkimFinePrint() =>
        "single-pass numbers (fine-step); each dip creeps deeper — plan pass by pass";

    // PR-I dev cheat (/map?skim=<bodyId>): boot the ship on a fast HYPERBOLIC inbound whose natural pass
    // grazes the body's cloud tops ~3 days out, so the 🔥 Skim panel's corridor gauge is reachable at
    // once (the natural pass already sits mid-corridor). Reuses the sling cheat's proven construction —
    // a retrograde hyperbolic excess about the body, backed off along the heliocentric velocity — and
    // BISECTS the impact parameter so the flown natural periapsis lands mid-corridor against the real
    // integrator (the encounter geometry is not analytic, so we solve it numerically).
    private void SeedSkimCheat(string bodyId)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? body = SlingBody(bodyId, out _);
        if (body is null || body.Atmosphere is null)
        {
            ShowPulseMessage($"🧪 skim cheat: '{bodyId}' has no atmosphere to skim");
            return;
        }

        double now = _ship.SimTime;
        double R = body.BodyRadius;
        const double passLead = 3.0 * 86400.0;         // ~3 days out — inside the plot horizon, geometry still clean
        double tCA = now + passLead;
        Vector2d jCA = _ephemeris.Position(bodyId, tCA);
        Vector2d jVel = (_ephemeris.Position(bodyId, tCA + 1.0) - _ephemeris.Position(bodyId, tCA - 1.0)) / 2.0;

        const double vInfMag = 14000.0;                 // solidly hyperbolic at the body (the heliocentric approach bleeds some off): shallow → skip out, deep → holes the sail
        Vector2d vinf = -jVel.Normalized() * vInfMag;   // retrograde arrival
        Vector2d vShipCA = jVel + vinf;
        Vector2d vInfHat = vinf.Normalized();
        var perp = new Vector2d(-vInfHat.Y, vInfHat.X); // the impact-parameter direction

        // Flown natural periapsis for an impact parameter b: start off-axis, backed off along the
        // heliocentric velocity so a coast reaches the offset point at the encounter; the body's gravity
        // then focuses it into a genuine close pass.
        ShipState BuildAt(double b) => new(jCA + perp * b - vShipCA * passLead, vShipCA, now, _ship.Charge);
        double NaturalPeri(double b)
        {
            IReadOnlyList<TrajectorySample> path = _simulator!.ProjectAdaptive(
                BuildAt(b), null, passLead + 3 * 86400.0, minTimeStep: 20, maxTimeStep: 3 * 3600, dynamicalTimeFraction: 1.0 / 96, maxSamples: 30_000);
            double best = double.MaxValue;
            foreach (TrajectorySample s in path)
            {
                if (s.SimTime < now)
                {
                    continue;
                }
                double d = (_ephemeris.Position(bodyId, s.SimTime) - s.Position).Length;
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        // Bisect the impact parameter so the flown natural periapsis lands mid-corridor (gravity focuses
        // a ~12 R offset down to a graze, so the periapsis rises monotonically with b past the impact b).
        double targetPeri = R + 0.4 * body.Atmosphere.TopAltitude; // mid-corridor, matching the panel's default depth
        double lo = 3 * R, hi = 30 * R;
        for (int i = 0; i < 34; i++)
        {
            double mid = 0.5 * (lo + hi);
            if (NaturalPeri(mid) > targetPeri)
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        _ship = BuildAt(0.5 * (lo + hi));
        _destinationBodyId = bodyId;
        _armedOrbitBodyId = null;
        _planNodes.Clear();
        RebuildPlan();
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        ShowPulseMessage($"🧪 skim cheat: hyperbolic inbound to {body.Name}, cloud tops ~2 days out. Open Plot ▸ 🔥 Skim.");
    }

    // PR-I · the live consequence. A cloud-top dip whose peak drag deceleration crosses the Core damage
    // line (Atmosphere.SailHoleDecelG) holes the sail: thrust and every pending burn are disabled for a
    // fixed repair window while the crew sews. Deterministic — driven only by the flown drag, no RNG.
    private void CheckSailHole()
    {
        if (!_sailHoled && _frameMaxDragDecel / 9.80665 >= Atmosphere.SailHoleDecelG)
        {
            _sailHoled = true;
            _sailRepairedAtSimTime = _ship.SimTime + SailRepairSeconds;
            StaleFutureNodes(); // the burns she can no longer make
            ShowPulseMessage("🔥 The rigging screams — sail holed in the cloud tops; the crew is sewing");
            RendererInterop.PlayCue("board");
        }
        else if (_sailHoled && _ship.SimTime >= _sailRepairedAtSimTime)
        {
            _sailHoled = false;
            ShowPulseMessage("🪡 Sail sewn shut — the drive answers again");
        }
    }

}
