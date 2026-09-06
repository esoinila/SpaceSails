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

// Map.Autopilot.FlightPlan — WHAT THE FLIGHT PLAN IS TOLD ABOUT THE ARM (PR-D1,
// docs/WednesdayPlan/UnifiedNavListNotes.md). Read-only derivation over state the arm already keeps: is
// the autopilot FLYING the approach or merely armed and waiting, and when is the armed insertion due —
// so the pilot banner and the Nav-desk header can never disagree about the same arm. The insertion
// editor's own toggle sits with them, being the door that opens onto the step they describe.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Autopilot.cs` — see the note at the head of that file for
// why it was cut and what "pure motion" is holding here.
public partial class Map
{
    // ===== PR-D1: the burn list read AS a flight plan (docs/WednesdayPlan/UnifiedNavListNotes.md) =====
    // Read-only derivation over existing state — NO flight-logic changes. Armed auto-orbit gains list
    // presence as a step; the owner's NOW/next status line and the step counter are derived once, here,
    // so the pilot banner and the Nav-desk header never disagree. FlightPlanStatusBuilder (Core, unit-
    // tested) owns the state + now/next decisions; this only feeds it facts already on screen elsewhere.

    // The autopilot is FLYING THE APPROACH (vs merely armed and waiting for the window) when it is armed
    // AND already within capture range — the same gate OrbitStatusLine reports as "flying the approach".
    // #969: …and never while a PLAN-TIME arm is still waiting for its pass. Mars's capture range is five
    // Hill radii wide, so a ship that has not left Earth can already be "in range" of the encounter it is
    // nine months from — and the banner would have claimed the autopilot was flying an approach through the
    // whole cruise while it was, correctly, doing nothing at all.
    private bool AutopilotFlyingApproach =>
        _armedOrbitBodyId is not null && !_orbitKept && !ArmedArrivalStillAhead
        && OrbitInfo() is { Armed: true, InCaptureRange: true };

    // The armed insertion's time when the plotted destination pass pins it; null = "at window" (unknown).
    // #969: a plan-time arm always knows its own moment — the pass it was rehearsed FOR — so it falls back
    // to that when the destination pass isn't the one talking (the projection is rebuilt on a cadence; the
    // pinned epoch never blinks).
    private double? ArmedInsertionSimTime =>
        _armedOrbitBodyId is not null && _destinationPass is { } dp
            && dp.BodyId == _armedOrbitBodyId && dp.SimTime > SimTime
            ? dp.SimTime
            : _armedArrivalPassSimTime is { } armedPass && armedPass > SimTime ? armedPass : null;

    private void ToggleInsertionEditor()
    {
        _openEditor = _openEditor == FlightEditorKind.Insertion ? FlightEditorKind.None : FlightEditorKind.Insertion;
        _selectedPlanNode = null;
    }
}
