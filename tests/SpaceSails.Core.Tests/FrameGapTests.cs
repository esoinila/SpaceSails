using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #825 · THE REAL STALL CLOCK, pinned in Core.
///
/// <para>The client's own guards (<c>TheStallSaysSoTests</c>) drive a real floor and prove that the banner
/// and the input path read this one clock through this one threshold. What is left for Core is the
/// arithmetic and the vocabulary — and the ONE structural fact the whole law hangs on: that
/// <see cref="FrameGap.StallSeconds"/> is DERIVED from <see cref="FrameGap.SpentPerFrameSeconds"/>, the
/// clamp the frame actually spends, rather than typed beside it and left to agree by luck.</para>
/// </summary>
public sealed class FrameGapTests
{
    /// <summary>The threshold is the clamp, times a stated number of frames. If somebody ever types a
    /// literal here, this is the line that notices.</summary>
    [Fact]
    public void TheThresholdIsDerivedFromTheClampAndSitsAboveIt()
    {
        Assert.Equal(FrameGap.SpentPerFrameSeconds * FrameGap.StallFrames, FrameGap.StallSeconds, 12);
        Assert.True(FrameGap.StallSeconds > FrameGap.SpentPerFrameSeconds,
            "the stall threshold has fallen to or below the clamp — every ordinary frame is now a stall.");
    }

    /// <summary>Strictly above, so the threshold itself is still a healthy machine. One-sided, because a
    /// banner that flickered on and off at the boundary would read as a fault of its own.</summary>
    [Fact]
    public void IsStalling_TurnsOverAtTheThresholdAndNotBefore()
    {
        Assert.False(FrameGap.IsStalling(0));
        Assert.False(FrameGap.IsStalling(FrameGap.SpentPerFrameSeconds));
        Assert.False(FrameGap.IsStalling(FrameGap.StallSeconds));
        Assert.True(FrameGap.IsStalling(FrameGap.StallSeconds + 1e-6));
        Assert.True(FrameGap.IsStalling(16.0));   // the owner's own gap
    }

    /// <summary>The dropped time is everything past the one step the frame may spend — the honest size of
    /// what the clamp threw away, which at the owner's sixteen seconds is 15.9 s of legs.</summary>
    [Fact]
    public void SecondsDropped_IsEverythingPastTheOneStepAFrameBuys()
    {
        Assert.Equal(0.0, FrameGap.SecondsDropped(0));
        Assert.Equal(0.0, FrameGap.SecondsDropped(FrameGap.SpentPerFrameSeconds));
        Assert.Equal(16.0 - FrameGap.SpentPerFrameSeconds, FrameGap.SecondsDropped(16.0), 12);
    }

    /// <summary>The banner is empty on a healthy machine (the calm path paints nothing), names the gap when
    /// it is not, and ends in the phrase the issue asked for.</summary>
    [Fact]
    public void StallBanner_IsSilentWhileTheMachineKeepsUpAndNamesTheGapWhenItDoesNot()
    {
        Assert.Equal(string.Empty, FrameGap.StallBanner(FrameGap.StallSeconds));
        string banner = FrameGap.StallBanner(16.0);
        Assert.Contains("16s", banner);
        Assert.Contains("controls held", banner);
    }

    /// <summary>
    /// TWO DIFFERENT FACTS, TWO DIFFERENT SENTENCES. The machine's banner must not read as the mothership's:
    /// <see cref="CommsLink.StaleBanner"/> is a scripted fiction about a downlink and this is a report about
    /// the frame budget, and the one afternoon they were both true the player could only read the wrong one.
    /// </summary>
    [Fact]
    public void TheMachinesBannerIsNotTheMothershipsBanner()
    {
        string stall = FrameGap.StallBanner(16.0);
        Assert.DoesNotContain("SIGNAL", stall);
        Assert.NotEqual(CommsLink.StaleBanner(CommsLink.Phase.Degraded, 16.0), stall);
        Assert.NotEqual(CommsLink.StaleBanner(CommsLink.Phase.Blackout, 16.0), stall);
        Assert.DoesNotContain("SIGNAL", FrameGap.HeldLine);
    }

    /// <summary>Tenths under a second (a stall you can only just see), whole seconds up to a minute, then
    /// minutes — deliberately not <see cref="CommsLink.Humanize"/>'s "moments", which is the right word for
    /// a radio and the wrong one for a frame budget.</summary>
    [Fact]
    public void Humanize_ReadsTenthsUnderASecondAndMinutesOverOne()
    {
        Assert.Equal("0.6s", FrameGap.Humanize(0.6));
        Assert.Equal("16s", FrameGap.Humanize(16.0));
        Assert.Equal("2 min", FrameGap.Humanize(120.0));
        Assert.Equal("moments", CommsLink.Humanize(0.6)); // the sibling's own word, kept apart on purpose
    }
}
