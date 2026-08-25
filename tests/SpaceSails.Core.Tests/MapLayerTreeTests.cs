namespace SpaceSails.Core.Tests;

/// <summary>
/// #405 — the map's Layers filter, rebuilt as a collapsible tree. These pin the pure resolution the
/// draw path and picker lean on: the tri-state cascade, the legacy-key migration, the per-desk
/// defaults, the LayerVisible resolution, and the safety invariant that a threat is never hideable.
/// </summary>
public class MapLayerTreeTests
{
    private static HashSet<string> Hidden(params string[] keys) => new(keys);

    // ---- LayerVisible resolution ----

    [Fact]
    public void UnlistedInHidden_IsVisible()
    {
        Assert.True(MapLayerTree.IsVisible(Hidden(), "traffic.live"));
    }

    [Fact]
    public void HiddenLeaf_IsNotVisible()
    {
        Assert.False(MapLayerTree.IsVisible(Hidden("routes.plan"), "routes.plan"));
    }

    // ---- Threats-never-hidden safety invariant ----

    [Fact]
    public void PinnedThreatLeaf_IsAlwaysVisible_EvenWhenInHiddenSet()
    {
        // Even if something forced the key into the hidden set, a pinned leaf resolves visible.
        Assert.True(MapLayerTree.IsVisible(Hidden("threats.rock"), "threats.rock"));
    }

    [Fact]
    public void ToggleLeaf_CannotHideAPinnedThreat()
    {
        var hidden = Hidden();
        MapLayerTree.ToggleLeaf(hidden, "threats.rock");
        Assert.DoesNotContain("threats.rock", hidden);
        Assert.True(MapLayerTree.IsVisible(hidden, "threats.rock"));
    }

    [Fact]
    public void CascadeGroup_OnPinnedFamily_IsInert()
    {
        MapLayerTree.Group threats = MapLayerTree.Groups.Single(g => g.Key == "threats");
        var hidden = Hidden();
        MapLayerTree.CascadeGroup(hidden, threats);
        Assert.Empty(hidden);
        Assert.Equal(MapLayerTree.TriState.On, MapLayerTree.GroupStateOf(hidden, threats));
    }

    [Fact]
    public void PinnedGroup_IsMarkedPinned_AndOthersAreNot()
    {
        Assert.True(MapLayerTree.Groups.Single(g => g.Key == "threats").Pinned);
        Assert.All(MapLayerTree.Groups.Where(g => g.Key != "threats"), g => Assert.False(g.Pinned));
    }

    // ---- Tri-state cascade ----

    [Fact]
    public void GroupState_AllChildrenVisible_IsOn()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        Assert.Equal(MapLayerTree.TriState.On, MapLayerTree.GroupStateOf(Hidden(), traffic));
    }

    [Fact]
    public void GroupState_AllChildrenHidden_IsOff()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        var hidden = Hidden("traffic.live", "traffic.ghosts", "traffic.beacons");
        Assert.Equal(MapLayerTree.TriState.Off, MapLayerTree.GroupStateOf(hidden, traffic));
    }

    [Fact]
    public void GroupState_SomeChildrenHidden_IsMixed()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        Assert.Equal(MapLayerTree.TriState.Mixed, MapLayerTree.GroupStateOf(Hidden("traffic.beacons"), traffic));
    }

    [Fact]
    public void Cascade_FromOn_TurnsWholeFamilyOff()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        var hidden = Hidden();
        MapLayerTree.CascadeGroup(hidden, traffic);
        Assert.Equal(MapLayerTree.TriState.Off, MapLayerTree.GroupStateOf(hidden, traffic));
        Assert.All(traffic.Leaves, l => Assert.False(MapLayerTree.IsVisible(hidden, l.Key)));
    }

    [Fact]
    public void Cascade_FromMixed_TurnsWholeFamilyOn()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        var hidden = Hidden("traffic.beacons");
        MapLayerTree.CascadeGroup(hidden, traffic);
        Assert.Equal(MapLayerTree.TriState.On, MapLayerTree.GroupStateOf(hidden, traffic));
    }

    [Fact]
    public void Cascade_FromOff_TurnsWholeFamilyOn()
    {
        MapLayerTree.Group traffic = MapLayerTree.Groups.Single(g => g.Key == "traffic");
        var hidden = Hidden("traffic.live", "traffic.ghosts", "traffic.beacons");
        MapLayerTree.CascadeGroup(hidden, traffic);
        Assert.Equal(MapLayerTree.TriState.On, MapLayerTree.GroupStateOf(hidden, traffic));
    }

    // ---- Per-desk defaults (#953: the lanes are archived, so there is nothing left to hide) ----

    /// <summary>#953 · THE LANES ARE NOT DEFAULTED OFF ANY MORE, THEY ARE GONE. #971 shipped every desk with
    /// the trade lanes hidden, after the owner opened his sensors desk onto a sky "covered in faint lines with
    /// no intersection". The follow-up ruling archived the overlay outright — "we have never used them to find
    /// anything" — so the hidden set is empty and every desk opens on the whole tree. What replaced the
    /// default is a leaf that does not exist; <c>TheShipLanesAreArchivedTests</c> proves nothing draws it.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultHidden_HidesNothing_BecauseTheOneHiddenLayerWasArchived(bool sensorsDesk)
    {
        var hidden = MapLayerTree.DefaultHidden(sensorsDesk);

        Assert.Empty(hidden);
        foreach (string key in MapLayerTree.AllLeafKeys)
        {
            Assert.True(MapLayerTree.IsVisible(hidden, key), $"{key} should open visible");
        }
    }

    /// <summary>…and the row itself is off the panel, so no captain is offered a checkbox that turns nothing
    /// on. The rest of the Routes family is named in the same breath: a tree that had lost the whole family
    /// would otherwise satisfy "no lane row" perfectly.</summary>
    [Fact]
    public void TheRoutesFamilyOffersNoTradeLanesRow()
    {
        Assert.True(ShipLanes.Archived);

        MapLayerTree.Group routes = MapLayerTree.Groups.Single(g => g.Key == "routes");
        Assert.Equal(["routes.plan", "routes.rails"], routes.Leaves.Select(l => l.Key));
        Assert.DoesNotContain("routes.lanes", MapLayerTree.AllLeafKeys);
    }

    /// <summary>#963 · The 🛬 the owner could not read ("what is the small symbol next to ganymede… it
    /// should have some kind of text pop-up?") is a canvas glyph, so its MEANING has to live on the layer
    /// that draws it — the one row in the Layers panel a hover can reach.</summary>
    [Fact]
    public void LandableMarksLeaf_CarriesTheSentenceThatExplainsTheGlyph()
    {
        MapLayerTree.Leaf landable = MapLayerTree.Groups
            .SelectMany(g => g.Leaves)
            .Single(l => l.Key == "labels.landable");

        Assert.Contains("landable", landable.Hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shuttle", landable.Hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(landable.Icon, landable.Hint);
    }

    // ---- Legacy key migration ----

    [Theory]
    [InlineData("scans", "sensors.scans")]
    public void MigrateLegacyKey_MapsFlatKeyIntoTheTree(string oldKey, string expectedFirst)
    {
        IReadOnlyList<string> mapped = MapLayerTree.MigrateLegacyKey(oldKey);
        Assert.Contains(expectedFirst, mapped);
        Assert.All(mapped, m => Assert.Contains(m, MapLayerTree.AllLeafKeys));
    }

    /// <summary>#953 · An old note asking for "lanes" is asking for a layer that no longer exists, and the
    /// migration says so by landing nowhere. Answering with a key the tree does not carry would hand a caller
    /// a hidden-set entry that gates nothing — which is the same quiet nonsense as a checkbox that turns
    /// nothing on.</summary>
    [Fact]
    public void MigrateLegacyKey_Lanes_LandsNowhere_BecauseTheLayerWasArchived()
    {
        Assert.Empty(MapLayerTree.MigrateLegacyKey("lanes"));
    }

    [Fact]
    public void MigrateLegacyKey_Traffic_FansOutToAllTrafficLeaves()
    {
        Assert.Equal(["traffic.live", "traffic.ghosts", "traffic.beacons"], MapLayerTree.MigrateLegacyKey("traffic"));
    }

    [Fact]
    public void MigrateLegacyKey_Depots_SplitsIntoDepotMarkersAndMinorLabels()
    {
        // The old "depots" key gated BOTH the depot markers and the #404 minor-station labels.
        Assert.Equal(["ports.depots", "labels.minor"], MapLayerTree.MigrateLegacyKey("depots"));
    }

    [Fact]
    public void MigrateLegacyKey_UnknownKey_PassesThrough()
    {
        Assert.Equal(["something.else"], MapLayerTree.MigrateLegacyKey("something.else"));
    }

    // ---- Tree shape sanity ----

    [Fact]
    public void EveryLeafKey_IsUnique()
    {
        List<string> keys = MapLayerTree.AllLeafKeys.ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void EveryLeaf_ResolvesBackToItsGroup()
    {
        Assert.All(MapLayerTree.AllLeafKeys, k => Assert.NotNull(MapLayerTree.GroupOf(k)));
    }

    [Fact]
    public void RoutesFamily_DefaultsCollapsed()
    {
        Assert.True(MapLayerTree.Groups.Single(g => g.Key == "routes").DefaultCollapsed);
    }
}
