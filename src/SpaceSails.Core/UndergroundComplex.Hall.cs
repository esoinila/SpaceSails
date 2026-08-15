using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>#751 · A carved hall, on its way to becoming an <see cref="Amenity"/>.</summary>
    /// <param name="Hall">The box and its cabinets, as published on the plan.</param>
    /// <param name="X">Where the fixture console stands — in front of the SERVING counter, on clear floor,
    /// at the middle of the run it answers over (#791).</param>
    /// <param name="Y">The same.</param>
    /// <param name="Tops">The round tops on the hall floor. Cabinet tops are NOT in here: a cabinet's chairs
    /// are extra, and the hall's own seat law is measured on this list.</param>
    /// <param name="Glass">#759 · The far wall, when it was built as glazing rather than as concrete —
    /// handed on so the park publishes the VERY segment the carve laid rather than a second one written from
    /// the same two corners. Two segments that are equal today and drawn from different arithmetic is the
    /// mirrored-constant bug with a one-line head start.</param>
    private readonly record struct HallSite(
        Hall Hall, double X, double Y, IReadOnlyList<(double X, double Y)> Tops,
        SurfaceLayout.Wall? Glass = null);

    // ── #751 · THE HALL'S OWN MODULE ─────────────────────────────────────────────────────────────────
    //
    // Nothing below is a size somebody liked the look of. Every number is either the facility's own module
    // (RoomWidthDu / RoomHeightDu), the doorway both this room and its corridor are cut to (DoorHalf), or a
    // clearance stated as what it is for.

    /// <summary>#751 · How many round tops the facility's own room module holds — three, which is what
    /// <see cref="Fitting"/> has put in a canteen since #707. It is the constant that turns a room's floor
    /// area into a table PITCH without anybody typing one.</summary>
    public const int HallTopsPerModule = 3;

    /// <summary>#751 · How far apart a hall's round tops stand, at the density the game's own canteens
    /// already use: one top per (module area ÷ <see cref="HallTopsPerModule"/>), squared back into a
    /// spacing — and then spread by <see cref="HallSpreadFactor"/>, because a hall is not a canteen with
    /// more chairs in it. A hall on tight ground packs closer than this; it never spreads wider.</summary>
    public static double HallTopPitchDu =>
        Math.Sqrt(RoomWidthDu * RoomHeightDu * HallSpreadFactor / HallTopsPerModule);

    /// <summary>
    /// #759 · HOW MUCH MORE FLOOR A HALL GIVES A TABLE THAN A ROOM DOES — the owner's <i>"the hall is like
    /// cramped … At least double or triple it"</i>, stated as the one number it actually is.
    ///
    /// <para>The first hall was laid at the ordinary canteen's density (<see cref="HallTopsPerModule"/>
    /// tops to a room module) and simply repeated it eighty seats' worth, which is how a room ends up
    /// twenty tables wide and still feeling like a corridor: the crowding a player feels is the PITCH, not
    /// the table count. Three times the floor per top is the whole of the fix, and everything downstream —
    /// how deep the carve asks the ground to be, where the counter's line falls, how far the pillars stand
    /// apart — follows from it without a second number being typed.</para>
    /// </summary>
    public const double HallSpreadFactor = 3.0;

    /// <summary>#751 · The clear strip inside the hall's doors. Nothing is laid in it — a doorway a captain
    /// has to path around a table to use is #585's stranded room with better furniture.</summary>
    public const double HallDoorAisleDu = 4.0;

    // ── #775 · CIRCULATION: PEOPLE-DOORS, SAFETY-DOORS, AND WHERE THE GOODS COME IN ──────────────────────
    //
    // Owner, walking the new B1 the night #790 landed, three complaints in one breath:
    //
    //   (1) "The bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to really look for the way
    //       in; a venue's entrance should find YOU."
    //   (2) "A canteen this size would have MORE THAN TWO DOORS just for safety reasons — egress is a code,
    //       and the base was built by people who file paperwork about codes."
    //   (3) "The facility needs FREIGHT ACCESS somewhere — a freight elevator or a long drive-in ramp for
    //       supplies; eighty seats of food and twelve beds of produce do not arrive through a personnel
    //       door."
    //
    // All three are one law said three times — THE LAYOUT MATCHES ITS FUNCTION — and all three are about
    // circulation rather than about rooms: people-doors where people come from, safety-doors because rules,
    // freight where freight goes.
    //
    // WHY THE ROOM HAD NO FRONT DOOR IN THE FIRST PLACE, because it is not an oversight anybody could have
    // seen from the plan. The hall's near wall IS the spine's own face (#751: "the front wall and the near
    // wall are not built at all — they are the rib's face and the spine's"), and the spine's faces were
    // poured BEFORE anybody knew where the hall was going to stand. So the one wall a walker on the main
    // corridor actually meets was the one wall the hall was structurally incapable of cutting. The carve
    // hands the wall builder a list of spans now, and the wall builder runs after it.

    /// <summary>#775 · HOW MUCH FLOOR ONE REQUIRED EGRESS DOOR COVERS. The one number the code-mandated
    /// door count is derived from, so nothing anywhere types how many doors a hall has.
    ///
    /// <para>Not a guess about du: the shipped halls run 2 100 – 7 300 du² and a du is about a metre of the
    /// deck the captain walks, which puts the biggest of them at the floor area of a real assembly hall.
    /// One way out per fifteen hundred of those is a conservative reading of every occupancy code anybody
    /// ever filed, and the people who built this place filed all of them.</para></summary>
    public const double HallDu2PerEgressDoor = 1500.0;

    /// <summary>#775 · THE FLOOR UNDER THE EGRESS COUNT — the owner's "more than two doors" as an integer.
    /// A room with two ways out has one way out the day one of them is where the fire is.</summary>
    public const int HallMinDoors = 3;

    /// <summary>#775 · HOW MANY DOORS A HALL OF THIS FLOOR AREA MUST HAVE. Published, and asked by BOTH the
    /// carve and the guard — a second opinion about a door count is the mirrored-constant bug with a whole
    /// building to be wrong in.</summary>
    public static int HallEgressDoors(double floorDu2) => Math.Max(
        HallMinDoors, (int)Math.Ceiling(floorDu2 / HallDu2PerEgressDoor));

    /// <summary>#775 · THE FRONT DOOR GOES WHERE THE WALKER IS, and this is how near it may be allowed to
    /// get to the corners of the room it is cut into: a door's own half-width, and a little.</summary>
    public const double HallSpineDoorEdgeDu = DoorHalf + 2.0;

    /// <summary>#775 · How wide the goods hoist's car is. Twelve du by the counter band's own five — the
    /// footprint of something a pallet goes into, parked in the one part of the room the customer never
    /// stands in.</summary>
    public const double FreightCarWidthDu = 12.0;

    /// <summary>
    /// #775 · THE GOODS HOIST — freight access, on the deck, drawn and collidable, and shut.
    ///
    /// <para>Owner: <i>"eighty seats of food and twelve beds of produce do not arrive through a personnel
    /// door."</i> It is parked in the counter's own service band — the band #751 closed off because it is
    /// the one part of a bar the customer never stands in — at the end of that band nearest the park's
    /// gate, which is the end the produce comes in by. Behind it, through the glass, are the beds it exists
    /// to carry; in front of it is the hall floor it feeds. One fixture, both jobs, no sentence needed.</para>
    ///
    /// <para><b>The captain cannot ride it, and is TOLD so.</b> The shutter is a
    /// <see cref="LockedDoor"/> — the building's own grammar for a door that will not open: drawn shut, a
    /// wall poured behind it, and its plate is exactly what [E] reads. Nothing here simulates freight and
    /// nothing pretends to; the fixture exists, it is labelled, a body stops at it, and the refusal is a
    /// sentence rather than an absence (#757's lesson about what a player cannot read).</para>
    /// </summary>
    /// <param name="X0">The car's box, min/max normalised where it is carved.</param>
    /// <param name="Shutter">The roller door in the counter's line, hall side.</param>
    /// <param name="PlateX">Where the plate is read from — on the HALL floor, in front of the shutter.</param>
    public readonly record struct FreightLift(
        double X0, double Y0, double X1, double Y1,
        SurfaceLayout.Doorway Shutter, double PlateX, double PlateY, string Plate)
    {
        /// <summary>Is this spot inside the car? The box the walls were laid on, and nothing else — the
        /// hall's own law (<see cref="Hall.Contains"/>).</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>#775 · What is stencilled on the goods hoist's shutter, and therefore what the captain is
    /// told when they press it. The bureaucracy's own register: a number, a window, and whose side of the
    /// shutter you are on — no explanation, no apology, and not one word about the building.
    ///
    /// <para>It carries no glyph of its own because the sign console already hangs the 🔒 on it, which is
    /// the building's grammar for a door that will not open (#585). Two glyphs on one plate is a plate
    /// nobody reads.</para></summary>
    public const string FreightPlate =
        "GOODS HOIST 1 · DELIVERIES 04:00–06:00 · CREW SIDE ONLY";

    /// <summary>#775 · What is painted on the floor in front of it, so the fixture reads as a fixture from
    /// across the room rather than as one more shut door in a building full of them.</summary>
    public const string FreightSign = "🚛 GOODS HOIST 1";

    /// <summary>#775 · What is painted beside the hall's main front door, the one nearest the lift. The
    /// venue announcing ITSELF on the corridor a walker is already on — the owner's <i>"a venue's entrance
    /// should find YOU"</i>, which a plate reading only ENTRANCE would not do: a door labelled "the way in"
    /// says nothing about what it is the way in TO.
    ///
    /// <para>Composed from the room's OWN sign rather than spelled a second time, and cut at its first
    /// separator: <see cref="AmenitySigns"/> writes the venue, then who it is for, then the pass rule, and
    /// only the first of those three belongs on a corridor at walking pace. So the bar's front door says
    /// <c>🍸 CANTEEN 1 · ENTRANCE</c> and the staff mess's says <c>🍽 CANTEEN 2 · ENTRANCE</c>, and the day
    /// either room is renamed both plates follow without anybody remembering this one exists.</para></summary>
    public static string HallEntrancePlate(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string plate = AmenitySigns(bodyId, use).Plate;
        int cut = plate.IndexOf(" · ", StringComparison.Ordinal);
        return $"{(cut < 0 ? plate : plate[..cut])} · ENTRANCE";
    }

    /// <summary>#775 · What is painted beside the hall's OTHER front doors. They exist because a code says
    /// a room this size has them, so they say what a code-mandated door says and nothing else — and the
    /// number is the one thing that makes a corridor of them read as a building rather than as a wall with
    /// holes in it.</summary>
    public static string HallEgressPlate(int number) =>
        $"⇥ EXIT {number} · KEEP CLEAR";

    /// <summary>#751 · How deep the cabinets run off the hall's outer wall.</summary>
    public const double HallCabinetDepthDu = 10.0;

    /// <summary>#751 · The band at the hall's far wall that THE COUNTER and its service side own — the one
    /// part of a bar the customer never stands in, closed off exactly the way it would be (#707).</summary>
    public const double HallCounterBandDu = 5.0;

    /// <summary>#751 · How much of a hall's own edge is left clear of furniture.</summary>
    public const double HallEdgePadDu = 2.0;

    /// <summary>
    /// #792/#827 · How far out from the DESK'S FRONT FACE a stool's centre is bolted down: <b>a body's
    /// radius</b>, so the person sitting on it is touching the counter.
    ///
    /// <para>Owner, evening playtest 2026-08-11: <i>"Now the blue seats are like without the table that the
    /// counter always provides."</i> The row stood 1.6 du off the counter's line — a stool's depth plus a
    /// pair of knees, which is a real quantity and the wrong one to lay a SEAT MARKER at. On a plan the seat
    /// is a dot for the body, not a box for the furniture, and a dot a full step clear of the desk reads as
    /// a chair in open floor. A body's radius puts the dot's edge on the desk's edge, which is what
    /// "bellying up to the bar" looks like from above.</para>
    ///
    /// <para><b>Three quarters of a body's width</b>: the radius, plus a quarter more. The extra quarter is
    /// not a taste — <c>SurfaceCollision</c> stops a body that is TOUCHING a wall (<c>distance &lt;
    /// radius</c>), so a seat laid at exactly a radius off the counter is a seat #820's snap cannot put the
    /// captain on. Watched go red at <c>the seat at (18.0,-202.8) is inside something solid</c>, eight seats
    /// a hall, on every hall in the game.</para>
    ///
    /// <para>Off <see cref="SurfaceScale.CaptainWidthDu"/> and never a literal — it is the captain's own
    /// body, the same one the collision measures, so the day the avatar changes size the row follows. It
    /// stays well inside <see cref="HallCounterBandDu"/>'s clearance, so a seat still cannot land on a
    /// top.</para>
    /// </summary>
    public static double HallStoolStandoffDu => SurfaceScale.CaptainWidthDu * 0.75;

    /// <summary>
    /// #827 · HOW MANY GAPS ARE LEFT IN THE ROW OF STOOLS — the standing service points a customer walks UP
    /// to, rather than sits at.
    ///
    /// <para>Owner, completing the counter model: <i>"there are gaps for people to walk to the cashier
    /// etc."</i> Two: the till (<see cref="CounterPost.Till"/>) and the collection end
    /// (<see cref="CounterPost.Collection"/>). They are places in the same published row as the seats, so
    /// the renderer, the collision and the service verbs read one list and none of them can invent a row of
    /// its own — which is exactly how the seats and the [E] rail came to disagree in the first place.</para>
    /// </summary>
    public const int HallCounterGaps = 2;

    /// <summary>
    /// #791 · How far out from the desk's line A SERVED CUSTOMER STANDS — the line the service run is laid
    /// along, and therefore the line the [E] bus answers from.
    ///
    /// <para>It is a hair further out than <see cref="HallStoolStandoffDu"/> on purpose: the stools are
    /// bolted between it and the desk, so the standing customer is behind the row rather than inside it,
    /// which is where a person stands at a bar with stools at it.</para>
    ///
    /// <para>Named rather than left as the <see cref="HallEdgePadDu"/> it used to borrow. The two happen to
    /// be the same number today and they are not the same QUANTITY — one is how much of a room's edge is
    /// kept clear of furniture, the other is how far a body stands off a counter — and a mirrored constant
    /// is the second-named bug class in this file's own table.</para>
    /// </summary>
    public const double HallServiceStandoffDu = 2.0;

    /// <summary>
    /// #751 · WHICH COLUMN THE HALL STANDS ON — the same criterion #707 already uses for the canteen, asked
    /// one step earlier.
    ///
    /// <para>"Nearest the car, every time": a building puts its catering by the lift, and a bar you have to
    /// go looking for is not a bar anybody drank in on a shift. The old carve chose the nearest ROOM out of
    /// the rooms the floor had built; a hall has to be chosen before any room exists, so this asks the same
    /// question of the room SLOTS — the very positions <see cref="RoomCentresAlong"/> is about to place. Same
    /// answer, computed from the same arithmetic, one pass earlier.</para>
    ///
    /// <para>#759 · …<b>of the slots that can actually hold one.</b> Nearest-the-car on its own put every
    /// hall in the game on the rib beside the shaft, and on the floors whose rib runs UP that is the one
    /// column in the building with the lift alcove standing in front of it — so the room the owner called
    /// cramped was cramped by a fixture thirty du away, and no amount of spreading its tables could have
    /// answered him. The ground is asked FIRST now (<see cref="HallGround"/>, the same arithmetic the carve
    /// itself uses, so the two can never disagree), the best floor wins, and nearest-the-car breaks the tie
    /// — which it does on every floor where two slots would both take the hall whole, i.e. the rule is
    /// unchanged everywhere it was ever doing any work.</para>
    /// </summary>
    private static (int Rib, int Side)? HallSlotFor(
        string bodyId, int level, List<Rib> ribs, in SurfaceLayout.Field field,
        double shaftX, double? serviceX, double shaftY, double leftEnd, double rightEnd, double roomScale)
    {
        if (!IsHallFloor(bodyId, level) || ribs.Count == 0)
        {
            return null;
        }

        Comfort use = HallUseOn(bodyId, level);
        double roomW = RoomWidthDu * roomScale;
        (int Rib, int Side)? best = null;
        double bestFloor = -1, bestD2 = double.MaxValue;

        for (int i = 0; i < ribs.Count; i++)
        {
            (double mouth, double far) = RibReach(field, shaftY, ribs[i].Down, hall: true);
            List<double> ys = RoomCentresAlong(mouth, far, ribs[i].Down, roomScale);
            if (ys.Count == 0)
            {
                continue;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                if (HallGround(
                        bodyId, use, ribs, i, side, mouth, far, shaftX, serviceX, leftEnd, rightEnd,
                        roomScale)
                    is not { } ground)
                {
                    continue;   // the ground here would not take a hall at all
                }

                // The floor this slot would yield, and never more than the hall ASKED for — so two slots
                // that both take it whole are equal and the tie falls to the lift, which is the #751 rule.
                double floor = Math.Min(ground.Width, ground.Wanted) * ground.Length;

                double cx = ribs[i].X + (side * (CorridorHalf + (roomW / 2)));
                double d2 = double.MaxValue;
                foreach (double cy in ys)
                {
                    double dx = cx - shaftX, dy = cy - shaftY;
                    d2 = Math.Min(d2, (dx * dx) + (dy * dy));
                }

                if (floor > bestFloor + 0.5 || (floor > bestFloor - 0.5 && d2 < bestD2))
                {
                    (best, bestFloor, bestD2) = ((i, side), Math.Max(floor, bestFloor), d2);
                }
            }
        }

        return best;
    }

    /// <summary>#751 · Does this floor get a hall at all? The two customers of the carve, asked in one
    /// place: the floor the bar is on, and the floor the mess is on.</summary>
    public static bool IsHallFloor(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return TopPressurisedFloor(bodyId) == level || StaffCanteenFloor(bodyId) == level;
    }

    /// <summary>#751 · Which hall this floor's is. The mess wins where a site is shallow enough for the two
    /// to land on the same floor — but <see cref="StaffCanteenFloor"/> returns null in exactly that case, so
    /// this is belt and braces rather than a rule.</summary>
    private static Comfort HallUseOn(string bodyId, int level) =>
        TopPressurisedFloor(bodyId) == level ? Comfort.UpperCanteen : Comfort.StaffCanteen;

    /// <summary>
    /// #759 · HOW MUCH GROUND A SLOT HAS FOR A HALL, AND HOW MUCH THE HALL WANTS — the arithmetic
    /// <see cref="CarveHall"/> used to do inline, lifted out whole so <see cref="HallSlotFor"/> can ask the
    /// same question one pass earlier.
    ///
    /// <para>It is lifted rather than copied for the reason this file opens with a table of: a chooser that
    /// worked out available width on its own would be a second opinion about a room the carve owns, and the
    /// two would drift apart the first time either grew a clause. Null means this ground will not take a
    /// hall at all.</para>
    /// </summary>
    private static (double Width, double Wanted, double Length, double VSpan)? HallGround(
        string bodyId, Comfort use, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double? serviceX, double leftEnd, double rightEnd,
        double roomScale)
    {
        double ribX = ribs[ribIndex].X;
        double roomW = RoomWidthDu * roomScale;

        // ── HOW FAR OUT THE GROUND GOES. Clamped against the things that are already spoken for rather
        //    than against a guess: the next rib's chambers, the lift alcove where the hall shares a spine
        //    face with it, and the spine's own end cap.
        //
        // #759 · …and a rib pointing the OTHER WAY off the spine is not in the way of anything. This used
        // to reserve a full room column beside EVERY neighbouring rib, which on half the floors in the game
        // was ground held back for chambers standing on the far side of the spine — sixty du of rock the
        // hall was refused because of rooms it could not have reached with a drill. The clamp asks which
        // way the neighbour runs now, and against a neighbour that shares this band it stops at the
        // CORRIDOR rather than at the far side of that corridor's rooms: those room slots are ground, the
        // claim ledger drops what stands on the hall, and a passage is the one thing that may never be
        // covered.
        double limit = side > 0 ? rightEnd - HallEdgePadDu : leftEnd + HallEdgePadDu;
        foreach (Rib other in ribs)
        {
            if (other.Down != ribs[ribIndex].Down)
            {
                continue;
            }
            if (side > 0 && other.X > ribX)
            {
                limit = Math.Min(limit, other.X - CorridorHalf - 1.5);
            }
            else if (side < 0 && other.X < ribX)
            {
                limit = Math.Max(limit, other.X + CorridorHalf + 1.5);
            }
        }
        // The lift alcove hangs off the TOP face, so only a rib that runs UP can meet it. #585 was a wall
        // lying across a mouth; a hall laid over the alcove would be the same mistake with the captain's own
        // way home inside it.
        //
        // #759 · …and only where the alcove is actually in the way, which is the clause this shipped
        // without. A hall growing LEFT off a rib that is already left of the shaft was being clamped to a
        // limit on the shaft's far side — a lower bound raised above the wall it was bounding, so
        // `available` came out as the distance to a point behind the hall and the room was laid seventy du
        // outside the field. It never fired while every hall in the game stood on the rib beside the car;
        // the moment #759 let the chooser look at the other seven slots, it laid a bar through the edge of
        // the world. A clamp that does not ask which side its obstacle is on is not a clamp.
        if (!ribs[ribIndex].Down)
        {
            if (side > 0 && shaftX > ribX)
            {
                limit = Math.Min(limit, shaftX - ShaftHalf - 1.5);
            }
            else if (side < 0 && shaftX < ribX)
            {
                limit = Math.Max(limit, shaftX + ShaftHalf + 1.5);
            }
        }

        // #801 · …and the GOODS CAR's alcove, which hangs off the LOWER face, so it is the ribs running DOWN
        // that can meet it. The same two lines the other way up — stated as its own clause rather than as a
        // loop over a list of cars, because the clause a car needs is WHICH FACE it is on, and a list that
        // had lost that would be a clamp that does not ask which side its obstacle is on.
        if (ribs[ribIndex].Down && serviceX is { } carX)
        {
            if (side > 0 && carX > ribX)
            {
                limit = Math.Min(limit, carX - ShaftHalf - 1.5);
            }
            else if (side < 0 && carX < ribX)
            {
                limit = Math.Max(limit, carX + ShaftHalf + 1.5);
            }
        }

        double faceX = ribX + (side * CorridorHalf);
        double available = Math.Abs(limit - faceX);
        double length = Math.Abs(far - mouth);

        // ── WHAT THE HALL NEEDS. The tops come first: the seat target decides the bill, the bill decides
        //    how many tops, and the tops decide how deep the room has to be at the pitch a hall lays its
        //    tables out at (HallSpreadFactor — the owner's "at least double or triple it").
        // …and the cabinets are the cantina's alone. The mess is the room the shift stopped coming to; a
        // door for sensitive negotiations in it would be furnishing a joke nobody is in the room to make.
        double cabBand = use == Comfort.UpperCanteen ? HallCabinetDepthDu : 0.0;

        int tops = HallSeatBill(bodyId, use, HallSeatsFor(bodyId, use)).Count;
        double pitch = HallTopPitchDu;
        double vSpan = length - (2 * HallEdgePadDu) - HallCounterBandDu - HallEdgePadDu;
        int rowsAtPitch = Math.Max(1, (int)(vSpan / pitch));
        int colsNeeded = (tops + rowsAtPitch - 1) / rowsAtPitch;
        double wanted = HallDoorAisleDu + (colsNeeded * pitch) + HallEdgePadDu + cabBand;

        double width = Math.Min(available, wanted);

        // A hall that cannot hold its own doorways, its aisle and a table strip is not a hall. Saying so
        // and standing down is the honest answer; a guard asserts it never actually happens.
        double minWidth = HallDoorAisleDu + cabBand + HallEdgePadDu + (2 * DoorHalf);
        return width < minWidth || vSpan < 4 * DoorHalf ? null : (available, wanted, length, vSpan);
    }

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

    /// <summary>
    /// #827 · THE COUNTER, LAID OUT ONCE — the box, its customer face, and the row of seats and gaps along
    /// that face, all out of the hall's own (u, v) and all in one place.
    ///
    /// <para>This exists because the bar used to be built four times: a collidable wall on the counter's
    /// line, a photograph over the band behind it, a row of stools 1.6 du in front of the line and an [E]
    /// run 2.0 du in front of it. Every one of those was correct arithmetic and they landed the counter at
    /// three different heights on the deck, which the owner walked into and read as <i>"the blue seats are
    /// like without the table that the counter always provides."</i> One carve, four readers, no drift.</para>
    ///
    /// <h3>The row, and why it has holes in it</h3>
    ///
    /// <para>Owner: <i>"the counter is the biggest table with customer seats only on one side … but not
    /// continuously … there are gaps for people to walk to the cashier etc."</i> So the face is cut into
    /// <c>TheStools.Count + <see cref="HallCounterGaps"/></c> even places and two of them are STANDING ones:
    /// the till a quarter of the way along — the first thing you meet after the door aisle, and where the
    /// keep stands on the other side — and the collection point at the far end, where what you ordered comes
    /// back over the desk. The seats keep their ordinals through the gaps: entry <c>s</c> of
    /// <see cref="CounterDesk.Stools"/> is still <c>Interior.TheStools</c>' stool <c>s</c>, which is what
    /// #820's snap and #792's occupancy both read the row by.</para>
    ///
    /// <para>Both the count and the two positions are DERIVED — a quarter of the row, and the end of it —
    /// so a bar that one day seats twelve keeps its cashier a quarter of the way along instead of at a
    /// literal index somebody typed when there were eight.</para>
    /// </summary>
    /// <param name="at">The hall's own (u, v) → field projection, handed in so this never has to know which
    /// way the rib points. <b>v</b> runs from the spine to the far wall, so the hall is at v below the
    /// face.</param>
    /// <param name="u0">Where the SERVING desk starts — past the goods hoist's divider (#775).</param>
    /// <param name="u1">Where it ends.</param>
    /// <param name="faceV">The counter's line: the customer edge of the band.</param>
    /// <param name="backV">The far wall behind it.</param>
    /// <param name="serves">Whether anybody is ever served over this desk. A counter that takes no orders
    /// publishes a box and an empty row, which is a true statement about the staff mess.</param>
    private static CounterDesk TheCounterDesk(
        Func<double, double, (double X, double Y)> at,
        double u0, double u1, double faceV, double backV, bool serves)
    {
        (double fx0, double fy0) = at(u0, faceV);
        (double fx1, double fy1) = at(u1, faceV);
        (double bx0, double by0) = at(u0, backV);
        (double bx1, double by1) = at(u1, backV);

        var places = new List<CounterPlace>();
        if (serves)
        {
            int count = Interior.TheStools.Count + HallCounterGaps;
            int till = Math.Clamp(count / 4, 0, count - 2);
            int collection = count - 1;
            int stool = 0;
            for (int i = 0; i < count; i++)
            {
                double u = u0 + ((u1 - u0) * ((i + 0.5) / count));
                CounterPost post =
                    i == till ? CounterPost.Till
                    : i == collection ? CounterPost.Collection
                    : CounterPost.Stool;

                // A SEAT is a body's radius off the face (elbows on the counter); a GAP is where a body
                // STANDS to be served, which is further out and is the same standoff #791 named. Two
                // quantities, both published, neither typed here.
                double standoff = post == CounterPost.Stool ? HallStoolStandoffDu : HallServiceStandoffDu;
                (double faceX, double faceY) = at(u, faceV);
                (double bodyX, double bodyY) = at(u, faceV - standoff);
                places.Add(new CounterPlace(
                    i, post, post == CounterPost.Stool ? stool++ : -1, faceX, faceY, bodyX, bodyY));
            }
        }

        return new CounterDesk(
            Math.Min(Math.Min(fx0, fx1), Math.Min(bx0, bx1)),
            Math.Min(Math.Min(fy0, fy1), Math.Min(by0, by1)),
            Math.Max(Math.Max(fx0, fx1), Math.Max(bx0, bx1)),
            Math.Max(Math.Max(fy0, fy1), Math.Max(by0, by1)),
            fx0, fy0, fx1, fy1,
            RingOffice.Seating.OneSide, places);
    }
}
