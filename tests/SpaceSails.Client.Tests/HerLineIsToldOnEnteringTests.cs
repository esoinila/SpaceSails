using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 2 QA · <b>HER LINE IS DECIDED ON ENTERING THE GALLERY.</b> The 2026-09-29 QA pass heard
/// <i>"She is at the far table with the recorder and a stack of pages…"</i> a minute after the captain came into
/// the gallery and seconds after TAKE THE PAGES — told the moment she sat back down, to a captain who had watched
/// her feed the machine the whole time. The rule (Fable): it is told, once per contract, only when she is at her
/// table the moment he enters; a visit that finds her away says nothing, and a later entry with her seated may tell
/// it; and it is never told after her pages have been taken. Driven on the booted page at
/// <c>/map?dock=selene-gate&amp;ashore=1&amp;spike=1</c>, her away staged on her own clock
/// (<see cref="Pages.Map.Walker.PassHeld"/>) as slice 2 does. Both guards were watched go red on the slice-2 check
/// (<c>!p.Seen &amp;&amp; !SheIsAway(her) &amp;&amp; InTheGallery</c>, every frame).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class HerLineIsToldOnEnteringTests
{
    private const string Port = "selene-gate";
    private const string Start = "/map?dock=selene-gate&ashore=1&spike=1";
    private const double Dt = 0.1;

    // ── the bench ───────────────────────────────────────────────────────────────────────────────────────

    private static void Frame(DeskBench b, int frames = 1)
    {
        for (int i = 0; i < frames; i++)
        {
            b.Poke("_lastTimestampMs", (double?)(((double?)b.Peek("_lastTimestampMs") ?? 0) + (Dt * 1000)));
            b.CallOnTheDispatcher("AdvanceBarWalkers", Dt);
        }
    }

    private static void StandAt(DeskBench b, double x, double y)
    {
        b.Poke("_avatarX", x);
        b.Poke("_avatarY", y);
        b.Poke("_lookPrevAvatarX", x);
        b.Poke("_lookPrevAvatarY", y);
    }

    private static bool InTheGallery(DeskBench b) =>
        HavenInterior.InTheGallery(Port, (double)b.Peek("_avatarX")!, (double)b.Peek("_avatarY")!);

    /// <summary>Out at the bar's threshold, and one frame there.</summary>
    private static void OutOfTheGallery(DeskBench b)
    {
        (double tx, double ty, _) = HavenInterior.BarThreshold;
        StandAt(b, tx, ty - 4);
        Frame(b);
        Assert.False(InTheGallery(b), "premise: the bar's threshold is not the gallery.");
    }

    /// <summary>Into the gallery at the vendor island, and one frame there.</summary>
    private static void IntoTheGallery(DeskBench b)
    {
        DeckReachability.Point island = HavenInterior.TheVendorsAt(Port)[^1];
        StandAt(b, island.X, island.Y);
        Frame(b);
        Assert.True(InTheGallery(b), "premise: the vendor island is in the gallery.");
    }

    private static Pages.Map.Walker Her(DeskBench b) =>
        ((List<Pages.Map.Walker>)b.Peek("_barAfoot")!).SingleOrDefault(w => w.For == Pages.Map.Errand.AtHerPages)
        ?? throw new InvalidOperationException("premise: she is on the gallery floor");

    private static bool SheIsSeated(DeskBench b) =>
        Her(b) is { Table: SpikeIt.HerTable } w && w.Walk.State == NpcWalk.Doing.Arrived;

    private static bool SheIsAtTheMachine(DeskBench b) => Her(b) is { Table: -1 } w && !w.Walk.Afoot;

    /// <summary>Her own clock past her writing spell, and frames until she stands at the machine.</summary>
    private static void SheGoesToTheMachine(DeskBench b)
    {
        Her(b).PassHeld = SpikeIt.WritesAtMostSeconds + 1;
        for (int i = 0; i < 600 && !SheIsAtTheMachine(b); i++)
        {
            Frame(b);
        }

        Assert.True(SheIsAtTheMachine(b), "premise: she went to the machine.");
    }

    /// <summary>Her own clock past her feeding spell, and frames until she is back in her chair.</summary>
    private static void SheComesBack(DeskBench b)
    {
        Her(b).PassHeld = SpikeIt.FeedsAtMostSeconds + 1;
        for (int i = 0; i < 600 && !SheIsSeated(b); i++)
        {
            Frame(b);
        }

        Assert.True(SheIsSeated(b), "premise: she came back to her table.");
    }

    private static Pages.Map.Quest Hers(DeskBench b) =>
        Assert.Single((List<Pages.Map.Quest>)b.Peek("_quests")!, q => q.Kind == Pages.Map.QuestKind.CarryThePress);

    private static CarryThePress.Passage PassageOf(DeskBench b) => CarryThePress.Passage.Read(Hers(b).Pin);

    private static bool Told(DeskBench b) => b.Pulse.Contains(SpikeIt.AtHerTableLine, StringComparison.Ordinal);

    /// <summary>The pulse slot emptied, so a later tell is a new one.</summary>
    private static void Hush(DeskBench b)
    {
        b.Poke("_pulse", PulseSlot.Empty);
        Assert.False(Told(b));
    }

    /// <summary>The start, with the captain taken out of the gallery before her first frame, and her in her chair.</summary>
    private static async Task<DeskBench> OutsideWithHerWriting()
    {
        DeskBench b = await DeskBench.BootAsync(Start);
        await b.RenderAsync();
        OutOfTheGallery(b);
        Frame(b, 5);
        Assert.True(SheIsSeated(b), "premise: she is at her table.");
        Assert.Equal(SpikeIt.HerLine.NotYet, PassageOf(b).Seen);
        Assert.False(Told(b));
        return b;
    }

    // ── THE GUARDS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A VISIT THAT FINDS HER AWAY SAYS NOTHING; A LATER ENTRY WITH HER SEATED TELLS IT, ONCE. He comes in while
    /// she stands at the machine: nothing, and nothing when she sits back down in front of him. He goes out and
    /// comes back in with her writing: the line, verbatim. Out and in again: nothing more.
    ///
    /// <para><b>RED</b> on the slice-2 check (the line was told the frame she sat back down, with him in the
    /// gallery the whole visit — the QA report).</para>
    /// </summary>
    [Fact]
    public async Task AVisitThatFindsHerAwaySaysNothingAndALaterSeatedEntryTellsItOnce()
    {
        DeskBench b = await OutsideWithHerWriting();

        SheGoesToTheMachine(b);
        IntoTheGallery(b);
        Assert.False(Told(b));
        Assert.Equal(SpikeIt.HerLine.NotThisVisit, PassageOf(b).Seen);

        SheComesBack(b);
        Frame(b, 20);
        Assert.False(Told(b), "her line was told when she sat back down, on a visit that found her away.");

        OutOfTheGallery(b);
        Assert.Equal(SpikeIt.HerLine.NotYet, PassageOf(b).Seen);
        IntoTheGallery(b);
        Assert.True(Told(b), "her line was not told on an entry that found her at her table.");
        Assert.Equal(SpikeIt.HerLine.Told, PassageOf(b).Seen);

        Hush(b);
        Frame(b, 20);
        OutOfTheGallery(b);
        IntoTheGallery(b);
        Frame(b, 20);
        Assert.False(Told(b), "her line was told twice.");
    }

    /// <summary>
    /// THE START STILL TELLS IT: at <c>?spike=1</c> the captain is stood in the gallery and she is put in her chair
    /// on the first frame — an entry that finds her at her table, so the line comes, once. (Deciding on her walk's
    /// settled state instead of her body in the chair spent that visit on the frame she is put there, before her
    /// one-step walk has run; this bench's boot has already stepped her, and it was
    /// <c>TheStoryCanBeSpikedTests.SheWritesAtTheFarTableAndFeedsTheMachineOnHerOwnClock</c> that went red.)
    /// </summary>
    [Fact]
    public async Task TheStartStillTellsItWithHerWriting()
    {
        DeskBench b = await DeskBench.BootAsync(Start);
        await b.RenderAsync();
        Assert.True(InTheGallery(b));
        Frame(b, 5);
        Assert.True(Told(b), "the start's first visit, with her writing, did not tell her line.");
        Assert.Equal(SpikeIt.HerLine.Told, PassageOf(b).Seen);
    }

    /// <summary>
    /// AFTER A TAKE IT IS NEVER TOLD. He comes in while she is at the machine, sits at her table, takes her pages,
    /// and gets up; she comes back; he goes out and comes back in with her writing at an empty table: nothing, on
    /// that visit or the one before.
    ///
    /// <para><b>RED</b> on the slice-2 check (the line was told seconds after TAKE THE PAGES, the frame she sat
    /// back down).</para>
    /// </summary>
    [Fact]
    public async Task AfterATakeHerLineIsNeverTold()
    {
        DeskBench b = await OutsideWithHerWriting();

        SheGoesToTheMachine(b);
        DeckReachability.Point top = HavenInterior.GalleryTops(Port)[SpikeIt.HerTable];
        StandAt(b, top.X, top.Y);
        Frame(b);
        Assert.True(InTheGallery(b));
        Assert.True((bool)b.CallOnTheDispatcher("TryTakeBarTop")!, "her table refused [E].");
        Frame(b);
        var t = (Pages.Map.TableTalk)b.Call("get_SeatedTable")!;
        Assert.Equal(SpikeIt.TakeThePages, SpikeIt.Offers(t.Scene));
        await (Task)b.CallOnTheDispatcher("TableMoveClicked", SpikeIt.TakeThePages)!;
        Assert.Equal(SpikeIt.Pages.Taken, PassageOf(b).Pages);
        b.CallOnTheDispatcher("CloseTable");

        SheComesBack(b);
        Frame(b, 20);
        Assert.False(Told(b), "her line was told after her pages were taken.");

        OutOfTheGallery(b);
        IntoTheGallery(b);
        Frame(b, 20);
        Assert.False(Told(b), "her line was told on a later entry, after her pages were taken.");
        Assert.NotEqual(SpikeIt.HerLine.Told, PassageOf(b).Seen);
    }
}
