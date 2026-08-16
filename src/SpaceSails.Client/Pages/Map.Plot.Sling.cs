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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — PR-G's sling: the pass the crank can work, the panel, the reduced-ephemeris solve and its quantized burn, the verdict lines, and the dev cheat that sets one up.
public partial class Map
{
    // ---- PR-G · the sling (SlingPlanner behind a plot-desk panel) ----

    // A closest pass the crank can work: a planet (not the sun, not a station), the pass ahead of us,
    // above the surface, and inside the body's Hill sphere where a flyby actually bends the track.
    private bool PassIsSlingable(ClosestApproach.Pass cp)
    {
        if (_ephemeris is null || cp.SimTime <= SimTime + 60)
        {
            return false;
        }

        CelestialBody? body = SlingBody(cp.BodyId, out CelestialBody? parent);
        if (body is null || parent is null || body.Kind == BodyKind.Station)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        return cp.Distance > body.BodyRadius * 2 && cp.Distance < hill;
    }

    private CelestialBody? SlingBody(string bodyId, out CelestialBody? parent)
    {
        parent = null;
        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris!.Bodies)
        {
            if (candidate.Id == bodyId) { body = candidate; }
        }
        if (body?.ParentId is null)
        {
            return null;
        }
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; }
        }
        return body;
    }

    private double SlingBodyRadius() =>
        _slingablePass is { } cp && SlingBody(cp.BodyId, out _) is { } b ? b.BodyRadius : 1.0;

    // PR-D2: open/close the sling compose editor through the accordion (closes any other open step).
    // Opening seeds a sane default pass distance from the current natural pass (rounded to whole radii,
    // clamped to the floor), so SOLVE has something reasonable to aim at.
    private void ToggleSlingPanel()
    {
        bool opening = _openEditor != FlightEditorKind.Sling;
        _openEditor = opening ? FlightEditorKind.Sling : FlightEditorKind.None;
        _selectedPlanNode = null;
        _slingResult = null;
        _slingFailure = null;
        if (opening && _slingablePass is { } cp)
        {
            double naturalR = cp.Distance / SlingBodyRadius();
            _slingPassRadii = Math.Clamp(Math.Round(naturalR), SlingMinRadii, SlingMaxRadii);
        }
    }

    private void SetSlingSide(SlingPlanner.PassSide side)
    {
        _slingSide = side;
        _slingResult = null;
        _slingFailure = null;
    }

    private void OnSlingRadiiInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
        {
            _slingPassRadii = Math.Clamp(r, SlingMinRadii, SlingMaxRadii);
            _slingResult = null;
            _slingFailure = null;
        }
    }

    // The burn node the solve aims from: a NEW node at scrub time when the scrub sits between now and
    // the pass, else now + 10 min (the solver is free to propose its own — we note which we used).
    private double SlingBurnTime(double passSimTime)
    {
        double scrub = Math.Floor(ScrubTime);
        bool scrubUsable = scrub > _ship.SimTime + 60 && scrub < passSimTime - 3600;
        return scrubUsable ? scrub : Math.Floor(_ship.SimTime) + 600;
    }

    private string SlingNodeNoteNow()
    {
        if (_slingablePass is not { } cp)
        {
            return "";
        }
        double t = SlingBurnTime(cp.SimTime);
        return t <= Math.Floor(_ship.SimTime) + 600 + 0.5
            ? "burn node: now + 10 min (scrub isn't before the pass)"
            : $"burn node: scrub {FormatSimTime(t)}";
    }

    // SOLVE: run the Core solver, then quantize Δv to whole Vector-burn pulses and RE-SUMMARIZE at the
    // quantized Δv, so every number shown is what "Add the burn" will actually fly (the honesty rule).
    private async Task RunSlingSolveAsync()
    {
        if (_ephemeris is null || _simulator is null || _slingablePass is not { } cp)
        {
            return;
        }

        _slingSolving = true;
        _slingResult = null;
        _slingFailure = null;
        StateHasChanged();
        await Task.Yield(); // let the "solving…" state paint before the synchronous solve

        double tBurn = SlingBurnTime(cp.SimTime);
        _slingBurnTime = tBurn;

        ShipState burnState = _simulator.RunAdaptive(_ship, Math.Max(1.0, tBurn - _ship.SimTime), _plan);
        double burnSpeed = burnState.Velocity.Length;
        double perPulseDv = Math.Max(1.0, burnSpeed * SlingBurnPercent / 100.0);
        int availablePulses = Math.Max(1, _reactionMassPulses - PlannedPulseTotal());
        double cap = Math.Min(SlingMaxAimDeltaV, Math.Max(perPulseDv, perPulseDv * availablePulses));

        var request = new SlingPlanner.Request(
            burnState, cp.BodyId, cp.SimTime,
            RequestedPassDistance: _slingPassRadii * SlingBodyRadius(),
            Side: _slingSide,
            MaxDeltaV: cap,
            PulseDeltaV: perPulseDv);

        // The client's WASM is IL-interpreted; the full 22-body ephemeris makes the dozens of
        // near-planet flights the solve needs unbearably slow. Run the SOLVE on a reduced ephemeris
        // (the sun + the target's parent chain — the only bodies that shape a flyby at this range),
        // then re-summarize the shown verdict on the FULL ephemeris so every displayed number, and the
        // burn the plan flies, are honest to the real physics.
        (Simulator solveSim, ICelestialEphemeris solveEph) = BuildSlingSolveContext(cp.BodyId);

        SlingPlanner.Result raw = SlingPlanner.Solve(solveSim, solveEph, request, maxIterations: 30);
        if (!raw.Ok)
        {
            _slingFailure = raw.Failure;
            _slingResult = null;
            _slingSolving = false;
            StateHasChanged();
            return;
        }

        // Quantize to whole pulses, then re-summarize at the quantized Δv on the FULL ephemeris.
        int pulses = Math.Max(1, (int)Math.Round(raw.DeltaVMagnitude / perPulseDv));
        Vector2d dir = raw.DeltaV.Normalized();
        Vector2d quantizedDv = dir * (pulses * perPulseDv);
        _slingPulses = pulses;
        _slingHeadingDeg = Math.Atan2(quantizedDv.Y, quantizedDv.X) * 180.0 / Math.PI;

        _slingResult = SlingPlanner.Summarize(_simulator, _ephemeris, request, quantizedDv);
        _slingSolving = false;
        StateHasChanged();
    }

    // A reduced ephemeris/simulator for the SOLVE only: the primary (sun), the target, its parent
    // chain, and the target's OWN MOONS. A flyby is shaped by the sun, the target's well, and — when
    // the pass threads the target's moon system, as a Jupiter pass does the Galilean moons — those
    // moons; every other body (sibling planets, their moons, distant stations, all >4 AU away for a
    // Jupiter pass) is negligible there yet dominates the per-step cost on IL-interpreted WASM.
    // Dropping only the negligible bodies keeps the solved trajectory faithful; the full-ephemeris
    // re-summary then reports the true, honest pass.
    private (Simulator Sim, ICelestialEphemeris Eph) BuildSlingSolveContext(string targetId)
    {
        var ids = new HashSet<string>();
        // Target + its parent chain up to the root.
        string? cur = targetId;
        while (cur is not null && ids.Add(cur))
        {
            cur = _ephemeris!.Bodies.FirstOrDefault(b => b.Id == cur)?.ParentId;
        }
        // The target's own moons (children) — they share the encounter region.
        foreach (CelestialBody b in _ephemeris!.Bodies)
        {
            if (b.ParentId == targetId)
            {
                ids.Add(b.Id);
            }
        }
        // Ensure the primary (heaviest parentless body — the sun) anchors the heliocentric frame.
        CelestialBody? root = _ephemeris.Bodies
            .Where(b => b.ParentId is null)
            .OrderByDescending(b => b.Mu)
            .FirstOrDefault();
        if (root is not null)
        {
            ids.Add(root.Id);
        }

        var bodies = _ephemeris.Bodies.Where(b => ids.Contains(b.Id)).ToList();
        var eph = new CircularOrbitEphemeris(bodies);
        return (new Simulator(eph, timeStepSeconds: 1.0), eph);
    }

    // Add the solved sling as a Vector-burn node (per #84 semantics: Δv along HeadingDegrees, per-pulse
    // = Percent% of entry speed), reproject, and let the ribbon bend through the pass.
    private void AddSlingBurn()
    {
        if (_slingResult is null || _slingPulses < 1)
        {
            return;
        }
        if (PlannedPulseTotal() + _slingPulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass for the aiming burn");
            return;
        }

        _planNodes.Add(new PlanNode
        {
            SimTime = _slingBurnTime,
            Action = ManeuverAction.Accelerate, // ignored for a Vector burn, but a sane default
            Pulses = _slingPulses,
            Percent = SlingBurnPercent,
            Mode = BurnMode.Vector,
            HeadingDegrees = _slingHeadingDeg,
        });
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage($"Sling burn laid in — {_slingPulses} pulse{(_slingPulses == 1 ? "" : "s")} at {_slingHeadingDeg:F0}° ⤴");
        _openEditor = FlightEditorKind.None; // PR-D2: committed — collapse the scratchpad; the step now lives in the list
        _slingResult = null;
    }

    // Precomputed verdict lines (no inner quotes / no markup in @onclick — the plot-desk idiom).
    private string SlingPassLine(SlingPlanner.Result r) =>
        $"Pass: {FormatDistance(r.AchievedPassDistance)} ({r.AchievedPassDistance / SlingBodyRadius():F1} R) at {FormatSimTime(r.PassEpoch)}";

    private string SlingBurnLine(SlingPlanner.Result r) =>
        $"Aiming burn: {r.DeltaVMagnitude:F0} m/s · {_slingPulses} pulse{(_slingPulses == 1 ? "" : "s")} (Vector, {_slingHeadingDeg:F0}°)";

    private string SlingOutcomeLine(SlingPlanner.Result r) =>
        (r.SpeedGain >= 0 ? $"Crank: +{r.SpeedGain:F0} m/s" : $"Crank: {r.SpeedGain:F0} m/s")
        + " · " + (r.Escapes ? "escapes the sun" : $"apoapsis {r.ApoapsisAU:F2} AU");

    private string SlingLeverLine(SlingPlanner.Result r) =>
        $"Lever: ±1 pulse of aim ⇒ ±{r.LeverGm:F1} Gm at the far end — re-trim after the pass";

    // PR-G dev cheat (/map?sling=<bodyId>): place the ship on an inbound arc whose closest pass by the
    // body lands ~12 days out (inside the default 30-day plot), so the ⤴ Sling panel is testable at
    // once. Deterministic seed (labs' aiming setup): a slow, Hohmann-ish v_inf retrograde to the
    // body's orbital motion, backed off along the ship's velocity so a coast reaches an off-center
    // point at the encounter — the body's own gravity then draws it into a genuine close flyby.
    private void SeedSlingCheat(string bodyId)
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? body = SlingBody(bodyId, out _);
        if (body is null)
        {
            ShowPulseMessage($"🧪 sling cheat: '{bodyId}' isn't a body with a parent to sling past");
            return;
        }

        double now = _ship.SimTime;
        const double passLead = 14 * 86400.0;
        double tCA = now + passLead;
        const double h = 1.0;
        Vector2d jCA = _ephemeris.Position(bodyId, tCA);
        Vector2d jVel = (_ephemeris.Position(bodyId, tCA + h) - _ephemeris.Position(bodyId, tCA - h)) / (2 * h);

        Vector2d vinf = -jVel.Normalized() * 9000.0;         // slow retrograde arrival — both flanks gain
        Vector2d vShipCA = jVel + vinf;
        Vector2d vHat = vShipCA.Normalized();
        var perp = new Vector2d(-vHat.Y, vHat.X);
        Vector2d startPos = jCA + perp * (18 * body.BodyRadius) - vShipCA * passLead;

        _ship = new ShipState(startPos, vShipCA, now, _ship.Charge);
        _destinationBodyId = bodyId;
        _armedOrbitBodyId = null;
        _planNodes.Clear();
        RebuildPlan();
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        ShowPulseMessage($"🧪 sling cheat: inbound to {body.Name} — a close pass ~12 days out. Open Plot ▸ ⤴ Sling.");
    }

}
