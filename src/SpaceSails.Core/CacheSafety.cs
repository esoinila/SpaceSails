namespace SpaceSails.Core;

/// <summary>#455 · The three rungs a hiding place can read as. Named for what a captain PROMISED HIMSELF
/// when he put the chest down, and the only three words the game ever uses for it.</summary>
public enum CacheSafetyRung
{
    /// <summary>It is where anyone would find it: dropped in the open, or up by the pad, on quiet ground.</summary>
    Exposed,

    /// <summary>Buried with the shovel, off the paths — a real hiding place, not an unbreakable one.</summary>
    Considered,

    /// <summary>A deep carry and/or ground the Old Ones haunt. The best vault in the system.</summary>
    Guarded,
}

/// <summary>#455 · ONE READ OF A HIDING PLACE — the whole answer, in one value, so the sentence and the dice
/// are the same arithmetic. <paramref name="ChancePerMille"/> is what the return-trip roll compares against;
/// <paramref name="Rung"/> is the word the bury line and the ledger row say. The three terms are carried
/// alongside so a card, a lab or a test can show its working instead of re-deriving it (which is how two
/// reporters of one truth start to drift).</summary>
/// <param name="ChancePerMille">The rival-discovery chance for one day, in per mille (10 = 1%).</param>
/// <param name="Rung">The word for this hiding place.</param>
/// <param name="DepthCredit">Per mille shaved off by the carry — the courage term (0 for a dropped chest).</param>
/// <param name="ShovelTerm">Per mille the shovel bought (negative) or the open ground cost (positive).</param>
/// <param name="WatchdogTerm">Per mille shaved off by the Old Ones standing over it (negative).</param>
public readonly record struct CacheSafetyRead(
    int ChancePerMille,
    CacheSafetyRung Rung,
    int DepthCredit,
    int ShovelTerm,
    int WatchdogTerm)
{
    /// <summary>The rung's WORD — "Exposed" / "Considered" / "Guarded". What the ledger row shows.</summary>
    public string Word => CacheSafety.Word(Rung);

    /// <summary>The rung's authored line (Fable canon, 2026-09-02) — what the bury/drop flow says.</summary>
    public string Line => CacheSafety.Line(Rung);

    /// <summary>The daily odds as a player-facing percentage ("0.4%" / "1%"). One decimal only when it
    /// needs one, so the common whole-percent reads stay clean.</summary>
    public string OddsText =>
        ChancePerMille % 10 == 0
            ? $"{ChancePerMille / 10}%"
            : $"{ChancePerMille / 10.0:0.#}%";

    /// <summary>The full safety read as one sentence: the word, the authored line, the real number behind
    /// it. This is what makes the shipped "Rivals may dig it up over the coming days" true arithmetic
    /// rather than flavour (#455 rule 3 / #761).</summary>
    public string Sentence => $"{Word} — {Line} (rivals: ~{OddsText} a day)";
}

/// <summary>
/// #455 · <b>THE ONE ORACLE FOR HOW SAFE A CACHE IS.</b>
///
/// <para>Owner, live 2026-07-27: <i>"venturing deeper exposes the player to more reevers, but it makes the
/// buried place also equally more safe"</i> … <i>"burying the chest further should be seen as making it even
/// more safe from looters"</i> … <i>"so even one left on surface might be quite safe thanks to reevers as
/// watch dogs"</i>. The governing law the issue states from those: <b>the same distance that makes the walk
/// dangerous is what makes the cache safe.</b></para>
///
/// <h3>Three terms, one function</h3>
/// <list type="number">
/// <item><b>The carry</b> — how far from the landing pad the shovel went in. Carried courage: the walk that
/// nearly killed you is the thing that pays. Full credit at the deep commitment anchor
/// (<see cref="FullCarryDu"/>, read off <see cref="SurfaceLayout.DefaultField"/> — never a literal here).</item>
/// <item><b>The shovel</b> — buried is a hiding place; a chest left lying where it fell is not. The premium
/// is small and the open-ground penalty is large, because the difference between the two is the whole of
/// rule 2.</item>
/// <item><b>The ground's Reever weight</b> — the watchdogs (#295). Each standing Old One shaves a full
/// percentage point off a rival's odds, which is the arithmetic that has shipped since #295 and is preserved
/// here to the point.</item>
/// </list>
///
/// <h3>Why it is one function and not two</h3>
/// <para>The line shown when you bury and the roll thrown when you are away read the SAME
/// <see cref="Read(double?, bool?, int)"/>. This repository's named bug class is one truth with two
/// reporters — a sim doing one thing while a sentence reports another — and a promise about a hiding place
/// is exactly the shape that bug likes. So the rung is derived from the chance
/// (<see cref="RungFor"/>), the chance is derived from the three terms, and there is nowhere else to
/// compute either.</para>
///
/// <h3>The legacy read</h3>
/// <para>A cache with no recorded shovel and no recorded carry — every chest saved before #455, every
/// rumour map — reads <c>Base − watchdogs</c>, which is EXACTLY the odds it has been living under since
/// #295 (4%, 3%, 2%, 1%). A chest already in the ground is not re-priced under a rule invented after it was
/// buried; it keeps the deal it was buried under.</para>
/// </summary>
public static class CacheSafety
{
    // ── The scale ────────────────────────────────────────────────────────────────────────────────────
    //
    // PER MILLE, not percent. #295's whole ladder was 4/3/2/1 percent, which left three points of room for
    // three terms — so any one of them saturated the floor on its own and the other two silently stopped
    // meaning anything. A finer grain is what lets "deeper is safer" AND "the Reevers are your watchdogs"
    // both be true at once on the same chest. The percent view (DiscoveryRule.DiscoveryChancePercent) is
    // still exactly this scale divided by ten, so nothing about the old ladder moved.

    /// <summary>An unhidden chest's baseline daily odds of being found by a rival: 40‰ = 4%, the number
    /// that has priced the hoard since the discovery roll shipped.</summary>
    public const int BaseChancePerMille = 40;

    /// <summary>The floor no amount of depth, shovel or watchdogs can breach (#295): a hoard is never
    /// immortal. 10‰ = 1%, unchanged.</summary>
    public const int MinChancePerMille = 10;

    /// <summary>The ceiling the terms can never push past — 60‰ = 6%. Nothing reachable today comes near it
    /// (the worst hiding place in the game, a chest dropped in the open on quiet ground, reads 54‰); it is a
    /// clamp so a later term cannot quietly turn a hoard into a lottery ticket.</summary>
    public const int MaxChancePerMille = 60;

    /// <summary>What the shovel itself buys, before the walk: modest on purpose. Digging a hole by the pad
    /// is not a plan.</summary>
    public const int ShovelPremiumPerMille = 4;

    /// <summary>What lying in the open costs. Large, because rule 2 is "buried beats dropped, BY A LOT" —
    /// and because a chest nobody dug a hole for is the only object in the game that is lost by running.</summary>
    public const int OpenGroundPenaltyPerMille = 14;

    /// <summary>What one standing Old One shaves off a rival's odds — one full percentage point, the #295
    /// ladder to the point.</summary>
    public const int WatchdogPerMille = 10;

    /// <summary>The most the carry can ever buy. Deliberately larger than the shovel premium: the walk is
    /// the expensive thing and it is the thing the issue is about.</summary>
    public const int MaxCarryCreditPerMille = 18;

    // ── The rung boundaries ──────────────────────────────────────────────────────────────────────────
    //
    // Read off the CHANCE, so the word can never promise something the dice do not deliver. The two
    // numbers are picked so the bands line up with the canon parentheticals (Fable, 2026-09-02):
    //   Exposed   = dropped in the open / near the pad / quiet ground
    //   Considered= buried with the shovel, off the paths
    //   Guarded   = deep carry and/or bad ground
    // and — checked by TheHidingPlaceIsOneOracleTests — so that no chest left lying in the open can ever
    // land in the CONSIDERED band, whose authored line says the word "Buried" out loud. A dropped chest
    // reads Exposed, or (on ground a full pack haunts) Guarded, and never a line that lies about a shovel.

    /// <summary>At or below this, the hiding place reads <see cref="CacheSafetyRung.Guarded"/>.</summary>
    public const int GuardedAtOrBelowPerMille = 24;

    /// <summary>At or above this, it reads <see cref="CacheSafetyRung.Exposed"/>.</summary>
    public const int ExposedAtOrAbovePerMille = 34;

    /// <summary>The pad the carry is measured from — the tube's own column at the landing band. Read from
    /// Core's one field envelope, so a field that grows re-prices the walk instead of leaving this file
    /// quietly auditing a world that no longer exists (#573's lesson, and this repo's bug class 1).</summary>
    public static (double X, double Y) Pad =>
        (SurfaceLayout.DefaultField.HomeX, SurfaceLayout.DefaultField.LandingBandY);

    /// <summary>The carry that earns FULL credit: pad to the deep commitment anchor — the heart of the deep
    /// field, the far end of the walk every body dresses differently. Not a tuned literal: it is the
    /// distance the field itself calls "deep".</summary>
    public static double FullCarryDu
    {
        get
        {
            SurfaceLayout.Field f = SurfaceLayout.DefaultField;
            double dx = f.AnchorX - f.HomeX, dy = f.AnchorY - f.LandingBandY;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }

    /// <summary>How far a spot on the surface is from the pad, in deck units — the datum a bury records.
    /// Zero anywhere up on (or above) the landing band; grows with the walk out and down.</summary>
    public static double PadDistanceOf(double x, double y)
    {
        (double px, double py) = Pad;
        double dx = x - px, dy = Math.Min(0, y - py); // above the band is still "at the pad"
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>The carry term for a recorded distance: 0 at the pad, <see cref="MaxCarryCreditPerMille"/>
    /// at the deep anchor and beyond, linear between. Truncated (never rounded up) so the credit is
    /// monotone in the walk and never flatters a step.</summary>
    public static int CarryCredit(double padDistanceDu) =>
        (int)(MaxCarryCreditPerMille * Math.Clamp(padDistanceDu / FullCarryDu, 0.0, 1.0));

    /// <summary>
    /// <b>THE ORACLE.</b> One read of one hiding place from the three terms.
    /// </summary>
    /// <param name="padDistanceDu">How far from the pad the chest was put down, in deck units. Null for a
    /// chest that never recorded it (a legacy save, a rumour map) — the carry then buys nothing.</param>
    /// <param name="buried">True = the shovel went in; false = it lies in the open where it was dropped;
    /// null = unrecorded, and the chest keeps the deal it was buried under.</param>
    /// <param name="reeverLevel">Standing Old Ones on this ground, 0..<see cref="ReeverRaid.MaxReevers"/>.</param>
    public static CacheSafetyRead Read(double? padDistanceDu, bool? buried, int reeverLevel)
    {
        // The carry is CARRIED COURAGE — a walk you chose, with a chest and a shovel, to put the thing
        // somewhere. A chest dropped mid-sprint was not placed anywhere; it fell where your legs gave out,
        // on whatever ground you happened to be crossing. So the open-ground case takes no carry credit,
        // and rule 2's "by a lot" is a whole band rather than a rounding difference.
        int carry = buried is true && padDistanceDu is { } du ? CarryCredit(du) : 0;

        int shovel = buried switch
        {
            true => -ShovelPremiumPerMille,
            false => +OpenGroundPenaltyPerMille,
            null => 0,
        };

        int watchdogs = -WatchdogPerMille * Math.Max(0, reeverLevel);

        int chance = Math.Clamp(
            BaseChancePerMille - carry + shovel + watchdogs,
            MinChancePerMille,
            MaxChancePerMille);

        return new CacheSafetyRead(chance, RungFor(chance), carry, shovel, watchdogs);
    }

    /// <summary>The same read, off a chest already in the ledger — what the return-trip roll and the ledger
    /// row both call. There is no second arithmetic anywhere: this forwards.</summary>
    public static CacheSafetyRead Read(TreasureCache cache) =>
        Read(cache.PadDistance, cache.Buried, cache.ReeverLevel);

    /// <summary>The rung for a chance. The ONLY place a rung is decided, which is what makes the promise
    /// and the dice the same fact.</summary>
    public static CacheSafetyRung RungFor(int chancePerMille) =>
        chancePerMille <= GuardedAtOrBelowPerMille ? CacheSafetyRung.Guarded
        : chancePerMille >= ExposedAtOrAbovePerMille ? CacheSafetyRung.Exposed
        : CacheSafetyRung.Considered;

    /// <summary>The rung's word — the one the ledger row and the map card show.</summary>
    public static string Word(CacheSafetyRung rung) => rung switch
    {
        CacheSafetyRung.Guarded => "Guarded",
        CacheSafetyRung.Considered => "Considered",
        _ => "Exposed",
    };

    /// <summary>The rung's line, authored by Fable on the issue (canon pass, 2026-09-02) and reproduced here
    /// VERBATIM. These three sentences are the whole of what the player is told about a hiding place, so
    /// they are the sort of string a test pins character for character.</summary>
    public static string Line(CacheSafetyRung rung) => rung switch
    {
        CacheSafetyRung.Guarded => "Nobody sane digs here. That is the whole of the safe.",
        CacheSafetyRung.Considered => "Buried, off the paths. A patient rival could still read the disturbed ground.",
        _ => "It lies where anyone can see it, on ground anyone would walk.",
    };
}
