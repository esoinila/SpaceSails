using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Labels — #402 nav-map label de-collision. The body-name labels (and the deflection threat
// rock's ⚠/name) are no longer drawn straight to the canvas; they are ENQUEUED for the frame, then
// LabelPlacement.Resolve (Core, tested) decides which survive when their screen rects collide. The
// declutter is screen-space, so it is zoom-agnostic; priority decides who yields — the docked station
// and the threat rock always win, depots and minor bodies stand down rather than smear on top.
public partial class Map
{
    // Priority ladder (HIGHER wins a collision). The two that carry the deflection money-moment sit at
    // the top so they are never culled: the inbound THREAT ROCK, then the DOCKED station.
    private const int LabelPriorityThreatRock = 1000;
    private const int LabelPriorityDocked = 900;
    private const int LabelPriorityDestination = 800;
    private const int LabelPriorityArmed = 700;
    private const int LabelPriorityPlanet = 300;
    private const int LabelPriorityHaven = 200;
    private const int LabelPriorityStation = 100; // depots and minor built things yield first

    // One label the frame wants placed. The anchor (X, Y) is the DrawText origin; Width/Height are the
    // estimated screen box used for the overlap test; a survivor may be drawn nudged down by LineHeight.
    private readonly record struct NavLabel(
        float X, float Y, string Text, RgbaColor Color, string Font, int Priority,
        double Width, double Height, double LineHeight);

    private readonly List<NavLabel> _frameLabels = new();

    private void BeginFrameLabels() => _frameLabels.Clear();

    // Enqueue a nav-map label. Width is estimated from the text length and font size (the renderer
    // has no measure-text seam on the C# side) — a slight over-estimate keeps the declutter honest.
    private void EnqueueNavLabel(float x, float y, string text, RgbaColor color, int priority,
        string font = "12px sans-serif")
    {
        double fontPx = ParseFontPx(font);
        _frameLabels.Add(new NavLabel(
            x, y, text, color, font, priority,
            Width: EstimateTextWidth(text, fontPx),
            Height: fontPx * 1.15,
            LineHeight: fontPx * 1.2));
    }

    // The priority a body's name label carries. The docked station and the aimed-at bodies outrank the
    // depots; a plain depot (station, no special role) yields first when the cluster fights for pixels.
    private int BodyLabelPriority(CelestialBody body)
    {
        if (body.Id == _dockedHavenId) return LabelPriorityDocked;
        if (body.Id == _destinationBodyId) return LabelPriorityDestination;
        if (body.Id == _armedOrbitBodyId) return LabelPriorityArmed;
        // A named PORT (a dockable haven or a moon haven) outranks a plain depot, so a real harbor's
        // label never yields to the cargo-pod clutter beside it — only a plain depot/station is the
        // "minor" label the 🗺 depots toggle may hide and the cull sheds first.
        if (IsDockableHaven(body) || body.IsHaven) return LabelPriorityHaven;
        if (body.Kind == BodyKind.Station) return LabelPriorityStation;
        return LabelPriorityPlanet;
    }

    // Resolve the frame's enqueued labels and draw the survivors. Called once per frame AFTER every
    // label producer (DrawCelestialBodies, DrawAsteroidThreat) has enqueued, and before EndFrame.
    private void FlushNavLabels()
    {
        if (_renderer is null || _frameLabels.Count == 0)
        {
            return;
        }

        var candidates = new LabelPlacement.Candidate[_frameLabels.Count];
        for (int i = 0; i < _frameLabels.Count; i++)
        {
            NavLabel l = _frameLabels[i];
            candidates[i] = new LabelPlacement.Candidate(
                Key: i,
                Rect: new LabelPlacement.Rect(l.X, l.Y, l.Width, l.Height),
                Priority: l.Priority,
                LineHeight: l.LineHeight);
        }

        IReadOnlyList<LabelPlacement.Placement> placed = LabelPlacement.Resolve(candidates);
        foreach (LabelPlacement.Placement p in placed)
        {
            if (!p.Draw)
            {
                continue; // lower-priority label yielded — dropped rather than drawn atop (#402)
            }

            NavLabel l = _frameLabels[p.Key];
            // A survivor may have been nudged down by whole line-heights; apply that shift to its origin.
            float drawY = l.Y + (float)(p.Rect.Y - l.Y);
            _renderer.DrawText(l.X, drawY, l.Text, l.Color, l.Font, TextAlign.Left);
        }
    }

    // Rough monospace-ish width estimate: average glyph advance ≈ 0.6 em for sans-serif. Kept slightly
    // generous so near-touching labels are treated as colliding (better to declutter than to smear).
    private static double EstimateTextWidth(string text, double fontPx) => text.Length * fontPx * 0.62;

    // Pull the leading pixel size out of a CSS font string ("12px sans-serif" → 12). Defaults to 12.
    private static double ParseFontPx(string font)
    {
        int i = 0;
        while (i < font.Length && !char.IsDigit(font[i]))
        {
            i++;
        }
        int start = i;
        while (i < font.Length && (char.IsDigit(font[i]) || font[i] == '.'))
        {
            i++;
        }
        return start < i && double.TryParse(
            font.AsSpan(start, i - start), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double px)
            ? px
            : 12.0;
    }
}
