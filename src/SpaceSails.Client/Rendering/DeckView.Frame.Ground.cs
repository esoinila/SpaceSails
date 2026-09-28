using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// DeckView.Frame.Ground — THE WORLD THE FRAME IS DRAWN ON, AND WHAT IS BUILT ON IT. The first seven
// passes of `Draw`, in the order the conductor calls them: the room backdrops and the grid's ribs, the
// hatch over every chamber nobody has looked into yet, the filled structure and the filled furniture, the
// walls, the room names, and #563's markings on the ground. Everything in this file is drawn BENEATH the
// dark and beneath every figure — a wall drawn after a room label paints over the label, which is why the
// order these are called in is the conductor's business and never theirs. Pure motion out of the 1,393-line
// DeckView.Frame.cs: not one mark moved, and EveryFrameHashesTheSameTests says so per frame.

public sealed partial class DeckView
{
    /// <summary>#870 lane 7b · THE GROUND, AND WHAT LIES ON IT — the room backdrops under every vector
    /// overlay, the grid's cold ribs, #563's scenery, and the falloff into the dark at an unseen bound.
    /// The first pass of the frame: every pass after it is drawn ON this.</summary>
    private void PaintTheGround(
        DeckPlan plan, float scale, float ox, float oy, double simTime,
        Func<double, double, (float X, float Y)> project)
    {
        // #759 · WHAT TIME IT IS IN THE PARK — asked once per frame, and of Core. Not a clock: a pure
        // function of the sim-time this frame is being drawn at and of the site whose green it is, which is
        // exactly why it is asked HERE, at the pen, rather than baked into the plan when the captain walked
        // in. 1 on every deck in the game that is not a park, so every line below that reads it is
        // arithmetic that changes nothing at all off that one floor.
        double grow = plan.Grow is { } rig ? ParkDay.LevelAt(simTime, rig.SiteId) : 1.0;

        // Room backdrops sit UNDER every vector overlay (walls, consoles, avatar, labels stay on top
        // for legibility — the hybrid look). Each is top-left at (X, Y) deck-units, W×H deck-units.
        // Registration is idempotent, so calling it per frame is cheap.
        foreach (DeckPlan.Backdrop bd in plan.Backdrops)
        {
            (float bx, float by) = project(bd.X, bd.Y);
            // #759 · …and the gravel is lit by something other than the building. SUBTLY: the room stays
            // legible at the bottom of the cycle, because the register is "never broken, never right" and a
            // park you cannot see the paths in has broken rather than drifted. The loud half of the reading
            // is the lamps themselves, in the pass immediately below.
            float alpha = bd.GrowLight ? bd.Alpha * (float)(GrowFloor + (GrowSwing * grow)) : bd.Alpha;
            _renderer.DrawImage(_renderer.RegisterImage(bd.Url), bx, by, bd.W * scale, bd.H * scale, alpha);
        }

        // #759 · THE MASTS KEEP THE DAY. Five posts against the far wall, drawn as lamp heads whose reach
        // and warmth are the cycle's own level — small dark discs at the bottom of it, a wash of
        // horticultural white at the top. ALWAYS drawn and never skipped when the lamps are off: a mark
        // that comes and goes is a frame whose call count depends on the hour, and a lamp that is off is
        // still a lamp.
        if (plan.Grow is { } lamps)
        {
            foreach ((float mx, float my) in lamps.Masts)
            {
                (float lx, float ly) = project(mx, my);
                _renderer.DrawCircle(
                    lx, ly, (float)(GrowHeadDu + (GrowReachDu * grow)) * scale,
                    GrowLampFill(grow), GrowLampEdge(grow), 1f);
            }
        }

        for (int gx = -22; gx <= 28; gx += 4)
        {
            DrawSeg(project(gx, -9.6), project(gx, 9.6), new RgbaColor(255, 255, 255, 10), 1f);
        }

        // #563 · THE FIELD FALLS INTO THE DARK. An UNSEEN wall stops the captain and draws nothing, which
        // fixed the owner's "square border … it seems artificial on a Moon" and immediately created the
        // other half of the problem: an invisible wall you walk into with no warning is worse than a fence,
        // not better. So the ground darkens over the last several deck units before any unseen bound.
        //
        // It is honest rather than decorative. An airless moon has no atmosphere to scatter light, so
        // regolith the lamp never reaches is simply black — the field does not END, it stops being visible,
        // and you read "there is nothing out that way" BEFORE you touch anything.
        //
        // THE FALLOFF DEPTH WOBBLES, and that is the whole point of doing it this way. Fading on the same
        // axis-aligned bounds would have drawn the identical rectangle in a softer pencil and left the
        // complaint untouched ("at least not obviously so with square area"). The wobble is keyed to world
        // position, not to time or camera, so the dark edge is a fact about the place and holds still while
        // you walk along it.
        //
        // Hung off the unseen walls themselves, so it appears exactly where a hidden bound is and nowhere
        // else — a ship's plan has none and is untouched.
        // #563 · TERRAIN, under the falloff so ground near the bound fades into the dark with everything
        // else. Owner: "put something more interesting in the landscape." These are drawn and never
        // collided — they live in their own array precisely so no oversight can give them substance.
        foreach (SpaceSails.Core.SurfaceScenery.Mark m in plan.Scenery)
        {
            // #563 · OFF THE GLASS IS NOT DRAWN. The regolith used to be one field's worth of weather and
            // could be painted whole without anybody noticing; it is a lattice of tiles now, and the frame
            // carries nine of them. Measured on the pinned regolith run, welding the chunk in put the
            // walked-view pen up by 661,200 calls — two and a half times the frame — and every one of the
            // new ones was a crater rim several hundred deck units off the side of the screen.
            //
            // This is the trap the issue named (the collision index exists because surface cost once timed
            // the shuttle ride out), arriving through the renderer rather than through the sim. The cull is
            // a screen-space reject on a segment whose ends are both past one edge: no picture changes,
            // because nothing that was visible is skipped.
            (float sx1, float sy1) = project(m.X1, m.Y1);
            (float sx2, float sy2) = project(m.X2, m.Y2);
            if (OffTheGlass(sx1, sy1, sx2, sy2))
            {
                continue;
            }

            (RgbaColor ink, float wide) = m.Of switch
            {
                SpaceSails.Core.SurfaceScenery.Kind.CraterRim => (new RgbaColor(74, 70, 64, 190), 1.4f),
                SpaceSails.Core.SurfaceScenery.Kind.Scree => (new RgbaColor(62, 58, 54, 150), 1f),
                SpaceSails.Core.SurfaceScenery.Kind.Ridge => (new RgbaColor(84, 78, 70, 200), 1.7f),
                _ => (new RgbaColor(58, 60, 66, 175), 1.3f),
            };
            DrawSeg((sx1, sy1), (sx2, sy2), ink, wide);
        }

        DrawUnseenFalloff(plan, scale, ox, oy);
    }

    /// <summary>#870 lane 7b · #371's still-UNSEEN chambers, painted as hatched voids over the floor and
    /// UNDER everything that follows — the walls, fittings and consoles inside them are skipped by the
    /// passes below, so there is nothing left to poke through.</summary>
    private void HideWhatNobodyHasLookedInto(
        System.Collections.Generic.IReadOnlyList<(double X0, double Y0, double X1, double Y1, int Seen)>? darkRegions,
        float scale, Func<double, double, (float X, float Y)> project)
    {
        // #371 Phase 3 fog: paint the still-UNSEEN forced chambers as dark hatched voids — unknown ground
        // behind a freshly-forced door — over the floor/grid, under everything that follows (the walls and
        // consoles inside are skipped, so nothing pokes through). Explored/visible chambers get no void.
        if (darkRegions is { Count: > 0 })
        {
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (st != 0)
                {
                    continue;
                }
                (float vx0, float vy0) = project(x0, y1); // deck +y is up on screen → y1 is the top edge
                float vw = (float)(x1 - x0) * scale, vh = (float)(y1 - y0) * scale;
                FillRect(vx0, vy0, vw, vh, VoidFill);
                for (float vhy = vy0 + 6f; vhy < vy0 + vh; vhy += 7f) // crude hatch
                {
                    DrawSeg((vx0, vhy), (vx0 + vw, vhy), VoidHatch, 1f);
                }
                _renderer.DrawText(vx0 + vw / 2f, vy0 + vh / 2f, "· ? ·", VoidText, "10px monospace", TextAlign.Center);
            }
        }
    }

    /// <summary>#870 lane 7b · #537's metal foam: the thickness a wall is made of, filled and hatched as
    /// CUT MATERIAL, so the one stretch of it that is hollow reads exactly like all the rest. The banner
    /// inside carries the owner's own words for both halves of it.</summary>
    private void FillTheStructure(
        DeckPlan plan, float scale, Func<double, double, (float X, float Y)> project)
    {
        // ── #537 · STRUCTURE, FILLED. Owner, reading the deck after the wall padding shipped: "we should cover
        //    those narrow spaces … all of them … if we can see into them from the hall then they don't hide
        //    anything", then how it should look — "some kind of fill there would make it look like the space is
        //    filled with stuff" — and then what it IS: "I like to think it is structurally optimal metal foam
        //    and technology of the ship :-D  metal foam :-D"
        //
        //    He is right about the bug and right about the material. A run drawn as two lines round a black gap
        //    reads as a SPACE, and a hiding place drawn as a space is not hidden — a captain could read every
        //    void off the map without knocking on anything, which made the clue redundant and the sounder a
        //    formality. And metal foam is the honest answer to why the walls are thick at all: closed-cell
        //    metallic foam is stiff for its mass, which is exactly what you fill a whipple layer with. The
        //    thickness is engineering, not an excuse for a hiding place.
        //
        //    So it is drawn as CELLS rather than as hatching: a stochastic scatter that reads as foam packed
        //    with kit, and — the part that matters — reads identically along its whole length, so the one
        //    stretch of it that is hollow looks like all the rest until somebody sounds it.
        foreach (DeckPlan.Structure s in plan.Structures)
        {
            (float fx0, float fy0) = project(Math.Min(s.X0, s.X1), Math.Max(s.Y0, s.Y1));
            (float fx1, float fy1) = project(Math.Max(s.X0, s.X1), Math.Min(s.Y0, s.Y1));
            float fw = fx1 - fx0, fh = fy1 - fy0;
            if (fw <= 0 || fh <= 0)
            {
                continue;
            }

            FillRect(fx0, fy0, fw, fh, FoamFill);

            // SECTION HATCH — the drawing convention for CUT MATERIAL, which is exactly what this is. Owner:
            // "could we get like a cross-section dashed line instead of the current fill?" He is right and it is
            // the better answer for two reasons. A deck plan IS a section drawing, so 45° hatching is the mark an
            // engineer would already read as "you are looking at the inside of a wall" — no legend needed. And it
            // is uniform: a stochastic scatter has clumps and sparse patches, and a player hunting for hiding
            // places will read a sparse patch as a lead. Hatching has nothing to find in it, which is the whole
            // job — the one stretch that is hollow must look like every other stretch until somebody knocks.
            float step = 0.85f * scale;
            if (step < 3f)
            {
                continue;   // finer than this is a smear at this zoom, not a hatch
            }

            float dash = step * 0.55f, gap = step * 0.35f;

            // 45° in SCREEN space: y = x − c. Sweep c so the family covers the whole rectangle.
            for (float c = fx0 - fh; c <= fx1; c += step)
            {
                // Where that diagonal enters and leaves this rectangle.
                float tFrom = Math.Max(fx0, c + fy0);
                float tTo = Math.Min(fx1, c + fy1);

                for (float td = tFrom; td < tTo; td += dash + gap)
                {
                    float tEnd = Math.Min(td + dash, tTo);
                    DrawSeg((td, td - c), (tEnd, tEnd - c), FoamHatch, 1f);
                }
            }
        }
    }

    /// <summary>#870 lane 7b · #868's fittings, filled — over the floor and UNDER the walls, so a wall
    /// segment that happens to be a desk's own edge still draws on top of its fill and nothing a captain
    /// can walk into is painted over. The banner inside is the owner's ruling.</summary>
    private void FillTheFurniture(
        DeckPlan plan, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        // ── #868 · THE FURNITURE, FILLED. Owner, reading a cold room off the plan: "The graphics kind of does
        //    not show there being a table" · "The bench is a line" · "The Shelving is clear as furniture
        //    goes" — one room, the negative control and the positive control three paces apart. His fix, in
        //    his own words: "could the table just be a different color rectangle in front of the chair, so
        //    arms (and papers) could rest on it?", sealed with "I think table should be similar just say
        //    table."
        //
        //    It is #537's argument said about the things IN a room rather than the things a room is made of.
        //    A rectangle drawn as four lines round a dark gap reads as SOMEWHERE YOU COULD STAND; the eye
        //    only calls it furniture once it is filled. Drawn HERE — over the floor, under the walls — so a
        //    wall segment that happens to be a fixture's own edge still draws on top of its fill and nothing
        //    a captain can walk into is painted over.
        //
        //    Every rectangle is Core's published box (RingOffice.Fixture) handed down whole. The pen measures
        //    nothing: a renderer that worked out where a desk was would be the second author of one desk,
        //    which is this house's own named way of ending up with a drawn shape that disagrees with the sim.
        foreach (DeckPlan.FurnitureSpot f in plan.Furniture)
        {
            (float gx0, float gy0) = project(Math.Min(f.X0, f.X1), Math.Max(f.Y0, f.Y1));
            (float gx1, float gy1) = project(Math.Max(f.X0, f.X1), Math.Min(f.Y0, f.Y1));
            float gw = gx1 - gx0, gh = gy1 - gy0;
            if (gw <= 0 || gh <= 0)
            {
                continue;   // a degenerate box is a SEGMENT (a screen, a chamber's own bench) and draws as one
            }
            if (darkState((f.X0 + f.X1) / 2.0, (f.Y0 + f.Y1) / 2.0) == 0)
            {
                continue;   // #371 · in a room nobody has looked into yet, there is no furniture to see
            }

            RgbaColor ink = f.Tone switch
            {
                1 => StorageFill,
                2 => SeatingFill,
                _ => SurfaceFill,
            };
            FillRect(gx0, gy0, gw, gh, ink);

            // …and a keyline round it, which is what makes a 2 du slab read at plate scale rather than
            // becoming a smudge the moment the camera pulls back.
            DrawSeg((gx0, gy0), (gx1, gy0), FurnitureEdge, 1f);
            DrawSeg((gx1, gy0), (gx1, gy1), FurnitureEdge, 1f);
            DrawSeg((gx1, gy1), (gx0, gy1), FurnitureEdge, 1f);
            DrawSeg((gx0, gy1), (gx0, gy0), FurnitureEdge, 1f);
        }
    }

    /// <summary>#870 lane 7b · The walls, in the ink of what they are made of — #589's body stone, #605's
    /// department livery, #677's third material that takes no ink from either, and #563's unseen bound
    /// that collides and draws nothing at all.</summary>
    private void DrawTheWalls(
        DeckPlan plan, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            // #563 · An UNSEEN wall is never drawn — the open field's envelope, which collides but has no
            // object in the world to be. It is checked before the fog so it stays invisible in every
            // lighting state, including the lit deck where the fog test passes everything.
            if (w.Unseen)
            {
                continue;
            }

            // #563 · …and neither is a wall that is off the glass. Same reject as the scenery above and for
            // the same reason: the frame carries a chunk of tiles now, not one field.
            (float wsx1, float wsy1) = project(w.X1, w.Y1);
            (float wsx2, float wsy2) = project(w.X2, w.Y2);
            if (OffTheGlass(wsx1, wsy1, wsx2, wsy2))
            {
                continue;
            }

            // #371 Phase 3 fog · #442 · A WALL IN A CHAMBER NOBODY HAS LOOKED INTO DRAWS DIM. IT DOES NOT
            // VANISH.
            //
            // This used to read `if (ws == 0) { continue; }`, and it was the purest invisible wall in the
            // game: collision has never heard of the fog, so inside a still-unseen forced chamber every wall
            // was fully solid and completely undrawn. Owner, live 2026-07-26: "See the invisible wall there
            // now?" … "There should be a refactor to make sure the visible and physics wall ALWAYS are 1 to
            // 1 the same." OneWallOneTruthTests generates that wall now — 131 of them over 40 seeded fields,
            // every one a place where the boot is stopped, the shamble is stopped, the round is stopped, the
            // eye is broken, and the pen drew nothing.
            //
            // #442 offers two structural answers and says to pick one and mean it: an unseen wall draws as a
            // darkened hint (still there, still solid), or an unseen region genuinely has no collision until
            // it is discovered. THE FIRST, and the reason is the door. A chamber only enters this state once
            // its door has been FORCED, so it is reachable the instant it exists; taking the collision away
            // would let a captain walk through the stone of a room they had opened and not yet looked into,
            // which trades an invisible wall for a wall that is not there at all — the same defect facing the
            // other way. Leaving it solid and drawing it costs the fog nothing it was for: the room's SHAPE
            // shows as dim linework over the hatched void, and everything IN it — the consoles, the labels,
            // the furniture — stays hidden by the passes that still test this state.
            //
            // Unseen and explored share one ink deliberately. Two dim greys would be a distinction the eye
            // cannot make, and the sim would then be keeping a promise nobody could read.
            int ws = darkState((w.X1 + w.X2) / 2.0, (w.Y1 + w.Y2) / 2.0);

            // #589 · A body's stone is drawn in a body's colour. Falls back to the old warm grey-brown
            // when a plan carries no ink (the ship, the stations, anything made of steel), so nothing that
            // is not a world changes at all.
            RgbaColor stone = plan.StoneInk is { } ink ? new RgbaColor(ink.R, ink.G, ink.B) : StoneLine;

            // #605 · A MADE structure can carry its own ink too. Owner, riding floors cut from the same
            // bones: "Let's like change the wall colors on different floors... now they look too same" —
            // answered with department livery rather than a per-floor gradient, so the colour is a language
            // and not decoration. Null everywhere it has always been null (the ship, the stations, the
            // wrecks are steel), so nothing outside the Hive changes by a pixel.
            RgbaColor hull = plan.HullInk is { } made ? new RgbaColor(made.R, made.G, made.B) : HullLine;

            // #677 · …AND A THIRD MATERIAL, WHICH TAKES NO INK FROM EITHER OF THEM. Both branches above read
            // a palette — the department that painted this corridor, the moon this rock came out of — and a
            // palette is an ANSWER. The found halls are drawn in one flat constant, ahead of both, because
            // the day a livery or a body colour reached them the walls would start saying whose they were.
            RgbaColor color = ws is 0 or 1 ? ExploredWall
                : w.IsWindow ? WindowLine
                : w.IsSeamless ? SeamlessLine
                : w.IsStone ? stone
                : w.IsHull ? hull
                : InnerLine;
            // Stone is drawn as heavy as hull: it is just as solid, and a monolith you could mistake for
            // rubble is a monolith that stops being the centrepiece of the moon it stands on. Seamless is
            // heavier than either, because it is the one surface in the game with no line-work inside it and
            // weight is all the drawing has left to say SOLID with.
            DrawSeg(project(w.X1, w.Y1), project(w.X2, w.Y2), color,
                w.IsSeamless ? 3.5f : w.IsHull || w.IsStone ? 2.5f : 1.5f);
        }
    }
}
