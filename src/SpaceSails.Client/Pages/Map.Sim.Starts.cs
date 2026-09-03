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

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — the start points: the picker, the /map?start= alias table, and where each one lays the ship down.
public partial class Map
{

    // --- Start points (2026-07-08; docked-starts rework 2026-07-18) ---
    // "Why should it always start from Earth?" — and the answer, owner ruling 2026-07-18: it never does.
    // Every named start now CLAMPS onto a station (DockedStarts maps the id → haven); the /map?start=<id>
    // URL routes through ApplyStart → the one shared clamp (StartDockedAtHaven). Body ids are
    // scenario data. Test:true starts are dev-only free-flying jumps (they exercise a free approach), hidden
    // from the picker. The picker itself no longer reads this list — it offers the live dockable-haven
    // registry (BerthStarts) — so this is now purely the /map?start= alias table with human labels.
    public sealed record StartPoint(string Id, string Icon, string Label, string Blurb, bool Test = false);

    private static readonly StartPoint[] StartPoints =
    [
        new("earth", "🌙", "Selene Gate — docked (Luna orbit)",
            "The classic first voyage, minus the Earth-centrism: begin clamped on at Selene Gate, in Luna's orbit, where the compute-core pods launch. The soft-catch lesson starts here."),
        new("cinder-roost", "🌋", "Cinder Roost — docked (Venus)",
            "In Venus' sulphur clouds — begin already clamped on at Cinder Roost, a short walk up the tube to The Cinder Lounge."),
        new("space-bar", "🍸", "The Rusty Roadstead — docked (Mars)",
            "Skip the haul to Mars — begin already clamped on at The Rusty Roadstead, a short walk up the tube to the bar's tables."),
        new("jupiter", "🪐", "The Red Eye — docked (Jupiter)",
            "Out among the Galilean moons — begin already clamped on at The Red Eye, Europa and Ganymede a short burn away."),
        new("saturn", "💍", "Ringside Exchange — docked (Saturn)",
            "In Saturn's rings — begin already clamped on at Ringside Exchange, a short walk up the tube to The Ringside Bar; Enceladus and Titan a burn away."),
        new("the-tilt", "❄️", "The Tilt — docked (Uranus)",
            "Way out at Uranus — begin already clamped on at The Tilt, a short walk up the tube to its cold, lonely bar."),
        new("the-deep", "🌀", "The Deep — docked (Neptune)",
            "At the edge of the charts — begin already clamped on at The Deep, above Neptune, a fuel pump and a long way from anyone."),
        new("wreck", "🚗", "The Derelict Roadster — alongside (test)",
            "Co-moving beside the lost roadster, sunward of Mars — for testing the fetch pickup.", Test: true),
        new("enceladus", "❄️", "Enceladus — alongside (test)",
            "Co-moving beside Enceladus, a short fall from its capture band — for testing the deep-well auto-orbit (#136).", Test: true),
    ];

    private bool _showStartPicker;

    // --- The dev start sites (#439) -----------------------------------------------------------------
    // Owner, 2026-07-26: "We should have a developer list of these quick starts in the UI also. We can
    // later disbale it." THIS is that switch: flip it to false and the whole section leaves the front door
    // (the catalogue itself lives in Core DevStarts, and docs/testing-guide.md keeps the prose twin). Left
    // collapsed by default so the logbook still opens on saves and berths, not on the service door.
    private const bool ShowDevStarts = true;
    private bool _showDevStarts;

    // Take a dev start. These are BOOT-TIME cheats — the world is built from the URL once, in OnInitialized
    // — so this is a full reload, not a router hop, or the cheat would be read against an already-built Sol.
    private void GoToDevStart(SpaceSails.Core.DevStarts.Entry entry) =>
        Navigation.NavigateTo(entry.Url, forceLoad: true);

    // Arrange the just-built (or, on a picker reopen, already-running) world for a chosen start.
    // Re-entrant: steps aboard and unclamps any current berth first, so it's safe to call any time.
    private void ApplyStart(string id)
    {
        _dockedHavenId = null;   // drop any prior clamp before the jump
        SetDeckForDock(null);    // back to the bare ship deck (pulls you aboard if you'd wandered ashore)

        // Owner ruling (2026-07-18): every start is a DOCKED start — clamp onto the haven the id names,
        // never a free-flying "fresh out of Earth orbit" spawn. The id resolves to a dockable haven via
        // DockedStarts (incl. the friendly aliases and the retired 'earth' → Selene Gate fallback), and
        // the ONE shared clamp lays it down — a co-moving berth, a welded interior (or the Nav map for a
        // pumps-only berth), HoldAtDock pinning it — so a picked start is byte-for-byte a real arrival.
        if (ResolveDockStartId(id) is { } havenId)
        {
            StartDockedAtHaven(havenId);
            MaybeGreetTutorialHome(havenId);
            return;
        }

        // The only non-docked starts left are the dev-only Test jumps (the derelict roadster, the
        // Enceladus capture band) that deliberately exercise a free-flying approach — kept for the bench.
        _ship = PlaceShipForStart(id);
        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        _showTutorial = false;
        _deckMode = false;
    }

    // #288: resolve a /map?dock=<id> value to a dockable-haven body id, or null if it names no berth.
    // Accepts both the haven's own body id (e.g. "the-tilt", "red-eye") and the friendly start aliases
    // (e.g. "ringside" → "ringside-exchange", "space-bar" → "the-space-bar"), so either form docks.
    private string? ResolveDockStartId(string idOrAlias)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        string havenId = DockedStarts.TryGetValue(idOrAlias, out string? mapped) ? mapped : idOrAlias;
        return _ephemeris.Bodies.FirstOrDefault(b => b.Id == havenId && DockableHavens.IsDockable(b))?.Id;
    }

    // #288: boot already clamped onto ANY dockable station haven — the smoke-test hook that generalises
    // ApplyStart's docked branch (four curated DockedStarts) to every haven in the scenario. Rides the
    // one true clamp (ClampOntoHaven: co-moving berth via BerthState.CoMoving, welds any interior, pins
    // via HoldAtDock, saves the resume vault) so a docked-cheat start is byte-for-byte a real arrival.
    // Steps ashore where there's a walkable interior; otherwise leaves you on the bare ship deck at Nav.
    private void StartDockedAtHaven(string havenId)
    {
        if (_ephemeris is null || ResolveDockHaven(havenId) is not { } dock || !DockableHavens.IsDockable(dock.Body))
        {
            return;
        }

        _showStartPicker = false;
        _showTutorial = false;          // an outer berth is no place for the Earth-anchored checklist
        SetDeckForDock(null);           // drop any deck we might be jumping from
        ClampOntoHaven(dock.Body, dock.Pos);

        if (HavenInterior.HasInterior(havenId))
        {
            (_avatarX, _avatarY, _avatarHeading) = (2.5, 6, Math.PI / 2); // in the airlock, facing up the tube
            _deckMode = true;
            _activeDesk = ShipDesk.Deck;
        }
        else
        {
            _deckMode = false;          // no walkable complex out here — sit on the Nav map, clamped on
        }

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
    }

    // The ship's state for a start point. Reuses InitializeShipState's finite-difference "co-moving
    // with a body" idiom, just keyed off a different body — a small radial offset keeps the ship clear
    // of the body's surface. "earth" (and any unknown id) falls back to the standard Earth spawn.
    private ShipState PlaceShipForStart(string id)
    {
        if (DockedStarts.TryGetValue(id, out string? dockBody))
        {
            return CoMovingBy(dockBody, 3_000); // just off the ~1 km station, well within dock reach
        }
        // Every one of these is a FREE park: the ship is let go alongside, not clamped on, so she flies
        // whatever orbit the standoff's direction gives her. #742 — laid along the Sun's radius, the
        // Enceladus spawn struck the ice at +9.17 h on one arrival phase of its 24, and the Europa spawn
        // struck Europa at +12.00 h on one of its own. That second one is the part nobody had looked for:
        // the ice moon was the REPORTED case, never the only one, because the geometry belongs to the
        // construction and not to Enceladus. CoOrbitalBy lays the standoff along the body's own track, and
        // the arrival phase stops deciding anything at either of them.
        return id switch
        {
            "jupiter" => CoOrbitalBy("europa", 2e7),           // clear of Europa's surface, amid the Galilean system
            "saturn" => CoOrbitalBy("ringside-exchange", 2e7), // by the ring station, Enceladus/Titan a burn away
            "enceladus" => CoOrbitalBy("enceladus", 5e6),      // (test) co-moving alongside Enceladus, ~5 Hill radii out (#136)
            "wreck" => CoOrbitalBy("derelict-roadster", 2_000), // (test) alongside the wreck, inside fetch-pickup range
            _ => InitializeShipState(),
        };
    }

    // A ship state co-moving with a body at boot (SimTime 0), a given distance radially outward from it
    // (from the Sun's frame). offsetMeters 0 sits right on the body; a few thousand metres clears a
    // station, ~1e7+ a moon. Delegates to the shared BerthState.CoMoving construction (#269). This is the
    // CLAMPED idiom — the docked starts above, where the berth owns the position the moment we arrive.
    private ShipState CoMovingBy(string bodyId, double offsetMeters)
        => BerthState.CoMoving(_ephemeris!, bodyId, 0, offsetMeters);

    // …and the FREE one (#742): the same standoff laid along the body's own track about its parent, so a
    // ship nobody is holding stays where she was let go instead of flying off on a slightly wrong ellipse.
    private ShipState CoOrbitalBy(string bodyId, double offsetMeters)
        => BerthState.CoOrbital(_ephemeris!, bodyId, 0, offsetMeters);

    // The Captain's "🧭 Set course to a start point…" button: bring the chooser back up mid-run so a
    // locale can be (re)picked from the chart-room, not just at boot. ApplyStart is re-entrant, so the
    // jump is safe from anywhere.
    private void ReopenStartPicker() => _showStartPicker = true;

    // #292/ruling-2 (owner 2026-07-18): the nav-screen checklist is no billboard. It greets ONLY a
    // brand-new captain beginning a fresh voyage at the cislunar tutorial home (Selene Gate, in Luna's
    // orbit — where the first lesson's compute-core pod actually launches), and seeds that pod RIGHT
    // HERE, at acceptance, relative to where the ship is NOW — never on a T=0 Earth clock. Any other
    // berth, or a captain who has already played, keeps the real estate clear; the Captain's Tutorials
    // tab (0) reopens a lesson deliberately (StartTutorial reseeds and re-shows). Called after the clamp
    // is laid, from every fresh-voyage start path (ApplyStart and the picker's ChooseBerthStart).
    private void MaybeGreetTutorialHome(string havenId)
    {
        if (havenId != TutorialHomeHavenId
            || !TutorialPromotion.ShouldPromote(TutorialStartMode.FreshFromEarth, _tutorialPlayed))
        {
            return;
        }

        _tutorialStep = 0;
        _showTutorial = true;
        SeedFirstHuntTarget(); // the target gets going when the lesson is taken on, wherever we are
    }
}
