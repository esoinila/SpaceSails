using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Fable's rulings of 2026-11-07 (owner-delegated), the page half of the misc lane: R1-F91, R1-F93, R1-F103,
/// R1-F102, R1-F95, R1-F62 polish, U03-F18, U09-F02 and U09-F07. Each test names the one hunk whose reversion
/// turns it red.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class FableRulings1107MiscTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private const string ABerth = "/map?dock=selene-gate&body=luna&site=1";
    private const string TheShed = "/map?kaamos=hq&land=1";
    private const string AnOrdinaryMoon = "/map?dock=the-space-bar&body=phobos&site=0&land=1";

    private static string ClientSource(string relative)
    {
        string dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "SpaceSails.slnx")))
        {
            dir = Path.GetDirectoryName(dir)!;
        }
        return File.ReadAllText(Path.Combine(dir!, "src", "SpaceSails.Client", relative));
    }

    private static object AFreezeFrameDeath()
    {
        Type encounter = typeof(SpaceSails.Client.Pages.Map).GetNestedType("BustedEncounter", Hidden)!;
        Type stage = encounter.GetNestedType("Stage", Hidden)!;
        object death = Activator.CreateInstance(encounter, nonPublic: true)!;
        encounter.GetProperty("HunterId", Hidden)!.SetValue(death, "no-such-hunter");
        encounter.GetProperty("HunterCallsign", Hidden)!.SetValue(death, "The Debt Collector");
        encounter.GetProperty("Heat", Hidden)!.SetValue(death, 3);
        encounter.GetProperty("Seed", Hidden)!.SetValue(death, 12345UL);
        encounter.GetProperty("Phase", Hidden)!.SetValue(death, Enum.Parse(stage, "FreezeFrame"));
        return death;
    }

    // ── R1-F91 · deferred beats die with the captain, wherever he fell ─────────────────────────────────

    /// <summary>RED: delete the two clears in <c>BustedResurrect</c>. A death in a berth (no excursion to fold, so
    /// <c>ForgetTheExcursionsGlass</c> never runs) leaves the dead captain's queued beat to raise on the
    /// successor's first calm frame.</summary>
    [Fact]
    public async Task ADeadCaptainsQueuedBeatDoesNotRaiseForTheSuccessor()
    {
        using DeskBench bench = await DeskBench.BootAsync(ABerth);
        await bench.RenderAsync();
        Assert.False(bench.OnSurface);   // the berth death: the excursion fold is not the thing doing the clearing

        ((IList)bench.Peek("_deferredBeats")!).Add((StoryBeats.Beat.CrewDeputation, (string?)null, (string?)null));
        bench.Poke("_scrimQueued", new List<(Action, Func<bool>, string?)> { (() => { }, () => true, null), (() => { }, () => true, "k2") });
        // …and the rest of what the dead captain's glass held: the open card, the line behind it, the plate, the held pulses.
        bench.Poke("_storyCard", (StoryBeats.Beat.CrewMeeting, (string?)null, (string?)null));
        ((IList)bench.Peek("_storyWaiting")!).Add((StoryBeats.Beat.CrewDeputation, (string?)null, (string?)null));
        bench.Poke("_storyPlate", (StoryBeats.Beat.Flashback, (string?)"old life", 1e9));
        bench.Poke("_busted", AFreezeFrameDeath());

        bench.CallOnTheDispatcher("BustedResurrect");

        Assert.Empty((IList)bench.Peek("_deferredBeats")!);
        Assert.Null(bench.Peek("_scrimQueued"));
        Assert.Null(bench.Peek("_storyCard"));
        Assert.Empty((IList)bench.Peek("_storyWaiting")!);
        Assert.Null(bench.Peek("_storyPlate"));

        // The successor's first calm frame: the death card is read and put away, the page ticks the cards.
        bench.Poke("_busted", null);
        bench.CallOnTheDispatcher("AdvanceStoryCards");
        Assert.Null(bench.Peek("_storyCard"));
    }

    // ── R1-F93 · the world runs under a card on an excursion, and the dim lets the strip through ─────────

    /// <summary>RED: remove the excursion rule from Map.razor.css (and its comment) and this fails on the missing
    /// rule. It is the guard of last resort for a CSS-only change: the computed style itself is the UiGate's.</summary>
    [Fact]
    public void TheDimOnAnExcursionIsLighterThanTheDockedDimAndSaysWhyTheWorldRuns()
    {
        string css = ClientSource("Pages/Map.razor.css");

        Assert.Contains("The world runs under this card (Fable ruling 2026-11-07, R1-F93).", css, StringComparison.Ordinal);
        int rule = css.IndexOf(".map-page:has(.captains-remote-btn) .view-object-backdrop", StringComparison.Ordinal);
        Assert.True(rule > 0, "no excursion-only backdrop rule");
        string body = css.Substring(rule, css.IndexOf('}', rule) - rule);
        Assert.Contains("rgba(4, 6, 11, 0.55)", body, StringComparison.Ordinal);

        // …and the docked/seated dim is exactly what it was.
        int baseRule = css.IndexOf(".view-object-backdrop {", StringComparison.Ordinal);
        Assert.Contains("background: rgba(4, 6, 11, 0.85);", css.Substring(baseRule, css.IndexOf('}', baseRule) - baseRule), StringComparison.Ordinal);

        // The marker the rule hangs on exists only on the deck HUD of an excursion.
        Assert.Contains("_deckMode && _surface is not null",
            ClientSource("Pages/Map/CaptainsRemoteButton.razor"), StringComparison.Ordinal);
    }

    // ── R1-F103 · the head office's shed is a lift head ─────────────────────────────────────────────────

    /// <summary>RED: drop <c>ex.SecretLabForced = true</c> in the head-office branch of <c>ResolveSecretLab</c> and the
    /// chamber is never drawn; drop the head-office branch in <c>SecretLab.Build</c> and Vantar's furniture returns.</summary>
    [Fact]
    public async Task TheHeadOfficeShedStandsAsOnePlainChamberWithNothingToForce()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheShed);
        object ex = bench.Peek("_surface")!;
        Type exType = ex.GetType();
        Assert.True((bool)exType.GetProperty("SecretLabForced")!.GetValue(ex)!, "the shed's chamber is standing from the first frame");

        var plan = (DeckPlan)bench.Peek("_deckPlan")!;
        Assert.DoesNotContain(plan.Consoles, c => c.Kind is DeckPlan.ConsoleKind.SecretDoor or DeckPlan.ConsoleKind.LabConsole
            or DeckPlan.ConsoleKind.LabDoor or DeckPlan.ConsoleKind.LabCache or DeckPlan.ConsoleKind.LabDoorBoard
            or DeckPlan.ConsoleKind.LabAlarm or DeckPlan.ConsoleKind.LabKeyCard);
        Assert.Contains(plan.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveHead);

        // The chamber is really there: the plan carries walls where the head office's one chamber stands.
        var placement = (SecretLab.Placement)exType.GetProperty("Lab")!.GetValue(ex)!;
        SecretLab.Region chamber = SecretLab.Build(KaamosLore.IceMoonBodyId, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY);
        Assert.Equal(6, chamber.Walls.Count);
        // The fire code (#822), now that the lab sweep skips the head office: each face leaves a doorway at the chamber's
        // centre line at least two captain radii wide.
        double cy = chamber.Walls.Where(w => Math.Abs(w.Y1 - w.Y2) < 1e-9).Average(w => w.Y1);
        foreach (double x in chamber.Walls.Where(w => Math.Abs(w.X1 - w.X2) < 1e-9).Select(w => w.X1).Distinct())
        {
            var face = chamber.Walls.Where(w => Math.Abs(w.X1 - x) < 1e-9 && Math.Abs(w.X2 - x) < 1e-9).ToArray();
            double lowEnd = face.Select(w => Math.Max(w.Y1, w.Y2)).Where(y => y <= cy).DefaultIfEmpty(double.NaN).Max();
            double highEnd = face.Select(w => Math.Min(w.Y1, w.Y2)).Where(y => y >= cy).DefaultIfEmpty(double.NaN).Min();
            Assert.False(double.IsNaN(lowEnd) || double.IsNaN(highEnd), $"face at x={x} has no doorway at the centre line");
            Assert.True(highEnd - lowEnd >= 2 * DeckPlan.AvatarRadius, $"face at x={x} doorway {highEnd - lowEnd} du is narrower than the captain");
        }
        Assert.Contains(plan.Walls, w => chamber.Walls.Any(c =>
            Math.Abs(c.X1 - w.X1) < 0.01 && Math.Abs(c.Y1 - w.Y1) < 0.01 && Math.Abs(c.X2 - w.X2) < 0.01 && Math.Abs(c.Y2 - w.Y2) < 0.01));

        // Nothing armed off the flag: no alarm, no lab doors held.
        Assert.Empty((IDictionary)bench.Peek("_labDoors")!);
    }

    // ── R1-F102 · no Old Ones on Enceladus, ever ────────────────────────────────────────────────────────

    /// <summary>RED: delete the head-office return in <c>StepTide</c> (the tide claws one up) or in
    /// <c>SpawnReevers</c> (the pod cheat's pack arrives). The ordinary moon is the control that lets it fail.</summary>
    [Fact]
    public async Task EnceladusSpawnsNoContactsAndAnOrdinaryMoonDoes()
    {
        using DeskBench moon = await DeskBench.BootAsync(AnOrdinaryMoon);
        for (int i = 0; i < 6000; i++)
        {
            moon.CallOnTheDispatcher("StepTide", 0.1);
            if (((IList)moon.Peek("_reevers")!).Count > 0)
            {
                break;
            }
        }
        Assert.NotEmpty((IList)moon.Peek("_reevers")!);   // the control: a long tide on a moon does raise contacts

        using DeskBench ice = await DeskBench.BootAsync(TheShed);
        ((IList)ice.Peek("_reevers")!).Clear();
        for (int i = 0; i < 6000; i++)
        {
            ice.CallOnTheDispatcher("StepTide", 0.1);
        }
        ice.CallOnTheDispatcher("SpawnReevers", 7);   // the dig roll, the lab's pack, the expedition's, the cheat
        Assert.Empty((IList)ice.Peek("_reevers")!);
    }

    // ── R1-F95 · the underwriter's line is stamped on the card, not floated over the column ─────────────

    /// <summary>RED: put the label back in <c>WreckInterior</c> (a SUBSTRATE SPAR room label returns) or drop the
    /// stamp row from <c>ArchiveVisionCard.razor</c>.</summary>
    [Fact]
    public void NoUnderwriterLabelFloatsOverTheColumnAndTheCardWearsTheStamp()
    {
        var hull = new Derelict.Wreck("second-marriage", "Second Marriage", Derelict.WreckCause.Infested, 250_000, 40.0);
        foreach (bool purged in new[] { false, true })
        {
            DeckPlan deck = WreckInterior.WreckDeck(
                hull, new HashSet<string>(StringComparer.Ordinal), salvaged: false,
                droidCount: 0, fillDroids: static (_, _) => { }, archiveAboard: true, archivePurged: purged);

            Assert.DoesNotContain(deck.RoomLabels, l => l.Text.Contains("SUBSTRATE SPAR", StringComparison.Ordinal));
            DeckPlan.ConsoleSpot column = Assert.Single(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.ArchiveNode);
            Assert.Equal(purged ? "❄ THE COLUMN — cold" : "❄ THE COLUMN IN THE HOLD · ⚡", column.Label);
        }

        string card = ClientSource("Pages/Map/ArchiveVisionCard.razor");
        int stamp = card.IndexOf("@vision.Stamp", StringComparison.Ordinal);
        Assert.True(stamp > 0 && stamp < card.IndexOf("@vision.Title", StringComparison.Ordinal),
            "the stamp is the card's first row, above the title");
    }

    // ── U03-F18 · Kolt is never on a gig rock ───────────────────────────────────────────────────────────

    /// <summary>RED: drop <c>onAGigRock: ex.Deflection</c> and <c>&amp;&amp; !ex.Deflection</c> in Map.Hardcase.cs.</summary>
    [Fact]
    public async Task ADeflectionRockNeverSeatsKoltAndAnOrdinaryMoonCan()
    {
        using DeskBench bench = await DeskBench.BootAsync(AnOrdinaryMoon);
        object ex = bench.Peek("_surface")!;
        Assert.NotNull(bench.Call("TheGroundUnderfootForKolt"));
        Assert.True((bool)bench.Call("HeCouldBeAfootNow")!);   // the control

        ex.GetType().GetProperty("Deflection")!.SetValue(ex, true);

        Assert.Null(bench.Call("TheGroundUnderfootForKolt"));
        Assert.False((bool)bench.Call("HeCouldBeAfootNow")!);
    }

    // ── U09-F02 · the pendant's first opening is a card that closes with the cross and with Esc ─────────

    private static Pages.Map TheFirstOpeningRaised()
    {
        var map = new Pages.Map();
        typeof(Microsoft.AspNetCore.Components.ComponentBase).GetField("_hasPendingQueuedRender", Hidden)!.SetValue(map, true);
        void Set(string f, object? v) => typeof(Pages.Map).GetField(f, Hidden)!.SetValue(map, v);
        Set("_avatarX", 12.75);   // the captain's cabin
        Set("_avatarY", -6.5);
        Set("_nerve", 40.0);
        Set("_showSatchel", true);
        typeof(Pages.Map).GetMethod("PressKeepsake", Hidden)!.Invoke(map, [Keepsake.Pendant]);
        return map;
    }

    private static object? Card(Pages.Map map) => typeof(Pages.Map).GetField("_storyCard", Hidden)!.GetValue(map);

    [Fact]
    public void ThePendantsCardClosesWithTheCross()
    {
        Pages.Map map = TheFirstOpeningRaised();
        Assert.NotNull(Card(map));

        typeof(Pages.Map).GetMethod("CloseStoryCard", Hidden)!.Invoke(map, null);   // what the ✕ and the backdrop call

        Assert.Null(Card(map));
    }

    [Fact]
    public void ThePendantsCardClosesWithEsc()
    {
        Pages.Map map = TheFirstOpeningRaised();
        Assert.NotNull(Card(map));

        bool handled = (bool)typeof(Pages.Map).GetMethod("TryDismissTopOverlay", Hidden)!.Invoke(map, null)!;

        Assert.True(handled);
        Assert.Null(Card(map));
    }

    // ── U09-F07 · the Hive's counter names no haven regulars ────────────────────────────────────────────

    /// <summary>RED: drop the <c>_surface is not null</c> return in <c>WhoIsInTonight</c> (the Hive counter reads the
    /// docked haven's roster). The haven berth is the control.</summary>
    [Fact]
    public async Task TheHivesCounterNamesNobodyAndAHavenBarStill()
    {
        using DeskBench berth = await DeskBench.BootAsync("/map?dock=the-space-bar");
        berth.Poke("_barMenu", SpaceSails.Core.Interior.Barkeeps.For("the-space-bar"));
        Assert.NotNull(berth.Call("WhoIsInTonight"));   // the control: a haven bar names its regulars

        using DeskBench hive = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&secretlab=deep&land=1&floor=1&counter=1");
        Assert.True(hive.OnSurface);
        Assert.NotNull(hive.Peek("_dockedHavenId"));
        Assert.Null(hive.Call("WhoIsInTonight"));
    }

    /// <summary>RED: gate the Round button on <c>!SelfService</c> again (or drop the early return in <c>BuyRoundForRoom</c>).</summary>
    [Fact]
    public async Task AKeptWatchCounterOffersTheRumorAndNoRound()
    {
        using DeskBench haven = await DeskBench.BootAsync("/map?dock=the-space-bar");
        var havenKeep = SpaceSails.Core.Interior.Barkeeps.For("the-space-bar")!;
        haven.Poke("_barMenu", havenKeep);
        haven.Poke("_credits", 1000);
        haven.CallOnTheDispatcher("BuyRoundForRoom");
        Assert.True((int)haven.Peek("_credits")! < 1000, "the control: a haven bar still sells a round");

        using DeskBench hive = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&secretlab=deep&land=1&floor=1&counter=1");
        long kept = Enumerable.Range(0, 400).Select(w => (long)w).First(w => SpaceSails.Core.Interior.TheKeep.KeptWatch(w));
        var keep = SpaceSails.Core.Interior.CounterService.OnWatch(
            SpaceSails.Core.Interior.CounterService.For("test-rock", UndergroundComplex.Comfort.UpperCanteen)!, kept);
        Assert.False(keep.SelfService);
        Assert.False(keep.HasRegularsToWarm);
        hive.Poke("_barMenu", keep);
        hive.Poke("_credits", 1000);
        hive.CallOnTheDispatcher("BuyRoundForRoom");
        Assert.Equal(1000, (int)hive.Peek("_credits")!);   // no round is sold where there is no room to warm

        // The markup draws the rumor button for the person behind the counter and the round button for regulars only.
        string card = ClientSource("Pages/Map/BarMenuCard.razor");
        Assert.Contains("@if (keep.HasRegularsToWarm)", card, StringComparison.Ordinal);
        Assert.Contains("AskBarkeepForRumor", card, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRoundButtonIsDrawnOnTheKeepsRegularsAndRumorsOnTheKeepsPerson()
    {
        string card = ClientSource("Pages/Map/BarMenuCard.razor");
        int round = card.IndexOf("@if (keep.HasRegularsToWarm)", StringComparison.Ordinal);
        int roundButton = card.IndexOf("BuyRoundForRoom", StringComparison.Ordinal);
        int rumor = card.IndexOf("AskBarkeepForRumor", StringComparison.Ordinal);
        Assert.True(round > 0 && round < roundButton, "the round button is gated on HasRegularsToWarm");
        Assert.True(card.LastIndexOf("@if (!keep.SelfService)", rumor, StringComparison.Ordinal) > roundButton,
            "hear a rumor stays on the person behind the counter");
    }

    // ── R1-F62 polish · the contract paragraph ends on the key, and the key wears the double mark ──────

    /// <summary>RED: revert the prefix in <c>KaamosLedgerTip</c> / <c>NebulaLedgerTip</c> and the key reads with the
    /// intel's single mark.</summary>
    [Fact]
    public async Task TheKeyLinesWearTheDoubleMarkAndComeLast()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?kaamos=hq");
        Assert.Null(b.Threw);

        string[] kaamos = LinesOf(b, "KaamosLedgerTip");
        int keyAt = Array.FindIndex(kaamos, l => l.Contains(KaamosLore.KeyFragment.Title, StringComparison.Ordinal));
        Assert.True(keyAt >= 0, "the hq boot holds the berth code");
        Assert.StartsWith("❄❄ ", kaamos[keyAt], StringComparison.Ordinal);
        Assert.All(kaamos.Take(keyAt), l => Assert.StartsWith("◆ ", l, StringComparison.Ordinal));

        b.Read<NebulaProgress>("_nebula").Load(NebulaLore.Fragments.Select(f => f.Id));
        string[] nebula = LinesOf(b, "NebulaLedgerTip");
        int nKey = Array.FindIndex(nebula, l => l.Contains(NebulaLore.KeyFragment.Title, StringComparison.Ordinal));
        Assert.True(nKey >= 0);
        Assert.StartsWith("▓▓ ", nebula[nKey], StringComparison.Ordinal);
        Assert.All(nebula.Take(nKey), l => Assert.StartsWith("▓ ", l, StringComparison.Ordinal));
        Assert.Equal(nebula.Length - 2, nKey);   // the key is the last fragment line, ahead only of the progress line
    }

    private static string[] LinesOf(PastTheGateBench.Boot b, string tip)
    {
        object t = typeof(Pages.Map).GetMethod(tip, Hidden)!.Invoke(b.Page, null)!;
        return (string[])t.GetType().GetProperty("Lines")!.GetValue(t)!;
    }
}
