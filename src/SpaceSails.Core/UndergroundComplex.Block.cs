using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #813 · THE BLOCK'S OWN NUMBERS AND ITS OWN REGISTERS — the Manhattan ruling and every dimension it
/// decides, the fire code, the block as it is measured off a field, and the lists of plates and sizes the
/// rest of the family reads off it. What a room IS lives in <c>UndergroundComplex.Block.Rooms.cs</c>, what
/// a ring room is in <c>…Block.Ring.cs</c>, the carve that cuts a side into them in <c>…Block.Carve.cs</c>,
/// and the amenity gradient in <c>…Block.Amenities.cs</c>.
///
/// <para><b>#1163's static-class law is why this file is a little wider than that one concern.</b> Static
/// field initializers of a partial class run in the order the compiler reads the FILES, not the order a
/// reader sees, so a <c>static readonly</c> moved into a new partial can initialise against a zero it was
/// never written to see. Every declaration of one — <see cref="ParkViewPlates"/>,
/// <see cref="HallTopSizes"/>, <see cref="PrincipalPlates"/> — therefore stays HERE, in its original
/// order, along with the run of members it sits among; #251 took only the sections that declare none. The
/// cut is where the initializers allow rather than where the concerns are, which is the ruling
/// <c>HavenInterior</c> paid for with 33 moved frames.</para>
/// </summary>
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
}
