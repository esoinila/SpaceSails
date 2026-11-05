using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1392 · The ground writ wore the in-space voice (grapples across a window, "heave to", a "Fly on" button for a
/// captain with no ship). The id the panel holds is what tells the two catches apart; the words follow it.
/// <b>RED</b> by returning "Fly on" for every id from <c>CloseFace</c>.
/// </summary>
public sealed class TheGroundWritSpeaksOnFootTests
{
    [Fact]
    public void AGroundCrewClosesWithGoOnAndAHullWithFlyOn()
    {
        Assert.Equal("…go on", CollectorLanding.CloseFace($"{CollectorLanding.GroundHunterIdPrefix}luna"));
        Assert.Equal("Fly on", CollectorLanding.CloseFace("hunter-3"));
        Assert.Equal("Fly on", CollectorLanding.CloseFace(null));
    }

    [Fact]
    public void TheCaptionOnFootNamesNoWindowNoGrapplesAndNoOrderToHeaveTo()
    {
        string caption = CollectorLanding.WritCaptionOnFoot("THE LONG ARM");
        string quote = CollectorLanding.WritQuoteOnFoot("hard");
        Assert.Contains("THE LONG ARM", caption);
        Assert.Contains("carry loop", caption);
        foreach (string text in new[] { caption, quote })
        {
            Assert.DoesNotContain("grapple", text, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("window", text, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("heave", text, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
