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

        // #563 · FEATURES MAY NOT BE LAID ON TOP OF ONE ANOTHER. Harmless while every shape was an open
        // span or a U — two overlapping rubble walls are just messier rubble. The moment buildings arrived
        // it stopped being harmless: a second feature's wall laid across a doorway SEALS the room behind
        // it, and the reachability flood caught exactly that on four sites (21-29 du^2 of interior nobody
        // could reach). So each placement claims a footprint with a little elbow room, and anything that
        // would land on a claim is skipped rather than squeezed — a slightly emptier field beats a building
        // you can see into and never enter.
        // Elbow room is a GAP between footprints, not a exclusion zone: 3 du rejected so much that the
        // Ridge Camp fell from 25 segments to 10 and the "more interesting landscape" came out emptier than
        // before. 1.5 is enough to keep one feature's wall off another's doorway.
        var claimed = new System.Collections.Generic.List<(double X0, double Y0, double X1, double Y1)>();
        const double Elbow = 1.5;

        bool Claim(double cx, double cy, double halfW, double halfH)
        {
            (double x0, double y0, double x1, double y1) =
                (cx - halfW - Elbow, cy - halfH - Elbow, cx + halfW + Elbow, cy + halfH + Elbow);
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

        // The deep landmark's own fixture is laid AFTER this loop but stands on real ground. Claim it first,
        // or a building can be seeded around the anchor and then have the fixture dropped across its door.
        Claim(ax, ay, 2, 2);

        for (int i = 0; i < features; i++)
        {
            double cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}"));
            double cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}"));
            double len = 5 + (7 * Frac(bodyId, $"len:{i}")); // 5..12 du
            int shape = Face(bodyId, $"shape:{i}", 4);        // 0..3
            bool horizontal = Frac(bodyId, $"rot:{i}") < 0.5;

            // Try a few seeded spots before abandoning a feature. Skipping on the first clash threw most of
            // the field away; a handful of retries keeps the ground full while still never overlapping.
            // Buildings are bigger than `len` (SurfaceStructure clamps them up to a workable size) and
            // carry thick walls, so they claim a footprint sized like the thing that will actually be laid.
            double claimHalf = shape == 2 ? 13.0 : len / 2;
            bool placed = Claim(cx, cy, claimHalf, claimHalf);
            for (int attempt = 1; attempt < 5 && !placed; attempt++)
            {
                cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}:{attempt}"));
                cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}:{attempt}"));
                placed = Claim(cx, cy, claimHalf, claimHalf);
            }
            if (!placed)
            {
                continue;
            }

            switch (shape)
            {
                case 0: // a bare rubble wall (a fallen span)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    break;
                case 1: // an L — two spans meeting at a corner (a collapsed room angle)
                    AddClampedSpan(walls, f, cx, cy, len, horizontal, hull: false);
                    AddClampedSpan(walls, f, cx, cy, len * 0.7, !horizontal, hull: false);
                    break;
                case 2: // a real BUILDING — thick walls, a doorway through the mass, seeded shape and angle
                    AddStructure(walls, f, cx, cy, len, bodyId, $"bld:{i}");
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

    /// <summary>#563 · Place one of <see cref="SurfaceStructure"/>'s buildings on the ground: seeded shape,
    /// angle, door count and wall thickness, clamped so the whole footprint stays inside the safe span (a
    /// building clipped by the edge lane would lose the face its doorway was on and become a solid block).
    ///
    /// <para>Thickness is seeded 1.2..2.4 du — the owner's Greenland longhouse: on a cold world you build
    /// out of what is under your boots, and if the wall is also holding an atmosphere you build it fat.</para></summary>
    private static void AddStructure(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double size, string bodyId, string tag)
    {
        // 1.6..3.0 du of piled regolith — the owner's Greenland longhouse, and comfortably above the
        // captain's own 1.4 du width so the hatching never emits a segment shorter than a body.
        double thickness = 1.6 + (1.4 * Frac(bodyId, $"{tag}:thick"));
        double w = System.Math.Clamp(size * 1.4, 12, 20);
        double h = System.Math.Clamp(size * 1.1, 10, 16);

        // Keep the whole thing (walls included) off the edge lanes.
        double halfW = (w / 2) + thickness, halfH = (h / 2) + thickness;
        cx = System.Math.Clamp(cx, f.LeftX + EdgeMargin + halfW, f.RightX - EdgeMargin - halfW);
        cy = System.Math.Clamp(cy, f.BottomY + 2 + halfH, f.LandingBandY - 2 - halfH);

        var spec = new SurfaceStructure.Spec(
            cx, cy, w, h,
            AngleRad: Frac(bodyId, $"{tag}:angle") * System.Math.Tau,
            Doors: 1 + Face(bodyId, $"{tag}:doors", 2),
            WallThickness: thickness,
            Shape: (SurfaceStructure.Footprint)Face(bodyId, $"{tag}:shape", 3));

        walls.AddRange(SurfaceStructure.Build(spec).Walls);
    }

    /// <summary>
    /// #563 · A SMALL BUILDING — four walls, a doorway you walk through, and usually a room inside it.
    ///
    /// <para>Owner, 2026-08-01: <i>"as for content there needs to be more than silly U shapes... more stuff
    /// like small buildings with actual walls and doors."</i> He is right, and the U was the weakest thing
    /// the generator made: a rectangle with one side left off is not a ruin, it is a rectangle somebody
    /// forgot to finish. It has no inside, so there is nothing to enter and nothing to find.</para>
    ///
    /// <para>A building has a real threshold. You walk THROUGH something to be inside it, and inside there
    /// is a partition with its own doorway, so even a small footprint gives two spaces and a reason to walk
    /// the second one. That is what turns a shape on the ground into a place.</para>
    ///
    /// <para>Every opening is <see cref="DoorwayHalf"/> × 2 wide — comfortably more than the captain's
    /// diameter — and the partition's doorway is deliberately offset from the outer one so the two are
    /// never in line. A straight shot from the street to the back wall makes the interior read as a corridor
    /// rather than as rooms, and it also means one glance from outside tells you everything.</para>
    ///
    /// <para>These are ruins, so the openings are OPENINGS — no hinges, nothing to force. The lockable
    /// version is the outpost hut (<see cref="SurfaceOutpost"/>), which is a different thing on purpose: one
    /// is scenery you can step into, the other is a decision with a locker behind it.</para>
    /// </summary>
    private static void AddBuilding(System.Collections.Generic.List<Wall> walls, in Field f,
        double cx, double cy, double w, double h, string bodyId, string tag)
    {
        // Keep the whole footprint inside the safe span; a building clipped by the edge lane would have its
        // doorway cut off and become a solid block.
        double halfW = System.Math.Min(w, 14) / 2, halfH = System.Math.Min(h, 12) / 2;
        double x0 = System.Math.Max(f.LeftX + EdgeMargin, cx - halfW);
        double x1 = System.Math.Min(f.RightX - EdgeMargin, cx + halfW);
        double y0 = System.Math.Max(f.BottomY + 2, cy - halfH);
        double y1 = System.Math.Min(f.LandingBandY - 2, cy + halfH);

        // Too small to hold a doorway and a room? Then it is rubble, and rubble is what it should look like.
        //
        // Emphatically NOT a closed box: that was the first thing written here, and a footprint clipped by
        // the edge lane can still be large, so "too small for a door" was quietly producing big SEALED
        // interiors — precisely the failure the doorway exists to avoid. Two walls that meet at a corner
        // enclose nothing, whatever size they are.
        if (x1 - x0 < MinDooredFace + 2 || y1 - y0 < MinDooredFace + 2)
        {
            walls.Add(new(x0, y0, x1, y0, false));
            walls.Add(new(x0, y0, x0, y1, false));
            return;
        }

        int doorWall = Face(bodyId, $"{tag}:door", 4);   // which face carries the way in
        double doorAlong = Lerp(0.3, 0.7, Frac(bodyId, $"{tag}:doorat"));

        // Bottom, top, left, right — each solid unless it is the one with the doorway in it.
        AddFace(walls, x0, y0, x1, y0, horizontal: true, gapAt: doorWall == 0 ? Lerp(x0, x1, doorAlong) : null);
        AddFace(walls, x0, y1, x1, y1, horizontal: true, gapAt: doorWall == 1 ? Lerp(x0, x1, doorAlong) : null);
        AddFace(walls, x0, y0, x0, y1, horizontal: false, gapAt: doorWall == 2 ? Lerp(y0, y1, doorAlong) : null);
        AddFace(walls, x1, y0, x1, y1, horizontal: false, gapAt: doorWall == 3 ? Lerp(y0, y1, doorAlong) : null);

        // One interior partition, across the building's SHORT axis so both rooms stay usably wide, with its
        // own doorway pushed to the far side from the outer door.
        bool splitVertically = (x1 - x0) >= (y1 - y0);
        double innerDoor = Lerp(0.25, 0.75, 1.0 - doorAlong);
        if (splitVertically)
        {
            double px = Lerp(x0, x1, Lerp(0.4, 0.6, Frac(bodyId, $"{tag}:split")));
            AddFace(walls, px, y0, px, y1, horizontal: false, gapAt: Lerp(y0, y1, innerDoor));
        }
        else
        {
            double py = Lerp(y0, y1, Lerp(0.4, 0.6, Frac(bodyId, $"{tag}:split")));
            AddFace(walls, x0, py, x1, py, horizontal: true, gapAt: Lerp(x0, x1, innerDoor));
        }
    }

    /// <summary>One wall face, solid or split around a doorway at <paramref name="gapAt"/>. The gap is
    /// clamped so it can never run off the end of the face and quietly delete a whole wall.</summary>
    private static void AddFace(System.Collections.Generic.List<Wall> walls,
        double x0, double y0, double x1, double y1, bool horizontal, double? gapAt)
    {
        if (gapAt is not { } g)
        {
            walls.Add(new(x0, y0, x1, y1, false));
            return;
        }

        // MinStub, not 0.5. A doorway clamped hard against the end of a face leaves a stub shorter than the
        // captain is wide, and DegenerateWallScan is right to call that an invisible wall: you cannot see it,
        // you cannot walk through it, and it reads as the game cheating. Either a face has room for a door
        // with real jambs either side, or it does not get the door.
        if (horizontal)
        {
            if (x1 - x0 < MinDooredFace) { walls.Add(new(x0, y0, x1, y1, false)); return; }
            g = System.Math.Clamp(g, x0 + DoorwayHalf + MinStub, x1 - DoorwayHalf - MinStub);
            walls.Add(new(x0, y0, g - DoorwayHalf, y0, false));
            walls.Add(new(g + DoorwayHalf, y1, x1, y1, false));
        }
        else
        {
            if (y1 - y0 < MinDooredFace) { walls.Add(new(x0, y0, x1, y1, false)); return; }
            g = System.Math.Clamp(g, y0 + DoorwayHalf + MinStub, y1 - DoorwayHalf - MinStub);
            walls.Add(new(x0, y0, x0, g - DoorwayHalf, false));
            walls.Add(new(x1, g + DoorwayHalf, x1, y1, false));
        }
    }

    /// <summary>Half a doorway's width. The captain is 1.4 du across; this leaves room to walk it badly,
    /// which is the bar every doorway in this game is held to (#498's "a bit narrow but navigatable").</summary>
    private const double DoorwayHalf = 1.6;

    /// <summary>The shortest jamb a doorway may leave beside it. Anything less is a stub nobody can see and
    /// nobody can pass — an invisible wall, which DegenerateWallScan exists to refuse.</summary>
    private const double MinStub = 1.6;

    /// <summary>The shortest face that can carry a doorway at all: the opening plus a real jamb each side.</summary>
    private const double MinDooredFace = (DoorwayHalf + MinStub) * 2;

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
