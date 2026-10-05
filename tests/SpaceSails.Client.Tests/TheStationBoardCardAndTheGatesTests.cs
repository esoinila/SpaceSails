using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 · THE INSPECTION'S FIXES, PLAYED THROUGH THE SHIPPING COMPONENT: the shuttle's board card at a station is
/// the HULL card (not the regolith's), the moon's down-tube does not rearm sentries inside her drum, a cut face is
/// remembered across a lift-off and a second boarding, and the key bar advertises the sentry verbs.
/// </summary>
public sealed class TheStationBoardCardAndTheGatesTests
{
    private const string Id = "dev-station-6";

    private static object? Get(object instance, string member) =>
        instance.GetType().GetProperty(member, TestTree.AnythingAtAll)?.GetValue(instance)
        ?? instance.GetType().GetField(member, TestTree.AnythingAtAll)?.GetValue(instance);

    private static void Set(object instance, string member, object? value)
    {
        if (instance.GetType().GetProperty(member, TestTree.AnythingAtAll) is { } p)
        {
            p.SetValue(instance, value);
            return;
        }

        instance.GetType().GetField(member, TestTree.AnythingAtAll)!.SetValue(instance, value);
    }

    private static async Task<DeskBench> LandedAsync()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&station=1&land=1");
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);
        return bench;
    }

    private static object TheStationStop(DeskBench bench) =>
        ((System.Collections.IEnumerable)bench.Call("ShuttleDestinationsInRange")!).Cast<object>()
            .Single(s => StationAboard.TryParseStationId((string)Get(Get(s, "Body")!, "Id")!, out _));

    // ── 1 · The board card ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheBoardCardAtAStationIsTheHullCardWithHerBlurb()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&station=1");
        await bench.RenderAsync();
        object stop = TheStationStop(bench);
        // A crossing of 59 m 50 s: with the lock's 20 s it reads "1 h", without it "59 m" — so the ETA line cannot
        // pass by the truncation hiding the difference.
        object shortHop = stop.GetType().GetMethod("<Clone>$")!.Invoke(stop, null)!;
        Set(shortHop, "TravelSeconds", 3590.0);
        double crossing = 3590.0;

        bench.CallOnTheDispatcher("OpenBoardingPanel", shortHop);

        // Anti-vacuity: this body WOULD offer a site chooser if the card took her for a moon.
        Assert.True(((System.Collections.ICollection)bench.Peek("_boardSites")!).Count > 1,
            "the body offers one site only — a regolith card would show no chooser anyway, so this proves nothing.");

        var painted = await bench.RenderAsync();
        var nodes = painted.Root.SelfAndDescendants().ToArray();
        var card = nodes.Single(n => n.HasClass("deck-shuttle-card"));
        var inside = card.SelfAndDescendants().ToArray();

        Assert.DoesNotContain(inside, n => n.HasClass("board-sites"));                       // no site chooser
        Assert.DoesNotContain(inside, n => n.HasClass("bury-coin") && n.Name.EndsWith(" cr", StringComparison.Ordinal)); // no coin dial
        Assert.DoesNotContain(inside, n => n.HasClass("bury-gear"));                         // no beach-comber kit
        Assert.DoesNotContain(inside, n => n.Name.Contains("Board the shuttle →", StringComparison.Ordinal));
        Assert.Contains(inside, n => n.Name.Contains("Take the shuttle across →", StringComparison.Ordinal));

        var note = Assert.Single(inside, n => n.HasClass("bury-note"));
        Assert.Equal(StationAboard.BoardBlurb, note.Name);

        // The ETA says what the clock will really be charged: the crossing AND the crew lock's cycle.
        string eta = (string)typeof(Client.Pages.Map).GetMethod("FormatDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [crossing + StationAboard.LockCycleSeconds])!;
        Assert.Equal("1 h", eta);
        string said = System.Text.RegularExpressions.Regex.Replace(string.Join(" ¦ ", card.SelfAndDescendants().Select(n => n.Name)), "[ \t\r\n]+", " ");
        Assert.True(said.Contains("ETA " + eta, StringComparison.Ordinal) || painted.MarkupBlobs.Any(b => b.Contains("ETA " + eta, StringComparison.Ordinal)),
            "the ETA line does not read " + eta + ": " + said);
    }

    // ── 3 · The moon's tube is not in her drum ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AnEmptySentryCarriedThroughTheDrumIsNeverRackedByTheMoonsTube()
    {
        DeskBench bench = await LandedAsync();
        object ex = bench.Peek("_surface")!;

        const double x = -7, y = -10.1;
        Assert.True(MoonSurface.IsInDownTube(x, y),
            "the moon's down-tube strip no longer overlaps her drum — this guard has drifted.");
        Assert.True(StationWreck.ModuleOf(StationWreck.ModuleId.Hub).X0 < x && x < StationWreck.ModuleOf(StationWreck.ModuleId.Hub).X1);

        object bot = ((System.Collections.IEnumerable)Get(ex, "Bots")!).Cast<object>().First();
        Set(bot, "Rounds", 0);
        Set(bot, "Deployed", false);
        int credits = (int)bench.Peek("_credits")!;
        Assert.True(credits >= SentryBot.RestockPricePerRound, "the captain cannot afford a rack — the guard would pass vacuously.");

        bench.Poke("_avatarX", x);
        bench.Poke("_avatarY", y);
        for (int i = 0; i < 6; i++)
        {
            bench.CallOnTheDispatcher("StepTubeRearm", SentryBot.RearmSecondsPerMagazine);
        }

        Assert.Equal(0, (int)Get(bot, "Rounds")!);
        Assert.Equal(credits, (int)bench.Peek("_credits")!);
        Assert.Null(Get(ex, "RearmBotIndex"));
    }

    // ── 4 · A cut face is remembered ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ACutFaceSurvivesLiftOffAndASecondBoardingAndSpendsNoSecondCut()
    {
        DeskBench bench = await LandedAsync();
        bench.Poke("_satchel", new List<Satchel.Item> { HullCutter.FreshRig });

        StationWreck.Access reactor = StationAboard.AccessOf(Id, StationWreck.ModuleId.Reactor);
        DeckPlan deck = (DeckPlan)bench.Peek("_deckPlan")!;
        var hop = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.StationHop && c.Label.Contains(reactor.Name, StringComparison.Ordinal));
        bench.Poke("_avatarX", (double)hop.X);
        bench.Poke("_avatarY", (double)hop.Y);
        bench.CallOnTheDispatcher("InteractAtConsole");
        Assert.Equal(HullCutter.CutsPerCell - 1, HullCutter.CutsLeft((IReadOnlyList<Satchel.Item>)bench.Peek("_satchel")!));

        // The register carries it (what the vault saves).
        var register = (HashSet<string>)bench.Peek("_roomsTurnedOver")!;
        Assert.Contains(StationAboard.CutTag(Id, StationWreck.ModuleId.Reactor), register);

        // Lift off from the boat's own lock…
        var home = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        deck = (DeckPlan)bench.Peek("_deckPlan")!;
        home = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        bench.Poke("_avatarX", (double)home.X);
        bench.Poke("_avatarY", (double)home.Y);
        bench.CallOnTheDispatcher("InteractAtConsole");
        Assert.False(bench.OnSurface, "the boat did not lift off.");

        // …and board her again.
        await (Task)bench.CallOnTheDispatcher(
            "BeginSurfaceExcursion", TheStationStop(bench), ShuttleExcursion.Pack(0, 0, []), 0, null)!;
        Assert.True(bench.OnSurface);

        object ex = bench.Peek("_surface")!;
        var cuts = (HashSet<StationWreck.ModuleId>)Get(Get(ex, "Station")!, "Cuts")!;
        Assert.Contains(StationWreck.ModuleId.Reactor, cuts);

        deck = (DeckPlan)bench.Peek("_deckPlan")!;
        Assert.Contains(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.StationHop
            && c.Label.StartsWith("🛸 →", StringComparison.Ordinal) && c.Label.Contains(reactor.Name, StringComparison.Ordinal));
        Assert.DoesNotContain(deck.Consoles, c => c.Label.StartsWith('✂'));
        Assert.Equal(HullCutter.CutsPerCell - 1, HullCutter.CutsLeft((IReadOnlyList<Satchel.Item>)bench.Peek("_satchel")!));
    }

    // ── 5 · Affordances never hide ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheKeyBarNamesTheSentryVerbsWhileABotRidesTheSling()
    {
        DeskBench bench = await LandedAsync();
        object ex = bench.Peek("_surface")!;
        Assert.Contains(((System.Collections.IEnumerable)Get(ex, "Bots")!).Cast<object>(), b => !(bool)Get(b, "Deployed")!);

        string bar = (string)bench.Call("StationKeyHints", ex)!;

        Assert.Contains("H — weapons tight", bar, StringComparison.Ordinal);
        Assert.Contains("T — " + SentryDoctrine.DeployHereLabel, bar, StringComparison.Ordinal);
        Assert.Contains("⇧T — " + SentryDoctrine.HoldMyLineHomeLabel, bar, StringComparison.Ordinal);

        // With nobody riding the sling the bar says none of it.
        ((System.Collections.IList)Get(ex, "Bots")!).Clear();
        string bare = (string)bench.Call("StationKeyHints", ex)!;
        Assert.DoesNotContain("🤖", bare, StringComparison.Ordinal);
    }
}
