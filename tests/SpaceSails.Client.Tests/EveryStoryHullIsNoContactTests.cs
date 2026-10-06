using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1357 - every story hull parked as a depot NpcState (the charter hull AND the old ship) is no contact: the
/// trade list, war room, hunt booth, boarding window and interest target never carry her. Shape and rig follow
/// <see cref="TheCharterHullIsNoContactTests"/> (which stays untouched); the control ship keeps every verb
/// honest, since a verb returning nothing would pass an "absent" assertion for the wrong reason. The hunt
/// booth's draw is weighted on sim-time, so it is swept, never sampled once.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(StopRegisterCollection.Name)]
public sealed class EveryStoryHullIsNoContactTests
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Rock = "luna";

    private sealed class Rig
    {
        public required Pages.Map Map;
        public required string HullId;
        public required string ControlId;
    }

    public static IEnumerable<object[]> Hulls() => [["charter"], ["oldship"]];

    private static object? Get(object on, string name) =>
        on.GetType().GetField(name, Any)?.GetValue(on) ?? on.GetType().GetProperty(name, Any)?.GetValue(on);

    private static void Set(object on, string name, object? value)
    {
        FieldInfo? f = on.GetType().GetField(name, Any);
        if (f is not null) { f.SetValue(on, value); return; }
        on.GetType().GetProperty(name, Any)!.SetValue(on, value);
    }

    private static object Call(Pages.Map map, string method, params object?[] args) =>
        typeof(Pages.Map).GetMethod(method, Any)!.Invoke(map, args)!;

    private static async Task<Rig> Rigged(string kind)
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        TheBootBuildsTheSameWorldTests.Hand(map, "Http", TheBootBuildsTheSameWorldTests.ScenariosFromDisk());
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench("/map"));
        try
        {
            await (Task)typeof(Pages.Map).GetMethod("BootTheWorldAsync", Any)!.Invoke(map, [CancellationToken.None])!;
        }
        catch (Exception)
        {
            // the browser gate, reached after the world was built
        }
        Assert.NotNull(Get(map, "_ephemeris"));

        string hullId;
        if (kind == "charter")
        {
            Set(map, "_shuttle", new ReturningShuttle.Row(Rock, 0));
            Call(map, "TheCharterHullIsOnTheBoard");
            hullId = ReturningShuttle.ShipIdFor(Rock);
        }
        else
        {
            var eph = (ICelestialEphemeris)Get(map, "_ephemeris")!;
            var old = new Pages.Map.NpcState { Ship = TheOldShip.Berthed(eph, Rock, "test-thread") };
            Set(map, "_npcStates", ((IEnumerable<Pages.Map.NpcState>)Get(map, "_npcStates")!).Append(old).ToArray());
            hullId = TheOldShip.ShipId;
        }

        var states = ((IEnumerable)Get(map, "_npcStates")!).Cast<object>().ToList();
        string idOf(object n) => ((NpcShip)Get(n, "Ship")!).Id;
        object hull = states.Single(n => idOf(n) == hullId);
        object? control = states.FirstOrDefault(n =>
            !StoryHulls.IsOne(idOf(n)) && !((NpcShip)Get(n, "Ship")!).IsPod
            && ((NpcShip)Get(n, "Ship")!).DepotBodyId is null);
        Assert.NotNull(control);

        foreach (object n in states)
        {
            bool live = ReferenceEquals(n, hull) || ReferenceEquals(n, control);
            Set(n, "Active", live);
            Set(n, "Arrived", false);
            Set(n, "Boarded", false);
            Set(n, "CurrentlyObserved", live);
        }

        return new Rig { Map = map, HullId = hullId, ControlId = idOf(control!) };
    }

    [Theory, MemberData(nameof(Hulls))]
    public async Task TheTradeListNeverContainsTheHull(string kind)
    {
        Rig r = await Rigged(kind);
        var ids = ((IEnumerable)Call(r.Map, "LocalShips")).Cast<CommerceRule.LocalShip>().Select(s => s.Id).ToArray();
        Assert.Contains(r.ControlId, ids);
        Assert.DoesNotContain(r.HullId, ids);
    }

    [Theory, MemberData(nameof(Hulls))]
    public async Task TheWarRoomNeverContainsTheHull(string kind)
    {
        Rig r = await Rigged(kind);
        var ids = ((IEnumerable)Call(r.Map, "WarRoomContacts")).Cast<object>()
            .Select(c => ((NpcShip)Get(c, "Ship")!).Id).ToArray();
        Assert.Contains(r.ControlId, ids);
        Assert.DoesNotContain(r.HullId, ids);
    }

    /// <summary>The booth's pick is a weighted draw on sim-time: sweep 120 sim-times, every offer is the control.</summary>
    [Theory, MemberData(nameof(Hulls))]
    public async Task TheHuntBoothNeverOffersTheHull(string kind)
    {
        Rig r = await Rigged(kind);
        for (int i = 0; i < 120; i++)
        {
            Set(r.Map, "SimTime", i * 977.0);
            object? offer = Call(r.Map, "MakeHuntOffer", "a stranger");
            Assert.NotNull(offer);
            Assert.Equal(r.ControlId, (string)Get(offer!, "TargetShipId")!);
        }
    }

    [Theory, MemberData(nameof(Hulls))]
    public async Task TheHullIsNeverABoardingCandidate(string kind)
    {
        Rig r = await Rigged(kind);
        Set(r.Map, "_selectedTargetId", r.ControlId);
        Assert.NotNull(Call(r.Map, "SelectedCaptureTarget"));

        Set(r.Map, "_selectedTargetId", r.HullId);
        Assert.Null(typeof(Pages.Map).GetMethod("SelectedCaptureTarget", Any)!.Invoke(r.Map, null));
    }

    [Theory, MemberData(nameof(Hulls))]
    public async Task TheHullIsNeverATargetOfInterest(string kind)
    {
        Rig r = await Rigged(kind);
        Set(r.Map, "_interestTargetId", null);
        try { Call(r.Map, "SetInterestTarget", r.ControlId); } catch (TargetInvocationException) { }
        Assert.Equal(r.ControlId, Get(r.Map, "_interestTargetId"));

        Set(r.Map, "_interestTargetId", null);
        try { Call(r.Map, "SetInterestTarget", r.HullId); } catch (TargetInvocationException) { }
        Assert.Null(Get(r.Map, "_interestTargetId"));
    }

    private static string Page(string name) => File.ReadAllText(Path.Combine(
        TheBootBuildsTheSameWorldTests.RepoRoot(), "src", "SpaceSails.Client", "Pages", name));

    // The ordnance loop, the finder pool and the universe-switch purge are not reachable as a pure answer from
    // outside (a step of the simulation, a null case, a thread switch), so each is pinned by source law: the
    // code that walks the roster asks StoryHulls.IsOne.

    [Fact]
    public void TheOrdnanceLoopAsksWhetherItIsAStoryHull()
    {
        string ordnance = Page("Map.Combat.Ordnance.cs");
        int loop = ordnance.IndexOf("Hit anything in the way", StringComparison.Ordinal);
        Assert.True(loop > 0);
        Assert.Contains("StoryHulls.IsOne", ordnance[loop..(loop + 700)]);
    }

    [Fact]
    public void TheFinderPoolAsksWhetherItIsAStoryHull()
    {
        string finder = Page("Map.Finder.cs");
        int hulls = finder.IndexOf("new List<FinderCase.Hull>", StringComparison.Ordinal);
        Assert.True(hulls > 0);
        Assert.Contains("StoryHulls.IsOne", finder[hulls..(hulls + 500)]);
    }

    [Fact]
    public void TheUniverseSwitchPurgeAsksWhetherItIsAStoryHull()
    {
        string ads = Page("Map.Ads.cs");
        int purge = ads.IndexOf("A universe switch leaves the last world", StringComparison.Ordinal);
        Assert.True(purge > 0);
        Assert.Contains("StoryHulls.IsOne", ads[purge..(purge + 400)]);
    }

    /// <summary>The ship menu's 🎯 press on a story hull is silent: no interest, no "Target of interest" pulse
    /// (silence, never new prose). On the control ship it still marks her and writes the pulse.</summary>
    [Theory, MemberData(nameof(Hulls))]
    public async Task TheMenuInterestPressIsSilentForTheHullAndStillWorksForTheControl(string kind)
    {
        Rig r = await Rigged(kind);

        Set(r.Map, "_interestTargetId", null);
        object? before = Get(r.Map, "_pulse");
        try { Call(r.Map, "InterestFromMenu", r.HullId); } catch (TargetInvocationException) { }
        Assert.Null(Get(r.Map, "_interestTargetId"));
        Assert.Equal(before, Get(r.Map, "_pulse"));

        try { Call(r.Map, "InterestFromMenu", r.ControlId); } catch (TargetInvocationException) { }
        Assert.Equal(r.ControlId, Get(r.Map, "_interestTargetId"));
        Assert.NotEqual(before, Get(r.Map, "_pulse"));
    }

    /// <summary>The 🎯 buttons (ship menu, dossier) are not drawn for a story hull: the markup gates each on
    /// StoryHulls.IsOne (source law; the panels take a dozen delegates and are not renderable alone).</summary>
    [Fact]
    public void TheInterestButtonsAreGatedOnTheStoryHullPredicate()
    {
        string menu = Page(Path.Combine("Map", "ShipMenuPanel.razor"));
        int m = menu.IndexOf("Mark her a target of interest", StringComparison.Ordinal);
        Assert.True(m > 0);
        Assert.Contains("!StoryHulls.IsOne(menuShipId)", menu[Math.Max(0, m - 250)..m]);

        string card = Page(Path.Combine("Map", "DossierCard.razor"));
        int d = card.IndexOf("🎯 interest</button>", StringComparison.Ordinal);
        Assert.True(d > 0);
        Assert.Contains("!StoryHulls.IsOne(dossierId)", card[Math.Max(0, d - 500)..d]);
    }
}
