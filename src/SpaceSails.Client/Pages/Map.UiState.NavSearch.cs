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
/// #251 · THE NAV SEARCH BOX — the typed query, the rows it collects out of the scenario, the keyboard
/// that walks them, and what a chosen row does to the view. One dropdown's whole life, from the focus call
/// to the jump. Split out of <c>Map.UiState.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.
/// </summary>
public partial class Map
{
    // One found target. Kind mirrors PickCandidate's ('B' body, 'S' contact/depot, 'H' hunter) so the
    // jump reuses the same select paths. Hidden = the Layers filter would keep it OFF the map even after
    // we jump there — true only for a layer-filtered contact/depot; a charted body's disc always draws,
    // and an UNCHARTED body is excluded from search entirely (see CollectSearchCandidates).
    public readonly record struct NavSearchRow(char Kind, string Id, string Name, string Flavor, string Icon, bool Hidden);

    private ElementReference _navSearchInput;
    private string _navSearchQuery = "";
    private bool _navSearchOpen;
    private int _navSearchIndex;                 // which row the keyboard has highlighted
    private List<NavSearchRow> _navSearchRows = [];
    private const int NavSearchMaxRows = 12;     // a busy scenario has dozens of bodies — cap the dropdown

    // `/` (OnKeyDown) opens the box and hands it the keyboard. Rendered only on the map desks, so the
    // @ref is live whenever this runs.
    private async Task FocusNavSearch()
    {
        _navSearchOpen = true;
        RecomputeNavSearch();       // in case a query is already typed (re-opening)
        await _navSearchInput.FocusAsync();
    }

    private void OnNavSearchInput(ChangeEventArgs e)
    {
        _navSearchQuery = e.Value?.ToString() ?? "";
        RecomputeNavSearch();
    }

    private void RecomputeNavSearch()
    {
        List<NavSearchRow> ranked = NavSearch.FilterAndRank(_navSearchQuery, CollectSearchCandidates(), r => r.Name);
        _navSearchRows = ranked.Count > NavSearchMaxRows ? ranked.GetRange(0, NavSearchMaxRows) : ranked;
        _navSearchIndex = 0;
    }

    /// <summary>Everything the search can jump to, in the same intent order the click-picker ranks by:
    /// threats first, then live/last-seen contacts and depots, then every CHARTED body. Unlike the
    /// picker this ignores the screen position and the forgiving radius (you're searching, not clicking)
    /// AND ignores the Layers filter (a search should find what you explicitly asked for even if your own
    /// filter is hiding it) — but it marks a layer-hidden contact so the row says so. Uncharted bodies
    /// (IsBodyHidden, PR-A) stay OUT: naming an undiscovered wreck would spoil it, and the picker excludes
    /// them too.</summary>
    private List<NavSearchRow> CollectSearchCandidates()
    {
        var rows = new List<NavSearchRow>();

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.BrokenOff || hunter.CaughtPlayer)
            {
                continue;
            }

            rows.Add(new NavSearchRow('H', hunter.Id, hunter.Callsign, "hunter", "🐺", false));
        }

        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived)
            {
                continue;
            }

            // A never-observed contact has no known place to jump to and no name on the plot — skip it,
            // matching the picker (it needs a live position or a last-seen ghost to answer at all).
            if (!npc.CurrentlyObserved && npc.LastObservation is null)
            {
                continue;
            }

            bool isDepot = npc.Ship.DepotBodyId is not null;
            string leaf = isDepot ? "ports.depots" : npc.CurrentlyObserved ? "traffic.live" : "traffic.ghosts";
            string flavor = isDepot ? "depot — cargo pod" : npc.CurrentlyObserved ? "contact" : "last seen";
            string icon = isDepot ? "📦" : "🛰";
            rows.Add(new NavSearchRow('S', npc.Ship.Id, npc.Ship.Callsign, flavor, icon, !LayerVisible(leaf)));
        }

        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null)
                {
                    continue;   // the sun is not a jump target you frame on (nothing orbits it locally here)
                }

                if (IsBodyHidden(body.Id))
                {
                    continue;   // uncharted — off the search, same as the picker (PR-A: don't spoil undiscovered finds)
                }

                (string flavor, string icon) = ClassifyBodyForSearch(body);
                rows.Add(new NavSearchRow('B', body.Id, body.Name, flavor, icon, false));
            }
        }

        return rows;
    }

    // The picker's flavor/icon rules for a body, distilled into one classifier (the picker spreads them
    // across three position-gated loops). Kept in sync so a searched body reads the same as a clicked one.
    private (string Flavor, string Icon) ClassifyBodyForSearch(CelestialBody body)
    {
        bool isDeflectionRock = _deflection is { } dgig && body.Id == dgig.RockBodyId;
        (string flavor, string icon) =
            isDeflectionRock ? ("deflection target — on a collision course", "⚠")
            : IsDockableHaven(body) ? ("dock haven — walk ashore", "⚓")
            : body.IsHaven ? ("haven — lie low in orbit", "🏴")
            : body.Kind == BodyKind.Station ? ("station — come alongside", "🛰")
            : ("body", "🪐");

        if (ShuttleExcursion.IsLandableSurface(body.Kind))
        {
            flavor += _landableInRangeIds.Contains(body.Id)
                ? " · 🛬 landable — shuttle range"
                : " · 🛬 landable — out of shuttle reach";
        }

        return (flavor, icon);
    }

    private async Task OnNavSearchKeyDown(KeyboardEventArgs e)
    {
        // The input's own @onkeydown:stopPropagation keeps these keys out of OnKeyDown, so typing a name
        // (w/s/o/1-7…) or steering with the arrows never drives the ship or switches desks while you search.
        switch (e.Key)
        {
            case "ArrowDown":
                if (_navSearchRows.Count > 0)
                {
                    _navSearchIndex = (_navSearchIndex + 1) % _navSearchRows.Count;
                }

                break;
            case "ArrowUp":
                if (_navSearchRows.Count > 0)
                {
                    _navSearchIndex = (_navSearchIndex - 1 + _navSearchRows.Count) % _navSearchRows.Count;
                }

                break;
            case "Enter":
                if (_navSearchIndex >= 0 && _navSearchIndex < _navSearchRows.Count)
                {
                    await JumpToSearchResult(_navSearchRows[_navSearchIndex]);
                }

                break;
            case "Escape":
                CloseNavSearch();
                await RefocusMap();   // hand the keyboard back to the helm
                break;
        }
    }

    // #406 chosen SELECT action — "focus + frame + centre", the single most useful "take me there" for
    // planning; the target's context menu (arm autopilot, set destination, track) is then one click away
    // now that it sits under the camera. We deliberately do NOT auto-open that menu (it would need a
    // screen anchor and pre-empts the choice the captain may not want yet — the issue's steer).
    //   • a BODY becomes the plot-frame origin (SetPlotFrame — the map + ribbon co-move with it) AND the
    //     camera centres on it.
    //   • a CONTACT/HUNTER can't be a frame origin (not an ephemeris body), so we SELECT it (scope + the
    //     war-room interest bracket) and centre on its last-known spot.
    private async Task JumpToSearchResult(NavSearchRow row)
    {
        Vector2d? world = null;
        switch (row.Kind)
        {
            case 'B':
                if (_ephemeris is not null)
                {
                    SetPlotFrame(row.Id);
                    world = _ephemeris.Position(row.Id, SimTime);
                }

                break;
            case 'S':
                if (FindNpc(row.Id) is { } npc)
                {
                    if (_selectedTargetId != row.Id)
                    {
                        SelectTarget(row.Id);
                    }

                    world = npc.CurrentlyObserved ? npc.State.Position : npc.LastObservation?.Position;
                }

                break;
            case 'H':
                foreach (HunterState hunter in _hunters)
                {
                    if (hunter.Id != row.Id)
                    {
                        continue;
                    }

                    MarkHunterOfInterest(row.Id);
                    world = hunter.State.Position;
                    break;
                }

                break;
        }

        if (world is { } target)
        {
            FollowShip = false;         // centring must stick — follow-ship would snap back to the ship next frame
            _followDest = false;        // …and so would follow-destination (#956)
            _camera.CenterOn(target);
        }

        CloseNavSearch();
        await RefocusMap();             // the keyboard goes back to the helm so w/s/o etc. work at once
    }

    private void CloseNavSearch()
    {
        _navSearchOpen = false;
        _navSearchQuery = "";
        _navSearchRows = [];
        _navSearchIndex = 0;
    }
}
