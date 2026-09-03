using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L4 · THE WALLS ARE HUNG, AND WALKING UP TO ONE READS IT. Core decides what the three plates say,
/// which line each is worth and when a place finishes a page (<c>TheWallsRememberForYouTests</c> holds all of
/// that). What is left is the half only the client can get wrong, and it is the half this repo has paid for
/// repeatedly:
///
/// <list type="bullet">
///   <item>a fixture that is not actually on any deck — a rule that is true in the model and unreachable;</item>
///   <item>three fixtures close enough together that [E] grabs the wrong one, so one wall is silently three;</item>
///   <item>an arrival edge somebody forgot, so the lane happens on a berth and not on a landing.</item>
/// </list>
///
/// <para><b>Why some of these are source-shape guards.</b> This project has no component renderer, so a claim
/// about ROUTING — which method a press reaches, which edges call an arrival — is read off the shipping method
/// bodies. The claims about the DECK are made against the real built deck plan, because that is a value.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWallsAreHungAndReadTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(params string[] file) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", "Pages", .. file]));

    /// <summary>One method body, from its signature to the next member at the same indent — the same cut the
    /// sibling client guards make, so a body read here is a body read there.</summary>
    private static string Method(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"the client no longer has `{signature}` — this guard cannot find what it audits.");
        int end = source.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert.True(end > start, $"`{signature}` does not close where this guard expects.");
        return source[start..end];
    }

    // ── THE WALLS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY PORT HANGS ALL THREE, and it is not a list somebody remembered to write: the claim is made over
    /// <see cref="HavenInterior.InteriorBodyIds"/>, so a ninth haven added next month either hangs the three
    /// plates or turns this red. A great port is asserted by name on top of the sweep, because the brief's
    /// floor is the great-port tier and a sweep that quietly went to zero havens would still be green.
    /// </summary>
    [Fact]
    public void EveryPortHangsAllThreePlatesAndEachIsTheOneItSays()
    {
        Assert.Contains("red-eye", HavenInterior.InteriorBodyIds);   // the great-port tier, by name

        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            DeckPlan deck = HavenInterior.DockedDeck(body)
                ?? throw new InvalidOperationException($"{body} has no docked deck.");

            var hung = new Dictionary<int, DeckPlan.ConsoleSpot>();
            foreach (DeckPlan.ConsoleSpot spot in deck.Consoles)
            {
                if (StationAds.IndexOfLabel(spot.Label) is { } index)
                {
                    Assert.False(hung.ContainsKey(index),
                        $"{body} hangs plate {index} twice — one wall is not two.");
                    hung[index] = spot;
                    Assert.Equal(DeckPlan.ConsoleKind.ViewObject, spot.Kind);
                }
            }

            Assert.Equal(StationAds.Ads.Count, hung.Count);
            for (int i = 0; i < StationAds.Ads.Count; i++)
            {
                Assert.True(hung.ContainsKey(i), $"{body} does not hang plate {i}.");

                // The whole of the advertising is on the label the captain reads walking past…
                Assert.Contains(StationAds.Ads[i].Text, hung[i].Label, StringComparison.Ordinal);

                // …and there is nothing UNDER it: no canvas (a text plate in the poster's own idiom, like
                // the lifeboat muster) and no caption, because the card [E] raises is this same sentence
                // held closer and a caption would print it a second time on the same card.
                Assert.Null(hung[i].Caption);
                Assert.Null(hung[i].ImageUrl);
            }

            // …and the poster that has hung here since #380 is still here, and is NOT one of them.
            DeckPlan.ConsoleSpot poster = deck.Consoles.Single(
                c => c.Label.Contains("PIRATE INSURANCE", StringComparison.Ordinal)
                     && c.Y < HavenInterior.BarBand(body)!.Value.Tops[0].Y);
            Assert.Null(StationAds.IndexOfLabel(poster.Label));
        }
    }

    /// <summary>
    /// STANDING AT A PLATE READS THAT PLATE. The press resolves through <see cref="DeckPlan.
    /// NearestConsoleSpot"/>, so three walls hung within one interact radius of each other — or of the
    /// poster, the plaque, the lifeboat or a ring hatch — would be one wall the game read as three at random.
    /// Asserted where the captain actually stands: at each plate's own square, the nearest console IS it.
    ///
    /// <para><b>Proven RED</b> by moving plate 2 onto plate 1's square: the press resolves to plate 1 from
    /// both, and this reddens on every haven.</para>
    /// </summary>
    [Fact]
    public void StandingAtAPlateReadsThatPlateAndNotTheOneNextToIt()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            DeckPlan deck = HavenInterior.DockedDeck(body)!;

            foreach (DeckPlan.ConsoleSpot spot in deck.Consoles)
            {
                if (StationAds.IndexOfLabel(spot.Label) is not { } index)
                {
                    continue;
                }

                DeckPlan.ConsoleSpot? under = deck.NearestConsoleSpot(spot.X, spot.Y);
                Assert.NotNull(under);
                Assert.Equal(index, StationAds.IndexOfLabel(under!.Value.Label));

                // …and nothing else on this deck is inside the reach of it either, so the press cannot be a
                // coin toss the moment the captain stands a half-du off the middle of the plate.
                foreach (DeckPlan.ConsoleSpot other in deck.Consoles)
                {
                    if (other.Label == spot.Label)
                    {
                        continue;
                    }

                    double d = Math.Sqrt(((other.X - spot.X) * (other.X - spot.X))
                                       + ((other.Y - spot.Y) * (other.Y - spot.Y)));
                    Assert.True(d > DeckPlan.InteractRadius,
                        $"{body}: `{other.Label}` stands {d:F1} du from plate {index} — inside the [E] reach.");
                }
            }
        }
    }

    // ── THE PRESS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PRESS ROUTES A PLATE TO ITS LINE AND THE POSTER TO ITS THIRD READ — and the poster's own two reads
    /// are untouched, which is asserted by ORDER: the rebirth branch is inside the poster's block and AFTER
    /// the <c>fine-print</c> assembly it must not displace.
    /// </summary>
    [Fact]
    public void TheFixtureReaderRoutesEachPlateAndGivesThePosterItsThirdRead()
    {
        string body = Method(Pages("Map.Deck.Fixtures.cs"), "private void ViewNearbyObject()");

        int firstRead = body.IndexOf("NebulaLore.PosterFirstReadLine", StringComparison.Ordinal);
        int finePrint = body.IndexOf("TryAssembleNebula(\"fine-print\"", StringComparison.Ordinal);
        int rebirth = body.IndexOf("ThePosterAfterARebirth()", StringComparison.Ordinal);
        int plate = body.IndexOf("TheAdIsRead(adIndex)", StringComparison.Ordinal);

        Assert.True(firstRead >= 0 && finePrint > firstRead,
            "the poster's own two reads are no longer where this guard expects them.");
        Assert.True(rebirth > finePrint, "the rebirth read displaced one of the poster's own two reads.");
        Assert.True(plate > rebirth, "the plates are not routed after the poster's block.");

        // The plate is resolved through Core's own words, not through an index baked into a label.
        Assert.Contains("Core.StationAds.IndexOfLabel(adLabel)", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE POSTER'S THIRD READ ASKS CORE AND SPENDS THE ONE WRITER. Both halves matter: the decision is
    /// <see cref="StationAds.LookAfterARebirth"/> (so the rebirth gate and the once-a-life latch are stated
    /// once, in the file the test that proves them lives beside), and the FILING is
    /// <c>FileTheSigningSheet</c> — the same writer the rep spends, so a captain who got the afternoon off a
    /// wall and one who got it off Harlan Fess hold one page rather than two versions of it.
    /// </summary>
    [Fact]
    public void ThePosterAsksCoreAndFilesThroughTheOneWriter()
    {
        string body = Method(Pages("Map.Ads.cs"), "private void ThePosterAfterARebirth()");

        Assert.Contains("StationAds.LookAfterARebirth(", body, StringComparison.Ordinal);
        Assert.Contains("FileTheSigningSheet()", body, StringComparison.Ordinal);
        Assert.Contains("RaiseStoryBeat(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId)", body,
            StringComparison.Ordinal);
        Assert.Contains("ShowPulseMessage(StationAds.PosterAgainToast)", body, StringComparison.Ordinal);

        // …and it does NOT build the afternoon's words itself. A second assembler is a second thing to keep
        // in step with the first, which is exactly how one memory becomes two.
        Assert.DoesNotContain("NebulaRep.SigningMemory +", body, StringComparison.Ordinal);
    }

    /// <summary>Reading a plate writes ONE sheet under Core's id, with Core's text, and says Core's sentence —
    /// the whole afternoon's line when the third one lands and the arriving line before that.</summary>
    [Fact]
    public void ReadingAPlateGrowsTheOneSheetAndSaysWhatArrived()
    {
        string body = Method(Pages("Map.Ads.cs"), "private void TheAdIsRead(int index)");

        Assert.Contains("StationAds.LinesIn(had?.Text)", body, StringComparison.Ordinal);
        Assert.Contains("StationAds.TextFor(lines)", body, StringComparison.Ordinal);
        Assert.Contains("StationAds.IsWhole(text)", body, StringComparison.Ordinal);
        Assert.Contains("StationAds.TheFilingDay", body, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Mark.Mine", body, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Theory.Money", body, StringComparison.Ordinal);
        Assert.Contains("StationAds.WholeToast", body, StringComparison.Ordinal);
        Assert.Contains("RequestVaultSave()", body, StringComparison.Ordinal);
    }

    // ── THE ARRIVALS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ARRIVAL IN THE GAME TELLS THE BOOK. The brief names three — dock, orbit insertion, landing — and
    /// the game has five edges that are one of those: the clamp, the shuttle hop between havens, the
    /// autopilot's park, the manual insertion, and the boat setting down. A lane wired at four of them would
    /// happen at a berth and not on a moon, which is exactly the shape of bug this repo keeps paying for.
    ///
    /// <para>The count is asserted as well as the sites, so a sixth arrival edge added later cannot silently
    /// skip this without a red row.</para>
    /// </summary>
    [Fact]
    public void EveryArrivalEdgeInTheGameTellsTheBook()
    {
        (string File, string Signature)[] edges =
        [
            ("Map.Docking.cs", "TheArrivalIsRemembered(dest.Id)"),    // the shuttle hop between havens
            ("Map.Docking.cs", "TheArrivalIsRemembered(dock.Id)"),    // the clamp
            ("Map.Autopilot.cs", "TheArrivalIsRemembered(body.Id)"),  // the autopilot's park
            ("Map.Autopilot.cs", "TheArrivalIsRemembered(oi.Body.Id)"), // the manual insertion
            ("Map.Surface.cs", "TheArrivalIsRemembered(stop.Body.Id)"), // the boat setting down
        ];

        foreach ((string file, string call) in edges)
        {
            Assert.Contains(call, Pages(file), StringComparison.Ordinal);
        }

        // …and every arrival edge stands beside the `ArrivedAt` hook that already marks one, or (the landing)
        // at the one place a boat is mated. Counted, so a new edge cannot be added without noticing this.
        int wired = 0;
        foreach (string file in new[] { "Map.Docking.cs", "Map.Autopilot.cs", "Map.Surface.cs" })
        {
            string source = Pages(file);
            int at = 0;
            while ((at = source.IndexOf("TheArrivalIsRemembered(", at, StringComparison.Ordinal)) >= 0)
            {
                wired++;
                at += 1;
            }
        }

        Assert.Equal(edges.Length, wired);
    }

    /// <summary>
    /// THE PLACE SAYS BOTH SENTENCES AND WRITES THE PAGE BACK. Core picks which pages
    /// (<see cref="StationAds.PagesFinishedBy"/>) — including the two fences the client must not re-implement
    /// — and the client's only job is to un-grey them, say the two lines and raise ONE plate.
    /// </summary>
    [Fact]
    public void ThePlaceSaysBothSentencesAndUnGreysThroughCore()
    {
        string body = Method(Pages("Map.Ads.cs"), "private void ThePlaceFinishesThePages(string? bodyId)");

        Assert.Contains("StationAds.PagesFinishedBy(LedgerPagesForFiling(), _filingBook, BodyName(bodyId))",
            body, StringComparison.Ordinal);
        Assert.Contains("ShowPulseMessage(StationAds.BeenHereToast)", body, StringComparison.Ordinal);
        Assert.Contains("ShowPulseMessage(StationAds.PlaceFinishesToast)", body, StringComparison.Ordinal);
        Assert.Contains("FilingLine.PageState.CameBack", body, StringComparison.Ordinal);

        // ONE beat for however many pages the place finished — three plates over one gangway would be the
        // cadence law broken by arithmetic.
        Assert.Contains("RaiseStoryBeat(StoryBeats.Beat.Flashback, finished[0])", body, StringComparison.Ordinal);
        Assert.Equal(1, body.Split("RaiseStoryBeat").Length - 1);

        // …and no roll is thrown. The place finishes the page FOR FREE; that is the whole of what it is for.
        Assert.DoesNotContain("Flashback.Roll", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyNerveShock", body, StringComparison.Ordinal);
    }

    // ── THE OLD SHIP ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE IS BERTHED BEFORE THE SCOPE LOOKS AT ANYTHING, and both ways of meeting her reach ONE writer. A
    /// second filer would be a second latch to keep in step with the first, and the sheet is the latch.
    /// </summary>
    [Fact]
    public void SheIsBerthedOnTheSweepAndBothWaysOfMeetingHerReachOneWriter()
    {
        string npc = Pages("Map.Npc.cs");
        Assert.Contains("EnsureTheOldShipIsBerthed();", Method(npc, "private void SweepSensors()"),
            StringComparison.Ordinal);
        Assert.Contains("TheOldShipIsSeen(id);", Method(npc, "private void TrackShipFromMenu(string id)"),
            StringComparison.Ordinal);

        string ads = Pages("Map.Ads.cs");
        Assert.Contains("TheOldShipIsAlongside(bodyId)",
            Method(ads, "private void TheArrivalIsRemembered(string? bodyId)"), StringComparison.Ordinal);

        string seen = Method(ads, "private void TheOldShipIsSeen(string? shipId)");
        Assert.Contains("HeldMemory.Find(_heldMemories, TheOldShip.SheetId) is not null", seen,
            StringComparison.Ordinal);
        Assert.Contains("TheOldShip.SheetText", seen, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Theory.Love", seen, StringComparison.Ordinal);
        Assert.Contains("RaiseStoryBeat(StoryBeats.Beat.Flashback, TheOldShip.SheetId)", seen,
            StringComparison.Ordinal);

        // #973 L5b's Ilse FIND ends at her, so the predicate that says where she is has to exist and be
        // asked of the universe rather than of whatever berth the captain happens to be at.
        Assert.Contains("private string? TheReachBodyId(string? threadId)", ads, StringComparison.Ordinal);
    }

    // ── THE CUSTOMS DESK (#380 item 10) ──────────────────────────────────────────────────────────────
    //
    // The audit's last open mystifier: a counter, a gate, a signed authority and an officer standing at it
    // promised a CHECK, and the captain walked through carrying whatever he liked, for ever. The answer is
    // one card on the officer's own square, per tier — and these hold the three ways it could still be a
    // lie: hung at the wrong tiers, saying something other than what was authored, or standing close enough
    // to the lifeboat muster that [E] reads the wrong fixture.

    private static string ScenarioPath(string file)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : Path.Combine(dir.FullName, "scenarios", file);
    }

    /// <summary>The SHIPPING scenario, not a fixture: the whole claim of the tube module is that the tier is
    /// derived from traffic somebody already wrote down, and a fixture would let the desk and the economy
    /// drift apart.</summary>
    private static readonly Lazy<ICelestialEphemeris> Sol =
        new(() => CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(ScenarioPath("sol.json"))));

    private static DeckPlan.ConsoleSpot? CustomsAt(DeckPlan deck)
    {
        DeckPlan.ConsoleSpot[] found = [.. deck.Consoles.Where(
            c => string.Equals(c.Label, ArrivalTube.CustomsLabel, StringComparison.Ordinal))];
        Assert.True(found.Length <= 1, "one gate, one desk — this concourse hangs the customs card twice.");
        return found.Length == 1 ? found[0] : null;
    }

    /// <summary>
    /// EVERY PORT'S GATE SAYS WHAT IT IS FOR — AND AN OUTPOST'S DOES NOT, because an outpost has no queue and
    /// nobody to run one. Swept over <see cref="HavenInterior.InteriorBodyIds"/> against the shipping
    /// <c>sol.json</c>, so a ninth haven added next month either gets the card its traffic earns or turns this
    /// red; and the sweep is required to MEET all three tiers, so one that quietly collapsed to a single tier
    /// could not stay green.
    ///
    /// <para>The words are compared VERBATIM: they are the authored pair (#380, 2026-09-03) and the only new
    /// prose on this desk. The reserved word (<c>worldbuilding-notes.md</c> §8) is asserted absent from
    /// both.</para>
    ///
    /// <para><b>Proven RED</b> by returning the working-berth line at <see cref="ArrivalTube.Tier.Outpost"/>:
    /// The Tilt and The Deep grow a desk and this fails on both.</para>
    /// </summary>
    [Fact]
    public void TheGateSaysWhatItIsForAtTheTwoTiersThatFundOneAndNowhereElse()
    {
        const string greatPort =
            "Anyone who wanted a look at you had the length of that concourse to take it. "
            + "This desk stamps what they decided.";
        const string workingBerth =
            "A formality this berth does not fund. The stamp is real; the looking is not.";

        Assert.Equal(greatPort, ArrivalTube.CustomsLine(ArrivalTube.Tier.GreatPort));
        Assert.Equal(workingBerth, ArrivalTube.CustomsLine(ArrivalTube.Tier.WorkingBerth));
        Assert.Null(ArrivalTube.CustomsLine(ArrivalTube.Tier.Outpost));   // no officer, so no opinion

        foreach (string line in new[] { greatPort, workingBerth })
        {
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase); // §8: reserved
        }

        var met = new HashSet<ArrivalTube.Tier>();
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            ArrivalTube.Tier tier = ArrivalTube.TierFor(Sol.Value, body);
            met.Add(tier);
            DeckPlan deck = HavenInterior.DockedDeck(body, null, 0, false, null, null, tier)!;
            DeckPlan.ConsoleSpot? desk = CustomsAt(deck);

            if (tier == ArrivalTube.Tier.Outpost)
            {
                Assert.True(desk is null, $"{body} is an outpost and still keeps a customs desk.");
                continue;
            }

            Assert.True(desk is not null, $"{body} ({tier}) has a gate and no card at it.");
            Assert.Equal(DeckPlan.ConsoleKind.ViewObject, desk!.Value.Kind);
            Assert.Null(desk.Value.ImageUrl);      // a text plate, in the lifeboat muster's idiom
            Assert.Equal(tier == ArrivalTube.Tier.GreatPort ? greatPort : workingBerth, desk.Value.Caption);
        }

        // The shipping scenario actually exercises all three tiers — a sweep that only ever met one would
        // prove nothing about the other two.
        Assert.Equal(3, met.Count);
    }

    /// <summary>
    /// THE DESK AND THE TUBE PLATE ARE ONE READING OF ONE BERTH. The captain read
    /// <see cref="ArrivalTube.WalkLine"/> on the plate ninety seconds earlier; the officer's card has to agree
    /// with it, and it does because both come out of the same tier switch on the same
    /// <see cref="ArrivalTube.TierFor"/> answer. Asserted as a PAIR — a desk exactly where the plate said
    /// there was a walk to be watched — and as ROUTING, because the client could still hand the deck a tier
    /// it worked out some other way, at one weld and not the other.
    ///
    /// <para><b>Proven RED</b> by handing the second weld <c>ArrivalTube.Tier.GreatPort</c> instead of
    /// <c>TubeTierAt(id)</c>: the routing count drops to one and this fails.</para>
    /// </summary>
    [Fact]
    public void TheDeskAgreesWithTheTubePlateAboutTheSameBerth()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            ArrivalTube.Tier tier = ArrivalTube.TierFor(Sol.Value, body);
            DeckPlan deck = HavenInterior.DockedDeck(body, null, 0, false, null, null, tier)!;

            // A desk stands exactly where the plate said somebody could look at you on the way in.
            Assert.Equal(ArrivalTube.WalkLine(tier) != ArrivalTube.WalkLine(ArrivalTube.Tier.Outpost),
                CustomsAt(deck) is not null);

            // …and the card is this berth's tier's card, not the neighbour's.
            if (CustomsAt(deck) is { } desk)
            {
                Assert.Equal(ArrivalTube.CustomsLine(tier), desk.Caption);
                foreach (ArrivalTube.Tier other in Enum.GetValues<ArrivalTube.Tier>())
                {
                    if (other != tier)
                    {
                        Assert.NotEqual(ArrivalTube.CustomsLine(other), desk.Caption);
                    }
                }
            }
        }

        // ROUTING: the page hands the deck ITS OWN TierFor read — the same call Map.Docking makes for the
        // plate — at BOTH welds (the first dock, and every re-weld after a wing opens), so a captain who
        // cracks a hatch mid-visit does not lose the officer.
        string deckPage = Pages("Map.Deck.cs");
        Assert.Contains("ArrivalTube.TierFor(sky, havenId)", deckPage, StringComparison.Ordinal);
        Assert.Equal(2, deckPage.Split("TheBarsChurn, TubeTierAt(id))").Length - 1);
    }

    /// <summary>
    /// STANDING AT THE DESK READS THE DESK. It is hung on the officer's own square — a du behind the starboard
    /// counter, and this asserts the clearance from every other fixture on the deck rather than trusting the
    /// arithmetic in the comment, at the square the captain actually stands on.
    ///
    /// <para>It also holds the officer and his card TOGETHER: the droid and the console read one constant, so
    /// [E] cannot answer from the next square along and the man cannot be somebody else's.</para>
    ///
    /// <para><b>Proven RED</b> by hanging the console on the lifeboat's square: the nearest console at the
    /// desk resolves to the muster, and the officer stands eight du off his own card.</para>
    /// </summary>
    [Fact]
    public void StandingAtTheCustomsDeskReadsTheOfficerAndNothingElse()
    {
        int checked_ = 0;
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            ArrivalTube.Tier tier = ArrivalTube.TierFor(Sol.Value, body);
            DeckPlan deck = HavenInterior.DockedDeck(body, null, 0, false, null, null, tier)!;
            if (CustomsAt(deck) is not { } desk)
            {
                continue;
            }

            checked_++;
            DeckPlan.ConsoleSpot? under = deck.NearestConsoleSpot(desk.X, desk.Y);
            Assert.NotNull(under);
            Assert.Equal(ArrivalTube.CustomsLabel, under!.Value.Label);

            foreach (DeckPlan.ConsoleSpot other in deck.Consoles)
            {
                if (string.Equals(other.Label, desk.Label, StringComparison.Ordinal))
                {
                    continue;
                }

                double d = Math.Sqrt(((other.X - desk.X) * (other.X - desk.X))
                                   + ((other.Y - desk.Y) * (other.Y - desk.Y)));
                Assert.True(d > DeckPlan.InteractRadius,
                    $"{body}: `{other.Label}` stands {d:F1} du from the customs desk — inside the [E] reach.");
            }

            // The man is on the card's square: the figure the captain walks up to IS the one who speaks.
            var buffer = new DeckPlan.Droid[DeckPlan.MaxDroids];
            deck.FillDroids(0, buffer);
            DeckPlan.Droid officer = buffer.Take(deck.DroidCount).Single(
                dr => string.Equals(dr.Name, "Customs", StringComparison.Ordinal));
            Assert.Equal(desk.X, officer.X, 3);
            Assert.Equal(desk.Y, officer.Y, 3);
        }

        Assert.True(checked_ >= 5, $"only {checked_} ports were checked — the sweep found almost no desks.");
    }
}
