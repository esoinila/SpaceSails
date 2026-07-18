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
}
