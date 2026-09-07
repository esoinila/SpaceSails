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

/// <summary>
/// Friday §0 · A KEPT ORBIT, AND THE CAPTAIN'S OWN INSERTION — owner ruling: <i>"armed auto-orbit ends
/// in a KEPT orbit, not an achieved one."</i> The autopilot holds the park with trim burns and quotes
/// the trim budget out of Lab 25.
///
/// <para>Trims are CONSIDERED once every quarter park period rather than every tick — riding the
/// tide's reversible forced eccentricity instead of fighting it, which is the lab's treadmill. There
/// are two ways out and both are loud: the captain disarms, or the tank cannot afford the next trim
/// and the ship is handed back, after which the #180 degradation alert is the backstop.</para>
///
/// <para><c>EnterOrbit</c> is the other half and deliberately sits beside it: the captain's own ⏎
/// insertion, a hand pulse that costs what it costs and is never economized.</para>
///
/// <para>Split out of <c>Map.Autopilot.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // Friday §0: STATION-KEEPING — the autopilot holds the park with trim burns (OrbitKeeping, budgets
    // from Lab 25). Runs every tick while _orbitKept, but only CONSIDERS a trim once every quarter park
    // period (OrbitKeeping.TrimCadenceFraction) — riding the tide's reversible forced eccentricity
    // instead of fighting it every tick (the lab's treadmill). Two ways out, both loud: the captain
    // disarms (ToggleArmedInsertion's #179 double-confirm), or the tank can't afford the next trim —
    // a LOUD handback, after which the #180 degradation alert becomes the backstop as the orbit decays.
    private void StationKeep(CelestialBody body, CelestialBody parent, Vector2d bodyPos, Vector2d bodyVel, double hill)
    {
        // The orbit gone (an external burn flung it out, or it decayed unbound): keeping is over. Stand
        // down loudly; the #180 alert covers the decay from here.
        if (!OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill))
        {
            _orbitKept = false;
            AutopilotStandDown($"autopilot lost the orbit at {body.Name} — no longer bound; you have the ship");
            return;
        }

        // #286: trim back to the CLAMPED park (bounded so the kept orbit clears the parent), not the raw
        // tide-stable radius — otherwise a clamped orbit would be trimmed back out toward the planet.
        double park = KeptParkRadius(body, hill, KeptRadiusCap(body, parent));
        if (SimTime < _keepNextCheckTime)
        {
            return; // between cadence points — let the reversible oscillation reverse itself
        }
        _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);

        if (!OrbitKeeping.NeedsTrim(_ship, bodyPos, bodyVel, body))
        {
            return; // still tight inside the tolerance — nothing to spend
        }

        // #928: a keeping trim is NOT economized. The tenth is the price of an APPROACH — the fine
        // trajectory a hand cannot fly — while the kept orbit's trims are quoted honestly and separately
        // from Lab 25's per-body table ("trim ≈N p/day") and spent in full, so OrbitHold's "holds for N
        // days" and the tank keep telling the same story (the Lab 25 law is untouched).
        int cost = OrbitKeeping.TrimPulseCost(_ship, bodyPos, bodyVel, body, park);
        // (b) the tank running dry — the LOUD handback (Friday §0). Reuse the escalating degradation
        // surface as the BACKSTOP, not the defense: keeping can no longer hold, so hand back loudly and
        // let the #180 alert shout amber/red as the orbit strips.
        if (cost > _reactionMassPulses)
        {
            _orbitKept = false;
            AutopilotStandDown(
                $"⚠ TANK DRY at {body.Name} — the autopilot can no longer hold the orbit ({cost} p needed, {_reactionMassPulses} left). It will now decay — you have the ship.");
            return;
        }

        Vector2d beforeTheTrim = _ship.Velocity;
        _ship = OrbitKeeping.Trim(_ship, bodyPos, bodyVel, body, park);
        _reactionMassPulses -= cost;
        _armedSpentPulses += cost;
        _keepTrimsFired++;
        // #167 BURN KIND 6/9 - THE STATION-KEEPING TRIM. Small, periodic, and the whole reason the banner
        // says AUTOPILOT HOLDS THE ORBIT - a held park that visibly spends a puff now and then is the
        // owner's ruling made legible without opening a panel.
        BurnFired(cost, _ship.Velocity - beforeTheTrim);
        StaleFutureNodes();
        ShowPulseMessage($"🛰 orbit trim at {body.Name} ({cost} p) — holding the park");
    }

    private void EnterOrbit()
    {
        if (RejectNavWhileDocked())
        {
            return;
        }

        // No silent no-ops (issue #136): if there is nothing to orbit, say so.
        if (_ephemeris is null || OrbitInfo() is not { } candidate)
        {
            ShowPulseMessage("No moon or planet in range to orbit — pick a destination (🎯) or coast closer.");
            return;
        }

        // Outside the open window the button (and O) toggles the autopilot instead: arm it
        // anywhere inside capture range and the ship flies the approach itself (M25).
        if (!candidate.CanEngage)
        {
            if (candidate.Armed)
            {
                if (candidate.InCaptureRange)
                {
                    // Arm-once (issue #136): the autopilot is already flying this approach. Report
                    // its status instead of re-firing a burn or dropping the arm mid-flight — it
                    // disarms itself on arrival, or the watchdog stands it down if it can't close.
                    ShowPulseMessage($"🛰 {candidate.Body.Name}: {OrbitStatusLine(candidate)}");
                }
                else
                {
                    ToggleArmedInsertion(candidate.Body.Id); // armed but not yet closing — a press stands it down
                }
                return;
            }
            if (candidate.InCaptureRange)
            {
                ToggleArmedInsertion(candidate.Body.Id); // in range — arm the approach
                return;
            }
            // In view but out of auto-orbit reach: say why the press did nothing, with the gap.
            ShowPulseMessage($"{candidate.Body.Name} is out of auto-orbit range — coast within {FormatDistance(candidate.CaptureRange)} (still {FormatDistance(candidate.Distance - candidate.CaptureRange)} to go).");
            return;
        }

        OrbitAssistInfo oi = candidate;

        // #180 moon-grade orbit: the window is open, but never SILENTLY circularize at a radius the
        // sun's tide will strip (the owner's ≈0.53-Hill Enceladus park, Lab 16). When the current
        // radius is outside the tide-stable band, hand the descent to the armed autopilot — the same
        // machinery that parks at the stable radius — instead of parking unstably here.
        if (!oi.RadiusInStableBand)
        {
            if (oi.Armed)
            {
                ShowPulseMessage($"🛰 {oi.Body.Name}: {OrbitStatusLine(oi)}");
            }
            else
            {
                ToggleArmedInsertion(oi.Body.Id); // descends to the tide-stable park (≈0.33 Hill)
            }
            return;
        }

        // Insert relative to the panel's own body — it can be an armed/destination target,
        // not necessarily the nearest one whose position the tick loop caches.
        Vector2d bodyPos = _ephemeris.Position(oi.Body.Id, SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(oi.Body.Id, SimTime + h) - _ephemeris.Position(oi.Body.Id, SimTime - h)) / (2 * h);
        Vector2d beforeTheOrbit = _ship.Velocity;
        _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, oi.Body);
        _reactionMassPulses -= oi.Cost;
        // #167 BURN KIND 7/9 - THE PANEL'S OWN ORBITAL INSERTION (the button / the `o` key). The `board`
        // cue below is the ARRIVAL's jingle and stays; this is the burn that got her there.
        BurnFired(oi.Cost, _ship.Velocity - beforeTheOrbit);
        ArrivedAt(oi.Body.Id);
        StaleFutureNodes();
        CompleteBoundCargoRunQuests(); // a parcel bound for this moon haven delivers on the park (#175)
        ShowPulseMessage($"Orbital insertion — bound to {oi.Body.Name} 🛰");
        TheArrivalIsRemembered(oi.Body.Id);   // #973 L4: …and the place may finish a grey page, said after the receipt
        RendererInterop.PlayCue("board");
    }

    private float[] _autopilotPlanScratch = [];
}
