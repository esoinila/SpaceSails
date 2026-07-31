using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#563 · The map-just-grew card. Pinned the way <see cref="GroundLesson"/> is: this is a card a
/// captain sees exactly once, ever, so there is no second chance for it to be wrong.</summary>
public class GroundGrowsTests
{
    [Fact]
    public void TheCard_IsFullyDressed()
    {
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Stamp));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Head));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Dismiss));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Foot));
        Assert.NotEmpty(GroundGrows.Beats);
    }

    [Fact]
    public void EveryBeat_IsRealProse_NotAStub()
    {
        // The failure this card exists to end is a one-line toast that faded before it taught anything.
        // Replacing it with three stub bullets would be the same failure wearing a bigger frame.
        foreach (string beat in GroundGrows.Beats)
        {
            Assert.True(beat.Length > 60, $"a beat barely explains itself: \"{beat}\"");
        }
    }

    [Fact]
    public void ItPromisesTheThingThatChangesHowYouPlay_ThatAWallIsNotAlwaysTheEnd()
    {
        // The entire point. A captain who believes the site has edges stops at them; one who believes a
        // sealed thing is a question goes looking. Everything #563 builds — huts, caches, breadcrumbs —
        // is worth nothing to a player who never learned to try the door.
        string all = string.Join(" ", GroundGrows.Beats).ToUpperInvariant();
        Assert.Contains("SEALED", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItSaysNobodyWasTeleported()
    {
        // The mechanic is genuinely unusual and easy to misread as a level load. If the card does not say
        // the ground simply CONTINUES, a player reasonably assumes they were moved somewhere else — and
        // then the walk back to the tube, which is the whole tether (#562), stops making sense to them.
        string first = GroundGrows.Beats[0].ToUpperInvariant();
        Assert.Contains("NOT MOVED", first, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItNamesTheCost_BecauseForcingSomethingOpenIsNeverFree()
    {
        // "buys time, never safety" is the law the sentries live under and the door channel obeys too: the
        // tracker keeps sweeping while you work. A card that sold expansion as pure upside would be lying.
        string all = string.Join(" ", GroundGrows.Beats).ToUpperInvariant();
        Assert.Contains("TIME", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheArt_IsUnderTheArtFolder_AndIsAJpeg()
    {
        // The house rule: art paths are owned by Core so the markup cannot drift from the file on disk.
        Assert.StartsWith("art/", GroundGrows.ArtFile, System.StringComparison.Ordinal);
        Assert.EndsWith(".jpg", GroundGrows.ArtFile, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheDismiss_IsAnAcknowledgement_NotAnOk()
    {
        // Same voice rule as GroundLesson.Dismiss ("Boots on, then."). A bare OK on a card about the world
        // getting bigger is a wasted line.
        Assert.DoesNotContain("OK", GroundGrows.Dismiss, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(GroundGrows.Dismiss.Length > 10);
    }
}
