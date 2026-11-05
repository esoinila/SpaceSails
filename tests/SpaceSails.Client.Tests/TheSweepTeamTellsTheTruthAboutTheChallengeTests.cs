using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1397 · The insurance-job sweep team challenges ("stand still") and shoots three seconds later whether or not the
/// captain obeyed (canon: the user guide's three seconds are a window to break the lamp's sight, not a mercy), and
/// the death card used to say a motionless captain "did not" stand still. The card now forks on what the captain did.
/// Driven on the real page: the real wreck, the real ChallengeRunsOut, the real painted card.
/// <b>RED</b> by making <c>ChallengeRunsOut</c> pass <c>heldStill: false</c> (the motionless captain is told he did not).
/// </summary>
public sealed class TheSweepTeamTellsTheTruthAboutTheChallengeTests
{
    private const string Url = "/map?dock=selene-gate&wreck=insurancejob&land=1";

    private static async Task<string> TheCardAfterTheChallengeRunsOut(double driftDu)
    {
        using var bench = await DeskBench.BootAsync(Url);
        await bench.RenderAsync();   // the first paint hands the page its render handle
        for (int i = 0; i < 100 && !bench.OnSurface; i++)
        {
            await Task.Delay(100);
            await bench.RenderAsync();
        }
        Assert.True(bench.OnSurface, "the bench never landed on the wreck");
        Assert.Null(bench.Peek("_busted"));

        double x = (double)bench.Peek("_avatarX")!, y = (double)bench.Peek("_avatarY")!;
        var sweeper = new Pages.Map.Sweeper { Callsign = "SWEEP-3", LastSeenX = x - driftDu, LastSeenY = y };
        bench.CallOnTheDispatcher("ChallengeRunsOut", sweeper);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants().FirstOrDefault(n => n.HasClass("busted-card"))
            ?? throw new Xunit.Sdk.XunitException("no death card was drawn after the challenge ran out");
        // The pulse is the deterministic half of the account (the card's line is one of three by seed); the card is
        // checked for the one sentence that must never appear on a motionless captain's card.
        return bench.Pulse + " || " + card.Spoken;
    }

    [Fact]
    public async Task AMotionlessCaptainIsNotToldHeDidNotStandStill()
    {
        string card = await TheCardAfterTheChallengeRunsOut(driftDu: 0.0);
        Assert.Contains("and standing still. It made no difference", card, StringComparison.Ordinal);
        Assert.DoesNotContain("and did not", card, StringComparison.Ordinal);
        Assert.DoesNotContain("not standing still", card, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACaptainWhoMovedStillReadsHeDidNot()
    {
        string card = await TheCardAfterTheChallengeRunsOut(driftDu: 6.0);
        Assert.Contains("and not standing still", card, StringComparison.Ordinal);
        Assert.DoesNotContain("It made no difference", card, StringComparison.Ordinal);
    }
}
