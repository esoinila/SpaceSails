using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: one frame of the deck (part of DeckView).
//
// #870 lane 7b · Draw was 1,058 lines in one method, with five banners inside itself marking where one
// pass of the pen ended and the next began. It is the conductor now: it sets the projection up, arms and
// disarms the lamp, and calls the passes IN ORDER — and the order is the whole picture, which is why the
// split was made under a snapshot (EveryFrameHashesTheSameTests pins the ordered draw-call list of
// thirty-three real frames by sha256). Every pass keeps the comments it was written with.
public sealed partial class DeckView
{
    // #424 HULL-SHUDDER · the unison pause. When a shudder fires on a populated interior deck (the ship,
    // a haven bar/hall) the client hands a FROZEN npc-hold time here for the held-breath beat: every present
    // NPC/patron is filled at that ONE shared timestamp — so their idle thermal jitter and patrol/pace all
    // stop together (the synchronized freeze IS the feature) — and their heads turn up as one. Null the rest
    // of the time, when the deck fills live at simTime. The deck-shake itself rides the render pan (panX/panY),
    // a pure transient offset that never moves an entity anchor.
    // #424 THE UNEXPLAINED SIGNAL · the crew glance. A companion ambient event: when a faint distant buzzer
    // sounds off-deck the STAFF (not the drinking patrons) briefly catch each other's eye — <paramref
    // name="crewGlance"/> turns every working crew member (barkeep, customs, the ship's own droids) to face
    // the nearest other crew member for the beat, a synchronized look. The patrons keep animating, oblivious.
    public void Draw(DeckPlan plan, int widthPx, int heightPx, double simTime, in State state,
        double panX = 0, double panY = 0, SurfaceHud? surface = null, double? npcHoldTime = null,
        bool crewGlance = false)
    {
        _perf?.BeginDraw();     // #841 · null in every build nobody armed; see FramePerf
        _renderer = _canvas;    // never inherit a mask from a frame that threw
        _renderer.BeginFrame(widthPx, heightPx, state.Dark ? Pitch : Floor);

        Placement place = PlacementFor(plan, widthPx, heightPx, state.AvatarX, state.AvatarY, panX, panY);
        float scale = place.Scale, ox = place.Ox, oy = place.Oy;

        // #870 lane 7b · THE PROJECTION, WRITTEN DOWN ONCE AND HANDED ROUND. Every pass below is given
        // this rather than a scale and an origin to multiply out for itself: the arithmetic that says
        // where a deck unit lands on the glass exists in exactly one place on this page, and it is the
        // exact inverse of the one the click reads back through Placement.ToDeck (#729).
        Func<double, double, (float X, float Y)> project =
            (dx, dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // #708 · ON A DARK FLOOR THE PEN GOES BEHIND THE LAMP. Everything from here to the sentries is the
        // WORLD — ground, walls, doors, plates, fixtures, husks, bodies — and on a dark floor none of it
        // exists outside the headlights. The mask is disarmed again before the instruments, which are not
        // part of the world and never were.
        if (state.Dark)
        {
            _mask.Arm(state.AvatarX, state.AvatarY, state.HeadingRad, LampRingDu, scale, ox, oy);
            _renderer = _mask;
        }

        // #371 Phase 3 fog: the visibility state of a point against the forced-chamber overlay — -1 = not in
        // any chamber (draw as normal), 0 = unseen (hidden under the void), 1 = explored (dim), 2 = visible.
        var darkRegions = surface?.DarkRegions;
        int DarkState(double x, double y)
        {
            if (darkRegions is null)
            {
                return -1;
            }
            int best = -1;
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (x >= x0 && x <= x1 && y >= y0 && y <= y1 && st > best)
                {
                    best = st; // a point in overlapping rects takes the most-revealed state
                }
            }
            return best;
        }

        // …and the same, handed round as one answer rather than as the overlay to re-search.
        Func<double, double, int> darkState = DarkState;

        // Ship-only dressing (cargo crates, shuttle cradle, reactor, cantina tables) is hardcoded to
        // the ship's geometry — a bare haven room has none of it, but a docked complex still contains
        // the ship. Everything else (backdrops, walls, doors, labels, consoles, droids, the avatar) is
        // plan-driven and general.
        bool isShip = plan.ShipFixtures;

        // ── #870 lane 7b · THE ORDER IS THE PICTURE ────────────────────────────────────────────────
        //
        //    A deck plan is painted the way a scene is: the ground, then what is built on it, then what
        //    stands in it, then the dark, then the instruments — each pass covering some of what the one
        //    before it laid down. NOT ONE LINE OF THIS MAY BE REORDERED, and that is not a style note:
        //    a wall drawn after a room label paints over the label, and a plate drawn before the fan's
        //    smudges hides a contact heard through a wall. The snapshot guard pins this list.
        //
        //    #841 / Lab 46 · …AND IT IS ALSO THE SEAM A CLOCK GOES ON. One timestamp after each pass, so a
        //    pass's cost is the gap to the one before it. `_perf` is null unless ?perf=1 armed it, the
        //    names are LITERALS (a pass renamed without its mark is red — see FramePerf.Mark), and a pass
        //    behind an `if` is marked OUTSIDE the `if` on purpose: "the ship's dressing cost nothing on
        //    this floor because there is no ship" is a reading, and a row that came and went between
        //    frames would be a table that changes shape while you read it.

        PaintTheGround(plan, scale, ox, oy, project);
        _perf?.Mark("PaintTheGround");
        HideWhatNobodyHasLookedInto(darkRegions, scale, project);
        _perf?.Mark("HideWhatNobodyHasLookedInto");
        FillTheStructure(plan, scale, project);
        _perf?.Mark("FillTheStructure");
        FillTheFurniture(plan, project, darkState);
        _perf?.Mark("FillTheFurniture");
        DrawTheWalls(plan, project, darkState);
        _perf?.Mark("DrawTheWalls");
        DrawTheDoors(plan, in state, project);
        _perf?.Mark("DrawTheDoors");
        NameTheRooms(plan, project, darkState);
        _perf?.Mark("NameTheRooms");
        MarkTheGround(surface, scale, project);
        _perf?.Mark("MarkTheGround");

        if (isShip)
        {
            DressTheShip(in state, simTime, scale, project);
        }

        _perf?.Mark("DressTheShip");

        DrawTheSeats(plan, scale, project);
        _perf?.Mark("DrawTheSeats");
        DrawTheFigures(plan, simTime, npcHoldTime, crewGlance, scale, project);
        _perf?.Mark("DrawTheFigures");

        // ── #708 · AND HERE THE DARK IS LAID DOWN. The world is drawn; the mask comes off; the black goes on
        //    over everything the headlights do not reach, with a hard edge where the cone stops.
        //
        //    Everything BELOW this line is drawn over the dark on purpose, and each for its own reason:
        //    a deployed sentry (it carries a lamp — you can see a light in a dark hall even if you cannot
        //    see what it lights), the motion fan's smudges and ghosts (an instrument, #591, whose whole
        //    worth is hearing what you cannot see), the overload countdown (a lit display), the blood and
        //    the screen-flash (they happen to YOU), the captain's own mark, and the corner gauges.
        if (state.Dark)
        {
            _renderer = _canvas;
            PaintTheDark(widthPx, heightPx, in state, scale, ox, oy);
        }

        _perf?.Mark("PaintTheDark");

        DrawTheSentries(surface, simTime, scale, project);
        _perf?.Mark("DrawTheSentries");
        DrawWhatTheFanHeard(surface, simTime, scale, project);
        _perf?.Mark("DrawWhatTheFanHeard");
        CountDownTheOverload(surface, scale, project);
        _perf?.Mark("CountDownTheOverload");
        DrawTheConsoles(plan, in state, project, darkState);
        _perf?.Mark("DrawTheConsoles");
        DrawTheCaptain(in state, surface, widthPx, heightPx, scale, project);
        _perf?.Mark("DrawTheCaptain");
        DrawTheInstruments(in state, surface, simTime, widthPx, heightPx, ox);
        _perf?.Mark("DrawTheInstruments");

        _mask.Disarm();     // #708 · the lamp is a per-frame fact; nothing survives the frame it was aimed in
        _renderer.EndFrame();

        // #841 · …and THIS is the number #841 has been missing: the one line of the frame that crosses into
        // JavaScript and hands the recorded command buffer to the canvas (CanvasRenderer.EndFrame). Every
        // pass above only fills an array; nothing is DRAWN until here.
        _perf?.Mark(FramePerf.FlushRow);
        _perf?.CloseDraw();
    }

    /// <summary>#870 lane 7b · THE GROUND, AND WHAT LIES ON IT — the room backdrops under every vector
    /// overlay, the grid's cold ribs, #563's scenery, and the falloff into the dark at an unseen bound.
    /// The first pass of the frame: every pass after it is drawn ON this.</summary>
    private void PaintTheGround(
        DeckPlan plan, float scale, float ox, float oy, Func<double, double, (float X, float Y)> project)
    {
        // Room backdrops sit UNDER every vector overlay (walls, consoles, avatar, labels stay on top
        // for legibility — the hybrid look). Each is top-left at (X, Y) deck-units, W×H deck-units.
        // Registration is idempotent, so calling it per frame is cheap.
        foreach (DeckPlan.Backdrop bd in plan.Backdrops)
        {
            (float bx, float by) = project(bd.X, bd.Y);
            _renderer.DrawImage(_renderer.RegisterImage(bd.Url), bx, by, bd.W * scale, bd.H * scale, bd.Alpha);
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
            (RgbaColor ink, float wide) = m.Of switch
            {
                SpaceSails.Core.SurfaceScenery.Kind.CraterRim => (new RgbaColor(74, 70, 64, 190), 1.4f),
                SpaceSails.Core.SurfaceScenery.Kind.Scree => (new RgbaColor(62, 58, 54, 150), 1f),
                SpaceSails.Core.SurfaceScenery.Kind.Ridge => (new RgbaColor(84, 78, 70, 200), 1.7f),
                _ => (new RgbaColor(58, 60, 66, 175), 1.3f),
            };
            DrawSeg(project(m.X1, m.Y1), project(m.X2, m.Y2), ink, wide);
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

            // #371 Phase 3 fog: a wall inside a still-unseen forced chamber is hidden (the room is unknown
            // until the captain looks in); one in an explored-but-out-of-sight chamber draws dim.
            int ws = darkState((w.X1 + w.X2) / 2.0, (w.Y1 + w.Y2) / 2.0);
            if (ws == 0)
            {
                continue;
            }
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
            RgbaColor color = ws == 1 ? ExploredWall
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

    /// <summary>#870 lane 7b · The doors: #462's airlock interlock (only the leaf nearest the captain may
    /// stand open), #585's locked hatch drawn heaviest of all, #592's imported ink and #606's machined
    /// frame. Drawn after the walls because a door is set INTO one.</summary>
    private void DrawTheDoors(
        DeckPlan plan, in State state, Func<double, double, (float X, float Y)> project)
    {
        // Automatic airlock doors (the docking tube): shut across the passage until you near them,
        // then they retract to a stub at each jamb. Purely visual — the passage is always walkable.
        foreach (DeckPlan.Door d in plan.Doors)
        {
            if (d.Locked)
            {
                // Another berth's sealed hatch — always shut, drawn cold (steel-blue), a real wall behind.
                //
                // #585 · Owner, in the Hive: "the doors should be different color than the walls and say
                // locked on approach." The cold steel-blue already differs from every wall ink in the game;
                // what it lacked was WEIGHT — at 3.5px against hull-bright walls it read as just another
                // line. A door that will never open is the most informative object in a facility, so it is
                // drawn heaviest of all, with a second inner stroke so it looks barred rather than merely
                // shut. (The "say locked on approach" half is the console at its midpoint, which names what
                // is behind it as you come near.)
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), DoorLocked, 5.5f);
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), new RgbaColor(20, 26, 38, 220), 2.0f);
                continue;
            }
            double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
            double toDoor = Math.Sqrt((state.AvatarX - mx) * (state.AvatarX - mx)
                                    + (state.AvatarY - my) * (state.AvatarY - my));
            // #462 · THE AIRLOCK INTERLOCK. Owner, 2026-07-27: "only one door in a tube is open at a time…
            // think of airlock" — "both doors being open at the same time defeats the purpose". Doors in the
            // same group take turns: only the one NEAREST the captain may stand open, so the far end is
            // always drawn shut. That is the visible barrier the Old Ones stop at (they used to halt at a gap
            // painted open, because the captain standing at the threshold held BOTH ends retracted), and it
            // is what seals a tailgater in the tube with the built-in gun (#461) instead of letting it
            // follow you aboard. The rule itself lives in Core Airlock so CI pins it.
            double nearestPartner = double.PositiveInfinity;
            if (d.Interlock != 0)
            {
                foreach (DeckPlan.Door other in plan.Doors)
                {
                    if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                    {
                        continue;
                    }
                    double pmx = (other.X1 + other.X2) / 2.0, pmy = (other.Y1 + other.Y2) / 2.0;
                    double toOther = Math.Sqrt((state.AvatarX - pmx) * (state.AvatarX - pmx)
                                             + (state.AvatarY - pmy) * (state.AvatarY - pmy));
                    nearestPartner = Math.Min(nearestPartner, toOther);
                }
            }
            // #592 · A door is made of the hill it is set in — unless somebody paid to ship it here. The
            // ship and the stations keep the old amber (StoneInk is null there): they ARE steel, and nothing
            // about a bulkhead should start depending on which moon is outside.
            RgbaColor shut = DoorShut, leaf = DoorOpen;
            if (plan.DoorInk is { } local)
            {
                SpaceSails.Core.BodyPalette.Ink di = d.Imported
                    ? SpaceSails.Core.BodyPalette.Imported
                    : local;
                shut = new RgbaColor(di.R, di.G, di.B, 230);
                leaf = new RgbaColor(di.R, di.G, di.B, 95);
            }

            // #606 · A MACHINED DOOR IS A DIFFERENT OBJECT, not a different colour. Owner, hiding the lift
            // head in an ordinary hut: "The expensive doors would be the clue."
            //
            // Colour had already been asked to carry this and could not (#585) — violet means shelter, means
            // one ruin hatch in seven, and means the way down, so it identified nothing. Weight is a second
            // channel: a fat leaf with an inner rail and its frame picked out at the jambs, against the single
            // thin stroke every hatch on the moon is drawn with. That reads at a glance, from close, without a
            // word of copy — which is the whole technique (docs/art-manifest-hive.md).
            //
            // It still retracts. SEALED is what it looks like, not what it does: a door here that refused to
            // open would strand a captain in a lift head, and the reachability audits would be right to say so.
            float weight = d.Machined ? 6f : 3.5f;
            bool open = Airlock.MayOpen(toDoor, nearestPartner, DoorOpenRadius);
            if (open)
            {
                // Retracted: a short leaf at each jamb (25% in from each end).
                DrawSeg(project(d.X1, d.Y1), project(d.X1 + (d.X2 - d.X1) * 0.25f, d.Y1 + (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
                DrawSeg(project(d.X2, d.Y2), project(d.X2 - (d.X2 - d.X1) * 0.25f, d.Y2 - (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
            }
            else
            {
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), shut, weight);
                if (d.Machined)
                {
                    DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), new RgbaColor(18, 20, 30, 210), 2f);
                }
            }
            if (d.Machined)
            {
                // The frame: a short stub across the opening at each jamb, the way a plan draws a door that
                // was set into a hole somebody cut rather than built around.
                float jx = d.X2 - d.X1, jy = d.Y2 - d.Y1;
                float jl = MathF.Sqrt((jx * jx) + (jy * jy));
                if (jl > 0.01f)
                {
                    float nx = -jy / jl * 0.9f, ny = jx / jl * 0.9f;
                    DrawSeg(project(d.X1 - nx, d.Y1 - ny), project(d.X1 + nx, d.Y1 + ny), shut, 2.5f);
                    DrawSeg(project(d.X2 - nx, d.Y2 - ny), project(d.X2 + nx, d.Y2 + ny), shut, 2.5f);
                }
            }
        }
    }

    /// <summary>#870 lane 7b · What the structure is CALLED — #600/#612's signage on its plate first and
    /// in a dimmer ink, then #348's room labels on theirs. One pass, because the order between the two is
    /// the whole of the rule: paint on a wall must not compete with the caption over a console.</summary>
    private void NameTheRooms(
        DeckPlan plan, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        // #348: each room label on its own dark backing plate for contrast over the art panels, with
        // MED BAY drawn as the clean-room exception (see the RoomLabel* colours above).
        // #600 · SIGNAGE, painted on the structure at the size a facility actually paints it. Owner, riding
        // between floors cut from the same bones: "something different in every floor so we visually spot
        // some difference when we go to different floors."
        //
        // Drawn before the room labels and in a dimmer ink than them ON PURPOSE: this is paint on a wall the
        // captain glances at, not a caption competing with the consoles. It is big enough to read without
        // looking for it and quiet enough to ignore while doing something else.
        //
        // #612 · ON A PLATE, not merely in a louder colour. The dim-paint idea above was right about the
        // FICTION and wrong about the screen: paint over a lit corridor is hard to read, and the owner hit
        // that twice ("they are kind of hidden now", then again after the ink was brightened). A dark panel
        // behind the letters is what makes signage legible in the real world too, and it is the same trick
        // #348 already uses one size down for the room labels — so the Hive's plate and the ship's cabin
        // labels are now the same instrument at two scales, which is one thing to learn instead of two.
        foreach ((float bx, float by, string text, float px, int tone) in plan.BigLabels)
        {
            if (darkState(bx, by) == 0)
            {
                continue;
            }
            (float bxp, float byp) = project(bx, by);
            // #612 · Owner: "The meters and the floor name could be yellow here... they are kind of hidden
            // now.... it should say if the floor is pressurized also." Tone chooses the ink and nothing
            // else: tone 1 is the relief of somewhere you can breathe, tone 2 is the one that costs you, and
            // everything else is paint on a wall. A state gets the colour that state wears everywhere else
            // in the game — the same green and the same amber as the chip on the suit gauge.
            RgbaColor ink = tone switch
            {
                1 => StencilAir,
                2 => StencilDead,
                _ => StencilPaint,
            };

            // Monospace, so the width is arithmetic rather than a measurement the renderer cannot do — the
            // same 0.6-em-per-glyph estimate DrawRoomLabel has used since #348, with the baseline sitting
            // roughly three quarters down the panel (canvas draws text from its alphabetic baseline).
            float w = (text.Length * px * 0.62f) + (px * 0.9f);
            float h = px * 1.32f;
            float x0 = bxp - (w / 2f), y0 = byp - (h * 0.77f);
            FillRect(x0, y0, w, h, StencilPlate);
            DrawRectOutline(x0, y0, w, h, new RgbaColor(ink.R, ink.G, ink.B, 90));
            _renderer.DrawText(bxp, byp, text, ink, $"bold {px:0}px monospace", TextAlign.Center);
        }

        foreach ((float lx, float ly, string text) in plan.RoomLabels)
        {
            int ls = darkState(lx, ly); // #371 Phase 3 fog: hide an unseen chamber's label, dim an explored one
            if (ls == 0)
            {
                continue;
            }
            (float lxp, float lyp) = project(lx, ly);
            if (ls == 1)
            {
                _renderer.DrawText(lxp, lyp, text, ExploredText, "10px monospace", TextAlign.Center);
            }
            else
            {
                DrawRoomLabel(lxp, lyp, text, medBay: text == "MED BAY");
            }
        }
    }

    /// <summary>#870 lane 7b · #313's surface ground marks — the swept grid first, then own caches, a
    /// panic-dropped chest, #314's husks and #371's movement echoes. All of it under the movers, so a
    /// figure can stand on any of it.</summary>
    private void MarkTheGround(
        SurfaceHud? surface, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #313 surface ground overlays: own caches' ✗ marks and a panic-dropped chest (drawn under the
        // avatar/droids so a mover can stand on them).
        if (surface is { } hud)
        {
            // Beach-comber kit: the per-visit swept grid, drawn FIRST so every other ground mark sits on
            // top. A checked square is a faint dug divot (a small ring + tick); a bedrock square rings off
            // with a dim ✕ — the sweep at a glance, in the deck-plan NetHack idiom (subtle, never loud).
            if (hud.SweptSquares is { } swept)
            {
                foreach ((double swx, double swy, bool hard) in swept)
                {
                    (float sx, float sy) = project(swx, swy);
                    if (hard)
                    {
                        _renderer.DrawText(sx, sy + 3, "✕", new RgbaColor(120, 110, 95, 150), "10px monospace", TextAlign.Center);
                    }
                    else
                    {
                        _renderer.DrawCircle(sx, sy, 0.35f * scale, null, new RgbaColor(110, 130, 120, 130), 1f);
                        _renderer.DrawText(sx, sy + 3, "·", new RgbaColor(120, 150, 135, 160), "10px monospace", TextAlign.Center);
                    }
                }
            }
            foreach ((double mx, double my, bool haunted) in hud.CacheMarks)
            {
                (float sx, float sy) = project(mx, my);
                var xcol = haunted ? new RgbaColor(230, 120, 90, 230) : new RgbaColor(230, 210, 120, 230);
                _renderer.DrawText(sx, sy + 4, "✗", xcol, "bold 16px monospace", TextAlign.Center);
                if (haunted)
                {
                    _renderer.DrawText(sx, sy - 12, "yours · something walks near it", new RgbaColor(230, 120, 90, 170), "8px monospace", TextAlign.Center);
                }
            }
            if (hud.HasDroppedChest)
            {
                (float dx2, float dy2) = project(hud.DropX, hud.DropY);
                _renderer.DrawText(dx2, dy2 + 5, "🧰", new RgbaColor(200, 160, 90, 240), "15px monospace", TextAlign.Center);
                _renderer.DrawText(dx2, dy2 - 11, "dropped chest", new RgbaColor(200, 160, 90, 180), "8px monospace", TextAlign.Center);
            }
            // #314: husks of downed Old Ones — dim marks left where they fell (the forensic seed, #316).
            if (hud.Husks is { } husks)
            {
                foreach ((double hkx, double hky) in husks)
                {
                    (float sx, float sy) = project(hkx, hky);
                    _renderer.DrawCircle(sx, sy, 0.55f * scale, HuskColor, HuskColor);
                    _renderer.DrawText(sx, sy + 3, "×", new RgbaColor(90, 60, 60, 220), "bold 11px monospace", TextAlign.Center);
                }
            }
            // #371 Phase 3: movement echoes — where a contact was last seen before it slipped behind cover.
            // A dim tracker-green ripple that fades over its life; "here was movement before" (owner's ask),
            // making the motion tracker's through-wall blips all the more exciting to chase.
            if (hud.Echoes is { } echoes)
            {
                foreach ((double ex2, double ey2, double alpha) in echoes)
                {
                    (float sx, float sy) = project(ex2, ey2);
                    byte a = (byte)Math.Clamp(alpha * 180.0, 0, 180);
                    var ring = new RgbaColor(EchoColor.R, EchoColor.G, EchoColor.B, a);
                    _renderer.DrawCircle(sx, sy, (0.35f + 0.5f * (float)alpha) * scale, null, ring, 1.2f);
                    _renderer.DrawText(sx, sy + 3, "·", ring, "10px monospace", TextAlign.Center);
                }
            }
        }
    }

    /// <summary>#870 lane 7b · The ship's own dressing, and only hers: the crates in the top-port hold,
    /// the shuttle in its cradle or away doing piracy, and the reactor with #295's charge conduit. A bare
    /// haven room has none of it; a docked complex still contains the ship.</summary>
    private void DressTheShip(
        in State state, double simTime, float scale, Func<double, double, (float X, float Y)> project)
    {
        // Cargo crates: one per unit aboard (in the top-port hold now — #295).
        for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
        {
            (float cx, float cy) = project(-10 + (i % 4) * 1.9, 5 + (i / 4) * 1.6);
            DrawBox(cx, cy, 0.65f * scale, CrateColor);
        }

        // Shuttle in its cradle (bottom-port bay now — #295) — or away doing piracy.
        if (!state.ShuttleAway)
        {
            DrawShuttle(project(-6.5, -6.5), scale, simTime);
        }
        else
        {
            (float bx, float by) = project(-6.5, -6.5);
            _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
            if (Math.Sin(simTime * 0.005) > 0)
            {
                DrawSeg(project(-9, -9.9), project(-5, -9.9), new RgbaColor(255, 120, 80, 220), 3f);
            }
        }

        // Reactor + charge conduit (engine room).
        (float rx, float ry) = project(-19, 2.5);
        _renderer.DrawCircle(rx, ry, 1.6f * scale, null, InnerLine, 2f);
        double throb = 0.5 + 0.5 * Math.Sin(simTime * 0.002);
        var reactor = new RgbaColor(120, 200, 255, (byte)(90 + 70 * throb));
        _renderer.DrawCircle(rx, ry, 0.9f * scale, reactor, reactor);
        if (state.ElectricUniverse)
        {
            var conduit = new RgbaColor(255, 240, 120, (byte)(40 + 180 * state.Charge));
            DrawSeg(project(-19, 1), project(-20, -4), conduit, 3f);
        }
    }

    /// <summary>#870 lane 7b · #792/#793's seats — a top's ring and the chairs round it, the counter's
    /// tall stools, and the park's benches end by end. Free, taken, and who is already talking, in the
    /// two inks this deck has meant those things with since they were drawn.</summary>
    private void DrawTheSeats(
        DeckPlan plan, float scale, Func<double, double, (float X, float Y)> project)
    {
        // Round tables (plan-driven: the ship's cantina, a haven bar) — a ring on the floor, and — where the
        // plan bothered to say — the chairs round it and who is in them (#792).
        foreach (DeckPlan.TableTop top in plan.Tables)
        {
            (float cx2, float cy2) = project(top.X, top.Y);
            _renderer.DrawCircle(cx2, cy2, 0.9f * scale, null, InnerLine, 1.5f);
            DrawSeatsRound(cx2, cy2, top, scale);
        }

        // #792 · The tall seats at a counter — free and taken, in the same two inks the chairs use.
        foreach (DeckPlan.StoolSpot stool in plan.Stools)
        {
            (float sx, float sy) = project(stool.X, stool.Y);
            DrawBacklessSeat(sx, sy, stool.Taken, stool.RowHasSomebody, scale);
        }

        // #793 · …and the park's benches, END BY END, in exactly those two glyphs and no third one. A plank
        // has no back to draw any more than a bar stool has, and a free end beside somebody is the same offer
        // a free chair at an occupied top is. THE WHOLE BENCH is the privacy predicate
        // (SeatedSpread.CanSpreadTheCase at the ParkBench rung), so the deck has to be able to say which half
        // is gone before a captain walks the length of a 278 du park to find out by pressing.
        foreach (DeckPlan.BenchSpot end in plan.BenchSeats)
        {
            (float bx, float by) = project(end.X, end.Y);
            DrawBacklessSeat(bx, by, end.Taken, end.BenchHasSomebody, scale);
        }
    }

    /// <summary>#870 lane 7b · Everybody on the deck who is not the captain — #295's Old Ones, #583's repo
    /// crew, #538's sweep team and its lamp cone, #804's guard on his round, the working crew, #424's
    /// unison pause and crew glance, #793's held figure and #832's smeared one at the far end of the eye.
    /// The last pass of the WORLD: everything after it is drawn over the dark.</summary>
    private void DrawTheFigures(
        DeckPlan plan, double simTime, double? npcHoldTime, bool crewGlance,
        float scale, Func<double, double, (float X, float Y)> project)
    {
        // Droid pirate infantry (the ship's; a haven has none — DroidCount 0).
        // #424 HULL-SHUDDER: during the unison pause the NPCs are filled at the FROZEN onset time (all their
        // simTime-driven idle jitter / patrol / pace stop together — the synchronized held breath), and their
        // heads turn up as one (facing snapped screen-up). A Reever is never a patron, so it keeps its facing.
        bool headsUp = npcHoldTime.HasValue;
        plan.FillDroids(npcHoldTime ?? simTime, _droids);
        // #424 THE UNEXPLAINED SIGNAL: pre-compute each working crew member's glance — the facing toward the
        // NEAREST other crew member — so the barkeep and the dock-hand catch each other's eye as one. Only
        // built when a signal is glancing; a Reever or a drinking patron is never crew (StaffFacing skips them).
        double?[]? glance = crewGlance ? BuildCrewGlance(plan.DroidCount) : null;
        for (int di = 0; di < plan.DroidCount; di++)
        {
            DeckPlan.Droid droid = _droids[di];
            (float dx, float dy) = project(droid.X, droid.Y);
            // #295: the Reevers read hostile — a red mark, not the crew's grey.
            bool reever = droid.Name == "Reever";
            bool collector = droid.Name == "Collector";   // #583: a repo crew on foot, amber not red
            // #538: the sweep team, by callsign. They collide and are seen on the captain's own radius, so
            // they are drawn on it too — the #473 lesson about daylight showing between a body and its
            // picture.
            //
            // #633 · THREE KINDS OF FIGURE ON ONE DECK, and each branch only knew two. The pack is red, the
            // repo crew amber, a professional cold blue: what is walking toward you matters, and two hostiles
            // that read identically on the map are one hostile with two names.
            bool sweeper = IsSweeper(droid.Name);
            // #804 · …and a FIFTH: a guard walking a round on a restricted floor of the Hive. Institutional
            // green, because they are the one figure on this deck that is not a hostile at all — they are
            // an employee, and the mark has to say so before the card does.
            bool guard = SpaceSails.Core.PatrolBeat.IsGuardName(droid.Name);
            RgbaColor mark = reever ? ReeverColor
                : collector ? CollectorColor
                : sweeper ? SweeperColor
                : guard ? GuardColor
                : DroidColor;
            // #832 · THE FAR END OF THE EYE. Owner, watching one wink out mid-stride in an open corridor:
            // "Now the guard just vanishes into thin air .. that is like huge magic trick". A figure at the
            // limit of what a person can resolve is drawn thinner and softer, and (below) wears no name —
            // the same "when unsure, draw LESS" idiom the fan's blob and the on-grid smudge already use. The
            // tier itself is Core's (PatrolBeat.SightingFor); this only spends it.
            bool smeared = droid.Smeared;
            if (smeared)
            {
                mark = mark with { A = (byte)(mark.A * SmearInk) };
            }
            // #473 · AN OLD ONE'S PICTURE IS ITS BODY. The captain is drawn at exactly DeckPlan.AvatarRadius
            // (below), but the Old Ones — who collide, catch, block and get shoved apart on that SAME radius —
            // were drawn a tenth of a deck unit smaller. Every law that reads their body therefore fired with
            // daylight still showing between the dots: a catch at CatchRadius = 1.4 left a 0.2du gap on
            // screen, a pack held at PersonalSpace looked loose rather than shoulder to shoulder, and each one
            // parked against a wall floated just off it. Owner: "check all reever collisions… the radius must
            // be used in every single one" — the drawing is one of them. Crew stay at 0.5: nothing collides
            // with a barkeep, so their mark is free to be a mark.
            // #583: a collector has a body that catches on the same radius as everyone else's, so it is
            // drawn at that radius for the same reason an Old One is — the picture IS the law. Same for a
            // sweeper (#538), and for the same reason.
            float bodyRadius = reever || collector || sweeper || guard ? (float)DeckPlan.AvatarRadius : 0.5f;
            _renderer.DrawCircle(dx, dy, bodyRadius * scale, mark, mark);
            // Heads up as one (hull-shudder pause), or the crew catch each other's eye (unexplained signal),
            // else the droid's own facing. The shudder pause wins if both somehow overlap.
            double facing = headsUp && !reever && !collector && !sweeper && !guard ? Math.PI / 2
                : glance?[di] ?? droid.FacingRad;
            float fx = dx + (float)Math.Cos(facing) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(facing) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), mark, 1.5f);

            // #793 · …AND WHETHER THEY STOPPED WHEN YOU DID. Owner, on the whole point of a park bench:
            // "it is a good gumshoe move to see if anyone is following us by foot, as they would need to
            // stop moving also." A tail that has to hold is drawn holding — a bar struck across their back,
            // in #792's own warm SEATED ink, because that is the ink this deck already uses for a figure who
            // has settled. Handed down on the droid (DeckPlan.Droid.Held); this pen works nothing out.
            //
            // NOTHING SHIPPED SETS IT: no mover in the game today is a tail (a patrol walks a round that was
            // laid before the captain arrived). So this branch is the SEAM, and its drawing is guarded with
            // a test-only held figure rather than with an NPC nobody designed.
            if (droid.Held)
            {
                float bx = dx - ((float)Math.Cos(facing) * scale * HeldBarDu);
                float by = dy + ((float)Math.Sin(facing) * scale * HeldBarDu);
                float px = -(float)Math.Sin(facing) * scale * SeatChairDu;
                float py = -(float)Math.Cos(facing) * scale * SeatChairDu;
                DrawSeg((bx - px, by - py), (bx + px, by + py), SeatTaken, 2.4f);
            }

            // #538 · THE LAMP, DRAWN AT EXACTLY THE ANGLE THE RULE CHECKS. InspectionTeam.LampConeHalfAngleDegrees
            // and LampRange are read straight from Core here rather than eyeballed, because a cone drawn wider than
            // it is tested is a lie the player learns the expensive way — and this cone IS the counter-play, so it
            // has to be trustworthy enough to stand three metres to the side of.
            if (sweeper)
            {
                double half = SpaceSails.Core.InspectionTeam.LampConeHalfAngleDegrees * Math.PI / 180.0;
                double range = SpaceSails.Core.InspectionTeam.LampRange;
                RgbaColor lamp = SweeperColor with { A = 44 };
                for (int e = -1; e <= 1; e += 2)
                {
                    double edge = facing + (e * half);
                    // AND STOPPED AT THE FIRST BULKHEAD, because the RULE stops there. First pass drew both
                    // edges to full reach through steel — cone tested right, cone drawn wrong, which is the
                    // same lie as drawing it too wide and just as expensive to learn from: a captain would
                    // have read light spilling into a compartment nobody could actually see into.
                    double lit = plan.CastRay(droid.X, droid.Y, Math.Cos(edge), Math.Sin(edge),
                                              out double hit, out _, out _, out _)
                        ? Math.Min(range, hit)
                        : range;
                    float reach = (float)lit * scale;
                    DrawSeg((dx, dy),
                            (dx + (float)Math.Cos(edge) * reach, dy - (float)Math.Sin(edge) * reach),
                            lamp, 1f);
                }
            }

            // #832 · …and the DISTANT FIGURE wears no name. That is the whole of the tier: a silhouette
            // without a plate or a round number on it, because a captain who can read "PATROL 2" off a
            // figure has resolved it, and out here they have not. Writing the label anyway would be the
            // picture claiming a certainty the sim just said it did not have.
            if (!smeared)
            {
                _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name,
                    reever ? ReeverColor
                        : collector ? CollectorColor
                        : sweeper ? SweeperColor
                        : guard ? GuardColor
                        : TextDim,
                    "8px monospace", TextAlign.Center);
            }
        }
    }

    /// <summary>#870 lane 7b · #314's deployed sentries and their scoreboard magazines — drawn ON the grid
    /// and OVER the dark, because a sentry carries a lamp and you can see a light in an unlit hall even
    /// when you cannot see what it lights.</summary>
    private void DrawTheSentries(
        SurfaceHud? surface, double simTime, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #314: deployed sentries — a gun-green mark (dim once dry), a zap line to the Old One it's
        // dropping, and its crude two-digit magazine readout riding above (seven-segment red, dim at 00).
        // Drawn ON the grid, not a corner widget — the counter is meant to be read from across the map.
        if (surface is { Bots: { } sentries })
        {
            // Keep the per-bot change-tracking arrays as long as the deployed list (grows only).
            if (_botCounters.Length < sentries.Count)
            {
                System.Array.Resize(ref _botCounters, sentries.Count);
                System.Array.Resize(ref _botCounterChanged, sentries.Count);
            }
            for (int i = 0; i < sentries.Count; i++)
            {
                (double bxr, double byr, string counter, bool dry, bool firing, double aimX, double aimY) = sentries[i];
                (float sx, float sy) = project(bxr, byr);
                if (firing && !dry)
                {
                    (float zx, float zy) = project(aimX, aimY);
                    DrawSeg((sx, sy), (zx, zy), ZapColor, 1.6f);
                    _renderer.DrawCircle(zx, zy, 3f, ZapColor, ZapColor);
                }
                RgbaColor body = dry ? BotDim : BotColor;
                DrawBox(sx, sy, 0.55f * scale, body);
                _renderer.DrawCircle(sx, sy, 0.3f * scale, body, body);

                // The number changed this frame? Stamp the moment so the pop below can key off it. (First
                // sight of a bot counts as a change — a one-off blip as it deploys, which reads as intent.)
                if (_botCounters[i] != counter)
                {
                    _botCounters[i] = counter;
                    _botCounterChanged[i] = simTime;
                }
                double since = simTime - _botCounterChanged[i];
                float pop = since >= 0 && since < MagFlash ? (float)(1.0 - since / MagFlash) : 0f;

                // #314 low-ammo warning (owner, 2026-07-19): the magazine's house red is the identity down
                // the top of the belt; it warms to amber under 25 and snaps to a hot alarm red under 10 —
                // the small honest touch the counter never had. Non-numeric readouts keep the house red.
                RgbaColor digit = dry ? SegDim : SegLit;
                if (!dry && int.TryParse(counter, out int rounds))
                {
                    if (rounds < 10) digit = SegAlarm;
                    else if (rounds < 25) digit = SegWarn;
                }
                // On a decrement the digits flash brighter and swell for a frame or two — the owner loves
                // to watch them move, so the change gets a subtle brighten-toward-white + size pop.
                if (!dry && pop > 0f) digit = LerpToWhite(digit, 0.7f * pop);
                float fontPx = MagBasePx * (1f + 0.16f * pop);

                // The readout: a dark scoreboard panel with the two big digits, anchored above the bot so
                // it never covers the mark or its neighbours. Plate stays a steady size; only the number pops.
                float pw = 3.0f * scale, ph = 2.0f * scale;
                float plateBottom = sy - 0.8f * scale;      // clears the bot box (half 0.55·scale) with a gap
                float plateTop = plateBottom - ph;
                FillRect(sx - pw / 2, plateTop, pw, ph, new RgbaColor(16, 10, 10, 225));
                float baseY = (plateTop + plateBottom) / 2f + fontPx * 0.35f; // optical centre for the fixed-px glyphs
                _renderer.DrawText(sx, baseY, counter, digit,
                    $"bold {fontPx:0.#}px monospace", TextAlign.Center);
            }
        }
    }

    /// <summary>#870 lane 7b · #488's instrument half: the edgeless smudge over roughly where a return came
    /// from, and the colder broken ring where the fan last had something. Both are painted as AREAS on
    /// purpose — a dot would claim a precision a crude fan does not have.</summary>
    private void DrawWhatTheFanHeard(
        SurfaceHud? surface, double simTime, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #488 · WHAT THE FAN HEARS THROUGH STEEL. A soft, edgeless bloom over roughly where the return
        // came from — big enough that it names a REGION and not a spot. Drawn under everything else so a
        // contact you can actually see is always the sharper mark on the deck.
        if (surface is { Smudges: { } heard })
        {
            foreach ((double smx, double smy, double smr) in heard)
            {
                (float ssx, float ssy) = project(smx, smy);
                float rPx = (float)(smr * scale);
                // Three widening rings, each fainter: no hard edge anywhere, so the eye reads "somewhere
                // about here" rather than a position.
                // Owner: "let's show them much better on motion detector still." The first pass was so
                // faint it read as a rendering artefact; a return you have to hunt for is not a warning.
                // Loud enough to catch the eye, still edgeless enough that it can never be mistaken for a
                // position — and it BREATHES, so a live return is obviously live.
                float pulse = 0.82f + 0.18f * (float)Math.Sin(simTime * 0.004);
                for (int ring = 4; ring >= 1; ring--)
                {
                    float f = ring / 4f;
                    byte alpha = (byte)Math.Clamp(96 * (1.05f - f) * pulse, 0f, 255f);
                    _renderer.DrawCircle(ssx, ssy, rPx * f * pulse, new RgbaColor(226, 96, 84, alpha), default);
                }
            }
        }

        // #488 · GHOSTS: where the fan last had something. Dimmer and colder than a live return, and drawn
        // with a broken ring so it never reads as a contact — this is a memory, not a target.
        if (surface is { Ghosts: { } ghosts })
        {
            foreach ((double gx, double gy, double fade) in ghosts)
            {
                (float gsx, float gsy) = project(gx, gy);
                byte a = (byte)Math.Clamp(70 * fade, 0f, 255f);
                float gr = (float)(2.4 * scale);
                _renderer.DrawCircle(gsx, gsy, gr, new RgbaColor(150, 120, 160, (byte)(a / 3)), default);
                // Four short arcs of a ring, so the eye reads "was here" rather than "is here".
                for (int seg = 0; seg < 4; seg++)
                {
                    double a0 = (seg * Math.PI / 2) + 0.35;
                    DrawSeg(
                        (gsx + (float)(Math.Cos(a0) * gr), gsy + (float)(Math.Sin(a0) * gr)),
                        (gsx + (float)(Math.Cos(a0 + 0.75) * gr), gsy + (float)(Math.Sin(a0 + 0.75) * gr)),
                        new RgbaColor(170, 140, 180, a), 1.1f);
                }
            }
        }
    }

    /// <summary>#870 lane 7b · #488's overload countdown, anchored to the thing that is about to fail so it
    /// recedes behind the captain as they run — the one number that decides whether they live, kept out of
    /// the message channel where the PA calls are.</summary>
    private void CountDownTheOverload(
        SurfaceHud? surface, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #488 · THE OVERLOAD, ON THE GRID. Same scoreboard as a magazine, bigger and always alarm-red,
        // anchored to the thing that is about to fail — so it recedes behind the captain as they run, and
        // the one number that decides whether they live is never in the message channel with the PA calls.
        if (surface is { Countdown: { } burn })
        {
            (float bx, float by) = project(burn.X, burn.Y);
            float pw = 5.4f * scale, ph = 3.2f * scale;
            float top = by - 2.4f * scale;

            FillRect(bx - pw / 2, top, pw, ph, new RgbaColor(20, 6, 6, 235));
            // A hard border so it reads as a fitted instrument rather than a floating label.
            DrawSeg((bx - pw / 2, top), (bx + pw / 2, top), SegAlarm, 1.2f);
            DrawSeg((bx - pw / 2, top + ph), (bx + pw / 2, top + ph), SegAlarm, 1.2f);

            float px = MagBasePx * 1.5f;
            _renderer.DrawText(bx, top + ph / 2 + px * 0.35f, burn.Text, SegAlarm,
                $"bold {px:0.#}px monospace", TextAlign.Center);
        }
    }

    /// <summary>#870 lane 7b · The consoles, and the ONE prompt that is the true one — the offer is drawn
    /// only where <see cref="DeckPlan.NearestConsoleSpot"/> would actually answer, and #791's service run
    /// is marked down the whole length that answers.
    ///
    /// <para>#708 · This pass puts the lamp BACK on the pen and takes it off again. Consoles are drawn late
    /// — after the fan's smudges, so a contact heard through a wall is not painted over by a plate — which
    /// puts them on the far side of the blackout; they are WORLD all the same, so the world is drawn in two
    /// passes and both of them are behind the headlights.</para></summary>
    private void DrawTheConsoles(
        DeckPlan plan, in State state, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        // Consoles.
        //
        // ONE PROMPT, AND IT IS THE TRUE ONE. Owner, twice, on two different decks: "there two e's are too
        // close to each others now" and then "see the two crowded consoles at the back of our ship". Both
        // times I moved a console — and both times the real fault was here: this drew an [E] over EVERY
        // console inside the interact radius, while the key itself only ever answers the NEAREST one
        // (InteractAtConsole → NearestConsoleSpot). So a captain standing between two fittings saw two
        // offers, and one of them was a lie.
        //
        // Geometry could never fix that. A bridge is dense on purpose — helm, nav post, scope and three
        // desks inside a few du — so "keep every pair 6 du apart" is not a ship anyone would want to walk.
        // Asking the same function the key asks is the fix, it is one line, and it is right on every deck in
        // the game at once: her own, a derelict's, a station's, the regolith.
        //
        // #708 · AND THE LAMP GOES BACK ON THE PEN FOR THEM. Consoles are drawn late — after the fan's
        // smudges, so a contact heard through a wall is not painted over by a plate — which puts them on the
        // wrong side of the blackout. They are WORLD, though: a fitting bolted to a wall in an unlit hall is
        // not visible because it is important. So the world is drawn in two passes and both of them are
        // behind the headlights, rather than moving the blackout and quietly hiding the instrument.
        if (state.Dark)
        {
            _renderer = _mask;
        }

        DeckPlan.ConsoleSpot? answering = plan.NearestConsoleSpot(state.AvatarX, state.AvatarY);

        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // #371 Phase 3 fog: a console inside an unseen chamber is unknown (hidden); an explored one is
            // dimmed. A still-sealed door's console sits OUTSIDE any chamber rect, so it always shows.
            if (darkState(console.X, console.Y) == 0)
            {
                continue;
            }
            (float sx, float sy) = project(console.X, console.Y);

            // Lit only when [E] would actually reach THIS console. The radius check is still the gate —
            // NearestConsoleSpot applies it — so nothing lights up across the ship; what changed is that a
            // second console in range no longer claims a key it will not get.
            bool near = answering == console;
            RgbaColor c = near ? ConsoleNear : ConsoleGlow;

            // ── #791 · A FIXTURE THAT IS A RUN IS DRAWN AS ONE ────────────────────────────────────────
            //
            // Owner, at the B1 bar: "we should probably have service on the whole length indicated somehow."
            // The desk's front is now the press zone, so the desk's front is now MARKED — a service rail
            // down the whole of it with a serving tick struck across it every few du, in the same console
            // ink the dot has always used. No text is repeated along it (#782: a plate you cannot read is
            // worse than none, and one you read forty times is a wall of noise); the plate is said once, at
            // the fixture's own middle, exactly as it always was.
            //
            // IT IS THE VERY SEGMENT THE KEY MEASURES. Both come off ConsoleSpot's own span, which came off
            // Core's Hall.Service — so the length that is lit and the length that answers cannot disagree,
            // which is the split this deck has paid for more than any other.
            if (console.IsRun)
            {
                DrawServiceRun(console, c, near, project);
            }

            _renderer.DrawCircle(sx, sy, near ? 5f : 3.5f, c, c);
            _renderer.DrawText(sx, sy - 10, console.Label, near ? ConsoleNear : TextDim,
                near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                // …and the offer is drawn WHERE YOU ARE STANDING. On a point console that is the console;
                // on an eighty-du desk it is the stretch of counter under your elbow, because an [E] forty
                // du away at the plate would be the game answering a press it looks like it is refusing.
                (float ex, float ey) = console.NearestPointTo(state.AvatarX, state.AvatarY);
                (float px, float py) = project(ex, ey);
                _renderer.DrawText(px, py + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
            }
        }

        _renderer = _canvas;    // #708 · and off again — everything below is the captain, or an instrument
    }

    /// <summary>#870 lane 7b · The captain, and the things that happen to THEM — #453's blood on the ground
    /// and #467's wash round the edges of the screen, #784's seated figure or the standing one and its
    /// spoke, and #313's channel bar with #562's glyph saying which slow thing this is.</summary>
    private void DrawTheCaptain(
        in State state, SurfaceHud? surface, int widthPx, int heightPx, float scale,
        Func<double, double, (float X, float Y)> project)
    {
        // The captain.
        (float ax, float ay) = project(state.AvatarX, state.AvatarY);

        // #453 · BLOOD, when a blow gets past the block (owner: "Maybe a splash of blood when reever hit
        // goes through players attempt to block it. :-D"). Seeded spatter around the captain, thrown on the
        // regolith UNDER them so it reads as coming off the body. Brief — it is punctuation, not a decal.
        // #467 · THE SCREEN REACTS. Owner: "I had no sound to alert that I was taking damage… I should know
        // when I'm hurt." A small spatter under the boots was too easy to miss mid-fight, so a blow also
        // washes the EDGES of the screen red on the same fade. Peripheral, never over the grid — the deck
        // stays readable while you decide whether to run.
        if (surface is { BloodSplash: > 0 } hurt)
        {
            double f = Math.Clamp(hurt.BloodSplash, 0, 1);
            byte a = (byte)Math.Clamp(150 * f, 0, 255);
            var edge = new RgbaColor(150, 12, 12, a);
            float band = Math.Max(10f, heightPx * 0.055f);
            FillRect(0, 0, widthPx, band, edge);
            FillRect(0, heightPx - band, widthPx, band, edge);
            FillRect(0, 0, band, heightPx, edge);
            FillRect(widthPx - band, 0, band, heightPx, edge);
        }

        if (surface is { BloodSplash: > 0 })
        {
            double fade = Math.Clamp(surface.Value.BloodSplash, 0, 1);
            for (int i = 0; i < 9; i++)
            {
                // A fixed fan, so the spatter is stable for the moment it is up rather than crawling.
                double a = i * 2.399963229728653;             // the golden angle again
                double reach = scale * (0.5 + (0.16 * (i % 4)));
                float bx = ax + (float)(Math.Cos(a) * reach);
                float by = ay + (float)(Math.Sin(a) * reach);
                var blood = new RgbaColor(190, 30, 30, (byte)Math.Clamp(235 * fade, 0, 255));
                _renderer.DrawCircle(bx, by, Math.Max(1.5f, 0.16f * scale), blood, blood);
            }
        }

        if (state.Seated)
        {
            // ── #784 · SITTING DOWN, DRAWN ──
            //
            // Owner: "Let's make the graphics say I am sitting down at the avatar level." A standing captain
            // is a body and a long spoke pointing where they are going. A seated one is going nowhere, so
            // the spoke is gone entirely — in its place a CHAIR BACK behind the shoulders and a short bar of
            // ARMS on the table in front, and a body that takes a little less floor because it is folded
            // into a chair. Same ink, same anchor: it is the same captain, in a different posture, and the
            // three marks read as one figure rather than as furniture that has appeared beside them.
            DrawSeated(ax, ay, state.HeadingRad, scale);
        }
        else
        {
            // #473: the captain's mark already happened to equal AvatarRadius — say so, so the two can never
            // drift apart again the way the Old Ones' mark had.
            _renderer.DrawCircle(ax, ay, (float)DeckPlan.AvatarRadius * scale, AvatarColor, AvatarColor);
            float hx = ax + (float)Math.Cos(state.HeadingRad) * scale * 1.1f;
            float hy = ay - (float)Math.Sin(state.HeadingRad) * scale * 1.1f;
            DrawSeg((ax, ay), (hx, hy), AvatarColor, 2f);
        }

        // #313 the dig channel: a shovel glyph over the captain and a crude progress bar — the
        // vulnerability window, drawn ON the grid so the player watches the tracker while it fills.
        if (surface is { DigProgress: >= 0 } dig)
        {
            // #562: the glyph and the tint say WHICH slow thing this is. A shovel over a magazine being
            // racked would be the same class of lie this project keeps paying for.
            RgbaColor glyphInk = dig.ChannelIsAid
                ? new RgbaColor(150, 235, 200, 245)
                : new RgbaColor(255, 230, 140, 240);
            RgbaColor fillInk = dig.ChannelIsAid
                ? new RgbaColor(120, 215, 175, 240)
                : new RgbaColor(255, 200, 90, 240);
            _renderer.DrawText(ax, ay - 1.6f * scale, dig.ChannelGlyph, glyphInk, "bold 15px monospace", TextAlign.Center);
            float bw = 3.2f * scale, bh = 0.45f * scale;
            float bx0 = ax - bw / 2, by0 = ay + 1.1f * scale;
            FillRect(bx0, by0, bw, bh, new RgbaColor(20, 24, 30, 220));
            FillRect(bx0, by0, bw * (float)Math.Clamp(dig.DigProgress, 0, 1), bh, fillInk);
        }
    }

    /// <summary>#870 lane 7b · The corner and edge chrome, which is not the world and never was: #313's
    /// motion fan, #317/#330's nerve gauge and its ledger, #327's orbit line, #825's stall banner on its
    /// own line under it, the keybar, and #440's standing prompt above it.</summary>
    private void DrawTheInstruments(
        in State state, SurfaceHud? surface, double simTime, int widthPx, int heightPx, float ox)
    {
        // #313 the motion tracker: a crude corner fan of MOVING blips (bearing/range), including
        // contacts beyond the grid edge — the early warning. Cadence pulses the blips as they close.
        if (surface is { Instruments: true } tHud)
        {
            DrawMotionTracker(widthPx, heightPx, simTime, tHud);
        }

        // #317/#330 the nerve gauge: a crude deck-plan bar in the TOP-LEFT column. On the surface it is the
        // full-size head of the instrument column (the tracker seats beneath it); aboard the ship and in a
        // haven it whispers (compact, tucked below the deck chrome). Shown in every walk mode, never flight.
        if (state.ShowNerve)
        {
            DrawNerveLedger(state, heightPx);
            DrawNerveGauge(simTime, state.Nerve, state.NerveReadout, state.NerveCompact, state.HitsTaken, surface?.BloodSplash ?? 0);
        }

        // #327 the ship calls home: the mothership's orbit line, painted plainly across the TOP-CENTRE —
        // the one channel the owner's Miranda maroon never had. Never buried (the #324 visibility law):
        // calm teal while it holds, amber as it slips, a pulsing red for the last call and the maroon.
        if (surface is { OrbitComms: { Length: > 0 } orbitLine } oHud)
        {
            RgbaColor color = oHud.OrbitSeverity switch
            {
                >= 2 => new RgbaColor(255, 90, 70, (byte)(170 + 85 * (0.5 + 0.5 * Math.Sin(simTime * 4.0)))),
                1 => new RgbaColor(255, 190, 100, 235),
                _ => new RgbaColor(130, 225, 205, 220),
            };
            // COMMS-LOSS: when the downlink is degraded/blacked out the orbit line is a STALE readout — drop
            // it to a cold signal-grey and flicker its alpha like breaking static (faster + deeper on a full
            // blackout), so the frozen last-known value LOOKS lost, not just worded so. The honesty is in the
            // banner text (SurfaceComms); this is the matching visual.
            if (oHud.CommsState > 0)
            {
                double flickerHz = oHud.CommsState >= 2 ? 11.0 : 6.0;
                double floor = oHud.CommsState >= 2 ? 0.28 : 0.55; // blackout drops darker between flickers
                double f = floor + (1.0 - floor) * (0.5 + 0.5 * Math.Sin(simTime * flickerHz));
                color = new RgbaColor(170, 180, 190, (byte)(255 * Math.Clamp(f, 0.0, 1.0)));
            }
            _renderer.DrawText(widthPx / 2f, 20, orbitLine, color, "13px monospace", TextAlign.Center);
        }

        // #825 · THE MACHINE'S OWN BANNER, on its own line, under the ship's. The owner had "SIGNAL BREAKING
        // UP" across the top while the thing that was actually broken was the frame rate — and a sentence
        // about a downlink is a worse answer than no sentence at all when the question is "why will my legs
        // not move". So it is drawn SEPARATELY (never appended to the orbit line), in the amber of a
        // machine-level warning rather than the comms grey, and it steps down a line when the ship is
        // already talking so neither fact ever paints over the other.
        if (state.StallBanner is { Length: > 0 } stall)
        {
            float y = surface is { OrbitComms: { Length: > 0 } } ? 38 : 20;
            _renderer.DrawText(widthPx / 2f, y, stall,
                new RgbaColor(255, 190, 100, 235), "13px monospace", TextAlign.Center);
        }

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention. On the surface
        // the keybar turns contextual (#324): the deploy/drop keys spell themselves out while they matter.
        string bottomHint = surface is { KeyHints: { Length: > 0 } hints }
            ? hints
            : state.Docked
                ? "docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ Q — helm"
                : "WASD / arrows — move ∙ E — interact ∙ Q — back to the helm";
        _renderer.DrawText(ox, heightPx - 10, bottomHint, TextDim, "11px monospace", TextAlign.Center);

        // #440: the standing prompt rides just ABOVE the keybar, bright and a size up — the same eyeline the
        // player already checks for keys, but unmistakably not chrome. Gently breathing so it reads as a
        // thing still owed rather than furniture.
        if (surface is { StandingPrompt: { Length: > 0 } standing })
        {
            double breathe = 0.78 + (0.22 * Math.Sin(simTime * 0.001 * 2.2));
            var promptColor = new RgbaColor(255, 205, 90, (byte)Math.Clamp(255 * breathe, 60, 255));
            _renderer.DrawText(ox, heightPx - 30, standing, promptColor, "bold 14px monospace", TextAlign.Center);
        }
    }

    // #424 THE UNEXPLAINED SIGNAL · the crew glance. From the freshly-filled _droids, work out each WORKING
    // crew member's facing toward the nearest OTHER crew member — so the barkeep and the dock-hand (and, on
    // the bare ship, the ship's own droids) catch each other's eye as one. A drinking patron (a seated
    // regular, the Magpie) and a Reever are never crew, so their entry stays null (they keep their own
    // facing, oblivious to the buzzer). Returns a per-droid facing override, or null where there's no glance.
    private double?[] BuildCrewGlance(int count)
    {
        var facing = new double?[count];
        // The crew indices + their world positions this frame.
        Span<int> crew = stackalloc int[count];
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            if (IsCrew(_droids[i].Name))
            {
                crew[n++] = i;
            }
        }
        if (n < 2)
        {
            return facing; // a lone crew member has no one to catch eyes with — no glance
        }
        for (int a = 0; a < n; a++)
        {
            DeckPlan.Droid da = _droids[crew[a]];
            double bestSq = double.MaxValue;
            int nearest = -1;
            for (int b = 0; b < n; b++)
            {
                if (b == a)
                {
                    continue;
                }
                DeckPlan.Droid db = _droids[crew[b]];
                double d = (db.X - da.X) * (db.X - da.X) + (db.Y - da.Y) * (db.Y - da.Y);
                if (d < bestSq)
                {
                    (bestSq, nearest) = (d, crew[b]);
                }
            }
            DeckPlan.Droid dn = _droids[nearest];
            facing[crew[a]] = Math.Atan2(dn.Y - da.Y, dn.X - da.X); // world radians toward the caught eye
        }
        return facing;
    }

    // A WORKING crew member (the people who work the deck): the barkeep, the customs officer, the ship's own
    // droids — anyone who is neither a Reever nor a drinking PATRON (a seated bar regular, or the Magpie).
    private static bool IsCrew(string name) =>
        name is not ("Reever" or "Collector") && !IsSweeper(name)
        // #804 · Nor a guard on a round. Nobody on a security rota is going to catch a barkeep's eye during
        // a hull shudder, and the crew's grey would hide the one figure the Hive's floors have.
        && !SpaceSails.Core.PatrolBeat.IsGuardName(name) && !IsPatron(name);

    /// <summary>#538 · A sweeper, by callsign. Never crew: nobody on that team is going to catch a barkeep's eye
    /// during a hull shudder, and giving them the crew's grey would hide the second hostile thing on the deck.</summary>
    private static bool IsSweeper(string name) => name.StartsWith("SWEEP-", StringComparison.Ordinal);

    // The drinking patrons — the regulars' short names (HavenInterior.ShortNameFor) + the roaming Magpie +
    // the station Oracle (a ranting-drunk bar fixture, #425, not working staff) + the empty-chair fallback.
    // They never react to the off-deck buzzer; only the staff do.
    private static bool IsPatron(string name) => name switch
    {
        "Silas" or "Coil" or "Gilt-Eye" or "The Fixer" or "Regular" or "Magpie" or "Oracle" => true,
        _ => false,
    };

    // #314: brighten a colour toward white by t (0..1) — the one-frame decrement flash on the magazine
    // digits. Alpha is preserved; only the RGB warms up.
    private static RgbaColor LerpToWhite(RgbaColor c, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        static byte L(byte v, float t) => (byte)(v + (255 - v) * t);
        return new RgbaColor(L(c.R, t), L(c.G, t), L(c.B, t), c.A);
    }
}
