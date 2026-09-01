namespace SpaceSails.Core;

/// <summary>
/// THE PLAYER'S OWN SHIP, AS COMPARTMENTS AND DOORS — in Core, where a test can walk her.
///
/// <para>Owner, after a weekend of building damage control for other people's ships: <i>"I think our ship
/// should also have these controls. They are so cool and it would be consistent in the universe."</i> And
/// then, looking at her: <i>"we don't even have the doors in our own ship :-D"</i></para>
///
/// <para>He is right on both counts, and the second is the reason the first cannot be done yet. She HAS
/// rooms — bridge, cantina, three cabins and a head, the hold, the shuttle bay, the engine room — but every
/// opening in her is a GAP IN A WALL. Nothing to shut, nothing that knows a room is a room, and therefore
/// nothing for a valve board to act on. A damage-control panel needs compartments before it needs valves.</para>
///
/// <para>This is also the third time this weekend that geometry has been moved out of the client, and the
/// first two both turned out to be ALREADY WRONG the moment a test could reach them — the nest two
/// compartments from its own name, the dead bridge panel standing on the nav post. Her hull has lived as a
/// wall literal in <c>DeckPlan.BuildShip</c> since the beginning, audited by nothing. Same lesson as
/// <see cref="WreckLayout"/>: a wall you cannot pass has no test that fails.</para>
///
/// <para>Every number here is read off her existing walls rather than invented, so this describes the ship
/// that already exists. What it adds is the ability to ASK her questions.</para>
/// </summary>
public static class ShipLayout
{
    /// <summary>Her corridor runs down the centreline between the bridge and engine bulkheads, y −3…3 —
    /// the same shape the wreck's spine has, because it is the same kind of ship.</summary>
    public const float SpineHalfHeight = 3f;

    /// <summary>The bridge bulkhead, and the aft one. Everything between them is corridor.</summary>
    public const float BridgeBulkheadX = 18f;
    public const float EngineBulkheadX = -14f;

    /// <summary>A compartment of the player's ship: a rectangle, and the wall its door is cut in.</summary>
    /// <param name="Name">What the crew calls it — the name the board will show.</param>
    /// <param name="X0">Aft edge.</param>
    /// <param name="X1">Forward edge.</param>
    /// <param name="Y0">Starboard edge (more negative).</param>
    /// <param name="Y1">Port edge.</param>
    /// <param name="DoorAcrossX">True when the door is cut in a VERTICAL wall (a bulkhead the corridor runs
    /// through, like the bridge's), false when it is cut in a horizontal one (a room off the corridor).</param>
    /// <param name="DoorAt">The coordinate of the wall the door is in: an x for a bulkhead, a y for a room.</param>
    /// <param name="DoorCentre">Where along that wall the opening sits.</param>
    /// <param name="DoorHalfWidth">Half the opening. Every one of these must clear the captain, which
    /// <c>ShipLayoutTests</c> holds.</param>
    public readonly record struct Room(
        string Name, float X0, float X1, float Y0, float Y1,
        bool DoorAcrossX, float DoorAt, float DoorCentre, float DoorHalfWidth);

    /// <summary>
    /// Her compartments, bow to stern. READ OFF THE EXISTING WALLS, not invented — every bound and every
    /// door gap here already exists in <c>DeckPlan.BuildShip</c>, which is what makes this a description of
    /// the ship rather than a second, competing ship.
    ///
    /// <para>The corridor itself is not in the list, for the same reason the wreck's spine is not: it is
    /// what the doors open ONTO, and a compartment is a thing you can shut.</para>
    /// </summary>
    public static readonly Room[] Rooms =
    [
        // Forward of the bridge bulkhead: helm, nav post, the glass. Her door is on the centreline.
        new("BRIDGE", 18f, 30f, -10f, 10f, DoorAcrossX: true, DoorAt: BridgeBulkheadX, DoorCentre: 0f, DoorHalfWidth: 2f),

        // Port side, forward: the cantina and its panoramic window.
        new("CANTINA", 6f, 18f, 3f, 10f, DoorAcrossX: false, DoorAt: 3f, DoorCentre: 11f, DoorHalfWidth: 2f),

        // Starboard side: four berths in a row, each with its own narrow door onto the corridor. These are
        // the tightest openings on the ship and therefore the ones an audit should watch.
        new("THE HEAD", 14.5f, 18f, -10f, -3f, DoorAcrossX: false, DoorAt: -3f, DoorCentre: 16.25f, DoorHalfWidth: 1.25f),
        new("CABIN 1", 11f, 14.5f, -10f, -3f, DoorAcrossX: false, DoorAt: -3f, DoorCentre: 12.75f, DoorHalfWidth: 1.25f),
        new("CABIN 2", 7.5f, 11f, -10f, -3f, DoorAcrossX: false, DoorAt: -3f, DoorCentre: 9.25f, DoorHalfWidth: 1.25f),
        new("CABIN 3", 4f, 7.5f, -10f, -3f, DoorAcrossX: false, DoorAt: -3f, DoorCentre: 5.75f, DoorHalfWidth: 1.25f),

        // Port side, aft: the hold.
        new("CARGO HOLD", -12f, -1f, 3f, 10f, DoorAcrossX: false, DoorAt: 3f, DoorCentre: -5f, DoorHalfWidth: 2f),

        // Starboard side, aft: the bay, on the wild side where her bottom hatch is.
        new("SHUTTLE BAY", -12f, 2f, -10f, -3f, DoorAcrossX: false, DoorAt: -3f, DoorCentre: -5f, DoorHalfWidth: 2f),

        // Aft of the engine bulkhead. Her door is on the centreline, like the bridge's.
        new("ENGINE ROOM", -24f, -14f, -10f, 10f, DoorAcrossX: true, DoorAt: EngineBulkheadX, DoorCentre: 0f, DoorHalfWidth: 2f),
    ];

    /// <summary>The airlock vestibule is deliberately NOT a compartment. It is a bump-out off the corridor
    /// with no door of its own — a defensible kill-box by design (the owner's Expanse consult), and the one
    /// place aboard where "shut it and walk away" is exactly what you must not be able to do.</summary>
    public const string VestibuleIsNotACompartment =
        "The airlock vestibule opens straight onto the corridor. There is nothing to dog: if something comes " +
        "through that hatch, it is already in the ship with you.";

    /// <summary>The corridor, as the board names it — the volume every one of her doors opens onto.</summary>
    public const string SpineName = "THE CORRIDOR";

    /// <summary>Which compartment a point is in, or null out in the corridor. One place the answer is
    /// computed, so her map, her board and her rules can never disagree.</summary>
    public static string? CompartmentAt(double x, double y)
    {
        foreach (Room r in Rooms)
        {
            if (x >= r.X0 && x <= r.X1 && y >= r.Y0 && y <= r.Y1)
            {
                return r.Name;
            }
        }

        return null;
    }

    /// <summary>Where a room's door actually is, as a point — for placing its console and for the audit to
    /// aim at.</summary>
    public static DeckReachability.Point DoorPoint(in Room r) =>
        r.DoorAcrossX ? new(r.DoorAt, r.DoorCentre) : new(r.DoorCentre, r.DoorAt);

    /// <summary>Somewhere inside the room, clear of its walls — where the audit walks TO, and where a
    /// compartment's own fittings belong.</summary>
    public static DeckReachability.Point Inside(in Room r) =>
        new((r.X0 + r.X1) / 2.0, (r.Y0 + r.Y1) / 2.0);

    /// <summary>The door itself, as the segment that fills its opening when it is dogged shut. ONE
    /// definition, read both by the thing that draws the door and by the thing that walks into it — the
    /// wreck's hard-won lesson (a gap nobody can see is the same as no gap) applied before it can bite
    /// here.</summary>
    public static (float X1, float Y1, float X2, float Y2) DoorSegment(in Room r) =>
        r.DoorAcrossX
            ? (r.DoorAt, r.DoorCentre - r.DoorHalfWidth, r.DoorAt, r.DoorCentre + r.DoorHalfWidth)
            : (r.DoorCentre - r.DoorHalfWidth, r.DoorAt, r.DoorCentre + r.DoorHalfWidth, r.DoorAt);

    /// <summary>How far off a doorway its control sits.</summary>
    private const float DoorConsoleStandoff = 1.5f;

    /// <summary>
    /// Where the hatch control stands: in the CORRIDOR, on the far side of the door from the room.
    ///
    /// <para>Derived from which way the room lies rather than listed, so it is right for a bulkhead door on
    /// the centreline and a cabin door in a side wall without either being a special case. The wreck put its
    /// door consoles on the corridor side for a reason worth repeating: the captain shutting a door is
    /// almost always the one who does NOT want to be sealed in behind it.</para>
    /// </summary>
    /// <summary>How far a BULKHEAD door's control sits off the centreline, toward port.
    ///
    /// <para>Not decoration. Her bulkhead doors are on the centreline, and THE HEAD is the forwardmost
    /// starboard room — hard against the bridge bulkhead — so the two controls landed 1.5 du apart in the
    /// corridor, one [E] prompt over two hatches. The separation audit found it the moment it existed. Two
    /// units to port clears the head's control and still leaves 0.8 du between the console and the cantina
    /// wall at y = 3, which is more than the captain's own radius.</para></summary>
    private const float BulkheadConsolePortOffset = 2.0f;

    public static DeckReachability.Point DoorConsolePoint(in Room r)
    {
        if (r.DoorAcrossX)
        {
            float away = r.DoorAt < (r.X0 + r.X1) / 2f ? -DoorConsoleStandoff : DoorConsoleStandoff;
            return new(r.DoorAt + away, r.DoorCentre + BulkheadConsolePortOffset);
        }

        float off = r.DoorAt < (r.Y0 + r.Y1) / 2f ? -DoorConsoleStandoff : DoorConsoleStandoff;
        return new(r.DoorCentre, r.DoorAt + off);
    }

    /// <summary>Her damage-control board. Aft in the ENGINE ROOM, for the same reason the wreck's is aft:
    /// the valves are where the machinery is. Unlike a derelict's, hers has a live bridge repeater — which
    /// is the whole difference between owning a ship and boarding one.
    ///
    /// <para>NOT (−19, −5), which is where I first put it: 1.1 du from her charge dump, so one [E] prompt
    /// was fighting over two consoles and the captain got whichever won. The wreck has had a test against
    /// exactly this since the scuttling panel landed on the nest — the ship had none, which is why I was
    /// free to make the same mistake a third time. <c>ShipLayoutTests</c> holds it now.</para>
    ///
    /// <para>Then (−17, −6), which the audit passed and the owner still rejected on sight: <i>"see the two
    /// crowded consoles at the back of our ship"</i>. 3.35 du clears the 3 du interact radius by a whisker,
    /// so the captain got the console they aimed at — but BOTH still drew an [E], because the renderer lit
    /// every console in range while the key only ever answered the nearest. The honest fix was in
    /// <c>DeckView</c> (one prompt, and it is the true one); this is the belt to that braces — the engine
    /// room is 10 × 20 du and there was never a reason to crowd her valves against her capacitor.</para></summary>
    public static DeckReachability.Point ValveStation => new(-16f, -7f);

    /// <summary>Her capacitor dump — a ship-systems control that has nothing to do with air, and everything
    /// to do with why the atmosphere board could not be called the vent panel. In Core so it is a thing the
    /// separation audit can SEE; it was a client literal, which is precisely how it got sat on.</summary>
    public static DeckReachability.Point ChargeDumpStation => new(-21f, -3f);

    /// <summary>
    /// HER SCUTTLING CHARGES. Owner: <i>"that ship also has the scuttling charges... let's have a captains
    /// approval mechanic for that also on our ship. :-D ... it is the last defence against the Borg in Star
    /// Trek :-D"</i>
    ///
    /// <para>Port side aft, in the machinery space — the same room a derelict keeps hers in, and the same room
    /// the atmosphere valves are in, because damage control and the last resort are one station's business.
    /// Well clear of the valves (14 du) and the charge dump, so nobody reaches for the wrong handle in a
    /// hurry: <c>ConsoleCrowdingTests</c> holds that, and the whole reason this is a Core constant rather than
    /// a client literal is that four collisions have already shipped from literals.</para></summary>
    public static DeckReachability.Point ScuttleStation => new(-17f, 7f);

    /// <summary>Her builder's plate, on the engine-room bulkhead by the keel.</summary>
    public static DeckReachability.Point BuildersPlateStation => new(-20f, 4f);

    /// <summary>Everything bolted to her that the captain can press, in one list — so nothing new can be
    /// added on top of something old without a test going red.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Fittings
    {
        get
        {
            var all = new List<(string Name, DeckReachability.Point At)>
            {
                ("the atmosphere valves", ValveStation),
                ("the bridge repeater", BridgeRepeaterStation),
                ("the charge dump", ChargeDumpStation),
                ("the builder's plate", BuildersPlateStation),
                ("the scuttling charges", ScuttleStation),
                ("the gangway placard", PlacardStation),
            };

            // #1040 · …AND A DESK PER BERTH THAT HAS ONE. It was one entry while there was one desk; a second
            // berth got one the moment the owner asked for it, and a list that named only the first would be
            // the separation audit walking half the furniture. Written as a loop over the same source the
            // deck builds from, so a third berth is audited on the day it is furnished rather than on the day
            // somebody remembers this list.
            foreach (string cabin in DeskCabins)
            {
                all.Add(($"the {cabin.ToLowerInvariant()} desk", CabinDeskStationIn(cabin)));
            }

            return all;
        }
    }

    // ── #1016 · A DESK IN THE CAPTAIN'S OWN BERTH ────────────────────────────────────────────────────
    //
    // Owner, on 7 Deck, looking at a cabin with a bunk in it and nothing else: "Why no table in cabin
    // either?" — and, of the ship as a whole, "I expect to have a bar table like this in this ships galley
    // also.... feature complete."
    //
    // IN CORE, LIKE HER OTHER STATIONS, AND FOR THE SAME REASON THE CHARGE DUMP IS. Four console collisions
    // have shipped on this ship out of client literals, and the comment about the last of them in
    // `DeckPlan.BuildShip` says it in the plainest terms there are: "two numbers for one console, which is
    // the exact shape of every console collision this ship has had. One of them has to be the truth; a test
    // can only walk the one in Core."

    /// <summary>#1016 · Which berth carries the desk. CABIN 1 is the TIDY one — the berth that already has
    /// the bunk in it, and the one the captain sleeps in.</summary>
    public const string DeskCabin = "CABIN 1";

    /// <summary>
    /// #1040 · <b>EVERY BERTH THAT CARRIES A DESK</b> — CABIN 1's, and now CABIN 2's beside it.
    ///
    /// <para>Owner, filing #1040 off the #1015 read: <i>"CABIN 2 could take a desk like CABIN 1's."</i> The
    /// two berths are the same room built twice (3.5 du of hull, one narrow leaf onto the corridor, a
    /// backdrop), so the desk is the same fitting placed by the same rule rather than a second design — and
    /// the placement rule is <see cref="CabinDeskStationIn"/>, which reads a berth's own bounds.</para>
    ///
    /// <para>CABIN 3 is the MED BAY (owner, 2026-07-18) and is deliberately not on this list: it is a berth
    /// that stopped being a berth, and a desk in it would be furniture in the one room aboard whose whole
    /// point is that it holds something else.</para>
    /// </summary>
    public static readonly string[] DeskCabins = ["CABIN 1", "CABIN 2"];

    /// <summary>#1016 · How far a berth's own fittings stand off its walls. One deck unit clears the
    /// captain's own body (0.7 du) with room to walk past, which is about all a 3.5 du berth can afford.</summary>
    private const float BerthFittingInset = 1f;

    /// <summary>#1016 · How many a top in her cantina seats. Four, like the station bars' — the owner's own
    /// framing of the ask was <i>"a bar table like this in this ships galley"</i>, and it is the same
    /// furniture. Stated once, because the panel says it in chairs and a second count would be the strip and
    /// the picture disagreeing about how alone the captain is.</summary>
    public const int CantinaTopSeats = 4;

    /// <summary>#1016 · …and how many the desk in her berth seats. One. A desk is a place for the person
    /// whose berth it is, and a panel offering a spare chair in a room with a bunk in it would be inventing
    /// company this boat does not carry.</summary>
    public const int CabinDeskSeats = 1;

    /// <summary>
    /// #1016 · HER DESK, in the forward-outboard corner of <see cref="DeskCabin"/> — <b>derived from the
    /// berth's own bounds and never typed</b>.
    ///
    /// <para><b>The corner is the whole of the placement, and it is a decision about the CHAIR.</b> Her bunk
    /// stands mid-berth (it is <see cref="Inside"/> of this very room, which is where
    /// <c>DeckPlan.BuildShip</c> put it), and a seat's chair square is sounded one body-width off the
    /// fixture on the first side the stone allows. In the corner the hull and the berth divider refuse three
    /// of those four sides, so the chair lands INBOARD — two and a half du from the bunk against one and
    /// four-tenths from the desk. That is what makes [E] answer the desk from the chair rather than putting
    /// the captain to bed. <c>ConsoleCrowdingTests</c> holds the label clearance between the two; the
    /// nearest-console law does the rest.</para>
    /// </summary>
    public static DeckReachability.Point CabinDeskStation => CabinDeskStationIn(DeskCabin);

    /// <summary>#1040 · …AND THE SAME CORNER IN ANY BERTH THAT ASKS FOR ONE. The paragraph above is the whole
    /// of the placement and every word of it is about a berth's own bounds, so it generalises without being
    /// re-reasoned: CABIN 2 is CABIN 1 shifted 3.5 du aft, and its desk lands in the same corner of its own
    /// room with its chair pushed inboard by the same two walls.</summary>
    /// <param name="cabin">The berth's name, as <see cref="Rooms"/> spells it.</param>
    public static DeckReachability.Point CabinDeskStationIn(string cabin)
    {
        foreach (Room r in Rooms)
        {
            if (string.Equals(r.Name, cabin, System.StringComparison.Ordinal))
            {
                return new(r.X1 - BerthFittingInset, r.Y0 + BerthFittingInset);
            }
        }

        throw new System.InvalidOperationException(
            $"#1016 · there is no compartment called {cabin} aboard, so the desk has no berth to "
            + "stand in. Rename the constant in the same commit as the room.");
    }

    /// <summary>#1040 · What the sitting at a berth desk is keyed on — the berth, so two desks aboard cannot
    /// file their business in one drawer. Built here rather than in the client, because the client would have
    /// to know how the rooms are named to build it.</summary>
    public static string CabinDeskKey(string cabin) =>
        "ship:" + cabin.ToLowerInvariant().Replace(' ', '-') + ":desk";

    // ── #1040 · HER CANTINA IS A BAR, AND A BAR HAS A COUNTER ────────────────────────────────────────
    //
    // Owner, on 7 Deck with the room in front of him: "Our on ship bar can be upgraded to match the other
    // bars... the UI represents code long time ago." He is describing a room that was drawn before any of
    // the furniture laws existed: three rings on an empty floor with the galley console standing in the
    // middle of them, and — the thing his sentence is really about — NO COUNTER AT ALL, in a room whose own
    // backdrop is a photograph of a counter with a row of stools down it.
    //
    // What a haven bar has and this room did not (HavenInterior.BuildComplex, #247 and #973 L0): a counter
    // that is a REAL WALL you belly up to rather than walk through; a service point on the players' side of
    // it; seats along it; and tables set away from it with room to walk between. All four are below, and
    // every number is read off the CANTINA compartment's own bounds — the fifth time geometry has been moved
    // out of `DeckPlan.BuildShip` into Core, and the first four all turned out to be already wrong the
    // moment a test could reach them.
    //
    // The composition is the room's own picture, which is the standard the owner set for the havens and
    // never for this room: `BarDesks` reads every bar art the same way — "bottle shelves and counter down
    // the LEFT, planet window top-right, patron tables to the right" — and art/the-space-bar.jpg, which is
    // the cantina's backdrop, draws exactly that. So the counter runs down her AFT wall with the back-bar
    // behind it, and the tops sit forward under the panoramic window.

    /// <summary>#1040 · The room the counter is in. Named once so the geometry below and any guard about it
    /// read one string.</summary>
    public const string CantinaRoom = "CANTINA";

    /// <summary>#1040 · How far the counter's own face stands off the cantina's aft wall. Three du: enough
    /// for the back-bar shelving, the counter's depth and a keep's shoulders behind it.</summary>
    private const float CounterStandoff = 3f;

    /// <summary>#1040 · How deep the counter and the back-bar shelving are drawn — the filled rectangles that
    /// make a fitting read as furniture rather than as floor (#868, the owner's own fix: <i>"could the table
    /// just be a different color rectangle"</i>).</summary>
    public const float CounterDepth = 0.8f;

    /// <summary>#1040 · One body-width, which is the offset every seat in this game is sounded at
    /// (<c>HavenInterior.BesideATop</c> uses two avatar radii). A stool tucks exactly that far off the
    /// counter's face: any nearer and the body is inside the counter, any further and it is not at it.</summary>
    private const float BodyWidth = 1.4f;

    /// <summary>
    /// #1040 · How far the counter's near end stands clear of the corridor wall — the way IN to the servery,
    /// so a captain can walk round the end of his own bar rather than being sealed out of half the galley
    /// (never-empty-floor, and its harder sibling: floor nobody can reach is not floor).
    ///
    /// <para>It is <b>not</b> one body-width, and the first cut of this lane made exactly that mistake: 1.4
    /// du leaves a body of radius 0.7 exactly one line of squares to walk down, pinched further by the
    /// counter's own end cap, and <c>DeckReachability</c>'s flood could not get behind the bar at all. A
    /// doorway is measured in bodies-you-can-pass-in, not in bodies-you-can-stand-in.</para>
    /// </summary>
    private const float CounterEndGap = 2.6f;

    /// <summary>#1040 · How far the first and last stool sit in from the counter's two ends, so no stool is
    /// closer to a wall than a body is wide.</summary>
    private const float StoolEndInset = 1f;

    /// <summary>#1040 · How many stools the row carries — three, at 1.2 du of elbow room each, which is what
    /// the counter's usable length affords once the servery has a door at one end and the window wall closes
    /// the other.</summary>
    public const int CantinaStoolCount = 3;

    /// <summary>#1040 · How many a stool seats. One, and the number is stated for the same reason
    /// <see cref="CantinaTopSeats"/> is: the panel says it in chairs, and a second count would be the strip
    /// and the picture disagreeing about how alone the captain is.</summary>
    public const int CantinaStoolSeats = 1;

    private static Room TheCantina
    {
        get
        {
            foreach (Room r in Rooms)
            {
                if (string.Equals(r.Name, CantinaRoom, System.StringComparison.Ordinal))
                {
                    return r;
                }
            }

            throw new System.InvalidOperationException(
                $"#1040 · there is no compartment called {CantinaRoom} aboard, so her counter has no room to "
                + "stand in. Rename the constant in the same commit as the room.");
        }
    }

    /// <summary>
    /// #1040 · <b>THE COUNTER'S OWN FACE</b> — the segment a captain leans on, and the wall that stops him
    /// walking through it.
    ///
    /// <para>It is ONE definition, exactly as <see cref="DoorSegment"/> is, and for the wreck's hard-won
    /// reason: the thing that DRAWS the counter, the thing that WALKS into it and the thing that measures a
    /// stool's standoff off it must be handed the same two points. A counter drawn where nothing collides is
    /// a bar you walk through; a counter that collides where nothing is drawn is a wall in an empty room.</para>
    ///
    /// <para>It runs from a body-width clear of the corridor wall right up into the window wall, so the
    /// servery has exactly one way in — round the near end — and nobody is ever standing behind the bar by
    /// accident.</para>
    /// </summary>
    public static (float X1, float Y1, float X2, float Y2) CantinaCounter
    {
        get
        {
            Room r = TheCantina;
            float x = r.X0 + CounterStandoff;
            return (x, r.Y0 + CounterEndGap, x, r.Y1);
        }
    }

    /// <summary>#1040 · The BACK-BAR — the shelving strip against the cantina's aft wall, behind the counter.
    /// A rectangle (X0, Y0, X1, Y1) in the room's own units; the pen fills it in the ink it fills every other
    /// thing-you-keep-things-in with.</summary>
    public static (float X0, float Y0, float X1, float Y1) CantinaBackBar
    {
        get
        {
            Room r = TheCantina;
            (float _, float y0, float _, float y1) = CantinaCounter;
            return (r.X0, y0, r.X0 + CounterDepth, y1);
        }
    }

    /// <summary>#1040 · …and the counter's own top, as the rectangle standing behind its face.</summary>
    public static (float X0, float Y0, float X1, float Y1) CantinaCounterTop
    {
        get
        {
            (float x, float y0, float _, float y1) = CantinaCounter;
            return (x - CounterDepth, y0, x, y1);
        }
    }

    /// <summary>
    /// #1040 · <b>THE STOOL ROW</b>, in the row's own order — one body-width off the counter's face, spaced
    /// evenly down its length with a stool's own inset at each end.
    ///
    /// <para>Derived and never listed, because a row of typed coordinates beside a counter of typed
    /// coordinates is two numbers for one fixture, which is this ship's named console bug (she has had four
    /// of them). Move the counter and the stools go with it; lengthen the room and the row spreads out.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> CantinaStools
    {
        get
        {
            (float x, float y0, float _, float y1) = CantinaCounter;
            float first = y0 + StoolEndInset, last = y1 - StoolEndInset;
            var row = new List<DeckReachability.Point>(CantinaStoolCount);
            for (int i = 0; i < CantinaStoolCount; i++)
            {
                double t = CantinaStoolCount == 1 ? 0.5 : (double)i / (CantinaStoolCount - 1);
                row.Add(new(x + BodyWidth, first + ((last - first) * t)));
            }

            return row;
        }
    }

    /// <summary>#1040 · Where the plate for the whole row is drawn and where [E] is answered FROM — the
    /// middle of the row, on a square a body can stand on (#827: a counter's plate stands in front of the
    /// desk; the desk's front face is what you order over, and the face is <see cref="CantinaCounter"/>).</summary>
    public static DeckReachability.Point CantinaCounterService
    {
        get
        {
            IReadOnlyList<DeckReachability.Point> row = CantinaStools;
            return new(row[0].X, (row[0].Y + row[^1].Y) / 2.0);
        }
    }

    /// <summary>
    /// #1040 · <b>THE GALLEY CONSOLE</b> — the one that opens the galley card, moved off the middle of the
    /// floor and onto the forward window corner where the food machines are.
    ///
    /// <para>It stood at (11, 7.5) — dead centre of the room, with a drawn table 1.5 du under it, which is
    /// how #1016 came to publish a seat on two of her three tops and refuse the third. That is the owner's
    /// own <i>"not at the middle of the empty floor"</i> ruling about the havens' barkeeps, unenforced in the
    /// one room he owns; the counter it belonged on did not exist until this lane built it.</para>
    /// </summary>
    public static DeckReachability.Point CantinaGalleyStation
    {
        get
        {
            Room r = TheCantina;
            return new(r.X1 - 1.5, r.Y1 - 1.0);
        }
    }

    /// <summary>
    /// #1040 · <b>HER TOPS</b>, forward of the counter and under the panoramic window — where a bar art puts
    /// its patron tables, and where the one view aboard actually is.
    ///
    /// <para>They were (8, 7.5), (11, 6) and (14, 7.5), laid across the middle of the room with the galley
    /// console on top of the middle one. They are read off the cantina's forward bounds now, and all three
    /// clear every fixture in the room — so the label law that decides which tops may carry a seat
    /// (<c>DeckPlan.LabelClearance</c>) passes all three rather than two.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> CantinaTops
    {
        get
        {
            Room r = TheCantina;
            return
            [
                new(r.X1 - 4.5, r.Y0 + 2.8),
                new(r.X1 - 1.5, r.Y0 + 2.5),
                new(r.X1 - 4.5, r.Y1 - 1.0),
            ];
        }
    }

    /// <summary>#1040 · Where the room writes its own name on the floor — clear of the stool row, the tops
    /// and the galley console, because a label under a fixture is the one thing this deck's crude grid cannot
    /// draw twice.</summary>
    public static DeckReachability.Point CantinaLabelStation
    {
        get
        {
            Room r = TheCantina;
            return new(r.X1 - 3.5, ((r.Y0 + r.Y1) / 2.0) + 0.9);
        }
    }

    /// <summary>The bridge repeater — the panel a derelict has and cannot power. On her it works, so the
    /// captain can shut the ship from the helm and only has to walk aft when the bus is out.
    ///
    /// <para>NOT (21, −6), where it landed the night it was built: 1.41 du from the COMMS SEAT console and
    /// 6.3 du off the captain's own spawn point. Two labels drawn on top of each other, and the fourth
    /// instance of the same mistake — which is why the audit stopped being a ship-only test and became one
    /// that walks every deck in the game (<c>ConsoleCrowdingTests</c>).</para>
    ///
    /// <para>(23, −6) is starboard-aft on the bridge: 3.2 du off the nav post, 3.2 off the comms seat, 1 du
    /// inside the bow-starboard glass, and well clear of where the captain wakes up standing.</para></summary>
    public static DeckReachability.Point BridgeRepeaterStation => new(23f, -6f);

    /// <summary>The placard by her airlock, the same plate every ship in the fleet carries. Consistency in
    /// the universe was the owner's own reason for all of this.</summary>
    public static DeckReachability.Point PlacardStation => new(2.5f, 11.5f);

    /// <summary>The compartment her board lives in.</summary>
    public const string ValveCompartment = "ENGINE ROOM";
}
