namespace SpaceSails.Core;

/// <summary>
/// #986 F2 · THE TOP-CENTRE BAND THE SHIP'S OWN LINES OWN.
///
/// <para>#327 paints the mothership's orbit line plainly across the top centre of the walked frame, and the
/// comment three lines above it says <i>"Never buried (the #324 visibility law)"</i>. It was not true. The
/// line was drawn as bare text on bare pixels while every other line the law protects — the NERVE gauge, the
/// #612 AIR bar and its source chip — sits on a dark backing plate. On
/// <c>/map?dock=the-tilt&amp;site=0&amp;land=1</c> the sentry at the airlock projects to the top centre of the
/// screen and its alarm-red magazine readout struck straight through the words <i>"holds the ship,"</i>.</para>
///
/// <para><b>Two readers, one measurement.</b> A plate alone would only trade one buried line for another: the
/// counter is drawn <i>"ON the grid, not a corner widget — meant to be read from across the map"</i>, and
/// putting an opaque plate over it would bury THAT. So this band is both:</para>
/// <list type="bullet">
///   <item>what <c>DeckView.DrawTheInstruments</c> paints its plate to, so the ship's lines are legible over
///   whatever the world happens to have under them; and</item>
///   <item>what <c>DeckView.DrawTheSentries</c> keeps out of, so a magazine readout that would reach into the
///   band is seated below the band instead of above its bot.</item>
/// </list>
///
/// <para><see cref="ReservedBottom"/> is measured for <see cref="MaxLines"/> whether or not the second line is
/// up, on purpose: #825's stall banner comes and goes with the frame rate, and a counter that drifted in and
/// out of the band as the machine complained would be a worse instrument than one that simply sits below it.</para>
/// </summary>
public static class CommsBand
{
    /// <summary>The declared font size of both lines — <c>"13px monospace"</c> in <c>DeckView</c>.</summary>
    public const double LinePx = 13.0;

    /// <summary>The orbit line's baseline (#327). The ship's own line is the first one.</summary>
    public const double FirstBaselineY = 20.0;

    /// <summary>#825 · How far the machine's own banner steps down when the ship is already talking, so
    /// neither fact ever paints over the other.</summary>
    public const double LineStepPx = 18.0;

    /// <summary>How many lines the band ever holds: the ship's, and the machine's under it.</summary>
    public const int MaxLines = 2;

    /// <summary>The margin the plate keeps around the ink it backs — the same idiom the nerve plate keeps at
    /// its own edges (<see cref="HudColumn.PlateInsetPx"/>), a little tighter because this one is a single
    /// row of text and not a stack of instruments.</summary>
    public const double PlateInsetPx = 5.0;

    /// <summary>How far a 13px monospace line reaches above its own baseline. A whole em rather than a cap
    /// height: the plate may only ever be too generous, never too small.</summary>
    public const double AscentPx = LinePx;

    /// <summary>…and below it, for the descenders in "spent on keeping".</summary>
    public const double DescentPx = 4.0;

    /// <summary>The advance width of one monospace glyph at <see cref="LinePx"/>. The same 0.62 the #612 air
    /// chip measures its own plate with (<c>chip.Length * 6.2f</c> at 10px).</summary>
    public const double GlyphWidthRatio = 0.62;

    /// <summary>Where the n-th line's baseline sits (0 = the ship's own line).</summary>
    public static double BaselineY(int line) => FirstBaselineY + (line * LineStepPx);

    /// <summary>The plate's top edge — the first line's ascent plus the plate's own inset.</summary>
    public static double PlateTop => FirstBaselineY - AscentPx - PlateInsetPx;

    /// <summary>The plate's bottom edge for a band actually holding <paramref name="lines"/> lines.</summary>
    public static double PlateBottom(int lines) =>
        BaselineY(System.Math.Max(lines, 1) - 1) + DescentPx + PlateInsetPx;

    /// <summary>The plate's height for a band actually holding <paramref name="lines"/> lines.</summary>
    public static double PlateHeight(int lines) => PlateBottom(lines) - PlateTop;

    /// <summary>THE BAND NOTHING ON THE GRID MAY REACH INTO — measured for the full <see cref="MaxLines"/>,
    /// so what the grid may draw does not change with what the ship happens to be saying.</summary>
    public static double ReservedBottom => PlateBottom(MaxLines);

    /// <summary>How wide a line of the band's text draws, in pixels. Anything outside plain ASCII (the ⚓ the
    /// orbit line opens with, the 📡/⚙ a banner may carry) is counted at a full em, because a monospace face
    /// renders those about half again as wide as a letter and a plate that came up short would be the very
    /// thing this class exists to stop.</summary>
    public static double WidthOf(string text) => WidthOf(text, LinePx);

    /// <inheritdoc cref="WidthOf(string)"/>
    /// <param name="text">The line.</param>
    /// <param name="px">The declared font size it is drawn at — the sentry's magazine digits are drawn far
    /// larger than the band's own lines, and a guard that has to compare the two rows needs both measured the
    /// same way.</param>
    public static double WidthOf(string text, double px)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0.0;
        }
        double w = 0.0;
        foreach (char c in text)
        {
            // Surrogates arrive in pairs and draw as one glyph: count the lead, skip the trail.
            if (char.IsLowSurrogate(c))
            {
                continue;
            }
            w += c < 0x2000 ? px * GlyphWidthRatio : px;
        }
        return w;
    }

    /// <summary>The plate a centred band of <paramref name="lines"/> needs at <paramref name="centreX"/>,
    /// wide enough for its widest line.</summary>
    public static (double X, double Y, double W, double H) PlateFor(double centreX, params string?[] lines)
    {
        int used = 0;
        double widest = 0.0;
        foreach (string? line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            used++;
            widest = System.Math.Max(widest, WidthOf(line));
        }
        if (used == 0)
        {
            return (0, 0, 0, 0);
        }
        double w = widest + (2 * PlateInsetPx);
        return (centreX - (w / 2), PlateTop, w, PlateHeight(used));
    }
}
