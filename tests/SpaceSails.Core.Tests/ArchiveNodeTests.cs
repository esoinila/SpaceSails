namespace SpaceSails.Core.Tests;

/// <summary>
/// THE ARCHIVE NODE — the rules that make standing near it a decision rather than a cutscene.
/// See <c>docs/features/the-archive-node.md</c>.
/// </summary>
public class ArchiveNodeTests
{
    private static ArchiveNode.Confrontation Fresh => new(
        PipsNow: 10, Suited: true, PriorConfrontations: 0, NebulaShardsHeld: 0, HasEverDied: false);

    // ── Where one is ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheVentedHullAlwaysCarriesOne_BecauseItIsWhySheDied()
    {
        for (int i = 0; i < 40; i++)
        {
            Assert.True(ArchiveNode.IsAboard($"wreck-{i}", Derelict.WreckCause.VentedByOneOfTheirOwn));
        }
    }

    [Fact]
    public void TheInfestedHullNeverCarriesOne()
    {
        // Two horrors in one room is one horror less. That hull already has a full deck of tension.
        for (int i = 0; i < 40; i++)
        {
            Assert.False(ArchiveNode.IsAboard($"wreck-{i}", Derelict.WreckCause.Infested));
        }
    }

    [Fact]
    public void OnEligibleCauses_ItIsRare_ButItDoesHappen()
    {
        // Rarity is the point: a node the player expects on every salvage is furniture.
        var ids = Enumerable.Range(0, 300).Select(i => $"wreck-{i}").ToList();
        int found = ids.Count(id => ArchiveNode.IsAboard(id, Derelict.WreckCause.Mutiny));

        Assert.True(found > 0, "no eligible wreck ever carries one — the feature is unreachable");
        Assert.True(found < ids.Count / 2, $"{found} of {ids.Count} carry one — that is furniture, not a find");
    }

    [Fact]
    public void WhetherOneIsAboardIsStable_SoARumourAboutAHullIsWorthSomething()
    {
        foreach (string id in new[] { "wreck-7", "wreck-51", "wreck-200" })
        {
            bool first = ArchiveNode.IsAboard(id, Derelict.WreckCause.InsuranceJob);
            Assert.Equal(first, ArchiveNode.IsAboard(id, Derelict.WreckCause.InsuranceJob));
        }
    }

    // ── Layer 1: the dwell ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFieldCoversTheRoom_ButArmsLengthIsSomewhereYouHaveToGo()
    {
        Assert.True(ArchiveNode.InField(0, 8));
        Assert.False(ArchiveNode.InField(0, 10));
        Assert.True(ArchiveNode.AtArmsLength(1, 1));
        Assert.False(ArchiveNode.AtArmsLength(0, 5));

        // Confronting is strictly a subset of dwelling: you cannot be at the handle and outside the field.
        Assert.True(ArchiveNode.ConfrontRadius < ArchiveNode.FieldRadius);
    }

    [Fact]
    public void TheDwellIsTheSlowestBeatInTheGame_SoCrossingTheRoomIsFree()
    {
        // If it bit as fast as being cornered, the compartment would just be a wall and there would be no
        // decision in it. The player must be able to walk through and pay effectively nothing.
        Assert.True(NervePips.ArchiveBeatSeconds > NervePips.CorneredBeatSeconds);
        Assert.True(NervePips.ArchiveBeatSeconds > NervePips.CloseBeatSeconds);
        Assert.True(NervePips.IsSustained(NervePips.Cause.Archive));
        Assert.Equal(NervePips.BeatPips, NervePips.Cost(NervePips.Cause.Archive));
    }

    [Fact]
    public void TheDwellHasItsOwnClock_SoItNeverStealsAnotherPressuresBeat()
    {
        var beats = NervePips.Beats.Fresh.With(NervePips.Cause.Archive, 4.0);

        Assert.Equal(4.0, beats.For(NervePips.Cause.Archive));
        Assert.Equal(0.0, beats.For(NervePips.Cause.Close));
        Assert.Equal(0.0, beats.For(NervePips.Cause.Cornered));
    }

    [Fact]
    public void TheLedgerSaysWhyThePipWent()
    {
        // #480's whole deliverable: the captain is told what took it.
        Assert.Contains("stood too long", NervePips.Name(NervePips.Cause.Archive));
    }

    // ── Layer 2: the confrontation ────────────────────────────────────────────────────────────────────

    [Fact]
    public void KnowingMoreMakesItWorse_WhichIsThePoint()
    {
        // Understanding is the injury: a captain holding arc 2's shards can read what they are looking at.
        var ignorant = Fresh with { NebulaShardsHeld = 0 };
        var learned = Fresh with { NebulaShardsHeld = 4 };

        Assert.True(Total(learned) < Total(ignorant));
    }

    [Fact]
    public void AFrayedCaptainIsLookedAt_AndASteadyOneLooks()
    {
        Assert.True(Total(Fresh with { PipsNow = 2 }) < Total(Fresh with { PipsNow = 9 }));
    }

    [Fact]
    public void EveryLookMakesTheNextOneWorse()
    {
        Assert.True(Total(Fresh with { PriorConfrontations = 3 }) < Total(Fresh with { PriorConfrontations = 0 }));
    }

    [Fact]
    public void ACaptainItHasNothingOnIsSafer()
    {
        Assert.True(Total(Fresh with { HasEverDied = false }) > Total(Fresh with { HasEverDied = true }));
    }

    [Fact]
    public void EveryModifierIsNamed_SoABadThrowIsReadableRatherThanUnfair()
    {
        IReadOnlyList<DiceModifier> mods = ArchiveNode.Modifiers(
            new(PipsNow: 2, Suited: true, PriorConfrontations: 2, NebulaShardsHeld: 3, HasEverDied: true));

        Assert.NotEmpty(mods);
        Assert.All(mods, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));
    }

    [Fact]
    public void TheSameCaptainOnTheSameHullGetsTheSameAnswer()
    {
        // No reload-scumming a vision out of it.
        var a = ArchiveNode.Confront("wreck-9", Fresh);
        var b = ArchiveNode.Confront("wreck-9", Fresh);

        Assert.Equal(a.Roll.Face, b.Roll.Face);
        Assert.Equal(a.Band, b.Band);
        Assert.Equal(a.Vision.Id, b.Vision.Id);
    }

    [Fact]
    public void TheBandsCoverTheWholeRange_WithNoGapAndNoOverlap()
    {
        Assert.True(ArchiveNode.LookedAwayAt > ArchiveNode.SawAt);
        Assert.True(ArchiveNode.SawAt > ArchiveNode.NoticedAt);

        // Every possible total lands in exactly one band, including a badly modified one.
        for (int total = -20; total <= 40; total++)
        {
            int hits = 0;
            hits += total >= ArchiveNode.LookedAwayAt ? 1 : 0;
            hits += total is var t && t < ArchiveNode.LookedAwayAt && t >= ArchiveNode.SawAt ? 1 : 0;
            hits += total < ArchiveNode.SawAt && total >= ArchiveNode.NoticedAt ? 1 : 0;
            hits += total < ArchiveNode.NoticedAt ? 1 : 0;
            Assert.Equal(1, hits);
        }
    }

    [Fact]
    public void TheCommonCaseIsTheBargain_NotTheCatastrophe()
    {
        // A steady, suited, unfamiliar captain should mostly be trading pips for a vision — the middle band
        // is the trade the whole feature exists for. If the catastrophe were common nobody would look twice.
        int inside = 0;
        for (int i = 0; i < 200; i++)
        {
            if (ArchiveNode.Confront($"hull-{i}", Fresh).Band == ArchiveNode.Band.Inside)
            {
                inside++;
            }
        }

        Assert.True(inside < 40, $"{inside}/200 confrontations end inside it — that is a tax, not a gamble");
    }

    [Fact]
    public void SeeingOneCostsWhatTheMonolithCosts_AndBeingInsideOneCostsMore()
    {
        Assert.Equal(NervePips.MonolithPips, ArchiveNode.PipCost(ArchiveNode.Band.Saw));
        Assert.True(ArchiveNode.PipCost(ArchiveNode.Band.Inside) > ArchiveNode.PipCost(ArchiveNode.Band.Saw));
        Assert.True(ArchiveNode.PipCost(ArchiveNode.Band.LookedAway) < ArchiveNode.PipCost(ArchiveNode.Band.Saw));
    }

    [Fact]
    public void EveryBandSaysSomething()
    {
        foreach (ArchiveNode.Band b in System.Enum.GetValues<ArchiveNode.Band>())
        {
            Assert.False(string.IsNullOrWhiteSpace(ArchiveNode.BandLine(b)));
        }
    }

    // ── The visions ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThePoolIsAuthoredAndComplete()
    {
        Assert.Equal(5, ArchiveNode.Visions.Length);
        Assert.All(ArchiveNode.Visions, v =>
        {
            Assert.False(string.IsNullOrWhiteSpace(v.Id));
            Assert.False(string.IsNullOrWhiteSpace(v.Title));
            Assert.False(string.IsNullOrWhiteSpace(v.Lore));
            Assert.StartsWith("art/", v.ArtFile);
        });
        Assert.Equal(ArchiveNode.Visions.Length, ArchiveNode.Visions.Select(v => v.Id).Distinct().Count());
    }

    [Fact]
    public void TheRackWithYourNumberIsNeverShownToACaptainItCannotMeanAnythingTo()
    {
        // It is not lore about a stranger — it is the player's own save looking back. A captain the
        // insurance has never paid out on has no number on a jar, and showing them one is just a picture.
        var neverDied = Fresh with { HasEverDied = false };

        foreach (ArchiveNode.Band band in System.Enum.GetValues<ArchiveNode.Band>())
        {
            for (int prior = 0; prior < 8; prior++)
            {
                ArchiveNode.Vision v = ArchiveNode.VisionFor(band, neverDied with { PriorConfrontations = prior });
                Assert.NotEqual("archive-your-rack", v.Id);
            }
        }
    }

    [Fact]
    public void ACaptainWhoHasDied_AndGoesAllTheWayIn_FindsTheirOwnNumber()
    {
        var died = Fresh with { HasEverDied = true };
        Assert.Equal("archive-your-rack", ArchiveNode.VisionFor(ArchiveNode.Band.Inside, died).Id);
    }

    [Fact]
    public void TheHandlersAreNeverExplained()
    {
        // The spiders are a QUESTION planted in a mechanic — the seed for a third arc. The moment this
        // text answers what they are, the feature has spent something it cannot get back.
        string note = ArchiveNode.HandlerNote;

        Assert.Contains("does not know", note);
        Assert.DoesNotContain("because", note);
    }

    [Fact]
    public void LookingAwayBuysNoFragment()
    {
        // The cheap band has to stay cheap in BOTH directions, or it is just a free vision.
        ArchiveNode.Vision v = ArchiveNode.VisionFor(ArchiveNode.Band.LookedAway, Fresh);
        Assert.NotEqual(ArchiveNode.Visions[0].Lore, v.Lore);
    }

    // ── Layer 3: the switch ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLabelSaysExactlyWhatItDoes()
    {
        // The label IS the confirmation dialog. That restraint is the entire joke and the entire mechanic:
        // if this ever grows an "are you sure?", the feature is gone.
        Assert.Contains("PURGE", ArchiveNode.SwitchLegend);
        Assert.Contains("NOT RECOVERABLE", ArchiveNode.SwitchLegend);
    }

    [Fact]
    public void TheHandleNeverTellsYouWhoseItWas()
    {
        // A purge the captain did not pay to read stays unknown forever. The record of it is the silence.
        string line = ArchiveNode.PurgeLine;

        Assert.DoesNotContain("stranger", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("your own", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("policy number", line, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReliefIsRealAndThatIsTheTrap()
    {
        Assert.True(ArchiveNode.PurgeRelief > 0);
    }

    [Fact]
    public void MostlyItIsAStranger_ButYourOwnIsRareRatherThanImpossible()
    {
        var counts = new Dictionary<ArchiveNode.Resident, int>();
        for (int i = 0; i < 400; i++)
        {
            ArchiveNode.Resident r = ArchiveNode.ResidentOf($"hull-{i}");
            counts[r] = counts.GetValueOrDefault(r) + 1;
        }

        Assert.True(counts.GetValueOrDefault(ArchiveNode.Resident.Stranger) > 200,
            "pulling an unread handle should usually cost you nothing you ever learn about");
        Assert.True(counts.GetValueOrDefault(ArchiveNode.Resident.YourOwn) > 0,
            "if it can never be yours, the label is not a gamble and the joke does not land");
        Assert.True(counts.GetValueOrDefault(ArchiveNode.Resident.YourOwn) < 60,
            "your own pattern should be a story, not a tax");
        Assert.True(counts.GetValueOrDefault(ArchiveNode.Resident.DelinquentSubscriber) > 0,
            "the reckless road needs a real payment or the handle is only ever a punishment");
    }

    [Fact]
    public void WhoIsInsideIsFixedBeforeTheCaptainEverComesAboard()
    {
        foreach (string id in new[] { "hull-3", "hull-77", "hull-401" })
        {
            Assert.Equal(ArchiveNode.ResidentOf(id), ArchiveNode.ResidentOf(id));
        }
    }

    [Fact]
    public void TheDeathAfterAPurgeSaysTheThingTheCardHasNeverSaid()
    {
        Assert.Contains("NO PATTERN ON FILE", ArchiveNode.NoRestoreLine);
        Assert.Contains("read the label", ArchiveNode.NoRestoreLine);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────────

    private static int Total(in ArchiveNode.Confrontation c) =>
        ArchiveNode.Modifiers(c).Sum(m => m.Value);
}
