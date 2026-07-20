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
        Assert.False(MapLayerTree.IsVisible(Hidden("routes.lanes"), "routes.lanes"));
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

    // ---- Per-desk defaults (preserves the lanes-off default) ----

    [Fact]
    public void DefaultHidden_NonSensorsDesk_HidesTradeLanesOnly()
    {
        var hidden = MapLayerTree.DefaultHidden(isSensorsDesk: false);
        Assert.Equal(new HashSet<string> { "routes.lanes" }, hidden);
        Assert.False(MapLayerTree.IsVisible(hidden, "routes.lanes"));
        Assert.True(MapLayerTree.IsVisible(hidden, "routes.plan"));
    }

    [Fact]
    public void DefaultHidden_SensorsDesk_ShowsEverything()
    {
        var hidden = MapLayerTree.DefaultHidden(isSensorsDesk: true);
        Assert.Empty(hidden);
        Assert.True(MapLayerTree.IsVisible(hidden, "routes.lanes"));
    }

    // ---- Legacy key migration ----

    [Theory]
    [InlineData("lanes", "routes.lanes")]
    [InlineData("scans", "sensors.scans")]
    public void MigrateLegacyKey_MapsFlatKeyIntoTheTree(string oldKey, string expectedFirst)
    {
        IReadOnlyList<string> mapped = MapLayerTree.MigrateLegacyKey(oldKey);
        Assert.Contains(expectedFirst, mapped);
        Assert.All(mapped, m => Assert.Contains(m, MapLayerTree.AllLeafKeys));
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
