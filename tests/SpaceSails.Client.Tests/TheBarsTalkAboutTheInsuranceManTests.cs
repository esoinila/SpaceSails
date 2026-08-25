using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 · THE VOID'S WEATHER, IN THE ROOM — the client half.
///
/// <para>OWNER RULING 2026-08-25: the walking insurance men are <i>"the thing people talk about in the bars
/// that unites them, a bit like talking about the weather on planet side."</i></para>
///
/// <para>Core owns the eight lines, the selection law and how a block is composed
/// (<c>TheVoidsWeatherTests</c>, sixteen of them next door). What is left to get wrong here is the WIRING —
/// whether the strip on the shipped card actually draws it, whether the round's own button turns it into the
/// room's topic, and whether the one sentence that touches the arc writes its sheet under the one condition
/// it is allowed to. So every claim below is made against a REAL RENDER of the shipping <see cref="Map"/>
/// through <see cref="DeskBench"/>, and the round is stood by pressing the button the render tree drew.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBarsTalkAboutTheInsuranceManTests
{
    private const string Station = "the-space-bar";

    /// <summary>Boot ashore, open the counter card the shipped way, and put the captain in a room where the
    /// weather is already blowing — the outcome fields the draw sets, so this file can assert about the
    /// SURFACE without re-rolling Core's dice beside it.</summary>
    private static async Task<(DeskBench Bench, string Speaker, string Text)> AtACounterWithAnInsuranceLine(
        int line = 0, params string[] counterSaid)
    {
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1");
        Barkeep keep = Barkeeps.For(Station)!;

        // What this counter has already said to the captain — the book the block draws from.
        var book = new List<OverheardLine>();
        foreach (string said in counterSaid)
        {
            book.Add(new OverheardLine(said, 10.0, "A REGULAR", keep.BarName));
        }

        bench.Poke("_overheard", book);
        bench.Poke("_credits", 9999);

        // The room's weather, as the draw would have left it.
        string speaker = "GALE CORBIN";
        string text = InsuranceWeather.Lines[line].Text;
        bench.Poke("_weatherStation", Station);
        bench.Poke("_weatherAsked", true);
        bench.Poke("_weatherSaidId", InsuranceWeather.Lines[line].Id);
        bench.Poke("_weatherSpeaker", speaker);

        // The shipped doorway onto the card — not a field write, so the card is the one a captain opens.
        bench.Call("OpenCounterService", keep);
        return (bench, speaker, text);
    }

    /// <summary>The <i>Overheard here</i> strip as it was drawn, in order.</summary>
    private static List<string> BlockOf(DeskBench.Painted painted) =>
        [.. painted.Root.Descendants()
            .Where(n => n.HasClass("bar-overheard-line"))
            .Select(n => n.Spoken)];

    // ── THE STRIP ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheCounterCardDrawsTheInsuranceLineAmongWhatTheRoomAlreadySaid()
    {
        (DeskBench bench, string who, string said) =
            await AtACounterWithAnInsuranceLine(0, "🍺 the keep: one", "🍺 the keep: two", "🍺 the keep: three");
        using DeskBench held = bench;

        List<string> block = BlockOf(await bench.RenderAsync());

        // THE BLOCK STAYS THREE LINES. Red proof: append the weather rather than giving it a slot and this
        // comes back four — the bug the owner would see as a strip that grew a row.
        Assert.Equal(InsuranceWeather.BlockLines, block.Count);
        Assert.Contains(block, l => l.Contains(said, StringComparison.Ordinal));
        Assert.Contains(block, l => l.Contains(who, StringComparison.Ordinal));

        // …and it cost the OLDEST thing this counter said, not one of the two newest. The strip reads
        // newest-first, so of the three lines poked in order it is the FIRST one that falls off.
        Assert.Contains(block, l => l.Contains("three", StringComparison.Ordinal));
        Assert.Contains(block, l => l.Contains("two", StringComparison.Ordinal));
        Assert.DoesNotContain(block, l => l.Contains(": one", StringComparison.Ordinal));

        Assert.Empty(bench.EscapedPastTheGate);
    }

    [Fact]
    public async Task ACounterThatHasSaidNothingYetStillCarriesTheWeather()
    {
        // The point of the feature: it is the small talk that unites the room, so it must reach a captain
        // who has never bought a rumour here. Red proof: gate the strip on the durable book being non-empty
        // (which is what the card did before this lane) and the first-visit case draws nothing at all.
        (DeskBench bench, string _, string said) = await AtACounterWithAnInsuranceLine();
        using DeskBench held = bench;

        List<string> block = BlockOf(await bench.RenderAsync());

        Assert.Single(block);
        Assert.Contains(said, block[0], StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    // ── THE ROUND ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StandingARoundMakesTheLineTheWholeRoomsTopic()
    {
        (DeskBench bench, string who, string said) =
            await AtACounterWithAnInsuranceLine(3, "🍺 the keep: one", "🍺 the keep: two");
        using DeskBench held = bench;

        DeskBench.Painted before = await bench.RenderAsync();
        Assert.Equal(InsuranceWeather.BlockLines, BlockOf(before).Count);

        // Pressed, not called: the button the render tree drew, through the renderer's own event channel.
        DeskBench.Painted.Node round = before.Root.Descendants().First(
            n => n.Element == "button" && n.Name.Contains("Round for the room", StringComparison.Ordinal));
        await bench.PressAsync(round.Handlers["onclick"]);

        List<string> block = BlockOf(await bench.RenderAsync());

        // THE WHOLE BLOCK BECOMES THE TOPIC AND THE ROOM'S ANSWER TO IT. Red proof: leave the block composed
        // from the book and the shared topic is a toast that fades, which is the thing #212 forbids.
        Assert.Equal(2, block.Count);
        Assert.Contains(who, block[0], StringComparison.Ordinal);
        Assert.Contains(said, block[0], StringComparison.Ordinal);
        Assert.Equal(InsuranceWeather.RoomsReaction, block[1]);

        // …and the round's own receipt carries it, so a captain reading the notice hears the room too.
        Assert.Contains(InsuranceWeather.RoomsReaction, bench.Pulse, StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    [Fact]
    public async Task ARoundInAQuietRoomConjuresNothing()
    {
        // At most ONE insurance line per bar visit: a round is the amplifier, never a second source. Red
        // proof: let the round draw a line of its own and this comes back with one in the block.
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1");
        using DeskBench held = bench;
        Barkeep keep = Barkeeps.For(Station)!;
        bench.Poke("_credits", 9999);
        bench.Poke("_weatherStation", Station);
        bench.Poke("_weatherAsked", true);      // the visit asked, and the room was on something else
        bench.Call("OpenCounterService", keep);

        bench.Call("BuyRoundForRoom");

        Assert.False((bool)bench.Peek("_weatherShared")!);
        Assert.DoesNotContain(InsuranceWeather.RoomsReaction, bench.Pulse, StringComparison.Ordinal);
        foreach (InsuranceWeather.Line line in InsuranceWeather.Lines)
        {
            Assert.DoesNotContain(line.Text, bench.Pulse, StringComparison.Ordinal);
        }
    }

    // ── THE ONE PLACE IT TOUCHES THE ARC ───────────────────────────────────────────────────────────────

    /// <summary>Hearing the cousin's line with the fleet-day page already in the book files a sheet — marked
    /// his, tagged money, the line exactly as it was heard with the name of whoever said it.</summary>
    [Fact]
    public async Task TheLapsedCousinFilesASheetForACaptainHoldingTheFleetDayPage()
    {
        using DeskBench bench = await HearingTheCousin(holdingTheFleetDay: true);

        var book = (IReadOnlyList<HeldMemory.Sheet>)bench.Peek("_heldMemories")!;
        HeldMemory.Sheet? filed = HeldMemory.Find(book, InsuranceWeather.LapsedCousinSheetId);

        Assert.NotNull(filed);
        Assert.Equal(HeldMemory.Mark.His, filed!.Value.Mark);
        Assert.Equal(HeldMemory.Theory.Money, filed.Value.Tag);
        Assert.Contains(
            InsuranceWeather.TextOf(InsuranceWeather.LapsedCousinId)!, filed.Value.Text, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(filed.Value.HandedBy));
        Assert.Contains(filed.Value.HandedBy, filed.Value.Text, StringComparison.Ordinal);
    }

    /// <summary>…and a captain who is not holding it hears a man complaining about forty credits. The other
    /// branch, because a condition tested on one side is a condition nobody has tested.</summary>
    [Fact]
    public async Task WithoutTheFleetDayPageTheCousinIsJustAStoryAboutForty()
    {
        using DeskBench bench = await HearingTheCousin(holdingTheFleetDay: false);

        var book = (IReadOnlyList<HeldMemory.Sheet>)bench.Peek("_heldMemories")!;
        Assert.Null(HeldMemory.Find(book, InsuranceWeather.LapsedCousinSheetId));
    }

    /// <summary>Walk the shipping draw until the room is actually on the cousin — every other line retired,
    /// so the pool holds exactly that one and the only question left is which visit it comes up on. The line
    /// is HEARD through <c>TheWeatherComesIn</c> and never poked in, so what is under test is the real
    /// road.</summary>
    private static async Task<DeskBench> HearingTheCousin(bool holdingTheFleetDay)
    {
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1");
        bench.Poke("_credits", 9999);
        bench.Poke("_repWorkingHere", true);   // #976 · his rota walked this room this watch

        if (holdingTheFleetDay)
        {
            bench.Poke("_heldMemories", (IReadOnlyList<HeldMemory.Sheet>)
            [
                new HeldMemory.Sheet(
                    OldCrewScene.SummerPartyId, HeldMemory.Mark.Mine, HeldMemory.Theory.Love,
                    OldCrewScene.SummerPartyPage, [], 1.0, Filed: true),
            ]);
        }
        else
        {
            bench.Poke("_heldMemories", (IReadOnlyList<HeldMemory.Sheet>)[]);
        }

        var heard = (Dictionary<string, int>)bench.Peek("_weatherHeard")!;
        foreach (InsuranceWeather.Line line in InsuranceWeather.Lines)
        {
            if (line.Id != InsuranceWeather.LapsedCousinId)
            {
                heard[line.Id] = InsuranceWeather.RetireAfterHearings;
            }
        }

        var visits = (Dictionary<string, int>)bench.Peek("_weatherStationVisits")!;
        Barkeep keep = Barkeeps.For(Station)!;

        for (int visit = 0; visit < 200; visit++)
        {
            visits[Station] = visit;
            bench.Poke("_weatherStation", Station);
            bench.Poke("_weatherAsked", false);
            bench.Poke("_weatherSaidId", null);
            bench.Call("OpenCounterService", keep);

            if (bench.Peek("_weatherSaidId") is string said)
            {
                Assert.Equal(InsuranceWeather.LapsedCousinId, said);
                return bench;
            }
        }

        throw new InvalidOperationException(
            "the cousin's line never came up in 200 visits with the other seven retired — this guard would "
            + "have proved nothing about either branch.");
    }
}
