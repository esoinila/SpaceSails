namespace SpaceSails.Core.Tests;

/// <summary>
/// #620 slice 3 · THE COLLAR ON THE SHELF — the shelf's first FIND. No face, no theory, all gravity: it
/// restores CLEAN at the standard 22 through the existing seam, has NO sting band (the unease lives in the
/// lines, never in a mechanic), no first opening and no flashback, and draws its four clean lines (Fable's
/// canon addendum 4, byte-verbatim) off the SAME seeded roll every other piece reads.
///
/// <para>The worlds can tell pass from fail: the sting sim-times are LITERAL ones (38, 50, 65, 102) where the
/// band is KNOWN to fire for the pendant and a Money sheet; a find pressed at exactly those times must restore
/// clean. And because a find draws the band and DISCARDS it, its line index equals a Love sheet's at every sim
/// time — a find that skipped the draw would shift the stream and break that equality.</para>
/// </summary>
public class KeepsakeFindTests
{
    private const double Never = double.NegativeInfinity;
    private static readonly long[] StingTimes = [38, 50, 65, 102];

    private static readonly Satchel.Item CollarItem = new(Satchel.Kind.Relic, "hive:luna:-3:7");
    private static readonly Satchel.Item HallItem = new(Satchel.Kind.Relic, UndergroundComplex.HallFindPrefix + ":x:1");

    private static HeldMemory.Sheet ASheet(string id, HeldMemory.Mark mark, HeldMemory.Theory tag) =>
        new(id, mark, tag, "a held page", ["Somebody"], 86400.0 * 3, HandedBy: "Hollis");

    private static readonly HeldMemory.Sheet Photo = ASheet(HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love);
    private static readonly HeldMemory.Sheet Slip = ASheet(HeldMemory.SlipId("fixer"), HeldMemory.Mark.His, HeldMemory.Theory.Money);

    private static Keepsake.QuietMinute PressFind(double nerve, double simTime, double since = Never,
        bool inCabin = true, bool firstOpening = false) =>
        Keepsake.Open(Keepsake.Collar, inCabin, firstOpening, nerve, since, simTime);

    // ── THE SHELF ROW (derived from the held relic; no new persisted state) ──────────────────────────

    [Fact]
    public void TheCollarIsOnTheShelfExactlyWhileTheSatchelHoldsIt_BetweenThePendantAndTheFaces()
    {
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf([], null).Select(p => p.Id));
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf([], []).Select(p => p.Id));
        Assert.Equal([Keepsake.PendantId, Keepsake.CollarId], Keepsake.Shelf([], [CollarItem]).Select(p => p.Id));
        Assert.Equal([Keepsake.PendantId, Keepsake.CollarId, HeldMemory.PhotographId, HeldMemory.SlipId("fixer")],
            Keepsake.Shelf([Photo, Slip], [CollarItem]).Select(p => p.Id));

        // not a collar: a hall record is a different relic; ordinary pocket things are not finds
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf([], [HallItem]).Select(p => p.Id));
        Assert.Equal([Keepsake.PendantId], Keepsake.Shelf([], [new Satchel.Item(Satchel.Kind.Paper, "hive:luna:-3:7")]).Select(p => p.Id));

        // put it down and it leaves the shelf — derived, never remembered
        Assert.Equal([Keepsake.PendantId],
            Keepsake.Shelf([], Satchel.Remove([CollarItem], CollarItem.Kind, CollarItem.Id, 1)).Select(p => p.Id));

        // the slice-2 one-argument shelf is unchanged
        Assert.Equal([Keepsake.PendantId, HeldMemory.PhotographId], Keepsake.Shelf([Photo]).Select(p => p.Id));
    }

    [Fact]
    public void TheCollarRowWearsItsExistingNumberFourteenIdentity_AndIsAFindNotAMementoNotASheet()
    {
        Keepsake.Piece c = Keepsake.Collar;
        Assert.Equal(CarriedObject.CollarLabel, c.Title);
        Assert.Equal("⭕ MEASURED FOR SOMETHING", c.Title);
        Assert.StartsWith(c.CardLine, CarriedObject.CollarStory); // the closed line is the story's own opening
        Assert.DoesNotContain("\n", c.CardLine);
        Assert.True(c.IsFind);
        Assert.False(c.IsSheet);
        Assert.Equal("", c.FlashbackSubject);
        Assert.False(Keepsake.Pendant.IsFind);
        Assert.False(Keepsake.FromSheet(Photo).IsFind);
    }

    // ── CLEAN, AT THE STANDARD 22, NEVER STUNG ────────────────────────────────────────────────────────

    [Fact]
    public void AFindRestoresCleanAtTheStandardChunk_NeverStung_AcrossTwelveThousandSimSeconds_AllFourLinesReached()
    {
        Assert.Equal(22.0, NerveModel.KeepsakeRestore, 6);
        var seen = new HashSet<string>();
        for (long t = 0; t < 12_000; t++)
        {
            Keepsake.QuietMinute m = PressFind(50.0, t);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.Equal(NerveModel.KeepsakeRestore, m.Delta, 6);
            Assert.Equal(NerveModel.DrinkRestore(50.0, NerveModel.DrinkKind.Keepsake, 1), m.Nerve, 6);
            Assert.Contains(m.Line, Keepsake.FindCleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.SheetCleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.CleanPool);
            Assert.DoesNotContain(m.Line, Keepsake.StingPool);
            Assert.DoesNotContain(m.Line, Keepsake.MoneyStingPool);
            seen.Add(m.Line);
        }
        Assert.Equal(4, seen.Count);
    }

    [Fact]
    public void AFindPressedAtAKnownStingRollRestoresClean_WhileThePendantAndAMoneySheetAreStungOnThatVeryTime()
    {
        foreach (long t in StingTimes)
        {
            // the world can tell: the band IS firing here for the others…
            Assert.Equal(Keepsake.Outcome.Stung, Keepsake.Open(Keepsake.Pendant, true, false, 50.0, Never, t).Outcome);
            Assert.Equal(Keepsake.Outcome.Stung, Keepsake.Open(Keepsake.FromSheet(Slip), true, false, 50.0, Never, t).Outcome);

            // …and the find is clean, at 22, from its own pool, even on its own first press
            foreach (bool first in new[] { false, true })
            {
                Keepsake.QuietMinute m = PressFind(50.0, t, firstOpening: first);
                Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
                Assert.Equal(NerveModel.KeepsakeRestore, m.Delta, 6);
                Assert.Contains(m.Line, Keepsake.FindCleanPool);
            }
        }
    }

    [Fact]
    public void TheBandRollIsStillDrawnAndDiscarded_TheFindsLineIndexIsALoveSheetsAtEverySimTime_AndStingCountsOfTheOthersAreUnchanged()
    {
        const int span = 12_000;
        int pendantStings = 0, moneyStings = 0, findStings = 0;
        for (long t = 0; t < span; t++)
        {
            Keepsake.QuietMinute find = PressFind(50.0, t);
            Keepsake.QuietMinute love = Keepsake.Open(Keepsake.FromSheet(Photo), true, false, 50.0, Never, t);
            Assert.Equal(Keepsake.SheetCleanPool.ToList().IndexOf(love.Line), Keepsake.FindCleanPool.ToList().IndexOf(find.Line));
            findStings += find.Outcome == Keepsake.Outcome.Stung ? 1 : 0;
            moneyStings += Keepsake.Open(Keepsake.FromSheet(Slip), true, false, 50.0, Never, t).Outcome == Keepsake.Outcome.Stung ? 1 : 0;
            pendantStings += Keepsake.Open(Keepsake.Pendant, true, false, 50.0, Never, t).Outcome == Keepsake.Outcome.Stung ? 1 : 0;
        }
        Assert.Equal(0, findStings);
        Assert.Equal(moneyStings, pendantStings);
        Assert.InRange(moneyStings, 850, 1150); // 1-in-12 of 12,000 is 1,000 — unchanged by a find existing
    }

    // ── THE CANON LINES, BYTE-VERBATIM, ON LITERAL SIM-TIMES ──────────────────────────────────────────

    [Fact]
    public void TheFourFindLinesAreFableCanon_AllReachedOnLiteralSimTimes()
    {
        string[] canon =
        [
            "It is exactly as heavy as it was yesterday. You check anyway. Something in you is keeping books on it.",
            "You turn it once around, the way you'd walk a fence line. All quiet. All wrong. All yours. Steadier, somehow.",
            "The light goes around it and comes back with nothing to report. You could look at that for a long time. You do.",
            "You put it away before you notice how long you've been holding it. The minute was good. You don't ask it why.",
        ];
        Assert.Equal(canon, Keepsake.FindCleanPool);

        // known rolls (the same stream the sheet pool is pinned on): sim-time 2 draws line 0, 7 line 1, 5 line 2, 0 line 3
        long[] at = [2, 7, 5, 0];
        for (int i = 0; i < at.Length; i++)
        {
            Assert.Equal(canon[i], PressFind(50.0, at[i]).Line);
        }
    }

    // ── NO FIRST OPENING, NO FLASHBACK ────────────────────────────────────────────────────────────────

    [Fact]
    public void AFindHasNoFirstOpeningAndNoFlashback_ItIsOnlyEverAQuietMinute()
    {
        for (long t = 0; t < 200; t++)
        {
            Keepsake.QuietMinute m = PressFind(40.0, t, firstOpening: true);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.False(m.RaisesFlashback);
            Assert.NotEqual(Keepsake.FirstOpeningLine, m.Line);
            Assert.Contains(m.Line, Keepsake.FindCleanPool);
        }
        Assert.Equal("", Keepsake.Collar.FlashbackSubject);
        Assert.Null(Keepsake.FlashbackCaption(Keepsake.Collar.FlashbackSubject));
    }

    // ── THE SHARED REFUSALS, BOTH DIRECTIONS ──────────────────────────────────────────────────────────

    [Fact]
    public void TheSatietyWindowIsOneForTheWholeShelf_FindAndPendantAndSheet_EveryOrderedPair()
    {
        Keepsake.Piece[] pieces = [Keepsake.Collar, Keepsake.Pendant, Keepsake.FromSheet(Photo), Keepsake.FromSheet(Slip)];
        foreach (Keepsake.Piece first in pieces)
        {
            Keepsake.QuietMinute spent = Keepsake.Open(first, true, false, 50.0, Never, simTime: 0);
            Assert.True(spent.Opened);
            foreach (Keepsake.Piece second in pieces)
            {
                foreach (long t in new long[] { 0, StingTimes[0] }) // a clean roll and a sting roll alike
                {
                    Keepsake.QuietMinute m = Keepsake.Open(second, true, false, 50.0, secondsSinceMinute: 600.0, simTime: t);
                    Assert.Equal(Keepsake.Outcome.Sated, m.Outcome);
                    Assert.False(m.Opened);
                    Assert.Equal(50.0, m.Nerve, 6);
                    Assert.Equal(Keepsake.SatietyLine, m.Line);
                }
                Assert.Equal(Keepsake.Outcome.Sated,
                    Keepsake.Open(second, true, false, 50.0, Keepsake.QuietWindowSeconds - 1.0, 0).Outcome);
            }
        }
        Assert.Equal(Keepsake.Outcome.Restored, PressFind(50.0, 0, Keepsake.QuietWindowSeconds).Outcome);
    }

    [Fact]
    public void OutsideTheCabinTheFindDoesNotOpen_EvenOnAKnownStingRoll_AndStampsNothing()
    {
        foreach (long t in new long[] { 0, StingTimes[0], StingTimes[3] })
        {
            Keepsake.QuietMinute m = PressFind(50.0, t, inCabin: false);
            Assert.Equal(Keepsake.Outcome.NotHere, m.Outcome);
            Assert.False(m.Opened);
            Assert.Equal(50.0, m.Nerve, 6);
            Assert.Equal(0.0, m.Delta, 6);
            Assert.Contains(m.Line, Keepsake.NotHerePool);
        }
    }

    [Fact]
    public void AFindNeverTakesTheGaugeAboveTheCeiling_ThroughTheSeam()
    {
        Keepsake.QuietMinute m = PressFind(NerveModel.Max - 3.0, 0);
        Assert.Equal(NerveModel.Max, m.Nerve, 6);
        Assert.Equal(3.0, m.Delta, 6);
    }
}
