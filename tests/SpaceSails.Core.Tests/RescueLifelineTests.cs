namespace SpaceSails.Core.Tests;

using SpaceSails.Core;
using static SpaceSails.Core.OverlayLayout;

/// <summary>
/// #293 — the first regression gate for the ship's lifeline. The owner: "the rescue-me button was
/// barely clickable when we ran out of power… this problem keeps biting us as we develop." So we assert
/// it, against the real Map HUD encoded in <see cref="RescueLifeline"/>: in the out-of-power state the
/// reopen affordance is present, enabled, on-screen, and painted over by nothing it cannot beat — at
/// every standard viewport size. The gate also CATCHES the layout that shipped the original bug.
/// </summary>
public class RescueLifelineTests
{
    [Fact]
    public void OutOfPower_TheReopenAffordance_IsFullyReachable()
    {
        Rect viewport = RescueLifeline.Viewport();
        Overlay pill = RescueLifeline.ReopenPill(viewport, RescueLifeline.LifelineZIndex);

        ReachResult r = Evaluate(pill, viewport, RescueLifeline.OutOfPowerOverlays(viewport));

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
        Assert.Equal(1.0, r.FreeFraction, 6); // nothing higher-z overlaps the lifeline band.
        Assert.Empty(r.OccludedBy);
    }

    [Theory]
    [InlineData(1280, 800)]  // desktop
    [InlineData(1024, 768)]  // small laptop
    [InlineData(390, 844)]   // phone portrait
    [InlineData(844, 390)]   // phone landscape
    [InlineData(320, 480)]   // the smallest supported canvas
    public void OutOfPower_TheReopenAffordance_StaysReachable_AtEverySize(double w, double h)
    {
        Rect viewport = RescueLifeline.Viewport(w, h);
        Overlay pill = RescueLifeline.ReopenPill(viewport, RescueLifeline.LifelineZIndex);

        ReachResult r = Evaluate(pill, viewport, RescueLifeline.OutOfPowerOverlays(viewport));

        Assert.True(r.Ok, $"lifeline unreachable at {w}x{h}: {r.Verdict} (free {r.FreeWidth:F0}x{r.FreeHeight:F0})");
    }

    [Fact]
    public void TheLifelineBand_SitsAboveEveryOverlayItCanShareTheScreenWith()
    {
        Rect viewport = RescueLifeline.Viewport();
        int highestOther = RescueLifeline.OutOfPowerOverlays(viewport).Max(o => o.ZIndex);

        // Above every raisable HUD panel, and below the rescue modal it opens (1360).
        Assert.True(RescueLifeline.LifelineZIndex > highestOther, "lifeline must out-rank every co-raised panel");
        Assert.True(RescueLifeline.LifelineZIndex < 1360, "lifeline must sit below the rescue modal backdrop (z 1360)");
    }

    [Fact]
    public void TheGateCatchesTheOriginalBug_TheStripBuriedUnderTheMasthead()
    {
        // The pre-#262 layout: the adrift strip at top:0.75rem, trapped under .map-topstack, with the
        // masthead painting over it. This is exactly what "barely clickable" looked like — the gate,
        // had it existed, would have failed the build.
        Rect viewport = RescueLifeline.Viewport();
        Overlay strip = RescueLifeline.BuriedStrip(viewport);
        Overlay masthead = RescueLifeline.MastheadBand(viewport);

        ReachResult r = Evaluate(strip, viewport, [masthead]);

        Assert.NotEqual(ReachVerdict.Reachable, r.Verdict);
        Assert.Contains("masthead/pilot-banner", r.OccludedBy);
    }

    [Fact]
    public void TheReservedBand_EarnsItsPlace_BeatingADeskBandPopupThatZ30CannotBeat()
    {
        // A forward guard: drop ANY pointer-events pop-up from the desk/deck band (z≤1320) over the
        // bottom-centre. The old z-30 pill loses; only the reserved lifeline band survives it. This is
        // why reachability is now a LAW, not a hand-tuned z-value re-checked by eye every time.
        Rect viewport = RescueLifeline.Viewport();
        Overlay hostile = RescueLifeline.DeskBandPopupOverBottom(viewport);

        ReachResult atPreLab = Evaluate(
            RescueLifeline.ReopenPill(viewport, RescueLifeline.PreLabZIndex), viewport, [hostile]);
        ReachResult atLifeline = Evaluate(
            RescueLifeline.ReopenPill(viewport, RescueLifeline.LifelineZIndex), viewport, [hostile]);

        Assert.NotEqual(ReachVerdict.Reachable, atPreLab.Verdict);
        Assert.Equal(ReachVerdict.Reachable, atLifeline.Verdict);
    }
}
