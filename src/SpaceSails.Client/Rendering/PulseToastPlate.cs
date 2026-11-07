using System;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// R1-F76 · WHERE THE PULSE TOAST LIES ON THE GLASS. The toast is a DOM box (<c>.deck-pulse-toast</c>, Map.razor.css)
/// laid over the canvas, so the pen cannot see it and the world drew its words straight under it: the floor plate
/// ("B24 · THE BERTH OFFICE", "-426 m"), the lift sign. <see cref="BandYield"/> needs the rectangle, so it is
/// worked out here from the same numbers the stylesheet states (top 6rem or, with a card open, the foot of the glass;
/// max-width min(40rem, 90vw), and left: 50% so never wider than half the glass; 1rem x 0.5rem padding; 16 px at 1.5 line height; one ellipsised row when low).
/// A little over-measured on purpose: a plate that came up short is the very overprint this exists to stop.
/// </summary>
internal static class PulseToastPlate
{
    private const double Rem = 16.0, LinePx = 16.0, LineStep = 24.0, PadX = 16.0, PadY = 8.0, Border = 2.0;

    /// <summary>The toast's rectangle (x, y, w, h) on a glass of this size, for this line.</summary>
    internal static (double X, double Y, double W, double H) Of(string text, bool low, double glassW, double glassH)
    {
        // left: 50% leaves the box half the glass to shrink-to-fit in, so the cap is the least of the three.
        double maxW = Math.Min(40.0 * Rem, 0.5 * glassW);
        double inner = maxW - (2 * PadX) - Border;
        double textW = CommsBand.WidthOf(text, LinePx) * (0.55 / CommsBand.GlyphWidthRatio);   // bold sans, not monospace
        if (low)
        {
            double lw = Math.Min(maxW, textW + (2 * PadX) + Border);
            double lh = LineStep + (2 * 0.2 * Rem) + Border;
            return ((glassW - lw) / 2.0, glassH - lh, lw, lh);
        }
        int lines = Math.Max(1, (int)Math.Ceiling(textW / (inner * 0.95)));
        double w = lines == 1 ? Math.Min(maxW, textW + (2 * PadX) + Border) : maxW;
        double h = (lines * LineStep) + (2 * PadY) + Border;
        return ((glassW - w) / 2.0, 6.0 * Rem, w, h);
    }
}
