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

    /// <summary>Her builder's plate, on the engine-room bulkhead by the keel.</summary>
    public static DeckReachability.Point BuildersPlateStation => new(-20f, 4f);

    /// <summary>Everything bolted to her that the captain can press, in one list — so nothing new can be
    /// added on top of something old without a test going red.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Fittings =>
    [
        ("the atmosphere valves", ValveStation),
        ("the bridge repeater", BridgeRepeaterStation),
        ("the charge dump", ChargeDumpStation),
        ("the builder's plate", BuildersPlateStation),
        ("the gangway placard", PlacardStation),
    ];

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
