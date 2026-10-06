using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1151 slice 3 · <b>THE UNEASE FLASHBACK, IN CORE.</b> The canon bytes, the latch law, the margin law, the plate's
/// words and the dev start's pinned booking. The page's half (the plate, the dab and the two records at the places they
/// draw) is <c>TheSideOfficesKeepTheirHoursTests.Unease</c>.
/// </summary>
public sealed class ClaimInterviewUneaseTests
{
    /// <summary>
    /// <b>EVERY WORD IS THE CUT'S, BYTE FOR BYTE.</b> Single ASCII apostrophes (the issue's doubled one is comment
    /// escaping), the em dashes where the cut has them; and each string is in the interview's sweep.
    ///
    /// <para><b>Proven RED</b> by a doubled apostrophe in the margin line and by the caption's last clause trimmed.</para>
    /// </summary>
    [Fact]
    public void TheCanonIsTheCutsByteForByte()
    {
        Assert.Equal("the clause in her voice", ClaimInterview.FlashbackSubject);
        Assert.Equal(
            "Her voice does the clause in the company's cadence, and halfway through it your own hand answers — the pen, the cold of a counter, a date you could not produce now for any money. You signed this. The memory ends at the signature, as if the signature were a door.",
            ClaimInterview.FlashbackCaption);
        Assert.Equal("the clause answered in your own hand", ClaimInterview.UneaseShockLabel);
        Assert.Equal("In the margin, in a hand that is not yours: 'Read to claimant in full.'", ClaimInterview.MarginLine);
        Assert.Equal(
            "She read a clause today that I knew before she finished it. Filed under: things the fine print kept.",
            ClaimInterview.UneaseBookLine);
        Assert.Equal(4.0, ClaimInterview.UneaseNerve);
        Assert.Equal(Keepsake.MoneyStingNerve, ClaimInterview.UneaseNerve);   // the pendant's sting idiom, one dab

        List<string> prose = [.. ClaimInterview.AllProse()];
        foreach (string line in new[]
        {
            ClaimInterview.FlashbackCaption, ClaimInterview.UneaseShockLabel, ClaimInterview.MarginLine, ClaimInterview.UneaseBookLine,
        })
        {
            Assert.Contains(line, prose);
            Assert.DoesNotContain("''", line, StringComparison.Ordinal);
            Assert.DoesNotContain("\"", line, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <b>THE PLATE WEARS THE GENERIC STAMP AND THE CLAUSE'S CAPTION.</b> The title is the one every page-flashback
    /// wears (no new title; the pendant keeps its own), the caption is the cut's, the art is the one bleached plate, and
    /// no other subject's words moved.
    ///
    /// <para><b>Proven RED</b> by a new title arm for the subject and by the caption arm removed (the generic caption).</para>
    /// </summary>
    [Fact]
    public void ThePlateWearsTheGenericStampAndTheClausesCaption()
    {
        string generic = StoryBeats.Title(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId);
        Assert.Equal(generic, StoryBeats.Title(StoryBeats.Beat.Flashback, ClaimInterview.FlashbackSubject));
        Assert.EndsWith("A PAGE YOU DON'T REMEMBER WRITING", generic, StringComparison.Ordinal);
        Assert.Equal(ClaimInterview.FlashbackCaption, StoryBeats.Caption(StoryBeats.Beat.Flashback, ClaimInterview.FlashbackSubject));
        Assert.NotEqual(ClaimInterview.FlashbackCaption, StoryBeats.Caption(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId));
        Assert.NotEqual(StoryBeats.Title(StoryBeats.Beat.Flashback, Keepsake.PendantSubject), generic);
        Assert.Equal(StoryBeats.ArtFile(StoryBeats.Beat.Flashback), StoryBeats.ArtFile(StoryBeats.Beat.Flashback, ClaimInterview.FlashbackSubject));
        Assert.Null(ClaimInterview.FlashbackCaptionFor(Keepsake.PendantSubject));
        Assert.Null(ClaimInterview.FlashbackCaptionFor(null));
    }

    /// <summary>
    /// <b>THE LATCH: ONLY AN ADJUSTED OUTCOME, ONLY WHILE THE RUN'S TAG IS UNSPENT.</b>
    ///
    /// <para><b>Proven RED</b> by the outcome clause dropped (beat on PAID and DECLINED too) and by the tag clause dropped
    /// (beat on every ADJUSTED).</para>
    /// </summary>
    [Fact]
    public void OnlyTheFirstAdjustedOutcomeRaisesIt()
    {
        Assert.True(ClaimInterview.RaisesTheUnease(ClaimInterview.Outcome.Adjusted, []));
        Assert.False(ClaimInterview.RaisesTheUnease(ClaimInterview.Outcome.Paid, []));
        Assert.False(ClaimInterview.RaisesTheUnease(ClaimInterview.Outcome.Declined, []));
        string[] spent = ["claim:settled:5", ClaimInterview.UneaseTag];
        Assert.False(ClaimInterview.RaisesTheUnease(ClaimInterview.Outcome.Adjusted, spent));
        Assert.True(ClaimInterview.RaisesTheUnease(ClaimInterview.Outcome.Adjusted, ["claim:settled:5", "claim:unease-not"]));
        Assert.Equal("claim:unease", ClaimInterview.UneaseTag);
    }

    /// <summary>
    /// <b>THE MARGIN IS APPENDED ONCE, AND SURVIVES THE SHEET'S OWN ROW.</b> The line grows the sheet by one sentence; a
    /// second append is the same text; the row the vault stores round-trips it whole (positive control: the stored row
    /// holds the margin, and a sheet without it holds none).
    ///
    /// <para><b>Proven RED</b> by the append unguarded (the margin twice).</para>
    /// </summary>
    [Fact]
    public void TheMarginIsAppendedOnceAndTheRowKeepsIt()
    {
        string plain = NebulaRep.SigningMemoryFor(0);
        Assert.False(ClaimInterview.HasTheMargin(plain));
        string grown = ClaimInterview.WithTheMargin(plain);
        Assert.Equal(plain + " " + ClaimInterview.MarginLine, grown);
        Assert.Equal(grown, ClaimInterview.WithTheMargin(grown));
        Assert.Equal(1, CountOf(grown, ClaimInterview.MarginLine));

        var sheet = new HeldMemory.Sheet(
            NebulaRep.SigningMemoryId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money, grown, [], 12.5);
        Assert.Contains(ClaimInterview.MarginLine, sheet.Stored, StringComparison.Ordinal);
        Assert.True(HeldMemory.Sheet.TryParse(sheet.Stored, out HeldMemory.Sheet back));
        Assert.Equal(grown, back.Text);
        Assert.Equal(1, CountOf(back.Text, ClaimInterview.MarginLine));
        Assert.True(HeldMemory.Sheet.TryParse((sheet with { Text = plain }).Stored, out HeldMemory.Sheet bare));
        Assert.False(ClaimInterview.HasTheMargin(bare.Text));
        Assert.False(ClaimInterview.HasTheMargin(null));
    }

    private static int CountOf(string text, string needle)
    {
        int n = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }

    /// <summary>
    /// <b>THE DEV START'S BOOKING IS ADJUSTED WHICHEVER WAY QUESTION THREE GOES, BY THE REAL DICE.</b> The pinned moment
    /// is run through Core's own interview — outcome and clause — on both verdicts, never a forced roll; a sweep over a
    /// thousand other moments shows ADJUSTED is the house outcome, not an accident of this number.
    ///
    /// <para><b>Proven RED</b> by the constant moved to a moment that settles DECLINED under one verdict.</para>
    /// </summary>
    [Fact]
    public void TheDevStartsBookingIsAdjustedByTheRealDice()
    {
        var loss = new HullClaim.Loss(ClaimInterview.Claim3When, 20);
        foreach (bool accepted in new[] { true, false })
        {
            ClaimInterview.Settlement s = ClaimInterview.Settle(loss, accepted);
            Assert.Equal(ClaimInterview.Outcome.Adjusted, s.Outcome);
            Assert.InRange(s.Pick, 1, ClaimInterview.PoolSize);
        }

        int adjusted = Enumerable.Range(1, 1000).Count(w =>
            ClaimInterview.OutcomeOf(w, false) == ClaimInterview.Outcome.Adjusted);
        Assert.InRange(adjusted, 600, 730);   // 8 of 12 on the base weights
    }

    /// <summary>The dev start parses level three, and the signing variant, and only them.</summary>
    [Fact]
    public void TheDevStartParsesLevelThreeAndTheSigningVariant()
    {
        Assert.Equal(3, HullClaim.CheatLevel("/map?claim=3"));
        Assert.Equal(3, HullClaim.CheatLevel("/map?claim=3&signing=1"));
        Assert.Equal(0, HullClaim.CheatLevel("/map?claim=4"));
        Assert.True(HullClaim.SigningStaged("/map?claim=3&signing=1"));
        Assert.True(HullClaim.SigningStaged("/map?signing=1&claim=3#x"));
        Assert.False(HullClaim.SigningStaged("/map?claim=3"));
        Assert.False(HullClaim.SigningStaged("/map?claim=3&signing=0"));
        Assert.False(HullClaim.SigningStaged(null));
        Assert.Equal(PreservationOffice.Cheat.Open, PreservationOffice.CheatIn("/map?claim=3"));
        Assert.Single(DevStarts.All, e => e.Url == "/map?claim=3");
        Assert.Single(DevStarts.All, e => e.Url == "/map?claim=3&signing=1");
    }
}
