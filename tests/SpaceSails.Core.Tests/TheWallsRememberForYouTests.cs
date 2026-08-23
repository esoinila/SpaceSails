using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L4 · THE WALLS REMEMBER FOR YOU — the four laws of the ads-and-missions lane, and every one of them
/// is the sort that rots quietly:
///
/// <list type="bullet">
///   <item>THE AFTERNOON IS IN THE AFTERNOON'S ORDER. Three plates hung round a concourse, read in whatever
///   order a captain wanders past them, assemble ONE memory in the order the afternoon happened — and are
///   whole only at three. This is <i>a list built by appending is not a list in order</i> (the fourth named
///   bug class) pointed straight at a fiction, which is why it is swept over every permutation.</item>
///   <item>THE POSTER'S THIRD READ. Only after a rebirth, only once a life, and it files the signing sheet
///   only when that afternoon is not already in the book.</item>
///   <item>THE PLACE THAT FINISHES A PAGE. Only a grey page, only one that NAMES the place, and NEVER one
///   that came back wrong — the SPREAD is the only thing in the game that corrects a lie.</item>
///   <item>THE OLD SHIP. One berth per universe, deterministic, at a great port when the world has one.</item>
/// </list>
///
/// <para>…and the arc's own law swept over every new surface: the word for what the clinic does is never
/// printed, and nothing here names what was in the pods.</para>
/// </summary>
public sealed class TheWallsRememberForYouTests
{
    private const double Day = 86400.0;

    /// <summary>A world of berths with a real spread of tiers — built here rather than taken off a scenario,
    /// so a test of the impound berth is a test of the rule and not of whatever Saturn's tonnage is today.</summary>
    private static readonly OldCrew.Berth[] Berths =
    [
        new("ringside", ArrivalTube.Tier.GreatPort),
        new("red-eye", ArrivalTube.Tier.GreatPort),
        new("selene-gate", ArrivalTube.Tier.WorkingBerth),
        new("rusty-roadstead", ArrivalTube.Tier.WorkingBerth),
        new("cinder-roost", ArrivalTube.Tier.Outpost),
    ];

    private static IEnumerable<string> ManyThreads(int count) =>
        Enumerable.Range(0, count).Select(i => $"thread-{i}");

    // ── §1 · THE AFTERNOON ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE LINES LAND IN THE AFTERNOON'S ORDER, WHICHEVER WALL CAME FIRST. Swept over all six orders the
    /// captain can read three plates in: every one of them produces the same page, and it is the page with
    /// the clerk first, the small print second and the pen last.
    ///
    /// <para>The teeth: assembling in visit order would pass any single-order test and fail five of these
    /// six, and a rule that simply appended would produce six different memories of one afternoon.</para>
    /// </summary>
    [Fact]
    public void TheThreeLinesLandInTheAfternoonsOrderWhicheverPlateWasReadFirst()
    {
        int[][] everyOrder =
        [
            [0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0],
        ];

        string canonical = StationAds.TextFor([0, 1, 2]);
        foreach (int[] walk in everyOrder)
        {
            // …grown one plate at a time, exactly as a captain grows it, rather than handed the whole set.
            var read = new List<int>();
            string page = "";
            foreach (int plate in walk)
            {
                read.Add(plate);
                page = StationAds.TextFor(read);
            }

            Assert.Equal(canonical, page);
        }

        // And the order really is the afternoon's, stated against the authored lines themselves.
        int clerk = canonical.IndexOf(StationAds.Ads[0].Line, StringComparison.Ordinal);
        int print = canonical.IndexOf(StationAds.Ads[1].Line, StringComparison.Ordinal);
        int pen = canonical.IndexOf(StationAds.Ads[2].Line, StringComparison.Ordinal);
        Assert.True(clerk >= 0 && clerk < print && print < pen,
            $"the afternoon came out in the wrong order: clerk {clerk}, small print {print}, pen {pen}");
    }

    /// <summary>
    /// AND IT IS WHOLE ONLY AT THREE. Two plates in any combination is an unfinished afternoon; the third —
    /// whichever it is — finishes it. A completion rule that counted READS rather than DISTINCT plates would
    /// pass on a captain who stopped at the same wall three times, which is the failure this closes.
    /// </summary>
    [Fact]
    public void TheAfternoonIsWholeOnlyWhenAllThreePlatesHaveBeenRead()
    {
        Assert.False(StationAds.IsWhole(StationAds.TextFor([])));

        for (int a = 0; a < StationAds.Ads.Count; a++)
        {
            Assert.False(StationAds.IsWhole(StationAds.TextFor([a])),
                $"one plate ({a}) made a whole afternoon");

            // The same wall, over and over, is one wall.
            Assert.False(StationAds.IsWhole(StationAds.TextFor([a, a, a])),
                $"reading plate {a} three times made a whole afternoon");

            for (int b = 0; b < StationAds.Ads.Count; b++)
            {
                if (a == b)
                {
                    continue;
                }

                Assert.False(StationAds.IsWhole(StationAds.TextFor([a, b])),
                    $"two plates ({a},{b}) made a whole afternoon");
                Assert.True(StationAds.IsWhole(StationAds.TextFor([a, b, 3 - a - b])),
                    $"three plates ({a},{b},{3 - a - b}) did not make a whole afternoon");
            }
        }

        Assert.Equal(StationAds.Ads.Count, StationAds.LinesInTheAfternoon);
    }

    /// <summary>
    /// THE PAGE IS THE WHOLE OF THE BOOKKEEPING. Which plates a captain has read is read back OFF the sheet
    /// (<see cref="StationAds.LinesIn"/>) rather than out of a second store — so the round trip has to be
    /// exact in both directions, or a reloaded save would offer a wall the captain has already read.
    /// </summary>
    [Fact]
    public void WhichPlatesHaveBeenReadIsReadBackOffTheSheetItself()
    {
        Assert.Empty(StationAds.LinesIn(null));
        Assert.Empty(StationAds.LinesIn(""));

        for (int mask = 0; mask < 8; mask++)
        {
            var read = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    read.Add(i);
                }
            }

            Assert.Equal(read, StationAds.LinesIn(StationAds.TextFor(read)));
        }
    }

    /// <summary>Every plate is a DISTINCT wall with a DISTINCT line, and the label a fixture is hung under
    /// resolves back to the plate that owns it. A label that matched two ads, or none, would hang three walls
    /// that were secretly one.</summary>
    [Fact]
    public void EveryPlateIsItsOwnWallAndItsOwnLine()
    {
        Assert.Equal(3, StationAds.Ads.Count);
        Assert.Equal(3, StationAds.Ads.Select(a => a.Text).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, StationAds.Ads.Select(a => a.Line).Distinct(StringComparer.Ordinal).Count());

        for (int i = 0; i < StationAds.Ads.Count; i++)
        {
            Assert.Contains(StationAds.Ads[i].Text, StationAds.Ads[i].Label, StringComparison.Ordinal);
            Assert.Equal(i, StationAds.IndexOfLabel(StationAds.Ads[i].Label));
        }

        // …and nothing else on any deck is one of them.
        Assert.Null(StationAds.IndexOfLabel("📋 PIRATE INSURANCE"));
        Assert.Null(StationAds.IndexOfLabel("⛑ LIFEBOAT STATION"));
        Assert.Null(StationAds.IndexOfLabel(null));
        Assert.Null(StationAds.IndexOfLabel(""));
    }

    // ── §2 · THE POSTER'S THIRD READ ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// A FIRST-LIFE CAPTAIN GETS NOTHING NEW FROM THE WALL. The poster's own two reads are untouched by this
    /// lane; the third one exists only for a man who has been through the clinic.
    /// </summary>
    [Fact]
    public void ThePosterSaysNothingNewToACaptainWhoHasNeverDied()
    {
        foreach (bool filed in new[] { false, true })
        {
            Assert.Equal(
                StationAds.PosterLook.NothingToSay,
                StationAds.LookAfterARebirth(retiredCaptains: 0, lastLifeLookedAt: 0, life: 1, filed));
        }
    }

    /// <summary>
    /// ONCE PER LIFE, AND IT FILES THE AFTERNOON ONLY IF THE AFTERNOON IS NOT ALREADY THERE. Driven as a run
    /// of lives rather than asserted a case at a time, because the two gates interact: a captain who read the
    /// wall in life 2 gets the wall back in life 3, and the sheet he filed in life 2 changes what it says.
    /// </summary>
    [Fact]
    public void ThePosterFilesTheAfternoonOnceALifeAndOnlyWhenItIsNotAlreadyFiled()
    {
        int lastLife = 0;
        bool filed = false;

        for (int life = 2; life <= 5; life++)
        {
            StationAds.PosterLook first = StationAds.LookAfterARebirth(life - 1, lastLife, life, filed);

            // The first stop of a life always says something…
            Assert.NotEqual(StationAds.PosterLook.NothingToSay, first);
            Assert.Equal(
                life == 2 ? StationAds.PosterLook.FilesTheAfternoon : StationAds.PosterLook.SaysTheSentence,
                first);

            if (first == StationAds.PosterLook.FilesTheAfternoon)
            {
                filed = true;
            }

            lastLife = life;

            // …and every stop after it, this life, says nothing at all.
            for (int again = 0; again < 3; again++)
            {
                Assert.Equal(
                    StationAds.PosterLook.NothingToSay,
                    StationAds.LookAfterARebirth(life - 1, lastLife, life, filed));
            }
        }
    }

    /// <summary>The afternoon the poster files is the SAME afternoon the rep hands over — one text, one
    /// reborn line, and never a second version of a memory the captain already holds.</summary>
    [Fact]
    public void ThePosterAndTheRepFileTheSameAfternoon()
    {
        Assert.Equal(NebulaRep.SigningMemory, NebulaRep.SigningMemoryFor(0));
        Assert.EndsWith(NebulaRep.SigningMemoryReborn, NebulaRep.SigningMemoryFor(1), StringComparison.Ordinal);

        // Both doors write under one id, so `HeldMemory.Put` replaces rather than doubles.
        IReadOnlyList<HeldMemory.Sheet> book = HeldMemory.Put([], new HeldMemory.Sheet(
            NebulaRep.SigningMemoryId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money,
            NebulaRep.SigningMemoryFor(0), [], Day));
        book = HeldMemory.Put(book, new HeldMemory.Sheet(
            NebulaRep.SigningMemoryId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money,
            NebulaRep.SigningMemoryFor(1), [], Day));

        Assert.Single(book);
        Assert.Equal(NebulaRep.SigningMemoryFor(1), book[0].Text);
    }

    // ── §3 · THE PLACE THAT FINISHES A PAGE ──────────────────────────────────────────────────────────

    private static LedgerPage Page(string id, string title, string line, string provenance) =>
        new(id, 3 * Day, title, [line], provenance);

    private static IReadOnlyList<FilingLine.Page> BookWith(params FilingLine.Page[] pages) => pages;

    /// <summary>
    /// A PLACE FINISHES ONLY THE PAGES THAT NAME IT, AND ONLY THE GREY ONES. Four pages, one arrival: the
    /// grey page that names the place is finished, the grey page that names somewhere else is not, the page
    /// that is already the captain's is not, and a place with no name at all finishes nothing.
    /// </summary>
    [Fact]
    public void OnlyAGreyPageThatNamesThePlaceIsFinishedByStandingOnIt()
    {
        LedgerPage names = Page("p1", "Fuel bought", "40 units", "the dockmaster · Ringside Exchange · day 3");
        LedgerPage elsewhere = Page("p2", "Fuel bought", "40 units", "the dockmaster · Selene Gate · day 3");
        LedgerPage alreadyHis = Page("p3", "A drink at Ringside Exchange", "with the fence", "standing note");
        LedgerPage inTheTitle = Page("p4", "Ringside Exchange — a berth taken", "clamped on", "logged 2h ago");
        LedgerPage[] ledger = [names, elsewhere, alreadyHis, inTheTitle];

        IReadOnlyList<FilingLine.Page> book = BookWith(
            new FilingLine.Page("p1", FilingLine.PageState.Unremembered, FilingLine.Detail.None, "", ""),
            new FilingLine.Page("p2", FilingLine.PageState.Unremembered, FilingLine.Detail.None, "", ""),
            new FilingLine.Page("p3", FilingLine.PageState.Remembered, FilingLine.Detail.None, "", ""),
            new FilingLine.Page("p4", FilingLine.PageState.Unremembered, FilingLine.Detail.None, "", ""));

        IReadOnlyList<string> finished = StationAds.PagesFinishedBy(ledger, book, "Ringside Exchange");
        Assert.Equal(["p1", "p4"], finished);

        // A blank name matches nothing — every string in the world contains the empty one, and a body whose
        // name failed to resolve would otherwise finish the entire book on one landing.
        Assert.Empty(StationAds.PagesFinishedBy(ledger, book, ""));
        Assert.Empty(StationAds.PagesFinishedBy(ledger, book, null));
        Assert.Empty(StationAds.PagesFinishedBy(ledger, book, "   "));
    }

    /// <summary>
    /// AND NEVER A PAGE THAT CAME BACK WRONG. A page re-greyed by a later rebirth while still carrying a
    /// moved detail names the place exactly as truly as any other page — and the place must not touch it. The
    /// SPREAD is the only thing in this game that corrects a lie, and a rule that let a corridor do it would
    /// quietly delete the only piece of detective work the black book exists for.
    /// </summary>
    [Fact]
    public void APageThatCameBackWrongIsNeverFinishedByThePlaceItNames()
    {
        LedgerPage page = Page("p1", "Fuel bought", "40 units", "the dockmaster · Ringside Exchange · day 3");

        // Grey, and carrying a lie an earlier captain already paid the pip for.
        IReadOnlyList<FilingLine.Page> lying = BookWith(new FilingLine.Page(
            "p1", FilingLine.PageState.Unremembered, FilingLine.Detail.Number, "40", "60"));
        Assert.Empty(StationAds.PagesFinishedBy([page], lying, "Ringside Exchange"));

        // …while the same page with nothing moved IS finished — so the guard is the alteration and not the
        // page, and this pair cannot both pass on a rule that simply refused everything.
        IReadOnlyList<FilingLine.Page> honest = BookWith(new FilingLine.Page(
            "p1", FilingLine.PageState.Unremembered, FilingLine.Detail.None, "", ""));
        Assert.Equal(["p1"], StationAds.PagesFinishedBy([page], honest, "Ringside Exchange"));
    }

    /// <summary>
    /// ONCE EACH. The page's own standing is the latch: write back what the arrival wrote back, and the
    /// second arrival at the same berth finishes nothing. A refusal is a grey page too, and a place finishes
    /// it — a captain who rolled and got nothing IS owed the one the ground gives him for free.
    /// </summary>
    [Fact]
    public void ThePlaceFinishesEachPageExactlyOnce()
    {
        LedgerPage page = Page("p1", "Fuel bought", "40 units", "the dockmaster · Ringside Exchange · day 3");
        IReadOnlyList<FilingLine.Page> book = BookWith(new FilingLine.Page(
            "p1", FilingLine.PageState.Refused, FilingLine.Detail.None, "", ""));

        IReadOnlyList<string> first = StationAds.PagesFinishedBy([page], book, "Ringside Exchange");
        Assert.Equal(["p1"], first);

        foreach (string id in first)
        {
            book = FilingLine.Put(book, FilingLine.Standing(book, id) with
            {
                State = FilingLine.PageState.CameBack,
            });
        }

        Assert.Empty(StationAds.PagesFinishedBy([page], book, "Ringside Exchange"));
        Assert.Empty(StationAds.PagesFinishedBy([page], book, "Ringside Exchange"));
    }

    // ── §4 · THE OLD SHIP ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE BERTH PER UNIVERSE, AND IT IS A GREAT PORT. She is impounded, and an impound lot is where the
    /// traffic and the paperwork are — the same rule that puts a Nebula claims desk at one. Swept over many
    /// universes so "always" is the claim, and asserted to actually MOVE between universes, because a rule
    /// that returned the same berth every time would also pass the determinism half.
    /// </summary>
    [Fact]
    public void TheReachIsBerthedAtAGreatPortAndAtTheSameOneEveryTime()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string thread in ManyThreads(60))
        {
            string? berth = TheOldShip.BerthFor(thread, Berths);
            Assert.NotNull(berth);
            Assert.Equal(ArrivalTube.Tier.GreatPort, Berths.Single(b => b.Id == berth).Tier);

            // …and the same universe ties her up in the same place every time it is asked.
            Assert.Equal(berth, TheOldShip.BerthFor(thread, Berths));
            seen.Add(berth!);
        }

        Assert.True(seen.Count > 1, $"every universe parked her at one berth ({string.Join(",", seen)})");
    }

    /// <summary>A world with no great port still floats her — at whatever berth it has — and a world with no
    /// berth at all does not. A lane that quietly did not happen in some scenarios would be worse than one
    /// that happens somewhere slightly wrong.</summary>
    [Fact]
    public void AWorldWithNoGreatPortStillFloatsHerAndAWorldWithNoBerthDoesNot()
    {
        OldCrew.Berth[] modest =
        [
            new("selene-gate", ArrivalTube.Tier.WorkingBerth),
            new("cinder-roost", ArrivalTube.Tier.Outpost),
        ];

        foreach (string thread in ManyThreads(20))
        {
            string? berth = TheOldShip.BerthFor(thread, modest);
            Assert.NotNull(berth);
            Assert.Contains(berth, modest.Select(b => b.Id));
            Assert.Equal(berth, TheOldShip.BerthFor(thread, modest));
        }

        Assert.Null(TheOldShip.BerthFor("thread-0", []));
    }

    /// <summary>
    /// HER RECORD IS AUTHORED, NOT ROLLED — and it is the ONE hull in the game that is. Every other id still
    /// falls through to the seeded Victoria-I story, so this override cannot have been a change to the pools.
    /// </summary>
    [Fact]
    public void OneHullInTheGameCarriesTheOldNameAndItIsHers()
    {
        ShipHistory hers = ShipHistories.For(TheOldShip.ShipId);
        Assert.Equal([TheOldShip.FormerNameEntry], hers.FormerNames);
        Assert.Contains(TheOldShip.FormerName, hers.FormerNamesLine, StringComparison.Ordinal);
        Assert.Contains(TheOldShip.Class, hers.FormerNamesLine, StringComparison.Ordinal);

        // A rename is a re-registration, so she can never be shallower in owners than in names.
        Assert.True(hers.OwnersDeep >= hers.FormerNames.Count);

        // …and no ordinary hull in a long sweep of ids ever carries the name.
        for (int i = 0; i < 2000; i++)
        {
            Assert.DoesNotContain(TheOldShip.FormerName, ShipHistories.For($"npc-{i}").FormerNamesLine,
                StringComparison.Ordinal);
        }

        Assert.True(TheOldShip.IsHer(TheOldShip.ShipId));
        Assert.False(TheOldShip.IsHer("npc-3"));
        Assert.False(TheOldShip.IsHer(null));
    }

    /// <summary>A world with one great port in it, enough to berth a hull at.</summary>
    private sealed class OnePort : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } =
        [
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("ringside", "Ringside Exchange", "sun", 0, 2e4, 1.5e11, 3.15e7, 0,
                BodyKind.Station, IsHaven: true),
        ];

        public Vector2d Position(string bodyId, double simTime) =>
            bodyId == "sun" ? default : new Vector2d(1.5e11, simTime);
    }

    /// <summary>
    /// SHE IS A HULL PARKED AT A BERTH AND NOTHING ELSE — the supply depot's own shape: her destination is
    /// the berth she is at, her plan is empty, her maneuver budget is zero, and she carries nothing anybody
    /// can prise out of her. That last is what keeps the fence, the boarding party and the trade desk out of
    /// this lane without one line of special-casing in any of them.
    /// </summary>
    [Fact]
    public void SheIsAHullTiedUpAtABerthCarryingNothing()
    {
        var world = new OnePort();
        NpcShip her = TheOldShip.Berthed(world, "ringside", "thread-7");

        Assert.Equal(TheOldShip.ShipId, her.Id);
        Assert.Equal(TheOldShip.Callsign, her.Callsign);
        Assert.Equal("ringside", her.DepotBodyId);
        Assert.Equal("ringside", her.OriginId);
        Assert.Equal("ringside", her.DestinationId);
        Assert.Empty(her.Plan.Nodes);
        Assert.Equal(0, her.ManeuverBudget);
        Assert.Equal(0, her.CargoUnits);
        Assert.False(her.IsPod);
        Assert.Equal(TheOldShip.Manifest, her.CargoClass);
        Assert.DoesNotContain(her.CargoClass, new[] { "He3", "Compute cores", "Machinery", "Ice", "Alloys" });

        // Same universe, same berthing — down to which side of the port she is tied up on.
        NpcShip again = TheOldShip.Berthed(world, "ringside", "thread-7");
        Assert.Equal(her.DepotPhase, again.DepotPhase);
        Assert.Equal(her.DepotOrbitRadius, again.DepotOrbitRadius);
        Assert.Equal(her.InitialState.Position, again.InitialState.Position);

        // …and a different universe does not put her on the same side of it.
        Assert.NotEqual(her.DepotPhase, TheOldShip.Berthed(world, "ringside", "thread-8").DepotPhase);
    }

    // ── §5 · THE ARC'S LAW ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WORD THE ARC NEVER SAYS, AND THE THING IN THE PODS THAT IS NEVER NAMED — swept over every surface
    /// this lane can put in front of a player: three ads, three memory lines, four toasts, her sheet, her
    /// former-name line and her whole service record.
    /// </summary>
    [Fact]
    public void NoGameTextInThisLaneNamesTheThing()
    {
        List<string> everySurface =
        [
            .. StationAds.Ads.Select(a => a.Text),
            .. StationAds.Ads.Select(a => a.Label),
            .. StationAds.Ads.Select(a => a.Line),
            StationAds.TextFor([0, 1, 2]),
            StationAds.WholeToast,
            StationAds.PosterAgainToast,
            StationAds.BeenHereToast,
            StationAds.PlaceFinishesToast,
            TheOldShip.SheetText,
            TheOldShip.FormerNameEntry,
            TheOldShip.Callsign,
            TheOldShip.Manifest,
            TheOldShip.History.LaidDown,
            TheOldShip.History.Condition,
            TheOldShip.History.FormerNamesLine,
            TheOldShip.History.Teaser,
        ];

        Assert.All(everySurface, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.All(everySurface, line =>
            Assert.DoesNotContain("copy", line, StringComparison.OrdinalIgnoreCase));

        foreach (string forbidden in new[] { "restore", "clone", "backup", "cadaver", "archive", "lattice" })
        {
            Assert.All(everySurface, line =>
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase));
        }

        // …and the pods stay sealed. The crew opened one; the game never says what was in it. (The words
        // swept for are the ones the BIBLE uses about that manifest, not every word for a person — "Somebody
        // behind you sighed" is a queue at a counter and the guard has to be able to tell the difference.)
        foreach (string forbidden in new[] { "reefer", "medical", "sleeper", "corpse", "manifest" })
        {
            Assert.All(everySurface, line =>
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase));
        }
    }
}
