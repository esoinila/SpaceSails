using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// A 2D top-down camera over the ecliptic plane. Converts world positions (meters, doubles,
/// solar-system scale) to canvas pixels and back.
///
/// <para><b>Floating origin.</b> The transform is always relative to <see cref="Center"/>, so a
/// point near the center is computed as a small (world - Center) difference before being divided
/// down to pixels. Absolute world coordinates (up to ~4.5e12 m for Neptune) never reach the pixel
/// math directly, keeping precision high even at extreme zoom-in.</para>
///
/// <para><b>Axes.</b> World Y points up (standard math); canvas Y points down. The transform flips Y.</para>
///
/// <para>Zoom is expressed as <see cref="MetersPerPixel"/>: larger = further out. The default clamp
/// spans ~13 orders of magnitude, from sub-meter ship close-ups to the whole outer system.</para>
/// </summary>
public sealed class Camera
{
    /// <summary>Tightest zoom-in (finest detail). 1e-2 m/px → a 1000 px viewport shows 10 m.</summary>
    public const double DefaultMinMetersPerPixel = 1e-2;

    /// <summary>Widest zoom-out. 1e11 m/px → a 1000 px viewport shows 1e14 m (well past Neptune).</summary>
    public const double DefaultMaxMetersPerPixel = 1e11;

    private double _metersPerPixel;

    public Camera(double metersPerPixel = 1e9)
    {
        MinMetersPerPixel = DefaultMinMetersPerPixel;
        MaxMetersPerPixel = DefaultMaxMetersPerPixel;
        _metersPerPixel = Math.Clamp(metersPerPixel, MinMetersPerPixel, MaxMetersPerPixel);
    }

    /// <summary>World point (meters) shown at the center of the viewport.</summary>
    public Vector2d Center { get; set; } = Vector2d.Zero;

    /// <summary>Current viewport size in pixels, updated each frame from the canvas.</summary>
    public int WidthPx { get; private set; }

    public int HeightPx { get; private set; }

    public double MinMetersPerPixel { get; init; }

    public double MaxMetersPerPixel { get; init; }

    /// <summary>Zoom level, always within [<see cref="MinMetersPerPixel"/>, <see cref="MaxMetersPerPixel"/>].</summary>
    public double MetersPerPixel
    {
        get => _metersPerPixel;
        set => _metersPerPixel = Math.Clamp(value, MinMetersPerPixel, MaxMetersPerPixel);
    }

    /// <summary>Record the current canvas pixel size. Call once per frame before drawing.</summary>
    public void SetViewport(int widthPx, int heightPx)
    {
        WidthPx = widthPx;
        HeightPx = heightPx;
    }

    /// <summary>World meters → canvas pixels (origin top-left, Y down).</summary>
    public (float X, float Y) WorldToScreen(Vector2d world)
    {
        double sx = WidthPx * 0.5 + (world.X - Center.X) / _metersPerPixel;
        double sy = HeightPx * 0.5 - (world.Y - Center.Y) / _metersPerPixel;
        return ((float)sx, (float)sy);
    }

    /// <summary>Canvas pixels → world meters. Inverse of <see cref="WorldToScreen"/>.</summary>
    public Vector2d ScreenToWorld(double xPx, double yPx)
    {
        double wx = Center.X + (xPx - WidthPx * 0.5) * _metersPerPixel;
        double wy = Center.Y - (yPx - HeightPx * 0.5) * _metersPerPixel;
        return new Vector2d(wx, wy);
    }

    /// <summary>
    /// Zoom by <paramref name="factor"/> (&gt;1 zooms out, &lt;1 zooms in) while keeping the world
    /// point currently under the given screen pixel fixed under it — the standard "zoom toward the
    /// cursor" behavior. Pass the viewport center to zoom toward the middle.
    /// </summary>
    public void ZoomBy(double factor, double anchorXPx, double anchorYPx)
    {
        Vector2d anchorWorldBefore = ScreenToWorld(anchorXPx, anchorYPx);
        MetersPerPixel *= factor;
        Vector2d anchorWorldAfter = ScreenToWorld(anchorXPx, anchorYPx);
        // Shift the center so the anchor world point lands back under the same pixel.
        Center += anchorWorldBefore - anchorWorldAfter;
    }

    /// <summary>
    /// Pan the view by a screen-pixel drag delta (e.g. mouse movement while dragging). Dragging the
    /// content right (positive <paramref name="dxPx"/>) moves the camera center left, as expected.
    /// </summary>
    public void PanByPixels(double dxPx, double dyPx)
    {
        Center = new Vector2d(Center.X - dxPx * _metersPerPixel, Center.Y + dyPx * _metersPerPixel);
    }

    /// <summary>Center the camera on a world point (follow-ship mode calls this each frame).</summary>
    public void CenterOn(Vector2d world) => Center = world;
}
