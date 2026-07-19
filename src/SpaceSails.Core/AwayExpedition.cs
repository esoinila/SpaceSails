namespace SpaceSails.Core;

/// <summary>
/// #370 — THE AWAY EXPEDITION. The owner's spec (issue #370, verbatim): <b>"the new mission type would
/// be to take a group of scientists to a site … some dig site where they scurry about some mystical
/// ruins or structures, crashlanded ships, weird stuff … they always find something that should not be
/// possible and it tests the groups cohesion and sanity."</b> A high-pay, high-risk "take group X to Y
/// to do Z" gig — the homage is to Alien/Prometheus energy, never a reproduction.
///
/// <para>Two flavors ride the SAME skeleton (owner: "platinum etc mining survey group is an alternative
/// type with the same set-up"): a <see cref="ExpeditionFlavor.Science"/> dig or a
/// <see cref="ExpeditionFlavor.MiningSurvey"/>. The site is a <b>mission-spawned body</b> — "some
/// passing asteroid that just happens to be coming our way once we launch the mission … a piece of rock
/// that only rarely visits so it is reachable" — seeded near the ship the instant the mission is
/// accepted (the tutorial-target idiom, not a scenario edit), and it qualifies as a landable surface for
/// the shuttle.</para>
///
/// <para>This is the pure, deterministic Core spine (repo law §9 — determinism is law in Core; the
/// client stays thin): the site SPAWN math (<see cref="ExpeditionSite"/>), the hold-in-range WINDOW clock
/// (<see cref="ExpeditionWindow"/>), the diced on-site EVENT table (<see cref="AwayExpeditionEvents"/>,
/// its own file) and the PAYOUT composition (<see cref="ExpeditionReward"/>). The special surface
/// GEOGRAPHY is authored in <see cref="SurfaceLayout"/> (the ruins / crashed hull / sealed tunnel
/// schemes). Every roll is seeded from sim state the caller folds in, so client and any replay agree.</para>
/// </summary>
public enum ExpeditionFlavor
{
    /// <summary>A team of scientists sent to scurry the ruins — leans discoveries and the rare horror.</summary>
    Science,

    /// <summary>A platinum/rare-mineral survey crew (owner's alternative) — leans finds and the cold
    /// scare of "this rock was mined before, and the tool marks don't look familiar."</summary>
    MiningSurvey,
}

/// <summary>The character of the special surface the site wears — a distinct <see cref="SurfaceLayout"/>
/// scheme each (owner: "mystical ruins or structures, crashlanded ships … a previously sealed piece of
/// tunnel"). Chosen deterministically per site so the same seed always lands on the same ground.</summary>
public enum ExpeditionSiteKind
{
    /// <summary>Mystical ancient ruins — standing stones and broken arcs (the Prometheus energy).</summary>
    MysticalRuins,

    /// <summary>A crash-landed ship — a torn hull half-buried in the regolith.</summary>
    CrashedHull,

    /// <summary>A sealed tunnel cracked open — the owner's Fate-system anecdote: a charge arc holed the
    /// rock and revealed a tunnel of habitants ejected in a violent interplanetary event, dead there.</summary>
    SealedTunnel,
}

/// <summary>
/// One accepted away-expedition mission — the captain's contract, held session-only by the client (there
/// is no save system yet, same as every other mission). Pure data: the flavor, the spawned site's id and
/// how it reads, the team ferried down, the authored base fee, and the sim time the gig was struck (the
/// seed anchor for every roll it later makes). The live window/reward are derived, never stored.
/// </summary>
public sealed record ExpeditionPlan(
    ExpeditionFlavor Flavor,
    ExpeditionSiteKind SiteKind,
    string SiteBodyId,
    string SiteDisplayName,
    int TeamSize,
    int BaseFee,
    double AcceptedSimTime)
{
    /// <summary>The captain's-chip one-liner (matches the house voice of <see cref="ShipMission.Describe"/>):
    /// "Away team: Ferry scientists to The Drifter's ruins".</summary>
    public string Describe()
    {
        string who = Flavor == ExpeditionFlavor.Science ? "scientists" : "a survey crew";
        return $"Away team: {who} → {SiteDisplayName}";
    }
}

/// <summary>
/// THE SITE SPAWN — pure, deterministic placement of the passing rock relative to where the ship floats
/// when the mission is struck (owner: "spawn some special outdoors target … reachable"). The rock is
/// seeded a fraction of a shuttle-hop off the ship (comfortably IN range, so a fresh mission is instantly
/// testable — the cheat spawns it at the berth in reach), and given the ship's own velocity plus a slow
/// OUTWARD drift so the gap opens on its own: the ship must actively match course to hold the shuttle
/// window (see <see cref="ExpeditionWindow"/>). Everything here is a pure function of the seed and the
/// ship state — no clock, no <see cref="System.Random"/>.
/// </summary>
public static class ExpeditionSite
{
    /// <summary>How far off the ship the rock spawns, as a fraction of the shuttle's one-hop reach — half
    /// a hop, so it is always comfortably inside range at accept (the window has room to tick down before
    /// it is lost).</summary>
    public const double SpawnFraction = 0.5;

    /// <summary>The rock's slow outward drift relative to the ship (m/s) — a "passing asteroid" easing
    /// away. Small next to the shuttle's 8 km/s cruise, so course-matching nulls it easily; left unmatched
    /// it opens the gap and the hold-window clock runs down. OWNER-TUNABLE (issue #370: "this just needs
    /// to be playtested"); tuned so the default window is on the order of a sim-day.</summary>
    public const double DriftSpeedMps = 2500.0;

    /// <summary>A plausible little-rock radius (m) — small enough that the shuttle board never reads the
    /// ship as "basically sitting on it" (that exclusion trips inside a body's own radius).</summary>
    public const double BodyRadiusMeters = 4.0e6;

    /// <summary>The id FAMILY every spawned expedition rock carries — one runtime body at a time (a new gig
    /// replaces the old rock, exactly as the tutorial hunt re-seeds its single target). The concrete id
    /// carries the site kind as a suffix (<see cref="BodyIdFor"/>) so the surface (<see cref="SurfaceLayout.For"/>)
    /// routes straight to the right authored ground with no extra plumbing.</summary>
    public const string BodyId = "expedition-site";

    /// <summary>The concrete runtime body id for a site of <paramref name="kind"/> — the family prefix plus
    /// a kind suffix, e.g. <c>expedition-site-ruins</c>. Encoding the kind in the id lets the pure surface
    /// lookup pick the authored scheme by id alone (no client thread-through, memoization key unchanged).</summary>
    public static string BodyIdFor(ExpeditionSiteKind kind) => $"{BodyId}-{Suffix(kind)}";

    /// <summary>Recover the site kind from a body id minted by <see cref="BodyIdFor"/>. False for any id
    /// that is not an expedition rock — so <see cref="SurfaceLayout.For"/> falls through to its other
    /// schemes untouched for every ordinary body.</summary>
    public static bool TryParseKind(string? bodyId, out ExpeditionSiteKind kind)
    {
        kind = ExpeditionSiteKind.MysticalRuins;
        if (bodyId is null || !bodyId.StartsWith(BodyId + "-", System.StringComparison.Ordinal))
        {
            return false;
        }

        switch (bodyId[(BodyId.Length + 1)..])
        {
            case "ruins": kind = ExpeditionSiteKind.MysticalRuins; return true;
            case "wreck": kind = ExpeditionSiteKind.CrashedHull; return true;
            case "tunnel": kind = ExpeditionSiteKind.SealedTunnel; return true;
            default: return false;
        }
    }

    private static string Suffix(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => "wreck",
        ExpeditionSiteKind.SealedTunnel => "tunnel",
        _ => "ruins",
    };

    /// <summary>The seeded spawn of one expedition site: where the rock sits, how it drifts, its physical
    /// size, and the flavor-appropriate look. <paramref name="seed"/> folds the accept moment; the offset
    /// bearing and the site kind are drawn from it, so the same struck mission always lands the same rock.
    /// The velocity is <paramref name="shipVelocity"/> + an outward drift, so an untended ship opens the
    /// gap.</summary>
    public static SiteSpawn Spawn(ulong seed, Vector2d shipPosition, Vector2d shipVelocity, ExpeditionFlavor flavor)
    {
        var rng = new DeterministicRandom(seed);
        double bearing = rng.NextDouble(0.0, 2.0 * System.Math.PI);
        var direction = new Vector2d(System.Math.Cos(bearing), System.Math.Sin(bearing));
        double distance = SpawnFraction * ShuttleRange.RangeMeters;

        Vector2d position = shipPosition + (direction * distance);
        Vector2d velocity = shipVelocity + (direction * DriftSpeedMps); // drift straight outward → gap opens

        ExpeditionSiteKind kind = (ExpeditionSiteKind)rng.NextInt(0, 3);
        string name = SiteName(seed, kind, flavor);
        return new SiteSpawn(BodyIdFor(kind), name, kind, position, velocity, BodyRadiusMeters);
    }

    /// <summary>A house-voice display name for the rock, seeded so it reads distinct per gig — "The
    /// Drifter", "Object Kepler-9", etc. Flavor colours nothing here; the ground does.</summary>
    private static string SiteName(ulong seed, ExpeditionSiteKind kind, ExpeditionFlavor flavor)
    {
        string[] prefixes = ["The Drifter", "The Wanderer", "The Straggler", "Object 2-9-1", "The Visitor", "Cold Iris"];
        string prefix = prefixes[new DeterministicRandom(DiceRule.Seed(seed, "site-name")).NextInt(0, prefixes.Length)];
        string tail = kind switch
        {
            ExpeditionSiteKind.MysticalRuins => "ruins",
            ExpeditionSiteKind.CrashedHull => "wreck",
            _ => "tunnels",
        };
        _ = flavor;
        return $"{prefix} {tail}";
    }
}

/// <summary>The seeded spawn of one expedition rock — its id and display name, the ground it wears, where
/// it is and how it drifts, and its physical radius. The client turns this into its runtime landable
/// body / free-drifting contact; Core only decides the numbers.</summary>
public readonly record struct SiteSpawn(
    string BodyId,
    string DisplayName,
    ExpeditionSiteKind Kind,
    Vector2d Position,
    Vector2d Velocity,
    double BodyRadiusMeters);

/// <summary>
/// THE HOLD-IN-RANGE WINDOW — the honest clock the owner asked for: "I guess this could be a mission
/// clock at the away site that ticks down the window of being in shuttle range enough." Pure math over
/// the ship↔site geometry (the same reach law the shuttle board runs, <see cref="ShuttleRange"/>): read
/// the closing/opening RATE off the relative motion, and if the gap is opening, how long until it crosses
/// the shuttle-range edge and strands the team. Course-match (null the rate) and the clock holds; drift
/// and it runs down. No clock, no state — the caller folds live positions/velocities in.
/// </summary>
public static class ExpeditionWindow
{
    /// <summary>The reach edge the window is measured against — one shuttle hop (<see cref="ShuttleRange.RangeMeters"/>).</summary>
    public const double RangeMeters = ShuttleRange.RangeMeters;

    /// <summary>The sponsor's contracted on-site budget (ground-seconds): how long the mothership can hold
    /// the course-match before it must break station, so there is always a VISIBLE clock ticking at the
    /// away site (owner: "a mission clock at the away site that ticks down the window") even while the ship
    /// holds perfect range. The honest geometry window (<see cref="TimeLeftInRangeSeconds"/>) can cut this
    /// short if the ship actually drifts out. OWNER-TUNABLE.</summary>
    public const double DefaultHoldWindowSeconds = 300.0;

    /// <summary>Inside this many seconds the away-site clock reads "last call" — recall the team NOW.</summary>
    public const double DefaultCriticalSeconds = 60.0;

    /// <summary>The contracted budget remaining after <paramref name="elapsedOnSiteSeconds"/> of ground
    /// time, floored at zero — the visible countdown at the away site.</summary>
    public static double OnSiteRemainingSeconds(double holdWindowSeconds, double elapsedOnSiteSeconds) =>
        System.Math.Max(0.0, holdWindowSeconds - System.Math.Max(0.0, elapsedOnSiteSeconds));

    /// <summary>The clock the HUD actually shows: the tighter of the contracted budget remaining and the
    /// honest geometry window — whichever runs out first strands the team. An infinite (held) geometry
    /// window simply leaves the contracted budget in charge.</summary>
    public static double EffectiveClockSeconds(double onSiteRemainingSeconds, double geometryWindowSeconds) =>
        System.Math.Min(onSiteRemainingSeconds, geometryWindowSeconds);

    /// <summary>The radial RANGE-RATE (m/s) of the site relative to the ship: positive = the gap is
    /// OPENING (drifting apart), negative = CLOSING. The component of the relative velocity along the
    /// line between them — exactly what a course-match zeroes. A coincident pair reads zero.</summary>
    public static double RangeRate(Vector2d relativePosition, Vector2d relativeVelocity)
    {
        double dist = relativePosition.Length;
        return dist <= 0.0 ? 0.0 : relativeVelocity.Dot(relativePosition) / dist;
    }

    /// <summary>Seconds of window left before the team is out of shuttle reach, given the current
    /// <paramref name="distanceMeters"/> and the opening <paramref name="rangeRateMps"/>. Already past the
    /// edge → 0 (the window is lost). Holding or closing (rate ≤ 0) → <see cref="double.PositiveInfinity"/>
    /// — a matched course carries no clock. Opening → the honest time to the edge.</summary>
    public static double TimeLeftInRangeSeconds(double distanceMeters, double rangeRateMps)
    {
        if (distanceMeters >= RangeMeters)
        {
            return 0.0;
        }

        return rangeRateMps <= 0.0 ? double.PositiveInfinity : (RangeMeters - distanceMeters) / rangeRateMps;
    }

    /// <summary>How the window HUD should read — a coarse status the client colours. Out of reach = the
    /// team is stranded (the mission's failure branch); inside <paramref name="criticalSeconds"/> = a red
    /// "last call"; an opening gap with time to spare = amber "ticking"; a held/closing course = green.</summary>
    public static WindowStatus Classify(double distanceMeters, double rangeRateMps, double criticalSeconds)
    {
        if (distanceMeters >= RangeMeters)
        {
            return WindowStatus.Lost;
        }

        double left = TimeLeftInRangeSeconds(distanceMeters, rangeRateMps);
        if (double.IsPositiveInfinity(left))
        {
            return WindowStatus.Holding;
        }

        return left <= criticalSeconds ? WindowStatus.Critical : WindowStatus.Ticking;
    }
}

/// <summary>How the hold-in-range window reads right now (see <see cref="ExpeditionWindow.Classify"/>).</summary>
public enum WindowStatus
{
    /// <summary>Course matched (or closing) — the shuttle window holds; no clock.</summary>
    Holding,

    /// <summary>The gap is opening with time to spare — the clock ticks down.</summary>
    Ticking,

    /// <summary>Last call — inside the critical margin; recall the team NOW.</summary>
    Critical,

    /// <summary>The gap crossed the shuttle-range edge — the team is stranded (the failure branch).</summary>
    Lost,
}

/// <summary>
/// THE PAYOUT — "these gigs would pay really well but also carry risks." Composed the house way (owner
/// spec: "compose HaulReward.WithFloor-style with a fat expedition premium"): an authored fat base fee
/// carried through <see cref="HaulReward.WithFloor"/> so a survey dragged to a farther rock still earns
/// the distance on top, then the diced DISCOVERY bonuses are added and a per-scientist penalty is docked
/// for anyone lost to the dark — "risks priced in narration, not fine print." Never pays below the
/// keep-your-shirt floor. Pure and order-free.
/// </summary>
public static class ExpeditionReward
{
    /// <summary>The authored expedition base fee (credits) — a fat premium over ordinary haul work, since
    /// the gig risks sanity and lives. OWNER-TUNABLE.</summary>
    public const int BaseFee = 6000;

    /// <summary>What a lost scientist costs off the payout (credits) — a body left on a rock is not paid
    /// for, and the sponsor docks the gig. Narrated, not fine print.</summary>
    public const int PerScientistLostPenalty = 1500;

    /// <summary>The floor the gig can never fall below however badly it went — the crew still flew, still
    /// risked it; the sponsor pays this much for the attempt.</summary>
    public const int Floor = 1000;

    /// <summary>#370 · "the truth is worth more": the bonus a gig earns for coming home HAVING WITNESSED the
    /// reveal (<see cref="ExpeditionBrief"/>). The sponsor sold a sugar-coated lie and pays extra for what
    /// the team actually found out there — surviving past the bigger picture is the richest part of the
    /// story. OWNER-TUNABLE.</summary>
    public const int TruthBonus = 2000;

    /// <summary>The full expedition payout: the fat base carried through the haul-distance floor
    /// (<see cref="HaulReward.WithFloor"/> over the two heliocentric radii — ≈ the base for a local rock,
    /// more for a survey dragged out), plus every diced discovery bonus, plus the "truth is worth more"
    /// <paramref name="truthBonus"/> if the reveal was witnessed and survived, minus the per-scientist
    /// penalty for those lost, floored at <see cref="Floor"/>. Order-free and clamped.</summary>
    public static int Total(
        int baseFee, double fromRadiusMeters, double toRadiusMeters,
        int discoveryBonus, int scientistsLost, int truthBonus = 0)
    {
        int distanced = HaulReward.WithFloor(baseFee, fromRadiusMeters, toRadiusMeters);
        int gross = distanced + System.Math.Max(0, discoveryBonus) + System.Math.Max(0, truthBonus)
            - (System.Math.Max(0, scientistsLost) * PerScientistLostPenalty);
        return System.Math.Max(Floor, gross);
    }
}
