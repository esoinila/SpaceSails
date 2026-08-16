using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #707 · THE AMENITIES — SOMEBODY WORKED SHIFTS DOWN HERE ──────────────────────────────────────────
    //
    // Owner, the morning after walking a clinic: "all the secret labs dont have any cantina / bar nor any
    // toilets. We should add those like to the most top most pressurized floor. The toilets should have like
    // bathroom level equipments and the high level important rooms would have their built in bathrooms and
    // be pressurized."
    //
    // It is the cheapest storytelling left in this building and the most damning. Everything down here says
    // BUDGET — a lined shaft, poured walls, a lift on somebody's account decades after the last invoice —
    // and none of it says PEOPLE. A canteen and a wall of cubicles say people, in the only register this
    // ground is allowed to use: what somebody was made to pay for.
    //
    // THE TWO TIERS, which is the owner's ruling of 2026-08-05 and is a design and not a decoration:
    //
    //   1. THE UPPER CANTEEN, on the topmost floor that holds pressure. "publicly accessible and just
    //      happens to be in the secret base" — vendors drink here, normal credits work, and security is
    //      loose BY DESIGN. Classy, dangerous, and tight-lipped: there are strangers in the room and
    //      everybody knows it.
    //   2. THE STAFF CANTEEN, on the deepest floor the building ADMITS to that still holds pressure.
    //      Machines, no bottles, and a room where every face is known — so the talk is careless in exactly
    //      the room a stranger cannot stand in.
    //
    // And the upper one is the answer to a question the mechanics have been shipping since #590 without one.
    // Owner, closing the loop: "setting access to off the books secret lab to partners all trying to keep
    // things off records would be bureaucratic nightmare of office interorganization bureaucracy so the
    // underground bar just is there with access from surface. It kind of provides cover-story as well."
    // Band 0 has never wanted a card. Now it has a reason: credentialing every deniable partner across
    // organisations that all deny existing was never going to happen, so the first floor is simply OPEN, and
    // the bar is why anybody believes the shed on the surface is what it pretends to be. Access control
    // starts where the drinks stop. Nothing is built for that here — the plate carries it in four words and
    // the room prose carries the rest, and neither of them ever explains a thing.
    //
    // WHY THE AMENITIES ONLY EXIST ON FLOORS THAT BREATHE, which is the one rule the whole section turns on:
    // a canteen, a cubicle and an en-suite are all PLUMBING, and plumbing is for people out of their suits.
    // The owner already ruled the general form of it — "any room that would house like office work would be
    // pressurized by that constraint ... any kind of fine motor skill stuff" — and eating, washing and
    // signing things are the same constraint. So there is NO SECOND PRESSURE MAP here and there never will
    // be: HoldsPressure is asked, and where it says no, nothing is plumbed. A private washroom breathing on
    // a floor the plate by the lift is calling NO ATMOSPHERE would be two instruments disagreeing about air,
    // which is the one thing §13.13 says is worse than saying nothing.

    /// <summary>#707 · What an amenity room is FOR. Three, and the difference between the first and the
    /// third is the whole of the owner's inverted-economics ruling rather than a change of furniture.</summary>
    public enum Comfort
    {
        /// <summary>The bar on the top floor that holds pressure — the one room in the building outsiders
        /// are in, and the reason nobody at band 0 is ever asked for a card.</summary>
        UpperCanteen,
        /// <summary>Cubicles, a basin run and a mirror. Bathroom-grade, per the owner.</summary>
        Washroom,
        /// <summary>Machines and close tables, on the deepest floor the directory admits to that still
        /// breathes. Staff only, and the paperwork says it is somewhere else entirely.</summary>
        StaffCanteen,
    }

    /// <summary>One amenity room, taken out of the rooms the floor had already built — same discipline as
    /// <see cref="Refuge"/>, and for the same reason: a room is already audited walkable from the lift,
    /// already has a door, and already sits down a rib.</summary>
    /// <param name="Use">Which of the three it is.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Plate">What is stencilled beside the door.</param>
    /// <param name="Fixture">What the thing in the middle of the room is called, at console size.</param>
    /// <param name="Tables">Round tops on the floor, drawn in the game's existing table idiom. Empty in a
    /// washroom, which is the one amenity nobody sits down in.</param>
    /// <param name="Hall">#751 · The hall this amenity IS, when it is one — a room that left the standard
    /// grammar. Null for the ordinary three-top canteen and for every washroom.</param>
    public readonly record struct Amenity(
        Comfort Use, double X, double Y, string Plate, string Fixture,
        IReadOnlyList<(double X, double Y)> Tables,
        Hall? Hall = null)
    {
        /// <summary>#725 · Is the captain standing in this room? <see cref="RefugeHolds"/>, because an
        /// amenity is one of the floor's own rooms taken over — the same poured box, with the same square
        /// corners — and a second containment box written here would be a room whose walls the sim and the
        /// picture disagreed about. One law, asked in one place, exactly as the refuge does it.
        ///
        /// <para>#751 · …unless it is a HALL, in which case the box is the hall's own — carved, published,
        /// and the very same rectangle the walls were laid on. A hall is thirty times the floor area of the
        /// module, so a refuge-sized containment box would have said "you are not in the canteen" from
        /// almost everywhere inside the canteen.</para></summary>
        public bool Contains(double x, double y) => Hall is { } hall
            ? hall.Contains(x, y)
            : RefugeHolds(X, Y, x, y);
    }

    // ── #751 · THE HALL RULE — WHEN AN AMENITY STOPS BEING A ROOM ─────────────────────────────────────
    //
    // Owner, 2026-08-06: "The Canteen is way too small… It needs to house like 80 customers… I am thinking
    // like Mos Eisley Space port size bar." And, an hour later, the second customer: "The canteen for only
    // staff can also be a lot bigger ... usually people eat lunch at same time so the whole staff using it
    // should about fit in."
    //
    // TWO ROOMS, ONE CARVE. They are opposite rooms in every way that matters — one heaves with strangers on
    // a day watch, the other has been empty since before the captain was born — and they are the SAME
    // geometry problem: a seat count that the standard 15 x 12 module cannot hold. So there is exactly one
    // hall carver (CarveHall), it takes a SEAT TARGET, and the two customers differ only in what number they
    // hand it. A second copy of this for the mess is the shape of bug the table at the top of this file is
    // a list of.
    //
    // WHY IT IS A RIB'S ROOM COLUMN AND NOT A BOX DROPPED ON THE FLOOR PLAN. #585's law is that the doorway
    // a room cuts and the gap its corridor leaves are ONE gap, computed once. A hall drawn as its own
    // rectangle would have had to cut its own doors — a second answer to a question RibFace already owns,
    // and the disease that sealed every room in the building for a day. Instead the hall simply IS the
    // ground the rib's room column stood on: its front wall is the rib's own face, already built, already
    // cut with a doorway at every room slot the corridor has. The hall has two doors and the corridor has
    // two gaps and they are the same two openings, because nothing ever made a second set.
    //
    // WHAT IT COSTS THE FLOOR, STATED. A hall eats the two room slots of one column, and the claim ledger
    // is told about the box before any other placer runs, so nothing is ever laid on top of it and no room
    // is silently dropped. The floor loses two rooms and gains a hall; that is the trade, it is stated
    // here, and TheCantinaHallTests measures it rather than trusting this paragraph.

    /// <summary>#751 · One enclosed side room off a hall — <b>CABINET · BY ARRANGEMENT</b>.
    ///
    /// <para>Owner: <i>"Definitely want to make the B1 bar be fancy ... and have cabinet-spaces for
    /// sensitive negotiations."</i> Six chairs, one door, and no line of sight to the counter — and that
    /// last clause is the whole mechanic, because #746's file-on-the-table is LOUD precisely because
    /// <i>"the counter has eyes"</i>. A room the counter cannot see is a room where it does not.</para>
    ///
    /// <para>Empty of people in v1. They are geometry plus a rule; #731's walkers will put somebody in
    /// one.</para></summary>
    /// <param name="Number">1-based, as the plate reads.</param>
    /// <param name="X">Centre.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="HalfW">Half-width of the enclosed box.</param>
    /// <param name="HalfH">Half-height.</param>
    /// <param name="Table">The one round top in it, at its own centre.</param>
    public readonly record struct Cabinet(
        int Number, double X, double Y, double HalfW, double HalfH, (double X, double Y) Table,
        // #822 · The gaps in its own face. Appended, so every caller that builds one positionally still
        // means the same booth.
        IReadOnlyList<SurfaceLayout.Doorway>? Leaves = null)
    {
        /// <summary>#822 · Every way out of it, never null. A cabinet is a room with a door in one wall and
        /// three solid ones, so this is the whole of its egress and the fire code reads it directly.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Ways => Leaves ?? [];

        /// <summary>Is the captain inside this cabinet? The box the walls were laid on, and nothing
        /// else.</summary>
        public bool Contains(double x, double y) =>
            Math.Abs(x - X) <= HalfW && Math.Abs(y - Y) <= HalfH;

        /// <summary>What is stencilled beside its door.</summary>
        public string Plate => CabinetPlate(Number);
    }

    /// <summary>
    /// #827 · WHAT ONE PLACE AT THE COUNTER IS FOR. The row is not all seats: a counter has gaps in it, and
    /// the gaps are where the standing business of a bar happens.
    ///
    /// <para>Owner, completing the model: <i>"In a sense the counter is the biggest table with customer
    /// seats only on one side … but not continuously … there are gaps for people to walk to the cashier
    /// etc."</i> So the face publishes ONE list — stool, stool, gap, stool… — and a gap is a fact about the
    /// counter rather than an absence somebody has to infer from the spacing of the seats.</para>
    /// </summary>
    public enum CounterPost
    {
        /// <summary>A tall seat, bolted down. The customer sits, the case does not come out of the sleeve
        /// (the SeatedSpread bar rule), and the drink is poured at the elbow.</summary>
        Stool,

        /// <summary>The gap you pay at, a quarter of the way along — near enough the way in to be the first
        /// thing you meet, far enough along that the door does not queue into the aisle. The keep stands on
        /// the other side of this one.</summary>
        Till,

        /// <summary>The gap at the far end, where what you ordered comes back over the desk. A counter with
        /// no collection point is a counter where the tray has to be put down on somebody's elbows.</summary>
        Collection,
    }

    /// <summary>#827 · One place along the counter's front face — a seat or a standing gap, in the row's own
    /// order.</summary>
    /// <param name="Index">Its ordinal along the face, gaps included. The row reads in this order and the
    /// renderer, the collision and the verbs all walk it in this order.</param>
    /// <param name="Post">What it is for. See <see cref="CounterPost"/>.</param>
    /// <param name="Stool">Which of <c>Interior.TheStools</c> this seat is, or <c>-1</c> at a gap — so the
    /// seat a captain is told is free and the seat drawn free are the same piece of furniture, which is the
    /// law <see cref="Hall.StoolRow"/> has carried since #792 and #820's snap now sits a body on.</param>
    /// <param name="FaceX">Where this place MEETS THE DESK — a point on <see cref="CounterDesk.Face"/>
    /// itself, and the reason the row can never drift off the counter again: it is the face's own
    /// coordinate, not a second opinion about where the bar is.</param>
    /// <param name="FaceY">The same.</param>
    /// <param name="X">Where the BODY goes: on the stool (a body's radius off the face, so the person on it
    /// has their elbows on the desk), or standing in the gap
    /// (<see cref="HallServiceStandoffDu"/> out, which is where a person stands at a counter they are not
    /// sitting at).</param>
    /// <param name="Y">The same.</param>
    public readonly record struct CounterPlace(
        int Index, CounterPost Post, int Stool, double FaceX, double FaceY, double X, double Y)
    {
        /// <summary>Is somebody sat down here, or stood up at it?</summary>
        public bool Seated => Post == CounterPost.Stool;

        /// <summary>How far out from the desk this place puts a body. The one number a guard needs to say
        /// "the row is ON the counter" without knowing which way this hall's rib points.</summary>
        public double StandoffDu =>
            Math.Sqrt(((X - FaceX) * (X - FaceX)) + ((Y - FaceY) * (Y - FaceY)));

        /// <summary>What is painted on the floor at a GAP, so a hole in the row of stools reads as a place
        /// to stand rather than as a seat somebody unbolted. Empty at a stool: eight plates down a bar is
        /// #782's wall of noise, and the seats already say what they are by being seats.</summary>
        public string Plate => Post switch
        {
            CounterPost.Till => CounterTillPlate,
            CounterPost.Collection => CounterCollectionPlate,
            _ => "",
        };
    }

    /// <summary>
    /// #827 · THE COUNTER DESK — the box the bar IS, and the one fact everything about the bar reads.
    ///
    /// <para>Owner, evening playtest 2026-08-11, from stool 3: <i>"the counter should be at the counter
    /// position in the underlying picture … the yellow box seems like it is the counter … we should have the
    /// counter as something that cannot be walked through but can be used as a table."</i> The bar had three
    /// authors — the [E] service run, the row of stools and the desk's own photograph — laid at three
    /// different offsets off one line, so the deck drew the counter at two heights and the stools bellied up
    /// to a rail floating in open floor. This record is the fix: the RECT is carved once, its
    /// <see cref="Face"/> is handed to the wall, the run, the row and the picture, and nothing measures a
    /// bar it did not carve.</para>
    ///
    /// <para><b>It is the table family's limit case.</b> Owner: <i>"the counter is the biggest table with
    /// customer seats only on one side."</i> So it carries the same <see cref="RingOffice.Seating"/> field
    /// #834 gave the office fixtures, and it carries <see cref="RingOffice.Seating.OneSide"/> — the keep's
    /// side of a bar publishes no seats, ever, and that is a statement in the record rather than a habit of
    /// the loop that fills it.</para>
    /// </summary>
    /// <param name="X0">Left edge of the desk's box, in the surface's own coordinates. Already
    /// min/max normalised, so a rib that runs down the field and one that runs up it hand a reader the same
    /// rectangle.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="FaceX0">One end of the CUSTOMER FACE — the edge of the box the hall is on, which is the
    /// edge you lean on, order over and are served across. NOT normalised: it runs the desk's own way, so
    /// the row's order and the run's direction are one direction.</param>
    /// <param name="FaceY0">The same.</param>
    /// <param name="FaceX1">The other end of it.</param>
    /// <param name="FaceY1">The same.</param>
    /// <param name="Sides">Which sides carry seats. Always <see cref="RingOffice.Seating.OneSide"/> on a
    /// counter — see the summary.</param>
    /// <param name="Places">The row along the face, in order: seats and gaps together. Empty where the
    /// counter takes no orders (the staff mess, the head office's sideboard), which is a true statement
    /// about those rooms and not a missing one.</param>
    public readonly record struct CounterDesk(
        double X0, double Y0, double X1, double Y1,
        double FaceX0, double FaceY0, double FaceX1, double FaceY1,
        RingOffice.Seating Sides,
        IReadOnlyList<CounterPlace>? Places = null)
    {
        /// <summary>The row along the face, never null — a caller drawing a counter must not have to tell an
        /// empty row from a missing one.</summary>
        public IReadOnlyList<CounterPlace> Row => Places ?? [];

        /// <summary>The customer face as a wall segment: the very line the carve collides on and the very
        /// line the [E] run is laid along. One segment, four readers.</summary>
        public SurfaceLayout.Wall Face => new(FaceX0, FaceY0, FaceX1, FaceY1, true);

        /// <summary>How long the desk serves for.</summary>
        public double FaceLengthDu => Math.Sqrt(
            ((FaceX1 - FaceX0) * (FaceX1 - FaceX0)) + ((FaceY1 - FaceY0) * (FaceY1 - FaceY0)));

        /// <summary>How far a spot is from the face — <c>SurfaceCollision</c>'s own segment distance, the one
        /// every other reach in this game is measured with. The number a guard asks when it wants to know
        /// whether the row is ON the counter or a step in front of it.</summary>
        public double DistanceToFace(double x, double y) =>
            SurfaceCollision.DistanceToSegment(x, y, FaceX0, FaceY0, FaceX1, FaceY1);

        /// <summary>Is this spot inside the desk itself? The box the wall segments were laid on, and nothing
        /// else — what "you cannot walk through the counter" is asked of.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>The seats, in the counter's own order — entry <c>s</c> is
        /// <c>Interior.TheStools</c>' stool <c>s</c>. Derived from <see cref="Row"/> and never carved a
        /// second time, which is the whole point of publishing the row at all.</summary>
        public IReadOnlyList<(double X, double Y)> Stools
        {
            get
            {
                var seats = new List<(double X, double Y)>(Row.Count);
                foreach (CounterPlace p in Row)
                {
                    if (p.Seated)
                    {
                        seats.Add((p.X, p.Y));
                    }
                }
                return seats;
            }
        }

        /// <summary>Where the gap of this kind is, or null on a counter that publishes no row.</summary>
        public CounterPlace? Gap(CounterPost post)
        {
            foreach (CounterPlace p in Row)
            {
                if (p.Post == post)
                {
                    return p;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// #791 · THE DESK'S SERVICE RUN — the whole front of the bar, as one segment, plus the spot behind it
    /// the service comes FROM.
    ///
    /// <para>Owner, live playtest 2026-08-08: <i>"The Bar desk is really long now, but there is only one
    /// spot to get service on it… we should probably have service on the whole length indicated somehow, so
    /// we would need an E-bus of the bar desk length instead of one bar keep cashier at a single spot. Now
    /// the counter is in front of the desk, where the bar keep physically would be behind the bar desk
    /// instead of being in front of it."</i></para>
    ///
    /// <h3>Why the fixture stopped being a point</h3>
    ///
    /// <para>The counter has been a POINT since #707 — one console, one interact radius — and #751 then grew
    /// the desk to eighty-odd deck units. The picture, the collidable wall and the row of stools all ran the
    /// length of it; the one thing that answered [E] covered about a fourteenth of it, in the middle, and
    /// nothing on the floor said which fourteenth. That is the drawn room and the pressed room disagreeing,
    /// which is this project's third named bug class, at the fixture the owner spends the most time at.</para>
    ///
    /// <para>So the fixture publishes a RUN and the client's press reaches the nearest point on it. One
    /// fixture, one card, many approach points — the owner's own sentence, and it costs a segment.</para>
    ///
    /// <h3>The three bands, in order, and they are an order</h3>
    ///
    /// <para><b>hall floor · the run · the desk · the keep's side.</b> <see cref="X0"/>..<see cref="X1"/> is
    /// the DESK'S OWN FRONT FACE — <see cref="CounterDesk.Face"/>, handed over rather than measured — so the
    /// length that is lit, the length that answers [E] and the edge of the thing you are leaning on are one
    /// segment. <see cref="StandX"/>/<see cref="StandY"/> is the square a served customer stands on, one
    /// <see cref="HallServiceStandoffDu"/> out from that face on the hall side.
    /// <see cref="KeepX"/>/<see cref="KeepY"/> is BEHIND the desk, in the sealed band — the strip #751 closed
    /// off because it is the one part of a bar the customer never stands in, and it is opposite the
    /// <see cref="CounterPost.Till"/>, which is where a cashier stands. The counter's own collidable wall is
    /// between the two and stays the law: nothing here opens it, and a body may not cross it.</para>
    ///
    /// <para><b>#827 · The run used to float.</b> It was laid <see cref="HallServiceStandoffDu"/> out from
    /// the counter's line, on the square a customer stands on rather than on the desk — so the deck drew a
    /// cyan service rail labelled THE COUNTER two du clear of the yellow bar-desk photograph, with the stool
    /// markers stranded between them. Owner, from stool 3: <i>"the counter should be at the counter position
    /// in the underlying picture … the yellow box seems like it is the counter."</i> Three authors, one bar.
    /// The desk is the authority now and everything else reads it.</para>
    ///
    /// <para>It spans the SERVING desk only — from where the goods hoist's divider leaves off (#775) to the
    /// far end of the counter — because a bar you can order at across a freight shutter is not a bar. That
    /// is the same span the desk's own photograph is stretched over, so what is drawn and what serves are
    /// one length by construction rather than by two authors agreeing.</para>
    /// </summary>
    /// <param name="X0">One end of the run, ON the desk's front face.</param>
    /// <param name="Y0">The same.</param>
    /// <param name="X1">The other end.</param>
    /// <param name="Y1">The same.</param>
    /// <param name="KeepX">Where the service comes from — behind the desk, in the sealed band, opposite the
    /// till.</param>
    /// <param name="KeepY">The same.</param>
    /// <param name="StandX">#827 · Where a customer stands to be served — clear floor on the hall side of
    /// the face, at the middle of the desk, one <see cref="HallServiceStandoffDu"/> out. The run itself is
    /// ON a wall now, so a caller that wants a SQUARE rather than a segment asks for one instead of taking
    /// the middle of the rail and hoping. It is the fixture's own spot (<c>Amenity.X/Y</c>), which is the
    /// square every walkability audit in the game stands a body on and the square <c>?counter=1</c> sets a
    /// tester down on.</param>
    /// <param name="StandY">The same.</param>
    public readonly record struct ServiceRun(
        double X0, double Y0, double X1, double Y1, double KeepX, double KeepY,
        double StandX = 0, double StandY = 0)
    {
        /// <summary>The middle of the run — the fixture's own spot, where its one plate is read and where
        /// the console dot is drawn.</summary>
        public double MidX => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double MidY => (Y0 + Y1) / 2.0;

        /// <summary>Half the run, as a vector off <see cref="MidX"/>/<see cref="MidY"/>. This is the shape a
        /// client's interaction point wants: an anchor and a reach, so a fixture that is a point today is a
        /// run tomorrow without the press learning a new idea.</summary>
        public double HalfSpanX => (X1 - X0) / 2.0;

        /// <summary>The same.</summary>
        public double HalfSpanY => (Y1 - Y0) / 2.0;

        /// <summary>How long the desk serves for, in deck units. The number the guards measure "the whole
        /// length" against, so "a bus" can never quietly become "a slightly wider point".</summary>
        public double LengthDu => Math.Sqrt(((X1 - X0) * (X1 - X0)) + ((Y1 - Y0) * (Y1 - Y0)));

        /// <summary>How far this spot is from the desk's service line — <see cref="SurfaceCollision"/>'s own
        /// segment distance, the one every other reach in this game is measured with.</summary>
        public double DistanceTo(double x, double y) =>
            SurfaceCollision.DistanceToSegment(x, y, X0, Y0, X1, Y1);

        /// <summary>Is a captain standing here within <paramref name="reach"/> of being served? The whole of
        /// the E-bus, in one call, so the press and every guard ask the same question.</summary>
        public bool Serves(double x, double y, double reach) => DistanceTo(x, y) <= reach;
    }

    /// <summary>#751 · A hall: the box, what it seats, and the cabinets off it.</summary>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="SeatTarget">How many the hall was asked to seat — the owner's eighty for the cantina,
    /// <see cref="ImpliedComplement"/> for the mess. The bill of tops is derived from it and the guards
    /// measure the tops rather than reading this.</param>
    /// <param name="Cabinets">The enclosed side rooms. Empty on a mess — nobody negotiates anything in a
    /// room the shift stopped coming to.</param>
    /// <param name="BoardX">Where THE BOARD hangs — by the door, which is where a rota goes. Carried on the
    /// hall rather than computed from a fixed offset, because a hall's door is on whichever face the rib is
    /// on and a renderer guessing at that would be doing geometry about a room it does not own.</param>
    /// <param name="BoardY">The same.</param>
    /// <param name="PlateX">Where the room's own stencilled plate reads from — down the door wall, a
    /// quarter of the way along, clear of the board. Same reason as <paramref name="BoardX"/>.</param>
    /// <param name="PlateY">The same.</param>
    /// <param name="ArtUrl">#756 · The picture this floor WEARS — drawn under the vector overlay, stretched
    /// across the hall's own box, the same seam the ship's rooms have used since the 3D renovation (the
    /// CANTINA wears <c>art/the-space-bar.jpg</c>). Owner: <i>"let's put todo to have gen-AI Bar image on
    /// the background like we have in space ports."</i> Published HERE, beside the box it is stretched over,
    /// because a renderer choosing which picture goes on which floor would be a second opinion about a room
    /// it does not own — the discipline <paramref name="BoardX"/> and <paramref name="PlateX"/> already
    /// answer to. Null leaves the floor bare, which is every hall nobody has painted yet.</param>
    /// <param name="Spots">#780 · The hall's FURNITURE, painted over its own boxes — the counter, and
    /// whatever fixture the next issue carves. Drawn over <paramref name="ArtUrl"/> and under every vector
    /// mark, at <see cref="SpotArtAlpha"/>. Empty on a hall with nothing painted in it, which is not a
    /// missing feature: a room's ambience and a room's furniture are two different claims and a hall may
    /// make either without making the other.</param>
    /// <param name="Desk">#827 · THE COUNTER ITSELF — the box, its customer face, and the row of seats and
    /// gaps along that face. The ONE fact about where the bar is: the collidable wall, the [E] run, the
    /// stools and the desk's own photograph are all built from it, and a hall's counter cannot be in two
    /// places any more because there is only one place for it to be. Null on a floor with no counter at all.
    /// See <see cref="CounterDesk"/>.</param>
    /// <param name="Service">#791 · WHERE THIS DESK SERVES — the whole front of it as one segment, and the
    /// spot behind it the service comes from. Null on a hall whose counter takes no orders (the staff mess,
    /// the head office's sideboard), which is a true statement about those rooms: there is a counter in
    /// them, and nobody has ever been served over it. See <see cref="ServiceRun"/>.</param>
    public readonly record struct Hall(
        double X0, double Y0, double X1, double Y1, int SeatTarget, IReadOnlyList<Cabinet> Cabinets,
        double BoardX = 0, double BoardY = 0, double PlateX = 0, double PlateY = 0,
        string? ArtUrl = null, IReadOnlyList<SpotArt>? Spots = null,
        CounterDesk? Desk = null,
        IReadOnlyList<SurfaceLayout.Doorway>? Doors = null, FreightLift? Freight = null,
        ServiceRun? Service = null)
    {
        /// <summary>#780 · The furniture pictures, never null — a caller drawing a room must not have to
        /// ask whether "no spots" means an empty list or a missing one.</summary>
        public IReadOnlyList<SpotArt> Painted => Spots ?? [];

        /// <summary>#792 · The row of tall seats, never null — same reason as <see cref="Painted"/>. A hall
        /// whose counter does not serve has an empty row, which is a true statement about it and not a
        /// missing one.
        ///
        /// <para>#827 · Read off the DESK's own published row rather than carried beside it. It used to be
        /// a second list laid at its own standoff off the counter's line, which is how the seats came to
        /// belly up to a rail floating in open floor. One row, one authority, one place to move it
        /// from.</para></summary>
        public IReadOnlyList<(double X, double Y)> StoolRow => Desk?.Stools ?? [];

        /// <summary>#827 · The whole row along the counter's face — seats AND the standing gaps between
        /// them, in the counter's own order. Never null.</summary>
        public IReadOnlyList<CounterPlace> CounterRow => Desk?.Row ?? [];

        /// <summary>#775 · EVERY WAY IN AND OUT OF THIS ROOM, published rather than inferred — the same
        /// discipline #587 applied to the ribs, for the same reason: a law about how many doors a hall has
        /// cannot be written against a list nobody keeps.
        ///
        /// <para>It holds BOTH kinds and does not distinguish them, because egress does not: the gaps in
        /// the rib's own face (which the hall never cut — see the module's header) and the front doors cut
        /// into the spine's face for #775. They come off one carve, out of the two functions that already
        /// own those two walls, so a door here is a gap there by construction.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Openings => Doors ?? [];

        /// <summary>#775 · How many doors a room this size is REQUIRED to have — asked of its own published
        /// box, so a hall that grows a du grows its egress duty with it.</summary>
        public int EgressRequired => HallEgressDoors(FloorDu2);

        /// <summary>#775 · How much floor it has. The unit the egress law and the owner's "a canteen this
        /// size" are both stated in.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);

        /// <summary>Is the captain inside the hall? Cabinets are inside it, by construction.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>Which cabinet holds this spot, or null for the hall floor itself.</summary>
        public Cabinet? CabinetAt(double x, double y)
        {
            foreach (Cabinet c in Cabinets)
            {
                if (c.Contains(x, y))
                {
                    return c;
                }
            }
            return null;
        }
    }
}
