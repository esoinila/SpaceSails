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
        Specimen? Specimen = null)
    {
        /// <summary>#1063 · The preserved doorway on this floor, where this floor keeps one — which is the
        /// listed bottom of a filled ground and no other floor in the game.</summary>
        public Specimen? TheSpecimen => Specimen;

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
    /// captain can learn it and come back for the door they could not open.</summary>
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
        ParkBlock? blockOn = HasParkBlock(bodyId, level) ? BlockOn(field) : null;

        var ribXs = new System.Collections.Generic.List<(double X, bool Down)>();
        // #801 · The x's come from RibColumnsOn now — the same list the second car is placed against, so a
        // car and a corridor can never disagree about where the corridors are. Which WAY each one runs is
        // still this floor's own seeded business.
        //
        // #813 · …on every floor but ONE. On the block's floor the lower half of the field IS the block, all
        // of it, so a column runs DOWN if and only if the block uses it as a gate through the ring, and
        // every other column runs UP into the ordinary grid. That is not the seed being overruled for
        // convenience: a rib running down anywhere else would arrive in the middle of somebody's office,
        // and the seeded direction is a fact about a floor with two open halves.
        foreach ((int ordinal, double rx) in RibColumnsOn(field))
        {
            bool ribDown = Frac(bodyId, $"hive:{level}:rib-dir:{ordinal}") < 0.62;
            if (blockOn is { } gated)
            {
                ribDown = false;
                foreach (double sx in gated.SpurXs)
                {
                    ribDown |= Math.Abs(sx - rx) < 0.001;
                }
            }
            ribXs.Add((rx, ribDown));
        }

        // #587 · The ribs, exactly as built, published on the plan. This used to be taken HERE, ahead of the
        // two alcoves being appended to the list below, because an alcove is a mouth in a wall and not a
        // corridor anybody walks down. #819 · Nothing is appended any more — the alcoves cut their own spans
        // — so the two lists are the same ribs said twice, and this one is the one the plan is published in.
        var ribList = new List<Rib>(ribXs.Count);
        foreach ((double rx, bool rdown) in ribXs)
        {
            ribList.Add(new Rib(rx, rdown));
        }

        // ── #819 · AN ALCOVE'S MOUTH IS CUT TO THE ALCOVE'S OWN WIDTH ────────────────────────────────────
        //
        // Owner, on B1 at GOODS CAR 2: "the elevator here has little gaps to the wall." He was reading a
        // seam with two authors on it. Both alcoves used to be APPENDED INTO `ribXs` — they were mouths in a
        // face, so the rib list looked like the place to say so — and everything that comes in by that door
        // is cut at rx ± CorridorHalf, 3.5 du, because that is what a rib is. But the alcove BOX either side
        // of the car stands at ± ShaftHalf, 3.0 du. The face therefore ended half a du outboard of the wall
        // it was supposed to meet, on each side of each car, on both faces, on every floor: ~11 px of
        // daylight between a lift and its own wall at playtest zoom, which is exactly what he saw.
        //
        // A CORRIDOR's width governing a SHAFT's mouth — the "one constant governing the wrong thing" class
        // this file keeps a table of, and the fix is the one #775 already found for the hall's front door:
        // the alcove hands the sweep a SPAN at its own width, the way the hall's and the ring's doors hand
        // theirs at DoorHalf. One sorted list, one cursor, #587's law untouched — and the rib list goes back
        // to holding nothing but ribs, so nothing downstream has to do arithmetic to recognise an alcove.
        var alcoveMouths = new List<(double Y, double Lo, double Hi)>
        {
            (shaftY + CorridorHalf, shaftX - ShaftHalf, shaftX + ShaftHalf),
        };

        // #801 · …and the GOODS CAR's alcove, as a mouth in the LOWER face at the blind end of the corridor.
        // Two cars on one face would read as one machine room; on opposite faces, at opposite ends, they read
        // as two ways out, which is the whole of the feature.
        double? serviceX = ServiceShaftAt(field) is { } car ? car.X : null;
        if (serviceX is { } sx2)
        {
            alcoveMouths.Add((shaftY - CorridorHalf, sx2 - ShaftHalf, sx2 + ShaftHalf));
        }

        // #1063 · …and, on a ground the neighbours have filled in, the recess the old door is kept in. Cut
        // HERE because it is a mouth in the spine's own face and the sweep below is what carries it, and
        // because it must claim its ground before any room placer runs. On every site nobody has buried —
        // which is every site in almost every world — this returns having done nothing at all.
        CarveSpecimen(bodyId, level, field, walls, alcoveMouths, claimed);
        Specimen? specimen = SpecimenOn(bodyId, level, field);

        // #1074 · …and, on a ground whose deep working the Authority has closed, the recess the SEAL stands
        // in. The same pocket, cut here for the same two reasons, and it may be the same pocket because a
        // ground is stopped or buried and never both (StopOrder.TheOfficeGetsThisOne). What differs is what
        // is at the back of it: #1063 keeps a preserved doorway with nothing written on it, and this hangs a
        // leaf that will not open with an office's stamp on it. On every site nobody has stopped — which is
        // every site in almost every world — this returns having done nothing at all.
        CarveStopSeal(bodyId, level, field, walls, alcoveMouths, claimed, locked);

        // #719 · …and THE SECOND WAY OUT, at the other blind end of the same corridor. Cut here with its two
        // neighbours for the same two reasons — it is a mouth in the spine's own face, and it must claim its
        // ground before any room placer runs — and its own placer is written to refuse the ends those two
        // stand in, so a stair can never be cut through a preserved doorway or a stop order's seal. On every
        // floor below the listed bottom this returns having done nothing at all: the building files a
        // means-of-escape drawing for the floors it admits to and for no others.
        CarveStair(bodyId, level, field, walls, alcoveMouths, claimed, doorways, labels);

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

        // One face of the spine, built as segments that stop either side of every mouth cut into it.
        void SpineFace(double y, Func<double, bool, bool> cutHere)
        {
            // #587 · A CURSOR THAT WALKS A LINE MUST BE GIVEN THE LINE IN ORDER.
            //
            // This is the third bug on this wall and the first one that was invisible from the plan: the
            // geometry was right, the mouths were right, and the WALLS BETWEEN THEM were built by a cursor
            // sweeping left to right over a list that was not sorted left to right. `ribXs` holds the ribs in
            // ascending x (they are Lerped in order) and then the lift alcove APPENDED at the end, at the
            // shaft's own x — which on this field sits left of the right-most rib.
            //
            // So the sweep ran out to the far rib, advanced the cursor past it, then met the alcove behind it
            // and emitted a segment from cursor BACK to the alcove's near edge: one long wall lying across
            // everything between the two, re-sealing both mouths it had just been asked to open. The A*
            // audit reported it as the two room columns beside the right-most rib plus the lift itself —
            // and it only ever happened when that rib pointed UP, because the alcove is only cut into the
            // top face, which is exactly the pattern #587 recorded and could not explain.
            //
            // RibFace already sorts its cuts for precisely this reason. Both faces sort now, and the cursor
            // can only ever move forward — so an overlapping pair of mouths degrades to one wide mouth
            // rather than to a wall.
            //
            // #775 · …and a mouth is a SPAN now rather than a centre, because the two things cut into this
            // wall are no longer the same width: a rib mouth is the corridor's own CorridorHalf and a hall's
            // front door is DoorHalf, the number every other door in the building is cut to. One sweep, one
            // sorted list of spans, and the cursor still only ever moves forward.
            //
            // #819 · …and the alcoves come in as spans of their own now rather than as entries in the rib
            // list, so `ribXs` is ribs and nothing else and this loop cuts corridors only. The sort stays
            // where #587 put it for the reason #587 gave: a door span may arrive from any of four lists and
            // none of them owes this cursor an x order.
            var mouths = new List<(double Lo, double Hi)>();
            foreach ((double rx, bool down) in ribXs)
            {
                if (cutHere(rx, down))
                {
                    mouths.Add((rx - CorridorHalf, rx + CorridorHalf));
                }
            }
            foreach ((double ay, double lo, double hi) in alcoveMouths)
            {
                if (Math.Abs(y - ay) < 0.001)
                {
                    mouths.Add((lo, hi));
                }
            }
            if (Math.Abs(y - hallSpineFaceY) < 0.001)
            {
                mouths.AddRange(hallSpineCuts);
                mouths.AddRange(spineMouths);
                foreach ((double lo, double hi, double _, string _) in ringSpineCuts)
                {
                    mouths.Add((lo, hi));
                }
            }
            mouths.Sort((a, b) => a.Lo.CompareTo(b.Lo));

            double cursor = left;
            foreach ((double lo, double hi) in mouths)
            {
                double near = Math.Max(cursor, lo);
                if (near > cursor)
                {
                    walls.Add(new(cursor, y, near, y, true));
                }
                cursor = Math.Max(cursor, hi);
            }
            walls.Add(new(cursor, y, right, y, true));
        }

        // BOTH ends shut. The missing right-hand cap is the "open end" itself.
        walls.Add(new(left, shaftY - CorridorHalf, left, shaftY + CorridorHalf, true));
        walls.Add(new(right, shaftY - CorridorHalf, right, shaftY + CorridorHalf, true));
        // #605 · The floor's name used to be pinned 26 du off down the spine, which is most of a screen
        // from the only thing that tells you which floor you are on. It is painted at the LIFT now
        // (HiveInterior), stacked under the depth, so the plate and the number are read together.

        // ── THE SHAFT. Same spot on every floor.
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf, shaftX - ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX + ShaftHalf, shaftY + CorridorHalf, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf + 5, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));

        // ── #801 · AND THE SECOND ONE, the same box mirrored onto the lower face. Same spot on every floor,
        //    for the cage's own reason: a car a captain has to look for twice is a car they will not use.
        //    Claimed as well as built — the ground it stands on is past the last rib's chambers, so nothing
        //    was ever going to be laid here, and the ledger says so rather than leaving it to arithmetic.
        if (serviceX is { } carX)
        {
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf, carX - ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX + ShaftHalf, shaftY - CorridorHalf, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf - 5, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            claimed.Add((
                carX - ShaftHalf - 1.5, shaftY - CorridorHalf - 6.5,
                carX + ShaftHalf + 1.5, shaftY - CorridorHalf));
        }
        // #605 · The "LIFT" plate is gone from here. The console at the car mouth is already labelled LIFT,
        // and the signage stack above it (HiveInterior) now answers the bigger question in the same wall
        // space. Three plates on one wall is a wall nobody reads.

        // ── #677 · HOW BIG THE CHAMBERS ARE ON THIS FLOOR, decided ONCE and handed to both builders.
        //
        // The wall builder and the room builder must be given the same number for the same reason they are
        // already given the same centres function (#585): the doorway a room cuts and the gap its corridor
        // leaves are one gap, and two copies of a scale would open a door onto a wall on every floor of every
        // hall in the game.
        //
        // CAPPED BY THE GROUND, not by a guess. Two ribs' facing room columns must not meet, so the widest a
        // chamber may grow is half the closest rib spacing this field actually produced, less the corridor it
        // opens off. Below that the claim ledger would simply drop rooms — correct, and silent, which is the
        // shape of bug this file's spec opens with a table of.
        double roomScale = RoomScaleOn(bodyId, level);
        if (roomScale > 1.0 && ribList.Count > 1)
        {
            double closest = double.MaxValue;
            for (int i = 1; i < ribList.Count; i++)
            {
                closest = Math.Min(closest, ribList[i].X - ribList[i - 1].X);
            }
            double widest = (closest / 2.0) - CorridorHalf;
            roomScale = Math.Min(roomScale, Math.Max(1.0, widest / RoomWidthDu));
        }

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

        // #759 · The park's own glazing, kept apart from the poured walls all the way out of this method.
        // See CarveHall: one segment, in the list that says what it is MADE OF, and the client turns it back
        // into a wall the eye reads as glass and the boots read as wall.
        var glass = new List<SurfaceLayout.Wall>();

        if (blockOn is { } block)
        {
            Comfort use = HallUseOn(bodyId, level);
            double mouth = block.SpineFaceY, far = block.Y1;

            // ── WHICH SUB-SEGMENT OF THE BAND · #751's rule, unchanged, asked of the block's own list
            //    (RingNearSegments) rather than of the room slots down a rib: the best FLOOR wins and
            //    nearest-the-cage breaks the tie. The hall takes the whole of the segment it stands in
            //    unless what is left over would still make a room, and never leaves a strip too narrow to
            //    be one — the owner's "not unused" applied to the ground the biggest room in the building
            //    does not want.
            var wide = new List<Rib> { new(block.WestInnerX - CorridorHalf, true) };
            double wanted = HallGround(
                bodyId, use, wide, 0, +1, mouth, far, shaftX, serviceX, left, right + 400.0, roomScale)
                is { } asked ? asked.Wanted : 0.0;

            double bestLo = double.NaN, bestW = 0, bestD2 = double.MaxValue;
            foreach ((double lo, double hi) in RingNearSegments(block))
            {
                double span = hi - lo;
                double w = span <= wanted || span - wanted < RingRoomMinDu ? span : wanted;
                var pseudo = new List<Rib> { new(lo - CorridorHalf, true) };
                if (HallGround(
                        bodyId, use, pseudo, 0, +1, mouth, far, shaftX, serviceX,
                        left, lo + w + HallEdgePadDu, roomScale)
                    is null)
                {
                    continue;   // this segment will not take a hall, and that is a real answer
                }
                double d2 = ((lo + (w / 2.0)) - shaftX) * ((lo + (w / 2.0)) - shaftX);
                if (w > bestW + 0.5 || (w > bestW - 0.5 && d2 < bestD2))
                {
                    (bestLo, bestW, bestD2) = (lo, Math.Max(w, bestW), d2);
                }
            }

            if (!double.IsNaN(bestLo))
            {
                var stand = new List<Rib> { new(bestLo - CorridorHalf, true) };
                hallSite = CarveHall(
                    walls, glass, hallSpineCuts, bodyId, level, stand, 0, +1, mouth, far,
                    shaftX, serviceX, left, bestLo + bestW + HallEdgePadDu, roomScale, glazed: true);
            }

            if (hallSite is { } built)
            {
                // #775 · WHICH FACE OF THE SPINE THE HALL'S FRONT DOORS ARE CUT IN — the carve's own mouth,
                // and never a second opinion about it. It is the whole near band's face now.
                hallSpineFaceY = mouth;
                claimed.Add((
                    built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                    built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));

                // ── #813 · THE RING, AND THEN THE GREEN IN THE MIDDLE OF IT ────────────────────────────
                //
                // In this order because the ring owns the park's whole boundary: every wall the park has is
                // a room's glass or a gate's stub, so the green is what is left when the block has been
                // built rather than a box with openings cut in it afterwards.
                var parkGates = new List<SurfaceLayout.Doorway>();
                ring = CarveRing(
                    walls, glass, doorways, labels, claimed, ringSpineCuts, spineMouths, parkGates,
                    bodyId, level, block, built.Hall);

                park = CarvePark(
                    walls, bodyId, level, built.Hall, built.Glass!.Value, block, parkGates, ring);
            }
        }
        else
        {
            // ── EVERY OTHER FLOOR, UNCHANGED · the staff mess two hundred metres down stands on a rib's
            //    room column exactly as it has since #751, and there is no park behind it to glaze.
            hallSlot = HallSlotFor(
                bodyId, level, ribList, field, shaftX, serviceX, shaftY, left, right, roomScale);
            if (hallSlot is { } slot)
            {
                (double hmouth, double hfar) = RibReach(field, shaftY, ribList[slot.Rib].Down, hall: true);
                hallSite = CarveHall(
                    walls, glass, hallSpineCuts, bodyId, level, ribList, slot.Rib, slot.Side, hmouth, hfar,
                    shaftX, serviceX, left, right, roomScale, glazed: false);
                if (hallSite is { } built)
                {
                    hallSpineFaceY = hmouth;
                    claimed.Add((
                        built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                        built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));
                }
                else
                {
                    hallSlot = null;   // the ground would not take one. It keeps its ordinary canteen.
                }
            }
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
        SpineFace(shaftY + CorridorHalf, (_, down) => !down);
        SpineFace(shaftY - CorridorHalf, (_, down) => down);

        // #775 · THE FRONT DOORS, PUBLISHED FROM THE VERY LIST THE WALL WAS CUT FROM. #585's one-gap law
        // with no room left for a second opinion: the segments above stop at these spans and the leaves
        // below are drawn across them, off one list, in one method.
        //
        // …and nothing is drawn past the seam, exactly as AddRoomsAlong has it: a gallery has no door in it,
        // only a way through, so the gap is cut and no imported leaf is hung in it.
        if (!IsFound(bodyId, level))
        {
            // …and the plate goes on the CORRIDOR side of each of them, which is the whole point of the
            // feature: a walker on the spine is told what the wall beside them is before they have to
            // wonder. The first cut is the entrance (it was placed at the lift), the rest are the doors a
            // code put there and they say so.
            //
            // BESIDE the door and never over it, on the side the walker is coming from. A plate centred on
            // its own doorway is a plate with the captain standing on top of it the moment they arrive —
            // watched happen in the browser on the first boot of ?frontdoor=1, the dot sitting squarely on
            // the word CANTEEN — and a sign you have to step off to read is not signage.
            double plateY = hallSpineFaceY > shaftY ? hallSpineFaceY - 2.0 : hallSpineFaceY + 2.0;
            double aside = DoorHalf + 3.0;
            for (int d = 0; d < hallSpineCuts.Count; d++)
            {
                (double lo, double hi) = hallSpineCuts[d];
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));

                double cx = (lo + hi) / 2.0;
                double plateX = Math.Clamp(
                    cx + (cx > shaftX ? -aside : aside),
                    hallSite is { } signed ? signed.Hall.X0 : cx,
                    hallSite is { } bounded ? bounded.Hall.X1 : cx);
                labels.Add(new(plateX, plateY,
                    d == 0
                        ? HallEntrancePlate(bodyId, HallUseOn(bodyId, level))
                        : HallEgressPlate(d + 1)));
            }

            // #813 · …and the near band's other doors, out of the very same list the wall was swept from.
            // Their plates are the building's own vocabulary rather than the venue's, and they go on the
            // corridor side for the reason the hall's do: a walker on the spine is told what the wall beside
            // them is before they have to wonder.
            //
            // #817 · …and a room with three of them is stencilled ONCE. The carve hands the plate over with
            // the FIRST of a room's cuts and hands the rest over blank, so a landscape office that earned
            // more leaves does not also earn three copies of its own name across one wall.
            foreach ((double lo, double hi, double plateX, string plate) in ringSpineCuts)
            {
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));
                if (plate.Length > 0)
                {
                    labels.Add(new(plateX, plateY, plate));
                }
            }
        }

        // #775 · AND THE FREIGHT DOOR, HUNG. The shutter is a locked door like every other door down here
        // that will not open — drawn shut, walled behind, and its plate is what [E] reads — so the refusal
        // is TOLD in the grammar the building already speaks, and no new client idiom was invented for it.
        if (hallSite is { } withFreight && withFreight.Hall.Freight is { } hoist)
        {
            locked.Add(new(
                hoist.Shutter.X1, hoist.Shutter.Y1, hoist.Shutter.X2, hoist.Shutter.Y2, hoist.Plate));
            labels.Add(new(hoist.PlateX, hoist.PlateY, FreightSign));
        }

        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them.
        for (int i = 0; i < ribXs.Count; i++)
        {
            // #819 · There is no longer a test here for "is this entry actually an alcove". There cannot be
            // an alcove in this list: the cars hand their mouths to the wall sweep as spans of their own
            // width, and `ribXs` holds ribs. A skip kept out of caution would be a line asserting the
            // opposite of what the list above it says.
            (double x, bool down) = ribXs[i];
            if (blockOn is not null && down)
            {
                // #813 · The block's own gates. Their walls, their claim and the doorway at the end of them
                // were laid by CarveRing, because the walls either side of a gate ARE the party walls of the
                // rooms it runs between — one wall, laid once, by whichever placer owns both of its faces.
                // A second pass over them here is exactly the two-authors-one-line bug this file opens with
                // a table of.
                continue;
            }
            // #759 · The hall's rib runs longer than the rest, and this is the same call the carve made —
            // never a second answer. `hallRib` is also the one rib in the building whose far end is a WAY IN
            // rather than an end: the park is behind it.
            bool hallRib = hallSlot is { } onThis && onThis.Rib == i;
            (double mouth, double far) = RibReach(field, shaftY, down, hall: hallRib);

            // #585 · THE RIB'S OWN WALLS ARE CUT WHERE ROOMS OPEN OFF THEM. Owner: "a door is missing here
            // towards down", and his A* suggestion found it everywhere at once — 94 floors, not one room
            // reachable.
            //
            // The rooms cut a doorway in their OWN corridor-facing face, at x ± CorridorHalf. The rib's side
            // wall runs down that exact line. So every door in the building opened onto a wall: the plan drew
            // a facility and the collision field was a set of sealed boxes beside a sealed tube. Two walls on
            // one line, each correct on its own, and neither aware of the other — the same shape as every
            // expensive bug on this ground.
            // #822 · …and the rooms are laid FIRST now, because the face has one more kind of gap in it and
            // only the room placer knows where those fell. Nothing else about the order changes: the two
            // builders touch different walls, and the one wall they share is the one this hands over.
            (List<(double Lo, double Hi)> minusMouths, List<(double Lo, double Hi)> plusMouths) =
                AddRoomsAlong(
                    walls, doorways, locked, rooms, ensuites, claimed, bodyId, level, i, x, mouth, far, down,
                    roomScale, hallSlot is { } taken && taken.Rib == i ? taken.Side : 0);

            RibFace(walls, x - CorridorHalf, mouth, far, bodyId, level, i, -1, down, roomScale, minusMouths);
            RibFace(walls, x + CorridorHalf, mouth, far, bodyId, level, i, +1, down, roomScale, plusMouths);

            // The rib's far end. #585: it is ALWAYS closed — by a sealed door with a distance on it, or by a
            // plain wall. It was 40/60 before, and a corridor that simply stops in mid-air is the same
            // topology bug one level down ("a door is missing here towards down").
            //
            // #677 · NEVER a sealed mouth in the halls. `⟶ SECTOR 7 · 2.4 km` is a plate somebody stencilled,
            // and a stencil is a department, a survey and a decision about where somebody's authority stops.
            // Down here the passage simply ends in the same material as everything else, and the captain gets
            // no number to reason with — which is worse, and is the point.
            //
            // #759 · …EXCEPT THE ONE THAT IS THE PARK GATE. On the hall's rib, where a park was carved, the
            // corridor does not end: it opens, through a doorway cut to the same DoorHalf every other door
            // in the building is cut to, and that gap is the ONLY way a body gets into the park. (The wall
            // it shares with the hall is glass — an eye crosses it and nothing else does.) A sealed mouth
            // with a distance stencilled on it here would be a sign lying about a door you can see through.
            //
            // #775 · …AND IT IS NO LONGER THE ONLY ONE. Owner: "let's have multiple doors to the park — it
            // is a kind of place people like to walk through on their way." Every rib pointing the park's
            // way now runs the extra HallRibExtraDu that the hall's rib always ran and opens where it
            // arrives, so a route down one rib and up another crosses the green instead of going round it.
            // The extension is corridor, not room: the chambers were laid against `far` and nothing stands
            // in the sixteen du beyond it — that is the same unused band the park itself came out of.
            // #813 · Every rib that gets this far runs away from the block, so its far end is an end.
            // The one that used to be a way in is a gate through the ring now, and it never reaches here.
            if (!IsFound(bodyId, level) && Frac(bodyId, $"hive:{level}:rib-far:{i}") < 0.55)
            {
                double km = 0.8 + (Frac(bodyId, $"hive:{level}:rib-km:{i}") * 3.4);
                locked.Add(new(x - CorridorHalf, far, x + CorridorHalf, far,
                    SealedMouthSign(bodyId, i, km)));
            }
            walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, true));

            // #751 · …and on the column the hall is standing on, no rooms at all. The rib's own face is
            // still built above (RibFace), with its doorway at every slot — those gaps ARE the hall's doors,
            // and they are the same gaps the corridor has because nothing ever cut a second set.
        }

        // #608 · LAST, because a refuge is taken out of the rooms this floor actually managed to build. Any
        // earlier and it would be a designated INDEX rather than a designated ROOM — and the claim ledger
        // above drops a room whenever one would sit on something already standing, so an index chosen before
        // the loop is an index that sometimes names nothing. That is exactly the shape of the bug KeyRoomFor
        // was written to avoid, and a safety regulation may not be the second thing in this file to trip
        // over it.
        // #707 · …and the amenities, out of the same pool and BEFORE the refuge, so the two can never take
        // the same room. They never compete in practice — an amenity is only ever plumbed on a floor that
        // holds pressure and a refuge is only ever carved on one that does not — but the order says so
        // rather than leaving it to be rediscovered.
        // #813 · …and the ring's own rooms join the pool BEFORE either of them, which is #775's amenity
        // gradient cashed out: a washroom on the park side of the block is a washroom with a window, and
        // "amenities follow rank" means the best room in the building is a candidate for the best of them.
        // The back of house is NOT in here — it goes in below, after both have chosen, for #801's reason.
        foreach (RingRoom room in ring)
        {
            if (room.Side != RingSide.Far)
            {
                // #818 · …carrying the furniture #817 already stood in it. The suites were furnished at the
                // carve and the sweep reads ONE list for the whole building, so what a ring room holds is
                // handed over here rather than looked up again through the park — a second reader of the
                // same fact is a second answer waiting to disagree with the first.
                rooms.Add(new Room(
                    room.X0, room.Y0, room.X1, room.Y1, room.Plate, room.WaysOut, RoomKind.RingSuite,
                    room.Furniture, room.Seats));
            }
        }

        // #822 · THE BUILDING AS CARVED, taken here — before the amenities and the refuge start REMOVING
        // rooms from the pool. A canteen is a chamber with a counter in it and a refuge is a chamber with a
        // tank in it: both are still rooms a captain stands in, both still have to have two ways out, and a
        // sweep read off the pool after they had taken theirs would quietly stop asking about them.
        var published = new List<Room>(rooms);

        List<Amenity> amenities = CarveAmenities(bodyId, level, rooms, walls, shaftX, shaftY, hallSite);
        List<Refuge> refuges = CarveRefuges(bodyId, level, rooms, field);

        // #801 · …and the park's back of house LAST of all, appended after both of those have chosen. They
        // are rooms — they hold what any room down here holds and the A* audit walks to every one of them —
        // but they are the garden's, and an amenity or a refuge carved out of one would be the building
        // taking back the thing this feature exists to give: somewhere on the far side of the green.
        if (park is not null)
        {
            // #813 · …asked of the RING rather than of Park.Rooms, which is the #801 view of it and holds
            // only the ones with a door onto the gravel. The far band's two CORNER rooms stand past the end
            // of the park's own wall, so they have no gravel door and are not back rooms in #801's sense —
            // and reading the narrower list here left two rooms on every block floor with a door cut, a
            // plate hung and nothing behind them. Watched go red: "34 doors were cut and only 28 of them
            // lead anywhere."
            foreach (RingRoom room in ring)
            {
                if (room.Side == RingSide.Far)
                {
                    var back = new Room(
                        room.X0, room.Y0, room.X1, room.Y1, room.Plate, room.WaysOut, RoomKind.RingSuite,
                        room.Furniture, room.Seats);
                    rooms.Add(back);
                    published.Add(back);
                }
            }
        }

        // #822 · …and the rooms that were never in the pool at all, because nothing may ever take them: the
        // hall, the cabinets down its outer wall, the WC cubicles in the block's washroom and the en-suite
        // cells hung off the principal chambers. The last three are the exemption the law is written with —
        // a booth and a cell are bedroom-small (see <see cref="FireCodeSmallRoomDu"/>) and keep their single
        // leaf, which is exactly what #821's catch is bolted to.
        if (hallSite is { } venue)
        {
            Hall theHall = venue.Hall;
            published.Add(new Room(
                theHall.X0, theHall.Y0, theHall.X1, theHall.Y1,
                AmenitySigns(bodyId, HallUseOn(bodyId, level)).Item1, theHall.Openings, RoomKind.Hall));
            foreach (Cabinet cab in theHall.Cabinets)
            {
                published.Add(new Room(
                    cab.X - cab.HalfW, cab.Y - cab.HalfH, cab.X + cab.HalfW, cab.Y + cab.HalfH,
                    cab.Plate, cab.Ways, RoomKind.Cabinet));
            }
        }
        foreach (RingRoom room in ring)
        {
            foreach (RingOffice.Stall cell in room.Cubicles)
            {
                published.Add(new Room(
                    cell.X0, cell.Y0, cell.X1, cell.Y1, cell.Plate, [cell.Door], RoomKind.Cubicle));
            }
        }
        foreach (EnSuite cell in ensuites)
        {
            published.Add(new Room(
                cell.X - (EnSuiteDepth / 2.0), cell.Y - EnSuiteHalfHeight,
                cell.X + (EnSuiteDepth / 2.0), cell.Y + EnSuiteHalfHeight,
                cell.Of, cell.Ways, RoomKind.Cell));
        }

        // ── #818 · AND WHAT IS STANDING ON THE FLOOR OF EVERY ONE OF THEM ────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor."
        //
        // HERE, and not down in AddRoomsAlong, for the reason RingOffice is called last in the ring's carve:
        // a placer that ran before the recesses were cut would be measuring its clearances against a wall
        // with no holes in it yet, and #822's fire doors are the newest holes in this building. By this line
        // every way out of every room is published and the furnisher can be handed the finished box.
        //
        // The solids go into the SAME wall list every other piece of furniture down here goes into (the
        // park's raised beds, the en-suite's pan, the ring's desks) so one segment is both the drawing and
        // the collision — and it happens BEFORE the bins, which measure their own clearance against every
        // wall the floor ended up with and would otherwise fit a bin inside a fume hood.
        var cellLeaves = new List<SurfaceLayout.Doorway>(ensuites.Count);
        foreach (EnSuite cell in ensuites)
        {
            foreach (SurfaceLayout.Doorway leaf in cell.Ways)
            {
                // An en-suite's leaf is NOT a way out (a cell is a dead end, and the fire code says so), so
                // it is not in Room.Ways — and it is still a hole a body goes through, which is the only
                // thing the furnisher needs to know about it. Handed over as a floor-wide list rather than
                // matched to a parent: a leaf in somebody else's wall is too far away to claim any of this
                // room's line, so the conservative reading costs nothing and cannot mis-pair.
                cellLeaves.Add(leaf);
            }
        }

        string? department = ChamberFitting.DepartmentOn(bodyId, level);
        Kind trade = KindOn(bodyId, level);

        // …counted PER KIT as the floor is walked, which is what turns "a laboratories floor has fume hoods,
        // vacuum chambers and furnaces on it" from a probability into a fact. See ChamberFitting.Fit's
        // ordinal: a seeded pick left fifteen floors in the sweep short of one piece each.
        var dealt = new Dictionary<ChamberFitting.Kit, int>();
        for (int r = 0; r < published.Count; r++)
        {
            Room carved = published[r];
            if (carved.Kind != RoomKind.Chamber)
            {
                continue;   // the suites, the hall, its cabinets and the cells are furnished by their own
            }

            var holes = new List<SurfaceLayout.Doorway>(carved.Ways.Count + cellLeaves.Count);
            holes.AddRange(carved.Ways);
            holes.AddRange(cellLeaves);

            ChamberFitting.Kit trade0 = ChamberFitting.KitFor(carved.Plate, department, trade);
            int ordinal = dealt.TryGetValue(trade0, out int seen) ? seen : 0;
            dealt[trade0] = ordinal + 1;

            RingOffice.Furnishing kit = ChamberFitting.Fit(in carved, trade0, holes, ordinal);
            if (kit.Fixtures.Count == 0)
            {
                continue;   // a gallery, an empty store, or a room whose every wall is a doorway's clearance
            }

            published[r] = carved with { Fittings = kit.Fixtures, Chairs = kit.Chairs };
            foreach (SurfaceLayout.Wall solid in kit.Solids)
            {
                walls.Add(solid);
            }
        }

        // #853 · …and the conference posters, on the one department they are about. Hung off the chambers'
        // own doorways, so a floor whose rooms move takes its wall dressing with it.
        IReadOnlyList<LabPosters.Poster> posters =
            LabPosters.On(bodyId, level, published, shaftX, shaftY);

        // #864 · …and the incident board, on ONE lab chamber's own wall. A poster is signage for somebody
        // walking past a door; a safety board hangs INSIDE the room it is about, so it is placed by the
        // furnishing law (ChamberFitting's measured walls) rather than the signage one — handed every hole
        // on the floor, because an opening in somebody else's wall is too far away to claim any of the line
        // and the conservative list cannot mis-pair.
        var everyHole = new List<SurfaceLayout.Doorway>(cellLeaves);
        foreach (Room carved in published)
        {
            everyHole.AddRange(carved.Ways);
        }
        IncidentBoard.Board? board =
            IncidentBoard.On(bodyId, level, published, everyHole, shaftX, shaftY);

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
            glass, park, bins, published, posters, board, specimen);
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
