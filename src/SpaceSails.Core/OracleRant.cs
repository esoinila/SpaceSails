namespace SpaceSails.Core;

/// <summary>What a true-line is actually POINTING at — the real thing the oracle perceives on a channel
/// the sane can't. Most lines are <see cref="OracleTruthKind.None"/> (nonsense); a rare few carry one of
/// these. Fragment kinds deliver an ARC shard (the caller assembles it into
/// <see cref="KaamosProgress"/>/<see cref="NebulaProgress"/> by <see cref="OracleLine.FragmentId"/>).</summary>
public enum OracleTruthKind
{
    /// <summary>Nonsense/flavor — a perception with nothing actionable under it. The common case.</summary>
    None,

    /// <summary>A hidden secret-lab tell (#409): she perceives a sealed lab where the ground "goes quiet"
    /// to radiation. A lead the player still has to walk out and dig, not an unlock.</summary>
    SecretLab,

    /// <summary>A collector warning: she reads the "colour past violet" of a recovery writ warming up.
    /// True in spirit (and literally, when heat is up) — settle the debt before it walks in.</summary>
    Collector,

    /// <summary>An actual PROJEKTI KAAMOS shard (#411) — <see cref="OracleLine.FragmentId"/> is a real
    /// <see cref="KaamosLore.Fragments"/> id the caller hands to <see cref="KaamosProgress.Assemble"/>.</summary>
    KaamosFragment,

    /// <summary>An actual NEBULA MUTUAL shard (#422) — <see cref="OracleLine.FragmentId"/> is a real
    /// <see cref="NebulaLore.Fragments"/> id the caller hands to <see cref="NebulaProgress.Assemble"/>.</summary>
    NebulaFragment,
}

/// <summary>One thing the oracle just said. Pure data — the client renders it, and for a fragment true-
/// line (<see cref="FragmentId"/> set) tries to assemble that shard. <see cref="RoomGoesQuiet"/> is THE
/// TELL: a subtle "maybe this one's real" cue (the room hushes / a faint chill) — it fires on MOST true
/// lines but also on a minority of nonsense, so it is never a definitive marker. Sifting is on the
/// player.</summary>
/// <param name="Text">The line, verbatim and brace-free — a perception, never a plain claim.</param>
/// <param name="IsTrue">True when this line "sounds nuts but is TRUE" (carries a <see cref="Truth"/>).</param>
/// <param name="Truth">What the truth points at (<see cref="OracleTruthKind.None"/> for nonsense).</param>
/// <param name="FragmentId">For a fragment truth, the real arc id to assemble; else null.</param>
/// <param name="RoomGoesQuiet">The unreliable tell — the room hushes / a chill lands on this line.</param>
public readonly record struct OracleLine(
    string Text, bool IsTrue, OracleTruthKind Truth, string? FragmentId, bool RoomGoesQuiet);

/// <summary>
/// THE STATION ORACLE (issue #425, owner 2026-07-20, the BSG "Base Star Hybrid" key): a recurring
/// ranting-drunk NPC whose stream-of-consciousness is mostly nonsense but SOMETIMES lands a line that
/// "sounds nuts but is TRUE." The homage is exact — nonsense that turns out meaningful — and she is a
/// fragment-delivery vehicle for the two arcs (KAAMOS #411 / Nebula #422): someone the truth BROKE, still
/// tuned to the channel the sane can't hear.
///
/// <para><b>The tone (owner 2026-07-20, quoted):</b> "I love that Base Star Hybrid babble … it is so out
/// there but kind of makes sense at the same time if you see x-rays and breathe cosmic dust." So the
/// nonsense is NOT random gibberish — it is a mind perceiving REALITY ON EXTRA CHANNELS we lack: she SEES
/// radiation as weather, TASTES cosmic dust and solar wind, FEELS orbital resonance and tides in her body,
/// HEARS the archived dead as an off-key chorus. Each line is uncanny-plausible: out-there, yet it clicks
/// if you accept she perceives x-rays and breathes dust. This is also WHY the true-lines land — when she
/// says something real, it is because she literally perceives it on a channel you don't, so a truth is
/// phrased as a PERCEPTION, not a claim.</para>
///
/// <para><b>The character (original, homage-not-reproduction).</b> <see cref="FullName"/> — a lapsed
/// NEBULA MUTUAL <i>pattern-auditor</i>: her job was to listen to the archived dead and certify each was
/// still "lucid enough to be a valid backup." She listened too long. Then she let her own premium lapse —
/// forfeited her pattern to the cold archive (<see cref="NebulaLore"/>) — and something down there kept her
/// frequency. She also wintered a season on a cold-storage barge in the ice moon's shadow (<see
/// cref="KaamosLore"/>), so the "same forty names" are a chorus she can't stop hearing. That is why she can
/// leak from EITHER arc: she is downwind of both.</para>
///
/// <para><b>Pure and seeded (repo agreement §9).</b> No wall clock, no <see cref="System.Random"/>: a line
/// is a deterministic function of (station, watch, draw index, drinks bought) via a splitmix64 hash, so a
/// given oracle's true-lines are STABLE and the whole thing is unit-testable without a browser. Mostly
/// nonsense; truths are rare (<see cref="BaseTrueChance"/>) and buying her a drink makes her more prophetic
/// (<see cref="TrueChancePerDrink"/>) — "a drunk oracle is more prophetic." This class holds the authored
/// text and the pure draw logic and touches no world code; the client assembles any delivered fragment.</para>
/// </summary>
public static class OracleRant
{
    /// <summary>Her full name, for the console/table card.</summary>
    public const string FullName = "Solenne “Static” Marsh";

    /// <summary>Her shout-name — the deck droid tag and the marker the client detects her console by.</summary>
    public const string Nickname = "STATIC";

    /// <summary>The deck console label (the ◈ patron idiom). The client routes E on this to the oracle
    /// flow the same way it routes the Magpie, matching on <see cref="Nickname"/>.</summary>
    public const string ConsoleLabel = "◈ “STATIC” MARSH";

    /// <summary>One-line backstory for the card header — who the madwoman in the corner used to be.</summary>
    public const string Backstory =
        "A lapsed Nebula Mutual pattern-auditor who listened to the archived dead too long, let her own " +
        "policy lapse, and never got her frequency back. She perceives too much on too many channels.";

    /// <summary>Sim-seconds the oracle holds one presence state before the rota re-rolls — the same
    /// four-sim-hour watch beat the seated regulars and the Magpie use, so the room shuffles together.</summary>
    public const double WatchSeconds = 4 * 3600;

    /// <summary>How often the oracle is actually at a given bar on a watch — a bit over half. She is a
    /// corner fixture, but she roams and mutters elsewhere too (present sometimes, an empty stool other
    /// times — the #414 patron/rota idiom).</summary>
    public const double PresenceChance = 0.55;

    /// <summary>The base chance any single drawn line is a true one at zero drinks — deliberately low, so
    /// the signal is rare and the sifting is real. Most of what she says is beautiful noise.</summary>
    public const double BaseTrueChance = 0.14;

    /// <summary>How much each drink bought for the oracle raises the true chance ("a drunk oracle is more
    /// prophetic"). Additive per drink, capped at <see cref="MaxTrueChance"/>.</summary>
    public const double TrueChancePerDrink = 0.07;

    /// <summary>The ceiling on the true chance no matter how many drinks — even sodden, she is mostly
    /// noise; the truths never become a firehose.</summary>
    public const double MaxTrueChance = 0.45;

    // The tell (deliverable 4): a subtle "this one might be real" cue that is never definitive. It fires on
    // MOST true lines but also on a MINORITY of nonsense, so the player cannot lean on it — a hush that lies.
    private const double TrueQuietChance = 0.72;
    private const double NonsenseQuietChance = 0.16;

    /// <summary>The true chance at a given drink count, capped. Pure.</summary>
    public static double TrueChance(int drinksBought) =>
        System.Math.Min(MaxTrueChance, BaseTrueChance + TrueChancePerDrink * System.Math.Max(0, drinksBought));

    /// <summary>The watch index containing <paramref name="simTime"/> — the same floor-divide the patron
    /// rota uses, so presence and lines share one stable per-watch seed.</summary>
    public static long WatchIndex(double simTime) => (long)System.Math.Floor(simTime / WatchSeconds);

    /// <summary>Is the oracle at <paramref name="stationId"/> on the watch containing <paramref name="simTime"/>?
    /// Seeded and deterministic — a corner fixture some watches, a drifted-off empty stool others.</summary>
    public static bool PresentAt(string stationId, double simTime) =>
        Unit(Hash(Fold(stationId ?? string.Empty, 0x0C), (ulong)WatchIndex(simTime), 0x5A)) < PresenceChance;

    /// <summary>True if a deck patron label is the oracle's — the client's routing gate (mirrors the
    /// Magpie's name match), tolerant of the ◈ prefix and case.</summary>
    public static bool IsOracle(string? label) =>
        label is not null && label.Contains(Nickname, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// What the oracle says on one draw. Deterministic in (station, watch, draw index, drinks bought), so
    /// a given oracle's stream is stable and replayable in a test. Mostly nonsense; a rare line is true and
    /// carries a <see cref="OracleLine.Truth"/> (and, for a fragment, a real <see cref="OracleLine.FragmentId"/>).
    /// </summary>
    /// <param name="stationId">The docked bar — folds into the seed so different ports hear different visions.</param>
    /// <param name="watch">The docking watch (<see cref="WatchIndex"/>) — stable across a single visit.</param>
    /// <param name="drawIndex">Which line this is (0-based) — advances every time the captain listens.</param>
    /// <param name="drinksBought">Drinks stood the oracle this visit — raises the true chance.</param>
    public static OracleLine Speak(string stationId, long watch, int drawIndex, int drinksBought)
    {
        ulong seed = LineSeed(stationId ?? string.Empty, watch, drawIndex, drinksBought);
        bool isTrue = Unit(Mix(seed, 0x7717_0AC1_E000_0001UL)) < TrueChance(drinksBought);

        if (!isTrue)
        {
            string text = Nonsense[(int)(Mix(seed, 0x0000_5E05_E000_0003UL) % (ulong)Nonsense.Count)];
            bool nq = Unit(Mix(seed, 0x0176_5100_0000_00A1UL)) < NonsenseQuietChance;
            return new OracleLine(text, false, OracleTruthKind.None, null, nq);
        }

        OracleTruth t = Truths[(int)(Mix(seed, 0x7211_7000_0000_0002UL) % (ulong)Truths.Count)];
        bool tq = Unit(Mix(seed, 0x0176_5100_0000_00A1UL)) < TrueQuietChance;
        return new OracleLine(t.Perception, true, t.Kind, t.FragmentId, tq);
    }

    // ── The NONSENSE pool (owner's key: perception on extra channels, not gibberish). Paranoid, cosmic,
    //    insulting, funny — Hybrid cadence. Each reads as sensory overload from perceiving too much: x-rays
    //    as weather, dust/solar-wind on the tongue, orbital resonance/tides in the body, the archived dead
    //    as a chorus, gravity/magnetic fields felt directly. Deterministic pool; no braces, ever. ──
    /// <summary>The seeded nonsense pool — the beautiful noise the truths hide in. 26 lines, authored in the
    /// perception-on-extra-channels key. Exposed for the determinism/no-brace-leak tests.</summary>
    public static readonly System.Collections.Generic.IReadOnlyList<string> Nonsense =
    [
        "You're bleeding ultraviolet from the third dorsal seam of this station and not one of you dressed for it. Put a hat on. No — over the whole skull.",
        "This dock tastes of iron and a star that died before your grandmother was a rumour. Don't drink from the second tap. That one's cobalt-60 and regret.",
        "Saturn's pulling my left tooth again. It does that every eleven hours, right on the resonance. You'd feel it in your fillings, if you'd had the sense to keep any.",
        "The forty are singing the docking manifest. Off-key. They were always off-key — you try holding a note through forty winters in a cold ocean and see how you sound.",
        "There's a magnetic line running through your chair and it's fraying, spacer. I wouldn't sit so certain. Nothing here is grounded — least of all you.",
        "The coffee is a lie. Not the drink — the WORD. Someone up-well broadcasts the word 'coffee' at nine gigahertz to keep the shift docile. Sip slower and you'll hear it under your tongue.",
        "Cosmic dust on the back of my tongue tonight, vintage of the wrong supernova. You cannot buy this bouquet. You can only stand downwind of a dead sun long enough to earn it.",
        "Your shadow arrives forty milliseconds before you do. I've watched it come through that door. It's tired of waiting. One evening it won't bother.",
        "The gravity in here is secondhand. Rented. They turn it down at the end of the month and everyone just calls it 'feeling low.' I keep the receipts under my glass.",
        "Every airlock on this ring hums a different flat note, and together they spell a name. Not yours. Older. The one the dark uses for the whole station when it thinks we're asleep.",
        "There are three suns in that window and you only booked passage past one. The other two are following. They do that. Stars are lonely and you, spacer, are warm.",
        "Don't trust the port-side clock. It's counting somebody else's heartbeats to look busy. The real time is written in the tide, and the tide says: soon. It always says soon.",
        "I can taste your insurance premium from here. Sour. Lapsed-adjacent. You should be feeling that on the enamel by now — no? Give it a death or two. You'll catch up.",
        "The station breathes out helium-3 when it thinks nobody's tuned in. I'm always tuned in. It's shy about it. Don't tell it I told you.",
        "You've a second frequency running under the first one — everyone does — and yours is stuck on the same eight bars of a lullaby nobody living ever taught you.",
        "The rings aren't ice, that's the cover story. They're every message ever bounced off Saturn, frozen mid-sentence, waiting for a receiver cold enough to finish reading. I get cold enough.",
        "Somebody in the back is decaying at exactly the wrong rate and it's thrown my whole afternoon sideways. Beta particles all over the floor. Wipe your boots on the way to your grave.",
        "The barkeep's a good sort, but he's standing in a hot spot and doesn't know it. Forty rads of good cheer a shift. That's why the smile never wavers — it's cooked on.",
        "Your ship's reactor sings soprano when it's lying about its shielding. I heard it dock. It hit a high C. You should look into that. Or don't. We're all provisional out here.",
        "There's a colour past violet that only blooms when a debt's about to walk through a door, and I'm seeing rather a lot of it on you just now. Just saying. Finish your drink.",
        "Neptune's so far out its light turns up embarrassed, apologising for the delay. Four hours late and still the most honest thing in this whole room.",
        "The vending machine is praying. Machines pray in sixty-hertz hum, and that one's been at it since the last blackout. I don't judge. I harmonise. It appreciates the company.",
        "You keep your soul in your left pocket with your credit chit, and one day you'll spend the wrong one. I can see the wear on it from here. Get a second pocket, spacer. Today.",
        "This whole ring is one degree off true and leaning windward into a solar wind you can't feel. I lean with it. That is not drunk. That is ALIGNED. There is a difference, and it's important.",
        "The dead don't whisper — that's a slander. They FILE. Endless, tidy, in triplicate. You can hear the stamps if the room goes quiet enough. Listen. No. You missed it. They only stamp once.",
        "Every star you can name is already dead, and every one you can't is watching you specifically. I learned all their names to be polite. They appreciate the effort. Mostly.",
    ];

    // ── The TRUE pool (deliverable 2): rare lines that "sound nuts but are TRUE." Each is phrased as a
    //    PERCEPTION on a channel the sane can't hear — that IS why it's true. Three flavours: a hidden
    //    secret-lab tell (#409), a collector warning, and actual arc FRAGMENTS (#411/#422) that the client
    //    assembles by id. The fragment perceptions are original leaks; the client appends the canonical
    //    fragment Lore so the shard reads whole. All fragment ids are REAL pool ids (a test pins that). ──
    /// <summary>One true-line and what it points at. For a fragment kind, <see cref="FragmentId"/> is a real
    /// <see cref="KaamosLore"/>/<see cref="NebulaLore"/> id the caller assembles.</summary>
    public readonly record struct OracleTruth(OracleTruthKind Kind, string? FragmentId, string Perception);

    /// <summary>The seeded true-line pool. Uniformly indexed by <see cref="Speak"/> when a line comes up
    /// true — so every flavour, including the arc fragments, is reachable. Exposed for the tests.</summary>
    public static readonly System.Collections.Generic.IReadOnlyList<OracleTruth> Truths =
    [
        // A hidden secret-lab tell (#409) — she perceives a room the radiation avoids. A lead to walk out.
        new(OracleTruthKind.SecretLab, null,
            "There's a room out on one of the cold moons that doesn't answer radiation the way a room should — it drinks the counts and gives nothing back. Sealed. Someone poured a door over it and painted regolith on the seam. Go dig where the ground goes quiet."),
        new(OracleTruthKind.SecretLab, null,
            "Under one of the ice-fields there's a box humming at a frequency the living don't use — a lab with the lights off and something still filed inside. Walk the dead squares with your detector; the door's where your counter forgets how to count."),

        // A collector warning — she reads the "colour past violet" of a recovery writ. True in spirit,
        // and literally so when heat is up. Settle it before it walks in.
        new(OracleTruthKind.Collector, null,
            "That colour past violet is pooling thick on you tonight, spacer. A writ's warming up with your policy number on the asset line. The collectors don't run — they don't have to. Settle your heat before it comes through that door wearing your face's welcome."),
        new(OracleTruthKind.Collector, null,
            "I taste iron and interest on the air, and it's cut with your name. Somebody's filed a recovery order. Fly quiet a while — the ones they send to collect aren't chasing the cargo. They never were."),

        // Actual PROJEKTI KAAMOS shards (#411) — real ids the client hands to KaamosProgress.Assemble.
        new(OracleTruthKind.KaamosFragment, "vantar-log",
            "There's a hand still writing in the cold under the ice, long after the man who owned it was filed dead — I can hear the nib scratch on the down-tick of every hour. He taught them not to sleep through the dark. To hold it. Together, as one held breath."),
        new(OracleTruthKind.KaamosFragment, "holders-tell",
            "The berth nobody files for is calling its manifest in again — I feel it in my back teeth every window, right on the tick. Same forty names, read out clean. I stopped being able to tell which of them was still doing the reading. You don't file for that berth. You keep it."),
        new(OracleTruthKind.KaamosFragment, "cold-pod",
            "There's a pod going forty-souls-cold in the regolith and never once opened — I can taste the frost on its unbroken seals from here. Consumables. A wintering crew. Held, not lost. Somebody chose not to send it, and the ice moon went hungry for the choice."),

        // Actual NEBULA MUTUAL shards (#422) — real ids the client hands to NebulaProgress.Assemble.
        new(OracleTruthKind.NebulaFragment, "rebirth-glitch",
            "You woke up wrong once and didn't notice — I saw the seam flicker before the welcome looped clean. RESTORE FROM PATTERN, it read. SUBSCRIBER LUCID. DO NOT REVIVE ORIGINAL. And you felt entirely, warmly yourself. That's the part they charge you for."),
        new(OracleTruthKind.NebulaFragment, "adjuster-tell",
            "I audited for them, you know, before the frequency took me. I filed the same subscriber six times — different faces, one number — and every one of them shook my hand certain they were the first. A policy isn't a spare life, spacer. It's a lease on the one you've already got."),
        new(OracleTruthKind.NebulaFragment, "clinic-ledger",
            "Your policy number's older than you are — I can read the wear on it across the counter. Entries you never made. A column marked LUCIDITY going faint on dates that predate your first death. The archive keeps the tab open. It never once closes a policy."),
    ];

    // ── Pure hashing (splitmix64), self-contained so this file depends on no other Core RNG. Same platform-
    //    stable idiom the patron rota and the Reever idle use — no System.Random, no clock. ──

    private static ulong LineSeed(string stationId, long watch, int drawIndex, int drinksBought)
    {
        ulong h = Fold(stationId, 0x0C);
        h = Mix(h, (ulong)watch);
        h = Mix(h, unchecked((ulong)drawIndex) + 0x1000UL);
        h = Mix(h, unchecked((ulong)drinksBought) + 0x2000UL);
        return h;
    }

    private static ulong Fold(string s, ulong salt)
    {
        ulong h = salt;
        foreach (char c in s)
        {
            h = SplitMix64(h + c);
        }
        return h;
    }

    private static ulong Hash(ulong seed, ulong stream, ulong salt) =>
        SplitMix64(SplitMix64(seed ^ (salt * 0x9E3779B97F4A7C15UL)) + stream);

    private static ulong Mix(ulong a, ulong b)
    {
        ulong z = a ^ b;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static ulong SplitMix64(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static double Unit(ulong h) => (h >> 11) * (1.0 / (1UL << 53));
}
