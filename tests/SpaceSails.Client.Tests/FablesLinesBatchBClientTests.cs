using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Fable's lines, batch B (11-07) — the page-side wiring: R1-F13 (a head-office dead floor's shudder), R1-F23
/// (an impact into a station and the <c>?death=impact</c> cheat at one), R1-F26/F40 (a wreck shudders aloud),
/// R1-F69 (the tank card's face) and U03-F07 (the gig rock's card, row and console).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class FablesLinesBatchBClientTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static readonly string[] AllPlates = ["Ringside Exchange"];

    private static Pages.Map OnAFloorOf(string bodyId, int floor)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(bodyId, "Somewhere", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "x", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_avatarX", 0.0);
        Set(map, "_avatarY", 0.0);
        Set(map, "_shudderSeed", 0xC0FFEE_1234UL);
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden) ?? throw new InvalidOperationException(field)).SetValue(target, value);

    private static string TheShudderSaid(Pages.Map map, int index)
    {
        Set(map, "_shudderIndex", index);
        typeof(Pages.Map).GetMethod("FireShudder", Hidden)!.Invoke(map, [0.0]);
        var slot = (PulseSlot)typeof(Pages.Map).GetField("_pulse", Hidden)!.GetValue(map)!;
        Assert.NotNull(slot.Message);
        return slot.Message!.TrimStart('〰', ' ');
    }

    // ── R1-F13 ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ADeadFloorOfTheHeadOfficeShuddersInItsOwnVoiceAndAHiveFloorKeepsTheSealedSitesVoice()
    {
        var hq = HullShudder.LinesFor(HullShudder.Setting.HeadOfficeVacuum, HullShudder.HavenRoom.Concourse);
        var sealedSite = HullShudder.LinesFor(HullShudder.Setting.DeepSite, HullShudder.HavenRoom.Concourse);
        for (int i = 0; i < 12; i++)
        {
            Assert.Contains(TheShudderSaid(OnAFloorOf(KaamosLore.IceMoonBodyId, -2), i), hq);
            Assert.Contains(TheShudderSaid(OnAFloorOf("miranda", -2), i), sealedSite);
        }
    }

    /// <summary>The two call sites the pure selectors cannot reach: the card raised on the first dead floor and
    /// the locked door's body each ask the head office before they speak.</summary>
    [Fact]
    public void TheDeadAirCardAndTheLockedDoorAskTheHeadOfficeBeforeTheySpeak()
    {
        string ride = MapMarkup.PagesFamily("Map.Surface.Hive.Ride.cs");
        Assert.Contains("UndergroundComplex.IsHeadOffice(ex.Stop.Body.Id)", ride, StringComparison.Ordinal);
        Assert.Contains("UndergroundComplex.HeadOfficeVacuumCard(", ride, StringComparison.Ordinal);
        Assert.Contains("UndergroundComplex.VacuumCard(", ride, StringComparison.Ordinal);

        string sign = MapMarkup.PagesFamily("Map.Surface.Hive.Search.cs");
        Assert.Contains("UndergroundComplex.IsHeadOffice(hq.Stop.Body.Id)", sign, StringComparison.Ordinal);
        Assert.Contains("UndergroundComplex.HeadOfficeLockedLine(sign)", sign, StringComparison.Ordinal);
        Assert.Contains("UndergroundComplex.LockedLine(sign)", sign, StringComparison.Ordinal);
    }

    // ── R1-F26 / R1-F40 ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AWreckShudderSpeaksTheDeadHullPoolAndNothingOfTheGround()
    {
        var pool = HullShudder.LinesFor(HullShudder.Setting.DeadHull, HullShudder.HavenRoom.Concourse);
        string[] ground = ["regolith", "kiosk", "dig", "rock", "tremor"];
        for (int i = 0; i < 24; i++)
        {
            string said = TheShudderSaid(OnAFloorOf(Derelict.BodyIdFor("test-hull"), 0), i);
            Assert.Contains(said, pool);
            foreach (string bad in ground)
            {
                Assert.DoesNotContain(bad, said, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── R1-F23 ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheImpactCheatAtAStationNamesTheHostWorldAndAGroundImpactReadsTheGround()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?death=impact&start=earth");
        await bench.RenderAsync();
        object busted = bench.Peek("_busted")!;
        Assert.NotNull(busted);
        Type t = busted.GetType();
        Assert.False((bool)t.GetProperty("ImpactIntoStation")!.GetValue(busted)!);
        Assert.Equal("Earth", (string)t.GetProperty("DeathBodyName")!.GetValue(busted)!);
    }

    [Fact]
    public async Task ARealImpactIntoAStationIsToldHullOnHull()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?start=earth");
        await bench.RenderAsync();
        string station = HavenInterior.InteriorBodyIds.First();
        Assert.Null(bench.Peek("_busted"));

        object crossing = new SurfaceImpact.Crossing(station, "Ringside Exchange", 1.0, 0.0, Vector2d.Zero);
        bench.CallOnTheDispatcher("TriggerImpact", crossing);

        object busted = bench.Peek("_busted")!;
        Type t = busted.GetType();
        Assert.True((bool)t.GetProperty("ImpactIntoStation")!.GetValue(busted)!);

        string pulse = ((PulseSlot)bench.Peek("_pulse")!).Message!;
        Assert.Contains("Hull on hull: the berth you were meant to slow for.", pulse, StringComparison.Ordinal);
        Assert.StartsWith("💥 IMPACT — the ship struck Ringside Exchange.", pulse, StringComparison.Ordinal);

        DeskBench.Painted painted = await bench.RenderAsync();
        string quote = string.Join(" ", painted.Root.Descendants().Where(n => n.HasClass("busted-quote")).Select(n => n.Spoken));
        Assert.DoesNotContain("periapsis", quote, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ringside Exchange", quote, StringComparison.Ordinal);
    }

    /// <summary>#1472 item 1: the impact freeze card's caption and art title fork on <c>ImpactIntoStation</c>.
    /// RED: revert the two forks in BustedCard.razor's Impact case.</summary>
    [Fact]
    public async Task TheImpactFreezeCardNamesTheBerthAtAStationAndTheSurfaceOnTheGround()
    {
        const string stationCaption = "\"…the approach said 'berth ahead'. Nobody said 'slow'.\"";
        const string groundCaption = "\"…the periapsis said 'under the surface'. It wasn't bluffing.\"";

        using (DeskBench station = await DeskBench.BootAsync("/map?start=earth"))
        {
            await station.RenderAsync();
            string id = HavenInterior.InteriorBodyIds.First();
            station.CallOnTheDispatcher("TriggerImpact", new SurfaceImpact.Crossing(id, "Ringside Exchange", 1.0, 0.0, Vector2d.Zero));
            DeskBench.Painted painted = await station.RenderAsync();
            var caption = painted.Root.Descendants().Single(n => n.HasClass("busted-freeze-caption"));
            Assert.Equal(stationCaption, caption.Spoken);
            var art = painted.Root.Descendants().Single(n => n.HasClass("busted-explosion"));
            Assert.Equal("art: the ship folding into a station's plating at the berth — hull on hull, the kerosene-explosion budget",
                art.Attributes["title"]);
        }

        using (DeskBench ground = await DeskBench.BootAsync("/map?death=impact&start=earth"))
        {
            DeskBench.Painted painted = await ground.RenderAsync();
            var caption = painted.Root.Descendants().Single(n => n.HasClass("busted-freeze-caption"));
            Assert.Equal(groundCaption, caption.Spoken);
        }
    }

    // ── R1-F69 ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLowTankCardWearsTheUndergroundFaceUndergroundAndTheSurfaceOneOnTheSurface()
    {
        PropertyInfo face = typeof(Pages.Map).GetProperty("AirCardFace", Hidden)!;

        var below = (AirCard.Face)face.GetValue(OnAFloorOf("miranda", -2))!;
        Assert.Equal(AirCard.DismissUnderground, below.Dismiss);
        Assert.Equal(AirCard.BeatsUnderground, below.Beats);
        (string name, int up) = UndergroundComplex.NearestAir("miranda", -2);
        Assert.Equal(AirCard.HeadUnderground(name, up), below.Head);

        var above = (AirCard.Face)face.GetValue(OnAFloorOf("miranda", 0))!;
        Assert.Equal(AirCard.Head, above.Head);
        Assert.Equal(AirCard.Dismiss, above.Dismiss);
        Assert.Same(AirCard.Beats, above.Beats);
    }

    // ── U03-F07 ───────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map WithTheGigRock(out string rockId)
    {
        var map = new Pages.Map();
        Type plan = typeof(Pages.Map).GetNestedType("DeflectionPlan", Hidden)!;
        rockId = DeflectionGig.BodyId;
        object d = Activator.CreateInstance(plan,
            "ringside", "Ringside Exchange", rockId, "Hull Breaker", new RockType(RockComposition.MType), "earth",
            1.0, 1.0, 0.0, 1.0, default(DeflectionGig.RockRail), 100.0, 0.0, 1000, 0.0)!;
        Set(map, "_deflection", d);
        return map;
    }

    [Fact]
    public void TheGigRockCarriesItsOwnBlurbAndItsOwnRowAndNoOtherBodyDoes()
    {
        Pages.Map map = WithTheGigRock(out string rock);
        MethodInfo row = typeof(Pages.Map).GetMethod("GigRockWindowRow", Hidden)!;

        Assert.Equal("⏳ T−6:00 to Ringside Exchange · shuttle ETA 12 m — the rock does not wait for a window",
            (string?)row.Invoke(map, [rock, 720.0]));
        Assert.Null(row.Invoke(map, ["luna", 720.0]));
        Assert.Null(row.Invoke(new Pages.Map(), [rock, 720.0]));

        // The blurb needs the board target; with none set it is null (and so the coin dial stays).
        PropertyInfo blurb = typeof(Pages.Map).GetProperty("GigRockBlurb", Hidden)!;
        Assert.Null(blurb.GetValue(map));

        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object Stop(string id) => Activator.CreateInstance(stopType,
            new CelestialBody(id, "Somewhere", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        Set(map, "_boardTarget", Stop(rock));
        Assert.Equal(
            DeflectionGig.BoardingBlurb("Hull Breaker", new RockType(RockComposition.MType), "Ringside Exchange"),
            blurb.GetValue(map));
        Set(map, "_boardTarget", Stop("luna"));
        Assert.Null(blurb.GetValue(map));
    }

    [Fact]
    public async Task TheBoardingCardOnTheGigRockCarriesTheBlurbAndDropsTheCoinDial()
    {
        // An ordinary ground keeps the coin dial and the old blurb.
        DeskBench.Painted ground = await TheBoardingCard(withTheGigRock: false);
        Assert.Contains(ground.Root.Descendants(), n => n.Spoken.Contains("Coin in the chest", StringComparison.Ordinal));
        Assert.Contains(ground.Root.Descendants(), n => n.Spoken.Contains("Hold loaded with it", StringComparison.Ordinal));
        Assert.Contains(ground.Root.Descendants(), n => n.Spoken.Contains("Beach-comber kit", StringComparison.Ordinal));

        // The gig rock: no coin dial and no hold row (its blurb is printed in the bury-note slot).
        DeskBench.Painted rock = await TheBoardingCard(withTheGigRock: true);
        // The rock card prints the blurb in the bury-note slot (this was blank once: an empty <text></text>).
        Assert.Contains(rock.Root.Descendants(), n => n.HasClass("bury-note") && n.Spoken.Contains("One marked DRILL POINT", StringComparison.Ordinal));
        Assert.DoesNotContain(rock.Root.Descendants(), n => n.Spoken.Contains("Beach-comber kit", StringComparison.Ordinal));
        Assert.Contains(rock.Root.Descendants(), n => n.HasClass("deck-shuttle-title"));
        Assert.DoesNotContain(rock.Root.Descendants(), n => n.Spoken.Contains("Coin in the chest", StringComparison.Ordinal));
        Assert.DoesNotContain(rock.Root.Descendants(), n => n.Spoken.Contains("Hold loaded with it", StringComparison.Ordinal));
    }

    private static async Task<DeskBench.Painted> TheBoardingCard(bool withTheGigRock)
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        object ex = bench.Peek("_surface")!;
        object stop = ex.GetType().GetProperty("Stop")!.GetValue(ex)!;
        string bodyId = ((CelestialBody)stop.GetType().GetProperty("Body")!.GetValue(stop)!).Id;
        bench.Poke("_boardTarget", stop);
        if (withTheGigRock)
        {
            Type plan = typeof(Pages.Map).GetNestedType("DeflectionPlan", Hidden)!;
            bench.Poke("_deflection", Activator.CreateInstance(plan,
                "ringside", "Ringside Exchange", bodyId, "Hull Breaker", new RockType(RockComposition.MType), "earth",
                1.0, 1.0, 0.0, 1.0, default(DeflectionGig.RockRail), 100.0, 0.0, 1000, 0.0)!);
        }
        return await bench.RenderAsync();
    }

    [Fact]
    public void TheDrillPointConsoleNamesItsVerbAndThenTheFiring()
    {
        Pages.Map map = OnAFloorOf("luna", 0);
        object ex = typeof(Pages.Map).GetField("_surface", Hidden)!.GetValue(map)!;
        MethodInfo compose = typeof(Pages.Map).GetMethod("ComposeDeflectionSite", Hidden)!;
        FieldInfo plan = typeof(Pages.Map).GetField("_deckPlan", Hidden)!;

        compose.Invoke(map, [ex]);
        Assert.Contains(((DeckPlan)plan.GetValue(map)!).Consoles,
            c => c.Kind == DeckPlan.ConsoleKind.DrillPoint && c.Label == "🛠 DRILL POINT — set the bore");

        ex.GetType().GetProperty("ChargeArmed")!.SetValue(ex, true);
        Set(map, "_deckPlan", DeckPlan.ShipWith([]));
        compose.Invoke(map, [ex]);
        Assert.Contains(((DeckPlan)plan.GetValue(map)!).Consoles,
            c => c.Kind == DeckPlan.ConsoleKind.DrillPoint && c.Label == "🧨 FIRE THE CHARGE");
    }
}
