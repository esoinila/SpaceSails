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

    /// <summary>#775 · What is stencilled at the mouth of the walk down to the park — the arrow idiom this
    /// building already paints on a corridor whose end is somewhere else
    /// (<see cref="SealedMouthSign"/>). The difference is the whole point: that one names a place you will
    /// never reach, and this one is a way through.</summary>
    public const string ParkWaySign = "⟶ THE PARK";
}
