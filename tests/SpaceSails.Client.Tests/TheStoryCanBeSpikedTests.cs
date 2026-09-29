using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 2 · <b>SPIKE IT, ON A LIVE PAGE.</b> The words and the decisions are Core's (<c>SpikeItTests</c>);
/// what is driven here is the page doing its half on the page itself: the desk row, her at the far table in
/// Selene Gate's gallery on her own clock, the two moves on that table's card, the window's one outcome on the
/// wire and in the book, the desk's pulse after it, the contract across a save, and the two dev starts. Every
/// guard was watched go red on the revert its summary names.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class TheStoryCanBeSpikedTests
{
    private const double Day = 86400.0;
    private const double Tick = 1.0 / 30.0;

    // ── the room ────────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map Clamped(string canvas)
    {
        Pages.Map map = Boot(canvas);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        Assert.True(HavenInterior.HasObservationWalk(Port));
        return map;
    }

    private static List<Pages.Map.Quest> Quests(Pages.Map map) => (List<Pages.Map.Quest>)Read(map, "_quests")!;

    private static Pages.Map.Quest Hers(Pages.Map map) =>
        Assert.Single(Quests(map), q => q.Kind == Pages.Map.QuestKind.CarryThePress);

    private static CarryThePress.Passage PassageOf(Pages.Map map) =>
        CarryThePress.Passage.Read(Hers(map).Pin);

    private static void Rewrite(Pages.Map map, CarryThePress.Passage p) => Invoke(map, "RewritePassage", Hers(map), p);

    private static double Now(Pages.Map map) => (double)Read(map, "SimTime")!;

    /// <summary>Her contract, paid <paramref name="ago"/> seconds back, with a spike (or not) against it — the
    /// record play would have left, built by the page's own contract maker.</summary>
    private static Pages.Map.Quest Plant(Pages.Map map, double ago, bool spike, SpikeIt.Pages pages = SpikeIt.Pages.OnHerTable)
    {
        // Her ground is Luna's first site, named the way her card names it; the fare is slice 1's own arithmetic.
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
        return home;
    }

    private static string Body(Pages.Map map) => (string)Invoke(map, "BodyName", Hers(map).DestBodyId!)!;

    private static List<Satchel.Item> Satchel_(Pages.Map map) => [.. (IEnumerable<Satchel.Item>)Read(map, "_satchel")!];

    private static void Give(Pages.Map map, Satchel.Item item) =>
        Set(map, "_satchel", Satchel.Add(Satchel_(map), item).ToList());

    private static void RoomFrame(Pages.Map map, double dt = Tick) => Invoke(map, "AdvanceBarWalkers", dt);

    private static List<Pages.Map.Walker> BarAfoot(Pages.Map map) => (List<Pages.Map.Walker>)Read(map, "_barAfoot")!;

    private static Pages.Map.Walker? Her(Pages.Map map) =>
        BarAfoot(map).SingleOrDefault(w => w.For == Pages.Map.Errand.AtHerPages);

    private static void StandInTheGallery(Pages.Map map)
    {
        DeckReachability.Point island = HavenInterior.TheVendorsAt(Port)[^1];
        Set(map, "_avatarX", island.X);
        Set(map, "_avatarY", island.Y);
    }

    private static Pages.Map.TableTalk SitAt(Pages.Map map, int table)
    {
        DeckReachability.Point top = HavenInterior.GalleryTops(Port)[table];
        Set(map, "_avatarX", top.X);
        Set(map, "_avatarY", top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, $"gallery table {table} refused [E].");
        return (Pages.Map.TableTalk)Read(map, "SeatedTable")!;
    }

    /// <summary>Send her to the machine: her own clock run past her writing spell, and the room's frames until
    /// she stands at it.</summary>
    private static void SheGoesToTheMachine(Pages.Map map)
    {
        Pages.Map.Walker her = Her(map) ?? throw new InvalidOperationException("premise: she is at her table");
        her.PassHeld = SpikeIt.WritesAtMostSeconds + 1;
        for (int i = 0; i < 2000 && Her(map) is { } w && !(w.Table == -1 && w.Walk.State == NpcWalk.Doing.Arrived); i++)
        {
            RoomFrame(map, 0.1);
        }

        Assert.True(Her(map) is { Table: -1 } w2 && w2.Walk.State == NpcWalk.Doing.Arrived,
            "she never got to the machine.");
    }

    private static int Filed(Pages.Map map, string line) =>
        ((IEnumerable<FieldNote>)Read(map, "_fieldNotes")!).Count(n => string.Equals(n.Text, line, StringComparison.Ordinal));

    private static List<NewsWire.NewsEvent> Wire(Pages.Map map) => (List<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;

    private static string Pulse(Pages.Map map) => Read(map, "_pulse")?.ToString() ?? "";

    private static double Dist(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    private static Task Press(Pages.Map map, string move) => (Task)Invoke(map, "TableMoveClicked", move)!;

    // ── THE ROW ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ROW IS THERE ONLY WHILE HER STORY IS PENDING AND NO SPIKE IS IN HAND: absent with no contract, absent
    /// once the story is due, present in between with the canon line filled; taking it puts the client's page
    /// in the satchel, says the terms, and the row is gone.
    ///
    /// <para><b>RED</b> by dropping <c>SimTime &lt; at</c> from <c>TheStoryStillToRun</c> (the row sat on the
    /// desk over a story that had already run).</para>
    /// </summary>
    [Fact]
    public void TheRowIsThereOnlyWhileHerStoryIsPending()
    {
        Pages.Map map = Clamped("spike-row");
        Assert.Null(Invoke(map, "SpikeOnOffer"));

        Plant(map, CarryThePress.StoryAfterSeconds + 1, spike: false);
        Assert.Null(Invoke(map, "SpikeOnOffer"));

        Rewrite(map, PassageOf(map) with { TurnedIn = Now(map) - Day });
        var offer = (Pages.Stations.DarkWeb.SpikeOffer?)Invoke(map, "SpikeOnOffer");
        Assert.NotNull(offer);
        Assert.Equal(SpikeIt.Row(Body(map), SpikeIt.Purse(Hers(map).Reward)), offer!.Value.SubLine);

        Invoke(map, "TakeTheSpike");
        Assert.True(PassageOf(map).Spike);
        Assert.Contains(Satchel_(map), i => i.Kind == Satchel.Kind.Paper && SpikeIt.IsTheSwap(i.Id));
        Assert.Contains(SpikeIt.Taken(Body(map)), Pulse(map), StringComparison.Ordinal);
        Assert.Null(Invoke(map, "SpikeOnOffer"));
    }

    // ── HER AT HER TABLE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE IS AT THE FAR TABLE, PLATED, AND ON HER OWN CLOCK SHE FEEDS THE MACHINE AND COMES BACK. Her line is
    /// said once however long the captain stands in the gallery; she stands at the machine behind her table and
    /// walks back to her chair; and she is not on the floor with no spike in hand, nor once the window has come.
    ///
    /// <para><b>RED</b> by dropping the window clause from <c>AdvanceTheStringerAtHerPages</c> (she was still at
    /// her table after her story was due).</para>
    /// </summary>
    [Fact]
    public void SheWritesAtTheFarTableAndFeedsTheMachineOnHerOwnClock()
    {
        Pages.Map map = Clamped("spike-her");
        StandInTheGallery(map);
        RoomFrame(map);
        Assert.Null(Her(map));

        Plant(map, Day, spike: true);
        StandInTheGallery(map);
        for (int i = 0; i < 20; i++)
        {
            RoomFrame(map);
        }

        Pages.Map.Walker her = Her(map) ?? throw new InvalidOperationException("she is not at her table.");
        Assert.Equal(CarryThePress.Plate, her.Walk.Plate);
        Assert.Equal(SpikeIt.HerTable, her.Table);
        Assert.Equal(NpcWalk.Doing.Arrived, her.Walk.State);
        Assert.True(PassageOf(map).Seen);
        Assert.Contains(SpikeIt.AtHerTableLine, Pulse(map), StringComparison.Ordinal);

        DeckReachability.Point top = HavenInterior.GalleryTops(Port)[SpikeIt.HerTable];
        Assert.True(Dist(her.Walk.X - top.X, her.Walk.Y - top.Y) < 3 * DeckPlan.AvatarRadius,
            "she is not at the far table.");

        SheGoesToTheMachine(map);
        DeckReachability.Point machine = HavenInterior.TheVendorsAt(Port)[SpikeIt.HerTable];
        her = Her(map)!;
        Assert.True(Dist(her.Walk.X - machine.X, her.Walk.Y - machine.Y) < 1.0, "she is not at the machine.");

        her.PassHeld = SpikeIt.FeedsAtMostSeconds + 1;
        for (int i = 0; i < 2000 && Her(map) is { } w && !(w.Table == SpikeIt.HerTable && w.Walk.State == NpcWalk.Doing.Arrived); i++)
        {
            RoomFrame(map, 0.1);
        }

        Assert.True(Her(map) is { Table: SpikeIt.HerTable } back && back.Walk.State == NpcWalk.Doing.Arrived,
            "she never came back to her table.");

        Rewrite(map, PassageOf(map) with { TurnedIn = Now(map) - CarryThePress.StoryAfterSeconds });
        RoomFrame(map);
        Assert.Null(Her(map));
    }

    // ── THE TWO MOVES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MOVES ARE ABSENT WHILE SHE SITS AND AT THE OTHER TABLE: at her table with her in her chair the card is
    /// the plain table's; at the other table with her at the machine it is the plain table's; at her table with
    /// her away it carries TAKE THE PAGES — and loses it the moment somebody shares the table.
    ///
    /// <para><b>RED</b> by dropping the <c>t.Index != SpikeIt.HerTable</c> clause from <c>ThePagesMoveHere</c>
    /// (the move was on the other table's card), and by making <c>SheIsAway</c> answer true for a body at her
    /// table (the move was on the card with her in the chair).</para>
    /// </summary>
    [Fact]
    public void TheMovesAreAbsentWhileSheSitsAndAtTheOtherTable()
    {
        Pages.Map map = Clamped("spike-absent");
        Plant(map, Day, spike: true);
        Give(map, SpikeIt.TheSwap("Luna"));
        StandInTheGallery(map);
        RoomFrame(map);
        RoomFrame(map);
        Assert.NotNull(Her(map));

        Pages.Map.TableTalk t = SitAt(map, SpikeIt.HerTable);
        IReadOnlyList<string> plain = [.. t.Scene.Moves.Select(m => m.Id)];
        for (int i = 0; i < 5; i++)
        {
            RoomFrame(map);
        }

        Assert.Null(SpikeIt.Offers(t.Scene));
        Assert.Equal(plain, t.Scene.Moves.Select(m => m.Id));
        Invoke(map, "CloseTable");

        Pages.Map.TableTalk other = SitAt(map, 1 - SpikeIt.HerTable);
        IReadOnlyList<string> otherPlain = [.. other.Scene.Moves.Select(m => m.Id)];
        SheGoesToTheMachine(map);
        Assert.Null(SpikeIt.Offers(other.Scene));
        Assert.Equal(otherPlain, other.Scene.Moves.Select(m => m.Id));
        Invoke(map, "CloseTable");

        Pages.Map.TableTalk hers = SitAt(map, SpikeIt.HerTable);
        RoomFrame(map);
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(hers.Scene));
        Assert.Equal(SpikeIt.TakeThePages, hers.Scene.Moves[^2].Id);

        hers.SharedSeat = true;
        RoomFrame(map);
        Assert.Null(SpikeIt.Offers(hers.Scene));
        hers.SharedSeat = false;
        RoomFrame(map);
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(hers.Scene));
    }

    /// <summary>
    /// TAKE THE PAGES, THEN LEAVE YOUR PAGE: the first puts her pages in the satchel, files the book's line with
    /// the watches to the window, says the canon on the card and turns the card to the second move; the second
    /// takes both papers out of the satchel, says its line, and leaves the card plain. A stale second press of
    /// either writes nothing.
    ///
    /// <para><b>RED</b> by not removing <c>press-pages</c> on LEAVE YOUR PAGE (the pages were in the satchel and
    /// on her table at once).</para>
    /// </summary>
    [Fact]
    public async Task TakingThePagesThenLeavingYourPage()
    {
        Pages.Map map = Clamped("spike-moves");
        Plant(map, Day, spike: true);
        Give(map, SpikeIt.TheSwap("Luna"));
        StandInTheGallery(map);
        RoomFrame(map);
        RoomFrame(map);
        SheGoesToTheMachine(map);
        Pages.Map.TableTalk t = SitAt(map, SpikeIt.HerTable);
        RoomFrame(map);
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(t.Scene));

        await Press(map, SpikeIt.TakeThePages);
        await Press(map, SpikeIt.TakeThePages);
        Assert.Equal(SpikeIt.Pages.Taken, PassageOf(map).Pages);
        Assert.Single(Satchel_(map), i => SpikeIt.IsThePages(i.Id));
        Assert.Equal(SpikeIt.TakeThePagesLine, t.Outcome);
        int watches = SpikeIt.WatchesUntil(CarryThePress.StoryAt(PassageOf(map))!.Value, Now(map));
        Assert.Equal(12, watches);
        Assert.Equal(1, Filed(map, SpikeIt.TookThePages(watches)));
        Assert.Equal(SpikeIt.LeaveYourPage, SpikeIt.Offers(t.Scene));

        await Press(map, SpikeIt.LeaveYourPage);
        await Press(map, SpikeIt.LeaveYourPage);
        Assert.Equal(SpikeIt.Pages.Swapped, PassageOf(map).Pages);
        Assert.DoesNotContain(Satchel_(map), i => SpikeIt.IsThePages(i.Id));
        Assert.DoesNotContain(Satchel_(map), i => SpikeIt.IsTheSwap(i.Id));
        Assert.Equal(SpikeIt.LeaveYourPageLine, t.Outcome);
        RoomFrame(map);
        Assert.Null(SpikeIt.Offers(t.Scene));
    }

    // ── THE WINDOW ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXACTLY ONE OF THREE AT THE WINDOW, ON THE WIRE AND IN THE BOOK. SPIKED: nothing prints — not her story,
    /// not the client's, not the floor's reaction — and the hole is filed once under ⬚. ALTERED: the client's
    /// sentence prints under her byline, once, and her own story does not; the book says so; the floor reacts.
    /// LATE: her own story runs as slice 1 wrote it. The decision is remembered.
    ///
    /// <para><b>RED</b> by printing her story on SPIKED (the <c>continue</c> dropped), and by passing
    /// <c>CarryThePress.Story</c> for ALTERED (her story ran over the client's page).</para>
    /// </summary>
    [Theory]
    [InlineData(SpikeIt.Pages.Taken, SpikeIt.Outcome.Spiked)]
    [InlineData(SpikeIt.Pages.Swapped, SpikeIt.Outcome.Altered)]
    [InlineData(SpikeIt.Pages.OnHerTable, SpikeIt.Outcome.Late)]
    public void TheWindowPrintsExactlyOneOfThree(SpikeIt.Pages pages, SpikeIt.Outcome expected)
    {
        Pages.Map map = Clamped($"spike-window-{pages}");
        Plant(map, CarryThePress.StoryAfterSeconds + CarryThePress.FloorAfterStorySeconds + 1, spike: true, pages);
        string body = Body(map);
        Invoke(map, "ThePressRunsHerStory");
        Invoke(map, "FileNote", "a line between two advances", "✎");
        Invoke(map, "ThePressRunsHerStory");

        Assert.Equal(expected, PassageOf(map).Outcome);
        var stories = Wire(map).Where(e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled).ToList();
        var floors = Wire(map).Where(e => e.Kind == NewsWire.NewsEventKind.PressFloorReaction).ToList();
        switch (expected)
        {
            case SpikeIt.Outcome.Spiked:
                Assert.Empty(stories);
                Assert.Empty(floors);
                Assert.Single((IEnumerable<FieldNote>)Read(map, "_fieldNotes")!,
                    n => n.Text == SpikeIt.Spiked(body) && n.Glyph == MissingMiddle.Glyph);
                Assert.Equal(0, Filed(map, CarryThePress.StoryRanLine));
                break;
            case SpikeIt.Outcome.Altered:
                Assert.Equal(SpikeIt.Altered(body), Assert.Single(stories).Subject);
                Assert.Single(floors);
                Assert.Equal(1, Filed(map, SpikeIt.AlteredEntry));
                Assert.Equal(0, Filed(map, CarryThePress.StoryRanLine));
                break;
            default:
                Assert.Equal(CarryThePress.Story(body, withTheTin: true), Assert.Single(stories).Subject);
                Assert.Single(floors);
                Assert.Equal(1, Filed(map, CarryThePress.StoryRanLine));
                Assert.Equal(0, Filed(map, SpikeIt.AlteredEntry));
                break;
        }

        // …and the decision does not move when the pages do afterwards.
        Rewrite(map, PassageOf(map) with { Pages = SpikeIt.Pages.OnHerTable });
        Invoke(map, "ThePressRunsHerStory");
        Assert.Equal(expected, PassageOf(map).Outcome);
    }

    /// <summary>
    /// THE DESK PAYS ONCE AFTER THE WINDOW: the full purse on 💳 with no words when nothing ran, half when it ran
    /// different, and — when it ran as she wrote it — no credits and the one line where the money would be. A
    /// second desk open pays nothing more. Before the window, nothing.
    ///
    /// <para><b>RED</b> by not writing <c>Paid</c> in <c>TheSpikeIsSettled</c> (the purse paid on every open).</para>
    /// </summary>
    [Theory]
    [InlineData(SpikeIt.Pages.Taken)]
    [InlineData(SpikeIt.Pages.Swapped)]
    [InlineData(SpikeIt.Pages.OnHerTable)]
    public void TheDeskPaysOnceAfterTheWindow(SpikeIt.Pages pages)
    {
        Pages.Map map = Clamped($"spike-pays-{pages}");
        Plant(map, Day, spike: true, pages);
        int before = (int)Read(map, "_credits")!;
        Invoke(map, "TheSpikeIsSettled");
        Assert.Equal(before, (int)Read(map, "_credits")!);
        Assert.False(PassageOf(map).Paid);

        Rewrite(map, PassageOf(map) with { TurnedIn = Now(map) - CarryThePress.StoryAfterSeconds - 1 });
        Invoke(map, "TheSpikeIsSettled");
        int purse = SpikeIt.Purse(Hers(map).Reward);
        int paid = (int)Read(map, "_credits")! - before;
        SpikeIt.Outcome outcome = SpikeIt.AtTheWindow(pages);
        Assert.Equal(SpikeIt.Pays(outcome, purse), paid);
        Assert.Contains(outcome == SpikeIt.Outcome.Late ? "💳 " + SpikeIt.LateLine : $"💳 +{paid:N0} cr",
            Pulse(map), StringComparison.Ordinal);
        Assert.True(PassageOf(map).Paid);

        Invoke(map, "TheSpikeIsSettled");
        Assert.Equal(paid, (int)Read(map, "_credits")! - before);
    }

    /// <summary>
    /// AFTER A SPIKED WINDOW SHE IS NOT DRAWN, AND THE GALLERY SAYS THE RECORDER IS LEFT — once. Not after an
    /// ALTERED one, which has no such line. <b>RED</b> by dropping the <c>Gone: false</c> clause from
    /// <c>TheRecorderIsLeftOnTheTable</c> (said on every frame).</para>
    /// </summary>
    [Fact]
    public void AfterASpikedWindowTheRecorderIsLeftOnTheTable()
    {
        Pages.Map map = Clamped("spike-gone");
        Plant(map, CarryThePress.StoryAfterSeconds + 1, spike: true, SpikeIt.Pages.Taken);
        Invoke(map, "ThePressRunsHerStory");
        StandInTheGallery(map);
        RoomFrame(map);
        Assert.True(PassageOf(map).Gone);
        Assert.Contains(SpikeIt.GoneLine, Pulse(map), StringComparison.Ordinal);
        Assert.Null(Her(map));

        object slot = Read(map, "_pulse")!;
        Set(map, "_pulse", slot.GetType().GetField("Empty", Hidden)?.GetValue(null)
                           ?? slot.GetType().GetProperty("Empty", Hidden)!.GetValue(null));
        for (int i = 0; i < 10; i++)
        {
            RoomFrame(map);
        }

        Assert.DoesNotContain(SpikeIt.GoneLine, Pulse(map), StringComparison.Ordinal);
        Assert.Null(Her(map));
    }

    /// <summary>
    /// NO CONTRACT, NOTHING ANYWHERE: nobody at the far table, no row, and a gallery table's card never touched —
    /// the very same scene object, frame after frame, at both tables. (Byte-identical frames are the frame
    /// ledgers' job; they are green un-repinned.)
    /// </summary>
    [Fact]
    public void NoContractNothingAnywhere()
    {
        Pages.Map map = Clamped("spike-nothing");
        Assert.Null(Invoke(map, "SpikeOnOffer"));
        for (int table = 0; table < HavenInterior.GalleryTops(Port).Count; table++)
        {
            Pages.Map.TableTalk t = SitAt(map, table);
            Encounter.Scene before = t.Scene;
            for (int i = 0; i < 5; i++)
            {
                RoomFrame(map);
            }

            Assert.Same(before.Moves, t.Scene.Moves);
            Assert.Null(Her(map));
            Invoke(map, "CloseTable");
        }
    }

    // ── ACROSS A SAVE, AND THE DEV STARTS ───────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CONTRACT SURVIVES A VAULT ROUND-TRIP: the spike, where her pages are, and whether her line was said
    /// ride the quest record through the real serializer and come back as they went. <b>RED</b> by dropping the
    /// <c>"pages"</c> arm from <c>Passage.Read</c>.
    /// </summary>
    [Fact]
    public async Task TheSpikeSurvivesTheVault()
    {
        DeskBench b = await DeskBench.BootAsync("/map?dock=selene-gate&ashore=1&spike=1");
        var q = Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress);
        b.Call("RewritePassage", q, CarryThePress.Passage.Read(q.Pin) with { Pages = SpikeIt.Pages.Taken, Seen = true });
        CarryThePress.Passage before = CarryThePress.Passage.Read(
            Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress).Pin);

        var section = (QuestsSection)b.Call("BuildQuestsSection")!;
        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault { SavedSimTime = 1.0, Quests = section }));
        b.Call("ApplyObligationsAndQuests", back.Quests);

        CarryThePress.Passage after = CarryThePress.Passage.Read(
            Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress).Pin);
        Assert.Equal(before, after);
        Assert.True(after.Spike);
        Assert.Equal(SpikeIt.Pages.Taken, after.Pages);
    }

    /// <summary><c>?spike=1</c> · her story pending with a spike in hand, the client's page in the satchel, the
    /// captain in the gallery, and the DEV line naming the table.</summary>
    [Fact]
    public async Task ThePendingStartHasTheSpikeInHandAndTheCaptainInTheGallery()
    {
        DeskBench b = await DeskBench.BootAsync("/map?dock=selene-gate&ashore=1&spike=1");
        var q = Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress);
        CarryThePress.Passage p = CarryThePress.Passage.Read(q.Pin);
        Assert.Equal(Pages.Map.QuestState.TurnedIn, q.State);
        Assert.True(p.Spike);
        Assert.Equal(SpikeIt.Outcome.None, p.Outcome);
        Assert.False(CarryThePress.StoryIsDue(p, (double)b.Peek("SimTime")!));
        Assert.Contains((IEnumerable<Satchel.Item>)b.Peek("_satchel")!, i => SpikeIt.IsTheSwap(i.Id));
        Assert.True(HavenInterior.InTheGallery(Port, (double)b.Peek("_avatarX")!, (double)b.Peek("_avatarY")!));
        Assert.Contains("DEV ?spike=1", b.Pulse, StringComparison.Ordinal);
    }

    /// <summary><c>?spike=spiked</c> · the window passed spiked: nothing on the wire, the hole in the book under
    /// ⬚, and the pages in the satchel.</summary>
    [Fact]
    public async Task TheSpikedStartHasTheHoleInTheBook()
    {
        DeskBench b = await DeskBench.BootAsync("/map?dock=selene-gate&ashore=1&spike=spiked");
        var q = Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, x => x.Kind == Pages.Map.QuestKind.CarryThePress);
        Assert.Equal(SpikeIt.Outcome.Spiked, CarryThePress.Passage.Read(q.Pin).Outcome);
        Assert.DoesNotContain((List<NewsWire.NewsEvent>)b.Peek("_newsEvents")!, e => e.Kind == NewsWire.NewsEventKind.PressStoryFiled);
        Assert.Single((IEnumerable<FieldNote>)b.Peek("_fieldNotes")!, n => n.Glyph == MissingMiddle.Glyph && n.Text.EndsWith("the shape of the hole.", StringComparison.Ordinal));
        Assert.Contains((IEnumerable<Satchel.Item>)b.Peek("_satchel")!, i => SpikeIt.IsThePages(i.Id));
    }
}
