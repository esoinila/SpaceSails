using System.Reflection;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #436 · SEEING YOU IS A ROLL (owner, live 2026-07-26: "There needs to be a reevers observation roll to its
/// line of sight environment… Then the moment reever discovers becomes special").
///
/// <para>These pin the pure rule: geometry is PERMISSION and never knowledge (no sightline, no die); the
/// one-way latch cannot be undone by arithmetic because a fixed contact is never rolled for again; the odds
/// move the right way in each of the three things they read, with a world at each end that can genuinely
/// tell pass from fail; the cadence casts exactly one die per look and phases the pack apart; and every face
/// replays from its seed.</para>
///
/// <para>And the line: ONE new authored sentence on this side, verbatim, with the reserved word (§8) absent
/// from every string the rule owns.</para>
///
/// <para><b>Every case here was proved RED</b>, by breaking the rule one clause at a time and watching the
/// named case go down (then restoring):</para>
/// <list type="bullet">
/// <item>the <c>!hasLineOfSight</c> arm deleted →
/// <c>WithNoSightline_NoDieIsCast_AndNothingIsStirred</c>;</item>
/// <item>the <c>alreadyFixed</c> arm deleted →
/// <c>AFixedContactIsNeverRolledForAgain_WithOrWithoutASightline</c>;</item>
/// <item>the range term inverted (near becomes the hard look) → four cases, led by
/// <c>TheOddsFallWithRange_AndBothEndsAreRealWorlds</c>;</item>
/// <item>every business modifier flattened to 0 → <c>TheOddsRiseWithBusiness_InTheEnumsOwnOrder</c> and the
/// range case's "both ends are real worlds" half;</item>
/// <item>the "still inside this look" early-out deleted → <c>ADieLeavesTheCupOncePerLook_NotOncePerFrame</c>
/// (600 dice where 13 are owed);</item>
/// <item><c>PhaseOf</c> returning a constant → <c>EachContactLooksOnItsOwnPhase</c>;</item>
/// <item><c>LookSeed</c> ignoring the contact → <c>TheSameLookReplaysExactly</c>;</item>
/// <item>the sentence reworded, a third string published, and the reserved word reached for → all three of
/// the line's cases at once.</item>
/// </list>
/// </summary>
public class ReeverObservationTests
{
    private const ulong Contact = 0x01D_0AE_5EEDUL;

    // A captain the odds can be measured against: mid-field, at a full walk, doing nothing in particular.
    private static ReeverObservation.View Walking(double rangeDu = 14.0) =>
        new(rangeDu, SuitAir.WalkSpeedDu, ReeverObservation.Doing.Nothing);

    // ── Geometry is permission, not knowledge ────────────────────────────────────────────────────────

    /// <summary>THE ROLL HAPPENS ONLY WHERE THE SIGHTLINE DOES. With stone between the two nothing is cast
    /// and nothing is stirred — which is also how the head goes back DOWN, and is the whole of the fear
    /// window. Asserted across a long span of sim seconds so it cannot be an accident of one instant.</summary>
    [Fact]
    public void WithNoSightline_NoDieIsCast_AndNothingIsStirred()
    {
        long carried = long.MinValue;
        for (int frame = 0; frame < 400; frame++)
        {
            ReeverObservation.Glance g = ReeverObservation.Look(
                hasLineOfSight: false, alreadyFixed: false, Contact, frame * 0.05, carried,
                // Point blank, at a run, under a muzzle flash: the view that CANNOT fail a roll. If a die
                // were being cast here at all this would fix on the very first call.
                new ReeverObservation.View(0.0, SuitAir.WalkSpeedDu, ReeverObservation.Doing.MuzzleFlash));
            carried = g.LookIndex;

            Assert.False(g.Rolled);
            Assert.Null(g.Roll);
            Assert.Equal(ReeverObservation.Watch.Unaware, g.State);
        }
    }

    /// <summary>…and with one, the head comes up on the very first call, before any die has decided
    /// anything. STIRRED is not a consolation prize for a failed roll: it is what having a sightline at all
    /// means.</summary>
    [Fact]
    public void WithASightline_TheHeadComesUpEvenWhenTheLookDoesNotFix()
    {
        // The far end of a long sightline, standing still, doing nothing: a view that cannot fix at all.
        var hopeless = new ReeverObservation.View(
            ReeverObservation.LongLookDu, 0.0, ReeverObservation.Doing.Nothing);
        Assert.Equal(0, ReeverObservation.ChanceIn20(hopeless));

        long carried = long.MinValue;
        for (int frame = 0; frame < 400; frame++)
        {
            ReeverObservation.Glance g = ReeverObservation.Look(
                hasLineOfSight: true, alreadyFixed: false, Contact, frame * 0.05, carried, hopeless);
            carried = g.LookIndex;
            Assert.Equal(ReeverObservation.Watch.Stirred, g.State);
        }
    }

    // ── The latch is one-way ─────────────────────────────────────────────────────────────────────────

    /// <summary>ONCE FIXED, FIXED FOR THE EXCURSION (canon, 2026-09-05). A contact that already has you is
    /// never rolled for again — not with the worst view in the game, not with no sightline at all — so
    /// there is no arithmetic anywhere in this rule that could un-fix it.</summary>
    [Fact]
    public void AFixedContactIsNeverRolledForAgain_WithOrWithoutASightline()
    {
        var hopeless = new ReeverObservation.View(
            ReeverObservation.LongLookDu, 0.0, ReeverObservation.Doing.Nothing);

        foreach (bool sight in new[] { true, false })
        {
            long carried = long.MinValue;
            for (int frame = 0; frame < 400; frame++)
            {
                ReeverObservation.Glance g = ReeverObservation.Look(
                    sight, alreadyFixed: true, Contact, frame * 0.05, carried, hopeless);
                carried = g.LookIndex;

                Assert.Equal(ReeverObservation.Watch.Fixed, g.State);
                Assert.False(g.Rolled);
            }
        }
    }

    // ── The odds move the right way, in each of the three things they read ───────────────────────────

    /// <summary>RANGE: the long look is the hard look. Monotone non-increasing over the whole span, and —
    /// the half that stops this being a threshold that selects everything — point blank is a certainty and
    /// the far end of a still captain's sightline is an impossibility, so the two ends of the sweep are
    /// worlds that genuinely tell pass from fail.</summary>
    [Fact]
    public void TheOddsFallWithRange_AndBothEndsAreRealWorlds()
    {
        int previous = 21;
        for (double range = 0; range <= ReeverObservation.LongLookDu + 20; range += 0.25)
        {
            int chance = ReeverObservation.ChanceIn20(Walking(range));
            Assert.True(chance <= previous,
                $"the odds rose with range at {range} du: {previous} → {chance} in 20.");
            previous = chance;
        }

        // A captain at a walk under somebody's nose cannot be missed…
        Assert.Equal(20, ReeverObservation.ChanceIn20(
            new ReeverObservation.View(0.0, SuitAir.WalkSpeedDu, ReeverObservation.Doing.MuzzleFlash)));
        // …and one standing still at the far end of the longest look on the ground cannot be found.
        Assert.Equal(0, ReeverObservation.ChanceIn20(
            new ReeverObservation.View(
                ReeverObservation.LongLookDu, 0.0, ReeverObservation.Doing.Nothing)));
        // The sweep is not flat: near really is better than far for the same captain.
        Assert.True(ReeverObservation.ChanceIn20(Walking(2.0)) > ReeverObservation.ChanceIn20(Walking(30.0)));
    }

    /// <summary>MOTION: a moving captain is a gift and standing still is cover — and the step is at
    /// <see cref="MotionTracker.StillSpeed"/>, the very threshold the motion fan reads, because the ground
    /// has ONE motion law and two instruments rather than two rules that agree today.</summary>
    [Fact]
    public void TheOddsRiseWithMotion_AtTheTrackersOwnThreshold()
    {
        int previous = -1;
        for (double speed = 0; speed <= SuitAir.WalkSpeedDu * 2; speed += 0.05)
        {
            int chance = ReeverObservation.ChanceIn20(
                new ReeverObservation.View(14.0, speed, ReeverObservation.Doing.Nothing));
            Assert.True(chance >= previous,
                $"the odds fell as the captain sped up at {speed} du/s: {previous} → {chance} in 20.");
            previous = chance;
        }

        // The step is the tracker's, not one this rule typed for itself.
        Assert.Equal(ReeverObservation.StillPenalty,
            ReeverObservation.MotionModifier(MotionTracker.StillSpeed));
        Assert.True(ReeverObservation.MotionModifier(MotionTracker.StillSpeed + 0.01)
            > ReeverObservation.MotionModifier(MotionTracker.StillSpeed));

        // And it decides real outcomes at a real range: standing still at 24 du is genuinely cover, and
        // walking at the same spot genuinely is not. A guard whose two worlds both answered the same thing
        // would be asserting nothing.
        Assert.Equal(0, ReeverObservation.ChanceIn20(
            new ReeverObservation.View(24.0, 0.0, ReeverObservation.Doing.Nothing)));
        Assert.True(ReeverObservation.ChanceIn20(
            new ReeverObservation.View(24.0, SuitAir.WalkSpeedDu, ReeverObservation.Doing.Nothing)) > 0);
    }

    /// <summary>BUSINESS: each thing the captain can be caught doing is more of a gift than the one before
    /// it, in the enum's own order — swept over the enum itself, so a value added later cannot quietly sit
    /// out of order.</summary>
    [Fact]
    public void TheOddsRiseWithBusiness_InTheEnumsOwnOrder()
    {
        ReeverObservation.Doing[] order = Enum.GetValues<ReeverObservation.Doing>()
            .OrderBy(d => (int)d).ToArray();

        int previous = -1;
        foreach (ReeverObservation.Doing doing in order)
        {
            int chance = ReeverObservation.ChanceIn20(new ReeverObservation.View(24.0, 0.0, doing));
            Assert.True(chance >= previous, $"{doing} is worth less to a watching eye than what precedes it.");
            previous = chance;
        }

        // Both ends are real: a captain standing still at 24 du is not seen doing nothing, and IS seen
        // digging there. Nothing about this guard passes if the business line is worth zero.
        Assert.Equal(0, ReeverObservation.ChanceIn20(
            new ReeverObservation.View(24.0, 0.0, ReeverObservation.Doing.Nothing)));
        Assert.True(ReeverObservation.ChanceIn20(
            new ReeverObservation.View(24.0, 0.0, ReeverObservation.Doing.Digging)) > 0);
    }

    /// <summary>The odds are the arithmetic the die is actually measured against — asserted by CASTING every
    /// face's worth of looks and counting, rather than by re-deriving the sum. A ChanceIn20 that agreed with
    /// itself and not with <see cref="ReeverObservation.Fixes"/> would be the green number that asserts
    /// nothing.</summary>
    [Theory]
    [InlineData(2.0, 0.0, ReeverObservation.Doing.Nothing)]
    [InlineData(14.0, SuitAir.WalkSpeedDu, ReeverObservation.Doing.Nothing)]
    [InlineData(24.0, 0.0, ReeverObservation.Doing.Digging)]
    [InlineData(28.0, SuitAir.WalkSpeedDu, ReeverObservation.Doing.Hauling)]
    public void TheStatedOddsAreTheOddsTheDiceActuallyPay(double range, double speed, ReeverObservation.Doing doing)
    {
        var view = new ReeverObservation.View(range, speed, doing);

        int fixes = 0;
        for (int face = 1; face <= DiceRule.D20; face++)
        {
            if (new DiceRoll("d20", face, ReeverObservation.ModifiersFor(view), 0UL).Total
                >= ReeverObservation.FixThreshold)
            {
                fixes++;
            }
        }

        Assert.Equal(fixes, ReeverObservation.ChanceIn20(view));
    }

    // ── The cadence ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>ONE DIE PER LOOK, however many frames the look spans. Sixty frames a second across ten
    /// seconds of unbroken sightline casts as many dice as there are look intervals in ten seconds — plus or
    /// minus the one the phase straddles — and not six hundred.</summary>
    [Fact]
    public void ADieLeavesTheCupOncePerLook_NotOncePerFrame()
    {
        var hopeless = new ReeverObservation.View(
            ReeverObservation.LongLookDu, 0.0, ReeverObservation.Doing.Nothing);

        const double seconds = 10.0;
        const double frame = 1.0 / 60.0;
        int cast = 0;
        long carried = long.MinValue;
        for (double t = 0; t < seconds; t += frame)
        {
            ReeverObservation.Glance g = ReeverObservation.Look(
                hasLineOfSight: true, alreadyFixed: false, Contact, t, carried, hopeless);
            carried = g.LookIndex;
            if (g.Rolled)
            {
                cast++;
            }
        }

        int looks = (int)(seconds / ReeverObservation.LookIntervalSeconds);
        Assert.InRange(cast, looks, looks + 1);
    }

    /// <summary>THE PACK DOES NOT BLINK IN UNISON. Each contact's looks fall at its own phase, deterministic
    /// per contact — so a field of them never fixes on the same instant, which is a drumbeat and is the one
    /// effect HullShudder teaches must only ever happen on purpose.</summary>
    [Fact]
    public void EachContactLooksOnItsOwnPhase()
    {
        var phases = new HashSet<double>();
        for (ulong contact = 1; contact <= 40; contact++)
        {
            double phase = ReeverObservation.PhaseOf(contact * 0xD1B54A32D192ED03UL);
            Assert.InRange(phase, 0.0, ReeverObservation.LookIntervalSeconds);
            phases.Add(phase);
        }
        Assert.Equal(40, phases.Count);
    }

    // ── Determinism ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>SAME SEED, SAME OUTCOME. Every face, every look index and every phase replays exactly —
    /// there is no clock and no <see cref="System.Random"/> anywhere under this, which is law in Core.</summary>
    [Fact]
    public void TheSameLookReplaysExactly()
    {
        for (long look = -3; look < 60; look++)
        {
            Assert.Equal(ReeverObservation.LookSeed(Contact, look), ReeverObservation.LookSeed(Contact, look));
            DiceRoll a = ReeverObservation.RollFor(ReeverObservation.LookSeed(Contact, look), Walking());
            DiceRoll b = ReeverObservation.RollFor(ReeverObservation.LookSeed(Contact, look), Walking());
            Assert.Equal(a.Face, b.Face);
            Assert.Equal(a.Total, b.Total);
            Assert.InRange(a.Face, 1, DiceRule.D20);
        }

        Assert.Equal(ReeverObservation.PhaseOf(Contact), ReeverObservation.PhaseOf(Contact));
        Assert.Equal(ReeverObservation.LookIndexAt(Contact, 12.5), ReeverObservation.LookIndexAt(Contact, 12.5));

        // Two contacts do not share a stream: the same look index off different seeds is not the same die.
        var faces = new HashSet<int>();
        for (ulong contact = 1; contact <= 60; contact++)
        {
            faces.Add(ReeverObservation.RollFor(ReeverObservation.LookSeed(contact, 7), Walking()).Face);
        }
        Assert.True(faces.Count > 1, "sixty contacts rolled the same face on the same look index.");
    }

    /// <summary>The look index only ever moves forward with the clock — a cadence that could go backwards
    /// would re-cast a look already taken, which is the one thing the carried index exists to prevent.</summary>
    [Fact]
    public void TheLookIndexNeverGoesBackwards()
    {
        long previous = long.MinValue;
        for (double t = 0; t < 120; t += 0.01)
        {
            long index = ReeverObservation.LookIndexAt(Contact, t);
            Assert.True(index >= previous, $"the look index fell at t = {t}.");
            previous = index;
        }
    }

    // ── The one authored line ────────────────────────────────────────────────────────────────────────

    /// <summary>THE LINE, VERBATIM. Fable canon 2026-09-05 — the only sentence this feature authors, and the
    /// only thing said out loud between the head coming up (drawn) and the coming (the walk).</summary>
    [Fact]
    public void TheLineIsTheOneThatWasAuthored()
    {
        Assert.Equal("One of them has stopped. It is looking at you.", ReeverObservation.FixedOnYouLine);
    }

    /// <summary>AND IT IS THE ONLY NEW STRING. A reflection sweep over everything the rule publishes: exactly
    /// one player-facing sentence, its glyph, and nothing else that could be mistaken for narration. The
    /// modifier stack's row labels are arithmetic (DiceRule's whole homage is that the math can be SHOWN) and
    /// are held to the same reserved-word law anyway.</summary>
    [Fact]
    public void TheAuthoredLineIsTheOnlyNewStringTheRulePublishes()
    {
        var published = typeof(ReeverObservation)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);

        Assert.Equal(
            new[] { nameof(ReeverObservation.FixedOnYouGlyph), nameof(ReeverObservation.FixedOnYouLine) },
            published.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
    }

    /// <summary>§8 · THE RESERVED WORD IS ABSENT, from the sentence and from every label the stack can put
    /// on screen beside it. Nothing this rule says names what they are, what left them there, or asks the
    /// question — the beat is a thing STOPPING, and that is all it is allowed to be.</summary>
    [Fact]
    public void NothingTheRuleSaysReachesForTheReservedWord()
    {
        string[] reserved =
        [
            "monolith", "not ours", "not natural", "no seam", "reever", "old one", "ancient", "alien",
            "whose", "who left", "who stacked",
        ];

        var said = new List<string> { ReeverObservation.FixedOnYouLine };
        foreach (ReeverObservation.Doing doing in Enum.GetValues<ReeverObservation.Doing>())
        {
            foreach (double range in new[] { 0.0, 14.0, 40.0 })
            {
                foreach (double speed in new[] { 0.0, SuitAir.WalkSpeedDu })
                {
                    said.AddRange(ReeverObservation.ModifiersFor(
                        new ReeverObservation.View(range, speed, doing)).Select(m => m.Label));
                }
            }
        }

        foreach (string text in said)
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
