namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT ENDS THE HUNT, SAID OUT LOUD (#962) — the warrant line, the hiding and nerve terms, the good
/// life, the warning-off, sun glare, and the seed hash.
///
/// <para>Split out of <c>EncounterRule.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its one field is a <c>const</c>.</para>
/// </summary>
public static partial class EncounterRule
{
    // ── #962 · WHAT ENDS THE HUNT, SAID OUT LOUD, WITH THE LIVE NUMBER ────────────────────────────────
    //
    // Owner, docked at a haven with the heat gauge reading zero and a collector still inbound: "So we have
    // zero heat and are docked at haven ... why is this still hunting us?" The rules that would have
    // answered him were all RIGHT HERE and none of them was ever said to his face: the contract is bought
    // once and does not care what the heat gauge says afterwards; hiding two unbroken days at a haven is
    // what makes her lose the scent; warning shots erode her nerve until she voids it; a holed sail ends
    // it outright. He was reading a dossier that told him her callsign, her range, and nothing he could act
    // on.
    //
    // These live in EncounterRule, beside ApplyBreakOff and WarnOff, ON PURPOSE. This repo has a named bug
    // class for a sentence that reports one thing while the sim does another, and the only structural
    // defence is that the sentence and the rule read the SAME constant in the same file — so the countdown
    // below reaches zero on exactly the tick ApplyBreakOff fires, and the shot count below reaches zero on
    // exactly the shot WarnOff voids the contract on. Both agreements are swept in EncounterRuleTests.

    /// <summary>Who bought this contract. A collector is hired over ONE job — the hull you took — and where
    /// that name is known it is named, because "why is this still hunting us" is not a question a callsign
    /// can answer. The unattributed case says the true thing too: nobody the captain has ever met.</summary>
    public static string WarrantLine(HunterState hunter) =>
        hunter.Warrant is { Length: > 0 } hull
            ? $"⚖ writ served over the {hull} job — underwriters, not the law. Heat cools; a contract does not."
            : "⚖ writ served by underwriters you never met — recovery of an insured asset. Heat cools; a contract does not.";

    /// <summary>The hiding clause, with the clock running. <paramref name="hiddenDurationSeconds"/> is the
    /// caller's continuous haven-hiding clock — the same value it hands <see cref="ApplyBreakOff"/> — and
    /// <paramref name="hiddenNow"/> says whether that clock is running at all, because a clock that is not
    /// running is the single most useful thing a captain can be told while a collector closes.</summary>
    public static string HidingTerm(double hiddenDurationSeconds, bool hiddenNow)
    {
        // Invariant throughout: determinism is law in Core, and a decimal comma in a Finnish browser
        // would make this sentence differ from the one a test read.
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        string window = BreakOffHiddenDays.ToString("0.#", invariant);
        if (!hiddenNow)
        {
            return $"🏴 she loses the scent after {window} d hidden at a haven — the clock is NOT running";
        }

        double remaining = BreakOffHiddenDays * DaySeconds - hiddenDurationSeconds;
        return remaining <= 0
            ? "🏴 she has lost the scent — the haven kept you"
            : $"🏴 she loses the scent after {window} d hidden at a haven — "
              + $"{(remaining / DaySeconds).ToString("0.0", invariant)} d to go";
    }

    /// <summary>The nerve clause, counted down to the shot that actually voids the contract — the same
    /// arithmetic <see cref="WarnOff"/> does, off the same <see cref="NerveThreshold"/>.</summary>
    public static string NerveTerm(HunterState hunter, int playerHeat)
    {
        int remaining = NerveThreshold(hunter.Id, playerHeat) - hunter.WarningShotsTaken;
        return remaining <= 0
            ? "🎖 she has had enough already — the contract is void"
            : remaining == 1
                ? "🎖 …or ONE more warning shot near her voids the contract"
                : $"🎖 …or {remaining} more warning shots near her void the contract";
    }

    /// <summary>The third door, which needs no number: hole her sail and she is out of the chase for good.
    /// Said here so the card carries every way out, not the two that happen to have clocks.</summary>
    public const string SailTerm = "🎯 …or hole her sail — a holed collector breaks off for good";

    /// <summary>Deterministic per-collector disposition: does this one prize the good life over
    /// the fee? Such a collector voids the contract at the very first warning shot. Rarer as heat
    /// (the bounty) rises. Salted apart from <see cref="ComplianceOf"/> so a ship and a hunter that
    /// happen to share an id never correlate.</summary>
    public static bool PrefersTheGoodLife(string hunterId, int playerHeat)
    {
        double fraction = Math.Max(MinLaDolceVitaFraction,
            LaDolceVitaFraction - LaDolceVitaFractionPerHeatLevel * Math.Max(0, playerHeat));
        double roll = new DeterministicRandom(HashSeed(hunterId) ^ 0x4C61446F6C6365UL).NextDouble();
        return roll < fraction;
    }

    /// <summary>How many warning shots this collector weathers before giving up for good: the
    /// good-life sort quit at the first, everyone else needs <see cref="BaseHunterNerve"/>, plus
    /// one per heat level (a notorious captain draws grittier muscle).</summary>
    public static int NerveThreshold(string hunterId, int playerHeat) =>
        PrefersTheGoodLife(hunterId, playerHeat) ? 1 : BaseHunterNerve + Math.Max(0, playerHeat);

    /// <summary>A warning shot lands near the collector: its nerve erodes. Each shot buys a longer
    /// coast-off (it stops closing — see <see cref="AdvanceHunter"/>), and once the shots reach its
    /// <see cref="NerveThreshold"/> it voids the contract for good. A caught or already-broken
    /// hunter is unmoved. Pure: the peel window is derived from the shot count and sim time, no
    /// live RNG.</summary>
    public static HunterState WarnOff(HunterState hunter, int playerHeat, double simTime)
    {
        if (hunter.CaughtPlayer || hunter.BrokenOff)
        {
            return hunter;
        }

        int shots = hunter.WarningShotsTaken + 1;
        if (shots >= NerveThreshold(hunter.Id, playerHeat))
        {
            return hunter with { WarningShotsTaken = shots, BrokenOff = true };
        }

        double peelUntil = simTime + shots * HunterPeelStepDays * DaySeconds;
        return hunter with { WarningShotsTaken = shots, PeeledUntilSimTime = peelUntil };
    }

    /// <summary>Attack out of the sun: true when the player sits inside the glare cone as seen from
    /// the hunter (the sun is at the world origin) AND is on the sunward side — the hunter would be
    /// squinting into the star to find them. Pure geometry, no state.</summary>
    public static bool SunBlinded(Vector2d hunterPosition, Vector2d playerPosition)
    {
        double hunterDist = hunterPosition.Length;
        // Player must be nearer the sun than the hunter — i.e. genuinely between star and pursuer.
        if (playerPosition.Length >= hunterDist || hunterDist <= 0)
        {
            return false;
        }

        Vector2d toPlayer = playerPosition - hunterPosition;
        Vector2d toSun = -hunterPosition; // origin - hunter
        double toPlayerLen = toPlayer.Length;
        if (toPlayerLen <= 0)
        {
            return false;
        }

        double cosAngle = toPlayer.Dot(toSun) / (toPlayerLen * hunterDist);
        return cosAngle >= Math.Cos(SunGlareConeDegrees * Math.PI / 180.0);
    }

    // FNV-1a 64-bit: stable across processes and platforms (unlike string.GetHashCode, which is
    // randomized per run) — determinism is law, and ship ids seed every deterministic roll here.
    private static ulong HashSeed(string id)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in id)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
