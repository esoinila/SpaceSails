namespace SpaceSails.Core.Tests;

/// <summary>
/// #620 slice 1 · THE KEEPSAKE SHELF. Every band the pendant's quiet minute has, pinned: the restore amount on
/// the existing relief seam, the shared satiety window's two edges, the 1-in-12 unsettled band on the one
/// seeded roll (with LITERAL sim times, so a changed seed formula goes red, not just a changed count), which
/// canon line each roll picks, the refusals' order, and the first opening.
///
/// <para>Worlds are chosen so a test can tell pass from fail: the sting tests stand on sim times where the
/// seeded roll is KNOWN to fire (38, 50, 65, 102) and where it is known NOT to (0..7); the band's width is
/// counted across 12,000 sim-seconds rather than asserted on one lucky draw.</para>
/// </summary>
public class KeepsakeTests
{
    private const double Never = double.NegativeInfinity;
    private static readonly long[] StingTimes = [38, 50, 65, 102]; // the seeded roll fires: lines 2, 2, 0, 1
    private static readonly int[] StingLineAt = [2, 2, 0, 1];

    private static Keepsake.QuietMinute Later(double nerve, double simTime, double since = Never, bool inCabin = true) =>
        Keepsake.Open(Keepsake.Pendant, inCabin, firstOpening: false, nerve, since, simTime);

    // ── THE SHELF ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheRunStartsWithThePendantOnTheShelf_Unsettled()
    {
        Keepsake.Piece only = Assert.Single(Keepsake.Shelf());
        Assert.Equal(Keepsake.PendantId, only.Id);
        Assert.Equal("A gold pendant, worn smooth", only.Title);
        Assert.Equal("A hinge, a hairline seam, and more weight than the gold explains.", only.CardLine);
        Assert.Equal(HeldMemory.Theory.Unsettled, only.Theory);
        Assert.Equal(HeldMemory.Mark.Mine, only.Mark);
        Assert.Equal("the face in the pendant", only.FlashbackSubject);
        Assert.Equal("unsettled", HeldMemory.Label(HeldMemory.Theory.Unsettled));
        Assert.Equal("love", HeldMemory.Label(HeldMemory.Theory.Love));
        Assert.Equal("money", HeldMemory.Label(HeldMemory.Theory.Money));
    }

    // ── THE RESTORE (the existing relief seam) ────────────────────────────────────────────────────────

    [Fact]
    public void AQuietMinuteRestoresTheKeepsakeChunk_FlatAndLevelIndependent_ThroughTheReliefSeam()
    {
        // t=0 is a known CLEAN draw. Flat at shot, middling and steady nerves; the number the seam itself gives.
        foreach (double nerve in new[] { 3.0, 40.0, 70.0 })
        {
            Keepsake.QuietMinute m = Later(nerve, simTime: 0);
            Assert.Equal(Keepsake.Outcome.Restored, m.Outcome);
            Assert.Equal(NerveModel.KeepsakeRestore, m.Delta, 6);
            Assert.Equal(NerveModel.DrinkRestore(nerve, NerveModel.DrinkKind.Keepsake, 1), m.Nerve, 6);
        }
        Assert.Equal(22.0, NerveModel.KeepsakeRestore, 6);
    }

    [Fact]
    public void TheMinuteSitsBetweenALoneTotAndANightsBunk()
    {
        Assert.True(NerveModel.KeepsakeRestore > NerveModel.GalleyTotBaseRestore);
        Assert.True(NerveModel.KeepsakeRestore < NerveModel.SleepRestore);
    }

    [Fact]
    public void TheRestoreClampsAtTheFullGauge()
    {
        Keepsake.QuietMinute m = Later(95.0, simTime: 0);
        Assert.Equal(NerveModel.Max, m.Nerve, 6);
        Assert.Equal(NerveModel.Max - 95.0, m.Delta, 6);
    }

    // ── THE SHARED SATIETY WINDOW ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void InsideTheWindowTheLocketDoesNothingButSayWhy()
    {
        foreach (double since in new[] { 0.0, 1.0, Keepsake.QuietWindowSeconds - 1.0 })
        {
            Keepsake.QuietMinute m = Later(50.0, simTime: 0, since);
            Assert.Equal(Keepsake.Outcome.Sated, m.Outcome);
            Assert.False(m.Opened);
            Assert.Equal(50.0, m.Nerve, 6);
            Assert.Equal(0.0, m.Delta, 6);
            Assert.Equal("It's the same picture it was an hour ago. That's not what it's for.", m.Line);
        }
    }

    [Fact]
    public void TheWindowsEdges_InclusiveAtZero_ExclusiveAtTheFar_AndNeverOpenedReadsAsNotSated()
    {
        Assert.True(Keepsake.StillSated(0.0));
        Assert.True(Keepsake.StillSated(Keepsake.QuietWindowSeconds - 0.001));
        Assert.False(Keepsake.StillSated(Keepsake.QuietWindowSeconds));
        Assert.False(Keepsake.StillSated(-1.0));                    // a clock that jumped back reads NOT sated
        Assert.False(Keepsake.StillSated(double.NegativeInfinity)); // never opened: the first minute always lands
        Assert.Equal(2.0 * 3600.0, Keepsake.QuietWindowSeconds, 6);

        Assert.Equal(Keepsake.Outcome.Restored, Later(50.0, 0, Keepsake.QuietWindowSeconds).Outcome);
    }

    [Fact]
    public void TheWindowIsSpentByAMinuteAndOnlyByAMinute()
    {
        Assert.True(Later(50.0, 0).Opened);               // restored
        Assert.True(Later(50.0, StingTimes[0]).Opened);   // stung — a minute spent all the same
        Assert.True(Keepsake.Open(Keepsake.Pendant, true, true, 50.0, Never, 0).Opened); // first opening
        Assert.False(Later(50.0, 0, since: 5.0).Opened);                // sated
        Assert.False(Later(50.0, 0, inCabin: false).Opened);            // not here
    }

    // ── THE UNSETTLED BAND ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnAKnownBadRoll_TheFaceReadsDifferently_AndTheMinuteCostsADab()
    {
        for (int i = 0; i < StingTimes.Length; i++)
        {
            Keepsake.QuietMinute m = Later(50.0, StingTimes[i]);
            Assert.Equal(Keepsake.Outcome.Stung, m.Outcome);
            Assert.Equal(50.0 - Keepsake.StingNerve, m.Nerve, 6);
            Assert.Equal(-Keepsake.StingNerve, m.Delta, 6);
            Assert.Equal(Keepsake.StingPool[StingLineAt[i]], m.Line);
        }
        Assert.Equal(3, Keepsake.StingPool.Count);
        Assert.Equal(3, StingLineAt.Distinct().Count()); // all three canon stings are reached above
    }

    [Fact]
    public void AStingNeverTakesTheGaugeBelowZero()
    {
        Keepsake.QuietMinute m = Later(1.0, StingTimes[0]);
        Assert.Equal(Keepsake.Outcome.Stung, m.Outcome);
        Assert.Equal(NerveModel.Min, m.Nerve, 6);
        Assert.Equal(NerveModel.Min - 1.0, m.Delta, 6);
    }

    [Fact]
    public void TheBandIsOneInTwelve_OnTheSameRollThatPicksTheLine()
    {
        const int span = 12_000;
        int stings = 0;
        for (long t = 0; t < span; t++)
        {
            if (Later(50.0, t).Outcome == Keepsake.Outcome.Stung)
            {
                stings++;
            }
        }
        // 1-in-12 of 12,000 is 1,000. The band is not "never" and it is not "often": +-15% around it.
        Assert.InRange(stings, 850, 1150);
        Assert.Equal(12, Keepsake.StingOneIn);
        Assert.Equal(4.0, Keepsake.StingNerve, 6);
    }

    [Fact]
    public void TheCleanLinesRotate_AllFiveReachable_EachCanonVerbatim()
    {
        var seen = new HashSet<string>();
        for (long t = 0; t < 400; t++)
        {
            Keepsake.QuietMinute m = Later(50.0, t);
            if (m.Outcome == Keepsake.Outcome.Restored)
            {
                Assert.Contains(m.Line, Keepsake.CleanPool);
                seen.Add(m.Line);
            }
        }
        Assert.Equal(5, seen.Count);
        Assert.Equal(5, Keepsake.CleanPool.Count);
        // Literal pins: a changed seed formula moves these.
        Assert.Equal(Keepsake.CleanPool[3], Later(50.0, 0).Line);
        Assert.Equal(Keepsake.CleanPool[4], Later(50.0, 1).Line);
        Assert.Equal(Keepsake.CleanPool[1], Later(50.0, 3).Line);
        Assert.Equal(Keepsake.CleanPool[0], Later(50.0, 4).Line);
        Assert.StartsWith("You look until the lamp's hum comes back.", Keepsake.CleanPool[0]);
        Assert.StartsWith("You don't say anything.", Keepsake.CleanPool[4]);
    }

    [Fact]
    public void ARunReplaysExactly_SameInputsSameMinute()
    {
        for (long t = 0; t < 200; t++)
        {
            Assert.Equal(Later(37.0, t), Later(37.0, t));
        }
    }

    [Fact]
    public void NoLineEverSettlesTheFace_NoneNamesLoveOrMoney()
    {
        foreach (string line in Keepsake.CleanPool.Concat(Keepsake.StingPool).Concat(Keepsake.NotHerePool)
                     .Append(Keepsake.FirstOpeningLine).Append(Keepsake.FieldBookLine).Append(Keepsake.SatietyLine))
        {
            Assert.DoesNotContain("love", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("money", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("betray", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── NOT HERE ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OutsideTheCabinTheLocketDoesNotOpen_EvenOnAKnownCleanOrStingRoll()
    {
        foreach (long t in new long[] { 0, 1, 2, 3, StingTimes[0], StingTimes[2] })
        {
            Keepsake.QuietMinute m = Later(50.0, t, inCabin: false);
            Assert.Equal(Keepsake.Outcome.NotHere, m.Outcome);
            Assert.Equal(50.0, m.Nerve, 6);
            Assert.Equal(0.0, m.Delta, 6);
            Assert.Contains(m.Line, Keepsake.NotHerePool);
        }
        Assert.Equal(2, Keepsake.NotHerePool.Count);
        Assert.Equal(Keepsake.NotHerePool[0], Later(50.0, 0, inCabin: false).Line);
        Assert.Equal(Keepsake.NotHerePool[1], Later(50.0, 2, inCabin: false).Line);
        Assert.Equal("Not here. Not with the concourse watching.", Keepsake.NotHerePool[0]);
    }

    [Fact]
    public void NotHereComesBeforeSatiety_SoARefusalAwayFromTheCabinNeverReadsTheWindow()
    {
        Keepsake.QuietMinute m = Later(50.0, 0, since: 5.0, inCabin: false);
        Assert.Equal(Keepsake.Outcome.NotHere, m.Outcome);
    }

    // ── THE FIRST OPENING ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFirstOpeningRaisesTheFlashback_RestoresClean_AndSpeaksTheCanonText()
    {
        Keepsake.QuietMinute m = Keepsake.Open(Keepsake.Pendant, true, firstOpening: true, 50.0, Never, simTime: 0);
        Assert.Equal(Keepsake.Outcome.FirstOpening, m.Outcome);
        Assert.True(m.RaisesFlashback);
        Assert.Equal(50.0 + NerveModel.KeepsakeRestore, m.Nerve, 6);
        Assert.StartsWith("You know the face the way you know a word in a language you've stopped dreaming in.", m.Line);
        Assert.EndsWith("The picture doesn't say. It never has.", m.Line);
    }

    [Fact]
    public void TheFirstOpeningIsNeverStung_EvenOnAKnownBadRoll()
    {
        foreach (long t in StingTimes)
        {
            Keepsake.QuietMinute m = Keepsake.Open(Keepsake.Pendant, true, firstOpening: true, 50.0, Never, t);
            Assert.Equal(Keepsake.Outcome.FirstOpening, m.Outcome);
            Assert.True(m.Delta > 0);
        }
    }

    [Fact]
    public void OnlyTheFirstOpeningRaisesTheFlashback_NotALaterMinuteNorARefusal()
    {
        Assert.False(Later(50.0, 0).RaisesFlashback);
        Assert.False(Later(50.0, StingTimes[0]).RaisesFlashback);
        Assert.False(Keepsake.Open(Keepsake.Pendant, false, true, 50.0, Never, 0).RaisesFlashback); // not here
        Assert.False(Keepsake.Open(Keepsake.Pendant, true, true, 50.0, 5.0, 0).RaisesFlashback);    // sated
    }

    [Fact]
    public void TheFlashbackPlateCaptionIsChosenBySubject_AndEveryOtherSubjectKeepsItsOwn()
    {
        Assert.Equal(Keepsake.FirstOpeningLine, Keepsake.FlashbackCaption(Keepsake.PendantSubject));
        Assert.Null(Keepsake.FlashbackCaption("photograph"));
        Assert.Null(Keepsake.FlashbackCaption(null));
        // …and the real plate agrees: the story-beat caption for the pendant's subject is the canon text.
        Assert.Equal(Keepsake.FirstOpeningLine, StoryBeats.Caption(StoryBeats.Beat.Flashback, Keepsake.PendantSubject));
        Assert.NotEqual(Keepsake.FirstOpeningLine, StoryBeats.Caption(StoryBeats.Beat.Flashback, "photograph"));
    }

    [Fact]
    public void TheSteadyingNoteSpeaksInTheMinutesOwnVoice()
    {
        Assert.Contains("minute", NerveModel.SteadyingNote(NerveModel.DrinkKind.Keepsake, 1, 22.0));
        Assert.DoesNotContain("rum", NerveModel.SteadyingNote(NerveModel.DrinkKind.Keepsake, 3, 22.0));
    }
}
