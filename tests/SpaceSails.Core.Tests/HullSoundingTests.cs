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

    // ── The lie in her paperwork ──────────────────────────────────────────────────────────────────────

    private static HullSounding.HiddenVoid? VoidOn(string wreckId) =>
        HullSounding.VoidFor(wreckId, WreckLayout.Compartments, WreckLayout.SpineHalfHeight,
                             WreckLayout.TopY, WreckLayout.BottomY);

    /// <summary>
    /// A HULL IS OR NEVER WAS. Seeded off her id, so a captain who leaves and comes back finds the same ship — a
    /// void that re-rolled on the second boarding would turn the whole search into a slot machine, and the clue
    /// into a lie.
    /// </summary>
    [Fact]
    public void WhatSheHidesIsDecidedByWhoSheIsAndNeverRerolled()
    {
        foreach (string id in new[] { "quiet-sister", "marbury", "ptarmigan", "long-shrift" })
        {
            HullSounding.HiddenVoid? first = VoidOn(id);
            for (int again = 0; again < 5; again++)
            {
                Assert.Equal(first, VoidOn(id));
            }
        }
    }

    /// <summary>
    /// MOST SHIPS ARE EXACTLY WHAT THEY LOOK LIKE. Lab 44 probe F: 3 of the 10 seeded hulls hide something. If
    /// every wreck lied, measuring would be routine and finding one would be worth nothing; if none did, the
    /// manifest would be furniture. Pinned as a band rather than a count, so the roll can be tuned.
    /// </summary>
    [Fact]
    public void MostHullsAreHonestAndSomeAreNot()
    {
        int lying = 0, total = 0;
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            if (Derelict.SeededWithCause(cause) is not { } w)
            {
                continue;
            }
            total++;
            if (VoidOn(w.Id) is not null)
            {
                lying++;
            }
        }

        Assert.True(total >= 8, "the seeded causes must actually produce hulls to measure");
        Assert.True(lying > 0, "if no hull ever lies, her manifest is furniture");
        Assert.True(lying < total, "if every hull lies, the manifest may as well be a treasure map");

        // The RATE is pinned on the rule rather than on this small sample: ten seeded causes is far too few for
        // an observed count to mean anything (it has read 2, 3 and 4 across three shuffles of the placement code
        // without the odds changing at all). What must hold is that a hull hiding something stays UNCOMMON —
        // one in five — because at one in two, measuring becomes routine and finding one is worth nothing.
        Assert.True(HullSounding.VoidOnARollOf <= 4,
                    "a void on more than a fifth of hulls makes the search a chore rather than a find");
    }

    /// <summary>
    /// THE VOID HAS SOMEWHERE TO BE — the law the whole redesign exists for. Owner, looking at the map:
    /// <i>"The Quiet Sister's room map did not show any extra space on the sides. Is the room smaller on the
    /// inside than in the map?"</i> It was not, and that was the bug: her compartments are contiguous, so a void
    /// inside one had nowhere to physically exist. His own answer became the fix — <i>"making outside walls
    /// thicker (shielding etc) might offer less audited dimensions"</i> — so every void now sits in the shielding
    /// band, which is real space, on every hull, outboard of the rooms.
    /// </summary>
    [Fact]
    public void EveryVoidSitsInSpaceTheShipActuallyHas()
    {
        int seen = 0;
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            if (Derelict.SeededWithCause(cause) is not { } w || VoidOn(w.Id) is not { } hidden)
            {
                continue;
            }

            seen++;
            Assert.InRange(hidden.X0, WreckLayout.TransomX, WreckLayout.ShieldingForwardEnd);
            Assert.InRange(hidden.X1, WreckLayout.TransomX, WreckLayout.ShieldingForwardEnd);
            Assert.True(hidden.X1 > hidden.X0, $"{w.ShipName}'s void has no length");
            Assert.True(hidden.AreaSquareDu > 5, $"{w.ShipName}'s void is too small to hide anything in");
        }

        Assert.True(seen > 0, "no hull hid anything, so this law tested nothing");
    }

    /// <summary>
    /// AND THE PLATE IS SOMEWHERE A CAPTAIN CAN STAND AND REACH. A false plate inside a wall, in a corner, or in
    /// a room nobody can enter is a find nobody can make — the reachability failure the deck audit exists for, in
    /// a place the deck audit cannot see, because the plate is not on the plan until it has been found.
    /// </summary>
    [Fact]
    public void ThePlateIsOnAWallOfARoomWithSpaceToStandOffIt()
    {
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            if (Derelict.SeededWithCause(cause) is not { } w || VoidOn(w.Id) is not { } hidden)
            {
                continue;
            }

            (string name, float x0, float x1, bool top) = System.Array.Find(
                WreckLayout.Compartments, c => c.Name == hidden.NearRoom);

            Assert.Equal(hidden.NearRoom, name);
            Assert.Equal(top, hidden.Top);
            Assert.Equal(top ? WreckLayout.TopY : WreckLayout.BottomY, hidden.PlateY, 3);
            Assert.True(hidden.PlateX > x0 + 1.5 && hidden.PlateX < x1 - 1.5,
                        $"{w.ShipName}'s plate is jammed into a corner of {name}");
        }
    }

    /// <summary>The band the clue names is the band the plate opens into — a clue that points somewhere other than
    /// the find is the bug Lab 44 caught in the first geometry rule, wearing a new costume.</summary>
    [Fact]
    public void TheClueAndThePlateAgreeAboutWhereSheIsLying()
    {
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            if (Derelict.SeededWithCause(cause) is not { } w || VoidOn(w.Id) is not { } hidden)
            {
                continue;
            }

            HullSounding.Discrepancy clue = HullSounding.AsDiscrepancy(hidden);

            Assert.Equal(hidden.Top, clue.Top);
            Assert.InRange(hidden.PlateX, clue.X0, clue.X1);
            Assert.Equal(hidden.AreaSquareDu, clue.AreaSquareDu, 3);
        }
    }

    /// <summary>The manifest's lie reads out as the same <see cref="HullSounding.Discrepancy"/> the geometry rule
    /// produces, so one panel can show either without knowing which it got — and it still refuses to draw a
    /// conclusion.</summary>
    [Fact]
    public void TheManifestLieBecomesAnOrdinaryDiscrepancy()
    {
        HullSounding.HiddenVoid hidden =
            new("DEEP HOLD", Outboard: true, -12, -6, true, -9, WreckLayout.TopY,
                6 * WreckLayout.ShieldingDepth, "something");
        HullSounding.Discrepancy d = HullSounding.AsDiscrepancy(hidden);

        Assert.Equal(6 * WreckLayout.ShieldingDepth, d.AreaSquareDu, 3);
        Assert.Contains("shielding", d.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hidden", HullSounding.ClueLine(d), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The find line describes the contents and never prices them — the #533 discipline at the one moment
    /// the game could get away with breaking it.</summary>
    [Fact]
    public void TheFindDescribesAndDoesNotValue()
    {
        HullSounding.HiddenVoid hidden =
            new("DEEP HOLD", Outboard: true, -12, -6, true, -9, WreckLayout.TopY,
                6 * WreckLayout.ShieldingDepth, "A rack of code keys.");
        string line = HullSounding.FoundItLine(hidden);

        Assert.Contains("A rack of code keys.", line, StringComparison.Ordinal);
        Assert.DoesNotContain("worth", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" cr", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("valuable", line, StringComparison.OrdinalIgnoreCase);
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
