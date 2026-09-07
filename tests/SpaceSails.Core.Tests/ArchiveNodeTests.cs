namespace SpaceSails.Core.Tests;

/// <summary>
/// THE ARCHIVE NODE — the rules that make standing near it a decision rather than a cutscene.
/// See <c>docs/features/the-archive-node.md</c>.
/// </summary>
[SlowGate] // #251 · 23 s over 51 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
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

    // ── Layer 1, WIRED: the dwell actually spends the pip ─────────────────────────────────────────────
    //
    // Everything above this line was true on the day Core landed and the feature was still unreachable:
    // Cause.Archive existed, its beat length existed, Beats.Archive existed — and NervePips.Advance never
    // once looked at any of them. A constant nothing consumes is a comment. These are the tests that pin
    // the seam being CONSUMED, and each one was proven RED by taking the archive branch back out of
    // Advance (see the PR body).

    /// <summary>One frame inside a derelict, standing in the field. A wreck's interior is "aboard" to every
    /// other rule in NervePips — OnRegolith is false in here — which is exactly the trap.</summary>
    private static NervePips.Frame Dwelling(double dt) => new(
        OnExcursion: true, OnRegolith: false, SeesMonolith: false,
        Stressors: default, FreshSightings: 0, Touched: false, DtSeconds: dt,
        HealthPipsLeft: int.MaxValue, InArchiveField: true);

    private static NervePips.Frame Aboard(double dt) => Dwelling(dt) with { InArchiveField = false };

    /// <summary>A captain who has already spent a few pips — and THIS IS LOAD-BEARING, not flavour.
    ///
    /// <para>Written at <see cref="NerveModel.Steady"/> (a full gauge) every one of these tests passed on
    /// the broken behaviour, because the give-back they exist to forbid clamps at the ceiling and emits no
    /// event when it does. The assertions were right; the WORLD could not tell pass from fail. Hand the
    /// guard a captain with room to be given a pip back, and it can.</para></summary>
    private static readonly double Frayed = NervePips.FromPips(6);

    [Fact]
    public void StandingInTheFieldSpendsAWholePip_AndNamesIt()
    {
        NervePips.Step step = NervePips.Advance(
            Frayed, monolithSeen: false, NervePips.Beats.Fresh,
            Dwelling(NervePips.ArchiveBeatSeconds));

        Assert.Equal(NervePips.PipsOf(Frayed) - 1, NervePips.PipsOf(step.Nerve));
        NervePips.Event fired = Assert.Single(step.Events);
        Assert.Equal(NervePips.Cause.Archive, fired.Cause);
        Assert.Contains("stood too long", fired.Line);
    }

    [Fact]
    public void TheDwellTAKESWhileItIsRunning_ItDoesNotQuietlyCANCELAgainstTheShipsOwnSafety()
    {
        // THE BUG THIS GUARD EXISTS FOR, and it is bug class 3 in its purest form. A derelict's interior
        // scores as SAFE, so the airlock's give-back beat runs in there. Add an archive cost on top without
        // taking safety away and the two very nearly annihilate: the gauge sits still, and the ledger prints
        // "you have stood too long beside the thing in the hold" while nothing whatsoever is being spent.
        //
        // So the law is not "the archive costs pips" — it is "while the archive applies, the captain is not
        // safe". Nothing in the same frame may hand a pip back.
        NervePips.Step step = NervePips.Advance(
            Frayed, monolithSeen: false, NervePips.Beats.Fresh,
            Dwelling(NervePips.ArchiveBeatSeconds * 3));

        Assert.DoesNotContain(step.Events, e => e.Cause == NervePips.Cause.Airlock);
        Assert.All(step.Events, e => Assert.True(e.Delta < 0, $"{e.Line} gave pips back beside the node"));
        Assert.Equal(NervePips.PipsOf(Frayed) - 3, NervePips.PipsOf(step.Nerve));
    }

    [Fact]
    public void WalkingThroughCostsNothingWorthCounting()
    {
        // "The player sets their own dose." Crossing the compartment must be effectively free, or the room
        // is just a wall and there is no decision in it.
        NervePips.Step step = NervePips.Advance(
            Frayed, monolithSeen: false, NervePips.Beats.Fresh,
            Dwelling(NervePips.ArchiveBeatSeconds * 0.9));

        Assert.Equal(Frayed, step.Nerve);
        Assert.Empty(step.Events);
    }

    [Fact]
    public void PressureDoesNotBankBetweenVisits()
    {
        // Build most of a beat, walk out of the field, come back: the part-beat must not have been saved up
        // to fire a free pip. Same law the rest of the gauge already keeps.
        NervePips.Step near = NervePips.Advance(
            Frayed, monolithSeen: false, NervePips.Beats.Fresh,
            Dwelling(NervePips.ArchiveBeatSeconds * 0.95));
        Assert.True(near.Beats.For(NervePips.Cause.Archive) > 0);

        NervePips.Step away = NervePips.Advance(near.Nerve, false, near.Beats, Aboard(0.5));
        Assert.Equal(0.0, away.Beats.For(NervePips.Cause.Archive));
    }

    [Fact]
    public void TheClockIsCARRIED_SoALongFrameDoesNotEatTheBeat()
    {
        // The Archive carry was being dropped on the floor when the clock was rebuilt each frame — every
        // beat restarted from zero and the dwell could never complete at a realistic frame length.
        double dt = NervePips.ArchiveBeatSeconds / 4.0;
        double nerve = Frayed;
        var beats = NervePips.Beats.Fresh;
        int fired = 0;

        for (int i = 0; i < 4; i++)
        {
            NervePips.Step s = NervePips.Advance(nerve, false, beats, Dwelling(dt));
            nerve = s.Nerve;
            beats = s.Beats;
            fired += s.Events.Count(e => e.Cause == NervePips.Cause.Archive);
        }

        Assert.Equal(1, fired);
        Assert.Equal(NervePips.PipsOf(Frayed) - 1, NervePips.PipsOf(nerve));
    }

    [Fact]
    public void NothingHappensToACaptainWhoNeverWalksIntoIt()
    {
        // §9: "You can always walk out." A hull with a node aboard costs a captain who stays out of that
        // compartment exactly nothing.
        NervePips.Step step = NervePips.Advance(
            NerveModel.Steady, monolithSeen: false, NervePips.Beats.Fresh,
            Aboard(NervePips.ArchiveBeatSeconds * 5));

        Assert.DoesNotContain(step.Events, e => e.Cause == NervePips.Cause.Archive);
    }

    // ── Where it stands, on a deck a test can walk ────────────────────────────────────────────────────

    [Fact]
    public void TheNodeStandsInTheCompartmentItsOwnRuleNames()
    {
        // The nest spent the whole of #488 two compartments from its own name. Same failure mode, pinned
        // before it can happen: HoldCompartment is a string in one file and the station is a point in
        // another, and nothing but this makes them agree.
        Assert.Equal(ArchiveNode.HoldCompartment,
            WreckLayout.CompartmentAt(WreckLayout.ArchiveStation.X, WreckLayout.ArchiveStation.Y));
        Assert.Equal(ArchiveNode.HoldCompartment,
            WreckLayout.CompartmentAt(WreckLayout.ArchiveSwitchStation.X, WreckLayout.ArchiveSwitchStation.Y));
    }

    [Theory]
    [MemberData(nameof(EveryCause))]
    public void OnAHullCarryingOne_EveryStationIsStillReachableAndNoneShareADoorstep(Derelict.WreckCause cause)
    {
        const double avatarRadius = 0.7;          // DeckPlan.AvatarRadius
        const double comfortRadius = avatarRadius * 1.5;
        const double interactRadius = 3.0;        // DeckPlan.InteractRadius
        var spawn = new DeckReachability.Point(WreckLayout.SpawnX, WreckLayout.SpawnY);

        IReadOnlyList<(string Name, DeckReachability.Point At)> stations =
            WreckLayout.StationsWithArchive(cause);

        IReadOnlyList<string> tight = DeckReachability.Unreachable(
            spawn, stations, WreckLayout.Walls(cause), comfortRadius, WreckLayout.Bounds);
        Assert.True(tight.Count == 0,
            $"{cause}: cannot comfortably reach {string.Join(", ", tight)} on a hull with a node aboard.");

        for (int i = 0; i < stations.Count; i++)
        {
            for (int j = i + 1; j < stations.Count; j++)
            {
                double dx = stations[i].At.X - stations[j].At.X;
                double dy = stations[i].At.Y - stations[j].At.Y;
                double gap = System.Math.Sqrt((dx * dx) + (dy * dy));
                Assert.True(gap > interactRadius,
                    $"{cause}: '{stations[i].Name}' and '{stations[j].Name}' are {gap:0.0} du apart.");
            }
        }
    }

    [Fact]
    public void YouCanWalkToTheHANDLEWithoutEverComingToArmsLengthOfTheCOLUMN()
    {
        // THE LAW THE WHOLE JOKE RESTS ON (§5): "You may pull the handle without paying, and you will never
        // find out what you did." Arm's length forces a throw, and a throw costs nerve — so if the only route
        // to the handle passes through the confront radius, the game has quietly put a price on the one
        // decision that is supposed to be free, and the Ren & Stimpy beat is gone.
        //
        // This is geometry, so it is checked as geometry, on the real walls.
        var spawn = new DeckReachability.Point(WreckLayout.SpawnX, WreckLayout.SpawnY);
        DeckReachability.Walk walk = DeckReachability.Path(
            spawn,
            new(WreckLayout.ArchiveSwitchStation.X, WreckLayout.ArchiveSwitchStation.Y),
            WreckLayout.Walls(Derelict.WreckCause.VentedByOneOfTheirOwn),
            0.7, WreckLayout.Bounds);

        Assert.True(walk.Reached, "there is no way to the purge handle at all");
        foreach (DeckReachability.Point p in walk.Path)
        {
            Assert.False(
                ArchiveNode.AtArmsLength(p.X - WreckLayout.ArchiveStation.X,
                                         p.Y - WreckLayout.ArchiveStation.Y),
                $"the only route to the handle passes within arm's length of the column at ({p.X:0.0}, {p.Y:0.0})");
        }
    }

    public static TheoryData<Derelict.WreckCause> EveryCause()
    {
        var data = new TheoryData<Derelict.WreckCause>();
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            data.Add(c);
        }
        return data;
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
    public void TheCollarIsBoughtWithABadThrow_NeverGivenWithAGoodOne()
    {
        // "The instrument cannot tell you. The dice can. The dice cost nerve." Knowing whose pattern is in
        // the spar is the price of having been LOOKED AT — if a clean throw ever read it, the decision at
        // the handle stops being a gamble and the whole §5 shape collapses.
        Assert.False(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.LookedAway));
        Assert.False(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.Saw));
        Assert.True(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.Noticed));
        Assert.True(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.Inside));
    }

    [Fact]
    public void TheCollarIsADOCUMENT_NotAVerdict()
    {
        foreach (ArchiveNode.Resident who in System.Enum.GetValues<ArchiveNode.Resident>())
        {
            string line = ArchiveNode.CollarLine(who);
            Assert.False(string.IsNullOrWhiteSpace(line));

            // Evidence, never conclusion — and never a word about what will happen if you pull. The stencil
            // is a number and a status, in the same clerical hand as every other piece of Nebula paper.
            Assert.DoesNotContain("purge", line, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("do not", line, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("warning", line, System.StringComparison.OrdinalIgnoreCase);
        }

        // And the three read DIFFERENTLY, or paying for the look bought nothing.
        Assert.Equal(3, System.Enum.GetValues<ArchiveNode.Resident>()
            .Select(ArchiveNode.CollarLine).Distinct().Count());
    }

    [Fact]
    public void TheFirstThingSaidAboutItIsTheTEMPERATURE_AndNothingElse()
    {
        // The fiction arrives one beat early: the captain is told the hold is warm BEFORE the pip row moves
        // and before anything has been touched. It must not name what is doing it, warn about it, or tell
        // the captain what to do — a hint that instructs is a prompt, and this feature does not prompt.
        string line = ArchiveNode.FieldEntryLine;

        Assert.Contains("warm", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("archive", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nebula", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nerve", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("danger", line, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NothingAnywhereInThisFeatureExplainsTheOldOnes()
    {
        // House canon, and the one line that may never be crossed: a crew member may speculate, the game may
        // never confirm. Every sentence this feature can put on a screen, swept in one place.
        var everything = new List<string>
        {
            ArchiveNode.HandlerNote, ArchiveNode.SwitchLegend, ArchiveNode.PurgeLine,
            ArchiveNode.NoRestoreLine, ArchiveNode.FieldEntryLine, ArchiveNode.ColdColumnLine,
        };
        everything.AddRange(ArchiveNode.Visions.Select(v => v.Lore));
        everything.AddRange(System.Enum.GetValues<ArchiveNode.Band>().Select(ArchiveNode.BandLine));
        everything.AddRange(System.Enum.GetValues<ArchiveNode.Resident>().Select(ArchiveNode.CollarLine));

        foreach (string line in everything)
        {
            Assert.DoesNotContain("old one", line, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("reever", line, System.StringComparison.OrdinalIgnoreCase);
            // …nor the restore theory itself, stated as fact by anything the game prints.
            Assert.DoesNotContain("failed restore", line, System.StringComparison.OrdinalIgnoreCase);
        }
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
