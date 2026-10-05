using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #653 slice 1 · THE DEAD STATION GETS A CLIENT — the pure half of walking one.
///
/// <para><see cref="StationWreck"/> already says where every wall, module and way in IS, and
/// <see cref="StationHop"/> already prices flying between the parts. What neither says is the small set of
/// facts the CLIENT needs to put a captain inside: how a body id names a station (the same pseudo-body trick a
/// derelict uses, <see cref="Derelict.BodyIdPrefix"/>), what each module's door-plate reads, where the shuttle's
/// own fixtures stand at whichever access she is mated to, and how near the dock counts as "back at the boat".
/// All of it is geometry and canon, so all of it is here, where a test can walk it.</para>
///
/// <para><b>Canon rule (Fable, 2026-10-05).</b> Wherever <see cref="StationWreck"/> or <see cref="StationHop"/>
/// already carries a line (<see cref="StationWreck.BlockageLine"/>, <see cref="StationHop.CastOffLine"/>,
/// <see cref="StationHop.RefusalLine"/>) the Core line stands. The lines below fill only the gaps the issue
/// listed: the first standing aboard, the field-book entry, the module plates, the serviceable lock and the cut
/// face. Nothing here states why the station is dead or whether anyone remained (§13.8).</para>
/// </summary>
public static class StationAboard
{
    // ── The body id ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every station body's id starts with this, so the client can route a station by id alone — the
    /// trick the derelict (<see cref="Derelict.BodyIdPrefix"/>) and the expedition sites already use.</summary>
    public const string BodyIdPrefix = "station-";

    /// <summary>The body id a given station spawns under.</summary>
    public static string BodyIdFor(string stationId) => BodyIdPrefix + (stationId ?? string.Empty);

    /// <summary>Is this body a dead station, and if so which one? The one predicate the client branches on.</summary>
    public static bool TryParseStationId(string? bodyId, out string stationId)
    {
        if (bodyId is not null && bodyId.StartsWith(BodyIdPrefix, StringComparison.Ordinal)
            && bodyId.Length > BodyIdPrefix.Length)
        {
            stationId = bodyId[BodyIdPrefix.Length..];
            return true;
        }

        stationId = string.Empty;
        return false;
    }

    // ── The canon, verbatim (issue #653, "CANON: slice 1's lines") ───────────────────────────────────────

    /// <summary>Canon addendum (Fable, 2026-10-05): the station's name on the board and in every label.</summary>
    public const string DevStationName = "Ledger Point";

    /// <summary>Canon addendum, verbatim (U+2014): the board / approach blurb.</summary>
    public const string BoardBlurb = "Ledger Point — dark these forty years, and still nothing owing.";

    /// <summary>Every line of prose this file carries — the four lines, the five plates, the name and the
    /// blurb — so one guard can hold them all to the canon and to the no-cause law.</summary>
    public static IReadOnlyList<string> AllProse() =>
    [
        FirstStandingLine, FieldBookLine, LockLine, CutFaceLine,
        .. StationWreck.Modules.Select(m => PlateOf(m.Id)),
        DevStationName, BoardBlurb,
    ];

    /// <summary>First standing aboard — said once.</summary>
    public const string FirstStandingLine =
        "Your lamp is the only thing with an opinion. The station keeps its own counsel: air flat, decks true, "
        + "every door exactly as somebody left it.";

    /// <summary>The field book's 📍 line — filed once.</summary>
    public const string FieldBookLine =
        "A dead station, tubes severed, books balanced. Nobody home and nothing missing — which is two strange "
        + "things, not one.";

    /// <summary>The serviceable lock: locked door = TIME, never a key (the 2026-09-13 ruling).</summary>
    public const string LockLine =
        "The lock is a standard pattern, forty years polite. It will open for patience, and patience is the one "
        + "thing aboard in quantity.";

    /// <summary>After the hull cutter does its work.</summary>
    public const string CutFaceLine =
        "The face comes away clean. You are now somebody who cuts into stations. The station doesn't mind. "
        + "That's the part you file.";

    /// <summary>The door-plate on each module — every plate enforcing rules on nobody, played absolutely
    /// straight.</summary>
    public static string PlateOf(StationWreck.ModuleId module) => module switch
    {
        StationWreck.ModuleId.Hub => "HUB — TRANSFERS & TALLY",
        StationWreck.ModuleId.Habitat => "HABITAT — 40 BERTHS, KEEP IT DOWN",
        StationWreck.ModuleId.Foundry => "FOUNDRY — EAR PROTECTION PAST THIS LINE",
        StationWreck.ModuleId.Docking => "DOCKING — DECLARE BEFORE YOU BERTH",
        _ => "REACTOR — TWO-MAN RULE, NO EXCEPTIONS",
    };

    // ── The time a serviceable lock costs ────────────────────────────────────────────────────────────────

    /// <summary>How long a serviceable lock takes to cycle, in seconds. The cost of a locked door is TIME and
    /// never a key, and the clock is the one the boat's own crossing already spends. FLAGGED for the owner's
    /// tuning: it is longer than a cut (<see cref="HullCutter.CutSeconds"/>) on purpose — patience is what the
    /// lock asks for, and a captain who carries a cutter has bought the faster, louder, permanent road.</summary>
    public const double LockCycleSeconds = 20.0;

    // ── Where the shuttle's own fixtures stand ───────────────────────────────────────────────────────────

    /// <summary>How near the mated access counts as "back at the shuttle" — the station's answer to the moon's
    /// tube mouth and the derelict's lock (<c>AwayTeamSide</c>). Wide enough to take in the dock fixtures and
    /// the arrival square, narrow enough that a captain has to walk away from the boat to be away.</summary>
    public const double DockReachDu = 6.0;

    /// <summary>How far inboard of the wall the dock's fixtures stand, so the captain can reach them.</summary>
    public const double FixtureInboardDu = 1.2;

    /// <summary>How far apart the dock's fixtures stand along the wall — over the console crowding law's 2.0.</summary>
    public const double FixtureSpacingDu = 2.2;

    /// <summary>The unit step from an access INTO its module — the same direction
    /// <see cref="StationWreck.SpawnFor"/> uses for its three-unit setting-down.</summary>
    public static (double X, double Y) InboardOf(StationWreck.ModuleId module) => module switch
    {
        StationWreck.ModuleId.Hub => (1, 0),
        StationWreck.ModuleId.Habitat => (0, -1),
        StationWreck.ModuleId.Foundry => (0, 1),
        StationWreck.ModuleId.Docking => (-1, 0),
        _ => (1, 0),
    };

    /// <summary>Where the dock's <paramref name="slot"/>th fixture stands at this access. Slot 0 is the boat's
    /// own lock; slots 1.. are the destinations a hop can name. Offsets run 0, +, −, +, − along the wall, so
    /// the hub's short stretch of wall above its crew lock takes three and every arm takes all four.</summary>
    public static (double X, double Y) DockFixtureAt(in StationWreck.Access access, int slot)
    {
        (double ix, double iy) = InboardOf(access.Module);
        (double tx, double ty) = (-iy, ix);   // along the wall, perpendicular to the way in
        int rank = (slot + 1) / 2;            // 0, 1, 1, 2, 2
        double along = (slot % 2 == 1 ? 1 : -1) * rank * FixtureSpacingDu;
        return (access.X + (ix * FixtureInboardDu) + (tx * along),
                access.Y + (iy * FixtureInboardDu) + (ty * along));
    }

    /// <summary>The most destinations a dock can name: the hub's own group can lose three arms, and an arm
    /// can name the hub and the three others.</summary>
    public const int MostDestinations = 4;

    /// <summary>Is the captain within reach of the access the boat is mated to?</summary>
    public static bool AtTheDock(in StationWreck.Access dock, double x, double y) =>
        DistanceToTheDock(dock, x, y) <= DockReachDu;

    /// <summary>How far the captain is from the boat — the one distance the suit has an opinion about.</summary>
    public static double DistanceToTheDock(in StationWreck.Access dock, double x, double y)
    {
        double dx = x - dock.X, dy = y - dock.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>0 at the dock, 1 at the far corner of the station — the same question
    /// <c>AwayTeamSide.HowFarInside</c> asks of a moon and a derelict, for the comms onset.</summary>
    public static double HowFarFromTheDock(in StationWreck.Access dock, double x, double y)
    {
        (double minX, double minY, double maxX, double maxY) = StationWreck.Bounds;
        double span = Math.Sqrt(((maxX - minX) * (maxX - minX)) + ((maxY - minY) * (maxY - minY)));
        return Math.Clamp(DistanceToTheDock(dock, x, y) / span, 0.0, 1.0);
    }

    /// <summary>The access into one module — every module has exactly one, always (<see cref="StationWreck.Accesses"/>).</summary>
    public static StationWreck.Access AccessOf(string stationId, StationWreck.ModuleId module)
    {
        foreach (StationWreck.Access a in StationWreck.Accesses(stationId))
        {
            if (a.Module == module)
            {
                return a;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(module), module, "every module has an access");
    }

    // ── What a hop costs once the lock is counted ────────────────────────────────────────────────────────

    /// <summary>The quote with the destination's lock folded in: a serviceable lock the boat comes alongside
    /// must still cycle (<see cref="LockCycleSeconds"/>), so the honest number a captain plans around is the
    /// flight PLUS the lock. A refused quote is returned untouched, and a face that has been cut stands open
    /// at no extra cost. One number then rides the label, the cast-off line and the clock.</summary>
    public static StationHop.Quote WithTheLock(in StationHop.Quote quote, in StationWreck.Access arrival) =>
        quote.Refused == StationHop.Refusal.None && arrival.Kind == StationWreck.AccessKind.ServiceableLock
            ? quote with { Seconds = quote.Seconds + LockCycleSeconds }
            : quote;

    // ── The severed tubes, read from either end ──────────────────────────────────────────────────────────

    /// <summary>Where a captain stands to read a tube that will not let them through — one end on the hub's
    /// face, one on the arm's. Inboard of a solid face, in the middle of the bore.</summary>
    public static IReadOnlyList<(StationWreck.ModuleId Standing, double X, double Y)> TubeEnds(
        StationWreck.ModuleId arm)
    {
        StationWreck.Module hub = StationWreck.ModuleOf(StationWreck.ModuleId.Hub);
        StationWreck.Module m = StationWreck.ModuleOf(arm);
        double i = FixtureInboardDu;
        return arm switch
        {
            StationWreck.ModuleId.Habitat =>
                [(StationWreck.ModuleId.Hub, 0, hub.Y1 - i), (arm, 0, m.Y0 + i)],
            StationWreck.ModuleId.Foundry =>
                [(StationWreck.ModuleId.Hub, 0, hub.Y0 + i), (arm, 0, m.Y1 - i)],
            StationWreck.ModuleId.Docking =>
                [(StationWreck.ModuleId.Hub, hub.X1 - i, 0), (arm, m.X0 + i, 0)],
            _ => [(StationWreck.ModuleId.Hub, hub.X0 + i, 0), (arm, m.X1 - i, 0)],
        };
    }

    /// <summary>Which severed tube a reading spot belongs to, asked of the spot's own coordinates — the press
    /// asks the SPOT, never a remembered target (#801's lesson). Null when the point is not a tube end.</summary>
    public static StationWreck.ModuleId? ArmOfTubeEnd(double x, double y)
    {
        foreach (StationWreck.ModuleId arm in StationWreck.Arms)
        {
            foreach ((StationWreck.ModuleId _, double ex, double ey) in TubeEnds(arm))
            {
                if (Math.Abs(ex - x) < 0.05 && Math.Abs(ey - y) < 0.05)
                {
                    return arm;
                }
            }
        }
        return null;
    }

    /// <summary>Which destination stands at a fixture — the quote the deck was built from, found again from
    /// the fixture's coordinates. The deck's slot order is <see cref="StationHop.Destinations"/>' own, so a
    /// press and a label can never name different places.</summary>
    public static (StationWreck.ModuleId Module, StationHop.Quote Quote)? DestinationAt(
        string stationId, StationWreck.ModuleId dock,
        IReadOnlyCollection<StationWreck.ModuleId>? cutFaces, double x, double y)
    {
        StationWreck.Access at = AccessOf(stationId, dock);
        int slot = 1;
        foreach ((StationWreck.ModuleId module, StationHop.Quote quote)
                 in StationHop.Destinations(stationId, dock, cutFaces))
        {
            (double fx, double fy) = DockFixtureAt(at, slot++);
            if (Math.Abs(fx - x) < 0.05 && Math.Abs(fy - y) < 0.05)
            {
                return (module, quote);
            }
        }
        return null;
    }

    // ── A cut face is remembered ─────────────────────────────────────────────────────────────────────────

    /// <summary>The tag a cut face leaves in the durable register (#615's <c>_roomsTurnedOver</c>, the idiom
    /// #711/#794/#319 already ride): permanent evidence that somebody came in the side.</summary>
    public static string CutTag(string stationId, StationWreck.ModuleId module) =>
        $"station-cut:{stationId}:{module}";

    /// <summary>Which faces of this station the register says have been cut. Core reads its own key, so no
    /// second reader of the format lives in the client.</summary>
    public static IReadOnlyList<StationWreck.ModuleId> CutsIn(IEnumerable<string> register, string stationId)
    {
        ArgumentNullException.ThrowIfNull(register);
        ArgumentNullException.ThrowIfNull(stationId);
        var cuts = new HashSet<string>(register, StringComparer.Ordinal);
        return [.. StationWreck.Arms.Where(m => cuts.Contains(CutTag(stationId, m)))];
    }

    // ── Which module a point is in ───────────────────────────────────────────────────────────────────────

    /// <summary>The module a point stands in, or null in a tube or outside the station.</summary>
    public static StationWreck.Module? ModuleAt(double x, double y)
    {
        foreach (StationWreck.Module m in StationWreck.Modules)
        {
            if (x >= m.X0 && x <= m.X1 && y >= m.Y0 && y <= m.Y1)
            {
                return m;
            }
        }
        return null;
    }
}
