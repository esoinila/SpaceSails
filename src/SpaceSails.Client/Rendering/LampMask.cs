using System;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #708 · THE DARK, ENFORCED AT THE PEN. An <see cref="IRenderer"/> that sits between
/// <see cref="DeckView"/> and the canvas while a DARK floor's world is being drawn, and simply refuses to
/// pass on anything the suit's headlights do not reach.
///
/// <para><b>Why a wrapper and not a hundred `if`s.</b> <c>DeckView.Draw</c> is eight hundred lines of
/// immediate-mode drawing with no drawable-list step to filter — the #371 fog had to grow five inline
/// <c>DarkState</c> checks to gate five kinds of thing, and it still only knows about walls, labels and
/// consoles. A cone that missed one of them would leak a fixture into a black hall and nobody would notice
/// for weeks. Here there is exactly ONE gate and it is the last thing before the ink, so a thing that gets
/// drawn in the dark is a thing that was drawn INSIDE THE CONE, whatever it was and whoever added it.</para>
///
/// <para>It also makes the law assertable rather than merely believable: hand DeckView a recording renderer,
/// draw a dark floor, and the recording contains no primitive outside the light. Not dim — absent.</para>
///
/// <para><b>What this does NOT do is clip.</b> A wall that starts under your feet and runs forty du into the
/// black passes the gate — part of it is lit — and would then be drawn in full. The crisp edge is
/// <see cref="DeckView"/>'s own job, painted over the top once the world is down. Two mechanisms, one each:
/// this one decides what EXISTS, that one decides where it STOPS.</para>
/// </summary>
internal sealed class LampMask(IRenderer inner) : IRenderer
{
    private readonly IRenderer _inner = inner;

    private bool _armed;
    private double _ax, _ay, _heading, _ringDu;
    private float _scale = 1f, _ox, _oy;

    /// <summary>Point the lamp: the captain's world position and facing, the arm's-reach ring, and the
    /// world→screen transform <c>DeckView</c> is drawing with this frame.</summary>
    internal void Arm(double avatarX, double avatarY, double headingRad, double ringDu,
                      float scale, float ox, float oy)
    {
        (_ax, _ay, _heading, _ringDu) = (avatarX, avatarY, headingRad, ringDu);
        (_scale, _ox, _oy) = (scale <= 0 ? 1f : scale, ox, oy);
        _armed = true;
    }

    /// <summary>Put it away. Everything after this reaches the canvas untouched.</summary>
    internal void Disarm() => _armed = false;

    // ── The gate ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Screen pixels back to deck units. <c>DeckView</c>'s own <c>P()</c>, inverted — the y flip is
    /// the canvas convention (deck +y is up, screen +y is down).</summary>
    private (double X, double Y) World(float px, float py) => ((px - _ox) / _scale, (_oy - py) / _scale);

    private bool LitPx(float px, float py)
    {
        (double wx, double wy) = World(px, py);
        return SuitLamp.Lights(_ax, _ay, _heading, wx, wy, _ringDu);
    }

    /// <summary>A disc is lit if its centre or any point on its rim is. Four rim samples: everything drawn
    /// as a circle on this deck is a body, a mark or a blip, none of them wider than a room.</summary>
    private bool LitDisc(float px, float py, float rPx) =>
        LitPx(px, py)
        || LitPx(px + rPx, py) || LitPx(px - rPx, py)
        || LitPx(px, py + rPx) || LitPx(px, py - rPx);

    /// <summary>
    /// A run of points is lit if ANY part of it is — which for a long wall is emphatically not the same as
    /// "either end is". A Hive spine runs the width of the field, so both its ends can be in the black while
    /// the middle of it crosses the cone; testing the vertices alone would have deleted exactly the wall the
    /// captain is standing in front of.
    ///
    /// <para>Cheap rejection first: if the whole edge stays farther from the captain than the lamp throws,
    /// nothing on it can be lit and no sampling is needed. That is most of a floor, most frames.</para>
    /// </summary>
    private bool LitRun(ReadOnlySpan<float> pointsXY, bool closed)
    {
        int n = pointsXY.Length / 2;
        if (n == 0)
        {
            return false;
        }

        if (n == 1)
        {
            return LitPx(pointsXY[0], pointsXY[1]);
        }

        int edges = closed ? n : n - 1;
        for (int e = 0; e < edges; e++)
        {
            int a = e, b = (e + 1) % n;
            float x0 = pointsXY[a * 2], y0 = pointsXY[(a * 2) + 1];
            float x1 = pointsXY[b * 2], y1 = pointsXY[(b * 2) + 1];

            (double wx0, double wy0) = World(x0, y0);
            (double wx1, double wy1) = World(x1, y1);
            if (DistanceToSegmentDu(wx0, wy0, wx1, wy1) > SuitLamp.RangeDu)
            {
                continue;   // the whole edge is out of the lamp's reach, wherever it is pointed
            }

            double len = Math.Sqrt(((wx1 - wx0) * (wx1 - wx0)) + ((wy1 - wy0) * (wy1 - wy0)));
            int steps = Math.Clamp((int)Math.Ceiling(len / SampleStepDu), 1, MaxSamplesPerEdge);
            for (int s = 0; s <= steps; s++)
            {
                double t = (double)s / steps;
                if (SuitLamp.Lights(_ax, _ay, _heading,
                                    wx0 + ((wx1 - wx0) * t), wy0 + ((wy1 - wy0) * t), _ringDu))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>How finely an edge is walked, in deck units. Half the arm's-reach ring the game actually
    /// ships, so the narrowest lit sliver a captain can stand in still catches a sample.</summary>
    private const double SampleStepDu = 1.5;

    /// <summary>…and the ceiling on that walk, so a wall a kilometre long cannot cost a kilometre of
    /// arithmetic. It is only ever reached by an edge far longer than the lamp throws, and such an edge has
    /// already been through the cheap rejection above.</summary>
    private const int MaxSamplesPerEdge = 48;

    private double DistanceToSegmentDu(double x0, double y0, double x1, double y1)
    {
        double vx = x1 - x0, vy = y1 - y0;
        double len2 = (vx * vx) + (vy * vy);
        double t = len2 <= 1e-12 ? 0 : Math.Clamp((((_ax - x0) * vx) + ((_ay - y0) * vy)) / len2, 0, 1);
        double dx = _ax - (x0 + (vx * t)), dy = _ay - (y0 + (vy * t));
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private bool LitRect(float x, float y, float w, float h) =>
        LitPx(x, y) || LitPx(x + w, y) || LitPx(x, y + h) || LitPx(x + w, y + h)
        || LitPx(x + (w / 2f), y + (h / 2f));

    // ── IRenderer ────────────────────────────────────────────────────────────────────────────────────────

    public void BeginFrame(int widthPx, int heightPx, RgbaColor background) =>
        _inner.BeginFrame(widthPx, heightPx, background);

    public void EndFrame() => _inner.EndFrame();

    public int RegisterImage(string url) => _inner.RegisterImage(url);

    public void DrawCircle(float xPx, float yPx, float radiusPx, RgbaColor? fill, RgbaColor stroke,
                           float strokeWidthPx = 1f)
    {
        if (!_armed || LitDisc(xPx, yPx, radiusPx))
        {
            _inner.DrawCircle(xPx, yPx, radiusPx, fill, stroke, strokeWidthPx);
        }
    }

    public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float widthPx = 1f)
    {
        if (!_armed || LitRun(pointsXY, closed: false))
        {
            _inner.DrawPolyline(pointsXY, stroke, widthPx);
        }
    }

    public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke,
                            float strokeWidthPx = 1f)
    {
        if (!_armed || LitRun(pointsXY, closed: true))
        {
            _inner.DrawPolygon(pointsXY, fill, stroke, strokeWidthPx);
        }
    }

    public void DrawText(float xPx, float yPx, string text, RgbaColor color,
                         string font = "12px sans-serif", TextAlign align = TextAlign.Left)
    {
        if (!_armed || LitPx(xPx, yPx))
        {
            _inner.DrawText(xPx, yPx, text, color, font, align);
        }
    }

    public void DrawImage(int imageId, float xPx, float yPx, float widthPx, float heightPx, float alpha = 1f)
    {
        if (!_armed || LitRect(xPx, yPx, widthPx, heightPx))
        {
            _inner.DrawImage(imageId, xPx, yPx, widthPx, heightPx, alpha);
        }
    }

    public void DrawImageSlice(int imageId,
        float srcXFrac, float srcYFrac, float srcWFrac, float srcHFrac,
        float dstXPx, float dstYPx, float dstWPx, float dstHPx, float alpha = 1f)
    {
        if (!_armed || LitRect(dstXPx, dstYPx, dstWPx, dstHPx))
        {
            _inner.DrawImageSlice(imageId, srcXFrac, srcYFrac, srcWFrac, srcHFrac,
                                  dstXPx, dstYPx, dstWPx, dstHPx, alpha);
        }
    }
}
