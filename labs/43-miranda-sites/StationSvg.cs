using System.Globalization;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab43;

/// <summary>
/// #531 · Draws a dead station's plan — modules, tubes, and which of those tubes will not let you through.
///
/// <para>The same reason SiteSvg exists: the game's own renderer cannot be screenshotted from an automated
/// tab, so without this the only way to judge whether a generated station READS as a station is to fly out
/// there by hand. A severed tube is drawn in red with its far end capped, so a picture shows at a glance
/// what the A* tests prove — which parts of her are islands.</para>
/// </summary>
internal static class StationSvg
{
    private const double Pad = 8.0;
    private const double Scale = 7.0;

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    internal static string Draw(string stationId)
    {
        (double minX, double minY, double maxX, double maxY) = StationWreck.Bounds;
        double w = (maxX - minX) + (2 * Pad);
        double h = (maxY - minY) + (2 * Pad);

        double PX(double x) => (x - minX + Pad) * Scale;
        double PY(double y) => (maxY - y + Pad) * Scale;   // flip: +y is up on screen

        var s = new StringBuilder();
        s.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(w * Scale)}\" height=\"{N(h * Scale)}\" ");
        s.Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {N(w * Scale)} {N(h * Scale)}\">");
        s.Append("<rect width=\"100%\" height=\"100%\" fill=\"#0a0e16\"/>");
        s.Append("<g font-family=\"monospace\">");

        // Every wall the collision system will actually use.
        foreach (SurfaceCollision.Segment seg in StationWreck.Walls(stationId))
        {
            // One interpolated string, not a concatenation: `$"..." + "..."` stops being an interpolated
            // string handler and StringBuilder.Append(IFormatProvider, …) no longer resolves.
            s.Append(CultureInfo.InvariantCulture,
                $"<line x1=\"{N(PX(seg.X1))}\" y1=\"{N(PY(seg.Y1))}\" x2=\"{N(PX(seg.X2))}\" y2=\"{N(PY(seg.Y2))}\" stroke=\"#aab9cd\" stroke-width=\"2\"/>");
        }

        // Module names, and a red brand on the ones you cannot walk to.
        var severed = new System.Collections.Generic.HashSet<StationWreck.ModuleId>(
            StationWreck.SeveredTubes(stationId));

        foreach (StationWreck.Module m in StationWreck.Modules)
        {
            bool stranded = severed.Contains(m.Id);
            string ink = stranded ? "#ff6b5a" : "#9fd8c8";
            Text(s, PX(m.CentreX), PY(m.CentreY), ink, 11, m.Name);
            if (stranded)
            {
                Text(s, PX(m.CentreX), PY(m.CentreY) + 14, "#ff6b5a", 9, "— NO WAY THROUGH —");
            }
        }

        // Ways in: a ring for a lock that cycles, a cross for a face you have to cut.
        foreach (StationWreck.Access a in StationWreck.Accesses(stationId))
        {
            bool cut = a.Kind == StationWreck.AccessKind.CutFace;
            string ink = cut ? "#ffb46b" : "#7fd8c8";
            if (cut)
            {
                string d = $"M{N(PX(a.X) - 5)},{N(PY(a.Y) - 5)} L{N(PX(a.X) + 5)},{N(PY(a.Y) + 5)} M{N(PX(a.X) + 5)},{N(PY(a.Y) - 5)} L{N(PX(a.X) - 5)},{N(PY(a.Y) + 5)}";
                s.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"{d}\" stroke=\"{ink}\" stroke-width=\"2.5\"/>");
            }
            else
            {
                s.Append(CultureInfo.InvariantCulture,
                    $"<circle cx=\"{N(PX(a.X))}\" cy=\"{N(PY(a.Y))}\" r=\"5\" fill=\"none\" stroke=\"{ink}\" stroke-width=\"2.5\"/>");
            }
        }

        Text(s, 10, 18, "#e6edf5", 14, Escape(stationId.ToUpperInvariant()), "start");
        Text(s, 10, 34, "#7f8ea3", 11,
            $"{StationWreck.WalkGroups(stationId).Count} walk-groups · ○ lock cycles · ✕ cut your way in", "start");

        s.Append("</g></svg>");
        return s.ToString();
    }

    private static void Text(
        StringBuilder s, double x, double y, string fill, int px, string body, string anchor = "middle") =>
        s.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{N(x)}\" y=\"{N(y)}\" fill=\"{fill}\" font-size=\"{px}\" text-anchor=\"{anchor}\">{Escape(body)}</text>");

    private static string Escape(string t) =>
        t.Replace("&", "&amp;", System.StringComparison.Ordinal)
         .Replace("<", "&lt;", System.StringComparison.Ordinal)
         .Replace(">", "&gt;", System.StringComparison.Ordinal);
}
