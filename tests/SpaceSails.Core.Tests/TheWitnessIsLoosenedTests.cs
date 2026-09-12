using System;
using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #417 slice 2a · <b>THE DRINK-LOOSENED LEAD — the rule, in Core, where it can be asked the same question
/// twice and answer the same way.</b>
///
/// <para>Slice 1's witness filed his sentence into the field book the moment the captain reached him. These
/// guards hold the correction: the lead is behind a glass he may wave off, the glass is the bar's OWN
/// <see cref="ContactDrink.OfferDrink"/> and not a second roll, and a refusal costs the captain the watch
/// rather than the lead.</para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names, and the revert and
/// the failure it produced are quoted on it — the shape this ground has kept since #587.</para>
/// </summary>
public sealed class TheWitnessIsLoosenedTests
{
    /// <summary>A captain who has taken the case and walked none of it. The shape every guard here starts
    /// from, built through the record rather than through <c>default</c>, so a field added tomorrow makes
    /// this file say what it means about that field too.</summary>
    private static FinderCase.Progress TheCaseIsTaken => new(
        Taken: true, WitnessHeard: false, PaperFound: false, HullRead: false,
        HerringCleared: false, Revealed: false, Settled: FinderCase.Outcome.Open, PaidOff: false);

    /// <summary>A real watch index off a real sim time, so nothing here is asked about a watch the clock
    /// cannot produce. Four sim-hours in is watch one.</summary>
    private static long AWatch => PatronRota.WatchIndex(PatronRota.WatchSeconds + 60);

    /// <summary>…and the next one.</summary>
    private static long TheWatchAfter => PatronRota.WatchIndex(2 * PatronRota.WatchSeconds + 60);

    // ══ THE LEAD IS BEHIND THE GLASS ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>NOTHING FILES UNTIL A GLASS IS TAKEN, AND A REFUSED GLASS FILES NOTHING.</b> The whole of the
    /// slice in one clause: the same man, the same watch, the same case — one verdict says he talks and the
    /// other says he does not, and only the first is the lead.
    ///
    /// <para><b>Watched RED:</b> <c>WhatTheGlassDoes</c>'s last line made to return <c>Talks</c> whatever
    /// the verdict (<c>accepted ? Talks : Talks</c>) — <i>"Assert.Equal() Failure · Expected: StaysOnShift ·
    /// Actual: Talks"</i>, the witness handing the trail over to a refused drink.</para>
    /// </summary>
    [Fact]
    public void HeTalksToAnAcceptedGlassAndNotToARefusedOne()
    {
        FinderCase.WitnessWatch watch = FinderCase.WitnessWatch.Fresh.On(AWatch);

        Assert.Equal(FinderCase.WitnessAnswer.Talks,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, watch, accepted: true));
        Assert.Equal(FinderCase.WitnessAnswer.StaysOnShift,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, watch, accepted: false));

        // …and each answer carries the sentence the canon pass wrote for it, off the one place that decides.
        Assert.Equal(FinderCase.WitnessTakesTheGlass,
                     FinderCase.LineFor(FinderCase.WitnessAnswer.Talks));
        Assert.Equal(FinderCase.WitnessStaysOnShift,
                     FinderCase.LineFor(FinderCase.WitnessAnswer.StaysOnShift));
        Assert.Equal("", FinderCase.LineFor(FinderCase.WitnessAnswer.Nothing));
    }

    /// <summary>
    /// <b>AND HE HAS NOTHING TO SAY TO A CAPTAIN WHO IS NOT WORKING THE CASE.</b> No case taken, or the lead
    /// already in the book: the glass is a glass, nothing is said, and the ordinary drink the bar has always
    /// poured is the ordinary drink the bar has always poured.
    ///
    /// <para><b>Watched RED:</b> the <c>p.WitnessHeard</c> clause dropped out of
    /// <c>WhatTheGlassDoes</c>'s first <c>if</c> — <i>"Assert.Equal() Failure · Expected: Nothing · Actual:
    /// Talks"</i>, the lead filing a second time on a second glass.</para>
    /// </summary>
    [Fact]
    public void HeHasNothingToSayWithoutACaseOrAfterHeHasSaidIt()
    {
        FinderCase.WitnessWatch watch = FinderCase.WitnessWatch.Fresh.On(AWatch);

        Assert.Equal(FinderCase.WitnessAnswer.Nothing,
                     FinderCase.WhatTheGlassDoes(FinderCase.Progress.Fresh, watch, accepted: true));

        FinderCase.Progress spoken = TheCaseIsTaken with { WitnessHeard = true };
        Assert.Equal(FinderCase.WitnessAnswer.Nothing,
                     FinderCase.WhatTheGlassDoes(spoken, watch, accepted: true));
        Assert.Equal(FinderCase.WitnessAnswer.Nothing,
                     FinderCase.WhatTheGlassDoes(spoken, watch, accepted: false));
    }

    // ══ ONE ASK A WATCH ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>A REFUSAL COSTS THE WATCH AND NOT THE LEAD.</b> The man's one ask is spent whichever way it went,
    /// so a captain who is waved off cannot stand him eleven drinks in ninety seconds until the dice come up
    /// — and the next watch he is asked again as if nothing had happened, because nothing did.
    ///
    /// <para>This is the clause that makes the trail a rhythm rather than a slot machine: the bar's offer
    /// seed folds the SIM-SECOND (<c>DiceRule.Seed($"drink-offer:{giver}", (long)SimTime)</c>), so without
    /// the spent ask a refusal would be worth nothing at all — press again next second, new dice.</para>
    ///
    /// <para><b>Watched RED:</b> the <c>w.Asked</c> clause dropped out of <c>WhatTheGlassDoes</c> —
    /// <i>"Assert.Equal() Failure · Expected: Nothing · Actual: Talks"</i>, the second glass of the same
    /// watch buying the lead the first one was refused.</para>
    /// </summary>
    [Fact]
    public void ARefusalSpendsTheWatchAndTheNextWatchAsksAgain()
    {
        FinderCase.WitnessWatch watch = FinderCase.WitnessWatch.Fresh.On(AWatch);
        Assert.Equal(FinderCase.WitnessAnswer.StaysOnShift,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, watch, accepted: false));

        // He was asked. Everything else this watch is only a drink — including a glass he WOULD have taken.
        watch = watch.Asking();
        Assert.Equal(FinderCase.WitnessAnswer.Nothing,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, watch, accepted: true));
        Assert.Equal(FinderCase.WitnessAnswer.Nothing,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, watch, accepted: false));

        // …and the next watch is a new evening, the same fold, and the lead is still there to be bought.
        FinderCase.WitnessWatch later = watch.On(TheWatchAfter);
        Assert.False(later.Asked);
        Assert.False(later.Greeted);
        Assert.Equal(FinderCase.WitnessAnswer.Talks,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, later, accepted: true));
    }

    /// <summary>
    /// <b>AND HE SAYS HE DOESN'T WORK FOR YOU ONCE A WATCH.</b> Not once ever (he does not remember the
    /// captain, which is the point of him) and not once a press (a sentence repeated at every press is a
    /// machine talking).
    ///
    /// <para><b>Watched RED:</b> <c>WitnessWatch.On</c> made to return <c>this</c> unconditionally —
    /// <i>"Assert.True() Failure · he never said it again on a later watch"</i>; and the <c>!w.Greeted</c>
    /// clause dropped from <c>TheGreetingIsDue</c> — <i>"Assert.False() Failure · he said it twice in one
    /// watch"</i>.</para>
    /// </summary>
    [Fact]
    public void TheGreetingIsSaidOnceAWatchAndNotAfterHeHasTalked()
    {
        FinderCase.WitnessWatch watch = FinderCase.WitnessWatch.Fresh.On(AWatch);
        Assert.True(FinderCase.TheGreetingIsDue(TheCaseIsTaken, watch));

        watch = watch.Greeting();
        Assert.False(FinderCase.TheGreetingIsDue(TheCaseIsTaken, watch), "he said it twice in one watch.");

        FinderCase.WitnessWatch later = watch.On(TheWatchAfter);
        Assert.True(FinderCase.TheGreetingIsDue(TheCaseIsTaken, later),
                    "he never said it again on a later watch.");

        // …and a captain with no case, or one whose book already has the lead, hears nothing from him.
        Assert.False(FinderCase.TheGreetingIsDue(FinderCase.Progress.Fresh, later));
        Assert.False(FinderCase.TheGreetingIsDue(TheCaseIsTaken with { WitnessHeard = true }, later));

        // THE FOLD IS ABOUT A WATCH AND NOT ABOUT A PRESS: greeting him does not spend his glass, and being
        // offered a glass does not stop him saying his line. Two facts, and a guard that watched only one of
        // them would stay green with the other deleted.
        FinderCase.WitnessWatch greeted = FinderCase.WitnessWatch.Fresh.On(AWatch).Greeting();
        Assert.False(greeted.Asked);
        Assert.Equal(FinderCase.WitnessAnswer.Talks,
                     FinderCase.WhatTheGlassDoes(TheCaseIsTaken, greeted, accepted: true));

        FinderCase.WitnessWatch asked = FinderCase.WitnessWatch.Fresh.On(AWatch).Asking();
        Assert.True(FinderCase.TheGreetingIsDue(TheCaseIsTaken, asked));
    }

    /// <summary>
    /// THE FRESH FOLD IS ABOUT A WATCH THE CLOCK CANNOT REACH, so the first real watch always forgets it.
    /// Without this the whole rule could be passing on a fold that happened to start on watch zero — and
    /// watch zero is where every game begins.
    ///
    /// <para><b>Watched RED:</b> <c>WitnessWatch.Fresh</c> given <c>0</c> for its watch —
    /// <i>"Assert.True() Failure · Expected: True · Actual: False"</i>, a brand-new captain walking up to a
    /// witness the fold already thinks has been greeted and asked.</para>
    /// </summary>
    [Fact]
    public void AFreshFoldIsForgottenByTheFirstRealWatch()
    {
        Assert.Equal(0, PatronRota.WatchIndex(0));
        Assert.True(FinderCase.WitnessWatch.Fresh.Watch < PatronRota.WatchIndex(0));

        FinderCase.WitnessWatch spent = FinderCase.WitnessWatch.Fresh.Greeting().Asking();
        FinderCase.WitnessWatch atZero = spent.On(PatronRota.WatchIndex(0));
        Assert.False(atZero.Greeted);
        Assert.False(atZero.Asked);
    }

    // ══ THE GLASS IS THE BAR'S OWN GLASS ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE VERDICT IS THE BAR'S, AND THE SAME SEED GIVES THE SAME EVENING TWICE.</b> Nothing in this
    /// slice rolls anything: <c>WhatTheGlassDoes</c> is handed
    /// <see cref="DrinkOfferResult.Accepted"/> off the shipped <see cref="ContactDrink.OfferDrink"/>, so the
    /// determinism the trail inherits is the determinism the drink already had — one seed, one verdict, one
    /// answer, on any machine and across a reload.
    ///
    /// <para>The seed sweep is the honest half: a rule read off a single lucky seed would say nothing about
    /// whether the two arms are even reachable, so the sweep asserts that this bench really produces both a
    /// taken and a refused glass — a threshold that selected everything is the other half of the fifth named
    /// bug class.</para>
    ///
    /// <para><b>Watched RED:</b> <c>WhatTheGlassDoes</c> made to ignore its <c>accepted</c> argument —
    /// <i>"Assert.True() Failure · a refused glass filed the lead"</i> on the first refusing seed in the
    /// sweep.</para>
    /// </summary>
    [Fact]
    public void TheDrinksOwnVerdictDecidesItAndTheSameSeedDecidesItTheSameWay()
    {
        var taken = new List<ulong>();
        var refused = new List<ulong>();

        for (ulong tick = 0; tick < 400; tick++)
        {
            ulong seed = DiceRule.Seed("drink-offer:GILT-EYE", (long)tick);
            DrinkOfferResult offered = ContactDrink.OfferDrink(seed, currentGoodwill: 0, holdingSecret: false);

            // The same seed, asked twice, is the same evening — Core's own law, and the thing that makes a
            // captain's reload land on the answer he already had.
            Assert.Equal(offered.Accepted,
                         ContactDrink.OfferDrink(seed, currentGoodwill: 0, holdingSecret: false).Accepted);

            FinderCase.WitnessAnswer answer = FinderCase.WhatTheGlassDoes(
                TheCaseIsTaken, FinderCase.WitnessWatch.Fresh.On(AWatch), offered.Accepted);

            if (offered.Accepted)
            {
                taken.Add(seed);
                Assert.True(answer == FinderCase.WitnessAnswer.Talks, "a taken glass did not file the lead.");
            }
            else
            {
                refused.Add(seed);
                Assert.True(answer == FinderCase.WitnessAnswer.StaysOnShift, "a refused glass filed the lead.");
            }
        }

        Assert.NotEmpty(taken);
        Assert.NotEmpty(refused);
    }
}
