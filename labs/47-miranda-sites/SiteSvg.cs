using System.Globalization;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab47;

/// <summary>
/// Draws a landing site's generated ground as a standalone SVG.
///
/// <para>Why this exists: the game's own renderer cannot be screenshotted from an automated browser tab.
/// A descent runs several synchronous blocks that yield between phases waiting for repaints, and a tab
/// driven by automation counts as hidden — requestAnimationFrame is throttled, the repaints never come,
/// and <c>?land=1</c> hangs mid-descent. So "boot every scene and check the parts are in the right place",
/// which is how nearly every expensive bug in this project has actually been caught, could not be done
/// to landing sites at all.</para>
///
/// <para>This draws the same segments the game draws, in the same ink rules (#563: hull is bright and
/// thick, inner lines dim, the field's envelope is UNSEEN and therefore ghosted here rather than drawn),
/// so a picture from this file and a screenshot from the game should show the same ground.</para>
/// </summary>
internal static class SiteSvg
{
    private const double Pad = 6.0;      // deck units of margin around the field
    private const double Scale = 9.0;    // pixels per deck unit

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    internal static string Draw(
        string body,
        LandingSite site,
        in SurfaceLayout.Field env,
        SurfaceLayout.Plan plan,
        double tubeLeft,
        double tubeRight)
    {
        // Copied out of the `in` parameter so the local projection functions may close over them.
        double leftX = env.LeftX, rightX = env.RightX, topY = env.TopY, bottomY = env.BottomY;
        double bandY = env.LandingBandY;

        double w = (rightX - leftX) + (2 * Pad);
        double h = (topY - bottomY) + (2 * Pad);

        // Deck units -> pixels. Y is flipped: the field runs from TopY (shallow, near the ship) DOWN to
        // BottomY (the deep), and on screen shallow is at the top.
        double PX(double x) => (x - leftX + Pad) * Scale;
        double PY(double y) => (topY - y + Pad) * Scale;

        var s = new StringBuilder();
        s.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(w * Scale)}\" height=\"{N(h * Scale)}\" ");
        s.Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {N(w * Scale)} {N(h * Scale)}\">");
        s.Append("<rect width=\"100%\" height=\"100%\" fill=\"#0a0e16\"/>");
        s.Append("<g font-family=\"monospace\">");

        // #563 · TERRAIN — crater rims, scree, scarps and rilles. Drawn, never collided.
        foreach (SurfaceScenery.Mark m in SurfaceScenery.For(body, site.LayoutSalt, env))
        {
            Line(s, PX(m.X1), PY(m.Y1), PX(m.X2), PY(m.Y2),
                "stroke=\"#4a463f\" stroke-width=\"1.3\"");
        }

        // #563 · THE ACTUAL BOUND — read from SurfaceEdge, the same function the collision walls are built
        // from, NOT a copy of it. An earlier version of this file re-implemented the shape here so it could
        // be pictured; that is precisely the "the drawing says one thing and the sim does another" failure
        // this project keeps paying for, and an invisible bound is the worst place to have it. In game these
        // segments are UNSEEN — they stop you and are never drawn — so they are ghosted here, and this is
        // the only place the shape can be looked at.
        foreach ((double bx1, double by1, double bx2, double by2) in SurfaceEdge.Bound(body, site.LayoutSalt, env))
        {
            Line(s, PX(bx1), PY(by1), PX(bx2), PY(by2),
                "stroke=\"#2b3a4e\" stroke-width=\"1.5\" stroke-dasharray=\"4 4\"");
        }

        // The ship's underside, port and starboard of the tube mouth — real hull, drawn as hull.
        string hull = "stroke=\"#aab9cd\" stroke-width=\"2.5\" stroke-linecap=\"square\"";
        Line(s, PX(leftX), PY(topY), PX(tubeLeft), PY(topY), hull);
        Line(s, PX(tubeRight), PY(topY), PX(rightX), PY(topY), hull);

        // The down-tube: the way home, and the anchor the whole supply line hangs from (#562).
        Line(s, PX(tubeLeft), PY(topY + 6), PX(tubeLeft), PY(topY), hull);
        Line(s, PX(tubeRight), PY(topY + 6), PX(tubeRight), PY(topY), hull);
        Text(s, PX((tubeLeft + tubeRight) / 2), PY(topY + 2.5), "#7fd8c8", 11, "TUBE");

        // The body's own geography.
        foreach (SurfaceLayout.Wall wall in plan.Walls)
        {
            string ink = wall.IsHull
                ? "stroke=\"#aab9cd\" stroke-width=\"2.5\""
                : "stroke=\"#6e7d91\" stroke-width=\"1.5\"";
            Line(s, PX(wall.X1), PY(wall.Y1), PX(wall.X2), PY(wall.Y2), ink);
        }

        // #563 · The outpost hut, drawn as it stands once forced — walls, landmark, and its two
        // interactables. A hut that is still shut is only a console on the plan, so this is the state worth
        // looking at: it is the one that can bottle the edge lane.
        SurfaceOutpost.Placement hut = SurfaceOutpost.For(body, site.LayoutSalt, env);
        if (hut.HasOutpost)
        {
            SurfaceOutpost.Region room = SurfaceOutpost.Build(body, site.LayoutSalt, hut);
            foreach (SurfaceLayout.Wall wall in room.Walls)
            {
                Line(s, PX(wall.X1), PY(wall.Y1), PX(wall.X2), PY(wall.Y2), hull);
            }
            foreach (SurfaceOutpost.OutpostConsole c in room.Consoles)
            {
                s.Append(CultureInfo.InvariantCulture,
                    $"<circle cx=\"{N(PX(c.X))}\" cy=\"{N(PY(c.Y))}\" r=\"3\" fill=\"#7fd8c8\"/>");
            }
            foreach (SurfaceLayout.Landmark m in room.Landmarks)
            {
                Text(s, PX(m.X), PY(m.Y) - 4, "#8fd0b8", 9, Escape(m.Label));
            }
            // The hatch, on the open-ground side.
            Text(s, PX(hut.DoorX - (hut.ExtendDir * 2.5)), PY(hut.DoorY), "#e0b070", 9,
                Escape(SurfaceOutpost.DoorLabel(SurfaceOutpost.CoverFor(body, site.LayoutSalt))),
                hut.ExtendDir < 0 ? "start" : "end");
        }

        foreach (SurfaceLayout.Landmark m in plan.Landmarks)
        {
            Text(s, PX(m.X), PY(m.Y) - 4, "#d8c58a", 11, Escape(m.Label));
        }

        // The landing band — where the tube, the kiosk and the way home cluster.
        Line(s, PX(leftX), PY(bandY), PX(rightX), PY(bandY),
            "stroke=\"#1d2a3d\" stroke-width=\"1\" stroke-dasharray=\"2 8\"");

        // Caption.
        Text(s, 10, 18, "#e6edf5", 14, $"{Escape(body.ToUpperInvariant())} — {Escape(site.Name)}", "start");
        Text(s, 10, 34, "#7f8ea3", 11,
            $"{Escape(plan.Scheme)} · {plan.Walls.Count} segments · site {site.Index}", "start");

        s.Append("</g></svg>");
        return s.ToString();
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
