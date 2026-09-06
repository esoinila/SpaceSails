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

// Map.Autopilot.ParkWatch — WHICH BODY THE SHIP IS ACTUALLY BOUND TO, AND WHETHER THAT PARK IS COMING
// APART. Ground truth recomputed every frame from `OrbitRule.IsBound`, deliberately independent of the
// orbit-assist UI's armed/nearest framing, which can point at a different body than the one the ship is
// really circling — and the #180 edge-triggered degradation alert that rides on it: raise on a transition
// INTO a risk verdict, clear when stability returns, and never a word in between, because an alert that
// speaks every tick is an alert nobody reads. Losing an orbit must never be discovered by looking (the
// owner's Enceladus strand).
//
// This is the watchdog, not the pilot: nothing here arms, burns or stands the autopilot down. It only
// ASKS `_orbitKept` and `AutopilotFlyingApproach` who has the helm, so a kept park's between-trim brush
// at the band ceiling is read as the keeper working rather than as decay (`OrbitDegradeAlertRule`).
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Autopilot.cs` — see the note at the head of that file for
// why it was cut and what "pure motion" is holding here.
public partial class Map
{
    // The body the ship is gravitationally bound to right now (M20 orbit rules) — ground truth
    // for CommerceRule's "same orbit" case, independent of the orbit-assist UI's armed/nearest
    // framing. Cached alongside its position/Hill radius so DrawNpcs can ring-highlight co-orbiting
    // contacts without recomputing them.
    private string? _orbitedBodyId;
    private Vector2d _orbitedBodyPosition;
    private double _orbitedBodyHillRadius;

    // ---- #180 orbit-degradation alert. Edge-triggered off OrbitRule.ParkStability for the bound
    // body: on a transition INTO the tide-chaotic band (amber) or a surface-grazing orbit (red) we
    // raise a persistent pilot-banner warning, drop warp to 1×, and log it; it clears when stability
    // returns. TODO(#166): migrate this into the ShipAlerts channel (+🦜) when that lands. ----
    private string? _orbitDegradeWarning;                 // persistent amber/red "orbit degrading" line
    private int _orbitDegradeSeverity;                    // 0 none · 1 TideRisk (amber) · 2 Subsurface (red)
    private OrbitRule.ParkStabilityVerdict _lastParkStability = OrbitRule.ParkStabilityVerdict.NotBound;
    private string? _parkStabilityBodyId;                 // body _lastParkStability refers to (fresh watch on change)

    // Recomputed every frame from ground truth (OrbitRule.IsBound), same math UpdateEffectiveWarp
    // already relies on — not the orbit-assist UI's armed/nearest framing, which can point at a
    // different body than the one the ship is actually bound to.
    private void UpdateOrbitedBody()
    {
        _orbitedBodyId = null;
        CelestialBody? boundBody = null;
        Vector2d boundBodyPos = default, boundBodyVel = default;
        double boundHill = 0;
        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null)
                {
                    continue; // the sun: everyone "orbits" it, not a bus stop
                }

                CelestialBody? parent = null;
                foreach (CelestialBody candidate in _ephemeris.Bodies)
                {
                    if (candidate.Id == body.ParentId) { parent = candidate; break; }
                }
                if (parent is null)
                {
                    continue;
                }

                Vector2d bodyPos = _ephemeris.Position(body.Id, SimTime);
                const double h = 1.0;
                Vector2d bodyVel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
                double hill = OrbitRule.HillRadius(body, parent.Mu);
                if (OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill))
                {
                    _orbitedBodyId = body.Id;
                    _orbitedBodyPosition = bodyPos;
                    _orbitedBodyHillRadius = hill;
                    boundBody = body;
                    boundBodyPos = bodyPos;
                    boundBodyVel = bodyVel;
                    boundHill = hill;
                    break;
                }
            }
        }

        // #180: watch the bound orbit's tide-stability every tick (cheap — one bound body at most)
        // and alert on the edge into decay. Ground truth, independent of the orbit-assist UI.
        UpdateParkStability(boundBody, boundBodyPos, boundBodyVel, boundHill);

        // PR-11 deviation: this used to auto-pop the (small, floating) Local Space panel on the
        // rising edge of a bind (vision par. 10). Now that Trade is a full-screen desk, yanking
        // the player's whole view there mid-flight would be jarring rather than helpful — the
        // Trade chip (TradeChip()) already updates live so the player notices the new contact,
        // and switching desks stays a deliberate action (number key / tab / chip click).
        // (The haven news + lesson advance moved to the "hidden at a haven" rising edge in
        // UpdateEncounters, so a mass-less dock — which never orbit-binds — triggers them too.)
    }

    // #180: edge-triggered orbit-degradation alert. Evaluate the bound body's ParkStability each
    // tick and fire ONLY on a transition into a risk verdict (TideRisk / Subsurface), or clear when
    // stability returns. Losing an orbit must never be discovered by looking — the owner's Enceladus
    // strand. TODO(#166): route this through the ShipAlerts channel (+🦜) when it lands.
    private void UpdateParkStability(CelestialBody? body, Vector2d bodyPos, Vector2d bodyVel, double hill)
    {
        OrbitRule.ParkStabilityVerdict verdict = body is null
            ? OrbitRule.ParkStabilityVerdict.NotBound
            : OrbitRule.ParkStability(_ship, bodyPos, bodyVel, body, hill);

        // WHICH SHIP THIS WATCHDOG IS ALLOWED TO JUDGE — Friday §0 + #962, settled in Core against
        // concrete numbers (OrbitDegradeAlertRule). A KEPT park's between-trim brush at the band ceiling
        // is the keeper working, not decay; and a ship the autopilot is FLYING along a rehearsed path
        // that cleared this very body has no park to degrade at all — her osculating conic is a
        // prediction about a coast the next approach burn is about to erase. Both deferrals stay
        // falsifiable: a plan that did not itself clear the floor here, or a ship gone deeper toward the
        // body than the plan ever went, is judged raw and shouts.
        // (The plan question is only ASKED on a risk verdict, and AutopilotFlyingApproach — which walks
        // the body list through OrbitInfo — is the last term of it, so the quiet tick stays a comparison.)
        bool risk = verdict is OrbitRule.ParkStabilityVerdict.TideRisk or OrbitRule.ParkStabilityVerdict.Subsurface;
        verdict = OrbitDegradeAlertRule.Evaluate(
            verdict,
            keepingHoldsOrbit: _orbitKept,
            autopilotFlyingRehearsedPath: risk && body is not null
                && _autopilotPlanPath is { Count: >= 2 } && AutopilotFlyingApproach,
            planClosestApproach: body is not null && _autopilotPlanBodyClearance is { } cleared
                && cleared.TryGetValue(body.Id, out double planPass)
                    ? planPass
                    : double.NaN,
            shipDistanceNow: body is null ? 0 : (_ship.Position - bodyPos).Length,
            surfaceFloor: body is null ? 0 : OrbitRule.SurfaceParkRadii * body.BodyRadius);

        bool IsRisk(OrbitRule.ParkStabilityVerdict v) =>
            v is OrbitRule.ParkStabilityVerdict.TideRisk or OrbitRule.ParkStabilityVerdict.Subsurface;

        // A change of bound body resets the watch — a fresh park is a fresh baseline. Warn straight
        // away if we arrive already in a risk state; otherwise clear any stale warning.
        if (body?.Id != _parkStabilityBodyId)
        {
            _parkStabilityBodyId = body?.Id;
            _lastParkStability = verdict;
            if (body is not null && IsRisk(verdict))
            {
                RaiseOrbitDegrade(body, bodyPos, bodyVel, verdict);
            }
            else
            {
                ClearOrbitDegrade();
            }
            return;
        }

        if (verdict == _lastParkStability)
        {
            return; // no transition — nothing to do (edge-triggered, not continuous)
        }

        if (IsRisk(verdict))
        {
            // Into a risk state, or an escalation/de-escalation between the two risk verdicts.
            if (body is not null)
            {
                RaiseOrbitDegrade(body, bodyPos, bodyVel, verdict);
            }
        }
        else if (IsRisk(_lastParkStability))
        {
            ClearOrbitDegrade(); // stability returned (or the ship left the well)
        }

        _lastParkStability = verdict;
    }

    private void RaiseOrbitDegrade(CelestialBody body, Vector2d bodyPos, Vector2d bodyVel, OrbitRule.ParkStabilityVerdict verdict)
    {
        bool subsurface = verdict == OrbitRule.ParkStabilityVerdict.Subsurface;
        string reason = subsurface
            ? "periapsis under the surface — impact coming"
            : "drifting past the tide-stable band (Lab 16) — it strips over hours";
        // Ballpark corrective-burn cost from the current state — a hint for the "re-park or leave"
        // choice; the orbit/autopilot button recomputes the exact bill when pressed.
        int reparkCost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);

        // #962, second half: the offer must be a choice the captain HAS. The owner was shown
        // "re-park (≈48 p) or leave" while the banner one line above read "AUTOPILOT HAS THE SHIP" —
        // a manual insertion there is a burn that fights the plan still being flown. A ship under the
        // autopilot is told what has the helm instead. "Under the autopilot" means at the HELM — flying
        // the approach, or holding the park. A #969 plan-time arm still waiting for its pass is not that
        // (the captain's own plotted burns fly the ship through the hold), so there she keeps the bill.
        bool autopilotHasTheShip = AutopilotFlyingApproach || _orbitKept;
        string offer = OrbitDegradeAlertRule.Offer(autopilotHasTheShip, reparkCost);

        _orbitDegradeSeverity = subsurface ? 2 : 1;
        _orbitDegradeWarning = $"⚠ orbit degrading at {body.Name} — {reason}; {offer}";
        Warp = 1; // auto-drop so the decay isn't blown past at warp
        LogAutopilotEvent(_orbitDegradeWarning);
        ShowPulseMessage(_orbitDegradeWarning);

        // #166: the third founding alert now speaks through the shared channel too. Raise fires only on
        // the rising edge / an escalation to red — so the parrot squawks once per crossing, not per tick.
        if (_shipAlerts.Raise(AlertKind.OrbitDegrade, subsurface ? AlertSeverity.Red : AlertSeverity.Amber,
                _orbitDegradeWarning, SimTime))
        {
            SquawkNow(Parrot.Squawk.OrbitDecay, _lastTimestampMs ?? 0, force: true);
        }
    }

    private void ClearOrbitDegrade()
    {
        _shipAlerts.Clear(AlertKind.OrbitDegrade);

        if (_orbitDegradeWarning is null)
        {
            return;
        }

        LogAutopilotEvent("orbit stable again — tide-risk cleared");
        _orbitDegradeWarning = null;
        _orbitDegradeSeverity = 0;
    }
}
