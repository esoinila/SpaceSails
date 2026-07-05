namespace SpaceSails.Core;

/// <summary>
/// The ship's news wire (PR-14, docs/SaturdayPlan/StationDesks.md #14): one deterministic feed
/// of world "events" that backs both the Comms desk's ticker and the Galley's full news feed.
/// Two sources: <see cref="Ambient"/> rotates flavor gossip that is a pure function of the
/// scenario's own bodies, cargo classes, and the sim calendar; <see cref="Headline"/> narrates a
/// small set of gameplay hooks the UI pushes explicitly (robbery, hunter dispatch, intel buy,
/// orbiting a haven). Determinism is law in Core (§9): no <c>DateTime.Now</c>, no
/// <c>System.Random</c> — the same scenario and the same sim-day always read the same headline
/// on every machine, exactly like <see cref="EncounterRule"/>'s per-id hail lines.
/// </summary>
public static class NewsWire
{
    public const double SecondsPerDay = 86400.0;

    /// <summary>The small set of gameplay hooks Map.razor can push onto the wire.</summary>
    public enum NewsEventKind
    {
        RobberyCommitted,
        HunterDispatched,
        IntelPurchased,
        OrbitEnteredHaven,
        SlugHit,
        SlugMissed,
    }

    /// <summary>One player-triggered event, dated and named. <paramref name="Subject"/> is the
    /// headline's main name (a callsign or a body); <paramref name="Detail"/> is an optional
    /// second name (e.g. the port a hunter fits out at).</summary>
    public readonly record struct NewsEvent(NewsEventKind Kind, double SimTime, string Subject, string? Detail = null);

    /// <summary>One dated line on the wire — what both the Comms ticker and the Galley's long
    /// feed render, whether it came from <see cref="Ambient"/> or from <see cref="Headline"/>.</summary>
    public readonly record struct NewsItem(double SimTime, string Headline);

    // ---- Ambient flavor: rotating gossip derived from scenario content, seeded by sim-day ----
    // (mirrors the Galley v1 stub's "one deterministic headline per sim-day", now pulling real
    // body names and cargo classes instead of a fixed list, so it reads fresh in any scenario.)

    private static readonly string[] FlatLines =
    [
        "The Titan haulers' union is 'reviewing' its timetable policy. Read: going quiet.",
        "Deep-range scan folk swear a pyramid crossed their bow out past 2 AU — impossibly fast, dead silent, gone by second look.",
        "A quiet corner of the dark web is offering route intel at half price. Feels like bait.",
        "Enceladus haven regulars swap the same three rumors, louder every night.",
        "Ringside Exchange floor traders are jumpy — nobody will say why, which is answer enough.",
        "Mercury Compute Farms is hiring 'discreet' couriers. Pay in credits, not questions.",
        "Someone laser-ranged a haven last week. The haven laser-ranged back.",
        "A masked freighter cleared customs without a manifest. Nobody asked twice.",
        "The underwriters are quietly raising piracy premiums again. Somebody's business is booming.",
    ];

    private static readonly string[] BodyTemplates =
    [
        "{0} traffic control reports a backlog of hopeful haulers — everyone wants a slot.",
        "Word from {0}: the docking fee schedule went up again. The regulars grumble, the desperate pay.",
        "{0} quietly doubled its transit tolls. The regulars are not amused.",
        "Dockhands at {0} are on a work slowdown — 'security concerns' nobody will name.",
        "A captain swears {0} traffic control waved through a ship with no transponder at all.",
    ];

    private static readonly string[] RouteTemplates =
    [
        "Ringside Exchange reports a glut of futures on the {0}–{1} run — margins are thin this week.",
        "The {0}–{1} corridor has a new toll collector, or so the gossip runs.",
        "A trading post near {1} is paying premium for stale {0} route intel — no questions asked.",
        "Rumor: a captain out of the Belt is buying up old mass-driver pods along the {0}–{1} line.",
    ];

    private static readonly string[] CargoTemplates =
    [
        "{0} futures ticked up on the Ringside Exchange overnight; haulers grumble about margins.",
        "Mercury Compute Farms is quietly stockpiling {0} — for a project nobody will name.",
        "Someone's cornering the {0} market. Ask no questions, sell no lies.",
        "A {0} shipment went 'missing' in transit. The insurers are not amused; the fences are thrilled.",
    ];

    private static readonly string[] CargoClasses = ["He3", "Compute cores", "Alloys", "Machinery", "Ice"];

    /// <summary>
    /// Rotating ambient flavor: <paramref name="count"/> items, one per sim-day, newest (today)
    /// first. A pure function of the ephemeris' own bodies and the sim calendar — the same
    /// scenario and the same day always produce the same line, so revisiting a day never
    /// contradicts what was already read.
    /// </summary>
    public static IReadOnlyList<NewsItem> Ambient(ICelestialEphemeris ephemeris, double simTime, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        long today = (long)Math.Floor(simTime / SecondsPerDay);
        List<CelestialBody> bodies = NamedBodies(ephemeris);

        var items = new List<NewsItem>(count);
        for (int i = 0; i < count; i++)
        {
            long day = today - i;
            items.Add(new NewsItem(day * SecondsPerDay, HeadlineForDay(bodies, day)));
        }

        return items;
    }

    /// <summary>Bodies worth gossiping about — everything except the sun (a parentless body is
    /// the primary itself, not a bus stop; <c>Map.razor</c>'s own orbit tracking skips it for the
    /// same reason).</summary>
    private static List<CelestialBody> NamedBodies(ICelestialEphemeris ephemeris)
    {
        var bodies = new List<CelestialBody>(ephemeris.Bodies.Count);
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId is not null)
            {
                bodies.Add(body);
            }
        }

        return bodies;
    }

    private static string HeadlineForDay(List<CelestialBody> bodies, long day)
    {
        var rng = new DeterministicRandom(HashSeed(day));

        var groups = new List<string[]> { FlatLines, CargoTemplates };
        if (bodies.Count >= 1)
        {
            groups.Add(BodyTemplates);
        }
        if (bodies.Count >= 2)
        {
            groups.Add(RouteTemplates);
        }

        string[] group = groups[rng.NextInt(0, groups.Count)];
        string template = group[rng.NextInt(0, group.Length)];

        if (group == BodyTemplates)
        {
            return string.Format(template, bodies[rng.NextInt(0, bodies.Count)].Name);
        }

        if (group == RouteTemplates)
        {
            int i = rng.NextInt(0, bodies.Count);
            int j = (i + 1 + rng.NextInt(0, bodies.Count - 1)) % bodies.Count; // always != i
            return string.Format(template, bodies[i].Name, bodies[j].Name);
        }

        if (group == CargoTemplates)
        {
            return string.Format(template, CargoClasses[rng.NextInt(0, CargoClasses.Length)]);
        }

        return template; // FlatLines — no substitution
    }

    // FNV-1a 64-bit, salted so this stream never collides with any other deterministic roll
    // keyed by the same sim-day elsewhere in Core (mirrors EncounterRule.HashSeed's reasoning).
    private static ulong HashSeed(long day)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis ^ 0x4E657773576972UL; // "NewsWir"
        unchecked
        {
            ulong bits = (ulong)day;
            for (int shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(bits >> shift);
                hash *= prime;
            }
        }

        return hash;
    }

    // ---- Event headlines: pirate-flavored narration of a small set of gameplay hooks ----

    /// <summary>Narrates a pushed <see cref="NewsEvent"/> — pure formatting, no randomness, so
    /// the same event always reads the same.</summary>
    public static string Headline(NewsEvent evt) => evt.Kind switch
    {
        NewsEventKind.RobberyCommitted =>
            $"Piracy alert: {evt.Subject} was boarded and cleaned out. The underwriters are already drafting angry letters.",
        NewsEventKind.HunterDispatched =>
            $"{evt.Subject} is fitting out at {evt.Detail ?? "a policed port"} — the hunt is on.",
        NewsEventKind.IntelPurchased =>
            $"Word on the wire: somebody just bought a fix on {evt.Subject}. Watch your six.",
        NewsEventKind.OrbitEnteredHaven =>
            $"A ship slipped quietly into orbit at {evt.Subject} — the regulars ask no names.",
        NewsEventKind.SlugHit =>
            $"Someone put a slug through {evt.Subject}'s sail — she's dead in the water and drifting{(evt.Detail is null ? "" : $" near {evt.Detail}")}.",
        NewsEventKind.SlugMissed =>
            $"A mass-driver round evaporated somewhere past {evt.Subject}'s wake. Warning, or bad gunnery — opinions differ.",
        _ => "Static on the wire.",
    };
}
