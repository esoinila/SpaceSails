using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>One floor, laid out. Walls and doorways in the same shapes <see cref="SurfaceLayout"/> speaks,
    /// so the client lays a floor exactly the way it lays a ground.</summary>
    public readonly record struct FloorPlan(
        int Level,
        string Name,
        bool Pressurised,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Doorway> Doorways,
        IReadOnlyList<LockedDoor> Locked,
        IReadOnlyList<SurfaceLayout.Landmark> Labels,
        IReadOnlyList<(double X, double Y)> RoomCentres,
        IReadOnlyList<Rib> Ribs,
        IReadOnlyList<Refuge> Refuges,
        IReadOnlyList<Amenity> Amenities,
        IReadOnlyList<EnSuite> EnSuites,
        // #759 · THE GLAZING, kept out of Walls on purpose. Every segment here is a wall a body may not
        // pass and an eye may — the renderer puts them back into the deck in the window idiom the ship's
        // own bridge glass already uses, so one segment carries both halves and nothing draws a second one.
        IReadOnlyList<SurfaceLayout.Wall>? Windows = null,
        // #759 · The park, on the one floor that has one.
        Park? Park = null,
        // #798 · Somewhere to put a document you never want read again. Appended and never inserted: every
        // caller of this record builds it positionally.
        IReadOnlyList<RipAndBin.Bin>? Bins = null,
        // #822 · EVERY CARVED SPACE A CAPTAIN CAN STAND IN, with the holes in its own walls. The fire code
        // is swept over this and nothing else. Appended, for the reason above.
        IReadOnlyList<Room>? Rooms = null,
        // #853 · The conference posters on a laboratories floor's corridor walls. Appended, same reason.
        IReadOnlyList<LabPosters.Poster>? Posters = null,
        // #864 · THE INCIDENT BOARD, on the one lab chamber wall that carries one. Appended, same reason.
        IncidentBoard.Board? Board = null,
        // #1063 · THE PRESERVED DOORWAY at the back of the burial's recess, on the listed bottom of a ground
        // somebody filled in — and NOWHERE else. It is kept out of Walls on purpose, exactly as #759's
        // glazing is: the list a segment arrives in is what decides its ink, and this one is drawn in the
        // found band's own no-texture idiom on a floor that is otherwise entirely poured. Appended, for the
        // reason above.
        Specimen? Specimen = null,
        // #775 · THE MEETING ROOMS in a landscape floor's core — the glass boxes off the open floor, and
        // nowhere else in the game. Appended and never inserted, for the reason every optional on this
        // record is appended: every caller of it builds it positionally.
        IReadOnlyList<MeetingRoom>? Meetings = null,
        // #775 · THE RING ITSELF, on the plan rather than inside the park's own record.
        //
        // It has lived in Park.Frontage since #813, which was true while a ring only ever existed on a
        // floor with a garden in the middle of it — and that is precisely the conflation this issue undoes.
        // Every consumer that wants the rooms (the seat verb, the cubicle lock, the renderer, the counter's
        // own booking) was reaching for them THROUGH the park, so on a landscape floor they would each have
        // found nothing and said nothing. Park.Frontage is untouched and still the park's own view of its
        // frontage; this is the building's. Appended, same reason as every optional above.
        IReadOnlyList<RingRoom>? Ring = null,
        // #775 · EVERY GATE THROUGH THE RING — the crossings that arrive in the middle of the block, on
        // whichever kind of floor it is. Park.Ways is the GARDEN'S view of this same list and stays exactly
        // what it was; this is the building's, and it exists because a floor whose middle is a core of
        // meeting rooms has the same six crossings and no park to publish them. Appended, same reason as
        // every optional above.
        IReadOnlyList<SurfaceLayout.Doorway>? Crossings = null)
    {
        /// <summary>#1063 · The preserved doorway on this floor, where this floor keeps one — which is the
        /// listed bottom of a filled ground and no other floor in the game.</summary>
        public Specimen? TheSpecimen => Specimen;

        /// <summary>#775 · The meeting rooms in this floor's core, never null. Empty on every floor that is
        /// not a landscape floor, which is a true statement about them rather than a missing one.</summary>
        public IReadOnlyList<MeetingRoom> TheMeetingRooms => Meetings ?? [];

        /// <summary>#775 · Every room on this floor's ring, never null — the block's own frontage, asked of
        /// the FLOOR and not of the garden that used to be the only reason a floor had one. Empty on every
        /// floor without a block, which is a true statement about them rather than a missing one.</summary>
        public IReadOnlyList<RingRoom> TheRing => Ring ?? [];

        /// <summary>#775 · Every gate through this floor's ring, never null. Empty on every floor without a
        /// block, which is a true statement about them rather than a missing one.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> TheCrossings => Crossings ?? [];

        /// <summary>#853 · The framed posters on this floor, never null. Empty on every floor that is not a
        /// laboratories floor, which is a true statement about them rather than a missing one.</summary>
        public IReadOnlyList<LabPosters.Poster> TheWalls => Posters ?? [];

        /// <summary>#864 · The incident board, where this floor has one. At most ONE per laboratories floor
        /// and null on every other floor in the game — see <see cref="IncidentBoard.On"/>.</summary>
        public IncidentBoard.Board? TheBoard => Board;

        /// <summary>#798 · Somewhere to put a paper on this floor, never null — a caller asking "is there a
        /// bin here" must not have to tell an empty list from a missing one.</summary>
        public IReadOnlyList<RipAndBin.Bin> TheBins => Bins ?? [];

        /// <summary>#822 · Every room on this floor, never null. <see cref="RoomCentres"/> is the POOL the
        /// amenities and the refuge were drawn out of and it shrinks as they take from it; this is the
        /// building as carved, and it is the list the fire code walks.</summary>
        public IReadOnlyList<Room> TheRooms => Rooms ?? [];

        /// <summary>
        /// #820 · THE TALL SEATS ON THIS FLOOR, in the counter's own order — entry <c>s</c> is
        /// <c>Interior.TheStools</c>' stool <c>s</c>, exactly as <see cref="Hall.StoolRow"/> publishes them.
        ///
        /// <para>The row has been carved beside the counter's own segments since #792 and had one reader,
        /// the renderer, which walked the amenities to find it. The [E] press needs the same coordinates now
        /// that sitting down puts the body ON the seat, and a second walk over the same list is a second
        /// author for one row. Empty on every floor whose counter does not serve, which is a true statement
        /// about those floors rather than a missing one.</para>
        /// </summary>
        public IReadOnlyList<(double X, double Y)> TheStoolRow
        {
            get
            {
                foreach (Amenity a in Amenities)
                {
                    if (a.Hall is { } hall && hall.StoolRow.Count > 0)
                    {
                        return hall.StoolRow;
                    }
                }
                return [];
            }
        }
    }

    /// <summary>#587 · A CROSS CORRIDOR, PUBLISHED RATHER THAN INFERRED.
    ///
    /// <para>The ribs used to be a local of <see cref="Build"/>, so the only thing outside this file that
    /// could say where one was, was arithmetic that copied the placement — which is the mirrored-constant
    /// bug this ground keeps paying for. #587 was a mouth that had been cut and then walled over again, and
    /// no guard could state that in Core because no guard could name the mouth. Now it can.</para>
    ///
    /// <para><b>Down</b> means the rib runs toward the deep field, away from the landing band, and therefore
    /// opens off the spine's LOWER face; an up rib opens off the upper one. That flag is the whole reason
    /// #587 only ever struck some floors.</para></summary>
    public readonly record struct Rib(double X, bool Down);

    /// <summary>A door that never opens. The cheapest illusion of scale there is, and the owner asked for it
    /// by name — <i>"we can again use the locked doors to give the illusion of much larger space"</i>. Each
    /// carries the sign that was on it, which is what does the work: a corridor of shut doors with departments
    /// painted on them is a facility, and the same corridor with blank doors is a wall.</summary>
    public readonly record struct LockedDoor(double X1, double Y1, double X2, double Y2, string Sign);

    /// <summary>Build one floor. Pure and deterministic per (body, level): the same complex every visit, so a
    /// captain can learn it and come back for the door they could not open.
    ///
    /// <para>#251 · THIS IS A DRIVER NOW, and it was 837 lines. Eleven named passes do the work, in the order
    /// they are called here, and the ORDER IS THE ARCHITECTURE — nearly every comment in this family is a
    /// note about why one thing has to happen before another, and every one of them was paid for by a bug:
    /// the hall is carved before the rib loop because it is the only placer that cannot be refused; the
    /// spine's faces are poured after it because a wall poured first is a wall with no front door in it; a
    /// refuge is taken after the rooms are built because a refuge chosen earlier is an index that sometimes
    /// names nothing; the bins go last because a bin is fitted into a room that is already finished.</para>
    ///
    /// <list type="bullet">
    /// <item><c>.Corridors.cs</c> — <see cref="PlanTheRibs"/>, <see cref="CutTheAlcoveMouths"/>,
    /// <see cref="PourTheSpineEnds"/>, <see cref="SpineFace"/>.</item>
    /// <item><c>.Block.cs</c> — <see cref="HowBigTheChambersAre"/>, <see cref="SiteTheBlock"/>,
    /// <see cref="SiteTheHallOnARib"/>, <see cref="HangTheFrontDoors"/>.</item>
    /// <item><c>.Rooms.cs</c> — <see cref="RunTheRibs"/>, <see cref="PublishTheRooms"/>,
    /// <see cref="FurnishTheChambers"/>.</item>
    /// </list>
    ///
    /// <para>Every line of every pass body is the line that was inline, in the order it was in. Three of them
    /// came out of an <c>if</c> or a local function and are therefore their base lines with four leading
    /// spaces removed and nothing else; the other eight are byte for byte. What is new is a name, a docblock,
    /// and the fact that each pass's inputs have to be said out loud — which is the whole return on the cut:
    /// <see cref="SpineFace"/> used to be a local function closing over four lists that are filled a hundred
    /// lines after it is written and read two hundred lines after that.</para>
    ///
    /// <para>The passes that carve into a shared accumulator take it as a parameter and the ones that decide
    /// a value take it by <c>ref</c>, so the moved statements are unchanged. Nothing in this family declares
    /// a static field, so there is no initializer-order hazard here — see
    /// <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for what one would cost.</para></summary>
    public static FloorPlan Build(string bodyId, int level, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var walls = new List<SurfaceLayout.Wall>();
        var doorways = new List<SurfaceLayout.Doorway>();
        var locked = new List<LockedDoor>();
        var labels = new List<SurfaceLayout.Landmark>();

        // #707 · A ROOM CARRIES ITS OWN PLATE THROUGH THE BUILD. It used to be a bare centre, because the
        // only thing that ever asked a room what it was, was the locked door hung on it — and a room that
        // opens has never had a sign drawn on it. That is still true on screen and it stopped being true in
        // the generator the moment rank became readable in plumbing: which rooms get an en-suite, and which
        // rooms are the wrong ones to turn into a canteen, are both questions about the plate. Carried in the
        // same list rather than in a second one kept in lockstep beside it, for the obvious reason.
        var rooms = new List<Room>();
        var ensuites = new List<EnSuite>();

        // #585 · A CLAIM LEDGER, DOWN HERE TOO. The A* audit found rooms that were drawn and could not be
        // entered, and the cause is the one this project keeps paying for: two rooms (or a room and the
        // spine) laid on the same ground, each sealing the other's doorway with its own wall. Every placer
        // that writes into one space needs to see what is already in it.
        var claimed = new List<(double X0, double Y0, double X1, double Y1)>();

        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double shaftX, double shaftY) = ShaftAt(field);
        claimed.Add((left - 1, shaftY - CorridorHalf - 1, right + 1, shaftY + CorridorHalf + 1));

        // ── #585 · THE SPINE, CLOSED AT BOTH ENDS AND OPEN WHERE IT SHOULD BE.
        //
        // Owner, walking it: "see this empty tube end here... it is like I walk into the ground here" and
        // then, exactly: "this open end is a bug of topology."
        //
        // It was, and it was two bugs wearing one coat. The spine was capped on the LEFT and not on the
        // right, so walking east you left the building through the end of the corridor into open coordinate
        // space — which, drawn in the old dim ink, looked precisely like walking out into regolith. And the
        // spine's long walls ran unbroken from end to end ACROSS every rib mouth, so the cross corridors did
        // not actually open off it: the plan showed a facility and the collision said one sealed tube.
        //
        // A corridor is defined by where it does NOT have walls. Both faces are now built in segments with a
        // deliberate gap at each rib, and both ends are shut.
        // ── #813 · IS THIS THE BLOCK'S FLOOR? Decided first, because it decides which way every corridor on
        //    it runs. See the Manhattan header above ParkBlock.
        // #775 · …and it is HasBlockOn now, not HasParkBlock. The block is a ring of large rooms with a
        //    street on every side of it; the PARK is what stands in the middle of it on the one floor of a
        //    branch office with a garden. Those were one predicate until the owner asked for landscape
        //    offices on B1 AND BELOW, and the audit on #938 found that the whole of what stood between him
        //    and them was this line. See UndergroundComplex.Landscape.cs.
        ParkBlock? blockOn = HasBlockOn(bodyId, level) ? BlockOn(field) : null;

        // ── #801/#813 · WHERE THE CROSS CORRIDORS ARE. See UndergroundComplex.FloorPlan.Corridors.cs.
        (List<(double X, bool Down)> ribXs, List<Rib> ribList) = PlanTheRibs(bodyId, level, field, blockOn);

        // ── #819 · AND EVERY MOUTH IN THE SPINE'S FACES THAT IS NOT ONE OF THEM — the two cages' alcoves,
        //    and the three pockets a site's own history cuts. Claimed before any room placer runs.
        (List<(double Y, double Lo, double Hi)> alcoveMouths, double? serviceX, Specimen? specimen) =
            CutTheAlcoveMouths(
                bodyId, level, field, shaftX, shaftY, walls, claimed, locked, doorways, labels);

        // #775 · THE DOORS THE HALL CUTS IN THE SPINE'S OWN FACE, filled in by the carve below and read by
        // the wall builder — one list, so the gap the corridor leaves and the door the hall publishes are
        // the same gap. #585's law, said about the one wall the hall did not previously own a hole in.
        //
        // Owner, walking the new B1: "the bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to
        // really look for the way in; a venue's entrance should find YOU." He was right about the topology:
        // the hall's near wall IS the spine's face, and until this list existed that face ran unbroken past
        // it, so a hundred and ten du of canteen frontage on the building's only through-corridor had no
        // way in at all. You had to turn down the rib and find a gap in the side wall.
        var hallSpineCuts = new List<(double Lo, double Hi)>();
        double hallSpineFaceY = double.NaN;

        // #813 · …and the SAME wall, for the same reason, on behalf of every other room in the near band.
        // The block's premium suites front the spine exactly the way the hall does — their door is a gap in
        // this face and nothing else — so they hand their spans to the very list the wall is swept from and
        // never cut a wall of their own. Kept apart from the hall's cuts only because the plates differ:
        // the hall's first cut is the venue's entrance and the rest are its egress doors, and an office's
        // plate is the office's own.
        var ringSpineCuts = new List<(double Lo, double Hi, double PlateX, string Plate)>();

        // #775 · …and the mouths on that face that are corridors rather than doors: the walks down to the
        // park. Kept in their own list because the two are drawn differently and always were — a doorway
        // gets an imported leaf and a plate, a corridor mouth gets neither, exactly as the ribs' mouths get
        // neither.
        //
        // #813 · There were one of these and there are four: the block's two service streets and a gate
        // down each of the near band's crossings. The spine is the block's own near street now, and a street
        // that meets it has a mouth in it.
        var spineMouths = new List<(double Lo, double Hi)>();

        // ── #585/#801 · BOTH ENDS SHUT, AND THE TWO CAGE BOXES.
        PourTheSpineEnds(walls, claimed, left, right, shaftX, shaftY, serviceX);

        // ── #677 · HOW BIG THE CHAMBERS ARE ON THIS FLOOR, decided ONCE and handed to both builders.
        double roomScale = HowBigTheChambersAre(bodyId, level, ribList);

        // ── #751 · THE HALL, FIRST, BECAUSE IT IS THE ONLY PLACER THAT CANNOT BE REFUSED ────────────────
        //
        // Carved BEFORE the rib loop and claimed immediately, so everything after it — rooms, en-suites,
        // refuges — sees the box and steps around it. The alternative was to carve it last and delete the
        // walls of whatever it had swallowed, which is the same thing said in a way that can go wrong.
        //
        // #813 · …and on the block's floor it is not carved off a rib at all. It is a RING ROOM: the widest
        // sub-segment of the near band, front doors on the spine, glass on the park — which is what it has
        // been in every way but its bookkeeping since #775 hung its doors on the main corridor. What the
        // Manhattan ruling changed is that the ground either side of it is now a room too.
        HallSite? hallSite = null;
        Park? park = null;
        (int Rib, int Side)? hallSlot = null;
        var ring = new List<RingRoom>();

        // #775 · …and the crossings through it, hoisted here so the plan can publish them on both kinds of
        // floor. The park has published its own view of this list as Park.Ways since #759; a floor whose
        // middle is a core of meeting rooms has the very same gates and nothing to publish them through.
        var parkGates = new List<SurfaceLayout.Doorway>();

        // #775 · …and what stands in the middle of a block with no garden in it: the meeting rooms.
        var meetings = new List<MeetingRoom>();

        // #759 · The park's own glazing, kept apart from the poured walls all the way out of this method.
        // See CarveHall: one segment, in the list that says what it is MADE OF, and the client turns it back
        // into a wall the eye reads as glass and the boots read as wall.
        var glass = new List<SurfaceLayout.Wall>();

        if (blockOn is { } block)
        {
            SiteTheBlock(
                bodyId, level, block, walls, glass, doorways, labels, locked, claimed,
                hallSpineCuts, ringSpineCuts, spineMouths, parkGates,
                shaftX, serviceX, left, right, roomScale,
                ref hallSite, ref park, ref ring, ref meetings, ref hallSpineFaceY);
        }
        else
        {
            SiteTheHallOnARib(
                bodyId, level, ribList, field, walls, glass, hallSpineCuts, claimed,
                shaftX, serviceX, shaftY, left, right, roomScale,
                ref hallSlot, ref hallSite, ref hallSpineFaceY);
        }

        // ── #585/#775 · THE SPINE'S TWO LONG FACES, BUILT LAST OF THE THREE ──────────────────────────────
        //
        // The lift alcove hangs off the TOP face, so that face needs a mouth for it too — otherwise the car
        // opens into a sealed box and the captain cannot reach their own way out. The A* audit reported this
        // as "the lift cannot be reached from the lift", which is as clear as a guard gets.
        //
        // #775 · …and these two lines used to stand above the hall carve, which is why the hall could not
        // have a front door: the wall was already poured by the time anybody knew where the room was. They
        // are the SAME two calls, moved after the carve, reading the one list of cuts the carve filled in.
        // Nothing else between here and there touches this wall — walls are a set, not a sequence.
        //
        // #801 · …and the LOWER face now has an alcove of its own to leave a mouth for. Same clause, same
        // wall, the other way up: the goods car opens into a sealed box otherwise, which is #585's "the lift
        // cannot be reached from the lift" said about the car nobody had built yet.
        //
        // #819 · …and neither of these predicates does arithmetic on a shaft's x any more. They used to have
        // to recognise the alcove that had been appended into the rib list and let it through; the alcove
        // hands the sweep its own span now, so each of these is back to the one thing a face has to know —
        // does this corridor run MY way.
        SpineFace(shaftY + CorridorHalf, (_, down) => !down, walls, ribXs, alcoveMouths,
            hallSpineFaceY, hallSpineCuts, spineMouths, ringSpineCuts, left, right);
        SpineFace(shaftY - CorridorHalf, (_, down) => down, walls, ribXs, alcoveMouths,
            hallSpineFaceY, hallSpineCuts, spineMouths, ringSpineCuts, left, right);

        // ── #775/#813 · THE FRONT DOORS, off the list the wall was swept from, and the freight shutter.
        HangTheFrontDoors(
            bodyId, level, doorways, labels, locked, hallSpineCuts, ringSpineCuts,
            hallSpineFaceY, shaftX, shaftY, hallSite);

        // ── #585 · THE RIBS. Cross corridors off the spine, with rooms flanking them.
        RunTheRibs(
            bodyId, level, field, walls, doorways, locked, rooms, ensuites, claimed,
            ribXs, blockOn, hallSlot, shaftY, roomScale);

        // ── #822 · WHAT THE FLOOR PUBLISHES AS A ROOM, and in which order the takers get their pick.
        (List<Room> published, List<Amenity> amenities, List<Refuge> refuges) = PublishTheRooms(
            bodyId, level, field, walls, rooms, ensuites, ring, meetings, hallSite, shaftX, shaftY);

        // ── #818/#853/#864 · AND WHAT IS STANDING ON THE FLOOR OF EVERY ONE OF THEM, and on their walls.
        (IReadOnlyList<LabPosters.Poster> posters, IncidentBoard.Board? board) = FurnishTheChambers(
            bodyId, level, walls, ensuites, published, shaftX, shaftY);

        var centres = new List<(double X, double Y)>(rooms.Count);
        foreach (Room pooled in rooms)
        {
            centres.Add((pooled.X, pooled.Y));
        }

        // #798 · LAST OF ALL, THE BINS — because a bin is fitted into a room that is already finished. It
        // is the only placer in this method that has to see EVERY wall the floor ended up with (its own
        // clearance is measured against them) and every piece of furniture that was laid in it, and a
        // placer that ran earlier would be measuring a room that did not exist yet. It appends walls of its
        // own, which is why nothing below it may read `walls` again.
        // #828 · …and the RING goes in with it now, because the third rung of the disposal ladder is a
        // fixture a premium suite already stands (RingOffice.SecureDisposal): the bin is READ OFF the box
        // the furnishing published rather than placed a second time, which is the only way the plate on the
        // plan and the bucket the verb feeds can be one rectangle.
        List<RipAndBin.Bin> bins = CarveBins(
            bodyId, level, walls, doorways, centres, amenities, refuges, ribList, park, ring,
            shaftX, shaftY);

        // #1068 · …AND LAST OF ALL, THE WORLD DECLINES ONE DOOR. Taken after every placer above has laid its
        // work against a building whose doors were all open, so what the captain comes back to is the floor
        // he walked out of with one leaf shut — the poster still beside it, the canteen still where it was,
        // the room still where its centre says it is. See UndergroundComplex.Decline.cs: on every floor of
        // every site in a world where nobody has been past a seam a whole window ago it returns before it
        // builds so much as a list, which is almost every floor of almost every world.
        DeclineOneDoor(bodyId, level, doorways, locked, published, refuges, amenities);

        return new FloorPlan(level, NameOf(bodyId, level), HoldsPressure(bodyId, level),
            walls, doorways, locked, labels, centres, ribList, refuges, amenities, ensuites,
            glass, park, bins, published, posters, board, specimen, meetings, ring, parkGates);
    }

    /// <summary>#585/#751 · How far a rib reaches off the spine, and where its mouth is. ONE function,
    /// because the wall builder, the room builder and now the hall carver all have to be given the same two
    /// numbers — and this was three copies of the same two lines the moment the hall arrived.
    ///
    /// <para>#759 · <paramref name="hall"/> is the ONE rib that reaches further, and it reaches further for
    /// a reason that can be said in a sentence: the room on it is a hall. Owner, standing in the first
    /// one — <i>"it is like cramped… At least double or triple it"</i> — and a hall grown only sideways is a
    /// corridor with tables in it. The extra length comes out of the band beyond the rib ends that nothing
    /// has ever stood in, and the park takes what is left of that band.</para></summary>
    private static (double Mouth, double Far) RibReach(
        in SurfaceLayout.Field field, double shaftY, bool down, bool hall = false)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double reach = hall ? RibReachDu + HallRibExtraDu : RibReachDu;
        return down
            ? (shaftY - CorridorHalf, Math.Max(field.BottomY + margin, shaftY - reach))
            : (shaftY + CorridorHalf, Math.Min(field.LandingBandY - margin, shaftY + reach));
    }

    /// <summary>#585 · How far an ordinary rib runs off the spine. This was a literal <c>52</c> written
    /// twice inside <see cref="RibReach"/>; it is named here because #759 needed to say "further than an
    /// ordinary one" without retyping it.</summary>
    public const double RibReachDu = 52.0;

    /// <summary>#759 · How much further the HALL's own rib runs. The owner's <i>"at least double or triple
    /// it"</i> is a floor-AREA ask and floor area has two axes; this is the second one, and it is spent on
    /// ground the generator has never used — every rib in the building stops <see cref="RibReachDu"/> off
    /// the spine while the field runs on for another sixty du past that.</summary>
    public const double HallRibExtraDu = 16.0;

    /// <summary>#759/#813 · How deep the park is, measured from its own near wall (the hall's glass)
    /// outward. Stated as a number so the park's area law has something to be measured against rather than
    /// a coordinate.
    ///
    /// <para>#813 · It grew by two du when the Manhattan ruling turned the band into a block. That reads
    /// like a rounding error and it is not: the park lost most of its WIDTH — it no longer runs from one end
    /// cap of the spine to the other, because a room that runs to the end caps has ends nobody can build
    /// against — and every du of the depth budget freed by moving the hall's band and the back of house
    /// into one ring went back into the green. What is left of the field beyond the block is the rock the
    /// back street is cut in.</para></summary>
    public const double ParkDepthDu = 45.0;
}
