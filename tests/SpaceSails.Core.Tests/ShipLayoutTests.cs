namespace SpaceSails.Core.Tests;

/// <summary>
/// THE SAME AUDIT, TURNED ON OUR OWN SHIP. Owner: <i>"looks like those core audits are really useful"</i>
/// and <i>"we don't even have the doors in our own ship :-D"</i>.
///
/// <para>Her hull has lived as a wall literal in the client since the beginning, walked by nobody. The
/// wreck's geometry was audited only after the owner boarded a derelict that was sealed in half with every
/// build green — and each of the three literals moved into Core since then turned out to be already wrong.
/// These are the questions nobody has ever asked of the ship the player actually lives on.</para>
/// </summary>
public class ShipLayoutTests
{
    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius

    [Fact]
    public void EveryDoorwayIsWiderThanTheCaptain()
    {
        // With ROOM, not merely passable: a door exactly the captain's width is a door they will spend ten
        // seconds shuffling into. The cabins are the tight ones — 2.5 du against a 1.4 du captain.
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            double width = r.DoorHalfWidth * 2;
            Assert.True(width > AvatarRadius * 2,
                        $"{r.Name}'s door is {width} du and the captain is {AvatarRadius * 2} du wide");
            Assert.True(width >= 2.0, $"{r.Name}'s door is {width} du — passable, but not walkable");
        }
    }

    [Fact]
    public void EveryDoorIsCutInTheWallItSaysItIsCutIn()
    {
        // A door whose coordinate does not lie on its own room's edge is a door into the middle of a wall
        // somewhere else. This is the check that would have caught the bridge panel on the nav post.
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            if (r.DoorAcrossX)
            {
                Assert.True(Math.Abs(r.DoorAt - r.X0) < 0.001 || Math.Abs(r.DoorAt - r.X1) < 0.001,
                            $"{r.Name}'s door is at x={r.DoorAt}, which is neither of its x edges");
                Assert.InRange(r.DoorCentre, r.Y0, r.Y1);
            }
            else
            {
                Assert.True(Math.Abs(r.DoorAt - r.Y0) < 0.001 || Math.Abs(r.DoorAt - r.Y1) < 0.001,
                            $"{r.Name}'s door is at y={r.DoorAt}, which is neither of its y edges");
                Assert.InRange(r.DoorCentre, r.X0, r.X1);
            }
        }
    }

    [Fact]
    public void ADoorOpensOntoTheCorridorAndNotIntoAnotherRoom()
    {
        // The rule that makes her atmosphere model the same shape as the wreck's: one hatch per compartment,
        // opening on the corridor. A door that led room-to-room would make the connectivity search's star
        // graph a lie — and the pump would price the wrong volume.
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            DeckReachability.Point door = ShipLayout.DoorPoint(r);

            // A step through the door, away from the room, must land in nobody's compartment.
            double stepX = r.DoorAcrossX ? (r.DoorAt > 0 ? -1.0 : 1.0) : 0;
            double stepY = r.DoorAcrossX ? 0 : (r.DoorAt > 0 ? -1.0 : 1.0);

            Assert.Null(ShipLayout.CompartmentAt(door.X + stepX, door.Y + stepY));
        }
    }

    [Fact]
    public void NoTwoCompartmentsOverlap()
    {
        // Overlapping rooms would put a point in two atmospheres at once, and the volume search would report
        // whichever it happened to find first.
        for (int i = 0; i < ShipLayout.Rooms.Length; i++)
        {
            for (int j = i + 1; j < ShipLayout.Rooms.Length; j++)
            {
                ShipLayout.Room a = ShipLayout.Rooms[i], b = ShipLayout.Rooms[j];
                bool apart = a.X1 <= b.X0 || b.X1 <= a.X0 || a.Y1 <= b.Y0 || b.Y1 <= a.Y0;
                Assert.True(apart, $"{a.Name} and {b.Name} overlap");
            }
        }
    }

    [Fact]
    public void TheCompartmentLookupAgreesWithEveryRoomsOwnMiddle()
    {
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            DeckReachability.Point inside = ShipLayout.Inside(r);
            Assert.Equal(r.Name, ShipLayout.CompartmentAt(inside.X, inside.Y));
        }
    }

    [Fact]
    public void TheCorridorBelongsToNoCompartment()
    {
        // Her spine, like the wreck's, is what the doors open onto — never a room the board can blow.
        Assert.Null(ShipLayout.CompartmentAt(0, 0));
        Assert.Null(ShipLayout.CompartmentAt(10, 0));
        Assert.Null(ShipLayout.CompartmentAt(-10, 0));
    }

    [Fact]
    public void HerFittingsStandInsideTheCompartmentsTheyClaim()
    {
        // The exact failure the wreck had twice: a station written down in one place and a compartment name
        // written down in another, drifting apart with nothing to notice.
        Assert.Equal(ShipLayout.ValveCompartment,
                     ShipLayout.CompartmentAt(ShipLayout.ValveStation.X, ShipLayout.ValveStation.Y));
        Assert.Equal("BRIDGE",
                     ShipLayout.CompartmentAt(ShipLayout.BridgeRepeaterStation.X,
                                              ShipLayout.BridgeRepeaterStation.Y));
    }

    [Fact]
    public void HerPlacardIsByTheAirlockAndNotInsideAnyRoom()
    {
        // It belongs to the vestibule, which is deliberately not a compartment — see
        // ShipLayout.VestibuleIsNotACompartment for why that room can never be dogged shut.
        Assert.Null(ShipLayout.CompartmentAt(ShipLayout.PlacardStation.X, ShipLayout.PlacardStation.Y));
        Assert.False(string.IsNullOrWhiteSpace(ShipLayout.VestibuleIsNotACompartment));
    }

    [Fact]
    public void EveryCompartmentSharesAtmosphereWithTheCorridorWhenItsDoorIsOpen()
    {
        // The whole point of the exercise: her rooms feed the SAME connectivity model the wreck's do, so one
        // valve board serves both ships and a pump prices what it will actually empty.
        var open = new List<HullVenting.Space>();
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            open.Add(new HullVenting.Space(r.Name, DoorShut: false, Vented: false,
                                           Infested: false, HoldsSurvivor: false));
        }

        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(HullVenting.SpineName, open);

        Assert.Equal(ShipLayout.Rooms.Length + 1, volume.Count);
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            Assert.Contains(r.Name, volume);
        }
    }
}
