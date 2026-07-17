namespace SpaceSails.Core.Tests;

/// <summary>
/// #253 — a context menu must never render an action off-screen. The owner clicked The Tilt at the
/// bottom-right; its menu ran off the bottom and hid the 🚀 Long haul entry. These pin down the
/// geometry: open down-right of the click when there's room, flip above/left near an edge, and never
/// let the box cross a viewport boundary.
/// </summary>
public class MenuLayoutTests
{
    // A roomy canvas and a middling menu box the fixtures reuse.
    private const double Vw = 1280;
    private const double Vh = 800;
    private const double Mw = 256; // the .map-body-menu max-width (16rem)
    private const double Mh = 160;

    [Fact]
    public void ClickInTheOpen_OpensDownRightOfTheClick()
    {
        (double x, double y) = MenuLayout.ClampMenuPosition(400, 300, Mw, Mh, Vw, Vh);

        // No edge nearby: the menu keeps its down-right offset from the click, untouched.
        Assert.Equal(400 + MenuLayout.DefaultOffsetPx, x);
        Assert.Equal(300, y);
    }

    [Fact]
    public void NearTheRightEdge_FlipsToTheLeftOfTheClick()
    {
        // Click 40 px from the right edge — opening right would push the 256 px box off-screen.
        double anchorX = Vw - 40;
        (double x, double _) = MenuLayout.ClampMenuPosition(anchorX, 300, Mw, Mh, Vw, Vh);

        Assert.Equal(anchorX - MenuLayout.DefaultOffsetPx - Mw, x);
        Assert.True(x + Mw <= Vw, "the box must sit fully within the right edge");
    }

    [Fact]
    public void NearTheBottomEdge_FlipsAboveTheClick()
    {
        // The Tilt's bug: click 30 px from the bottom, the 160 px menu would overflow downward.
        double anchorY = Vh - 30;
        (double _, double y) = MenuLayout.ClampMenuPosition(400, anchorY, Mw, Mh, Vw, Vh);

        Assert.Equal(anchorY - Mh, y);
        Assert.True(y + Mh <= Vh, "the box must sit fully within the bottom edge");
    }

    [Fact]
    public void BottomRightCorner_FlipsBothWays_StaysOnScreen()
    {
        // The exact playtest corner: click in the bottom-right, both flips fire at once.
        (double x, double y) = MenuLayout.ClampMenuPosition(Vw - 20, Vh - 20, Mw, Mh, Vw, Vh);

        Assert.True(x >= 0 && x + Mw <= Vw, "fully within left/right");
        Assert.True(y >= 0 && y + Mh <= Vh, "fully within top/bottom");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1279, 799)]
    [InlineData(640, 780)]
    [InlineData(1260, 400)]
    public void AnywhereOnScreen_TheBoxIsFullyContained(double anchorX, double anchorY)
    {
        double margin = MenuLayout.DefaultMarginPx;
        (double x, double y) = MenuLayout.ClampMenuPosition(anchorX, anchorY, Mw, Mh, Vw, Vh);

        Assert.True(x >= margin, "left edge respects the margin");
        Assert.True(y >= margin, "top edge respects the margin");
        Assert.True(x + Mw <= Vw - margin, "right edge respects the margin");
        Assert.True(y + Mh <= Vh - margin, "bottom edge respects the margin");
    }

    [Fact]
    public void AMenuTallerThanTheViewport_PinsToTheTop_NeverNegative()
    {
        // Degenerate guard: a box taller than the canvas can't fit — it must still pin on-screen at
        // the top-left rather than clamping to a negative (off-screen) coordinate.
        (double x, double y) = MenuLayout.ClampMenuPosition(50, 50, 300, 2000, 400, 300);

        Assert.True(x >= 0);
        Assert.True(y >= 0);
    }
}
