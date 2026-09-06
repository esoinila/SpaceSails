namespace SpaceSails.Core;

/// <summary>Part of <see cref="SurfaceLayout"/> (the header note lives in SurfaceLayout.cs) — THE FOUR
/// GROUNDS. Three are authored and one is generated. Miranda is the maze, canon, reproduced from the
/// original hand-built geometry and the ground that must NOT change. Phobos carries THE MONOLITH, on the
/// rim every treasure map has paced off since #164. Luna is the mass-driver ruins — long parallel launch
/// rails and strip foundations, visibly not a box maze, out of worldbuilding §1. And every other landable
/// body gets the seeded signature: a deterministic scatter off the one shared <see cref="DiceRule"/>
/// engine, so a body nobody has authored still has a ground of its own.</summary>
public static partial class SurfaceLayout
{
    // ── Miranda — the maze (canon, owner's #313). Reproduced exactly from the original hand-built
    //    geometry: concentric gapped corridor rows the Old Ones exploit to corner a dawdler, two spurs,
    //    and the freestanding slab at the heart. This is the ground that must NOT change.
    //
    //    #649 · The slab at the heart is THE FALSE SLAB now, not the monolith: the owner reserved that word
    //    for the one object, and it stands on Phobos, where every treasure map has paced off it since #164.
    //    Every number below is the number that was there yesterday (FalseSlab's constants are the slab's own
    //    former values), so this ground generates byte-for-byte the walls it generated before. Only the NAME
    //    and the CARD moved — the geometry the owner authored is untouched, as it must be. ──
    private static Plan Miranda(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        double left = ax - 18, right = ax + 18;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        AddGappedRow(walls, left, right, ay + 12, ax + 10, 3);
        AddGappedRow(walls, left, right, ay + 6, ax - 11, 3);
        AddGappedRow(walls, left, right, ay - 4, ax + 9, 3);
        walls.Add(new(ax - 6, ay + 12, ax - 6, ay + 6, false));
        walls.Add(new(ax + 4, ay + 6, ax + 4, ay - 4, false));
        // #586 / #649 · THE SLAB AT THE HEART OF THE MAZE. A wall of stacked courses inside the cell the
        // canon maze rows leave for it — the maze is canon and every row above stays exactly where it was
        // authored. Its numbers are FalseSlab's own now, so a change to the monolith's scale (which the
        // owner wants to be enormous) can never quietly resize somebody else's corridor.
        AddSolidMass(walls, ax - FalseSlab.HalfWidth, ay - FalseSlab.HalfHeight,
            ax + FalseSlab.HalfWidth, ay + FalseSlab.HalfHeight, hull: true);

        // The four approach stubs, just inside the apron: the remains of something that was walked to on
        // purpose. Small, solid, and unmistakably PLACED — the difference between a rock and a ruin.
        foreach ((double mx, double my) in new[]
        {
            (ax, ay + FalseSlab.MarkerRing), (ax, ay - FalseSlab.MarkerRing),
            (ax - FalseSlab.MarkerRing, ay), (ax + FalseSlab.MarkerRing, ay),
        })
        {
            AddSolidMass(walls, mx - FalseSlab.MarkerHalf, my - FalseSlab.MarkerHalf,
                mx + FalseSlab.MarkerHalf, my + FalseSlab.MarkerHalf, hull: true);
        }

        // #563 · THE CANON GROUND GETS BUILDINGS TOO. Owner, standing on it: "no real buildings and
        // one-thick walls still" — because site 0 is AUTHORED and routes here, bypassing the seeded
        // generator where the structures live. So the flagship ground, the one carrying the story and the
        // one every captain lands on by default, was the only one that never got the new content: Lab 47
        // measured it at 12 wall segments over 12% of the field, the emptiest site on the moon.
        //
        // The maze itself is untouched — it is canon and stays exactly as authored. These stand OUT in the
        // empty flanks and shallows the maze never occupied, which is most of the field.
        AddOutlyingStructures(walls, doorways, centres, f, "miranda", ax, ay, keepOut, footprints);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, FalseSlab.ConsoleLabel) };
        return new Plan(FalseSlab.Scheme, walls, marks, doorways, centres, footprints);
    }

    // ── Phobos — THE MONOLITH (#164 / #586 / #649). The one object, on the one ground, on the moon whose
    //    name has been printed on every treasure map since the first one was minted.
    //
    //    Owner's ruling (worldbuilding §8): it must NOT sit in a fenced little plot with the set dressing
    //    around it — "whatever ground carries it has to be open enough that the object IS the horizon, not a
    //    prop in a room." So this scheme is defined as much by what it does not lay as by what it does:
    //    there is no maze, no corridor rows, no ruin field between you and it. Open regolith from the
    //    landing band all the way down, and the thing standing in it. The buildings this ground gets are
    //    AddOutlyingStructures' four, which are held clear of the signature by construction and end up out
    //    in the flanks — a rim camp on the edge of a plain, not a courtyard.
    //
    //    What the slab and its ceremony actually MEASURE is #649's second half (the scale pass) and is not
    //    decided here: every number below is read from Monolith, which is the one source. ──
    private static Plan Phobos(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // THE MONOLITH. One solid mass, clamped into the field like every other authored signature so the
        // edge lanes stay open and a way down always exists however large it grows.
        // hatchDrawn: false — the mass is hatched through for collision (nothing can stand inside it) and
        // NONE of those strokes is drawn. Every other solid on every moon reads as piled regolith because of
        // that hatch; this one must read as ONE FACE, which is what its own card says it is ("No seam") and
        // what the owner's "not having been built by us" looks like on a crude grid. The picture is the
        // filled mass MoonSurface lays over it.
        // courses: Upright — #883 made the hatch take the SHORT side by default, which on a 54 × 14 du slab
        // would swap forty-nine upright strokes for twelve flat ones. Cheaper, and not this sweep's to
        // spend: the courses are authored (TheMassStillCollidesThroughEvenThoughNoneOfItIsPainted counts
        // them, and counts them upright), and a furniture sweep does not re-cut the one object on the moon.
        AddClampedSolidMass(walls, f, ax - Monolith.HalfWidth, ay - Monolith.HalfHeight,
            ax + Monolith.HalfWidth, ay + Monolith.HalfHeight, hull: true, hatchDrawn: false,
            courses: Hatch.Upright);

        // The four stubs at the compass points, just inside the swept apron: the remains of an approach.
        // Small, solid, and unmistakably PLACED — the difference between a rock and a ruin. They are the
        // only other made thing within sight of it.
        foreach ((double mx, double my) in new[]
        {
            (ax, ay + Monolith.MarkerRing), (ax, ay - Monolith.MarkerRing),
            (ax - Monolith.MarkerRing, ay), (ax + Monolith.MarkerRing, ay),
        })
        {
            AddClampedSolidMass(walls, f, mx - Monolith.MarkerHalf, my - Monolith.MarkerHalf,
                mx + Monolith.MarkerHalf, my + Monolith.MarkerHalf, hull: true);
        }

        // #563 · Real buildings with real doors, the same as every other ground gets — but only out in the
        // flanks. AddOutlyingStructures refuses to place anything near the signature, which is exactly the
        // "not a boxed backyard" rule expressed as code rather than as a wish.
        AddOutlyingStructures(walls, doorways, centres, f, Monolith.BodyId, ax, ay, keepOut, footprints);

        var marks = new System.Collections.Generic.List<Landmark> { new(ax, ay - 3, Monolith.ConsoleLabel) };
        return new Plan(MonolithScheme, walls, marks, doorways, centres, footprints);
    }

    /// <summary>The location line the deep area reads on the monolith's ground. Named for the place, not the
    /// object: the Stickney rim is the real feature the real 85 m boulder sits on
    /// (<see cref="Landmarks.PhobosMonolith"/>), and the house rule holds — the PLACE is real, what happens
    /// in its shadow is ours.</summary>
    public const string MonolithScheme = "THE STICKNEY RIM";

    // ── Luna — the MASS-DRIVER RUINS (worldbuilding §1: the lunar mass drivers). A visibly different
    //    scheme (owner: "come up with something different… at least the walls of buildings should not be
    //    the same layout"): NO box maze. Instead the wreck of the old launcher — a long twin launch RAIL
    //    running up the field (broken into staggered segments so you weave lane to lane), the muzzle
    //    block at the deep head, and a scatter of rectangular STRIP FOUNDATIONS (the factory footings)
    //    that read as strips, not cells. The rails sit in the central band; the field's flanks stay open
    //    regolith, so combing the ruins is a very different walk from Miranda's concentric maze. ──
    private static Plan Luna(in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

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

        AddOutlyingStructures(walls, doorways, centres, f, "luna", ax, ay, keepOut, footprints);   // #563: the authored ground gets buildings too

        var marks = new System.Collections.Generic.List<Landmark>
        {
            new(ax - 4, ay - 9, "⛓ MASS-DRIVER MUZZLE"),
            new(ax + 14, ay + 17, "▭ STRIP FOUNDATIONS"),
        };
        return new Plan("THE MASS-DRIVER RUINS", walls, marks, doorways, centres, footprints);
    }

    // ── Every other landable body — a SEEDED signature. A deterministic scatter of ruin blocks and
    //    broken arcs across the deep field, salted per body id off the shared dice engine, so each new
    //    outdoors differs by construction while always leaving the flanks open (pathability by design).
    //    Miranda and Luna never reach here; this serves phobos, europa, ganymede, callisto, titan,
    //    enceladus and any future landable body. ──
    private static Plan Seeded(string bodyId, in Field f, System.Collections.Generic.List<(double X, double Y, double R)> keepOut)
    {
        double ax = f.AnchorX, ay = f.AnchorY;
        var walls = new System.Collections.Generic.List<Wall>();
        var doorways = new System.Collections.Generic.List<Doorway>();
        var centres = new System.Collections.Generic.List<(double X, double Y)>();
        var footprints = new System.Collections.Generic.List<(double X, double Y, double R)>();

        // The safe span features may occupy — inside the kept-open edge lanes.
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double minY = f.BottomY + 4, maxY = f.LandingBandY - 6;

        // #573 · SCALED TO THE FIELD. This was a flat 5..9, sized for a 78 x 64 du field; dropping the same
        // handful into a field sixteen times the area would have made "more explorable space" read as a
        // bigger emptiness. One feature per ~700 du^2, so density holds however the field is sized.
        double area = (maxX - minX) * (maxY - minY);
        int features = System.Math.Clamp(5 + (int)(area / 700.0), 5, 90) + Face(bodyId, "count", 5);

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

        // #585 · RECORDING IS NOT ASKING. Reserve puts a footprint in the ledger unconditionally, for things
        // that are ALREADY STANDING and are not up for negotiation.
        //
        // The distinction cost a site. The shelters were pre-claimed through Claim() above — which REJECTS on
        // overlap — so two shelters that are legally placed (52 du apart, their own rule) but whose square
        // claim boxes happened to touch knocked each other out: the second Claim returned false and never
        // recorded, leaving that shelter invisible to the ledger and the ground under it free for a building.
        // The audit caught exactly that on luna/The Shadowed Rille, where a hut ended up 21.5 du from a
        // shelter needing 31.3.
        //
        // Claim() asks "may I build here?". Reserve() says "something is here." Using the asking one for the
        // saying job is the same category error as sharing a console kind between two different doors.
        void Reserve(double cx, double cy, double halfW, double halfH) =>
            claimed.Add((cx - halfW - Elbow, cy - halfH - Elbow, cx + halfW + Elbow, cy + halfH + Elbow));

        // The deep landmark's own fixture is laid AFTER this loop but stands on real ground. Claim it first,
        // or a building can be seeded around the anchor and then have the fixture dropped across its door.
        //
        // #586: claimed at the CEREMONY's radius, not the fixture's. A deep landmark is not just the stone —
        // there is cleared ground around it — and a building seeded on that would put somebody's hut in the
        // middle of the one ceremonial space on the moon.
        //
        // #649 · This read Monolith.ApronRadius, which is bug class 2 in waiting: the monolith stands on ONE
        // ground and this function lays every ground that is NOT it, so growing the slab (which the owner has
        // asked for) would have silently moved the huts on eight moons that have never seen it. Same value,
        // its own name, no shared fate.
        Reserve(ax, ay, AnchorReserveRadius, AnchorReserveRadius);

        // #585 · AND THE SHELTERS, which are laid by SurfaceShelter on a completely separate pass and were
        // therefore invisible to this ledger — so a seeded feature could be dropped straight through a
        // pressure drum. They are claimed first and never yielded: a hut that has to move costs nothing, a
        // shelter that has to move costs a captain who walked to where the beacon said it was.
        foreach ((double sx, double sy, double sr) in keepOut ?? [])
        {
            Reserve(sx, sy, sr, sr);
        }

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
            // #585: a BUILDING claims the footprint it will really stand on — the clamped centre and the
            // rotation-proof radius — instead of a flat 13 taken on trust. Rubble keeps its own span.
            bool placed;
            if (shape == 2)
            {
                (double bx, double by, double br) = StructureFootprint(f, cx, cy, len, bodyId, $"bld:{i}");
                placed = Claim(bx, by, br, br);
                for (int attempt = 1; attempt < 5 && !placed; attempt++)
                {
                    cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}:{attempt}"));
                    cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}:{attempt}"));
                    (bx, by, br) = StructureFootprint(f, cx, cy, len, bodyId, $"bld:{i}");
                    placed = Claim(bx, by, br, br);
                }
            }
            else
            {
                double claimHalf = len / 2;
                placed = Claim(cx, cy, claimHalf, claimHalf);
                for (int attempt = 1; attempt < 5 && !placed; attempt++)
                {
                    cx = Lerp(minX, maxX, Frac(bodyId, $"x:{i}:{attempt}"));
                    cy = Lerp(minY, maxY, Frac(bodyId, $"y:{i}:{attempt}"));
                    placed = Claim(cx, cy, claimHalf, claimHalf);
                }
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
                    AddStructure(walls, doorways, centres, f, cx, cy, len, bodyId, $"bld:{i}", footprints);
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

        return new Plan("THE DEEP RUINS", walls, marks, doorways, centres, footprints);
    }
}
