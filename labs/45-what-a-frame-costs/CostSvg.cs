using System.Text;

namespace SpaceSails.Labs.Lab45;

/// <summary>
/// #852 · The one picture this lab needs: what a line-of-sight ask costs as the building grows, drawn twice
/// — once against the plain segment list the sightline predicates are handed today, once against the
/// <c>WallIndex</c> the very same floor already carries for the walkers.
///
/// <para>Hand-rolled SVG rather than LabViz, for Lab 48's reason: <c>SpaceSails.LabViz</c> is a heliocentric
/// trajectory viewer — bodies on rails, paths in metres, an epoch — and it has no idea what a nanosecond is.
/// A lab that bent it into a bar chart would be the wrong tool wearing the right name.</para>
///
/// <para>Dark panel, light ink: the deck plan's own palette, and it reads the same in a light and a dark
/// README. The whole project runs <c>InvariantGlobalization</c>, so the interpolations below are invariant
/// and the file is byte-identical wherever it is generated.</para>
/// </summary>
internal static class CostSvg
{
    private const int W = 780, H = 430;
    private const int L = 92, R = 28, T = 58, B = 62;

    internal static string Draw(
        IReadOnlyList<int> segments,
        IReadOnlyList<double> plainNs,
        IReadOnlyList<double> indexNs,
        IReadOnlyList<(string Name, int Segs)> realFloors,
        int queryCount)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(plainNs);
        ArgumentNullException.ThrowIfNull(indexNs);
        ArgumentNullException.ThrowIfNull(realFloors);

        double maxX = 0, maxY = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            maxX = Math.Max(maxX, segments[i]);
            maxY = Math.Max(maxY, plainNs[i]);
        }
        maxX = Math.Ceiling(maxX / 100) * 100;
        maxY = Math.Ceiling(maxY / 5000) * 5000;

        double Px(double segs) => L + (segs / maxX * (W - L - R));
        double Py(double ns) => H - B - (ns / maxY * (H - T - B));

        var s = new StringBuilder();
        void A(string line) => s.Append(line).Append('\n');

        A($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\" font-family=\"ui-monospace, SFMono-Regular, Menlo, monospace\">");
        A($"  <rect x=\"0\" y=\"0\" width=\"{W}\" height=\"{H}\" fill=\"#0d1117\"/>");
        A($"  <text x=\"{L}\" y=\"28\" fill=\"#e6edf3\" font-size=\"15\">Lab 45 · what one CLEAR line-of-sight ask costs</text>");
        A($"  <text x=\"{L}\" y=\"46\" fill=\"#8b949e\" font-size=\"11\">real Hive wall segments · native Release · the same {queryCount} query pairs at every size</text>");

        for (int g = 0; g <= 4; g++)
        {
            double ns = maxY * g / 4;
            double y = Py(ns);
            A($"  <line x1=\"{L}\" y1=\"{y:F1}\" x2=\"{W - R}\" y2=\"{y:F1}\" stroke=\"#21262d\" stroke-width=\"1\"/>");
            A($"  <text x=\"{L - 8}\" y=\"{y + 4:F1}\" fill=\"#8b949e\" font-size=\"10\" text-anchor=\"end\">{ns / 1000:F0} us</text>");
        }
        A($"  <line x1=\"{L}\" y1=\"{T}\" x2=\"{L}\" y2=\"{H - B}\" stroke=\"#484f58\" stroke-width=\"1\"/>");

        // Where the real building actually sits on this axis — the whole point of the picture.
        foreach ((string name, int segs) in realFloors)
        {
            double x = Px(segs);
            A($"  <line x1=\"{x:F1}\" y1=\"{T}\" x2=\"{x:F1}\" y2=\"{H - B}\" stroke=\"#484f58\" stroke-width=\"1\" stroke-dasharray=\"3 4\"/>");
            A($"  <text x=\"{x + 5:F1}\" y=\"{T + 13}\" fill=\"#8b949e\" font-size=\"10\">{name} · {segs} segs</text>");
        }

        Series(segments, plainNs, "#f0883e");
        Series(segments, indexNs, "#3fb950");

        for (int i = 0; i < segments.Count; i++)
        {
            A($"  <text x=\"{Px(segments[i]):F1}\" y=\"{H - B + 18}\" fill=\"#8b949e\" font-size=\"10\" text-anchor=\"middle\">{segments[i]}</text>");
        }
        A($"  <text x=\"{(L + W - R) / 2}\" y=\"{H - 20}\" fill=\"#8b949e\" font-size=\"11\" text-anchor=\"middle\">wall segments on the floor</text>");

        // Legend, on the lines themselves rather than in a box.
        A($"  <text x=\"{Px(segments[^1]) - 12:F1}\" y=\"{Py(plainNs[^1]) + 26:F1}\" fill=\"#f0883e\" font-size=\"11\" text-anchor=\"end\">plain List&lt;Segment&gt; — what the EYE is handed today (O(walls))</text>");
        A($"  <text x=\"{Px(segments[^1]) - 12:F1}\" y=\"{Py(indexNs[^1]) - 14:F1}\" fill=\"#3fb950\" font-size=\"11\" text-anchor=\"end\">WallIndex — what the LEGS are already handed (flat)</text>");
        A("</svg>");
        return s.ToString();

        void Series(IReadOnlyList<int> xs, IReadOnlyList<double> ys, string colour)
        {
            var pts = new StringBuilder();
            for (int i = 0; i < xs.Count; i++)
            {
                pts.Append($"{Px(xs[i]):F1},{Py(ys[i]):F1} ");
            }
            A($"  <polyline fill=\"none\" stroke=\"{colour}\" stroke-width=\"2\" points=\"{pts}\"/>");
            for (int i = 0; i < xs.Count; i++)
            {
                A($"  <circle cx=\"{Px(xs[i]):F1}\" cy=\"{Py(ys[i]):F1}\" r=\"3.5\" fill=\"{colour}\"/>");
            }
        }
    }
}
