namespace SpaceSails.Core;

/// <summary>
/// PR-317 · The nerve gauge — the FIRST PLAYABLE SLICE of #226's Fail Forward sanity system (owner,
/// live 2026-07-18, right after ruling the Reevers canonical stressors: "maybe show a sanity bar when on
/// planet :-D"; and "Reevers definitely increase the player stress / tax sanity :-D").
///
/// <para>This slice is deliberately the GAUGE and its inputs, nothing more. It is the pure, deterministic
/// spine the client draws in a corner during a surface excursion: a nerve value (0..<see cref="Max"/>,
/// where full is steady hands and empty is nerves shot), the per-second drains the regolith's stressors
/// apply, the one-time Lovecraftian hit the monolith deals, the ease-off the ship's safety returns, and
/// the escalating house-voice flavor ladder the bar reads out as it falls.</para>
///
/// <para><b>Display-first (owner's law for this slice).</b> There are NO mechanical consequences here —
/// no throws, no dramatic exits, no run-out state machine. The bar bottoming out only SPEAKS
/// (<see cref="NerveBand.Shot"/> → "nerves shot — get aboard"). Consequences and the deeper restoration
/// economy (sleep, R&R, a drink with a friend — seams named in #306/#308) stay with #226 proper.</para>
///
/// <para><b>Deterministic.</b> Every method is pure arithmetic on the current nerve + the situation, so a
/// test can pin an exact drop for an exact set of stressors over an exact dt — determinism is law in Core.
/// Numbers below are MODEST and FLAGGED for the owner's tuning.</para>
/// </summary>
public static class NerveModel
{
    /// <summary>Steady hands — a full gauge, the captain who has seen nothing they cannot believe.</summary>
    public const double Max = 100.0;

    /// <summary>Nerves shot — the floor. The bar can read here but (this slice) does nothing but say so.</summary>
    public const double Min = 0.0;

    // ── Per-second drains while OUT ON THE REGOLITH (all FLAGGED for the owner's tuning) ──
    //
    // #379 (owner, Ganymede playtest 2026-07-19 + Evening wind #18): the moving-contact SIGHTING stress no
    // longer lives here as a linear per-second term. "Seeing one reever after already seeing one more does
    // not make you that much faster more nuts" — a wall of signal draining linearly is exactly why the gauge
    // bottomed out too easily. Sightings are now DISCRETE, per-spell diminishing jolts (see the sighting
    // seam below), so this continuous rate carries only the SUSTAINED situation: a live chase, a dig you
    // cannot abandon under threat, a corner. The tracker's movers gate the dig-under-threat term (something
    // is inbound) but no longer add a rate of their own.

    /// <summary>A pack is up and converging — a live chase in progress. Drains per second on top of the
    /// per-contact prickle: the knowledge that the ground roused, not just that something moves.</summary>
    public const double ChaseDrainPerSecond = 2.2;

    /// <summary>Digging while contacts are inbound — the shovel-work you cannot abandon with the tide
    /// closing. Only bites when something is ACTUALLY inbound (a calm dig on empty ground costs nothing).</summary>
    public const double DigUnderThreatDrainPerSecond = 3.0;

    /// <summary>Cornered — a net wedged between the captain and the tube mouth. The sharpest routine
    /// drain: the escape itself is contested.</summary>
    public const double CorneredDrainPerSecond = 5.0;

    // ── The Lovecraftian one-time hit ──

    /// <summary>FIRST SIGHT OF THE MONOLITH (the #226 hook #313/#318 named) — the big hit, a lump not a
    /// rate. Fires exactly once in a captain's life (persisted in the vault), never again on a revisit.</summary>
    public const double MonolithSightShock = 24.0;

    /// <summary>A REEVER LAYS HANDS ON YOU (owner, Evening wind #19, 2026-07-19: "if they get to skin, that
    /// is a different thing"). Touch is NOT a sighting — it is a big, flat lump that BYPASSES the
    /// per-spell diminishing rule entirely (habituation never dulls being grabbed) and, like the monolith,
    /// bypasses the S-curve so it always hurts noticeably. Larger than a whole spell's worth of sighting
    /// jolts. Debounced by the client's catch cadence so one brush is not a stunlock. FLAGGED for tuning.</summary>
    public const double TouchShock = 12.0;

    // ── Ease-off: the ship is safety ──

    /// <summary>Back aboard through the airlock — or flying, or docked — the nerve returns gently toward
    /// steady, this much per second. This is the whole ease-off of THIS slice (the airlock is safety); the
    /// active accelerants (sleep, R&R, a drink) stay with #226.</summary>
    public const double AboardRecoveryPerSecond = 3.5;

    /// <summary>A steady, full-gauge captain — the starting nerve and the value a pre-#317 save defaults to.</summary>
    public static double Steady => Max;

    /// <summary>The live excursion situation the drain is priced from: how many contacts move on the
    /// tracker, whether a pack is up, whether the captain is mid-dig, and whether they are cornered. The
    /// client reads these off the live surface each frame; Core only prices them.</summary>
    public readonly record struct Stressors(int MovingContacts, bool ChaseActive, bool Digging, bool Cornered);

    /// <summary>The total nerve drain per second for a situation — the sum of every applicable stressor.
    /// Digging only counts when something is actually inbound (a chase, or a mover on the tracker), so a
    /// quiet dig on empty ground never frays the captain.</summary>
    public static double DrainRatePerSecond(in Stressors s)
    {
        double rate = 0.0;
        // #379: moving contacts no longer add a linear per-second rate — their SIGHTING stress is priced as
        // discrete, per-spell diminishing jolts (see AdvanceSightings / SightingSeriesCost). They still gate
        // the dig-under-threat term below (a mover on the tracker means something is inbound).
        if (s.ChaseActive)
        {
            rate += ChaseDrainPerSecond;
        }
        if (s.Digging && (s.ChaseActive || s.MovingContacts > 0))
        {
            rate += DigUnderThreatDrainPerSecond;
        }
        if (s.Cornered)
        {
            rate += CorneredDrainPerSecond;
        }
        return rate;
    }

    // ── #379 · The S-curve rate law (owner, Ganymede playtest 2026-07-19) ────────────────────────────
    //
    //   "My sanity is bottoming out too easily at Ganymede … it restores too quick back on board. It should
    //    be kind of logarithmic … S-curve … slow at ends but quite fast in middle."
    //
    // Every CONTINUOUS change to the nerve — the regolith's drain, the ship's ease-off, and each discrete
    // SIGHTING jolt — is scaled by a shape keyed on the CURRENT nerve level. A steady captain shrugs off the
    // first frights (drain slow near full); a shattered one is slow to mend (recovery slow near empty); the
    // middle of the gauge is the slide, where change is fastest. The math is a floored parabola:
    //
    //   RateScale(n) = RateFloor + (1 − RateFloor) · 4·f·(1 − f),   f = n / Max
    //
    // 4·f·(1−f) is the classic logistic-shaped bump: 0 at both ends, exactly 1.0 at mid-gauge (f = ½). The
    // floor lifts the ends off zero so a captain can still (slowly) bottom out and still (slowly) climb back
    // — "slow at the ends", never frozen. Because the peak is pinned at 1.0, MID-GAUGE change is UNCHANGED
    // from the pre-#379 tuning: the owner's current feel is preserved exactly at n = 50, and only the ends
    // taper. Symmetric, so ONE shape serves both drain and recovery. DISCRETE reliefs (drink/pill/sleep) do
    // NOT ride this — they keep their flat, already-tuned magnitudes.

    /// <summary>How slowly the gauge moves at the very ends, as a fraction of its mid-gauge speed — the floor
    /// that keeps "slow at the ends" from becoming "frozen at the ends" (a captain must still be able to
    /// bottom out, and to climb back). FLAGGED for the owner's tuning.</summary>
    public const double RateFloor = 0.15;

    /// <summary>The S-curve rate multiplier for the current <paramref name="nerve"/>: a floored parabola that
    /// is <see cref="RateFloor"/> at both ends and exactly 1.0 at mid-gauge, so continuous change is slowest
    /// near full and near empty and fastest through the middle. Pure and in [<see cref="RateFloor"/>, 1].</summary>
    public static double RateScale(double nerve)
    {
        double f = Fraction(nerve);
        return RateFloor + ((1.0 - RateFloor) * 4.0 * f * (1.0 - f));
    }

    /// <summary>Apply <paramref name="dtSeconds"/> of a situation's drain to the current nerve, clamped to
    /// the gauge. The raw per-second rate is shaped by the <see cref="RateScale"/> S-curve at the current
    /// level (slow near the ends, fast mid-gauge). Pure — same nerve + same stressors + same dt always yields
    /// the same result.</summary>
    public static double Drain(double nerve, in Stressors s, double dtSeconds) =>
        Clamp(nerve - (DrainRatePerSecond(s) * RateScale(nerve) * System.Math.Max(0.0, dtSeconds)));

    /// <summary>The ease-off: return the nerve toward <see cref="Max"/> at <see cref="AboardRecoveryPerSecond"/>
    /// for <paramref name="dtSeconds"/>, shaped by the <see cref="RateScale"/> S-curve (a shattered captain
    /// mends slowly near the floor, a near-steady one settles gently near full), clamped. The safety the ship
    /// (or the airlock) provides.</summary>
    public static double Recover(double nerve, double dtSeconds) =>
        Clamp(nerve + (AboardRecoveryPerSecond * RateScale(nerve) * System.Math.Max(0.0, dtSeconds)));

    /// <summary>Deal a one-time lump shock (the monolith's first-sight hit, or a Reever's touch), clamped to
    /// the gauge. FLAT — a lump, not a rate: it bypasses the <see cref="RateScale"/> S-curve so a big
    /// one-time horror always lands at its full magnitude (owner's #19: touch "must always hurt").</summary>
    public static double Shock(double nerve, double amount) => Clamp(nerve - amount);

    // ── #379 · Diminishing SIGHTINGS (owner, Evening wind #18, 2026-07-19) ───────────────────────────
    //
    //   "seeing one reever after already seeing one more does not make you that much faster more nuts."
    //
    // A fresh contact cresting onto the tracker is a JOLT — but the shocks habituate over a watch. The FIRST
    // fresh sighting of a spell lands the full <see cref="SightingShock"/>; each subsequent fresh contact
    // within the same spell lands a <see cref="SightingDecay"/> fraction of the one before (so the whole
    // spell's jolts sum to at most SightingShock / (1 − SightingDecay), never a runaway flood). The tally
    // resets after the tracker has been genuinely QUIET for a while — a fresh spell starts fresh-frightened.
    // Each jolt still rides the <see cref="RateScale"/> S-curve, so a steady captain shrugs the first frights.
    //
    // This is the sighting counterpart to the tracker's first-contact chirp (MotionTracker.StepChirp): the
    // same 0→N edge and the same "clear for a while" hysteresis, but counting EVERY fresh contact of a spell
    // (not just the first) so the diminishing has something to count. Pure and seeded-free (a plain tally).

    /// <summary>The full nerve cost of the FIRST fresh sighting of a spell, before the per-spell diminishing
    /// and the S-curve. A discrete jolt, not a rate. FLAGGED for the owner's tuning.</summary>
    public const double SightingShock = 4.0;

    /// <summary>The per-spell diminishing factor: the Nth fresh sighting of a spell costs this fraction of the
    /// (N−1)th (so 1.0, 0.5, 0.25, … at 0.5). Below 1 so repeats soothe toward nothing; the whole spell sums
    /// to <see cref="SightingShock"/> / (1 − this). FLAGGED for the owner's tuning.</summary>
    public const double SightingDecay = 0.5;

    /// <summary>How long the tracker must be QUIET (no movers heard) before the sighting spell resets and the
    /// next fresh contact is a full fright again — the habituation's memory. Kin to
    /// <see cref="MotionTracker.ChirpReArmSeconds"/> (the chirp's re-arm), a touch longer so a brief lull does
    /// not wipe a watch's worth of steadying. FLAGGED for the owner's tuning.</summary>
    public const double SightingQuietResetSeconds = 6.0;

    /// <summary>The running state of a sighting SPELL (one excursion-watch's worth of frights): how many
    /// fresh contacts have been seen so far (<paramref name="Seen"/> — drives the diminishing), how many
    /// movers the tracker heard LAST frame (<paramref name="PrevMovers"/> — so a RISE is a fresh contact),
    /// and how long the fan has been quiet (<paramref name="QuietSeconds"/> — the reset timer). Pure.</summary>
    public readonly record struct SightingSpell(int Seen, int PrevMovers, double QuietSeconds)
    {
        /// <summary>A fresh watch: nothing seen, nothing heard, so the very first mover is a full fright.</summary>
        public static SightingSpell Fresh => new(0, 0, 0.0);
    }

    /// <summary>Advance the sighting spell one frame. <paramref name="movingContacts"/> is how many movers the
    /// tracker HEARS this frame (the same long-ear count that drives the chirp). Returns the next spell state
    /// and how many FRESH contacts crested THIS frame (a rise over last frame's count) — the client prices
    /// those through <see cref="SightingSeriesCost"/>. Sustained quiet (≥ <see cref="SightingQuietResetSeconds"/>)
    /// ends the spell, so the next fright is full again. Pure: same state + same count + same dt → same result.</summary>
    public static (SightingSpell Next, int FreshSightings) AdvanceSightings(
        SightingSpell prev, int movingContacts, double dtSeconds)
    {
        double dt = System.Math.Max(0.0, dtSeconds);
        if (movingContacts <= 0)
        {
            double quiet = prev.QuietSeconds + dt;
            // Once the fan has been clear long enough, the watch's habituation lapses — reset the tally.
            int seen = quiet >= SightingQuietResetSeconds ? 0 : prev.Seen;
            return (new SightingSpell(seen, 0, quiet), 0);
        }

        // Movers present: any rise over last frame's count is that many fresh contacts cresting the ear.
        int fresh = System.Math.Max(0, movingContacts - prev.PrevMovers);
        return (new SightingSpell(prev.Seen + fresh, movingContacts, 0.0), fresh);
    }

    /// <summary>The raw nerve cost (before the S-curve) of <paramref name="freshCount"/> fresh sightings that
    /// crest when <paramref name="priorSeen"/> have already been seen this spell: a geometric run of
    /// <see cref="SightingShock"/> · <see cref="SightingDecay"/>^k. The first fright of a fresh spell is full;
    /// the more a watch has already borne, the smaller each new one. Never negative.</summary>
    public static double SightingSeriesCost(int priorSeen, int freshCount)
    {
        if (freshCount <= 0 || priorSeen < 0)
        {
            return 0.0;
        }
        double first = SightingShock * System.Math.Pow(SightingDecay, priorSeen);
        // Geometric sum first·(1 + d + … + d^(fresh-1)); the d==1 branch guards the (1−d) divide.
        double run = SightingDecay >= 1.0
            ? first * freshCount
            : first * (1.0 - System.Math.Pow(SightingDecay, freshCount)) / (1.0 - SightingDecay);
        return run;
    }

    /// <summary>Apply the fresh sightings' jolt to the current nerve: the diminishing
    /// <see cref="SightingSeriesCost"/> shaped by the <see cref="RateScale"/> S-curve at the current level,
    /// clamped. Pure — the discrete-jolt counterpart of <see cref="Drain"/>.</summary>
    public static double SightingDrain(double nerve, int priorSeen, int freshCount) =>
        Clamp(nerve - (SightingSeriesCost(priorSeen, freshCount) * RateScale(nerve)));

    // ── A drink steadies the nerve (#308/#321 → #226): the NAMED SANITY-RELIEF SEAM, wired. ──────────
    //
    // Owner, live at a bar with a shot gauge (2026-07-18): "I need a drink to restore sanity?" — yes.
    // And the ruling that shapes the curve (same day, supersedes the flat first-draft amounts):
    //   "Drinking with somebody should restore sanity at any level (conversation + drink) but drinking
    //    alone only [helps a little] … you cannot drink yourself back from the edge alone."
    //
    // The law, in three layers, all pure and FLAGGED for the owner's tuning:
    //   1. TYPE. A drink SHARED with a contact (the #308 flow — conversation AND the glass) is the real
    //      medicine: a flat lump that lands at ANY nerve level, even nerves shot. A LONE drink (the bar
    //      house special, the galley tot) is weak medicine that WEAKENS as the nerve worsens — from the
    //      type's full value at steady hands down to a single point at the shot floor. Ordering in
    //      spirit stays tot < bar < shared (shared is now categorically different, not merely bigger).
    //   2. DIMINISHING REPEAT. Rounds in quick succession soothe less each time — the second half of the
    //      first — read straight off the EXISTING tilty-legs tot count (no parallel counter invented).
    //   3. DRUNK STOPS IT. Once the tot count reaches the deck's own tilty-legs threshold, a further
    //      drink restores NOTHING — "the rum has stopped helping, captain." Drunk is not sane.
    //
    // NOT here (still #226 proper): rest, sleep, and the full R&R economy — the deeper accelerants. This
    // seam is the DRINK only.

    /// <summary>The three ways a drink reaches the captain's nerve (#308/#321). The medicine differs by
    /// company, not just by price.</summary>
    public enum DrinkKind
    {
        /// <summary>A tot poured aboard from the galley locker — a lone drink, the weakest solo medicine.</summary>
        GalleyTot,

        /// <summary>The bar's house special, drunk alone at the counter — solo medicine, a touch stronger.</summary>
        BarSpecial,

        /// <summary>A glass shared with a contact across the table (#308) — conversation AND the drink,
        /// the real medicine that steadies the hands at ANY level.</summary>
        SharedWithContact,

        /// <summary>A calming pill from the ship's MED BAY (owner's Evening-wind ruling, 2026-07-18:
        /// "change one cabin into med bay where calming pills can be retrieved to help restore sanity to
        /// captain"). Not a drink, but it reaches the nerve through THIS same relief seam — reused, not
        /// parallelled. A pill rides no rum spree and never makes the deck tilty; its finite shipboard
        /// stock is the only limiter, so the client doses it as a single, un-diminished round (tot 1).</summary>
        CalmingPill,

        /// <summary>A good night's sleep in a cabin bunk (owner's live ruling 2026-07-19: "Let's have a
        /// sanity restoring sleep action in one of the cabins" — the REST half of Evening-wind #21). Not a
        /// drink either, but rest reaches the nerve through THIS same relief seam — reused, not parallelled.
        /// The biggest single restore there is (a whole night), flat and level-independent because real rest
        /// steadies the hands at any level; it rides no rum spree and never makes the deck tilty (tot 1). Its
        /// only limiter is honest tiredness — the WELL-RESTED satiety window <see cref="CabinComforts"/>
        /// owns, not drunkenness.</summary>
        Sleep,
    }

    /// <summary>Full restore of a lone galley tot at steady hands, before the level-curve and diminishing
    /// bite (FLAGGED for tuning).</summary>
    public const double GalleyTotBaseRestore = 10.0;

    /// <summary>Full restore of a lone bar house special at steady hands, before the level-curve and
    /// diminishing bite (FLAGGED for tuning).</summary>
    public const double BarSpecialBaseRestore = 18.0;

    /// <summary>A shared drink's restore — flat and level-independent: company steadies the hands even
    /// when nerves are shot (owner's trust-anthropology ruling; FLAGGED for tuning).</summary>
    public const double SharedDrinkRestore = 24.0;

    /// <summary>A calming pill's restore (owner 2026-07-18) — flat and level-independent like a shared
    /// drink, because real medicine steadies the hands even when nerves are shot; a touch stronger than a
    /// lone galley tot (10) and on a par with the bar's best pour, but flat rather than curved. Its finite
    /// shipboard stock, not drunkenness, bounds its use (FLAGGED for tuning).</summary>
    public const double CalmingPillRestore = 20.0;

    /// <summary>A night's sleep restore (owner 2026-07-19) — flat and level-independent like a shared drink
    /// or a pill, because real rest steadies the hands even when nerves are shot. The BIGGEST single restore
    /// there is — a whole night's bunk beats the bar's best pour (18) and the shared glass (24) — but honest:
    /// it does NOT full-heal a shot captain (0 → 40), and its WELL-RESTED satiety (CabinComforts) stops it
    /// being the grind. FLAGGED for the owner's tuning.</summary>
    public const double SleepRestore = 40.0;

    /// <summary>The single point a lone drink can still manage at the shot floor — you cannot drink your
    /// way back from the edge alone; you need a face across the table (owner: "moves the needle by one").</summary>
    public const double SoloFloorRestore = 1.0;

    /// <summary>The tot count (1-based, AFTER the pour is counted) at which drunkenness has set in and a
    /// further drink restores nothing. THE SAME tilty-legs threshold the deck's rum law uses (the third
    /// tot makes the deck tilty) — one drunkenness law, not a parallel one.</summary>
    public const int DrunkTotCount = 3;

    /// <summary>Whether this pour count is at/past the tilty-legs threshold — drunk, and past helping.</summary>
    public static bool DrunkAt(int totNumber) => totNumber >= DrunkTotCount;

    /// <summary>The diminishing-repeat factor for the Nth pour of a spree: the first soothes full, the
    /// second half, the drunk third-and-after nothing. Keyed off the existing tot count.</summary>
    public static double RepeatFactor(int totNumber) => totNumber switch
    {
        <= 1 => 1.0,
        2 => 0.5,
        _ => 0.0, // drunk — see DrunkAt
    };

    /// <summary>The solo weak-medicine curve: <see cref="SoloFloorRestore"/> at the shot floor, rising to
    /// <paramref name="full"/> at steady hands, linear in the current nerve fraction. A lone drink helps
    /// least exactly when the captain needs it most.</summary>
    private static double SoloCurve(double full, double nerve) =>
        SoloFloorRestore + (full - SoloFloorRestore) * Fraction(nerve);

    /// <summary>The base restore a drink offers at the given nerve, BEFORE diminishing-repeat: shared
    /// drinks are flat and level-independent; lone drinks ride the weak-medicine curve down toward a
    /// single point at the floor.</summary>
    public static double BaseRestoreAt(DrinkKind kind, double nerve) => kind switch
    {
        DrinkKind.SharedWithContact => SharedDrinkRestore,
        DrinkKind.CalmingPill => CalmingPillRestore, // flat, level-independent — medicine, not a mood
        DrinkKind.Sleep => SleepRestore,             // flat, level-independent — a whole night's rest
        DrinkKind.BarSpecial => SoloCurve(BarSpecialBaseRestore, nerve),
        DrinkKind.GalleyTot => SoloCurve(GalleyTotBaseRestore, nerve),
        _ => 0.0,
    };

    /// <summary>How much nerve this drink actually returns: the level-shaped base times the
    /// diminishing-repeat factor, and flat zero once drunk. Never negative.</summary>
    public static double RestoreAmount(DrinkKind kind, double nerve, int totNumber) =>
        DrunkAt(totNumber)
            ? 0.0
            : System.Math.Max(0.0, BaseRestoreAt(kind, nerve) * RepeatFactor(totNumber));

    /// <summary>Apply a drink's restore to the current nerve, clamped to the gauge — the seam the bar and
    /// the galley both call. Pure: same nerve + same drink + same tot count always yields the same rise.</summary>
    public static double DrinkRestore(double nerve, DrinkKind kind, int totNumber) =>
        Clamp(nerve + RestoreAmount(kind, nerve, totNumber));

    /// <summary>The in-voice steadying note for a drink's receipt/pulse line — the words the gauge's rise
    /// is spoken with. Drunk says the rum has stopped helping; a shared glass names the company; a lone
    /// drink that barely moves the needle at the edge admits it needs a face across the table.</summary>
    public static string SteadyingNote(DrinkKind kind, int totNumber, double restored)
    {
        if (kind == DrinkKind.CalmingPill)
        {
            // The med bay's voice (owner 2026-07-18): a pill has no drunk state and no rum spree — either
            // it takes hold, or the nerves were already too steady for it to register.
            return restored < 2.0
                ? "the nerves were already steady — the pill barely registers"
                : "the calming pill takes hold — the pulse slows, the hands go still";
        }
        if (kind == DrinkKind.Sleep)
        {
            // Rest's own voice (owner 2026-07-19) — no drunk state, no rum spree; CabinComforts usually
            // supplies the fuller bunk line, but this keeps the note voice-true wherever it's read.
            return restored < 2.0
                ? "you were already steady — the bunk barely changes a thing"
                : "a full bunk — you wake with the shakes gone and your hands your own again";
        }
        if (DrunkAt(totNumber))
        {
            return "the rum has stopped helping, captain — drunk is not steady hands";
        }
        if (restored < 2.0)
        {
            return kind == DrinkKind.SharedWithContact
                ? "the company helps more than the glass"
                : "barely a flicker — you'd need a face across the table for more";
        }
        return kind == DrinkKind.SharedWithContact
            ? "the company steadies the hands — the shakes ease"
            : "the hands remember how to be still";
    }

    /// <summary>Clamp any value into the gauge's [<see cref="Min"/>, <see cref="Max"/>] range.</summary>
    public static double Clamp(double nerve) => System.Math.Clamp(nerve, Min, Max);

    /// <summary>The gauge fill, 0..1, for the corner bar the client draws.</summary>
    public static double Fraction(double nerve) => Clamp(nerve) / Max;

    /// <summary>The escalating stress-reaction ladder, high nerve → low. The owner's table law: the
    /// transition should feel steppless — a good player slides from steady hands to a shaking one without
    /// a visible click. These are the DISPLAY rungs the flavor and the gauge colour key off.</summary>
    public enum NerveBand
    {
        /// <summary>Steady hands — nothing here they cannot believe.</summary>
        Steady,
        /// <summary>Rattled — a hitch in the breath, the first tell.</summary>
        Rattled,
        /// <summary>Shaken — a tremor in the glyph; the readout wavers.</summary>
        Shaken,
        /// <summary>Fraying — the hands won't hold still; the tell is plain now.</summary>
        Fraying,
        /// <summary>Shot — nerves gone. This slice only SAYS so; #226 owns what happens next.</summary>
        Shot,
    }

    /// <summary>Which rung of the ladder a nerve value stands on. The bands widen downward so the deep
    /// end (a captain in real trouble) reads distinctly.</summary>
    public static NerveBand BandFor(double nerve) => Clamp(nerve) switch
    {
        >= 75.0 => NerveBand.Steady,
        >= 50.0 => NerveBand.Rattled,
        >= 25.0 => NerveBand.Shaken,
        >= 10.0 => NerveBand.Fraying,
        _ => NerveBand.Shot,
    };

    // ── The one-per-frame state advance: the on-planet-only law, the airlock ease-off, the once-in-a-life
    //    monolith hit — all in one pure step the client calls each tick, and a test can pin exactly. ──

    /// <summary>One frame's worth of the captain's situation, as the client reads it off the live world.
    /// <paramref name="OnExcursion"/> is true whenever a surface excursion is live at all (the gauge shows
    /// only then — on-planet only). <paramref name="OnRegolith"/> is true only when actually out on the
    /// surface (false when stood back up through the airlock — the ship is safety). <paramref name="SeesMonolith"/>
    /// is whether the monolith is in sight THIS frame. Stressors are only consulted on the regolith.</summary>
    public readonly record struct Frame(
        bool OnExcursion, bool OnRegolith, bool SeesMonolith, Stressors Stressors, double DtSeconds);

    /// <summary>The result of advancing one frame: the new nerve, the (possibly newly-set) monolith-seen
    /// flag, whether the big first-sight hit fired THIS frame (so the client can sound the cue and speak),
    /// and whether the gauge should be visible (on-planet only).</summary>
    public readonly record struct Step(double Nerve, bool MonolithSeen, bool MonolithHitFired, bool GaugeVisible);

    /// <summary>Advance the nerve one frame. The whole on-planet law in one deterministic place:
    /// <list type="bullet">
    /// <item>the gauge is visible ONLY during an excursion (<see cref="Frame.OnExcursion"/>) — hidden aboard
    /// the ship in flight or docked;</item>
    /// <item>out on the regolith the stressors drain it, and the FIRST time the monolith comes into sight
    /// (once in a life — the flag latches) it takes the big lump;</item>
    /// <item>anywhere safe — up through the airlock mid-excursion, or off-planet entirely — the nerve eases
    /// back toward steady.</item>
    /// </list>
    /// Pure: same nerve + same seen-flag + same frame → same <see cref="Step"/>, every time.</summary>
    public static Step Advance(double nerve, bool monolithSeen, in Frame f)
    {
        double n = nerve;
        bool fired = false;

        if (f.OnExcursion && f.OnRegolith)
        {
            n = Drain(n, f.Stressors, f.DtSeconds);
            if (!monolithSeen && f.SeesMonolith)
            {
                monolithSeen = true;
                fired = true;
                n = Shock(n, MonolithSightShock);
            }
        }
        else
        {
            n = Recover(n, f.DtSeconds); // the ship is safety — the airlock ease-off, and off-planet calm
        }

        return new Step(n, monolithSeen, fired, GaugeVisible: f.OnExcursion);
    }

    /// <summary>The house-voice line for a rung — the escalating flavor the gauge reads out. No numbers,
    /// no mechanics; a mood that slides as the bar falls.</summary>
    public static string Flavor(NerveBand band) => band switch
    {
        NerveBand.Steady => "steady hands",
        NerveBand.Rattled => "a hitch in the breath",
        NerveBand.Shaken => "a tremor in the glyph",
        NerveBand.Fraying => "the hands won't hold still",
        NerveBand.Shot => "nerves shot — get aboard",
        _ => "",
    };

    /// <summary>The flavor for a nerve value directly — what the corner gauge writes beneath the bar.</summary>
    public static string Readout(double nerve) => Flavor(BandFor(nerve));
}
