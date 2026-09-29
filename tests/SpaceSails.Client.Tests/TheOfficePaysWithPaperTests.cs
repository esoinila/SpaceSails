using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 4 · <b>CHARGED TO PRESERVATION, ON A LIVE PAGE.</b> The words and the two decisions are Core's
/// (<c>ChargedToPreservationTests</c>); what is driven here is the page's half: the receipt going into the satchel
/// with the dark-web desk's 💳 (and the book's entry under the Authority and the body), and the port rag's line the
/// cycle after a SPIKED window. Every guard was watched go red on the revert its summary names.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class TheOfficePaysWithPaperTests
{
    private const double Day = 86400.0;

    // ── the bench (TheStoryCanBeSpikedTests' own) ───────────────────────────────────────────────────────

    private static Pages.Map Clamped(string canvas)
    {
        Pages.Map map = Boot(canvas);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    private static List<Pages.Map.Quest> Quests(Pages.Map map) => (List<Pages.Map.Quest>)Read(map, "_quests")!;

    private static Pages.Map.Quest Hers(Pages.Map map) =>
        Assert.Single(Quests(map), q => q.Kind == Pages.Map.QuestKind.CarryThePress);

    private static CarryThePress.Passage PassageOf(Pages.Map map) => CarryThePress.Passage.Read(Hers(map).Pin);

    private static void Rewrite(Pages.Map map, CarryThePress.Passage p) => Invoke(map, "RewritePassage", Hers(map), p);

    private static double Now(Pages.Map map) => (double)Read(map, "SimTime")!;

    /// <summary>Her contract, paid <paramref name="ago"/> seconds back, with a spike (or not) against it.</summary>
    private static void Plant(Pages.Map map, double ago, bool spike, SpikeIt.Pages pages = SpikeIt.Pages.OnHerTable)
    {
        string site = FieldNotes.PlaceLabel("Luna", LandingSites.At("luna", 0).Name);
        var offer = new Pages.Map.Quest("press-guard", Pages.Map.QuestKind.CarryThePress, CarryThePress.Giver, "",
            site, CarryThePress.CardTitle, CarryThePress.Offer(site), 1250, DestBodyId: "luna", SourceBodyId: Port,
            Pin: new CarryThePress.Passage(0).Write());
        Quests(map).Add(offer);
        var home = (Pages.Map.Quest)Invoke(map, "RewritePassage", offer, CarryThePress.Passage.Read(offer.Pin) with
        {
            Landed = true, Walked = true, Tin = true, TurnedIn = Now(map) - ago, Spike = spike, Pages = pages,
        })!;
        Invoke(map, "AdvanceMission", home, Pages.Map.QuestState.TurnedIn, null);
    }

    private static string Body(Pages.Map map) => (string)Invoke(map, "BodyName", Hers(map).DestBodyId!)!;

    private static List<Satchel.Item> Satchel_(Pages.Map map) => [.. (IEnumerable<Satchel.Item>)Read(map, "_satchel")!];

    private static int Receipts(Pages.Map map) =>
        Satchel_(map).Where(i => i.Kind == Satchel.Kind.Paper && SpikeIt.IsTheReceipt(i.Id)).Sum(i => i.Count);

    private static List<FieldNote> Book(Pages.Map map) => [.. (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!];

    private static List<NewsWire.NewsEvent> Wire(Pages.Map map) => (List<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;

    private static int RagLines(Pages.Map map) =>
        Wire(map).Count(e => string.Equals(e.Subject, SpikeIt.BylineMissingLine, StringComparison.Ordinal));

    private static IReadOnlyList<NewsWire.NewsItem> Feed(Pages.Map map, NewsWire.NewsScope scope) =>
        (IReadOnlyList<NewsWire.NewsItem>)Invoke(map, "NewsFeed", 0, scope, null)!;

    private static string Pulse(Pages.Map map) => Read(map, "_pulse")?.ToString() ?? "";

    // ── THE RECEIPT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A PAID SPIKE LEAVES A LINE ITEM, ONCE: on the desk's payout after a SPIKED window (full purse) or an ALTERED
    /// one (half) the receipt goes into the satchel with the 💳, and the book files its document once under 📋 about
    /// the Authority and the story's body; the pulse is still the bare 💳 with no words. A second desk open pays
    /// nothing and lands nothing. <b>RED</b> by dropping the <c>TheOfficePaysWithPaper</c> call from
    /// <c>TheSpikeIsSettled</c> (no paper, no entry).
    /// </summary>
    [Theory]
    [InlineData(SpikeIt.Pages.Taken)]
    [InlineData(SpikeIt.Pages.Swapped)]
    public void APaidSpikeLeavesOneLineItem(SpikeIt.Pages pages)
    {
        Pages.Map map = Clamped($"receipt-{pages}");
        Plant(map, CarryThePress.StoryAfterSeconds + 1, spike: true, pages);
        int before = (int)Read(map, "_credits")!;
        Assert.Equal(0, Receipts(map));

        Invoke(map, "TheSpikeIsSettled");
        int paid = (int)Read(map, "_credits")! - before;
        Assert.True(paid > 0);
        Assert.Contains($"Message = 💳 +{paid:N0} cr,", Pulse(map), StringComparison.Ordinal);   // the bare 💳, no words
        Assert.Equal(1, Receipts(map));
        Satchel.Item receipt = Satchel_(map).Single(i => SpikeIt.IsTheReceipt(i.Id));
        Assert.Equal(SpikeIt.TheReceipt(Body(map)), receipt);

        FieldNote entry = Assert.Single(Book(map), n => n.Text == SpikeIt.ReceiptDocument);
        Assert.Equal(SpikeIt.ReceiptGlyph, entry.Glyph);
        Assert.Equal(SpikeIt.ReceiptSubjects(Body(map)), entry.Subjects);
        Assert.Contains(MoneyTrail.TheOffice, CaseSubjects.On(entry));
        Assert.Contains(CaseSubjects.Place(Body(map)), CaseSubjects.On(entry));

        Invoke(map, "TheSpikeIsSettled");
        Invoke(map, "TheSpikeIsSettled");
        Assert.Equal(paid, (int)Read(map, "_credits")! - before);
        Assert.Equal(1, Receipts(map));
        Assert.Single(Book(map), n => n.Text == SpikeIt.ReceiptDocument);
    }

    /// <summary>
    /// NO PAYOUT, NO PAPER: a LATE window (her pages untouched) pays nothing and itemises nothing; a spike whose
    /// window has not come lands nothing; and a contract nobody spiked, long past its window and its floor, leaves
    /// the satchel and the book exactly as they were. <b>RED</b> by dropping the <c>ReceiptLands</c> clause from
    /// <c>TheOfficePaysWithPaper</c> (a LATE window itemised).
    /// </summary>
    [Fact]
    public void NoPayoutNoPaper()
    {
        Pages.Map late = Clamped("receipt-late");
        Plant(late, CarryThePress.StoryAfterSeconds + 1, spike: true, SpikeIt.Pages.OnHerTable);
        Invoke(late, "TheSpikeIsSettled");
        Assert.Contains(SpikeIt.LateLine, Pulse(late), StringComparison.Ordinal);
        Assert.Equal(0, Receipts(late));
        Assert.DoesNotContain(Book(late), n => n.Text == SpikeIt.ReceiptDocument);

        Pages.Map early = Clamped("receipt-early");
        Plant(early, Day, spike: true, SpikeIt.Pages.Taken);
        Invoke(early, "TheSpikeIsSettled");
        Assert.Equal(0, Receipts(early));

        Pages.Map plain = Clamped("receipt-nospike");
        Plant(plain, CarryThePress.StoryAfterSeconds + CarryThePress.FloorAfterStorySeconds + 1, spike: false);
        List<Satchel.Item> had = Satchel_(plain);
        Invoke(plain, "TheSpikeIsSettled");
        Invoke(plain, "ThePressRunsHerStory");
        Assert.Equal(had, Satchel_(plain));
        Assert.Equal(0, Receipts(plain));
        Assert.DoesNotContain(Book(plain), n => n.Text == SpikeIt.ReceiptDocument);
        Assert.Equal(0, RagLines(plain));
    }

    // ── THE RAG ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CYCLE AFTER A SPIKED WINDOW, THE PORT RAG NOTICES — ONCE, AND ONLY THERE: nothing the day of the window;
    /// the line on the wire once the floor's own clock comes round, dated then; printed under a port's rag and never
    /// on the system wire; a second (and third) advance does not print it again; and the contract remembers it on
    /// its <c>Floored</c> key. <b>RED</b> by dropping the <c>FloorIsDue</c> clause from <c>TheRagNoticesTheHole</c>
    /// (printed the day of the window), and by pushing it with the body as its Detail (a ✂ CLIP filed it under
    /// Luna).
    /// </summary>
    [Fact]
    public void TheCycleAfterASpikedWindowThePortRagNotices()
    {
        Pages.Map map = Clamped("rag-spiked");
        Plant(map, CarryThePress.StoryAfterSeconds + 1, spike: true, SpikeIt.Pages.Taken);
        Invoke(map, "ThePressRunsHerStory");
        Assert.Equal(SpikeIt.Outcome.Spiked, PassageOf(map).Outcome);
        Assert.Equal(0, RagLines(map));
        Assert.False(PassageOf(map).Floored);

        Rewrite(map, PassageOf(map) with
        {
            TurnedIn = Now(map) - CarryThePress.StoryAfterSeconds - CarryThePress.FloorAfterStorySeconds - 1,
        });
        Invoke(map, "ThePressRunsHerStory");
        Invoke(map, "ThePressRunsHerStory");
        Invoke(map, "ThePressRunsHerStory");
        Assert.Equal(1, RagLines(map));
        NewsWire.NewsEvent rag = Wire(map).Single(e => e.Subject == SpikeIt.BylineMissingLine);
        Assert.Equal(NewsWire.NewsEventKind.PressFloorReaction, rag.Kind);
        Assert.Equal(CarryThePress.FloorAt(PassageOf(map))!.Value, rag.SimTime);
        Assert.Equal("", NewsWire.SubjectsFor(rag));
        Assert.True(PassageOf(map).Floored);

        Assert.Single(Feed(map, NewsWire.NewsScope.PortRag), i => i.Headline == SpikeIt.BylineMissingLine);
        Assert.DoesNotContain(Feed(map, NewsWire.NewsScope.SystemWire), i => i.Headline == SpikeIt.BylineMissingLine);

        // Nothing about her story, and none of the floor's own opinion, is on the wire.
        Assert.DoesNotContain(Wire(map), e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.DoesNotContain(Wire(map), e => e.Subject == CarryThePress.Floor(Body(map)));
    }

    /// <summary>
    /// ONLY A SPIKED HOLE: after an ALTERED window her byline ran, so the floor has its ordinary opinion and the
    /// rag never prints the byline line; after a LATE window likewise. <b>RED</b> by dropping the outcome clause
    /// from <c>TheRagNoticesTheHole</c> and asking it on the path of a story that ran (printed after ALTERED and
    /// LATE).
    /// </summary>
    [Theory]
    [InlineData(SpikeIt.Pages.Swapped)]
    [InlineData(SpikeIt.Pages.OnHerTable)]
    public void AStoryThatRanLeavesNoHoleOnTheRag(SpikeIt.Pages pages)
    {
        Pages.Map map = Clamped($"rag-ran-{pages}");
        Plant(map, CarryThePress.StoryAfterSeconds + CarryThePress.FloorAfterStorySeconds + 1, spike: true, pages);
        Invoke(map, "ThePressRunsHerStory");
        Assert.Equal(0, RagLines(map));
        Assert.Single(Wire(map), e => e.Kind == NewsWire.NewsEventKind.PressFloorReaction);
        Assert.Single(Wire(map), e => e.Subject == CarryThePress.Floor(Body(map)));
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>?spike=paid</c> · a SPIKED window this instant and the desk already paid: the purse on the credits, the
    /// receipt in the satchel beside her pages, the book's line under the Authority and the body, the contract
    /// Paid, and no rag line yet (it is a cycle off). <b>RED</b> by dropping the <c>TheSpikeIsSettled</c> call from
    /// the <c>Paid</c> arm of <c>SpikeItIfAsked</c>.
    /// </summary>
    [Fact]
    public async Task ThePaidStartHasTheReceiptAndNoRagYet()
    {
        DeskBench b = await DeskBench.BootAsync("/map?dock=selene-gate&ashore=1&spike=paid");
        var q = Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress);
        CarryThePress.Passage p = CarryThePress.Passage.Read(q.Pin);
        Assert.Equal(SpikeIt.Outcome.Spiked, p.Outcome);
        Assert.True(p.Paid);
        Assert.False(p.Floored);

        var satchel = (IEnumerable<Satchel.Item>)b.Peek("_satchel")!;
        Assert.Single(satchel, i => SpikeIt.IsTheReceipt(i.Id));
        Assert.Contains(satchel, i => SpikeIt.IsThePages(i.Id));
        Assert.Single((IEnumerable<FieldNote>)b.Peek("_fieldNotes")!,
            n => n.Text == SpikeIt.ReceiptDocument && n.Subjects.Length > 0);
        Assert.DoesNotContain((List<NewsWire.NewsEvent>)b.Peek("_newsEvents")!, e => e.Subject == SpikeIt.BylineMissingLine);
        Assert.Contains("DEV ?spike=paid", b.Pulse, StringComparison.Ordinal);
    }
}
