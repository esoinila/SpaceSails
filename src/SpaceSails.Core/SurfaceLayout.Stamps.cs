namespace SpaceSails.Core;

/// <summary>
/// U05-F01 (#1419) · LANDMARK STAMPS — the board promises each landing site a character ("Fused rockcrete and
/// dead floodlights", "A gully of permanent night"), and the seeded ground used to ignore it: every secondary
/// site was the same rubble scatter under a different seed. Fable's ruling (2026-11-07): stamp ONE signature
/// per kind, placed off the landing band; <see cref="LandingSiteKind.QuietBasin"/> and
/// <see cref="LandingSiteKind.WildPlain"/> get none, and site 0 stays byte for byte what it was.
///
/// <para>A stamp is laid by the seeded generator BEFORE its feature loop, through the generator's own claim
/// ledger — the same one the shelters, the hidden lab, the monolith's keep-out and the landing square are
/// already in — so a stamp can never overlap any of them, and the rubble that follows lays itself around the
/// stamp. A piece the ledger refuses is dropped, never squeezed in: a missing post is scenery, a wall through a
/// shelter is a dead captain.</para>
///
/// <para>Fire code #822: nothing a stamp builds is an ENCLOSED space. The aprons are flat, every hut is a
/// three-sided U, the rille's two walls and the fissure's slot are open at both ends. Nothing needs a door.</para>
/// </summary>
public static partial class SurfaceLayout
{
    /// <summary>What a kind's stamp adds to a ground: its collidable walls and its drawn-only marks.</summary>
    public readonly record struct Stamp(
        IReadOnlyList<Wall> Walls, IReadOnlyList<SurfaceScenery.Mark> Marks)
    {
        /// <summary>No stamp at all (WildPlain, QuietBasin, an unknown salt).</summary>
        public static Stamp None { get; } = new([], []);
    }

    /// <summary>The kind a layout salt names. A site's salt IS its kind's name (see
    /// <see cref="LandingSites"/>); site 0's empty salt, a tile's lattice key and any stray string are
    /// <see cref="LandingSiteKind.WildPlain"/>, which stamps nothing.</summary>
    public static LandingSiteKind KindOfSalt(string? siteSalt)
    {
        foreach (LandingSiteKind k in System.Enum.GetValues<LandingSiteKind>())
        {
            if (k != LandingSiteKind.WildPlain && string.Equals(siteSalt, k.ToString(), System.StringComparison.Ordinal))
            {
                return k;
            }
        }
        return LandingSiteKind.WildPlain;
    }

    /// <summary>The stamp a (body, site salt) ground carries, computed against the same ledger the seeded
    /// generator uses — what a test or the scenery layer asks, instead of re-deriving it.</summary>
    public static Stamp StampFor(string bodyId, in Field field, string? siteSalt)
    {
        LandingSiteKind kind = KindOfSalt(siteSalt);
        if (kind is LandingSiteKind.WildPlain or LandingSiteKind.QuietBasin)
        {
            return Stamp.None;
        }
        var claimed = new System.Collections.Generic.List<(double X0, double Y0, double X1, double Y1)>();
        ReserveOn(claimed, field.AnchorX, field.AnchorY, AnchorReserveRadius, AnchorReserveRadius);
        foreach ((double sx, double sy, double sr) in StandingClaims(bodyId, siteSalt, field))
        {
            ReserveOn(claimed, sx, sy, sr, sr);
        }
        return PlaceStamp(kind, $"{bodyId}~{siteSalt}", field, claimed);
    }

    // The ledger's two verbs, shared by the generator and StampFor so they cannot disagree.
    private const double LedgerElbow = 1.5;

    private static bool ClaimOn(System.Collections.Generic.List<(double X0, double Y0, double X1, double Y1)> claimed,
        double cx, double cy, double halfW, double halfH)
    {
        (double x0, double y0, double x1, double y1) =
            (cx - halfW - LedgerElbow, cy - halfH - LedgerElbow, cx + halfW + LedgerElbow, cy + halfH + LedgerElbow);
        foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
        {
            if (x0 < ax1 && x1 > ax0 && y0 < ay1 && y1 > ay0)
            {
                return false;
            }
        }
        claimed.Add((x0, y0, x1, y1));
        return true;
    }

    private static void ReserveOn(System.Collections.Generic.List<(double X0, double Y0, double X1, double Y1)> claimed,
        double cx, double cy, double halfW, double halfH) =>
        claimed.Add((cx - halfW - LedgerElbow, cy - halfH - LedgerElbow, cx + halfW + LedgerElbow, cy + halfH + LedgerElbow));

    /// <summary>One thing a stamp lays, claimed as a unit: the box it occupies and the walls it adds.</summary>
    private readonly record struct Piece(double Cx, double Cy, double HalfW, double HalfH, Wall[] Walls);

    private readonly record struct Candidate(List<Piece> Pieces, List<SurfaceScenery.Mark> Marks);

    /// <summary>Lay the kind's stamp on the ledger. The shelters, the hidden lab, the anchor and the landing
    /// square are already in <paramref name="claimed"/> and never yield; the stamp therefore tries a few dozen
    /// seeded placements and keeps the first that fits whole, or the one that keeps the most of itself. A piece
    /// the ledger refuses is dropped rather than squeezed in.</summary>
    private static Stamp PlaceStamp(LandingSiteKind kind, string key, in Field f,
        List<(double X0, double Y0, double X1, double Y1)> claimed)
    {
        Candidate? best = null;
        int bestScore = -1;
        foreach (Candidate c in Candidates(kind, key, f))
        {
            var trial = new List<(double X0, double Y0, double X1, double Y1)>(claimed);
            int fit = 0;
            foreach (Piece p in c.Pieces)
            {
                if (ClaimOn(trial, p.Cx, p.Cy, p.HalfW, p.HalfH))
                {
                    fit++;
                }
            }
            if (fit > bestScore)
            {
                (best, bestScore) = (c, fit);
            }
            if (fit == c.Pieces.Count)
            {
                break;
            }
        }
        if (best is not { } chosen)
        {
            return Stamp.None;
        }

        var walls = new List<Wall>();
        foreach (Piece p in chosen.Pieces)
        {
            if (ClaimOn(claimed, p.Cx, p.Cy, p.HalfW, p.HalfH))
            {
                walls.AddRange(p.Walls);
            }
        }
        return new Stamp(walls, chosen.Marks);
    }

    private static IEnumerable<Candidate> Candidates(LandingSiteKind kind, string key, Field f)
    {
        double hx = f.HomeX, band = f.LandingBandY;
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double side = Frac(key, "stamp:side") < 0.5 ? -1 : 1;

        switch (kind)
        {
            case LandingSiteKind.DepotApron:
            {
                // A swept rockcrete apron 18 du below the band (centred on the landing column when it can be);
                // its four corner posts are the dead floodlights. The posts sit outside the landing square
                // (x +/- 12 clears its 6 du radius) by construction.
                double[] shifts = [0, 30, -30, 60, -60, 90, -90, 120, -120];
                foreach (double dx in shifts)
                {
                    double cx = hx + dx, cy = band - 18, hw = 12, hh = 7;
                    var marks = new List<SurfaceScenery.Mark>();
                    RectMarks(marks, cx - hw, cy - hh, cx + hw, cy + hh, SurfaceScenery.Kind.Ridge);
                    var pieces = new List<Piece>();
                    foreach ((double px, double py) in new[] { (cx - hw, cy - hh), (cx + hw, cy - hh), (cx - hw, cy + hh), (cx + hw, cy + hh) })
                    {
                        var w = new List<Wall>();
                        AddBox(w, px - 0.75, py - 0.75, px + 0.75, py + 0.75, hull: true);
                        pieces.Add(new(px, py, 0.75, 0.75, [.. w]));
                    }
                    yield return new(pieces, marks);
                }
                break;
            }

            case LandingSiteKind.CraterShelf:
            {
                // One long hull wall along the bottom edge, with two 4 du gaps — the shelf's lip.
                double mid = (minX + maxX) / 2;
                double g1 = Lerp(minX + 30, mid - 15, Frac(key, "stamp:gap1"));
                double g2 = Lerp(mid + 15, maxX - 30, Frac(key, "stamp:gap2"));
                (double From, double To)[] runs = [(minX, g1 - 2), (g1 + 2, g2 - 2), (g2 + 2, maxX)];
                foreach (double lift in new double[] { 8, 4, 12, 16, 20, 26, 32, 38, 44, 52 })
                {
                    double y = f.BottomY + lift;
                    var pieces = new List<Piece>();
                    foreach ((double from, double to) in runs)
                    {
                        int n = System.Math.Max(1, (int)System.Math.Ceiling((to - from) / 20.0));
                        for (int i = 0; i < n; i++)
                        {
                            double a = Lerp(from, to, i / (double)n), b = Lerp(from, to, (i + 1) / (double)n);
                            pieces.Add(new((a + b) / 2, y, (b - a) / 2, 0.5, [new Wall(a, y, b, y, true)]));
                        }
                    }
                    yield return new(pieces, []);
                }
                break;
            }

            case LandingSiteKind.IceFissure:
            {
                // A jagged crack across the field with a 2 du slot of walls down it and ONE 4 du bridge.
                const int Steps = 12;
                int bridge = 2 + Face(key, "stamp:bridge", Steps - 3);
                for (int c = 0; c < 24; c++)
                {
                    double yc = Lerp(f.AnchorY + 45, band - 55, ((c * 7 % 24) + Frac(key, "stamp:y")) / 24.0);
                    var xs = new double[Steps + 1];
                    var ys = new double[Steps + 1];
                    for (int i = 0; i <= Steps; i++)
                    {
                        xs[i] = Lerp(minX, maxX, i / (double)Steps);
                        ys[i] = yc + ((Frac(key, $"stamp:j:{i}") - 0.5) * 12.0);
                    }
                    var marks = new List<SurfaceScenery.Mark>();
                    var pieces = new List<Piece>();
                    for (int i = 0; i < Steps; i++)
                    {
                        marks.Add(new(xs[i], ys[i], xs[i + 1], ys[i + 1], SurfaceScenery.Kind.Rille));
                        double lo = System.Math.Min(ys[i], ys[i + 1]) - 1, hi = System.Math.Max(ys[i], ys[i + 1]) + 1;
                        double span = xs[i + 1] - xs[i];
                        var w = new List<Wall>();
                        for (int sd = -1; sd <= 1; sd += 2)
                        {
                            if (i == bridge)
                            {
                                AddSlotRun(w, xs[i], ys[i], xs[i + 1], ys[i + 1], sd, 0, 0.5 - (2.0 / span));
                                AddSlotRun(w, xs[i], ys[i], xs[i + 1], ys[i + 1], sd, 0.5 + (2.0 / span), 1);
                            }
                            else
                            {
                                AddSlotRun(w, xs[i], ys[i], xs[i + 1], ys[i + 1], sd, 0, 1);
                            }
                        }
                        pieces.Add(new((xs[i] + xs[i + 1]) / 2, (lo + hi) / 2, span / 2, (hi - lo) / 2, [.. w]));
                    }
                    yield return new(pieces, marks);
                }
                break;
            }

            case LandingSiteKind.DerelictPad:
            {
                // A cracked pad with the hull of what set down on it: a 10 x 4 U, open end to the band.
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    double sd = attempt % 2 == 0 ? side : -side;
                    double cx = hx + (sd * (26 + (12 * (attempt / 2))));
                    double cy = band - 30 - (4 * ((attempt / 2) % 2));
                    if (cx - 5 < minX || cx + 5 > maxX)
                    {
                        continue;
                    }
                    var marks = new List<SurfaceScenery.Mark>();
                    RectMarks(marks, cx - 9, cy - 6, cx + 9, cy + 6, SurfaceScenery.Kind.Ridge);
                    var w = new List<Wall>();
                    AddU(w, cx, cy, 10, 4, UOpen.Up, hull: true);
                    yield return new([new Piece(cx, cy, 6.5, 3.5, [.. w])], marks);
                }
                break;
            }

            case LandingSiteKind.ShadowedRille:
            {
                // Two walls 10 du apart running from the band toward the deep edge, open at both ends.
                double[] offsets = [22, 36, 50, 64, 78, 92, 106, 120];
                for (int c = 0; c < offsets.Length * 2; c++)
                {
                    double cx = hx + ((c % 2 == 0 ? side : -side) * offsets[c / 2]);
                    if (cx - 5 < minX || cx + 5 > maxX)
                    {
                        continue;
                    }
                    var pieces = new List<Piece>();
                    for (int p = 0; p < 6; p++)
                    {
                        double y1 = band - 14 - (p * 10), y2 = y1 - 10;
                        pieces.Add(new(cx, (y1 + y2) / 2, 5.5, 5,
                            [new Wall(cx - 5, y1, cx - 5, y2, false), new Wall(cx + 5, y1, cx + 5, y2, false)]));
                    }
                    yield return new(pieces, []);
                }
                break;
            }

            case LandingSiteKind.RidgeCamp:
            {
                // The bones of a survey camp near the band: two U huts, a lean-to, one mast.
                double[] shifts = [0, 20, -20, 40, -40, 60, -60, 80, -80];
                foreach (double shift in shifts)
                {
                    (double Dx, double Dy, double W, double H, UOpen Open)[] huts =
                    [
                        (-30, -22, 7, 5, UOpen.Right),
                        (30, -26, 6, 6, UOpen.Left),
                        (48, -14, 5, 5, UOpen.Up),
                    ];
                    var pieces = new List<Piece>();
                    foreach ((double dx, double dy, double w, double h, UOpen open) in huts)
                    {
                        var ws = new List<Wall>();
                        AddU(ws, hx + shift + dx, band + dy, w, h, open, hull: false);
                        pieces.Add(new(hx + shift + dx, band + dy, (w / 2) + 1, (h / 2) + 1, [.. ws]));
                    }
                    var mast = new List<Wall>();
                    AddBox(mast, hx + shift - 46.75, band - 12.75, hx + shift - 45.25, band - 11.25, hull: true);
                    pieces.Add(new(hx + shift - 46, band - 12, 0.75, 0.75, [.. mast]));
                    yield return new(pieces, []);
                }
                break;
            }
        }
    }

    private enum UOpen { Up, Down, Left, Right }

    /// <summary>Three walls of a w x h rectangle centred at (cx, cy); the named side is left open.</summary>
    private static void AddU(System.Collections.Generic.List<Wall> walls,
        double cx, double cy, double w, double h, UOpen open, bool hull)
    {
        double x0 = cx - (w / 2), x1 = cx + (w / 2), y0 = cy - (h / 2), y1 = cy + (h / 2);
        if (open != UOpen.Down) { walls.Add(new(x0, y0, x1, y0, hull)); }
        if (open != UOpen.Up) { walls.Add(new(x0, y1, x1, y1, hull)); }
        if (open != UOpen.Left) { walls.Add(new(x0, y0, x0, y1, hull)); }
        if (open != UOpen.Right) { walls.Add(new(x1, y0, x1, y1, hull)); }
    }

    private static void RectMarks(System.Collections.Generic.List<SurfaceScenery.Mark> marks,
        double x0, double y0, double x1, double y1, SurfaceScenery.Kind kind)
    {
        marks.Add(new(x0, y0, x1, y0, kind));
        marks.Add(new(x0, y1, x1, y1, kind));
        marks.Add(new(x0, y0, x0, y1, kind));
        marks.Add(new(x1, y0, x1, y1, kind));
    }

    /// <summary>One bank of the fissure's slot, between two parameters along its centre segment.</summary>
    private static void AddSlotRun(System.Collections.Generic.List<Wall> walls,
        double xa, double ya, double xb, double yb, int side, double t0, double t1)
    {
        if (t1 - t0 < 0.01)
        {
            return;
        }
        walls.Add(new(
            Lerp(xa, xb, t0), Lerp(ya, yb, t0) + side,
            Lerp(xa, xb, t1), Lerp(ya, yb, t1) + side, false));
    }
}
