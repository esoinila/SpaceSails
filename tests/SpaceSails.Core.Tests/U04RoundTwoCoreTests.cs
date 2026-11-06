using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Round 2 · test user U04 (Rook) · #1418. The Core-side facts: what a surface death's pulse says, and what a pass
/// prints on its face. Each names the revert that turned it RED.
/// </summary>
public sealed class U04RoundTwoCoreTests
{
    /// <summary>
    /// U04-F07 · The fifth blow is not a nerve break. <b>Proven RED</b> by deleting the
    /// <c>DeathCause.Reevers when !nerveRanOut</c> arm: the pulse falls to the default and says "past empty".
    /// </summary>
    [Theory]
    [InlineData("Miranda")]
    [InlineData("Luna")]
    [InlineData("Phobos")]
    public void ADeathByBlowsDoesNotClaimTheNerveBroke(string body)
    {
        string blows = DeathNarration.SurfaceDeathPulse(DeathCause.Reevers, body, nerveRanOut: false, heldStill: false);
        Assert.DoesNotContain("past empty", blows, StringComparison.Ordinal);
        Assert.DoesNotContain("breaks", blows, StringComparison.Ordinal);
        Assert.Contains(body, blows, StringComparison.Ordinal);

        // ...and the nerve death keeps its sentence.
        string nerve = DeathNarration.SurfaceDeathPulse(DeathCause.Reevers, body, nerveRanOut: true, heldStill: false);
        Assert.Contains("past empty", nerve, StringComparison.Ordinal);
    }

    /// <summary>U04-F08 · A captain who stood still in the lamp is never told he was not standing still.
    /// (Fixed on main by #1397/#1430; pinned here from the pulse's own seam.)</summary>
    [Fact]
    public void ACaptainWhoStoodStillIsNotToldHeDidNot()
    {
        string still = DeathNarration.SurfaceDeathPulse(DeathCause.Inspected, "ship", nerveRanOut: false, heldStill: true);
        Assert.DoesNotContain("not standing still", still, StringComparison.Ordinal);
        string moved = DeathNarration.SurfaceDeathPulse(DeathCause.Inspected, "ship", nerveRanOut: false, heldStill: false);
        Assert.Contains("not standing still", moved, StringComparison.Ordinal);
    }

    /// <summary>
    /// U04-F23 · One readable site name per pass, whatever the id. <b>Proven RED</b> by restoring
    /// the old <c>{Designation} SITE</c> suffix in <c>BadgeTitle</c>.
    /// </summary>
    [Theory]
    [InlineData("secret-lab-site-unlisted", "SITE PASS · GENERAL HANDS · SECRET LAB SITE · UNLISTED")]
    [InlineData("secret-lab-site-sealed", "SITE PASS · GENERAL HANDS · SECRET LAB SITE · SEALED")]
    [InlineData("secret-lab-site", "SITE PASS · GENERAL HANDS · SECRET LAB SITE")]
    [InlineData("luna", "SITE PASS · GENERAL HANDS · LUNA SITE")]
    public void APassNamesItsSiteOnce(string body, string expected)
    {
        Assert.Equal(expected, PatrolBeat.BadgeTitle(body));
    }
}
