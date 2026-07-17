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

// Map.Deck — the walkabout: deck mode and first person, the avatar and doors/hatches, the
// scope, the rum ledger and the view-objects you lean into. Split from Map.razor per #251.
public partial class Map
{

    /// <summary>The Galley's "Pour a tot" button funnels through the exact same PourRum the deck
    /// cantina console uses (see InteractAtConsole's Cantina case) — one rum ledger, two doors.</summary>
    private void PourRumFromGalley() => ShowPulseMessage(PourRum(null));

    private bool RumWobbleActive => (_lastTimestampMs ?? 0) < _wobbleUntilMs;

    // M11 — the telescope (worldbuilding notes §5)
    // M12 — deck view (walk your ship)
    private bool _deckMode;
    private bool _fpMode;
    private double _deckPanX, _deckPanY;
    private DeckView? _deckView;
    private FirstPersonView? _fpView;
    private double _avatarX = DeckPlan.Ship.SpawnX, _avatarY = DeckPlan.Ship.SpawnY, _avatarHeading; // 0 = facing the bow glass

    // The active deck (go-ashore, 2026-07-07; walk-through tube, 2026-07-08). The bare ship by
    // default; while docked at a haven with an interior it becomes the combined ship+tube+station
    // complex, which you walk across continuously (see SetDeckForDock). The renderers and the avatar
    // loop all read _deckPlan, so nothing else needs to know which deck is active.
    private DeckPlan _deckPlan = DeckPlan.Ship;
    private bool _ashore;                          // true once you're past the tube, in the station room
    private string _havenName = "";               // the docked haven welded on, or ""

    // Doors that grow the world (Wednesday plan §3 PR-F): the set of station hatches cracked open this
    // session, as composite "<bodyId>:<hatchId>" keys. A hatch that grows a wing (HavenInterior.
    // HatchGrowsWing) welds its back room onto the deck plan when unlocked. Per-session only — the
    // owner accepted that for v1 (Wednesday plan §1); it lives beside the other session-scoped state.
    private readonly HashSet<string> _unlockedHatches = [];

    // The bare hatch ids cracked open at a given station — the subset HavenInterior.DockedDeck needs.
    private IReadOnlySet<string> UnlockedHatchesFor(string bodyId) =>
        _unlockedHatches
            .Where(k => k.StartsWith(bodyId + ":", StringComparison.Ordinal))
            .Select(k => k[(bodyId.Length + 1)..])
            .ToHashSet();

    private bool IsHatchUnlocked(string bodyId, string hatchId) =>
        _unlockedHatches.Contains($"{bodyId}:{hatchId}");

    // Crack a hatch open for the session and, if it grows a wing, weld the room on by rebuilding the
    // docked deck plan (the world literally grew a room behind you).
    private void UnlockHatch(string bodyId, string hatchId)
    {
        if (_unlockedHatches.Add($"{bodyId}:{hatchId}"))
        {
            RebuildDockedDeck();
        }
    }

    // Re-weld the deck plan for the station we're tied up at, honoring the current unlock set. Keeps
    // the avatar where they stand — an opened wing appears without teleporting anyone.
    private void RebuildDockedDeck()
    {
        if (_dockedHavenId is { } id && HavenInterior.DockedDeck(id, UnlockedHatchesFor(id)) is { } complex)
        {
            _deckPlan = complex;
        }
    }

    // A wreck-orbit tip the Fixer hands over instead of a map pin (Tuesday plan PR-A): an estimate
    // in the game's voice, plus the true body id so the "point the scope" hook can aim an area scan.
    // Provenance (Giver/Station/AcquiredSimTime, PR-J) is optional and client-side — who slid it to
    // you, where, and when — so the Captain's ledger can attribute it; older tips render without it.
    private sealed record ScopeIntel(string Id, string BodyId, string Headline, IReadOnlyList<string> Lines,
        string? Giver = null, string? Station = null, double AcquiredSimTime = 0);

    private readonly List<FirstPersonView.SkyBody> _skyBodies = [];
    private readonly HashSet<string> _deckKeys = [];
    private const double AvatarSpeed = 9.0; // deck units per real second

    private const string ScopeCanvasId = "scope-canvas";
    private const int ScopeSizePx = 280;
    private ScopeView? _scopeView;
    private bool _showScope = true;

    private void ToggleScope() => _showScope = !_showScope;

    private void ToggleDeck()
    {
        _deckMode = !_deckMode;
        _deckKeys.Clear();
    }

    // Movement keys are held-state (smooth walk); E interacts; Q returns to the map. Returns
    // true when the key was consumed by the deck so it can't also fire a thrust pulse.
    private bool HandleDeckKey(string key)
    {
        switch (key)
        {
            case "w" or "W" or "ArrowUp":
            case "a" or "A" or "ArrowLeft":
            case "s" or "S" or "ArrowDown":
            case "d" or "D" or "ArrowRight":
                _deckKeys.Add(Canonical(key));
                return true;
            case "f" or "F":
                ToggleFirstPerson();
                return true;
            case "q" or "Q":
                SwitchDesk(ShipDesk.Nav); // the deck is continuous now — Q always steps up to the helm
                return true;
            case "e" or "E":
                InteractAtConsole();
                return true;
            case "b" or "B":
                // PR-WIRE: bank at the contact's table — deposit, withdraw or borrow (in person).
                OpenBankAtBar();
                return true;
            default:
                return false;
        }
    }

    private void MoveAvatar(double dtRealSeconds)
    {
        double dt = Math.Min(dtRealSeconds, 0.1);

        // Three tots of rum and the deck tilts (M21): the heading sways for a while. Purely
        // cosmetic mischief — collision and interaction are unaffected.
        double wobble = (_lastTimestampMs ?? 0) < _wobbleUntilMs
            ? Math.Sin((_lastTimestampMs ?? 0) * 0.004) * 0.9 * dt
            : 0;

        if (_fpMode)
        {
            _avatarHeading += wobble;
            // Tank controls: A/D turn, W/S walk along the view direction.
            const double turnRate = 2.6; // rad/s
            if (_deckKeys.Contains("a")) _avatarHeading += turnRate * dt;
            if (_deckKeys.Contains("d")) _avatarHeading -= turnRate * dt;
            double walk = (_deckKeys.Contains("w") ? 1 : 0) - (_deckKeys.Contains("s") ? 1 : 0);
            if (walk != 0)
            {
                double step = AvatarSpeed * dt * walk;
                (_avatarX, _avatarY) = _deckPlan.Move(_avatarX, _avatarY,
                    Math.Cos(_avatarHeading) * step, Math.Sin(_avatarHeading) * step);
                RefreshAshore();
            }
            return;
        }

        double dx = 0, dy = 0;
        if (_deckKeys.Contains("w")) dy += 1;   // +Y = port (up on screen)
        _ = wobble; // top-down: applied to the move vector below
        if (_deckKeys.Contains("s")) dy -= 1;
        if (_deckKeys.Contains("a")) dx -= 1;
        if (_deckKeys.Contains("d")) dx += 1;
        if (dx == 0 && dy == 0)
        {
            return;
        }

        double norm = Math.Sqrt(dx * dx + dy * dy);
        double step2 = AvatarSpeed * dt;
            if (wobble != 0 && (dx != 0 || dy != 0))
            {
                double a = Math.Sin((_lastTimestampMs ?? 0) * 0.004) * 0.45;
                (dx, dy) = (dx * Math.Cos(a) - dy * Math.Sin(a), dx * Math.Sin(a) + dy * Math.Cos(a));
            }
        (_avatarX, _avatarY) = _deckPlan.Move(_avatarX, _avatarY, dx / norm * step2, dy / norm * step2);
        _avatarHeading = Math.Atan2(dy, dx);
        RefreshAshore();
    }

    // Sky lights for the first-person windows, from the REAL ephemeris: world angle from the
    // ship to each body, angular radius from its physical size and live distance — the sun
    // blazes bigger the closer you fly.
    private void BuildSkyBodies()
    {
        _skyBodies.Clear();
        foreach (CelestialBody body in _ephemeris!.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // no sky-dot for an uncharted body in the windows (PR-A)
            Vector2d offset = _ephemeris.Position(body.Id, SimTime) - _ship.Position;
            double distance = offset.Length;
            if (distance <= body.BodyRadius)
            {
                continue;
            }

            _skyBodies.Add(new FirstPersonView.SkyBody(
                Math.Atan2(offset.Y, offset.X),
                Math.Asin(Math.Clamp(body.BodyRadius / distance, 0, 1)),
                BodyColor(body.Id),
                body.Id == "sun"));
        }
    }

    private string LocationHint() => _deckPlan.Location(_avatarX, _avatarY);

    private void InteractAtConsole()
    {
        switch (_deckPlan.NearestConsole(_avatarX, _avatarY))
        {
            case DeckPlan.ConsoleKind.Airlock:
                // Only the bare-ship gangway raises this (the docked complex drops the console — you
                // walk the tube). So it's always the "can't go ashore here" nudge.
                ShowPulseMessage(_dockedHavenId is null
                    ? "No gangway rigged — clamp onto a haven first (⚓ Dock)."
                    : "Nothing to step into here — no deck ashore at this berth (yet).");
                break;
            case DeckPlan.ConsoleKind.BarPatron:
                TalkToStranger();
                break;
            case DeckPlan.ConsoleKind.Hatch:
                KnockOnHatch();
                break;
            case DeckPlan.ConsoleKind.Stash:
                LiftStash();
                break;
            case DeckPlan.ConsoleKind.ViewObject:
                ViewNearbyObject();
                break;
            case DeckPlan.ConsoleKind.Helm:
                SwitchDesk(ShipDesk.Nav);
                ShowPulseMessage("Back at the helm");
                break;
            case DeckPlan.ConsoleKind.NavPost:
                SwitchDesk(ShipDesk.Nav);
                if (!PlotMode)
                {
                    TogglePlotMode();
                }
                ShowPulseMessage("Nav post: plotting table lit");
                break;
            case DeckPlan.ConsoleKind.Scope:
                SwitchDesk(ShipDesk.Sensors);
                ShowPulseMessage("Scope alcove: sensors online");
                break;
            case DeckPlan.ConsoleKind.Cantina:
                SwitchDesk(ShipDesk.Galley);
                ShowPulseMessage("Cantina: galley's this way");
                break;
            case DeckPlan.ConsoleKind.Head:
                ShowPulseMessage(HeadQuip());
                break;
            case DeckPlan.ConsoleKind.Vent:
                VentCharge();
                break;
            case DeckPlan.ConsoleKind.Cargo:
                ShowPulseMessage(_cargoUnits > 0
                    ? $"Hold: {_cargoUnits} units (worth {_cargoValue:N0} cr)"
                    : "Hold: empty. The fence weeps.");
                break;
            case DeckPlan.ConsoleKind.Shuttle:
                if (_captureEngaged && SelectedCaptureTarget() is { } prey)
                {
                    LaunchShuttleRun(prey);
                }
                else if (_plunderOpportunityTargetId is not null)
                {
                    // In range, but the felony isn't authorized yet — the shuttle waits on the word.
                    ShowPulseMessage("Shuttle's fuelled — but boarding's piracy. Authorize the 🏴 plunder first (the Nav HUD's asking).");
                }
                else
                {
                    ShowPulseMessage("Shuttle ready in the cradle. K-77 and R-3B standing by.");
                }
                break;
            case DeckPlan.ConsoleKind.ShuttleAirlock:
                OpenShuttleBayDoor();
                break;
            case DeckPlan.ConsoleKind.CommsSeat:
                SwitchDesk(ShipDesk.Comms);
                ShowPulseMessage("Comms seat: patched through");
                break;
            case DeckPlan.ConsoleKind.TacticalSeat:
                SwitchDesk(ShipDesk.WarRoom);
                ShowPulseMessage("Tactical seat: war room manned");
                break;
            case DeckPlan.ConsoleKind.TradeSeat:
                SwitchDesk(ShipDesk.Trade);
                ShowPulseMessage("Trade seat: ledgers open");
                break;
        }
    }

    private string? _scopeManualId; // null = AUTO

    // Everything the scope can look at right now: observed ships first, then every body.
    private List<string> ScopeCandidates()
    {
        List<string> ids = [];
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Active && !npc.Arrived && npc.CurrentlyObserved) ids.Add(npc.Ship.Id);
        }
        foreach (CelestialBody b in _ephemeris!.Bodies)
        {
            if (IsBodyHidden(b.Id)) continue; // hidden bodies never ride the scope carousel (PR-A)
            ids.Add(b.Id);
        }
        return ids;
    }

    private void CycleScopeTarget(int step)
    {
        List<string> ids = ScopeCandidates();
        if (ids.Count == 0) return;
        int i = _scopeManualId is null ? (step > 0 ? -1 : 0) : ids.IndexOf(_scopeManualId);
        _scopeManualId = ids[((i + step) % ids.Count + ids.Count) % ids.Count];
        if (_scopeView is not null) _scopeView.LockLabel = "◆ TRACK";
    }

    // Auto-lock priority: the selected tracked target, else the nearest currently-observed
    // contact, else the nearest celestial body — the scope always has something to show a
    // pirate. A manual ▶◀ pick overrides. Optical truth only: unobserved ships never appear.
    private ScopeView.Target PickScopeTarget()
    {
        if (_scopeManualId is not null)
        {
            ScopeView.Target? manual = ResolveScopeTarget(_scopeManualId);
            if (manual is not null) return manual.Value;
            _scopeManualId = null; // target gone (arrived / out of sensor range): fall back to auto
        }

        // M29: a deliberately SELECTED contact outranks the destination while the selection
        // lives (owner: "target selection should work even when the ship is on course to
        // orbit") — deselect and the DEST lock returns.
        if (_selectedTargetId is not null && ResolveScopeTarget(_selectedTargetId) is { } picked)
        {
            if (_scopeView is not null) _scopeView.LockLabel = "◆ TRACK";
            return picked;
        }

        // M25: otherwise the destination owns the scope (owner: "the destination should also be
        // pictured in the video box"). Manual prev/next cycling above still overrides it.
        if (_destinationBodyId is not null && ResolveScopeTarget(_destinationBodyId) is { } dest)
        {
            if (_scopeView is not null) _scopeView.LockLabel = "🎯 DEST";
            return dest;
        }

        if (_scopeView is not null) _scopeView.LockLabel = "◆ AUTO";

        NpcState? locked = null;
        bool lockedBySelection = false;
        double bestSq = double.MaxValue;
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived || !npc.CurrentlyObserved) continue;
            if (npc.Ship.Id == _selectedTargetId) { locked = npc; lockedBySelection = true; break; }
            double d = (npc.State.Position - _ship.Position).LengthSquared;
            if (d < bestSq) { (bestSq, locked) = (d, npc); }
        }

        // AUTO means the nearest OBJECT (owner, M20): a planet filling the window beats a
        // freighter half a system away. A selected target still wins outright.
        if (!lockedBySelection && _nearestBody is not null
            && (_nearestBodyPosition - _ship.Position).LengthSquared < bestSq)
        {
            locked = null;
        }

        if (locked is not null)
        {
            return new ScopeView.Target(
                locked.Ship.IsPod ? ScopeView.TargetKind.Pod : ScopeView.TargetKind.Freighter,
                locked.Ship.Callsign, $"cargo: {locked.Ship.CargoClass} ({locked.Ship.CargoUnits}u)",
                locked.State.Position, locked.State.Velocity,
                0, NpcColor, InPlasmaAt(locked.State.Position),
                IsDepot: locked.Ship.DepotBodyId is not null);
        }

        if (_nearestBody is CelestialBody body)
        {
            return new ScopeView.Target(
                ScopeView.TargetKind.Body, body.Name, null,
                _nearestBodyPosition, _nearestBodyVelocity,
                body.BodyRadius, BodyColor(body.Id), InPlasmaAt(_nearestBodyPosition),
                IsHaven: body.IsHaven, Dockable: IsDockableHaven(body));
        }

        return new ScopeView.Target(ScopeView.TargetKind.None, "", null, Vector2d.Zero, Vector2d.Zero, 0, default, false);
    }

    private ScopeView.Target? ResolveScopeTarget(string id)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == id && npc.Active && !npc.Arrived && npc.CurrentlyObserved)
            {
                return new ScopeView.Target(
                    npc.Ship.IsPod ? ScopeView.TargetKind.Pod : ScopeView.TargetKind.Freighter,
                    npc.Ship.Callsign, $"cargo: {npc.Ship.CargoClass} ({npc.Ship.CargoUnits}u)",
                    npc.State.Position, npc.State.Velocity,
                    0, NpcColor, InPlasmaAt(npc.State.Position),
                    IsDepot: npc.Ship.DepotBodyId is not null);
            }
        }

        foreach (CelestialBody body in _ephemeris!.Bodies)
        {
            if (body.Id == id && !IsBodyHidden(body.Id)) // don't resolve a scope target onto a hidden body (PR-A)
            {
                Vector2d position = _ephemeris.Position(body.Id, SimTime);
                const double h = 1.0;
                Vector2d velocity = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
                return new ScopeView.Target(
                    ScopeView.TargetKind.Body, body.Name, null,
                    position, velocity, body.BodyRadius, BodyColor(body.Id), InPlasmaAt(position),
                    IsHaven: body.IsHaven, Dockable: IsDockableHaven(body));
            }
        }

        return null;
    }

    // ---- M21: the rum locker (PR-11: shared 1:1 by the Galley desk via PourRumFromGalley) ----
    private int _rumTots;
    private double _lastRumMs = double.MinValue;
    private double _wobbleUntilMs = double.MinValue;
    private string? _lastRumLine;

    private static readonly string[] RumLines =
    [
        "Rum, dark as the void. The view is free.",
        "A tot for the helm. K-77 pretends not to count.",
        "The good bottle — saved since Luna. Today qualifies.",
        "Grog ration doubled. Morale follows.",
        "V-1K reports the rum locker 'adequately defended'.",
        "To absent friends and slow freighters. 🍹",
    ];

    private string PourRum(string? overrideLine)
    {
        double now = _lastTimestampMs ?? 0;
        _rumTots = now - _lastRumMs < 90_000 ? _rumTots + 1 : 1;
        _lastRumMs = now;
        RendererInterop.PlayCue("rum");
        string line = _rumTots >= 3
            ? "That was the third tot. The deck feels… tilty. 🍹"
            : overrideLine ?? RumLines[(int)((SimTime / 60) % RumLines.Length)];
        if (_rumTots >= 3)
        {
            _wobbleUntilMs = now + 25_000;
        }

        _lastRumLine = line;
        return line;
    }

    // ---- The HEAD 🚽 (3D-reno Phase 3): a gag fixture in the starboard berths. Pure flavor —
    //      pressing E by the space-toilet returns a deterministic one-liner, no state, no Core. ----
    private static readonly string[] HeadLines =
    [
        "The space-toilet gurgles, thinks better of it, and settles down. 🚽",
        "Zero-g plumbing: brace, aim, and commit. No do-overs.",
        "The vacuum flush could de-orbit a small moon. You use it sparingly.",
        "V-1K has zip-tied an 'OUT OF ORDER' sign over it. You do not trust V-1K.",
        "'Recycled water,' the placard insists. You elect not to ask from what.",
        "Somewhere a pipe knocks three times, like it wants to talk. 🚽",
    ];

    private string HeadQuip() => HeadLines[(int)((SimTime / 60) % HeadLines.Length)];

    /// <summary>Flip between the top-down deck plan and first-person walk (the F key and the on-screen
    /// deck-view-toggle button both land here). Clears held movement keys so a key isn't "stuck down"
    /// across the mode switch.</summary>
    private void ToggleFirstPerson()
    {
        _fpMode = !_fpMode;
        _deckKeys.Clear();
    }

    // The 🔭 hook the intel card (and the quest card) carries: aim a prioritized area scan at where
    // the wreck should be a touch from now, then drop the captain at the Sensors desk to watch it
    // land. The box is deliberately generous — good enough to catch the wreck's drift before the
    // pass completes (the reveal check does the exact geometry).
    private void PointScopeWhereIntelSays(ScopeIntel intel)
    {
        if (_ephemeris is null || _trackingPost is null)
        {
            return;
        }
        if (!IsBodyHidden(intel.BodyId))
        {
            // Already charted — the tip has served its purpose; drop the stale card.
            _scopeIntel.RemoveAll(si => si.Id == intel.Id);
            return;
        }
        double aimTime = SimTime + IntelScanLeadSeconds;
        Vector2d center = _ephemeris.Position(intel.BodyId, aimTime);
        string label = $"intel fix · {intel.Headline}";
        _trackingPost.EnqueueAndPrioritize(SensorTask.AreaScan(center, WreckScanRadiusM, label));
        SwitchDesk(ShipDesk.Sensors);
        ShowPulseMessage("🔭 Scope slewing to the intel fix — watch the Sensors desk. Warp time to let the pass land.");
    }

    private ScopeIntel? ScopeIntelById(string nodeId) =>
        _scopeIntel.FirstOrDefault(si => $"scopeintel:{si.Id}" == nodeId);

    // Go-ashore (2026-07-07; the walk-through tube, 2026-07-08). Docking now welds the ship to the
    // station by a narrow umbilical with two automatic airlock doors, and you simply WALK your avatar
    // down it into the station — no gangway console, no teleport. While clamped to a haven with an
    // interior, _deckPlan becomes the combined ship+tube+station complex (HavenInterior.DockedDeck);
    // a haven without one keeps the plain ship deck, whose ⚓ gangway just says "nothing ashore yet".
    private const double ShipDeckTopY = 14;   // the ship's airlock hatch; above it lies the tube
    private const double StationFloorY = 22;  // past the tube you're in the station (lobby, then bar)

    private void SetDeckForDock(string? havenId)
    {
        if (havenId is { } id && HavenInterior.DockedDeck(id, UnlockedHatchesFor(id)) is { } complex)
        {
            _deckPlan = complex;
            _havenName = _ephemeris?.Bodies.FirstOrDefault(b => b.Id == id)?.Name ?? "the haven";
        }
        else
        {
            _deckPlan = DeckPlan.Ship;
            _havenName = "";
            PullAvatarAboard();
        }
        _deckPanX = _deckPanY = 0; // fresh deck: drop any drag-pan so the follow-cam isn't offset
        _shuttleBayStops = null;   // a fresh deck shuts any open shuttle-bay hatch (#163)
        RefreshAshore();
    }

    // Casting off: if you'd wandered up the tube or into the station, step you back aboard so you
    // never undock standing in a berth that's no longer welded on.
    private void PullAvatarAboard()
    {
        if (_avatarY > ShipDeckTopY)
        {
            (_avatarX, _avatarY, _avatarHeading) = (2.5, 8, -Math.PI / 2); // back in the airlock corridor, facing in
        }
        _ashore = false;
    }

    // "Ashore" is a place on the continuous deck now, not a mode: true once you're past the tube in
    // the station room. Kept fresh as you walk so quest/status flavor can read it.
    private void RefreshAshore() => _ashore = _deckPlan.FollowCam && _avatarY > StationFloorY;

    // --- Ashore quests (M-Q1): the hooded stranger at the bar table ---

    // Walk up to a booth and press E. Which patron you're next to (from their console label) sets who
    // you're dealing with and their trade — One-Eye Silas fences bounties (hunts), Madam Coil runs
    // parcels (cargo runs). If you already owe this giver a job, they just nod at it.
    // A Gen-AI image the player is currently viewing (a souvenir, a lore prop), or null. Pressing E on
    // a ViewObject console pops it up; E again (or the close button / clicking away) dismisses it.
    private DeckPlan.ConsoleSpot? _viewObject;

    private void ViewNearbyObject()
    {
        if (_viewObject is not null)
        {
            _viewObject = null; // E again closes
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.ViewObject } spot)
        {
            _viewObject = spot;
        }
    }

    private void CloseViewObject() => _viewObject = null;

    private void KnockOnHatch()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Hatch } hatch)
        {
            return;
        }
        string id = HatchId(hatch.Label);

        // An opened expansion joint (PR-F): the hatch is already cracked and the back room is welded
        // on — there's nothing to knock on, just a doorway to step through.
        if (_dockedHavenId is { } open && HavenInterior.HatchGrowsWing(open, id) && IsHatchUnlocked(open, id))
        {
            ShowPulseMessage($"{hatch.Label} stands open — the back room's yours. Step inside. 📂");
            return;
        }

        // Is this the specific hatch a crack job sent us to (at this station)? If so, the knock isn't
        // an idle rap — it brings up the keypad.
        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.TargetShipId == id && q.SourceBodyId == _dockedHavenId);
        if (job is { State: QuestState.Active })
        {
            _pinJob = job;
            _pinHatch = hatch;
            _pinEntry = "";
            return;
        }
        if (job is not null && job.State != QuestState.Active)
        {
            ShowPulseMessage($"{hatch.Label} — already cracked. You pull it shut behind you.");
            return;
        }

        ShowPulseMessage($"{hatch.Label} — sealed. You knock; only the station's hum answers. 🔒");
    }

    // Parse a hatch label ("🔒 BONDED STORES · V-06") without leaning on the exact separator glyph. The
    // id is the last whitespace token ("V-06"); the department is the run of all-letter tokens
    // ("BONDED STORES") — which skips the emoji tag, the separator, and the id itself.
    private static string HatchId(string label)
    {
        string[] parts = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : label.Trim();
    }

    private static string HatchDept(string label) =>
        string.Join(' ', label.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.All(char.IsLetter)));

    // A stable 4-digit access code for a hatch — the same code every time so the Fixer can quote it and
    // it still works when you walk over. Deterministic (no RNG in player-facing quest gen).
    private static string MakePin(string hatchId) =>
        (hatchId.Sum(ch => ch) * 137 % 9000 + 1000).ToString(CultureInfo.InvariantCulture);

    // --- Keypad state for cracking a locked hatch -----------------------------------------------------
    private Quest? _pinJob;                 // the crack job whose hatch we're keying into, or null
    private DeckPlan.ConsoleSpot? _pinHatch; // the hatch being cracked (for the keypad's header)
    private string _pinEntry = "";           // digits keyed so far (max 4)

    // Four slots, filled left to right: keyed digits, then "·" placeholders.
    private string PinDisplay => string.Concat(Enumerable.Range(0, 4)
        .Select(i => i < _pinEntry.Length ? _pinEntry[i] : '·'));

    private void PinPush(string digit)
    {
        if (_pinEntry.Length < 4)
        {
            _pinEntry += digit;
        }
    }

    private void PinClear() => _pinEntry = "";

    private void CancelPin()
    {
        _pinJob = null;
        _pinHatch = null;
        _pinEntry = "";
    }

    private void SubmitPin()
    {
        if (_pinJob is not { } job)
        {
            return;
        }
        if (_pinEntry == job.Pin)
        {
            RendererInterop.PlayCue("board");
            if (_dockedHavenId is { } station && HavenInterior.HatchGrowsWing(station, job.TargetShipId))
            {
                // Doors that grow the world (PR-F): this hatch opens a real back room. Weld it on and
                // leave the job Active — you still have to walk in and lift the package off the shelf.
                UnlockHatch(station, job.TargetShipId);
                ShowPulseMessage("The lock blinks green — the hatch grinds aside onto a dark back room. Something's on the shelf inside. Step in and take it. 📦");
            }
            else
            {
                // A plain lockup: the package is simply behind the panel, pocketed on the spot.
                job.State = QuestState.PickedUp;
                ShowPulseMessage("The lock blinks green — the hatch sighs open. You pocket the package and pull it shut behind you. 📦");
            }
            CancelPin();
        }
        else
        {
            _pinEntry = "";
            ShowPulseMessage("The panel buzzes red — wrong code. 🔴");
        }
    }

    // The quest card's 🔭 button (mirrors the Comms intel card): find the live wreck tip (rebuilding
    // it if it was cleared) and aim the scope from wherever the player is standing.
    private void PointScopeForActiveFetch()
    {
        ScopeIntel? tip = _scopeIntel.FirstOrDefault();
        if (tip is null)
        {
            Quest? fetch = _quests.FirstOrDefault(
                q => q is { Kind: QuestKind.Fetch } && q.SourceBodyId is { } s && IsBodyHidden(s));
            if (fetch?.SourceBodyId is { } wid)
            {
                tip = BuildWreckIntel(wid);
            }
        }
        if (tip is not null)
        {
            PointScopeWhereIntelSays(tip);
        }
    }

    // The ledger's scope-tip 🔭 (by tip id): same handler as the Comms intel card — aim the scan, jump
    // to Sensors.
    private void PointScopeFromLedger(string tipId)
    {
        if (_scopeIntel.FirstOrDefault(si => si.Id == tipId) is { } tip)
        {
            PointScopeWhereIntelSays(tip);
        }
    }
}
