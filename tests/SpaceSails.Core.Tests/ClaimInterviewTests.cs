using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1151 slice 2 · <b>THE INTERVIEW — THE WORDS, THE DICE AND THE ARITHMETIC.</b> Fable canon (the 2026-10-07 slice-2
/// brief cut), verbatim and retyped here from the issue (its doubled apostrophes are comment escaping; the shipped
/// strings carry single ASCII ones) so the guards have a source the implementation cannot move. The three rolls are
/// swept over many bookings' seeds with count bands — one sample proves nothing.
/// </summary>
public sealed partial class ClaimInterviewTests
{
    private const int Sweep = 12000;

    private static HullClaim.Loss LossAt(long when, int tenths = 20) => new(when, tenths);

    /// <summary>
    /// <b>THE CANON, TO THE BYTE.</b> Every sentence the interview speaks, full-Assert.Equal, retyped from the issue.
    ///
    /// <para><b>Proven RED</b> by a curly apostrophe in a clause, a dropped full stop in the paid document and a
    /// reworded Q2 answer.</para>
    /// </summary>
    [Fact]
    public void TheCanonIsPinnedToTheByte()
    {
        Assert.Equal("She reads the form before she looks at you, which tells you the order of things here.", ClaimInterview.SeatingLine);
        Assert.Equal("State the loss. The form states it; state it anyway. The company likes the two to match.", ClaimInterview.Q1Ask);
        Assert.Equal(
            "Hull, holed. 3.4 days under way. The form agrees with you. That is the last easy thing that will happen here.",
            ClaimInterview.Q1Right(34));
        Assert.Equal("That is a document. It is not this document.", ClaimInterview.Q1Wrong);
        Assert.Equal(
            "The company pays captains, not ships. Show me the ship is yours the way the collectors would ask it.",
            ClaimInterview.Q2Ask);
        Assert.Equal(
            "So the ship is yours. The collectors will be told you could prove it; it ruins their whole afternoon.",
            ClaimInterview.Q2Right);
        Assert.Equal("The company has files on who owns what. I am asking whether you do.", ClaimInterview.Q2Wrong);
        Assert.Equal(
            "The company pays the captains it insures. You are not one, which the company considers a fixable condition.",
            ClaimInterview.Q2NoPolicyLine);
        Assert.Equal("Last box. Why was this loss not your doing? You may show me anything. People do.", ClaimInterview.Q3Ask);
        Assert.Equal(
            "…This will do. Not because it proves anything — because it is the kind of paper the file wants inside it.",
            ClaimInterview.Q3AcceptedLine);
        Assert.Equal("I have read it. The file would read it differently.", ClaimInterview.Q3RefusedLine);
        Assert.Equal("No answer is an answer. The file has a box for it.", ClaimInterview.Q3NothingLine);

        Assert.Equal("Paid in full. Do not tell anyone; the company has a reputation.", ClaimInterview.PaidLine);
        Assert.Equal("A settlement, paid", ClaimInterview.PaidTitle);
        Assert.Equal(
            "Hull, holed. 2.0 days under way at the policy's day-rate. Paid without deduction. "
            + "The adjuster notes, for the file, that this was the correct outcome and that she expects consequences.",
            ClaimInterview.PaidDocument(20));
        Assert.Equal("A settlement, adjusted", ClaimInterview.AdjustedTitle);
        Assert.Equal(
            "Declined. The cause is procedural, which the company prefers, because procedure holds up.",
            ClaimInterview.DeclinedLine);
        Assert.Equal("A claim form, returned", ClaimInterview.ReturnedTitle);
        Assert.Equal(" · DECLINED", ClaimInterview.DeclinedStamp);
    }

    /// <summary>
    /// <b>THE CLAUSE POOL AND THE CAUSE POOL, TO THE BYTE — AND THE ADJUSTED LINES AND DOCUMENTS THEY FILL.</b> Three
    /// of each, in print order, and the templates around them.
    ///
    /// <para><b>Proven RED</b> by clause 2 and cause 3 swapped, and by the colon after "Clause {c}" dropped.</para>
    /// </summary>
    [Fact]
    public void TheClausesAndCausesAreThePrintedPools()
    {
        string[] clauses =
        [
            "Days lost to weather of the company's own classification are borne gladly by the insured.",
            "The first day of any loss is the captain's day, in recognition of the sea tradition.",
            "Loss of use is compensated at the rate the use would probably have earned, as the company reckons probability.",
        ];
        string[] causes =
        [
            "Filed on a form series the company retired while the claim was pending.",
            "The loss and the claimant are in order; the policy's schedule for this class of loss is under revision.",
            "The figures are correct but arrived in the wrong order.",
        ];
        Assert.Equal(3, ClaimInterview.PoolSize);
        for (int i = 1; i <= 3; i++)
        {
            Assert.Equal(clauses[i - 1], ClaimInterview.ClauseText(i));
            Assert.Equal(causes[i - 1], ClaimInterview.CauseText(i));
            Assert.Equal(
                $"Adjusted and paid. The deduction is Clause {i}: '{clauses[i - 1]}'. You signed it. Most people did.",
                ClaimInterview.AdjustedLine(i));
            Assert.Equal(
                $"Hull, holed. 2.0 days at the policy's day-rate, less Clause {i}. The arithmetic is enclosed. "
                + "The arithmetic is correct. Correct is the company's favourite word.",
                ClaimInterview.AdjustedDocument(20, i));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => ClaimInterview.ClauseText(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ClaimInterview.CauseText(4));
    }

    /// <summary>
    /// <b>EVERY WORD IS IN THE SWEEP AND NONE SPENDS A RESERVED WORD OR NAMES ANYBODY.</b> No word of the arc the
    /// captain has not earned (nothing says what Nebula Mutual truly is — the §422 law), no regular's name, no straight
    /// double quote inside a sentence (the canon quotes with single ASCII apostrophes) and no doubled apostrophe (the
    /// issue's comment escaping must not leak into a shipped string).
    ///
    /// <para><b>Proven RED</b> by "restore" in a cause and by a doubled apostrophe in a clause.</para>
    /// </summary>
    [Fact]
    public void NotOneWordSpendsAReservedWordOrNamesAnybody()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "ancient", "alien", "not ours", "not natural", "restore", "backup",
            "kaamos", "minister", "donor", "they were people", "whose", "who made", "flashback", "remember",
        ];

        List<string> prose = [.. ClaimInterview.AllProse()];
        Assert.Equal(prose.Count, prose.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(34, prose.Count);   // 19 single lines and documents, and the five that fill once per clause or cause
        foreach (string line in prose)
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }

            foreach (string regular in PatronRota.Roster)
            {
                Assert.DoesNotContain(regular, line, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("''", line, StringComparison.Ordinal);
            Assert.DoesNotContain("\"", line, StringComparison.Ordinal);
            Assert.DoesNotContain("’", line, StringComparison.Ordinal);
        }
    }

    // ── THE FLAGGED NUMBERS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CUT'S NUMBERS, EXACTLY.</b> Base {1, 8, 3} of 12, accepted {2, 9, 1} of 12, the day-rate 40 cr; every
    /// weight set sums to the die it is rolled on. The crew's two gaps (acceptance chance, clause bites) are consts too,
    /// and the bites are fractions of a gross.
    ///
    /// <para><b>Proven RED</b> by the base PAID weight at 2 and by the day-rate at 50.</para>
    /// </summary>
    [Fact]
    public void TheFlaggedNumbersAreTheCutsAndEverySetSumsToTheDie()
    {
        Assert.Equal([1, 8, 3], ClaimInterview.BaseWeights.ToArray());
        Assert.Equal([2, 9, 1], ClaimInterview.AcceptedWeights.ToArray());
        Assert.Equal(12, ClaimInterview.WeightTotal);
        Assert.Equal(12, ClaimInterview.BaseWeights.Sum());
        Assert.Equal(12, ClaimInterview.AcceptedWeights.Sum());
        Assert.Equal(40, ClaimInterview.ClaimDayRate);
        Assert.Equal(ClaimInterview.PoolSize, ClaimInterview.ClauseBitePercent.Count);
        Assert.All(ClaimInterview.ClauseBitePercent, p => Assert.InRange(p, 1, 99));
        Assert.InRange(ClaimInterview.Q3AcceptedInTwelve, 1, 11);

        // F4 · read-only from outside: not arrays, and a write through the list interface throws.
        foreach (IReadOnlyList<int> set in new[] { ClaimInterview.BaseWeights, ClaimInterview.AcceptedWeights, ClaimInterview.ClauseBitePercent })
        {
            Assert.False(set is int[]);
            Assert.Throws<NotSupportedException>(() => ((IList<int>)set)[0] = 99);
        }

        Assert.Equal([1, 8, 3], ClaimInterview.BaseWeights.ToArray());   // …and the attempts changed nothing
    }

    /// <summary>The day-rate arithmetic: 2.0 days is 80 cr; 3.4 is 136; the bite is a fraction of that.</summary>
    [Fact]
    public void TheGrossIsTheDayRateTimesTheDays()
    {
        Assert.Equal(80, ClaimInterview.GrossCr(20));
        Assert.Equal(136, ClaimInterview.GrossCr(34));
        Assert.Equal(0, ClaimInterview.GrossCr(0));
        for (int c = 1; c <= 3; c++)
        {
            int bite = ClaimInterview.BiteCr(20, c);
            Assert.InRange(bite, 1, 79);
            Assert.Equal((int)Math.Round(80 * ClaimInterview.ClauseBitePercent[c - 1] / 100.0, MidpointRounding.AwayFromZero), bite);
        }
    }

    // ── THE THREE QUESTIONS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>FAIL FORWARD: A WRONG PAPER LEAVES THE QUESTION STANDING AND COSTS NOTHING.</b> Question one: every other
    /// paper — the blank form, another loss's filled form (the stale one), a settlement, a random id, the policy, the
    /// nothing — says the wrong line and stays; only THIS loss's filled form moves on. Question two: only the policy,
    /// and only in force. No answer changes the credit-free, paper-free Reply.
    ///
    /// <para><b>Proven RED</b> by any filled form passing question one, and by a lapsed policy passing question two.</para>
    /// </summary>
    [Fact]
    public void AWrongPaperLeavesTheQuestionStandingAndCostsNothing()
    {
        HullClaim.Loss loss = LossAt(172800);
        var right = new ClaimInterview.Show(ClaimInterview.ShowKind.Paper, HullClaim.FilledId(loss), "x");
        ClaimInterview.Show[] wrongs =
        [
            new(ClaimInterview.ShowKind.Paper, AdjustersRoom.SheetId, "x"),
            new(ClaimInterview.ShowKind.Paper, HullClaim.FilledId(LossAt(300000, 31)), "x"),
            new(ClaimInterview.ShowKind.Paper, HullClaim.FilledId(LossAt(172800, 21)), "x"),
            new(ClaimInterview.ShowKind.Paper, ClaimInterview.PaidId(loss), "x"),
            new(ClaimInterview.ShowKind.Paper, "spread-demo-1", "x"),
            new(ClaimInterview.ShowKind.Policy, "Premium", "x"),
            new(ClaimInterview.ShowKind.Nothing, "", "x"),
        ];

        foreach (ClaimInterview.Show wrong in wrongs)
        {
            ClaimInterview.Reply r = ClaimInterview.Step(ClaimInterview.Stage.Loss, wrong, loss, true);
            Assert.Equal(ClaimInterview.Stage.Loss, r.Next);
            Assert.Equal(ClaimInterview.Q1Wrong, r.Said);
        }

        ClaimInterview.Reply one = ClaimInterview.Step(ClaimInterview.Stage.Loss, right, loss, true);
        Assert.Equal(ClaimInterview.Stage.Ship, one.Next);
        Assert.Equal(ClaimInterview.Q1Right(20), one.Said);

        var policy = new ClaimInterview.Show(ClaimInterview.ShowKind.Policy, "Premium", "Premium");
        foreach (ClaimInterview.Show wrong in wrongs.Where(w => w.Kind != ClaimInterview.ShowKind.Policy))
        {
            ClaimInterview.Reply r = ClaimInterview.Step(ClaimInterview.Stage.Ship, wrong, loss, true);
            Assert.Equal(ClaimInterview.Stage.Ship, r.Next);
            Assert.Equal(ClaimInterview.Q2Wrong, r.Said);
        }

        // NO policy card at all: her own line, whatever is shown (every kind), and the question stands. A held-but-lapsed
        // card (above) is still the ordinary wrong-paper line.
        foreach (ClaimInterview.Show any in wrongs.Append(policy).Append(right))
        {
            ClaimInterview.Reply none = ClaimInterview.Step(ClaimInterview.Stage.Ship, any, loss, false, false);
            Assert.Equal(ClaimInterview.Stage.Ship, none.Next);
            Assert.Equal(ClaimInterview.Q2NoPolicyLine, none.Said);
            Assert.False(none.Q3Accepted);
        }

        Assert.Equal(ClaimInterview.Stage.Ship, ClaimInterview.Step(ClaimInterview.Stage.Ship, policy, loss, false).Next);
        Assert.Equal(ClaimInterview.Q2Wrong, ClaimInterview.Step(ClaimInterview.Stage.Ship, policy, loss, false).Said);
        ClaimInterview.Reply two = ClaimInterview.Step(ClaimInterview.Stage.Ship, policy, loss, true);
        Assert.Equal(ClaimInterview.Stage.Fault, two.Next);
        Assert.Equal(ClaimInterview.Q2Right, two.Said);
    }

    /// <summary>
    /// <b>QUESTION THREE TAKES ONE OFFER — ANY PAPER, OR AN EXPLICIT NOTHING — AND THE PAPER NEVER CHOOSES THE ROLL.</b>
    /// Swept over many bookings: every kind of paper offered at the same booking gets the SAME verdict (the roll is
    /// salted from the booking alone, so no paper can be shopped for a better one); offering nothing is never
    /// accepted; both lines are reached across the sweep and the accepted share sits inside its band.
    ///
    /// <para><b>Proven RED</b> by the roll salted with the offered paper's id (verdicts differ per paper), and by
    /// "nothing" rolling like a paper.</para>
    /// </summary>
    [Fact]
    public void QuestionThreeTakesAnyPaperToTheSameVerdictAndNothingNeverPasses()
    {
        string[] papers = [AdjustersRoom.SheetId, "spread-demo-1", "claim-form-blank", "anything at all"];
        int accepted = 0;
        for (long when = 1; when <= Sweep; when++)
        {
            HullClaim.Loss loss = LossAt(when * 60);
            bool first = ClaimInterview.Step(
                ClaimInterview.Stage.Fault,
                new ClaimInterview.Show(ClaimInterview.ShowKind.Paper, papers[0], "x"), loss, true).Q3Accepted;
            foreach (string paper in papers)
            {
                ClaimInterview.Reply r = ClaimInterview.Step(
                    ClaimInterview.Stage.Fault, new ClaimInterview.Show(ClaimInterview.ShowKind.Paper, paper, "x"), loss, true);
                Assert.Equal(first, r.Q3Accepted);
                Assert.Equal(ClaimInterview.Stage.Settled, r.Next);
                Assert.Equal(first ? ClaimInterview.Q3AcceptedLine : ClaimInterview.Q3RefusedLine, r.Said);
            }

            ClaimInterview.Reply none = ClaimInterview.Step(
                ClaimInterview.Stage.Fault, new ClaimInterview.Show(ClaimInterview.ShowKind.Nothing, "", "x"), loss, true);
            Assert.False(none.Q3Accepted);
            Assert.Equal(ClaimInterview.Q3NothingLine, none.Said);
            Assert.Equal(ClaimInterview.Stage.Settled, none.Next);
            accepted += first ? 1 : 0;
        }

        // 5 in 12 of 12000 = 5000; sigma ~ 54; the band is a generous +-5 sigma.
        Assert.InRange(accepted, 4700, 5300);
    }
}
