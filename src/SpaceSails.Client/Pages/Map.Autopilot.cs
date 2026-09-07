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

// Map.Autopilot — the pilot's hands: rehearsal and promise, arm and stand-down, the transfer
// burns, and the station-keeping that holds a KEPT orbit. Lifted whole from Map.razor for #251.
//
// #251 · WHY THE FILE WAS CUT, and what "pure motion" is holding across the family. At 1,442 lines this
// was the longest hand-written file in `src/`, and it was the file that set the size gate's daylight
// (`NoSourceFileIsTooLongTests`, the line at 1,500): the next section anybody added here would have had to
// be shoved somewhere else first. So it is cut along the seams it already had — one subject per partial,
// every line moved VERBATIM, no rename, no signature change, no statement reordered inside a method. A
// `partial` is the same class, so the page's field roster is untouched by construction.
//
// THE FAMILY (1,442 lines → six files under #870, and the largest of those, this one, is six more under
// #251 — eleven files, largest 278):
//   · Map.Autopilot.cs               — the armed state itself: which body, which pass, what it was quoted,
//                                      and the one predicate every desk chip reads (AutopilotStoodDown).
//   · Map.Autopilot.Budget.cs        — what the arm costs, the plan cached for the alarm, and the transfer
//                                      impulse: the first of the family's five tank debits.
//   · Map.Autopilot.StandDown.cs     — handing the ship back, and the one SUCCESS that is not a handback.
//   · Map.Autopilot.Arm.cs           — the press: rehearse, price, refuse out loud, confirm the disarm —
//                                      and #286's two radii, the cap and the clamped park under it.
//   · Map.Autopilot.Approach.cs      — M25's loop: the approach burn, the trims, the insertion.
//   · Map.Autopilot.Keep.cs          — station-keeping, and the captain's own ⏎ insertion beside it.
//   · Map.Autopilot.ParkWatch.cs     — which body the ship is bound to, and the #180 degradation alert.
//   · Map.Autopilot.OrbitAssist.cs   — the M20 panel's readout and its one line of approach coaching.
//   · Map.Autopilot.ArrivalWindow.cs — #957's refusal arithmetic and #969's promise coming round.
//   · Map.Autopilot.Ancients.cs      — the M28 pyramid grants and the course one charge buys.
//   · Map.Autopilot.FlightPlan.cs    — PR-D1's read-only derivations for the banner and the Nav header.
//
// #870's cut was made to leave five source guards' literals in THIS file, so that not one guard had to be
// edited. #251's cut cannot do that — those literals are spread over four of the six now — so the guards
// learned to read the SUBJECT instead: `MapMarkup.TheArmedAutopilot()` hands them these six files in the
// order the one file laid them out, and the whole `Map.Autopilot*.cs` census is counted so a seventh cannot
// join unread. DECLARED order and not alphabetical, because one of those guards asserts a SEQUENCE: the
// five `_reactionMassPulses -=` debits in order — `charge`, `approachCharge`, `insertCharge`, `cost`,
// `oi.Cost` — across `ApplyTransferBurn`, `CheckArmedInsertion`, `StationKeep` and `EnterOrbit`, which
// alphabetical order gets wrong while still finding all five.
//
// The five guards: `TheArrivalEndsWhereTheErrandIsTests` (the `BodyKind.Station` fork of
// `CheckArmedInsertion`, sliced structurally down to the `#146 the moon run` comment that follows it — both
// ends of that slice are in `.Approach`), `TheTenthIsQuotedAndOnlyTheAutopilotsTests` (the refusal's numbers
// and the debit ledger), `TheKeptParkIsOneRadiusTests` (#286's clamped park, named at exactly one site),
// `TheWallsAreHungAndReadTests` (both `TheArrivalIsRemembered` arrival edges, counted) and
// `TheWreckHasItsOwnArrivalTests` (`AutopilotStandInEnvelope`, and the FABLE marker that must not be in it).
//
// No static field is declared anywhere in this family, so there is no initializer-order hazard here —
// see `NoPartialClassSpreadsItsStaticFieldsTests` for what one would cost.
public partial class Map
{

    // ---- M22: planned insertion — "it is part of flight planning" (owner) ----
    private string? _armedOrbitBodyId;

    // ---- #969 — ARMED *THEN*, NOT ONLY *NOW*. Owner ruling 2026-08-23: "I want dock / orbit option as
    // normal part, a step in the plan. Say three burns and one autopilot to finish the trip to Mars. After
    // that no, absolutely no steps needed if the ship is not interfered with."
    //
    // This ONE nullable is the whole difference between the two arms, and it is deliberately the smallest
    // thing that can tell them apart (no forked autopilot): the sim-time of the PASS the arm's promise was
    // rehearsed for. Null = the historic NOW arm — the ship is at the door and the loop below has the
    // controls from this instant. Set = a plan-time promise about a moment that has not come round yet, so
    // CheckArmedInsertion keeps its hands off entirely (ArrivalStepRule.ArrivalPromiseIsStillAhead) while
    // the captain's plotted burns fly her, and clears itself the instant the pass — or the body — arrives.
    // From then on it IS an ordinary armed arrival and the unchanged insertion/dock path finishes the trip.
    private double? _armedArrivalPassSimTime;

    /// <summary>#969: the arm on the board is a plan-time promise whose pass has not come round yet — the
    /// autopilot is armed, and correctly doing nothing.</summary>
    private bool ArmedArrivalStillAhead => _armedArrivalPassSimTime is not null;

    // #136 convergence watchdog: an armed approach that fires burns without ever beating its
    // closest-ever distance is going nowhere (bad geometry, or the old empty-window fuel trap).
    // Track burns and the best distance so the tick executor can stand the autopilot down and
    // preserve the remaining fuel instead of firing forever with no feedback (the owner's report).
    private int _approachBurnCount;
    private int _approachStalledBurns;
    private double _approachMinDistance = double.MaxValue;
    private const int AutopilotMaxStalledBurns = 6;

    // Fresh convergence tracking for a new (or ended) armed session.
    private void ResetApproachTracking()
    {
        _approachBurnCount = 0;
        _approachStalledBurns = 0;
        _approachMinDistance = double.MaxValue;
    }

    // ---- The autopilot's promise (issues #146/#147). The whole feasibility question is settled at
    // arm time by AutopilotRehearsal, which flies the armed journey in Core and prices it. An
    // affordable trip is armed with its quoted budget REMEMBERED (shown on the insertion step); an
    // un-affordable one is refused with the numbers and never armed. In flight the autopilot keeps a
    // reserve floor: it never burns the tank below it, so it is never stranded — and any stand-down
    // (reserve reached, watchdog, refusal) is LOUD: a persistent reason, a ledger receipt, warp
    // dropped to 1×. The owner's ruling: "dropping from autopilot should never happen when there was
    // nothing external to cause it." ----
    private int _armedBudgetPulses;              // rehearsal-quoted CHARGED cost for the current arm (#928: the tenth)
    // #928 THE TENTH'S ACCUMULATOR, and it is this field: the RAW (un-economized) approach+insert pulses
    // burnt since this arm. Every autopilot burn asks AutopilotRehearsal.ChargeForBurn(_armedSpentPulses,
    // rawCost) what the TANK loses and adds the RAW cost here, so the flown total charged is exactly
    // ⌈raw/10⌉ — the same formula the arm-time estimate quotes. Ten one-pulse burns cost one pulse, not
    // ten and not zero. No new state was needed for the accumulator (#905's frame ledger stays pinned):
    // the running raw total IS the accumulator, and every arm resets it. (Station-keeping trims also add
    // here for the diagnostic count, but they are charged in FULL — the tenth is the price of an
    // approach, not of holding a park — and no approach burn can follow a kept orbit without a re-arm.)
    private int _armedSpentPulses;
    private string? _autopilotStandDownReason;   // persistent "you have the ship" line (decline/handback)
    private string? _dockReadyStatus;            // #155: persistent "in the envelope — hit ⚓ Dock" line, a station SUCCESS (never a #147 handback)
    private IReadOnlyList<TrajectorySample>? _autopilotPlanPath; // #148: the rehearsed INTENDED path
    // #196: the tightest pass of the rehearsed plan path, cached at arm time (the plan is fixed, so it
    // costs one MostSevere pass, not one per tick). The collision alarm judges THIS while armed — the
    // insert burn resolves the ballistic impact, so that impact is the plan working, not news. A plan
    // whose OWN path goes subsurface leaves this an Impact pass, and the alarm shouts red immediately.
    private ClosestApproach.Pass? _autopilotPlanClosestPass;
    // #962: the same rehearsed plan, cached per BODY — how close the plan's own path came to each world
    // it passed. The collision alarm above asks "does the plan hit anything"; the #180 park-degradation
    // watchdog asks the other question of the same numbers — "did the plan clear the body this ship is
    // BOUND to" — because an osculating conic is not the course of a ship the autopilot is still flying.
    // Cached with the path and the pass (the #219 one-arm law): all three live and die together.
    private IReadOnlyDictionary<string, double>? _autopilotPlanBodyClearance;

    // ---- #179: disarming the autopilot is what dropped the owner off orbit; confirm it once. The
    // first disarm click arms this pending flag (with a short expiry) and asks; a second click within
    // the window actually stands down. No browser confirm() — those hang automation. ----
    private string? _disarmConfirmBodyId;        // body whose disarm is pending a confirming second click
    private double _disarmConfirmExpiresMs;      // rAF-clock deadline for that second click
    private const double DisarmConfirmWindowMs = 4000;

    // ---- Friday §0 (owner ruling): "armed auto-orbit ends in a KEPT orbit, not an achieved one."
    // When the autopilot inserts, it does NOT hand the ship back — it enters STATION-KEEPING for
    // _armedOrbitBodyId: holding the park with trim burns (OrbitKeeping, budgets from Lab 25) until
    // the captain deliberately disarms (the #179 double-confirm) or the tank runs dry (a LOUD
    // handback, after which the #180 degradation alert is the backstop). The status reads "AUTOPILOT
    // HOLDS THE ORBIT", never "you have the ship", expressed through FlightPlanStatus. ----
    private bool _orbitKept;                              // parked and now station-keeping _armedOrbitBodyId
    private int _keepTrimPulsesPerDay;                    // Lab 25 trim budget quoted at arm time / at park
    private double _keepNextCheckTime;                    // sim-time of the next trim-cadence check
    private int _keepTrimsFired;                          // trims spent this keeping session (diagnostic/ledger)
    // #220: the next trim is affordable — recomputed every tick keeping is active (CheckArmedInsertion's
    // kept branch). Read ONLY as `_orbitKept && _keepTrimFunded`, so a stale value after a disarm/handback
    // (when _orbitKept is already false) can never keep the collision alarm falsely trusting a dead keep.
    private bool _keepTrimFunded;

    // ---- #146 the moon run: the cached in-well transfer plan for the current arm. When arming rides a
    // cheap Lambert arc (TransferPlanner) instead of the legacy approach loop, the schedule's departure
    // burn(s) are fired EXACTLY at their epochs by the tick-advance split, and AutopilotDecision stays
    // muzzled until the arc is honestly near the target (CheckArmedInsertion's gate) so the giant's pull
    // can't restart the velocity-reset bleed straight through the cheap arc. ----
    private TransferPlanner.Schedule? _armedTransferSchedule; // null when this arm flies the legacy loop
    private string? _armedTransferSummary;                    // the planner's one-line quote for the status
    private int _armedTransferBurnsFired;                     // how many scheduled burns have already fired

    // The autopilot has stood down (refused to arm, or handed the ship back) and the captain has not
    // yet re-engaged — the single predicate every desk chip and the pilot banner read so none can
    // claim a mission the autopilot no longer flies (#147).
    private bool AutopilotStoodDown => _armedOrbitBodyId is null && _autopilotStandDownReason is not null;
}
