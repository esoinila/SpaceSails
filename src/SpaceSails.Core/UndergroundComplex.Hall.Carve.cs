using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #751 · THE HALL, CARVED — the one method that turns a chosen column and a ground measurement into
/// walls, glass, a counter, a goods hoist, a desk, stools, cabinets, tops, pillars and doors.
///
/// <para>It stayed one method through the cut on purpose. Its twelve internal banners read as passes, but
/// they are passes over ONE running set of locals — the (u, v) projection, the wall lists, the running
/// door budget — and turning them into named methods is a behaviour-bearing split, which this repo holds
/// to the snapshot-first standard (a guard captured on the old code and pushed before a line moves) and
/// not to the pure-move standard this lane is working to. The file-level cut is free; that one is not, and
/// it is a separate lane.</para>
///
/// <para>Split out of <c>UndergroundComplex.Hall.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>
    /// #751 · THE HALL, CARVED. Returns null when the ground will not take one, in which case the floor
    /// keeps the ordinary three-top canteen and a guard says so out loud.
    ///
    /// <para>Laid out in the hall's own two axes so nothing here has to think about which way the rib
    /// points: <b>u</b> runs outward from the rib's face, <b>v</b> runs down the rib from the spine. The
    /// front wall (u = 0) and the near wall (v = 0) are not built at all — they are the rib's face and the
    /// spine's face, already standing, already cut. That is the whole of the one-gap law here: the hall
    /// cannot open a door in the wrong place because it never opens one.</para>
    /// </summary>
    private static HallSite? CarveHall(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<(double Lo, double Hi)> spineCuts,
        string bodyId, int level, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double? serviceX, double leftEnd, double rightEnd,
        double roomScale, bool glazed)
    {
        Comfort use = HallUseOn(bodyId, level);

        if (HallGround(
                bodyId, use, ribs, ribIndex, side, mouth, far, shaftX, serviceX, leftEnd, rightEnd,
                roomScale)
            is not { } ground)
        {
            return null;
        }

        double ribX = ribs[ribIndex].X;
        bool cabs = use == Comfort.UpperCanteen;
        double cabBand = cabs ? HallCabinetDepthDu : 0.0;
        double pitch = HallTopPitchDu;
        int tops = HallSeatBill(bodyId, use, HallSeatsFor(bodyId, use)).Count;
        double faceX = ribX + (side * CorridorHalf);
        double length = ground.Length;
        double width = Math.Min(ground.Width, ground.Wanted);

        // ── FROM (u, v) TO THE FIELD'S OWN COORDINATES, in one place. ───────────────────────────────────
        bool down = ribs[ribIndex].Down;
        double X(double u) => faceX + (side * u);
        double Y(double v) => down ? mouth - v : mouth + v;

        double x0 = Math.Min(X(0), X(width)), x1 = Math.Max(X(0), X(width));
        double y0 = Math.Min(Y(0), Y(length)), y1 = Math.Max(Y(0), Y(length));

        // ── THE THREE WALLS THE HALL OWNS. (The fourth and fifth are the rib's face and the spine's.)
        walls.Add(new(X(width), Y(0), X(width), Y(length), true));        // the outer wall

        // ── #759 · THE FAR WALL, WHICH IS GLASS WHERE THERE IS A PARK BEHIND IT ─────────────────────────
        //
        // Owner requirement, pinned: the restaurant scene must have a) A VIEW TO THE PARK and b) A WINDOW
        // WALL BETWEEN. Both of the room's own pictures are shot through it — the stool view is counter,
        // glass, green; the hall's own establishing art is steel tables, riveted glass, green — so the deck
        // plan drawing an ordinary poured wall there would be the drawn room and the pictured room
        // disagreeing about the one surface both of them are about.
        //
        // ONE SEGMENT, PUBLISHED TWICE AND BUILT ONCE. It goes in the `glass` list rather than the wall
        // list, and the client puts it back into the deck as a wall that draws in the window idiom — so it
        // collides exactly like the poured wall it replaced (a body may not pass) and reads as glass (an eye
        // may). A second segment laid on the same line would be the drawn-versus-simulated split this house
        // has a name for.
        var farWall = new SurfaceLayout.Wall(X(0), Y(length), X(width), Y(length), true);
        (glazed ? glass : walls).Add(farWall);

        // ── THE COUNTER · a long bar wall along the far end, with the service side shut off behind it.
        double counterV = length - HallCounterBandDu;
        double counterU0 = HallDoorAisleDu;
        double counterU1 = width - cabBand - HallEdgePadDu;

        // ── #775 · AND THE GOODS HOIST, PARKED AT ONE END OF THAT BAND ─────────────────────────────────
        //
        // The band is already a sealed strip the customer never stands in, five du deep and the length of
        // the bar. The hoist is the first twelve du of it, divided off by one wall — so the car's four
        // sides are the band's end cap, that new divider, the room's own far wall behind it, and the
        // shutter in the counter's line in front. Four walls, one of them new: freight access is a fixture
        // in a room somebody already carved, which is the cheapest honest version of the owner's ask.
        //
        // WHICH END. The one nearest u = 0, which on the hall's rib is the end the park's gate is on — the
        // beds' produce comes in the gate at the far end of the corridor and goes straight into the hoist,
        // and the counter that serves it is on the other side of the same divider. Nothing says any of
        // that; the geometry is the sentence.
        //
        // …and it is only fitted where BOTH halves of the band survive it: a car narrower than a doorway is
        // not a freight lift, and a bar left with less counter than that is not a bar. Every hall the game
        // ships clears both by a wide margin; the clause is here so the day one does not, the floor says so
        // by having no hoist rather than by having a serving hatch.
        double hoistU1 = Math.Min(counterU0 + FreightCarWidthDu, counterU1);
        bool hoisted = hoistU1 - counterU0 >= 2 * DoorHalf && counterU1 - hoistU1 >= 2 * DoorHalf;
        double serveU0 = hoisted ? hoistU1 : counterU0;

        // ── #827 · THE DESK, CARVED ONCE ───────────────────────────────────────────────────────────────
        //
        // Owner, from stool 3: "we should have the counter as something that cannot be walked through but
        // can be used as a table." The box below is the WHOLE of where the bar is, and every other clause in
        // this method now reads it instead of re-deriving it off counterV: the collidable front, the [E]
        // run, the row of stools and the photograph. Three of those four used to do their own arithmetic on
        // the same line and land at three different offsets from it.
        //
        // WHICH EDGE IS THE FACE. v = counterV is the hall side of the band — the edge a customer can reach.
        // Stated in (u, v) like everything else here, so a rib that runs up the field and one that runs down
        // it hand the same answer without a normal being written down anywhere.
        bool serves = Interior.CounterService.For(bodyId, use) is not null;
        CounterDesk desk = TheCounterDesk((u, v) => (X(u), Y(v)), serveU0, counterU1, counterV, length, serves);

        walls.Add(desk.Face);
        walls.Add(new(X(counterU0), Y(counterV), X(counterU0), Y(length), true));
        walls.Add(new(X(counterU1), Y(counterV), X(counterU1), Y(length), true));

        FreightLift? freight = null;
        if (hoisted)
        {
            // #827 · AND THE SHUTTER STAYS A DOOR. The serving desk's own front is walled by its face
            // above; the twelve du in front of the car are not, and they must not be — the
            // shutter is a LOCKED DOOR (see Build's freight clause), which is this building's grammar for a
            // way through that will not open: the client hangs a leaf on it, walls it behind, and #803 lets
            // a captain take the hasp off it with a sentry. A poured wall laid here as well would be a door
            // that opens in the sentence and stays walled on the plan, which is the same bug read backwards.
            walls.Add(new(X(hoistU1), Y(counterV), X(hoistU1), Y(length), true));   // the car's divider
            freight = new FreightLift(
                Math.Min(X(counterU0), X(hoistU1)), Math.Min(Y(counterV), Y(length)),
                Math.Max(X(counterU0), X(hoistU1)), Math.Max(Y(counterV), Y(length)),
                new SurfaceLayout.Doorway(X(counterU0), Y(counterV), X(hoistU1), Y(counterV)),
                X((counterU0 + hoistU1) / 2.0), Y(counterV - HallEdgePadDu),
                FreightPlate);
        }

        // #780 · …and THE PICTURE OF IT, over the same three walls' own box. Owner: "see how in the space
        // bars we have the image of bar desk at the spot where the bar desk is." Built HERE, out of the very
        // (u, v) the segments above were built from, so the frame and the furniture cannot drift apart —
        // the alternative was a renderer measuring a counter it did not carve, which is the mistake that has
        // set this project's captain down inside a wall twice. Only where a counter actually serves: the
        // washroom has no bar, and a picture of one over a room's back wall would be the game saying
        // something about that room that is not true.
        //
        // #775 · …and it starts where the SERVING counter starts, which since the goods hoist took the end
        // of the band is not where the band starts. A bar-desk photograph stretched over the hoist's own
        // twelve du would be a picture of a counter drawn across a freight car — the drawn room and the
        // carved room disagreeing about one wall, which is precisely the split #759 kept the glass out of.
        //
        // #827 · …and it is the DESK'S OWN BOX, handed over rather than measured a second time out of the
        // same four numbers. The photograph was the one thing on this bar that was in the right place, and
        // it was in the right place by two authors agreeing — which is a coincidence with a maintenance
        // schedule, not a law. Now the picture is stretched over the rect and the rect is the counter.
        var spots = new List<SpotArt>(1);
        if (CounterArtFor(bodyId, use) is { } deskArt)
        {
            spots.Add(new SpotArt(deskArt, desk.X0, desk.Y0, desk.X1, desk.Y1));
        }

        // ── #792 · AND THE STOOLS ALONG THE FRONT OF IT ────────────────────────────────────────────────
        //
        // Owner, playtest 2026-08-08: "people looking to sit down look at those like hungry wild beasts
        // look at their prey… Now I have trouble finding a free table." The row has existed in Core since
        // #756 — eight seats, occupied or not, watch by watch — and has never been anywhere on the floor,
        // so a captain could be told the row was full only by walking up and pressing.
        //
        // WHERE, out of the very (u, v) the three counter segments above were built from, exactly as the
        // desk picture is. The order is the row's own: entry s is stool s, and it has to be, because that
        // ordinal is what Interior.TheStools.Taken answers about — a row published in some other order
        // would draw one seat's occupancy over another seat, which is this project's drawn-versus-simulated
        // class with somebody sitting in it.
        //
        // AND ONLY WHERE A COUNTER SERVES. Asked of Interior.CounterService, which is the same call the
        // stool verb itself is gated on, rather than restated here as "upper canteen and not the head
        // office" — two spellings of one condition is how a room grows eight seats nothing will ever sit on.
        //
        // #791 · …AND ALONG THE SERVING DESK, which is not the whole band. This laid the row from counterU0
        // — the start of the band — so on every hall in the game the first stool and part of the second
        // stood in front of the GOODS HOIST'S SHUTTER (#775 took the first twelve du of that band for a
        // freight car). Two tall seats at a roller door, in a room whose own photograph starts twelve du
        // further along: the drawn desk and the seated row disagreeing about where the bar is. They run the
        // service run's length now, which is the picture's length, which is the desk's length.
        //
        // #827 · …AND THEY ARE THE DESK'S OWN ROW NOW, laid inside TheCounterDesk beside the gaps they
        // alternate with. There is no second list here to keep in step with the first: Hall.StoolRow reads
        // the desk, and a seat that moved moved because the counter moved.

        // ── #791 · AND THE RUN THE WHOLE DESK SERVES OVER ──────────────────────────────────────────────
        //
        // Owner, live: "there is only one spot to get service on it… we would need an E-bus of the bar desk
        // length instead of one bar keep cashier at a single spot."
        //
        // #827 · THE RUN IS THE DESK'S FACE — the very segment the collidable wall was laid on, handed over.
        // It used to be laid HallServiceStandoffDu out from the counter's line, on the square a customer
        // stands on; so the deck lit a cyan rail labelled THE COUNTER two du clear of the bar's photograph,
        // and the owner read the picture as the counter because the picture WAS the counter. Where a body
        // stands is a different question from where the desk is, and it is answered separately below.
        //
        // …and the keep stands opposite the TILL rather than at the middle of the band. A cashier stands at
        // the till; that is the whole of what a till gap is for, and the gap is published, so nothing here
        // has to work out where "a quarter of the way along" fell.
        //
        // Only where the counter serves, off the same one call the desk's row asks. A run published for a
        // desk nobody serves at would be an [E] bus to a card that does not exist.
        double bandHalf = HallCounterBandDu / 2.0;
        (double keepX, double keepY) =
            (X((serveU0 + counterU1) / 2.0), Y(counterV + bandHalf));
        (double standX, double standY) =
            (X((serveU0 + counterU1) / 2.0), Y(counterV - HallServiceStandoffDu));
        if (desk.Gap(CounterPost.Till) is { StandoffDu: > 0 } paid)
        {
            // INTO the desk is out of the customer's own square and through the face — one published place
            // read backwards, rather than a fifth restatement of which way this hall's v axis runs.
            double nx = (paid.FaceX - paid.X) / paid.StandoffDu;
            double ny = (paid.FaceY - paid.Y) / paid.StandoffDu;
            (keepX, keepY) = (paid.FaceX + (nx * bandHalf), paid.FaceY + (ny * bandHalf));
        }

        ServiceRun? service = serves
            ? new ServiceRun(
                desk.FaceX0, desk.FaceY0, desk.FaceX1, desk.FaceY1,
                keepX, keepY, standX, standY)
            : null;

        // ── THE CABINETS · a row of doors down the hall's outer wall.
        var cabinets = new List<Cabinet>(CabinetsPerHall);
        if (cabs)
        {
            double band = (length - (2 * HallEdgePadDu)) / CabinetsPerHall;
            double cabU0 = width - HallCabinetDepthDu;
            double ccU = (cabU0 + width) / 2.0;

            // ── #822 · THE ROW INTERCONNECTS, AND THAT IS WHERE THE SECOND WAY OUT COMES FROM ───────────
            //
            // A cabinet is nowhere near bedroom-small — it is a negotiating room, not a phone booth — so a
            // single leaf made it the most literal trap on the floor: a windowless box off the back wall of
            // a bar, with the only way out behind whoever you came in to meet.
            //
            // Two leaves in its own face is the obvious answer and it is the WRONG one here, and the ring
            // already paid to learn why (#817/#724): this face is thirteen to eighteen du long, and two
            // full-width leaves in it leave a pier of about a du — which the movement funnel reads as
            // standing in a doorway and answers by holding still. The room needs two ways out; it does not
            // need both of them in the same wall.
            //
            // So the PARTY WALLS carry them. The cabinets are laid edge to edge on the band's own division
            // lines instead of with a du of rock between them, each dividing wall is built once, and the
            // inner ones have a leaf in the middle of their depth. Three booths become a run you can pass
            // through — and a captain cornered in cabinet 3 leaves through cabinet 2, which is exactly the
            // kind of route the standing law was asked for.
            var partyLeaf = new SurfaceLayout.Doorway?[CabinetsPerHall + 1];
            for (int k = 0; k <= CabinetsPerHall; k++)
            {
                double v = HallEdgePadDu + (k * band);
                if (k == 0 || k == CabinetsPerHall)
                {
                    walls.Add(new(X(cabU0), Y(v), X(width), Y(v), true));   // the two ends of the run
                    continue;
                }
                walls.Add(new(X(cabU0), Y(v), X(ccU - DoorHalf), Y(v), true));
                walls.Add(new(X(ccU + DoorHalf), Y(v), X(width), Y(v), true));
                partyLeaf[k] = new SurfaceLayout.Doorway(X(ccU - DoorHalf), Y(v), X(ccU + DoorHalf), Y(v));
            }

            for (int c = 0; c < CabinetsPerHall; c++)
            {
                double vLo = HallEdgePadDu + (c * band);
                double vHi = HallEdgePadDu + ((c + 1) * band);
                double vMid = (vLo + vHi) / 2.0;

                // The face, with the one gap it has always had — cut to the same DoorHalf the corridor and
                // the en-suites are cut to. The party walls above are the other two sides.
                walls.Add(new(X(cabU0), Y(vLo), X(cabU0), Y(vMid - DoorHalf), true));
                walls.Add(new(X(cabU0), Y(vMid + DoorHalf), X(cabU0), Y(vHi), true));

                var leaves = new List<SurfaceLayout.Doorway>(3)
                {
                    new(X(cabU0), Y(vMid - DoorHalf), X(cabU0), Y(vMid + DoorHalf)),
                };
                foreach (int k in (int[])[c, c + 1])
                {
                    if (partyLeaf[k] is { } through)
                    {
                        leaves.Add(through);
                    }
                }

                cabinets.Add(new Cabinet(
                    c + 1, (X(cabU0) + X(width)) / 2.0, Y(vMid),
                    HallCabinetDepthDu / 2.0, (vHi - vLo) / 2.0,
                    (X(ccU), Y(vMid)), leaves));
            }
        }

        // ── THE TOPS · a grid in what is left, at whatever pitch the ground allows up to the module's own.
        double uLo = HallDoorAisleDu;
        double uHi = width - cabBand - HallEdgePadDu;
        double tvLo = HallEdgePadDu;
        double tvHi = counterV - (2 * HallEdgePadDu);
        double uw = Math.Max(pitch, uHi - uLo), vh = Math.Max(pitch, tvHi - tvLo);

        int cols = Math.Clamp((int)Math.Round(Math.Sqrt(tops * uw / vh), MidpointRounding.AwayFromZero), 1, tops);
        int rows = (tops + cols - 1) / cols;

        var laid = new List<(double X, double Y)>(tops);
        for (int t = 0; t < tops; t++)
        {
            double u = uLo + ((((t % cols) + 0.5) / cols) * uw);
            double v = tvLo + ((((t / cols) + 0.5) / rows) * vh);
            laid.Add((X(u), Y(v)));
        }

        // ── THE PILLARS · poured, load-bearing, and honest: this rock is heavy. Placed on the grid's own
        //    seams so they break sightlines without ever standing on a chair.
        double ph = Math.Min(0.9, Math.Min(uw / cols, vh / rows) / 5.0);
        for (int p = 1; p < Math.Min(cols, 4); p++)
        {
            double u = uLo + ((p / (double)cols) * uw);
            double v = tvLo + (((p % 2 == 0 ? 1 : 2) / 3.0) * vh);
            walls.Add(new(X(u - ph), Y(v - ph), X(u + ph), Y(v - ph), true));
            walls.Add(new(X(u - ph), Y(v + ph), X(u + ph), Y(v + ph), true));
            walls.Add(new(X(u - ph), Y(v - ph), X(u - ph), Y(v + ph), true));
            walls.Add(new(X(u + ph), Y(v - ph), X(u + ph), Y(v + ph), true));
        }

        // ── #775 · THE DOORS · WHAT THE ROOM ALREADY HAD, AND WHAT A CODE SAYS IT MUST HAVE ─────────────
        //
        // WHAT IT ALREADY HAD, asked of the function that cut them rather than counted off the wall. The
        // hall never opened a door in the rib's face — RibFace leaves a gap at every room slot on that
        // column and those gaps ARE the hall's doors (#751). RoomCentresAlong is the one function that says
        // where a slot is, and it is called here for the same reason the wall builder and the room builder
        // both call it: a second answer about where a door is, is this file's oldest and most expensive bug.
        var doors = new List<SurfaceLayout.Doorway>();
        foreach (double cy in RoomCentresAlong(mouth, far, down, roomScale))
        {
            doors.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
        }

        // WHAT THE CODE SAYS. The count comes off the room's own published floor (HallEgressDoors) and the
        // shortfall is cut into the spine's face — never fewer than one, because the owner's first
        // complaint is not about arithmetic: a venue with no door on the main corridor has no entrance at
        // all, whatever the total says.
        double floorDu2 = (x1 - x0) * (y1 - y0);
        int wantSpine = Math.Max(1, HallEgressDoors(floorDu2) - doors.Count);

        // WHERE THEY GO, in the hall's own u. Clear of both corners, and clear of the cabinet band — a
        // front door opening into the back of a negotiating cabinet is #585's stranded room told from the
        // corridor side.
        double duLo = HallSpineDoorEdgeDu;
        double duHi = width - cabBand - HallSpineDoorEdgeDu;

        // THE FIRST ONE IS THE ENTRANCE, AND IT GOES WHERE THE WALKER IS. #751 already puts the hall on the
        // column nearest the car; this is that same rule one step further in, and it is the whole of the
        // owner's "a venue's entrance should find YOU": the captain steps out of the lift onto the spine,
        // turns, and the door is the nearest thing on the wall.
        var atU = new List<double>();
        if (duHi > duLo)
        {
            double toShaft = side * (shaftX - faceX);   // the shaft, in this hall's own outward axis
            atU.Add(Math.Clamp(toShaft, duLo, duHi));

            // THE REST ARE SPREAD, each one put as far from every door already placed as the wall allows —
            // which is what a fire officer means by spread, and what a fixed pitch stops meaning the moment
            // the entrance is not in the middle. Placed only while they can still be a door's width and a
            // half apart: two exits sharing a jamb are one exit with a thick frame.
            const int samples = 240;
            while (atU.Count < wantSpine)
            {
                double best = duLo, bestGap = -1;
                for (int s = 0; s <= samples; s++)
                {
                    double u = duLo + ((duHi - duLo) * s / samples);
                    double gap = double.MaxValue;
                    foreach (double placed in atU)
                    {
                        gap = Math.Min(gap, Math.Abs(placed - u));
                    }
                    if (gap > bestGap)
                    {
                        (bestGap, best) = (gap, u);
                    }
                }
                if (bestGap < 3 * DoorHalf)
                {
                    break;   // the wall has run out of room. The guard says so out loud rather than here.
                }
                atU.Add(best);
            }
        }

        foreach (double u in atU)
        {
            double dx0 = Math.Min(X(u - DoorHalf), X(u + DoorHalf));
            double dx1 = Math.Max(X(u - DoorHalf), X(u + DoorHalf));
            spineCuts.Add((dx0, dx1));
            doors.Add(new SurfaceLayout.Doorway(dx0, Y(0), dx1, Y(0)));
        }

        // The board hangs half-way down the door wall and the plate reads a quarter of the way along it, so
        // neither crowds the other and both are things you meet on the way in rather than across the room.
        return new HallSite(
            new Hall(
                x0, y0, x1, y1, HallSeatsFor(bodyId, use), cabinets,
                X(HallDoorAisleDu / 2.0), Y(length / 2.0),
                X(HallDoorAisleDu / 2.0), Y(length * 0.25),
                HallArtFor(bodyId, use), spots, desk, doors, freight, service),

            // #791 · THE FIXTURE'S OWN SPOT is the MIDDLE OF THE DESK THAT SERVES, and not the middle of
            // the band. It used to be (uLo + uHi) / 2 — the mid-point of the counter's whole length
            // including the goods hoist's twelve du — so the one plate, the one console dot and the spot
            // ?counter=1 sets a tester down on all sat six du off centre, toward a freight shutter. The run
            // knows where its own middle is; nothing here works it out a second time.
            //
            // #827 · …and it is the run's own STANDING SQUARE, not the run's middle, because the run is now
            // the desk's front FACE — a wall. The fixture's spot has to be somewhere a body can be: every
            // walkability audit in the game asks whether a room's own console can be stood on and walked to,
            // and a plate on a wall is a plate the audits report as a sealed room. What moved onto the
            // counter is the RAIL the client draws and the [E] reach, which is what the owner was looking
            // at; where you stand to press it is the same square it has always been.
            service?.StandX ?? X((uLo + uHi) / 2.0),
            service?.StandY ?? Y(counterV - HallServiceStandoffDu),
            laid,
            glazed ? farWall : null);
    }
}
