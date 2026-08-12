using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #836 · WHICH ONE OF YOU IS HE MEETING — the Fletch wallet, and the four laws it lives or dies by.
///
/// <para>Owner, evening playtest 2026-08-11: <i>"I think I should be able to pick the badge I show the guard...
/// like Fletch ... suppose we have 4 different ID's ... one of them real ... but not authorized for access to
/// all places we roam"</i>. And, the morning after, the clause that decides how the chooser may help:
/// <i>"when we select Id we should have some idea about which Id would be ok :-D"</i> — from the captain's own
/// knowledge, never from an oracle.</para>
///
/// <para><b>The one that had to be proven RED</b> is
/// <see cref="TheReadTakesTheOnePaperHeWasHandedAndNeverTheBestOne"/>. Reverting
/// <see cref="PatrolBeat.TheGuardReads"/> to the shipped behaviour — walk the satchel, answer with this
/// site's pass if it is anywhere in it — makes the bad-choice case come back SATISFIED, which is the sim
/// playing the captain's hand for him and the whole feature undone. Watched fail on exactly that revert.</para>
/// </summary>
public sealed class TheFletchWalletTests
{
    private const string Plate = "◈ A CONTRACT GUARD, WALKING THE ROUND";
    private const string Here = "luna";
    private const string Elsewhere = "europa";

    private static Satchel.Item TheRealOne => PatrolBeat.Badge(Here);

    private static Satchel.Item TheOtherSitesPass => PatrolBeat.Badge(Elsewhere);

    private static Satchel.Item TheCageChit => CanteenTable.Chit(underAnotherName: true);

    // ── THE FAN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>ONE PAPER IS EXACTLY TODAY. The chooser is a thing that happens when there is a decision, and
    /// a captain carrying one pass must never be asked which one of it he wants.</summary>
    [Fact]
    public void TheFanOpensOnlyWhenThereIsAChoiceToMake()
    {
        Assert.False(WalletChoice.Fans(Here, null), "an empty wallet fanned itself.");
        Assert.False(WalletChoice.Fans(Here, [TheRealOne]));
        Assert.False(WalletChoice.Fans(Here, [TheCageChit]));
        Assert.True(WalletChoice.Fans(Here, [TheRealOne, TheCageChit]));

        // …and a card that runs a SHAFT is not an identity. An authority is an office vouching for a hole;
        // handing one to a man who wants to know who you are is not a move this game has.
        Satchel.Item authority = new(
            Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(Here, 1).Id);
        Assert.False(WalletChoice.Fans(Here, [TheRealOne, authority]));
        Assert.DoesNotContain(authority, WalletChoice.Fan(Here, [TheRealOne, authority]));

        // Nor is a handful of rounds, a file on somebody, or anything else in the pockets.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            Assert.Equal(
                kind is Satchel.Kind.Badge or Satchel.Kind.Chit, WalletChoice.AGuardWouldReadIt(kind));
        }
    }

    /// <summary>The fan's order is a LAW, not a convenience: this site's own pass, then the other sites', then
    /// the chits — and it does not depend on the order things were picked up in, because a list that
    /// re-sorted itself between the hail and the arrival would be a captain handing over a paper they did not
    /// pick.</summary>
    [Fact]
    public void TheFanIsInOneOrderWhicheverWayThePocketFilled()
    {
        Satchel.Item[] oneWay = [TheCageChit, TheOtherSitesPass, TheRealOne];
        Satchel.Item[] theOther = [TheRealOne, TheCageChit, TheOtherSitesPass];

        Assert.Equal(WalletChoice.Fan(Here, oneWay), WalletChoice.Fan(Here, theOther));
        Assert.Equal(
            [TheRealOne, TheOtherSitesPass, TheCageChit],
            WalletChoice.Fan(Here, oneWay).ToArray());

        // Stand on the other moon and the same wallet fans in the other order — the site decides which pass
        // is "the real one" here, and nothing else does.
        Assert.Equal(TheOtherSitesPass, WalletChoice.Fan(Elsewhere, oneWay)[0]);
    }

    // ── THE DEFAULT ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LAST SHOWN HERE, ELSE THE REAL ONE — and deterministic to the letter, because the fan opens over a
    /// walk-up a captain is allowed to ignore entirely. Whatever is in the hand when he arrives has to be the
    /// paper a reasonable person would already be holding.
    /// </summary>
    [Fact]
    public void TheDefaultIsTheRealOneUntilTheBookSaysOtherwise()
    {
        Satchel.Item[] wallet = [TheCageChit, TheOtherSitesPass, TheRealOne];

        // A fresh captain: the site's own pass, every time, on every ordering.
        Assert.Equal(TheRealOne, WalletChoice.DefaultFor(Here, wallet, null));
        Assert.Equal(TheRealOne, WalletChoice.DefaultFor(Here, wallet, []));

        // A read filed somewhere ELSE says nothing about this building.
        IReadOnlyList<WalletChoice.Shown> abroad = WalletChoice.Remember(
            null, new WalletChoice.Shown(TheCageChit.Id, Elsewhere, -2, WalletChoice.Outcome.WrongPaper));
        Assert.Equal(TheRealOne, WalletChoice.DefaultFor(Here, wallet, abroad));

        // …and the habit, once there is one: the last paper handed to anybody in this building.
        IReadOnlyList<WalletChoice.Shown> habit = WalletChoice.Remember(
            abroad, new WalletChoice.Shown(TheCageChit.Id, Here, -1, WalletChoice.Outcome.WrongPaper));
        Assert.Equal(TheCageChit, WalletChoice.DefaultFor(Here, wallet, habit));

        // A habit whose paper is no longer in the wallet is not a habit — it is a pass in somebody's breast
        // pocket, and the hand falls back to the fan's own first band rather than to nothing.
        Assert.Equal(
            TheRealOne, WalletChoice.DefaultFor(Here, [TheRealOne, TheOtherSitesPass], habit));

        // An empty wallet defaults to an empty hand, which is the rung the read already has a sentence for.
        Assert.Null(WalletChoice.DefaultFor(Here, [], habit));
    }

    // ── THE READ ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE READS WHAT YOU GAVE HIM. The load-bearing guard of the whole issue: a wallet holding a good paper
    /// AND a bad one, with the bad one chosen, must produce the bad outcome.
    ///
    /// <remarks>PROVEN RED: restore the shipped read — <c>if (BadgeHeld(bodyId, carried)) return
    /// SatisfiedLine;</c> over the satchel — and the two <c>WrongPaper</c>/<c>WrongSite</c> cases below come
    /// back satisfied ("the bad paper was quietly rescued by the sim"). That is exactly the automatic read
    /// optimising, which is the sim lying about the fiction.</remarks>
    /// </summary>
    [Fact]
    public void TheReadTakesTheOnePaperHeWasHandedAndNeverTheBestOne()
    {
        // The wallet is the same in every case below. Only the hand changes.
        Satchel.Item[] wallet = [TheRealOne, TheOtherSitesPass, TheCageChit];
        Assert.True(PatrolBeat.BadgeHeld(Here, wallet), "the good paper is in this wallet the whole time.");

        PatrolBeat.Read good = PatrolBeat.TheGuardReads(Here, Plate, TheRealOne);
        Assert.True(good.Satisfied);
        Assert.Equal(PatrolBeat.SatisfiedLine, good.Line);
        Assert.Null(good.Consequence);

        PatrolBeat.Read wrongPaper = PatrolBeat.TheGuardReads(Here, Plate, TheCageChit);
        Assert.False(wrongPaper.Satisfied, "the chit was chosen and the good pass answered for it.");
        Assert.Equal(PatrolBeat.WrongPaperLine, wrongPaper.Line);
        Assert.Equal(PatrolBeat.EscortLine, wrongPaper.Consequence);

        PatrolBeat.Read wrongSite = PatrolBeat.TheGuardReads(Here, Plate, TheOtherSitesPass);
        Assert.False(wrongSite.Satisfied, "the other site's pass was chosen and the good pass answered for it.");
        Assert.Equal(PatrolBeat.WrongSiteLine(Elsewhere), wrongSite.Line);

        PatrolBeat.Read nothing = PatrolBeat.TheGuardReads(Here, Plate, null);
        Assert.False(nothing.Satisfied);
        Assert.Equal(PatrolBeat.NothingLine, nothing.Line);
    }

    /// <summary>All four rungs are reachable BY CHOICE out of one fixed wallet, which is the issue's own
    /// acceptance test: drive every paper through one guard and get every answer.</summary>
    [Fact]
    public void EveryRungIsReachableFromOneWalletByChoosingDifferently()
    {
        Satchel.Item[] wallet = [TheRealOne, TheOtherSitesPass, TheCageChit];

        var answers = new HashSet<string>(StringComparer.Ordinal);
        foreach (Satchel.Item paper in WalletChoice.Fan(Here, wallet))
        {
            answers.Add(PatrolBeat.TheGuardReads(Here, Plate, paper).Line);
        }
        answers.Add(PatrolBeat.TheGuardReads(Here, Plate, null).Line);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                PatrolBeat.SatisfiedLine,
                PatrolBeat.WrongSiteLine(Elsewhere),
                PatrolBeat.WrongPaperLine,
                PatrolBeat.NothingLine,
            },
            answers);
    }

    /// <summary>ONE LADDER, TWO READERS. The card's prose and the book's note are both composed off
    /// <see cref="WalletChoice.WhatHappens"/>, so a challenge cannot be told one way and remembered
    /// another — the third named bug class, in the one system whose whole register is procedure.</summary>
    [Fact]
    public void TheCardAndTheBookAgreeBecauseTheyAskTheSameQuestion()
    {
        foreach (Satchel.Item? handed in new Satchel.Item?[]
                 { null, TheRealOne, TheOtherSitesPass, TheCageChit })
        {
            WalletChoice.Outcome how = WalletChoice.WhatHappens(Here, handed);
            PatrolBeat.Read read = PatrolBeat.TheGuardReads(Here, Plate, handed);
            Assert.Equal(how == WalletChoice.Outcome.Worked, read.Satisfied);
        }
    }

    // ── THE ROW, AND THE ORACLE IT MAY NOT BE ─────────────────────────────────────────────────────────

    /// <summary>
    /// A FRESH CAPTAIN KNOWS NOTHING, AND THE ROWS SAY SO. The anti-oracle guard the owner asked for in
    /// writing: <i>a test seeds a fresh captain and asserts the chooser shows "never shown" everywhere; an
    /// omniscient hint that knows the guard's answer before the guard does is the sim leaking through the
    /// fiction.</i>
    ///
    /// <para>And the sharper half of it: with an empty book the hint on the paper that WOULD work is
    /// character-for-character the hint on the paper that would get you walked to the lift. There is nothing
    /// in a fresh row to read the answer off.</para>
    /// </summary>
    [Fact]
    public void AFreshCaptainsWalletSaysNeverShownOnEveryRow()
    {
        Satchel.Item[] wallet = [TheRealOne, TheOtherSitesPass, TheCageChit];

        foreach (Satchel.Item paper in WalletChoice.Fan(Here, wallet))
        {
            Assert.Equal(WalletChoice.NeverShownLine, WalletChoice.HistoryLine(paper, Here, null));
            Assert.Equal(WalletChoice.NeverShownLine, WalletChoice.HistoryLine(paper, Here, []));
        }

        Assert.Equal(
            WalletChoice.HistoryLine(TheRealOne, Here, null),
            WalletChoice.HistoryLine(TheCageChit, Here, null));
    }

    /// <summary>
    /// THE HINT IS DERIVED FROM FILED ROWS AND FROM NOTHING ELSE — and it is the captain's own book, so it
    /// only ever speaks about the building they are standing in.
    /// </summary>
    [Fact]
    public void TheBookOnlySaysWhatTheBookWasToldAndOnlyAboutHere()
    {
        IReadOnlyList<WalletChoice.Shown> book = null!;
        book = WalletChoice.Remember(
            book, new WalletChoice.Shown(TheRealOne.Id, Here, -1, WalletChoice.Outcome.Worked));
        Assert.Equal("worked here, once", WalletChoice.HistoryLine(TheRealOne, Here, book));

        book = WalletChoice.Remember(
            book, new WalletChoice.Shown(TheRealOne.Id, Here, -2, WalletChoice.Outcome.Worked));
        Assert.Equal("worked here, twice", WalletChoice.HistoryLine(TheRealOne, Here, book));

        // The other moon's rows are the other moon's business.
        Assert.Equal(WalletChoice.NeverShownLine, WalletChoice.HistoryLine(TheRealOne, Elsewhere, book));

        // A refusal names the floor it happened on, off the building's own plate, and why in the captain's
        // own shorthand — the owner's example, verbatim in shape: "refused on B2 · wrong site code".
        book = WalletChoice.Remember(
            book, new WalletChoice.Shown(TheOtherSitesPass.Id, Here, -2, WalletChoice.Outcome.WrongSite));
        string refused = WalletChoice.HistoryLine(TheOtherSitesPass, Here, book);
        Assert.Contains("refused on B2", refused, StringComparison.Ordinal);
        Assert.Contains(WalletChoice.ReasonTag(WalletChoice.Outcome.WrongSite), refused, StringComparison.Ordinal);
        Assert.StartsWith("B2", WalletChoice.FloorTag(Here, -2), StringComparison.Ordinal);

        // …and one paper's history never leaks onto another's row.
        Assert.Equal("worked here, twice", WalletChoice.HistoryLine(TheRealOne, Here, book));
        Assert.Equal(WalletChoice.NeverShownLine, WalletChoice.HistoryLine(TheCageChit, Here, book));

        // Both histories on one paper: the row says both, in the order a person would say them.
        book = WalletChoice.Remember(
            book, new WalletChoice.Shown(TheRealOne.Id, Here, -3, WalletChoice.Outcome.WrongPaper));
        string both = WalletChoice.HistoryLine(TheRealOne, Here, book);
        Assert.Contains("worked here, twice", both, StringComparison.Ordinal);
        Assert.Contains("refused on B3", both, StringComparison.Ordinal);
    }

    /// <summary>The row shows the paper's own printed face and nothing a designer typed twice: the title
    /// comes from the file that prints it, and the name is the captain's own except on the one paper that was
    /// never made out to them.</summary>
    [Fact]
    public void TheRowShowsThePapersOwnFaceAndTheNameOnIt()
    {
        Assert.Equal(PatrolBeat.BadgeTitle(Here), WalletChoice.Claims(TheRealOne));
        Assert.Equal(PatrolBeat.BadgeTitle(Elsewhere), WalletChoice.Claims(TheOtherSitesPass));
        Assert.Equal(CanteenTable.ChitTitle, WalletChoice.Claims(TheCageChit));

        Assert.Equal(PatrolBeat.BadgeGlyph, WalletChoice.GlyphOf(TheRealOne));
        Assert.Equal(CanteenTable.ChitGlyph, WalletChoice.GlyphOf(TheCageChit));

        Assert.Equal("HALVORSEN", WalletChoice.NameOn(TheRealOne, "HALVORSEN"));
        Assert.Equal("HALVORSEN", WalletChoice.NameOn(CanteenTable.Chit(underAnotherName: false), "HALVORSEN"));

        // #718's chit is the one paper in the game that is not made out to you, and the wallet says so
        // rather than pretending the captain forgot.
        Assert.Equal(WalletChoice.AnotherNameLine, WalletChoice.NameOn(TheCageChit, "HALVORSEN"));

        // The claim is never a verdict. Nothing printed on a row tells the captain how it will go.
        foreach (Satchel.Item paper in new[] { TheRealOne, TheOtherSitesPass, TheCageChit })
        {
            foreach (string oracle in new[] { "will work", "good here", "safe", "%", "✓", "recommend" })
            {
                Assert.DoesNotContain(oracle, WalletChoice.Claims(paper), StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── THE PAPER TRAIL ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Every read files a line, and the line NAMES THE PAPER — #836's second half, and the owner's
    /// own reading of it: <i>every challenge writes down which identity you showed.</i></summary>
    [Fact]
    public void EveryReadFilesALineThatNamesTheNameYouGave()
    {
        foreach (Satchel.Item paper in new[] { TheRealOne, TheOtherSitesPass, TheCageChit })
        {
            WalletChoice.Outcome how = WalletChoice.WhatHappens(Here, paper);
            string note = WalletChoice.ShownNote(paper, Here, -2, how, "HALVORSEN");

            Assert.Contains(WalletChoice.NameOn(paper, "HALVORSEN"), note, StringComparison.Ordinal);
            Assert.Contains(WalletChoice.Claims(paper), note, StringComparison.Ordinal);
            Assert.Contains("B2", note, StringComparison.Ordinal);
        }

        // An empty hand is filed too, and it names no paper because there was none.
        string emptyHanded = WalletChoice.ShownNote(
            null, Here, -2, WalletChoice.Outcome.NothingShown, "HALVORSEN");
        Assert.DoesNotContain("HALVORSEN", emptyHanded, StringComparison.Ordinal);
        Assert.Contains("B2", emptyHanded, StringComparison.Ordinal);
    }

    /// <summary>The book is capped like every other durable list in this game, and the newest lines are the
    /// ones that survive — a hint built off the oldest reads in a captain's career would be the wrong
    /// memory.</summary>
    [Fact]
    public void TheBookIsAPocketBookAndNotAnArchive()
    {
        IReadOnlyList<WalletChoice.Shown> book = [];
        for (int i = 0; i < WalletChoice.BookKeeps + 25; i++)
        {
            book = WalletChoice.Remember(
                book, new WalletChoice.Shown(TheRealOne.Id, Here, -1, WalletChoice.Outcome.Worked));
        }

        Assert.Equal(WalletChoice.BookKeeps, book.Count);
        Assert.Contains($"worked here, {WalletChoice.BookKeeps} times", WalletChoice.HistoryLine(TheRealOne, Here, book));
    }

    /// <summary>A row round-trips through a string — ids keep every colon they came with, which is
    /// <see cref="Satchel.Item.Stored"/>'s own lesson and the reason the id goes last.</summary>
    [Fact]
    public void ARowSurvivesBeingWrittenDown()
    {
        foreach (WalletChoice.Outcome how in Enum.GetValues<WalletChoice.Outcome>())
        {
            var row = new WalletChoice.Shown("badge:luna:with:colons", Here, -7, how);
            Assert.True(WalletChoice.Shown.TryParse(row.Stored, out WalletChoice.Shown back));
            Assert.Equal(row, back);
        }

        foreach (string junk in new[] { "", "not a row", "9:-1:luna:badge:luna", "0:x:luna:badge", "0:-1::id" })
        {
            Assert.False(WalletChoice.Shown.TryParse(junk, out _), $"the book swallowed `{junk}`.");
        }
    }

    /// <summary>…AND IT SURVIVES THE VAULT. "Worked here, twice" is worth nothing if the book forgets between
    /// excursions; a pre-#836 file simply has no section and every row honestly reads
    /// <see cref="WalletChoice.NeverShownLine"/>.</summary>
    [Fact]
    public void ThePaperTrailSurvivesTheVault()
    {
        IReadOnlyList<WalletChoice.Shown> book = WalletChoice.Remember(
            WalletChoice.Remember(
                null, new WalletChoice.Shown(TheRealOne.Id, Here, -1, WalletChoice.Outcome.Worked)),
            new WalletChoice.Shown(TheOtherSitesPass.Id, Here, -2, WalletChoice.Outcome.WrongSite));

        var vault = new Vault
        {
            PapersShown = new PapersShownSection { Shown = [.. book.Select(r => r.Stored)] },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(loaded.Tampered);

        List<WalletChoice.Shown> back = [];
        foreach (string stored in loaded.PapersShown!.Shown)
        {
            Assert.True(WalletChoice.Shown.TryParse(stored, out WalletChoice.Shown row));
            back.Add(row);
        }

        Assert.Equal(book, back);
        Assert.Equal("worked here, once", WalletChoice.HistoryLine(TheRealOne, Here, back));
        Assert.Contains("refused on B2", WalletChoice.HistoryLine(TheOtherSitesPass, Here, back), StringComparison.Ordinal);

        // A file from before this feature: no section, no memory, and no pretending otherwise.
        Vault older = VaultSerializer.Load(VaultSerializer.Save(new Vault()));
        Assert.Null(older.PapersShown);
    }

    // ── THE REGISTER ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The canon grep, walking this file's own catalog so a line added tomorrow is checked tomorrow.
    /// Nothing the wallet says explains anything, threatens anything, or quotes odds at the captain — it is a
    /// gumshoe reading his own pocket by lamplight.</summary>
    [Fact]
    public void NothingTheWalletSaysIsAnOracleOrAnExplanation()
    {
        string[] forbidden =
        [
            "old one", "reever", "kaamos", "minister", "ancient", "alien", "experiment",
            "chance", "odds", "%", "probability", "recommend", "guaranteed", "safe bet",
        ];

        int lines = 0;
        foreach (string line in WalletChoice.AllProse())
        {
            lines++;
            Assert.False(string.IsNullOrWhiteSpace(line), "an empty sentence in the catalog.");
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.True(lines >= 10, $"the canon grep only walked {lines} sentences.");
    }
}
