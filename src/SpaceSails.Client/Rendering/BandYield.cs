using System;
using System.Globalization;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1466 · THE WORLD'S WORDS YIELD TO THE HUD BAND. An <see cref="IRenderer"/> between <see cref="DeckView"/> and
/// the canvas that, while armed with the comms band's plate rectangle, refuses any text whose ink box meets it.
/// The band is drawn last, but its plate is translucent, so a room name, the ⚓ or the boxed AIRLOCK under it
/// showed through "bore, then FIRE" on a 390 px phone (the live pass of #1464). Walls, plates and floor art are
/// untouched: only words yield, and only while the band is up. <see cref="DeckView"/> arms it once the band is
/// laid out and disarms it before the band itself is drawn.
/// </summary>
internal sealed class BandYield(IRenderer inner) : IRenderer
{
    private static readonly Regex PxOfFont = new(@"(\d+(?:\.\d+)?)px", RegexOptions.Compiled);

    private readonly IRenderer _inner = inner;
    private bool _armed;
    private double _x, _y, _w, _h;

    internal void Arm(double x, double y, double w, double h)
    {
        _armed = w > 0 && h > 0;
        (_x, _y, _w, _h) = (x, y, w, h);
    }

    internal void Disarm() => _armed = false;

    /// <summary>Whether a line of text hung at this anchor would touch the armed rectangle.</summary>
    internal bool Meets(float x, float y, string text, string font, TextAlign align)
    {
        if (!_armed)
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
        return left < _x + _w && left + width > _x && top < _y + _h && bottom > _y;
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
        if (!Meets(xPx, yPx, text, font, align))
        {
            _inner.DrawText(xPx, yPx, text, color, font, align);
        }
    }

    public void DrawImage(int imageId, float xPx, float yPx, float widthPx, float heightPx, float alpha = 1f) =>
        _inner.DrawImage(imageId, xPx, yPx, widthPx, heightPx, alpha);

    public void DrawImageSlice(int imageId, float srcXFrac, float srcYFrac, float srcWFrac, float srcHFrac,
        float dstXPx, float dstYPx, float dstWPx, float dstHPx, float alpha = 1f) =>
        _inner.DrawImageSlice(imageId, srcXFrac, srcYFrac, srcWFrac, srcHFrac, dstXPx, dstYPx, dstWPx, dstHPx, alpha);
}
