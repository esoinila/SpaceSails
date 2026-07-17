namespace SpaceSails.Core.Tests;

// #209 — the plotting ribbon's horizon arithmetic, pinned pure. The Venus→Mars playtest bug was the
// #145 frame crop silently shrinking a departure ribbon to a Venus loop; these lock the plan-aware
// auto-horizon rule (no plan → local-period crop; plan → covers the furthest encounter; can't reach →
// flagged) that fixes it, independent of any browser.
public class PlotHorizonTests
{
    private const double Day = 86400.0;
    private const double MinSeconds = 30 * Day;
    private const double MarginSeconds = 90 * Day;
    private const double CapSeconds = 730 * Day;
    private const double BaseFloor = 6 * 3600;

    // ---- AutoProjectionSeconds ----------------------------------------------------------------

    [Fact]
    public void AutoProjection_NoPlan_HoldsTheLocalMinimum()
    {
        Assert.Equal(MinSeconds, PlotHorizon.AutoProjectionSeconds(0, MinSeconds, MarginSeconds, CapSeconds));
        Assert.Equal(MinSeconds, PlotHorizon.AutoProjectionSeconds(-5 * Day, MinSeconds, MarginSeconds, CapSeconds));
    }

    [Fact]
    public void AutoProjection_WithPlan_ReachesFurthestEncounterPlusMargin()
    {
        // A Venus→Mars hop whose furthest encounter is 120 d out projects to 120 d + the 90 d margin.
        double horizon = PlotHorizon.AutoProjectionSeconds(120 * Day, MinSeconds, MarginSeconds, CapSeconds);
        Assert.Equal((120 + 90) * Day, horizon, 3);
    }

    [Fact]
    public void AutoProjection_NearPlan_ReachesEncounterPlusMargin()
    {
        // A plan just 2 d out projects the encounter + the 90 d margin (92 d) — comfortably past the 30 d
        // minimum, matching the pre-#209 "last node + 90 d" behaviour.
        Assert.Equal((2 + 90) * Day, PlotHorizon.AutoProjectionSeconds(2 * Day, MinSeconds, MarginSeconds, CapSeconds), 3);
    }

    [Fact]
    public void AutoProjection_RunawayPlan_ClampedToTheCap()
    {
        // A plan past the projection cap can never ask for an un-affordable re-projection.
        double horizon = PlotHorizon.AutoProjectionSeconds(2000 * Day, MinSeconds, MarginSeconds, CapSeconds);
        Assert.Equal(CapSeconds, horizon, 3);
    }

    // ---- DrawnWindow: no scalable frame (Sun / dock) ------------------------------------------

    [Fact]
    public void DrawnWindow_SunFrame_DrawsTheFullProjection()
    {
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 200 * Day, localWindowSeconds: 0, planFurthestSeconds: 0, BaseFloor);
        Assert.Equal(200 * Day, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.None, r.Note);
    }

    [Fact]
    public void DrawnWindow_SunFrame_PlanPastCap_IsFlagged()
    {
        // Even with no frame crop, a plan running past the (capped) projection is an honest short picture.
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: CapSeconds, localWindowSeconds: 0, planFurthestSeconds: 900 * Day, BaseFloor);
        Assert.Equal(CapSeconds, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.CappedShortOfPlan, r.Note);
    }

    // ---- DrawnWindow: co-moving frame ---------------------------------------------------------

    [Fact]
    public void DrawnWindow_FrameNoPlan_CropsToLocalPeriod_AndSaysSo()
    {
        // Venus frame, no plan: ~1.25 Venus periods (say ~2.8 d) crops the 200 d ribbon — flagged so the
        // captain knows the picture is frame-scaled, not the whole story.
        double localWindow = 2.8 * Day;
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 200 * Day, localWindowSeconds: localWindow, planFurthestSeconds: 0, BaseFloor);
        Assert.Equal(localWindow, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.FrameLocalPeriods, r.Note);
    }

    [Fact]
    public void DrawnWindow_FramePlan_FloorsUpToCoverTheEncounter_NoNote()
    {
        // The #209 fix: Venus frame with a plotted Mars burn 120 d out. The local Venus-period crop would
        // hide it; the plan floors the drawn window up to the encounter, and the ribbon reaches it — so
        // no "short picture" note.
        double localWindow = 2.8 * Day;
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 210 * Day, localWindowSeconds: localWindow, planFurthestSeconds: 120 * Day, BaseFloor);
        Assert.Equal(120 * Day, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.None, r.Note);
    }

    [Fact]
    public void DrawnWindow_FramePlanPastCap_FlaggedShort()
    {
        // The plan runs past what was projected (perf cap): the drawn ribbon is the full projection and
        // the panel must admit it can't reach the plan.
        double localWindow = 2.8 * Day;
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 100 * Day, localWindowSeconds: localWindow, planFurthestSeconds: 300 * Day, BaseFloor);
        Assert.Equal(100 * Day, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.CappedShortOfPlan, r.Note);
    }

    [Fact]
    public void DrawnWindow_FrameBaseFloor_KeepsNearTermReadable()
    {
        // With no plan but an imminent node folding into the base floor (say 5 d), the drawn window is at
        // least that floor even though 1.25 local periods is shorter — and it's still a local-scaled crop.
        double localWindow = 2.8 * Day;
        double baseFloor = 5 * Day;
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 200 * Day, localWindowSeconds: localWindow, planFurthestSeconds: 0, baseFloor);
        Assert.Equal(5 * Day, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.FrameLocalPeriods, r.Note);
    }

    [Fact]
    public void DrawnWindow_FrameWindowNeverExceedsFullProjection()
    {
        // A slow, wide frame orbit whose local period dwarfs the projection just draws all of it.
        var r = PlotHorizon.DrawnWindow(fullHorizonSeconds: 10 * Day, localWindowSeconds: 40 * Day, planFurthestSeconds: 0, BaseFloor);
        Assert.Equal(10 * Day, r.DrawnSeconds, 3);
        Assert.Equal(PlotHorizon.RibbonNote.None, r.Note); // reaches the full projection: nothing to flag
    }

    // ---- #265: BoundOrbitHorizon — cap a captured ship's ribbon to ~one revolution -----------------

    [Fact]
    public void BoundOrbitHorizon_Captured_CapsToAboutOneRevolution()
    {
        // A 2-day park revolution projected over the 60-day horizon drew the owner's Uranus flower; cap
        // it to ~1.15 revolutions so the ribbon closes its loop and reads "and so on".
        double period = 2 * Day;
        double horizon = PlotHorizon.BoundOrbitHorizon(60 * Day, period, planFurthestSeconds: 0);
        Assert.Equal(1.15 * period, horizon, 3);
    }

    [Fact]
    public void BoundOrbitHorizon_UnboundLeg_KeepsTheFullHorizon()
    {
        // Null period = a transfer/hyperbolic leg where the future genuinely extends: draw all of it.
        Assert.Equal(60 * Day, PlotHorizon.BoundOrbitHorizon(60 * Day, null, planFurthestSeconds: 0), 3);
    }

    [Fact]
    public void BoundOrbitHorizon_CapturedButDepartureIsPlotted_KeepsTheFullHorizon()
    {
        // Bound now, but a plotted departure reaches 40 d out — keep the transfer ribbon, don't clip it
        // to the park loop.
        double horizon = PlotHorizon.BoundOrbitHorizon(60 * Day, 2 * Day, planFurthestSeconds: 40 * Day);
        Assert.Equal(60 * Day, horizon, 3);
    }

    [Fact]
    public void BoundOrbitHorizon_RevolutionCapNeverExceedsTheProjection()
    {
        // A slow park whose 1.15 revolutions dwarf the (already short) projection just draws the projection.
        double horizon = PlotHorizon.BoundOrbitHorizon(5 * Day, 40 * Day, planFurthestSeconds: 0);
        Assert.Equal(5 * Day, horizon, 3);
    }
}
