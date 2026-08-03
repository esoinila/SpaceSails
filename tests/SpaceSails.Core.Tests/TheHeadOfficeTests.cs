namespace SpaceSails.Core.Tests;

/// <summary>
/// #411/#635 · THE HEAD OFFICE — the owner's ruling of 2026-08-03, as guards.
///
/// <para><i>"The KAAMOS destination is the HEAD of the organization … As fancy as the secret labs … The Hive
/// facilities are branch offices. HQ outclasses them, and it should outclass them IN THE SAME VOCABULARY, so
/// a player who has crawled a Hive recognises the rank difference without being told it."</i></para>
///
/// <para>That last clause is the whole engineering problem, and it is what these tests are about. "Fancier"
/// is not a feeling here; it is a set of comparisons against the thing the generator actually produces for
/// an ordinary site, in the same seven or eight pieces of vocabulary the player has spent hours learning
/// underground. Every claim below is therefore <b>two-sided</b>: it says what the head office does AND what
/// a branch office does, so a change that quietly levels them down fails.</para>
///
/// <para>And the discipline that outranks all of it: <b>none of it explains anything.</b> The plates are
/// held to the same forbidden vocabulary as the rest of the arc — the deepest floor of the biggest building
/// in the game may be full of evidence of an enormous, decades-long operation and may never once say what
/// the operation produced. That is standing canon (owner, 2026-07-30) and this is the single most tempting
/// place in the game to break it.</para>
/// </summary>
public class TheHeadOfficeTests
{
    private const string Hq = KaamosLore.IceMoonBodyId;

    /// <summary>The branch offices a player actually crawls, from the scenario's own moons — the comparison
    /// class. Typed here rather than loaded because these are the sites the shipped audits already walk
    /// (<c>YouCanWalkTheHiveTests</c> uses the same list), and a comparison against a set nobody visits
    /// would be a guard handed the wrong world.</summary>
    private static readonly string[] BranchOffices =
    [
        "luna", "phobos", "europa", "ganymede", "callisto", "titan", "miranda", "triton", "the-clinker",
    ];

    // ── There is exactly one, and no seed can make another ───────────────────────────────────────────────

    [Fact]
    public void ThereIsOneHeadOfficeAndItIsUnderTheIceMoon()
    {
        Assert.True(UndergroundComplex.IsHeadOffice(Hq));
        Assert.Equal(UndergroundComplex.Kind.HeadOffice, UndergroundComplex.KindFor(Hq));

        foreach (string body in BranchOffices)
        {
            Assert.False(UndergroundComplex.IsHeadOffice(body));
            Assert.NotEqual(UndergroundComplex.Kind.HeadOffice, UndergroundComplex.KindFor(body));
        }
    }

    /// <summary>The die that picks a site's kind has never heard of the head office. Swept wide, because a
    /// head office a seed could roll twice would not be a head office — and because the enum grew, which is
    /// exactly the edit that would have made <c>KindFor</c>'s roll start producing it.</summary>
    [Fact]
    public void NoSeedCanEverRollAHeadOffice()
    {
        for (int i = 0; i < 3000; i++)
        {
            Assert.NotEqual(UndergroundComplex.Kind.HeadOffice, UndergroundComplex.KindFor($"seeded-moon-{i}"));
        }
    }

    // ── The ice is empty until your hull is on the board ─────────────────────────────────────────────────

    /// <summary>
    /// The head office is the only site in the game whose existence is a fact about the CAPTAIN. A captain
    /// who flies to the ice moon without the berth code must find featureless ice and a good view — not a
    /// locked door, not a refusal, not a hint. Refusal-by-absence is the whole reason the arrival lands.
    ///
    /// <para>Two-sided against the arc's real gate: four shards of five is not enough (that is the shape,
    /// not the string), the capstone pasted in with no intel behind it is not enough either, and both
    /// together open it. If this ever passes on a bare progress the arc has lost its lock.</para>
    /// </summary>
    [Fact]
    public void TheIceIsEmptyUntilTheHullIsOnTheBoard()
    {
        var nothing = new KaamosProgress();
        Assert.False(UndergroundComplex.HeadOfficePresent(Hq, nothing.CanReachEnceladus));

        var shape = new KaamosProgress();
        foreach (KaamosFragment f in KaamosLore.IntelFragments)
        {
            shape.Assemble(f.Id);
        }
        Assert.False(UndergroundComplex.HeadOfficePresent(Hq, shape.CanReachEnceladus),
            "every shard of intel and no code opened the ice — the capstone is the key, not the shape");

        var pasted = new KaamosProgress();
        pasted.Assemble(KaamosLore.KeyFragment.Id);
        Assert.False(UndergroundComplex.HeadOfficePresent(Hq, pasted.CanReachEnceladus),
            "a berth code with nothing behind it opened the ice");

        var earned = new KaamosProgress();
        foreach (KaamosFragment f in KaamosLore.Fragments)
        {
            earned.Assemble(f.Id);
        }
        Assert.True(UndergroundComplex.HeadOfficePresent(Hq, earned.CanReachEnceladus));
    }

    /// <summary>And it is the ice moon or nowhere: being on the board does not grow a head office under
    /// somebody else's moon.</summary>
    [Fact]
    public void BeingOnTheBoardDoesNotPutAHeadOfficeUnderEveryMoon()
    {
        foreach (string body in BranchOffices)
        {
            Assert.False(UndergroundComplex.HeadOfficePresent(body, onTheBoard: true));
        }
    }

    // ── Depth: it takes the whole allowance, and lists all of it ─────────────────────────────────────────

    /// <summary>
    /// Measured against what the generator can actually produce, not against a number chosen by eye.
    ///
    /// <para>The comparison that matters is <b>the depth the building ADMITS to</b> — what is on the lift
    /// directory and the plan in the lobby, which is the only depth a captain can read. No branch office
    /// anywhere admits to as many floors as the head office, and the head office takes the performance
    /// guard's entire allowance.</para>
    ///
    /// <para>Deliberately NOT <c>TrueDepthOf</c>: a rare branch office with a band nobody listed bottoms out
    /// at the same hard floor, and that is fine — its bottom four are the ones it is hiding. The rank shows
    /// in what the panel says, and the next test is about the gap between the two.</para>
    /// </summary>
    [Fact]
    public void TheHeadOfficeAdmitsToMoreFloorsThanAnythingASeedCanDig()
    {
        int hq = UndergroundComplex.DepthOf(Hq);
        Assert.Equal(UndergroundComplex.DeepestPossibleFloor, hq);
        Assert.Equal(hq, UndergroundComplex.TrueDepthOf(Hq));

        int deepestListed = 0;
        string worst = "";
        for (int i = 0; i < 3000; i++)
        {
            int listed = UndergroundComplex.DepthOf($"seeded-moon-{i}");
            if (listed < deepestListed)
            {
                deepestListed = listed;
                worst = $"seeded-moon-{i}";
            }
        }

        Assert.True(hq < deepestListed,
            $"the head office's directory ends at B{-hq} and {worst}'s ends at B{-deepestListed} — " +
            "the rank difference has to be readable on the lift panel");
    }

    /// <summary>
    /// THE HEAD OFFICE HAS NOTHING TO HIDE FROM ITSELF, and the absence is the rank.
    ///
    /// <para>A branch office lies by omission to its own staff: one site in four has a shaft not in the
    /// directory and a floor the panel does not list. Here the directory is complete — depth admitted equals
    /// depth walked, no gap in the floor list, no floor without a plate.</para>
    /// </summary>
    [Fact]
    public void TheDirectoryIsCompleteAndTheBranchOfficesAreNot()
    {
        Assert.False(UndergroundComplex.HasUnlistedBand(Hq));
        Assert.Equal(UndergroundComplex.DepthOf(Hq), UndergroundComplex.TrueDepthOf(Hq));

        int[] floors = [.. UndergroundComplex.FloorsOf(Hq)];
        Assert.Equal(-UndergroundComplex.DeepestPossibleFloor, floors.Length);
        for (int i = 0; i < floors.Length; i++)
        {
            Assert.Equal(-(i + 1), floors[i]);   // no gap where nothing was dug
            Assert.False(UndergroundComplex.IsUnlisted(Hq, floors[i]));
            Assert.DoesNotContain("NO PLATE", UndergroundComplex.NameOf(Hq, floors[i]), StringComparison.Ordinal);
        }

        // …and the other side of it: the world still contains buildings that DO hide a band, or the
        // comparison above is against nothing.
        Assert.Contains(BranchOffices.Concat(Enumerable.Range(0, 200).Select(i => $"seeded-moon-{i}")),
            UndergroundComplex.HasUnlistedBand);
    }

    // ── The plates: twenty-four, none repeated ───────────────────────────────────────────────────────────

    /// <summary>A branch reuses its plate stock — B1 and B9 are both ADMINISTRATION and are meant to feel
    /// alike. The head office had one made per floor. That contrast IS the tell, so both halves are pinned.</summary>
    [Fact]
    public void EveryHeadOfficeFloorHasItsOwnPlateAndABranchOfficeRepeats()
    {
        var hqNames = new List<string>();
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            hqNames.Add(UndergroundComplex.DepartmentOf(Hq, level));
        }

        Assert.Equal(hqNames.Count, hqNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(-UndergroundComplex.DeepestPossibleFloor, UndergroundComplex.HeadOfficeDepartments.Length);

        // The branch office cycles, and is meant to.
        Assert.Equal(UndergroundComplex.DepartmentOf("miranda", -1), UndergroundComplex.DepartmentOf("miranda", -9));
        Assert.NotEqual(UndergroundComplex.DepartmentOf(Hq, -1), UndergroundComplex.DepartmentOf(Hq, -9));
    }

    /// <summary>The plate is on the floor's name, so a captain reads it at the lift with the depth.</summary>
    [Fact]
    public void TheFloorNameCarriesTheHeadOfficePlate()
    {
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            string name = UndergroundComplex.NameOf(Hq, level);
            Assert.StartsWith($"B{-level}", name, StringComparison.Ordinal);
            Assert.Contains(UndergroundComplex.DepartmentOf(Hq, level), name, StringComparison.Ordinal);
            Assert.DoesNotContain(name, UndergroundComplex.Departments);   // never a branch office's word
        }
    }

    // ── The car answers ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// At a branch office an authority card opens exactly one shaft band, and the way down is a piece of
    /// paper somebody left in a room. The head office asks for nothing, on any floor — a hull that is on the
    /// board is expected. Same panel, same button; only one of them negotiates.
    /// </summary>
    [Fact]
    public void TheHeadOfficeLiftNeverAsksForACardAndABranchOfficeAlwaysDoes()
    {
        string[] noCards = [];

        int gatedHere = 0;
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(Hq, level, noCards))
            {
                Assert.Null(stop.Refusal);
                if (stop.Name.Contains("SEALED", StringComparison.Ordinal))
                {
                    gatedHere++;
                }
            }
        }

        Assert.Equal(0, gatedHere);

        // The other end, or the claim is about nothing: somewhere out there a car refuses a captain with an
        // empty pocket, and it is the ordinary case.
        int gatedElsewhere = 0;
        foreach (string body in BranchOffices)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                gatedElsewhere += UndergroundComplex.LiftPanel(body, level, noCards).Count(s => s.Refusal is not null);
            }
        }

        Assert.True(gatedElsewhere > 0,
            "no branch office in the system gates a shaft — then 'the head office asks for nothing' says nothing");
    }

    /// <summary>And the way OUT is on every panel, on all twenty-four floors. #600's bug (the lift only went
    /// down) survived three PRs because an audit proves you can REACH a lift, never that it is a way home —
    /// and this is the deepest building in the game to be wrong about.</summary>
    [Fact]
    public void EveryFloorOfTheHeadOfficeCanGoStraightBackToTheSurface()
    {
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            IReadOnlyList<UndergroundComplex.LiftStop> panel = UndergroundComplex.LiftPanel(Hq, level, []);
            UndergroundComplex.LiftStop surface = panel.Single(s => s.Level == 0);
            Assert.Null(surface.Refusal);
            Assert.Equal("SURFACE", surface.Name);
        }
    }

    // ── The mouths, an order of magnitude ────────────────────────────────────────────────────────────────

    /// <summary>The same plate, the same typeface, one order of magnitude, and a word that says the thing
    /// beyond it is not a sector of this building but a whole part of it.</summary>
    [Fact]
    public void TheSealedMouthsAreWingsAndTheyAreTenTimesFurther()
    {
        for (int i = 0; i < 5; i++)
        {
            double km = 0.8 + (i * 0.7);
            string hq = UndergroundComplex.SealedMouthSign(Hq, i, km);
            string branch = UndergroundComplex.SealedMouthSign("miranda", i, km);

            Assert.Contains("WING", hq, StringComparison.Ordinal);
            Assert.Contains("SECTOR", branch, StringComparison.Ordinal);
            Assert.Contains($"{km * 10.0:F1} km", hq, StringComparison.Ordinal);
            Assert.Contains($"{km:F1} km", branch, StringComparison.Ordinal);
            Assert.NotEqual(hq, branch);
        }
    }

    // ── The paint is kept ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every floor is painted, all the way down. At a branch office the band nobody listed is bare
    /// concrete and that absence is its tell; here there is no such band, and the maintained livery is this
    /// building's.</summary>
    [Fact]
    public void EveryHeadOfficeFloorIsPainted()
    {
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            Assert.NotNull(UndergroundComplex.LiveryFor(Hq, level));
        }

        // The language has TWO words, and both have to be readable:
        //
        //   · the HUE says which wing (the ratio between the channels is a fact about the band), and
        //   · the VALUE says how far down the wing (each floor darker than the one above it).
        //
        // A flat colour per wing would say the first and not the second, and hands four identical floors to
        // a captain riding between them — which is the owner's original complaint about this whole feature
        // ("now they look too same"), and which `ConsecutiveFloorsNeverLookAlike` caught when the first cut
        // of this livery did exactly that.
        var topOfBand = new Dictionary<int, BodyPalette.Ink>();
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            int band = UndergroundComplex.BandOf(level);
            BodyPalette.Ink ink = UndergroundComplex.LiveryFor(Hq, level)!.Value;

            if (!topOfBand.TryGetValue(band, out BodyPalette.Ink top))
            {
                topOfBand[band] = ink;
                continue;
            }

            // Same hue as the top of its wing: every channel is scaled by ONE factor, so the ratios between
            // them survive. (Compared this way rather than against the constant itself, because a guard that
            // restates the implementation's numbers is a second source of truth for them.)
            double kr = ink.R / (double)top.R, kg = ink.G / (double)top.G, kb = ink.B / (double)top.B;
            Assert.True(Math.Abs(kr - kg) < 0.02 && Math.Abs(kg - kb) < 0.02,
                $"B{-level} is not the same hue as the top of its wing (k = {kr:F3}/{kg:F3}/{kb:F3})");

            // …and darker than it, or "how far down the wing" is not being said at all.
            Assert.True(kr < 1.0, $"B{-level} is not stepped down from the top of its wing");
        }

        Assert.True(topOfBand.Count >= 6, $"only {topOfBand.Count} wings — the head office is meant to be six deep");
        Assert.Equal(topOfBand.Count, topOfBand.Values.Distinct().Count());

        // And no two floors anywhere in the building are painted identically.
        var inks = UndergroundComplex.FloorsOf(Hq).Select(l => UndergroundComplex.LiveryFor(Hq, l)!.Value).ToList();
        Assert.Equal(inks.Count, inks.Distinct().Count());
    }

    // ── And none of it explains anything ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The deepest floor of the biggest building in the game may be full of evidence of an enormous,
    /// well-funded, decades-long operation and may never once name what the operation produced. Standing
    /// canon (owner 2026-07-30), and the same forbidden vocabulary the arc's front door and route are held
    /// to.
    /// </summary>
    [Fact]
    public void NothingPaintedInTheHeadOfficeExplainsAnything()
    {
        var prose = new List<string>
        {
            UndergroundComplex.TitleOf(UndergroundComplex.Kind.HeadOffice),
        };
        prose.AddRange(UndergroundComplex.HeadOfficeDepartments);
        prose.AddRange(UndergroundComplex.SignsFor(UndergroundComplex.Kind.HeadOffice));
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            prose.Add(UndergroundComplex.NameOf(Hq, level));
            prose.Add(UndergroundComplex.SignFor(Hq, level, $"tag{level}"));
        }

        string[] forbidden =
        [
            "Vantar", "KAAMOS", "forty", "lucid", "wintering crew", "one voice", "continuous",
            "Old One", "Reever", "restore", "backup", "copy",
        ];

        foreach (string line in prose)
        {
            foreach (string word in forbidden)
            {
                Assert.False(line.Contains(word, StringComparison.OrdinalIgnoreCase),
                    $"the head office said \"{word}\" out loud: {line}");
            }
        }
    }

    /// <summary>The establishing card too — it is the loudest thing this building says, and the one place a
    /// captain who has crawled a Hive is meant to feel the rank in one breath. It is built out of the same
    /// four things the Hive's is (a shaft, a directory, a lobby, a floor) with every one answered
    /// differently, and it still explains nothing.</summary>
    [Fact]
    public void TheFirstDescentCardIsItsOwnAndSaysOnlyWhatIsThere()
    {
        (string label, string art, string card) = UndergroundComplex.FirstDescentCard(Hq);
        (string bLabel, string bArt, string bCard) = UndergroundComplex.FirstDescentCard("miranda");

        Assert.NotEqual(bLabel, label);
        Assert.NotEqual(bArt, art);
        Assert.NotEqual(bCard, card);
        Assert.Equal(UndergroundComplex.DescentCard, bCard);

        // The four answers, each of which is a fact a captain can see and none of which is an explanation.
        Assert.Contains("TWENTY-FOUR", card, StringComparison.Ordinal);
        Assert.Contains("no dust", card, StringComparison.OrdinalIgnoreCase);

        foreach (string word in new[] { "Vantar", "KAAMOS", "forty", "wintering", "waiting" })
        {
            Assert.False(card.Contains(word, StringComparison.OrdinalIgnoreCase),
                $"the establishing card said \"{word}\": {card}");
        }
    }

    /// <summary>The door vocabulary is the head office's own — nothing furtive, nothing clandestine. It
    /// sounds like an administration, which after four floors of it under a kilometre of ice with nobody in
    /// the corridors is far worse than a clinic's would be.</summary>
    [Fact]
    public void TheDoorsReadLikeAnAdministrationAndNotLikeAHideout()
    {
        string[] hq = UndergroundComplex.SignsFor(UndergroundComplex.Kind.HeadOffice);
        Assert.NotEmpty(hq);

        foreach (string[] branch in new[]
                 {
                     UndergroundComplex.SignsFor(UndergroundComplex.Kind.Laboratory),
                     UndergroundComplex.SignsFor(UndergroundComplex.Kind.BlackClinic),
                     UndergroundComplex.SignsFor(UndergroundComplex.Kind.ProcessingDepot),
                     UndergroundComplex.SignsFor(UndergroundComplex.Kind.RecordsAnnex),
                     UndergroundComplex.SignsFor(UndergroundComplex.Kind.TransitStation),
                 })
        {
            Assert.Empty(hq.Intersect(branch, StringComparer.Ordinal));
        }

        // A door that the generator can put on a head-office floor is drawn from that stock and no other.
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            Assert.Contains(UndergroundComplex.SignFor(Hq, level, "door"), hq);
        }
    }
}
