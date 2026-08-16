using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #759 · THE PARK BEHIND THE BAR ───────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-06 night: "Let's go Vault Tech fancy and have a view to an underground park behind the
    // bar, windows between… a recreation device made to squeeze more out of their workers." And then, from
    // a cruise ship two days later, the scale: "the reference is the ship's Central Park — the meeting place
    // the cafeterias and restaurants ring… Do not make the park a puny small closet. Make it too big."
    //
    // WHY IT IS A ROOM AND NOT A PICTURE. The bar's stool view and the hall's own establishing art are both
    // shot THROUGH the glass at green — so the moment those pictures shipped, the deck plan owed the player
    // a room on the other side of that wall. A backdrop with nothing behind it is the drawn world and the
    // simulated world disagreeing, which is the bug class this file keeps a table of.
    //
    // WHERE THE GROUND CAME FROM. Every rib in the building stops RibReachDu off the spine and the field
    // runs on for sixty du past that — a band the width of the base that no placer has ever put anything
    // in. The hall's rib now reaches HallRibExtraDu further into it (the owner's "at least double or triple
    // it"), and the park is the rest of it: as wide as the spine is long, which makes it several times the
    // floor area of the hall and the largest single room in the game.
    //
    // AND THE WALL BETWEEN IS GLASS, which is the one geometric fact the whole feature turns on: sight
    // crosses it, bodies do not, and the way in is a door at the end of a corridor somewhere else.

    /// <summary>#759 · One raised growing bed in the park — a solid box on the plan, stencilled with what is
    /// in it and where it goes.</summary>
    /// <param name="Number">1-based, as the plate reads.</param>
    /// <param name="X">Centre.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="HalfW">Half-width of the box.</param>
    /// <param name="HalfH">Half-height.</param>
    /// <param name="Crop">What is growing in it, in the building's own shouting stencil voice.</param>
    public readonly record struct GrowingBed(
        int Number, double X, double Y, double HalfW, double HalfH, string Crop)
    {
        /// <summary>Is this spot inside the bed? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) =>
            Math.Abs(x - X) <= HalfW && Math.Abs(y - Y) <= HalfH;

        /// <summary>What is stencilled on the end of it. The crop, and the room it is going to — which is
        /// the whole of the food connection: the counter's sign is CANTEEN 1 and so is this.</summary>
        public string Plate => $"🌱 BED {Number} · {Crop} · TO {ParkBedDestination}";
    }

    /// <summary>#801 · A room on the far side of the park — the back of house, entered off the gravel.
    ///
    /// <para>Its own box, its own door, its own plate, published for the same reason
    /// <see cref="Hall.Openings"/> is: "the far gates lead somewhere real" is a law about a list, and a law
    /// about a list nobody keeps is a law nobody can fail.</para></summary>
    /// <param name="Door">The gap cut in the park's far wall. It is the room's ONLY door — the band behind
    /// the park is the last of the field, and there is no corridor back there for a second one.</param>
    public readonly record struct BackRoom(
        double X0, double Y0, double X1, double Y1, SurfaceLayout.Doorway Door, string Plate)
    {
        /// <summary>The middle of it — where the search console stands and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Is the captain in it?</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>#759 · The park: the box, the walks through it, and what is standing in it.</summary>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Walk">The gravel walks, as the centre-line the ground was cleared along — the gate spur
    /// first, then the long curve. PUBLISHED because it is what makes the park a place you stroll rather
    /// than a lawn you look at: the beds are laid around it, and a guard walks every metre of it.</param>
    /// <param name="Beds">The raised beds, which are solid.</param>
    /// <param name="Benches">Steel benches beside the walk, one per bend.</param>
    /// <param name="Masts">Floodlight masts — the artificial day, as posts on the plan.</param>
    /// <param name="Gate">The doorway into it, as the segment across the opening.</param>
    /// <param name="Window">The glazed wall it shares with the hall, as the segment along it.</param>
    /// <param name="X">Where the park's own plate reads from — just inside the gate.</param>
    /// <param name="Y">The same.</param>
    /// <param name="FigureX">The lone figure on the far bench. Scenery: a plate at a coordinate, with
    /// nothing to press and nothing to say.</param>
    /// <param name="FigureY">The same.</param>
    /// <param name="FigurePlate">What the figure is, at plate size — one of <see cref="CanteenRegulars"/>'
    /// own strangers, seeded off the site so a park is the same park every time it is walked. Chosen here
    /// rather than in the renderer for the reason every string on these rooms is chosen here, and for one
    /// more: a client picking it out of the list with <c>string.GetHashCode</c> would pick a DIFFERENT one
    /// on every process start, and a guard run in the same process would never see it.</param>
    /// <param name="ArtUrl">The picture the floor of it WEARS — same seam as <see cref="Hall.ArtUrl"/>,
    /// laid in panels (<see cref="ParkArtPanels"/>) because one photograph stretched over a room six times
    /// wider than it is deep is a photograph nobody can read.</param>
    public readonly record struct Park(
        double X0, double Y0, double X1, double Y1,
        IReadOnlyList<(double X, double Y)> Walk,
        IReadOnlyList<GrowingBed> Beds,
        IReadOnlyList<(double X, double Y)> Benches,
        IReadOnlyList<(double X, double Y)> Masts,
        SurfaceLayout.Doorway Gate,
        SurfaceLayout.Wall Window,
        double X, double Y, double FigureX, double FigureY, string FigurePlate = "",
        string? ArtUrl = null,
        IReadOnlyList<SurfaceLayout.Doorway>? Gates = null,
        IReadOnlyList<BackRoom>? Back = null,
        IReadOnlyList<RingRoom>? Ring = null)
    {
        /// <summary>
        /// #813 · THE ROOMS WITH THE VIEW, all the way round — the near band's suites, the back of house,
        /// and the two end blocks, in that order.
        ///
        /// <para><b>The HALL is not one of them, and the omission is deliberate.</b> It is a ring room in
        /// every way that matters — its doors are gaps in the spine's face, its far wall is glass on the
        /// green — and it is already published twice: as this floor's <see cref="Amenity"/> and, for the
        /// wall itself, as <see cref="Window"/>. A third entry would be the same room in a third list, and
        /// a caller summing frontage or counting rooms off two of the three would get a different answer
        /// depending which two. So the ring is <i>the rooms the ring carved</i>, the hall is the hall, and
        /// anything measuring the park's whole perimeter unions the two out loud (see
        /// <c>TheParkIsTheCentreOfTheBlockTests.EveryDuOfTheFrontageIsARoomOrAGate</c>).</para>
        ///
        /// <para>Never null, so a caller asking "what faces the park" cannot mistake an empty ring for a
        /// missing one — and on the one floor that has a park, an empty ring is a bug rather than an
        /// answer.</para></summary>
        public IReadOnlyList<RingRoom> Frontage => Ring ?? [];

        /// <summary>
        /// #801 · WHAT IS ON THE OTHER SIDE, and the reason the far wall stopped being a horizon.
        ///
        /// <para>Owner, 2026-08-09: <i>"we could have rooms to explore below the park also (on the map).
        /// Walking through the park is fun, it should not be the edge."</i> He is describing a map problem
        /// and it was a real one: #775 made the green a thoroughfare between corridors, and a thoroughfare
        /// with a painted wall along one whole side is still a room you cross rather than a place you are
        /// IN. The back of house is what a park of this size actually has behind it — potting, soil, feed,
        /// a cold room the counter draws on — and it is the one row of doors in the building a captain
        /// reaches by walking across a garden.</para>
        ///
        /// <para>They are ordinary rooms: they appear in <see cref="FloorPlan.RoomCentres"/>, they hold what
        /// any room down here holds, and the A* audit that walks every room from the car walks these. What
        /// makes them the park's is only where their doors are.</para></summary>
        public IReadOnlyList<BackRoom> Rooms => Back ?? [];

        /// <summary>
        /// #801 · The doors OFF THE GRAVEL, in the ring's own order. Kept out of <see cref="Ways"/> on
        /// purpose: a Way is a way THROUGH the park, corridor to corridor, and these are ways OUT OF it into
        /// a room. The distinction is not decorative — the amenities' conservation sum counts a Way as a
        /// place and would count these twice.
        ///
        /// <para>#817 · IT IS READ OFF <see cref="Frontage"/> NOW, not off <see cref="Rooms"/>. The far
        /// band's back doors used to be the only openings in the park's boundary that led into a room, so
        /// "the back rooms' doors" and "the doors off the gravel" were the same list; the owner's ruling
        /// that every premium suite gets a door onto the green made them two different lists, and the one
        /// this property has always MEANT is this one. The sealing experiment in
        /// <c>TheParkIsWalkableTests</c> plugs whatever is in here and says out loud that anything adding a
        /// way onto the green belongs in it — reading the narrower list would have left that guard passing
        /// while proving nothing.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> BackDoors
        {
            get
            {
                var doors = new List<SurfaceLayout.Doorway>(Frontage.Count);
                foreach (RingRoom r in Frontage)
                {
                    if (r.Gate is { } onto)
                    {
                        doors.Add(onto);
                    }
                }
                return doors;
            }
        }

        /// <summary>
        /// #775 · EVERY WAY IN, and there is more than one now.
        ///
        /// <para>Owner, 2026-08-09: <i>"let's have multiple doors to the park… it is a kind of place people
        /// like to walk through on their way."</i> That is what a central park is FOR — the crossing, not
        /// the visit — and #790 shipped it with one gate at the end of one corridor, which makes it a
        /// destination and a cul-de-sac.</para>
        ///
        /// <para><see cref="Gate"/> is still the hall's own — the first of these, and the one the room's
        /// plate and its dev route are pinned to. This is all of them, published for the same reason
        /// <see cref="Hall.Openings"/> is: a law about how a room is entered cannot be written against a
        /// list nobody keeps.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Ways => Gates ?? [Gate];

        /// <summary>Is the captain in the park? The box the walls were laid on — the hall's own law
        /// (<see cref="Hall.Contains"/>), for the same reason: a refuge-sized containment box in a room this
        /// size would say "you are not in the park" from almost everywhere in the park.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>How much floor it has. The owner's "do not make it a puny small closet", in the one
        /// unit a guard can measure.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#759 · Where the beds' produce goes, and it is the sign over the counter that serves it —
    /// <see cref="AmenitySigns"/>'s own CANTEEN 1, said by the thing that grows the food. That is the whole
    /// of the connection and it needs no sentence: the bed and the till name the same room.</summary>
    public const string ParkBedDestination = "CANTEEN 1";

    /// <summary>#759 · What the beds are growing, in the order they are laid. The card under the glass on
    /// the counter (#756) sells coffee, a fry-up and a stew whose ingredients are "sourced from as far down
    /// as we are willing to say" — every one of these is one of those things, standing in soil ten metres
    /// from the table it is served at, and nothing anywhere points that out.</summary>
    public static readonly IReadOnlyList<string> ParkCrops =
    [
        "TABLE GREENS",
        "STEW ROOT",
        "BREAKFAST TOMATO",
        "SOFT HERBS",
        "SALAD STOCK",
        "COFFEE · SIX TREES · TRIAL",
    ];

    /// <summary>#759 · What is stencilled at the gate. The owner's own two phrases, in the inspectorate
    /// voice the issue asks for: a company that builds a park underground is squeezing morale like any
    /// other ore, and it does not pretend otherwise on the sign.</summary>
    public const string ParkPlate =
        "🌳 THE PARK · RECREATION SCHEDULE POSTED · ATTENDANCE IS RECORDED";

    /// <summary>#759 · What the field book keeps of a walk in the park — filed once per excursion, the
    /// cabinet's own idiom (<see cref="CabinetNote"/>). Authored, verbatim.
    ///
    /// <para>The surveillance is a LINE and not a system, which is the whole restraint of the beat: the
    /// plate says attendance is recorded, the book records that it said so, and nothing anywhere counts
    /// anything. §13.8 holds — the park says what the KITCHEN is for and never once what the facility
    /// is.</para></summary>
    public const string ParkNote =
        "An indoor park behind the canteen's glass: gravel walks, raised beds under grow-lamps, and a "
        + "plate at the gate that says attendance is recorded. The beds are stencilled for the counter — "
        + "including the stew the card sources from as far down as they are willing to say, which is "
        + "growing ten metres from the table it is served at.";

    /// <summary>#759 · The glyph the park's filed line wears.</summary>
    public const string ParkGlyph = "🌳";

    /// <summary>#759 · What a bench is, on the plan. Steel, bolted, and a seat — the seat verb is #778's and
    /// arrives with it; this is the furniture it will arrive at.</summary>
    public const string ParkBenchPlate = "🪑 A STEEL BENCH";

    /// <summary>#759/#793 · Half the length of one, in deck units — the segment the carve bolts down and
    /// therefore the segment a body collides with. PUBLISHED because #793 makes the bench a SEAT WITH TWO
    /// ENDS (<see cref="ParkBenches"/>): where you sit down, where somebody else can sit, and how far apart
    /// the two are, are all this number. A caller measuring it off a screenshot would be doing geometry
    /// about furniture it did not bolt down (§13.15).</summary>
    public const double ParkBenchHalfDu = 1.8;

    /// <summary>#759 · The picture the park's floor wears. The owner's shotcrete ruling applies to it and it
    /// is the regenerated shot: <i>"the crude rock is not up to modern mining smooth spray concrete
    /// specs."</i></summary>
    public const string ParkArtUrl = "art/b1-park-walk.jpg";

    /// <summary>#759 · Which hall has a park behind it, asked exactly the way <see cref="HallArtFor"/> asks
    /// which halls get a picture — because it is the same question. The branch office's upper canteen is the
    /// room whose own art is shot through a window wall at the green; the head office's dining room is a
    /// different room in a different building with its own everything (#411), and the staff mess two
    /// hundred metres down has no view of anything.
    ///
    /// <para>ONE predicate, asked by the carve and by the paint, so a park can never be laid on a floor that
    /// then declines to paint it — or, worse, painted on a floor that has no room behind the glass.</para></summary>
    public static bool HasPark(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId);
    }

    /// <summary>#813 · Does this floor get the block? The one floor that gets a park, asked exactly the way
    /// <see cref="HasPark"/> asks it, because it is the same question — a block with no park in the middle
    /// of it is a ring of offices around a hole.</summary>
    public static bool HasParkBlock(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHallFloor(bodyId, level) && HasPark(bodyId, HallUseOn(bodyId, level));
    }

    /// <summary>#759 · Which picture the park's floor wears, or null where there is no park.</summary>
    public static string? ParkArtFor(string bodyId, Comfort use) =>
        HasPark(bodyId, use) ? ParkArtUrl : null;

    /// <summary>#759 · The park's floor art, cut into panels across its own box.
    ///
    /// <para>The hall wears ONE picture because a hall is about as wide as a photograph is. The park is six
    /// times wider than it is deep — the owner's "make it too big" — and the same seam used once would
    /// stretch a 16:9 frame to 6:1 and turn a garden into a smear. So the law is published HERE, beside the
    /// box, for the reason every other number on these rooms is: a renderer working out how many copies of
    /// a picture go on a floor would be doing geometry about a room it does not own.</para></summary>
    public static IReadOnlyList<(double X0, double Y0, double X1, double Y1)> ParkArtPanels(in Park park)
    {
        double w = park.X1 - park.X0, h = park.Y1 - park.Y0;
        int panels = Math.Max(1, (int)Math.Round(w / (h * ParkArtAspect), MidpointRounding.AwayFromZero));
        var cut = new List<(double, double, double, double)>(panels);
        for (int i = 0; i < panels; i++)
        {
            cut.Add((
                park.X0 + (i * w / panels), park.Y0,
                park.X0 + ((i + 1) * w / panels), park.Y1));
        }
        return cut;
    }

    /// <summary>#759 · The shape of the frames this set is painted in — 1280 × 720, every one of them.</summary>
    public const double ParkArtAspect = 16.0 / 9.0;

    /// <summary>#759 · Half the width of a gravel walk. Comfortably wider than <see cref="DoorHalf"/>, for
    /// the reason DoorHalf itself was widened: a path narrower than a couple of the reachability flood's
    /// grid steps is a path that is open in the geometry and shut to anything that pathfinds.</summary>
    public const double ParkWalkHalfDu = 3.5;

    /// <summary>#759 · The promenade kept clear against the park's own walls, so the walk is never the only
    /// way across and a bed is never laid against the glass.</summary>
    public const double ParkEdgeClearDu = 5.0;

    /// <summary>#759 · Half a raised bed, across the park and along it.</summary>
    public const double ParkBedHalfWDu = 7.0;

    /// <summary>#759 · Half a raised bed, the short way.</summary>
    public const double ParkBedHalfHDu = 3.5;

    /// <summary>#759 · How many bends the long walk takes between the two ends. Curved is the owner's word
    /// — <i>"It must be WALKABLE, with curved paths … the curve that hides the far end"</i> — and a curve on
    /// a deck plan is a run of walkable ground whose beds were laid around it.</summary>
    public const int ParkWalkBends = 3;

    /// <summary>#759/#801 · How many floodlight masts stand against the far wall. It was a literal 5 inside
    /// the carve; it is named because the back of house is laid in the BAYS BETWEEN them (#801) and a second
    /// copy of the count would have put a door in front of a lamp post.</summary>
    public const int ParkMastCount = 5;

    /// <summary>#801 · Where the masts stand along the park, as a pure function of its box. One answer, asked
    /// by the carve that erects them and by the carve that lays rooms between them.</summary>
    public static IReadOnlyList<double> ParkMastXs(double x0, double x1)
    {
        double uLo = x0 + ParkEdgeClearDu, uHi = x1 - ParkEdgeClearDu;
        double span = uHi - uLo;
        var xs = new List<double>(ParkMastCount);
        for (int k = 0; k < ParkMastCount; k++)
        {
            xs.Add(uLo + (span * (k + 0.5) / ParkMastCount));
        }
        return xs;
    }

    // ── #801 · THE BACK OF HOUSE, ON THE FAR SIDE OF THE GREEN ────────────────────────────────────────────
    //
    // Owner, 2026-08-09: "we could have rooms to explore below the park also (on the map). Walking through
    // the park is fun, it should not be the edge."
    //
    // WHERE THE GROUND CAME FROM, written down because it is the whole engineering answer and it is not
    // obvious. The park is already the biggest room in the game and its size is a LAW — it must stand
    // ParkDepthDu deep and hold half again the floor of the hall behind it, and on the shipped field the
    // second of those binds at 38.3 du of the 42 it has. So the band beyond it could not be bought by making
    // the park shallower: there is 3.7 du of slack in the whole feature and a room needs twelve.
    //
    // It is bought instead from the LAST STRIP OF THE FIELD. The park's far wall clamps at
    // BottomY + EdgeMargin, and the edge margin is a SURFACE law — the half-lane the regolith generator
    // keeps clear so nothing is drawn into the #563 falloff. There is no falloff on a floor with a roof on
    // it: a Hive deck publishes no unseen wall at all, so nothing down here fades and nothing is clipped.
    // The band is 16.5 du of the field's own envelope that no floor has ever used, and a chamber module is
    // twelve. The one law that DOES bind is the envelope itself (`ItNEVERLeavesTheSurfacesOwnEnvelope`), and
    // the back wall is laid inside it with rock to spare.

    /// <summary>#801 · How deep a back-of-house room is. The facility's own chamber module
    /// (<see cref="RoomHeightDu"/>) and not a number of its own: these are ordinary rooms that happen to be
    /// entered off a garden.</summary>
    public static double ParkBackDepthDu => RoomHeightDu;

    /// <summary>#801 · How much rock is left between the back wall and the end of the field. Small on
    /// purpose — this is the last strip of the world and the building is meant to read as having used it —
    /// but never zero, because a wall standing exactly on the envelope is a wall one rounding error outside
    /// it.</summary>
    public const double ParkBackRockDu = 3.0;

    /// <summary>#801 · The pier of rock left between two back rooms.</summary>
    public const double ParkBackPierDu = 8.0;

    /// <summary>#801 · What is stencilled beside the doors in the far wall.
    ///
    /// <para>§13.8, and this row is a soft place to break it: a room behind a garden is one sentence away
    /// from being a room about what the garden was really for. Every one of these says what the KITCHEN and
    /// the GROUNDS are for — the same restraint the beds are stencilled with — and not one of them is about
    /// the facility. The cold room names CANTEEN 1 because the beds already do, which is the entire food
    /// connection and is still never pointed out.</para></summary>
    public static readonly IReadOnlyList<string> ParkBackPlates =
    [
        "🌱 POTTING · SOIL, TRAYS, GRIT",
        "🧰 GROUNDS PLANT · LAMPS, FEED, TIMERS",
        "❄ COLD ROOM · TO CANTEEN 1",
        "🧤 GROUNDS STORE · TOOLS SIGNED OUT AND BACK",
        "🚿 WASH-DOWN",
        "📋 GROUNDS OFFICE · ROTA POSTED",
    ];

    /// <summary>
    /// #759 · THE PARK, CARVED — the one room in the building that is not a box off a corridor.
    ///
    /// <para>Laid in the park's own two axes, the hall's discipline: <b>u</b> runs along the spine and
    /// <b>w</b> runs outward from the wall it shares with the hall. It owns three walls and half of a
    /// fourth: the far wall, the two ends, and the near wall in the segments left over once the corridor's
    /// gate and the hall's GLASS have been taken out of it. Neither of those two openings is cut here —
    /// the gate is the rib's own far end (the corridor stops being a dead end) and the glass is the hall's
    /// own far wall — which is #585's one-gap law said about a room that has two neighbours.</para>
    ///
    /// <para><b>The walk comes first and the planting is laid around it</b>, which is the whole reason a
    /// park drawn on a square grid can have a curve in it. The centre-line is a smooth line the length of
    /// the room; a bed is only laid where it clears that line by a walk's half-width and then some. Do it
    /// the other way — beds first, path threaded after — and the path is whatever the beds left, which is
    /// how a garden becomes a maze and how a guard stops being able to fail.</para>
    /// </summary>
    private static Park CarvePark(
        List<SurfaceLayout.Wall> walls, string bodyId, int level, in Hall hall, SurfaceLayout.Wall glass,
        in ParkBlock block, IReadOnlyList<SurfaceLayout.Doorway> gates, IReadOnlyList<RingRoom> ring)
    {
        // ── #813 · THE BOX IS THE BLOCK'S. Every one of the park's four walls belongs to a room or to a
        //    gate (CarveRing), so nothing here pours a single boundary segment: what is left for this method
        //    is the GROUND — the walk, the planting, the benches, the masts, and the one figure sitting at
        //    the far end of it.
        //
        //    That is a real narrowing and it is the point. The park used to own three of its own walls and
        //    half of a fourth, which meant the shape of the room and the shape of the fabric around it were
        //    two answers to one question; now the fabric IS the shape, and a wall the ring did not lay is a
        //    wall the park does not have.
        double x0 = block.X0, x1 = block.X1;
        double y0 = block.Y0, y1 = block.Y1;
        double depth = y1 - y0;
        double W(double w) => y1 - w;

        // WHICH GATE THE ROOM'S PLATE AND ITS DEV ROUTE ARE PINNED TO: the one on the hall's own side of
        // the green, nearest the bar, which is the gate a drinker walks out of. #775's rule, said about a
        // wall that now has six openings in it instead of two.
        SurfaceLayout.Doorway first = gates[0];
        double bestD = double.MaxValue;
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (Math.Abs(g.Y1 - y1) > 0.001 || Math.Abs(g.Y2 - y1) > 0.001)
            {
                continue;   // not on the near wall: a gate off the back street or one of the two ends
            }
            double d = Math.Abs(((g.X1 + g.X2) / 2.0) - ((hall.X0 + hall.X1) / 2.0));
            if (d < bestD)
            {
                (first, bestD) = (g, d);
            }
        }
        double gateX = (first.X1 + first.X2) / 2.0;

        // ── THE WALK · one long curve down the room, and a spur in from the gate.
        double uLo = x0 + ParkEdgeClearDu, uHi = x1 - ParkEdgeClearDu;
        double span = uHi - uLo;
        double mid = depth / 2.0;
        double amp = Math.Max(1.0, mid - ParkEdgeClearDu - ParkWalkHalfDu - 2.0);
        double Curve(double u) =>
            mid + (amp * Math.Sin(2 * Math.PI * ParkWalkBends * (u - uLo) / span));

        var walk = new List<(double X, double Y)>();
        double gateU = Math.Clamp(gateX, uLo, uHi);
        for (double w = 0; w < Curve(gateU); w += 1.5)
        {
            walk.Add((gateU, W(w)));      // in from the gate, until it meets the long walk
        }
        for (double u = uLo; u <= uHi + 0.001; u += 1.5)
        {
            walk.Add((u, W(Curve(u))));
        }

        // ── THE BEDS · a grid of raised boxes, minus every one that would stand on the walk.
        var beds = new List<GrowingBed>();
        double bu0 = uLo + ParkBedHalfWDu, bu1 = uHi - ParkBedHalfWDu;
        double bw0 = ParkEdgeClearDu + ParkBedHalfHDu, bw1 = depth - ParkEdgeClearDu - ParkBedHalfHDu;
        // #813 · The grid is a du tighter both ways than the band-shaped park's was. The green kept its area
        // when it became a block and it lost most of its LENGTH, and a planting pitch measured for a room
        // six times wider than it is deep leaves a garden with six beds in it — which is what the first
        // Manhattan carve produced, and the number a guard measures rather than a paragraph.
        int cols = Math.Max(1, (int)((bu1 - bu0) / ((2 * ParkBedHalfWDu) + 5)) + 1);
        int rows = Math.Max(1, (int)((bw1 - bw0) / ((2 * ParkBedHalfHDu) + 2)) + 1);
        double clearU = ParkBedHalfWDu + ParkWalkHalfDu + 1.0;
        double clearW = ParkBedHalfHDu + ParkWalkHalfDu + 1.0;

        // #813 · …and minus every one that would stand in front of a GATE. There are six of them now and
        // they arrive on all four sides, so "the ground in front of a door is clear" stopped being a thing
        // the old single gate got for free off the gate spur's own walk. A bed across a doorway is the
        // shape of bug this file keeps a table of: drawn correct, walked shut.
        var mouths = new List<(double X, double Y)>(gates.Count);
        foreach (SurfaceLayout.Doorway g in gates)
        {
            mouths.Add(((g.X1 + g.X2) / 2.0, (g.Y1 + g.Y2) / 2.0));
        }

        // …and the back of house's own doors onto the gravel (#801) are mouths in this wall exactly as the
        // gates are. They are not in `gates` — a Way is a way THROUGH the park and these are ways OUT of it
        // into a room, a distinction Park.BackDoors has kept since #801 — and leaving them out of THIS list
        // is what put a floodlight mast in front of two of them on every floor in the game. Watched go red.
        foreach (RingRoom room in ring)
        {
            if (room.Gate is { } gravel)
            {
                mouths.Add(((gravel.X1 + gravel.X2) / 2.0, (gravel.Y1 + gravel.Y2) / 2.0));
            }
        }

        for (int r = 0; r < rows; r++)
        {
            double bw = rows == 1 ? (bw0 + bw1) / 2.0 : bw0 + (r * (bw1 - bw0) / (rows - 1));
            for (int c = 0; c < cols; c++)
            {
                double bu = cols == 1 ? (bu0 + bu1) / 2.0 : bu0 + (c * (bu1 - bu0) / (cols - 1));

                // Would it stand on the gravel? Asked of the walk the room actually published, sample by
                // sample — never of the formula, which is the same discipline as measuring the tops rather
                // than reading the seat target.
                bool blocked = false;
                foreach ((double px, double py) in walk)
                {
                    double pw = Math.Abs(py - y1);
                    if (Math.Abs(px - bu) < clearU && Math.Abs(pw - bw) < clearW)
                    {
                        blocked = true;
                        break;
                    }
                }

                double by = W(bw);
                foreach ((double mx, double my) in mouths)
                {
                    blocked |= Math.Abs(mx - bu) < ParkBedHalfWDu + ParkWalkHalfDu + 1.0
                        && Math.Abs(my - by) < ParkBedHalfHDu + ParkWalkHalfDu + 1.0;
                }
                if (blocked)
                {
                    continue;
                }

                // ── #874 · AND IT IS SOLID, WHICH IS WHAT THE ART HAS ALWAYS SAID IT WAS.
                //
                //    It was four rails. Four rails round a box fourteen deck units by seven leave a
                //    12.6 × 5.6 du pocket of perfectly standable floor with no way in — 275 lattice squares
                //    a body fits on and no route on this floor can end in, nine times over, in every park
                //    in the game. Nobody could ever SEE that: the bed is DRAWN as a filled box and the
                //    owner's own complaint about it (#866) was that his finger would not take him there.
                //
                //    So it is laid with the one thing in this codebase that means SOLID —
                //    SurfaceLayout.AddSolidMass, #586's own answer to exactly this on the monolith, where a
                //    sealed cavity in a slab of stone read as 99 cells of ground nobody could reach. Same
                //    outline, to the coordinate; the inside simply stops being a place.
                SurfaceLayout.AddSolidMass(walls,
                    bu - ParkBedHalfWDu, by - ParkBedHalfHDu,
                    bu + ParkBedHalfWDu, by + ParkBedHalfHDu, true);

                beds.Add(new GrowingBed(
                    beds.Count + 1, bu, by, ParkBedHalfWDu, ParkBedHalfHDu,
                    ParkCrops[beds.Count % ParkCrops.Count]));
            }
        }

        // ── THE BENCHES · one at every bend, on the outside of it, where a bench goes.
        var benches = new List<(double X, double Y)>();
        for (int k = 0; k < 2 * ParkWalkBends; k++)
        {
            double u = uLo + (span * (0.25 + (k * 0.5)) / ParkWalkBends);
            double w = Curve(u);
            double off = w > mid ? ParkWalkHalfDu + 1.6 : -(ParkWalkHalfDu + 1.6);
            double by = W(w + off);
            benches.Add((u, by));
            walls.Add(new(u - ParkBenchHalfDu, by, u + ParkBenchHalfDu, by, true));
        }

        // ── THE MASTS · the artificial day, standing along the far pavement. The far wall used to be the
        //    edge of the world and is a row of shop fronts now, so a mast is STEPPED ASIDE where one would
        //    otherwise stand in front of somebody's door — nudged, never dropped: the artificial day is what
        //    makes an underground garden legible and a park with two lamps in it is a car park.
        var masts = new List<(double X, double Y)>();
        double mastW = depth - (ParkEdgeClearDu / 2.0);
        foreach (double at in ParkMastXs(x0, x1))
        {
            double my = W(mastW), u = at;
            foreach ((double mx, double gy) in mouths)
            {
                if (Math.Abs(gy - my) >= DoorHalf + ParkEdgeClearDu || Math.Abs(mx - u) >= DoorHalf + 2.0)
                {
                    continue;
                }
                double step = DoorHalf + 2.0 + 0.1;
                u = mx + (mx <= (x0 + x1) / 2.0 ? step : -step);
            }
            u = Math.Clamp(u, x0 + 1.0, x1 - 1.0);
            masts.Add((u, my));
            walls.Add(new(u - 0.6, my - 0.6, u + 0.6, my - 0.6, true));
            walls.Add(new(u - 0.6, my + 0.6, u + 0.6, my + 0.6, true));
            walls.Add(new(u - 0.6, my - 0.6, u - 0.6, my + 0.6, true));
            walls.Add(new(u + 0.6, my - 0.6, u + 0.6, my + 0.6, true));
        }

        // ── #801/#813 · THE BACK OF HOUSE, which is now the ring's FAR band. It is published in both
        //    shapes on purpose and it is ONE set of rooms: Park.Rooms is the view #801's own consumers were
        //    written against (a box, a door off the gravel, a plate) and Park.Frontage is the whole ring.
        //    A second carve for the second shape is the mirrored-constant bug with a record's clothes on.
        var back = new List<BackRoom>();
        foreach (RingRoom room in ring)
        {
            if (room.Side == RingSide.Far && room.Gate is { } gravel)
            {
                back.Add(new BackRoom(room.X0, room.Y0, room.X1, room.Y1, gravel, room.Plate));
            }
        }

        // The lone figure, on the bench furthest from the gate. Scenery: the owner's own "benches, the lone
        // figure, the curve that hides the far end", and nothing to press — a park that started offering
        // things would be a park that had noticed you.
        (double figX, double figY) = benches[0];
        foreach ((double bx, double by) in benches)
        {
            if (Math.Abs(bx - gateX) > Math.Abs(figX - gateX))
            {
                (figX, figY) = (bx, by);
            }
        }

        // #775 · Every gate as a published doorway, the hall's own FIRST — it is the one the room's plate
        // and its dev route are pinned to, and the order is the only thing that says which is which.
        var ordered = new List<SurfaceLayout.Doorway>(gates.Count) { first };
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (g != first)
            {
                ordered.Add(g);
            }
        }

        return new Park(
            x0, y0, x1, y1, walk, beds, benches, masts,
            first,
            glass,
            gateX, W(4.0), figX, figY,
            CanteenRegulars.StrangerPlates[
                (int)(Frac(bodyId, $"hive:{level}:park-figure") * CanteenRegulars.StrangerPlates.Count)
                    % CanteenRegulars.StrangerPlates.Count],
            ParkArtFor(bodyId, HallUseOn(bodyId, level)),
            ordered,
            back,
            ring);
    }

    /// <summary>#775 · What is stencilled at the mouth of the walk down to the park — the arrow idiom this
    /// building already paints on a corridor whose end is somewhere else
    /// (<see cref="SealedMouthSign"/>). The difference is the whole point: that one names a place you will
    /// never reach, and this one is a way through.</summary>
    public const string ParkWaySign = "⟶ THE PARK";

    /// <summary>
    /// #775 · WHERE THE DEDICATED WALK DOWN TO THE PARK IS CUT, or null where this floor has no room for
    /// one.
    ///
    /// <para>Owner: <i>"multiple doors to the park"</i> — and the corridors that already reach it are the
    /// ribs pointing its way, which on a quarter of the shipped sites is exactly ONE (the hall's own). A
    /// park with one door is a cul-de-sac however many ribs happen to fall the right way, so the building
    /// gets a passage whose only job is that crossing: off the main corridor, straight down, into the
    /// green.</para>
    ///
    /// <para>It is placed at the point on the spine's park-side face FURTHEST from everything already
    /// standing on that side — the hall's box, the corridors that reach the park, and the lift alcove where
    /// the park is on the alcove's own face. Furthest rather than first-fit because the room columns either
    /// side of a rib are ground this passage would otherwise take: the claim ledger would drop them
    /// silently, and a floor quietly losing chambers is the shape of bug this file keeps a table of.</para>
    /// </summary>
    private static double? GardenWalkX(
        List<Rib> ribs, bool parkSide, in Hall hall, double shaftX, double? serviceX,
        double leftEnd, double rightEnd)
    {
        var keepOff = new List<(double Lo, double Hi)> { (hall.X0, hall.X1) };
        foreach (Rib r in ribs)
        {
            if (r.Down == parkSide)
            {
                keepOff.Add((r.X - CorridorHalf, r.X + CorridorHalf));
            }
        }
        if (!parkSide)
        {
            keepOff.Add((shaftX - ShaftHalf, shaftX + ShaftHalf));   // the alcove hangs off the top face
        }
        else if (serviceX is { } carX)
        {
            // #801 · …and on the other face the goods car stands, at the blind end — which is precisely
            // where this max-min search likes to land, because the emptiest x on a face is very often the
            // last one. Without this clause the walk down to the green and the second car would have been
            // cut into the same six du of wall.
            keepOff.Add((carX - ShaftHalf, carX + ShaftHalf));
        }

        double clear = CorridorHalf + 2.0;
        // …and it keeps a wall's worth of ground off the ends of the building, which the max-min search
        // would otherwise walk straight into: the emptiest x on this face is very often the last one.
        double lo = leftEnd + clear + CorridorHalf, hi = rightEnd - clear - CorridorHalf;
        double bestX = double.NaN, bestRoom = clear;

        for (double x = lo; x <= hi + 0.001; x += 1.0)
        {
            double room = double.MaxValue;
            foreach ((double a, double b) in keepOff)
            {
                room = Math.Min(room, x < a ? a - x : x > b ? x - b : 0.0);
            }
            if (room > bestRoom)
            {
                (bestRoom, bestX) = (room, x);
            }
        }
        return double.IsNaN(bestX) ? null : bestX;
    }
}
