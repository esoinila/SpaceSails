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

// Map.Docking — the clamp: match-and-berth, undock, the dock affordance and reach envelope,
// and the shuttle runs that ferry the last mile. Moved out of Map.razor for #251.
public partial class Map
{

    // M6 additions — piracy economy, capture, dock, tutorial
    // A market body's "port zone" (0.067 AU). Deliberately generous: with ±10% prograde-only
    // pulses, *returning* to a body you've left is the hardest maneuver in the game — offline
    // search showed post-capture orbits graze 5.1e9 from Earth for weeks without re-threading
    // anything tighter. The player spawns inside Earth's zone (fence available immediately —
    // natural onboarding), and the Luna-pod tutorial hunt completes within it; the real hunts
    // (He3 haulers) still demand full interplanetary voyages between zones.
    private const double DockRadiusMeters = 1e10;

    private bool _docked;
    private string? _dockBodyId;
    private ShuttleFlightView? _shuttleView;
    private ShuttleFlightView.Run? _shuttleRun;
    private NpcState? _shuttleTarget;

    // The berth we're tied up at, for a tip's provenance line; "ashore" when not docked to a named body.
    private string DockedStationName() =>
        _dockedHavenId is { } id && _ephemeris is not null
            ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == id)?.Name ?? id
            : "ashore";

    // Nav commands that begin flight are refused while clamped to a station: the berthing arm
    // holds the ship fast (HoldAtDock pins it back onto the dock every tick), so an approach or
    // insertion burn would just fight the clamp — Undock first (issue #126). NB the OTHER dock
    // flag, _docked, is mere market proximity (within DockRadiusMeters of a trade body): the ship
    // still flies freely there, so it is deliberately NOT a nav lock. Only the clamp locks.
    private bool NavLockedByDock => _dockedHavenId is not null;
    private const string DockNavLockTip = "Undock first — you're clamped to the station";

    // Returns true (and toasts) when a flight command must be refused because we're clamped on.
    private bool RejectNavWhileDocked()
    {
        if (!NavLockedByDock)
        {
            return false;
        }
        ShowPulseMessage($"⚓ {DockNavLockTip}.");
        return true;
    }

    // Approach coaching for a mass-less haven dock — no orbit, you clamp on (the dock mirror of
    // OrbitStatusLine). Keyed off the same DockReachMeters/DockMatchSpeedMps gates as the ⚓ button.
    private string DockStatusLine(OrbitAssistInfo oi)
    {
        string km(double m) => (m / 1000).ToString("N0", CultureInfo.InvariantCulture);
        if (_dockedHavenId == oi.Body.Id) return "clamped on — lying low";
        if (oi.Distance > DockReachMeters) return $"coast within {km(DockReachMeters)} km to clamp on";
        // #213: in range but too hot — the ⚓ Match & clamp button flies the terminal match; the line
        // names the same act instead of the old "slow it down yourself, then hit ⚓ Dock".
        if (oi.RelSpeed > DockMatchSpeedMps) return "alongside but hot — hit ⚓ Match & clamp to null the drift into the window";
        return "alongside and matched — hit ⚓ Dock to clamp on";
    }

    // Start ids that begin already docked at a walkable station, mapped to the station body. These
    // reuse the one docked-arrival path in ApplyStart / PlaceShipForStart (weld the complex, step
    // aboard by the gangway) — so a new interior station is a scenario body + a spec + one line here.
    private static readonly Dictionary<string, string> DockedStarts = new()
    {
        ["cinder-roost"] = "cinder-roost",
        ["space-bar"] = "the-space-bar",
        ["ringside"] = "ringside-exchange",
        ["the-tilt"] = "the-tilt",
    };

    // M29: situational awareness for trading — the shuttle-range ring around the ship,
    // visible whenever the zoom makes it readable. A partner inside the ring is a deal.
    private void DrawShuttleRange()
    {
        float radiusPx = (float)(CommerceRule.ShuttleRangeMeters / _camera.MetersPerPixel);
        if (radiusPx < 16 || radiusPx > 4000)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(_ship.Position);
        _renderer!.DrawCircle(sx, sy, radiusPx, null, LocalContactRingColor with { A = 60 }, 1f);
        if (radiusPx > 60)
        {
            _renderer.DrawText(sx, sy - radiusPx - 6, "shuttle range", LocalContactRingColor with { A = 150 },
                "10px monospace", TextAlign.Center);
        }
    }

    // The berth: a steel arm/clamp from the dock to the ship while it's held fast (owner's
    // "tube / arm / clamps keeps the ship connected to the dock"). Drawn under the ship marker.
    private void DrawDockArm()
    {
        if (_dockedHavenId is null || _ephemeris is null)
        {
            return;
        }

        Vector2d dockPos = _ephemeris.Position(_dockedHavenId, SimTime);
        (float dx, float dy) = _camera.WorldToScreen(dockPos);
        (float sx, float sy) = _camera.WorldToScreen(_ship.Position);

        RgbaColor clamp = new(210, 220, 235, 230); // steel
        Span<float> arm = stackalloc float[4];
        arm[0] = dx; arm[1] = dy; arm[2] = sx; arm[3] = sy;
        _renderer!.DrawPolyline(arm, clamp, 2f);
        _renderer.DrawCircle(dx, dy, 5f, null, clamp, 1.5f);          // the clamp collar at the dock
        _renderer.DrawText((dx + sx) / 2, (dy + sy) / 2 - 6, "🔗 berthed",
            clamp with { A = 210 }, "10px sans-serif", TextAlign.Center);
    }

    private void UpdateDockStatus()
    {
        _docked = false;
        _dockBodyId = null;
        foreach (string id in MarketBodies)
        {
            Vector2d pos = _ephemeris!.Position(id, _ship.SimTime);
            if ((_ship.Position - pos).Length <= DockRadiusMeters)
            {
                _docked = true;
                _dockBodyId = id;
                break;
            }
        }

    }

    // ---- M14: the boarding run ----

    private void LaunchShuttleRun(NpcState prey)
    {
        double distance = (prey.State.Position - _ship.Position).Length;
        double relSpeed = (prey.State.Velocity - _ship.Velocity).Length;
        _shuttleTarget = prey;
        _shuttleRun = ShuttleFlightView.Launch(distance, relSpeed, prey.Ship.Callsign, prey.Ship.IsPod);
        _deckKeys.Clear();
        ShowPulseMessage("Shuttle away — you have the stick");
    }

    private void UpdateShuttleRun(double dtRealSeconds)
    {
        ShuttleFlightView.Run run = _shuttleRun!;
        bool windowOpen = _captureEngaged && _shuttleTarget is { Boarded: false, Arrived: false };
        ShuttleFlightView.Update(run, dtRealSeconds,
            _deckKeys.Contains("w"), _deckKeys.Contains("s"),
            _deckKeys.Contains("a"), _deckKeys.Contains("d"),
            windowOpen);

        switch (run.State)
        {
            case ShuttleFlightView.RunState.Docked when run.StateTime > 1.6:
                if (_shuttleTarget is { } prey && !prey.Boarded)
                {
                    Board(prey); // instant: the pilot earned it
                }
                EndShuttleRun(boarded: true, null);
                break;
            case ShuttleFlightView.RunState.WindowLost when run.StateTime > 1.6:
                EndShuttleRun(boarded: false, "Window lost — shuttle recovered");
                break;
        }
    }

    private void EndShuttleRun(bool boarded, string? message)
    {
        _shuttleRun = null;
        _shuttleTarget = null;
        _deckKeys.Clear();
        if (message is not null)
        {
            ShowPulseMessage(message);
        }
    }

    // ---- Berthing at a grey-market dock (owner: "like a dry-dock — some kind of tube/arm/clamps
    // keeps the ship connected; the ship should not need guiding while docked; there the heat
    // lowers"). A station haven (mu = 0) can't be orbited, so you clamp onto it: an arm reaches out,
    // the ship rides the dock's drift, and the heat bleeds off. ----
    private string? _dockedHavenId;   // the station haven we're clamped to, or null
    private Vector2d _dockOffset;     // frozen ship-minus-dock offset while clamped (the arm's reach)

    // #268 pay-at-the-pump: the deferred bill for a ⚓ Match & clamp burn. The redirect impulse fires at the
    // press (instant, #213), but its pulses are NOT taken then — they ride this tab and settle only when the
    // clamp lands (ClampOntoHaven). A diverging/aborted approach that never clamps drops the tab uncharged
    // (UpdateDockAffordance), so a leg that never delivered keeps no fuel it never earned. Pure logic in
    // Core.MatchClampLedger; this is the one live copy the client mutates.
    private MatchClampLedger _matchLedger = MatchClampLedger.Empty;

    // #212/#211/#213: the ONE dock affordance truth — the toolbar ⚓ button and the envelope line both
    // read this, so text and button can never disagree. Recomputed every OnTick (paused or not, so the
    // quote survives pause), latch state carried frame-to-frame so the offer doesn't blink as the
    // orbiting station's relative speed phases in and out.
    private DockAffordance _dockAffordance = DockAffordance.Hidden;
    private bool _dockLatched;

    // Recompute the one-truth dock affordance from the SAME focus-first selection the envelope line uses
    // (destination/armed haven first, then nearest dockable haven). Runs every frame — including paused —
    // so a captain reading the board sees the same affordances and the #213 match quote before anything
    // fires. While clamped there is no affordance (the 🚀 Undock button owns the slot).
    private void UpdateDockAffordance()
    {
        if (_dockedHavenId is not null || _ephemeris is null)
        {
            _dockAffordance = DockAffordance.Hidden;
            _dockLatched = false;
            return;
        }

        List<DockHaven> havens = new();
        const double h = 1.0;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!IsDockableHaven(body) || IsBodyHidden(body.Id))
            {
                continue;
            }

            Vector2d pos = _ephemeris.Position(body.Id, SimTime);
            Vector2d vel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
            bool focus = body.Id == _destinationBodyId || body.Id == _armedOrbitBodyId;
            havens.Add(new DockHaven(body, pos, vel, focus));
        }

        // #268: the affordance reads the EFFECTIVE tank — pulses already on a pending match tab are
        // spoken-for (committed, just not yet settled), so the ⚓ offer and its affordability reflect what's
        // actually free to burn, not the full tank the deferred take hasn't hit yet.
        int effectiveTank = Math.Max(0, _reactionMassPulses - _matchLedger.Pulses);
        _dockAffordance = DockAffordanceRule.Evaluate(_ship, havens, effectiveTank, _dockLatched);
        _dockLatched = _dockAffordance.Latched;

        // #268: a match tab whose berth is no longer under the clamp button — the ship diverged out of the
        // envelope, or the affordance now names a different haven — dropped without delivery. Release it
        // UNCHARGED: an aborted or diverging approach keeps no money it never earned. (Settlement is only ever
        // the clamp; while the ship holds the berth the affordance keeps a ⚓ button on this same haven.)
        if (_matchLedger.Owes
            && !(_dockAffordance.HavenId == _matchLedger.HavenId && _dockAffordance.ShowButton))
        {
            ShowPulseMessage($"⚓ match stood down at {BodyName(_matchLedger.HavenId!)} — {_matchLedger.Pulses} p released, unspent (never clamped on).");
            _matchLedger = _matchLedger.Abort();
        }
    }

    // Resolve a haven's live world state straight from the ephemeris — used so docking clamps onto the
    // SELECTED haven (the affordance's one truth), never onto whichever body happens to be _nearestBody
    // (Mars photobombing the Roadstead was exactly #212).
    private (CelestialBody Body, Vector2d Pos, Vector2d Vel)? ResolveDockHaven(string id)
    {
        if (_ephemeris is null || _ephemeris.Bodies.FirstOrDefault(b => b.Id == id) is not { } body)
        {
            return null;
        }

        const double h = 1.0;
        Vector2d pos = _ephemeris.Position(id, SimTime);
        Vector2d vel = (_ephemeris.Position(id, SimTime + h) - _ephemeris.Position(id, SimTime - h)) / (2 * h);
        return (body, pos, vel);
    }

    // The dock envelope lives in Core (DockRule, #155) so the arm-time rehearsal, the live station
    // stand-down and this UI can never quote different numbers. These aliases keep the call sites reading
    // the same way they did when the literals lived here.
    private const double DockReachMeters = DockRule.EnvelopeMeters;    // how close you must coast to throw the clamp on
    private const double DockMatchSpeedMps = DockRule.MatchSpeed;      // and how nearly matched to its drift

    // A station haven you clamp onto (⚓): mass-less, so it can't be orbited — the dock is the only
    // way to lie low there. Moon havens (mu > 0) you hide at by orbiting instead. This is the split
    // the ⚓ marker, the scope's "HAVEN ⚓ DOCK" tag and the Nav hint all key off.
    // The one truth (#288): shared with the Core DockableHavens registry the ?dock cheat and the CI smoke
    // sweep read, so the client's clamp gate and the tested berth list can never drift apart.
    private static bool IsDockableHaven(CelestialBody body) => DockableHavens.IsDockable(body);

    // The station haven we're close enough (and slow enough) to clamp onto right now, if any — read
    // from the one-truth affordance (#212) so it names the SAME haven the envelope line does, never the
    // raw nearest body.
    private CelestialBody? DockableHavenHere() =>
        _dockedHavenId is null && _dockAffordance.CanClampNow && _dockAffordance.HavenId is { } id
            ? BodyById(id)
            : null;

    // ---- The shuttle-bay airlock: "the door you understand as a flight" (#163) ----
    // Walking up to the bay's airlock (ConsoleKind.ShuttleAirlock) opens a pop-up of the places within
    // one shuttle hop (ShuttleRange). Picking a berth and confirming IS the trip: the sim clock advances
    // by the crossing and you step off ashore in the destination's interior — no separate flight
    // minigame. The bay's airlock travels with the ship into every docked complex (HavenInterior seeds
    // its doors from the ship's), so the same door at the destination is the ride home — never stranded.

    // One reachable stop: the body, how far, the crossing time, and whether there's a berth to step off
    // at (a walkable interior). Bodies in range without one show honestly as "no berth ashore yet".
    private sealed record ShuttleStop(CelestialBody Body, double DistanceMeters, double TravelSeconds, bool HasBerth);

    // Null = the hatch is shut; a list (possibly empty) = the pop-up is open.
    private List<ShuttleStop>? _shuttleBayStops;

    // Every non-planet body within one hop of where the ship floats now, nearest first. The sun and the
    // planets themselves are never shuttle berths; the berth we're already clamped to is skipped.
    private List<ShuttleStop> ShuttleDestinationsInRange()
    {
        var stops = new List<ShuttleStop>();
        if (_ephemeris is null)
        {
            return stops;
        }
        Vector2d shipPos = _ship.Position;
        foreach (CelestialBody b in _ephemeris.Bodies)
        {
            if (b.ParentId is null || b.Kind == BodyKind.Planet || b.Id == _dockedHavenId)
            {
                continue; // the sun / a gas giant / the berth we're already at
            }
            double dist = (shipPos - _ephemeris.Position(b.Id, SimTime)).Length;
            if (dist <= b.BodyRadius || !ShuttleRange.InRange(dist))
            {
                continue; // basically on it already, or out of shuttle range
            }
            stops.Add(new ShuttleStop(b, dist, ShuttleRange.TravelSeconds(dist), HavenInterior.HasInterior(b.Id)));
        }
        stops.Sort((a, c) => a.DistanceMeters.CompareTo(c.DistanceMeters));
        return stops;
    }

    private void OpenShuttleBayDoor() => _shuttleBayStops = ShuttleDestinationsInRange();

    private void CloseShuttleBayDoor() => _shuttleBayStops = null;

    // Take the shuttle to a berth in range — the door is the flight. Only interior station havens (μ≤0,
    // clampable) are ever pickable, so arriving reuses the clamp-and-go-ashore path: advance the clock
    // by the crossing, clamp at the destination, weld on its walkable complex, and step off in the hall.
    private void TakeShuttleTo(ShuttleStop stop)
    {
        if (_ephemeris is null)
        {
            return;
        }
        CelestialBody dest = stop.Body;
        double newT = SimTime + stop.TravelSeconds;

        // The shuttle sets the ship down on the destination's rail — the shared co-moving berth state (#269).
        Vector2d destPos = _ephemeris.Position(dest.Id, newT);
        _ship = BerthState.CoMoving(_ephemeris, dest.Id, newT, BerthState.BerthOffsetMeters, _ship.Charge);
        SimTime = newT;

        _dockedHavenId = dest.Id;
        _dockOffset = _ship.Position - destPos;             // freeze the arm's reach at the new berth
        _nearestBody = dest;
        _nearestBodyPosition = destPos;
        _nearestBodyVelocity = _ship.Velocity;              // the berth rides the destination's drift
        _armedOrbitBodyId = null;                           // a berth disarms any pending auto-insert
        _autopilotStandDownReason = null; _dockReadyStatus = null;
        ResetAutopilotBudget();
        _matchLedger = _matchLedger.Abort();                // #268: a shuttle hop abandons any match tab, uncharged
        StaleFutureNodes();                                 // a berth cancels any pending burns
        HoldAtDock();                                       // pin the ship to the station's drift now
        SetDeckForDock(dest.Id);                            // weld on the destination's walkable complex

        // Emerge in the destination interior: you stepped off the shuttle past the gangway, into the
        // concourse — not back aboard your own bridge.
        (_avatarX, _avatarY, _avatarHeading) = (2.5, StationFloorY + 6, Math.PI / 2);
        RefreshAshore();
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;

        _shuttleBayStops = null;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🚀 Shuttle away — {FormatDuration(stop.TravelSeconds)} later you step off at {dest.Name}.");
        CompleteCargoRunQuests(dest.Id); // arriving at a delivery berth still closes a cargo run (M-Q3)
        PayCompletedQuests();
    }

    // The shuttle round-trip's clock cost: advance the sim clock and re-stamp the loitering ship at the
    // new time (frozen in place — the shuttle moved, not the mothership). Re-pin to the dock if berthed.
    private void AdvanceShuttleClock(double travelSeconds)
    {
        double newT = SimTime + Math.Max(0.0, travelSeconds);
        _ship = new ShipState(_ship.Position, _ship.Velocity, newT, _ship.Charge);
        SimTime = newT;
        if (_dockedHavenId is not null)
        {
            HoldAtDock();
        }
        RunCacheDiscoveryWatch();
    }

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
        RendererInterop.PlayCue("burn");
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
        _ship = BerthState.CoMoving(_ephemeris!, dock.Id, SimTime, BerthState.BerthOffsetMeters, _ship.Charge);
        _dockOffset = _ship.Position - dockPos;               // the arm's reach is now the berth offset
        HoldAtDock();                                        // and HoldAtDock keeps it pinned every tick
        StaleFutureNodes();                                  // a berth cancels any pending burns
        _armedOrbitBodyId = null;                            // and disarms any pending auto-insert (issue #126)
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
    }

    private const double UndockPushMps = 300; // gentle shove off the clamp so the ship drifts clear

    private void Undock()
    {
        if (_dockedHavenId is null)
        {
            return;
        }

        SetDeckForDock(null); // back to the bare ship deck; pulls you aboard if you'd wandered up the tube

        // A gentle shove clear of the clamp — the ship drifts away with some motion of its own
        // rather than hanging dead-still on the dock (owner: "push the ship off so it has motion
        // away from the station"). Pushed radially out along the berthing arm.
        if (_ephemeris is not null)
        {
            Vector2d dockPos = _ephemeris.Position(_dockedHavenId, SimTime);
            Vector2d outward = (_ship.Position - dockPos).Normalized();
            _ship = _ship with { Velocity = _ship.Velocity + outward * UndockPushMps };
        }

        string name = _nearestBody?.Name ?? "the dock";
        _dockedHavenId = null;
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
