using System.Reflection;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #620 slice 2 · THE SHEETS ON THE SHELF. The #973 photograph and the slips are mementos beside the pendant,
/// and the quiet minute reaches them: a LOVE-marked sheet restores clean (its own four-line clean pool, per canon addendum 3, at the pendant's
/// 22), a MONEY-marked one restores LESS (14) and on the same seeded 1-in-12 roll STINGS instead, from the
/// issue's three Money lines. The pendant's behaviour is not touched (<see cref="KeepsakeTests"/> holds it).
///
/// <para>The worlds can tell pass from fail: the sting tests stand on LITERAL sim times (38, 50, 65, 102) where
/// the seeded roll is KNOWN to fire and on times where it is known NOT to; the band's width is counted across
/// 12,000 sim-seconds rather than asserted on one lucky draw, and the Love theory is swept over the same span to
/// prove it is NEVER stung — a single clean sample would pass a mutated Love that stings sometimes.</para>
/// </summary>
public class KeepsakeSheetTests
{
    private const double Never = double.NegativeInfinity;
    private static readonly long[] StingTimes = [38, 50, 65, 102]; // the seeded roll fires: lines 2, 2, 0, 1
    private static readonly int[] StingLineAt = [2, 2, 0, 1];

    private static HeldMemory.Sheet ASheet(string id, HeldMemory.Mark mark, HeldMemory.Theory tag, string by = "") =>
        new(id, mark, tag, "a held page", ["Somebody"], 86400.0 * 3, HandedBy: by);

    private static readonly HeldMemory.Sheet Photo = ASheet(HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love, "Hollis");
    private static readonly HeldMemory.Sheet Slip = ASheet(HeldMemory.SlipId("fixer"), HeldMemory.Mark.His, HeldMemory.Theory.Money, "Brant");

    private static Keepsake.QuietMinute Press(HeldMemory.Sheet sheet, double nerve, double simTime,
        double since = Never, bool inCabin = true, bool firstOpening = false) =>
        Keepsake.Open(Keepsake.FromSheet(sheet), inCabin, firstOpening, nerve, since, simTime);

    // ── THE SHELF ROWS ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheShelfHoldsThePendantAndExactlyTheSheetsTheCaptainHolds_InTheBooksOrder()
    {
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf([]).Select(p => p.Id));

        HeldMemory.Sheet[] book = [Photo, Slip];
        Assert.Equal([Keepsake.PendantId, HeldMemory.PhotographId, HeldMemory.SlipId("fixer")],
            Keepsake.Shelf(book).Select(p => p.Id));

        // …and a stray is NOT a face: the shelf is an explicit include list (F1 ruling)
        HeldMemory.Sheet stray = ASheet(Flashback.StrayId(0), HeldMemory.Mark.NotAnyones, HeldMemory.Theory.Money);
        Assert.Equal([Keepsake.PendantId, HeldMemory.PhotographId], Keepsake.Shelf([Photo, stray]).Select(p => p.Id));

        // the no-argument shelf is still the slice-1 shelf
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf().Select(p => p.Id));
    }

    [Fact]
    public void TheShelfIsForFaces_LedgerPagesAreNotOnIt_ButThePhotographSlipsAndSummerPartyAre()
    {
        HeldMemory.Sheet[] ledgerPages =
        [
            ASheet(NebulaRep.SigningMemoryId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money),
            ASheet(StationAds.TheFilingDay, HeldMemory.Mark.Mine, HeldMemory.Theory.Money),
            ASheet(WalkIn.FirstSlipId("job-1"), HeldMemory.Mark.NotAnyones, HeldMemory.Theory.Money),
            ASheet(WalkIn.NoteId(WalkIn.Who.Ilse), HeldMemory.Mark.Hers, HeldMemory.Theory.Love),
            ASheet(InsuranceWeather.LapsedCousinSheetId, HeldMemory.Mark.His, HeldMemory.Theory.Money),
            ASheet(TheOldShip.SheetId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money),
            ASheet(PreservationOffice.SheetId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money),
            ASheet(Flashback.StrayId(2), HeldMemory.Mark.NotAnyones, HeldMemory.Theory.Money),
        ];
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf(ledgerPages).Select(p => p.Id));

        HeldMemory.Sheet party = ASheet(OldCrewScene.SummerPartyId, HeldMemory.Mark.Mine, HeldMemory.Theory.Love);
        Assert.Equal([Keepsake.PendantId, HeldMemory.PhotographId, HeldMemory.SlipId("fixer"), OldCrewScene.SummerPartyId],
            Keepsake.Shelf([.. ledgerPages, Photo, Slip, party]).Select(p => p.Id));
    }

    [Fact]
    public void AnUnsettledSheetRoutesAsLove_TwentyTwoCleanFromTheSheetPool_NeverStung_NeverThePendantsLines()
    {
        HeldMemory.Sheet odd = ASheet("slip:odd", HeldMemory.Mark.His, HeldMemory.Theory.Unsettled);
        var seen = new HashSet<string>();
        for (long t = 0; t < 12_000; t++)
        {
            Keepsake.QuietMinute m = Press(odd, 50.0, t);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.Equal(NerveModel.KeepsakeRestore, m.Delta, 6);
            Assert.Contains(m.Line, Keepsake.SheetCleanPool);
            seen.Add(m.Line);
        }
        Assert.Equal(4, seen.Count);
        Assert.False(Keepsake.FromSheet(odd).Theory == HeldMemory.Theory.Money);
        Assert.Equal(Keepsake.Outcome.Restored, Press(odd, 50.0, StingTimes[0], firstOpening: true).Outcome); // no first opening either
    }

    [Fact]
    public void ASheetsCardIsBuiltFromTheSheetsOwnFields_AndItCarriesNoFlashback()
    {
        Keepsake.Piece p = Keepsake.FromSheet(Photo);
        Assert.Equal(HeldMemory.PhotographId, p.Id);
        Assert.Equal(HeldMemory.RowTitle(Photo), p.Title);
        Assert.Equal(Photo.BookLine, p.CardLine);
        Assert.Equal(HeldMemory.Mark.His, p.Mark);
        Assert.Equal(HeldMemory.Theory.Love, p.Theory);
        Assert.Equal("", p.FlashbackSubject); // its flashback fired at handover (#973)

        // the summer-party page keeps its own row title
        HeldMemory.Sheet party = ASheet(OldCrewScene.SummerPartyId, HeldMemory.Mark.Mine, HeldMemory.Theory.Love);
        Assert.Equal(OldCrewScene.SummerPartyTitle, Keepsake.FromSheet(party).Title);
    }

    // ── LOVE: CLEAN ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALoveSheetRestoresClean_TheExistingChunkAndTheExistingPool_AndIsNeverStung()
    {
        var seen = new HashSet<string>();
        for (long t = 0; t < 12_000; t++)
        {
            Keepsake.QuietMinute m = Press(Photo, 50.0, t);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.Equal(NerveModel.KeepsakeRestore, m.Delta, 6);
            Assert.Contains(m.Line, Keepsake.SheetCleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.CleanPool); // a photograph has no hinge
            seen.Add(m.Line);
        }
        Assert.Equal(4, seen.Count); // all four sheet lines reachable
    }

    [Fact]
    public void TheSheetCleanLinesAreFableCanon_PinnedOnLiteralSimTimes_AllFourReachedByLoveAndMoney()
    {
        string[] canon =
        [
            "You hold it by the edges, the way you were never taught and always knew. A minute passes that nobody bills.",
            "The faces have not aged a day since the last time you looked. Somebody in this picture still owes somebody a drink.",
            "Paper remembers differently than you do — flatter, kinder. You let its version stand a while.",
            "You know where you were standing when this was taken. You can almost feel the floor of it under your boots.",
        ];
        Assert.Equal(canon, Keepsake.SheetCleanPool);

        // known clean rolls: sim-time 2 draws line 0, 7 line 1, 5 line 2, 0 line 3 (both theories, one stream)
        long[] at = [2, 7, 5, 0];
        for (int i = 0; i < at.Length; i++)
        {
            Assert.Equal(canon[i], Press(Photo, 50.0, at[i]).Line);
            Assert.Equal(canon[i], Press(Slip, 50.0, at[i]).Line);
        }
    }


    // ── MONEY: LESS, AND THE STING ────────────────────────────────────────────────────────────────────

    [Fact]
    public void AMoneySheetRestoresLessThanALovedFace_ThroughTheReliefSeam_FlatAndLevelIndependent()
    {
        Assert.Equal(14.0, NerveModel.KeepsakeMoneyRestore, 6);
        Assert.True(NerveModel.KeepsakeMoneyRestore < NerveModel.KeepsakeRestore);
        Assert.True(NerveModel.KeepsakeMoneyRestore > 0.0);

        foreach (double nerve in new[] { 3.0, 40.0, 70.0 })
        {
            Keepsake.QuietMinute m = Press(Slip, nerve, simTime: 0); // t=0 is a known clean draw
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.Equal(NerveModel.KeepsakeMoneyRestore, m.Delta, 6);
            Assert.Equal(NerveModel.DrinkRestore(nerve, NerveModel.DrinkKind.KeepsakeMoney, 1), m.Nerve, 6);
            Assert.Contains(m.Line, Keepsake.SheetCleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.CleanPool); // a photograph has no hinge
        }
    }

    [Fact]
    public void OnAKnownBadRoll_AMoneySheetStings_FromTheThreeCanonMoneyLines_AndCostsADab()
    {
        for (int i = 0; i < StingTimes.Length; i++)
        {
            Keepsake.QuietMinute m = Press(Slip, 50.0, StingTimes[i]);
            Assert.Equal(Keepsake.Outcome.Stung, m.Outcome);
            Assert.Equal(50.0 - Keepsake.MoneyStingNerve, m.Nerve, 6);
            Assert.Equal(-Keepsake.MoneyStingNerve, m.Delta, 6);
            Assert.Equal(Keepsake.MoneyStingPool[StingLineAt[i]], m.Line);
            Assert.DoesNotContain(m.Line, Keepsake.StingPool); // never the pendant's pool
        }
        Assert.Equal(3, StingLineAt.Distinct().Count()); // all three Money lines reached above
        Assert.Equal(4.0, Keepsake.MoneyStingNerve, 6);

        // canon, byte-identical (full strings)
        Assert.Equal("A good evening, a steady hand on your shoulder, and you know exactly what it cost you, to the decimal. You look anyway.", Keepsake.MoneyStingPool[0]);
        Assert.Equal("They're smiling because the deal hadn't landed yet. You keep it because somebody has to remember the before.", Keepsake.MoneyStingPool[1]);
        Assert.Equal("The picture hasn't changed. Your reading of it does, some nights, and tonight is one of them.", Keepsake.MoneyStingPool[2]);
        Assert.Equal(3, Keepsake.MoneyStingPool.Count);
    }

    [Fact]
    public void AMoneyStingNeverTakesTheGaugeBelowZero()
    {
        Keepsake.QuietMinute m = Press(Slip, 1.0, StingTimes[0]);
        Assert.Equal(Keepsake.Outcome.Stung, m.Outcome);
        Assert.Equal(NerveModel.Min, m.Nerve, 6);
        Assert.Equal(NerveModel.Min - 1.0, m.Delta, 6);
    }

    [Fact]
    public void TheMoneyBandIsOneInTwelve_AndTheLoveBandIsNone_OverTheSameSpan()
    {
        const int span = 12_000;
        int moneyStings = 0, loveStings = 0, pendantStings = 0;
        for (long t = 0; t < span; t++)
        {
            moneyStings += Press(Slip, 50.0, t).Outcome == Keepsake.Outcome.Stung ? 1 : 0;
            loveStings += Press(Photo, 50.0, t).Outcome == Keepsake.Outcome.Stung ? 1 : 0;
            pendantStings += Keepsake.Open(Keepsake.Pendant, true, false, 50.0, Never, t).Outcome == Keepsake.Outcome.Stung ? 1 : 0;
        }
        Assert.InRange(moneyStings, 850, 1150); // 1-in-12 of 12,000 is 1,000
        Assert.Equal(0, loveStings);
        Assert.Equal(moneyStings, pendantStings); // one roll, one band — the same sim times sting
    }

    // ── THE REFUSALS REACH SHEETS TOO ─────────────────────────────────────────────────────────────────

    [Fact]
    public void InsideTheSharedWindowASheetDoesNothingButSayWhy_WhateverItsTheory()
    {
        foreach (HeldMemory.Sheet sheet in new[] { Photo, Slip })
        {
            foreach (long t in new long[] { 0, StingTimes[0] }) // a clean roll and a sting roll alike
            {
                Keepsake.QuietMinute m = Press(sheet, 50.0, t, since: 5.0);
                Assert.Equal(Keepsake.Outcome.Sated, m.Outcome);
                Assert.False(m.Opened);
                Assert.Equal(50.0, m.Nerve, 6);
                Assert.Equal(Keepsake.SatietyLine, m.Line);
            }
            Assert.Equal(Keepsake.Outcome.Restored, Press(sheet, 50.0, 0, Keepsake.QuietWindowSeconds).Outcome);
        }
    }

    [Fact]
    public void OutsideTheCabinASheetDoesNotOpen_EvenOnAKnownStingRoll()
    {
        foreach (HeldMemory.Sheet sheet in new[] { Photo, Slip })
        {
            Keepsake.QuietMinute m = Press(sheet, 50.0, StingTimes[0], inCabin: false);
            Assert.Equal(Keepsake.Outcome.NotHere, m.Outcome);
            Assert.False(m.Opened);
            Assert.Equal(50.0, m.Nerve, 6);
            Assert.Contains(m.Line, Keepsake.NotHerePool);
        }
    }

    // ── NO FLASHBACK FOR A SHEET ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void AFirstOpeningOfASheetRaisesNoFlashback_ItIsJustAQuietMinute()
    {
        foreach (HeldMemory.Sheet sheet in new[] { Photo, Slip })
        {
            Keepsake.QuietMinute m = Press(sheet, 50.0, 0, firstOpening: true);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.False(m.RaisesFlashback);
            Assert.NotEqual(Keepsake.FirstOpeningLine, m.Line);
            Assert.Contains(m.Line, Keepsake.SheetCleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.CleanPool); // a photograph has no hinge
        }

        // …and unlike the pendant (whose first opening is never stung) a Money sheet CAN sting on the first press
        Keepsake.QuietMinute stung = Press(Slip, 50.0, StingTimes[0], firstOpening: true);
        Assert.Equal(Keepsake.Outcome.Stung, stung.Outcome);
        Assert.Equal(Keepsake.Outcome.FirstOpening,
            Keepsake.Open(Keepsake.Pendant, true, true, 50.0, Never, 0).Outcome); // the pendant is unchanged
    }

    // ── THE WORDS ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NothingElseIsAuthoredHere_EveryPublicStringIsDeclaredInAllProse()
    {
        List<string> declared = Keepsake.AllProse().ToList();
        Assert.Equal(6 + 5 + 4 + 4 + 3 + 3 + 2, declared.Count);

        string[] notProse = [nameof(Keepsake.PendantId), nameof(Keepsake.CollarId), nameof(Keepsake.PendantSubject), nameof(Keepsake.FieldBookGlyph)];
        foreach (FieldInfo f in typeof(Keepsake).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string s && !notProse.Contains(f.Name))
            {
                Assert.True(declared.Contains(s, StringComparer.Ordinal), $"Keepsake publishes undeclared prose: {f.Name}");
            }
        }
        foreach (string line in Keepsake.CleanPool.Concat(Keepsake.SheetCleanPool).Concat(Keepsake.FindCleanPool).Concat(Keepsake.StingPool).Concat(Keepsake.MoneyStingPool).Concat(Keepsake.NotHerePool))
        {
            Assert.Contains(line, declared);
        }
    }

    [Fact]
    public void NoKeepsakeLineSettlesAFaceOrNamesTheThing_SweptOverEveryPublishedString()
    {
        // opinion, never verdict (§13.8); and the reserved words the arc never says
        foreach (string line in Keepsake.AllProse())
        {
            foreach (string bad in new[] { "love", "money", "betray", "restore", "clone", "backup", "archive", "lattice", "copy", "kaamos", "reever" })
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
