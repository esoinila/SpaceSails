namespace SpaceSails.Core;

/// <summary>
/// #305 — the dice come out for the haze. The aerobrake QUOTE is pure and advisory (a deterministic
/// expectation, <see cref="Aerobrake.Price"/>); the FLOWN pass is where fate rolls. The owner ruled the
/// aerocapture risk currency is DICE ("Let's use dice there, I love it"), plus the TTRPG homage — show
/// the cast dice. This is that table: each flown pass rolls a 2D6 episode in the BUSTED-engine house
/// style (clean pass, heat spike, g wobble, corridor drama, a torn sail on a bad roll), seeded and
/// fully deterministic in Core so client and any replay agree on every face.
///
/// <para><b>The roll.</b> 2D6 (natural 2–12), skewed by how hard the deterministic pass already bit: a
/// pass riding the g-line carries a negative "load in the corridor" modifier, so a hot pass is genuinely
/// more likely to go dramatic — the dice never contradict the physics, they colour its margin. A pass
/// the physics already holed (peak g at/over the 3 g line) always reads as an over-the-line episode; the
/// dice only decide the flavour of a survivable pass's margin.</para>
///
/// <para><b>The currency.</b> Each episode returns an adjusted <see cref="Aerobrake.PassCost"/> — the
/// dice-scripted currency the owner's Q3 asked for: a low roll on a hot-but-under-line pass can tear the
/// sail early; a great roll spares its heat. The QUOTE never rolls, so a menu quote stays byte-identical
/// whether episodes are live or not — only the flown pass carries the dice.</para>
/// </summary>
public static class AerobrakeEpisodes
{
    /// <summary>The kind of pass the dice narrated — coarse buckets the tray and tests read.</summary>
    public enum Kind
    {
        /// <summary>The dice were kind: a textbook skim, the haze took its due and handed you back.</summary>
        GracefulSkim,

        /// <summary>A clean pass — nominal, nothing the crew will retell.</summary>
        CleanPass,

        /// <summary>The leading edge glowed past the gauge, then the thinning air let go — heat, no hole.</summary>
        HeatSpike,

        /// <summary>The rig shuddered as the load swung past spec, but the spars held — a scare, no hole.</summary>
        GWobble,

        /// <summary>You skipped the shell shallow and clawed the corridor back by hand — real drama.</summary>
        CorridorDrama,

        /// <summary>A bad roll on a hot margin: a seam let go and the sail tore on the way through.</summary>
        TornSail,

        /// <summary>The physics itself pegged past the 3 g line — the sail splits, dice or no dice.</summary>
        OverTheLine,
    }

    /// <summary>One flown pass's rolled outcome: the dice event to SHOW, the adjusted per-pass cost the
    /// ship actually pays, and the coarse <see cref="Kind"/> for the log and tests.</summary>
    public readonly record struct Episode(DiceEvent Event, Aerobrake.PassCost Cost, Kind Kind)
    {
        /// <summary>True when this pass tore the sail (a bad roll, or the physics crossed the line).</summary>
        public bool HolesSail => Cost.HolesSail;
    }

    /// <summary>The source tag every aerobrake dice event carries (the tray's caption).</summary>
    public const string Source = "AEROBRAKE";

    /// <summary>Fold a flown pass into a stable seed: the pass ordinal plus a sim-state number (sim time,
    /// a body id hash) so the roll is reproducible from the exact moment it happened, and successive
    /// passes on one arrival roll independent episodes.</summary>
    public static ulong Seed(int passOrdinal, long simStateA, long simStateB = 0) =>
        DiceRule.Seed("aerobrake-episode", passOrdinal, simStateA, simStateB);

    /// <summary>
    /// Roll the 2D6 episode for one flown pass whose deterministic cost is <paramref name="deterministic"/>,
    /// seeded by <paramref name="seed"/>. Pure and deterministic. The QUOTE is never touched — only the
    /// flown pass rolls here.
    /// </summary>
    public static Episode Roll(Aerobrake.PassCost deterministic, ulong seed)
    {
        // A hot pass (already near the 3 g line) drags the roll down — the physics colours the odds, the
        // dice never overrule it. 0 g → 0, at-the-line → −4. Small integers; never OP (the house law).
        int loadPenalty = -(int)System.Math.Round(System.Math.Clamp(deterministic.HullLoadFraction, 0, 1) * 4);
        List<DiceModifier> mods = [];
        if (loadPenalty != 0)
        {
            mods.Add(new DiceModifier("load in the corridor", loadPenalty));
        }

        DicePool pool = DiceRule.RollPool(seed, count: 2, sides: 6, mods);
        int total = pool.Total;

        // The physics already crossed the line → an over-the-line episode regardless of the dice; the
        // sail splits. Otherwise the 2D6 total picks the flavour of a survivable pass's margin.
        (Kind kind, string headline, string detail, Aerobrake.PassCost cost) = deterministic.HolesSail
            ? OverTheLineEpisode(deterministic)
            : total switch
            {
                <= 3 => TornSailEpisode(deterministic),
                <= 5 => CorridorDramaEpisode(deterministic),
                <= 7 => GWobbleEpisode(deterministic),
                <= 9 => HeatSpikeEpisode(deterministic),
                <= 11 => CleanPassEpisode(deterministic),
                _ => GracefulSkimEpisode(deterministic),
            };

        return new Episode(DiceEvent.FromPool(Source, pool, headline, detail), cost, kind);
    }

    // ===== The episode table — the house voice (crude TTRPG entries, one per bucket) =================

    private static (Kind, string, string, Aerobrake.PassCost) GracefulSkimEpisode(Aerobrake.PassCost c) => (
        Kind.GracefulSkim,
        "🪂 Textbook skim — you thread the corridor like it was painted on.",
        $"The crew barely spills the coffee. Peak {c.PeakG:F1} g, the sail whole.",
        c with { HullLoadFraction = System.Math.Max(0, c.HullLoadFraction - 0.05) });

    private static (Kind, string, string, Aerobrake.PassCost) CleanPassEpisode(Aerobrake.PassCost c) => (
        Kind.CleanPass,
        "🪂 A clean pass — the haze takes its due and hands you back to the dark.",
        $"Nothing the crew will retell. Peak {c.PeakG:F1} g, well under the line.",
        c);

    private static (Kind, string, string, Aerobrake.PassCost) HeatSpikeEpisode(Aerobrake.PassCost c) => (
        Kind.HeatSpike,
        "🔥 Heat spike — the leading edge glows past the gauge.",
        $"Then the air thins and lets you go. Peak {c.PeakG:F1} g; the rigging held, but it smelt the burn.",
        c with { HullLoadFraction = System.Math.Min(1.0, c.HullLoadFraction + 0.10) });

    private static (Kind, string, string, Aerobrake.PassCost) GWobbleEpisode(Aerobrake.PassCost c) => (
        Kind.GWobble,
        "😬 G wobble — the rig shudders as the load swings past spec.",
        $"The spars hold, just. Peak {c.PeakG:F1} g; a scare, not a scar.",
        c with { HullLoadFraction = System.Math.Min(1.0, c.HullLoadFraction + 0.05) });

    private static (Kind, string, string, Aerobrake.PassCost) CorridorDramaEpisode(Aerobrake.PassCost c) => (
        Kind.CorridorDrama,
        "🎢 Corridor drama — you skip off the shell shallower than planned.",
        $"You claw the line back by hand. Peak {c.PeakG:F1} g; the sail frays but holds.",
        c with { HullLoadFraction = System.Math.Min(1.0, c.HullLoadFraction + 0.20) });

    private static (Kind, string, string, Aerobrake.PassCost) TornSailEpisode(Aerobrake.PassCost c) => (
        Kind.TornSail,
        "💥 The haze bites deep — a seam lets go and the sail tears on the way through.",
        // #227 flavor: the fine print notices the hobby — a claim they'll fight.
        $"The crew is already sewing. (The policy calls it \"atmospheric entry, intentional\" — a claim they'll fight.)",
        c with { HolesSail = true, HullLoadFraction = System.Math.Max(0.90, c.HullLoadFraction) });

    private static (Kind, string, string, Aerobrake.PassCost) OverTheLineEpisode(Aerobrake.PassCost c) => (
        Kind.OverTheLine,
        $"💥 The load pegs at {c.PeakG:F1} g — past the 3 g line; the sail splits.",
        "No dice needed for this one — the corridor took its toll. The crew is sewing.",
        c with { HolesSail = true });
}
