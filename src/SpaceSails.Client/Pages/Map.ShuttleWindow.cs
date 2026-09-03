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
// ONE law serves both cases (Core ExpeditionWindow.ClassifyClock, with the reopening folded in); only the
// HORIZON differs — a berth waits out one synodic period, an armed plan waits out what is left of its span,
// and a loose ship waits out its contracted budget. Both the closing and the reopening are MEASURED by
// walking the geometry, never extrapolated from a range-rate: a moon at closest approach has no rate at all,
// and the straight line through it read "window closes in 986 d" on the shipping board.
public partial class Map
{
    /// <summary>How the shuttle window to the ground the captain is standing on reads right now: the coarse
    /// status, the seconds left before it shuts, when it next opens if it is shut, and the RETURN BY the
    /// plotted route imposes when the ship is flying on without him.</summary>
    public readonly record struct AwayWindow(
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

    /// <summary>Where the ship is at <paramref name="simTime"/>, by the best promise she is under: on an
    /// ARMED plan (#969) she flies the plotted ribbon and finishes it with no input at all, so the ribbon IS
    /// her future; clamped with no such promise she simply rides the berth's rail; otherwise she coasts from
    /// where she is. This is the one place "the ship flies on" is turned into a position.</summary>
    private Vector2d ShipAnchorAt(double simTime)
    {
        // The ribbon wins wherever it reaches — INCLUDING from a berth. #955 NAV-1 made cast off a step, so a
        // plan armed while clamped is projected from PlanStartState(): the ribbon's own first sample IS the
        // berth, and it then carries her off it. Asking the clamp first would answer "she stays at the berth
        // for ever" about a ship that is on her way out, which is the one lie this file exists to stop.
        if (ShipIsFlyingAPlottedRoute && _samples.Count > 1 && simTime <= _samples[^1].SimTime)
        {
            return NodeFrame.PositionAt(_samples, simTime, _ship.Position);
        }

        if (_dockedHavenId is { } berth && _ephemeris is not null)
        {
            return _ephemeris.Position(berth, simTime) + _dockOffset;
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
        // Armed first, berth second — for the same reason ShipAnchorAt reads the ribbon first. A plan armed
        // at the clamp (#955 NAV-1's cast-off step) means she is leaving, so the berth's synodic period is
        // no longer the time anybody has.
        if (_armedArrivalPassSimTime is { } pass && pass > SimTime)
        {
            return pass - SimTime;
        }

        if (_dockedHavenId is { } berth && BodyById(berth) is { } berthBody && BodyById(bodyId) is { } site)
        {
            return ExpeditionWindow.SynodicPeriodSeconds(berthBody.OrbitPeriod, site.OrbitPeriod);
        }

        return ExpeditionWindow.DefaultHoldWindowSeconds;
    }

    /// <summary>Seconds until the window to this body reopens, or null when nothing inside the horizon
    /// brings it back. Cached on the absolute instant it promises, so the HUD counts down smoothly and the
    /// bounded scan runs on a cadence rather than per frame.</summary>
    private double? AwayReopenSeconds(string bodyId) => AwayFlipSeconds(bodyId, wantInRange: true);

    /// <summary>Seconds until the window to this body SHUTS, or +∞ when it holds for the whole horizon.
    /// Measured by walking the geometry, never extrapolated from a range-rate: a moon at its closest
    /// approach has a rate of zero, and dividing a real gap by that reads as "closes in 986 days"
    /// (<see cref="ExpeditionWindow.ClassifyClock"/> carries the whole story).</summary>
    private double AwayCloseSeconds(string bodyId) =>
        AwayFlipSeconds(bodyId, wantInRange: false) ?? double.PositiveInfinity;

    /// <summary>The one cached scan both directions ride: when does the reach test next flip? Cached on the
    /// ABSOLUTE sim time it promises, so the number on the HUD counts down smoothly between scans and the
    /// scan itself re-runs only once its own promise has come due (or gone stale).</summary>
    private double? AwayFlipSeconds(string bodyId, bool wantInRange)
    {
        string key = wantInRange ? bodyId : bodyId + " close";
        bool fresh = _awayWindowScans.TryGetValue(key, out var cached)
            && SimTime - cached.ScannedAt <= AwayWindowRescanSeconds
            && SimTime - cached.ScannedAt >= 0
            && (cached.ReopenSimTime is not { } promised || promised > SimTime);

        if (!fresh)
        {
            // The two questions want different horizons. "Will it come BACK?" is answered inside the time
            // the captain actually has — one turn of the geometry at a berth, what is left of an armed plan,
            // or the contracted budget — because a reopening later than that saves nobody. "When does it
            // SHUT?" is not bounded by any of that: the answer is simply when, and a window that holds past
            // the far horizon is a window that holds.
            double horizon = wantInRange ? AwayReopenHorizonSeconds(bodyId) : ExpeditionWindow.MaxReopenHorizonSeconds;
            double? found = wantInRange
                ? ExpeditionWindow.SecondsUntilReopen(dt => AwaySeparationAt(bodyId, SimTime + dt), horizon)
                : ExpeditionWindow.SecondsUntilClose(dt => AwaySeparationAt(bodyId, SimTime + dt), horizon);
            cached = (found is { } f ? SimTime + f : null, SimTime);
            _awayWindowScans[key] = cached;
        }

        return cached.ReopenSimTime is { } when ? Math.Max(0.0, when - SimTime) : null;
    }

    // ── The window on one body, by the one law ─────────────────────────────────────────────────────

    /// <summary>How the shuttle window to <paramref name="bodyId"/> reads right now. The status is Core's
    /// (<see cref="ExpeditionWindow.ClassifyClock"/>) with the reopening folded in, so a periodic geometry reads
    /// Closed — reopens in X — and only a gap nothing brings back reads Lost.</summary>
    private AwayWindow WindowOn(string bodyId)
    {
        if (_ephemeris is null)
        {
            return new AwayWindow(WindowStatus.Holding, double.PositiveInfinity, null, null);
        }

        double distance = AwaySeparationAt(bodyId, SimTime);
        bool inReach = distance < ExpeditionWindow.RangeMeters;
        double? reopen = inReach ? null : AwayReopenSeconds(bodyId);
        double left = inReach ? AwayCloseSeconds(bodyId) : 0.0;
        WindowStatus status = ExpeditionWindow.ClassifyClock(
            distance, left, ExpeditionWindow.DefaultCriticalSeconds, reopen);

        return new AwayWindow(status, left, reopen, RouteReturnBySimTime(bodyId));
    }

    // ── The plotted route's own windows ────────────────────────────────────────────────────────────

    /// <summary>The shuttle windows the plotted path opens on one body — the span of the SAME samples the
    /// ARRIVE step reads that lie inside a shuttle hop of it, each with its RETURN BY. Empty unless the ship
    /// is actually promised to fly that path: a ribbon nobody armed is a drawing, not a future, and offering
    /// a RETURN BY against it would be the game lying to a captain about to step off.</summary>
    private IReadOnlyList<RouteShuttleWindow.Window> RouteWindowsOn(string bodyId)
    {
        // Windows already shut are not offers. The ribbon itself is NOT trimmed to now first: a window the
        // ship is standing in the middle of has its true OPENING in the past, and cutting the ribbon there
        // would move that opening to wherever the projection's first sample happened to fall — which is how
        // "am I inside this window?" quietly becomes "did the window start on a sample boundary?".
        var ahead = new List<RouteShuttleWindow.Window>();
        foreach (RouteShuttleWindow.Window w in RouteWindowsAlongTheWholeRibbon(bodyId))
        {
            if (w.ClosesSimTime > SimTime)
            {
                ahead.Add(w);
            }
        }
        return ahead;
    }

    // Sweeping the whole ribbon against a body is thousands of ephemeris reads, and the away clock asks for
    // it every frame. The windows are ABSOLUTE sim times, so they do not age — only the "still ahead" filter
    // does — and the ribbon is replaced wholesale on every reprojection. So the projection's own object
    // identity is the exact cache key: same ribbon, same answer; new ribbon, one fresh sweep per body.
    private readonly Dictionary<string, IReadOnlyList<RouteShuttleWindow.Window>> _routeWindows = [];
    private object? _routeWindowsRibbon;
    private double? _routeWindowsArmedFor;

    private IReadOnlyList<RouteShuttleWindow.Window> RouteWindowsAlongTheWholeRibbon(string bodyId)
    {
        if (_ephemeris is null || !ShipIsFlyingAPlottedRoute || _samples.Count < 2)
        {
            return [];
        }

        if (!ReferenceEquals(_routeWindowsRibbon, _samples) || _routeWindowsArmedFor != _armedArrivalPassSimTime)
        {
            _routeWindows.Clear();
            _routeWindowsRibbon = _samples;
            _routeWindowsArmedFor = _armedArrivalPassSimTime;
        }
        else if (_routeWindows.TryGetValue(bodyId, out IReadOnlyList<RouteShuttleWindow.Window>? hit))
        {
            return hit;
        }

        var separations = new List<RouteShuttleWindow.RouteSample>(_samples.Count);
        foreach (TrajectorySample s in _samples)
        {
            separations.Add(new RouteShuttleWindow.RouteSample(
                s.SimTime, (s.Position - _ephemeris.Position(bodyId, s.SimTime)).Length));
        }

        IReadOnlyList<RouteShuttleWindow.Window> windows = RouteShuttleWindow.Along(separations);
        _routeWindows[bodyId] = windows;
        return windows;
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
            return null;   // nothing is flying on and the window holds — no clock worth a line
        }

        return RouteShuttleWindow.RemoteLine(SimTime, w.ReturnBySimTime, w.ReopenSeconds, w.SecondsLeft);
    }

    // ── The board's "on the route" section ────────────────────────────────────────────────────────

    /// <summary>One shuttle window the ARMED plan opens on a body, ready to render: which ground, whether
    /// this is the first crossing or a later one (the owner's ingress / egress), and the window itself.</summary>
    public readonly record struct RouteWindowRow(
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
