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
/// #313 · THE DESTINATION BOARD — every stop the shuttle can actually reach from where she is standing,
/// the two or three landables just beyond reach, and the words the board says about both.
///
/// <para>The reachable set and the map's landable-in-range marks are the SAME sweep
/// (<c>ShuttleExcursion.Destinations</c> / <c>ShuttleRange</c>), cached once a tick, so a mark on the
/// map and a row on the board can never disagree about what is in range. The out-of-reach rows carry a
/// trend rather than a promise: two samples an hour apart say whether the gap is closing.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // the ground. The classification is the pure, tested ShuttleExcursion.Destinations rule.
    public sealed record ShuttleStop(
        CelestialBody Body, double DistanceMeters, double TravelSeconds,
        bool HasBerth, bool IsLandable, bool HasCache);

    // Null = the hatch is shut; a list (possibly empty) = the destination board is open.
    private List<ShuttleStop>? _shuttleBayStops;

    // #368: one row on the "nearest ground beyond reach" list — the informational tail under the boardable
    // destinations (owner: "list the nearest landing sites here at shuttle door with their ranges"). Not
    // boardable: current separation, the shuttle's reach for contrast, and whether the gap is closing or
    // opening now. Parent system carried for the "Ganymede (Jupiter)" read. Classification is the pure Core
    // rule ShuttleExcursion.NearestOutOfReach.
    public sealed record ShuttleFarStop(
        CelestialBody Body, string ParentName, double DistanceMeters, double RangeMeters,
        ShuttleExcursion.RangeTrend Trend);

    // Snapshot taken when the board opens (alongside _shuttleBayStops), so the tail doesn't re-sort every
    // frame. Empty when nothing landable sits just beyond reach.
    private List<ShuttleFarStop> _shuttleBayFarStops = new();

    // The destination board: every body within one hop, classified by orbit context (the pure Core
    // rule), then re-joined to the live CelestialBody the UI needs to render each row.
    private List<ShuttleStop> ShuttleDestinationsInRange()
    {
        var stops = new List<ShuttleStop>();
        if (_ephemeris is null)
        {
            return stops;
        }
        Vector2d shipPos = _ship.Position;
        var byId = _ephemeris.Bodies.ToDictionary(b => b.Id);
        var candidates = _ephemeris.Bodies.Select(b => new ShuttleExcursion.Candidate(
            b.Id, b.Kind, b.ParentId,
            (shipPos - _ephemeris.Position(b.Id, SimTime)).Length, b.BodyRadius,
            HasInterior: HavenInterior.HasInterior(b.Id), HasCache: _caches.HasCacheAt(b.Id)));

        foreach (ShuttleExcursion.Destination d in ShuttleExcursion.Destinations(candidates, _dockedHavenId))
        {
            if (byId.TryGetValue(d.BodyId, out CelestialBody? body))
            {
                stops.Add(new ShuttleStop(body, d.DistanceMeters, d.TravelSeconds, d.HasBerth, d.IsLandableSurface, d.HasCache));
            }
        }
        return stops;
    }

    // #339-follow (owner, 2026-07-19 playtest): the Nav-map 🛬 landable glyph's bright state — the body ids
    // of the shuttle-landable grounds within reach of the ship RIGHT NOW. It is exactly the landable
    // subset of the shuttle-bay board (ShuttleDestinationsInRange), so the map mark and the board can
    // never disagree and the range math lives in ONE place (ShuttleExcursion / ShuttleRange). Refreshed
    // once per tick on the same cadence the dock affordance uses, so the per-frame draw reads a cache
    // rather than re-running the range sweep every frame.
    private HashSet<string> _landableInRangeIds = new();

    private void UpdateLandableInRange()
    {
        var ids = new HashSet<string>();
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable)
            {
                ids.Add(stop.Body.Id);
            }
        }
        _landableInRangeIds = ids;
    }

    // #368: the nearest few LANDABLE surfaces the shuttle can NOT reach yet, nearest-first, each with the
    // live closing/opening trend — the honest tail the owner asked for. State-driven from the SAME range
    // and landable predicates as the boardable board (ShuttleRange.RangeMeters / IsLandableSurface via the
    // Core rule), no hardcoded bodies. The trend is a pure two-sample compare: the separation now vs the
    // separation one hour on, re-using the same ephemeris propagation — the ship rides its berth (or coasts
    // at its own velocity if flying free), the ground drifts along its orbit.
    private const double TrendSampleSeconds = 3600.0; // one hour on — enough drift for a clean sign
    private List<ShuttleFarStop> NearestOutOfReachLandables(int limit)
    {
        var result = new List<ShuttleFarStop>();
        if (_ephemeris is null)
        {
            return result;
        }
        Vector2d shipNow = _ship.Position;
        // Where the ship will be one sample-step on: riding the clamp if berthed, else coasting ballistically.
        Vector2d shipNext = _dockedHavenId is not null
            ? _ephemeris.Position(_dockedHavenId, SimTime + TrendSampleSeconds) + _dockOffset
            : _ship.Position + _ship.Velocity * TrendSampleSeconds;

        var byId = _ephemeris.Bodies.ToDictionary(b => b.Id);
        var samples = _ephemeris.Bodies.Select(b => new ShuttleExcursion.RangeSample(
            b.Id, b.Kind, b.ParentId,
            (shipNow - _ephemeris.Position(b.Id, SimTime)).Length,
            (shipNext - _ephemeris.Position(b.Id, SimTime + TrendSampleSeconds)).Length,
            b.BodyRadius));

        foreach (ShuttleExcursion.NearbyLandable n in ShuttleExcursion.NearestOutOfReach(samples, _dockedHavenId, limit))
        {
            if (byId.TryGetValue(n.BodyId, out CelestialBody? body))
            {
                string parent = body.ParentId is { } pid && byId.TryGetValue(pid, out CelestialBody? p) ? p.Name : "";
                result.Add(new ShuttleFarStop(body, parent, n.DistanceMeters, n.RangeMeters, n.Trend));
            }
        }
        return result;
    }

    private void OpenShuttleBayDoor()
    {
        _shuttleBayStops = ShuttleDestinationsInRange();
        _shuttleBayFarStops = NearestOutOfReachLandables(3); // the nearest 2-3 beyond reach
    }

    private void CloseShuttleBayDoor()
    {
        _shuttleBayStops = null;
        _shuttleBayFarStops = new();
    }

    // #368 house-voice copy for the beyond-reach tail. Separation humanized WITH thousands separators (the
    // owner's "690,000 km"), stepping up to M km / AU when the gap is huge so a distant moon never reads as
    // a seven-digit smear.
    private static string ShuttleSeparationText(double meters)
    {
        const double metersPerAu = 1.495978707e11;
        if (meters >= metersPerAu / 10)
        {
            return $"{meters / metersPerAu:F2} AU";
        }
        if (meters >= 1e9)
        {
            return $"{meters / 1e9:F2} M km";
        }
        return $"{(meters / 1000).ToString("N0", CultureInfo.InvariantCulture)} km";
    }

    // The shuttle's reach, spoken for contrast — always in plain km so "shuttle reaches 500,000 km" reads
    // straight against the separation above it.
    private static string ShuttleReachText(double rangeMeters) =>
        $"{(rangeMeters / 1000).ToString("N0", CultureInfo.InvariantCulture)} km";

    // The gap's live trend, in the house voice: closing toward reach, opening away, or holding steady.
    private static string ShuttleTrendWord(ShuttleExcursion.RangeTrend trend) => trend switch
    {
        ShuttleExcursion.RangeTrend.Closing => "closing",
        ShuttleExcursion.RangeTrend.Opening => "opening",
        _ => "holding steady",
    };
}
