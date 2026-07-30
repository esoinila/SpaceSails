using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #537 · KNOCKING ON HER WALLS, AS LAWS. Owner: <i>"Sweeping could cost time… like pumping vacuum does… idea is to
/// know where to do it… so some logic on selection based on map or other clues"</i> and <i>"Also it might be noisy
/// to say knock on walls etc."</i>
///
/// <para>Numbers derived in <c>labs/44-knock-on-the-hull</c>, which found two things worth having: the clue is
/// worth 22× and 21 loud events (so it is a mechanic), and the first version of the clue rule pointed at the wrong
/// wall on the wrong side of the keel.</para>
/// </summary>
public sealed class HullSoundingTests
{
    private static double Hull() => HullSounding.HullArea(
        WreckLayout.AftX, WreckLayout.BowX, WreckLayout.TopY, WreckLayout.BottomY);

    // ── The two gears, and the trade between them ─────────────────────────────────────────────────────

    /// <summary>
    /// PAY IN TIME OR PAY IN NOISE — and nobody gets to pay in neither. This is the pump's own asymmetry re-used,
    /// and the whole reason there are two gears. If one gear were both quieter AND faster per square metre, the
    /// other would be dead weight and the choice would be fake.
    /// </summary>
    [Fact]
    public void TheQuietGearIsSlowerAndTheFastGearIsLouder()
    {
        Assert.True(HullSounding.Earshot(HullSounding.Method.Knuckles)
                    < HullSounding.Earshot(HullSounding.Method.Sounder));
        Assert.True(HullSounding.Seconds(HullSounding.Method.Knuckles)
                    > HullSounding.Seconds(HullSounding.Method.Sounder));
        Assert.True(HullSounding.Radius(HullSounding.Method.Knuckles)
                    < HullSounding.Radius(HullSounding.Method.Sounder));

        // …and the loud one really is better per unit of ship, or nobody would ever take the risk.
        Assert.True(HullSounding.SecondsToCover(HullSounding.Method.Sounder, Hull())
                    < HullSounding.SecondsToCover(HullSounding.Method.Knuckles, Hull()));
    }

    /// <summary>The earshots are the wreck lane's OWN two numbers (QuietEarshot 13, LoudEarshot 26), so a knock is
    /// exactly as loud as dogging a hatch by hand and the sounder exactly as loud as running a pump. Nothing about
    /// searching gets private acoustics — if these drift, the hull starts hearing searching differently from
    /// everything else a captain does.</summary>
    [Fact]
    public void SoundingBorrowsTheHullsOwnTwoEarshots()
    {
        Assert.Equal(13.0, HullSounding.Earshot(HullSounding.Method.Knuckles));
        Assert.Equal(26.0, HullSounding.Earshot(HullSounding.Method.Sounder));
    }

    /// <summary>An interrupted sounding bought nothing — the pump's rule and the archive node's rule. What it did
    /// NOT undo is the noise: the ship heard the first half.</summary>
    [Fact]
    public void AnInterruptedSoundingIsNoSounding()
    {
        HullSounding.Method m = HullSounding.Method.Sounder;

        Assert.False(HullSounding.Completed(0, m));
        Assert.False(HullSounding.Completed(HullSounding.Seconds(m) - 0.01, m));
        Assert.True(HullSounding.Completed(HullSounding.Seconds(m), m));

        Assert.Contains("already heard", HullSounding.AbandonedLine, StringComparison.OrdinalIgnoreCase);
    }

    // ── What one sounding says ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ItReadsHollowOnTopOfIt_OddNearIt_AndSolidAwayFromIt()
    {
        HullSounding.Method m = HullSounding.Method.Sounder;
        double reach = HullSounding.Radius(m);

        Assert.Equal(HullSounding.Reading.Hollow, HullSounding.Read(m, 0, 0, 0, 0));
        Assert.Equal(HullSounding.Reading.Hollow, HullSounding.Read(m, 0, 0, reach - 0.01, 0));
        Assert.Equal(HullSounding.Reading.Odd, HullSounding.Read(m, 0, 0, reach + 0.01, 0));
        Assert.Equal(HullSounding.Reading.Odd,
                     HullSounding.Read(m, 0, 0, (reach * HullSounding.OddBandFactor) - 0.01, 0));
        Assert.Equal(HullSounding.Reading.Solid,
                     HullSounding.Read(m, 0, 0, (reach * HullSounding.OddBandFactor) + 0.01, 0));
    }

    /// <summary>
    /// THE ODD READING IS THE QUIET GEAR'S ENABLER, AND NOT A GENERAL SPEED-UP — Lab 44 probe E refuted the
    /// claim I built it on. Tiling beats striding for the SOUNDER at a 24 du band (3 spots vs 4, because a 4 du
    /// disc is already big against 24), and striding beats tiling for KNUCKLES everywhere past a short band
    /// (12 du: 6 → 4; 60 du: 29 → 11).
    ///
    /// <para>Pinned as the INEQUALITY the finding rests on rather than the spot counts, so the number can be
    /// tuned and the reason it exists cannot quietly evaporate: the odd band only helps when the reach is small
    /// against the distance being searched.</para>
    /// </summary>
    [Fact]
    public void TheOddBandOnlyPaysWhenTheReachIsSmallAgainstTheSearch()
    {
        double knuckleStride = HullSounding.Radius(HullSounding.Method.Knuckles) * HullSounding.OddBandFactor * 2;
        double sounderStride = HullSounding.Radius(HullSounding.Method.Sounder) * HullSounding.OddBandFactor * 2;

        // A 24 du band: the knuckles' stride needs several steps (so the third answer saves work), while the
        // sounder crosses most of it in one or two (so it does not).
        Assert.True(24.0 / knuckleStride > 3, "knuckles must need several strides across a 24 du band");
        Assert.True(24.0 / sounderStride < 2, "the sounder must cross a 24 du band in a stride or two");
    }

    // ── The clue: her own map ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A CLEAN HULL MUST MEASURE CLEAN. If the shipped layout ever registered a discrepancy, the clue would cry
    /// wolf on every wreck in the game and a captain would learn to ignore the one that matters. This is the law
    /// that keeps the instrument trustworthy.
    /// </summary>
    [Fact]
    public void TheShippedWreckBalancesExactly() =>
        Assert.Empty(HullSounding.Discrepancies(
            WreckLayout.Compartments, WreckLayout.SpineHalfHeight, WreckLayout.TopY, WreckLayout.BottomY));

    /// <summary>The doctored layout Lab 44 probe D uses: NEAR HOLD's bulkhead four frames forward, which is what
    /// a void built into her aft end looks like on the plans.</summary>
    private static (string, float, float, bool)[] WithAVoidAftOfTheNearHold() =>
    [
        ("BRIDGE", 13f, WreckLayout.BowX - 6, true),
        ("CREW SPACES", 0f, 13f, true),
        ("FORWARD LOCKER", 13f, WreckLayout.BowX - 6, false),
        ("LIFEBOAT CRADLES", 0f, 13f, false),
        ("DEEP HOLD", -15f, 0f, true),
        ("NEAR HOLD", -11f, 0f, false),
        ("ENGINEERING", WreckLayout.AftX, -15f, true),
        ("REACTOR SPACES", WreckLayout.AftX, -15f, false),
    ];

    /// <summary>
    /// AND IT MUST NAME THE RIGHT WALL. Lab 44 caught the first version of this rule pointing at <c>x −4…0</c> on
    /// the TOP side when the unaccounted space is <c>x −15…−11</c> on the BOTTOM — wrong end, wrong side of the
    /// keel. A clue that names the wrong wall is worse than no clue: a captain sounds it, hears solid, and stops
    /// believing the instrument. So the exact band is pinned.
    /// </summary>
    [Fact]
    public void TheClueNamesTheExactBandAndTheCorrectSideOfTheKeel()
    {
        IReadOnlyList<HullSounding.Discrepancy> found = HullSounding.Discrepancies(
            WithAVoidAftOfTheNearHold(), WreckLayout.SpineHalfHeight,
            WreckLayout.TopY, WreckLayout.BottomY);

        HullSounding.Discrepancy d = Assert.Single(found);

        Assert.Equal(-15.0, d.X0, 3);
        Assert.Equal(-11.0, d.X1, 3);
        Assert.False(d.Top, "the void is on the side that is MISSING the room, not the side that has it");
        Assert.Equal(24.0, d.AreaSquareDu, 3);   // 4 frames × 6 du of depth
    }

    /// <summary>
    /// THE CLUE HAS TO BE WORTH BELIEVING, or none of this is a mechanic. Lab 44: 22× on the sounder and 43× on
    /// knuckles, and — the part that actually bites — 21 fewer loud events.
    /// </summary>
    [Fact]
    public void BelievingTheClueIsWorthAnOrderOfMagnitude()
    {
        IReadOnlyList<HullSounding.Discrepancy> found = HullSounding.Discrepancies(
            WithAVoidAftOfTheNearHold(), WreckLayout.SpineHalfHeight,
            WreckLayout.TopY, WreckLayout.BottomY);
        double clue = found[0].AreaSquareDu;

        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            Assert.True(HullSounding.CluedSpeedup(m, Hull(), clue) >= 10,
                        $"{m}: a clue worth less than 10x is not worth reading a map for");
        }

        int noiseSaved = HullSounding.SpotsToCover(HullSounding.Method.Sounder, Hull())
                       - HullSounding.SpotsToCover(HullSounding.Method.Sounder, clue);
        Assert.True(noiseSaved >= 15, "the clue's real value is the rackets it does not make");
    }

    /// <summary>
    /// THE NOISE, NOT THE CLOCK, IS THE TEETH. Lab 44's sharpest finding: a blind sounder search is ~22 separate
    /// loud events, and the pack's <c>NoiseRousesAtMost = 2</c> is a PACING rule that stops one racket summoning
    /// the ship — it does nothing about twenty-two. So a blind search must cost more loud events than the whole
    /// authored pack can absorb, or searching everywhere is simply the correct play.
    /// </summary>
    [Fact]
    public void ABlindSearchMakesMoreNoiseThanTheHullCanSleepThrough()
    {
        const int authoredPack = 4;
        const int rousesPerNoise = 2;

        int spots = HullSounding.SpotsToCover(HullSounding.Method.Sounder, Hull());
        Assert.True(spots * rousesPerNoise > authoredPack * 2,
                    "blind sounding must wake everything aboard several times over");
    }

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The offer names BOTH costs before either is spent — the seconds and the fact that it will be
    /// heard. A tool that only mentions its clock is hiding the half that kills you.</summary>
    [Fact]
    public void TheOfferAdmitsTheNoiseAndNotJustTheClock()
    {
        Assert.Contains("heard", HullSounding.OfferLine(HullSounding.Method.Sounder), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quiet", HullSounding.OfferLine(HullSounding.Method.Knuckles), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5", HullSounding.OfferLine(HullSounding.Method.Sounder), StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CLUE IS OFFERED AS A MEASUREMENT, NEVER AS AN ANSWER — the #533 breadcrumb discipline. A captain told
    /// "there is a void aft of the deep hold" has been handed the find; one told that a room runs four frames
    /// further aft than the room opposite it has been handed the QUESTION, which is the whole game.
    /// </summary>
    [Fact]
    public void TheClueStatesAMeasurementAndDrawsNoConclusion()
    {
        IReadOnlyList<HullSounding.Discrepancy> found = HullSounding.Discrepancies(
            WithAVoidAftOfTheNearHold(), WreckLayout.SpineHalfHeight,
            WreckLayout.TopY, WreckLayout.BottomY);
        string line = HullSounding.ClueLine(found[0]);

        Assert.Contains("frames", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("void", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hidden", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("search here", line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Every reading has words, and the ODD one points without promising.</summary>
    [Fact]
    public void EveryReadingHasWordsAndOddOnlyPoints()
    {
        foreach (HullSounding.Reading r in Enum.GetValues<HullSounding.Reading>())
        {
            Assert.False(string.IsNullOrWhiteSpace(HullSounding.ReadingLine(r)));
        }

        string odd = HullSounding.ReadingLine(HullSounding.Reading.Odd);
        Assert.Contains("not quite here", odd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("found", odd, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>And the blind-search warning quotes the real arithmetic rather than a mood, so a captain who wants
    /// to sound the whole ship is told what that is in seconds before starting.</summary>
    [Fact]
    public void TheBlindSearchWarningQuotesTheRealCost()
    {
        string line = HullSounding.BlindSearchLine(HullSounding.Method.Sounder, Hull());

        Assert.Contains(HullSounding.SpotsToCover(HullSounding.Method.Sounder, Hull()).ToString(),
                        line, StringComparison.Ordinal);
        Assert.Contains("audible", line, StringComparison.OrdinalIgnoreCase);
    }
}
