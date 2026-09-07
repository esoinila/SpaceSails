namespace SpaceSails.Core;

/// <summary>
/// #316 law 1 (second half) and law 4 · <b>A RIVAL VISIT LEAVES THE SAME EVIDENCE YOURS DOES.</b>
///
/// <para>Owner, live 2026-07-18: <i>"If we find already shot Reevers at a site then we know that somebody
/// else has been there to hide, pick-up, search etc :-D It serves as a clue."</i> #1105 shipped the half of
/// that a captain writes himself — his own downed Old Ones persist on the ground with where and when — and
/// #1127 gave them the three age lines that make the writing legible. What was still missing is the half the
/// clue is actually ABOUT: <b>somebody else</b>.</para>
///
/// <h3>What this is, and what it is emphatically not</h3>
/// <para>It is <b>not a generator</b>. Nothing here rolls a die to decide whether a rival came: the watchdog
/// economy already decided that, days ago, behind the captain's back, with the seeded discovery roll
/// (<see cref="DiscoveryRule"/>) that read the hiding place through the one oracle
/// (<see cref="CacheSafety"/>). This turns THAT settled event into the marks it must have left — law 4, the
/// determinism law, in one sentence: <i>remains derive from the same seeded events that already drive rival
/// discovery — no new randomness, just the existing rolls becoming visible in the world.</i></para>
///
/// <para>So there is exactly one thing here that is not already decided, and it is not a new question: <b>how
/// many watchdogs the rivals had to go through</b>. That is the 2D6 the captain's own dig rolls
/// (<see cref="ReeverRaid"/>), with the stash's standing watchdog level as its modifier, thrown on the seed
/// of the chest and the day it was taken. A rival digging that ground answers the same table the captain
/// answers, because it IS the same ground — and every Old One that turned out went down, because the chest
/// is gone and the rivals are not.</para>
///
/// <h3>The three marks</h3>
/// <list type="bullet">
/// <item><b>Husks</b> — one per Old One the pack turned out, scattered inside a gun's reach of the hole.
/// They read on return with the ages the ground already speaks (<see cref="GroundMemory.AgeLine(double,
/// double)"/>), so the captain can DATE the visit.</item>
/// <item><b>A pit</b> — the disturbed ground where his ✗ was. Always: the chest left somehow.</item>
/// <item><b>A dry bot</b> — only in the dire case, the full pack (<see cref="ReeverRaid.MaxReevers"/>).
/// Nobody carries a sentry out to a hole they expect to be quiet, and nobody leaves one standing unless
/// leaving was cheaper than the walk back to it.</item>
/// </list>
///
/// <h3>The moment</h3>
/// <para>Everything is stamped with the DAY THE ROLL LANDED, never "now". A warp that skips a fortnight
/// resolves the discovery on the day it actually happened (<see cref="DiscoveryRule.DiscoveredWithin(
/// TreasureCache, long, double, int)"/> hands that day back), so a captain who comes home to a robbed cache
/// reads dust that is honestly a fortnight old rather than a fresh scene that lies about when he was
/// beaten to it.</para>
/// </summary>
public static class RivalVisit
{
    /// <summary>Everything one resolved discovery left on the ground. Pure data — the caller writes it to
    /// <see cref="GroundMemory"/> and the vault carries it like any other mark.</summary>
    /// <param name="Roll">The 2D6 the rivals answered on this ground — kept so a lab or a guard can show
    /// its working rather than re-derive it (two reporters of one truth is how they drift).</param>
    /// <param name="Husks">The Old Ones that went down, one per rouser.</param>
    /// <param name="Pit">The robbed hole at the ✗.</param>
    /// <param name="DryBot">The sentry they left, or null — the dire case only.</param>
    /// <param name="AtSimTime">The sim moment all of it is stamped with: the start of the day the roll
    /// landed.</param>
    public readonly record struct Evidence(
        ReeverRoll Roll,
        IReadOnlyList<GroundMemory.Husk> Husks,
        GroundMemory.Scar Pit,
        GroundMemory.Scar? DryBot,
        double AtSimTime)
    {
        /// <summary>Every scar in one list — the pit, and the bot when there is one. What a writer walks.</summary>
        public IReadOnlyList<GroundMemory.Scar> Scars =>
            DryBot is { } bot ? [Pit, bot] : [Pit];
    }

    /// <summary>The sim moment a discovery period BEGAN — what every mark of that visit is stamped with.
    /// The period is whole days since the epoch (<see cref="DiscoveryRule.PeriodIndex"/>), so this is its
    /// own inverse and the age a captain reads is the age the world actually has.</summary>
    public static double MomentOf(long periodIndex) => periodIndex * DiscoveryRule.PeriodSeconds;

    /// <summary>How far from the hole the fight spread: a sentry's own engagement arc
    /// (<see cref="SentryBot.RangeDeckUnits"/>). Not a tuned literal — it is the distance at which anything
    /// in this game shoots at anything else, so the husks lie where a firefight over that hole would put
    /// them.</summary>
    public static double SpreadDu => SentryBot.RangeDeckUnits;

    /// <summary>
    /// THE WATCHDOGS THE RIVALS MET — the captain's own 2D6, thrown for somebody else's dig.
    ///
    /// <para>Same table, same modifier (the stash's standing watchdog level, which is what makes haunted
    /// ground the best vault), a seed folded from the chest and the day. Deterministic forever: replay the
    /// same discovery and the same faces come up, which is what makes the husk count a FACT about the world
    /// rather than a decoration that changes every time it is drawn.</para>
    /// </summary>
    public static ReeverRoll WatchdogsMet(string cacheId, int reeverLevel, long periodIndex)
    {
        ArgumentNullException.ThrowIfNull(cacheId);
        return ReeverRaid.Roll(DiceRule.Seed($"rival-search:{cacheId}", periodIndex), reeverLevel);
    }

    /// <summary>The husk count one resolved discovery leaves — the pack that turned out on the rivals, to
    /// the body. There is no typed number anywhere in this file: it is
    /// <see cref="ReeverRaid.ReeversFor(int)"/> off the roll above, which is the same ladder that decides how
    /// many Old Ones the captain has to sprint past. A quiet ground (6 or under) leaves NONE, and that is not
    /// a missing feature — it is the clue reading "they walked in and out and nothing woke up".</summary>
    public static int HusksLeftBy(ReeverRoll roll) => roll.Reevers;

    /// <summary>Did the rivals lose a sentry here? Only on the full pack — the issue's own "in dire cases".
    /// A crew that walked into six Old Ones over one hole did not stroll back for their hardware.</summary>
    public static bool LeftASentryBehind(ReeverRoll roll) => roll.Reevers >= ReeverRaid.MaxReevers;

    /// <summary>
    /// <b>WHAT THE GROUND CARRIES AFTER ONE RESOLVED DISCOVERY.</b> Pure and total: same chest, same spot,
    /// same period → the same marks in the same order, for ever.
    /// </summary>
    /// <param name="cacheId">The chest that was taken — the seed half of the roll.</param>
    /// <param name="reeverLevel">The stash's standing watchdog level: the modifier on the rivals' 2D6.</param>
    /// <param name="spotX">Where the ✗ was — the real dug spot when the bury recorded one, else the
    /// hash-scatter the mark was drawn at. The caller resolves it exactly as the ✗ resolves it, so the hole
    /// is where the mark was and not near it.</param>
    /// <param name="spotY">See <paramref name="spotX"/>.</param>
    /// <param name="periodIndex">The discovery period the roll landed in — the day it happened.</param>
    public static Evidence LeftBehind(
        string cacheId, int reeverLevel, double spotX, double spotY, long periodIndex)
    {
        ArgumentNullException.ThrowIfNull(cacheId);
        ReeverRoll roll = WatchdogsMet(cacheId, reeverLevel, periodIndex);
        double at = MomentOf(periodIndex);

        int count = HusksLeftBy(roll);
        var husks = new List<GroundMemory.Husk>(count);
        for (int i = 0; i < count; i++)
        {
            (double hx, double hy) = Scatter(cacheId, periodIndex, i, spotX, spotY);
            husks.Add(new GroundMemory.Husk(hx, hy, at));
        }

        var pit = new GroundMemory.Scar(GroundMemory.ScarKind.Pit, spotX, spotY, at);

        GroundMemory.Scar? bot = null;
        if (LeftASentryBehind(roll))
        {
            // The bot is scattered on the same ring as the bodies and off the same seed, one index past the
            // last of them — it stood in the same fight, so it is placed by the same arithmetic.
            (double bx, double by) = Scatter(cacheId, periodIndex, count, spotX, spotY);
            bot = new GroundMemory.Scar(GroundMemory.ScarKind.DryBot, bx, by, at);
        }

        return new Evidence(roll, husks, pit, bot, at);
    }

    /// <summary>One body's resting place: a bearing and a distance off the stable hash of the chest, the day
    /// and the index, kept inside a gun's reach of the hole and inside the field's own envelope (a husk
    /// outside the walls is a mark the captain can never walk up to and read).</summary>
    private static (double X, double Y) Scatter(
        string cacheId, long periodIndex, int index, double spotX, double spotY)
    {
        string key = $"{cacheId}#{periodIndex}#{index}";
        // 0..359 degrees and a radius that never sits ON the hole (a body in the hole would read as spoil).
        double angle = StableHash.Of(key, 11) % 360UL * (Math.PI / 180.0);
        double radius = SpreadDu * (0.25 + (StableHash.Of(key, 12) % 76UL / 100.0));

        SurfaceLayout.Field f = SurfaceLayout.DefaultField;
        double x = Math.Clamp(spotX + (Math.Cos(angle) * radius), f.LeftX, f.RightX);
        double y = Math.Clamp(spotY + (Math.Sin(angle) * radius), f.BottomY, f.TopY);
        return (x, y);
    }
}
