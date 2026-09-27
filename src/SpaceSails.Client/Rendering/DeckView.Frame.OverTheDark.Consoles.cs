using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE CONSOLES READ THEIR OWN PANELS — the consoles pass, drawn over the black.
///
/// <para>Split out of <c>DeckView.Frame.OverTheDark.cs</c> under #251 as a pure move: one contiguous run,
/// no member renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public sealed partial class DeckView
{
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
        // Which console the key would actually reach, asked as a name rather than as a comparison, because
        // two passes below ask it: the cull (which may never drop the one you are standing at) and the ink.
        bool IsTheOneAnswering(DeckPlan.ConsoleSpot spot) => answering == spot;

        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // #371 Phase 3 fog: a console inside an unseen chamber is unknown (hidden); an explored one is
            // dimmed. A still-sealed door's console sits OUTSIDE any chamber rect, so it always shows.
            if (darkState(console.X, console.Y) == 0)
            {
                continue;
            }
            (float sx, float sy) = project(console.X, console.Y);

            // #563 slice 2 · …and the same reject for a plate, at ITS OWN reach. A console is a dot with a
            // line of text centred over it, so the thing on screen is as wide as the label — tested as a
            // zero-length segment with a margin that covers half that width (the plates are drawn at ~6px a
            // character) plus the ring. Anything narrower would clip words off the edge of the screen, and a
            // cull that changes the picture is not a cull.
            //
            // The ANSWERING console is never skipped, whatever the arithmetic says: #212's law is that an
            // affordance the game will let you use may not be invisible. Neither is a service RUN, which is
            // eighty deck units of counter and not a point at all.
            //
            // What this drops is the several dozen plates a nine-tile chunk now carries, standing in ruins
            // several hundred deck units off the side of the screen.
            if (!IsTheOneAnswering(console) && !console.IsRun
                && OffTheGlass(sx, sy, sx, sy, 24f + (console.Label.Length * 3.5f)))
            {
                continue;
            }

            // Lit only when [E] would actually reach THIS console. The radius check is still the gate —
            // NearestConsoleSpot applies it — so nothing lights up across the ship; what changed is that a
            // second console in range no longer claims a key it will not get.
            bool near = IsTheOneAnswering(console);
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

            // #1218 · THE PLATE TAKES THE ROW ABOVE ITS OWN DOT, and the dot's own radius is what the lift is
            // measured from — `sy - 10` was one of the five literals, and it is the one that printed through
            // the forensic line at the owner's cache, because MoonSurface.Layout seeds a DigSite console at
            // that ✗'s own (x, y) BY CONSTRUCTION. A room is named several passes earlier, so a plate that
            // would land on a room's name takes the row over it rather than burying it.
            float dot = near ? 5f : 3.5f;
            double platePx = near ? 10.0 : 9.0;
            _renderer.DrawCircle(sx, sy, dot, c, c);
            _renderer.DrawText(
                sx, SeatAboveAMark(sx, sy, dot, console.Label, platePx, TextAlign.Center),
                console.Label, near ? ConsoleNear : TextDim,
                near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                // …and the offer is drawn WHERE YOU ARE STANDING. On a point console that is the console;
                // on an eighty-du desk it is the stretch of counter under your elbow, because an [E] forty
                // du away at the plate would be the game answering a press it looks like it is refusing.
                (float ex, float ey) = console.NearestPointTo(state.AvatarX, state.AvatarY);
                (float px, float py) = project(ex, ey);
                // #1218 · …AND THE ROW BELOW STAYS [E]'S. The ruling reserves it: an offer that went hunting
                // up the screen for clear space would be an offer pointing at the wrong fitting. It is
                // written into the band book all the same, so nothing drawn after it prints through it.
                _renderer.DrawText(px, py + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
                ReserveTheRow(px, py + 20, "[E]", 11.0, TextAlign.Center);
            }
        }

        _renderer = _canvas;    // #708 · and off again — everything below is the captain, or an instrument
    }
}
