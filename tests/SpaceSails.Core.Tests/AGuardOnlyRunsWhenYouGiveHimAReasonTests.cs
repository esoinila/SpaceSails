using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #835 · THE LAW THAT WAS REVERSED, AND EXACTLY HOW MUCH OF IT WAS.
///
/// <para>Owner, evening playtest 2026-08-11: <i>"they need to catch us .... like reevers :-D we could use
/// that code :-D"</i> — reversing his own standing law, which <c>PatrolBeat</c>'s header had carried since
/// #804: <i>"a rolling guard has no reason to run after anyone just on sight."</i></para>
///
/// <para><b>The half of that law which survives is the half this file is mostly about.</b> A round that
/// merely sees somebody still hails, still walks over, still reads, and still at worst walks you back to the
/// car. Pursuit is a BRANCH with exactly three doors into it and every door is a thing the captain did. The
/// tests below pin the doors, the numbers on the run, and the one thing the owner was most explicit about:
/// <i>"just no damage by default :-D"</i> — a catch is a hand on your arm and there is nothing in the
/// consequence that a body would notice.</para>
///
/// <para>The rules live here; the walking, the A*, the collision and the page's own wiring are
/// <c>TheGuardsCatchYouTests</c>'s, one project along, for the split <c>TheRoundsOnTheRestrictedFloors</c>
/// already keeps: this file owns everything that is a rule rather than a room.</para>
/// </summary>
public sealed class AGuardOnlyRunsWhenYouGiveHimAReasonTests
{
    private const string Plate = "◈ A CONTRACT GUARD, WALKING THE ROUND";

    // ── WHAT MAY START ONE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// AMBIENT ROUNDS NEVER RUN, and it is a fact about an enum rather than a clause somebody remembered.
    /// There is no member of <see cref="PatrolBeat.Provocation"/> that means "he saw you", because seeing you
    /// is what buys a hail; and <see cref="PatrolBeat.EarnsIt"/> is the single gate every road has to come
    /// through.
    /// </summary>
    [Fact]
    public void NothingButAThingYouDidEverStartsARun()
    {
        // The catalog is what it says it is — a sweep over a list nobody pinned is a green test that asserts
        // nothing, and this list growing quietly is precisely how "earned" would rot into "ambient".
        PatrolBeat.Provocation[] all = Enum.GetValues<PatrolBeat.Provocation>();
        Assert.Equal(4, all.Length);

        Assert.False(PatrolBeat.EarnsIt(PatrolBeat.Provocation.None),
            "the default earned a run, which is the law this issue did NOT reverse.");
        Assert.Equal(0, (int)PatrolBeat.Provocation.None);

        foreach (PatrolBeat.Provocation why in all)
        {
            Assert.Equal(why != PatrolBeat.Provocation.None, PatrolBeat.EarnsIt(why));
        }

        // …and every one of the three names a thing the captain chose to do.
        Assert.Contains(PatrolBeat.Provocation.WalkedAwayTwice, all);
        Assert.Contains(PatrolBeat.Provocation.SeenAtTheHasp, all);
        Assert.Contains(PatrolBeat.Provocation.BookedTooManyTimes, all);
    }

    /// <summary>
    /// THE FIRST WALK-AWAY IS FREE AND STAYS FREE. #833's approach is only a decision if walking off it is a
    /// real option; a man who followed you the first time would make the whole beat a trap.
    /// </summary>
    [Fact]
    public void WalkingOffOnceIsAllowedAndTwiceIsNot()
    {
        Assert.Equal(1, PatrolBeat.HailsYouMayWalkAwayFrom);

        Assert.False(PatrolBeat.WalkingOffEarnsIt(0));
        Assert.False(PatrolBeat.WalkingOffEarnsIt(PatrolBeat.HailsYouMayWalkAwayFrom));
        Assert.True(PatrolBeat.WalkingOffEarnsIt(PatrolBeat.HailsYouMayWalkAwayFrom + 1));
        Assert.True(PatrolBeat.WalkingOffEarnsIt(PatrolBeat.HailsYouMayWalkAwayFrom + 9));
    }

    /// <summary>
    /// THE FOURTH TIME IS THE DIFFERENT ONE. Owner's own evening: four challenges from one man on one floor,
    /// and <i>the fiction strains when the same guard books the same face four times and just keeps
    /// walking.</i> Three is what a watch will write down and still walk you to the lift.
    /// </summary>
    [Fact]
    public void AWatchWritesYouDownThreeTimesAndThenStopsBeingPatient()
    {
        Assert.Equal(3, PatrolBeat.EscortsAWatchAllows);

        for (int already = 0; already < PatrolBeat.EscortsAWatchAllows; already++)
        {
            Assert.False(PatrolBeat.BookedTooOften(already),
                $"{already} escort(s) already this watch was treated as too many.");
        }
        Assert.True(PatrolBeat.BookedTooOften(PatrolBeat.EscortsAWatchAllows));
        Assert.True(PatrolBeat.BookedTooOften(PatrolBeat.EscortsAWatchAllows + 5));

        // …and ONE number governs both halves of the ladder. The stop that stops being an escort is the stop
        // that becomes the way out of the building; two thresholds would be two things to drift apart.
        Assert.Equal(
            PatrolBeat.BookedTooOften(PatrolBeat.EscortsAWatchAllows),
            PatrolBeat.TheGuardHasYou(Plate, PatrolBeat.Provocation.SeenAtTheHasp,
                PatrolBeat.EscortsAWatchAllows).Consequence == PatrolBeat.KickOutDueLine);
    }

    // ── THE RUN ITSELF ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE IS QUICKER THAN HIS ROUND AND SLOWER THAN YOU — which is not a compromise, it is rung five. A
    /// captain who runs for the lift gets to the lift; he catches people who hesitate and people who are
    /// cornered.
    /// </summary>
    [Fact]
    public void TheNumbersOnARunLeaveTheEscapeIntact()
    {
        Assert.True(PatrolBeat.AfterYouSpeed > PatrolBeat.WalkSpeed,
            "a run at the round's own pace is not a run.");

        // The captain's own legs on the deck are 9.0 du/s (Map.Deck's AvatarSpeed, which Core cannot see —
        // the client's own sweep walks a captain away from one and proves the gap is real). A guard who could
        // outpace that would make "outrun him to the car" a sentence with no keys behind it.
        Assert.True(PatrolBeat.AfterYouSpeed < 9.0,
            $"a guard at {PatrolBeat.AfterYouSpeed} du/s can run a captain down in open corridor.");

        // The radio is a real beat and a short one — it is the warning, and a paragraph of it would be a
        // free head start rather than a warning.
        Assert.True(PatrolBeat.CallItInSeconds > 0.5 && PatrolBeat.CallItInSeconds < 3.0);

        // He gives up somewhere he can no longer see you, and inside a minute.
        Assert.True(PatrolBeat.LosesYouBeyondDu > PatrolBeat.GivesUpBeyondDu,
            "he gives up on a run sooner than he gives up on a walk-up, which is backwards.");
        Assert.True(PatrolBeat.LosesYouBeyondDu <= PatrolBeat.MarkerSightDu);
        Assert.True(PatrolBeat.AfterYouSecondsCap > PatrolBeat.WalkUpSeconds);
        Assert.True(PatrolBeat.AfterYouSecondsCap < 60.0, "he is a retired cop, not a wolf.");
    }

    /// <summary>
    /// A RUN ALWAYS ENDS, AND IT ENDS BY SOMETHING HAPPENING. Same shape as
    /// <see cref="PatrolBeat.StillComing"/> so the walk-up and the run cannot grow different edge cases.
    /// </summary>
    [Fact]
    public void HeStopsWhenYouAreAwayAndWhenHisWindIsGone()
    {
        // Standing at the reach he called it in from: still coming.
        Assert.True(PatrolBeat.StillAfterYou(0.0, 0, 0, PatrolBeat.NoticeDu, 0));
        Assert.True(PatrolBeat.StillAfterYou(
            PatrolBeat.AfterYouSecondsCap - 0.1, 0, 0, PatrolBeat.LosesYouBeyondDu - 0.01, 0));

        // A corridor ahead of him, or forty seconds of it: he is done, and he is still standing there.
        Assert.False(PatrolBeat.StillAfterYou(0.0, 0, 0, PatrolBeat.LosesYouBeyondDu + 0.01, 0));
        Assert.False(PatrolBeat.StillAfterYou(PatrolBeat.AfterYouSecondsCap + 0.01, 0, 0, 0.5, 0));
        Assert.False(PatrolBeat.StillAfterYou(double.NaN, 0, 0, 0.5, 0));
    }

    /// <summary>
    /// A CATCH IS <see cref="ReeverChase.Caught"/>, DOWN TO THE RADIUS. The owner named that code and this is
    /// it: #469 already settled what touching means on this ground, and a second contact law for the same
    /// event is exactly the bug that issue was filed about.
    /// </summary>
    [Fact]
    public void TheCatchIsTheOldOnesOwnGeometry()
    {
        foreach ((double x, double y) in new[] { (0.0, 0.0), (13.5, -40.25), (-7.0, 3.0) })
        {
            for (double gap = 0; gap < 4.0; gap += 0.1)
            {
                Assert.Equal(
                    ReeverChase.Caught(x, y, x + gap, y),
                    PatrolBeat.HasYou(x, y, x + gap, y));
            }
        }

        Assert.True(PatrolBeat.HasYou(0, 0, ReeverChase.CatchRadius - 0.01, 0));
        Assert.False(PatrolBeat.HasYou(0, 0, ReeverChase.CatchRadius + 0.01, 0));
    }

    /// <summary>
    /// #835 · THE OLD ONES' OWN STEP IS UNTOUCHED. The gait is an optional parameter with the stagger as its
    /// default, so every existing caller — the pack, the tide, the raid — steps exactly as it did before this
    /// branch existed. Owner's ruling, and it is a ruling about a different feature entirely: <i>"Lets not
    /// help reevers move in any easier if possible."</i>
    /// </summary>
    [Fact]
    public void GivingAGuardPersonsLegsDidNotGiveTheOldOnesAny()
    {
        // A wall across the run, with a doorway-sized gap in it a hand's width off the line of travel — the
        // shape #724's funnel is about, and therefore the shape the two gaits answer differently.
        var wall = new List<SurfaceCollision.Segment>
        {
            new(-20, 4, -0.9, 4),
            new(0.9, 4, 20, 4),
        };

        for (double offset = -1.6; offset <= 1.6; offset += 0.2)
        {
            (double sx, double sy) = (offset, 0.0);
            (double defaultX, double defaultY) =
                ReeverChase.Step(sx, sy, 0, 12, 0.6, double.PositiveInfinity, wall, 0.7);
            (double staggerX, double staggerY) = ReeverChase.Step(
                sx, sy, 0, 12, 0.6, double.PositiveInfinity, wall, 0.7,
                gait: SurfaceCollision.Gait.Stagger);

            Assert.Equal(staggerX, defaultX, 12);
            Assert.Equal(staggerY, defaultY, 12);
        }
    }

    // ── BEING CAUGHT ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CATCH CARD SAYS WHY, AND WHAT IT COSTS, AND THE COST IS NEVER A BODY. Same two-armed
    /// <see cref="PatrolBeat.Read"/> the wallet's own challenge is told on, so the client raises the identical
    /// card and neither can grow a presentation of its own (#684 / #736).
    /// </summary>
    [Fact]
    public void EveryCatchNamesItsReasonAndEndsInTheLadder()
    {
        foreach (PatrolBeat.Provocation why in Enum.GetValues<PatrolBeat.Provocation>())
        {
            if (!PatrolBeat.EarnsIt(why))
            {
                continue;
            }

            PatrolBeat.Read held = PatrolBeat.TheGuardHasYou(Plate, why, 0);

            Assert.False(held.Satisfied, "being caught satisfied somebody.");
            Assert.Equal(PatrolBeat.CaughtLabel, held.Label);
            Assert.Equal(PatrolBeat.WhyHeCame(why), held.Line);
            Assert.Contains(Plate, held.Card, StringComparison.Ordinal);

            // #603's law: the consequence is named, and it is ON the card under the reason (#736).
            Assert.Equal(PatrolBeat.EscortLine, held.Consequence);
            Assert.StartsWith(held.Line, held.Told, StringComparison.Ordinal);
            Assert.Contains(PatrolBeat.EscortLine, held.Told, StringComparison.Ordinal);

            // …and the three reasons are three sentences. One line reused would be a ladder with one rung.
            foreach (PatrolBeat.Provocation other in Enum.GetValues<PatrolBeat.Provocation>())
            {
                if (other != why && PatrolBeat.EarnsIt(other))
                {
                    Assert.NotEqual(PatrolBeat.WhyHeCame(other), held.Line);
                }
            }
        }
    }

    /// <summary>Past the threshold the same card says the walk goes somewhere else, and it says it in the
    /// place the captain acts on (#736). The number in the man's own sentence is the constant, so he and the
    /// rule can never be counting different evenings.</summary>
    [Fact]
    public void PastThePatienceTheCardSaysHeIsNotPressingYourFloor()
    {
        PatrolBeat.Read up = PatrolBeat.TheGuardHasYou(
            Plate, PatrolBeat.Provocation.BookedTooManyTimes, PatrolBeat.EscortsAWatchAllows);

        Assert.Equal(PatrolBeat.KickOutDueLine, up.Consequence);
        Assert.Contains(PatrolBeat.KickOutDueLine, up.Told, StringComparison.Ordinal);
        Assert.DoesNotContain(PatrolBeat.EscortLine, up.Told, StringComparison.Ordinal);

        Assert.Contains(
            (PatrolBeat.EscortsAWatchAllows + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            PatrolBeat.WhyHeCame(PatrolBeat.Provocation.BookedTooManyTimes),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// ZERO DAMAGE, AS A FACT ABOUT THE WHOLE FEATURE. Owner, verbatim: <i>"just no damage by default :-D"</i>
    /// — so no sentence this file owns describes an injury, and there is no arm of any read that hands back
    /// anything but the two rungs of the ladder. (The absence of the mechanic itself is pinned in the client,
    /// where the mechanic would have to live: there is no <c>HitsTaken</c> in <c>Map.Patrol.cs</c>.)
    /// </summary>
    [Fact]
    public void NothingThatHappensToYouHappensToYourBody()
    {
        string[] injuries =
        [
            "blood", "bleed", "bruis", "broke", "broken", "punch", "hit you", "struck", "baton",
            "cuff", "beat", "wound", "hurt",
        ];

        foreach (string line in PatrolBeat.AllProse())
        {
            foreach (string word in injuries)
            {
                Assert.False(
                    line.Contains(word, StringComparison.OrdinalIgnoreCase)
                    && !line.Contains("nothing is bleeding", StringComparison.OrdinalIgnoreCase),
                    $"a guard's sentence describes an injury: {line}");
            }
        }

        // And the whole set of consequences a CATCH can hand back is the ladder and nothing else.
        var consequences = new HashSet<string>(StringComparer.Ordinal);
        foreach (PatrolBeat.Provocation why in Enum.GetValues<PatrolBeat.Provocation>())
        {
            for (int already = 0; already <= PatrolBeat.EscortsAWatchAllows + 2; already++)
            {
                consequences.Add(PatrolBeat.TheGuardHasYou(Plate, why, already).Consequence ?? "<nothing>");
            }
        }
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                PatrolBeat.EscortLine, PatrolBeat.KickOutDueLine,
            },
            consequences);
    }

    // ── THE KICK-OUT ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE OWNER'S OWN COPY, VERBATIM. He authored it on the issue and the implementation matches it word for
    /// word — and the two lines under the big text are read off the same functions every other plate in the
    /// game reads them off, so the sign over the shed and the gauge on the suit are physically incapable of
    /// disagreeing about the moon a captain has just been put out onto.
    /// </summary>
    [Fact]
    public void TheEjectionSaysExactlyWhatItWasAuthoredToSay()
    {
        Assert.Equal("KICKED OUT", PatrolBeat.KickedOutBigText);
        Assert.Equal("The doors do not slam. They just close.", PatrolBeat.DoorsCloseLine);

        // The banner stack under it, from the building's own two instruments.
        Assert.Equal("SURFACE", UndergroundComplex.DepthPaint(0));
        Assert.Equal(
            "NO ATMOSPHERE · TANK RUNNING",
            SuitAir.PlateLine(SuitAir.SourceOf("luna", 0, insideShelter: false, aboard: false)));

        // "No red, no klaxon, no shake — the same typographic dignity as the depth readout." The plate is
        // three lines of stencil and nothing in this feature's copy reaches for anything louder.
        foreach (string line in PatrolBeat.AllProse())
        {
            foreach (string loud in new[] { "klaxon", "siren", "flash", "!" })
            {
                Assert.DoesNotContain(loud, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // And the sign comes down. A plate that stayed would be #694's facility name on all thirteen floors:
        // a thing you stop reading, which is the same as a thing that was never said.
        Assert.True(PatrolBeat.KickedOutPlateSeconds > 5.0);
        Assert.True(PatrolBeat.KickedOutPlateSeconds < 60.0);
    }

    /// <summary>The pass is a POSSESSION, so revoking it is a removal and nothing else — no flag, no parallel
    /// ledger. And the refusal that follows is machinery that was already there: a wallet with nothing in it
    /// is what the shaft's own gate is for.</summary>
    [Fact]
    public void TakingThePassIsTakingTheThingAndTheDoorAlreadyKnowsWhatThatMeans()
    {
        IReadOnlyList<Satchel.Item> carried = [PatrolBeat.Badge("luna"), PatrolBeat.Badge("europa")];
        Assert.True(PatrolBeat.BadgeHeld("luna", carried));

        IReadOnlyList<Satchel.Item> after =
            Satchel.Remove(carried, Satchel.Kind.Badge, PatrolBeat.BadgeId("luna"));

        Assert.False(PatrolBeat.BadgeHeld("luna", after));
        Assert.True(PatrolBeat.BadgeHeld("europa", after),
            "throwing a captain off one site took another site's paper out of his wallet.");

        // …and the man standing on that floor tomorrow reads the empty wallet the way he always has.
        // #836 · …and what he is handed is what a captain with that wallet would be holding: the default,
        // which is now europa's pass, because luna's is in somebody's breast pocket.
        Assert.False(
            PatrolBeat.TheGuardReads("luna", Plate, WalletChoice.DefaultFor("luna", after, null)).Satisfied);
    }
}
