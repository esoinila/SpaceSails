namespace SpaceSails.Core;

/// <summary>
/// PR-313 · The shuttle asks WHERE, not WHY. The owner's model (live playtest 2026-07-18): "I am
/// expecting to take the shuttle to Miranda ... or other options if there were... not say what I
/// intend to do there." So boarding the shuttle offers <b>destinations</b>, never intentions; the
/// intentions live on the ground, contextually, and a visit commits to nothing.
///
/// <para>This is the pure, unit-tested spine of that inversion — the two decisions the client used to
/// make inline and untestably:</para>
/// <list type="bullet">
/// <item><b>The destination list by orbit context</b> (<see cref="Destinations"/>): which bodies are
/// reachable in one shuttle hop and how each reads — a berth to step off at, a landable surface to
/// walk, a place with a chest already in the ground. The same filter the old shuttle-bay pop-up ran,
/// lifted out of the Blazor page so it can be pinned in tests.</item>
/// <item><b>The ground actions by what you carry</b> (<see cref="GroundActionsFor"/>): a surface visit
/// shows the ⛏ dig site only when there is a reason to dig — a chest in cargo to bury, or a cache
/// already buried at this X. Nothing in hand and nothing in the ground → you walk, look, come home,
/// and no dice are ever rolled (the 2D6 Reevers roll on the dig, never on the trip).</item>
/// </list>
/// </summary>
public static class ShuttleExcursion
{
    /// <summary>What the surface offers at the dig site right now, as a set of contextual affordances.
    /// A visit commits to nothing: <see cref="None"/> is a complete, valid excursion (walk and return).</summary>
    [System.Flags]
    public enum GroundAction
    {
        /// <summary>No dig site — nothing to bury, nothing buried here. Walk, look, come home. No dice.</summary>
        None = 0,
        /// <summary>A chest is in cargo — the ⛏ DIG HERE site appears; pressing it buries the chest.</summary>
        BuryHere = 1,
        /// <summary>A cache is already in the ground at this X — 'dig at the X' lifts it (re-rolls the watchdogs).</summary>
        DigAtX = 2,
    }

    /// <summary>The ground actions a surface visit offers, given what the shuttle carries and what is
    /// already buried here. Carrying a chest → you may bury it; a cache under this X → you may dig it.
    /// Both can be true (you flew a chest to ground you already salted); neither → a pure sightseeing
    /// visit that rolls no dice. This is the ONLY gate on the dig site, so it is the ONLY gate on the
    /// Reever roll — no chest and no cache means the shovel never comes out.</summary>
    public static GroundAction GroundActionsFor(bool carryingChest, bool cacheBuriedHere)
    {
        GroundAction actions = GroundAction.None;
        if (carryingChest)
        {
            actions |= GroundAction.BuryHere;
        }
        if (cacheBuriedHere)
        {
            actions |= GroundAction.DigAtX;
        }
        return actions;
    }

    /// <summary>The site's active act when the captain presses [E]: burying a carried chest takes
    /// precedence (it is the live intent of having loaded it), else lifting a cache already here, else
    /// nothing. One [E], one act — the contextual choice the owner asked for.</summary>
    public static GroundAction SiteActFor(bool carryingChest, bool cacheBuriedHere) =>
        carryingChest ? GroundAction.BuryHere
        : cacheBuriedHere ? GroundAction.DigAtX
        : GroundAction.None;

    /// <summary>A surface you can walk and put a chest on: a moon (never a station or a planet). The
    /// wing-per-destination slot (#295/#303) plugs future landable kinds — wrecks, asteroids — in here.</summary>
    public static bool IsLandableSurface(BodyKind kind) => kind == BodyKind.Moon;

    /// <summary>One body the shuttle could reach, before the range filter: its kind and orbit context
    /// (a moon has a parent; the sun/planets are excluded), how far it floats now, its physical radius
    /// (so "basically on it already" is excluded), whether it has a walkable berth interior, and
    /// whether we have a chest buried there.</summary>
    public readonly record struct Candidate(
        string BodyId, BodyKind Kind, string? ParentId,
        double DistanceMeters, double BodyRadiusMeters, bool HasInterior, bool HasCache);

    /// <summary>One reachable stop on the shuttle-bay board: the body, how far / how long the hop costs,
    /// and how the row reads — a berth to step off at (<see cref="HasBerth"/>), a landable surface to
    /// walk (<see cref="IsLandableSurface"/>), and/or a place with a chest already in the ground
    /// (<see cref="HasCache"/>). A stop can be several at once (a moon with a station and a buried cache).</summary>
    public readonly record struct Destination(
        string BodyId, double DistanceMeters, double TravelSeconds,
        bool HasBerth, bool IsLandableSurface, bool HasCache);

    /// <summary>The destination board for the shuttle bay: every candidate within one hop of where the
    /// ship floats now, classified and nearest-first. The orbit-context filter is exactly the old
    /// pop-up's — skip the sun and the gas giants (no parent, or a planet), skip the berth we are
    /// already clamped to, skip anything we are basically sitting on, skip anything past shuttle range —
    /// only now it is pure and testable. <paramref name="dockedBodyId"/> is the berth to omit, or null.</summary>
    public static IReadOnlyList<Destination> Destinations(
        System.Collections.Generic.IEnumerable<Candidate> candidates, string? dockedBodyId)
    {
        System.ArgumentNullException.ThrowIfNull(candidates);
        var stops = new System.Collections.Generic.List<Destination>();
        foreach (Candidate c in candidates)
        {
            if (c.ParentId is null || c.Kind == BodyKind.Planet || c.BodyId == dockedBodyId)
            {
                continue; // the sun / a gas giant / the berth we're already at
            }
            if (c.DistanceMeters <= c.BodyRadiusMeters || !ShuttleRange.InRange(c.DistanceMeters))
            {
                continue; // basically on it already, or out of shuttle range
            }
            stops.Add(new Destination(
                c.BodyId, c.DistanceMeters, ShuttleRange.TravelSeconds(c.DistanceMeters),
                HasBerth: c.HasInterior, IsLandableSurface: IsLandableSurface(c.Kind), HasCache: c.HasCache));
        }
        stops.Sort((a, b) => a.DistanceMeters.CompareTo(b.DistanceMeters));
        return stops;
    }

    /// <summary>
    /// The chest the shuttle carries down — a snapshot of what will go into the ground: coin off the
    /// books plus the whole small hold. Both the destination-first path (board → optionally load) and
    /// any thin 'board with a chest' shortcut build the load through <see cref="Pack"/>, so the two
    /// routes are provably one path (the owner's "no duplicate path" law, #313 deliverable 4).
    /// </summary>
    public readonly record struct ChestLoad(int Coin, System.Collections.Generic.IReadOnlyList<CacheCargo> Cargo)
    {
        /// <summary>An empty load — no coin, no cargo. Boarding with an empty load carries no chest, so
        /// the surface shows no dig site (a pure sightseeing hop).</summary>
        public bool IsEmpty => Coin <= 0 && (Cargo is null || Cargo.Count == 0);
    }

    /// <summary>Pack a chest from a purse figure and the current hold, clamped to what's actually on
    /// hand. The single builder both the long path and the shortcut call, so a shortcut can be proven
    /// equal to the long path for the same inputs.</summary>
    public static ChestLoad Pack(int coin, int credits, System.Collections.Generic.IReadOnlyList<CacheCargo> hold)
    {
        int clamped = System.Math.Clamp(coin, 0, System.Math.Max(0, credits));
        return new ChestLoad(clamped, hold ?? []);
    }

    // ── #368: the honest board — the nearest ground JUST BEYOND shuttle reach ──
    // Owner (live playtest 2026-07-19, The Red Eye's empty shuttle board, verbatim): "Maybe we should
    // list the nearest landing sites here at shuttle door with their ranges?" … "Like sorry too far, the
    // nearest ones are and then comparisons of their range to shuttle range." A fresh Red Eye / Ringside
    // start has NOTHING in shuttle reach, so the board sat empty with no explanation. Below the boardable
    // (in-range) rows we now name the nearest few landable surfaces the shuttle CAN'T reach yet, each with
    // its current separation, the shuttle's reach for contrast, and whether the gap is closing or opening
    // right now — informational, never boardable.

    /// <summary>Whether the range gap to an out-of-reach body is shrinking or growing right now, from a
    /// two-sample compare of the live separation against a next-epoch separation. No new physics — the
    /// caller re-uses the same ephemeris propagation the board already runs.</summary>
    public enum RangeTrend
    {
        /// <summary>The gap held steady between the two samples (relative drift below the noise floor).</summary>
        Steady = 0,
        /// <summary>The gap is shrinking — the ground is drifting toward shuttle reach.</summary>
        Closing = 1,
        /// <summary>The gap is widening — the ground is drifting further out of reach.</summary>
        Opening = 2,
    }

    /// <summary>One out-of-reach candidate for the "nearest ground" list, sampled at two epochs so the
    /// trend is derivable without inventing physics: its kind and orbit context (same gates as
    /// <see cref="Destinations"/>), how far it floats <paramref name="DistanceMeters"/> right now, how far
    /// it will float one propagation step later (<paramref name="DistanceMetersNext"/>), and its physical
    /// radius (so "basically on it" is excluded, same as the boardable board).</summary>
    public readonly record struct RangeSample(
        string BodyId, BodyKind Kind, string? ParentId,
        double DistanceMeters, double DistanceMetersNext, double BodyRadiusMeters);

    /// <summary>One row on the "nearest ground beyond reach" list: the body, its current separation, the
    /// shuttle's max reach for contrast, and whether the gap is closing or opening now. Informational —
    /// there is no berth to board here yet.</summary>
    public readonly record struct NearbyLandable(
        string BodyId, double DistanceMeters, double RangeMeters, RangeTrend Trend)
    {
        /// <summary>How many multiples of the shuttle's reach this separation is (e.g. 1.4× of range).
        /// A humanized "×N of reach" phrasing for the house voice; 0 when reach is non-positive.</summary>
        public double TimesRange => RangeMeters > 0 ? DistanceMeters / RangeMeters : 0.0;
    }

    /// <summary>The nearest few LANDABLE surfaces the shuttle can NOT reach right now, nearest-first, each
    /// tagged with its live closing/opening trend. Same orbit-context gates as <see cref="Destinations"/>
    /// (skip the sun / gas giants / the berth we're clamped to / anything we're basically sitting on), but
    /// this is the mirror set: only walkable surfaces (a berth-only station is not "ground"), and only
    /// those OUTSIDE shuttle range (the in-range ones are already boardable above). <paramref name="limit"/>
    /// caps the list (2-3 for the board); a negative limit keeps them all.</summary>
    public static IReadOnlyList<NearbyLandable> NearestOutOfReach(
        System.Collections.Generic.IEnumerable<RangeSample> samples, string? dockedBodyId, int limit)
    {
        System.ArgumentNullException.ThrowIfNull(samples);
        var list = new System.Collections.Generic.List<NearbyLandable>();
        foreach (RangeSample s in samples)
        {
            if (s.ParentId is null || s.Kind == BodyKind.Planet || s.BodyId == dockedBodyId)
            {
                continue; // the sun / a gas giant / the berth we're already at
            }
            if (!IsLandableSurface(s.Kind))
            {
                continue; // only ground you could walk belongs on this list — not a berth-only station
            }
            if (s.DistanceMeters <= s.BodyRadiusMeters || ShuttleRange.InRange(s.DistanceMeters))
            {
                continue; // basically on it, or already in reach (boardable above, not "beyond reach")
            }
            list.Add(new NearbyLandable(s.BodyId, s.DistanceMeters, ShuttleRange.RangeMeters, TrendOf(s)));
        }
        list.Sort((a, b) => a.DistanceMeters.CompareTo(b.DistanceMeters));
        if (limit >= 0 && list.Count > limit)
        {
            list.RemoveRange(limit, list.Count - limit);
        }
        return list;
    }

    /// <summary>The closing/opening sign from the two-epoch separation samples, with a small relative noise
    /// floor so an unmoving gap reads <see cref="RangeTrend.Steady"/> rather than flickering.</summary>
    private static RangeTrend TrendOf(RangeSample s)
    {
        double delta = s.DistanceMetersNext - s.DistanceMeters;
        double eps = System.Math.Max(1.0, System.Math.Abs(s.DistanceMeters) * 1e-9);
        if (delta < -eps)
        {
            return RangeTrend.Closing;
        }
        return delta > eps ? RangeTrend.Opening : RangeTrend.Steady;
    }

    /// <summary>Whether the board must explain itself with the apologetic "nothing within reach" headline:
    /// true exactly when no destination is boardable (the in-range list is empty). The one condition the
    /// empty-board copy keys off, pinned so the headline can't drift from the list it heads.</summary>
    public static bool ExplainsEmptyBoard(int inRangeCount) => inRangeCount <= 0;
}
