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
/// #251 · WHAT THE MAP IS SHOWING YOU, AND WHAT IT IS NOT — three answers to one question, kept together.
/// The scenario's hidden bodies and the session's revealed set (a body on its rail but off the charts until
/// a targeted scan finds it); the per-desk layer tree and its collapsed groups; and the peek that throws
/// the map over the desk pane while a key is held. Split out of <c>Map.UiState.cs</c> under #251 with no
/// member renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    // Tuesday plan PR-A (the hunt is the quest): a hidden body is on its rail but off the charts —
    // it doesn't draw, answer the picker, ride the scope carousel, count as "Nearest", or open a
    // body menu until a targeted, intel-fed scan reveals it. `_hiddenBodyIds` is the scenario's
    // "hidden":true set (loaded once); `_revealedBodyIds` is the session's found set (reveal is
    // session-scoped, matching the save model). A body is CHARTED when it isn't hidden, or has been
    // revealed.
    private readonly HashSet<string> _hiddenBodyIds = [];
    private readonly HashSet<string> _revealedBodyIds = [];

    // A hidden body still on the charts' blind side — everything player-facing must skip it.
    private bool IsBodyHidden(string id) => _hiddenBodyIds.Contains(id) && !_revealedBodyIds.Contains(id);

    // Bring a hidden body onto the charts for the rest of the session: fires a payoff line + cue,
    // repaints. Idempotent — a second reveal (or revealing a plainly-visible body) is a no-op.
    private void RevealBody(string id, string reason, bool announce = true)
    {
        if (!_hiddenBodyIds.Contains(id) || !_revealedBodyIds.Add(id))
        {
            return;
        }
        if (announce)
        {
            ShowPulseMessage(reason);
            RendererInterop.PlayCue("reveal");
        }
        _passDirty = true;
        StateHasChanged();
    }
    // Owner request: momentarily hide every panel to read the map, then bring them back. Pure
    // presentation — the sim, the active desk and all state are untouched; only the overlay
    // visibility/hit-testing changes (see the .map-peek rule in wwwroot/css/app.css — global, not
    // Map.razor.css, because a scoped `> *` cannot reach a child component's own root).
    //
    // #1038 · IT IS A MODE, AND THE POP-UP LAW GENERALISES TO IT. Three ways out, all of them things the
    // captain can find without being told: the 👁 button on the desk tab bar (which peek is now careful to
    // leave standing — .peek-keep), the ` hotkey it has always had, and Escape, which joins the house cancel
    // chain at the top (Map.Sim.Cancel). Whatever else this field ever hides, it may never hide all three.
    private bool _peekMap;

    private void TogglePeekMap() => _peekMap = !_peekMap;

    // #1038 · The one-way door OUT, for callers that mean "end it" rather than "flip it". The cancel chain
    // needs this rather than TogglePeekMap: Escape must never be able to START a peek, or a stray press on a
    // clear screen would blank the panels and the key that did it would look broken.
    private void EndPeekMap() => _peekMap = false;

    // 2026-07-18 playtest: the peek button shares the desk-tab bar, so a click stole focus off the map div
    // the same way a tab did. The mouse toggles peek through here so the keyboard comes home; the ` hotkey
    // still calls TogglePeekMap directly (it already owns focus).
    private async Task TogglePeekMapFromClick()
    {
        TogglePeekMap();
        await RefocusMap();
    }

    // M20: ships and pods are clickable on the map — same effect as picking their traffic row.
    // ---- The unified picker (owner: "hard to click things that are close by"; Gemini
    // consult: forgiving radius, chooser when several objects stack, lanes always last) ----

    private readonly Dictionary<ShipDesk, HashSet<string>> _hiddenLayersByDesk = [];
    private bool _layersOpen;

    // Which parent families are folded shut in the panel. UI-only (not per desk, not gating any
    // draw) — seeded once from the tree's DefaultCollapsed flags (Routes rides collapsed).
    private HashSet<string>? _collapsedLayerGroups;

    private HashSet<string> CollapsedLayerGroups =>
        _collapsedLayerGroups ??= [.. MapLayerTree.Groups.Where(g => g.DefaultCollapsed).Select(g => g.Key)];

    private void ToggleLayerGroupCollapsed(string groupKey)
    {
        if (!CollapsedLayerGroups.Remove(groupKey))
        {
            CollapsedLayerGroups.Add(groupKey);
        }

        StateHasChanged();
    }

    private HashSet<string> HiddenLayers
    {
        get
        {
            if (!_hiddenLayersByDesk.TryGetValue(_activeDesk, out HashSet<string>? hidden))
            {
                // Per-desk defaults live in Core. #953 archived the one layer that used to start
                // hidden (the trade lanes), so today every desk opens on the whole tree — but the seam
                // stays, because the next layer that wants a per-desk default has nowhere else to go.
                hidden = MapLayerTree.DefaultHidden(_activeDesk == ShipDesk.Sensors);
                _hiddenLayersByDesk[_activeDesk] = hidden;
            }

            return hidden;
        }
    }

    // The single source of truth the draw path + the click-picker resolve through. A pinned leaf
    // (Threats) is always visible no matter the hidden set — the safety invariant lives in Core.
    private bool LayerVisible(string key) => MapLayerTree.IsVisible(HiddenLayers, key);

    private void ToggleLayer(string key)
    {
        MapLayerTree.ToggleLeaf(HiddenLayers, key);
        StateHasChanged();
    }

    // Parent checkbox: tri-state cascade to the family (On→Off, Off/Mixed→On); pinned groups inert.
    private void ToggleLayerGroup(MapLayerTree.Group group)
    {
        MapLayerTree.CascadeGroup(HiddenLayers, group);
        StateHasChanged();
    }

    // The panel's bottom line: drop this desk back to its shipped default visibility.
    private void ResetLayersToDeskDefaults()
    {
        _hiddenLayersByDesk[_activeDesk] = MapLayerTree.DefaultHidden(_activeDesk == ShipDesk.Sensors);
        StateHasChanged();
    }

    // #405 pick-menu hint (owner: "some UI hint about adding more layers when wanted… maybe to the
    // selection pop-up"): the "Which one?" chooser's footer link closes itself and opens the Layers
    // panel, so a crowded knot points the captain straight at the filter. Reuses existing state.
    private void OpenLayersFromPick()
    {
        _pickMenu = null;
        _layersOpen = true;
        StateHasChanged();
    }

    // ---- #406 Nav search: type-to-find a jump target instead of zoom-hunting. Composes with the
    // existing machinery — the candidates are the same set the click-picker knows (bodies + depots +
    // live/last-seen contacts + hunters), the rows reuse the "name · kind · flavor" idiom, and the
    // select action reuses SetPlotFrame + the camera. The pure match/rank seam is Core's NavSearch. ----
}
