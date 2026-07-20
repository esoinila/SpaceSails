namespace SpaceSails.Core;

/// <summary>
/// Sunday-morning wind · #1–#2 (owner, 2026-07-19, verbatim): <b>"Earth Moon and Miranda out-doors were
/// extremely similar maps. For Moon we should come up with something different… at least the walls of
/// buildings should not be the same layout."</b> And: the other shuttle destinations that can have an
/// outdoors should get their own too. Today every landable body's walked surface reuses ONE geometry
/// (the monolith maze); this is the pure, deterministic generator that gives each body its OWN ground.
///
/// <para>The surface LAWS stay shared and live in the client's <c>MoonSurface</c> (the landing band at
/// the top, the tube mouth, the Reever barrier, the deep field + its tide spawn edge and home range).
/// Only the GEOGRAPHY varies here: the interior ruin/maze walls and the deep landmark, laid out inside
/// the shared field envelope the caller passes in. Two mechanisms, blended:</para>
/// <list type="bullet">
/// <item><b>Authored signatures</b> for the bodies with character — Miranda keeps THE MONOLITH maze
/// (it is canon), and Luna gets the mass-driver ruins (worldbuilding §1: the lunar mass drivers), a
/// visibly different scheme of long parallel launch rails and strip foundations, never a box maze.</item>
/// <item><b>A seeded signature</b> (deterministic per body id, off the one shared <see cref="DiceRule"/>
/// engine — never <see cref="System.Random"/> or the clock) for every other landable body, so each new
/// outdoors differs by construction.</item>
/// </list>
///
/// <para>Walls are collision LAW for everyone — the captain and the Old Ones (<see cref="ReeverChase"/>)
/// bump-and-slide on the same segments (#324) — so the geography is generated in Core where a test can
/// pin it: that Luna ≠ Miranda, that the seeded ground is deterministic, and that every scheme leaves a
/// walkable corridor from the tube mouth down to the deep field (features never seal the field's width;
/// the far-left and far-right regolith lanes are always kept open, so a way down always exists).</para>
/// </summary>
public static class SurfaceLayout
{
    /// <summary>A generated interior wall in deck units. <paramref name="IsHull"/> marks a solid opaque
    /// face (a landmark's own slab, the mass-driver muzzle) versus an open ruin/maze wall; the client
    /// maps both onto its collidable <c>DeckPlan.Wall</c>, so both stop a boot and a shamble alike.</summary>
    public readonly record struct Wall(double X1, double Y1, double X2, double Y2, bool IsHull);

    /// <summary>A deep-field landmark to label on the ground: its glyph-tagged text at (X, Y).</summary>
    public readonly record struct Landmark(double X, double Y, string Label);

    /// <summary>The shared field envelope the geography is laid inside — the LAWS, handed in from the
    /// client's <c>MoonSurface</c> so Core carries no client geometry constants yet lays ground within
    /// the sane bounds. <paramref name="AnchorX"/>/<paramref name="AnchorY"/> is the deep commitment
    /// area's centre (the old monolith spot), the heart every scheme dresses differently.</summary>
    public readonly record struct Field(
        double LeftX, double RightX, double TopY, double BottomY,
        double LandingBandY, double AnchorX, double AnchorY);

    /// <summary>One body's ground: a scheme name (for the deep-area location line and tests), the
    /// interior walls, and the deep landmark(s). The fence, tube, doors, kiosk and the way home are the
    /// caller's shared law — this is only what makes the body's geography its own.</summary>
    public readonly record struct Plan(
        string Scheme, IReadOnlyList<Wall> Walls, IReadOnlyList<Landmark> Landmarks);

    /// <summary>The safe half-lane kept open at each far edge of the field — no generated feature ever
    /// intrudes here, so a walk-around always exists and the deep is always reachable from the top.</summary>
    public const double EdgeMargin = 10.0;

    /// <summary>Lay out one landable body's ground. Miranda and Luna are authored; everything else is
    /// seeded deterministically from its id, so no two grounds are the same by construction.</summary>
    public static Plan For(string bodyId, in Field field) => (bodyId ?? "") switch
    {
        "miranda" => Miranda(field),
        "luna" => Luna(field),
        // #370: an away-expedition rock's id carries its kind — route straight to the authored site ground.
        _ when ExpeditionSite.TryParseKind(bodyId, out ExpeditionSiteKind kind) => ForExpedition(kind, field),
        _ => Seeded(bodyId ?? "", field),
    };

    /// <summary>#320 · Lay out a body's ground for a chosen LANDING SITE (<see cref="LandingSites"/>). An
    /// EMPTY salt is the body's canon site 0 — the authored/seeded signature, byte-for-byte the same ground
    /// as <see cref="For(string, in Field)"/> (so Miranda's monolith maze and Luna's rails are preserved).
    /// A non-empty salt is a secondary site: the ground is re-seeded off <c>(bodyId ~ salt)</c>, giving a
    /// visibly different wing/feature layout on the SAME body — different site, different deck-plan. An
    /// away-expedition rock keeps its authored per-kind ground regardless of salt (those gigs are single
    /// authored sites, never a seeded board).</summary>
    public static Plan For(string bodyId, in Field field, string? siteSalt) =>
        string.IsNullOrEmpty(siteSalt) || ExpeditionSite.TryParseKind(bodyId, out _)
            ? For(bodyId, field)
            : Seeded($"{bodyId ?? ""}~{siteSalt}", field);

    /// <summary>A stable order-independent hash of a plan's wall set — the test's "Luna ≠ Miranda"
    /// ground-truth handle (owner: the walls of buildings must not be the same layout), and a cheap way
    /// for any caller to tell two grounds apart.</summary>
    public static long WallHash(Plan plan)
    {
        unchecked
        {
            long acc = 1469598103934665603L;
            foreach (Wall w in plan.Walls)
            {
                // Quantise to 0.1 du so float noise never flips the hash, then fold each endpoint in an
                // order-independent way (sum of per-wall hashes) so wall list order can't matter.
                long h = 17;
                h = (h * 31) + Q(w.X1); h = (h * 31) + Q(w.Y1);
                h = (h * 31) + Q(w.X2); h = (h * 31) + Q(w.Y2);
                h = (h * 31) + (w.IsHull ? 1 : 0);
                acc += h;
            }
            acc = (acc * 31) + plan.Walls.Count;
            return acc;
        }
    }

    private static long Q(double v) => (long)System.Math.Round(v * 10.0);

    // ── Miranda — THE MONOLITH maze (canon, owner's #313). Reproduced exactly from the original
    //    hand-built geometry: concentric gapped corridor rows the Old Ones exploit to corner a dawdler,
    //    two spurs, and the freestanding slab at the heart. This is the ground that must NOT change. ──
    private static Plan Miranda(in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        double left = ax - 18, right = ax + 18;
        var walls = new System.Collections.Generic.List<Wall>();

        AddGappedRow(walls, left, right, ay + 12, ax + 10, 3);
        AddGappedRow(walls, left, right, ay + 6, ax - 11, 3);
        AddGappedRow(walls, left, right, ay - 4, ax + 9, 3);
        walls.Add(new(ax - 6, ay + 12, ax - 6, ay + 6, false));
        walls.Add(new(ax + 4, ay + 6, ax + 4, ay - 4, false));
        // The monolith itself: a short freestanding slab (a tiny box) at the heart.
        AddBox(walls, ax - 1.2, ay - 2.5, ax + 1.2, ay + 2.5, hull: true);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, "▮ THE MONOLITH") };
        return new Plan("THE MONOLITH MAZE", walls, marks);
    }

    // ── Luna — the MASS-DRIVER RUINS (worldbuilding §1: the lunar mass drivers). A visibly different
    //    scheme (owner: "come up with something different… at least the walls of buildings should not be
    //    the same layout"): NO box maze. Instead the wreck of the old launcher — a long twin launch RAIL
    //    running up the field (broken into staggered segments so you weave lane to lane), the muzzle
    //    block at the deep head, and a scatter of rectangular STRIP FOUNDATIONS (the factory footings)
    //    that read as strips, not cells. The rails sit in the central band; the field's flanks stay open
    //    regolith, so combing the ruins is a very different walk from Miranda's concentric maze. ──
    private static Plan Luna(in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();

        // The twin launch rail: two parallel lines running up-field from the deep head, each broken into
        // three segments with OFFSET gaps so the lanes cross-connect (a walker weaves through the breaks).
        double railTop = ay + 26, railBot = ay - 4;
        double leftRail = ax - 3.5, rightRail = ax + 3.5;
        AddBrokenVertical(walls, leftRail, railBot, railTop, gapAt: ay + 4, gapHalf: 3);
        AddBrokenVertical(walls, leftRail, railBot, railTop, gapAt: ay + 18, gapHalf: 3);
        AddBrokenVertical(walls, rightRail, railBot, railTop, gapAt: ay + 11, gapHalf: 3);
        // Cross-ties between the rails (the sleepers), a couple of short rungs — dead-end flavour.
        walls.Add(new(leftRail, ay + 22, rightRail, ay + 22, false));
        walls.Add(new(leftRail, ay + 8, rightRail, ay + 8, false));

        // The muzzle: a solid launch head block at the deep end, OFFSET to port (not centred like the
        // monolith) — the mass driver fired that way.
        AddBox(walls, ax - 6.5, ay - 8, ax - 1.5, ay - 4, hull: true);

        // Strip foundations: the factory footings, each two long parallel low walls (a strip outline,
        // open ended), staggered left and right up the deep field. Kept inside the edge margins.
        AddStrip(walls, f, cx: ax - 16, cy: ay + 4, len: 12, gap: 3);
        AddStrip(walls, f, cx: ax + 14, cy: ay + 14, len: 10, gap: 3);
        AddStrip(walls, f, cx: ax - 13, cy: ay + 20, len: 9, gap: 2.5);

        var marks = new System.Collections.Generic.List<Landmark>
        {
            new(ax - 4, ay - 9, "⛓ MASS-DRIVER MUZZLE"),
            new(ax + 14, ay + 17, "▭ STRIP FOUNDATIONS"),
        };
        return new Plan("THE MASS-DRIVER RUINS", walls, marks);
    }

    // ── Every other landable body — a SEEDED signature. A deterministic scatter of ruin blocks and
    //    broken arcs across the deep field, salted per body id off the shared dice engine, so each new
    //    outdoors differs by construction while always leaving the flanks open (pathability by design).
    //    Miranda and Luna never reach here; this serves phobos, europa, ganymede, callisto, titan,
    //    enceladus and any future landable body. ──
    private static Plan Seeded(string bodyId, in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();

        // The safe span features may occupy — inside the kept-open edge lanes.
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double minY = f.BottomY + 4, maxY = f.LandingBandY - 6;

        int features = 5 + Face(bodyId, "count", 5); // 5..9 ruins
        for (int i = 0; i < features; i++)
        {
            double cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}"));
            double cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}"));
            double len = 5 + (7 * Frac(bodyId, $"len:{i}")); // 5..12 du
            int shape = Face(bodyId, $"shape:{i}", 4);        // 0..3
            bool horizontal = Frac(bodyId, $"rot:{i}") < 0.5;

            switch (shape)
            {
                case 0: // a bare rubble wall (a fallen span)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    break;
                case 1: // an L — two spans meeting at a corner (a collapsed room angle)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    AddClampedSpan(walls, f, cx, cy, len * 0.7, !horizontal, hull: false);
                    break;
                case 2: // an open box missing one side (a shattered outpost)
                    AddOpenBox(walls, f, cx, cy, len, len * 0.7, gapSide: Face(bodyId, $"gap:{i}", 4));
                    break;
                default: // a small solid slab (an ancient spur / a plinth)
                    AddClampedBox(walls, f, cx - 1.4, cy - 1.4, cx + 1.4, cy + 1.4, hull: true);
                    break;
            }
        }

        // The deep landmark: a seeded ancient fixture near the anchor, with a glyph from a small palette.
        string[] glyphs = ["◭ ANCIENT SPUR", "⬡ SHATTERED DOME", "✶ SLAG FIELD", "⌂ COLLAPSED OUTPOST"];
        string glyph = glyphs[Face(bodyId, "glyph", glyphs.Length)];
        AddClampedBox(walls, f, ax - 2, ay - 2, ax + 2, ay + 2, hull: true); // the fixture's own footprint
        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, glyph) };

        return new Plan("THE DEEP RUINS", walls, marks);
    }

    // ── #370 · THE AWAY-EXPEDITION SITES. The special outdoors the owner's away-team gigs park next to
    //    (issue #370: "some dig site … mystical ruins or structures, crashlanded ships … a previously
    //    sealed piece of tunnel"). Three AUTHORED schemes, one per <see cref="ExpeditionSiteKind"/>, each
    //    visibly its own ground and distinct from Miranda/Luna/the seeded rubble — an homage to
    //    Alien/Prometheus energy, never a reproduction. The client calls this instead of For() when the
    //    excursion is an expedition; the fence/tube/tracker laws stay the caller's shared law. ──────────
    /// <summary>Lay out an away-expedition site's ground for its <paramref name="kind"/>. Authored, pure,
    /// and clamped inside the field's safe span exactly like every other scheme, so the way down always
    /// exists and the edge lanes stay open.</summary>
    public static Plan ForExpedition(ExpeditionSiteKind kind, in Field field) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => CrashedHull(field),
        ExpeditionSiteKind.SealedTunnel => SealedTunnel(field),
        _ => MysticalRuins(field),
    };

    // Mystical ruins — a HENGE: a ring of standing-stone slabs around a central altar, with no box maze.
    private static Plan MysticalRuins(in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();

        // Eight standing stones on a circle of radius ~10 du around the anchor (each a small solid slab).
        const int stones = 8;
        const double ring = 10.0;
        for (int i = 0; i < stones; i++)
        {
            double a = (2.0 * System.Math.PI * i) / stones;
            double sx = ax + (ring * System.Math.Cos(a));
            double sy = ay + (ring * System.Math.Sin(a));
            AddClampedBox(walls, f, sx - 1.1, sy - 1.1, sx + 1.1, sy + 1.1, hull: true);
        }

        // The central altar — a small freestanding hull slab at the heart.
        AddClampedBox(walls, f, ax - 1.6, ay - 1.4, ax + 1.6, ay + 1.4, hull: true);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, "⟁ THE STANDING STONES") };
        return new Plan("THE STANDING STONES", walls, marks);
    }

    // Crash-landed ship — a long TORN FUSELAGE half-buried up the field: the hull outline as an open box
    // with the port side blown out (the tear you walk in through), plus a few internal ribs. No ring, no
    // rails — reads as a wreck.
    private static Plan CrashedHull(in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();

        // The fuselage: a tall open box (deep→shallow), left side torn away (gapSide 2 = left open).
        AddOpenBox(walls, f, cx: ax, cy: ay + 8, w: 9, h: 30, gapSide: 2);
        // The nose: a solid crumpled block at the deep end.
        AddClampedBox(walls, f, ax - 3, ay - 8, ax + 3, ay - 4, hull: true);
        // Internal ribs — a few short cross-spans inside the hull (bulkhead frames), open ended.
        AddClampedSpan(walls, f, ax, ay + 2, 6, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 12, 6, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 20, 6, horizontal: true, hull: false);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 9, "⛢ THE CRASHED HULL") };
        return new Plan("THE CRASHED HULL", walls, marks);
    }

    // The owner's Fate-system anecdote made ground: a charge arc holed the rock and revealed a SEALED
    // TUNNEL of habitants ejected in a violent event, dead there. Two long parallel tunnel walls run deep
    // from a breach at the top, cross-bulkheads rung between them, and a chamber (the tomb) at the deep end.
    private static Plan SealedTunnel(in Field f)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();

        double tunTop = ay + 22, tunDeep = ay - 2;
        double leftWall = ax - 4, rightWall = ax + 4;
        // The two tunnel walls (solid hull), running deep from the breach; the breach itself is the open
        // top (no wall closes it), so you enter from the field into the shaft.
        AddClampedSpan(walls, f, leftWall, (tunTop + tunDeep) / 2, tunTop - tunDeep, horizontal: false, hull: true);
        AddClampedSpan(walls, f, rightWall, (tunTop + tunDeep) / 2, tunTop - tunDeep, horizontal: false, hull: true);
        // Cross-bulkheads (rungs) — short open spans between the walls, staggered, dead-end flavour.
        AddClampedSpan(walls, f, ax, ay + 16, 8, horizontal: true, hull: false);
        AddClampedSpan(walls, f, ax, ay + 6, 8, horizontal: true, hull: false);
        // The tomb chamber at the deep end — a small open box (one side breached).
        AddOpenBox(walls, f, cx: ax, cy: ay - 6, w: 12, h: 6, gapSide: 1);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 6, "⌸ THE SEALED TOMB") };
        return new Plan("THE SEALED TUNNEL", walls, marks);
    }

    // ── Builders. Every span is clamped into the field's safe span so no feature ever intrudes on the
    //    kept-open edge lanes — that is what guarantees a way down for the flood-fill test. ──

    private static void AddGappedRow(System.Collections.Generic.List<Wall> walls,
        double x1, double x2, double y, double gapCenter, double gapHalf)
    {
        walls.Add(new(x1, y, gapCenter - gapHalf, y, false));
        walls.Add(new(gapCenter + gapHalf, y, x2, y, false));
    }

    // A vertical line from y1 (deep) up to y2, broken by one gap centred at gapAt — the rail with a
    // washed-out sleeper section you weave through.
    private static void AddBrokenVertical(System.Collections.Generic.List<Wall> walls,
        double x, double y1, double y2, double gapAt, double gapHalf)
    {
        if (y1 < y2)
        {
            walls.Add(new(x, y1, x, gapAt - gapHalf, false));
            walls.Add(new(x, gapAt + gapHalf, x, y2, false));
        }
    }

    private static void AddBox(System.Collections.Generic.List<Wall> walls,
        double x1, double y1, double x2, double y2, bool hull)
    {
        walls.Add(new(x1, y1, x2, y1, hull));
        walls.Add(new(x1, y2, x2, y2, hull));
        walls.Add(new(x1, y1, x1, y2, hull));
        walls.Add(new(x2, y1, x2, y2, hull));
    }

    private static void AddStrip(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double len, double gap)
    {
        // Two long parallel walls (the strip's two footings), open at both ends.
        double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - len / 2);
        double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + len / 2);
        walls.Add(new(x1, cy - gap / 2, x2, cy - gap / 2, false));
        walls.Add(new(x1, cy + gap / 2, x2, cy + gap / 2, false));
    }

    private static void AddClampedSpan(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double len, bool horizontal, bool hull)
    {
        if (horizontal)
        {
            double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - len / 2);
            double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + len / 2);
            walls.Add(new(x1, cy, x2, cy, hull));
        }
        else
        {
            double y1 = System.Math.Max(f.BottomY + 2, cy - len / 2);
            double y2 = System.Math.Min(f.LandingBandY - 2, cy + len / 2);
            walls.Add(new(cx, y1, cx, y2, hull));
        }
    }

    private static void AddOpenBox(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double w, double h, int gapSide)
    {
        double x1 = System.Math.Max(f.LeftX + EdgeMargin, cx - w / 2);
        double x2 = System.Math.Min(f.RightX - EdgeMargin, cx + w / 2);
        double y1 = System.Math.Max(f.BottomY + 2, cy - h / 2);
        double y2 = System.Math.Min(f.LandingBandY - 2, cy + h / 2);
        if (gapSide != 0) { walls.Add(new(x1, y1, x2, y1, false)); } // bottom
        if (gapSide != 1) { walls.Add(new(x1, y2, x2, y2, false)); } // top
        if (gapSide != 2) { walls.Add(new(x1, y1, x1, y2, false)); } // left
        if (gapSide != 3) { walls.Add(new(x2, y1, x2, y2, false)); } // right
    }

    private static void AddClampedBox(System.Collections.Generic.List<Wall> walls, in Field f,
        double x1, double y1, double x2, double y2, bool hull)
    {
        x1 = System.Math.Max(f.LeftX + EdgeMargin, x1);
        x2 = System.Math.Min(f.RightX - EdgeMargin, x2);
        AddBox(walls, x1, y1, x2, y2, hull);
    }

    // ── Seeded sampling: pure and deterministic per (bodyId, tag) off the shared dice engine. ──
    private const int Resolution = 4096;

    private static double Frac(string bodyId, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"surface:{bodyId}:{tag}"), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    private static int Face(string bodyId, string tag, int sides) =>
        DiceRule.Roll(DiceRule.Seed($"surface:{bodyId}:{tag}"), sides).Face - 1; // 0..sides-1

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
