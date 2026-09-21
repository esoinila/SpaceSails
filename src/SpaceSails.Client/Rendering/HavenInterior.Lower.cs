using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1253 · Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — <b>DOWN
/// BELOW</b>: the service level under one station's concourse, and the three cages that reach it.
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station, so the tailing
/// task could start from the basement cabin and end at the observation deck? Otherwise the followed distance
/// is easily very short. The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>A LOWER CONCOURSE: the ring's own footprint with nothing cut into it, a row of crew cabins whose
/// leaves do not open for a captain, worklight, and three cars up to the hall — each landing in its own
/// place, so which car somebody took decides where they come up. Nothing down here explains anything. The
/// plates are maintenance plates and the doors are numbered, not named.</para>
///
/// <h3>WHY THIS FILE DECLARES NO STATIC FIELD (#1163)</h3>
///
/// <para>Almost every coordinate in this class is measured off <c>HallTopY</c> / <c>HallApothem</c>, which are
/// <c>static readonly</c> (they are <c>Math.Cos</c> of the ring's apothem, so they cannot be <c>const</c>).
/// Static initializers of a partial class run in the order the compiler READS THE FILES — not the order a
/// reader sees — so a <c>static readonly</c> declared HERE, in a file that sorts before
/// <c>HavenInterior.cs</c>, would be initialised against <c>HallTopY == 0</c> and would quietly build a
/// basement stacked on the origin, with a clean build and no warning. That is measured, not guessed: the
/// split that wrote this rule moved 33 pinned frames and reddened 14 guards.</para>
///
/// <para>So every number below is either a <c>const</c> (a compile-time expression the compiler folds, which
/// no file order can re-order) or a COMPUTED PROPERTY, which is evaluated when it is asked and therefore
/// cannot be evaluated early. <c>NoPartialClassSpreadsItsStaticFieldsTests</c> enforces the rule; this file
/// is written to it rather than around it.</para>
/// </summary>
public static partial class HavenInterior
{
    // ── WHICH EDGES THE CARS TOOK, AND WHY THOSE THREE ───────────────────────────────────────────────────
    //
    // Edge k of the ring faces (30 + 30k)°. Three are already spoken for at this station: 8 is the tube
    // (south), 2 is the wide door into the bar (north), 5 is #1199's observation walk (due west). Every
    // other edge is a sealed berth or a department panel, and a car takes one exactly as the walk took one —
    // the edge keeps its wall, keeps its cold locked leaf, and gives up only the department PLATE it would
    // have carried. The sealed-edge counter is still stepped over a cage, so every other edge on this
    // station keeps the tag and the hatch id it has always had.
    //
    // 0 (north-east), 6 (south-west) and 10 (east-south-east): no two of them adjacent, none of them the
    // three that are taken, and far enough apart round the ring that a captain standing at one is not
    // watching the other two. That is the mechanic the owner asked for — a man goes down at one edge and
    // comes up at whichever he chooses, and a captain who guessed wrong is standing at an empty car.
    //
    // WHY NOT 9, which is the other south-eastern face and was the first choice. The immigration counter
    // runs across the hall's southern quarter (deskY, x 4…9) and edge 9's doorstep lands 0.65 du off the end
    // of it — INSIDE a body's own radius of the counter, so the doors would have opened onto stone. That was
    // not reasoned out; it was measured, by the way-home guard below, which refused to walk a captain off a
    // car it could not stand him on. It is exactly the class of bug #602 shipped on the regolith (a captain
    // let out of a lift inside a wall), caught this time before anybody had to play it.

    /// <summary>#1253 · The north-east cage. Its ordinal is also the cage's own identity everywhere in the
    /// game — the panel, the ride, the landing and the beat all say "cage k" and mean this edge.</summary>
    private const int CageEdgeNorthEast = 0;

    /// <summary>#1253 · The south-west cage.</summary>
    private const int CageEdgeSouthWest = 6;

    /// <summary>#1253 · The eastern cage, low on the ring — see the note above for why it is not its
    /// neighbour.</summary>
    private const int CageEdgeEast = 10;

    /// <summary>#1253 · The three, in the ring's own order. A <c>const</c>-only array expression built on
    /// demand rather than a static field, for the reason at the head of this file.</summary>
    private static int[] CageEdges => [CageEdgeNorthEast, CageEdgeSouthWest, CageEdgeEast];

    /// <summary>#1253 · Is this edge of the ring a car? Asked by the concourse's own build loop, so the ring
    /// has one opinion about which of its twelve faces carry lifts.</summary>
    private static bool ACageStandsOnEdge(StationSpec spec, int edge) =>
        spec.Lower is not null && System.Array.IndexOf(CageEdges, edge) >= 0;

    /// <summary>#1253 · Where a cage's console hangs on the ring — the SAME place the department panel it
    /// replaced would have stood (nine tenths of the way out to the edge's middle), and the same place on
    /// BOTH floors. That is the Hive's own law about a shaft said about a berth: a car stands in the same
    /// spot on every floor of a building, so going down is legible and coming back up is never a search.</summary>
    private static (float X, float Y) TheCageOnEdge(int edge)
    {
        (float ax, float ay) = HallVertex(edge);
        (float bx, float by) = HallVertex((edge + 1) % HallSides);
        return (
            HallCenterX + (((ax + bx) / 2) - HallCenterX) * 0.9f,
            HallCenterY + (((ay + by) / 2) - HallCenterY) * 0.9f);
    }

    /// <summary>
    /// #1253 · <b>THE THREE CARS, AS PLACES A BODY CAN BE PUT</b> — in the ring's own order, or empty at a
    /// station with no floor under it, which is every haven in the game but one.
    ///
    /// <para>Published because five readers must mean the same three squares: the concourse's build (which
    /// hangs the consoles), the lower level's build (which hangs them again, on the same spots), the ride
    /// (which stands the captain at one), the beat that sends somebody down, and every guard that holds the
    /// floor below to having a way out of it.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> TheCagesAt(string bodyId)
    {
        if (!HasLowerLevel(bodyId))
        {
            return [];
        }

        var cars = new List<DeckReachability.Point>(CageEdges.Length);
        foreach (int edge in CageEdges)
        {
            (float x, float y) = TheCageOnEdge(edge);
            cars.Add(new DeckReachability.Point(x, y));
        }

        return cars;
    }

    /// <summary>
    /// #1253 · <b>WHERE THE DOORS PUT YOU</b> — one pace in off the car, toward the middle of the ring.
    ///
    /// <para>Inward and never a typed offset, because the ring is a twelve-gon and a car's alcove faces
    /// whichever way its edge does: a landing that kept its own <c>+1</c> would set a captain who rode the
    /// south-west car down inside the south-west wall. #801 shipped exactly that mistake underground once,
    /// which is why this is measured off the line from the car to the hall's own centre.</para>
    ///
    /// <para>Null for a cage index this station does not have, which is the honest answer and never a spot
    /// invented at the origin.</para>
    /// </summary>
    public static DeckReachability.Point? TheCageLandingAt(string bodyId, int cage)
    {
        IReadOnlyList<DeckReachability.Point> cars = TheCagesAt(bodyId);
        if (cage < 0 || cage >= cars.Count)
        {
            return null;
        }

        DeckReachability.Point car = cars[cage];
        double dx = HallCenterX - car.X, dy = HallCenterY - car.Y;
        double len = System.Math.Sqrt((dx * dx) + (dy * dy));
        return len <= 0
            ? car
            : new DeckReachability.Point(
                car.X + (dx / len * CageStepDu), car.Y + (dy / len * CageStepDu));
    }

    /// <summary>#1253 · How far out of the car a captain stands: two body-widths, which is far enough that
    /// the doors are behind him and near enough that the panel is still the console his [E] finds.</summary>
    private const double CageStepDu = 4 * DeckPlan.AvatarRadius;

    /// <summary>#1253 · The plate on the concourse side of a car's door, with the door's own findable id on
    /// the end of it — the station's existing <c>{authority}-{edge}</c> grammar, so a captain who has learnt
    /// to read this ring's panels can tell the three cars apart without one word being written for him. That
    /// legibility IS the mechanic: which car he took is the thing the captain has to read.</summary>
    private static string CagePlateAbove(StationSpec spec, int edge) =>
        $"\U0001F6D7 {HavenLevels.NoPublicAccessPlate} · {spec.Authority[0]}-{edge:D2}";

    /// <summary>#1253 · …and the plate on the other side of the same door. Down among the staff it is not a
    /// warning, it is the lift — Core's own cage sign, and the same id.</summary>
    private static string CagePlateBelow(StationSpec spec, int edge) =>
        $"{UndergroundComplex.CageSign} · {spec.Authority[0]}-{edge:D2}";

    // ── THE ROW OF CABINS ────────────────────────────────────────────────────────────────────────────────
    //
    // A block of crew cabins across the northern half of the level, measured off the ring's own apothem and
    // never typed in. Each is a sealed box with one leaf drawn on its corridor face — CLOSED, and closed the
    // way #563 rules a locked door is closed: it is TIME, not a key. There is nothing to pick, nothing to
    // find and nothing anywhere that says who is behind any of them. A number is not a name.
    //
    // The block stands clear of the ring on all four sides, so the service corridor runs the whole way round
    // it: there is no dead end down here, and the fire code's ≥2 ways out is met by three cars rather than
    // by an exemption.

    /// <summary>#1253 · The corridor face of the row — the wall the leaves hang on.</summary>
    private static float CabinRowSouthY => HallCenterY + (HallApothem * 0.20f);

    /// <summary>#1253 · How deep a cabin is. One deck unit under
    /// <see cref="UndergroundComplex.FireCodeSmallRoomDu"/>, which is the game's one statement of <i>a space
    /// you can cross in two paces</i> — so a cabin is bedroom-small BY MEASUREMENT and is let off the fire
    /// code's second door under the exemption that already exists (#822), rather than by a new one being
    /// argued for it. Derived from the law instead of chosen, so a row that ever grows takes the law with
    /// it.</summary>
    private static float CabinDepth => (float)UndergroundComplex.FireCodeSmallRoomDu - 1f;

    /// <summary>#1253 · …and the back of the row.</summary>
    private static float CabinRowNorthY => CabinRowSouthY + CabinDepth;

    /// <summary>#1253 · How far the row reaches either side of the hall's own axis. Measured off the apothem
    /// so the block stays inside the twelve-gon with a walkable corridor behind it at every point — the ring
    /// narrows as it goes north, and a row laid out to the flat north edge's width would push its corners
    /// through the wall.</summary>
    private static float CabinRowHalfWidth => HallApothem * 0.45f;

    private static float CabinRowWestX => HallCenterX - CabinRowHalfWidth;

    private static float CabinRowEastX => HallCenterX + CabinRowHalfWidth;

    /// <summary>#1253 · One cabin's frontage — the row divided by <see cref="HavenLevels.Cabins"/>, so the
    /// count is stated in Core once and the geometry follows it rather than agreeing with it.</summary>
    private static float CabinWidth => (CabinRowEastX - CabinRowWestX) / HavenLevels.Cabins;

    /// <summary>#1253 · The middle of cabin <paramref name="i"/>'s corridor face (zero-based), which is where
    /// its leaf is centred and where its plate hangs.</summary>
    private static float CabinDoorX(int i) => CabinRowWestX + (CabinWidth * (i + 0.5f));

    /// <summary>#1253 · How far into the corridor a cabin's plate stands off its own leaf — the same two du
    /// the bar's back-room hatches are set in from theirs.</summary>
    private const float CabinPlateStandoff = 2f;

    /// <summary>
    /// #1253 · <b>THE CABIN LEAVES</b>, as the building's own <see cref="UndergroundComplex.LockedDoor"/> —
    /// the type <see cref="Egress"/> insists on, and it insists for the reason that is this level's whole
    /// point: every member of that list is refused to the captain BY CONSTRUCTION, so nobody down here can be
    /// given a public door to go through by an oversight.
    ///
    /// <para>One list, consumed by the build (which draws them) and by <see cref="TheLowerConcourseBand"/>
    /// (which is what a walker comes out of), so the plate a body carries and the plate the captain is
    /// refused at are one string.</para>
    /// </summary>
    private static UndergroundComplex.LockedDoor[] CabinLeaves()
    {
        var leaves = new UndergroundComplex.LockedDoor[HavenLevels.Cabins];
        float half = CabinWidth / 2f;
        for (int i = 0; i < HavenLevels.Cabins; i++)
        {
            float x = CabinDoorX(i);
            leaves[i] = new UndergroundComplex.LockedDoor(
                x - (half * 0.5f), CabinRowSouthY, x + (half * 0.5f), CabinRowSouthY,
                HavenLevels.CabinPlate(i + 1));
        }

        return leaves;
    }

    /// <summary>#1253 · How many leaves the row has, published so a beat that seeds one of them a tenant
    /// (#1253 slice 2) and the guards that sweep them are counting the same doors.</summary>
    public static IReadOnlyList<string> CabinPlatesAt(string bodyId)
    {
        if (!HasLowerLevel(bodyId))
        {
            return [];
        }

        var plates = new List<string>(HavenLevels.Cabins);
        foreach (UndergroundComplex.LockedDoor leaf in CabinLeaves())
        {
            plates.Add(leaf.Sign);
        }

        return plates;
    }

    /// <summary>#1253 · Where cabin <paramref name="cabin"/>'s door is, as a place a body can stand — one
    /// plate's standoff out into the corridor, on the side the corridor is on. Null at a berth with no lower
    /// level or for a cabin this row does not have.</summary>
    public static DeckReachability.Point? TheCabinDoorstepAt(string bodyId, int cabin) =>
        !HasLowerLevel(bodyId) || cabin < 0 || cabin >= HavenLevels.Cabins
            ? null
            : new DeckReachability.Point(CabinDoorX(cabin), CabinRowSouthY - CabinPlateStandoff);

    // ── THE FLOOR, AS SOMETHING TO WALK ACROSS ───────────────────────────────────────────────────────────

    /// <summary>
    /// #1253 · <b>WHAT THE LOWER CONCOURSE IS, TO SOMEBODY WHO HAS TO WALK ACROSS IT.</b> The same
    /// <see cref="BarFloor"/> record the bar answers with, and deliberately so: everything that walks a haven
    /// floor wants the same four facts about it — which berth, where the room begins, which leaves a body may
    /// come out of, and where somebody with nothing to do stands — and a second record would be a second set
    /// of walkers with a second set of bugs.
    ///
    /// <para><b>No TOPS</b>, and that is the whole of what keeps the bar's own beats upstairs: the walk-in,
    /// the rep's pitch, the finder and the seated case all begin by finding the top the captain is at, and
    /// there is no table on this floor. The room is a corridor.</para>
    ///
    /// <para><c>FloorY</c> is the ring's own southern edge, which is the lowest ground on this plan — so
    /// "is the captain in this room" is true of the whole level, which is true: there is one room down here
    /// and the cabins are not part of it.</para>
    /// </summary>
    private static BarFloor TheLowerConcourseBand(StationSpec spec)
    {
        var fixtures = new List<DeckReachability.Point>(HavenLevels.Cages);
        for (int cage = 0; cage < HavenLevels.Cages; cage++)
        {
            if (TheCageLandingAt(spec.BodyId, cage) is { } landing)
            {
                fixtures.Add(landing);
            }
        }

        return new BarFloor(spec.BodyId, HallBottomY - 1, CabinLeaves(), fixtures, []);
    }

    // ── THE WELD ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1253 · <b>THE LEVEL ITSELF.</b> The ring's footprint with nothing cut into it, the cabin block inside
    /// it, three cars on the edges they hold upstairs, and one plate. That is the whole floor.
    ///
    /// <para><b>No ship and no tube</b>, and that is a decision rather than an omission: there is no gangway
    /// from a service level, which is precisely why the way home has to be proved rather than assumed. The
    /// law that proves it is the berth column in <c>TheStairIsAWayHomeTests</c>' twin — A* proves you can
    /// REACH a car, never that the car is a way OUT (#600/#719).</para>
    /// </summary>
    private static DeckPlan BuildLowerComplex(
        StationSpec spec, LowerSpec lower, System.Action<DeckPlan.Droid[], int>? fillWalkers)
    {
        var walls = new List<DeckPlan.Wall>(HallSides + 12);
        var doors = new List<DeckPlan.Door>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        var labels = new List<(float X, float Y, string Text)>();

        // The ring, unbroken. Upstairs three of these twelve faces are openings; down here the station is a
        // closed envelope with cars in it, because a service level does not have a berth or a bar hanging off
        // it and it certainly does not have a window over the drop.
        for (int k = 0; k < HallSides; k++)
        {
            (float ax, float ay) = HallVertex(k);
            (float bx, float by) = HallVertex((k + 1) % HallSides);
            walls.Add(new(ax, ay, bx, by, false, true));
        }

        // The cabin block: four walls round it and a party wall between each pair, so the row is a row of
        // boxes rather than one long dormitory. The leaves are DRAWN ON the corridor face (it stays unbroken
        // stone), exactly as the bar's cellar and storeroom leaves are — a door a captain is refused at is
        // not a gap in a wall.
        walls.Add(new(CabinRowWestX, CabinRowSouthY, CabinRowEastX, CabinRowSouthY, false, false));
        walls.Add(new(CabinRowWestX, CabinRowNorthY, CabinRowEastX, CabinRowNorthY, false, false));
        walls.Add(new(CabinRowWestX, CabinRowSouthY, CabinRowWestX, CabinRowNorthY, false, false));
        walls.Add(new(CabinRowEastX, CabinRowSouthY, CabinRowEastX, CabinRowNorthY, false, false));
        for (int i = 1; i < HavenLevels.Cabins; i++)
        {
            float x = CabinRowWestX + (CabinWidth * i);
            walls.Add(new(x, CabinRowSouthY, x, CabinRowNorthY, false, false));
        }

        // …and the leaves themselves, with the numbered plate the captain reads before pressing [E] and being
        // refused. The knockable panel is the ring's own Hatch console — the same kind, the same verb, and
        // therefore not one line of new dispatch anywhere.
        //
        // #1279 · THE PAINTED PLATE IS THE NUMBER AND NOT THE LEAF'S WHOLE NAME. Five of these stand at one
        // door's frontage — about three deck units, fifty-five pixels at the deck's own scale — and the full
        // plate is seventy-eight pixels wide, so the caption band had to stack them into two interleaved rows
        // and the owner read three of the five numbers as a smear. The register keeps the whole name
        // (`leaf.Sign`, which is what Egress seeds a door roll on and what CabinPlatesAt publishes); what is
        // PAINTED is what differs between the doors. The argument is at HavenLevels.CabinDoorPlate.
        int cabinNo = 0;
        foreach (UndergroundComplex.LockedDoor leaf in CabinLeaves())
        {
            doors.Add(new((float)leaf.X1, (float)leaf.Y1, (float)leaf.X2, (float)leaf.Y2, Locked: true));
            consoles.Add(new(
                DeckPlan.ConsoleKind.Hatch,
                CabinDoorX(cabinNo), CabinRowSouthY - CabinPlateStandoff,
                HavenLevels.CabinDoorPlate(cabinNo + 1)));
            cabinNo++;
        }

        // The three cars, on the same squares they stand on upstairs.
        foreach (int edge in CageEdges)
        {
            (float cx, float cy) = TheCageOnEdge(edge);
            consoles.Add(new(DeckPlan.ConsoleKind.HavenLift, cx, cy, CagePlateBelow(spec, edge)));
        }

        // ONE label, and it is the floor's own name. Nothing down here explains anything: the plates are
        // maintenance plates, the doors are numbered, and there is no welcome poster because nobody is being
        // welcomed.
        labels.Add((HallCenterX, HallCenterY - (HallApothem * 0.45f), lower.Name));

        var backdrops = new List<DeckPlan.Backdrop>
        {
            // The corridor's own canvas, in the concourse's grammar and at the concourse's own rectangle —
            // a picture over the ring, at an alpha, so the deck's floor still reads through it. One plate
            // for the level: the hall gets one and the bar gets one, and a floor whose whole content is a
            // corridor does not need two.
            new(lower.Art, HallCenterX - 16, HallCenterY + 9, 32, 18, 0.95f),
        };

        DeckReachability.Point spawn =
            TheCageLandingAt(spec.BodyId, 0) ?? new DeckReachability.Point(HallCenterX, HallCenterY);

        return new DeckPlan(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: spawn.X, spawnY: spawn.Y,
            droidCount: fillWalkers is null ? 0 : Egress.BandSlots,
            fillDroids: (_, buffer) => fillWalkers?.Invoke(buffer, 0),
            location: (_, _) => lower.Name,
            doors: doors.ToArray(), shipFixtures: false, followCam: true);
    }
}
