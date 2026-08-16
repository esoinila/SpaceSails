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

    // The sim-time this docking began (issue #410): the seated regulars' rota (PatronRota) is resolved at
    // THIS clock and baked for the whole visit, so a wing-unlock re-weld mid-dock doesn't jump anyone to a
    // new chair. Re-dock later (a new watch) and the room re-rolls — different faces, different seats.
    private double _dockVisitSimTime;

    // Re-weld the deck plan for the station we're tied up at, honoring the current unlock set. Keeps
    // the avatar where they stand — an opened wing appears without teleporting anyone.
    private void RebuildDockedDeck()
    {
        // Re-weld at the SAME watch this visit docked at, so an opened wing appears without re-rolling the
        // seated regulars mid-dock (their rota was baked when we tied up — issue #410).
        if (_dockedHavenId is { } id
            && HavenInterior.DockedDeck(id, UnlockedHatchesFor(id), _dockVisitSimTime, _oracleForce) is { } complex)
        {
            _deckPlan = complex;
        }
    }

    private readonly List<FirstPersonView.SkyBody> _skyBodies = [];

    private void ToggleDeck()
    {
        _deckMode = !_deckMode;
        _deckKeys.Clear();
        CancelAutoWalk(false);
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

    /// <summary>Flip between the top-down deck plan and first-person walk (the F key and the on-screen
    /// deck-view-toggle button both land here). Clears held movement keys so a key isn't "stuck down"
    /// across the mode switch.</summary>
    private void ToggleFirstPerson()
    {
        _fpMode = !_fpMode;
        _deckKeys.Clear();
        CancelAutoWalk(false); // #729: first person is tank controls and has no floor to click on
    }

    // Go-ashore (2026-07-07; the walk-through tube, 2026-07-08). Docking now welds the ship to the
    // station by a narrow umbilical with two automatic airlock doors, and you simply WALK your avatar
    // down it into the station — no gangway console, no teleport. While clamped to a haven with an
    // interior, _deckPlan becomes the combined ship+tube+station complex (HavenInterior.DockedDeck);
    // a haven without one keeps the plain ship deck, whose ⚓ gangway just says "nothing ashore yet".
    private const double ShipDeckTopY = 14;   // the ship's airlock hatch; above it lies the tube
    private const double StationFloorY = 22;  // past the tube you're in the station (lobby, then bar)

    private void SetDeckForDock(string? havenId)
    {
        _dockVisitSimTime = SimTime; // freeze the watch this docking sees the bar on (issue #410 rota)
        if (havenId is { } id
            && HavenInterior.DockedDeck(id, UnlockedHatchesFor(id), _dockVisitSimTime, _oracleForce) is { } complex)
        {
            _deckPlan = complex;
            _havenName = _ephemeris?.Bodies.FirstOrDefault(b => b.Id == id)?.Name ?? "the haven";
        }
        else
        {
            _deckPlan = ShipDeckNow();
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

    // #428 · ?ashore=1 — THE WALK, ALREADY WALKED. Stand the captain at the bar-room threshold of the
    // haven they just clamped onto, facing into the room, with the Deck up.
    //
    // Every bar beat we have — the oracle (#428), the stranger-bond (#429), the KAAMOS holder and the
    // Nebula adjuster (#411/#422), the Magpie's rota, the barkeep, the talking drinks — begins with the
    // same ship → airlock → tube → immigration hall → bar walk on EVERY boot. That walk is fine to play
    // and useless to test: in an MCP-driven tab the game is `document.hidden`, rAF is throttled and WASD
    // never lands, so not one of those beats could be smoke-tested at all. "A scene nobody can reach on
    // demand is a scene that ships broken" (this file's own neighbours).
    //
    // The position is NOT invented here: HavenInterior.BarThreshold derives it from the hall's north
    // doorway — the same gap the real walk crosses — so the cheat cannot drift from the geometry it is
    // pretending to have walked. Returns false (and moves nothing) at a berth with no interior to stand
    // in, so the caller can say so instead of teleporting the captain into a berth that has no bar.
    private bool StandAtTheBarThreshold()
    {
        if (_dockedHavenId is not { } id || !HavenInterior.HasInterior(id) || !_deckPlan.FollowCam)
        {
            return false;
        }

        (_avatarX, _avatarY, _avatarHeading) = HavenInterior.BarThreshold;
        RefreshAshore();
        // You are standing in a room, so show the room — the same two lines a shuttle arrival ashore
        // sets (TakeShuttleTo). Booting ashore onto the Nav map would put the captain in the bar and the
        // camera on the ecliptic, which is the sentence-versus-sim shape this project keeps paying for.
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;
        return true;
    }
}
