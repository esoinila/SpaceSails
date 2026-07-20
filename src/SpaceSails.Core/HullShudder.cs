namespace SpaceSails.Core;

/// <summary>
/// HULL-SHUDDER · the ambient-dread mood-setter (owner, at sea in rough weather 2026-07-20): <b>"The ship
/// sometimes shakes. Everybody pauses here at dinner when we probably hit a rough wave, but there is this
/// small moment of 'what was that sound and vibration and flex of the steel body.' As if joint, in unison,
/// people then decide.. probably just a wave."</b>
///
/// <para><b>The magic is the UNISON</b> — not each patron reacting on their own, but every head in the room
/// coming up at the SAME instant, a held breath, then — wordlessly, together — everyone deciding it was
/// nothing and resuming as one. This is a mood-setter, NOT a threat mechanic: mostly it IS nothing. On an
/// interior deck (a haven bar/hall, the ship, a lab/secret-lab/surface site) an occasional seeded tremor
/// fires; the client shakes the deck a little, freezes every present NPC's idle jitter for a shared beat,
/// turns their heads up together, then lets them resume — and speaks a house-voice line carrying that
/// unison-decide beat. The dread is the PAUSE, not damage.</para>
///
/// <para>Pure and fully deterministic from a seed and a monotonic shudder index — the same seeded-cadence
/// idiom as <see cref="ReeverTide"/> and <see cref="CommsLink"/>, salted off the ONE shared
/// <see cref="DiceRule"/> engine (never <see cref="System.Random"/> or the clock — determinism is law in
/// Core). Given a seed and the next index it answers "how long until the next shudder", "which house-voice
/// line speaks it" (context-flavored), and "does this one carry a chill" (the bounded story-site
/// escalation). The shudder's SHAPE — a brief decaying deck-shake and a held unison pause — is a pure,
/// bounded curve (the <see cref="ReeverIdle"/> oscillation idiom under a decay envelope), so the whole
/// thing pins in a test. The live accumulator, the render offset and the NPC freeze are the client's thin
/// real-time layer.</para>
/// </summary>
public static class HullShudder
{
    /// <summary>Where the captain is standing when a shudder rolls — the flavor the house voice takes.</summary>
    public enum Setting
    {
        /// <summary>A docked haven's bar/concourse — the reassuring read: the clamps, the station settling.</summary>
        Haven = 0,
        /// <summary>Aboard the ship's own deck — the old girl talking to herself, thermal creaks.</summary>
        Ship = 1,
        /// <summary>A lab / secret-lab / surface site — colder, unexplained: is it a wave, or something below?</summary>
        DeepSite = 2,
    }

    // ── Onset cadence (rare-ish): how long the calm holds between shudders. ───────────────────────────

    /// <summary>The mean quiet gap between shudders (real seconds on the deck). Generous — a shudder is an
    /// occasional mood-setter, not a drumbeat; long enough that a short stop may never feel one, short
    /// enough that a lingering watch at a bar or a site eventually gets the held breath. FLAGGED for the
    /// owner's tuning.</summary>
    public const double MeanGapSeconds = 78.0;

    /// <summary>How far a single gap jitters off the mean, as a fraction — each gap lands in
    /// <c>Mean × [1 − Jitter, 1 + Jitter]</c>, deterministic per (seed, index). "As if at random" (owner)
    /// without a fixed pulse — but never <see cref="System.Random"/>, so a test replays the exact schedule.</summary>
    public const double GapJitterFraction = 0.55;

    /// <summary>A hard floor on any gap (real seconds) so the jitter's low tail can never chain two
    /// shudders into a nauseating stutter — the room must fully settle back before it can be broken again.</summary>
    public const double MinGapSeconds = 34.0;

    // ── The shudder's SHAPE: a brief decaying deck-shake, then a held unison pause. ───────────────────

    /// <summary>How long the deck-shake lasts (seconds) before it has fully decayed back to rest. Short —
    /// the flex of the steel body, not an earthquake. The offset is guaranteed zero at and beyond this.</summary>
    public const double ShakeDurationSeconds = 0.85;

    /// <summary>The shake's e-folding decay time (seconds): the tremor is sharpest at the first instant and
    /// dies away, so it never reads as a sustained rumble. Paired with <see cref="ShakeDurationSeconds"/>'s
    /// linear taper so the curve reaches exactly zero at the end, not merely small.</summary>
    public const double ShakeDecayTau = 0.26;

    /// <summary>The peak per-axis shake amplitude, UNITLESS and bounded to [−1, 1]. The client scales this
    /// to a small pixel offset applied as a transient RENDER pan (never a move of any entity anchor), so a
    /// subtle deck-shake reads without ever touching the world. Each axis is bounded by this at t = 0 and
    /// decays from there; the whole thing is deliberately gentle (never nauseating — owner's constraint).</summary>
    public const double ShakeAmplitude = 1.0;

    // Two incommensurate tremor rates (rad/s ≈ 6.0 Hz and 9.6 Hz) at seeded phases — the ReeverIdle idiom,
    // so the shake is a rough shiver, not a clean metronome. Their weights sum to 1 so the amplitude bound
    // above holds exactly.
    private const double ShakeRate1RadPerSec = 37.7;   // ~6.0 Hz
    private const double ShakeRate2RadPerSec = 60.3;   // ~9.6 Hz, incommensurate with the first
    private const double ShakeWeight1 = 0.6;
    private const double ShakeWeight2 = 0.4;

    /// <summary>How long every present NPC holds the shared breath — heads up, idle jitter frozen — before
    /// the room resumes as one (seconds). A touch longer than the shake, so the STILLNESS outlasts the
    /// tremor: the pause is the feature, the shake only announces it. The client freezes all NPCs on the
    /// SAME onset timestamp, so the hold is exactly synchronized — that is the whole point.</summary>
    public const double PauseDurationSeconds = 1.15;

    // ── The story-site escalation (bounded): a chill that doesn't quite land as "just a wave". ────────

    /// <summary>At a secret lab, or once a story arc is deep, this fraction of shudders carry a CHILL — the
    /// together-decide never quite comes. Kept modest: mostly a deep-site shudder is still nothing (the
    /// dread is the pause, not damage). Only ever consulted at an eligible site; a haven/ship shudder never
    /// chills. FLAGGED for the owner's tuning.</summary>
    public const double ChillChance = 0.4;

    /// <summary>The tiny nerve tick a chill deals (via <see cref="NerveModel.Shock"/>) — a nerve prickle,
    /// far smaller than a Reever's touch or the monolith: the room's unease, made a hair real. FLAGGED.</summary>
    public const double ChillNerveTick = 4.0;

    // The fraction resolution: a large-faced die off the shared rule gives a smooth [0,1) sample while
    // staying every bit as platform-stable and replayable as the dice engine itself.
    private const int Resolution = 4096;

    // ── The line pools (house voice, the unison-decide beat), one per setting. ────────────────────────
    //
    // Every line carries the same shape the owner named: the flex/groan, every head coming up AS ONE, a
    // held breath, then — wordlessly, TOGETHER — the room deciding it was nothing and resuming. A haven
    // blames the clamps / the station settling; the ship blames herself; a deep site can't name what
    // settles this far down, and only almost believes the verdict.

    private static readonly string[] HavenLines =
    [
        "The deck flexes underfoot with a long steel groan. For a breath every head at the bar comes up as one — then, together, everyone decides it was the clamps settling, and the glasses go back down.",
        "A shudder walks through the concourse and every conversation stops mid-word. A held beat, eyes meeting eyes across the room — then, as one, everybody agrees it was just the station shifting on its moorings, and the noise floods back.",
        "Somewhere deep in the hull a joint takes up the slack with a boom you feel in your teeth. Heads lift all around, still as held breath — then, wordlessly and together, the bar decides it was nothing, and goes back to its drinks.",
    ];

    private static readonly string[] ShipLines =
    [
        "The hull flexes with a long steel groan and every head aboard comes up at once. A shared, silent beat — then, together, you all decide it was just the ship talking to herself, and you get back to it.",
        "A shudder runs the length of the deck. For one held breath everyone looks up as one — then, wordlessly, the crew decides it was nothing, the old girl only stretching, and the moment lets go.",
        "Something in the frame settles with a boom that stops every hand mid-task. Eyes meet across the deck, holding — then, as one, everyone shrugs it off as the hull breathing, and carries on.",
    ];

    private static readonly string[] DeepSiteLines =
    [
        "The ground shudders and a long groan rolls up through the soles of your boots. Every head in the place comes up as one, holding — then, together, you all decide it was nothing. You almost believe it.",
        "A tremor runs through the rock and the steel laid over it, and for a breath nobody moves. Heads lift in unison, listening — then, as one, everyone agrees it was just settling. No one says what settles this deep.",
        "Something flexes the hull with a slow steel moan, and the whole room freezes as one. A held beat, eyes wide and meeting — then, wordlessly and together, everyone decides it was nothing, and pretends the pause never happened.",
    ];

    // The chill pool (deep-site escalation): the same shape, but the together-decide never quite lands.
    private static readonly string[] ChillLines =
    [
        "The deck flexes with a groan that goes on a half-second too long. Heads come up as one — and this time nobody quite lets the breath back out. It didn't sound like settling. Nobody says so.",
        "A shudder rolls through the floor and every head lifts at once — but the together-decide never comes. You all just… listen. Whatever that was, it wasn't a wave.",
        "The hull moans low and long, and the room freezes as one. The held breath stretches, and stretches — and this time no one decides it was nothing.",
    ];

    /// <summary>The house-voice line pool for a <paramref name="setting"/> — exposed so a test can pin that
    /// every line is non-blank and the pool holds no duplicates. The chill pool
    /// (<see cref="ChillLine"/>) is separate; this is the ordinary "it was nothing" voice.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> LinesFor(Setting setting) => setting switch
    {
        Setting.Haven => HavenLines,
        Setting.Ship => ShipLines,
        _ => DeepSiteLines,
    };

    /// <summary>The chill-line pool (the bounded deep-site escalation) — exposed for the same non-blank /
    /// unique pinning as the ordinary pools.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> ChillLines_ => ChillLines;

    // ── The cadence. ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Seconds of calm until the <paramref name="shudderIndex"/>-th shudder (0-based), jittered
    /// around <see cref="MeanGapSeconds"/> and floored at <see cref="MinGapSeconds"/>. Pure and
    /// deterministic in <paramref name="seed"/> — the same deck replays the same rhythm.</summary>
    public static double NextGap(ulong seed, int shudderIndex)
    {
        double u = Fraction(seed, $"shudder-gap:{shudderIndex}");            // [0,1)
        double gap = MeanGapSeconds * ((1.0 - GapJitterFraction) + (2.0 * GapJitterFraction * u));
        return System.Math.Max(MinGapSeconds, gap);
    }

    // ── Context → setting. ────────────────────────────────────────────────────────────────────────────

    /// <summary>Which house voice speaks a shudder given the captain's context. A <paramref name="deepSite"/>
    /// (a surface/lab/secret-lab excursion) reads coldest and WINS — even at a docked haven, a landing is a
    /// landing; otherwise a docked haven speaks the reassuring clamps/settling voice; failing both, it's the
    /// ship's own deck. Pure, so the selection pins in a test.</summary>
    public static Setting SettingFor(bool deepSite, bool haven) =>
        deepSite ? Setting.DeepSite : haven ? Setting.Haven : Setting.Ship;

    // ── Line selection. ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The ordinary house-voice line for a shudder — deterministically drawn from the
    /// <paramref name="setting"/>'s pool per (seed, index), so the same shudder always speaks the same words
    /// and consecutive shudders rotate the pool rather than repeating.</summary>
    public static string Line(Setting setting, ulong seed, int shudderIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = LinesFor(setting);
        return pool[Index(seed, $"shudder-line:{shudderIndex}", pool.Count)];
    }

    /// <summary>The CHILL line for an escalated deep-site shudder — the one that doesn't land as "just a
    /// wave". Deterministically drawn from the chill pool per (seed, index), salted apart from the ordinary
    /// line stream so the two never lock together.</summary>
    public static string ChillLine(ulong seed, int shudderIndex) =>
        ChillLines[Index(seed, $"shudder-chill-line:{shudderIndex}", ChillLines.Length)];

    // ── The escalation gate. ──────────────────────────────────────────────────────────────────────────

    /// <summary>Does the <paramref name="shudderIndex"/>-th shudder carry a CHILL? Deterministic per (seed,
    /// index), salted apart from the gap and line streams. The caller only consults this at an eligible site
    /// (a secret lab, or an arc gone deep); a haven/ship shudder never chills. Fires
    /// <see cref="ChillChance"/> of the time — mostly even a deep-site shudder is still nothing.</summary>
    public static bool CarriesChill(ulong seed, int shudderIndex) =>
        Fraction(seed, $"shudder-chill:{shudderIndex}") < ChillChance;

    // ── The shudder's shape. ──────────────────────────────────────────────────────────────────────────

    /// <summary>The decay envelope of the deck-shake at <paramref name="tSinceOnset"/> seconds after the
    /// tremor began: 1 at the first instant, easing to exactly 0 at <see cref="ShakeDurationSeconds"/>.
    /// An exponential decay times a linear taper — sharp at the start, fully settled by the end, so the
    /// shake can never linger into a sustained (nauseating) rumble. In [0, 1].</summary>
    public static double ShakeEnvelope(double tSinceOnset)
    {
        if (tSinceOnset <= 0.0)
        {
            return tSinceOnset < 0.0 ? 0.0 : 1.0;
        }
        if (tSinceOnset >= ShakeDurationSeconds)
        {
            return 0.0;
        }
        double taper = 1.0 - (tSinceOnset / ShakeDurationSeconds);   // hits 0 at the end
        return System.Math.Exp(-tSinceOnset / ShakeDecayTau) * taper;
    }

    /// <summary>The seeded deck-shake OFFSET at <paramref name="tSinceOnset"/> seconds after onset — a
    /// bounded, decaying 2-D jitter (the <see cref="ReeverIdle"/> incommensurate-sinusoid idiom under the
    /// <see cref="ShakeEnvelope"/>). The two axes carry independent seeded phases so the shake is a shiver,
    /// not a diagonal slide. Each axis is bounded by <see cref="ShakeAmplitude"/> × the envelope, so it is
    /// always in [−1, 1] and is exactly (0, 0) at and beyond <see cref="ShakeDurationSeconds"/> (and before
    /// onset). The client multiplies this by a small pixel amplitude and adds it to the render pan — a pure
    /// visual offset that never moves an entity anchor.</summary>
    public static (double Dx, double Dy) ShakeOffset(ulong seed, double tSinceOnset)
    {
        double env = ShakeEnvelope(tSinceOnset);
        if (env <= 0.0)
        {
            return (0.0, 0.0);
        }
        double dx = env * Axis(seed, tSinceOnset, 0x51, 0x52);
        double dy = env * Axis(seed, tSinceOnset, 0x61, 0x62);
        return (dx, dy);
    }

    /// <summary>Is the unison pause still held at <paramref name="tSinceOnset"/> seconds after onset? The
    /// window all present NPCs freeze (heads up, idle jitter stopped) on the shared onset timestamp — the
    /// synchronized held breath — before resuming as one.</summary>
    public static bool Pausing(double tSinceOnset) =>
        tSinceOnset >= 0.0 && tSinceOnset < PauseDurationSeconds;

    // One shake axis: a weighted sum of two incommensurate sinusoids at seeded phases, scaled to the unit
    // amplitude. The weights sum to 1, so |value| ≤ ShakeAmplitude before the envelope multiplies it.
    private static double Axis(ulong seed, double t, ulong saltA, ulong saltB)
    {
        double pa = Phase(seed, saltA);
        double pb = Phase(seed, saltB);
        double s = (ShakeWeight1 * System.Math.Sin((ShakeRate1RadPerSec * t) + pa))
                 + (ShakeWeight2 * System.Math.Sin((ShakeRate2RadPerSec * t) + pb));
        return ShakeAmplitude * s;
    }

    // A deterministic phase in [0, 2π) from a seed and a salt — a splitmix64 finalizer, pure and platform
    // stable (no System.Random, no clock). Distinct salts give the independent per-axis phases.
    private static double Phase(ulong seed, ulong salt)
    {
        ulong z = seed + (salt * 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return z / (double)ulong.MaxValue * System.Math.Tau;
    }

    // A deterministic index into a pool of the given size, salted by the purpose tag off the shared rule.
    private static int Index(ulong seed, string tag, int count) =>
        count <= 1 ? 0 : (int)(Fraction(seed, tag) * count) % count;

    // ══ THE UNEXPLAINED SIGNAL · a sibling ambient event (owner, 2026-07-20) ═════════════════════════════
    //
    //   "Amongst the rough seas and dinner noise of clattering steel utensils on ceramic plates there was a
    //    distant buzzer that made the crew pause a bit and look at each other. Then they continued, I wonder
    //    if they knew what it was."
    //
    // The shudder's colder sibling. Where a hull-shudder is a PHYSICAL tremor the whole room rationalizes as
    // "probably a wave", the SIGNAL is a faint distant buzzer/tone off-deck that NOBODY explains — and it is
    // the STAFF who react, not the drinking patrons: the barkeep and the station-crew go still for a beat and
    // catch each other's eye (a synchronized glance, one shared timestamp), then resume, saying nothing. The
    // unresolved-ness IS the dread. No shake — only the tone (a short cue if the renderer has one) and the
    // glance. Rarer than the shudder; once a story arc has gone deep the glance holds a beat too long (the
    // COLD pool). Same seeded-cadence idiom, same synchronized-unison mechanic — the client is the thin layer.

    /// <summary>The mean quiet gap between unexplained signals (real deck-seconds) — RARER than a shudder
    /// (<see cref="MeanGapSeconds"/>): the buzzer is the once-a-long-while off-deck note, not a regular
    /// event. FLAGGED for the owner's tuning.</summary>
    public const double SignalMeanGapSeconds = 165.0;

    /// <summary>How far a signal gap jitters off the mean, as a fraction (same jitter law as the shudder).</summary>
    public const double SignalGapJitterFraction = 0.55;

    /// <summary>A hard floor on any signal gap (real seconds) — comfortably above the shudder's floor, so a
    /// signal never crowds the room's ambient rhythm.</summary>
    public const double SignalMinGapSeconds = 74.0;

    /// <summary>How long the staff hold the shared glance (seconds) before they resume, saying nothing. The
    /// client freezes/refaces all present STAFF on the SAME onset timestamp, so the look is exactly
    /// synchronized — the crew catch each other's eye as one.</summary>
    public const double GlanceDurationSeconds = 1.05;

    /// <summary>The COLD glance (the story-deep escalation): the crew's eyes lock a beat too long. Strictly
    /// longer than the ordinary glance — the held look IS the extra dread.</summary>
    public const double ColdGlanceDurationSeconds = 1.7;

    // The signal's line pools (house voice): the buzzer sounds off-deck, the STAFF trade a single glance and
    // resume, wordless — the patrons never look up. The COLD pool is the same beat, but the glance lingers,
    // and the crew have plainly heard it before.
    private static readonly string[] SignalLines =
    [
        "Somewhere off the deck a buzzer sounds, twice, and stops. The barkeep and the dock-hand catch each other's eye for half a second — then go back to work, and say nothing about it.",
        "A distant tone climbs somewhere past the bulkheads, holds, and cuts out. Behind the counter the staff go still as one and trade a single glance — then, wordlessly, they carry on. The drinkers never look up.",
        "Far off, a klaxon coughs once and dies. The crew's eyes meet across the room, just for a beat — then the glasses get wiped again, and no one explains what it was.",
    ];

    private static readonly string[] SignalColdLines =
    [
        "Somewhere off the deck a buzzer sounds, twice, and stops. The barkeep and the dock-hand catch each other's eye — and hold it a beat too long. Then they look away, and neither says the thing they're both thinking.",
        "A tone climbs past the bulkheads and doesn't quite stop when it should. The staff go still and trade a look that lingers — then, slowly, they go back to work. Whatever it means, they've heard it before.",
        "Far off, a klaxon sounds a pattern — not random, a pattern. The crew's eyes lock a second too long, and something passes between them. They resume. They do not tell you.",
    ];

    /// <summary>The unexplained-signal line pool — <paramref name="cold"/> for the story-deep escalation
    /// (the lingering glance), else the ordinary "back to work, saying nothing" voice. Exposed so a test can
    /// pin every line non-blank and the pool free of duplicates.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> SignalLinesFor(bool cold) =>
        cold ? SignalColdLines : SignalLines;

    /// <summary>Seconds of calm until the <paramref name="signalIndex"/>-th unexplained signal (0-based),
    /// jittered around <see cref="SignalMeanGapSeconds"/> and floored at <see cref="SignalMinGapSeconds"/>.
    /// Pure and deterministic in <paramref name="seed"/>, salted apart from the shudder's own gap stream so
    /// the two ambient rhythms never lock together.</summary>
    public static double SignalNextGap(ulong seed, int signalIndex)
    {
        double u = Fraction(seed, $"signal-gap:{signalIndex}");             // [0,1)
        double gap = SignalMeanGapSeconds * ((1.0 - SignalGapJitterFraction) + (2.0 * SignalGapJitterFraction * u));
        return System.Math.Max(SignalMinGapSeconds, gap);
    }

    /// <summary>The house-voice line for an unexplained signal — deterministically drawn from the ordinary
    /// or <paramref name="cold"/> pool per (seed, index), salted apart from every shudder stream.</summary>
    public static string SignalLine(bool cold, ulong seed, int signalIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = SignalLinesFor(cold);
        return pool[Index(seed, $"signal-line:{(cold ? "cold" : "warm")}:{signalIndex}", pool.Count)];
    }

    /// <summary>Is the staff glance still held at <paramref name="tSinceOnset"/> seconds after the buzzer?
    /// The <paramref name="cold"/> glance lingers a beat too long (<see cref="ColdGlanceDurationSeconds"/>)
    /// where the ordinary one has already let go (<see cref="GlanceDurationSeconds"/>).</summary>
    public static bool Glancing(double tSinceOnset, bool cold) =>
        tSinceOnset >= 0.0 && tSinceOnset < (cold ? ColdGlanceDurationSeconds : GlanceDurationSeconds);

    // ══ THE CAUTION ANNOUNCEMENT · a third sibling ambient event (owner, 2026-07-20, mid-storm) ═══════════
    //
    //   "Now there was an announcement encouraging to move cautiously on the ship."
    //
    // When the passage turns rough — a RUN of hull-shudders close together (the storm) — a station/ship PA
    // fires: the house voice over the deck asking all hands to move deliberately, because the deck isn't
    // itself tonight. Calm-but-not-quite-reassuring: the parenthetical undercut is the mood. No shake — a
    // pulse line, and a hair of nerve nuance (the reassurance steadies the hands a touch; at a deep/story
    // site the same words fray them instead). At deep/lab/story sites the announcement lands colder ("keep
    // one hand for the ship, and the other for yourself"). Fired by the client off a run of shudders; the
    // pool, the selection and the rough-patch threshold are the pure Core spine.

    /// <summary>How many hull-shudders in a row (before the run lapses) make a "rough patch" the caution PA
    /// answers — the storm the announcement is for. Two is a run; one is just an ambient beat. The client
    /// tracks the running count and its lapse; this is the threshold it compares against.</summary>
    public const int RoughPatchShudderRun = 2;

    /// <summary>The tiny nerve STEADYING an ordinary caution PA lands (the reassurance, a hair of recovery)
    /// — smaller than any drink; the house voice saying "this is routine" does steady the hands a little.
    /// FLAGGED for the owner's tuning.</summary>
    public const double CautionSteadyTick = 2.0;

    /// <summary>The tiny nerve FRAYING a COLD caution PA lands instead (a deep/story site, where "this is
    /// routine" plainly isn't) — a prickle, smaller than a shudder's chill. Applied via
    /// <see cref="NerveModel.Shock"/>. FLAGGED for the owner's tuning.</summary>
    public const double CautionColdTick = 3.0;

    // The caution PA's line pools (house voice): all-hands, the passage is rough, move deliberately — then
    // the parenthetical undercut that is the whole mood. The COLD pool is colder: out here, no one believes
    // "routine", and the advisory names the thing it's pretending isn't there.
    private static readonly string[] CautionLines =
    [
        "📢 ATTENTION ALL HANDS — the passage is rough tonight. Move deliberately, mind your footing, keep one hand for the ship. This is routine. (It is usually routine.)",
        "📢 ALL HANDS, this is the deck watch: heavy weather on the hull. Take it slow, watch your step, hold the rail on the stairs. Nothing to worry about. (Nothing we can name.)",
        "📢 NOTICE TO ALL DECKS — she's shipping some rough passage. Walk, don't hurry; one hand for yourself, one for the ship. Standard precaution. (Standard, mostly.)",
    ];

    private static readonly string[] CautionColdLines =
    [
        "📢 ATTENTION ALL HANDS — move carefully. Keep one hand for the ship, and the other for yourself. This is routine. (No one out here believes that.)",
        "📢 ALL DECKS: mind your footing tonight, and mind the quiet between the shudders. Hold the rail. Do not go below alone. Routine precaution. (It has never once been routine, this far out.)",
        "📢 NOTICE — the deck is not itself tonight. Move slow, stay in pairs, and if you hear it again, do not go looking. Standard advisory. (Standard, for a place that shouldn't exist.)",
    ];

    /// <summary>The caution-PA line pool — <paramref name="cold"/> for the deep/lab/story escalation, else
    /// the ordinary rough-passage advisory. Exposed so a test can pin every line non-blank, the pool free of
    /// duplicates, and the parenthetical undercut present (the mood).</summary>
    public static System.Collections.Generic.IReadOnlyList<string> CautionLinesFor(bool cold) =>
        cold ? CautionColdLines : CautionLines;

    /// <summary>The house-voice caution PA for a rough patch — deterministically drawn from the ordinary or
    /// <paramref name="cold"/> pool per (seed, index), salted apart from every shudder/signal stream.</summary>
    public static string CautionLine(bool cold, ulong seed, int cautionIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = CautionLinesFor(cold);
        return pool[Index(seed, $"caution-line:{(cold ? "cold" : "warm")}:{cautionIndex}", pool.Count)];
    }

    // A uniform [0,1) sample: one large-faced die off the shared rule, salted by the purpose tag so the
    // gap, line and chill streams are independent.
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }
}
