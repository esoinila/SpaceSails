namespace SpaceSails.Core.Tests;

using SpaceSails.Core;
using static SpaceSails.Core.OverlayLayout;

/// <summary>
/// #293 — the reachability law itself, pinned down on synthetic rectangles before it is pointed at the
/// real HUD. A control is pressable when enough of its hit-rect stays on-screen and clear of every
/// higher pointer-events layer; the verdicts name the failure modes, including the owner's exact
/// "barely clickable".
/// </summary>
public class OverlayLayoutTests
{
    private static readonly Rect Screen = new(0, 0, 1000, 1000);
    private static readonly Overlay Control = new("control", new Rect(400, 400, 200, 200), ZIndex: 50);

    [Fact]
    public void NothingOnTop_IsFullyReachable()
    {
        ReachResult r = Evaluate(Control, Screen, []);

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
        Assert.Equal(1.0, r.FreeFraction, 6);
        Assert.Equal(Control.Bounds.Area, r.FreeArea, 6);
    }

    [Fact]
    public void AHigherPointerEventsLayerOnTop_FullyOccludes()
    {
        Overlay cover = new("cover", Control.Bounds, ZIndex: 60);

        ReachResult r = Evaluate(Control, Screen, [cover]);

        Assert.Equal(ReachVerdict.Occluded, r.Verdict);
        Assert.Equal(0, r.FreeArea);
        Assert.Contains("cover", r.OccludedBy);
    }

    [Fact]
    public void ALowerLayer_DoesNotOcclude_TheControlPaintsOnTop()
    {
        Overlay under = new("under", Control.Bounds, ZIndex: 40);

        ReachResult r = Evaluate(Control, Screen, [under]);

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
        Assert.Empty(r.OccludedBy);
    }

    [Fact]
    public void AHigherLayerThatSwallowsNoClicks_DoesNotOcclude()
    {
        // pointer-events:none — it paints over the control but never eats a click (#195 gaps pattern).
        Overlay ghost = new("ghost", Control.Bounds, ZIndex: 99, PointerEvents: false);

        ReachResult r = Evaluate(Control, Screen, [ghost]);

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
    }

    [Fact]
    public void AHalfCover_HalvesTheFreeArea_ButStaysReachable()
    {
        // Covers the left half of the 200x200 control — 100x200 survives on the right.
        Overlay half = new("half", new Rect(400, 400, 100, 200), ZIndex: 60);

        ReachResult r = Evaluate(Control, Screen, [half]);

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
        Assert.Equal(0.5, r.FreeFraction, 6);
        Assert.Equal(100, r.FreeWidth, 6);
    }

    [Fact]
    public void ASliverLeftOver_IsBarelyClickable()
    {
        // Covers all but a 20px strip on the right — narrower than the 24px minimum target.
        Overlay nearlyAll = new("nearly-all", new Rect(400, 400, 180, 200), ZIndex: 60);

        ReachResult r = Evaluate(Control, Screen, [nearlyAll]);

        Assert.Equal(ReachVerdict.BarelyClickable, r.Verdict);
        Assert.True(r.FreeArea > 0, "a sliver survives");
        Assert.True(r.FreeWidth < MinTapPx, "the sliver is thinner than a fingertip");
    }

    [Fact]
    public void AFullScreenGate_Supersedes_RatherThanOccludes()
    {
        // A modal covering the whole viewport owns the screen and supplies its own resolution.
        Overlay gate = new("modal-gate", Screen, ZIndex: 90);

        ReachResult r = Evaluate(Control, Screen, [gate]);

        Assert.Equal(ReachVerdict.Reachable, r.Verdict);
        Assert.Contains("modal-gate", r.SupersededBy);
        Assert.Empty(r.OccludedBy);
    }

    [Fact]
    public void ADisabledControl_FailsRegardlessOfGeometry()
    {
        ReachResult r = Evaluate(Control, Screen, [], enabled: false);

        Assert.Equal(ReachVerdict.Disabled, r.Verdict);
    }

    [Fact]
    public void AControlPushedOffScreen_IsOffViewport()
    {
        Overlay offscreen = new("offscreen", new Rect(2000, 400, 200, 200), ZIndex: 50);

        ReachResult r = Evaluate(offscreen, Screen, []);

        Assert.Equal(ReachVerdict.OffViewport, r.Verdict);
    }

    [Fact]
    public void TwoOverlappingCovers_AreCountedAsUnion_NotDoubleSubtracted()
    {
        // Two higher covers overlapping each other: the left 120 and the middle 60..180. Their union is
        // 0..180 of the 200-wide control, leaving a 20px sliver — proving no double-counting.
        Overlay a = new("a", new Rect(400, 400, 120, 200), ZIndex: 60);
        Overlay b = new("b", new Rect(460, 400, 120, 200), ZIndex: 61);

        ReachResult r = Evaluate(Control, Screen, [a, b]);

        Assert.Equal(20, r.FreeWidth, 6);
        Assert.Equal(20 * 200, r.FreeArea, 6);
    }
}
