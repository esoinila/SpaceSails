using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#1151 slice 2 · the second half of <see cref="ClaimInterviewTests"/>: the swept weights, the determinism, the
/// settlement and the book — every roll over many bookings' seeds with count bands.</summary>
public sealed partial class ClaimInterviewTests
{
    // ── THE OUTCOME: SWEPT WEIGHTS ──────────────────────────────────────────────────────────────────────

    private static int[] Counts(bool accepted)
    {
        int[] counts = new int[3];
        for (long when = 1; when <= Sweep; when++)
        {
            counts[(int)ClaimInterview.OutcomeOf(when * 97, accepted)]++;
        }

        return counts;
    }

    /// <summary>
    /// <b>THE OUTCOME WEIGHTS, SWEPT.</b> Over twelve thousand bookings' seeds: the base set lands PAID/ADJUSTED/DECLINED
    /// near 1000/8000/3000, the accepted set near 2000/9000/1000 — each inside a +-5 sigma count band — and Q3 accepted
    /// moves the weights TOWARD the captain (more PAID, fewer DECLINED). A single roll proves none of this.
    ///
    /// <para><b>Proven RED</b> by Q3 accepted reading the base weights (the shift vanishes), by the base set swapped
    /// for the accepted set (bands break), and by a sweep that only ever rolls ADJUSTED.</para>
    /// </summary>
    [Fact]
    public void TheOutcomeWeightsAreSweptAndQuestionThreeShiftsThemTowardTheCaptain()
    {
        int[] b = Counts(false);
        int[] a = Counts(true);

        Assert.InRange(b[(int)ClaimInterview.Outcome.Paid], 850, 1150);
        Assert.InRange(b[(int)ClaimInterview.Outcome.Adjusted], 7700, 8300);
        Assert.InRange(b[(int)ClaimInterview.Outcome.Declined], 2700, 3300);
        Assert.InRange(a[(int)ClaimInterview.Outcome.Paid], 1800, 2200);
        Assert.InRange(a[(int)ClaimInterview.Outcome.Adjusted], 8700, 9300);
        Assert.InRange(a[(int)ClaimInterview.Outcome.Declined], 900, 1100);

        Assert.True(a[(int)ClaimInterview.Outcome.Paid] > b[(int)ClaimInterview.Outcome.Paid] + 500);
        Assert.True(a[(int)ClaimInterview.Outcome.Declined] + 1500 < b[(int)ClaimInterview.Outcome.Declined]);
        Assert.Equal(Sweep, b.Sum());
        Assert.Equal(Sweep, a.Sum());
    }

    /// <summary>
    /// <b>THE CLAUSE AND THE CAUSE ARE ONE SEEDED PICK — SWEPT.</b> All three of each pool are reached, each near a
    /// third, and the pick is independent of the outcome roll (every outcome sees every pick).
    ///
    /// <para><b>Proven RED</b> by a pick that always returned 1.</para>
    /// </summary>
    [Fact]
    public void TheClauseAndTheCauseArePickedAcrossTheWholePool()
    {
        int[] picks = new int[4];
        var seen = new HashSet<(ClaimInterview.Outcome, int)>();
        for (long when = 1; when <= Sweep; when++)
        {
            int pick = ClaimInterview.PickOf(when * 97);
            picks[pick]++;
            seen.Add((ClaimInterview.OutcomeOf(when * 97, false), pick));
        }

        Assert.Equal(0, picks[0]);
        for (int i = 1; i <= 3; i++)
        {
            Assert.InRange(picks[i], 3700, 4300);
        }

        Assert.Equal(9, seen.Count);
    }

    // ── DETERMINISM ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE COMPANY IS NOTHING IF NOT CONSISTENT: THE SAME BOOKING SETTLES THE SAME WAY, EVERY TIME.</b> Settled twice
    /// (a reload replays it) and settled for a loss of the same {when} with the same days: equal. Different bookings
    /// settle differently across the sweep (the roll is the booking's, not a constant).
    ///
    /// <para><b>Proven RED</b> by a roll salted from the wall clock or a counter (the replay differs), and by a
    /// constant roll (the sweep is a single settlement).</para>
    /// </summary>
    [Fact]
    public void TheSameBookingSettlesTheSameWayAndDifferentBookingsDoNot()
    {
        var distinct = new HashSet<string>();
        for (long when = 1; when <= 2000; when++)
        {
            HullClaim.Loss loss = LossAt(when * 61, 14 + (int)(when % 40));
            foreach (bool accepted in new[] { false, true })
            {
                ClaimInterview.Settlement one = ClaimInterview.Settle(loss, accepted);
                ClaimInterview.Settlement again = ClaimInterview.Settle(loss, accepted);
                Assert.Equal(one, again);
                distinct.Add(one.PaperId + one.Credits);
            }

            Assert.Equal(ClaimInterview.Q3Accepts(loss.DoneAt), ClaimInterview.Q3Accepts(loss.DoneAt));
        }

        Assert.True(distinct.Count > 300, $"only {distinct.Count} distinct settlements over two thousand bookings.");
    }

    // ── SETTLEMENT: WHAT MOVES AND WHAT IT HANDS OVER ───────────────────────────────────────────────────

    /// <summary>
    /// <b>CREDITS MOVE ONLY ON PAID AND ADJUSTED — AND THE PAPER IS THE OUTCOME'S OWN.</b> Swept: PAID pays exactly
    /// the gross; ADJUSTED pays the gross less its clause's bite (strictly less, never negative) and names that clause;
    /// DECLINED pays nothing. Each outcome's told line is its canon line and its paper reads back with the canon title
    /// and document (the returned form with the filled form's words and the cause as a final line).
    ///
    /// <para><b>Proven RED</b> by DECLINED carrying the gross, by ADJUSTED paying the gross (no bite), and by the
    /// returned form dropping its last line.</para>
    /// </summary>
    [Fact]
    public void CreditsMoveOnlyOnPaidAndAdjustedAndEachOutcomeHandsOverItsOwnPaper()
    {
        var seen = new HashSet<ClaimInterview.Outcome>();
        for (long when = 1; when <= Sweep; when++)
        {
            HullClaim.Loss loss = LossAt(when * 101, 10 + (int)(when % 50));
            ClaimInterview.Settlement s = ClaimInterview.Settle(loss, when % 2 == 0);
            seen.Add(s.Outcome);
            Assert.True(ClaimInterview.IsAPaper(s.PaperId));
            Assert.False(HullClaim.IsTheFilledForm(s.PaperId));
            switch (s.Outcome)
            {
                case ClaimInterview.Outcome.Paid:
                    Assert.Equal(ClaimInterview.GrossCr(loss.Tenths), s.Credits);
                    Assert.Equal(ClaimInterview.PaidLine, s.Told);
                    Assert.Equal(ClaimInterview.PaidTitle, ClaimInterview.TitleOf(s.PaperId));
                    Assert.Equal(ClaimInterview.PaidDocument(loss.Tenths), ClaimInterview.DocumentOf(s.PaperId));
                    break;
                case ClaimInterview.Outcome.Adjusted:
                    Assert.InRange(s.Pick, 1, 3);
                    Assert.Equal(ClaimInterview.GrossCr(loss.Tenths) - ClaimInterview.BiteCr(loss.Tenths, s.Pick), s.Credits);
                    Assert.True(s.Credits >= 0 && s.Credits < ClaimInterview.GrossCr(loss.Tenths));
                    Assert.Equal(ClaimInterview.AdjustedLine(s.Pick), s.Told);
                    Assert.Equal(ClaimInterview.AdjustedTitle, ClaimInterview.TitleOf(s.PaperId));
                    Assert.Equal(ClaimInterview.AdjustedDocument(loss.Tenths, s.Pick), ClaimInterview.DocumentOf(s.PaperId));
                    break;
                default:
                    Assert.Equal(0, s.Credits);
                    Assert.Equal(ClaimInterview.DeclinedLine, s.Told);
                    Assert.Equal(ClaimInterview.ReturnedTitle, ClaimInterview.TitleOf(s.PaperId));
                    Assert.Equal(
                        HullClaim.FilledDocument(loss.Tenths) + "\n— RETURNED. " + ClaimInterview.CauseText(s.Pick),
                        ClaimInterview.DocumentOf(s.PaperId));
                    break;
            }
        }

        Assert.Equal(3, seen.Count);
    }

    /// <summary>The papers read back; no other id is one; a malformed id is nobody's.</summary>
    [Fact]
    public void ThePapersAreRecognisedByTheirIdsAndNoOtherId()
    {
        HullClaim.Loss loss = LossAt(172800, 20);
        foreach (string id in new[] { ClaimInterview.PaidId(loss), ClaimInterview.AdjustedId(loss, 2), ClaimInterview.ReturnedId(loss, 3) })
        {
            Assert.True(ClaimInterview.IsAPaper(id));
            Assert.NotEqual("", ClaimInterview.TitleOf(id));
            Assert.NotEqual("", ClaimInterview.DocumentOf(id));
            Assert.True(FieldClue.IsAuthored(id));
            Assert.Equal(ClaimInterview.TitleOf(id), FieldClue.Title(id));
            Assert.Equal(ClaimInterview.DocumentOf(id), FieldClue.Document(id));
            Assert.Equal(ClaimInterview.TitleOf(id), CarriedObject.PaperReveal(id).Label);
            Assert.Equal(ClaimInterview.DocumentOf(id), CarriedObject.PaperReveal(id).Story);
        }

        foreach (string id in new[]
                 {
                     HullClaim.FilledId(loss), AdjustersRoom.SheetId, "spread-demo-1", "claim-settlement:paid:1", "claim-settlement:adjusted:1:2:9",
                     "claim-settlement:adjusted:1:2:0", "claim-form-returned:1:2", "claim-form-returned:a:2:3", "", "claim-settlement:",
                 })
        {
            Assert.False(ClaimInterview.IsAPaper(id), id);
            Assert.Equal("", ClaimInterview.TitleOf(id));
            Assert.Equal("", ClaimInterview.DocumentOf(id));
        }

        Assert.False(ClaimInterview.IsAPaper(null));
    }

    // ── THE BOOK AND THE ROW ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>DECLINED STAMPS THE LOSS LINE — ONE WORD, THE RIGHT LINE, ONCE.</b> The loss's own 📋 line (found by its words
    /// and its moment) gains " · DECLINED"; another loss's line, an unrelated line and a 📍 line are untouched; a line
    /// trimmed off a full book leaves the book as it was; a stamped line is not stamped twice.
    ///
    /// <para><b>Proven RED</b> by stamping every 📋 line, and by stamping an already-stamped line.</para>
    /// </summary>
    [Fact]
    public void TheStampLandsOnTheRightLossLineOnce()
    {
        HullClaim.Loss mine = LossAt(172800, 20);
        HullClaim.Loss other = LossAt(300000, 20);
        var book = new List<FieldNote>
        {
            new(HullClaim.LossLine(20), 300000.4, "The Deep", "📋", "s"),
            new("an unrelated line", 1, "The Deep", "·"),
            new(HullClaim.LossLine(20), 172800.7, "The Deep", "📋", "s"),
            new(HullClaim.BookEntryLine, 172900, "The Deep", "📍"),
        };

        IReadOnlyList<FieldNote> stamped = ClaimInterview.Stamp(book, mine);
        Assert.Equal(HullClaim.LossLine(20), stamped[0].Text);
        Assert.Equal("an unrelated line", stamped[1].Text);
        Assert.Equal(HullClaim.LossLine(20) + " · DECLINED", stamped[2].Text);
        Assert.Equal(ClaimInterview.StampedLossLine(20), stamped[2].Text);
        Assert.Equal(HullClaim.BookEntryLine, stamped[3].Text);
        Assert.Equal(book[2].SimTime, stamped[2].SimTime);
        Assert.Equal(book[2].Subjects, stamped[2].Subjects);

        Assert.Equal(stamped.Select(n => n.Text), ClaimInterview.Stamp(stamped, mine).Select(n => n.Text));
        Assert.Equal(HullClaim.LossLine(20) + " · DECLINED", ClaimInterview.Stamp(book, other)[0].Text);
        Assert.Equal(HullClaim.LossLine(20), ClaimInterview.Stamp(book, other)[2].Text);
        Assert.Equal(book.Select(n => n.Text), ClaimInterview.Stamp(book, LossAt(5, 31)).Select(n => n.Text));
        Assert.Empty(ClaimInterview.Stamp([], mine));
    }

    /// <summary>
    /// <b>THE BOOKED ROW IS OPEN UNTIL THE INTERVIEW IS HAD.</b> Booked and unsettled is open; unbooked is not; the
    /// settled tag closes it; another loss's settled tag does not.
    ///
    /// <para><b>Proven RED</b> by the settled clause dropped (a settled claim stays open).</para>
    /// </summary>
    [Fact]
    public void TheBookedRowClosesWhenTheInterviewIsHad()
    {
        const long When = 172800;
        Assert.False(ClaimInterview.IsOpen([], When));
        Assert.False(ClaimInterview.IsOpen([HullClaim.LossTag(LossAt(When))], When));
        Assert.True(ClaimInterview.IsOpen([HullClaim.BookedTag(When)], When));
        Assert.False(ClaimInterview.IsOpen([HullClaim.BookedTag(When), ClaimInterview.SettledTag(When)], When));
        Assert.False(ClaimInterview.IsOpen([ClaimInterview.SettledTag(When), HullClaim.BookedTag(When)], When));
        Assert.True(ClaimInterview.IsOpen([HullClaim.BookedTag(When), ClaimInterview.SettledTag(When + 1)], When));
        Assert.Equal("claim:settled:172800", ClaimInterview.SettledTag(When));
    }

    /// <summary>The dev start parses both levels, and only them.</summary>
    [Fact]
    public void TheDevStartParsesLevelOneAndTwo()
    {
        Assert.Equal(1, HullClaim.CheatLevel("/map?claim=1"));
        Assert.Equal(2, HullClaim.CheatLevel("/map?claim=2"));
        Assert.Equal(2, HullClaim.CheatLevel("/map?x=1&claim=2#top"));
        Assert.Equal(0, HullClaim.CheatLevel("/map?claim=0"));
        Assert.Equal(0, HullClaim.CheatLevel("/map?claim=3"));
        Assert.Equal(0, HullClaim.CheatLevel("/map"));
        Assert.Equal(0, HullClaim.CheatLevel(null));
        Assert.True(HullClaim.CheatIn("/map?claim=2"));
        Assert.Equal(PreservationOffice.Cheat.Open, PreservationOffice.CheatIn("/map?claim=2"));
        Assert.Contains(DevStarts.All, e => e.Url == "/map?claim=2");
    }
}
