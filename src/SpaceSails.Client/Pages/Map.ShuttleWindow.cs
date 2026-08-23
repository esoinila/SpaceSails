using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.ShuttleWindow — #955 NAV-2 · THE WINDOW THAT COMES BACK, AND THE WINDOW THAT READS THE ROUTE.
//
// Owner's payoff for the unified nav list (2026-08-23): "with a pre-plotted, armed route you can LEAVE the
// ship by shuttle mid-voyage (meteorites, small sites) with a return-window clock on the captain's remote
// — without a planned route you risk leaving the ship adrift", and "a sensitive visit could have separate
// ingress and egress shuttle windows". The ship is never adrift; the CAPTAIN is the one who can be left
// behind, and RETURN BY is the whole of the drama.
//
// Owner's corner case, same day: "docked at a Jupiter/Saturn haven the shuttle windows to the moons are
// PERIODIC by default … a window that closes while you are ashore must NOT mark the team dead when it will
// reopen on its own". Before this file the docked branch of Map.Expedition answered that by pretending the
// geometry was infinite — it never stranded anyone from a berth, but only by ignoring the moon's motion.
// Now it reads the real berth↔site rails and says "closed — next window in X".
//
// ONE law serves both cases (Core ExpeditionWindow.Classify with the reopening folded in); only the
// HORIZON differs — a berth scans one synodic period, an armed plan scans what is left of its span, and a
// loose ship scans its contracted budget.
public partial class Map
{
    /// <summary>How the shuttle window to the ground the captain is standing on reads right now: the coarse
    /// status, the seconds left before it shuts, when it next opens if it is shut, and the RETURN BY the
    /// plotted route imposes when the ship is flying on without him.</summary>
    private readonly record struct AwayWindow(
        WindowStatus Status,
        double SecondsLeft,
        double? ReopenSeconds,
        double? ReturnBySimTime);

    // The reopen scan is a bounded march over the ephemeris (Core caps it at
    // ExpeditionWindow.MaxReopenScanSamples), so it is not a per-frame cost. We cache the ABSOLUTE sim time
    // the window is promised to reopen and simply count down to it — the number on the HUD stays smooth,
    // and the scan re-runs only when the promise expires or the site under our feet changes.
    // Keyed by body, because two surfaces ask at once — the away-site clock under the captain's feet and
    // the shuttle board's rows — and a single-slot cache would thrash between them and re-scan every frame.
    private readonly Dictionary<string, (double? ReopenSimTime, double ScannedAt)> _awayWindowScans = [];

    /// <summary>How stale a reopen promise may get before it is re-asked (sim seconds). The scan is exact to
    /// <see cref="ExpeditionWindow.ReopenPrecisionSeconds"/> and the geometry it reads is on rails, so the
    /// only reason to re-ask at all is a ship that has since moved off the rail the promise assumed.</summary>
    private const double AwayWindowRescanSeconds = 1800.0;

    // ── Where the mothership will BE ───────────────────────────────────────────────────────────────

    /// <summary>Where the ship is at <paramref name="simTime"/>, by the best promise she is under: clamped
    /// to a berth she rides its rail; on an ARMED plan (#969) she flies the plotted ribbon and finishes it
    /// with no input at all, so the ribbon IS her future; otherwise she coasts from where she is. This is
    /// the one place the "the ship flies on" premise is turned into a position.</summary>
    private Vector2d ShipAnchorAt(double simTime)
    {
        if (_dockedHavenId is { } berth && _ephemeris is not null)
        {
            return _ephemeris.Position(berth, simTime) + _dockOffset;
        }

        if (ShipIsFlyingAPlottedRoute && _samples.Count > 1 && simTime <= _samples[^1].SimTime)
        {
            return NodeFrame.PositionAt(_samples, simTime, _ship.Position);
        }

        return _ship.Position + (_ship.Velocity * (simTime - SimTime));
    }

    /// <summary>Whether there is a plotted route the ship is actually promised to fly — #969's plan-time
    /// promise still ahead of us. Only then may a window quote the RIBBON as the ship's future; without it
    /// the honest answer is a ballistic coast, and leaving the ship is the gamble the owner described.</summary>
    private bool ShipIsFlyingAPlottedRoute =>
        _armedArrivalPassSimTime is { } pass && pass > SimTime;

    /// <summary>The honest gap between the mothership and a body at <paramref name="simTime"/>.</summary>
    private double AwaySeparationAt(string bodyId, double simTime) =>
        _ephemeris is null ? 0.0 : (_ephemeris.Position(bodyId, simTime) - ShipAnchorAt(simTime)).Length;

    /// <summary>How far ahead a closed window may be scanned for its reopening. A berth scans one full turn
    /// of the relative geometry (the synodic period of berth and site — the owner's periodic moon windows);
    /// a ship on an armed plan scans what is left of the plan; anything else scans the contracted away
    /// budget, because that is all the time the team has anyway.</summary>
    private double AwayReopenHorizonSeconds(string bodyId)
    {
        if (_dockedHavenId is { } berth && BodyById(berth) is { } berthBody && BodyById(bodyId) is { } site)
        {
            return ExpeditionWindow.SynodicPeriodSeconds(berthBody.OrbitPeriod, site.OrbitPeriod);
        }

        if (_armedArrivalPassSimTime is { } pass && pass > SimTime)
        {
            return pass - SimTime;
        }

        return ExpeditionWindow.DefaultHoldWindowSeconds;
    }

    /// <summary>Seconds until the window to this body reopens, or null when nothing inside the horizon
    /// brings it back. Cached on the absolute instant it promises, so the HUD counts down smoothly and the
    /// bounded scan runs on a cadence rather than per frame.</summary>
    private double? AwayReopenSeconds(string bodyId)
    {
        bool fresh = _awayWindowScans.TryGetValue(bodyId, out var cached)
            && SimTime - cached.ScannedAt <= AwayWindowRescanSeconds
            && SimTime - cached.ScannedAt >= 0
            && (cached.ReopenSimTime is not { } promised || promised > SimTime);

        if (!fresh)
        {
            double? found = ExpeditionWindow.SecondsUntilReopen(
                dt => AwaySeparationAt(bodyId, SimTime + dt), AwayReopenHorizonSeconds(bodyId));
            cached = (found is { } f ? SimTime + f : null, SimTime);
            _awayWindowScans[bodyId] = cached;
        }

        return cached.ReopenSimTime is { } when ? Math.Max(0.0, when - SimTime) : null;
    }

    // ── The window on one body, by the one law ─────────────────────────────────────────────────────

    /// <summary>How the shuttle window to <paramref name="bodyId"/> reads right now. The status is Core's
    /// (<see cref="ExpeditionWindow.Classify"/>) with the reopening folded in, so a periodic geometry reads
    /// Closed — reopens in X — and only a gap nothing brings back reads Lost.</summary>
    private AwayWindow WindowOn(string bodyId)
    {
        if (_ephemeris is null)
        {
            return new AwayWindow(WindowStatus.Holding, double.PositiveInfinity, null, null);
        }

        double distance = AwaySeparationAt(bodyId, SimTime);
        double rate = AwaySeparationAt(bodyId, SimTime + 1.0) - distance;   // per second, opening positive
        double? reopen = distance >= ExpeditionWindow.RangeMeters ? AwayReopenSeconds(bodyId) : null;
        WindowStatus status = ExpeditionWindow.Classify(
            distance, rate, ExpeditionWindow.DefaultCriticalSeconds, reopen);

        double left = ExpeditionWindow.TimeLeftInRangeSeconds(distance, rate);
        return new AwayWindow(status, left, reopen, RouteReturnBySimTime(bodyId));
    }

    // ── The plotted route's own windows ────────────────────────────────────────────────────────────

    /// <summary>The shuttle windows the plotted path opens on one body — the span of the SAME samples the
    /// ARRIVE step reads that lie inside a shuttle hop of it, each with its RETURN BY. Empty unless the ship
    /// is actually promised to fly that path: a ribbon nobody armed is a drawing, not a future, and offering
    /// a RETURN BY against it would be the game lying to a captain about to step off.</summary>
    private IReadOnlyList<RouteShuttleWindow.Window> RouteWindowsOn(string bodyId)
    {
        if (_ephemeris is null || !ShipIsFlyingAPlottedRoute || _samples.Count < 2)
        {
            return [];
        }

        var separations = new List<RouteShuttleWindow.RouteSample>(_samples.Count);
        foreach (TrajectorySample s in _samples)
        {
            if (s.SimTime < SimTime)
            {
                continue;   // the part of the ribbon already flown is not a window anybody can take
            }
            separations.Add(new RouteShuttleWindow.RouteSample(
                s.SimTime, (s.Position - _ephemeris.Position(bodyId, s.SimTime)).Length));
        }

        return RouteShuttleWindow.Along(separations);
    }

    /// <summary>The RETURN BY the route imposes on a captain standing on <paramref name="bodyId"/> right
    /// now: the egress of the window we are INSIDE, minus the ride home. Null when the ship is not flying a
    /// plotted route, or when the path is not passing this body at the moment.</summary>
    private double? RouteReturnBySimTime(string bodyId)
    {
        foreach (RouteShuttleWindow.Window w in RouteWindowsOn(bodyId))
        {
            if (w.OpensSimTime <= SimTime && SimTime <= w.ClosesSimTime)
            {
                return w.ReturnBySimTime;
            }
        }
        return null;
    }

    // ── What it SAYS ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The ground the captain is standing on, or null when he is aboard. The one place "ashore"
    /// is decided, so the remote and the away-site HUD can never disagree about whose window this is.</summary>
    private string? AshoreOnBodyId => _surface?.Stop.Body.Id;

    /// <summary>The captain's-remote line while ashore — RETURN BY and the next window, in one sentence,
    /// built by the Core copy so the away-site HUD quotes the identical numbers. Null aboard, and null on a
    /// ground with nothing to catch and nothing to wait for (a berth holding perfect range).</summary>
    private string? AwayWindowRemoteLine()
    {
        if (AshoreOnBodyId is not { } bodyId)
        {
            return null;
        }

        AwayWindow w = WindowOn(bodyId);
        if (w.ReturnBySimTime is null && w.Status is WindowStatus.Holding)
        {
            return null;   // nothing is flying on and the gap is not opening — no clock worth a line
        }

        return RouteShuttleWindow.RemoteLine(SimTime, w.ReturnBySimTime, w.ReopenSeconds);
    }

    // ── The board's "on the route" section ────────────────────────────────────────────────────────

    /// <summary>One shuttle window the ARMED plan opens on a body, ready to render: which ground, whether
    /// this is the first crossing or a later one (the owner's ingress / egress), and the window itself.</summary>
    private readonly record struct RouteWindowRow(
        CelestialBody Body, string Role, RouteShuttleWindow.Window Window);

    /// <summary>At most this many windows are listed per body. Two is the owner's case exactly — "a sensitive
    /// visit could have separate INGRESS and EGRESS shuttle windows" — and a plan that grazes a moon a dozen
    /// times is a plan whose board should not become a timetable.</summary>
    private const int MaxRouteWindowsPerBody = 2;

    /// <summary>The shuttle windows the armed plan opens on the landable ground ahead — the offer the owner
    /// asked for: <i>go by shuttle, the ship flies on</i>. Empty when nothing is armed, which is the honest
    /// answer: without a plotted route, stepping off risks leaving the ship adrift and no RETURN BY can be
    /// promised. Soonest first.</summary>
    private List<RouteWindowRow> ShuttleRouteWindowRows()
    {
        var rows = new List<RouteWindowRow>();
        if (_ephemeris is null || !ShipIsFlyingAPlottedRoute)
        {
            return rows;
        }

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.ParentId is null || !ShuttleExcursion.IsLandableSurface(body.Kind))
            {
                continue;   // only ground a captain could stand on — the same gate the board's own list uses
            }

            int taken = 0;
            foreach (RouteShuttleWindow.Window w in RouteWindowsOn(body.Id))
            {
                if (!w.IsUsable || w.ReturnBySimTime <= SimTime || taken >= MaxRouteWindowsPerBody)
                {
                    continue;
                }
                rows.Add(new RouteWindowRow(body, taken == 0 ? "ingress" : "egress", w));
                taken++;
            }
        }

        rows.Sort((a, b) => a.Window.OpensSimTime.CompareTo(b.Window.OpensSimTime));
        return rows;
    }

    /// <summary>How one route-window row reads: when it opens (or that it is open NOW), how long the captain
    /// gets on the ground once the ride home is taken out, and the RETURN BY he is bound by.</summary>
    private string RouteWindowRowText(RouteWindowRow row)
    {
        string when = row.Window.OpensSimTime <= SimTime
            ? "OPEN NOW"
            : $"opens in {RouteShuttleWindow.In(row.Window.OpensSimTime - SimTime)}";
        return $"{when} · {RouteShuttleWindow.In(row.Window.AshoreSeconds)} ashore · "
            + $"RETURN BY {RouteShuttleWindow.Stamp(row.Window.ReturnBySimTime)}";
    }

    /// <summary>The small print under the remote's clock: WHY there is a clock at all. A captain who reads
    /// "RETURN BY" wants to know in one line whether the ship is flying on without him or merely swinging
    /// out of reach for a while — those are different kinds of trouble.</summary>
    private string AwayWindowRemoteSubLine()
    {
        if (AshoreOnBodyId is not { } bodyId)
        {
            return string.Empty;
        }

        AwayWindow w = WindowOn(bodyId);
        if (w.ReturnBySimTime is not null)
        {
            return "She is flying the plan and will not wait. Miss the lift and the ship goes on without you.";
        }

        return w.Status switch
        {
            WindowStatus.Closed => "The gap is past a shuttle hop. Nobody is lost — sit tight until it swings back.",
            WindowStatus.Lost => "Nothing on the charts brings her back inside a hop. This is the bad one.",
            _ => "The gap is opening. The boat can still cross it.",
        };
    }

    /// <summary>How the shuttle-board / away-site rows say a window's state in a few words. Closed says the
    /// wait out loud, because "out of reach" with no reopening in it is what used to read as a death.</summary>
    private static string AwayWindowWord(AwayWindow w) => w.Status switch
    {
        WindowStatus.Holding => "in shuttle range",
        WindowStatus.Ticking => $"window closes in {RouteShuttleWindow.In(w.SecondsLeft)}",
        WindowStatus.Critical => "LAST CALL — the window is closing",
        WindowStatus.Closed => $"closed · next window in {RouteShuttleWindow.In(w.ReopenSeconds ?? 0)}",
        _ => "OUT OF REACH — nothing brings it back",
    };
}
