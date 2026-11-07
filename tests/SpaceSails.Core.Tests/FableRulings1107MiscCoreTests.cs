using System.Linq;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Fable's rulings of 2026-11-07 (owner-delegated), the Core half of the misc lane: R1-F100, R1-F92, R1-F103,
/// R1-F95, U03-F18, U07-F13, U09-F02 and U09-F07. Each test names the one hunk whose reversion turns it red.
/// </summary>
public sealed class FableRulings1107MiscCoreTests
{
    // ── R1-F100 + R1-F92 · the two re-authored KAAMOS passages, verbatim ───────────────────────────────

    [Fact]
    public void TheBoughtCoordinateNamesTheIceNotTheEphemeris()
    {
        string lore = KaamosLore.ById("bought-coordinate")!.Lore;

        Assert.Equal(
            "A round on the counter buys the rest of it: a fix on the ice itself — a spot the charts carry as " +
            "nothing at all, on a moon they name without comment — and a date, and the word CYCLER. \"The window's " +
            "real,\" they say, pocketing the coin. \"Comes round rare. A ship that's on the board when it opens can " +
            "ride it in. Getting back out — that's not the part they sell tickets for.\" You have the where and the when.",
            lore);
        Assert.DoesNotContain("ICE MOON — UNREACHABLE", lore);   // RED: revert KaamosLore.cs bought-coordinate
    }

    [Fact]
    public void TheColdPodCaptionReadsThePlateAndTheSlug()
    {
        string caption = KaamosLore.PlateFor("cold-pod")!.Caption;

        Assert.Equal(
            "Frost has split her along three seams and the dust has scoured her hull plate down to nothing you can " +
            "read. The manifest slug under the clamp is another matter — stamped, not painted — and legible to the " +
            "last word. Every clamp seal is still latched exactly as it left the yard. She was not lost on the way " +
            "out. She never went. Somebody stood here, in a shed, and set her down.",
            caption);   // RED: revert KaamosLore.cs cold-pod plate
    }

    // ── R1-F103 · the head office's shed is a lift head ─────────────────────────────────────────────────

    [Fact]
    public void TheHeadOfficeShedIsOnePlainChamber()
    {
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;
        SecretLab.Placement p = SecretLab.For(KaamosLore.IceMoonBodyId, field, forcePresent: true);
        SecretLab.Region hq = SecretLab.Build(KaamosLore.IceMoonBodyId, field, p.DoorX, p.DoorY);

        Assert.Empty(hq.Consoles);
        Assert.Empty(hq.Doors);
        Assert.Empty(hq.TheHidden);
        Assert.Empty(hq.Landmarks);
        Assert.Equal(0, hq.DiscoveryBonus);
        // One chamber: two side walls and a near and a far face with a doorway each (the fire code: two ways out),
        // nothing else (no spine, benches, boxes, deep rooms).
        Assert.Equal(6, hq.Walls.Count);
    }

    [Fact]
    public void AnOrdinaryMoonsLabKeepsEverythingTheHeadOfficeLost()
    {
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;
        SecretLab.Placement p = SecretLab.For("phobos", field, forcePresent: true);
        SecretLab.Region lab = SecretLab.Build("phobos", field, p.DoorX, p.DoorY);

        Assert.NotEmpty(lab.Consoles);   // the pair to the test above: the head office branch is the only thing that empties it
        Assert.NotEmpty(lab.Doors);
        Assert.NotEmpty(lab.TheHidden);
        Assert.True(lab.Walls.Count > 5);
    }

    // ── R1-F95 · the underwriter's stamp lives on the dwell card ────────────────────────────────────────

    [Fact]
    public void TheUnderwritersStampIsTheCardsFirstRow()
    {
        Assert.Equal("NEBULA MUTUAL · SUBSTRATE SPAR", ArchiveNode.UnderwriterStamp(purged: false));
        Assert.Equal("NEBULA MUTUAL · SUBSTRATE SPAR · cold", ArchiveNode.UnderwriterStamp(purged: true));
    }

    // ── U03-F18 · Kolt is never on a gig rock ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true, false, 0, false, true)]    // the open regolith of an ordinary moon: his ground
    [InlineData(true, false, 0, true, false)]    // the same regolith, but a deflection gig's rock: never
    public void HeIsNeverFoundOnADeflectionGigsRock(bool landed, bool wreck, int floor, bool gigRock, bool his) =>
        Assert.Equal(his, HardcaseRep.GroundLikeThis(landed, wreck, floor, onAGigRock: gigRock));

    // ── U07-F13 · the dock's own name ───────────────────────────────────────────────────────────────────

    [Fact]
    public void LayLowAndFlyToNameTheDockByItsOwnName()
    {
        var roadstead = new CelestialBody("the-space-bar", "The Rusty Roadstead", "mars", 0, 900, 1.2e7, 39910, 3.1, BodyKind.Station, IsHaven: true);
        CelestialBody[] bodies = [roadstead];

        ShipMission layLow = MissionCatalog.LayLowOptions(bodies).Single();
        Assert.Equal("Lay low: The Rusty Roadstead", layLow.Describe());
        // RED: drop HavenName from LayLowOptions and this reads the humanized id, "Lay low: The Space Bar".

        // The plot builds the same FlyTo without a name; it is still the one mission, so the desk lights its card.
        var fromThePlot = new ShipMission(MissionKind.FlyTo, DestinationBodyId: "the-space-bar");
        var fromTheDesk = new ShipMission(MissionKind.FlyTo, DestinationBodyId: "the-space-bar",
            DestinationName: "The Rusty Roadstead");
        Assert.Equal(fromThePlot, fromTheDesk);
        Assert.Equal("Make for: The Rusty Roadstead orbit", fromTheDesk.Describe());
        Assert.Equal("Make for: The Space Bar orbit", fromThePlot.Describe());
    }

    // ── U09-F02 · the pendant's first opening is a card ─────────────────────────────────────────────────

    [Fact]
    public void ThePendantsFirstOpeningIsACardThatSpeaksEveryTimeItIsRaised()
    {
        StoryBeats.Beat b = StoryBeats.Beat.KeepsakeFirstOpening;

        Assert.Equal(StoryBeats.Presentation.Card, StoryBeats.PresentationOf(b));
        Assert.Equal(StoryBeats.Cadence.EveryTime, StoryBeats.CadenceOf(b));
        Assert.Equal(FilingLine.Mark + " " + Keepsake.FlashbackTitleText, StoryBeats.Title(b, Keepsake.PendantSubject));
        Assert.Equal(Keepsake.FirstOpeningLine, StoryBeats.Caption(b, Keepsake.PendantSubject));
        Assert.NotEmpty(StoryBeats.ArtFile(b, Keepsake.PendantSubject));
    }

    // ── U09-F07 · only the haven keeps have regulars to warm ────────────────────────────────────────────

    [Fact]
    public void OnlyTheHavenKeepsHaveRegularsToWarm()
    {
        foreach (Barkeep keep in Barkeeps.AllBarkeeps)
        {
            Assert.True(keep.HasRegularsToWarm, keep.BodyId);
        }

        Barkeep self = CounterService.For("test-rock", UndergroundComplex.Comfort.UpperCanteen)!;
        Assert.False(self.HasRegularsToWarm);
        // …and #781's keep, the same counter with a person behind it, has a person and still no regulars.
        long aKeptWatch = Enumerable.Range(0, 400).Select(w => (long)w).First(w => TheKeep.KeptWatch(w));
        Barkeep kept = CounterService.OnWatch(self, aKeptWatch);
        Assert.False(kept.SelfService);
        Assert.False(kept.HasRegularsToWarm);   // RED: define HasRegularsToWarm as !SelfService and this fails
    }
}
