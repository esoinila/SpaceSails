namespace SpaceSails.Client.Rendering;

/// <summary>
/// Screen-space 2D drawing surface. The renderer is deliberately dumb: it knows nothing about
/// world coordinates, meters, or the camera. Callers convert world positions to pixels with
/// <see cref="Camera"/> first, then issue draw calls in <em>screen space</em> (pixels, origin
/// top-left, Y down — canvas convention).
///
/// This split is load-bearing for precision and performance:
/// <list type="bullet">
///   <item>All solar-system-scale math (meters, doubles) stays in C#; JavaScript only ever sees
///   small pixel floats, so float precision is never a problem across interop.</item>
///   <item>Implementations are expected to <em>batch</em> every primitive issued between
///   <see cref="BeginFrame"/> and <see cref="EndFrame"/> into a single interop call, so hundreds
///   of polylines per frame cost one round-trip, not hundreds.</item>
/// </list>
/// </summary>
public interface IRenderer
{
    /// <summary>Start a frame: record the canvas pixel size and clear to <paramref name="background"/>.</summary>
    void BeginFrame(int widthPx, int heightPx, RgbaColor background);

    /// <summary>Filled and/or stroked circle. A null fill or a zero-width stroke skips that part.</summary>
    void DrawCircle(float xPx, float yPx, float radiusPx, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f);

    /// <summary>
    /// Open polyline through <paramref name="pointsXY"/>, laid out as interleaved x,y pixel pairs
    /// (length must be even). Used for orbits and trajectory ribbons — the hot path at 500+ per frame.
    /// </summary>
    void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float widthPx = 1f);

    /// <summary>
    /// Closed polygon through <paramref name="pointsXY"/> (interleaved x,y pixel pairs; closed
    /// automatically). A null fill or a zero-width stroke skips that part. Used for area overlays
    /// (trade lanes, scan wedges) — a handful per frame, not a hot path.
    /// </summary>
    void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f);

    /// <summary>Text anchored at a screen point. Font is a CSS font string, e.g. "12px sans-serif".</summary>
    void DrawText(float xPx, float yPx, string text, RgbaColor color, string font = "12px sans-serif", TextAlign align = TextAlign.Left);

    /// <summary>Registers a raster image URL for drawing, returning a stable integer id. Idempotent
    /// per URL — call it every frame if you like; the load happens once. The image decodes
    /// asynchronously in JS, so <see cref="DrawImage"/> calls before it finishes simply draw nothing
    /// that frame, then it fades in once decoded.</summary>
    int RegisterImage(string url);

    /// <summary>Blits a registered image (see <see cref="RegisterImage"/>) into a screen-space rect,
    /// scaled to fill it, at <paramref name="alpha"/> (0..1). An unknown or not-yet-loaded id draws
    /// nothing. Used for interior room backdrops and, later, first-person wall textures — drawn
    /// BEFORE the vector overlays so consoles, avatar, and HUD stay legible on top.</summary>
    void DrawImage(int imageId, float xPx, float yPx, float widthPx, float heightPx, float alpha = 1f);

    /// <summary>Blits a sub-rectangle of a registered image into a screen-space rect. Source coords are
    /// <em>normalized</em> fractions of the image (0..1) so the caller never needs the texture's pixel
    /// size — the renderer multiplies by the decoded bitmap's natural size. This is the raycaster's
    /// textured-column primitive: sample a thin vertical strip of a wall texture (sx = along-the-wall
    /// fraction, full height) into one screen column. An unknown/not-yet-loaded id draws nothing.</summary>
    void DrawImageSlice(int imageId,
        float srcXFrac, float srcYFrac, float srcWFrac, float srcHFrac,
        float dstXPx, float dstYPx, float dstWPx, float dstHPx, float alpha = 1f);

    /// <summary>Finish the frame and flush all batched primitives to the canvas in one interop call.</summary>
    void EndFrame();
}

/// <summary>Horizontal text anchoring for <see cref="IRenderer.DrawText"/>.</summary>
public enum TextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>
/// Straight 8-bit RGBA color. Kept as a tiny struct (not a CSS string) so the batched command
/// buffer stays numeric; the renderer packs it to a canvas color once per draw.
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    public static RgbaColor FromHex(uint rgb, byte a = 255) =>
        new((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), a);

    /// <summary>Packed 0xAARRGGBB, convenient for shipping colors across interop as a single int.</summary>
    public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
}
