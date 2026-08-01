using System.Globalization;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab44;

/// <summary>
/// #589 · One floor of the Hive, drawn.
///
/// <para>Why this exists: the A* audit can tell you THAT a room is sealed and cannot tell you WHY. #587 was
/// three coordinates in a test message and an evening of one-build-at-a-time guessing, and the answer — a
/// wall lying across two mouths on the spine's top face — is the sort of thing you see in a quarter of a
/// second in a picture. This is the picture.</para>
///
/// <para>Everything drawn here is READ from the shipping objects: the walls and mouths from
/// <see cref="UndergroundComplex"/>, the wash from the client's own
/// <see cref="DeckPlan.CollisionField"/> flooded from <see cref="HiveInterior.SpawnOn"/>. A lab that
/// re-implements the geometry so it can be pictured is drawing a building the game does not have — that is
/// exactly the failure this project keeps paying for, and it is the worst possible place to have it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal static class FloorSvg
{
    private const double Pad = 6.0;      // deck units of margin around the field
    private const double Scale = 3.2;    // pixels per deck unit — a floor is 310 x 253 du

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    internal static string Draw(string body, int level, in SurfaceLayout.Field env, FloorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, env);

        // Copied out of the `in` parameter so the local projection functions may close over them.
        double leftX = env.LeftX, rightX = env.RightX, bottomY = env.BottomY, bandY = env.LandingBandY;

        double w = (rightX - leftX) + (2 * Pad);
        double h = (bandY - bottomY) + (2 * Pad) + 14;   // headroom for the caption

        // Deck units -> pixels. Y is flipped: the field runs from the landing band DOWN to BottomY, and on
        // screen shallow is at the top.
        double PX(double x) => (x - leftX + Pad) * Scale;
        double PY(double y) => (bandY - y + Pad + 14) * Scale;

        var s = new StringBuilder();
        s.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(w * Scale)}\" height=\"{N(h * Scale)}\" ");
        s.Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {N(w * Scale)} {N(h * Scale)}\">");
        s.Append("<rect width=\"100%\" height=\"100%\" fill=\"#0a0e16\"/>");
        s.Append("<g font-family=\"monospace\">");

        // ── LAYER 1 · THE WASH. Everywhere the captain can actually stand, flooded from the lift doors over
        //    the client's real collision field. This is the layer the whole lab is for: a sealed room is an
        //    island nobody coloured in, and it is visible at a glance from across the room.
        //
        //    Drawn UNDER the walls, as merged horizontal runs rather than one square per grid cell — a floor
        //    floods to roughly twenty thousand cells and an SVG with twenty thousand rects in it is a file
        //    nobody opens twice.
        foreach ((double x1, double x2, double y) in Runs(report.Wash, report.Step))
        {
            double half = report.Step / 2;
            s.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{N(PX(x1 - half))}\" y=\"{N(PY(y + half))}\" width=\"{N((x2 - x1 + report.Step) * Scale)}\" height=\"{N(report.Step * Scale)}\" fill=\"#1c4f47\"/>");
        }

        // ── LAYER 2 · THE SPINE'S OWN LINES, ghosted, so the mouths can be read against them. These are not
        //    walls — they are where the two long faces RUN, and #587 lived in the difference between a face
        //    and the mouths cut into it.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(env);
        double corridor = UndergroundComplex.CorridorHalf;
        foreach (double faceY in new[] { shaftY - corridor, shaftY + corridor })
        {
            Line(s, PX(leftX), PY(faceY), PX(rightX), PY(faceY),
                "stroke=\"#233448\" stroke-width=\"1\" stroke-dasharray=\"3 5\"");
        }

        // ── LAYER 3 · EVERY MOUTH THAT WAS CUT, marked on the face it was cut into — the ribs from the plan
        //    itself (FloorPlan.Ribs, #587) plus the lift alcove. Marked whether or not it is open, which is
        //    the point: a cross next to a mark is a mouth that was walled over again.
        foreach (UndergroundComplex.Rib rib in floor.Ribs)
        {
            double my = rib.Down ? shaftY - corridor : shaftY + corridor;
            Mouth(s, PX(rib.X - corridor), PX(rib.X + corridor), PY(my));
        }
        Mouth(s, PX(shaftX - corridor), PX(shaftX + corridor), PY(shaftY + corridor));

        // ── LAYER 4 · THE STRUCTURE. Everything down here is poured, welded and bolted, so it draws in the
        //    ship's own hull ink, exactly as HiveInterior draws it (#585).
        foreach (SurfaceLayout.Wall wall in floor.Walls)
        {
            Line(s, PX(wall.X1), PY(wall.Y1), PX(wall.X2), PY(wall.Y2),
                "stroke=\"#aab9cd\" stroke-width=\"1.6\" stroke-linecap=\"square\"");
        }

        // ── LAYER 5 · THE DOORS. Open ones in imported violet (the one colour down here that was flown in),
        //    sealed ones in the amber of a sign you may read and not pass.
        foreach (SurfaceLayout.Doorway d in floor.Doorways)
        {
            Line(s, PX(d.X1), PY(d.Y1), PX(d.X2), PY(d.Y2),
                "stroke=\"#b48ee8\" stroke-width=\"3\" stroke-linecap=\"round\"");
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            Line(s, PX(l.X1), PY(l.Y1), PX(l.X2), PY(l.Y2),
                "stroke=\"#e0a070\" stroke-width=\"3\" stroke-linecap=\"round\"");
        }

        // ── LAYER 6 · THE ROOMS, and the verdict on each one. A filled dot is a room the captain can walk
        //    to; a hollow ring with a cross through it is a room that is drawn, offers 🔦 SEARCH THE ROOM,
        //    and cannot be entered. That is #587's whole symptom, made visible.
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            (double rx, double ry) = floor.RoomCentres[i];
            bool ok = report.RoomReached[i];
            string dot = ok ? "fill=\"#7fd8c8\"" : "fill=\"none\" stroke=\"#ff6b6b\" stroke-width=\"1.6\"";
            s.Append(CultureInfo.InvariantCulture,
                $"<circle cx=\"{N(PX(rx))}\" cy=\"{N(PY(ry))}\" r=\"3.4\" {dot}/>");
            if (!ok)
            {
                Line(s, PX(rx) - 5, PY(ry) - 5, PX(rx) + 5, PY(ry) + 5, "stroke=\"#ff6b6b\" stroke-width=\"1.6\"");
                Line(s, PX(rx) - 5, PY(ry) + 5, PX(rx) + 5, PY(ry) - 5, "stroke=\"#ff6b6b\" stroke-width=\"1.6\"");
            }
        }

        // ── LAYER 7 · THE LIFT, and the spot the doors put you on. If the wash does not touch this, nothing
        //    else in the picture matters — the captain is sealed in a building on a dead floor.
        (double sx, double sy) = HiveInterior.SpawnOn(env);
        s.Append(CultureInfo.InvariantCulture,
            $"<circle cx=\"{N(PX(sx))}\" cy=\"{N(PY(sy))}\" r=\"3\" fill=\"#f0d070\"/>");

        // The floor's own labels — including its 🛗 LIFT — come from Core. Nothing here writes a second one:
        // a caption the lab invents is a caption that can disagree with the game's.
        foreach (SurfaceLayout.Landmark m in floor.Labels)
        {
            Text(s, PX(m.X), PY(m.Y), "#d8c58a", 9, Escape(m.Label), "start");
        }

        // ── THE CAPTION — the same numbers the stdout table prints, so a picture pulled out of labviz/ in
        //    six months still says what it is.
        // #592 · The FLOOR's kind, not the site's. Caught by this lab's own picture: an unlisted floor was
        // rendering with the title of the building above it — a caption saying one thing while the doors in
        // the same picture read another, which is the exact failure this lab exists to make visible.
        UndergroundComplex.Kind kind = UndergroundComplex.KindOn(body, level);
        string unlisted = UndergroundComplex.IsUnlisted(body, level) ? "  🕳 UNLISTED" : "";
        Text(s, 10, 18, "#e6edf5", 13,
            $"{Escape(body.ToUpperInvariant())} {Escape(floor.Name)} — {Escape(UndergroundComplex.TitleOf(kind))}" +
            $"{Escape(unlisted)}",
            "start");
        Text(s, 10, 34, report.AllReached ? "#7f8ea3" : "#ff6b6b", 11,
            $"{report.RoomsReached}/{report.Rooms} rooms reachable · lift {(report.LiftReached ? "reachable" : "SEALED")} · " +
            $"{floor.Walls.Count} segments · {floor.Locked.Count} sealed doors · " +
            $"{(floor.Pressurised ? "holds pressure" : "dead air")}",
            "start");

        s.Append("</g></svg>");
        return s.ToString();
    }

    /// <summary>A mouth the generator cut, marked on its face. Drawn under the walls, so a mouth that is
    /// still open shows this mark and a mouth that was walled over shows the wall lying on top of it.</summary>
    private static void Mouth(StringBuilder s, double px1, double px2, double py) =>
        Line(s, px1, py, px2, py, "stroke=\"#4fc3f7\" stroke-width=\"6\" stroke-linecap=\"butt\"");

    /// <summary>Merge the flooded cells into horizontal runs, one per grid row. Twenty thousand rects
    /// becomes a few hundred, and the picture is identical.</summary>
    private static List<(double X1, double X2, double Y)> Runs(
        IReadOnlyCollection<DeckReachability.Point> wash, double step)
    {
        var byRow = new Dictionary<long, List<double>>();
        foreach (DeckReachability.Point p in wash)
        {
            long key = (long)Math.Round(p.Y / step);
            if (!byRow.TryGetValue(key, out List<double>? xs))
            {
                xs = [];
                byRow[key] = xs;
            }
            xs.Add(p.X);
        }

        var runs = new List<(double, double, double)>();
        foreach ((long key, List<double> xs) in byRow)
        {
            xs.Sort();
            double y = key * step;
            double start = xs[0], prev = xs[0];
            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] - prev > step * 1.5)
                {
                    runs.Add((start, prev, y));
                    start = xs[i];
                }
                prev = xs[i];
            }
            runs.Add((start, prev, y));
        }
        return runs;
    }

    private static void Line(StringBuilder s, double x1, double y1, double x2, double y2, string ink) =>
        s.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" {ink}/>");

    private static void Text(
        StringBuilder s, double x, double y, string fill, int px, string body, string anchor = "middle") =>
        s.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{N(x)}\" y=\"{N(y)}\" fill=\"{fill}\" font-size=\"{px}\" text-anchor=\"{anchor}\">{body}</text>");

    private static string Escape(string t) =>
        t.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal);
}
