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
/// #268 · MATCH, CLAMP, AND SHOVE OFF — the berth being taken and given up, which is the only place in
/// this family the ship's own state is written.
///
/// <para>⚓ Match &amp; clamp is one press that owes three things: the redirect impulse fires instantly,
/// the deferred tab settles, and the ship is frozen onto the dock's drift with the arm's reach recorded
/// so undocking can put her back exactly where the geometry says she is. Undocking is a gentle shove
/// off the clamp rather than a teleport, so she drifts clear instead of re-latching.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    private void ToggleDock()
    {
        if (_dockedHavenId is not null)
        {
            Undock();
            return;
        }

        // Clamp onto the ONE dock truth (#212) — the affordance's selected haven — resolving its live
        // world state straight from the ephemeris, never from _nearestBodyPosition (which may be the
        // planet the station orbits).
        if (DockableHavenHere() is not { } dock || ResolveDockHaven(dock.Id) is not { } t)
        {
            return;
        }

        ClampOntoHaven(t.Body, t.Pos);
    }

    // #213: fly the terminal match, then leave the plain clamp. Reuses the SAME burn the armed autopilot
    // flies at a station (OrbitRule.Approach with μ=0 / no obstacle) and the SAME kernel to price it, so
    // the bill matches the quote the button showed (even while paused). One press nulls the drift into
    // the clamp window; the plain ⚓ Dock then goes live for the captain's confirming press.
    private void MatchAndClamp()
    {
        if (_dockedHavenId is not null)
        {
            return;
        }

        // Already matched (the drift phased under the cap between the render and the click) — just clamp.
        if (_dockAffordance.CanClampNow)
        {
            ToggleDock();
            return;
        }

        if (_dockAffordance.HavenId is not { } id || ResolveDockHaven(id) is not { } t)
        {
            return;
        }

        int quote = _dockAffordance.MatchPulses;
        int cost = OrbitRule.ApproachPulseCost(_ship, t.Pos, t.Vel, t.Body, null, 0);
        // #268: the refuse-with-reason gate still checks the WHOLE match against the tank BEFORE the burn —
        // can't start what you can't afford (#213/#262). But it checks the EFFECTIVE tank: any pulses already
        // on the tab for this same berth are spoken-for, so a second redirect can't over-commit what a first
        // already promised. Checking affordability and TAKING the money are different acts — the take waits
        // for the clamp.
        int committed = _matchLedger.HavenId == id ? _matchLedger.Pulses : 0;
        int free = _reactionMassPulses - committed;
        if (cost > free)
        {
            // #213 hopelessly hot: refuse with the numbers rather than a silent no-op or a broken clamp.
            ShowPulseMessage($"⚓ too hot to match at {t.Body.Name} — needs ≈{cost} p, only {free} free aboard. Bleed speed and come around.");
            return;
        }

        // #267 surface clearance: the terminal match aims straight at the station and ignores the planet it
        // orbits — from an offset it puts the ship on a Uranus orbit that dives BELOW the surface (the
        // owner's live "route through the planet"). Refuse BEFORE the impulse fires OR the tab opens (a
        // refusal must precede spending — or promising — fuel): OrbitRule.Approach is pure, so project the
        // candidate post-match line and, if it threads the haven's parent, bail with the reason having
        // touched neither _ship nor the ledger. The haven itself is judged from its achieved berth (a
        // legitimate arrival AT it is not a threaded planet, the #229 lesson). The captain flies clear first.
        ShipState matched = OrbitRule.Approach(_ship, t.Pos, t.Vel, t.Body, null, 0);
        if (PlannedClearanceViolation(matched, t.Body.Id) is { } clearance)
        {
            ShowPulseMessage($"⚓ {SurfaceClearance.RefusalText(clearance)}. Close the gap to {t.Body.Name} first.");
            return;
        }

        Vector2d beforeTheMatch = _ship.Velocity;
        _ship = matched;
        // #268 pay-at-the-pump: the redirect impulse fires NOW (instant match, #213) but its pulses are NOT
        // taken here — they go on the tab and settle only when the clamp lands (ClampOntoHaven). If the
        // approach diverges or is abandoned before the berth is made, the tab drops uncharged. The gauge stays
        // honest: it does not fall until the flight has actually delivered you.
        _matchLedger = _matchLedger.Accrue(t.Body.Id, cost);
        StaleFutureNodes();
        Warp = 1; _effectiveWarp = 1; // don't blow past the berth the match just delivered you to
        // #185 no-silent-money, applied to outflows: name the bill against the quote, and say plainly it is
        // owed-on-delivery, not taken now.
        ShowPulseMessage($"⚓ matched at {t.Body.Name} — {cost} p on the tab (quoted ≈{quote}); settles when you clamp on. Hit ⚓ Dock.");
        // #167 BURN KIND 8/9 - THE TERMINAL MATCH. It already made the burn noise; now it makes the burn
        // PICTURE too, and the noise is sized by the match's own bill instead of being one flat thump.
        BurnFired(cost, _ship.Velocity - beforeTheMatch);
        UpdateDockAffordance(); // so the plain ⚓ Dock button appears this frame
    }

    // #267 — the surface-clearance gate for a planner COMMIT point that produces a fresh ballistic state
    // (the terminal match). Projects the CANDIDATE post-burn state over the SAME plot horizon the ribbon
    // draws, with the SAME ProjectAdaptive kernel the ribbon and the collision pass use (one truth — the
    // check judges exactly the line the captain sees), then asks SurfaceClearance whether it clears every
    // body it passes. The arrival target is judged from its achieved end so a legitimate dock is never a
    // false refusal (the #229 lesson). Null = clear to fly; a Violation = refuse with the reason.
    private SurfaceClearance.Violation? PlannedClearanceViolation(ShipState candidate, string? arrivalBodyId)
    {
        if (_ephemeris is null || _simulator is null)
        {
            return null;
        }

        IReadOnlyList<TrajectorySample> path = _simulator.ProjectAdaptive(
            candidate, null, CurrentPlotHorizonSeconds, maxTimeStep: 3 * 3600, maxSamples: 8000);
        return SurfaceClearance.Check(path, _ephemeris, arrivalBodyId);
    }

    // The clamp itself — shared by the manual ⚓ Dock press and the #204 honest auto-dock. Freezes the
    // arm's reach off the haven's own position (not _nearestBody), welds the tube, settles cargo runs.
    private void ClampOntoHaven(CelestialBody dock, Vector2d dockPos, string? arrivalNote = null)
    {
        // #268 pay-at-the-pump: a deferred ⚓ Match & clamp burn settles HERE, on delivery — the leg landed,
        // so the pulses it actually fired come off the tank now (never at the button press). Clamping at any
        // OTHER berth clears the abandoned tab without charge; a diverging approach that never clamps already
        // dropped it uncharged upstream (UpdateDockAffordance).
        (int matchCharge, _matchLedger) = _matchLedger.Settle(dock.Id);
        if (matchCharge > 0)
        {
            _reactionMassPulses = Math.Max(0, _reactionMassPulses - matchCharge);
        }

        _dockedHavenId = dock.Id;
        // #269: completing the clamp ATTACHES. The old code only froze the arm at wherever the ship
        // floated (_ship.Position - dockPos) — but the 500,000 km approach envelope means that could be
        // 100,000 km out on a divergent conic (the owner clamped onto The Tilt from 103,989 km, still
        // diving at Uranus, while the HUD read "clamped on, rel 0.0"). Snap the ship onto the haven's
        // rail instead: the co-moving berth state (a berth offset out, the haven's orbital velocity), the
        // SAME construction a vault resume boots with. One body, one rail — the arm's reach is now a
        // berth's width, not a third of the map.
        //
        // #1068 · AND WHICH SLOT IS THE ROSTER'S. Until now every port in the game had exactly one berth and
        // pinned every hull in it. DockRoster reads how many slots a port keeps off the tube it has earned
        // and hands over the one this captain always gets — unless the harbour retyped its roster overnight,
        // in which case he is tied up somewhere else round the same station, once, with nothing said. Same
        // port, same tube, same tier, same walk ashore: nothing here goes near ArrivalTube.TierFor, which is
        // what #1066's shore-leave tally and #1078's establishing shot both read, a few lines below.
        //
        // #525 · …and the SLOT is kept, not just the bearing it points in. DockRoster.BearingAt IS
        // BearingOf(BerthGiven(…)), split into its two halves here for one reason: the very next line marks
        // the reassignment spent, after which asking the roster again answers with the ORDINARY slot while
        // the hull is pinned on the reassigned one. The port's PA reads this number out loud (#525).
        _berthSlot = TheSlotTheRosterGives(dock.Id);
        _ship = BerthState.CoMoving(
            _ephemeris!, dock.Id, SimTime, BerthState.BerthOffsetMeters, _ship.Charge,
            TheBearingOfSlot(dock.Id, _berthSlot.Value));
        TakeTheBerthTheRosterGave(dock.Id);                  // …and a reassignment is spent by being given
        _dockOffset = _ship.Position - dockPos;               // the arm's reach is now the berth offset
        HoldAtDock();                                        // and HoldAtDock keeps it pinned every tick
        StaleFutureNodes();                                  // a berth cancels any pending burns
        _armedOrbitBodyId = null;                            // and disarms any pending auto-insert (issue #126)
        // #962 · A BERTH IS AN ARRIVAL. Owner, a leg later and long gone from the place: "Now why does this
        // rustys roadshed show as any kind of navigation target here still? I left that place already. Why
        // can it not be dismissed / deleted from the screen now. It is just on the way here?"
        //
        // ArrivedAt is the "the voyage is over, the orders complete" hook — and it was wired to ORBITAL
        // INSERTION only (Map.Autopilot's armed insert and manual EnterOrbit). A dock haven is mass-less:
        // you clamp onto it, you never insert, so no arrival at any station in the game had ever completed
        // its own voyage. The DEST lock survived the clamp, survived the undock, and rode out of the berth
        // with the ship — for the rest of the session, with no reachable way to clear it while docked.
        // The clamp is the arrival; say so here, next to the auto-insert it already stands down.
        ArrivedAt(dock.Id);                                  // (no-ops unless this berth WAS the destination)
        _autopilotStandDownReason = null; _dockReadyStatus = null; ResetAutopilotBudget(); // a berth is a fresh start — clear any handback/dock-ready surface
        _dockAffordance = DockAffordance.Hidden; _dockLatched = false;
        SetDeckForDock(dock.Id);                             // weld on the walk-through tube if this haven has an interior
        string ashore = HavenInterior.HasInterior(dock.Id)
            ? $"⚓ Clamped on at {dock.Name} — gangway's mated. Head to the Deck and walk the tube ashore."
            : $"⚓ Clamped on at {dock.Name} — the arm's holding us. Lie low; the heat'll bleed off.";
        // #268: say the state — when the match tab settled on this clamp, name the pulses it took on delivery.
        string settled = matchCharge > 0 ? $"Match settled — {matchCharge} p spent. " : "";
        ShowPulseMessage(arrivalNote is null ? $"{settled}{ashore}" : $"{arrivalNote} {settled}{ashore}");
        RendererInterop.PlayCue("board");
        CompleteCargoRunQuests(dock.Id); // arriving at the delivery berth finishes a cargo run (M-Q3)
        PayCompletedQuests();            // then any finished bar contracts pay out (M-Q1)
        RequestVaultSave();              // #225: a dock is the canonical resume state — autosave it

        // #541 · THE TUBE IS THE ESTABLISHING SHOT. Owner, photographing a long glazed gangway on the way aboard
        // at Tallinn: "This kind of tunnel when docking the ship could be used in gen-ai to tell how big the docked
        // place is." Raised LAST, so the clamp, the settled match tab and the quest payouts have all had their say
        // first — this is scene-setting, and it goes through the one story-beat door like everything else. Once per
        // berth, ever, which is what taught that seam the OncePerSubject cadence.
        //
        // #1066 · AND THE SAME TIER IS WHAT THE CREW GOT OUT OF THIS BERTH. Worked out ONCE and handed to
        // both readers, which is #1065's one-event law: the picture that says "two streams of people … and
        // nobody who has any idea who you are" and the ledger line that says the crew went ashore are the
        // same statement about the same berth, and a second place deciding the tier again would be a second
        // set of books that could disagree with the shot on the screen.
        ArrivalTube.Tier tier = ArrivalTube.TierFor(_ephemeris!, dock.Id);
        NoteTheBerthTheCrewGot(tier);
        RaiseStoryBeat(ArrivalTube.BeatFor(tier), dock.Name);

        // #973 L4 · …and then the place says its own thing, if it has one to say: a page this captain does not
        // remember writing that NAMES this berth is finished by standing on it, and a hull impounded here is
        // one he stopped signing for. After the tube's plate, because the establishing shot comes first.
        TheArrivalIsRemembered(dock.Id);
    }

    private const double UndockPushMps = 300; // gentle shove off the clamp so the ship drifts clear

    /// <summary>
    /// The berth's own shove, as a pure function of the state it acts on. A gentle push clear of the clamp
    /// so the ship drifts away with some motion of her own rather than hanging dead-still on the dock
    /// (owner: "push the ship off so it has motion away from the station"), radially out along the berthing
    /// arm.
    ///
    /// <para>#955 NAV-1 made this a function rather than four lines inside <see cref="Undock"/>: the plotted
    /// ribbon has to start from the ship the cast-off will hand over (<c>PlanStartState</c>), and a departure
    /// the plan DRAWS and a departure the ship FLIES that were computed by two different pieces of arithmetic
    /// is precisely the named bug class — the sim doing one thing while a drawn shape reports another.</para>
    /// </summary>
    private static ShipState ShovedOffTheClamp(ShipState ship, Vector2d havenPosition)
    {
        Vector2d arm = ship.Position - havenPosition;
        Vector2d outward = arm.LengthSquared == 0
            ? (havenPosition == Vector2d.Zero ? new Vector2d(1, 0) : havenPosition.Normalized())
            : arm.Normalized();
        return ship with { Velocity = ship.Velocity + outward * UndockPushMps };
    }

    private void Undock()
    {
        if (_dockedHavenId is null)
        {
            return;
        }

        SetDeckForDock(null); // back to the bare ship deck; pulls you aboard if you'd wandered up the tube

        if (_ephemeris is not null)
        {
            _ship = ShovedOffTheClamp(_ship, _ephemeris.Position(_dockedHavenId, SimTime));
        }

        string name = _nearestBody?.Name ?? "the dock";
        // #962: casting off cannot leave the berth you are casting off FROM as your navigation target.
        // ClampOntoHaven already retires the destination on arrival; this is the same statement said at
        // the other end, and it heals a session that clamped on before that fix existed.
        ArrivedAt(_dockedHavenId);
        _dockedHavenId = null;
        _berthSlot = null;      // #525 · a ship that is not tied up is not in a slot
        ShowPulseMessage($"Clamps released — pushing off from {name}. 🚀");
        RequestVaultSave(); // #225: undock resumes at the nearest haven; persist the new resume basis
    }

    // While clamped, the ship rides the dock: recompute the station's state at the live SimTime and
    // pin the ship a frozen offset off it, drift matched. This OVERRIDES the gravity integrator (a
    // mass-less dock exerts no pull of its own), so no guiding is needed to stay berthed.
    private void HoldAtDock()
    {
        if (_dockedHavenId is null || _ephemeris is null)
        {
            return;
        }

        Vector2d dockPos = _ephemeris.Position(_dockedHavenId, SimTime);
        const double h = 1.0;
        Vector2d dockVel = (_ephemeris.Position(_dockedHavenId, SimTime + h) - _ephemeris.Position(_dockedHavenId, SimTime - h)) / (2 * h);
        _ship = _ship with { Position = dockPos + _dockOffset, Velocity = dockVel };

        // Refresh the nearest-body cache to the dock's fresh state. Otherwise the HUD's "Nearest"
        // line compares the just-pinned ship against a frame-stale body position and, at high warp
        // (the station sweeps hundreds of thousands of km per frame), reads a wildly wrong distance
        // even though the ship is berthed dead-steady alongside.
        if (_nearestBody?.Id == _dockedHavenId)
        {
            _nearestBodyPosition = dockPos;
            _nearestBodyVelocity = dockVel;
        }
    }
}
