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

// Map.LongHaul — crossing the deep black: jump engage, the void cinematic, skip-to-event and
// the closed-form coast that computes the wait instead of slogging it. #251 filing, motion only.

/// <summary>
/// #246/#249 · THE OFFER AND THE ENGAGE — the departure the captain is quoted and the crossing he buys.
/// The bill, the clearance refusal, the capture range, and the four ways a long haul is engaged.
///
/// <para>#251 · THIS FILE IS THE CROSSING'S OWN COMMERCE. The rest of the family is four siblings, each
/// named for the moment it owns: <c>.Skip</c> (the #172 fast-forward and the #261 computed coast),
/// <c>.Void</c> (the arrival-epoch re-seed and the "CROSSING THE VOID" cinematic), <c>.State</c>
/// (everything the family remembers between frames, in the one run the base file kept it in), and
/// <c>.Brake</c> (the #304 arrival brake that ASKS).</para>
///
/// <para>No member is renamed, re-scoped or re-ordered by the cut, and the family holds no static
/// field — see <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for why that is the thing to check.</para>
/// </summary>
public partial class Map
{
    // The last-mile pulse quote (#262): the arrival INSERTION brake, priced from the SOLVED departure's
    // arrival speeds — this works straight off a berth, where there is no plotted destination pass yet (the
    // #249 lesson). The departure solve already knows the arrival relative speed, so the brake to shed into
    // the clamp window is a pure function of it. Falls back to a plotted destination pass's insert cost only
    // when no long-haul departure is in hand (a manual coast already inside the plot horizon).
    private int LongHaulLastMilePulses() =>
        _longHaulDeparture is { Ok: true } dep
            ? LongHaul.InsertionFor(dep).Pulses
            : _destinationPass is { } dp && DestinationPassInfo(dp) is { } info ? info.EstPulses : 0;

    // #262 — the reserve-aware pulse budget the long-haul bills are weighed against (the tank minus the
    // autopilot reserve). One source so LongHaulOfferBlock and the engage's round-bill gate never diverge.
    private int LongHaulBudgetPulses() =>
        Math.Max(0, _reactionMassPulses - AutopilotRehearsal.ReservePulses(ReactionMassCapacity));

    // A destination is a genuine long-haul target when its own sun-orbiting planet exists AND the ship is
    // not already inside that planet's capture range (there is a real void to cross). Drives the offer's
    // visibility on the map menu, the nav card, and the toolbar chip — never gated on the CURRENT coast
    // reaching, which from a berth it never does (#249 fix).
    private CelestialBody? LongHaulTargetPlanet(string? destBodyId)
    {
        if (_ephemeris is null || destBodyId is null || LongHaul.JumpTargetPlanet(_ephemeris, destBodyId) is not { } planet)
        {
            return null;
        }

        CelestialBody? sun = planet.ParentId is null ? null : _ephemeris.Bodies.FirstOrDefault(b => b.Id == planet.ParentId);
        if (sun is null)
        {
            return null;
        }

        double capture = OrbitRule.CaptureRange(OrbitRule.HillRadius(planet, sun.Mu));
        double dist = (_ship.Position - _ephemeris.Position(planet.Id, SimTime)).Length;
        return dist > capture ? planet : null; // already in the vicinity → nothing to haul
    }

    // The gate for the DEPARTURE-based offer: null = clear to engage; else the spoken reason (visible-but-
    // disabled, never hidden — #212). Order: hunter on the board, a kept orbit to disarm, the planner can't
    // solve, then the tank can't afford it. No InsideWell check — the departure burn IS the well-escape;
    // engaging from a berth is exactly the point (the undock is part of engaging).
    // The offer gate. Order: hunter, kept orbit, no solve, unaffordable, then the #267 surface-clearance
    // refusal — the LAST and priciest check, so it is precomputed by the caller (the cached destination
    // path reads _longHaulClearanceBlock, recomputed on the reproject cadence; the menu path, which already
    // re-solves the departure per render, computes it fresh via LongHaulClearanceBlock). Null = clear to go.
    private string? LongHaulOfferBlock(LongHaul.Departure? departure, CelestialBody planet, string? clearanceBlock)
    {
        string planetName = planet.Name;
        if (_hunters.Any(h => !h.BrokenOff && !h.CaughtPlayer))
        {
            return LongHaul.RefusalText(LongHaul.Blocker.HunterActive, planetName);
        }

        if (_orbitKept)
        {
            return LongHaul.RefusalText(LongHaul.Blocker.Keeping, planetName);
        }

        if (departure is not { Ok: true } dep)
        {
            return "🚀 " + (departure?.Failure ?? "no departure arc from here yet — give the plot a moment");
        }

        int budget = LongHaulBudgetPulses();
        // #262: the round-bill gate. An unpayable DEPARTURE is always a hard refuse (that burn genuinely fires
        // at engage — you cannot fire what you cannot pay). An unpayable ROUND bill (departure + arrival brake)
        // is WARN-and-proceed by default, and only a hard block here when the owner flips
        // LongHaul.RefuseOnUnpayableRoundBill — so the button stays live for the warn path (the warning lands
        // at engage, not as a disabled button). RefuseDeparture reuses the existing budget refusal wording.
        LongHaul.Insertion insertion = LongHaul.InsertionFor(dep);
        LongHaul.RoundBillVerdict verdict = LongHaul.EvaluateRoundBill(dep.DeparturePulses, insertion.Pulses, budget);
        if (verdict == LongHaul.RoundBillVerdict.RefuseDeparture)
        {
            return LongHaul.RefusalBudget(dep.DeparturePulses, _reactionMassPulses);
        }

        if (verdict == LongHaul.RoundBillVerdict.RefuseRoundBill)
        {
            return LongHaul.RoundBillRefusal(planetName, dep.DeparturePulses + insertion.Pulses, _reactionMassPulses);
        }

        return clearanceBlock; // #267: the precomputed surface-clearance refusal, or null when the arc is clear
    }

    // #267 — the surface-clearance refusal for a solved long-haul departure, or null when the arc clears
    // every body. The departure is a point-mass conic: it can reach the destination while its path threads
    // the Sun (a low-perihelion transfer) or crosses another planet's disk. Sample the post-burn heliocentric
    // coast (the SAME closed-form kernel the jump rides — no second integrator) and judge it clear. The
    // destination planet is exempt: the haul stops at its capture range, far above the surface — arriving
    // there is the whole point (the #229 arrival rule). Priced once per reproject, not per render.
    private string? LongHaulClearanceBlock(LongHaul.Departure departure, CelestialBody planet)
    {
        if (_ephemeris is null || !departure.Ok
            || (planet.ParentId is { } sunId ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == sunId) : null) is not { } sun)
        {
            return null;
        }

        ShipState postBurn = _ship with { Velocity = departure.PostBurnVelocity };
        IReadOnlyList<TrajectorySample> path =
            LongHaul.SampleHeliocentricPath(postBurn, _ephemeris, sun, departure.ArrivalCenterTime);
        return SurfaceClearance.Check(path, _ephemeris, planet.Id) is { } clearance
            ? $"🚀 {SurfaceClearance.RefusalText(clearance)} — re-plot the departure before the long haul"
            : null;
    }

    // The destination planet's capture range (the void mode's stop), for the promise's "capture (X AU)".
    private double LongHaulCaptureRange(CelestialBody planet)
    {
        CelestialBody? sun = planet.ParentId is { } pid ? _ephemeris?.Bodies.FirstOrDefault(b => b.Id == pid) : null;
        return sun is null ? OrbitRule.CaptureRangeFloorMeters : OrbitRule.CaptureRange(OrbitRule.HillRadius(planet, sun.Mu));
    }

    // The nav-card 🚀 button — uses the reproject-computed departure for the current destination.
    private Task EngageLongHaul() => EngageLongHaulTo(_destinationBodyId, _longHaulPlanet, _longHaulDeparture);

    // The MAP CONTEXT-MENU primary entry (owner refinement): one click SETS the destination AND engages.
    // Solves the departure fresh from the current state so it works straight off a berth, no card-hunting.
    private async Task EngageLongHaulFromMenu(string bodyId)
    {
        CloseBodyMenu();
        SetDestination(bodyId);
        if (_ephemeris is null || LongHaul.JumpTargetPlanet(_ephemeris, bodyId) is not { } planet)
        {
            return;
        }

        await EngageLongHaulTo(bodyId, planet, LongHaul.SolveDeparture(_ship, _ephemeris, planet));
    }

    // Engage the haul (#246 solve → #255 crossing): refuse-with-reason if the gate is shut; else the solve
    // is in hand — charge the departure pulses, place the ship at the SOLVED conic's capture-range arrival
    // (reaches by construction), and advance the clock there. The void is NEVER integrated: instead the
    // world is RE-SEEDED at the arrival epoch with the same tested spawn/wave code a fresh boot uses (owner
    // 2026-07-17: "just use the mechanism we have for the different spawn points — the physics is preserved,
    // and that spawning code is tested to work"). Personal continuity (purse, heat, contacts, caches,
    // quests, insurance, upgrades) rides the live fields untouched; every time-derived personal system keys
    // off an ABSOLUTE sim-time checkpoint, so a decade of decay/accrual applies itself the moment the clock
    // jumps — no replay needed. The crossing runs as an awaited, painting cinematic so the tab never freezes.
    private async Task EngageLongHaulTo(string? destBodyId, CelestialBody? planet, LongHaul.Departure? departure)
    {
        if (_ephemeris is null || planet is null || _jumpInProgress)
        {
            return;
        }

        string destName = destBodyId is { } d ? BodyName(d) : planet.Name;
        // #267: compute the clearance verdict fresh on the click — the engage may run off a menu whose
        // solved departure was never the cached destination one.
        string? clearanceBlock = departure is { Ok: true } dep0 ? LongHaulClearanceBlock(dep0, planet) : null;
        string? block = LongHaulOfferBlock(departure, planet, clearanceBlock);
        if (block is not null)
        {
            ShowPulseMessage(block);
            return;
        }

        LongHaul.Departure dep = departure!.Value;

        // #262 — the arrival brake, quoted as a step of this trip, plus the round-bill verdict. The gate above
        // has already hard-refused an unpayable DEPARTURE (and, if the owner flipped RefuseOnUnpayableRoundBill,
        // an unpayable round bill). Here the DEFAULT (warn) path files the brake as a step and, when the tank
        // won't cover it, WARNS the captain in-voice — they sail on eyes-open, to coast in hot and shed by hand.
        LongHaul.Insertion insertion = LongHaul.InsertionFor(dep);
        LongHaul.RoundBillVerdict roundBill =
            LongHaul.EvaluateRoundBill(dep.DeparturePulses, insertion.Pulses, LongHaulBudgetPulses());
        bool hotArrival = roundBill == LongHaul.RoundBillVerdict.WarnHotArrival;

        // Compute the jump BEFORE committing any side effect: ride the post-burn conic to the capture gate.
        ShipState postBurn = _ship with { Velocity = dep.PostBurnVelocity };
        double horizon = (dep.ArrivalCenterTime - postBurn.SimTime) + 30.0 * DaySeconds;
        LongHaul.Reach reach = LongHaul.Project(postBurn, _ephemeris, planet, horizon);
        if (!reach.Reaches)
        {
            ShowPulseMessage($"🚀 the solved arc slipped past {planet.Name} — re-plot and try the long haul again");
            return; // vanishingly unlikely: the post-burn conic targets the planet by construction
        }

        double crossingSeconds = reach.ElapsedSecondsFrom(postBurn.SimTime);
        double daysPassed = Math.Round(crossingSeconds / DaySeconds);
        double arrivalEpoch = reach.ArrivalSimTime;

        // Commit the departure: undock (part of engaging), charge the honest pulses, flip the banner.
        if (_dockedHavenId is not null)
        {
            Undock();
        }

        ShowPulseMessage(LongHaul.BannerNow(destName));
        // #268 pay-at-the-pump — CORRECT AS-IS, left alone. Unlike ⚓ Match & clamp, this charge IS the
        // burn firing: the departure burn genuinely fires HERE, at engage, before the void-crossing jump
        // (#249/#250's burns-then-jumps order — the post-burn conic above was computed FROM this Δv). So
        // taking the pulses now is charging as the burn executes, not billing a flight the ship hasn't flown.
        _reactionMassPulses = Math.Max(0, _reactionMassPulses - dep.DeparturePulses);

        // #262: file the arrival brake as a step of the trip (persistent log), and — the conservative default —
        // WARN in-voice when the tank can't fund it. The brake is NOT charged here: it settles at delivery
        // through the existing match-and-clamp machinery (#277) when the ship clamps on, no second billing path.
        LogAutopilotEvent(LongHaul.InsertionStep(destName, insertion.Pulses));
        if (hotArrival)
        {
            LogAutopilotEvent(LongHaul.RoundBillWarning(destName, insertion.Pulses));
        }

        // #255 VAULT SAFETY (pre-advance autosave): commit the personal life NOW, so a tab death mid-crossing
        // loses only the jump itself. The undock above already set a berth-resume near the departure.
        RequestVaultSave();
        FlushVaultSaveIfDirty();

        // Raise the diegetic overlay and FREEZE the tick (the re-seed owns the clock; nothing integrates).
        // The #246 bottle-pop squawk fires at engage; ESC/cancel is not offered — the overlay says so.
        _jumpTotalYears = LongHaul.VoidYears(crossingSeconds);
        _jumpYear = 0;
        _jumpDestName = destName;
        _jumpFlavor = VoidFlavor(_jumpTotalYears);
        _jumpActive = true;
        _jumpInProgress = true;
        SquawkNow(Parrot.Squawk.LongHaul, _frameNowMs, force: true);
        RendererInterop.PlayCue("voidjump");
        StateHasChanged();

        // Play the crossing as a short cinematic beat — the year counter ticks up over a couple of seconds
        // while the tab stays fully responsive (there is NOTHING heavy to compute: the world is deterministic
        // from sim time, so the jump is a clock advance + a cheap re-seed, not a decade of integration).
        await RunVoidCinematic();

        // COMMIT the crossing atomically: re-seed the world at the arrival epoch, place the ship at the
        // capture-range arrival, advance the clock.
        ReseedWorldForJump(arrivalEpoch);
        _ship = reach.ArrivalState;
        SimTime = reach.ArrivalSimTime;

        StaleFutureNodes();          // any pending burns are behind us now
        ResetAutopilotBudget();
        _autopilotStandDownReason = null;
        _skipActive = false;         // any warp-skip is moot now
        _passDirty = true;           // reproject from the arrival state
        _scrubOffsetSeconds = 0;

        _jumpInProgress = false;
        _jumpActive = false;

        // Announce the arrival, book the ledger line, and autosave the FAR side of the void.
        ShowPulseMessage(LongHaul.Completed(destName, (int)daysPassed));
        // #304: the arrival brake ASKS (owner 2026-07-18). When the coast owes a real brake, arm the window
        // at this destination — the per-frame ArrivalBrake gate raises the ask while the ship is hot, and
        // fires only on consent (through the one settle path). No silent fire; the #262 warn-and-coast now
        // becomes an in-voice question with the quoted bill, re-raised until the captain answers or sheds.
        if (insertion.Needed)
        {
            _brakeArrivalBodyId = destBodyId ?? planet.Id;
            _brakeQuotedPulses = insertion.Pulses;
            _brakeDestName = destName;
            _brakeGate = ArrivalBrake.Gate.Closed; // a fresh arrival asks afresh (clears any spent gate)
            _brakeShowing = false;
        }
        RendererInterop.PlayCue("board");
        PushNewsEvent(NewsWire.NewsEventKind.LongHaulComplete, destName, $"{(int)daysPassed} d crossed");
        RequestVaultSave();
        ReprojectTrajectory();
        StateHasChanged();
    }
}
