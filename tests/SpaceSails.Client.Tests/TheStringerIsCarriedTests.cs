using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;
using Map = SpaceSails.Client.Pages.Map;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 1 · <b>CARRY THE PRESS, ON A BOOTED PAGE.</b> The arithmetic and the words are Core's
/// (<c>CarryThePressTests</c>); what is driven here is the page doing its half on the page itself — the dev
/// starts, her body on her ground and nowhere else, the tin under the probe, the fare at the clamp, and the
/// story on the wire exactly once, across a save. Every guard was watched go red on the revert its summary
/// names.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheStringerIsCarriedTests
{
    private const double Day = 86400.0;
    private const string Aboard = "/map?dock=selene-gate&press=1";
    private const string Filed = "/map?dock=selene-gate&press=filed";
    private const string OnLuna = "/map?dock=selene-gate&body=luna&site=1&land=1";

    // ── helpers ─────────────────────────────────────────────────────────────────────────────────────────

    private static List<Map.Quest> Quests(DeskBench b) => (List<Map.Quest>)b.Peek("_quests")!;

    private static Map.Quest Hers(DeskBench b) =>
        Assert.Single(Quests(b), q => q.Kind == Map.QuestKind.CarryThePress);

    private static List<NewsWire.NewsEvent> Wire(DeskBench b) => (List<NewsWire.NewsEvent>)b.Peek("_newsEvents")!;

    private static IReadOnlyList<NewsWire.NewsItem> Feed(DeskBench b, NewsWire.NewsScope scope) =>
        (IReadOnlyList<NewsWire.NewsItem>)b.Call("NewsFeed", 20, scope, "selene-gate")!;

    private static int BookEntries(DeskBench b, string line) =>
        ((IEnumerable<FieldNote>)b.Peek("_fieldNotes")!).Count(n => n.Text == line);

    private static void Rewrite(DeskBench b, Map.Quest q, CarryThePress.Passage p) =>
        b.Call("RewritePassage", q, p);

    private static List<Map.Walker> Afoot(DeskBench b)
    {
        object ex = b.Peek("_surface") ?? throw new InvalidOperationException("premise: the captain is on a surface");
        return (List<Map.Walker>)ex.GetType().GetProperty("Walkers")!.GetValue(ex)!;
    }

    // ── THE DEV STARTS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>?press=1</c> · her passage is on the books, Active, bound for a ground the shuttle reaches from this
    /// berth, and it says so on the ledger row. Nothing has printed and nothing is paid.
    /// </summary>
    [Fact]
    public async Task TheAboardStartPutsHerOnTheBooksForAGroundInReach()
    {
        DeskBench b = await DeskBench.BootAsync(Aboard);
        Map.Quest q = Hers(b);
        Assert.Equal(Map.QuestState.Active, q.State);
        Assert.Equal(CarryThePress.CardTitle, q.Title);
        Assert.Equal(CarryThePress.Giver, q.Giver);
        Assert.Equal("luna", q.DestBodyId);
        Assert.StartsWith("Luna · ", q.TargetCallsign, StringComparison.Ordinal);
        Assert.Equal(CarryThePress.Offer(q.TargetCallsign), q.Blurb);
        Assert.DoesNotContain(Wire(b), e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.Contains("DEV ?press=1", b.Pulse, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>?press=filed</c> · her story is on the system wire exactly once — the one WITH the tin, because the
    /// tin was dug — the floor's opinion is on the port's rag and not on the wire, and the book has filed that
    /// it ran, once. Nothing else moved: no heat.
    ///
    /// <para><b>RED</b> by dropping the <c>PrintsIn</c> clause from <c>NewsFeed</c>: the floor's line reached the
    /// ship's own wire.</para>
    /// </summary>
    [Fact]
    public async Task TheFiledStartHasHerStoryOnTheWireOnceAndTheFloorOnTheRag()
    {
        DeskBench b = await DeskBench.BootAsync(Filed);
        Map.Quest q = Hers(b);
        Assert.Equal(Map.QuestState.TurnedIn, q.State);
        string with = CarryThePress.Story("Luna", withTheTin: true);
        string without = CarryThePress.Story("Luna", withTheTin: false);
        string floor = CarryThePress.Floor("Luna");

        IReadOnlyList<NewsWire.NewsItem> wire = Feed(b, NewsWire.NewsScope.SystemWire);
        Assert.Single(wire, i => i.Headline == with);
        Assert.DoesNotContain(wire, i => i.Headline == without);
        Assert.DoesNotContain(wire, i => i.Headline == floor);
        Assert.Contains(CarryThePress.Byline, wire.Single(i => i.Headline == with).Subjects, StringComparison.Ordinal);

        IReadOnlyList<NewsWire.NewsItem> rag = Feed(b, NewsWire.NewsScope.PortRag);
        Assert.Single(rag, i => i.Headline == floor);
        Assert.Single(rag, i => i.Headline == with);

        Assert.Equal(1, BookEntries(b, CarryThePress.StoryRanLine));
        Assert.Contains((IEnumerable<Core.Satchel.Item>)b.Peek("_satchel")!, i => i.Kind == Core.Satchel.Kind.Paper && i.Id == CarryThePress.NoteId);
    }

    // ── THE WIRE ACROSS A SAVE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE STORY SURVIVES A VAULT ROUND-TRIP, PRINTED ONCE, NEVER TWICE. Pushed events are not saved: the quest
    /// record is, through the real serializer, and the story comes back onto the wire from it — once, however
    /// many sim advances run — and the book is not written in a second time.
    ///
    /// <para><b>RED</b> two ways: deleting the already-on-the-wire check in <c>OnTheWire</c> (two stories after two
    /// advances), and deleting the <c>!p.Printed</c> gate (a second "Her story ran" in the book).</para>
    /// </summary>
    [Fact]
    public async Task TheStorySurvivesTheVaultPrintedOnceNeverTwice()
    {
        DeskBench b = await DeskBench.BootAsync(Filed);
        string with = CarryThePress.Story("Luna", withTheTin: true);

        var section = (QuestsSection)b.Call("BuildQuestsSection")!;
        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault { SavedSimTime = 1.0, Quests = section }));

        Wire(b).Clear();
        b.Call("ApplyObligationsAndQuests", back.Quests);
        Assert.Equal(Map.QuestState.TurnedIn, Hers(b).State);

        b.Call("ThePressRunsHerStory");
        b.Call("ThePressRunsHerStory");
        Assert.Single(Wire(b), e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.Equal(with, Wire(b).Single(e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled).Subject);
        Assert.Single(Wire(b), e => e.Kind == NewsWire.NewsEventKind.PressFloorReaction);
        Assert.Equal(1, BookEntries(b, CarryThePress.StoryRanLine));
    }

    // ── THE CLOCK, AND THE TIN'S CHOICE ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FARE AND THE CLOCK. A trip that is done pays at the clamp, in her line, once; and the story is not on
    /// the wire one second before three sim-days after that.
    ///
    /// <para><b>RED</b> by comparing <c>StoryIsDue</c> against the turn-in itself in <c>ThePressRunsHerStory</c>:
    /// the story printed the instant she paid.</para>
    /// </summary>
    [Fact]
    public async Task ShePaysAtTheClampAndTheStoryWaitsThreeDays()
    {
        DeskBench b = await DeskBench.BootAsync(Aboard);
        Map.Quest q = Hers(b);
        q.State = Map.QuestState.Complete;
        int purse = (int)b.Peek("_credits")!;
        double now = (double)b.Peek("SimTime")!;

        b.Call("HerFareAtTheBerth");
        q = Hers(b);
        Assert.Equal(Map.QuestState.TurnedIn, q.State);
        Assert.Equal(purse + q.Reward, (int)b.Peek("_credits")!);
        Assert.Equal(now, CarryThePress.Passage.Read(q.Pin).TurnedIn);
        Assert.Contains(CarryThePress.TurnInLine, b.Pulse, StringComparison.Ordinal);

        b.Call("HerFareAtTheBerth");   // …and once: a paid fare is not paid again
        Assert.Equal(purse + q.Reward, (int)b.Peek("_credits")!);

        b.Poke("SimTime", now + (3 * Day) - 1);
        b.Call("ThePressRunsHerStory");
        Assert.DoesNotContain(Wire(b), e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.Equal(0, BookEntries(b, CarryThePress.StoryRanLine));

        b.Poke("SimTime", now + (3 * Day));
        b.Call("ThePressRunsHerStory");
        Assert.Single(Wire(b), e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.Equal(now + (3 * Day), Wire(b).Single(e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled).SimTime);
    }

    /// <summary>
    /// EXACTLY ONE OF THE TWO, CHOSEN BY THE TIN. A trip whose tin stayed in the ground prints the story without
    /// it, and never the one with it.
    ///
    /// <para><b>RED</b> by passing <c>true</c> for the tin at the push site.</para>
    /// </summary>
    [Fact]
    public async Task ATripWithoutTheTinPrintsTheOtherStoryAndNeverBoth()
    {
        DeskBench b = await DeskBench.BootAsync(Filed);
        Map.Quest q = Hers(b);
        Rewrite(b, q, CarryThePress.Passage.Read(q.Pin) with { Tin = false, Printed = false });
        Wire(b).Clear();

        b.Call("ThePressRunsHerStory");
        List<NewsWire.NewsEvent> stories = Wire(b).Where(e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled).ToList();
        Assert.Equal(CarryThePress.Story("Luna", withTheTin: false), Assert.Single(stories).Subject);
    }

    // ── HER BODY, AND WHERE IT IS DRAWN ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE IS NEVER DRAWN WITHOUT AN ACTIVE CONTRACT'S EXCURSION. On Luna with no contract: nobody. With a
    /// contract for a DIFFERENT ground on the same moon: nobody. With hers: she is on the ground with her own
    /// plate, her word about the tin is filed once, and the moment the trip is done she is gone.
    ///
    /// <para><b>RED</b> by dropping the <c>State: QuestState.Active</c> clause from
    /// <c>TheStringerOnThisGround</c>: she was still walking the regolith after the contract was Complete.</para>
    /// </summary>
    [Fact]
    public async Task SheIsDrawnOnlyOnHerGroundWhileHerContractIsActive()
    {
        DeskBench b = await DeskBench.BootAsync(OnLuna);
        Assert.True(b.OnSurface, "premise: the land cheat put the captain on Luna");
        await b.RenderAsync();
        object ex = b.Peek("_surface")!;
        object siteObj = ex.GetType().GetProperty("Site")!.GetValue(ex)!;
        int site = (int)siteObj.GetType().GetProperty("Index")!.GetValue(siteObj)!;

        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.DoesNotContain(Afoot(b), w => w.For == Map.Errand.RidingAlong);

        var elsewhere = new Map.Quest("press-901", Map.QuestKind.CarryThePress, CarryThePress.Giver, "", "Luna",
            CarryThePress.CardTitle, "", 500, DestBodyId: "luna", SourceBodyId: "selene-gate",
            Pin: new CarryThePress.Passage(site + 1).Write());
        Quests(b).Add(elsewhere);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.DoesNotContain(Afoot(b), w => w.For == Map.Errand.RidingAlong);
        Quests(b).Remove(elsewhere);

        var hers = new Map.Quest("press-902", Map.QuestKind.CarryThePress, CarryThePress.Giver, "", "Luna",
            CarryThePress.CardTitle, "", 500, DestBodyId: "luna", SourceBodyId: "selene-gate",
            Pin: new CarryThePress.Passage(site).Write());
        Quests(b).Add(hers);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Map.Walker her = Assert.Single(Afoot(b), w => w.For == Map.Errand.RidingAlong);
        Assert.Equal(CarryThePress.Plate, her.Walk.Plate);
        string landing = CarryThePress.Landing(CarryThePress.TheTin("press-902", "luna", site).BearingLine);
        Assert.Equal(1, BookEntries(b, landing));
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Landed);

        Hers(b).State = Map.QuestState.Complete;
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.DoesNotContain(Afoot(b), w => w.For == Map.Errand.RidingAlong);
    }

    /// <summary>
    /// THE TIN COMES UP UNDER THE PROBE, ONCE. At the tin's square the shovel finds it: the sleeve holds the
    /// press note, the contract remembers the tin, and the line is the tin's and hers. A second probe there
    /// finds regolith; and the liftoff ends her trip.
    ///
    /// <para><b>RED</b> by deleting the <c>PassageOf(q).Tin</c> clause from <c>TheTinComesUp</c>: the same hole
    /// gave up a second tin.</para>
    /// </summary>
    [Fact]
    public async Task TheTinComesUpUnderTheProbeOnceAndTheLiftoffEndsTheTrip()
    {
        DeskBench b = await DeskBench.BootAsync(OnLuna);
        object ex = b.Peek("_surface")!;
        object siteObj = ex.GetType().GetProperty("Site")!.GetValue(ex)!;
        int site = (int)siteObj.GetType().GetProperty("Index")!.GetValue(siteObj)!;
        var hers = new Map.Quest("press-903", Map.QuestKind.CarryThePress, CarryThePress.Giver, "", "Luna",
            CarryThePress.CardTitle, "", 500, DestBodyId: "luna", SourceBodyId: "selene-gate",
            Pin: new CarryThePress.Passage(site).Write());
        Quests(b).Add(hers);

        var tin = ((int X, int Y)?)b.Call("TheTinsSquare", hers, ex);
        Assert.NotNull(tin);
        Assert.False((bool)b.Call("TheTinComesUp", ex, tin!.Value.X + 5, tin.Value.Y + 5)!, "a probe five squares off found it");
        Assert.True((bool)b.Call("TheTinComesUp", ex, tin.Value.X + 1, tin.Value.Y - 1)!);
        Assert.Contains($"⛏ {CarryThePress.TinText} {CarryThePress.DigLine}", b.Pulse, StringComparison.Ordinal);
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Tin);
        Assert.Single((IEnumerable<Core.Satchel.Item>)b.Peek("_satchel")!, i => i.Id == CarryThePress.NoteId);
        Assert.False((bool)b.Call("TheTinComesUp", ex, tin.Value.X, tin.Value.Y)!);
        Assert.Single((IEnumerable<Core.Satchel.Item>)b.Peek("_satchel")!, i => i.Id == CarryThePress.NoteId);

        b.Call("SheSleepsTheBurnBack", ex);
        Assert.Equal(Map.QuestState.Complete, Hers(b).State);
        Assert.Contains(CarryThePress.LiftoffLine, b.Pulse, StringComparison.Ordinal);
    }
}
