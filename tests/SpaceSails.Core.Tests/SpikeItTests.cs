using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1202 slice 2 · <b>SPIKE IT — the arithmetic and the words.</b> The page's half is driven on a live page in
/// <c>TheStoryCanBeSpikedTests</c>; what is pinned here is what Core decides alone: the lines verbatim, the one
/// outcome of three, the purse, the two moves and when each is on offer, the two papers, the contract's line of
/// state, her cadence and the dev latch. Every guard below was watched go red on the sabotage its summary names.
/// </summary>
public sealed class SpikeItTests
{
    // ── THE WORDS ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SEVENTEEN LINES, VERBATIM, AND NO EIGHTEENTH. Every line of slice 2's brief and the three of slice 3's
    /// (#1202 · THE TAIL AT HER TABLE), letter for letter; all of them in <see cref="SpikeIt.AllProse"/>; and the
    /// slices' own three files read for any sentence typed into a method that is not one of them.
    ///
    /// <para><b>RED</b> by a full stop added to the DEV line in <c>SpikeItIfAsked</c> (a sentence nobody
    /// authored), and by "third from the top" rewritten "second from the top" in <c>LeaveYourPageLine</c>.</para>
    /// </summary>
    [Fact]
    public void TheLinesAreVerbatimAndThereIsNoEighteenth()
    {
        Assert.Equal("SPIKE IT", SpikeIt.RowLabel);
        Assert.Equal("Somebody would rather the {Body} story did not run. {cr} if it does not; half if it runs different. Her pages are wherever she writes. Nobody said how.", SpikeIt.RowLine);
        Assert.Equal("A page arrives with the terms: a paragraph in nobody's hand, about {Body}, saying nothing at all in perfect grammar.", SpikeIt.TakenLine);
        Assert.Equal("She is at the far table with the recorder and a stack of pages, writing the way people write when the window is closing.", SpikeIt.AtHerTableLine);
        Assert.Equal("TAKE THE PAGES", SpikeIt.TakeThePagesLabel);
        Assert.Equal("Six pages, close-written, the top one still damp. The recorder is in her pocket; the pages are not.", SpikeIt.TakeThePagesLine);
        Assert.Equal("Took a stringer's pages off her table while she fed the machine. The window is {N} watches off.", SpikeIt.TookThePagesEntry);
        Assert.Equal("LEAVE YOUR PAGE", SpikeIt.LeaveYourPageLabel);
        Assert.Equal("Your page goes on the stack, third from the top, where a tired eye reads without looking.", SpikeIt.LeaveYourPageLine);
        Assert.Equal("The {Body} story did not run. The wire is one line shorter and only you know the shape of the hole.", SpikeIt.SpikedEntry);
        Assert.Equal("{Body} reports an orderly quarter. Sources close to the site describe the workforce as 'accounted for'. — R. Lind, for the wire", SpikeIt.AlteredStory);
        Assert.Equal("Her story ran. It is your sentence and her name.", SpikeIt.AlteredEntry);
        Assert.Equal("No credits. One line where the money would be: 'Noted that you tried.'", SpikeIt.LateLine);
        Assert.Equal("She is not at the table. The recorder is. Somebody will come back for it, or nobody will.", SpikeIt.GoneLine);
        Assert.Equal("The coat at the mouth of the tube did not look up. He did not need to.", SpikeIt.SeenTakeEntry);
        Assert.Equal("Her stack is squared, one corner folded where somebody else's thumb was.", SpikeIt.SquaredLine);
        Assert.Equal("No credits. One line where the money would be: 'Noted that you were seen.'", SpikeIt.SeenLateLine);
        Assert.Equal("press-swap", SpikeIt.SwapId);
        Assert.Equal("press-pages", SpikeIt.PagesId);

        var prose = SpikeIt.AllProse().ToList();
        Assert.Equal(17, prose.Count);
        Assert.Equal(prose.Count, prose.Distinct(StringComparer.Ordinal).Count());

        var sentences = new List<string>();
        foreach (string file in new[]
        {
            SourceOf("src", "SpaceSails.Core", "SpikeIt.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.SpikeIt.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.SpikeIt.Seen.cs"),
        })
        {
            foreach (Match m in Regex.Matches(WithoutComments(file), "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                string text = m.Value.Trim('"');
                if (text.Contains(' ', StringComparison.Ordinal) && text.EndsWith('.')
                    && !prose.Any(p => p.Contains(text, StringComparison.Ordinal)))
                {
                    sentences.Add(m.Value);
                }
            }
        }

        Assert.True(sentences.Count == 0,
            "this slice's files carry a sentence nobody authored: " + string.Join(", ", sentences));
    }

    /// <summary>The fills put the game's words where the canon's braces are, and nowhere else.</summary>
    [Fact]
    public void TheFillsReplaceOnlyTheirOwnBraces()
    {
        Assert.Equal(
            "Somebody would rather the Luna story did not run. 2,500 cr if it does not; half if it runs different. Her pages are wherever she writes. Nobody said how.",
            SpikeIt.Row("Luna", 2500));
        Assert.Equal(
            "Took a stringer's pages off her table while she fed the machine. The window is 7 watches off.",
            SpikeIt.TookThePages(7));
        foreach (string line in new[] { SpikeIt.Taken("Luna"), SpikeIt.Spiked("Luna"), SpikeIt.Altered("Luna") })
        {
            Assert.DoesNotContain("{", line, StringComparison.Ordinal);
            Assert.Contains("Luna", line, StringComparison.Ordinal);
        }

        Assert.EndsWith(" — R. Lind, for the wire", SpikeIt.Altered("Luna"), StringComparison.Ordinal);
    }

    /// <summary>The window is counted in whole watches, rounded up, and a passed window is nought.</summary>
    [Fact]
    public void TheWindowIsCountedInWatchesRoundedUp()
    {
        Assert.Equal(1, SpikeIt.WatchesUntil(100.0, 99.0));
        Assert.Equal(1, SpikeIt.WatchesUntil(PatronRota.WatchSeconds, 0));
        Assert.Equal(2, SpikeIt.WatchesUntil(PatronRota.WatchSeconds + 1, 0));
        Assert.Equal(0, SpikeIt.WatchesUntil(100.0, 100.0));
        Assert.Equal(12, SpikeIt.WatchesUntil(CarryThePress.StoryAfterSeconds, CarryThePress.FloorAfterStorySeconds));
    }

    // ── THE WINDOW ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXACTLY ONE OF THREE, BY WHERE HER PAGES ARE: taken (in the satchel or binned) is SPIKED, back with the
    /// client's page is ALTERED, on her table is LATE — three different outcomes for three states, none of them
    /// None, and only the two that printed a story ran.
    ///
    /// <para><b>RED</b> by <c>Pages.Taken => Outcome.Late</c> in <c>AtTheWindow</c> (two states, one outcome),
    /// and by <c>Ran</c> answering true for SPIKED (a floor reaction about a story nobody printed).</para>
    /// </summary>
    [Fact]
    public void TheWindowMakesExactlyOneOfThree()
    {
        var outcomes = Enum.GetValues<SpikeIt.Pages>().Select(SpikeIt.AtTheWindow).ToList();
        Assert.Equal(3, outcomes.Count);
        Assert.Equal(3, outcomes.Distinct().Count());
        Assert.DoesNotContain(SpikeIt.Outcome.None, outcomes);
        Assert.Equal(SpikeIt.Outcome.Spiked, SpikeIt.AtTheWindow(SpikeIt.Pages.Taken));
        Assert.Equal(SpikeIt.Outcome.Altered, SpikeIt.AtTheWindow(SpikeIt.Pages.Swapped));
        Assert.Equal(SpikeIt.Outcome.Late, SpikeIt.AtTheWindow(SpikeIt.Pages.OnHerTable));

        Assert.False(SpikeIt.Ran(SpikeIt.Outcome.Spiked));
        Assert.True(SpikeIt.Ran(SpikeIt.Outcome.Altered));
        Assert.True(SpikeIt.Ran(SpikeIt.Outcome.Late));
        Assert.False(SpikeIt.Ran(SpikeIt.Outcome.None));
    }

    /// <summary>The desk pays the full purse if it did not run, half if it ran different, nothing if it ran.
    /// <b>RED</b> by paying the full purse for ALTERED.</summary>
    [Fact]
    public void TheDeskPaysFullHalfOrNothing()
    {
        int purse = SpikeIt.Purse(1250);
        Assert.Equal(2500, purse);
        Assert.Equal(purse, SpikeIt.Pays(SpikeIt.Outcome.Spiked, purse));
        Assert.Equal(purse / 2, SpikeIt.Pays(SpikeIt.Outcome.Altered, purse));
        Assert.Equal(0, SpikeIt.Pays(SpikeIt.Outcome.Late, purse));
        Assert.Equal(0, SpikeIt.Pays(SpikeIt.Outcome.None, purse));
    }

    // ── THE TWO MOVES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MOVES ARE ABSENT WHILE SHE SITS, and while the captain is not alone; TAKE THE PAGES only while they
    /// are on her table; LEAVE YOUR PAGE only once they are taken and the captain holds both them and the
    /// client's page; nothing once they are back.
    ///
    /// <para><b>RED</b> by dropping <c>!sheIsAway</c> from <c>MoveOnOffer</c> (TAKE THE PAGES with her in the
    /// chair), and by dropping <c>holdsThePages</c> (LEAVE YOUR PAGE with the pages in a bin).</para>
    /// </summary>
    [Fact]
    public void TheMovesAreOnOfferOnlyWhileSheIsAwayAndHeIsAlone()
    {
        foreach (SpikeIt.Pages pages in Enum.GetValues<SpikeIt.Pages>())
        {
            foreach (bool swap in new[] { true, false })
            {
                foreach (bool held in new[] { true, false })
                {
                    Assert.Null(SpikeIt.MoveOnOffer(pages, sheIsAway: false, alone: true, swap, held));
                    Assert.Null(SpikeIt.MoveOnOffer(pages, sheIsAway: true, alone: false, swap, held));
                }
            }
        }

        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.MoveOnOffer(SpikeIt.Pages.OnHerTable, true, true, true, false));
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.MoveOnOffer(SpikeIt.Pages.OnHerTable, true, true, false, false));
        Assert.Equal(SpikeIt.LeaveYourPage, SpikeIt.MoveOnOffer(SpikeIt.Pages.Taken, true, true, true, true));
        Assert.Null(SpikeIt.MoveOnOffer(SpikeIt.Pages.Taken, true, true, false, true));
        Assert.Null(SpikeIt.MoveOnOffer(SpikeIt.Pages.Taken, true, true, true, false));
        Assert.Null(SpikeIt.MoveOnOffer(SpikeIt.Pages.Swapped, true, true, true, true));
    }

    /// <summary>
    /// THE CARD: one move before the last, the other feature's move left where it was, idempotent both ways,
    /// and the plain card move for move when there is nothing to offer. <b>RED</b> by filtering every move but
    /// the last out of the card in <c>TheTable</c> (the chalk mark's FEEL UNDER THE LIP went with it).
    /// </summary>
    [Fact]
    public void TheCardCarriesOneMoveBeforeItsLastAndLeavesTheRest()
    {
        Encounter.Scene plain = SittingAlone.TheTable();
        Encounter.Scene chalked = ChalkMark.TheTable(plain, goodsUnderThisLip: true);
        var before = chalked.Moves.Select(m => m.Id).ToList();

        Encounter.Scene take = SpikeIt.TheTable(chalked, SpikeIt.TakeThePages);
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(take));
        Assert.Equal(SpikeIt.TakeThePages, take.Moves[^2].Id);
        Assert.Equal(SpikeIt.TakeThePagesLabel, take.Moves[^2].Label);
        Assert.True(ChalkMark.Offers(take));
        Assert.Equal(before.Count + 1, take.Moves.Count);
        Assert.Equal(take.Moves.Select(m => m.Id), SpikeIt.TheTable(take, SpikeIt.TakeThePages).Moves.Select(m => m.Id));

        Encounter.Scene leave = SpikeIt.TheTable(take, SpikeIt.LeaveYourPage);
        Assert.Equal(SpikeIt.LeaveYourPage, SpikeIt.Offers(leave));
        Assert.Equal(SpikeIt.LeaveYourPageLabel, leave.Moves[^2].Label);
        Assert.Equal(before.Count + 1, leave.Moves.Count);

        Encounter.Scene none = SpikeIt.TheTable(leave, null);
        Assert.Null(SpikeIt.Offers(none));
        Assert.Equal(before, none.Moves.Select(m => m.Id));
    }

    // ── THE PAPERS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TWO PAPERS ARE ONE SHEET EACH, THEIR PROSE REBUILT FROM THE ID: the client's page reads as the line it
    /// arrived with (the body filled from the id), her pages as the move's own line; both are authored (never
    /// torn into "page 3 of 4"), both are evidence a bin takes, and their titles are their opening words.
    /// <b>RED</b> by dropping <c>SpikeIt.IsAuthored</c> from <c>FieldClue.IsAuthored</c> (her pages became a
    /// seeded four-sheet folder).
    /// </summary>
    [Fact]
    public void TheTwoPapersAreAuthoredSheetsRebuiltFromTheirIds()
    {
        Satchel.Item swap = SpikeIt.TheSwap("Luna");
        Satchel.Item pages = SpikeIt.ThePages();
        Assert.Equal(Satchel.Kind.Paper, swap.Kind);
        Assert.Equal(Satchel.Kind.Paper, pages.Kind);
        Assert.True(SpikeIt.IsTheSwap(swap.Id));
        Assert.False(SpikeIt.IsTheSwap(pages.Id));
        Assert.True(SpikeIt.IsThePages(pages.Id));
        Assert.True(Satchel.Item.TryParse(swap.Stored, out Satchel.Item back));
        Assert.Equal(swap, back);

        foreach (Satchel.Item item in new[] { swap, pages })
        {
            Assert.True(FieldClue.IsAuthored(item.Id));
            Assert.Equal(1, PageGranularity.PagesIn(item.Id));
            Assert.True(RipAndBin.IsEvidence(item.Kind));
        }

        Assert.Equal(SpikeIt.Taken("Luna"), FieldClue.Document(swap.Id));
        Assert.Equal("A page arrives with the terms", FieldClue.Title(swap.Id));
        Assert.Equal(SpikeIt.TakeThePagesLine, FieldClue.Document(pages.Id));
        Assert.Equal("Six pages, close-written", FieldClue.Title(pages.Id));
        CarriedObject.Reveal card = CarriedObject.PaperReveal(pages.Id);
        Assert.Equal(("", "Six pages, close-written", SpikeIt.TakeThePagesLine), (card.ArtUrl, card.Label, card.Story));
    }

    // ── THE CONTRACT'S LINE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SPIKE RIDES SLICE 1's ONE LINE: every new key round-trips, and a contract with no spike taken writes
    /// the line slice 1 wrote, to the byte — so a slice-1 save reads the same and nothing a slice-1 contract
    /// pins moves. <b>RED</b> by writing the spike's keys unconditionally.
    /// </summary>
    [Fact]
    public void TheSpikeRidesTheSameLineAndSliceOnesLineIsUntouched()
    {
        var plain = new CarryThePress.Passage(2, Landed: true, Tin: true, TurnedIn: 1234.5, Printed: true);
        Assert.Equal("site=2;landed=1;walked=0;tin=1;in=1234.5;printed=1;floor=0", plain.Write());
        Assert.Equal(plain, CarryThePress.Passage.Read(plain.Write()));

        var spiked = plain with
        {
            Spike = true, Pages = SpikeIt.Pages.Swapped, Seen = SpikeIt.HerLine.Told, Outcome = SpikeIt.Outcome.Altered,
            Paid = true, Gone = true,
        };
        Assert.Equal(spiked, CarryThePress.Passage.Read(spiked.Write()));
        Assert.Equal(spiked with { Paid = false, Gone = false },
            CarryThePress.Passage.Read((spiked with { Paid = false, Gone = false }).Write()));
        Assert.Equal(CarryThePress.Passage.Read("site=1;spike=1;pages=9;out=7"),
            new CarryThePress.Passage(1, Spike: true));
    }

    // ── HER CADENCE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Her cadence is seeded off her contract: the same on every machine, inside its bounds, and not the
    /// same for every contract.</summary>
    [Fact]
    public void HerCadenceIsSeededAndBounded()
    {
        var writes = new HashSet<double>();
        for (int i = 0; i < 40; i++)
        {
            string id = $"press-{i}";
            double w = SpikeIt.WritesFor(id), f = SpikeIt.FeedsFor(id);
            Assert.Equal(w, SpikeIt.WritesFor(id));
            Assert.InRange(w, SpikeIt.WritesAtLeastSeconds, SpikeIt.WritesAtMostSeconds);
            Assert.InRange(f, SpikeIt.FeedsAtLeastSeconds, SpikeIt.FeedsAtMostSeconds);
            writes.Add(w);
        }

        Assert.True(writes.Count > 5, "every contract writes for the same spell — the seed is not reading the contract.");
        Assert.Equal(1, SpikeIt.HerTable);
        Assert.Equal(ObservationWalk.HavenId, SpikeIt.Haven);
    }

    /// <summary>The dev latch reads <c>spike=</c> and nothing else.</summary>
    [Fact]
    public void TheDevLatchReadsItsOwnKey()
    {
        Assert.Equal(SpikeIt.Cheat.Pending, SpikeIt.CheatIn("https://x/map?dock=selene-gate&ashore=1&spike=1"));
        Assert.Equal(SpikeIt.Cheat.Spiked, SpikeIt.CheatIn("https://x/map?dock=selene-gate&spike=spiked"));
        Assert.Equal(SpikeIt.Cheat.None, SpikeIt.CheatIn("https://x/map?dock=selene-gate&press=1"));
        Assert.Equal(SpikeIt.Cheat.None, SpikeIt.CheatIn("https://x/map?spike=nope"));
        Assert.Equal(SpikeIt.Cheat.None, SpikeIt.CheatIn(null));
    }

    private static string WithoutComments(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, @"//[^\n]*", " ");
    }

    private static string SourceOf(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));

    /// <summary>
    /// #1202 slice 2 QA · HER LINE IS DECIDED ON ENTERING: an entry with her at her table and her pages on it tells
    /// it; an entry with her away spends the visit, and nothing tells it until the captain has been out; once told,
    /// never again; and with her pages taken (or swapped) no entry ever tells it. Slice 2's <c>seen=0</c>/<c>seen=1</c>
    /// read back as they were written.
    ///
    /// <para><b>RED</b> by letting <see cref="SpikeIt.HerLine.NotThisVisit"/> tell when she is back at her table
    /// (the QA report: told the frame she sat down), and by dropping the pages clause (told after a take).</para>
    /// </summary>
    [Fact]
    public void HerLineIsDecidedOnEntering()
    {
        const SpikeIt.Pages On = SpikeIt.Pages.OnHerTable;
        Assert.Equal((SpikeIt.HerLine.NotYet, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotYet, false, true, On));
        Assert.Equal((SpikeIt.HerLine.Told, true), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotYet, true, true, On));
        Assert.Equal((SpikeIt.HerLine.NotThisVisit, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotYet, true, false, On));
        Assert.Equal((SpikeIt.HerLine.NotThisVisit, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotThisVisit, true, true, On));
        Assert.Equal((SpikeIt.HerLine.NotYet, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotThisVisit, false, true, On));
        Assert.Equal((SpikeIt.HerLine.Told, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.Told, true, true, On));
        Assert.Equal((SpikeIt.HerLine.Told, false), SpikeIt.HerLineOnEntering(SpikeIt.HerLine.Told, false, true, On));
        foreach (SpikeIt.Pages gone in new[] { SpikeIt.Pages.Taken, SpikeIt.Pages.Swapped })
        {
            Assert.False(SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotYet, true, true, gone).Tell);
            Assert.False(SpikeIt.HerLineOnEntering(SpikeIt.HerLine.NotThisVisit, true, true, gone).Tell);
        }

        Assert.Equal(SpikeIt.HerLine.NotYet, CarryThePress.Passage.Read("spike=1;seen=0").Seen);
        Assert.Equal(SpikeIt.HerLine.Told, CarryThePress.Passage.Read("spike=1;seen=1").Seen);
        var away = new CarryThePress.Passage(0, Spike: true, Seen: SpikeIt.HerLine.NotThisVisit);
        Assert.Contains(";seen=2;", away.Write(), StringComparison.Ordinal);
        Assert.Equal(away, CarryThePress.Passage.Read(away.Write()));
    }
}
