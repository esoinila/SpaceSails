namespace SpaceSails.Core;

/// <summary>Part of <see cref="SurfaceLayout"/> (the header note lives in SurfaceLayout.cs) — THE
/// PRIMITIVES EVERY SCHEME IS WRITTEN IN, and the sampler underneath them. Gapped rows and broken
/// verticals, solid masses hatched through so no gap in one can hold a body, boxes, strips, clamped
/// spans, #883's doored faces and the outlying structures a ground grows around its signature. Every span
/// here is clamped into the field's safe span, which is the single reason no feature ever intrudes on the
/// kept-open edge lanes and the flood-fill test can always find a way down. The file closes on the seeded
/// sampling — pure and deterministic per (bodyId, tag) off the shared dice engine, never
/// <see cref="System.Random"/> and never the clock — which is where the ground's own file put it.</summary>
public static partial class SurfaceLayout
{
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

    /// <summary>#586 · A box that is SOLID MASS, not a room with walls round it.
    ///
    /// <para>The monolith is a slab of stone. Drawn as four lines it has an INTERIOR, and on a crude grid an
    /// interior is somewhere a captain could stand if only they could get in — which the reachability audit
    /// correctly reports as sealed ground. At 2.4 x 5 du that cavity was under the guard's threshold and
    /// nobody noticed; widening the slab to something worth walking to made it 99 cells and the guard spoke
    /// up immediately, which is exactly what it is for.</para>
    ///
    /// <para>So a solid is hatched through, at closer than a captain's own width — the same trick
    /// <see cref="SurfaceStructure"/> already uses to make metres of piled regolith read as mass rather than
    /// as a suspiciously thick room. Nothing about the outline changes; the inside simply stops being a
    /// place.</para>
    ///
    /// <para>#874 · <b>PUBLIC, because a solid is a solid wherever it stands.</b> The monolith was not the
    /// only box in this game the art fills in and the sim leaves hollow. A growing bed in the Hive's park
    /// was four rails round a 12.6 × 5.6 du pocket — nine per park, eleven parks, 2,520 squares of standable
    /// floor per room that nobody could ever get into — and a waste bin is a 1.8 du box with exactly one
    /// sealed square in the middle of it. Every one of them is this method's own paragraph said again at
    /// furniture scale, so the furniture calls this rather than each placer laying four walls and hoping.
    /// One definition of SOLID, and a caller who gets the outline from it cannot forget the inside.</para>
    /// </summary>
    /// <param name="hatchDrawn">Whether the internal strokes are DRAWN as well as collided. True by default,
    /// and on anything of ordinary furniture size that is the right answer — see the class summary on
    /// <see cref="Wall"/>: on a small fixture those strokes are what makes it read as mass rather than as a
    /// suspiciously thick little room, and a raised bed full of soil is exactly that object. It is false for
    /// one thing only, the monolith, whose card says <i>no seam</i>.</param>
    /// <param name="courses">#883 · Which way the strokes run. See <see cref="Hatch"/> — the default is the
    /// FEWEST that can seal the box, and the one caller that overrides it is the one object whose masonry is
    /// authored.</param>
    public static void AddSolidMass(System.Collections.Generic.List<Wall> walls,
        double x1, double y1, double x2, double y2, bool hull, bool hatchDrawn = true,
        Hatch courses = Hatch.Fewest)
    {
        System.ArgumentNullException.ThrowIfNull(walls);
        AddBox(walls, x1, y1, x2, y2, hull);

        // ── #883 · A BOX THINNER THAN A BODY NEEDS NO HATCH AT ALL, and this building is full of them.
        //
        // The hatch exists for ONE reason, stated on the next line: no gap it leaves may hold a captain. A
        // box whose short side is already narrower than a captain's 1.4 du cannot hold one between its own
        // two rails either — the furthest into it a body can get from both is half the depth, which is less
        // than the body's own radius — so on a thin fixture the outline IS the whole of the solid and every
        // stroke through it is one more segment the patrol eye sweeps for nothing (Lab 45: O(walls)).
        //
        // Not a micro-optimisation: it is what makes the law affordable exactly where the law is needed
        // most. A laboratory bench and an office desk are 1.3 du deep (RingOffice.WorktopDepthDu, the
        // owner's own 90 cm), there are some 3,200 of them standing on 1,154 floors, and hatching every one
        // would have bought four strokes apiece to seal a pocket the four rails had already sealed.
        if (System.Math.Min(System.Math.Abs(x2 - x1), System.Math.Abs(y2 - y1))
            < SurfaceScale.CaptainWidthDu)
        {
            return;
        }

        // Closer than the captain's 1.4 du diameter, so no gap between hatches can ever hold a body.
        const double hatch = 1.1;
        double xLo = System.Math.Min(x1, x2), xHi = System.Math.Max(x1, x2);
        double yLo = System.Math.Min(y1, y2), yHi = System.Math.Max(y1, y2);

        // ── #883 · ACROSS THE SHORT SIDE, which is the same solid for a fraction of the segments.
        //
        // An upright stroke cuts the box into bands 1.1 du WIDE; a flat one cuts it into bands 1.1 du TALL.
        // Either seals it — a band narrower than a captain cannot hold one, whichever way round it lies — so
        // the choice is free and the count is not. A ring desk bank is 28.8 × 2.0 du: upright courses cost
        // TWENTY-FIVE strokes to say what one flat stroke says. Over the ring suites of a hundred sites that
        // is the difference between +9.9% segments building-wide and +2.4%, and between a worst floor at 853
        // and one at 638 — measured both ways before this line was written, because the patrol eye is
        // O(walls) (Lab 45) and a solid nobody can see is the last place to spend a frame.
        if (courses == Hatch.Fewest && (yHi - yLo) < (xHi - xLo))
        {
            for (double y = yLo + hatch; y < yHi - 1e-9; y += hatch)
            {
                walls.Add(new(x1, y, x2, y, hull, Unseen: !hatchDrawn));
            }
            return;
        }

        for (double x = xLo + hatch; x < xHi - 1e-9; x += hatch)
        {
            walls.Add(new(x, y1, x, y2, hull, Unseen: !hatchDrawn));
        }
    }

    /// <summary>#883 · Which way a solid's internal hatch runs. The strokes are what stop a body being held
    /// inside a mass (<see cref="AddSolidMass"/>), and either direction does that equally well — a band
    /// narrower than a captain cannot hold one whichever way round it lies — so this is a question about
    /// COST and about PICTURE, never about the law.</summary>
    public enum Hatch
    {
        /// <summary>Across whichever side is shorter: the fewest strokes that can seal the box. Right for
        /// every fixture in the building, and the reason the honest box is affordable in fifteen hundred
        /// rooms.</summary>
        Fewest,

        /// <summary>Stacked upright, whatever it costs. One caller: the monolith, whose masonry is authored
        /// — <c>TheMonolithShowsItsSizeTests</c> counts forty-nine parallel strokes across the one object in
        /// the game whose own card says <i>no seam</i>, and a 121 metre signature is not something a
        /// furniture sweep re-cuts on its way past.</summary>
        Upright,
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

    /// <summary>#563 · Scatter a few real buildings across an AUTHORED ground's empty parts, well clear of
    /// its signature. Miranda's maze and Luna's rails are canon and are not touched; what gets filled is the
    /// open regolith around them, which on Miranda is some 88% of the field.
    ///
    /// <para>Kept away from the deep anchor by <paramref name="anchorX"/>/<paramref name="anchorY"/>, so a
    /// structure can never crowd the monolith or the mass-driver muzzle — the thing you walked out there to
    /// see must stay the thing you see.</para></summary>
    private static void AddOutlyingStructures(
        System.Collections.Generic.List<Wall> walls,
        System.Collections.Generic.List<Doorway> doorways,
        System.Collections.Generic.List<(double X, double Y)> centres,
        in Field f, string bodyId, double anchorX, double anchorY,
        System.Collections.Generic.List<(double X, double Y, double R)>? keepOut = null,
        System.Collections.Generic.List<(double X, double Y, double R)>? footprints = null)
    {
        double minX = f.LeftX + EdgeMargin, maxX = f.RightX - EdgeMargin;
        double minY = f.BottomY + 4, maxY = f.LandingBandY - 6;
        const double ClearOfSignature = 26.0;

        // #585 · THE LEDGER THAT KNOWS ABOUT EVERYTHING. Owner, on a Miranda site: "check this structure
        // out... it functions but is kind of funny" — a shelter drum, an outlying hut and a maze fixture had
        // grown into one accidental mega-complex, with doorways opening onto another building's solid mass.
        //
        // Two mistakes, both here. This kept its OWN claim list, so it could not see the shelters at all.
        // And it tested a bare 30 du between CENTRES, which is only honest for a circle: AddStructure lays a
        // freely-rotated box up to 20 x 16 with walls up to 3 du thick, which sweeps a radius near 16 — so
        // two buildings 30 du apart genuinely overlap, and the generator was doing what it was told.
        //
        // Now it claims real radii (SurfaceStructure.KeepOutRadius, right at every angle) in a ledger that
        // starts with the shelters already in it, plus a little elbow so a wall never lands on a doorway.
        const double Elbow = 1.5;
        var claimed = new System.Collections.Generic.List<(double X, double Y, double R)>(keepOut ?? []);
        int placed = 0;

        for (int i = 0; i < 24 && placed < 4; i++)
        {
            double cx = Lerp(minX, maxX, Frac(bodyId, $"outly:x:{i}"));
            double cy = Lerp(minY, maxY, Frac(bodyId, $"outly:y:{i}"));
            double size = 8 + (4 * Frac(bodyId, $"outly:size:{i}"));

            // The spot and radius this building will ACTUALLY occupy, edge clamp included.
            (cx, cy, double radius) = StructureFootprint(f, cx, cy, size, bodyId, $"outly:{i}");

            // Never near the signature, and never on top of ANYTHING already standing.
            if (System.Math.Sqrt(((cx - anchorX) * (cx - anchorX)) + ((cy - anchorY) * (cy - anchorY))) < ClearOfSignature)
            {
                continue;
            }
            bool clash = false;
            foreach ((double px, double py, double pr) in claimed)
            {
                double gap = System.Math.Sqrt(((cx - px) * (cx - px)) + ((cy - py) * (cy - py)));
                clash |= gap < radius + pr + Elbow;
            }
            if (clash)
            {
                continue;
            }

            claimed.Add((cx, cy, radius));
            AddStructure(walls, doorways, centres, f, cx, cy, size, bodyId, $"outly:{i}", footprints);
            placed++;
        }
    }

    /// <summary>#563 · Place one of <see cref="SurfaceStructure"/>'s buildings on the ground: seeded shape,
    /// angle, door count and wall thickness, clamped so the whole footprint stays inside the safe span (a
    /// building clipped by the edge lane would lose the face its doorway was on and become a solid block).
    ///
    /// <para>Thickness is seeded 1.2..2.4 du — the owner's Greenland longhouse: on a cold world you build
    /// out of what is under your boots, and if the wall is also holding an atmosphere you build it fat.</para></summary>
    /// <summary>#585 · Where a structure of this size will ACTUALLY end up, and how much room it will take.
    ///
    /// <para>The last hiding place of the same bug. <see cref="AddStructure"/> clamps its centre inward so
    /// the walls stay off the edge lanes — and it did that AFTER the caller had already claimed the
    /// unclamped spot. So a building seeded near the rim was claimed in one place and built in another, and
    /// the ledger that was supposed to keep buildings apart was recording a position nothing stood at.</para>
    ///
    /// <para>Both placers now claim what this returns, so the ledger and the ground agree.</para></summary>
    private static (double X, double Y, double R) StructureFootprint(
        in Field f, double cx, double cy, double size, string bodyId, string tag)
    {
        SurfaceStructure.Spec spec = OrdinaryStructure(f, cx, cy, size, bodyId, tag);
        return (spec.CentreX, spec.CentreY, SurfaceStructure.KeepOutRadius(spec));
    }

    /// <summary>#606 · ONE description of an outlying building, asked twice.
    ///
    /// <para>This function and <see cref="AddStructure"/> used to hold the same four rolls and the same
    /// clamps side by side — the ledger's copy and the builder's copy of one fact, which is the failure this
    /// whole file is annotated with. They are the same sentence now, and <see cref="SurfaceStructure.Ordinary"/>
    /// is where "what a hut on this ground looks like" actually lives, so a lift head that wants to pass for
    /// one has something to ask instead of numbers to imitate.</para></summary>
    private static SurfaceStructure.Spec OrdinaryStructure(
        in Field f, double cx, double cy, double size, string bodyId, string tag)
    {
        SurfaceStructure.Spec spec = SurfaceStructure.Ordinary(
            cx, cy, size,
            thickFrac: Frac(bodyId, $"{tag}:thick"),
            angleFrac: Frac(bodyId, $"{tag}:angle"),
            doors: 1 + Face(bodyId, $"{tag}:doors", 2),
            shapeFace: Face(bodyId, $"{tag}:shape", 3));

        // Keep the whole thing (walls included) off the edge lanes.
        double halfW = (spec.Width / 2) + spec.WallThickness, halfH = (spec.Height / 2) + spec.WallThickness;
        return spec with
        {
            CentreX = System.Math.Clamp(cx, f.LeftX + EdgeMargin + halfW, f.RightX - EdgeMargin - halfW),
            CentreY = System.Math.Clamp(cy, f.BottomY + 2 + halfH, f.LandingBandY - 2 - halfH),
        };
    }

    private static void AddStructure(
        System.Collections.Generic.List<Wall> walls,
        System.Collections.Generic.List<Doorway> doorways,
        System.Collections.Generic.List<(double X, double Y)> centres,
        in Field f, double cx, double cy, double size, string bodyId, string tag,
        System.Collections.Generic.List<(double X, double Y, double R)>? footprints = null)
    {
        // 1.6..3.0 du of piled regolith — the owner's Greenland longhouse, and comfortably above the
        // captain's own 1.4 du width so the hatching never emits a segment shorter than a body. The numbers
        // and the clamps live in OrdinaryStructure now, because the claim ledger has to agree with them.
        SurfaceStructure.Spec spec = OrdinaryStructure(f, cx, cy, size, bodyId, tag);

        SurfaceStructure.Built built = SurfaceStructure.Build(spec);
        centres.Add((spec.CentreX, spec.CentreY));
        footprints?.Add((spec.CentreX, spec.CentreY, SurfaceStructure.KeepOutRadius(spec)));
        walls.AddRange(built.Walls);
        foreach (SurfaceStructure.Doorway d in built.Doorways)
        {
            doorways.Add(new Doorway(d.X1, d.Y1, d.X2, d.Y2));
        }
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

    /// <summary>#649 · <see cref="AddSolidMass"/>, clamped into the field's kept-open edge lanes — what an
    /// authored signature needs once it is big enough to reach them.
    ///
    /// <para>The monolith went onto Phobos as a clamped BOX and the reachability audit caught it in one run:
    /// 99 cells of sealed ground at (−8, −234.5), which is the inside of the slab. A solid drawn as four
    /// lines has an interior, and an interior is somewhere a captain could stand if only they could get in.
    /// The two properties are independent and the object needs both, so there is a helper that has both
    /// rather than a caller that remembers to.</para></summary>
    private static void AddClampedSolidMass(System.Collections.Generic.List<Wall> walls, in Field f,
        double x1, double y1, double x2, double y2, bool hull, bool hatchDrawn = true,
        Hatch courses = Hatch.Fewest)
    {
        x1 = System.Math.Max(f.LeftX + EdgeMargin, x1);
        x2 = System.Math.Min(f.RightX - EdgeMargin, x2);
        AddSolidMass(walls, x1, y1, x2, y2, hull, hatchDrawn, courses);
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
