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
    private const string Pending = "/map?dock=selene-gate&press=pending";
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
    /// <c>?press=pending</c> · THE SPIKE IT ROW, SEEN AT THE DESK. Booted, the desk opened through the comms node
    /// a player clicks, and the dark-web desk's own subtree read: the row's label and its canon line (her body,
    /// twice her fare) are there; the SPIKE IT button pressed, the purse is unchanged, the client's page is in
    /// the satchel, and the row is gone from the redrawn desk. The guide row for this start promises exactly this.
    ///
    /// <para><b>RED</b> by staging <c>press=pending</c> as <c>filed</c> (its <c>TurnedIn</c> four sim-days back):
    /// the story had run, and the desk carried no row.</para>
    /// </summary>
    [Fact]
    public async Task ThePendingStartPutsSpikeItOnTheDesk()
    {
        DeskBench b = await DeskBench.BootAsync(Pending);
        Map.Quest q = Hers(b);
        Assert.Equal(Map.QuestState.TurnedIn, q.State);
        Assert.False(CarryThePress.Passage.Read(q.Pin).Spike);
        Assert.Contains("DEV ?press=pending", b.Pulse, StringComparison.Ordinal);
        string body = (string)b.Call("BodyName", q.DestBodyId!)!;
        string line = SpikeIt.Row(body, SpikeIt.Purse(q.Reward));

        await b.SwitchAsync(Pages.ShipDesk.Comms);
        DeskBench.Painted comms = await b.RenderAsync();
        DeskBench.Painted.Node node = comms.Root.Descendants()
            .First(n => n.HasClass("comms-node") && n.Spoken.Contains("Dark web market", StringComparison.Ordinal));
        await b.PressAsync(node.Handlers["onclick"]);
        DeskBench.Painted.Node desk = TheDarkWebDesk(await b.RenderAsync());
        Assert.Contains(line, desk.Spoken, StringComparison.Ordinal);
        DeskBench.Painted.Node take = desk.Descendants()
            .Single(n => n.Element == "button" && n.Name == SpikeIt.RowLabel);

        int purse = (int)b.Peek("_credits")!;
        await b.PressAsync(take.Handlers["onclick"]);
        desk = TheDarkWebDesk(await b.RenderAsync());
        Assert.DoesNotContain(line, desk.Spoken, StringComparison.Ordinal);
        Assert.DoesNotContain(desk.Descendants(), n => n.Element == "button" && n.Name == SpikeIt.RowLabel);
        Assert.Equal(purse, (int)b.Peek("_credits")!);
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Spike);
        Assert.Contains(((IEnumerable<Satchel.Item>)b.Peek("_satchel")!), i => SpikeIt.IsTheSwap(i.Id));
        Assert.Empty(b.EscapedPastTheGate);
    }

    private static DeskBench.Painted.Node TheDarkWebDesk(DeskBench.Painted painted) =>
        painted.Root.Descendants().Single(n => n.HasClass("dark-web-card") && !n.Hidden);
    /// <summary>
    /// <c>?press=1</c> · THE LEDGER ROW NAMES HER AND HER GROUND, read off the painted page (0 Captain → 📜
    /// Ledger), not off the projection: the row a captain reads says who he is carrying and where to. The guide
    /// row for this start promises "a CARRY THE PRESS row, giver Rauha Lind".
    ///
    /// <para><b>RED</b> by deleting the <c>QuestKind.CarryThePress</c> arm of the ledger's detail switch: the row
    /// read the title, "▶ On the hook" and the purse, and neither her name nor the ground.</para>
    /// </summary>
    [Fact]
    public async Task TheLedgerRowNamesHerAndHerGround()
    {
        DeskBench b = await DeskBench.BootAsync(Aboard);
        Map.Quest q = Hers(b);
        await b.SwitchAsync(Pages.ShipDesk.Captain);
        DeskBench.Painted painted = await b.RenderAsync();
        DeskBench.Painted.Node ledgerTab = painted.Root.Descendants()
            .Single(n => n.Element == "button" && n.Name == "📜 Ledger");
        await b.PressAsync(ledgerTab.Handlers["onclick"]);
        painted = await b.RenderAsync();

        DeskBench.Painted.Node row = painted.Root.Descendants().Single(n =>
            n.HasClass("captain-card") && n.Spoken.StartsWith(CarryThePress.CardTitle, StringComparison.Ordinal));
        Assert.Contains("Rauha Lind", row.Spoken, StringComparison.Ordinal);
        Assert.Contains(q.TargetCallsign, row.Spoken, StringComparison.Ordinal);
        Assert.StartsWith("Luna · ", q.TargetCallsign, StringComparison.Ordinal);
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
        // …and something else goes in the book between two sim advances, because the book already folds an
        // entry identical to the one before it: a second "Her story ran" must be refused by the contract's own
        // memory, not by that fold.
        b.Call("FileNote", "a line between two advances", "✎");
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
        string landing = CarryThePress.Landing(CarryThePress.TheTin("press-902", "luna", site).BearingLine);

        // On the pad — the tube's own spawn — she has said nothing yet: her word waits for the captain's first
        // step onto the regolith (on a ground with no first-ground card; the card's close is the other moment).
        double x0 = (double)b.Peek("_avatarX")!, y0 = (double)b.Peek("_avatarY")!;
        b.Poke("_avatarX", Rendering.MoonSurface.SpawnX);
        b.Poke("_avatarY", Rendering.MoonSurface.SpawnY);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(0, BookEntries(b, landing));
        Assert.False(CarryThePress.Passage.Read(Hers(b).Pin).Landed);

        b.Poke("_avatarX", x0);
        b.Poke("_avatarY", y0);
        Afoot(b).RemoveAll(w => w.For == Map.Errand.RidingAlong);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Map.Walker her = Assert.Single(Afoot(b), w => w.For == Map.Errand.RidingAlong);
        Assert.Equal(CarryThePress.Plate, her.Walk.Plate);

        // Off the pad, but the slot still holds the landing's own line (no frame has aged it here): she waits.
        // The frame the slot comes free, she says it — once.
        Assert.NotNull(((PulseSlot)b.Peek("_pulse")!).Message);
        Assert.Equal(0, BookEntries(b, landing));
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(landing, ((PulseSlot)b.Peek("_pulse")!).Message);
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
    /// <summary>
    /// THE WALK, PLAYED. <c>?press=1</c> with the shuttle ridden straight down onto HER ground: she comes down
    /// behind the captain, her word about the tin is in the pulse and the book, and as he walks eighty deck units out
    /// onto the regolith and stops she follows on her own feet and ends inside the band behind him — and says
    /// her line about the tank, once.
    ///
    /// <para><b>RED</b> by deleting the re-plan on leaving the band in <c>AdvanceTheStringer</c>: she stood at the
    /// tube while he walked away. (Written before the fix it guards: the first cut re-planned only when he was
    /// too FAR, planned to the near edge of the band, and this play caught her standing 6.8 du off him.)</para>
    /// </summary>
    [Fact]
    public async Task SheComesDownBehindHimAndKeepsTheBandAsHeWalks()
    {
        int site = CarryThePress.Passage.Read(Hers(await DeskBench.BootAsync(Aboard)).Pin).Site;
        DeskBench b = await DeskBench.BootAsync($"{Aboard}&body=luna&site={site}&land=1");
        Assert.True(b.OnSurface, "premise: the shuttle set the captain down on her ground");
        await b.RenderAsync();

        b.Poke("_pulse", PulseSlot.Empty);   // the landing's lines have had their dwell (no frames age them here)
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.1);
        Map.Quest q = Hers(b);
        string landing = CarryThePress.Landing(CarryThePress.TheTin(q.Id, "luna", site).BearingLine);
        Assert.Equal(1, BookEntries(b, landing));   // the land cheat sets him down below the pad, on the regolith
        Assert.Single(Afoot(b), w => w.For == Map.Errand.RidingAlong);

        // Eighty deck units out, a few a second, and then he stands still.
        double x0 = (double)b.Peek("_avatarX")!, y0 = (double)b.Peek("_avatarY")!;
        for (int step = 1; step <= 100; step++)
        {
            b.Poke("_avatarY", y0 - (step * 0.8));
            b.CallOnTheDispatcher("AdvanceTheStringer", 0.1);
        }

        b.Poke("_pulse", PulseSlot.Empty);   // her word has had its dwell (no frames age the slot here)
        for (int still = 0; still < 600; still++)
        {
            b.CallOnTheDispatcher("AdvanceTheStringer", 0.1);
        }

        Map.Walker her = Assert.Single(Afoot(b), w => w.For == Map.Errand.RidingAlong);
        double dx = her.Walk.X - x0, dy = her.Walk.Y - (y0 - 80);
        double range = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(TheTailBehindYou.HoldsHisBand(range),
            $"she is {range:F1} du from the captain, outside the band [{TheTailBehindYou.StandsOffDu}, {TheTailBehindYou.LosesYouBeyondDu}]");
        Assert.True(her.Walk.Y < y0 - 5, $"she never left the pad: y {her.Walk.Y:F1}, the captain came down at {y0:F1}");
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Walked, "she never said her line about the tank");
    }

    // ── HER WORD, WHERE THE CAPTAIN CAN READ IT ─────────────────────────────────────────────────────────

    private static DeskBench.Painted.Node TheToast(DeskBench.Painted painted) =>
        Assert.Single(painted.Root.Descendants(), n => n.HasClass("deck-pulse-toast"));

    /// <summary>One live frame through the page's own <c>OnTick</c>, 100 ms after the last, on the renderer's
    /// dispatcher. The canvas flush is the one line that crosses into JavaScript
    /// (<c>CastawayBench.Frame</c>'s seam); everything the pulse is made of has run by then.</summary>
    private static void Frame(DeskBench b)
    {
        double at = Convert.ToDouble(b.Peek("_lastTimestampMs") ?? 0.0) + 100;
        try
        {
            b.CallOnTheDispatcher("OnTick", at);
        }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
        {
        }
    }

    /// <summary>
    /// HER WORD REACHES THE SCREEN, AFTER THE SUIT'S, AND EACH IS HELD FOR ITS DWELL. The real landing
    /// (<c>?press=1&amp;body=luna&amp;site=N&amp;land=1</c>) on a fresh captain; the first-ground card closed
    /// through its own button; then twelve seconds of LIVE frames through <c>OnTick</c>, reading the pulse slot
    /// the deck's <c>.deck-pulse-toast</c> draws after every one. The slot must read the suit's VACUUM crossing
    /// first, for its whole dwell, and then her line — the very next thing in the slot, filed in the book on the
    /// frame it appears, and up for her whole length-scaled dwell (<see cref="PulseSlot.DwellFor"/>); and then the
    /// tracker's first stir, which is ambience (Fable, 2026-09-29) and waits for the slot as she does.
    ///
    /// <para><b>RED</b> on #1324's code (the stir written the moment the first tide Reever rose): measured there,
    /// VACUUM 100 → 4200 ms, her word 4200 → 5700 ms, and the stir cut her off 1.5 s into a 7.6 s read.</para>
    ///
    /// <para><b>The one poke.</b> In a browser the descent pays one warm-up surface step under the door with the
    /// captain in her tube (<c>WarmFirstSurfaceFrameAsync</c>), which is what records <c>_airSupplyNoted</c> as
    /// her air; the bench has no canvas, so that step is skipped and the crossing would never be said. The
    /// field is set to what that step records, and nothing else is.</para>
    ///
    /// <para><b>RED</b> on #1321's code (her line said when the card closed, <c>AdvanceTheStringer</c> not waiting
    /// for the slot): the first live frame's VACUUM wrote over it and her line was never in the slot on any frame
    /// — the seventh bug class a second time, found by QA polling the toast in a real Chromium.</para>
    /// </summary>
    [Fact]
    public async Task HerWordIsSaidAfterTheSuitsCrossingHasHadItsDwell()
    {
        int site = CarryThePress.Passage.Read(Hers(await DeskBench.BootAsync(Aboard)).Pin).Site;
        DeskBench b = await DeskBench.BootAsync($"{Aboard}&body=luna&site={site}&land=1");
        Assert.True(b.OnSurface, "premise: the shuttle set the captain down on her ground");
        Assert.True((bool)b.Peek("_groundLessonOpen")!, "premise: a fresh captain's first ground raises the card");
        string landing = CarryThePress.Landing(CarryThePress.TheTin(Hers(b).Id, "luna", site).BearingLine);
        string vacuum = SuitAir.SupplyChangedLine(SuitAir.Supply.Tanks);

        DeskBench.Painted painted = await b.RenderAsync();
        Assert.StartsWith("🛸 Shuttle mated to Luna.", TheToast(painted).Spoken, StringComparison.Ordinal);
        Assert.Equal(0, BookEntries(b, landing));
        b.Poke("_renderer", new Rendering.CanvasRenderer("press-pulse"));
        b.Poke("_deckView", new Rendering.DeckView(new CastawayBench.APenThatDrawsNothing()));
        b.Poke("_airSupplyNoted", (SuitAir.Supply?)SuitAir.Supply.Ship);

        DeskBench.Painted.Node close = Assert.Single(painted.Root.Descendants(), n =>
            n.Element == "button" && n.Name == GroundLesson.Dismiss && n.Handlers.ContainsKey("onclick"));
        await b.PressAsync(close.Handlers["onclick"]);
        Assert.Equal(0, BookEntries(b, landing));   // not at the close: the suit has not spoken yet

        // Every change of the slot, with the frame clock it happened on.
        var said = new List<(string? Line, double AtMs)>();
        for (int frame = 0; frame < 200; frame++)
        {
            Frame(b);
            PulseSlot slot = (PulseSlot)b.Peek("_pulse")!;
            if (said.Count == 0 || said[^1].Line != slot.Message)
            {
                said.Add((slot.Message, (double)b.Peek("_lastTimestampMs")!));
                if (slot.Message == landing)
                {
                    Assert.Equal(1, BookEntries(b, landing));   // filed on the frame it is said, not before
                }
            }
        }

        string story = string.Join(Environment.NewLine, said.Select(s => $"  {s.AtMs,8:F0} ms  {s.Line ?? "(empty)"}"));
        int v = said.FindIndex(s => s.Line == vacuum);
        int h = said.FindIndex(s => s.Line == landing);
        Assert.True(v >= 0, $"the suit's crossing was never in the slot:{Environment.NewLine}{story}");
        Assert.True(h >= 0, $"her word was never in the slot:{Environment.NewLine}{story}");
        Assert.True(h > v, $"her word came before the suit's:{Environment.NewLine}{story}");
        Assert.True(said.Skip(v + 1).Take(h - v - 1).All(s => s.Line is null),
            $"something else was said between the suit's line and hers:{Environment.NewLine}{story}");
        Assert.True(said[v + 1].AtMs - said[v].AtMs >= PulseSlot.DwellFor(vacuum),
            $"the suit's line did not have its dwell ({PulseSlot.DwellFor(vacuum)} ms):{Environment.NewLine}{story}");
        double herSpan = (h + 1 < said.Count ? said[h + 1].AtMs : (double)b.Peek("_lastTimestampMs")!) - said[h].AtMs;
        Assert.True(herSpan >= PulseSlot.DwellFor(landing),
            $"her word was up only {herSpan} ms of its {PulseSlot.DwellFor(landing)} ms dwell:{Environment.NewLine}{story}");

        // …and the tracker's first stir is ambience: it waited for her, and it is the next thing said.
        (string? Line, double AtMs) next = said.Skip(h + 1).FirstOrDefault(s => s.Line is not null);
        Assert.True(next.Line is not null && next.Line.StartsWith("〜 The tracker stirs", StringComparison.Ordinal),
            $"the tracker's first stir did not follow her word:{Environment.NewLine}{story}");
        Assert.Equal(1, BookEntries(b, landing));
    }
}
