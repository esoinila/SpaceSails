using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE OBSERVATION WALK AND ITS GALLERY, AS PLACES A BODY CAN BE PUT (#1199) — the measuring of the
/// tube and the crossbar, the published points and boxes, the in-the-walk tests and the vending cafeteria.
///
/// <para>Split out of <c>HavenInterior.cs</c> under #251 as a pure move: three runs of the base file, with
/// no member renamed, re-scoped or re-ordered. What stayed behind is exactly the two <c>static readonly</c>
/// boxes (<c>TheWalk</c>, <c>TheGallery</c>) with the design banners and the edge constant that sit over
/// them: a static field initializer of this partial class must stay in the opening file, in its original
/// order (#1163). Their two MEASURE methods moved — a method is not initialised, it is called.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>#1199 · …and the measuring, off <see cref="HallVertex"/> and the SAME
    /// <see cref="DeckExpansions.CarveDoorway"/> proportions every other opened edge on this ring is cut
    /// with (0.30 … 0.70). Never typed-in coordinates: unaudited client geometry literals are this project's
    /// oldest and most reliably wrong bug class, and a doorway measured here but cut there would be two
    /// opinions about one gap.</summary>
    private static (float, float, float, float) MeasureTheObservationWalk()
    {
        (float ax, float ay) = HallVertex(ObservationWalkEdge);
        (float bx, float by) = HallVertex(ObservationWalkEdge + 1);
        (WingWall stubA, WingWall stubB, _) = DeckExpansions.CarveDoorway(ax, ay, bx, by, 0.30f, 0.70f);

        // The edge runs north to south, so stubA ends at the north jamb and stubB begins at the south one.
        return (stubA.X2, stubA.Y2, stubB.Y1, (float)(stubA.X2 - ObservationWalk.LengthDu));
    }

    /// <summary>#1199 · Is this haven the one with the walk? Asked of Core, so the room and every rule about
    /// it are reading one answer.</summary>
    public static bool HasObservationWalk(string bodyId) =>
        string.Equals(bodyId, ObservationWalk.HavenId, StringComparison.Ordinal) && HasInterior(bodyId);

    /// <summary>#1199 · The axis the whole T is built on — the middle of the tube, which is the middle of
    /// the doorway the ring was cut at. Stated once, because the tube's walls, the gallery's centre, the rail
    /// and every fixture in the room are all hung off it.</summary>
    private static float TheWalksAxisY => (TheWalk.NorthJambY + TheWalk.SouthJambY) / 2f;

    private static (float, float, float, float) MeasureTheGallery()
    {
        float half = (float)(ObservationWalk.GalleryWidthDu / 2.0);
        float axis = (TheWalk.NorthJambY + TheWalk.SouthJambY) / 2f;
        return (
            TheWalk.BlindX - (float)ObservationWalk.GalleryDepthDu,
            axis - half,
            TheWalk.BlindX,
            axis + half);
    }

    /// <summary>
    /// #1199 · <b>THE WALK, AS A PLACE A BODY CAN BE PUT.</b> Where the rail is — the only ground in this
    /// station that is out of sight of the concourse — handed out as a point so the tail's last leg, the
    /// wait's own clock and the beat's trigger are aimed at the same spot the walls were built round.
    ///
    /// <para><b>2026-09-18 · the rail moved with the room, and it is stated HERE and nowhere else.</b> It
    /// was half a walk short of the tube's blind wall; the blind wall is a doorless opening into the gallery
    /// now, and the rail is what it always was in the fiction — the thing you stand at, at the outer glass,
    /// with the drop under you. One body clear of the gallery's west wall, which is as close to the glass as
    /// a body gets.</para>
    ///
    /// <para><b>And half a hat NORTH of the axis, which is the whole mechanic.</b> Owner, 2026-09-18:
    /// <i>"The T could even be curved — both leg and hat. For tailing it would make sense."</i> What the
    /// curve is FOR is a rail the tube's mouth cannot see and a mouth the rail cannot see — a straight tube
    /// with the rail dead centre keeps the person being followed in the captain's line down its whole
    /// length, he holds at the mouth for ever and the beat stalls, which is the stall the owner watched
    /// happen. This room gets that property out of its own walls instead of out of an arc: at a quarter of
    /// the hat's width off the axis, the line from here to the doorway runs into the gallery's own back wall
    /// (the north stub between the crossbar and the tube's north jamb), and it is blocked in both
    /// directions. It is the one stretch of this rail the way in cannot see, which is where somebody who did
    /// not want to be watched would stand, and it is measured — two guards drive the line and go RED when
    /// the rail is put back on the axis.</para>
    ///
    /// <para>Every reader of "the rail" in the game asks this one method: the route the person of interest
    /// walks, the distance the card is gated on, and the place the note says nobody was. Null at every other
    /// berth in the game, which is all of them but one.</para>
    /// </summary>
    public static DeckReachability.Point? TheRailAt(string bodyId) =>
        HasObservationWalk(bodyId)
            ? new DeckReachability.Point(
                TheGallery.WestX + (2 * DeckPlan.AvatarRadius),
                TheWalksAxisY + (float)(ObservationWalk.GalleryWidthDu / 4.0))
            : null;

    /// <summary>
    /// #1199 (2026-09-18) · <b>THE THROAT — where the leg meets the hat.</b> The doorless opening the tube
    /// makes into the gallery, by its middle: the one place anybody entering this room has to come through,
    /// and therefore the thing a stakeout seat is a stakeout OF.
    ///
    /// <para>Published because three things must mean the same opening — the walls that stop either side of
    /// it, the guard that proves the cafeteria's seats can see it, and the guard that proves the rail cannot
    /// see past it to the concourse.</para>
    /// </summary>
    public static DeckReachability.Point? TheThroatAt(string bodyId) =>
        HasObservationWalk(bodyId)
            ? new DeckReachability.Point(TheGallery.EastX, TheWalksAxisY)
            : null;

    /// <summary>#1199 · Where the MOUTH of it is — the doorway's own middle, on the hall side of the line.
    /// Where a captain stands to watch somebody walk out over the drop, and where the wait is counted
    /// from.</summary>
    public static DeckReachability.Point? TheWalksMouthAt(string bodyId) =>
        HasObservationWalk(bodyId)
            ? new DeckReachability.Point(
                TheWalk.MouthX + (2 * DeckPlan.AvatarRadius),
                (TheWalk.NorthJambY + TheWalk.SouthJambY) / 2.0)
            : null;

    /// <summary>#1199 · The box the TUBE's walls were laid on — <c>(x0, y0, x1, y1)</c>, gallery end to
    /// mouth, south jamb to north. Published because "is that door on the walk" and "is the captain in the
    /// walk" are the same question about the same rectangle, and a guard that re-measured it from two points
    /// would be a second geometry agreeing with whatever the first one did.
    ///
    /// <para>The STEM only, since 2026-09-18 — <see cref="TheGalleryBox"/> is the crossbar. Two rectangles
    /// and not one, because the T is not a rectangle and a bounding box round it would claim the two corners
    /// of open station either side of the tube, which is outside the building.</para></summary>
    public static (double X0, double Y0, double X1, double Y1)? TheWalksBox(string bodyId) =>
        HasObservationWalk(bodyId)
            ? (TheWalk.BlindX, TheWalk.SouthJambY, TheWalk.MouthX, TheWalk.NorthJambY)
            : null;

    /// <summary>#1199 · …and the box the GALLERY's walls were laid on, in the same shape and the same order.
    /// The east face is the tube's own blind x, so the stem and the crossbar share an edge by construction
    /// rather than by agreement.</summary>
    public static (double X0, double Y0, double X1, double Y1)? TheGalleryBox(string bodyId) =>
        HasObservationWalk(bodyId)
            ? (TheGallery.WestX, TheGallery.SouthY, TheGallery.EastX, TheGallery.NorthY)
            : null;

    /// <summary>#1199 · Is this point inside the walk? The two boxes and nothing else — the same shape
    /// <c>UndergroundComplex.Room.Contains</c> answers with, so "in the walk" is one question with one answer
    /// rather than a threshold typed into whichever file asked last.
    ///
    /// <para>The layer label, the beat's own gate and the deck's location strip all come through here, which
    /// is what keeps <c>OBSERVATION WALK</c> on the WHOLE T: a captain at the rail, a captain at a table in
    /// the cafeteria and a captain halfway down the tube are all in the same named room, because there is
    /// only one room.</para></summary>
    /// <param name="level">#1253 · Which floor the point is on. The T hangs off the CONCOURSE and off nothing
    /// else — a station with a basement does not have a second observation walk under the first one — so this
    /// is false at every other level however the coordinates read. It is a parameter and not an assumption
    /// because the lower concourse is laid in the SAME coordinate space as the hall (the audit's own
    /// prediction: "the T's rectangle tests leak into the lower floor"), and a point out west at level −1 is
    /// in a service corridor rather than over a drop.</param>
    public static bool InTheObservationWalk(string bodyId, double x, double y, int level = HavenLevels.Concourse) =>
        level == HavenLevels.Concourse
        && (Inside(TheWalksBox(bodyId), x, y) || Inside(TheGalleryBox(bodyId), x, y));

    /// <summary>#1199 · Is this point in that box? One inclusive test, so the stem and the crossbar cannot
    /// come to two opinions about an edge they share.</summary>
    private static bool Inside((double X0, double Y0, double X1, double Y1)? box, double x, double y) =>
        box is { } b && x >= b.X0 && x <= b.X1 && y >= b.Y0 && y <= b.Y1;

    /// <summary>#1199 · Is this point in the GALLERY specifically? Asked by the beat (GILT-EYE's route ends
    /// in here and he vanishes in here) and by the guards that hold the cafeteria's fixtures to being inside
    /// the room they furnish.</summary>
    /// <param name="level">#1253 · Which floor. The crossbar is the concourse's, for
    /// <see cref="InTheObservationWalk"/>'s reason exactly.</param>
    public static bool InTheGallery(string bodyId, double x, double y, int level = HavenLevels.Concourse) =>
        level == HavenLevels.Concourse && Inside(TheGalleryBox(bodyId), x, y);

    /// <summary>#1199 · Is this point in the CAFETERIA BAND — the inner half, against the back wall, where
    /// the machines and the tables stand? Core owns the depth of the band
    /// (<see cref="ObservationWalk.CafeteriaBandDu"/>); this is where it lands on this deck. The rail half is
    /// everything else, and the guards hold every fixture but the binoculars to this side of the line.</summary>
    public static bool InTheCafeteriaBand(string bodyId, double x, double y) =>
        InTheGallery(bodyId, x, y) && x >= TheGallery.EastX - ObservationWalk.CafeteriaBandDu;

    // ── #1199 (2026-09-18) · THE VENDING CAFETERIA ───────────────────────────────────────────────────────
    //
    // Owner, live: "maybe even a small vending machine cafeteria with a couple of tables there… the station
    // likes to get the tourist money." Two machines flush to the back wall at the far ends of it, two steel
    // tables in front of them, and the coin binoculars out at the glass. Everything below is measured off the
    // gallery's own box and a body's own width — no typed-in coordinates, for the reason the room above gives.
    //
    // NOBODY IS THERE, and the fixtures are chosen so it stays that way: a vending machine is unstaffed by
    // definition, which is why the owner's cafeteria can be busy furniture in a room whose whole content is
    // that there is no one in it. The lore does not move an inch.

    /// <summary>#1199 · How deep a vending machine stands off the wall it is bolted to — one body's width,
    /// the game's own smallest real dimension, so a machine is a thing you walk round rather than a line.
    ///
    /// <para><c>const</c> and not <c>static readonly</c>, deliberately. A body's width is a constant
    /// expression, and a COMPUTED static in a partial class is the one member a concern-shaped split may not
    /// move — this file's own remarks say so and <c>NoPartialClassSpreadsItsStaticFieldsTests</c> enforces
    /// it. A pair the compiler folds cannot be re-ordered by a file name.</para></summary>
    private const float VendorDepth = (float)(2 * DeckPlan.AvatarRadius);

    /// <summary>#1199 · …and how wide it is. The same number: a machine is square in plan, which is what a
    /// drinks cabinet actually is, and two numbers for one box would be two numbers to keep in step.</summary>
    private const float VendorWidth = VendorDepth;

    /// <summary>#1199 · How far off the ends of the gallery the two machines stand — one machine's own width
    /// clear of the side glass, so neither is jammed into a corner the captain cannot walk round.</summary>
    private static float VendorInsetY => VendorWidth * 2f;

    /// <summary>
    /// #1199 · <b>THE THREE MACHINES, AS DRAWN BLOCKS.</b> Two flush to the gallery's back wall, one toward
    /// each end — and, since 2026-09-19, <b>a third standing on its own in the middle of the floor</b>.
    /// Filled rectangles in the pen's own furniture grammar (<c>DeckPlan.FurnitureSpot</c>, #868), which is
    /// how every other solid fitting in this game is drawn; no new renderer and no new token.
    ///
    /// <para><b>The third one is the ISLAND, and it is there to be walked behind.</b> Owner, live on the T:
    /// <i>"another vending machine or something else that only MOMENTARILY blocks the view."</i> It stands on
    /// the room's own axis, its back edge exactly on the cafeteria line
    /// (<see cref="ObservationWalk.CafeteriaBandDu"/> — the furthest out of the room a machine is allowed to
    /// stand), so it is the first thing anybody coming out of the tube meets and it is square in the middle
    /// of the sightline from the SOUTH stakeout table to the rail. Of the 8.92 du he walks from the throat to
    /// the rail, <b>4.13 du are out of that table’s sight</b> — and at a walker’s pace
    /// (<c>Interior.NpcWalk.PaceDu</c>) one <c>ReeverObservation.LookIntervalSeconds</c> is 1.50 du, so he is
    /// behind it for nearly three looks: long enough to go behind a machine and not come out the other side.</para>
    ///
    /// <para><b>One island and not two, and the reason is measured.</b> The two tables sit a quarter of the
    /// hat either side of the axis — twelve deck units apart, on opposite sides of the route — so the angle
    /// between "a line from the north table" and "a line from the south table" at any point on his walk is
    /// 69° at the rail and wider still further in. A machine is <see cref="VendorWidth"/> wide and a body
    /// cannot stand closer to it than <c>2 × DeckPlan.AvatarRadius</c>, so the widest it can ever screen is
    /// 53.1°. <b>No single fitting can stand between him and both tables at once</b>, and a
    /// second island put there to force it would be furniture written to satisfy a guard. The north table
    /// keeps its clean view of the rail, which is what makes it the good seat — and what a captain in it
    /// loses him behind is the paper or the eyepiece, which is the beat the owner asked for in the first
    /// place.</para>
    /// </summary>
    public static IReadOnlyList<(double X0, double Y0, double X1, double Y1)> TheVendingMachineBlocks(
        string bodyId)
    {
        if (!HasObservationWalk(bodyId))
        {
            return [];
        }

        float half = VendorWidth / 2f;
        return
        [
            (TheGallery.EastX - VendorDepth, NorthVendorY - half, TheGallery.EastX, NorthVendorY + half),
            (TheGallery.EastX - VendorDepth, SouthVendorY - half, TheGallery.EastX, SouthVendorY + half),
            (IslandWestX, TheWalksAxisY - half, IslandWestX + VendorWidth, TheWalksAxisY + half),
        ];
    }

    private static float NorthVendorY => TheGallery.NorthY - VendorInsetY;

    private static float SouthVendorY => TheGallery.SouthY + VendorInsetY;

    /// <summary>#1199 · The island's back edge — ON the cafeteria line, which is the furthest out of the room
    /// anything but the binoculars may stand (<see cref="InTheCafeteriaBand"/>). Derived from the band rather
    /// than chosen, so a room that ever re-argues its cafeteria takes the island with it.</summary>
    private static float IslandWestX => TheGallery.EastX - (float)ObservationWalk.CafeteriaBandDu;

    /// <summary>#1199 · Where a captain stands to use a machine — one body clear of its face, on the room's
    /// side of it, which is the same standoff the bar's own counter console keeps from the counter. Three of
    /// them, north first, in the room's own order; the island's is off its EAST face, because an island's
    /// "room side" is the side you arrive from and everybody arrives through the throat.</summary>
    public static IReadOnlyList<DeckReachability.Point> TheVendorsAt(string bodyId) =>
        HasObservationWalk(bodyId)
            ?
            [
                new DeckReachability.Point(VendorFaceX, NorthVendorY),
                new DeckReachability.Point(VendorFaceX, SouthVendorY),
                new DeckReachability.Point(IslandFaceX, TheWalksAxisY),
            ]
            : [];

    private static float VendorFaceX => TheGallery.EastX - VendorDepth - (float)(2 * DeckPlan.AvatarRadius);

    /// <summary>#1199 · …and the island's, off its east face — the side the throat is on.</summary>
    private static float IslandFaceX => IslandWestX + VendorWidth + (float)(2 * DeckPlan.AvatarRadius);

    /// <summary>
    /// #1199 · <b>THE TWO STEEL TABLES</b>, by their centres — in the cafeteria band, one in front of each
    /// machine and well clear of the tube's mouth so nothing stands in the way in.
    ///
    /// <para>Published as its own list and deliberately NOT appended to <see cref="BarFloor.Tops"/>: the
    /// bar's tops are what the room's own walkers cross the floor to, and a regular who wandered out to the
    /// end of the observation walk for a sit-down would be the one thing this whole feature cannot survive.
    /// The gallery is a room nobody is in. The SEAT is the same seat (<c>Seating.BarTop</c>, through
    /// <c>TheBarTopUnderfoot</c>) because a table is a table; what it is not is the bar.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> GalleryTops(string bodyId) =>
        HasObservationWalk(bodyId)
            ?
            [
                new DeckReachability.Point(TableX, NorthTableY),
                new DeckReachability.Point(TableX, SouthTableY),
            ]
            : [];

    /// <summary>#1199 · The tables stand down the MIDDLE of the cafeteria band — Core's own dimension,
    /// halved, so they are as far from the back wall as they are from the line past which nothing but the
    /// binoculars may stand.</summary>
    private static float TableX => TheGallery.EastX - (float)(ObservationWalk.CafeteriaBandDu / 2.0);

    private static float NorthTableY => TheWalksAxisY + ((TheGallery.NorthY - TheWalksAxisY) / 2f);

    private static float SouthTableY => TheWalksAxisY - ((TheWalksAxisY - TheGallery.SouthY) / 2f);

    /// <summary>
    /// #1199 · <b>THE COIN BINOCULARS</b> — the one fixture allowed past the cafeteria line, because a pair
    /// of binoculars anywhere but at the glass is a telescope pointed at a wall.
    ///
    /// <para>On the rail itself, on the T's own axis: the middle of the ten people standing side by side,
    /// which is where a station bolts the thing it wants the coins out of. That is also the spot the beat's
    /// own trigger is measured to (<see cref="TheRailAt"/>), and deliberately so — a captain who has walked
    /// all the way out to look for somebody is standing exactly where the machine is.</para>
    /// </summary>
    public static DeckReachability.Point? TheBinocularsAt(string bodyId) => TheRailAt(bodyId);
}
