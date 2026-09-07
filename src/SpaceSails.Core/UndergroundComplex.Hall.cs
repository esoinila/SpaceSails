using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #751 · THE HALL'S DIMENSIONS — the module it is sized by, the circulation code it is built to, the
/// goods hoist, and the plates the doors wear. Everything in this file is a NUMBER or a NAME: nothing
/// here carves anything.
///
/// <para>#251 · This is the opening file of a four-part family, and the other three are named for the
/// step they own: <c>.Ground</c> (which column the hall stands on, and whether the ground there will take
/// one at all), <c>.Carve</c> (the hall built, in its own two axes), and <c>.Desk</c> (the counter, laid
/// out once). No member is renamed, re-scoped or re-ordered by the cut.</para>
///
/// <para>Every constant here is a <c>const</c> and the two derived sizes are PROPERTIES, so this family
/// declares no static field at all — which is the reason the cut is free of the #1163 hazard rather than
/// merely lucky. See <c>NoPartialClassSpreadsItsStaticFieldsTests</c>.</para>
/// </summary>
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
}
