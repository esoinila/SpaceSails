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

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — the telescope: what it may look at, what it locks onto and why, and the 🔭 hook a tip, a quest card or the ledger pulls to aim a scan.
public partial class Map
{
    // A wreck-orbit tip the Fixer hands over instead of a map pin (Tuesday plan PR-A): an estimate
    // in the game's voice, plus the true body id so the "point the scope" hook can aim an area scan.
    // Provenance (Giver/Station/AcquiredSimTime, PR-J) is optional and client-side — who slid it to
    // you, where, and when — so the Captain's ledger can attribute it; older tips render without it.
    public sealed record ScopeIntel(string Id, string BodyId, string Headline, IReadOnlyList<string> Lines,
        string? Giver = null, string? Station = null, double AcquiredSimTime = 0);

    private const string ScopeCanvasId = "scope-canvas";
    private const int ScopeSizePx = 280;
    private ScopeView? _scopeView;
    // #963 · THE SCOPE'S SWITCH LIVES ON THE SCOPE. The Nav toolbar used to carry a "Scope" toggle, and the
    // owner asked why: "Why have the scope enable/disable button competing for attention at the top of the
    // screen — that functionality should stay at the scope position." So the toolbar button is gone and the
    // window minimises into a button-sized tile in its own corner, which is also what stops the toolbar from
    // drowning out Plot — "the heart of the navigation process" — and the Add-burn keys beside it.
    //
    // Remembered for the session (a field, like FollowShip): a captain who tucks the scope away expects it to
    // stay tucked away until he says otherwise.
    //
    // #997 · The field stays — the tick reads it, and an eyepiece nobody can see is an eyepiece nobody
    // should be drawing — but the TOGGLE is gone. The OverlayShell owns the gesture now, for this window
    // and for the dossier alike, and the page is bound to its answer (@bind-Minimized) rather than
    // keeping a second switch of its own beside it.
    private bool _scopeMinimized;

    /// <summary>What the minimised tile names, so the tile is an instrument and not a mystery button: the same
    /// priority <see cref="PickScopeTarget"/> resolves — a manual pick, then a selected contact, then the
    /// navigation destination, then whatever is nearest. Read-only on purpose: the tile renders outside the
    /// frame, and PickScopeTarget writes the scope's lock label as a side effect.</summary>
    private string ScopeTileTargetName()
    {
        foreach (string? id in new[] { _scopeManualId, _selectedTargetId, _destinationBodyId })
        {
            if (id is not null && ScopeSubjectName(id) is { } named)
            {
                return named;
            }
        }

        return _nearestBody?.Name ?? "the sky";
    }

    /// <summary>The name behind a scope-subject id — a contact's callsign or a body's name — or null when the
    /// id names nothing that is still out there.</summary>
    private string? ScopeSubjectName(string id)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == id)
            {
                return npc.Active && !npc.Arrived && npc.CurrentlyObserved ? npc.Ship.Callsign : null;
            }
        }

        foreach (CelestialBody b in _ephemeris?.Bodies ?? [])
        {
            if (b.Id == id)
            {
                return IsBodyHidden(b.Id) ? null : b.Name;
            }
        }

        return null;
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
        // #962: hired muscle rides the carousel too. A hunter was absent from this list AND from
        // ResolveScopeTarget, so the one contact a captain most wants in the video box was the one
        // thing the scope could not be pointed at, by any route.
        foreach (HunterState hunter in _hunters)
        {
            ids.Add(hunter.Id);
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
        // #962: …and so does the TARGET OF INTEREST, which is how a hunter becomes the dossier's
        // subject in the first place (a map click on a collector marks interest, never selection).
        // Without this the owner could open the Debt Collector's book, press every button on it,
        // and still watch the video box show his destination: "it is like our telescope pirate is
        // high on drugs". TacticalTargetId is the same id the dossier renders, so the book and the
        // box are now looking at one contact.
        if (TacticalTargetId is { } tactical && ResolveScopeTarget(tactical) is { } picked)
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
            // #954: AUTO reads the (now hysteresis-held) nearest body, so the box no longer ping-pongs
            // between a planet and the station in its Hill sphere every station orbit. Where there IS a
            // hierarchy, the sub-line says whose sphere we are in — the box still names, and still draws,
            // the object actually locked, so the words and the picture can never disagree.
            string? note = _nearestParentName is { } parentName && _nearestChildName == body.Name
                ? NearestRule.OrbitsNote(parentName)
                : null;
            return new ScopeView.Target(
                ScopeView.TargetKind.Body, body.Name, note,
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

        // #962: hired muscle is a legitimate thing to put in the video box — she is a contact whose
        // state the gun deck already reads exactly, so there is no optical-truth gate to pass.
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == id)
            {
                return new ScopeView.Target(
                    ScopeView.TargetKind.Freighter, hunter.Callsign, "hired muscle 🐺",
                    hunter.State.Position, hunter.State.Velocity,
                    0, HunterColor, InPlasmaAt(hunter.State.Position));
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
