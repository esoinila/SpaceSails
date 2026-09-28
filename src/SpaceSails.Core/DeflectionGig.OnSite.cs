namespace SpaceSails.Core;

/// <summary>
/// #251 · ON SITE (#394) — the rotation alignment and the burn's raise, the outcome bands, the impact
/// clock, the complications a beat rolls, and the pay.
///
/// <para>Split out of <c>DeflectionGig.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class DeflectionGig
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  ABLATION + ROTATION — the charge doesn't just push; it ablates a jet of rock, and the rock is
    //  SPINNING, so the jet only shoves the right way when the bore faces the required heading. Firing in
    //  the rotation window delivers full impulse; off-window wastes it. The client auto-fires at the next
    //  aligned moment, so a clean run lands ~1.0; a forced/misfired shot drops it.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The rotation ALIGNMENT [0..1] of the drilled bore at <paramref name="onSiteSeconds"/>, given
    /// the rock's spin (period <paramref name="spinPeriodSeconds"/>, phase <paramref name="spinPhase"/>). A
    /// raised-cosine of the angle between the bore heading and the required push heading: 1 when aligned, 0
    /// on the far side. The impulse scales by this.</summary>
    public static double RotationAlignment(double spinPeriodSeconds, double spinPhase, double onSiteSeconds)
    {
        if (spinPeriodSeconds <= 0.0)
        {
            return 1.0; // a non-spinning rock is always aligned
        }
        double angle = spinPhase + System.Math.Tau * onSiteSeconds / spinPeriodSeconds;
        return 0.5 * (1.0 + System.Math.Cos(angle));
    }

    /// <summary>Alignment at or above this counts as "in the firing window" — the client holds the charge
    /// until it, so a normal run fires clean (~1.0).</summary>
    public const double FiringWindowAlignment = 0.85;

    /// <summary>The periapsis raise (m) a burn delivers: the charge fraction actually drilled and fired ×
    /// the rock's ablation efficiency × the rotation alignment at fire × the ceiling. Pure — the whole
    /// deflection math funnels through here. Clamped non-negative.</summary>
    public static double PeriapsisRaiseForBurn(RockType type, double chargeFraction, double rotationAlignment) =>
        System.Math.Max(0.0, System.Math.Clamp(chargeFraction, 0.0, 1.0))
        * RockProfile.AblationEfficiency(type)
        * System.Math.Clamp(rotationAlignment, 0.0, 1.0)
        * MaxPeriapsisRaiseMeters;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  SUCCESS BANDS — read off the resulting miss distance. Full = clears with margin (heroic pay);
    //  grazing = a scrape (reduced, honest pay, heavy damage narration); impact = the rock hits (Ringside
    //  SURVIVES as canon — heavy damage + market disruption, never destroyed).
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>At or above this miss (m) the rock CLEARS the station cleanly — full deflection. ~2% of the
    /// canonical Ringside orbit radius, so the lifted rail reads plainly on the map. OWNER-TUNABLE.</summary>
    public const double SafeMissMeters = 3.0e7;

    /// <summary>Below this miss (m) the rock HITS — impact. Between graze and safe is a scrape. Comfortably
    /// above the rock's own radius so a "miss" is a real miss. OWNER-TUNABLE.</summary>
    public const double GrazeMissMeters = 8.0e6;

    /// <summary>Which band a miss of <paramref name="missMeters"/> lands in.</summary>
    public static DeflectionOutcome Classify(double missMeters) => missMeters switch
    {
        >= SafeMissMeters => DeflectionOutcome.FullDeflection,
        >= GrazeMissMeters => DeflectionOutcome.GrazingMiss,
        _ => DeflectionOutcome.Impact,
    };

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE DOOM CLOCK — a real-seconds budget from accept to impact (the #370 on-site idiom, but the
    //  headline names the stakes). It ticks while the crew works; run it out with no burn fired and the
    //  rock hits. Evacuate (lift off) before T-0 to abort with the crew alive.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Seconds from the moment the gig is struck to impact — the mission window. Long enough to
    /// land, drill the worst rock, and fire; tight enough to be loud. OWNER-TUNABLE.</summary>
    public const double ImpactBudgetSeconds = 360.0;

    /// <summary>Inside this many seconds the clock reads "LAST CALL" — fire or evacuate NOW.</summary>
    public const double CriticalSeconds = 45.0;

    /// <summary>Seconds to impact after <paramref name="elapsedOnSiteSeconds"/> of the budget, floored at 0.</summary>
    public static double SecondsToImpact(double elapsedOnSiteSeconds) =>
        System.Math.Max(0.0, ImpactBudgetSeconds - System.Math.Max(0.0, elapsedOnSiteSeconds));

    /// <summary>How the doom clock reads right now.</summary>
    public static ImpactClock ClassifyClock(double elapsedOnSiteSeconds)
    {
        double left = SecondsToImpact(elapsedOnSiteSeconds);
        if (left <= 0.0)
        {
            return ImpactClock.Impact;
        }
        return left <= CriticalSeconds ? ImpactClock.LastCall : ImpactClock.Counting;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE COMPLICATIONS — diced on the #370 cadence while the crew drills. The horror here is the CLOCK,
    //  not the pack (kept OFF this site — owner). The drill snaps (re-channel part way), a tremor shocks
    //  the nerve, a crew member bolts (retrieve or lose — the #386 beat), or the bit finds a good bite.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The tray caption every deflection complication carries.</summary>
    public const string Source = "DEFLECTION";

    /// <summary>Ground-time seconds between diced complications while the crew drills. OWNER-TUNABLE.</summary>
    public const double EventCadenceSeconds = 45.0;

    /// <summary>How many complications have come due by <paramref name="onSiteSeconds"/> — one per cadence.</summary>
    public static int EpisodesElapsed(double onSiteSeconds) =>
        onSiteSeconds <= 0.0 ? 0 : (int)(onSiteSeconds / EventCadenceSeconds);

    /// <summary>Fold one complication's stable seed from the accept moment, the rock id and the beat ordinal.</summary>
    public static ulong Seed(double acceptedSimTime, string rockBodyId, int ordinal) =>
        DiceRule.Seed("deflection-event", (long)acceptedSimTime, HashId(rockBodyId), ordinal);

    /// <summary>Roll one on-site complication for <paramref name="ordinal"/>, seeded by <paramref name="seed"/>,
    /// coloured by the rock <paramref name="type"/> — an M-type snaps bits harder (a deeper drill setback).
    /// 2D6; pure and deterministic.</summary>
    public static DeflectionComplication Roll(ulong seed, RockType type, int ordinal)
    {
        DicePool pool = DiceRule.RollPool(seed, count: 2, sides: 6);
        return pool.Total switch
        {
            <= 4 => DrillSnap(pool, type),
            <= 6 => CrewBolt(seed, pool),
            <= 8 => Tremor(pool),
            <= 10 => SteadyBite(pool),
            _ => GoodBite(pool),
        };
    }

    // 2–4 · THE DRILL SNAPS. The bit shears in the rock — lose a chunk of drill progress; re-channel from
    // there. A metallic rock snaps bits harder (a deeper setback). No nerve hit; the clock is the punishment.
    private static DeflectionComplication DrillSnap(DicePool pool, RockType type)
    {
        double loss = type.Composition == RockComposition.MType ? 0.35 : 0.22;
        return new DeflectionComplication(
            DiceEvent.FromPool(Source, pool, "🛠 The drill bit SNAPS.",
                $"It shears off deep in the rock — the boys swap the bit and set the shoulder again. Drilling backs up. The clock does not."),
            DeflectionBand.DrillSnap, NerveHit: 4, DrillProgressDelta: -loss, CrewLost: false);
    }

    // 5–6 · A CREW MEMBER BOLTS. Nerve goes standing on a rock falling at a city — they scramble for the
    // shuttle. A salted recovery roll: dragged back, or lost in the scramble (docks the pay). Nerve either way.
    private static DeflectionComplication CrewBolt(ulong seed, DicePool pool)
    {
        bool recovered = DiceRule.RollPool(DiceRule.Seed(seed, "bolt-recover"), 2, 6).FaceTotal >= 6; // ~72% back
        (string head, string detail) = recovered
            ? ("🏃 A crew member breaks for the shuttle.",
               "Standing on a falling mountain does it — they crack and run. The others tackle them at the airlock and haul them back to the rig, shaking.")
            : ("🏃 A crew member breaks — and is gone.",
               "They panic on the tether and cut it wrong; the rock's slow spin takes them over the limb before anyone can grab hold. The crew works on one short.");
        return new DeflectionComplication(
            DiceEvent.FromPool(Source, pool, head, detail),
            DeflectionBand.CrewBolts, NerveHit: 12, DrillProgressDelta: 0.0, CrewLost: !recovered);
    }

    // 7–8 · A TREMOR. The rock groans and shifts — a nerve lump, no lasting harm. The drilling holds.
    private static DeflectionComplication Tremor(DicePool pool) =>
        new(DiceEvent.FromPool(Source, pool, "🌋 The rock GROANS.",
                "A tremor runs the length of it — dust jumps off the regolith and hangs. Everyone freezes, then the rig bites again. Nerves fray."),
            DeflectionBand.Tremor, NerveHit: 7, DrillProgressDelta: 0.0, CrewLost: false);

    // 9–10 · A STEADY BITE. The drilling continues; nothing the crew will retell. No effect.
    private static DeflectionComplication SteadyBite(DicePool pool) =>
        new(DiceEvent.FromPool(Source, pool, "⚙ The rig bites steady.",
                "Clean cuttings, good depth. Nobody looks up at the station growing in the sky. No time."),
            DeflectionBand.Steady, NerveHit: 0, DrillProgressDelta: 0.0, CrewLost: false);

    // 11–12 · A GOOD BITE. The bore runs true and gains — a little drill progress banked back.
    private static DeflectionComplication GoodBite(DicePool pool) =>
        new(DiceEvent.FromPool(Source, pool, "⚙ The bore runs TRUE.",
                "The bit finds a clean seam and races — depth banks faster than the plan. For one minute, the crew is winning."),
            DeflectionBand.GoodBite, NerveHit: 0, DrillProgressDelta: +0.12, CrewLost: false);

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE PAYOUT — heroic. Composed the house way (the #370 ExpeditionReward idiom): a fat base carried
    //  through the haul-distance floor, scaled by the outcome band, docked per crew lost, floored.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The authored base fee (credits) for saving a port — a heroic premium. OWNER-TUNABLE.</summary>
    public const int BaseFee = 12000;

    /// <summary>What a lost crew member costs off the payout (credits).</summary>
    public const int PerCrewLostPenalty = 2000;

    /// <summary>The floor the gig can never fall below — the crew flew at a falling mountain; the port pays
    /// this even for a scrape or an honest abort.</summary>
    public const int Floor = 1500;

    /// <summary>The grazing-miss band pays this FRACTION of the full heroic pay — a scrape saved the port
    /// but left it bleeding; the exchange pays less and says so. OWNER-TUNABLE.</summary>
    public const double GrazingPayFraction = 0.5;

    /// <summary>The heroic deflection payout: the fat base carried through the haul-distance floor
    /// (<see cref="HaulReward.WithFloor"/> over the two heliocentric radii), scaled by the outcome band
    /// (full = 1.0, grazing = <see cref="GrazingPayFraction"/>, impact/abort = floor only), docked per crew
    /// lost, floored at <see cref="Floor"/>. Order-free and clamped.</summary>
    public static int Total(int baseFee, double fromRadiusMeters, double toRadiusMeters,
        DeflectionOutcome outcome, int crewLost)
    {
        int distanced = HaulReward.WithFloor(baseFee, fromRadiusMeters, toRadiusMeters);
        double bandScale = outcome switch
        {
            DeflectionOutcome.FullDeflection => 1.0,
            DeflectionOutcome.GrazingMiss => GrazingPayFraction,
            _ => 0.0, // impact / abort — floor only
        };
        int gross = (int)System.Math.Round(distanced * bandScale) - (System.Math.Max(0, crewLost) * PerCrewLostPenalty);
        return System.Math.Max(Floor, gross);
    }
}
