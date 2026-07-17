namespace SpaceSails.Core;

/// <summary>
/// Where a floating context menu should sit so it never spills past the map viewport (#253).
///
/// <para>The owner's playtest: clicking The Tilt at the bottom-right of the screen opened its menu
/// running off the bottom — "Set destination" showed, the rest (the 🚀 Long haul entry) rendered
/// off-screen. The rule is the standard one every OS context menu follows: open down-right of the
/// click, but flip <em>above</em> the click near the bottom edge and <em>left</em> of it near the
/// right edge, so the whole box always lands on screen.</para>
///
/// <para>Pure geometry, kept out of the razor so it can be unit-tested: the caller measures the box
/// (the CSS max-width, and a row-height estimate × the visible rows — deterministic, no interop)
/// and hands it the click anchor and the canvas size.</para>
/// </summary>
public static class MenuLayout
{
    /// <summary>The gap the menu opens to the right of (and, when it fits, below) the click — the
    /// small offset the map menus already used, now applied through the flip logic.</summary>
    public const double DefaultOffsetPx = 14;

    /// <summary>Breathing room kept between the menu box and every viewport edge.</summary>
    public const double DefaultMarginPx = 6;

    /// <summary>
    /// The on-screen top-left corner for a menu anchored at (<paramref name="anchorX"/>,
    /// <paramref name="anchorY"/>). Opens down-right of the anchor; flips left of it when the box
    /// would cross the right edge and above it when it would cross the bottom, then clamps as a final
    /// backstop so a box larger than the viewport still pins to the top-left rather than vanishing.
    /// </summary>
    /// <param name="anchorX">Click X in canvas pixels (0 = left edge).</param>
    /// <param name="anchorY">Click Y in canvas pixels (0 = top edge).</param>
    /// <param name="menuWidth">Measured/estimated menu width in pixels.</param>
    /// <param name="menuHeight">Measured/estimated menu height in pixels.</param>
    /// <param name="viewportWidth">Canvas width in pixels.</param>
    /// <param name="viewportHeight">Canvas height in pixels.</param>
    /// <param name="offset">Gap opened to the right of / below the anchor.</param>
    /// <param name="margin">Room kept from each edge.</param>
    public static (double X, double Y) ClampMenuPosition(
        double anchorX,
        double anchorY,
        double menuWidth,
        double menuHeight,
        double viewportWidth,
        double viewportHeight,
        double offset = DefaultOffsetPx,
        double margin = DefaultMarginPx)
    {
        // Horizontal: to the right of the click by default; flip to its left when that overflows.
        double x = anchorX + offset;
        if (x + menuWidth > viewportWidth - margin)
        {
            x = anchorX - offset - menuWidth;
        }

        // Vertical: level with the click by default; flip above it when the bottom would overflow.
        double y = anchorY;
        if (y + menuHeight > viewportHeight - margin)
        {
            y = anchorY - menuHeight;
        }

        // Backstop: keep the top-left on screen even if a flip (or an oversize box) pushed it off.
        // Math.Max guards the degenerate case where the menu is wider/taller than the viewport.
        x = Math.Clamp(x, margin, Math.Max(margin, viewportWidth - menuWidth - margin));
        y = Math.Clamp(y, margin, Math.Max(margin, viewportHeight - menuHeight - margin));
        return (x, y);
    }
}
