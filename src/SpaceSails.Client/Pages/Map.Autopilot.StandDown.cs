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
/// #147 · HANDING THE SHIP BACK — the ledger of every stand-down, the loud refusal, and the one
/// SUCCESS that is not a handback: standing in a station's envelope with the ⚓ still to press.
///
/// <para>A stand-down is persistent rather than a toast, because "you have the ship" is a fact the
/// captain has to be able to look up rather than a moment that scrolls past. The envelope line is the
/// opposite case and is deliberately kept apart from it: the autopilot did its whole job and the last
/// press is the captain's.</para>
///
/// <para>Split out of <c>Map.Autopilot.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // The autopilot log — a Captain's-ledger receipt for every stand-down (newest first), projected
    // into the ledger's Tips section alongside the intel receipts (the established idiom).
    private readonly List<(double SimTime, string Text)> _autopilotEvents = [];
    private void LogAutopilotEvent(string text) => _autopilotEvents.Insert(0, (SimTime, text));

    // A loud stand-down: clear the arm, remember WHY (persistent, not a 1.5-s toast), file a ledger
    // receipt, and drop warp to 1× — an event worth interrupting for, the #139 deep-well philosophy.
    private void AutopilotStandDown(string reason)
    {
        _armedOrbitBodyId = null;
        ResetApproachTracking();
        ResetAutopilotBudget();
        _dockReadyStatus = null; // a loud handback replaces any prior "dock is ready" success line
        _autopilotStandDownReason = reason;
        LogAutopilotEvent(reason);
        Warp = 1;               // auto-drop: the drop must not slip past unseen at 10,000× warp
        _effectiveWarp = 1;
        ShowPulseMessage($"🛰 {reason}");
    }

    // #155 the last mile: the GRACEFUL station stand-down. When the armed target is a μ=0 station and the
    // rendezvous schedule has flown the ship into the dock envelope (matched and alongside), the autopilot
    // has SUCCEEDED — this is NOT the #147 loud handback. The tell in code: it never sets
    // _autopilotStandDownReason, so AutopilotStoodDown stays FALSE and no "you have the ship / here's why
    // it failed" surface lights up; instead it posts _dockReadyStatus, a success line. It clears the arm,
    // files a ledger receipt, and (like every stand-down) drops warp to 1× so the captain doesn't blow past
    // the berth at 10,000×. Docking stays the captain's ⚓ click — the autopilot never auto-clamps.
    private void AutopilotStandInEnvelope(CelestialBody station)
    {
        // #204: when the errand is honest (a friendly dock haven, nothing hostile-flagged), the ⚓ belongs
        // in the autopilot list — the ship completes the clamp itself, through the SAME path the manual
        // press and the #213 match use. The terminal match that brought it into the envelope already
        // fired above (the legacy Approach loop), so this is just the confirming clamp. Hostile-flagged
        // anything NEVER auto-docks (#186/#178): the captain's-word grammar stays, standing down into the
        // envelope for the manual ⚓ press.
        if (AutoDockHonest(station) && ResolveDockHaven(station.Id) is { } t)
        {
            LogAutopilotEvent($"autopilot delivered {station.Name} — matched in the dock envelope; auto-docking (honest errand)");
            Warp = 1; _effectiveWarp = 1;
            ClampOntoHaven(t.Body, t.Pos, $"🛰 auto-docked at {station.Name} —");
            return;
        }

        _armedOrbitBodyId = null;
        ResetApproachTracking();
        ResetAutopilotBudget();
        _autopilotStandDownReason = null; // SUCCESS — deliberately NOT a handback surface (#147 vs #155)
        // #938 D3a / a live #212 breach: the branch that gets here asks BodyKind.Station, because μ=0 is
        // what makes a body unorbitable and the envelope its only arrival — that part is physics and stays.
        // What must NOT ride on BodyKind is the PROMISE. `hit ⚓ Dock to clamp on` names a button that
        // UpdateDockAffordance only ever offers for a DockableHavens.IsDockable body, and sol.json's
        // Derelict Roadster, Mercury Compute Farms and Highport Satellite Works are μ=0 stations with no
        // haven flag: the autopilot flew you to a wreck and told you to clamp onto it. The clamp clause is
        // now spoken only where a clamp exists; alongside a wreck the line stops at the truth.
        string clamp = IsDockableHaven(station) ? " — hit ⚓ Dock to clamp on" : "";
        _dockReadyStatus = $"🛰 in the dock envelope at {station.Name}{clamp}";
        LogAutopilotEvent($"autopilot delivered {station.Name} — matched inside the dock envelope{(clamp.Length > 0 ? "; hit ⚓ Dock to clamp on" : "")}");
        // #244 item 1 (canon pass, Fable, 2026-09-05) · …AND THE WRECK'S OWN SENTENCE. #1104 stopped the
        // autopilot promising a clamp here and left the arrival with nothing of its own to say, which is
        // what the owner walked into: "I think we dropped out of autopilot… did we miss the dock button
        // press while warping?" The line goes on the autopilot's OWN channel, beside the delivery it
        // belongs to, and it is said ONCE per arrival — a berth that repeats itself every tick is noise,
        // and the whole complaint was that the moment went unnoticed rather than unheard.
        if (Derelict.IsWreckBody(station.Id) && _pickupHailSaidAt != station.Id)
        {
            _pickupHailSaidAt = station.Id;
            LogAutopilotEvent(HarborVocabulary.PickupArrival);
        }
        Warp = 1;               // auto-drop so the arrival moment isn't missed at warp
        _effectiveWarp = 1;
        ShowPulseMessage(_dockReadyStatus);
    }

    /// <summary>#244 item 1 · The wreck this arrival has already been announced at, so the sentence is said
    /// once and not once per tick. Cleared when an insertion is armed (a fresh approach is a fresh arrival),
    /// which is what makes coming back to the same hull say it again.</summary>
    private string? _pickupHailSaidAt;

    // #204/#186: the autopilot completes the clamp itself only for an HONEST arrival — the armed
    // destination is a dock haven and nothing about the errand is hostile-flagged (no authorized plunder,
    // no plunder opportunity in play). A felony keeps the captain's-word grammar. Pure boundary lives in
    // Core (DockAffordanceRule.ShouldAutoDock) so it is unit-testable.
    private bool AutoDockHonest(CelestialBody station) =>
        DockAffordanceRule.ShouldAutoDock(
            IsDockableHaven(station),
            _plunderAuthorizedTargetId is not null || _plunderOpportunityTargetId is not null);

    private void ToggleArmedInsertionFromMenu(string bodyId)
    {
        ToggleArmedInsertion(bodyId);
        _bodyMenuBody = null;
        StateHasChanged();
    }
}
