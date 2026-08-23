using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L1 · THE FILING LINE, AS LAW. <i>"On rebirth the new captain remembers the ledger only up to the
/// line."</i> The mechanic is one comparison, which is exactly the kind of rule that rots into a habit —
/// so the comparison, the two ends of it, the roll that reads a grey page and the single detail a wrong
/// recollection moves are all held here rather than in the client that calls them.
///
/// <para>Every test in this file is written to go RED if the law is reverted rather than merely to pass on
/// the shipped code: an uninsured wake with anything remembered fails, a Premium wake with a page from
/// before the premium greyed fails, a second roll at the same page in the same life fails, a wrong page
/// that moves two details or forgets what it really said fails, and a reload that re-greys a recovered
/// page fails.</para>
/// </summary>
public sealed class ThePagesYouDontRememberWritingTests
{
    private const double Day = 86400.0;

    /// <summary>A small ledger with a page on each side of any line we care about, and enough shape that a
    /// wrong recollection has something to move: numbers in the lines, and the ledger's own
    /// "&lt;who&gt; · &lt;where&gt; · day N" provenance idiom on two of them.</summary>
    private static LedgerPage[] ABook() =>
    [
        new("plunder:1", 2 * Day, "🏴 Plunder", ["18 units of ore off the CRANE-7, worth 4,200 cr"],
            "the Fixer · Ringside Exchange · day 2"),
        new("overheard:2", 5 * Day, "👂 Gilt-Eye", ["She runs the Selene leg with 3 in the hold and no manifest."],
            "Gilt-Eye · The Red Eye · day 5"),
        new("autopilot:3", 9 * Day, "🛰 Autopilot", ["Stand-down at 240 km — the burn was cut."],
            "logged 2h ago"),
    ];

    private static PirateInsurance PremiumPaidThrough(double simTime) =>
        new(InsuranceTier.Premium, simTime);

    // ── §1 · THE LINE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DIE UNINSURED AND THE WHOLE BOOK IS SOMEBODY ELSE'S (owner ruling 2026-08-23 §2 — "uninsured wake
    /// with a blank ledger, true to the poster"). Nobody filed anything, so there is no date to remember up
    /// to: every row is grey, including the one written in the first hour of the run.
    /// </summary>
    [Fact]
    public void AnUninsuredRebirthLeavesNotOnePageTheCaptainRemembersWriting()
    {
        double line = FilingLine.At(PirateInsurance.Uninsured);

        Assert.True(double.IsNegativeInfinity(line));

        IReadOnlyList<FilingLine.Page> book = FilingLine.MarkTheBook([], ABook(), line);

        Assert.Equal(3, book.Count);
        Assert.All(book, p => Assert.Equal(FilingLine.PageState.Unremembered, p.State));
        Assert.All(book, p => Assert.True(p.IsGrey));
    }

    /// <summary>
    /// A PREMIUM PAID THROUGH DAY SEVEN REMEMBERS EVERYTHING UP TO DAY SEVEN — and the two pages before it
    /// are not merely "mostly" remembered, they are untouched. This is the half that would still pass if the
    /// comparison were inverted or the line were read off the wrong field, so it asserts both sides.
    /// </summary>
    [Fact]
    public void APremiumMarksOnlyThePagesWrittenAfterThePremiumRan()
    {
        // A captain who bought a policy and let it run out on day seven, and died on day ten. This is the
        // case the whole lane exists for and the one the wake card's insured sentence describes — "the pages
        // come back to where the premium was paid; after that line, the book is grey" — and it is reachable
        // only because the line asks WAS ANYTHING FILED rather than DOES THIS POLICY PAY (see FilingLine's
        // header). Under the other reading this test is the impossible world: a page dated after a death.
        PirateInsurance policy = PremiumPaidThrough(7 * Day);
        double line = FilingLine.At(policy);

        Assert.False(policy.IsActiveAt(10 * Day)); // it does not PAY — the clinic bill is the full one
        Assert.Equal(7 * Day, line);               // …and it still FILED you, up to the day the money ran

        IReadOnlyList<FilingLine.Page> book = FilingLine.MarkTheBook([], ABook(), line);

        Assert.Equal(FilingLine.PageState.Remembered, book[0].State);   // day 2 — filed
        Assert.Equal(FilingLine.PageState.Remembered, book[1].State);   // day 5 — filed
        Assert.Equal(FilingLine.PageState.Unremembered, book[2].State); // day 9 — after the money stopped
        Assert.False(book[0].IsGrey);
        Assert.False(book[1].IsGrey);
        Assert.True(book[2].IsGrey);
    }

    /// <summary>A page written EXACTLY on the line was filed. The boundary is stated rather than left to
    /// whichever way a strict comparison happened to be typed.</summary>
    [Fact]
    public void APageWrittenOnTheLineItselfComesBack()
    {
        Assert.False(FilingLine.IsAfterTheLine(7 * Day, 7 * Day));
        Assert.True(FilingLine.IsAfterTheLine(7 * Day, 7 * Day + 1));
    }

    /// <summary>
    /// PAID UP WHEN YOU DIED AND ALMOST NOTHING IS LOST. The money reached past the day, so there is no page
    /// dated after the line to lose — which is the Premium tier's whole promise and the reason the rep can
    /// say the thing he says about mothers' faces.
    /// </summary>
    [Fact]
    public void APolicyStillPaidUpAtTheDeathBringsTheWholeBookBack()
    {
        PirateInsurance covered = PremiumPaidThrough(30 * Day);

        Assert.True(covered.IsActiveAt(10 * Day));

        IReadOnlyList<FilingLine.Page> book = FilingLine.MarkTheBook([], ABook(), FilingLine.At(covered));

        Assert.All(book, p => Assert.False(p.IsGrey));
    }

    /// <summary>
    /// NEVER HELD A POLICY AND NOTHING WAS FILED, whatever number happens to be sitting in the field. A
    /// hand-built <c>new(None, day 500)</c> must not quietly file a captain's whole life for free — the tier
    /// is the question, and this is the guard that says so.
    /// </summary>
    [Fact]
    public void NoTierFilesNothingWhateverTheStampSays()
    {
        Assert.True(double.IsNegativeInfinity(FilingLine.At(PirateInsurance.Uninsured)));
        Assert.True(double.IsNegativeInfinity(FilingLine.At(new PirateInsurance(InsuranceTier.None, 500 * Day))));
    }

    /// <summary>The wake card's one line is decided by whether anything was ever FILED and not by how many
    /// rows happened to grey — so the captain whose premium ran out on day four is told the true thing about
    /// where their book stops, and only the captain nobody ever filed for gets the other sentence.</summary>
    [Fact]
    public void TheWakeNoticeIsDecidedByTheFilingAndNotByTheRowCount()
    {
        Assert.Equal(FilingLine.UninsuredWake, FilingLine.WakeNotice(PirateInsurance.Uninsured));
        Assert.Equal(FilingLine.InsuredWake, FilingLine.WakeNotice(PremiumPaidThrough(4 * Day)));
        Assert.Equal(FilingLine.InsuredWake, FilingLine.WakeNotice(PremiumPaidThrough(30 * Day)));
        Assert.Equal(FilingLine.UninsuredWake, FilingLine.WakeNotice(double.NegativeInfinity));
        Assert.NotEqual(FilingLine.UninsuredWake, FilingLine.InsuredWake);
    }

    /// <summary>A rebirth marks the book the captain actually has: rows that have left the ledger between
    /// two deaths are dropped rather than accumulating forever.</summary>
    [Fact]
    public void TheBookOnlyEverHoldsPagesTheLedgerStillHas()
    {
        IReadOnlyList<FilingLine.Page> first = FilingLine.MarkTheBook([], ABook(), double.NegativeInfinity);
        IReadOnlyList<FilingLine.Page> second =
            FilingLine.MarkTheBook(first, ABook().Take(1).ToArray(), double.NegativeInfinity);

        Assert.Single(second);
        Assert.Equal("plunder:1", second[0].EntryId);
    }

    // ── §2 · THE ROLL ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SAME PAGE, THE SAME LIFE, THE SAME ANSWER — forever. Determinism is law in Core and here it is
    /// also the anti-cheat: a reload that re-rolled a refusal would turn every grey page into a slot machine
    /// you play by pressing F5.
    /// </summary>
    [Fact]
    public void TheRollIsTheSameEveryTimeItIsAskedForTheSameLife()
    {
        for (int i = 0; i < 20; i++)
        {
            string id = $"plunder:{i}";
            DicePool a = Flashback.Roll("thread-7", id, life: 2, InsuranceTier.Basic);
            DicePool b = Flashback.Roll("thread-7", id, life: 2, InsuranceTier.Basic);

            Assert.Equal(a.Seed, b.Seed);
            Assert.Equal(a.FaceTotal, b.FaceTotal);
            Assert.Equal(Flashback.Read(a), Flashback.Read(b));
        }
    }

    /// <summary>…and a DIFFERENT life is a different question. The next captain reaching for the same page
    /// must not be handed the last one's answer, or a re-greyed book would be a book of foregone
    /// conclusions.</summary>
    [Fact]
    public void ADifferentLifeAsksTheSamePageADifferentQuestion()
    {
        var lives = new HashSet<ulong>();
        for (int life = 1; life <= 6; life++)
        {
            lives.Add(Flashback.SeedFor("thread-7", "plunder:1", life));
        }

        Assert.Equal(6, lives.Count);

        // …and so is a different universe, and a different page.
        Assert.NotEqual(Flashback.SeedFor("thread-7", "plunder:1", 1), Flashback.SeedFor("thread-8", "plunder:1", 1));
        Assert.NotEqual(Flashback.SeedFor("thread-7", "plunder:1", 1), Flashback.SeedFor("thread-7", "plunder:2", 1));
    }

    /// <summary>
    /// ONCE PER PAGE PER LIFE. The latch is a state and not a hope: a page that answered "nothing" is
    /// <see cref="FilingLine.PageState.Refused"/> and refuses to be read again — and a rebirth, which
    /// re-greys the book, lifts it, because that is a new captain asking.
    /// </summary>
    [Fact]
    public void APageThatGaveNothingIsNotAskedAgainUntilTheNextRebirth()
    {
        IReadOnlyList<FilingLine.Page> book = FilingLine.MarkTheBook([], ABook(), double.NegativeInfinity);
        Assert.True(FilingLine.Standing(book, "plunder:1").MayBeRead);

        book = FilingLine.Put(book, FilingLine.Standing(book, "plunder:1") with
        {
            State = FilingLine.PageState.Refused,
        });
        Assert.False(FilingLine.Standing(book, "plunder:1").MayBeRead);
        Assert.True(FilingLine.Standing(book, "plunder:1").IsGrey);

        // …and a recovered page is not re-readable either: there is nothing left to ask it.
        book = FilingLine.Put(book, FilingLine.Standing(book, "overheard:2") with
        {
            State = FilingLine.PageState.CameBack,
        });
        Assert.False(FilingLine.Standing(book, "overheard:2").MayBeRead);
        Assert.False(FilingLine.Standing(book, "overheard:2").IsGrey);

        // The rebirth re-greys the book, and the refusal is lifted with it.
        book = FilingLine.MarkTheBook(book, ABook(), double.NegativeInfinity);
        Assert.True(FilingLine.Standing(book, "plunder:1").MayBeRead);
        Assert.True(FilingLine.Standing(book, "overheard:2").MayBeRead);
    }

    /// <summary>
    /// THE TIER IS WORTH SOMETHING, and it is worth it in the direction the fiction claims. A Premium
    /// captain — the one who paid for the pattern to be kept — gets more of their life back than an
    /// uninsured one, across the same pages. A modifier that summed to nothing would pass every other test
    /// in this file.
    /// </summary>
    [Fact]
    public void ThePremiumPeopleGetMoreOfTheirLivesBack()
    {
        int premium = 0;
        int uninsured = 0;
        for (int i = 0; i < 400; i++)
        {
            string id = $"page:{i}";
            if (Flashback.Read(Flashback.Roll("t", id, 1, InsuranceTier.Premium)) == Flashback.Outcome.CameBack)
            {
                premium++;
            }

            if (Flashback.Read(Flashback.Roll("t", id, 1, InsuranceTier.None)) == Flashback.Outcome.CameBack)
            {
                uninsured++;
            }
        }

        Assert.True(premium > uninsured,
                    $"the Premium tier recovered {premium} pages of 400 and the uninsured {uninsured} — the "
                    + "tier modifier is not reaching the roll");

        Assert.Equal(+1, Flashback.TierModifier(InsuranceTier.Premium).Value);
        Assert.Equal(0, Flashback.TierModifier(InsuranceTier.Basic).Value);
        Assert.Equal(-1, Flashback.TierModifier(InsuranceTier.None).Value);
    }

    /// <summary>All three outcomes are reachable at every tier — a ladder that never lands on its middle
    /// rung would make "it comes back wrong" a rule nobody ever meets.</summary>
    [Theory]
    [InlineData(InsuranceTier.None)]
    [InlineData(InsuranceTier.Basic)]
    [InlineData(InsuranceTier.Premium)]
    public void AllThreeOutcomesHappen(InsuranceTier tier)
    {
        var seen = new HashSet<Flashback.Outcome>();
        for (int i = 0; i < 200; i++)
        {
            seen.Add(Flashback.Read(Flashback.Roll("t", $"page:{i}", 1, tier)));
        }

        Assert.Equal(3, seen.Count);
    }

    // ── §3 · THE LIE ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A WRONG PAGE MOVES EXACTLY ONE DETAIL, AND KEEPS WHAT IT REALLY SAID. Both halves matter: one, because
    /// a memory that came back with three things changed is not a misremembering, it is a different page;
    /// and the other, because the original is the only thing a later reconcile can disagree WITH — lose it
    /// and the lie becomes the truth.
    /// </summary>
    [Fact]
    public void AWrongPageMovesExactlyOneDetailAndTheOriginalIsKept()
    {
        LedgerPage[] book = ABook();

        for (int i = 0; i < book.Length; i++)
        {
            LedgerPage page = book[i];
            Flashback.Alteration lie = Flashback.Alter(page, book, Flashback.SeedFor("t", page.Id, 1));

            Assert.NotEqual(FilingLine.Detail.None, lie.Which);
            Assert.NotEqual("", lie.Original);
            Assert.NotEqual(lie.Original, lie.Substitute);

            LedgerPage moved = Flashback.Apply(page, lie);

            int fieldsChanged =
                (moved.Title != page.Title ? 1 : 0)
                + (moved.Provenance != page.Provenance ? 1 : 0)
                + page.Lines.Where((l, n) => moved.Lines[n] != l).Count();

            Assert.Equal(1, fieldsChanged);

            // …and the change inside that one field is the one substitution and nothing else: putting the
            // original back where the substitute landed restores the page byte for byte.
            Assert.Equal(page.Title, Undo(moved.Title, lie));
            Assert.Equal(page.Provenance, Undo(moved.Provenance, lie));
            Assert.Equal(page.Lines, moved.Lines.Select(l => Undo(l, lie)).ToArray());
        }
    }

    /// <summary>The alteration is deterministic — the same page in the same book on the same seed always
    /// misremembers the same thing, so a reload cannot shuffle the lie.</summary>
    [Fact]
    public void TheLieIsAlwaysTheSameLie()
    {
        LedgerPage[] book = ABook();
        for (int i = 0; i < book.Length; i++)
        {
            ulong seed = Flashback.SeedFor("t", book[i].Id, 3);
            Assert.Equal(Flashback.Alter(book[i], book, seed), Flashback.Alter(book[i], book, seed));
        }
    }

    /// <summary>
    /// …AND THE LIE IS ORDER-BLIND. The ledger is assembled fresh every render and its rows arrive in
    /// whatever order the six books happened to be walked in; a lie that depended on that order would change
    /// as the game ran. The donors are sorted before one is picked, and this is the guard on it.
    /// </summary>
    [Fact]
    public void TheLieDoesNotDependOnTheOrderTheLedgerWasAssembledIn()
    {
        LedgerPage[] book = ABook();
        LedgerPage[] shuffled = [book[2], book[0], book[1]];

        foreach (LedgerPage page in book)
        {
            ulong seed = Flashback.SeedFor("t", page.Id, 1);
            Assert.Equal(Flashback.Alter(page, book, seed), Flashback.Alter(page, shuffled, seed));
        }
    }

    /// <summary>
    /// THE ALTERATION IS MADE OF THE CAPTAIN'S OWN BOOK. A number they really wrote down, a name who really
    /// told them something, a place they really were — nothing invented. It is what makes a wrong page hard
    /// to catch, and it is also why the SPREAD will be able to catch it: every part is checkable.
    /// </summary>
    [Fact]
    public void WhatTheDetailMovesToIsSomethingElseOnTheSameCaptainsLedger()
    {
        LedgerPage[] book = ABook();
        LedgerPage page = book[0];
        Flashback.Alteration lie = Flashback.Alter(page, book, Flashback.SeedFor("t", page.Id, 1));

        string everythingElse = string.Concat(book.Skip(1).Select(
            p => p.Title + string.Concat(p.Lines) + p.Provenance));

        Assert.Contains(lie.Substitute, everythingElse, StringComparison.Ordinal);
    }

    /// <summary>A row with nothing on it a person could misremember one of — no number in its own words and
    /// no attribution — comes back WHOLE rather than being handed an invented detail. The honest degrade,
    /// stated rather than faked.</summary>
    [Fact]
    public void APageWithNothingToLoseComesBackWhole()
    {
        var thin = new LedgerPage("standing:1", Day, "⚓ Ports come in twos", ["The haven has the berth."],
                                  "standing note");

        Flashback.Alteration lie = Flashback.Alter(thin, [thin], Flashback.SeedFor("t", thin.Id, 1));

        Assert.Equal(FilingLine.Detail.None, lie.Which);
        Assert.Equal(thin, Flashback.Apply(thin, lie));
    }

    /// <summary>A row whose provenance is an AGE ("logged 2h ago") can only lose a number out of its own
    /// words — never a name or a place it never named. A lie written against a clock would stop matching
    /// within the hour and quietly become a no-op.</summary>
    [Fact]
    public void ARowWithNoAttributionNeverMisremembersWhoOrWhere()
    {
        LedgerPage[] book = ABook();
        LedgerPage dated = book[2]; // "logged 2h ago"

        Flashback.Alteration lie = Flashback.Alter(dated, book, Flashback.SeedFor("t", dated.Id, 1));

        Assert.Equal(FilingLine.Detail.Number, lie.Which);
        Assert.Equal(dated.Provenance, Flashback.Apply(dated, lie).Provenance);
    }

    // ── §4 · THE KEEPING ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A RELOAD NEVER RE-GREYS A PAGE THE CAPTAIN WON BACK AND NEVER RE-ROLLS ONE THEY LOST. Every field of
    /// every state rides the vault as an opaque row, including the hidden original — which is the one thing
    /// in this lane that exists only in the file and nowhere on any surface.
    /// </summary>
    [Fact]
    public void TheMarksTheLatchAndTheHiddenOriginalsAllSurviveAReload()
    {
        FilingLine.Page[] book =
        [
            new("plunder:1", FilingLine.PageState.CameBack, FilingLine.Detail.None, "", ""),
            new("overheard:2", FilingLine.PageState.Refused, FilingLine.Detail.None, "", ""),
            new("autopilot:3", FilingLine.PageState.CameBackWrong, FilingLine.Detail.Place,
                "Ringside Exchange", "The Red Eye"),
            new("field:4", FilingLine.PageState.Unremembered, FilingLine.Detail.None, "", ""),
        ];

        var vault = new Vault { Filing = new FilingSection { Pages = [.. book.Select(p => p.Stored)] } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);
        Assert.NotNull(loaded.Filing);

        var back = new List<FilingLine.Page>();
        foreach (string stored in loaded.Filing!.Pages)
        {
            Assert.True(FilingLine.Page.TryParse(stored, out FilingLine.Page page));
            back.Add(page);
        }

        Assert.Equal(book, back);
        Assert.Equal("Ringside Exchange", FilingLine.Standing(back, "autopilot:3").Original);
        Assert.False(FilingLine.Standing(back, "plunder:1").IsGrey);
        Assert.False(FilingLine.Standing(back, "overheard:2").MayBeRead);
        Assert.True(FilingLine.Standing(back, "field:4").MayBeRead);
    }

    /// <summary>A save written before this lane existed loads with nothing marked, which is the truth about
    /// a captain nobody ever filed a claim for — and an unreadable row is dropped rather than thrown over,
    /// the same tolerance every other opaque-row section gets.</summary>
    [Fact]
    public void AnOlderSaveLoadsWithNothingMarkedAndAJunkRowIsDropped()
    {
        Vault none = VaultSerializer.Load(VaultSerializer.Save(new Vault()));
        Assert.Null(none.Filing);
        Assert.Equal(FilingLine.PageState.Remembered, FilingLine.Standing([], "plunder:1").State);

        Assert.False(FilingLine.Page.TryParse(null, out _));
        Assert.False(FilingLine.Page.TryParse("", out _));
        Assert.False(FilingLine.Page.TryParse("nonsense", out _));
        Assert.False(FilingLine.Page.TryParse("99|1|id|a|b", out _));  // no such state
        Assert.False(FilingLine.Page.TryParse("1|9|id|a|b", out _));   // no such detail
        Assert.False(FilingLine.Page.TryParse("1|0||a|b", out _));     // no entry
    }

    /// <summary>A pipe in an entry's own words does not corrupt the row it is stored in — the one character
    /// the format spends is escaped in every field but the last.</summary>
    [Fact]
    public void APipeInTheOriginalStillRoundTrips()
    {
        var page = new FilingLine.Page("odd|id", FilingLine.PageState.CameBackWrong, FilingLine.Detail.Name,
                                       "a|b", "c|d");

        Assert.True(FilingLine.Page.TryParse(page.Stored, out FilingLine.Page back));
        Assert.Equal(page, back);
    }

    /// <summary>An entry id is stable across a re-render (it is built from the stamp and the row's own key,
    /// never from its rendered words) and distinct between rows.</summary>
    [Fact]
    public void AnEntryIdNamesOneRowAndKeepsNamingIt()
    {
        Assert.Equal(FilingLine.EntryId("plunder", 2 * Day, "18 units of ore"),
                     FilingLine.EntryId("plunder", 2 * Day, "18 units of ore"));

        Assert.NotEqual(FilingLine.EntryId("plunder", 2 * Day, "18 units of ore"),
                        FilingLine.EntryId("plunder", 2 * Day, "19 units of ore"));
        Assert.NotEqual(FilingLine.EntryId("plunder", 2 * Day, "ore"),
                        FilingLine.EntryId("plunder", 3 * Day, "ore"));
        Assert.NotEqual(FilingLine.EntryId("plunder", 2 * Day, "ore"),
                        FilingLine.EntryId("overheard", 2 * Day, "ore"));

        // …and it never carries the one character the stored row format spends.
        Assert.DoesNotContain('|', FilingLine.EntryId("plunder", 2 * Day, "a|b"));
    }

    // ── §5 · WHAT IS NEVER SAID ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO SURFACE IN THIS LANE NAMES THE THING. The same law the Reever origin keeps and the same law
    /// <c>NebulaArc.md</c> §2 opens with: the arc's truth is the writers' bible, and the game says
    /// <i>continuity</i>, <i>pages you don't remember</i>, <i>the pattern was kept</i> — never the plain word
    /// for what the clinic actually does. Every string this lane can put in front of a player is swept,
    /// because the one that leaks it will be the one somebody adds in a hurry.
    /// </summary>
    [Fact]
    public void NoGameTextInThisLaneNamesTheThing()
    {
        List<string> everySurface =
        [
            FilingLine.Mark,
            FilingLine.Label,
            FilingLine.UninsuredWake,
            FilingLine.InsuredWake,
            FilingLine.WakeNotice(PirateInsurance.Uninsured),
            FilingLine.WakeNotice(PremiumPaidThrough(2 * Day)),
            Flashback.WrongPageNerveLabel,
            StoryBeats.Title(StoryBeats.Beat.Flashback),
            StoryBeats.Caption(StoryBeats.Beat.Flashback),
            .. Enum.GetValues<Flashback.Outcome>().Select(Flashback.Toast),
            .. Enum.GetValues<InsuranceTier>().Select(t => Flashback.TierModifier(t).Label),
        ];

        Assert.All(everySurface, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.All(everySurface, line =>
            Assert.DoesNotContain("copy", line, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The three sentences are three DIFFERENT sentences — a toast switch that fell through to one
    /// answer would make the whole ladder invisible to the player it is played for.</summary>
    [Fact]
    public void EachOutcomeSaysItsOwnThing()
    {
        string[] said = [.. Enum.GetValues<Flashback.Outcome>().Select(Flashback.Toast)];

        Assert.Equal(3, said.Distinct(StringComparer.Ordinal).Count());
    }

    // Put the original back where the substitute landed. Used to prove that ONE substitution, and only one,
    // is the whole of the difference between the true page and the remembered one.
    private static string Undo(string text, Flashback.Alteration lie)
    {
        int at = text.IndexOf(lie.Substitute, StringComparison.Ordinal);
        return at < 0
            ? text
            : string.Concat(text.AsSpan(0, at), lie.Original, text.AsSpan(at + lie.Substitute.Length));
    }
}
