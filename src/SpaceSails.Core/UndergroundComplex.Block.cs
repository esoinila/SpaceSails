using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #813 · THE MANHATTAN RULING — THE PARK IS THE MIDDLE OF THE BLOCK ────────────────────────────────
    //
    // Owner, 2026-08-09 evening: "The central park needs to be in the center of all the other rooms… not on
    // the side. Think of New York, is the park on one side or is it in the center?" And the clause that
    // decides every number below: "make sure the park prime real estate is not wasted and not unused, not on
    // any side. It is the best real estate."
    //
    // WHAT WAS WRONG WITH THE SHIPPED PARK. #759 bought its ground out of the strip beyond the ribs' far
    // ends — the one band no placer had ever used — so the green ran the whole width of the field with the
    // hall's glass on one long side and PAINTED ROCK on the other three. #801 put a row of doors in the far
    // wall, which was the owner noticing the same thing from inside: "walking through the park is fun, it
    // should not be the edge." A room with three dead sides is a room on the side of the map however big it
    // is, and three quarters of the best frontage in the building was frontage onto nothing.
    //
    // WHAT A BLOCK IS. The park is now the middle of a city block, and everything else here is the block:
    //
    //   ─────────────── THE SPINE ─────────────────   the block's own near street, with the cage on it
    //   │ suite │ G │ suite │   T H E   H A L L   │   NEAR RING · doors on the spine, glass on the park
    //   ├───────┴───┴───────┴─────────────────────┤
    //  W│                                         │E  WEST/EAST RING · doors on the street, glass on the
    //  e│            T H E   P A R K              │a  park; one gate through each
    //  s│                                         │s
    //  t├─────────────────────────────────────────┤t
    //   │ store │ G │ potting │ cold │ G │ office │   FAR RING · the back of house, now ring fabric:
    //   ─────────── THE BACK STREET ──────────────    a door on the street AND its old door on the gravel
    //
    // FOUR LAWS, and each of them is a guard in TheParkIsTheCentreOfTheBlockTests:
    //
    //   1. CENTRALITY. Every one of the park's four walls is faced by carved fabric. No side of it is the
    //      field's edge and none of it is rock.
    //   2. THE RING IS COMPLETE. Every du of the park's perimeter that is not a gate is a room's park-facing
    //      wall, and that wall is GLASS (published in FloorPlan.Windows, blocking like any other wall). The
    //      owner's "not unused, not on any side", in the one unit a guard can measure.
    //   3. A CORRIDOR ON EVERY SIDE. The near street is the spine itself; the far street is the back street;
    //      the west and east streets join them into one loop. Every ring room's door is on a street and
    //      never on the park, so nobody walks through an office to reach an office.
    //   4. THE CAR GOES WHERE THE BUILDING IS THINNEST. The ring's own frontage decides which end of the
    //      block the goods car stands at — the density decides the shaft's side and never the other way
    //      round (see ServiceShaftAt).
    //
    // WHICH SIDE OF THE SPINE. Always the lower one. The cage's alcove hangs off the spine's UPPER face, and
    // a block that took that side would have to be carved around the captain's own way home — a notch in the
    // best frontage in the building, on the one column nothing may ever be laid across (#585). The block
    // takes the side the cage does not, which is a reason and not a coin toss.

    /// <summary>#813 · How far the block's two service streets stand in from the spine's own end caps. What
    /// is left outside them is the rock the goods car's alcove is cut into, which is why this is a number
    /// and not the edge itself.</summary>
    public const double BlockStreetInsetDu = 14.0;

    /// <summary>#813 · How deep the ring is on the SPINE side — the premium band, and the one the hall
    /// stands in. It is the hall's own length (the old <c>RibReachDu + HallRibExtraDu</c> less the corridor
    /// half it was measured from), because the room that has to fit is the one with eighty seats in it: a
    /// band shallower than this would make the bar wider than the ground it stands on.</summary>
    public const double RingNearDepthDu = 51.5;

    /// <summary>#813 · How deep the ring is on the BACK STREET side. The facility's own chamber module
    /// exactly (<see cref="ParkBackDepthDu"/>) and not a number of its own — these are #801's back of house
    /// re-anchored, and #801's law is that they are ordinary rooms that happen to be entered off a garden.
    /// A ring band a du deeper than the module would have made that sentence false by one du, which is how
    /// a law quietly stops being one.</summary>
    public static double RingFarDepthDu => ParkBackDepthDu;

    /// <summary>#813 · How deep the ring is on the two ends of the block. Deeper than a chamber and far
    /// shallower than the near band: a corner office with the green out of one wall.</summary>
    public const double RingSideDepthDu = 20.0;

    /// <summary>#813 · How wide a ring room WANTS to be. A band is cut into
    /// <c>max(1, round(span / this))</c> rooms of equal width, so the pier between two suites is their
    /// shared wall and no frontage is ever left over — the owner's "not unused" said as arithmetic rather
    /// than as an intention.</summary>
    public const double RingRoomTargetDu = 40.0;

    /// <summary>#813 · The narrowest a ring room may be. Also the clearance a gate must leave at each end of
    /// a band: a corridor cut so close to the corner that the room beside it is a cupboard is a corridor
    /// that has eaten the frontage it was there to serve.</summary>
    public const double RingRoomMinDu = 16.0;

    /// <summary>
    /// #817 · HOW MUCH STREET FACE ONE DOOR SERVES.
    ///
    /// <para>Owner, live in a 40 du landscape office with one leaf in it: <i>"Oh just one door in a landscape
    /// office?"</i> and, the same evening, <i>"bigger spaces must have much more doors."</i> That overrode
    /// <see cref="RingRoom.Door"/>'s documented "exactly one", and this is the number the override is stated
    /// as: a room's street frontage divided by this, rounded, is how many leaves it gets.</para>
    ///
    /// <para>Eighteen du is the precedent already standing in the building rather than a figure somebody
    /// liked — the hall's own corridor face carries four to five doors over its run (#775/#812), which is
    /// this ratio to within a leaf. It is also comfortably more than twice
    /// <see cref="DoorHalf"/>, so two doors on the narrowest frontage the ring will ever cut
    /// (<see cref="RingRoomMinDu"/>) still leave a pier of wall between them.</para>
    /// </summary>
    public const double RingStreetFaceDuPerDoor = 18.0;

    /// <summary>
    /// #822 · THE FIRE CODE — no space may have only one way out, except the bedroom-small ones.
    ///
    /// <para>Owner's standing law, issued mid-build: <i>"no space may have only one door except bedroom-small
    /// rooms."</i> Every ring room takes at least this many exits, counting its street doors and its gate
    /// onto the green, however little frontage it has. The exemption is
    /// <see cref="FireCodeSmallRoomDu"/>.</para>
    /// </summary>
    public const int FireCodeMinExits = 2;

    /// <summary>
    /// #822 · How big a room may be and still be let off with one door. A space whose LONGEST side is no
    /// longer than this is bedroom-small: a WC cubicle, a privacy booth, a cell you can cross in two paces
    /// and whose one door you are always within reach of.
    ///
    /// <para>Named here rather than inside <see cref="RingOffice"/> because the sweep this law is really
    /// about is building-wide (#818 will walk every carved room in the facility), and a threshold that lived
    /// in the ring's own furniture file would be re-typed the moment a laboratory needed it.</para>
    /// </summary>
    public const double FireCodeSmallRoomDu = 8.0;

    /// <summary>#822 · What KIND of carved space this is. Not decoration: a law about how you leave a room
    /// is often a law about one kind of room, and a guard that had to tell a chamber from a cabinet by its
    /// dimensions would be reading a coincidence.</summary>
    public enum RoomKind
    {
        /// <summary>A room off a rib — the module the whole building is made of.</summary>
        Chamber,

        /// <summary>A suite in the ring round the park, back of house included (#813/#817).</summary>
        RingSuite,

        /// <summary>The cantina hall (#751).</summary>
        Hall,

        /// <summary>A negotiating cabinet down the hall's outer wall (#751).</summary>
        Cabinet,

        /// <summary>A WC cubicle in the block's washroom (#821). Bedroom-small, by design.</summary>
        Cubicle,

        /// <summary>An en-suite cell hung off a principal chamber (#707). Bedroom-small, by design.</summary>
        Cell,

        /// <summary>#775 · A glass box in the middle of a landscape floor — one of the meeting rooms off the
        /// open core. Its own kind rather than a <see cref="Cabinet"/>, because a cabinet is a booking made
        /// at a bar's counter down a hall's outer wall and this is the room a department holds its
        /// meetings in.</summary>
        MeetingRoom,
    }

    /// <summary>
    /// #822 · ONE CARVED ROOM, WITH ITS OWN WAYS OUT — the list the fire code is swept over.
    ///
    /// <para>The building has published a room's CENTRE since #707 and its walls since the beginning, and
    /// neither of those can answer the owner's question. "How do you leave this room" is a fact about a
    /// BOX and the holes in it, and until this record existed the only way to ask it was to guess a room's
    /// extent by firing rays at the wall list — which walks straight out through the very doorway it is
    /// trying to count. So every placer down here hands over the box it laid and the gaps it cut, exactly
    /// as <see cref="Hall.Openings"/> and <see cref="RingRoom.Doors"/> already do, and the sweep reads one
    /// list nobody has to reconstruct.</para>
    ///
    /// <para><b>A way is a GAP, not a leaf.</b> The galleries of the found band hang no doors at all
    /// (#677 — an imported leaf down there would say somebody fitted it), so a room's ways are counted off
    /// the holes its own carver left in its own walls and never off <see cref="FloorPlan.Doorways"/>. A
    /// locked chamber is not in this list at all: it is not a space a captain can stand in, and the fire
    /// code is about the ones who are standing in them.</para>
    /// </summary>
    /// <param name="X0">Left edge of the box the walls were laid on.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Plate">What is stencilled on it, empty in the found band and on the plainer fittings.</param>
    /// <param name="Ways">Every hole in its walls a body can pass — street doors, corridor mouths, the gate
    /// onto the green, the door through to the recess next door. Never the locked ones.</param>
    /// <param name="Fittings">#818 · What is standing ON THE FLOOR of it. The owner's law — <i>"never ever
    /// empty floor"</i> — is a law about a list, and a law about a list nobody keeps is a law nobody can
    /// fail. Appended, so every caller that builds one positionally still means the same room. See
    /// <see cref="ChamberFitting"/> for the chambers and <see cref="RingOffice"/> for the suites: the two
    /// placers are different and the LIST is one, which is what lets a single sweep walk the building.</param>
    /// <param name="Chairs">#818 · Every seat in it, in the room's own order. Empty in a room nobody sits
    /// down in, which is a true statement about a store rather than a missing one.</param>
    public readonly record struct Room(
        double X0, double Y0, double X1, double Y1, string Plate,
        IReadOnlyList<SurfaceLayout.Doorway> Ways,
        RoomKind Kind = RoomKind.Chamber,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null)
    {
        /// <summary>#818 · The furniture, never null — a caller asking "is this floor bare" must not have to
        /// tell an empty list from a missing one.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>#818 · The seats, never null. Same reason.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>#818 · Does this room show what it is for? The owner's law asked of the room itself, so
        /// the carve, the renderer and the guard read one sentence.</summary>
        public bool IsFurnished => Furniture.Count > 0;

        /// <summary>The middle of it — where its plate is read from and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>How wide it is.</summary>
        public double WidthDu => X1 - X0;

        /// <summary>How deep it is.</summary>
        public double DepthDu => Y1 - Y0;

        /// <summary>Its longest wall — the number <see cref="FireCodeSmallRoomDu"/> is stated in.</summary>
        public double LongestSideDu => Math.Max(WidthDu, DepthDu);

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => WidthDu * DepthDu;

        /// <summary>#822 · Is it let off with one door? A space you can cross in two paces and whose one
        /// door you are never out of reach of — a WC cubicle, a privacy booth, an en-suite cell. The
        /// exemption #821's lock mechanic stands on.</summary>
        public bool BedroomSmall => LongestSideDu <= FireCodeSmallRoomDu;

        /// <summary>How many ways out of it there are — the number the fire code is stated in.</summary>
        public int Exits => Ways.Count;

        /// <summary>#822 · Does this room satisfy the standing law? Asked here rather than in the guard, so
        /// the carve and the sweep are reading one sentence.</summary>
        public bool MeetsFireCode => BedroomSmall || Exits >= FireCodeMinExits;

        /// <summary>Is the captain in it? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>
    /// #822 · How many doors a run of street frontage this long is served by, fire code included. The whole
    /// of the door law in one function, so the carve, the guards and any later sweep are reading one
    /// sentence.
    ///
    /// <para>The fire code is about WAYS OUT and not about street doors, so a room that already has another
    /// one — a suite with a gate onto the green — is not made to cut a second leaf in a face that has no
    /// room for it. That is not a softening: it is #724's jamb law and this one meeting. A 19 du end block
    /// forced to carry two 6.4 du leaves has 2 du of pier between them and 1.5 du at each end, and a captain
    /// standing anywhere on that face is within a sidestep of two different openings — which the funnel
    /// reads as <i>standing in a doorway</i> and answers by holding still. Watched go red exactly there:
    /// <c>+3.90 du off the centreline … 400 presses left the captain on the near side of the wall</c>. The
    /// room needs two ways out; it does not need both of them in the same wall.</para>
    /// </summary>
    /// <param name="frontageDu">The room's street face.</param>
    /// <param name="hasAnotherWayOut">Whether the room has an exit that is not in this face — today, a gate
    /// onto the park.</param>
    public static int DoorsForFrontage(double frontageDu, bool hasAnotherWayOut = false)
    {
        int wanted = Math.Max(1, (int)Math.Round(
            frontageDu / RingStreetFaceDuPerDoor, MidpointRounding.AwayFromZero));
        return frontageDu <= FireCodeSmallRoomDu || hasAnotherWayOut
            ? wanted
            : Math.Max(FireCodeMinExits, wanted);
    }

    /// <summary>
    /// #813 · WHAT IS STENCILLED ON A ROOM WITH THE VIEW — the six plates the block hangs on its park-facing
    /// frontage, and nowhere else in the building.
    ///
    /// <para>#775's amenity gradient says amenities follow rank. The Manhattan ruling is where that becomes
    /// a MAP: the rooms on the green are the expensive ones, so they get a vocabulary the corridors do not.
    /// Every other plate down here is drawn from <see cref="SignFor"/>'s own register of departments and
    /// refusals — <c>QUOTA OFFICE</c>, <c>DO NOT ADMIT UNESCORTED</c> — and those still go on the block's
    /// CORNER rooms, which stand past the end of the park's wall and have nothing to look at. The gradient
    /// is legible without a word being said about it: read along one wall and the rooms get better as the
    /// green comes into view.</para>
    ///
    /// <para>§13.8 holds, and this row is a soft place to break it exactly as the back of house is. Every
    /// one of these says what a ROOM is — a booking, a signature, an appointment — and not one of them says
    /// what the facility is for. The nearest any of them comes is #770's negotiation room, and all it names
    /// is where you book it: at the counter, which is the same sentence a cabinet's plate has carried since
    /// #751 (<see cref="CabinetPlate"/>). The building rents rooms with a view of a garden it built to
    /// squeeze morale out of a workforce, and it advertises the aspect.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> ParkViewPlates =
    [
        "REGISTERED OFFICE · GARDEN ASPECT",
        "NEGOTIATION ROOM · BOOK AT THE COUNTER",
        "SIGNATORY SUITE · TWO KEYS",
        "SENIOR ROTA · GREEN SIDE",
        "PRIVILEGED RECORDS · READING ROOM",
        "RECEPTION · APPOINTMENTS HELD",
    ];

    /// <summary>
    /// #821 · THE ONE ROOM ON THE BLOCK THAT IS NOT AN OFFICE.
    ///
    /// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we
    /// might want to hide from guards in one toilet cubicle we lock from inside :-D"</i>. The park is the
    /// building's one public ground and it had nowhere to wash your hands.</para>
    ///
    /// <para>It is a NEAR-band room re-plated — the premium band, which is where the hall is and therefore
    /// where the public already are — and it is exactly one per block, chosen off the ground rather than
    /// rolled (see <c>WashroomFrontageOn</c>). In practice that lands it on the band's NARROW end block: a
    /// building does not give its garden aspect to the WCs, which is #775's amenity gradient arriving one
    /// more time as plumbing, and it is the reason the plate below claims no view.</para>
    ///
    /// <para>§13.8 holds. It says what the room is and nothing about what the facility is for, and NO PASS
    /// REQUIRED is the canteen's own clause (<see cref="AmenitySigns"/>) — a fact about band 0 the building
    /// has been advertising since #590 and still never explains.</para>
    /// </summary>
    public const string ParkWashroomPlate = "🚻 PUBLIC WASHROOMS · NO PASS REQUIRED";

    /// <summary>#813 · Which side of the park a ring room faces it from. Near is the spine's side.</summary>
    public enum RingSide
    {
        /// <summary>The spine's side — the premium band, and the hall's.</summary>
        Near,

        /// <summary>The back street's side — the back of house.</summary>
        Far,

        /// <summary>The block's west end.</summary>
        West,

        /// <summary>The block's east end.</summary>
        East,
    }

    /// <summary>
    /// #813 · ONE ROOM ON THE RING — the room with the view, published with both of its walls.
    ///
    /// <para>Two walls are what make it one of these rather than an ordinary chamber, and they are on
    /// opposite sides of it: <see cref="View"/> is the park-facing wall, which is glass; <see cref="Door"/>
    /// is the way in, which is on a street. That is the owner's <i>"view side to the park, service side to
    /// the corridor"</i> as a record rather than as an arrangement two placers happen to agree on.</para>
    ///
    /// <para>Published for the reason <see cref="Hall.Openings"/> and <see cref="Park.Ways"/> are: "every
    /// park-facing wall is used" is a law about a list, and a law about a list nobody keeps is a law nobody
    /// can fail.</para>
    /// </summary>
    /// <param name="Number">1-based, in the order the ring was laid: near, far, west, east.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Side">Which of the park's four walls this room is one of.</param>
    /// <param name="Door">The FIRST way in, cut in the street's face. #817 · It used to be the only one and
    /// this parameter used to say so; the owner overrode that from inside a landscape office with one leaf
    /// in it. Every caller that means "the way in" still means this one, and every caller that means "all of
    /// them" says <see cref="RingRoom.Doors"/>.</param>
    /// <param name="View">The park-facing wall, as the segment it was built as — null on a CORNER room,
    /// which stands past the end of the park's own wall and therefore has nothing to look at. A corner
    /// office with no view is the amenity gradient (#775) drawn on the plan.</param>
    /// <param name="Gate">The door in the park-facing wall, where this room has one. #817 · EVERY ROOM WITH
    /// A VIEW HAS ONE — owner's ruling, live: a premium office on a garden gets a door to the garden and not
    /// only a window at it. The far band's back of house has kept #801's own way in off the gravel since it
    /// was carved, and this is the same door, granted to the rooms that were paying for the aspect. Null on
    /// a corner room, which has no park in front of it — and null on the HALL, whose glass is still never a
    /// door: that rule was always about the bar's window wall and never about the ring's.</param>
    /// <param name="Plate">What is stencilled beside the FIRST street door.</param>
    /// <param name="Ways">#817 · Every street door, in the order they were cut along the frontage. Null on a
    /// room built before the count scaled, which is why <see cref="Doors"/> falls back to
    /// <paramref name="Door"/> rather than to an empty list — an empty list would make "every ring room
    /// opens onto a street" vacuously true, which is the one way a law about a list can fail silently.</param>
    /// <param name="Fittings">#817 · What is standing on the floor of it. See <see cref="RingOffice"/>.</param>
    /// <param name="Chairs">#817 · Every seat in it, in the room's own order. Chairs face the GLASS, because
    /// the view is what the room rents for and the furniture should agree.</param>
    /// <param name="Cells">#821 · Every WC cubicle in it, each paired with its OWN published leaf — the
    /// staff pair in a big suite's service strip and the row in the block's public washroom, off one list
    /// because they are one kind of door. See <see cref="RingOffice.Stall"/>: the lock is a fact about one
    /// door of one cell, and nothing outside the placer that laid both can say which is which.</param>
    /// <param name="Taps">#821 · The basins along a public washroom's run.</param>
    public readonly record struct RingRoom(
        int Number, double X0, double Y0, double X1, double Y1, RingSide Side,
        SurfaceLayout.Doorway Door, SurfaceLayout.Wall? View, SurfaceLayout.Doorway? Gate,
        string Plate,
        IReadOnlyList<SurfaceLayout.Doorway>? Ways = null,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null,
        IReadOnlyList<RingOffice.Stall>? Cells = null,
        IReadOnlyList<RingOffice.Basin>? Taps = null,
        // #775 · Whether its street door is a leaf that will not open. FALSE on every room of the block
        // round the park, which is the public floor and has no closed rooms on it; true on a share of a
        // landscape floor's back band, which is the department's own — see CarveRing. Appended and never
        // inserted, for the reason every optional on this record is appended.
        bool Shut = false)
    {
        /// <summary>#821 · The cubicles, never null.</summary>
        public IReadOnlyList<RingOffice.Stall> Cubicles => Cells ?? [];

        /// <summary>#821 · The basins, never null.</summary>
        public IReadOnlyList<RingOffice.Basin> Basins => Taps ?? [];

        /// <summary>#817 · EVERY STREET DOOR. Owner: <i>"bigger spaces must have much more doors."</i>
        /// Published for the reason <see cref="Hall.Openings"/> is: a law about how a room is entered cannot
        /// be written against a list nobody keeps.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Doors => Ways ?? [Door];

        /// <summary>#817 · The furniture, never null.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>#817 · The seats, never null.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>#822 · EVERY WAY OUT OF IT — the street doors and, where it has one, the gate onto the
        /// green. The list <see cref="Room.Ways"/> is filled from, so a suite's egress is one fact told
        /// once rather than a count here and a list somewhere else.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> WaysOut =>
            Shut ? [] : Gate is { } onto ? [.. Doors, onto] : Doors;

        /// <summary>#822 · How many ways out of it there are altogether — the street doors and the gate onto
        /// the green. The number the fire code is stated in, asked of the room's own published lists.</summary>
        public int Exits => WaysOut.Count;

        /// <summary>The middle of it — where its plate is read from and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Is the captain in it? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);

        /// <summary>Does it look at the green? False on the four corner rooms, and that is a true statement
        /// about them rather than a missing one.</summary>
        public bool HasView => View is not null;
    }

    /// <summary>
    /// #813 · THE BLOCK, AS A PURE FUNCTION OF THE GROUND — every line the ring is laid on, decided once.
    ///
    /// <para>Field-pure on purpose, and it is the same discipline <see cref="RibColumnsOn"/> is written
    /// with: the goods car is placed against these numbers (<see cref="ServiceShaftAt"/>) and a car stands
    /// in the same place on every floor of a site, so anything the car is measured against has to hold for
    /// the whole building and not for the one floor that has a park on it.</para>
    /// </summary>
    /// <param name="WestStreetX">The centre line of the block's west street.</param>
    /// <param name="EastStreetX">The same, east.</param>
    /// <param name="X0">The park's own left edge.</param>
    /// <param name="X1">The park's own right edge.</param>
    /// <param name="Y0">The park's far wall — the back street's side.</param>
    /// <param name="Y1">The park's near wall — the spine's side, and the hall's glass.</param>
    /// <param name="SpineFaceY">The spine's lower face, which is the near ring's own front wall.</param>
    /// <param name="BackStreetY0">The back street's far face — the block's outer wall.</param>
    /// <param name="BackStreetY1">Its near face, which is the far ring's back wall.</param>
    /// <param name="SpurXs">Where a gate cuts through the near and far bands. These are rib columns, so a
    /// corridor and a cross corridor can never disagree about where the crossings are (#801's own
    /// reason).</param>
    public readonly record struct ParkBlock(
        double WestStreetX, double EastStreetX,
        double X0, double X1, double Y0, double Y1,
        double SpineFaceY, double BackStreetY0, double BackStreetY1,
        IReadOnlyList<double> SpurXs)
    {
        /// <summary>The inside face of the west street — the wall the west ring's doors are cut in.</summary>
        public double WestInnerX => WestStreetX + CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastInnerX => EastStreetX - CorridorHalf;

        /// <summary>The block's own outer wall, west.</summary>
        public double WestOuterX => WestStreetX - CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastOuterX => EastStreetX + CorridorHalf;

        /// <summary>How much floor the park itself has — what the owner's "do not make it a puny small
        /// closet" is measured in.</summary>
        public double ParkDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#813 · The block's lines, off the ground alone. See <see cref="ParkBlock"/>.</summary>
    public static ParkBlock BlockOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double _, double shaftY) = ShaftAt(field);

        double west = left + BlockStreetInsetDu, east = right - BlockStreetInsetDu;
        double spineFace = shaftY - CorridorHalf;
        double nearY = spineFace - RingNearDepthDu;
        double farY = nearY - ParkDepthDu;
        double streetY1 = farY - RingFarDepthDu;
        double streetY0 = streetY1 - (2 * CorridorHalf);

        double x0 = west + CorridorHalf + RingSideDepthDu;
        double x1 = east - CorridorHalf - RingSideDepthDu;

        // WHICH COLUMNS THE GATES GO DOWN. The rib columns, so a gate and a cross corridor are one line —
        // and only those that leave a whole room at each end of the band they cut. A gate against the corner
        // would buy a through-route by spending the frontage the through-route exists to show off.
        var spurs = new List<double>();
        foreach ((int _, double rx) in RibColumnsOn(field))
        {
            if (rx - CorridorHalf >= x0 + RingRoomMinDu && rx + CorridorHalf <= x1 - RingRoomMinDu)
            {
                spurs.Add(rx);
            }
        }

        return new ParkBlock(west, east, x0, x1, farY, nearY, spineFace, streetY0, streetY1, spurs);
    }

    /// <summary>
    /// #813 · HOW MUCH ROOM FRONTAGE THE BLOCK CARRIES ON EACH SIDE OF ITS OWN MIDDLE, in deck units.
    ///
    /// <para>The near and far bands run the length of the block and the gates cut them; what is left is
    /// room, and how much of it lies each side of the park's centre line is what "the less-built side"
    /// means. It is not symmetric, and the reason it is not is worth saying: the rib columns are laid at
    /// fifths of the spine and the one nearest the cage is DROPPED (<see cref="RibColumnsOn"/>), so the
    /// gates fall on one side of the middle and the long unbroken run of suites falls on the other.</para>
    ///
    /// <para>Field-pure, so the car it decides stands in the same place on every floor.</para>
    /// </summary>
    public static (double West, double East) RingFrontageOn(in SurfaceLayout.Field field)
    {
        ParkBlock block = BlockOn(field);
        double mid = (block.X0 + block.X1) / 2.0;
        double west = mid - block.X0, east = block.X1 - mid;
        foreach (double sx in block.SpurXs)
        {
            double lo = sx - CorridorHalf, hi = sx + CorridorHalf;
            west -= Math.Max(0, Math.Min(hi, mid) - Math.Max(lo, block.X0));
            east -= Math.Max(0, Math.Min(hi, block.X1) - Math.Max(lo, mid));
        }
        return (west, east);
    }

    /// <summary>#751 · What is stencilled beside a cabinet's door. Numbered, and it says how you get one:
    /// not off a menu.</summary>
    public static string CabinetPlate(int number) =>
        $"CABINET {number} · BY ARRANGEMENT · ASK AT THE COUNTER";

    /// <summary>#751 · How many a cabinet seats. SIX, on every one of them, and it is not a taste: the
    /// cabinet's own card and the field book both count the chairs out loud (<i>"six chairs, one door"</i>),
    /// and a cabinet that seated four would make a card lie about a room the captain is standing in.</summary>
    public const int CabinetSeats = 6;

    /// <summary>#751 · How many cabinets a cantina hall has. Three is a row of doors along a back wall —
    /// enough that the row reads as a FACILITY for the thing rather than as one odd room.</summary>
    public const int CabinetsPerHall = 3;

    /// <summary>
    /// #751 · HOW MANY PEOPLE THIS BUILDING IS FOR — the establishment, derived from the building's own
    /// stock and never typed.
    ///
    /// <para>Owner: <i>"usually people eat lunch at same time so the whole staff using it should about fit
    /// in."</i> That makes the mess's size a question about STAFFING, and this is the only place the game
    /// answers it — because staffing questions keep arriving (#618's guards, #717's rosters) and two answers
    /// to one of them is the table at the top of this file.</para>
    ///
    /// <para><b>The arithmetic, so it can be argued with.</b> A floor of this building IS a department —
    /// <see cref="DepartmentOf"/> gives exactly one plate per floor, and the lift panel has been printing it
    /// since #605. A department is a desk, a store, the plant that serves them and the hands that work all
    /// three: <see cref="HeadsPerDepartment"/>. So the complement is the departments the building admits to,
    /// times that. A twenty-storey clinic runs eighty people; a five-floor annex runs twenty, and its mess is
    /// smaller for an honest reason rather than because somebody typed a smaller number.</para>
    ///
    /// <para><b>LISTED floors only</b>, which is the line worth reading twice. <see cref="DepthOf"/> is what
    /// the directory admits to; the band nobody listed has no department, no plate and no livery (#592's
    /// whole tell is that absence) — so it has nobody on the books either. Whoever is down there is not on
    /// this payroll, and the catering budget says so without one word of prose.</para>
    /// </summary>
    public static int ImpliedComplement(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return -DepthOf(bodyId) * HeadsPerDepartment;
    }

    /// <summary>#751 · What one department is, in people: a desk, a store, the plant that serves them, and
    /// the hands. FLAGGED for the owner's tuning — it is the one number <see cref="ImpliedComplement"/>
    /// cannot derive from the building, because the building never wrote a payroll down.</summary>
    public const int HeadsPerDepartment = 4;

    /// <summary>
    /// #751 · HOW MANY THE B1 CANTINA HALL SEATS. The owner's own figure — <i>"It needs to house like 80
    /// customers"</i> — and it is a statement about the COVER rather than about the staff: eighty carriers
    /// eating on the company's coin, none of whom ask what the cage carries, is #707's lie rendered as a
    /// crowd. FLAGGED for tuning.
    /// </summary>
    public const int CantinaHallSeats = 80;

    /// <summary>#751 · Is this amenity carved as a hall? Both canteens are; a washroom never is (nobody
    /// eats a shift's lunch in the cubicles).</summary>
    public static bool IsHallClass(Comfort use) =>
        use is Comfort.UpperCanteen or Comfort.StaffCanteen;

    /// <summary>#751 · What a hall of this kind is asked to seat. The two customers of one carve, and the
    /// only line in the file where they differ.</summary>
    public static int HallSeatsFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.StaffCanteen ? ImpliedComplement(bodyId) : CantinaHallSeats;
    }

    /// <summary>#751 · The three sizes of round top a caterer buys, smallest first. The owner's own three
    /// (#746, <i>"tables should seat 2/4/more, not all pairs"</i>), stated as a list so a guard can pin them
    /// without knowing the arithmetic that fills a hall with them.</summary>
    public static readonly IReadOnlyList<int> HallTopSizes = [2, 4, 6];

    /// <summary>
    /// #751 · THE BILL OF FURNITURE — how many of each size a hall seating <paramref name="seatTarget"/>
    /// buys, and in what order they are laid out.
    ///
    /// <para><b>Designed, not rolled, and that is load-bearing.</b> A seeded 2/4/6 per top has a standard
    /// deviation of seven seats over twenty tables, so a hall asked for eighty would ship anywhere between
    /// sixty-five and ninety-five and the guard would be measuring a die. A caterer does not roll dice: they
    /// buy a stock — three tops in ten seat two, four seat four, three seat six — and the floor plan decides
    /// where each one goes. The stock's average is exactly four, so the total is exactly the target and the
    /// mix is exactly the owner's three.</para>
    ///
    /// <para>The ORDER is seeded off the site (never off the watch — #746's law: a canteen does not
    /// re-furnish itself every shift), so two halls of the same size are laid out differently and neither
    /// reads as three zones of identical furniture.</para>
    /// </summary>
    /// <param name="bodyId">The site, which decides only the arrangement.</param>
    /// <param name="use">Which hall, so the cantina and the mess of one site differ.</param>
    /// <param name="seatTarget">How many the hall must seat. Rounded up to a whole top.</param>
    public static IReadOnlyList<int> HallSeatBill(string bodyId, Comfort use, int seatTarget)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Four is the stock's average, so the table count falls straight out of the target. Rounded UP: a
        // mess that seats the shift less one is a mess that does not seat the shift.
        int tables = Math.Max(HallTopSizes.Count, (seatTarget + 3) / 4);

        // Three in ten either side of the middle. Equal counts of twos and sixes is what makes the total
        // land exactly on four per top — every 6 is paid for by a 2.
        int wings = Math.Max(1, (int)Math.Round(tables * 0.3, MidpointRounding.AwayFromZero));
        while ((2 * wings) + 1 > tables)
        {
            wings--;    // a tiny hall still gets one of each, and never more tops than it has
        }

        var bill = new List<int>(tables);
        for (int i = 0; i < wings; i++)
        {
            bill.Add(2);
        }
        for (int i = 0; i < wings; i++)
        {
            bill.Add(6);
        }
        while (bill.Count < tables)
        {
            bill.Add(4);
        }

        // …and shuffled into place with a seeded swap walk, so the twos are not all by the door. Same
        // skip-forward discipline the rest of this ground uses: a deterministic permutation, never a
        // re-roll loop.
        for (int i = bill.Count - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(
                DiceRule.Seed($"hive:hall:bill:{bodyId}:{(int)use}:{i}"), i + 1).Face - 1;
            (bill[i], bill[j]) = (bill[j], bill[i]);
        }

        return bill;
    }

    /// <summary>#707 · A private washroom cell hung off the back of a room that mattered.
    ///
    /// <para>Owner: <i>"the high level important rooms would have their built in bathrooms"</i> — and the
    /// design under it is that RANK IS READABLE IN PLUMBING. A captain who has learned the grammar reads
    /// "somebody with a name worked in here" off a door to a private cell, the same way sealed SECTOR doors
    /// read as scale. No card ever says it, and the cell itself carries no plate — a private washroom does
    /// not need a sign, and that absence is the last word of the tell.</para></summary>
    /// <param name="X">Centre of the cell.</param>
    /// <param name="Y">Centre of the cell.</param>
    /// <param name="Of">The plate of the room it hangs off — the reason it is there.</param>
    /// <param name="Open">Whether its parent room's own door opens. False behind a locked plate, where the
    /// cell is a thing you can only read from the corridor, exactly like the room it belongs to.</param>
    public readonly record struct EnSuite(
        double X, double Y, string Of, bool Open,
        // #822 · The gap cut in the parent's back wall — the cell's one door, and the whole of its egress.
        // Appended for the reason every optional on these records is appended.
        SurfaceLayout.Doorway? Leaf = null)
    {
        /// <summary>#822 · Every way out of it, never null. A cell is <see cref="EnSuiteDepth"/> by twice
        /// <see cref="EnSuiteHalfHeight"/> — eight du on its longest side, which is
        /// <see cref="FireCodeSmallRoomDu"/> exactly. It is the room the exemption was measured on.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Ways => Leaf is { } leaf ? [leaf] : [];
    }

    /// <summary>How deep the en-suite cell hangs off the back of its room, in deck units.</summary>
    public const double EnSuiteDepth = 5.0;

    /// <summary>Half the cell's height. Comfortably taller than <see cref="DoorHalf"/>, so the doorway cut
    /// in the parent's back wall always lands inside the cell rather than beside it.</summary>
    public const double EnSuiteHalfHeight = 4.0;

    /// <summary>#707 · THE TOPMOST FLOOR THAT HOLDS PRESSURE — where the bar is.
    ///
    /// <para>Derived rather than typed. It is B1 on every building in the game and writing <c>-1</c> here
    /// would be a second answer to a question <see cref="HoldsPressure"/> already owns, sitting quietly
    /// correct until somebody moves a band. Two sources, one of which never hears about a change, is the
    /// table at the top of this repo's spec.</para></summary>
    public static int? TopPressurisedFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · IsPlumbed, not HoldsPressure. The bar goes on the topmost floor that breathes AND has a
            // wet stack; the halls breathe and have no plant of any kind, and on a shallow site they would
            // otherwise be eligible for a counter and three round tops.
            if (IsPlumbed(bodyId, level))
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>#707 · WHERE THE STAFF CANTEEN IS: the deepest floor the building ADMITS to that still
    /// holds pressure, and null on a site too shallow to have a second one.
    ///
    /// <para><b>Deepest, and listed.</b> Two calls, each worth one line:</para>
    /// <list type="bullet">
    /// <item><b>Deepest</b> because the owner's inversion needs distance. The bar is the floor strangers
    /// walk into off the surface; the mess has to be as far from that as the building goes, so that a face
    /// nobody knows is a fact about the room rather than a matter of taste.</item>
    /// <item><b>Listed</b> (<see cref="DepthOf"/>, not <see cref="TrueDepthOf"/>) because catering is a
    /// thing a directory knows about. The band nobody listed has no department, no livery and no plate —
    /// #592's whole tell is the ABSENCE down there — and a canteen sign under it would be the building
    /// admitting to a floor in the one place it must not.</item>
    /// </list>
    ///
    /// <para>Null on a shallow site, and that is the honest answer rather than a gap: a three-floor annex
    /// has one canteen, because one canteen is the entire catering budget of a three-floor annex.</para></summary>
    public static int? StaffCanteenFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int? top = TopPressurisedFloor(bodyId);
        int? deepest = null;
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · …and never in the halls, which the directory could not list if it wanted to. IsPlumbed
            // already refuses them; the IsUnlisted clause stays because a floor can be listed-and-unplumbed
            // for the OTHER reason (§13.7's whole tell is the absence of a plate, not of a drain).
            if (IsPlumbed(bodyId, level) && !IsUnlisted(bodyId, level))
            {
                deepest = level;
            }
        }
        return deepest == top ? null : deepest;
    }

    /// <summary>
    /// #707 · WHICH DOOR PLATES BELONG TO SOMEBODY RATHER THAN TO SOMETHING — the rooms that get an
    /// en-suite.
    ///
    /// <para>The criterion, so it can be argued with instead of guessed at: <b>a plate is principal when it
    /// names an OFFICE or an AUTHORITY — somewhere a decision gets signed — rather than a process, a store,
    /// or a room where work is done TO somebody.</b> COLD STORE 2 is a place things are kept; SUBJECT PREP
    /// is a place things are done; QUOTA OFFICE is a place a person sits and rules on other people, and
    /// that person had a door of their own and did not queue for the cubicles on B1.</para>
    ///
    /// <para>And the RATIO is the rank difference, emergent and never stated: one plate in eight at a
    /// branch office, five in twelve at the head office. A captain who has crawled a Hive and then walks a
    /// head-office corridor sees private washrooms on half the doors, and nothing anywhere tells them what
    /// that means.</para>
    ///
    /// <para>Written as a list of plates taken verbatim out of <see cref="SignsFor"/> rather than as a
    /// keyword match on the string. A match on "OFFICE" would silently collect MANIFEST OFFICE and QUOTA
    /// OFFICE and then, the day somebody writes a plate reading POST OFFICE, that too — a rule that selects
    /// by accident is this repo's fifth bug class wearing a clever hat. Every entry here is proved to exist
    /// in some kind's vocabulary by <c>EveryPrincipalPlateIsAPlateThisBuildingActuallyHangs</c>.</para></summary>
    public static bool IsPrincipalRoom(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        foreach (string p in PrincipalPlates)
        {
            if (string.Equals(p, plate, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The plates a person sat behind. See <see cref="IsPrincipalRoom"/> for the criterion.</summary>
    public static readonly string[] PrincipalPlates =
    [
        "CONTINUITY — AUTHORISED ONLY",                       // Laboratory: the one plate that grants
        "OCCUPATIONAL REVIEW", "QUOTA OFFICE",                 // ProcessingDepot: a panel, and a desk
        "AUDIT — NO ADMITTANCE",                               // RecordsAnnex
        "CONSENT FILES",                                       // BlackClinic: somebody countersigned those
        "MANIFEST OFFICE",                                     // TransitStation
        // #411 · The head office is mostly people who sign things, and it shows in the plumbing.
        "OFFICE OF THE REGISTRAR", "ESTABLISHMENT BOARD", "COMMITTEE ROOM 2", "APPROPRIATIONS",
        "DEPUTATIONS",
    ];

    /// <summary>What is stencilled beside an amenity's door, and what the fixture in the middle of it is
    /// called. Both from one place, so the sign on the wall and the console under the captain's hand can
    /// never come to describe different rooms.
    ///
    /// <para>Institutional throughout, and explaining nothing — with one deliberate exception of TONE. The
    /// branch office's bar plate is the only WARM sign in the building, because it is the only sign in the
    /// building that is a lie: a rest-house plate on a corridor of DESTRUCTION QUEUE and MORTUARY. NO PASS
    /// REQUIRED is a fact about band 0 that the lift panel has been shipping since #590, said out loud on a
    /// wall for the first time and still not explained.</para>
    ///
    /// <para>The head office answers the same law in its own vocabulary (#411): not a canteen and a
    /// washroom but a DINING ROOM and a CLOAKROOM, and its staff hall is for the ESTABLISHMENT — which is
    /// the word on its own B2 plate. Same rule, same grammar, a rank nobody has to be told about.</para></summary>
    public static (string Plate, string Fixture) AmenitySigns(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return use switch
        {
            Comfort.UpperCanteen => hq
                ? ("🍸 THE DINING ROOM · GUESTS & DEPUTATIONS", "🍸 THE SIDEBOARD")
                : ("🍸 CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED", "🍸 THE COUNTER"),
            Comfort.StaffCanteen => hq
                ? ("🍽 THE STAFF DINING HALL · ESTABLISHMENT ONLY", "🍽 THE SERVERY")
                : ("🍽 CANTEEN 2 · STAFF ONLY · PASS TO BE SHOWN", "🍽 THE MACHINES"),
            _ => hq
                ? ("🚻 CLOAKS & WASHROOMS", "🚻 THE BASIN RUN")
                : ("🚻 WASHROOMS · STAFF & VISITORS", "🚻 THE BASIN RUN"),
        };
    }

    /// <summary>What one of these rooms says when the captain stands in it. Evidence, and then it stops —
    /// every one of them is about what somebody was made to pay for and none of them is about what any of
    /// it was for.</summary>
    public static string AmenityLine(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return (use, hq) switch
        {
            (Comfort.UpperCanteen, false) =>
                "🍸 A long counter, a mirror behind it with the bottles gone, and the stools bolted down in " +
                "a row. Somebody kept this room WARM: the paint is a colour that appears nowhere else in " +
                "the building and the tables have been wiped. Whoever came down that shaft with a delivery " +
                "was fed and watered before they went back up, and nothing on this floor ever asked them " +
                "for a pass to do it.",

            (Comfort.UpperCanteen, true) =>
                "🍸 A dining room, and it is LAID. Cloth on the tables, glasses upended on a tray, covers " +
                "still on the sideboard. Places for eleven, set at the same spacing all the way down, and " +
                "the chair at the head pulled out by a hand's width. Somebody set this for a date, and the " +
                "date is not on anything in the room.",

            (Comfort.StaffCanteen, false) =>
                "🍽 Four machines and not a bottle of anything in the racks: soup, tea, and a wall of the " +
                "same wrapped biscuit. The tables are close together and the chairs face each other, which " +
                "is what a room for people who already know each other looks like.\n\n" +
                "📋 Pinned by the machines, a delivery manifest renewed every quarter without a break — and " +
                "the address on it is a SCHOOL, on another world entirely, costed per head for a roll of " +
                "two hundred and forty. Same account number every quarter. Signed for by a name with no " +
                "initial.",

            (Comfort.StaffCanteen, true) =>
                "🍽 Long tables, a servery with the shutters down, and trays stacked to the ceiling with " +
                "nothing between them. Every tray is clean. The rota on the wall is ruled to the end of a " +
                "year nobody has written in yet.\n\n" +
                "📋 And the standing order over the servery is the OTHER HALF of a manifest you have read " +
                "somewhere else: same account number, same quarterly quantity, addressed to a school a very " +
                "long way from here. This is the copy the office kept.",

            (_, true) =>
                "🚻 Cloakroom and washrooms. Numbered hooks, none of them used. A basin run in stone rather " +
                "than steel, and the taps run clear from the first second — somebody flushed this system " +
                "through, and not decades ago.",

            _ =>
                "🚻 Cubicles, a basin run, and a mirror with a tally scratched into the corner and mostly " +
                "rubbed out again. The taps still turn. The water comes through brown for four seconds and " +
                "then runs clear, which means a pump somewhere under your boots has never once stopped.",
        };
    }

    /// <summary>#813 · How a band of ring frontage is cut into rooms — one answer, asked by the near band,
    /// the far band, the two ends, and by the chooser that decides which sub-segment the hall stands in.
    ///
    /// <para>The gates cut the band into segments; the park's own two ends cut it again, because a room that
    /// straddled the corner would have glass along part of its front wall and rock along the rest, and a
    /// wall that is two materials is a wall two placers will one day disagree about.</para></summary>
    private static List<(double Lo, double Hi)> RingSegments(
        double lo, double hi, IReadOnlyList<double> gapCentres, double half, params double[] splits)
    {
        var cuts = new List<(double Lo, double Hi)>(gapCentres.Count);
        foreach (double c in gapCentres)
        {
            cuts.Add((c - half, c + half));
        }
        cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        var spans = new List<(double Lo, double Hi)>();
        double cursor = lo;
        foreach ((double glo, double ghi) in cuts)
        {
            if (glo > cursor)
            {
                spans.Add((cursor, glo));
            }
            cursor = Math.Max(cursor, ghi);
        }
        if (cursor < hi)
        {
            spans.Add((cursor, hi));
        }

        var cut = new List<(double Lo, double Hi)>(spans.Count + splits.Length);
        foreach ((double slo, double shi) in spans)
        {
            double at = slo;
            foreach (double s in splits)
            {
                if (s > at + 0.001 && s < shi - 0.001)
                {
                    cut.Add((at, s));
                    at = s;
                }
            }
            cut.Add((at, shi));
        }
        return cut;
    }

    /// <summary>#813 · The near band's sub-segments — the run of ground between the spine and the park's own
    /// near wall, once the gates and the park's two ends have been taken out of it. Field-pure, and lifted
    /// out whole so the chooser that puts the hall in one of them and the carve that fills the rest are
    /// reading the SAME list rather than two copies of the same arithmetic (§13.15).</summary>
    public static IReadOnlyList<(double Lo, double Hi)> RingNearSegments(in ParkBlock block) =>
        RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1);

    /// <summary>#813 · How many rooms a run of frontage this long is cut into, and each of them the same
    /// width. Never fewer than one and never so many that one of them is under
    /// <see cref="RingRoomMinDu"/>.</summary>
    private static int RingRoomsIn(double span)
    {
        int n = Math.Max(1, (int)Math.Round(span / RingRoomTargetDu, MidpointRounding.AwayFromZero));
        while (n > 1 && span / n < RingRoomMinDu)
        {
            n--;
        }
        return n;
    }

    /// <summary>
    /// #813 · THE RING, CARVED — every room that faces the park, the four streets that serve them, and the
    /// six gates through them.
    ///
    /// <para>It owns the park's whole boundary, which is the one thing that makes the Manhattan ruling
    /// provable rather than decorative: every du of the park's four walls is laid HERE, either as a room's
    /// glass or as a gate's stub, so "no side of the park is wasted" is true by construction and not by an
    /// arrangement two placers happen to agree on. The single exception is the hall's own glass, which
    /// <see cref="CarveHall"/> laid before this ran, and which this therefore steps over — #585's one-gap
    /// law, said about a wall with two authors.</para>
    ///
    /// <para>Laid in one order, near then far then the two ends, so a room's <see cref="RingRoom.Number"/>
    /// is a fact about where it is rather than about when the loop got to it.</para>
    /// </summary>
    private static List<RingRoom> CarveRing(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi, double PlateX, string Plate)> spineDoors,
        List<(double Lo, double Hi)> spineMouths,
        List<SurfaceLayout.Doorway> gates, List<LockedDoor> locked,
        string bodyId, int level, in ParkBlock block, Hall? hall)
    {
        bool found = IsFound(bodyId, level);

        // #775 · IS THIS A LANDSCAPE FLOOR — a block with no garden in the middle of it? Two things follow
        // from the answer, and both of them are about not telling a lie the plan can be read for:
        //
        //   · THE PLATES. ParkViewPlates says GARDEN ASPECT and GREEN SIDE and ParkBackPlates is a potting
        //     shed's register. Every one of those is true of the one floor with a park behind the glass and
        //     false of every floor without one, so down here the rooms take the floor's own department
        //     register — SignFor, which is exactly what the block's corner offices have always taken.
        //   · THE DRESSING. A LABORATORIES floor's back band is a lab and not a store: benches with the
        //     glassware racked along them, and desks behind (#775 beat 2).
        bool landscape = !HasParkBlock(bodyId, level);
        bool labs = landscape && ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(bodyId, level));

        var ring = new List<RingRoom>();
        double sf = block.SpineFaceY;

        // ── THE SHELL · three walls, and the fourth side is the spine's own face.
        walls.Add(new(block.WestOuterX, sf, block.WestOuterX, block.BackStreetY0, true));
        walls.Add(new(block.EastOuterX, sf, block.EastOuterX, block.BackStreetY0, true));
        walls.Add(new(block.WestOuterX, block.BackStreetY0, block.EastOuterX, block.BackStreetY0, true));
        claimed.Add((block.WestOuterX - 1.5, block.BackStreetY0 - 1.5, block.EastOuterX + 1.5, sf + 1.5));

        // ── THE MOUTHS ON THE SPINE · the two streets and every gate down the near band. Corridor-width,
        //    and in the mouths list rather than the doors list, because a corridor mouth gets no leaf and no
        //    plate — exactly as a rib's mouth never has.
        spineMouths.Add((block.WestOuterX, block.WestInnerX));
        spineMouths.Add((block.EastInnerX, block.EastOuterX));
        foreach (double sx in block.SpurXs)
        {
            spineMouths.Add((sx - CorridorHalf, sx + CorridorHalf));
        }

        // ── #821 · WHICH OF THE NEAR SUITES IS THE BLOCK'S PUBLIC WASHROOM.
        //
        // Decided BEFORE anything is laid, because the plate is what the furnishing reads (RingOffice.
        // DressingFor) and a room cannot be re-plated after it has been furnished as an office.
        //
        // Off the ground and never off a die: the one nearest the middle of the block, of those wide enough
        // to hold a terrace and a basin run. That is a REASON — the middle of the near band is the busiest
        // frontage in the building, with the hall on one side of it and a gate onto the green on the other,
        // which is where a public washroom goes — and it makes the room a fact a captain can learn rather
        // than a shift's roll.
        double washroomAt = WashroomFrontageOn(block, hall);

        // ── (1) THE NEAR BAND · the premium suites, doors on the spine, glass on the green.
        foreach ((double lo, double hi) in RingNearSegments(block))
        {
            // The hall stands in one of these and it stands in the whole of it (see Build). Nothing else is
            // laid on that ground: the hall published its own glass and its own front doors already.
            //
            // #775 · …on the ONE floor that has a hall. A landscape floor's near band is offices end to end,
            // and the null here is a room that was never carved rather than a room this loop has to guess
            // the extent of — which is why the hall arrives as a Hall? and not as a box of NaNs.
            if (hall is { } venue && venue.X1 > lo + 0.001 && venue.X0 < hi - 0.001)
            {
                continue;
            }
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                bool washroom = !double.IsNaN(washroomAt)
                    && rx0 <= washroomAt + 0.001 && rx1 >= washroomAt - 0.001;

                // #817 · AND A DOOR ONTO THE GREEN. Owner's ruling, live in one of these: a premium office
                // that sold on the aspect gets a way OUT into the garden, not only a window at it. The far
                // band's back of house has had exactly this door since #801 and it is the same door.
                //
                // The hall's glass is untouched and always was: the segment the hall stands in is stepped
                // over above, and #751's "the glass between the bar and the green is never a door" was
                // always a rule about the BAR's window wall — a public room with eighty seats in it, whose
                // egress is its own front doors on the spine. RingBox cuts one only where there is park in
                // front of the room to cut it into, so the block's corner offices still have none.
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Near, rx0, block.Y1, rx1, sf, block, gate: true,
                    plateOverride: washroom ? ParkWashroomPlate : null,
                    landscape: landscape, labs: labs));
            }
        }

        // ── (2) THE FAR BAND · #801's back of house, re-anchored as ring fabric. It keeps the door onto the
        //    gravel that made it worth walking across a garden for, and it GAINS the street door the
        //    Manhattan ruling requires — the owner's "nobody walks through an office to reach an office"
        //    said about the one row that used to have no other way in.
        int back = 0;
        foreach ((double lo, double hi) in RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1))
        {
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                // ── #775 · AND ONE IN THREE OF THEM DOES NOT OPEN.
                //
                // Owner's oldest note about this building, and the cheapest thing in it: <i>"we can again
                // use the locked doors to give the illusion of much larger space."</i> The block round the
                // park is the PUBLIC floor — a bar, a garden, a washroom anybody may use — and every room on
                // it opens. A department's own floor does not work like that, and a landscape floor cut
                // entirely of open rooms reads as a showroom: it lost the closed doors the ordinary grid
                // gets for free (AddRoomsAlong shuts half its chambers) and the floor stopped implying
                // anything past itself. Watched go red on the guard that says so, at
                // `callisto B10 · NO PLATE: nothing implies the rest of it` — two locked doors on a whole
                // floor.
                //
                // THE BACK BAND, because that is where a department's closed rooms are: off the service
                // street, behind the core, away from the desks. EVERY OTHER one, off the ground rather than
                // off a die, because a seeded share can roll none and the illusion would then be missing on
                // some worlds forever with every test still green — the same reasoning KeyRoomFor is
                // designated for. Alternating rather than clustered, so the row reads as a department's own
                // doors down its own street rather than as one sealed corner.
                //
                // EVERY OTHER and not every third, and the difference is a law: the band is six rooms on the
                // thinnest floor the generator makes, so alternating guarantees THREE closed doors and a
                // third would have guaranteed two. Three is the number the unlisted band's own guard has
                // demanded of every floor in this building since #592 — watched go red at exactly two on
                // probe-moon-36 B10 with a third, which is the arithmetic saying so rather than a taste.
                bool closed = landscape && back % 2 == 1;
                back++;
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Far, rx0, block.BackStreetY1, rx1, block.Y0, block,
                    gate: true, plateOverride: null, landscape: landscape, labs: labs,
                    locked: locked, shut: closed));
            }
        }

        // ── (3) AND THE TWO ENDS · a gate through the middle of each, and a room above and below it.
        foreach (RingSide side in new[] { RingSide.West, RingSide.East })
        {
            bool west = side == RingSide.West;
            double inner = west ? block.WestInnerX : block.X1;
            double outer = west ? block.X0 : block.EastInnerX;
            double mid = (block.Y0 + block.Y1) / 2.0;
            foreach ((double lo, double hi) in RingSegments(
                block.Y0, block.Y1, [mid], CorridorHalf))
            {
                // #817 · …and the two ends are premium suites too: they look at the green out of one whole
                // wall, so they get the same door onto it the near band's do. It is also what keeps their
                // street face sane — see DoorsForFrontage: a 19 du end block with two leaves in it is a
                // face with nowhere to stand.
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, side, inner, lo, outer, hi, block, gate: true,
                    plateOverride: null, landscape: landscape, labs: labs));
            }
        }

        // ── (4) THE GATES · one spur per crossing, and the park's wall opened where it arrives.
        //
        //    THE HALL OPENS ONTO ONE OF THEM. #751's law is that the hall never cuts a door: the gaps in
        //    the corridor beside it ARE its doors, and it publishes the very slots the corridor was cut at.
        //    That corridor used to be a rib and is a gate now — so the gate's side wall is swept with the
        //    hall's own published openings taken out of it, and the room keeps every door it had. Watched go
        //    red without this: "the door at (-1.5,-177.4) is one the hall knows about and the deck plan does
        //    not", 208 of them, on every floor with a block on it.
        var abutting = new List<SurfaceLayout.Doorway>();
        foreach (SurfaceLayout.Doorway o in hall is { } withDoors ? withDoors.Openings : [])
        {
            if (Math.Abs(o.X1 - o.X2) < 0.001)
            {
                abutting.Add(o);

                // …and PUBLISHED, in the same list every other door down here is in, so the deck hangs its
                // imported leaf on it and an audit can find it without knowing anything about halls. This is
                // the job AddRoomsAlong used to do for the hall's own column (#751) and the column is a gate
                // now, so the gate does it. Watched go red without this: "the door at (-1.5,-177.4) is one
                // the hall knows about and the deck plan does not — nothing would be drawn there."
                if (!found)
                {
                    doorways.Add(o);
                }
            }
        }

        foreach (double sx in block.SpurXs)
        {
            RingGate(walls, doorways, claimed, gates, sx, sf, block.Y1, vertical: true, abutting);
            RingGate(walls, doorways, claimed, gates, sx, block.BackStreetY1, block.Y0, vertical: true);
        }
        double midY = (block.Y0 + block.Y1) / 2.0;
        RingGate(walls, doorways, claimed, gates, midY, block.WestInnerX, block.X0, vertical: false);
        RingGate(walls, doorways, claimed, gates, midY, block.EastInnerX, block.X1, vertical: false);

        return ring;
    }
}
