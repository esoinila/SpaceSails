using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1466 · THE WORLD'S WORDS YIELD TO THE HUD BAND. An <see cref="IRenderer"/> between <see cref="DeckView"/> and
/// the canvas that, while armed with the comms band's plate rectangle, dims (alpha x 0.25) any text whose ink box meets it.
/// The band is drawn last, but its plate is translucent, so a room name, the ⚓ or the boxed AIRLOCK under it
/// showed through "bore, then FIRE" on a 390 px phone (the live pass of #1464). Walls, plates and floor art are
/// untouched: only words yield, and only while the band is up. <see cref="DeckView"/> arms it once the band is
/// laid out and disarms it before the band itself is drawn.
/// </summary>
internal sealed class BandYield(IRenderer inner) : IRenderer
{
    private static readonly Regex PxOfFont = new(@"(\d+(?:\.\d+)?)px", RegexOptions.Compiled);

    private readonly IRenderer _inner = inner;
    private readonly List<(double X, double Y, double W, double H)> _plates = [];

    /// <summary>Arms the yield with ONE plate (the comms band's), dropping any other.</summary>
    internal void Arm(double x, double y, double w, double h)
    {
        _plates.Clear();
        Reserve(x, y, w, h);
    }

    /// <summary>R1-F76 · ONE RULE FOR EVERY PLATE THAT LIES OVER THE GLASS. The comms band was the first; the page's
    /// pulse toast (a DOM box laid over the canvas) and the standing prompt above the keybar are the others. Each
    /// publishes the rectangle it covers and the world's words that touch it are dimmed, never dropped.</summary>
    internal void Reserve(double x, double y, double w, double h)
    {
        if (w > 0 && h > 0)
        {
            _plates.Add((x, y, w, h));
        }
    }

    internal void Disarm() => _plates.Clear();

    /// <summary>Whether a line of text hung at this anchor would touch the armed rectangle.</summary>
    internal bool Meets(float x, float y, string text, string font, TextAlign align)
    {
        if (_plates.Count == 0)
        {
            return false;
        }
        // The cheap vertical bound first (no font is taller than 40 px): most labels never reach the regex.
        bool near = false;
        foreach ((double _, double py, double _, double ph) in _plates)
        {
            if (y + 12.0 >= py && y - 40.0 <= py + ph)
            {
                near = true;
                break;
            }
        }
        if (!near)
        {
            return false;
        }
        Match m = PxOfFont.Match(font);
        double px = m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 12.0;
        double width = CommsBand.WidthOf(text, px);
        double left = align switch
        {
            TextAlign.Center => x - (width / 2),
            TextAlign.Right => x - width,
            _ => x,
        };
        double top = y - MarkBand.AscentOf(px), bottom = y + MarkBand.DescentOf(px);
        foreach ((double px0, double py0, double pw, double ph) in _plates)
        {
            if (left < px0 + pw && left + width > px0 && top < py0 + ph && bottom > py0)
            {
                return true;
            }
        }
        return false;
    }

    public void BeginFrame(int widthPx, int heightPx, RgbaColor background) =>
        _inner.BeginFrame(widthPx, heightPx, background);

    public void EndFrame() => _inner.EndFrame();

    public int RegisterImage(string url) => _inner.RegisterImage(url);

    public void DrawCircle(float xPx, float yPx, float radiusPx, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f) =>
        _inner.DrawCircle(xPx, yPx, radiusPx, fill, stroke, strokeWidthPx);

    public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float widthPx = 1f) =>
        _inner.DrawPolyline(pointsXY, stroke, widthPx);

    public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f) =>
        _inner.DrawPolygon(pointsXY, fill, stroke, strokeWidthPx);

    public void DrawText(float xPx, float yPx, string text, RgbaColor color,
                         string font = "12px sans-serif", TextAlign align = TextAlign.Left)
    {
        // DIMMED, not dropped: a label that grazes the plate must not vanish and pop back as the camera moves.
        _inner.DrawText(xPx, yPx, text,
            Meets(xPx, yPx, text, font, align) ? new RgbaColor(color.R, color.G, color.B, (byte)(color.A * 0.25)) : color,
            font, align);
    }

    public void DrawImage(int imageId, float xPx, float yPx, float widthPx, float heightPx, float alpha = 1f) =>
        _inner.DrawImage(imageId, xPx, yPx, widthPx, heightPx, alpha);

    public void DrawImageSlice(int imageId, float srcXFrac, float srcYFrac, float srcWFrac, float srcHFrac,
        float dstXPx, float dstYPx, float dstWPx, float dstHPx, float alpha = 1f) =>
        _inner.DrawImageSlice(imageId, srcXFrac, srcYFrac, srcWFrac, srcHFrac, dstXPx, dstYPx, dstWPx, dstHPx, alpha);
}
