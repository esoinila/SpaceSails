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

    /// <summary>
    /// #1052 (L1) · ONE WIRE, THREE MASTHEADS. The same feed machinery read under a different
    /// masthead, because who prints the paper is itself a tell.
    /// </summary>
    public enum NewsScope
    {
        /// <summary>What has always existed: the anonymous system-wide wire. The ship's galley card
        /// (key 6) and the Comms ticker read this, and pushed <see cref="NewsEvent"/>s land here.</summary>
        SystemWire,

        /// <summary>A docked port's own sheet: the system wire PLUS a per-port ambient family, salted
        /// by site so the same sim-day reads differently at Ceres than at Pallas.</summary>
        PortRag,

        /// <summary>A secret lab's internal feed: <b>no</b> system wire content at all. The company
        /// talks only to itself, which is the whole point — a facility that prints its own weather.</summary>
        CompanyIntranet,
    }

    /// <summary>
    /// #1052 (L1) · THE SEAM. Where the captain is standing, in the only three terms the wire's
    /// masthead question needs. Deliberately small: L2's seat verb fills this in from the seated
    /// context it already has, and nothing else about a place leaks into Core.
    /// </summary>
    /// <param name="AboardShip">The captain is aboard his own hull (the galley card, the Comms desk,
    /// flight). His ship carries no local paper — he reads the system wire.</param>
    /// <param name="SiteBodyId">The body whose ground/port he is docked at, or null when aboard. This
    /// is also the <c>salt</c> the ambient stream takes, so two ports never read the same day alike.</param>
    /// <param name="InsideSecretLab">He is inside a forced <see cref="SecretLab"/> region — a lab
    /// canteen table, not the port bar outside it.</param>
    /// <param name="LabForced">The <c>?secretlab=1</c> cheat forced a lab onto a body the seed did not
    /// choose; mirrors <see cref="SecretLab.For"/>'s own <c>forcePresent</c> so the cheat's world and
    /// the wire's world agree.</param>
    public readonly record struct NewsPlace(
        bool AboardShip, string? SiteBodyId, bool InsideSecretLab = false, bool LabForced = false);

    /// <summary>
    /// #1052 (L1) · "What does this place read?" — the one pure Core question behind the three
    /// mastheads, and the seam L2's seat verb consumes. Aboard, or nowhere in particular, it is the
    /// <see cref="NewsScope.SystemWire"/>; inside a lab the <see cref="NewsScope.CompanyIntranet"/>;
    /// anywhere else the captain is docked, the local <see cref="NewsScope.PortRag"/>.
    /// </summary>
    public static NewsScope ScopeAt(in NewsPlace place)
    {
        if (place.AboardShip || string.IsNullOrEmpty(place.SiteBodyId))
        {
            return NewsScope.SystemWire;
        }

        return SecretLab.ReadsCompanyIntranet(place.SiteBodyId, place.InsideSecretLab, place.LabForced)
            ? NewsScope.CompanyIntranet
            : NewsScope.PortRag;
    }

    /// <summary>The salt <see cref="Ambient"/> should be given for a place — the site's own id, so the
    /// rag at one port and the intranet at one lab each rotate on their own stream. Aboard, no salt:
    /// the system wire is the same wire everywhere, which is what makes it a system wire.</summary>
    public static string? SaltFor(in NewsPlace place) => place.AboardShip ? null : place.SiteBodyId;

    /// <summary>The small set of gameplay hooks Map.razor can push onto the wire.</summary>
    public enum NewsEventKind
    {
        RobberyCommitted,
        HunterDispatched,
        IntelPurchased,
        OrbitEnteredHaven,
        SlugHit,
        SlugMissed,
        HunterBrokeOff,
        LongHaulComplete,

        // #394 — the asteroid deflection. AsteroidInbound is the LOUD emergency the gig fires on (Subject =
        // the threatened port, Detail = the rock's read); AsteroidDeflected/AsteroidStruck are the aftermath.
        AsteroidInbound,
        AsteroidDeflected,
        AsteroidStruck,

        // #411/#663 — a story arc reaching the point where the WORLD notices. Subject is the arc's own
        // flat, clerical headline: the wire never editorialises about a plot, it files it. This is the
        // event behind StoryBeats.Beat.ArcNewsBreaks, whose card caption is written for exactly this
        // moment ("one figure walks away from the screen instead of toward it").
        ArcBeatBreaks,
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
        "The Space Bar off Mars threw out two bounty hunters before last call — house rule: check your guns, drink your credits.",
        "Cinder Roost's scrap-welders swear the whole berth drifts a little every time Venus' storms kick up. Nobody's left over it yet.",
        "Nobody at The Tilt can agree which way is up; the bar's been listing sideways off Uranus since before anyone's tab opened.",
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

    // ---- #1052 · The two new mastheads' ambient families (AUTHORED CANON — verbatim from the issue) ----
    //
    // These strings are quoted from #1052's "Authored lines (implement verbatim; crews do not write
    // canon)" section and must not be edited by an implementer. NewsScopeTests keeps its own copy of
    // the issue's text and pins every rendered line against it character for character (see
    // CompanyIntranet_UsesAllEightAuthoredLinesVerbatimAndNothingElse), so an "improved" comma here
    // fails the build. The placeholders are the AUTHOR'S OWN tokens: they stay literal in the stored
    // template and are substituted by NAME (see Substitute) rather than by string.Format position —
    // {N} is not an index and would throw.
    //
    // CANON GUARDRAIL (#1052, binding): the intranet may HINT at the fourth-world material and must
    // never state it. Nothing here names KAAMOS's purpose, restores-as-labor, or the Old Ones — the
    // #649/#677 disclosure clock owns those. Inference horror only: the reader assembles it from a
    // cold-chain that keeps doubling and a wellness check about a shore leave nobody remembers; the
    // paper stays cheerful.

    private static readonly string[] IntranetTemplates =
    [
        "The board tours the facility on {DAY}. Badges visible above the waist. Smiles are optional but are noted.",
        "{N} days since the last unscheduled decompression. The counter is bolted down now.",
        "Staff turnover in Deep Storage remains within projections. Farewell cards are in the canteen.",
        "Cold-chain capacity doubles again this quarter. Logistics thanks you in advance for not asking why.",
        "Tuesday is protein day in the canteen. Wednesday is also protein day.",
        "The archive outage of {DAY} is resolved. Files restored from backup may differ in small ways. Do not file tickets about the differences.",
        "Reminder: personal effects found in decommissioned quarters go to Lost Property for {N} days, then to Procurement.",
        "Wellness check: if you cannot remember your last shore leave, that is normal and covered.",
    ];

    private static readonly string[] PortRagTemplates =
    [
        "Berth fees at {PORT} rise a third time this quarter. The harbourmaster calls it weather.",
        "A crewman off the {SHIPNAME-template} has not reported back. His tab remains open.",
        "Customs at {PORT} now opens one crate in {N}. The queue prefers the old arrangement.",
        "Solar conditions fair. The long-haul crowd drinks anyway.",
        "Lost: one pressure glove, left hand. Reward is a drink and no questions.",
        "The {PORT} pool on next month's tariff schedule is closed. The winner is not saying.",
    ];

    /// <summary>The small ints <c>{N}</c> stands in for — a counter on a noticeboard, a crate in ten.
    /// Kept plausible on both sides: "3 days since the last unscheduled decompression" is funnier and
    /// bleaker than "471", and "one crate in 4" is a customs post, not a fantasy.</summary>
    private const int SmallIntMin = 3;
    private const int SmallIntMax = 40; // exclusive

    // The rag names a hull the reader might plausibly have seen on the board outside. That is the
    // EXISTING ship-name material (TrafficSchedule's hauler callsigns), reused rather than reinvented,
    // so the crewman who did not report back came off a ship this scenario actually runs — and so the
    // wire has no ship-name table of its own that could ever drift toward the player's boat (#1052's
    // anonymity law, guarded by TheWireNeverNamesTheCaptainTests law C).
    private static string ShipNameFor(DeterministicRandom rng) =>
        TrafficSchedule.Callsigns[rng.NextInt(0, TrafficSchedule.Callsigns.Count)];

    /// <summary>
    /// Rotating ambient flavor: <paramref name="count"/> items, one per sim-day, newest (today)
    /// first. A pure function of the ephemeris' own bodies, the sim calendar, the masthead and the
    /// site salt — the same scenario, day, scope and salt always produce the same line, so
    /// revisiting a day never contradicts what was already read.
    /// </summary>
    /// <param name="scope">Which masthead is printing. <see cref="NewsScope.SystemWire"/> (the
    /// default) is the historical stream, unchanged to the byte. <see cref="NewsScope.PortRag"/>
    /// adds the port family on top of it; <see cref="NewsScope.CompanyIntranet"/> replaces it
    /// entirely — a lab's paper carries no system news.</param>
    /// <param name="salt">The site this is being read at (#1052). Two ports on the same sim-day read
    /// differently because the salt goes into the seed; null or empty means "no site", which is the
    /// system wire's own stream and reproduces the pre-#1052 output exactly.</param>
    public static IReadOnlyList<NewsItem> Ambient(
        ICelestialEphemeris ephemeris,
        double simTime,
        int count,
        NewsScope scope = NewsScope.SystemWire,
        string? salt = null)
    {
        if (count <= 0)
        {
            return [];
        }

        long today = (long)Math.Floor(simTime / SecondsPerDay);
        List<CelestialBody> bodies = NamedBodies(ephemeris);
        CelestialBody? port = PortBody(bodies, salt);

        var items = new List<NewsItem>(count);
        for (int i = 0; i < count; i++)
        {
            long day = today - i;
            items.Add(new NewsItem(day * SecondsPerDay, HeadlineForDay(bodies, day, scope, salt, port)));
        }

        return items;
    }

    /// <summary>The body a <c>{PORT}</c> token means: the one the salt names (by id or by name), so
    /// the rag at Ceres talks about Ceres. A salt that names no body in this ephemeris (a lab site
    /// id, a scenario that renamed its berths) falls through to null and the line takes a seeded
    /// body instead — a rag with a wrong-but-plausible port beats a rag with "{PORT}" in it.</summary>
    private static CelestialBody? PortBody(List<CelestialBody> bodies, string? salt)
    {
        if (string.IsNullOrEmpty(salt))
        {
            return null;
        }

        foreach (CelestialBody body in bodies)
        {
            if (string.Equals(body.Id, salt, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(body.Name, salt, StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }
        }

        return null;
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

    private static string HeadlineForDay(
        List<CelestialBody> bodies, long day, NewsScope scope, string? salt, CelestialBody? port)
    {
        var rng = new DeterministicRandom(HashSeed(day, salt));

        // The company talks only to itself: no system wire content reaches an intranet at all.
        if (scope == NewsScope.CompanyIntranet)
        {
            return Substitute(IntranetTemplates[rng.NextInt(0, IntranetTemplates.Length)], rng, day, bodies, port);
        }

        var groups = new List<string[]> { FlatLines, CargoTemplates };
        if (bodies.Count >= 1)
        {
            groups.Add(BodyTemplates);
        }
        if (bodies.Count >= 2)
        {
            groups.Add(RouteTemplates);
        }

        // The rag is the wire PLUS the port's own sheet — appended last so the SystemWire stream's
        // group ordering, and therefore its every historical headline, is untouched.
        if (scope == NewsScope.PortRag)
        {
            groups.Add(PortRagTemplates);
        }

        string[] group = groups[rng.NextInt(0, groups.Count)];
        string template = group[rng.NextInt(0, group.Length)];

        if (group == PortRagTemplates)
        {
            return Substitute(template, rng, day, bodies, port);
        }

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

    /// <summary>#1052 · Fill the AUTHOR'S named tokens in an authored line. Named rather than
    /// positional because the canon strings carry <c>{N}</c>, <c>{DAY}</c>, <c>{PORT}</c>,
    /// <c>{BODY}</c> and <c>{SHIPNAME-template}</c> literally — the stored template stays byte-for-byte
    /// what the issue authored, and only the render substitutes. Every token is drawn from
    /// <paramref name="rng"/> in a fixed order, so a line is a pure function of its day and salt.</summary>
    private static string Substitute(
        string template, DeterministicRandom rng, long day, List<CelestialBody> bodies, CelestialBody? port)
    {
        // Draw unconditionally and in a fixed order: a token's value must not depend on which OTHER
        // tokens the line happens to contain, or two lines from one family would share a stream.
        int smallInt = rng.NextInt(SmallIntMin, SmallIntMax);
        string shipName = ShipNameFor(rng);
        string anyBody = bodies.Count > 0 ? bodies[rng.NextInt(0, bodies.Count)].Name : "the yards";
        string portName = port?.Name ?? anyBody;

        return template
            .Replace("{SHIPNAME-template}", shipName, StringComparison.Ordinal)
            .Replace("{PORT}", portName, StringComparison.Ordinal)
            .Replace("{BODY}", anyBody, StringComparison.Ordinal)
            .Replace("{DAY}", DayToken(day), StringComparison.Ordinal)
            .Replace("{N}", smallInt.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>How a sim-date reads in a printed line — the same "day N" the save slots, the field
    /// book and the selfie ledger already speak, so a notice board and a logbook agree.</summary>
    private static string DayToken(long day) =>
        $"day {Math.Max(0, day).ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    // FNV-1a 64-bit, salted so this stream never collides with any other deterministic roll
    // keyed by the same sim-day elsewhere in Core (mirrors EncounterRule.HashSeed's reasoning).
    //
    // #1052 · The SITE salt is folded in AFTER the day's eight bytes, so an empty/absent salt leaves
    // the hash bit-for-bit what it was before this lane existed — that is what keeps the system wire's
    // every historical headline unchanged (AmbientSystemWire_ReadsExactlyAsItDidBefore pins it), while
    // Ceres and Pallas walk different streams on the same afternoon.
    private static ulong HashSeed(long day, string? salt = null)
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

            if (!string.IsNullOrEmpty(salt))
            {
                foreach (char c in salt)
                {
                    hash ^= (byte)c;
                    hash *= prime;
                    hash ^= (byte)(c >> 8);
                    hash *= prime;
                }
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
        NewsEventKind.HunterBrokeOff =>
            $"{evt.Subject} has broken off the chase — the contract, it seems, wasn't worth the hull. Someone's underwriters are furious.",
        NewsEventKind.LongHaulComplete =>
            $"A ship crossed the deep black to {evt.Subject} — weeks of void{(evt.Detail is null ? "" : $" ({evt.Detail})")}, one long silence, then a berth light on the scope.",
        NewsEventKind.AsteroidInbound =>
            $"⚠ COLLISION ALERT — an inbound rock is on a line for {evt.Subject}{(evt.Detail is null ? "" : $" ({evt.Detail})")}. Every hull with a drill and a death wish, the Exchange is paying.",
        NewsEventKind.AsteroidDeflected =>
            $"{evt.Subject} still stands: a crew rode the rock down and shoved it off the line before it arrived. The rings kept the orbit. Drinks are on the Exchange.",
        NewsEventKind.AsteroidStruck =>
            $"The rock reached {evt.Subject}. Heavy damage across the trade decks and the berths are a mess, but she held — the Exchange is already clearing wreckage and reopening dock by dock.",
        // #411/#663 — the subject IS the headline. An arc beat arrives already written in the voice of
        // whoever filed it, because the alternative is the wire explaining a plot to the player.
        NewsEventKind.ArcBeatBreaks => evt.Subject,
        _ => "Static on the wire.",
    };
}
