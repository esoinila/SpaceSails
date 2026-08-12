using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #837 · THE CHOOSER SPENDS THE POCKET EXACTLY — the arithmetic half, swept.
///
/// <para>Owner, evening playtest with the satchel open on twelve loose rounds: <i>"I would expect like load
/// it into one or both guns"</i> and, on the count control, <i>"by default I guess it would be all into one
/// in most cases, but we should give flexibility here."</i></para>
///
/// <para>The client guard (<c>LoadItFromTheRowTests</c>) drives the two grips and compares them. This file
/// pins the laws underneath both of them, across every magazine and every pocket rather than at the two or
/// three sizes a scene happens to produce — because the seam between a pocket and a drum is where this
/// repo's third named bug class lives with ammunition in it, and a seam only opens at particular
/// numbers.</para>
/// </summary>
public sealed class TheChooserSpendsThePocketExactlyTests
{
    /// <summary>
    /// #837 · THE REACH, AMENDED. A gun riding the sling is in reach at ANY distance — it is on the
    /// captain's own back, and the arms carrying it can reach it. A gun set down is a walk.
    ///
    /// <para>RED against the pre-#837 law (<c>deployed &amp;&amp; distance &lt;= radius</c>): the sling, the
    /// one case the owner filed this on, answers false at every distance there is.</para>
    /// </summary>
    [Fact]
    public void TheSlingIsAlwaysInReachAndTheGroundIsAWalk()
    {
        foreach (double du in new[] { 0.0, 0.5, 2.9, 3.0, 3.1, 40.0, 4000.0 })
        {
            Assert.True(SentryHandLoad.WithinHands(deployed: false, du, radiusDu: 3.0),
                $"a bot on the captain's own back was out of reach at {du} du.");
            Assert.Equal(du <= 3.0, SentryHandLoad.WithinHands(deployed: true, du, radiusDu: 3.0));
        }
    }

    /// <summary>
    /// #837 · THE DEFAULT IS ALL, AND EVERY ROUND IS EITHER GOING IN OR STAYING IN THE POCKET.
    ///
    /// <para>The owner's ruling — one press, done, the common case pays no interface tax — and the law that
    /// makes the stepper safe: for every drum and every pocket, what the row says is going plus what it says
    /// stays is exactly the pocket. Nothing may fall between the two.</para>
    /// </summary>
    [Fact]
    public void TheDefaultShareIsEverythingThatWouldFitAndNothingEvaporates()
    {
        for (int have = 0; have <= SentryBot.MaxMagazine; have++)
        {
            foreach (int pocket in new[] { 1, 6, 12, 22, 40, 99, 140 })
            {
                var aim = new SentryHandLoad.Aim(
                    "K-77", have, Ammunition.Issue.Id, Deployed: true, InReach: true);
                SentryHandLoad.Pick pick =
                    SentryHandLoad.Offered(aim, pocket, Ammunition.Issue.Id, wanted: null);

                int room = SentryBot.MaxMagazine - have;
                if (room == 0)
                {
                    // A full drum refuses, says so, and takes nothing out of the pocket.
                    Assert.False(pick.Worked);
                    Assert.Equal(0, pick.Ceiling);
                    Assert.Equal(pocket, pick.Kept);
                    continue;
                }

                Assert.True(pick.Worked);
                Assert.Equal(Math.Min(pocket, room), pick.Ceiling);
                Assert.Equal(pick.Ceiling, pick.Share);
                Assert.Equal(pocket, pick.Share + pick.Kept);
                Assert.Equal(pocket, pick.Pocket);
            }
        }
    }

    /// <summary>
    /// #837 · …AND A SHARE THE CAPTAIN CHOSE IS OBEYED, AND CLAMPED TO THE SAME CEILING.
    ///
    /// <para>Loading LESS than everything is legal — rounds kept back to read as evidence, or to feed a lock
    /// later, is a real choice in this game. What is not legal is a stepper that can be pushed past what the
    /// drum would take, because the number under the captain's thumb is the number the press commits.</para>
    /// </summary>
    [Fact]
    public void TheStepperIsObeyedInsideItsCeilingAndClampedOutsideIt()
    {
        var aim = new SentryHandLoad.Aim("K-77", 90, Ammunition.Issue.Id, Deployed: true, InReach: true);

        for (int wanted = -5; wanted <= 20; wanted++)
        {
            SentryHandLoad.Pick pick = SentryHandLoad.Offered(aim, 12, Ammunition.Issue.Id, wanted);

            Assert.Equal(9, pick.Ceiling);                       // 99 − 90
            Assert.Equal(Math.Clamp(wanted, 1, 9), pick.Share);
            Assert.Equal(12 - pick.Share, pick.Kept);
            Assert.Equal(12, pick.Share + pick.Kept);
        }
    }

    /// <summary>
    /// #837 · WHAT THE PICKER SHOWS IS WHAT THE MAGAZINES LINE WILL READ — before the press commits.
    ///
    /// <para>The issue's own clause, and #696's discipline pointed at a magazine. Both halves are swept: the
    /// <c>Now</c> cell against the instrument as it stands, and the <c>After</c> cell against the instrument
    /// once <see cref="SentryHandLoad.Offer"/> — the same function the preview was built with — has been
    /// committed with that share.</para>
    ///
    /// <para>There is no second arithmetic for it to drift from: both are
    /// <see cref="SentryBot.MagazineCell"/>, which is what <see cref="SentryBot.MagazinesReadout"/> is built
    /// out of.</para>
    /// </summary>
    [Fact]
    public void ThePickerNumbersAreTheMagazinesReadoutsOwnNumbers()
    {
        for (int have = 0; have < SentryBot.MaxMagazine; have++)
        {
            foreach (int pocket in new[] { 1, 7, 12, 40 })
            {
                var aim = new SentryHandLoad.Aim(
                    "R-3B", have, Ammunition.Issue.Id, Deployed: true, InReach: true);
                SentryHandLoad.Pick pick =
                    SentryHandLoad.Offered(aim, pocket, Ammunition.Issue.Id, wanted: null);

                string before = SentryBot.MagazinesReadout(
                    [new SentryBot.Carried("R-3B", have, Deployed: true)]);
                Assert.Contains($"R-3B {pick.Now}", before, StringComparison.Ordinal);

                SentryHandLoad.Load committed = SentryHandLoad.Offer(
                    "R-3B", have, Ammunition.Issue.Id, inReach: true, pick.Share, Ammunition.Issue.Id);
                string after = SentryBot.MagazinesReadout(
                    [new SentryBot.Carried("R-3B", committed.Magazine, Deployed: true)]);
                Assert.Contains($"R-3B {pick.After}", after, StringComparison.Ordinal);

                // …and the row said where the gun was in the readout's own two words.
                Assert.Contains(pick.Where, before, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>#837 · A REFUSAL NAMES ITS OWN REASON ON THE ROW, and the two refusals a chooser can raise
    /// are two different sentences — a full drum saying it is full, a wrong kind saying what is already in
    /// it. A picker whose refused rows all said the same nothing would teach a captain to stop reading
    /// them.</summary>
    [Fact]
    public void TheTwoRefusalsAPickerCanRaiseAreTwoDifferentSentences()
    {
        SentryHandLoad.Pick full = SentryHandLoad.Offered(
            new SentryHandLoad.Aim("K-77", SentryBot.MaxMagazine, Ammunition.Issue.Id, true, true),
            12, Ammunition.Issue.Id, wanted: null);
        SentryHandLoad.Pick wrongKind = SentryHandLoad.Offered(
            new SentryHandLoad.Aim("R-3B", 12, Ammunition.Issue.Id, true, true),
            12, Ammunition.LabTwoStage.Id, wanted: null);

        Assert.False(full.Worked);
        Assert.False(wrongKind.Worked);
        Assert.NotEqual(full.Note, wrongKind.Note);
        Assert.Contains(SentryBot.Readout(SentryBot.MaxMagazine), full.Note, StringComparison.Ordinal);
        Assert.Contains(Ammunition.Issue.Name, wrongKind.Note, StringComparison.Ordinal);

        // The pocket is untouched by either of them, and both rows still know how big it is — a refused row
        // is a row, not a gap in the list.
        Assert.Equal(12, full.Kept);
        Assert.Equal(12, wrongKind.Kept);
        Assert.Equal(12, full.Pocket);
        Assert.Equal(12, wrongKind.Pocket);
    }

    /// <summary>#837 · THE TWO REMAINDERS ARE TWO SENTENCES. What a drum REFUSED and what a captain KEPT are
    /// different facts, and blaming a willing magazine for a decision the player made is the kind of small
    /// lie that makes an instrument untrustworthy. Both are said; neither is silent.</summary>
    [Fact]
    public void WhatWasKeptBackIsSaidInItsOwnWords()
    {
        string keptOne = SentryHandLoad.KeptBackLine(1);
        string keptFour = SentryHandLoad.KeptBackLine(4);

        Assert.Contains("1 stays", keptOne, StringComparison.Ordinal);
        Assert.Contains("4 stay ", keptFour, StringComparison.Ordinal);
        Assert.NotEqual(keptOne, keptFour);

        // …and it is not the drum's own overflow sentence wearing a different number.
        string wouldNotFit = SentryHandLoad.LoadedLine(
            "K-77", 5, 94, SentryBot.MaxMagazine, Ammunition.Issue, 7);
        Assert.Contains("the drum would not take them", wouldNotFit, StringComparison.Ordinal);
        Assert.DoesNotContain("because you kept", wouldNotFit, StringComparison.Ordinal);
    }
}
