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
/// #953 · THE LANES, AFTER THE DRAWING WAS ARCHIVED — geometry for the telescope, nothing for the eye.
///
/// <para>Owner, 2026-08-25: <i>"we have never used them to find anything."</i> The inks, the quad build
/// and the draw are gone; what survives is not decoration, because a lane names the two sweep actions in
/// the open-sky menu and the telescope pass they enqueue puts real contacts in the tracking ledger.</para>
///
/// <para>The field names are left as they were: <c>EveryFrameLeavesTheSameFingerprintTests</c> renders
/// this page's fields whole, so a rename alone would move all thirty committed sweep hashes and say
/// nothing. Split out of <c>Map.Trade.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ---- The lanes, after #953: geometry for the telescope, nothing for the eye ----
    //
    // SundaySecondPlan PR-B drew a translucent quad and a name label per anchor pair, and made the corridor
    // a click target of its own. #971 took the click target away ("the routes as a whole should not even be
    // selectable, since they just colour the page in that option") and hid the drawing by default. This
    // ruling archived the display outright — owner, 2026-08-25: "we have never used them to find anything."
    // The three inks, the quad build and the draw that laid them are GONE; ShipLanes.Archived is where the
    // decision is written down.
    //
    // What survives is the geometry, because it is not decoration: a lane names the two sweep actions in the
    // open-sky menu, and the telescope pass they enqueue puts real contacts in the tracking ledger.

    // The field names are left as they were: EveryFrameLeavesTheSameFingerprintTests renders this page's
    // fields whole, so a rename alone would move all thirty committed sweep hashes and say nothing.
    private IReadOnlyList<CorridorRegion> _mapCorridors = [];
    private double _mapCorridorsBuiltAt = double.NegativeInfinity;

    /// <summary>The lanes as they lie right now, rebuilt at most hourly of sim time (the anchors are planets
    /// — an hour does not move them far enough to matter, and this is asked on every click).
    ///
    /// <para>#953 — this cache used to be filled INSIDE the corridor draw, so it was only ever populated when
    /// the Trade lanes layer was switched on. Once #971 hid that layer by default, <see cref="NearLaneFor"/>
    /// searched an empty list on every desk and the two lane sweeps in the open-sky menu could not appear at
    /// all unless a captain first ticked a decoration layer he was never shown. The geometry the ACTIONS need
    /// is now built by the actions' own path, where it belongs, and the drawing it used to ride on is gone.</para>
    /// </summary>
    private IReadOnlyList<CorridorRegion> LaneGeometry()
    {
        if (_ephemeris is null)
        {
            return [];
        }

        if (SimTime - _mapCorridorsBuiltAt > 3600)
        {
            _mapCorridors = TradeCorridors.Regions(_ephemeris, SimTime);
            _mapCorridorsBuiltAt = SimTime;
        }

        return _mapCorridors;
    }

    /// <summary>The lane nearest a sky point, when it counts as "near" — what names the open-sky menu's
    /// sweep actions ("this empty spot sits by the Earth–Mars lane; sweep the lane instead?").</summary>
    private CorridorRegion? NearLaneFor(Vector2d point)
    {
        IReadOnlyList<CorridorRegion> lanes = LaneGeometry();
        if (lanes.Count > 0
            && TradeCorridors.TryNearest(lanes, point, out CorridorRegion lane, out double distance)
            && distance <= lane.Radius * TradeCorridors.NearLaneFactor)
        {
            return lane;
        }

        return null;
    }
}
