using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L3 · THE BOOK, AS LAWS. Six things in this lane are rules rather than habits, and every one of them
/// is the sort that rots quietly:
///
/// <list type="bullet">
///   <item>the stray memory's three cadences — never the first grey page of a life, at most one nothing in
///   four, and never the same page twice in one universe;</item>
///   <item>the reconcile's three verdicts, and the fact that a DISAGREEMENT is the only one with physical
///   evidence behind it (#974's hidden original, kept for exactly this table);</item>
///   <item>three sheets that are nobody's assembling <c>the-bleed</c>, and two never doing it;</item>
///   <item>the sheet round-tripping the three fields this lane appended, and an L5a row loading clean;</item>
///   <item>the THREADS stacking and the money/love filter;</item>
///   <item>and the arc's own law, swept over every new surface: the word for what the clinic does is never
///   printed.</item>
/// </list>
/// </summary>
public sealed class TheBookHoldsWhatSomebodyRemembersTests
{
    private const double Day = 86400.0;

    private static HeldMemory.Sheet Stray(int index, double at = 3 * Day) => new(
        Flashback.StrayId(index), HeldMemory.Mark.NotAnyones, HeldMemory.Theory.Love,
        Flashback.Strays[index], [], at);

    // ── THE STRAYS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NEVER THE FIRST GREY PAGE OF A LIFE. The first read has to teach the ordinary three outcomes, and a
    /// rarity that could land on the tutorial is a rarity the player reads as the rule. Swept over a long run
    /// of pages, threads and lives rather than sampled once, because "never" is the claim.
    /// </summary>
    [Fact]
    public void NoStrayEverComesOnTheFirstGreyPageARebornCaptainReads()
    {
        for (int thread = 0; thread < 40; thread++)
        {
            for (int entry = 0; entry < 20; entry++)
            {
                for (int life = 1; life <= 4; life++)
                {
                    Assert.False(
                        Flashback.AStrayComesInstead($"thread-{thread}", $"entry:{entry}", life, 0),
                        $"a stray landed on the first read of life {life}");
                }
            }
        }
    }

    /// <summary>
    /// AT MOST ONE NOTHING IN FOUR. The rarity is arithmetic on the shared dice — a d4 coming up one — rather
    /// than a probability nobody can test, and this holds the RATE over a wide sweep rather than trusting the
    /// constant. It has teeth in both directions: a stray that never came would leave six authored pages
    /// unreachable, and one that came half the time would stop being a rarity.
    /// </summary>
    [Fact]
    public void RoughlyOneNothingInFourCarriesAStrayInstead()
    {
        int nothings = 0;
        int strays = 0;
        for (int thread = 0; thread < 60; thread++)
        {
            for (int entry = 0; entry < 60; entry++)
            {
                nothings++;
                if (Flashback.AStrayComesInstead($"thread-{thread}", $"entry:{entry}", 2, 1))
                {
                    strays++;
                }
            }
        }

        double rate = (double)strays / nothings;
        Assert.Equal(4, Flashback.StrayInNothings);
        Assert.InRange(rate, 0.20, 0.30);
    }

    /// <summary>
    /// WITHOUT REPLACEMENT, PER THREAD. A stray that came twice would stop being a memory and start being a
    /// message. The drawn set is the stray sheets already in the book — no second store — so this walks the
    /// draw the way the client does and proves the set completes and then stops.
    /// </summary>
    [Fact]
    public void AThreadNeverDrawsTheSameStrayTwiceAndStopsAtSix()
    {
        var held = new List<string>();
        for (int draw = 0; draw < Flashback.Strays.Count; draw++)
        {
            int? index = Flashback.DrawStray(held, Flashback.SeedFor("thread-a", $"entry:{draw}", 2));
            Assert.NotNull(index);
            Assert.DoesNotContain(Flashback.StrayId(index!.Value), held);
            held.Add(Flashback.StrayId(index.Value));
        }

        Assert.Equal(Flashback.Strays.Count, held.Distinct(StringComparer.Ordinal).Count());

        // …and a seventh is not a thing that exists. The roll falls back to the ordinary nothing.
        Assert.Null(Flashback.DrawStray(held, Flashback.SeedFor("thread-a", "entry:99", 2)));
    }

    /// <summary>The same page in the same life always draws the same stray — a reload can never re-roll a
    /// memory into a different one, which is the determinism law every other roll in this house keeps.</summary>
    [Fact]
    public void TheDrawIsDeterministic()
    {
        ulong seed = Flashback.SeedFor("thread-b", "entry:3", 1);
        Assert.Equal(Flashback.DrawStray([], seed), Flashback.DrawStray([], seed));
    }

    /// <summary>The toast is the sheet's FIRST SENTENCE and nothing else — the rest of it is on the sheet, in
    /// the book, where a memory belongs. A toast that read out the whole page would be the pulse doing the
    /// black book's job for it.</summary>
    [Fact]
    public void TheStrayToastIsTheFirstSentenceOnly()
    {
        Assert.Equal(
            "A corridor with the lights on the floor instead of the ceiling.",
            Flashback.StrayToast(Flashback.Strays[0]));

        // A one-sentence stray is its own toast rather than an empty one.
        Assert.Equal(Flashback.Strays[2], Flashback.StrayToast(Flashback.Strays[2]));

        foreach (string text in Flashback.Strays)
        {
            Assert.False(string.IsNullOrWhiteSpace(Flashback.StrayToast(text)));
            Assert.True(Flashback.StrayToast(text).Length <= text.Length);
        }
    }

    // ── THE RECONCILE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>They name the same person, so they agree — and both MEMORIES in the pair gain a point of
    /// confidence, which is the goodwill the owner's ruling asked for said as a number.</summary>
    [Fact]
    public void TwoPapersThatNameTheSamePersonAgreeAndWarmTheMemory()
    {
        var photograph = new HeldMemory.Sheet(
            HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love,
            OldCrewScene.Photograph, ["Corwin Sallis", "Maren Okafor"], 2 * Day);
        var slip = new HeldMemory.Sheet(
            HeldMemory.SlipId("maren"), HeldMemory.Mark.Hers, HeldMemory.Theory.Money,
            OldCrewScene.Slip("maren"), ["Maren Okafor"], 3 * Day);

        SpreadReconcile.Result result = SpreadReconcile.Lay(
            SpreadReconcile.Paper.Of(photograph, "a photograph"),
            SpreadReconcile.Paper.Of(slip, "a slip"));

        Assert.Equal(SpreadReconcile.Verdict.Agree, result.Verdict);
        Assert.Equal(SpreadReconcile.AgreeLine, result.Line);
        Assert.Equal([HeldMemory.PhotographId, HeldMemory.SlipId("maren")], result.Corroborated);

        // …and the second question is a COUNT and never a conclusion.
        Assert.Equal(1, result.Money);
        Assert.Equal(1, result.Love);
        Assert.Contains(SpreadReconcile.TheSecondQuestion, result.SecondQuestion, StringComparison.Ordinal);

        Assert.Equal(1, photograph.Warmer(SpreadReconcile.MostConfidence).Confidence);
        Assert.Equal(
            SpreadReconcile.MostConfidence,
            (photograph with { Confidence = SpreadReconcile.MostConfidence })
                .Warmer(SpreadReconcile.MostConfidence).Confidence);
    }

    /// <summary>
    /// THE HIDDEN ORIGINAL COMES BACK. A page that came back wrong (#974 moved exactly one detail and kept
    /// what it really said, unread by any surface) laid beside the document that carries the true detail is
    /// the one verdict with physical evidence behind it — and the SPREAD names the page to restore and what
    /// it really said, so the caller does not have to reach back into the paper for either.
    /// </summary>
    [Fact]
    public void AWrongPageLaidBesideWhatItContradictsGivesUpTheOriginal()
    {
        var page = new LedgerPage("tip:41:beef", 4 * Day, "A route tip", ["764 cr for the run"], "Pell · red-eye · day 4");
        var wrong = new FilingLine.Page(
            page.Id, FilingLine.PageState.CameBackWrong, FilingLine.Detail.Number, "764", "312");
        var receipt = new LedgerPage("tip:42:cafe", 5 * Day, "A receipt", ["764 cr, paid"], "standing note");

        SpreadReconcile.Result result = SpreadReconcile.Lay(
            SpreadReconcile.Paper.Of(page, wrong, []),
            SpreadReconcile.Paper.Of(receipt, FilingLine.Page.Remembered(receipt.Id), []));

        Assert.Equal(SpreadReconcile.Verdict.Disagree, result.Verdict);
        Assert.Equal(SpreadReconcile.DisagreeLine, result.Line);
        Assert.Equal(page.Id, result.CorrectedId);
        Assert.Equal("764", result.Restored);
        Assert.Empty(result.Corroborated);
    }

    /// <summary>…and the disagreement is asked FIRST. A pair that both names a person and catches a lie about
    /// them is a pair whose lie is the interesting half, and a rule that checked agreement first would have
    /// quietly buried every correction the table can make.</summary>
    [Fact]
    public void ALieIsFoundEvenWhenTheTwoPapersAlsoAgree()
    {
        var page = new LedgerPage("tip:7:aa", 4 * Day, "A tip", ["Corwin Sallis said so"], "Pell · red-eye · day 4");
        var wrong = new FilingLine.Page(
            page.Id, FilingLine.PageState.CameBackWrong, FilingLine.Detail.Name, "Maren Okafor", "Corwin Sallis");
        var sheet = new HeldMemory.Sheet(
            "photograph", HeldMemory.Mark.His, HeldMemory.Theory.Love,
            "Four of you.", ["Maren Okafor", "Corwin Sallis"], 2 * Day);

        SpreadReconcile.Result result = SpreadReconcile.Lay(
            SpreadReconcile.Paper.Of(page, wrong, []),
            SpreadReconcile.Paper.Of(sheet, "a photograph"));

        Assert.Equal(SpreadReconcile.Verdict.Disagree, result.Verdict);
        Assert.Equal("Maren Okafor", result.Restored);
    }

    /// <summary>A memory naming nothing the book has is not a failure — it is a LEAD. It goes onto the THREADS
    /// page under its own heading and waits for the paper that answers it.</summary>
    [Fact]
    public void AMemoryTheBookCannotAnswerBecomesALead()
    {
        var sheet = new HeldMemory.Sheet(
            "slip:dagny", HeldMemory.Mark.Hers, HeldMemory.Theory.Money,
            OldCrewScene.Slip("dagny"), ["Dagny Voss"], 6 * Day);
        var elsewhere = new LedgerPage("tip:9:bb", 7 * Day, "A cargo note", ["2 t of ice"], "standing note");

        SpreadReconcile.Result result = SpreadReconcile.Lay(
            SpreadReconcile.Paper.Of(sheet, "a slip"),
            SpreadReconcile.Paper.Of(elsewhere, FilingLine.Page.Remembered(elsewhere.Id), []));

        Assert.Equal(SpreadReconcile.Verdict.NamesNoDocument, result.Verdict);
        Assert.Equal(SpreadReconcile.NamesNoDocumentLine, result.Line);
        Assert.Equal(sheet.Id, result.LeadId);

        // …and the waiting is a THREAD, not a special box: the sheet takes the heading as a name it writes
        // down, so it stacks with everything else on the page it is waiting on.
        HeldMemory.Sheet waiting = sheet.Naming(SpreadReconcile.NotAnyonesYet);
        Assert.Contains(SpreadReconcile.NotAnyonesYet, waiting.Threads);
        Assert.Equal(waiting.Threads.Count, waiting.Naming(SpreadReconcile.NotAnyonesYet).Threads.Count);
    }

    /// <summary>
    /// #973 · <b>THE AUTHORED-REVEAL HOOK.</b> A lane that knows something about two SPECIFIC papers gets to
    /// say so before any general rule calls the pair an ordinary agreement — and a hook that returns null
    /// leaves the table behaving exactly as it does without one, which is what makes it safe to ship ahead
    /// of the lane that fills it (#973 L5b's walk-in is the first caller).
    /// </summary>
    [Fact]
    public void AnAuthoredRevealIsAskedFirstAndAnEmptyHookChangesNothing()
    {
        var hers = new HeldMemory.Sheet(
            "her-note", HeldMemory.Mark.Hers, HeldMemory.Theory.Love, "…", ["Ilse Marrow"], 4 * Day);
        var slip = new HeldMemory.Sheet(
            HeldMemory.SlipId("corwin"), HeldMemory.Mark.His, HeldMemory.Theory.Money,
            OldCrewScene.Slip(OldCrew.SignerId), ["Ilse Marrow"], 5 * Day);

        SpreadReconcile.Paper a = SpreadReconcile.Paper.Of(hers, "her note");
        SpreadReconcile.Paper b = SpreadReconcile.Paper.Of(slip, "a slip");

        // No hook, and a hook with nothing to say about this pair, are the same table.
        SpreadReconcile.Result plain = SpreadReconcile.Lay(a, b);
        SpreadReconcile.Result quiet = SpreadReconcile.Lay(a, b, (_, _) => null);
        Assert.Equal(SpreadReconcile.Verdict.Agree, plain.Verdict);
        Assert.Equal(plain.Verdict, quiet.Verdict);
        Assert.Equal(plain.Line, quiet.Line);

        // …and a reveal WINS over the general rules — including over the agreement this pair would
        // otherwise have been, which is the whole reason it is asked first.
        const string authored = "Her hand and the desk's hand are the same hand.";
        SpreadReconcile.Result revealed = SpreadReconcile.Lay(a, b, (x, y) =>
            SpreadReconcile.Reveals(SpreadReconcile.Verdict.Disagree, authored, x, y, correctedId: x.Id));

        Assert.Equal(SpreadReconcile.Verdict.Disagree, revealed.Verdict);
        Assert.Equal(authored, revealed.Line);
        Assert.Equal("her-note", revealed.CorrectedId);

        // The second question is the TABLE's and is counted for the reveal too, so an authored line never
        // has to carry the arithmetic.
        Assert.Equal(1, revealed.Money);
        Assert.Equal(1, revealed.Love);
        Assert.Contains(SpreadReconcile.TheSecondQuestion, revealed.SecondQuestion, StringComparison.Ordinal);
    }

    /// <summary>The three verdicts are three different sentences, and every one of them is Fable's.</summary>
    [Fact]
    public void TheThreeVerdictsAreThree()
    {
        SpreadReconcile.Verdict[] all = Enum.GetValues<SpreadReconcile.Verdict>();

        Assert.Equal(3, all.Length);
        Assert.Equal(3, all.Select(SpreadReconcile.Line).Distinct(StringComparer.Ordinal).Count());
        Assert.All(all, v => Assert.False(string.IsNullOrWhiteSpace(SpreadReconcile.Line(v))));
    }

    // ── THE LATTICE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THREE THAT ARE NOBODY'S. Two never do it, three do, and a table with one real memory mixed in never
    /// does — because the whole point of the shard is that nothing on the table belongs to anybody.
    /// </summary>
    [Fact]
    public void ThreeAgreeingStraysAssembleTheBleedAndTwoDoNot()
    {
        SpreadReconcile.Paper A(int i) => SpreadReconcile.Paper.Of(Stray(i), "not anyone's");

        Assert.False(SpreadReconcile.TheBleedAssembles([A(0), A(1)]));
        Assert.True(SpreadReconcile.TheBleedAssembles([A(0), A(1), A(2)]));

        // A real memory on the table, and the lattice does not close.
        var mine = new HeldMemory.Sheet(
            OldCrewScene.SummerPartyId, HeldMemory.Mark.Mine, HeldMemory.Theory.Love,
            OldCrewScene.SummerPartyPage, [], -30 * Day, Filed: true);
        Assert.False(SpreadReconcile.TheBleedAssembles(
            [A(0), A(1), SpreadReconcile.Paper.Of(mine, "fleet-day")]));

        // …and neither does a stray beside a document.
        var doc = new LedgerPage("tip:1:cc", Day, "A note", ["nothing"], "standing note");
        Assert.False(SpreadReconcile.TheBleedAssembles(
            [A(0), A(1), SpreadReconcile.Paper.Of(doc, FilingLine.Page.Remembered(doc.Id), [])]));

        // Two strays that disagree about the theory they serve do not agree at all — the only kind of
        // agreement a page naming nobody can have.
        SpreadReconcile.Paper odd = SpreadReconcile.Paper.Of(
            Stray(3) with { Tag = HeldMemory.Theory.Money }, "not anyone's");
        Assert.False(SpreadReconcile.TheyAgree(A(0), odd));
        Assert.True(SpreadReconcile.TheyAgree(A(0), A(1)));
        Assert.False(SpreadReconcile.TheBleedAssembles([A(0), A(1), odd]));
    }

    /// <summary>…and two strays laid together DO reconcile as an agreement, which is what makes the third one
    /// the moment rather than a fourth press.</summary>
    [Fact]
    public void TwoStraysLaidTogetherAgree()
    {
        SpreadReconcile.Result result = SpreadReconcile.Lay(
            SpreadReconcile.Paper.Of(Stray(0), "not anyone's"),
            SpreadReconcile.Paper.Of(Stray(1), "not anyone's"));

        Assert.Equal(SpreadReconcile.Verdict.Agree, result.Verdict);
        Assert.Equal(2, result.Love);
        Assert.Equal(0, result.Money);
    }

    /// <summary>The pool takes the id the table assembles. A shard the SPREAD can produce and the arc refuses
    /// would be the one bug that looks like nothing happening at all.</summary>
    [Fact]
    public void ThePoolAcceptsTheBleedAndHostsItOnTheSpread()
    {
        NebulaFragment? bleed = NebulaLore.ById(SpreadReconcile.TheBleedId);

        Assert.NotNull(bleed);
        Assert.Equal(NebulaSource.Lattice, bleed!.Source);
        Assert.False(bleed.IsKey);
        Assert.True(NebulaLore.PoolIsWellFormed);

        var progress = new NebulaProgress();
        Assert.True(progress.Assemble(SpreadReconcile.TheBleedId));
        Assert.False(progress.Assemble(SpreadReconcile.TheBleedId));   // idempotent
        Assert.True(progress.Has(SpreadReconcile.TheBleedId));

        // No plate of its own: the SPREAD is its host, and the four hosted shards keep the same rule.
        Assert.Null(NebulaLore.PlateFor(SpreadReconcile.TheBleedId));

        // The number of strays it takes is the shard's own prose — a corridor, a name, a glass.
        Assert.Equal(3, SpreadReconcile.StraysForTheBleed);
    }

    // ── THE SHEET ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The three fields this lane appended round-trip through the vault's row, and every one of the
    /// old ones still does — including a text with a pipe in it, which is what the escaping is for.</summary>
    [Fact]
    public void ASheetRoundTripsEverythingIncludingTheThreeNewFields()
    {
        var sheet = new HeldMemory.Sheet(
            "slip:teo", HeldMemory.Mark.His, HeldMemory.Theory.Love,
            "A berth listing | for the REACH.", ["Teodor \"Teo\" Brask", "Ilse Marrow"], 9 * Day,
            Filed: false, HandedBy: "Teodor \"Teo\" Brask", Confidence: 3, Corrected: true);

        Assert.True(HeldMemory.Sheet.TryParse(sheet.Stored, out HeldMemory.Sheet back));

        // Field by field, and the row itself — a record's own equality compares the thread LIST by
        // reference, so `Assert.Equal(sheet, back)` would be asserting that the parser handed back the same
        // object, which is not what a round trip is.
        Assert.Equal(sheet.Stored, back.Stored);
        Assert.Equal(sheet.Id, back.Id);
        Assert.Equal(sheet.Mark, back.Mark);
        Assert.Equal(sheet.Tag, back.Tag);
        Assert.Equal(sheet.Text, back.Text);              // the pipe survived the escaping
        Assert.Equal(sheet.Threads, back.Threads);
        Assert.Equal(sheet.SimTime, back.SimTime);
        Assert.Equal(sheet.Filed, back.Filed);
        Assert.Equal(sheet.HandedBy, back.HandedBy);
        Assert.Equal(sheet.Confidence, back.Confidence);
        Assert.Equal(sheet.Corrected, back.Corrected);
    }

    /// <summary>
    /// AN L5a SAVE LOADS CLEAN. The row this lane appended to was seven fields; a file written before it is
    /// exactly that, and it must come back as what it was — a sheet nobody handed over, never laid on the
    /// table, never corrected — rather than being dropped.
    /// </summary>
    [Fact]
    public void ARowFromBeforeThisLaneLoadsWithTheNewFieldsAtTheirDefaults()
    {
        // The exact shape L5a wrote: id | mark | tag | simtime | filed | threads | text.
        const string old = "photograph|1|1|172800|0|Corwin Sallis␟Maren Okafor|Four of you on the boat deck.";

        Assert.True(HeldMemory.Sheet.TryParse(old, out HeldMemory.Sheet sheet));
        Assert.Equal("photograph", sheet.Id);
        Assert.Equal(HeldMemory.Mark.His, sheet.Mark);
        Assert.Equal(HeldMemory.Theory.Love, sheet.Tag);
        Assert.Equal(2, sheet.Threads.Count);
        Assert.Equal("", sheet.HandedBy);
        Assert.Equal(0, sheet.Confidence);
        Assert.False(sheet.Corrected);
    }

    /// <summary>The book line says whose it is, which theory it serves, who put it in your hand and the day —
    /// and grows the two words the SPREAD can add. It never says what any of it MEANS.</summary>
    [Fact]
    public void TheBookLineSaysWhoseItIsAndNothingAboutWhatItMeans()
    {
        var sheet = new HeldMemory.Sheet(
            HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love,
            OldCrewScene.Photograph, ["Corwin Sallis"], 12 * Day, HandedBy: "Teodor \"Teo\" Brask");

        Assert.Contains("his", sheet.BookLine, StringComparison.Ordinal);
        Assert.Contains("love", sheet.BookLine, StringComparison.Ordinal);
        Assert.Contains("Teodor", sheet.BookLine, StringComparison.Ordinal);
        Assert.Contains("day 12", sheet.BookLine, StringComparison.Ordinal);
        Assert.DoesNotContain("corrected", sheet.BookLine, StringComparison.Ordinal);
        Assert.DoesNotContain("confidence", sheet.BookLine, StringComparison.Ordinal);

        HeldMemory.Sheet worked = (sheet with { Corrected = true }).Warmer(SpreadReconcile.MostConfidence);
        Assert.Contains("corrected", worked.BookLine, StringComparison.Ordinal);
        Assert.Contains("confidence 1", worked.BookLine, StringComparison.Ordinal);
    }

    // ── THREADS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The photograph's four faces are four threads — which is what L5a seeded them for — and a
    /// slip that names one of them lands under that name rather than starting a fifth.</summary>
    [Fact]
    public void FourFacesAreFourThreadsAndASlipJoinsTheOneItNames()
    {
        var photograph = new HeldMemory.Sheet(
            HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love,
            OldCrewScene.Photograph,
            ["Ilse Marrow", "Teodor \"Teo\" Brask", "Corwin Sallis", "Hollis Grey"], 2 * Day);
        var slip = new HeldMemory.Sheet(
            HeldMemory.SlipId("corwin"), HeldMemory.Mark.His, HeldMemory.Theory.Money,
            OldCrewScene.Slip(OldCrew.SignerId), ["Corwin Sallis"], 5 * Day);

        IReadOnlyList<HeldMemory.Stack> stacks = HeldMemory.Stacks([photograph, slip]);

        Assert.Equal(4, stacks.Count);
        HeldMemory.Stack corwin = stacks.Single(s => s.Name == "Corwin Sallis");
        Assert.Equal(2, corwin.Sheets.Count);
        Assert.Equal(1, corwin.Money);
        Assert.Equal(1, corwin.Love);

        // The stack with the newest sheet in it leads, exactly as the field book's own threads do.
        Assert.Equal("Corwin Sallis", stacks[0].Name);
    }

    /// <summary>The money/love filter is a filter on SHEETS, and it filters both readings of them — the
    /// stacks and the flat page — off one rule, so the two halves of the page cannot disagree.</summary>
    [Fact]
    public void TheMoneyAndLoveFilterShowsOnlyWhatItSays()
    {
        var love = new HeldMemory.Sheet(
            "a", HeldMemory.Mark.Hers, HeldMemory.Theory.Love, "…", ["Ilse Marrow"], Day);
        var money = new HeldMemory.Sheet(
            "b", HeldMemory.Mark.His, HeldMemory.Theory.Money, "…", ["Ilse Marrow"], 2 * Day);
        HeldMemory.Sheet[] book = [love, money];

        Assert.Equal(2, HeldMemory.Stacks(book).Single().Sheets.Count);
        Assert.Equal("a", HeldMemory.Stacks(book, HeldMemory.Theory.Love).Single().Sheets.Single().Id);
        Assert.Equal("b", HeldMemory.Stacks(book, HeldMemory.Theory.Money).Single().Sheets.Single().Id);

        Assert.Equal(2, HeldMemory.Filtered(book).Count);
        Assert.Equal("a", HeldMemory.Filtered(book, HeldMemory.Theory.Love).Single().Id);
        Assert.Equal((1, 1), HeldMemory.MoneyAndLove(book));

        // Newest first on the flat page — every reading surface in this game shows the book that way up.
        Assert.Equal("b", HeldMemory.Filtered(book)[0].Id);
    }

    // ── THE SIGNING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The signing sheet GROWS a line after a rebirth rather than being replaced by a different
    /// sheet — the memory did not change, it acquired a sentence, which is the whole of what a rebirth does
    /// to a page you still have.</summary>
    [Fact]
    public void TheSigningSheetGrowsOneLineAfterARebirth()
    {
        Assert.Equal(NebulaRep.SigningMemory, NebulaRep.SigningMemoryFor(0));
        Assert.StartsWith(NebulaRep.SigningMemory, NebulaRep.SigningMemoryFor(1), StringComparison.Ordinal);
        Assert.EndsWith(NebulaRep.SigningMemoryReborn, NebulaRep.SigningMemoryFor(1), StringComparison.Ordinal);
        Assert.DoesNotContain(NebulaRep.SigningMemoryReborn, NebulaRep.SigningMemoryFor(0), StringComparison.Ordinal);

        // …and a fourth captain's copy is the same one sentence longer, not four.
        Assert.Equal(NebulaRep.SigningMemoryFor(1), NebulaRep.SigningMemoryFor(4));
    }

    // ── THE ARC'S LAW ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WORD THE ARC NEVER SAYS, swept over every surface this lane can put in front of a player. The
    /// strays are the dangerous ones: six pages that are literally about somebody else's life, and not one
    /// of them may explain itself.
    /// </summary>
    [Fact]
    public void NoGameTextInThisLaneNamesTheThing()
    {
        List<string> everySurface =
        [
            .. Flashback.Strays,
            .. Flashback.Strays.Select(Flashback.StrayToast),
            Flashback.StrayNerveLabel,
            SpreadReconcile.AgreeLine,
            SpreadReconcile.DisagreeLine,
            SpreadReconcile.NamesNoDocumentLine,
            SpreadReconcile.NotAnyonesYet,
            SpreadReconcile.TheSecondQuestion,
            NebulaRep.SigningMemory,
            NebulaRep.SigningMemoryReborn,
            NebulaLore.ById(SpreadReconcile.TheBleedId)!.Lore,
            NebulaLore.ById(SpreadReconcile.TheBleedId)!.Title,
            .. SeatedSpread.AllProse(),
            .. OldCrew.Pool.Where(p => p.Living).Select(p => OldCrewScene.Slip(p.Id)),
            .. OldCrew.Pool.Where(p => p.Living)
                .SelectMany(p => Enum.GetValues<OldCrewScene.Answer>().Select(a => OldCrewScene.Reply(p.Id, a))),
        ];

        Assert.All(everySurface, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.All(everySurface, line =>
            Assert.DoesNotContain("copy", line, StringComparison.OrdinalIgnoreCase));

        foreach (string forbidden in new[] { "restore", "clone", "backup", "cadaver", "archive", "lattice" })
        {
            Assert.All(everySurface, line =>
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase));
        }
    }
}
