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

// Map.Autopilot.ArrivalWindow — THE ARRIVAL, BEFORE AND WHEN IT COMES ROUND. Two halves of one subject,
// both about the moment the promise is settled rather than about flying it:
//
//   · #957, the numbers a refusal is allowed to quote. `ArrivalCheckNow` judges the ship's arrival
//     geometry by the same Core law (`ArrivalStepRule`) the arrive step's own ✓/✗ row is judged by,
//     `NearestWindowNote` names the moment the plotted course actually comes nearest, and
//     `BrakeSearchHorizon` bounds how far a candidate correction is flown before it is given up on.
//     Together they are what turns "can't verify a capture from here" into a sentence with numbers in it.
//
//   · #969, the one frame a PLAN-TIME arm takes the controls. `OpenTheArrivalWindow` releases the hold
//     and fills in the #148/#196/#219 pair from a rehearsal flown at the ship's REAL state — and
//     deliberately never refuses, because the promise was settled at plan time.
//
// The refusal these feed is spoken in `ToggleArmedInsertion`, and the hold they lift is read in
// `CheckArmedInsertion`; both stay in `Map.Autopilot.cs`, where their guards read them.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Autopilot.cs` — see the note at the head of that file for
// why it was cut and what "pure motion" is holding here.
public partial class Map
{
    // #957 — the ship's arrival geometry RIGHT NOW, judged by the same Core law the arrive step's row is
    // judged by (ArrivalStepRule reads its thresholds off OrbitRule / DockRule). This is what turns "can't
    // verify a capture from here — no clear window within range" into a sentence with numbers in it: how
    // far out, how fast, and against which limits. Null for a body with no parent (the sun).
    private ArrivalStepRule.ArrivalCheck? ArrivalCheckNow(string bodyId)
    {
        if (_ephemeris is null
            || BodyById(bodyId) is not { ParentId: not null } body
            || BodyById(body.ParentId) is not { } parent)
        {
            return null;
        }

        Vector2d bodyPos = _ephemeris.Position(bodyId, SimTime);
        Vector2d bodyVel = TransferMath.BodyVelocity(_ephemeris, bodyId, SimTime);
        ArrivalStepRule.ArrivalKind kind = IsDockableHaven(body)
            ? ArrivalStepRule.ArrivalKind.Dock
            : ArrivalStepRule.ArrivalKind.Orbit;
        return ArrivalStepRule.Check(
            kind, body.Name,
            (_ship.Position - bodyPos).Length,
            (_ship.Velocity - bodyVel).Length,
            OrbitRule.HillRadius(body, parent.Mu));
    }

    // #957 — "how far off is the nearest usable window". The plotted course already knows where it comes
    // nearest this body; say it, so a refusal points at a moment the captain can scrub to rather than at a
    // shrug. Empty when the course has no future pass by it (nothing honest to name).
    private string NearestWindowNote(string bodyId)
    {
        if (ArrivePassFor(bodyId) is not { } pass || pass.SimTime <= SimTime)
        {
            return string.Empty;
        }
        return $"; this course's own closest pass by {BodyName(bodyId)} is {FormatDistance(pass.Distance)} at {FormatSimTime(pass.SimTime)}";
    }

    // #957 — how far each candidate correction is flown before it is given up on. This runs on a button in
    // WASM and every candidate is a real rehearsed flight, so the search is bounded by the encounter's own
    // clock: a few times the plotted time-to-pass, floored at five days so a near encounter still gets a
    // fair look and capped at the rehearsal's own horizon. A candidate that would only pay off months later
    // is not the answer to "I am right next to it, dock" — and a shortened horizon can only MISS a
    // solution, never invent one (CaptureBrake believes nothing it has not flown).
    private double BrakeSearchHorizon(string bodyId)
    {
        double toPass = ArrivePassFor(bodyId) is { } pass && pass.SimTime > SimTime
            ? pass.SimTime - SimTime
            : 0;
        return Math.Clamp(3 * toPass, 5 * 86400.0, AutopilotRehearsal.DefaultMaxHorizonSeconds);
    }

    // #969 — THE PROMISE COMES ROUND. The plan-time arm held its hands off for the whole cruise; this is the
    // one frame where it takes the controls. Two things happen and only two: the hold is released (so the
    // arm is from here on an ordinary armed arrival, indistinguishable from one pressed at the door), and
    // the #148/#196/#219 pair — the drawn INTENDED path and the collision alarm's plan pass — is filled in
    // from a rehearsal flown at the ship's REAL state, because only now is there an approach to draw.
    //
    // What deliberately does NOT happen: a refusal. The promise was settled at plan time, and #147's ruling
    // ("dropping from autopilot should never happen when there was nothing external to cause it") forbids
    // discovering a change of mind at the far end of a nine-month coast. If the fresh rehearsal cannot be
    // promised, the pair simply stays null — the ballistic ribbon and the ballistic alarm keep the watch, the
    // arrive row's own ✓/✗ has been speaking the whole way, and the loop below flies what it can with the
    // reserve floor underneath it, exactly as it would for any other arm.
    private void OpenTheArrivalWindow(CelestialBody body)
    {
        _armedArrivalPassSimTime = null;
        ResetApproachTracking(); // the convergence watchdog starts counting from the arrival, not the plot
        if (_ephemeris is not null && _simulator is not null)
        {
            int budget = Math.Max(0, _reactionMassPulses - AutopilotRehearsal.ReservePulses(ReactionMassCapacity));
            AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
                _ship, _ephemeris, _simulator, body.Id, budget, capturePath: true,
                schedule: _armedTransferSchedule);
            if (r.Deliverable)
            {
                CachePlanForAlarm(body.Id, r);
            }
        }

        string verb = IsDockableHaven(body) ? "brings her in to dock" : "flies the insertion";
        LogAutopilotEvent($"the plan's arrival at {body.Name} has come round — the autopilot has the ship and {verb}");
        ShowPulseMessage($"🛰 {body.Name} — the arrival step is live; the autopilot {verb}.");
    }

}
