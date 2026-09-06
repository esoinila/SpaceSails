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
public static partial class HiveInterior
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
    /// <param name="cabinetsDogged">#758 · The cabinets whose padded leaf is out of the wall and dogged, by
    /// <see cref="CabinetPrivacy.Key"/>. ABSENT MEANS CURTAIN — the state every cabinet is in until somebody
    /// decides otherwise — so a caller with nothing to say draws the building as it stands. The plan carries
    /// it as one glyph on the cabinet's own plate (<see cref="CabinetPrivacy.PlateFor"/>) and this file
    /// composes nothing: which mark means which stage is Core's, exactly as VACANT/OCCUPIED is.</param>
    /// <param name="booked">#770 · The negotiation room the captain is holding on this floor this watch, or
    /// null. Two marks and nothing else: the room's own door carries the BOOKED plate
    /// (<see cref="RoomBooking.Booking.Plate"/>) and its street leaves are drawn dogged. It is passed in
    /// rather than read out of a ledger here for the reason the watch is — the room that is DRAWN and the
    /// room the [E] key asks about have to be one room.</param>
    /// <param name="stoodUp">#731 · The canteen tops whose person has already got up and walked off this
    /// watch. Handed down for the same reason the watch itself is: a body crossing the hall on real legs
    /// must not ALSO be drawn sitting in the chair it left, and the one place that can be made true is the
    /// one function that answers who is in which chair (<see cref="CanteenRegulars.Tables"/>).</param>
    /// <param name="cameIn">#731 · …and the canteen tops somebody has WALKED IN off the oncoming rota and
    /// taken this watch, by the plate over their head. The mirror of <paramref name="stoodUp"/> and handed
    /// down for the mirror reason: a body the player watched cross the floor and sit down must be drawn in
    /// that chair by the one function that seats anybody, and the console over that top has to be theirs so
    /// the [E] press meets the person the room is showing.</param>
    public static DeckPlan FloorDeck(
        string bodyId, int level, in SurfaceLayout.Field field,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids,
        IReadOnlyCollection<int> emptiedRooms, long canteenWatch = 0,
        IReadOnlyCollection<string>? locksShotOpen = null,
        IReadOnlyCollection<string>? cubiclesShut = null,
        IReadOnlyCollection<string>? cabinetsDogged = null,
        RoomBooking.Booking? booked = null,
        IReadOnlySet<int>? stoodUp = null,
        IReadOnlyDictionary<int, string>? cameIn = null)
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

        PourTheStructure(walls, in floor, pastTheSeam);
        GlazeTheOpenings(walls, in floor, pastTheSeam);
        KeepTheSpecimen(walls, doors, in floor);

        HashSet<string> shut = WhichLeavesAreShut(in floor, level, cubiclesShut);
        HashSet<string> bookedLeaves = WhichLeavesAreBooked(in floor, level, canteenWatch, booked);
        HangTheDoorways(walls, doors, in floor, shut, bookedLeaves);
        HangTheLockedDoors(walls, doors, consoles, in floor, level, locksShotOpen);

        OfferTheRooms(consoles, in floor, level, emptiedRooms);
        MarkTheRefuges(consoles, labels, in floor);

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

        FitOutTheAmenities(
            consoles, labels, backdrops, tables, stools, in floor,
            bodyId, level, canteenWatch, cabinetsDogged, stoodUp, cameIn);

        DrawThePark(consoles, labels, backdrops, benchSeats, in floor);
        FurnishTheSuites(
            consoles, labels, furniture, in floor, level, canteenWatch, booked, cubiclesShut);
        FurnishTheMeetingRooms(consoles, labels, furniture, in floor);
        PlateTheBins(labels, in floor);
        FurnishTheChambers(consoles, labels, furniture, in floor, bodyId, level, kind);

        HangTheIncidentBoard(consoles, in floor);
        HangThePosters(consoles, in floor);
        PlateTheWatchclocks(labels, in floor, in field, bodyId, level);
        CallTheCars(consoles, in field);
        OpenTheStair(consoles, in field, bodyId, level);
        StencilTheLandmarks(labels, in floor);

        // #1164 · THE ONE LOCAL THAT WAS DECLARED IN THE WRONG SECTION. The car's own coordinate was
        // worked out inside the lift-console loop above and read by neither of its lines — both of its
        // readers are the two plates below, which is why it stands here now. Asked once, of Core, and
        // handed to both, so the depth over the mouth and the facility's name beside it cannot end up
        // measured off two different shafts.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);
        List<(float X, float Y, string Text, float Px, int Tone)> bigLabels =
            PlateTheFloorByTheLift(bodyId, level, shaftX, shaftY);
        NameTheFacility(labels, bodyId, level, kind, shaftX, shaftY);
        PlateTheRefuges(bigLabels, in floor);

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
