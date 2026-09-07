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
        (_viewW, _viewH) = (widthPx, heightPx);

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

        DrawTheSentries(surface, simTime, widthPx, heightPx, scale, project);
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
}
