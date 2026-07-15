using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Wednesday plan §3 PR-F / Tuesday vision §6 ("Doors that grow the world"): opening an unlockable
/// hatch appends a wing to a station's deck plan at runtime. The rules of which wings are welded on,
/// whether a hatch grows a room, and how a sealed hall edge is carved into a walkable doorway are
/// pure and deterministic (repo agreement §9), tested here without the client's renderer.
/// </summary>
public class DeckExpansionTests
{
    private static DeckWing BackRoom(string station = "cinder-roost", string hatch = "V-06") => new(
        Id: $"{station}-backroom",
        StationBodyId: station,
        UnlockHatchId: hatch,
        LocationName: "BACK ROOM",
        Walls: [new WingWall(-29, 28, -23, 18)],
        Doors: [new WingDoor(-12, 33, -10, 30, Locked: false)],
        Consoles: [new WingConsole(WingConsoleKind.Stash, -24, 24, "📦 FENCE'S STASH")],
        Labels: [new WingLabel(-21, 26, "BONDED STORES · BACK ROOM")]);

    private static readonly DeckWing[] Catalog =
    [
        BackRoom(),
        BackRoom(station: "the-space-bar", hatch: "M-06"),
    ];

    [Fact]
    public void ActiveWings_WeldsOnlyUnlockedHatchesForThisStation()
    {
        var unlocked = new HashSet<string> { "V-06" };

        List<DeckWing> venus = DeckExpansions.ActiveWings(Catalog, "cinder-roost", unlocked).ToList();
        Assert.Single(venus);
        Assert.Equal("cinder-roost-backroom", venus[0].Id);

        // Same unlock id, different station: nothing welded (unlocks are station-scoped by catalog).
        Assert.Empty(DeckExpansions.ActiveWings(Catalog, "the-space-bar", unlocked));
    }

    [Fact]
    public void ActiveWings_EmptyUnlockSet_WeldsNothing()
    {
        Assert.Empty(DeckExpansions.ActiveWings(Catalog, "cinder-roost", new HashSet<string>()));
    }

    [Fact]
    public void GrowsBehind_TrueOnlyForCatalogedHatches()
    {
        Assert.True(DeckExpansions.GrowsBehind(Catalog, "cinder-roost", "V-06"));
        Assert.False(DeckExpansions.GrowsBehind(Catalog, "cinder-roost", "V-01")); // a plain sealed berth
        Assert.False(DeckExpansions.GrowsBehind(Catalog, "the-tilt", "U-06"));     // no wing authored there
    }

    [Fact]
    public void CarveDoorway_KeepsEndStubsAndOpensAnUnlockedMiddle()
    {
        // A unit edge from (0,0) to (10,0), doorway across the middle 30%..70%.
        (WingWall stubA, WingWall stubB, WingDoor door) = DeckExpansions.CarveDoorway(0, 0, 10, 0, 0.30f, 0.70f);

        Assert.Equal((0f, 0f, 3f, 0f), (stubA.X1, stubA.Y1, stubA.X2, stubA.Y2));   // A-end stub
        Assert.Equal((7f, 0f, 10f, 0f), (stubB.X1, stubB.Y1, stubB.X2, stubB.Y2));  // B-end stub
        Assert.Equal((3f, 0f, 7f, 0f), (door.X1, door.Y1, door.X2, door.Y2));       // 4-wide opening
        Assert.False(door.Locked);                                                  // you cracked it — walkable
    }

    [Theory]
    [InlineData(-0.1f, 0.5f)]
    [InlineData(0.6f, 0.4f)]
    [InlineData(0.2f, 1.5f)]
    public void CarveDoorway_RejectsBadGap(float start, float end)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeckExpansions.CarveDoorway(0, 0, 10, 0, start, end));
    }

    [Fact]
    public void Validate_AcceptsAWellFormedWing()
    {
        DeckWing wing = DeckExpansions.Validate(BackRoom());
        Assert.Equal("cinder-roost-backroom", wing.Id);
    }

    [Fact]
    public void Validate_RejectsALockedConnectingDoor()
    {
        DeckWing bad = BackRoom() with { Doors = [new WingDoor(-12, 33, -10, 30, Locked: true)] };
        Assert.Throws<ArgumentException>(() => DeckExpansions.Validate(bad));
    }

    [Fact]
    public void Validate_RejectsAnUnenclosedWing()
    {
        DeckWing bad = BackRoom() with { Walls = [] };
        Assert.Throws<ArgumentException>(() => DeckExpansions.Validate(bad));
    }
}
