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

    /// <summary>Her damage-control board. Aft in the ENGINE ROOM, for the same reason the wreck's is aft:
    /// the valves are where the machinery is. Unlike a derelict's, hers has a live bridge repeater — which
    /// is the whole difference between owning a ship and boarding one.</summary>
    public static DeckReachability.Point ValveStation => new(-19f, -5f);

    /// <summary>The bridge repeater — the panel a derelict has and cannot power. On her it works, so the
    /// captain can shut the ship from the helm and only has to walk aft when the bus is out.</summary>
    public static DeckReachability.Point BridgeRepeaterStation => new(21f, -6f);

    /// <summary>The placard by her airlock, the same plate every ship in the fleet carries. Consistency in
    /// the universe was the owner's own reason for all of this.</summary>
    public static DeckReachability.Point PlacardStation => new(2.5f, 11.5f);

    /// <summary>The compartment her board lives in.</summary>
    public const string ValveCompartment = "ENGINE ROOM";
}
