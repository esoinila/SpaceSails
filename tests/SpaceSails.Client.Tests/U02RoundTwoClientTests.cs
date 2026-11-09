using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Round-2 test user U02 (Dov) · the client-side findings of checklist #1416, asked of the real page and
/// component through their own members (reflection, the idiom of the neighbouring Map benches).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class U02RoundTwoClientTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static Pages.Map NewMap()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        return map;
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden) ?? throw new InvalidOperationException($"no field {field}")).SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden) ?? throw new InvalidOperationException($"no field {field}")).GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden) ?? throw new InvalidOperationException($"no method {method}")).Invoke(o, args);

    // ── U02-F02 · "dockside now" was a drone ferry ──────────────────────────────────────────────────

    private static object NewLocalSpace()
    {
        object ls = Activator.CreateInstance(typeof(Pages.Stations.LocalSpace))!;
        typeof(Pages.Stations.LocalSpace).GetProperty("CargoUnits")!.SetValue(ls, 4);
        typeof(Pages.Stations.LocalSpace).GetProperty("CargoValue")!.SetValue(ls, 900);
        return ls;
    }

    private static CommerceRule.LocalContact AContact() =>
        (CommerceRule.LocalContact)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CommerceRule.LocalContact));

    [Theory]
    [InlineData(1.382e10, 14300.0)]   // Jupiter Depot, 13.82 M km away
    [InlineData(2.0e6, 0.0)]          // a partner 2,000 km off
    [InlineData(3_000.0, 0.0)]        // The Tilt, 3 km, matched: the dockside case
    public void ASameOrbitDealNamesTheDroneFerryAndItsTime(double distance, double relSpeed)
    {
        object ls = NewLocalSpace();
        MethodInfo buy = typeof(Pages.Stations.LocalSpace).GetMethod("BuyButtonLabel", Hidden)!;
        MethodInfo sell = typeof(Pages.Stations.LocalSpace).GetMethod("SellButtonLabel", Hidden)!;

        string bought = (string)buy.Invoke(ls, [CommerceRule.TradeMode.SameOrbit, AContact(), 1, 1200, relSpeed, distance])!;
        string sold = (string)sell.Invoke(ls, [AContact(), CommerceRule.TradeMode.SameOrbit, relSpeed, distance])!;

        foreach (string label in new[] { bought, sold })
        {
            Assert.Contains("no fee", label);
            Assert.Contains("drone", label);
            Assert.Matches(@"~\d+ min", label);
            Assert.DoesNotContain("now", label);
            // "dockside" only where the partner is within a few km at matched speed.
            Assert.Equal(distance <= 10_000 && relSpeed <= 50.0, label.Contains("dockside"));
        }
    }

    // ── U02-F24 · every post has its own price: the buy/sell row carries one told tail at a non-par post ──

    private static CommerceRule.LocalContact APost(string host, string cargoClass) =>
        new("depot-" + host, host + " Depot", CommerceRule.LocalContactKind.Depot, default, default, 0,
            CommerceRule.ActionKind.Trade, cargoClass, 40, HostBodyId: host);

    private static string SellLabelAt(string host, string cargoClass)
    {
        object ls = Activator.CreateInstance(typeof(Pages.Stations.LocalSpace))!;
        typeof(Pages.Stations.LocalSpace).GetProperty("CargoUnits")!.SetValue(ls, 10);
        typeof(Pages.Stations.LocalSpace).GetProperty("CargoValue")!.SetValue(ls, 1000);
        typeof(Pages.Stations.LocalSpace).GetProperty("CargoByClass")!.SetValue(ls, new Dictionary<string, int> { [cargoClass] = 10 });
        return (string)typeof(Pages.Stations.LocalSpace).GetMethod("SellButtonLabel", Hidden)!
            .Invoke(ls, [APost(host, cargoClass), CommerceRule.TradeMode.Shuttle, 100.0, 1e6])!;
    }

    [Fact]
    public void TheSellRowCarriesThePostsTailOffParAndNothingAtPar()
    {
        string earth = SellLabelAt("earth", "Ice");
        Assert.EndsWith(" · −15% here — Earth is short of nothing.", earth);
        Assert.Contains("+600 cr", earth); // 10 x 85 less the 250 cr shuttle fee
        string selene = SellLabelAt("selene-gate", "Ice");
        Assert.DoesNotContain("% here", selene);
        Assert.DoesNotContain(" · −", selene);
    }

    [Fact]
    public void TheBuyRowCarriesThePostsTailOffParAndNothingAtPar()
    {
        object ls = Activator.CreateInstance(typeof(Pages.Stations.LocalSpace))!;
        MethodInfo buy = typeof(Pages.Stations.LocalSpace).GetMethod("BuyButtonLabel", Hidden)!;
        string deep = (string)buy.Invoke(ls, [CommerceRule.TradeMode.Shuttle, APost("the-deep", "He3"), 2, 1080, 100.0, 1e6])!;
        Assert.EndsWith(" · −10% here — the moons make it.", deep);
        string selene = (string)buy.Invoke(ls, [CommerceRule.TradeMode.Shuttle, APost("selene-gate", "He3"), 2, 1200, 100.0, 1e6])!;
        Assert.DoesNotContain("% here", selene);
    }

    // ── U02-F11 · one distance formatter on the desk ────────────────────────────────────────────────

    [Fact]
    public void TheTradeDeskPrintsThousandsSeparatorsBelowTheMegaKmSwitch()
    {
        MethodInfo fmt = typeof(Pages.Stations.LocalSpace).GetMethod("FormatDistance", Hidden | BindingFlags.Static)!;
        Assert.Equal("387,401 km", (string)fmt.Invoke(null, [387_401_000.0])!);
        Assert.Equal("2 km", (string)fmt.Invoke(null, [1_997.0])!);
        Assert.Equal("13.82 M km", (string)fmt.Invoke(null, [1.382e10])!);
    }

    // ── U02-F03 · the keep at an underground counter does not speak a haven's regulars ───────────────

    [Fact]
    public void AnUndergroundKeepNeverNamesAHavensRegulars()
    {
        Barkeep underground = CounterService.For("luna", UndergroundComplex.Comfort.UpperCanteen)
            ?? throw new InvalidOperationException("no branch counter");
        int havens = 0, roomsSaid = 0;
        foreach (string haven in new[] { "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "red-eye", "the-deep", "the-tilt" })
        {
            Barkeep bar = Barkeeps.For(haven) ?? throw new InvalidOperationException(haven);
            havens++;
            foreach (double t in new[] { 0.0, 5 * 3600.0, 9 * 3600.0, 40 * 3600.0, 123456.0 })
            {
                var map = NewMap();
                Set(map, "_dockedHavenId", haven);
                Set(map, "_dockVisitSimTime", t);

                // At the haven's own bar the roster is told (the premise: the sentence exists)...
                Set(map, "_barMenu", bar);
                if ((string?)Invoke(map, "WhoIsInTonight") is { Length: > 0 })
                {
                    roomsSaid++;
                }

                // ...and at the company counter behind the same docked haven it is not.
                Set(map, "_barMenu", underground);
                Assert.Null((string?)Invoke(map, "WhoIsInTonight"));
            }
        }

        Assert.Equal(7, havens);
        Assert.True(roomsSaid > 0, "no haven bar ever told its roster - the bench proves nothing.");
    }

    // ── U02-F16 · the Magpie and the shelf agree ─────────────────────────────────────────────────────

    [Fact]
    public void TheMagpieDoesNotPointAtAParcelThatIsNotOnTheShelf()
    {
        const string haven = "cinder-roost";
        string hatch = HavenInterior.WingCatalog(haven).First().UnlockHatchId;
        double hour9 = 9 * 3600.0;

        string? SaidWith(bool crackJobOnTheBooks)
        {
            var map = NewMap();
            Set(map, "_dockedHavenId", haven);
            Set(map, "SimTime", hour9);
            Get<HashSet<string>>(map, "_unlockedHatches").Add($"{haven}:{hatch}");
            if (crackJobOnTheBooks)
            {
                Get<List<Pages.Map.Quest>>(map, "_quests").Add(new Pages.Map.Quest("crack-1", Pages.Map.QuestKind.Crack, "THE FIXER", hatch,
                    "the bonded stores package", "Crack hatch", "[test]", 2600,
                    DestBodyId: haven, SourceBodyId: haven, Pin: "0000") { State = Pages.Map.QuestState.Active });
            }

            NpcPost m = HavenInterior.ResolveMagpie(hour9, backRoomOpen: true);
            Assert.True(m.Present && m.Location == "BACK ROOM", "the bench is not standing at her third stop.");
            var spot = new DeckPlan.ConsoleSpot(DeckPlan.ConsoleKind.BarPatron, (float)m.X, (float)m.Y, "◈ THE MAGPIE");
            Invoke(map, "TalkToMagpie", spot);
            return Get<PulseSlot>(map, "_pulse").Message;
        }

        string? withJob = SaidWith(true);
        string? noJob = SaidWith(false);
        Assert.Contains("parcel", withJob ?? "", StringComparison.OrdinalIgnoreCase);
        // The stash says "nothing here worth pocketing" with no job, so she must not promise a parcel there.
        Assert.DoesNotContain("on the shelf", noJob ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("back shelf", noJob ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── U02-F17 · no "at this table" beside a standing patron ───────────────────────────────────────

    [Fact]
    public void TheKeybarOffersNoTableBesideTheStandingMagpie()
    {
        const string haven = "cinder-roost";
        string hatch = HavenInterior.WingCatalog(haven).First().UnlockHatchId;
        var unlocked = new HashSet<string> { hatch };
        double hour9 = 9 * 3600.0;
        DeckPlan plan = HavenInterior.DockedDeck(haven, unlocked, hour9) ?? throw new InvalidOperationException("no deck");

        DeckPlan.ConsoleSpot magpie = plan.Consoles.First(c =>
            c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase));
        DeckPlan.ConsoleSpot seated = plan.Consoles.First(c =>
            c.Kind == DeckPlan.ConsoleKind.BarPatron && !c.Label.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase));

        string? HintsAt(DeckPlan.ConsoleSpot at)
        {
            var map = NewMap();
            Set(map, "_deckPlan", plan);
            Set(map, "_deckMode", true);
            Set(map, "_avatarX", (double)at.X);
            Set(map, "_avatarY", (double)at.Y);
            return (string?)Invoke(map, "BuildDeckKeyHints");
        }

        Assert.DoesNotContain("at this table", HintsAt(magpie) ?? "");
        // The premise: the hint still shows where there IS a table (a seated regular).
        Assert.Contains("at this table", HintsAt(seated) ?? "");
    }

    // ── U02-F05 · the purse rides the Trade tile while a ferry runs ─────────────────────────────────

    [Fact]
    public void TheTradeTileKeepsThePurseWhileADroneFerryRuns()
    {
        var map = NewMap();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_ephemeris", eph);
        Set(map, "_ship", new ShipState(eph.Position("earth", 0), Vector2d.Zero, 0.0));
        Set(map, "_credits", 7160);
        Set(map, "_localTradeTargetId", "somebody");
        Set(map, "_localTradeProgress", 0.01);

        object chip = Invoke(map, "TradeChip")!;
        string line1 = (string)chip.GetType().GetProperty("Line1")!.GetValue(chip)!;
        string? line2 = (string?)chip.GetType().GetProperty("Line2")!.GetValue(chip);
        Assert.StartsWith("drones →", line1);
        Assert.Equal("7,160 cr", line2);
    }
}
