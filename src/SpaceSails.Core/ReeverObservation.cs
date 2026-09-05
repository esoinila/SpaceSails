namespace SpaceSails.Core;

/// <summary>
/// #436 · SEEING YOU IS A ROLL. Owner, live 2026-07-26: <i>"There needs to be a reevers observation roll to
/// its line of sight environment. Make an issue about that. Then the moment reever discovers becomes special.
/// 🤭"</i>
///
/// <para><b>What was there before.</b> Sight was binary and instant: <see cref="SurfaceCollision"/> said
/// stone / no stone and the frame geometry allowed it, the Old One <b>knew</b> — the captain's live position
/// snapped into its memory and the latch flipped, silently. The 2026-08-02 story-QA run and the 2026-09-03
/// audit both landed on the same two findings: there is no per-frame dice churn to unpick (the latch is
/// already one-way and clean), and <b>the instant one of them sees you produces not one byte of output</b>.
/// So there are two things to build, and this file is the first: an unobstructed sightline becomes
/// <b>permission to roll</b> rather than knowledge.</para>
///
/// <para><b>The eye rolls; the ear does not.</b> The owner's own reasoning on the two doors into acquisition
/// (<see cref="ReeverHearing"/> grants a PLACE with no sightline involved at all): <i>"a noise is not a
/// maybe, it is a place."</i> So <see cref="MakeNoise"/>-style grants stay exactly as deterministic as they
/// were; only the EYE comes through here.</para>
///
/// <para><b>Three states, and only one of them latches.</b> UNAWARE → <see cref="Watch.Stirred"/> (stone has
/// stopped standing between you and the head has come up) → <see cref="Watch.Fixed"/> (it has you). Stirred
/// is the fear window and it does NOT latch: back behind stone and the head goes down again, which is the
/// whole of the play this issue is about. Fixed is one-way for the excursion — the 2026-09-05 canon pass is
/// explicit that the dread is that it does not hurry, and nothing here forgets.</para>
///
/// <para>Pure and fully deterministic: seeded off the ONE shared <see cref="DiceRule"/> (never
/// <see cref="System.Random"/>, never the clock — determinism is law in Core), the <see cref="HullShudder"/>
/// idiom. Given a contact's own seed, the sim second, and what the sightline actually affords, it answers
/// whether this contact takes a look at all, what the odds were, and what the look decided.</para>
/// </summary>
public static class ReeverObservation
{
    // ── The three states ──────────────────────────────────────────────────────────────────────────────

    /// <summary>What one Old One currently makes of the captain.</summary>
    public enum Watch
    {
        /// <summary>It does not know there is anybody out here. It keeps whatever ground it claimed and
        /// shivers on it — #446's feature, and the owner's ruling that it is a feature.</summary>
        Unaware = 0,

        /// <summary>Stone has stopped standing between you. The head is up and it is turned your way, and it
        /// has not committed: <b>backing behind stone now still works</b>. Nothing is said for this — the
        /// pose IS the beat (the 2026-09-05 canon pass: "the head coming up is drawn").</summary>
        Stirred = 1,

        /// <summary>It has you. One-way for the rest of the excursion, and the one moment in this whole rule
        /// that is spoken aloud.</summary>
        Fixed = 2,
    }

    /// <summary>What the captain is DOING while somebody looks at them. One value, the loudest thing about
    /// them this instant, because the odds want a single ordered signal rather than a stack of flags — and
    /// because the order below is itself the claim being made: each is more of a gift than the one before it,
    /// which is the property a guard can sweep.</summary>
    public enum Doing
    {
        /// <summary>Standing there in a grey suit on grey ground.</summary>
        Nothing = 0,

        /// <summary>A chest in your arms. Bigger, slower, and a silhouette that is no longer person-shaped —
        /// the reason <c>DropChest</c> exists as a panic verb at all.</summary>
        Hauling = 1,

        /// <summary>The shovel. A body bent over one spot, moving, and not stopping — the signature trade of
        /// the surface, and the thing the ear is already loudest about.</summary>
        Digging = 2,

        /// <summary>A deployed sentry firing beside you. <b>Light, on a ground that has none.</b> The eye's
        /// half of #456's own doctrine — your own guns are the loudest thing on the moon, and they are the
        /// brightest as well.</summary>
        MuzzleFlash = 3,
    }

    // ── The cadence: how often one contact takes a look, and why it is not every frame ────────────────

    /// <summary>
    /// How long one Old One's look lasts before it takes another (real seconds on the ground).
    ///
    /// <para><b>Why a cadence at all.</b> A roll per frame is not a rule, it is a coin flipped sixty times a
    /// second, and it would make a sightline of any length whatsoever equivalent to a certainty. The look is
    /// the unit: the head comes up, it looks, and what it decides stands until it looks again.</para>
    ///
    /// <para><b>Why this number.</b> Three quantities fix it, and none of them is taste. A captain crosses
    /// <see cref="SuitAir.WalkSpeedDu"/> deck units in a second, so at three quarters of a second a look is
    /// worth about seven deck units of travel — a dash between two slabs (well under one look) is usually not
    /// looked at at all, which is exactly the "flash of exposure" the issue asks to survive, while standing
    /// in the open at close range is two looks from being found. And it is comfortably longer than a frame
    /// and comfortably shorter than the arrival grace (<see cref="SurfaceArrival.SpotGraceSeconds"/>), so
    /// neither of the two clocks that already govern being noticed is redefined by it. FLAGGED for the
    /// owner's tuning — this is the "how long does a corner buy me" dial.</para>
    /// </summary>
    public const double LookIntervalSeconds = 0.75;

    /// <summary>
    /// Which look this contact is inside at <paramref name="simSeconds"/> — a monotonic index, and the "tick
    /// bucket" half of the seed. The caller rolls exactly when this number CHANGES, so a look is taken once
    /// however many frames it spans and however many times it is asked.
    ///
    /// <para><b>The phase is per contact, and that is the point.</b> Without it every eye on the field would
    /// blink on the same instant — a pack that fixes in unison is a drumbeat, and the one thing
    /// <see cref="HullShudder"/> teaches about unison is that it is a deliberate effect and must never happen
    /// by accident. Seeded off the contact's own seed, so it is fixed for the life of that contact and
    /// replays exactly in a test.</para>
    /// </summary>
    public static long LookIndexAt(ulong contactSeed, double simSeconds)
    {
        if (double.IsNaN(simSeconds) || double.IsInfinity(simSeconds))
        {
            return long.MinValue;
        }
        return (long)System.Math.Floor((simSeconds + PhaseOf(contactSeed)) / LookIntervalSeconds);
    }

    /// <summary>Where in its own look interval this contact's looks fall, in seconds — deterministic per
    /// contact, uniform over <see cref="LookIntervalSeconds"/>.</summary>
    public static double PhaseOf(ulong contactSeed) =>
        new DeterministicRandom(DiceRule.Seed(contactSeed, "look:phase")).NextDouble() * LookIntervalSeconds;

    /// <summary>The seed for one look: the contact's own stable seed folded with the look index, through the
    /// one <see cref="DiceRule.Seed(ulong, string)"/> mixer every other consequence system uses. Same seed,
    /// same face, on every runtime — so a look can be re-cast in a test from the exact moment it happened.
    ///
    /// <para><b>The contact's own seed, and never its index in a list.</b> The list of Old Ones is mutated
    /// under the sim — the sentry volley drops downed entries and the tide appends — so an index is not an
    /// identity: seeding on one would re-seed every contact standing behind a downed one and re-cast looks
    /// that had already been taken. That is this project's fourth named bug class (one source consumed in the
    /// wrong order) wearing a different coat. The contact's own seed already folds the site and the contact
    /// together and never moves.</para></summary>
    public static ulong LookSeed(ulong contactSeed, long lookIndex) =>
        DiceRule.Seed($"look:{lookIndex}", unchecked((long)contactSeed));

    // ── The odds: what the sightline actually affords ─────────────────────────────────────────────────

    /// <summary>The number a look has to reach on the house d20 before it fixes. A hard-ish tabletop DC,
    /// chosen against the two ends of the modifier stack rather than for its own sake: at point-blank with a
    /// captain at a walk under a firing gun the look cannot fail, and at the far end of a long sightline with
    /// a captain standing still and doing nothing it cannot succeed. Both of those are properties a guard
    /// asserts, and both are the point.</summary>
    public const int FixThreshold = 14;

    /// <summary>Inside this, the look is the easy one. <see cref="NerveModel.DreadFullRangeDeckUnits"/> —
    /// the range at which Core already says an Old One is ON TOP of you, borrowed rather than re-typed so
    /// two rules cannot disagree about what "close" is on the same ground.</summary>
    public const double PointBlankDu = NerveModel.DreadFullRangeDeckUnits;

    /// <summary>And beyond this the look is as hard as it gets. The distance a gunshot carries
    /// (<see cref="ReeverHearing.Noise.Gunfire"/>) — "half the field", the game's own statement of the
    /// longest span on the ground that anything is expected to notice anything across.</summary>
    public static double LongLookDu => ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire);

    /// <summary>What being at or inside <see cref="PointBlankDu"/> is worth.</summary>
    public const int CloseBonus = 6;

    /// <summary>…and what being at or beyond <see cref="LongLookDu"/> costs. The long look is the hard look.</summary>
    public const int LongPenalty = -6;

    /// <summary>
    /// What standing still is worth, and it is the biggest single number in the stack.
    ///
    /// <para><b>YOU HIDE FROM THEM THE WAY THEY HIDE FROM YOU.</b> The motion tracker has always been
    /// motion-only: a contact whose speed falls under <see cref="MotionTracker.StillSpeed"/> drops off the
    /// fan entirely — not mostly, entirely. This is that same law pointed the other way, at the same
    /// threshold, so the ground has ONE motion law read by two instruments instead of two rules that happen
    /// to agree today.</para>
    /// </summary>
    public const int StillPenalty = -6;

    /// <summary>What being at a full walk is worth. A moving captain is a gift, and it is the only signal in
    /// the stack the captain can switch off at no cost but time.</summary>
    public const int MovingBonus = 4;

    /// <summary>What the sightline affords at the instant of one look. Everything the odds read, and nothing
    /// else — no site, no clock, no identity.</summary>
    /// <param name="RangeDu">How far the captain is, in deck units.</param>
    /// <param name="CaptainSpeedDu">How fast the captain is moving, in deck units per second — measured the
    /// same way a contact's own velocity is, so the eye and the fan read one number.</param>
    /// <param name="Doing">The loudest thing about the captain this instant.</param>
    public readonly record struct View(double RangeDu, double CaptainSpeedDu, Doing Doing);

    /// <summary>The range line of the stack: <see cref="CloseBonus"/> at or inside
    /// <see cref="PointBlankDu"/>, falling straight to <see cref="LongPenalty"/> at
    /// <see cref="LongLookDu"/> and holding there. Monotone non-increasing in range, by construction.</summary>
    public static int RangeModifier(double rangeDu)
    {
        if (double.IsNaN(rangeDu))
        {
            return LongPenalty;
        }
        double t = System.Math.Clamp((rangeDu - PointBlankDu) / (LongLookDu - PointBlankDu), 0, 1);
        return (int)System.Math.Round(CloseBonus + ((LongPenalty - CloseBonus) * t),
            System.MidpointRounding.AwayFromZero);
    }

    /// <summary>The motion line: <see cref="StillPenalty"/> at or under the tracker's own still threshold,
    /// then rising from nothing to <see cref="MovingBonus"/> at a full walk. Monotone non-decreasing in
    /// speed. The step at the threshold is deliberate and is the same step
    /// <see cref="MotionTracker.IsMoving"/> makes — stopping is a decisive act on both instruments or it is
    /// a decisive act on neither.</summary>
    public static int MotionModifier(double captainSpeedDu)
    {
        if (double.IsNaN(captainSpeedDu) || captainSpeedDu <= MotionTracker.StillSpeed)
        {
            return StillPenalty;
        }
        double t = System.Math.Clamp(
            (captainSpeedDu - MotionTracker.StillSpeed) / (SuitAir.WalkSpeedDu - MotionTracker.StillSpeed), 0, 1);
        return (int)System.Math.Round(MovingBonus * t, System.MidpointRounding.AwayFromZero);
    }

    /// <summary>The business line. Small integers, in the enum's own order — never OP (owner: "dice
    /// modifiers, never OP"), and monotone non-decreasing in <see cref="Doing"/> by construction.</summary>
    public static int BusinessModifier(Doing doing) => doing switch
    {
        Doing.Hauling => 1,
        Doing.Digging => 3,
        Doing.MuzzleFlash => 5,
        _ => 0,
    };

    /// <summary>The whole stack, named, in the order the UI would read it out — the homage's own rule that
    /// the player can be shown the math.</summary>
    public static IReadOnlyList<DiceModifier> ModifiersFor(in View view)
    {
        var stack = new List<DiceModifier>(3)
        {
            new(RangeLabel(view.RangeDu), RangeModifier(view.RangeDu)),
            new(MotionLabel(view.CaptainSpeedDu), MotionModifier(view.CaptainSpeedDu)),
        };
        if (BusinessModifier(view.Doing) != 0)
        {
            // A captain doing nothing gets no row at all rather than a row reading zero: the stack is what
            // the look actually weighed, and "nothing" is not a thing that was weighed.
            stack.Add(new DiceModifier(BusinessLabel(view.Doing), BusinessModifier(view.Doing)));
        }
        return stack;
    }

    /// <summary>How many of the twenty faces would fix, for this view — the odds, as a plain number a guard
    /// can be monotone about without casting a single die. 0 means it cannot happen at all; 20 means it
    /// cannot fail.</summary>
    public static int ChanceIn20(in View view)
    {
        int need = FixThreshold - RangeModifier(view.RangeDu)
                   - MotionModifier(view.CaptainSpeedDu) - BusinessModifier(view.Doing);
        return System.Math.Clamp(21 - need, 0, 20);
    }

    /// <summary>Cast one look's die, with the named stack on it.</summary>
    public static DiceRoll RollFor(ulong lookSeed, in View view) =>
        DiceRule.Roll(lookSeed, DiceRule.D20, ModifiersFor(view));

    /// <summary>Did this look fix? The total against <see cref="FixThreshold"/> — no natural-anything
    /// override, because the two ends of the stack are meant to be genuinely reachable: point-blank under a
    /// muzzle flash IS a certainty, and a captain frozen at the far end of a long sightline IS cover.</summary>
    public static bool Fixes(in DiceRoll roll) => roll.Total >= FixThreshold;

    // ── The whole rule, in one call ───────────────────────────────────────────────────────────────────

    /// <summary>What one look came to. <see cref="Roll"/> is null when no die was cast — which is most
    /// frames, and is the whole reason the cadence exists.</summary>
    /// <param name="State">What this contact now makes of the captain.</param>
    /// <param name="LookIndex">The look index to carry forward, so the next call knows whether the look has
    /// turned over.</param>
    /// <param name="Roll">The die this look cast, or null if it did not cast one.</param>
    public readonly record struct Glance(Watch State, long LookIndex, DiceRoll? Roll)
    {
        /// <summary>Did a die actually leave the cup this call?</summary>
        public bool Rolled => Roll is not null;

        /// <summary>Is it on you now?</summary>
        public bool IsFixed => State == Watch.Fixed;
    }

    /// <summary>
    /// One contact's look at the captain, this instant.
    ///
    /// <para><b>Geometry is permission, not knowledge.</b> With no sightline nothing is rolled and nothing is
    /// stirred — the head goes back down and stone is cover again, exactly as the issue asks. With a
    /// sightline the head comes up (<see cref="Watch.Stirred"/>) and a die is cast at most once per
    /// <see cref="LookIntervalSeconds"/>.</para>
    ///
    /// <para><b>An already-fixed contact never rolls again.</b> The latch is one-way by canon, and a rule
    /// that kept rolling for a contact that already has you would be free to un-fix it by arithmetic. It
    /// cannot, because no die is cast.</para>
    /// </summary>
    /// <param name="hasLineOfSight">Whether stone (and, aboard, a shut door) stands between the two — and
    /// whether the arrival grace has run. Both are the caller's to answer; this rule never re-decides them.</param>
    /// <param name="alreadyFixed">Whether this contact has already fixed, by this rule or by the ear.</param>
    /// <param name="contactSeed">The contact's own stable seed.</param>
    /// <param name="simSeconds">The sim second the look is taken at.</param>
    /// <param name="lastLookIndex">The index this contact carried out of its previous call.</param>
    /// <param name="view">What the sightline affords right now.</param>
    public static Glance Look(
        bool hasLineOfSight,
        bool alreadyFixed,
        ulong contactSeed,
        double simSeconds,
        long lastLookIndex,
        in View view)
    {
        if (alreadyFixed)
        {
            // It has you already. Nothing is cast, and nothing this rule does can take that back.
            return new Glance(Watch.Fixed, lastLookIndex, null);
        }
        if (!hasLineOfSight)
        {
            // Stone, a shut door, or the grace the landing bought. The head goes down.
            return new Glance(Watch.Unaware, lastLookIndex, null);
        }

        long index = LookIndexAt(contactSeed, simSeconds);
        if (index == lastLookIndex)
        {
            // Still inside the look it already took. Head up, no die.
            return new Glance(Watch.Stirred, lastLookIndex, null);
        }

        DiceRoll roll = RollFor(LookSeed(contactSeed, index), view);
        return new Glance(Fixes(roll) ? Watch.Fixed : Watch.Stirred, index, roll);
    }

    // ── The one authored line ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #436 · <b>THE ONE LINE THIS FEATURE AUTHORS</b>, spoken on the suit's pulse at the moment an Old One
    /// fixes — the beat the 2026-08-02 story-QA run found was worth building even without the dice, because
    /// the instant one of them saw you used to produce not one byte of output.
    ///
    /// <para>Fable canon, 2026-09-05, verbatim and the only new string on the eye's side. It names nothing —
    /// not what they are, not how many, not what happens next — because everything it could name is either
    /// already on the tracker or is the walk that follows. "Has stopped" is the sim's own truth (the shiver
    /// ends and the shamble begins), and the dread the canon pass asks for is that it does not hurry.</para>
    ///
    /// <para>Said ONCE per excursion, by the caller: the latch is one-way, so after the first fix the ground
    /// is never clean again and a second announcement would be reporting old news over the sound of the
    /// thing actually coming.</para>
    /// </summary>
    public const string FixedOnYouLine = "One of them has stopped. It is looking at you.";

    /// <summary>The glyph the pulse carries it on — the eye that has just opened, and the same pen every
    /// other watched-from-somewhere beat on this ground uses.</summary>
    public const string FixedOnYouGlyph = "👁";

    // ── Labels ────────────────────────────────────────────────────────────────────────────────────────
    //
    // These are the modifier stack's own row names, not narration: they exist so the math can be SHOWN
    // (DiceRule's whole homage) and so a log line about a look reads as arithmetic rather than as prose.

    private static string RangeLabel(double rangeDu) =>
        rangeDu <= PointBlankDu ? "close" : rangeDu >= LongLookDu ? "a long look" : "range";

    private static string MotionLabel(double captainSpeedDu) =>
        double.IsNaN(captainSpeedDu) || captainSpeedDu <= MotionTracker.StillSpeed ? "standing still" : "moving";

    private static string BusinessLabel(Doing doing) => doing switch
    {
        Doing.Hauling => "carrying a chest",
        Doing.Digging => "digging",
        Doing.MuzzleFlash => "muzzle flash",
        _ => "",
    };
}
