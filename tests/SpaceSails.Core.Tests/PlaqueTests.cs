using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// The commemorative plaques (owner's cruise ruling, 2026-07-19: "We could gen-AI the ships and docks
/// some space-dock plaques … add some depth to the world"). Covers the ship's builder's plate carrying
/// its service history (the Victoria-I "she used to be something" beat), that every walkable station has
/// its own non-empty dedication, that the dedications are distinct per port, and that each names the
/// station and points at plate art.
/// </summary>
public class PlaqueTests
{
    // The station ids HavenInterior builds walkable interiors (and now dedication plaques) for.
    private static readonly string[] WalkableStations =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt", "selene-gate", "red-eye", "the-deep"];

    [Fact]
    public void ShipBuildersPlate_CarriesCanonAndServiceHistory()
    {
        Plaque ship = Plaques.Ship;
        Assert.False(string.IsNullOrWhiteSpace(ship.ConsoleLabel));
        Assert.Equal("art/plaque-ship.jpg", ship.ArtUrl);

        // The canon set by this card (Koski & Daughters, Rauma Crater Luna, Hull No. 77, 2341)...
        Assert.Contains("Koski & Daughters", ship.Lore);
        Assert.Contains("Rauma Crater", ship.Lore);
        Assert.Contains("Hull No. 77", ship.Lore);
        Assert.Contains("2341", ship.Lore);
        // ...and the Victoria-I addendum: a builder's-plate card must carry a SERVICE HISTORY — the hull
        // used to be something, and shows its age now.
        Assert.Contains("glory days", ship.Lore);
        Assert.Contains("shows her age", ship.Lore);
    }

    [Fact]
    public void EveryWalkableStation_HasANonEmptyDedication_NamingThePort()
    {
        foreach (string id in WalkableStations)
        {
            Plaque? p = Plaques.For(id);
            Assert.NotNull(p);
            Assert.False(string.IsNullOrWhiteSpace(p!.ConsoleLabel), $"{id} plaque has no console label");
            Assert.False(string.IsNullOrWhiteSpace(p.Lore), $"{id} plaque has no dedication text");
            Assert.False(string.IsNullOrWhiteSpace(p.ArtUrl), $"{id} plaque has no art slot wired");
            Assert.StartsWith("art/plaque-", p.ArtUrl); // wired to an easel path (delivered or fallback)
        }
    }

    [Fact]
    public void Dedications_AreDistinctPerPort()
    {
        var lore = WalkableStations.Select(id => Plaques.For(id)!.Lore).ToList();
        Assert.Equal(lore.Count, lore.Distinct().Count()); // no two ports share a dedication
    }

    [Fact]
    public void KnownPorts_KeepTheirWorldbuildingCanon()
    {
        // Selene Gate — first port of the Moon, the mass-driver age.
        string selene = Plaques.For("selene-gate")!.Lore;
        Assert.Contains("2119", selene);
        Assert.Contains("mass-driver", selene, System.StringComparison.OrdinalIgnoreCase);

        // The Red Eye — the storm watch; "no storm outlasts the watcher".
        Assert.Contains("watcher", Plaques.For("red-eye")!.Lore, System.StringComparison.OrdinalIgnoreCase);

        // The Deep — the last port; the watch goes on.
        Assert.Contains("last port", Plaques.For("the-deep")!.Lore, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IceMoonProject_IsNamedOnlyOnRingside_AndEchoedUnnamedOnTheDeep()
    {
        // PROJEKTI KAAMOS is the mystery hook: named exactly once (Ringside), never explained.
        Assert.Contains("KAAMOS", Plaques.For("ringside-exchange")!.Lore);
        foreach (string id in WalkableStations.Where(s => s != "ringside-exchange"))
        {
            Assert.DoesNotContain("KAAMOS", Plaques.For(id)!.Lore); // the name surfaces nowhere else
        }

        // Both Ringside and The Deep gesture at the sealed "ice moon" berth; only Ringside gives it a name.
        Assert.Contains("ice moon", Plaques.For("ringside-exchange")!.Lore, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ice moon", Plaques.For("the-deep")!.Lore, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheDeep_IsTheBuildOriginException_TheOthersReadLocal()
    {
        // The Deep is the one hall not built where it floats — the mystique exception.
        Assert.Contains("not built here", Plaques.For("the-deep")!.Lore, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LifeboatMuster_IsStale_Stamped_AndKeepsItsAsterisk()
    {
        foreach (string id in WalkableStations)
        {
            string card = Plaques.LifeboatMuster(id);
            Assert.Contains("CAPACITY 40", card);
            Assert.Contains("LAST INSPECTED", card);
            Assert.Contains("*", card); // the asterisk does the work
            Assert.Equal(card, Plaques.LifeboatMuster(id)); // deterministic — canon, not per-run noise
        }
        Assert.False(string.IsNullOrWhiteSpace(Plaques.LifeboatLabel));
    }

    [Fact]
    public void For_UnknownBerth_HasNoPlaque()
    {
        Assert.Null(Plaques.For("earth"));
        Assert.Null(Plaques.For("not-a-station"));
    }

    [Fact]
    public void AllStationPlaques_MatchesTheWalkableSet()
    {
        Assert.Equal(WalkableStations.Length, Plaques.AllStationPlaques.Count);
    }
}
