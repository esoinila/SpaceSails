using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1332 B · Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — <b>THE GARDEN
/// BEHIND GLASS</b>: one small pressurised green off the concourse of every haven with a bar.
///
/// <para>Owner, 2026-09-29: <i>"the little garden on space ports could be used to produce salad, coffee etc.
/// comforts for levels sufficient for the restaurant."</i> The small cousin of the park behind the canteen
/// (#759): no sky, no attendance plate, four beds and a bench. Nothing happens in it yet. It exists so that
/// story can plant a meeting there (#1332 C, #1062, #1202) — a room a body can be met in, tailed to, or left
/// alone in — and so the station reads as a place people live. Every word it says is Core's
/// (<see cref="HavenGarden"/>); this file owns only where it stands.</para>
///
/// <h3>Where, and why there</h3>
///
/// <para>Off the ring's two north-western faces, in the corner the hall, the bar and (at Selene Gate) the
/// observation walk leave free. Edge <see cref="GardenDoorEdge"/> (4, facing 150°) carries THE door — the one
/// the beds are counted from — and edge <see cref="GardenGrowersEdge"/> (3, facing 120°, beside the bar's own
/// door) carries the second. Both are free at every haven: the tube is 8, the bar 2, the walk 5 (Selene Gate),
/// the cars 0/6/10 (Cinder Roost 0/5/10). Each face gives up the one department panel it carried, exactly the
/// bargain the walk and the cars made, and the sealed-edge counter is still stepped over it so every OTHER
/// edge keeps the tag and the hatch id it always had.</para>
///
/// <para><b>Both doors open onto the concourse</b>, on two different faces of the ring. The fire code asks for
/// two ways out (#822) and the brief offered the service corridor for the second; the concourse level has no
/// service corridor, and the lower level's corridor is a different floor — a door onto it would be a car or a
/// stair inside a garden. Two faces of one hall is the honest second way out, and it is also the tail's
/// shape: in by one door and out by the other, onto a different side of the room.</para>
///
/// <para><b>The glass</b> is the room's two outer walls (north and west), drawn as the gallery's is — the
/// pen's own <c>IsWindow</c>, collision on the frame, see-through in the ink. The bench stands by the north
/// glass; the beds stand in a row out from the door. The ring side is the station's stone.</para>
///
/// <h3>WHY THIS FILE DECLARES NO STATIC FIELD (#1163)</h3>
///
/// <para>Every number here is measured off <see cref="HallVertex"/> and <c>HallTopY</c>, which are
/// <c>static readonly</c> in the opening file. Static initializers of a partial class run in the order the
/// compiler reads the files, so a <c>static readonly</c> declared here would be initialised against a hall
/// that is not there yet. Everything below is a <c>const</c> or a computed member, which cannot be evaluated
/// early; <c>NoPartialClassSpreadsItsStaticFieldsTests</c> enforces it.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>#1332 B · The ring face THE door is cut in — edge 4, facing 150° (west-north-west). The beds
    /// are counted from this door.</summary>
    private const int GardenDoorEdge = 4;

    /// <summary>#1332 B · …and the face the second door is cut in — edge 3, facing 120°, the ring's face beside
    /// the bar's own door.</summary>
    private const int GardenGrowersEdge = 3;

    /// <summary>#1332 B · Where along its edge each doorway is cut, as <see cref="DeckExpansions.CarveDoorway"/>
    /// fractions from the edge's first vertex. The same 0.40 of an edge every opened joint on this ring is, but
    /// pushed toward the corner the two faces share — that is where the garden is deep enough to walk out of;
    /// at the other end of edge 3 the bar's wall is under two du away.</summary>
    private const float GardenGapFrom = 0.50f;

    /// <summary>#1332 B · …and where it ends.</summary>
    private const float GardenGapTo = 0.90f;

    /// <summary>#1332 B · How far west of the ring the room runs. Sixteen du, so the park's 16:9 canvas lies on
    /// the floor at its own shape (nine du tall inside a twelve-du room) rather than squeezed into a box of
    /// another shape — the fault the owner found on the gallery's first floor plate.</summary>
    private const float GardenWidthDu = 16f;

    /// <summary>#1332 B · A bed's half-width across the room and half-depth along the row.</summary>
    private const float BedHalfWidth = 1.0f;

    private const float BedHalfDepth = 0.6f;

    /// <summary>#1332 B · Centre to centre down the row. The 0.8 du left between two beds is under a body's
    /// width, so the row is one raised run with its four plates, not four islands to thread.</summary>
    private const float BedStepDu = 2.0f;

    /// <summary>#1332 B · The clear walk left at each end of the row — well over a body's width, so both ends
    /// are a way round.</summary>
    private const float BedEndPassDu = 2.4f;

    /// <summary>#1332 B · How far off the glass the bench's plank stands — under a body's width, so there is no
    /// standing room behind it.</summary>
    private const float BenchOffTheGlassDu = 1.0f;

    /// <summary>#1332 B · How far off the plank a captain stands beside it, and where standing up puts him —
    /// the park bench's own standoff (<c>Seating.BenchStandoffDu</c>: a body's radius and a du), so the two
    /// benches in the game are walked up to from the same distance.</summary>
    private const double GardenBenchStandoffDu = DeckPlan.AvatarRadius + 1.0;

    /// <summary>#1332 B · The alpha the canvas is laid at: the gallery's own 0.55, so the deck's floor still
    /// reads through it.</summary>
    private const float GardenArtAlpha = 0.55f;

    /// <summary>#1332 B · <b>DOES THIS HAVEN HAVE A GARDEN?</b> Every haven with a bar — a walkable interior with
    /// a keep behind its counter. Asked of the catalogue and of <see cref="Barkeeps"/> so the room and the
    /// guards read one answer.</summary>
    public static bool HasGarden(string bodyId) => HasInterior(bodyId) && Barkeeps.For(bodyId) is not null;

    /// <summary>#1332 B · Is this ring face one of the garden's two doorways at this station?</summary>
    private static bool TheGardenOpensOnEdge(StationSpec spec, int edge) =>
        edge is GardenDoorEdge or GardenGrowersEdge && HasGarden(spec.BodyId);

    // ── THE ROOM'S BOX ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The corner the door's face ends at — the ring's west-north-west vertex, where the room's
    /// south wall meets the ring.</summary>
    private static (float X, float Y) GardenSouthEastCorner => HallVertex(GardenDoorEdge + 1);

    private static float GardenEastX => GardenSouthEastCorner.X;

    private static float GardenSouthY => GardenSouthEastCorner.Y;

    /// <summary>The room's north glass stands on the bar's floor line, so the garden and the bar share the one
    /// wall they touch at.</summary>
    private static float GardenNorthY => HallTopY;

    private static float GardenWestX => GardenEastX - GardenWidthDu;

    /// <summary>
    /// #1332 B · <b>THE GARDEN'S BOX</b> — <c>(west, south, east, north)</c> of everything it can hold: the
    /// rectangle west of the ring and the corner between the two doors' faces and the bar's wall. The east face
    /// is the growers' face's far vertex. Null at a berth with no garden.
    /// </summary>
    public static (double X0, double Y0, double X1, double Y1)? TheGardenBox(string bodyId) =>
        HasGarden(bodyId)
            ? (GardenWestX, GardenSouthY, HallVertex(GardenGrowersEdge).X, GardenNorthY)
            : null;

    /// <summary>
    /// #1332 B · <b>IS THIS POINT IN THE GARDEN?</b> In its box and outside the hall ring — the two doors' faces
    /// are the only thing between the corner of it and the concourse. The concourse's and nothing else's: the
    /// lower level is laid in the same coordinates, for <see cref="InTheObservationWalk"/>'s reason.
    /// </summary>
    public static bool InTheGarden(string bodyId, double x, double y, int level = HavenLevels.Concourse) =>
        level == HavenLevels.Concourse
        && TheGardenBox(bodyId) is { } box
        && x >= box.X0 && x <= box.X1 && y >= box.Y0 && y <= box.Y1
        && OutsideTheRing(x, y);

    /// <summary>Beyond at least one face of the twelve-gon — the ring's own apothem against the point's reach
    /// along that face's normal, so "outside the hall" is the same shape the walls were cut to.</summary>
    private static bool OutsideTheRing(double x, double y)
    {
        for (int k = 0; k < HallSides; k++)
        {
            double a = (30 + (30 * k)) * System.Math.PI / 180.0;
            double reach = ((x - HallCenterX) * System.Math.Cos(a)) + ((y - HallCenterY) * System.Math.Sin(a));
            if (reach > HallApothem)
            {
                return true;
            }
        }

        return false;
    }

    // ── THE TWO DOORS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One doorway's two jambs on its face, cut with the ring's own carving arithmetic.</summary>
    private static (WingWall StubA, WingWall StubB, WingDoor Way) CarveTheGardenDoor(int edge)
    {
        (float ax, float ay) = HallVertex(edge);
        (float bx, float by) = HallVertex(edge + 1);
        return DeckExpansions.CarveDoorway(ax, ay, bx, by, GardenGapFrom, GardenGapTo);
    }

    /// <summary>
    /// #1332 B · <b>THE GARDEN'S TWO DOORS</b>, by their middles — THE door first (the concourse face the beds
    /// are counted from), the growers' door second. Empty at a berth with no garden. Published because the fire
    /// code's count, the reachability sweep, the bed order and the dev start's standing place must all mean the
    /// same two openings.
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> TheGardenDoorsAt(string bodyId)
    {
        if (!HasGarden(bodyId))
        {
            return [];
        }

        var doors = new List<DeckReachability.Point>(2);
        foreach (int edge in new[] { GardenDoorEdge, GardenGrowersEdge })
        {
            WingDoor way = CarveTheGardenDoor(edge).Way;
            doors.Add(new DeckReachability.Point((way.X1 + way.X2) / 2.0, (way.Y1 + way.Y2) / 2.0));
        }

        return doors;
    }

    /// <summary>#1332 B · Where the dev start stands the captain — one pace in off THE door's middle, on the
    /// concourse side, toward the hall's centre. Null at a berth with no garden.</summary>
    public static DeckReachability.Point? TheGardenThresholdAt(string bodyId)
    {
        if (TheGardenDoorsAt(bodyId) is not [DeckReachability.Point door, ..])
        {
            return null;
        }

        double dx = HallCenterX - door.X, dy = HallCenterY - door.Y;
        double len = System.Math.Sqrt((dx * dx) + (dy * dy));
        return new DeckReachability.Point(door.X + (dx / len * CageStepDu), door.Y + (dy / len * CageStepDu));
    }

    // ── THE BEDS ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The row stands a third of the room in from the ring.</summary>
    private static float BedRowX => GardenEastX - (GardenWidthDu / 3f);

    /// <summary>Bed <paramref name="i"/>'s centre, counting from the door's end of the row (the south end, the
    /// end the door looks at).</summary>
    private static float BedY(int i) => GardenSouthY + BedEndPassDu + BedHalfDepth + (i * BedStepDu);

    /// <summary>
    /// #1332 B · <b>THE FOUR RAISED BEDS</b>, as boxes, in canon order from the door (<see
    /// cref="HavenGarden.BedPlates"/>): lettuce nearest, tomatoes furthest. Solid, drawn as the gallery's
    /// machines are (<see cref="DeckPlan.FurnitureSpot"/>) and walled as they are, so the walked room and the
    /// drawn room are one room. Plates, not consoles (Kosh): nothing here is pressed. Empty at a berth with no
    /// garden.
    /// </summary>
    public static IReadOnlyList<(double X0, double Y0, double X1, double Y1)> TheGardenBedsAt(string bodyId)
    {
        if (!HasGarden(bodyId))
        {
            return [];
        }

        var beds = new List<(double, double, double, double)>(HavenGarden.BedPlates.Count);
        for (int i = 0; i < HavenGarden.BedPlates.Count; i++)
        {
            float y = BedY(i);
            beds.Add((BedRowX - BedHalfWidth, y - BedHalfDepth, BedRowX + BedHalfWidth, y + BedHalfDepth));
        }

        return beds;
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The plank runs along the north glass, a quarter of the room in from the west glass — clear of
    /// the row of beds and of both doors.</summary>
    private static float BenchX => GardenWestX + (GardenWidthDu / 4f);

    private static float BenchY => GardenNorthY - BenchOffTheGlassDu;

    /// <summary>
    /// #1332 B · <b>THE BENCH BY THE GLASS</b>, as the park's own seat record (<see cref="ParkBenches.Bench"/>)
    /// — the same plank, laid the same way (along x, the park's convention), with the same two ends and the
    /// same snap (#820: the end you walked up to). Nobody is ever on it: no walker, no gardener, no figure.
    /// Null at a berth with no garden.
    /// </summary>
    public static ParkBenches.Bench? TheGardenBenchAt(string bodyId) =>
        HasGarden(bodyId) ? new ParkBenches.Bench(0, BenchX, BenchY, Taken: false, Plate: null) : null;

    /// <summary>#1332 B · Where a captain stands beside the bench at the end nearest <paramref name="seatX"/> —
    /// the room's side of the plank, one standoff off it, which is also where standing up puts him (the plank
    /// is a solid segment, and a captain left where he sat would be standing inside the furniture).</summary>
    public static DeckReachability.Point TheGardenBenchStepOff(double seatX) =>
        new(seatX, BenchY - GardenBenchStandoffDu);

    // ── THE WELD'S GARDEN STEPS ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1332 B · <b>THE ROOM ITSELF</b> — walls, the plate and the four bed plates. The two doorways were
    /// already cut in the ring (<c>CutTheRing</c>); here the room closes round them: the north glass along the
    /// bar's floor line out to the bar's own west wall, the west glass, and the south stone back to the ring's
    /// corner. The beds and the plank are solid, like the gallery's machines.
    /// </summary>
    private static void LayTheGarden(StationSpec spec, List<DeckPlan.Wall> walls,
        List<(float X, float Y, string Text)> labels)
    {
        if (!HasGarden(spec.BodyId))
        {
            return;
        }

        walls.Add(new(GardenWestX, GardenNorthY, BarLeft, GardenNorthY, true, true));      // the north glass
        walls.Add(new(GardenWestX, GardenSouthY, GardenWestX, GardenNorthY, true, true));  // the west glass
        walls.Add(new(GardenWestX, GardenSouthY, GardenEastX, GardenSouthY, false, true)); // the south stone

        foreach ((double x0, double y0, double x1, double y1) in TheGardenBedsAt(spec.BodyId))
        {
            walls.Add(new((float)x0, (float)y0, (float)x1, (float)y0, false, false));
            walls.Add(new((float)x0, (float)y1, (float)x1, (float)y1, false, false));
            walls.Add(new((float)x0, (float)y0, (float)x0, (float)y1, false, false));
            walls.Add(new((float)x1, (float)y0, (float)x1, (float)y1, false, false));
        }

        // The plank: one segment along its own axis, the park's collision idiom (#790).
        walls.Add(new(BenchX - (float)UndergroundComplex.ParkBenchHalfDu, BenchY,
                      BenchX + (float)UndergroundComplex.ParkBenchHalfDu, BenchY, false, false));

        // ONE room plate, in the plate idiom, low in the room where the way in reads it — Core's string, never
        // retyped. A room plate on the concourse level, exactly as the walk's is; the floor's own labels are
        // the concourse's and this is not a second one of them.
        labels.Add((GardenWestX + (GardenWidthDu / 2f), GardenSouthY + 1f, HavenGarden.Plate));

        // …and the four beds' own plates, on the beds, in canon order from the door.
        IReadOnlyList<(double X0, double Y0, double X1, double Y1)> beds = TheGardenBedsAt(spec.BodyId);
        for (int i = 0; i < beds.Count; i++)
        {
            labels.Add((BedRowX, BedY(i), HavenGarden.BedPlates[i]));
        }
    }

    /// <summary>#1332 B · The bench's press and the drawn blocks. The bench is a seat and nothing announces it
    /// (Kosh): the console wears the park bench's own plate, the verb and nothing else.</summary>
    private static void FitTheGarden(StationSpec spec, List<DeckPlan.ConsoleSpot> consoles,
        List<DeckPlan.FurnitureSpot> furniture)
    {
        if (TheGardenBenchAt(spec.BodyId) is not { } bench)
        {
            return;
        }

        consoles.Add(new(DeckPlan.ConsoleKind.HiveBench, (float)bench.X, (float)bench.Y, bench.DeckPlate));

        foreach ((double x0, double y0, double x1, double y1) in TheGardenBedsAt(spec.BodyId))
        {
            furniture.Add(new((float)x0, (float)y0, (float)x1, (float)y1, 0));
        }

        float half = (float)UndergroundComplex.ParkBenchHalfDu;
        furniture.Add(new(BenchX - half, BenchY - 0.25f, BenchX + half, BenchY + 0.25f,
            DeckPlan.FurnitureSpot.ToneOf(RingOffice.Fitting.Bench)));
    }

    /// <summary>#1332 B · <b>THE CANVAS</b>: the park's own picture (<see cref="UndergroundComplex.ParkArtUrl"/>,
    /// raised beds, a bench, a walk) reused — no new art this slice — laid on the floor at its own 16:9 shape
    /// across the room's width and centred on its depth, at the gallery's alpha.</summary>
    private static void HangTheGardensCanvas(StationSpec spec, List<DeckPlan.Backdrop> backdrops)
    {
        if (!HasGarden(spec.BodyId))
        {
            return;
        }

        float h = GardenWidthDu * 9f / 16f;
        float midY = (GardenSouthY + GardenNorthY) / 2f;
        backdrops.Add(new(UndergroundComplex.ParkArtUrl, GardenWestX, midY + (h / 2f), GardenWidthDu, h,
            GardenArtAlpha));
    }
}
