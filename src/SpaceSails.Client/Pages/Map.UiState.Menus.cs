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
/// #251 · THE CONTEXT MENUS THE MAP OPENS UNDER A CLICK — the body menu, the disambiguation picker that
/// stands in front of it when a press could have meant three things, the ship menu and the empty-sky menu,
/// together with the clamp that keeps any of them on screen. Opening and closing state only: what each
/// entry DOES is the desk's or the plot's, and this file never decides it. Split out of
/// <c>Map.UiState.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
///
/// <para><c>PickHint</c> came with the picker rather than with the chips it was filed among: its one caller
/// is <c>PickMenuPanel</c>, which is the menu this file opens. Byte-identical code, nothing re-decided.</para>
/// </summary>
public partial class Map
{
    // #203 item 3: each disambiguation-picker entry states what opening it will do.
    private static string PickHint(PickCandidate pick) => pick.Kind switch
    {
        'S' => "Open this contact — track it, mark a target of interest, or set up a trade run",
        'H' => "Open this hunter — put the scope on it, or work a firing solution in the war room",
        'B' => "Open this body — set it as your destination or arm the autopilot to it",
        _ => "Open the one you meant",
    };

    private CelestialBody? _bodyMenuBody;   // planet click menu: which body, where on screen
    private double _bodyMenuX, _bodyMenuY;

    private void CloseBodyMenu()
    {
        _bodyMenuBody = null;
        StateHasChanged();
    }

    // ---- #253: keep every click menu inside the viewport ----
    // The owner's playtest: The Tilt sat bottom-right, its menu ran off the bottom and hid the 🚀
    // Long haul action. The map menus can't measure themselves without interop, so we estimate the
    // box deterministically — the CSS max-width, plus a per-row height × the rows the menu will draw
    // (header chrome folded into the base) — and hand it to MenuLayout, which flips it above/left of
    // the click near an edge. Over-estimating rows only flips a touch early; it never overflows.
    private const double MenuBoxWidthPx = 256;  // .map-body-menu max-width: 16rem
    private const double MenuRowPx = 30;        // one btn-sm/info line + the d-grid gap
    private const double MenuChromePx = 44;      // p-2 padding + the title/close header row

    /// <summary>The on-screen top-left for a menu anchored at the click, sized from its visible row
    /// count, clamped so no action ever renders past a viewport edge (#253).</summary>
    private (double X, double Y) ClampMenu(double anchorX, double anchorY, int rows) =>
        MenuLayout.ClampMenuPosition(
            anchorX, anchorY,
            MenuBoxWidthPx, MenuChromePx + rows * MenuRowPx,
            _viewportWidth, _viewportHeight);

    /// <summary>#997 wave 10 · The clamped anchor as the one inline declaration the four click menus wear.
    ///
    /// <para>It is a method rather than four <c>style="left: @@(…)px; top: @@(…)px"</c> attributes because a
    /// COMPONENT attribute may not mix markup and C# (RZ9986) and <see cref="Components.OverlayShell"/> draws
    /// these roots now. The rendering is the one the four menus have always written — <c>F0</c>, invariant —
    /// said once instead of eight times, and <c>MenuLayoutTests</c> owns the numbers that go into it.</para>
    /// </summary>
    private static string MenuAnchorStyle((double X, double Y) at) =>
        $"left: {at.X.ToString("F0", CultureInfo.InvariantCulture)}px; "
        + $"top: {at.Y.ToString("F0", CultureInfo.InvariantCulture)}px";

    public readonly record struct PickCandidate(char Kind, string Id, string Label, string Icon);

    private List<PickCandidate>? _pickMenu;
    private double _pickMenuX, _pickMenuY;

    private const double PickRadiusPx = 15;     // the forgiving direct-hit radius
    private const double PickNearRadiusPx = 28; // near-miss radius for the lane-vs-planet tiebreak
    // #402 follow-up: the per-body pick radius (and the deflection threat rock's widened, always-one-
    // click-away tolerance) is the pure, tested MapPick rule in Core — see the picker loops below.

    private void ClosePickMenu()
    {
        _pickMenu = null;
        StateHasChanged();
    }

    /// <summary>Everything a click at (x, y) could plausibly mean, ranked by likely intent:
    /// live contacts first, then depots, then bodies (each group nearest-first). Corridors and
    /// the empty-sky scan are appended by the pointer-up path — never here — so they always
    /// rank last, per the owner's rule that a lane is the LEAST likely meaning near a planet.</summary>
    private List<PickCandidate> CollectPointCandidates(double x, double y, double radiusPx)
    {
        var found = new List<(PickCandidate Pick, double DistSq, int Rank)>();
        double r2 = radiusPx * radiusPx;
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived) continue;
            bool isDepot = npc.Ship.DepotBodyId is not null;
            // PR-C (the Barnacle case): the dim last-seen marker answers clicks too — its menu
            // just offers a scan instead of live vitals. Nothing visible on the sky is mute.
            Vector2d position;
            if (npc.CurrentlyObserved)
            {
                position = npc.State.Position;
            }
            else if (npc.LastObservation is { } lastSeen)
            {
                position = lastSeen.Position;
            }
            else
            {
                continue;
            }

            // 🗺 Layers (#405): a hidden class stops answering clicks too, matched to the draw path —
            // depots ride ports.depots; a live contact vs its last-seen ghost split traffic's leaves.
            string trafficLeaf = isDepot ? "ports.depots" : npc.CurrentlyObserved ? "traffic.live" : "traffic.ghosts";
            if (!LayerVisible(trafficLeaf)) continue;

            (float sx, float sy) = _camera.WorldToScreen(position);
            double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
            if (d2 <= r2)
            {
                // #208: the depot twin of a port carries its kind AND a phrase, so it never reads as
                // a second look-alike of the dock haven ("Rusty Roadstead Depot · depot — cargo pod"
                // vs "Rusty Roadstead · dock haven — walk ashore"). Dock at havens; board depots.
                string flavor = isDepot ? "depot — cargo pod" : npc.CurrentlyObserved ? "contact" : "last seen";
                found.Add((new PickCandidate('S', npc.Ship.Id, $"{npc.Ship.Callsign} · {flavor}", isDepot ? "📦" : "🛰"),
                    d2, isDepot ? 1 : 0));
            }
        }

        // A hunter isn't in _npcStates, so it was never in this list — clicking the Debt Collector
        // hit empty sky. It's the most click-worthy thing on the screen: picking it locks the
        // war-room interest target (bracket + firing solution). Ranked ahead of haulers on a tie.
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.BrokenOff || hunter.CaughtPlayer) continue;
            (float sx, float sy) = _camera.WorldToScreen(hunter.State.Position);
            double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
            if (d2 <= r2)
            {
                found.Add((new PickCandidate('H', hunter.Id, $"{hunter.Callsign} · hunter", "🐺"), d2, -1));
            }
        }

        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue;          // a hidden body doesn't answer the picker (PR-A)
                if (body.ParentId is null) continue;          // the sun is not a destination you orbit
                if (body.Kind == BodyKind.Station) continue;  // a barge has no Hill sphere — dock, don't orbit
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                // Hit within the drawn disc, floored for pinprick planets and capped so a
                // zoomed-in world doesn't swallow every camera drag on the screen.
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                // #402: the deflection inbound rock is the most click-worthy thing in a station
                // cluster — name it as the threat (not a nameless "body") so the pick-menu answer at
                // the Ringside knot reads "⚠ Inbound rock — deflection target" against the depots.
                bool isDeflectionRock = _deflection is { } dgig && body.Id == dgig.RockBodyId;
                // #402 follow-up: the threat rock gets MapPick's widened tolerance so it's always one
                // click away — a click on the station knot lands it even when its own disc is a pinprick.
                double hit = isDeflectionRock
                    ? MapPick.ThreatRockHitRadiusPx(drawnPx, radiusPx)
                    : MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    // #208: a moon haven (parked-in, no ⚓ dock) carries its walk-in phrase too, so the
                    // picker's port entries all read their kind at a glance.
                    (string flavor, string icon) = isDeflectionRock
                        ? ("deflection target — on a collision course", "⚠")
                        : body.IsHaven ? ("haven — lie low in orbit", "🏴") : ("body", "🪐");
                    // #339-follow: a shuttle-landable ground (a moon) names its 🛬 mark and its live reach —
                    // "shuttle range" when the map glyph is bright, "out of shuttle reach" when it's dim — so
                    // the click hint says the same thing the glyph shows (the #195 all-controls-hinted law).
                    if (ShuttleExcursion.IsLandableSurface(body.Kind))
                    {
                        flavor += _landableInRangeIds.Contains(body.Id)
                            ? " · 🛬 landable — shuttle range"
                            : " · 🛬 landable — out of shuttle reach";
                    }
                    // #402: rank the threat rock ahead of ordinary bodies/depots so it heads the
                    // "which one?" list in a cluster (like the hunter is promoted above haulers).
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · {flavor}", icon), d2, isDeflectionRock ? -1 : 2));
                }
            }

            // Haven docks are stations (skipped above — you clamp, not orbit), but they must still be
            // pickable: stacked over their planet, the ⚓ in the name calls out the one you can dock at
            // right there in the "which one?" list — no zooming in to tell them apart (owner's ask).
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue; // (PR-A) — a hidden haven dock stays off the picker too
                if (!IsDockableHaven(body)) continue;
                if (!LayerVisible("ports.havens")) continue; // 🗺 Layers (#405): Dock havens off → its ⚓ pick answers no clicks
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                double hit = MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · dock haven — walk ashore", "⚓"), d2, 2));
                }
            }

            // The owner's roadster lesson: a REVEALED non-haven station (a wreck, a compute farm, a
            // factory) fell through both loops above — visible on the map, untargetable by click. They
            // are destinations too: the μ=0 arm flow flies you alongside (no clamp — that's the point;
            // the fetch pickup is proximity). Hidden ones stay off the picker until charted (PR-A).
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (IsBodyHidden(body.Id)) continue;
                if (body.Kind != BodyKind.Station || IsDockableHaven(body)) continue;
                (float sx, float sy) = _camera.WorldToScreen(_ephemeris.Position(body.Id, SimTime));
                double drawnPx = body.BodyRadius / _camera.MetersPerPixel;
                double hit = MapPick.BodyHitRadiusPx(drawnPx, radiusPx);
                double dx = x - sx, dy = y - sy, d2 = dx * dx + dy * dy;
                if (d2 <= hit * hit)
                {
                    found.Add((new PickCandidate('B', body.Id, $"{body.Name} · station — come alongside", "🛰"), d2, 2));
                }
            }
        }

        return [.. found.OrderBy(f => f.Rank).ThenBy(f => f.DistSq).Select(f => f.Pick)];
    }

    private void OpenPickMenu(List<PickCandidate> picks, double x, double y)
    {
        _pickMenu = picks;
        _pickMenuX = x;
        _pickMenuY = y;
        StateHasChanged();
    }

    private void OpenPickCandidate(PickCandidate pick)
    {
        double x = _pickMenuX, y = _pickMenuY;
        _pickMenu = null;
        OpenPickCandidateAt(pick, x, y);
    }

    private void OpenPickCandidateAt(PickCandidate pick, double x, double y)
    {
        switch (pick.Kind)
        {
            case 'S': OpenShipMenuFor(pick.Id, x, y); break;
            case 'H': MarkHunterOfInterest(pick.Id); break;
            case 'B': OpenBodyMenuFor(pick.Id, x, y); break;
            default: OpenSkyMenu(x, y); break;
        }
    }

    /// <summary>M29: clicking a contact SELECTS it (scope tracks, prediction pins — unchanged)
    /// AND opens its menu: track with the telescope, mark interest, read the vitals.</summary>
    private void OpenShipMenuFor(string shipId, double clientX, double clientY)
    {
        if (_selectedTargetId != shipId)
        {
            SelectTarget(shipId);
        }

        _shipMenuId = shipId;
        // #253: store the raw click anchor — ClampMenu applies the offset AND the edge flip at render.
        _shipMenuX = clientX;
        _shipMenuY = clientY;
        StateHasChanged();
    }

    private void OpenBodyMenuFor(string bodyId, double clientX, double clientY)
    {
        CelestialBody? body = _ephemeris?.Bodies.FirstOrDefault(b => b.Id == bodyId);
        if (body is null)
        {
            return;
        }

        _bodyMenuBody = body;
        _bodyMenuX = clientX; // #253: raw anchor; ClampMenu offsets + flips at render
        _bodyMenuY = clientY;
        ComputeAerobrakeQuote(body); // #290: price the aerobrake once on open (it flies drag — never per render)
        StateHasChanged(); // pointer events don't auto-render (IHandleEvent) — show the menu now
    }

    // ---- Map layers (owner: "filter what is being shown… they clutter the view a lot";
    // #405: rebuilt as a collapsible TREE — parent families you fold away, each a cascading
    // tri-state over its leaf toggles. The tree shape + all the resolution logic (visibility,
    // cascade, per-desk defaults, the threats-never-hidden invariant) is the pure, Core-tested
    // MapLayerTree; this partial only holds the per-desk hidden set and the UI collapse state. ----

    // ---- M29: the contact menu + target dossier ----
    private string? _shipMenuId;
    private double _shipMenuX, _shipMenuY;

    private void CloseShipMenu()
    {
        _shipMenuId = null;
        StateHasChanged();
    }

    // ---- SundaySecondPlan PR-C: point at the sky and ask ----
    // On the Sensors desk every click answers with scan options: ships (fixed or not), planets and empty
    // sky all open scan-contextual menus that enqueue telescope work.

    // #953 — the LANE is not one of the things a click can mean, and now it is not one of the things the
    // sky is painted with either. _corridorMenuLane, its anchor, CloseCorridorMenu and the CorridorAt
    // hit-test went with the per-lane menu (owner: "the routes as a whole should not even be selectable,
    // since they just colour the page"); the drawing followed it out on the archive ruling (ShipLanes).
    // The two sweep actions stayed — they find ships — and live in the open-sky menu below, under the lane
    // that menu names from geometry the page still keeps.
    private Vector2d? _skyMenuWorld;
    private double _skyMenuX, _skyMenuY, _skyMenuRadius;
    private bool _suppressClickMenu;

    private void CloseSkyMenu()
    {
        _skyMenuWorld = null;
        StateHasChanged();
    }

    private void OpenSkyMenu(double clientX, double clientY)
    {
        _skyMenuWorld = _camera.ScreenToWorld(clientX, clientY);
        // Scan size follows the zoom: what looks like "about here" on screen is what gets
        // scanned — zoom in for a tight expensive-per-area look, out for a broad survey.
        _skyMenuRadius = Math.Clamp(120 * _camera.MetersPerPixel, 2e9, 5e10);
        _skyMenuX = clientX; // #253: raw anchor; ClampMenu offsets + flips at render
        _skyMenuY = clientY;
        StateHasChanged();
    }
}
