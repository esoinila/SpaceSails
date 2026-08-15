using System;
using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #585 · ONE FLOOR OF THE HIVE, as a walkable deck.
///
/// <para>The client half of <see cref="UndergroundComplex"/>: it turns a floor's pure geometry into a
/// <see cref="DeckPlan"/> exactly the way <c>MoonSurface</c> turns a ground into one and <c>WreckInterior</c>
/// turns a dead hull into one. Nothing here decides anything — the layout, the signs, the hauls and the
/// pressure are all Core's, and this only draws them.</para>
///
/// <para>The whole architecture rests on one observation the owner made: <i>"we could go underground so that
/// we don't need to go out of the border on normal level."</i> A floor is laid inside the SURFACE'S OWN
/// envelope, so a facility the size of the entire field costs no new coordinate space. The renderer shows one
/// level at a time, which is the same deck swap the ship ↔ haven ↔ surface switch has always done.</para>
/// </summary>
public static class HiveInterior
{
    /// <summary>Where the captain stands when the lift doors open — just off the car, on the spine.
    ///
    /// <para>#801 · The CAGE's doorstep. Every caller that means "the way in" still means this one; the ones
    /// that mean "the car I just rode" say which (<see cref="SpawnOn(in SurfaceLayout.Field,
    /// UndergroundComplex.ShaftKind)"/>).</para></summary>
    public static (double X, double Y) SpawnOn(in SurfaceLayout.Field field) =>
        SpawnOn(field, UndergroundComplex.ShaftKind.Cage);

    /// <summary>#801 · Where the doors of THIS car open onto the floor.
    ///
    /// <para>The pace out of the car is the shaft's own (<see cref="UndergroundComplex.Shaft.Landing"/>) and
    /// not a sign written here: the two alcoves hang off opposite faces of the spine, so "a pace out" is
    /// +1 du for one of them and −1 for the other, and a renderer that kept its own copy of that would put
    /// a captain inside a wall the first time a car moved. Falls back to the cage where the ground would not
    /// take a second car — #602's law, said about a shaft that may not exist.</para></summary>
    public static (double X, double Y) SpawnOn(
        in SurfaceLayout.Field field, UndergroundComplex.ShaftKind car)
    {
        foreach (UndergroundComplex.Shaft shaft in UndergroundComplex.ShaftsOn(field))
        {
            if (shaft.Kind == car)
            {
                return shaft.Landing;
            }
        }
        (double x, double y) = UndergroundComplex.ShaftAt(field);
        return (x, y + 1.0);
    }

    /// <summary>Build one floor's deck.</summary>
    /// <param name="canteenWatch">#709 · Which shift the canteen's people are on
    /// (<see cref="PatronRota.WatchIndex"/>). Passed in already frozen rather than read from a clock here, so
    /// the room that is DRAWN and the room the [E] key later asks about can never be two different rooms.
    /// Defaults to the first watch, which is what every audit and lab wants: a fixed roster to walk.</param>
    /// <param name="locksShotOpen">#803 · The locks a designated shot has taken the hasp off, by
    /// <see cref="LockKey"/>. Replayed onto every rebuild exactly the way an emptied room is, so a door that
    /// was opened stays open through a floor change, a satchel press and a save — a world that grows its
    /// walls back while the captain is looking somewhere else is the oldest bug on this ground.</param>
    /// <param name="cubiclesShut">#821 · The WC cubicles whose catch is over, by <see cref="CubicleKey"/>.
    /// Replayed onto every rebuild for the reason a shot hasp is: a room searched, a satchel opened or a bin
    /// used rebuilds this deck, and a door that came open again while the captain sat still behind it would
    /// be the world growing its walls back with somebody looking straight at them.</param>
    public static DeckPlan FloorDeck(
        string bodyId, int level, in SurfaceLayout.Field field,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids,
        IReadOnlyCollection<int> emptiedRooms, long canteenWatch = 0,
        IReadOnlyCollection<string>? locksShotOpen = null,
        IReadOnlyCollection<string>? cubiclesShut = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);
        // #592 · The FLOOR's kind, not the site's: on the band nobody listed they differ, and the
        // title over the plan is where that lands first.
        UndergroundComplex.Kind kind = UndergroundComplex.KindOn(bodyId, level);

        var walls = new List<DeckPlan.Wall>();
        var doors = new List<DeckPlan.Door>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        var labels = new List<(float X, float Y, string Text)>();

        // ── #677 · WHICH SIDE OF THE SEAM THIS FLOOR IS ON ──────────────────────────────────────────────
        //
        // Asked ONCE, of Core, and then handed to everything below it. Owner's ruling on the halls' senses:
        // "the pre-existing tunnels would be scary as dark ones and totally different style … it is just
        // built into the smooth monolith style walls." The renderer's whole contribution to that is a
        // material, and a material is one bool applied uniformly — a floor half-poured and half-not would be
        // the seam drawn in the wrong place, which is the one geometric fact this feature has.
        bool pastTheSeam = UndergroundComplex.IsFound(bodyId, level);

        // The structure. Everything down here is MADE — poured, welded, bolted — so it draws in the ship's
        // own pressure-hull ink rather than in the body's stone (#589). That contrast is the point: you have
        // just left a world built out of what was under your boots and walked into something imported whole.
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            // #585 · EVERY wall down here is hull-bright, not just the spine. Owner, standing in a corridor:
            // "this looks weird now... I am like outside the structure?" / "like in the regolith."
            //
            // He was reading the INK, and the ink was lying. Only the spine carried IsHull, so the ribs and
            // every room wall drew in the dim inner-line stroke — which is the same faint grey the surface
            // uses for rubble and scree. A captain who has just ridden a lift into a poured, powered,
            // still-lit facility was being shown regolith line-work and correctly concluded they were
            // outside.
            //
            // Nothing down here is rubble. It was cut, poured and bolted by people with a budget, so it draws
            // like the made thing it is — the brightest structure the game has shown since the ship.
            //
            // #677 · …and past the seam it is none of that. A gallery is not poured and it is not the moon's
            // rock either, so it takes the third idiom and, with it, no ink from the department livery this
            // plan may be carrying. The concrete stops at a line and the material changes, which is the whole
            // sentence the drawing is allowed to say about it.
            walls.Add(new(
                (float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2,
                false, IsHull: !pastTheSeam, IsSeamless: pastTheSeam));
        }

        // ── #759 · THE GLAZING, PUT BACK INTO THE DECK AS WHAT IT IS ────────────────────────────────────
        //
        // Owner's pinned requirement for this room's own pictures: a) A VIEW TO THE PARK and b) A WINDOW
        // WALL BETWEEN. Core keeps these segments OUT of floor.Walls so nothing can draw them as poured
        // concrete; they come back in here as walls carrying IsWindow — the flag the ship's bridge glass and
        // her cantina's panoramic window have used since the deck was built, so the ink is the game's own
        // window ink and this file invents nothing.
        //
        // BOTH HALVES OFF ONE SEGMENT. It is a wall, so the collision field takes it and no body crosses it;
        // it is a window, so the eye does. Two segments on one line — one to draw, one to collide — is the
        // drawn-versus-simulated split this house has a name for.
        foreach (SurfaceLayout.Wall w in floor.Windows ?? [])
        {
            walls.Add(new(
                (float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2,
                IsWindow: true, IsHull: !pastTheSeam));
        }

        // ── #821 · WHICH CUBICLE LEAVES HAVE THE CATCH OVER ────────────────────────────────────────────
        //
        // Worked out BEFORE the doorway loop, because a shut cubicle is a door that is drawn shut AND a wall
        // that a body cannot cross — the very idiom #585's never-opening doors already use, one scale down.
        // Keyed on the leaf's own geometry so the doorway list and the cubicle list cannot come to two
        // different opinions about which hole in which partition this is.
        var shut = new HashSet<string>(StringComparer.Ordinal);
        if (cubiclesShut is { Count: > 0 } && floor.Park is { } shutPark)
        {
            foreach (UndergroundComplex.RingRoom suite in shutPark.Frontage)
            {
                foreach (RingOffice.Stall cell in suite.Cubicles)
                {
                    if (cubiclesShut.Contains(CubicleKey(level, in cell)))
                    {
                        shut.Add(LeafKey(cell.Door));
                    }
                }
            }
        }

        foreach (SurfaceLayout.Doorway d in floor.Doorways)
        {
            if (shut.Contains(LeafKey(d)))
            {
                // Drawn as the shut door it is, with the partition behind it. #821's law is that this buys
                // TIME and not safety: what it buys is exactly this one segment, and the man outside is
                // still on the floor.
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Locked: true));
                walls.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, false, false));
                continue;
            }
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: true));
        }

        // #585 · THE DOORS THAT NEVER OPEN. The owner asked for these by name as the illusion of scale, and
        // they are drawn Locked — cold, always shut, with a real wall behind them — so nothing about them
        // ever hints that a way through exists. [E] reads the sign; that is all it will ever do.
        //
        // #803 · …unless somebody took the hasp off it with a sentry. That is a LOUD, deliberate, six-round
        // act performed with the captain's own handset, and it is the one thing in the game allowed to move
        // one of these — so the illusion is untouched by walking past forty of them and spent, one door at a
        // time, by a captain who decided a particular door was worth telling the whole floor about.
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            if (locksShotOpen is not null && locksShotOpen.Contains(LockKey(level, l)))
            {
                // No wall behind it now, and the leaf is drawn as the ordinary way-through it has become.
                // The plate stays readable — an affordance that vanishes when it is used is #212's shape —
                // and it wears the hole rather than the padlock so the deck says which of the two it is.
                doors.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, Imported: true));
                consoles.Add(new(DeckPlan.ConsoleKind.HiveSign,
                    (float)((l.X1 + l.X2) / 2), (float)((l.Y1 + l.Y2) / 2), $"{ShotOpenGlyph} {l.Sign}"));
                continue;
            }

            doors.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, Locked: true));
            walls.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, false, false));
            consoles.Add(new(DeckPlan.ConsoleKind.HiveSign,
                (float)((l.X1 + l.X2) / 2), (float)((l.Y1 + l.Y2) / 2), $"🔒 {l.Sign}"));
        }

        // What is in the rooms that DO open. An emptied room keeps its walls and its door — it stays a place
        // you have been, the #573 law — and simply stops offering anything.
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (emptiedRooms.Contains(RoomKey(level, i)))
            {
                continue;
            }
            (double rx, double ry) = floor.RoomCentres[i];
            consoles.Add(new(DeckPlan.ConsoleKind.HiveHaul, (float)rx, (float)ry, "🔦 SEARCH THE ROOM"));
        }

        // ── #608 · THE REFUGES, DRAWN TO BE FOUND ───────────────────────────────────────────────────────
        //
        // Owner, after suffocating on B2: "the rooms should have airlocks etc ... some havens :-D" / "on
        // surface there are emergency shelters :-D" / "there should be like at least one air replenish
        // station in each of the airless labs underground... for pure safety".
        //
        // The hardest thing about a dead floor is that every door on it looks like every other door, and one
        // of them is the only one that matters. On the surface a shelter is a DIFFERENT SHAPE of building
        // seen across open ground; down here every room is the same poured box off the same rib, so the
        // refuge has to be told rather than shown — a plate over its door, at the signage size the depth
        // plate uses, because this is the second question a captain asks after "which floor is this".
        //
        // The rack is a console like any other so the [E] verb is the surface's verb, and the room's own
        // doorway is already drawn Imported violet with every other door down here (#592's language: what
        // was flown in, versus what was cut out of the moon).
        foreach (UndergroundComplex.Refuge refuge in floor.Refuges)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.HiveRefuge,
                (float)refuge.X, (float)refuge.Y, UndergroundComplex.RefugeTankLabel));
            labels.Add(((float)refuge.X, (float)(refuge.Y - UndergroundComplex.RefugeHalfHeight - 2.0),
                refuge.Sign));
        }

        // ── #707 · THE AMENITIES, DRAWN THE WAY THE REFUGE IS ───────────────────────────────────────────
        //
        // Owner: "all the secret labs dont have any cantina / bar nor any toilets."
        //
        // Same shape as the refuge below, because it is the same kind of object: a room Core carved out of
        // the floor's own rooms, with a console for the [E] verb and a plate for the eye. The FIXTURES are
        // already in floor.Walls — the counter, the cubicle dividers, the machines — so they were drawn and
        // collided with by the loop at the top of this method, and nothing here has to know their shape.
        // A renderer that laid out its own bar counter would be one more caller doing geometry about a
        // building it does not own (§13.15).
        var tables = new List<DeckPlan.TableTop>();

        // #792 · …and the tall seats, which Core has known the occupancy of since #756 and which have never
        // once been on the floor. Same rule as the tops below it: this file ASKS, and decides nothing.
        var stools = new List<DeckPlan.StoolSpot>();

        // #793 · …and the PARK'S BENCH ENDS, which have been drawn as labelled fixtures since #790 and have
        // never once said which half of one is free. Same rule a third time: Core owns who is on a bench
        // (ParkBenches.On, off #790's own lone figure and nothing new), and this list only carries the
        // answer to the pen.
        var benchSeats = new List<DeckPlan.BenchSpot>();

        // #868 · …and the FURNITURE, as the filled rectangles Core published. Same rule a fourth time: this
        // file asks and decides nothing — the box is RingOffice.Fixture's own, and the pen is told what a
        // piece IS (DeckPlan.FurnitureSpot.ToneOf) rather than what colour to make it.
        var furniture = new List<DeckPlan.FurnitureSpot>();

        // ── #756 · THE FLOOR WEARS ITS ART ─────────────────────────────────────────────────────────────
        //
        // Owner, walking the biggest social room in the game and finding bare grid: "let's put todo to have
        // gen-AI Bar image on the background like we have in space ports."
        //
        // THE SAME SEAM THE SHIP HAS USED SINCE THE 3D RENOVATION — DeckPlan.Backdrop, drawn by DeckView
        // under every vector overlay, exactly the way the ship's CANTINA wears art/the-space-bar.jpg. This
        // list was the bare `[]` in the constructor call at the bottom of this method; nothing about the
        // renderer had to learn a new idea, because a hall is a floor zone and a floor zone is what a
        // backdrop already was.
        //
        // WHICH PICTURE, AND OVER WHAT BOX, BOTH COME FROM CORE. The url is Hall.ArtUrl and the rectangle is
        // the hall's own published box — so the day a hall is carved a du wider its art follows without
        // anybody remembering to come here, and the park (#759) wears one by adding a row to HallArtFor and
        // nothing else at all.
        var backdrops = new List<DeckPlan.Backdrop>();

        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            // ── #791 · ONE FIXTURE, ONE CARD, AND THE WHOLE DESK-FRONT TO PRESS IT FROM ────────────────
            //
            // Owner, live at the B1 bar: "The Bar desk is really long now, but there is only one spot to get
            // service on it… we would need an E-bus of the bar desk length instead of one bar keep cashier
            // at a single spot."
            //
            // It is still ONE console — one dot, one plate, one card — and that is the point of doing it
            // this way rather than bolting a row of consoles along the bar. A row would be a dozen [E]
            // targets in a room already dotted with table consoles, which is the very crowding #212 and
            // both "there two e's are too close to each others" reports were about; and every one of them
            // would open the same card, so eleven of the twelve would be furniture pretending to be a
            // choice. A fixture that IS eighty du long says the true thing once.
            //
            // THE LENGTH IS CORE'S. Hall.Service is the run the carve laid, off the same (u, v) as the
            // counter's own wall segments, its photograph and its stools. Nothing here measures a bar.
            //
            // #827 · …and the run is the DESK'S FRONT FACE, which is not concentric with the plate: the
            // console dot stands on the square a body stands on, and the rail is drawn on the counter a step
            // behind it. Both coordinates are Core's; the only thing that happens here is a cast.
            UndergroundComplex.ServiceRun? run = a.Hall?.Service;
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveAmenity, (float)a.X, (float)a.Y, a.Fixture,
                Run: run is { } bus
                    ? ((float)bus.X0, (float)bus.Y0, (float)bus.X1, (float)bus.Y1)
                    : null));
            if (a.Hall is { ArtUrl: { } floorArt } painted)
            {
                // Top-left, W, H — the ship's own convention, and Y is the box's TOP edge because deck +y
                // is up. Hall.X0/Y0/X1/Y1 are already min/max normalised where they are carved, so there is
                // no orientation to work out here: a rib that runs down the field and one that runs up it
                // hand this the same rectangle.
                backdrops.Add(new(
                    floorArt,
                    (float)painted.X0, (float)painted.Y1,
                    (float)(painted.X1 - painted.X0), (float)(painted.Y1 - painted.Y0),
                    UndergroundComplex.HallArtAlpha));
            }

            // ── #780 · AND THE FURNITURE, ON TOP OF THE FLOOR IT STANDS ON ────────────────────────────
            //
            // Owner, live: "see how in the space bars we have the image of bar desk at the spot where the
            // bar desk is." AFTER the hall's own art in this list and never before it, because DeckView
            // walks Backdrops in order — a counter painted first would have the room's wallpaper laid back
            // over it, which is one picture drawn twice and neither of them seen. Still under every vector
            // mark, which is the one law this whole seam exists to keep.
            //
            // The rectangle is Core's and the alpha is Core's; this loop measures nothing. It converts a box
            // (X0,Y0,X1,Y1) into the ship's own top-left+W+H convention, and that is the entire contribution
            // a renderer is allowed to make to a room somebody else carved.
            foreach (UndergroundComplex.SpotArt spot in a.Hall is { } furnished ? furnished.Painted : [])
            {
                backdrops.Add(new(
                    spot.Url,
                    (float)spot.X0, (float)spot.Y1,
                    (float)(spot.X1 - spot.X0), (float)(spot.Y1 - spot.Y0),
                    UndergroundComplex.SpotArtAlpha));
            }

            // ── #792 · THE TALL SEATS, FREE AND TAKEN ──────────────────────────────────────────────────
            //
            // Owner: "there should be high chairs so sitting at the bar desk is also possible" (#756, built)
            // and then, on the 8th: "Now I have trouble finding a free table… lol story of my travelling
            // life right here."
            //
            // WHERE is the hall's (Core carved the row out of the counter's own segments); WHO IS ON THEM is
            // TheStools' (the same call the [E] press asks, off the same frozen watch, so the seat drawn
            // free is the seat pick-or-default will hand over). Both halves come from somewhere else on
            // purpose — a renderer that laid out eight stools would be doing geometry about a bar it did not
            // carve, and one that decided which were busy would be the drawn room and the pressed room
            // disagreeing, which is the bug class this project has paid for most often.
            if (a.Hall is { } counter)
            {
                // Asked over the whole row before any of it is drawn, because "is there anybody at this
                // counter" is a fact about the ROW and not about a seat: the free stool beside somebody and
                // the free stool in an empty bar are two different offers, and they are the same two offers
                // the tops make. One language, one question.
                bool anybody = false;
                for (int s = 0; s < counter.StoolRow.Count; s++)
                {
                    anybody |= TheStools.Taken(bodyId, level, s, canteenWatch);
                }
                for (int s = 0; s < counter.StoolRow.Count; s++)
                {
                    (double sx, double sy) = counter.StoolRow[s];
                    stools.Add(new(
                        (float)sx, (float)sy, TheStools.Taken(bodyId, level, s, canteenWatch), anybody));
                }

                // ── #827 · AND THE HOLES IN THE ROW, WHICH ARE FIXTURES TOO ──────────────────────────
                //
                // Owner, completing the counter model: "there are gaps for people to walk to the cashier
                // etc." A gap with nothing painted on it is a seat somebody unbolted; a gap with its own
                // stencil is a place to stand. Both the position and the words are Core's — this loop reads
                // the same one published row the stools above came out of, so a gap cannot end up somewhere
                // the seats do not agree with.
                foreach (UndergroundComplex.CounterPlace place in counter.CounterRow)
                {
                    if (!place.Seated)
                    {
                        labels.Add(((float)place.X, (float)place.Y, place.Plate));
                    }
                }
            }

            // #751 · A hall's plate is stencilled beside its DOOR, which is on whichever face the rib is
            // on — so Core says where, exactly as it says where the board hangs. The 7.6 du offset below is
            // measured off a 15 x 12 room and would hang the sign in mid-floor in a room fifty du deep.
            labels.Add(a.Hall is { } signed
                ? ((float)signed.PlateX, (float)signed.PlateY, a.Plate)
                : ((float)a.X, (float)(a.Y - 7.6), a.Plate));
            // ── #709/#746 · THE TOPS, AND — ON B1 ONLY — SOMEBODY SITTING AT THEM ─────────────────────
            //
            // Owner: "we should have people in the bar... we have cover story" and, in the same breath,
            // "for now let's keep the people in B1." Then, #746: "tables should seat 2/4/more, not all
            // pairs" — which is only worth having if somebody can ask whether a seat is FREE.
            //
            // ONE CALL FOR BOTH. This used to walk a.Tables for the round tops and then separately ask
            // CanteenRegulars.Sitting who was at them, which is two sources for one fact — the exact shape
            // #709's own docs warn about (the drawn room and the pressed room disagreeing). Core now
            // answers with the tops, their seat counts and their occupancy in one list, off the same
            // frozen watch, and the [E] press asks that same function. The renderer decides nothing.
            //
            // They stand ON the table's own spot rather than beside it, because the table IS the seat as
            // far as the deck is concerned: Core placed those round tops (#707) and a console offset by a
            // hand-typed du would be one more caller doing geometry about furniture it does not own.
            //
            // #751 · …and it is the same one call now that the room holds eighty. THREE TIERS COME OUT OF
            // IT — the named regulars, the background patrons that fill the hall by the watch, and the
            // cabinets' empty tops — and this loop cannot tell them apart, which is the point: a patron is
            // a plate at a coordinate, drawn like any console dot, with nothing to run per frame.
            //
            // #792 · THE SAME ONE CALL ANSWERS THE THREE GLANCES, TOO. A top now goes onto the plan with
            // its seat count, whether anybody is at it and whether they are TALKING — all three off this
            // list, off this frozen watch, so the chairs drawn free are the chairs [E] will offer. The
            // renderer downstream is handed the answers and never works one out: DeckView has no idea what
            // a canteen is, which is #788's own discipline for the seated captain applied to everybody else
            // in the room.
            foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(bodyId, level, a, canteenWatch))
            {
                // #823 · …and HOW MANY of them are at it, which the pen used to have to guess at and
                // therefore drew as one. Handed down like every other field on this record, off the same
                // frozen watch, so the bodies on the plan are the bodies Core seated.
                // #820 · …and WHERE ITS CHAIRS ARE, off Core's own ring, with Core's own answer about which
                // of them the party is in. Handed down for the reason every other field on this record is:
                // the captain is seated in one of these chairs now, and a pen that placed them itself would
                // be the drawn chair and the sat chair coming out of two authors.
                var chairs = new List<DeckPlan.TableChair>(Math.Max(0, top.Seats));
                for (int c = 0; c < top.Seats; c++)
                {
                    (double chx, double chy) = top.Chair(c);
                    chairs.Add(new((float)chx, (float)chy, top.PartyIn(c)));
                }

                tables.Add(new(
                    (float)top.X, (float)top.Y, top.Seats, top.Taken, top.Talking, top.Heads, chairs));
                // #757 · EVERY TOP IS NOW A CONSOLE, and which kind it is is the one fact the room already
                // knows: somebody at it, or nobody. Owner, live in the hall: "I have empty table but I
                // cannot sit down." An empty top used to be drawn as a ring on the floor and nothing else,
                // so [E] there had literally nothing to answer — the refusal the issue is titled after was
                // an ABSENCE, which is the one kind of refusal a player cannot read.
                //
                // Still one call, still the same frozen watch. The renderer does not decide who may be sat
                // with; it labels what Core says is there.
                //
                // …and TAKING one is offered in the room outsiders are admitted to and NOWHERE ELSE, which is
                // Core's own B1 ruling (CanteenRegulars.PeopleSitHere) rather than a clause typed here.
                // B17's staff mess is hall-class as well and just as full of tops, and its whole identity is
                // that the shift has not come — twenty FREE TABLE plates in it would be this renderer handing
                // that room a verb its own design refuses.
                if (top.Plate is { } plate)
                {
                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveRegular, (float)top.X, (float)top.Y, plate));
                }
                else if (CanteenRegulars.PeopleSitHere(bodyId, level, a))
                {
                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveTable, (float)top.X, (float)top.Y,
                        SittingAlone.FreeTablePlate));
                }
            }

            // ── #751 · THE CABINETS, PLATED ────────────────────────────────────────────────────────────
            //
            // Owner: "have cabinet-spaces for sensitive negotiations." A row of doors down the back wall of
            // a hall, each with what it is stencilled beside it — and the plate is the whole of how you get
            // one: BY ARRANGEMENT · ASK AT THE COUNTER. No console: there is nothing in a cabinet to work,
            // and the card fires by standing in it (the refuge idiom, same as the staff mess).
            if (a.Hall is { } theHall)
            {
                // Read from the hall side, in front of the door — never from inside the cabinet, and never
                // from the far side of the outer wall. Which side that is comes off the hall's own box
                // rather than a sign the renderer guessed at: the cabinets stand against whichever wall the
                // rib put them on, and this file does not know which one that was.
                double hallMidX = (theHall.X0 + theHall.X1) / 2.0;
                foreach (UndergroundComplex.Cabinet cabinet in theHall.Cabinets)
                {
                    double inward = cabinet.X > hallMidX ? -1.0 : 1.0;
                    labels.Add((
                        (float)(cabinet.X + (inward * (cabinet.HalfW + 2.0))),
                        (float)cabinet.Y, cabinet.Plate));
                }
            }

            // ── #709 · AND THE CORK BOARD ON THE WALL ─────────────────────────────────────────────────
            //
            // Owner: "let's add a bulletin board to the bar." It is where the cast's own working day is
            // written down — every notice on it belongs to somebody sitting in this room, and nothing
            // anywhere says so.
            //
            // CORE OWNS WHERE, exactly as it owns who sits down. A renderer choosing a spot on a wall would
            // be doing geometry about a room it does not own (§13.15), and the offsets are picked against the
            // counter's line and the tables' so the board owns its own patch of floor to be pressed from.
            if (CanteenBoard.At(bodyId, level, a) is { } board)
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveBoard, (float)board.X, (float)board.Y, CanteenBoard.Plate));
            }
        }

        // ── #759 · THE PARK, DRAWN ─────────────────────────────────────────────────────────────────────
        //
        // Owner: "on map the park needs to Exist there next to the bar. It is an indoor park where the fresh
        // stuff is grown that is served here on the plate."
        //
        // Every solid thing in it — the walls, the raised beds, the benches, the floodlight masts — is
        // already in floor.Walls and was therefore drawn and collided with by the loop at the top of this
        // method. Nothing here lays out a bed or decides where a bench goes; this is the SIGNAGE and the
        // floor art, which is the whole of a renderer's business in a room Core carved.
        if (floor.Park is { } green)
        {
            // The floor it wears, in panels. One 16:9 frame stretched over a room six times wider than it is
            // deep would be a smear; Core cuts the box (ParkArtPanels) because that is geometry about a room
            // this file does not own.
            if (green.ArtUrl is { } parkArt)
            {
                foreach ((double px0, double _, double px1, double py1) in
                    UndergroundComplex.ParkArtPanels(green))
                {
                    backdrops.Add(new(
                        parkArt, (float)px0, (float)py1,
                        (float)(px1 - px0), (float)(green.Y1 - green.Y0),
                        UndergroundComplex.HallArtAlpha));
                }
            }

            // The plate at the gate, in the inspectorate voice the room is stencilled in.
            labels.Add(((float)green.X, (float)green.Y, UndergroundComplex.ParkPlate));

            // What is in each bed, and where it goes when it is picked. THE FOOD CONNECTION IS THIS LINE OF
            // STENCIL and nothing else: the bed says CANTEEN 1 and so does the counter, and no card ever
            // points that out.
            foreach (UndergroundComplex.GrowingBed bed in green.Beds)
            {
                labels.Add(((float)bed.X, (float)bed.Y, bed.Plate));
            }

            // ── #793 · THE BENCHES TAKE THE SIT VERB ───────────────────────────────────────────────────
            //
            // #790 shipped them as plates over solid furniture and said so in this very comment: "sitting
            // down is #778's verb and arrives with it." It has arrived. A bench is a CONSOLE now, of its own
            // kind, and the plate says the verb the way a free table's does (#783: "why not use words like
            // SIT DOWN here if it means sitting down?").
            //
            // WHO IS ON WHICH BENCH IS CORE'S — ParkBenches.On reads #790's own lone figure and nothing was
            // populated to make the answer interesting. The two ends go onto the plan as SEATS, in the
            // counter's own idiom (#792/#795), so the deck can answer "is the whole bench free" from across
            // the room: that is the question the privacy predicate is written on.
            foreach (ParkBenches.Bench bench in ParkBenches.On(in green))
            {
                labels.Add(((float)bench.X, (float)(bench.Y - 2.2), UndergroundComplex.ParkBenchPlate));
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveBench, (float)bench.X, (float)bench.Y, bench.DeckPlate));

                for (int end = 0; end < ParkBenches.Ends; end++)
                {
                    (double ex, double ey) = bench.End(end);
                    benchSeats.Add(new(
                        (float)ex, (float)ey,
                        Taken: bench.Taken && end == ParkBenches.TakenEnd,
                        BenchHasSomebody: bench.Taken));
                }
            }

            // …and somebody on the far bench, who is scenery. Owner: "benches, the lone figure, the curve
            // that hides the far end." A plate at a coordinate, with nothing to press: a park that started
            // offering things would be a park that had noticed you.
            labels.Add((
                (float)green.FigureX, (float)(green.FigureY + 2.2), green.FigurePlate));

            // ── #817 · THE SUITES, FURNISHED ──────────────────────────────────────────────────────────
            //
            // Owner, standing in one of these on a bare deck: "It really needs tables … the cubicles etc
            // chairs maybe tables etc. It is way too empty" / "in office people sit down".
            //
            // EVERY SOLID THING IN HERE IS ALREADY IN floor.Walls — Core laid the desks, the screens, the
            // kitchenette and the cubicles as segments, so they were drawn and collided with by the loop at
            // the top of this method, exactly as the park's raised beds and the en-suite's own pan are (the
            // fixture idiom the owner named as the one interior in the building that reads right). What is
            // left for a renderer is the STENCIL and the VERB, which is the whole of a renderer's business
            // in a room somebody else furnished.
            foreach (UndergroundComplex.RingRoom suite in green.Frontage)
            {
                foreach (RingOffice.Fixture fitting in suite.Furniture)
                {
                    if (fitting.Plate.Length > 0)
                    {
                        labels.Add(((float)fitting.X, (float)fitting.Y, fitting.Plate));
                    }

                    // #868 · …AND THE BOX ITSELF, FILLED. Owner, sitting in one of these on the back street:
                    // "The graphics kind of does not show there being a table" / "The bench is a line" —
                    // and, three paces away, "The Shelving is clear as furniture goes."
                    //
                    // The sentence above ("what is left for a renderer is the STENCIL and the VERB") was
                    // true of a floor whose only fixtures were rectangles: four wall segments round a dark
                    // gap DO read as a cupboard. It was never true of the back of house, whose bench was one
                    // degenerate box and therefore one stroke, and it was never true of anything at all,
                    // because an outline reads as a space you could stand in. So the deck is handed the
                    // published BOX and fills it — Core's own rectangle, never one measured here (§13.15).
                    Furnish(furniture, in fitting);
                }

                // The chairs take the SIT verb, on the seam the stools and the park benches already use:
                // Core says where a seat is and this hangs a console on it. The plate carries the VERB
                // (#783: "why not use words like SIT DOWN here if it means sitting down?").
                foreach (RingOffice.Chair chair in suite.Seats)
                {
                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveOfficeChair,
                        (float)chair.X, (float)chair.Y, chair.DeckPlate));
                }

                // ── #821 · THE CUBICLES: A LEAF YOU PRESS, AND A SEAT INSIDE ──────────────────────────
                //
                // Owner, standing in the park: "we might want to hide from guards in one toilet cubicle we
                // lock from inside :-D"
                //
                // TWO consoles per cell and deliberately not one, because they are two verbs and the room
                // separates them: the LEAF is at the opening and the SEAT is at the pan, three du apart —
                // which is exactly DeckPlan.InteractRadius, so a captain at the door presses the door and a
                // captain at the pan presses the pan. A single console that meant "lock" or "sit down"
                // depending on which half of a six-du box you stood in would be one key doing two things
                // with no way for the plate to say which.
                //
                // The leaf's plate is the STATE (CubicleLock.PlateFor): a washroom door says VACANT or
                // OCCUPIED, and that is the whole of what a guard walking past this room can read.
                foreach (RingOffice.Stall cell in suite.Cubicles)
                {
                    bool over = cubiclesShut is not null
                        && cubiclesShut.Contains(CubicleKey(level, in cell));

                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveCubicle,
                        (float)cell.DoorX, (float)cell.DoorY, CubicleLock.PlateFor(over)));

                    // The seat takes the OFFICE CHAIR's verb, on the same seam and with the same snap
                    // (#820): Core says where the seat is and this hangs the press on it. The plate is the
                    // cell's own, so a captain reads which cubicle they are stepping into rather than
                    // "AN OFFICE CHAIR" in a WC.
                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveOfficeChair,
                        (float)cell.SeatX, (float)cell.SeatY, cell.Plate));
                }

                // ── #821 · AND THE BASIN RUN, AS ONE FIXTURE THE LENGTH OF ITSELF ─────────────────────
                //
                // #791's E-bus, borrowed whole: the run is one console with a LENGTH on it rather than a tap
                // per basin, for the reason the bar desk is — four [E] dots in a row all opening the same
                // beat would be three pieces of furniture pretending to be a choice. The run is taken off
                // the published taps, so nothing here measures a length of porcelain.
                //
                // #827 · …as the two END TAPS rather than a half-span about their midpoint, which is what a
                // run became when the counter's plate stopped standing at the middle of its own desk. Here
                // the two are the same segment either way; there it is the whole issue.
                if (suite.Basins.Count > 0)
                {
                    RingOffice.Basin first = suite.Basins[0], last = suite.Basins[^1];
                    consoles.Add(new(
                        DeckPlan.ConsoleKind.HiveBasin,
                        (float)((first.X + last.X) / 2.0), (float)((first.Y + last.Y) / 2.0),
                        RingOffice.BasinRunPlate,
                        Run: ((float)first.X, (float)first.Y, (float)last.X, (float)last.Y)));
                }
            }
        }

        // ── #798 · THE BINS, PLATED ────────────────────────────────────────────────────────────────────
        //
        // Owner: "those trash cans are needed so we get rid of the processed materials without connecting
        // them to us too clearly, like leaving them to the table."
        //
        // The BOX is already in floor.Walls — Core stood it there, so it was drawn and collided with by the
        // loop at the top of this method, and one rectangle carries both halves. This is the stencil on it
        // and nothing else, which is the whole of a renderer's business in a room somebody else carved.
        //
        // Plated and NOT a console, the park bench's own idiom one room over: tearing a document up is the
        // SATCHEL'S verb (you have to be holding the thing), and an [E] on a bin that answered nothing would
        // be #757's complaint restated in a corridor.
        foreach (RipAndBin.Bin bin in floor.TheBins)
        {
            labels.Add((
                (float)bin.X, (float)(bin.Y - RipAndBin.HalfDu - 1.4), bin.Plate));
        }

        // ── #818 · WHAT IS STANDING IN EVERY OTHER ROOM IN THE BUILDING ────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor."
        //
        // EVERY SOLID THING IN HERE IS ALREADY IN floor.Walls — Core laid the benches, the fume hoods, the
        // racking and the machines as segments, so they were drawn and collided with by the loop at the top
        // of this method, exactly as the ring's desks and the en-suite's pan are. What is left for a renderer
        // is the STENCIL and the VERB, which is the whole of a renderer's business in a room somebody else
        // furnished.
        //
        // ONE STENCIL PER ROOM, and that is Core's doing rather than a filter here: the placer plates the
        // signature fitting and hands the rest over blank, because a plate over every bay of racking in a
        // long store is a floor nobody can read. The `if` below is the ring's own (#817) and is what makes
        // that decision visible from this end.
        for (int r = 0; r < floor.TheRooms.Count; r++)
        {
            UndergroundComplex.Room room = floor.TheRooms[r];
            if (room.Kind != UndergroundComplex.RoomKind.Chamber)
            {
                continue;   // the suites, the hall and its booths are drawn by their own passes above
            }

            foreach (RingOffice.Fixture fitting in room.Furniture)
            {
                if (fitting.Plate.Length > 0)
                {
                    labels.Add(((float)fitting.X, (float)fitting.Y, fitting.Plate));
                }

                // #868 · The same fill the ring's furniture takes. A chamber's kit is laid as SEGMENTS on
                // purpose (ChamberFitting's class summary: Lab 45 says the sightline is O(walls), and four
                // rectangles per room in fifteen hundred rooms is a frame budget spent drawing cupboards),
                // so most of these are degenerate and the pen skips them — the call is made anyway, so the
                // day a chamber's bench grows a depth it is drawn without anybody remembering this line.
                Furnish(furniture, in fitting);
            }

            // The stools take the SIT verb on the seam the office chairs, the stools at the bar and the park
            // benches already use: Core says where a seat is and this hangs a console on it. The plate
            // carries the VERB (#783: "why not use words like SIT DOWN here if it means sitting down?").
            foreach (RingOffice.Chair seat in room.Seats)
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveOfficeChair,
                    (float)seat.X, (float)seat.Y, ChamberFitting.StoolPlate));
            }
        }

        // ── #853 · AND THE CONFERENCE POSTERS, ON THE ONE DEPARTMENT THEY ARE ABOUT ────────────────────
        //
        // Owner, a postdoc's own gag: "as gags we could have gen-ai conference posters … jokes about how
        // hydrogen is the new promising tech (still 100 years in future)".
        //
        // A ViewObject and NOT a new console kind, because it is not a new verb: stop at a thing on a wall,
        // look at it, and read the card. That is the same press the monolith, the false slab and the ports'
        // own PIRATE INSURANCE poster have taken since #380 — and the art slot degrades exactly as theirs
        // do, which is why these can ship with the copy today and the pictures whenever they are shot.
        //
        // The kicker hangs crooked. The deck draws its labels straight, so the crookedness is TOLD — Core
        // puts it in the plate (LabPosters.Poster.Plate) rather than this file inventing a rotation nobody
        // asked for. A renderer that can tilt a label cheaply has the flag published and waiting.
        foreach (LabPosters.Poster poster in floor.TheWalls)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.ViewObject,
                (float)poster.X, (float)poster.Y,
                poster.Plate, poster.ArtUrl, poster.Card));
        }

        // ── #831 · AND THE WATCHCLOCK STATIONS, ON THE FLOORS THAT HAVE A ROUND ────────────────────────
        //
        // Owner: "they actually in real life like have these check points they electronically sign on rounds
        // to prove they did their round." A small plate on a wall, and nothing else — no console, no verb, no
        // card. It answers a question the player asks with their eyes ("why is he standing there") and asking
        // it of a plate with [E] would turn an answer into an errand.
        //
        // Core says where every one of them is (PatrolBeat.CheckpointsOn) and what is stencilled on it; this
        // measures nothing.
        if (PatrolBeat.IsPatrolled(bodyId, level))
        {
            foreach (PatrolBeat.Checkpoint point in PatrolBeat.CheckpointsOn(floor, field))
            {
                labels.Add(((float)point.X, (float)point.Y, point.Plate));
            }
        }

        // The cars, on every floor, in the same places. #801 · Both of them, off one list, each with the
        // sign Core paints on it — a renderer choosing which console kind goes on which car would be a
        // second opinion about a machine it does not own.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);
        foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(field))
        {
            bool cage = car.Kind == UndergroundComplex.ShaftKind.Cage;
            consoles.Add(new(
                cage ? DeckPlan.ConsoleKind.HiveLift : DeckPlan.ConsoleKind.HiveServiceLift,
                (float)car.X,
                (float)(car.Y + ((cage ? 1 : -1) * (UndergroundComplex.CorridorHalf + 2.5))),
                car.Sign));
        }

        foreach (SurfaceLayout.Landmark m in floor.Labels)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }

        // #600 · THE DEPTH, PAINTED BY THE LIFT. Owner, riding between floors built from the same bones:
        // "something different in every floor so we visually spot some difference" / "we can use seriously
        // large numbers there :-D" / "or depths (in meters)".
        //
        // Two lines, stencilled on the wall beside the car the way a stairwell or a car park marks a level:
        // the depth, which is a fact about where you are standing, and the department, which is what this
        // floor was for. Together they are the glance that says which floor you stepped out on — and the
        // depth is the number that makes the walk back up mean something.
        // Owner, seeing the first cut: "Let's put the elevation next to the elevator... now it is too far
        // from it." It was 30 du off to one side, which is most of a screen — a number that far from the
        // thing it describes is not signage, it is litter. It sits just above the car's own mouth now, over
        // the 🛗 LIFT plate, which is where a building paints a level: on the wall you face when the doors
        // open.
        // #605 · THE PLATE BY THE CAR — depth over department, both painted at signage size.
        //
        // Owner, twice: "Let's put the elevation next to the elevator... now it is too far from it", then
        // "the name of the floor should read next to the elevator... we have them in the buttons let's have
        // them on the level also" and "it is way too small and too far from the elevator".
        //
        // Both complaints are the same fault. The name was pinned 26 du off down the spine at caption size,
        // which is neither next to the lift nor readable at a glance — so it was information the captain had
        // to go and look for, about the one thing they most need to know without looking.
        //
        // They are one plate now, directly over the car's mouth: the depth big because it is the number that
        // decides whether you can walk back up, the department under it because that is what the floor was
        // FOR — and it is what the panel's own buttons promised on the way in.
        // Owner, on why it has to dominate: "It is the where-am-I question answer when you come with the
        // elevator so it is like the most important thing to see."
        //
        // That is the whole brief. A captain steps out of a car onto one of twenty floors cut from identical
        // bones, and the first thing they need is not a console or a corridor — it is WHICH ONE. So the plate
        // sits directly over the car's mouth, in the eye-line of somebody who has just turned around, and it
        // is the largest thing drawn on the floor.
        //
        // And it is modelled on a real reflex, which is why it belongs here rather than in the HUD — owner:
        // "sometimes people get off the elevator at wrong floor so there is this instinct to always check
        // that the floor is correct." A number on the instrument panel would answer the question; a plate on
        // the WALL is the thing you actually look at, because looking at it is what people do.
        // #612 · AND WHETHER YOU CAN BREATHE HERE. Owner, reading the plate: "it should say if the floor is
        // pressurized also" — and, of the gauge: "where here does it say if I consume tanks or have air?"
        //
        // It is the same question twice, and the plate is the right place to answer it: a captain stepping
        // out of a car needs WHERE AM I and CAN I BREATHE in one glance, and the second one decides whether
        // everything they were about to do is affordable. Three lines, one plate, and the air line carries
        // the colour so it reads before it is read.
        //
        // THE WORDS AND THE VERDICT ARE BOTH SuitAir'S. This line first shipped calling
        // UndergroundComplex.HoldsPressure itself and spelling its own two strings — which made it the THIRD
        // place in the game deciding whether a tank is running, after the drain and the hud. Three places
        // that must agree is not redundancy, it is a countdown to a disagreement, and #608 proved it inside a
        // day by adding a fourth way to breathe that only the drain heard about. The plate asks
        // SuitAir.SourceOf of this level and prints SuitAir.PlateLine, so the sign on the wall and the gauge
        // on the suit are physically incapable of saying different things about the same floor.
        double signX = shaftX;
        double signY = shaftY + UndergroundComplex.CorridorHalf;
        SuitAir.Supply floorAir = SuitAir.SourceOf(bodyId, level, insideShelter: false, aboard: false);
        var bigLabels = new List<(float X, float Y, string Text, float Px, int Tone)>
        {
            ((float)signX, (float)(signY + 10.6), UndergroundComplex.DepthPaint(level), 44f, 0),
            ((float)signX, (float)(signY + 7.8), UndergroundComplex.NameOf(bodyId, level), 19f, 0),
            ((float)signX, (float)(signY + 5.4), SuitAir.PlateLine(floorAir), 17f,
                SuitAir.Drawing(floorAir) ? 2 : 1),
        };
        // ── #694 · AND THE FACILITY'S OWN NAME, ON THE FLOORS YOU ENTER IT BY AND NOWHERE ELSE ────────────
        //
        // Owner, standing on B11 of a thirteen-floor site: "every floor has the text 'The Clinic' on it.
        // Some kind of artifact?"
        //
        // It was not an artifact and it was not a leak — this line drew unconditionally, so a name that
        // should have landed once landed thirteen times, and by the third floor it had stopped being a name
        // and become part of the wallpaper. His question IS the finding: a sign a player asks about because
        // they suspect the RENDERER is doing something wrong is a sign that is no longer saying anything.
        //
        // A building says its name where you ENTER it. That is B1, and — where the site has one — the
        // unlisted band's own shaft head, which is the single place in the game where this plate names a
        // different Kind from everything above it: ▣ THE CLINIC under twelve floors of RETENTION 40 YR is
        // #592's whole arithmetic delivered by one sign, and it was being spent on every floor and therefore
        // on none. Everywhere else the plate over the car (B11 · LONG STORAGE) and the department livery
        // already answer which floor this is, which is what they are for.
        //
        // THE LAW IS CORE'S, NOT THIS FILE'S. Which floors you arrive on is a fact about the building — it
        // is BandTop and HasUnlistedBand, the same two calls the shafts and the cards are cut from — and a
        // renderer that answered it here would be one more caller reasoning about a shaft it does not own.
        // HiveInterior asks and draws.
        if (UndergroundComplex.ShowsFacilityPlate(bodyId, level))
        {
            labels.Add(((float)shaftX - 30f, (float)(shaftY + 4.5), UndergroundComplex.TitleOf(kind)));
        }

        // ── #608 · AND THE REFUGE'S OWN PLATE, IN THE PLATE-BY-THE-LIFT'S OWN LANGUAGE ───────────────────
        //
        // Smaller than the depth over the car, because the depth is the where-am-I question and this is the
        // where-is-the-air one — but the same KIND of lettering, so a captain crossing a dead floor reads it
        // the way they read a fire exit: without meaning to.
        //
        // TONE 1, WHICH IS THE WHOLE OF THE RECONCILIATION WITH #612. The plate over the lift now answers
        // "can I breathe here" in colour — StencilAir for PRESSURISED, StencilDead for NO ATMOSPHERE — and
        // #612's own rule is that the instruments may never disagree about air. On a dead floor that plate
        // is shouting NO ATMOSPHERE in the dead ink while this one says AIR forty du away, so the two would
        // read as a contradiction unless they are plainly speaking about different things in one shared
        // language. They are: tone 1 means YOU CAN BREATHE HERE, wherever "here" is, and the word REFUGE
        // says the "here" is this room and not this floor. The plate describes the level; this describes a
        // door. Same ink, same claim, different scope — and the hud's AIR: TANKS/ROOM agrees with both,
        // because all three now read TankIsDrawing.
        foreach (UndergroundComplex.Refuge refuge in floor.Refuges)
        {
            bigLabels.Add(((float)refuge.X, (float)(refuge.Y + UndergroundComplex.RefugeHalfHeight + 3.2),
                UndergroundComplex.RefugeGlyph, 26f, 1));
        }

        return new DeckPlan(
            [.. walls], [.. consoles], [.. labels], [.. backdrops],
            spawnX: SpawnOn(field).X, spawnY: SpawnOn(field).Y,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (_, _) => floor.Name,
            doors: [.. doors], shipFixtures: false, followCam: true,
            // #707 · THE CANTEEN'S OWN TOPS, and not the ship's any more. This read
            // `tables: DeckPlan.Ship.Tables` — three round tops at the SHIP's cantina coordinates, which on
            // a Hive floor land at y = +7.5, forty du above the top of the field and outside every floor
            // this generator has ever drawn. Nobody had reported it because nobody had reason to look up
            // there, and it is the mirrored-constant shape exactly: a table list borrowed from a building
            // whose coordinates mean something else. The rings belong to a room now, and the room is on
            // this floor.
            tables: [.. tables],
            stools: [.. stools],
            benchSeats: [.. benchSeats],
            furniture: [.. furniture],
            bigLabels: [.. bigLabels],
            // #605 · The floor's department livery. Null on the band nobody listed, so that concrete is the
            // one place down here left bare — the absence is the tell.
            hullInk: UndergroundComplex.LiveryFor(bodyId, level));
    }

    /// <summary>
    /// #868 · ONE PUBLISHED FITTING, HANDED TO THE PEN AS THE RECTANGLE IT IS.
    ///
    /// <para>The one seam between a piece of furniture Core stood in a room and a filled shape on the deck,
    /// written once so the ring's rooms and the building's chambers cannot each answer it their own way. It
    /// makes exactly two decisions and both are asked of the KIND: whether a fitting is furniture at all
    /// (a cubicle is a little ROOM you step inside — filling one would draw the building's only hiding place
    /// as a solid block) and what it IS (see <see cref="DeckPlan.FurnitureSpot.ToneOf"/>). A DEGENERATE box
    /// is dropped here rather than in the renderer, because a zero-area rectangle is not a picture of
    /// anything and a screen between two workstations honestly IS a line.</para>
    /// </summary>
    private static void Furnish(List<DeckPlan.FurnitureSpot> into, in RingOffice.Fixture fitting)
    {
        if (!DeckPlan.FurnitureSpot.IsFurniture(fitting.Kind)
            || Math.Abs(fitting.X1 - fitting.X0) < 0.001
            || Math.Abs(fitting.Y1 - fitting.Y0) < 0.001)
        {
            return;
        }

        into.Add(new(
            (float)fitting.X0, (float)fitting.Y0, (float)fitting.X1, (float)fitting.Y1,
            DeckPlan.FurnitureSpot.ToneOf(fitting.Kind)));
    }

    /// <summary>One key per room per floor, so a searched room on B2 is not a searched room on B3.</summary>
    public static int RoomKey(int level, int roomIndex) => (level * 1000) - roomIndex;

    /// <summary>#803 · What the deck wears where a lock used to be. Not the padlock (it is not locked any
    /// more) and not nothing (the plate is still worth reading) — the same hole the card and the field book
    /// use for a way that has been opened.</summary>
    public const string ShotOpenGlyph = "🕳";

    /// <summary>
    /// #803 · One key per LOCK per floor. Keyed on the door's own geometry rather than on its sign, because
    /// a branch office reuses its door vocabulary — a floor can carry two doors reading LONG STORAGE, and a
    /// captain who shoots one of them has not opened the other. The floor plan is pure and deterministic per
    /// (body, level), so the same door produces the same key on every rebuild, every visit and every load.
    /// </summary>
    public static string LockKey(int level, UndergroundComplex.LockedDoor l) =>
        FormattableString.Invariant($"{level}|{l.X1:F2},{l.Y1:F2},{l.X2:F2},{l.Y2:F2}");

    /// <summary>
    /// #821 · One key per CUBICLE per floor, keyed on the cell's own box for <see cref="LockKey"/>'s reason
    /// — a floor carries a row of cubicles all plated CUBICLE 1 · STEP IN, one per big suite plus the row in
    /// the public washroom, and a captain who shut one of them has not shut the others. The generator is pure
    /// and deterministic per (body, level), so the same cell keys the same on every rebuild.
    /// </summary>
    /// <summary>#821 · One leaf, as the string the doorway sweep matches on. Its own geometry and nothing
    /// else, so a cell's door and the floor's copy of that same door key identically — they ARE the same
    /// record, and this is only how the sweep says so without a nested loop per floor.</summary>
    private static string LeafKey(in SurfaceLayout.Doorway d) =>
        FormattableString.Invariant($"{d.X1:F2},{d.Y1:F2},{d.X2:F2},{d.Y2:F2}");

    public static string CubicleKey(int level, in RingOffice.Stall stall) =>
        FormattableString.Invariant(
            $"wc|{level}|{stall.X0:F2},{stall.Y0:F2},{stall.X1:F2},{stall.Y1:F2}");
}
