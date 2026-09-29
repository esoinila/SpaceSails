using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using Map = SpaceSails.Client.Pages.Map;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 slice 4, part 2 · <b>THE STRINGER IS AWAY, AND COMES BACK WITH ONE LINE.</b> After a SPIKED window Rauha
/// Lind's own bar seat (#1335) stands empty for a seeded three to six watches — no console, no figure, nothing
/// greyed, nothing said; the regular sitting nearest the empty chair, asked, gives #1074 beat 4's own sentence,
/// once; and the first time the captain walks up to her chair after the absence she says her one line before
/// any card. Driven on the shipping page (<see cref="DeskBench"/>) docked at Selene Gate on her own watch. Every
/// guard was watched go red on the revert its summary names.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class HerSeatStandsEmptyTests
{
    private const string Berth = "selene-gate";
    private static readonly string HerLabel = $"◈ {CarryThePress.Giver}";

    // ── the bench ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A watch that is hers at this bar, with her chair free and at least two regulars sitting, at or
    /// after <paramref name="from"/>.</summary>
    private static long HerWatch(long from = 0)
    {
        for (long w = from; w < from + 400; w++)
        {
            double t = (w * PatronRota.WatchSeconds) + 1;
            if (CarryThePress.AtTheTable(Berth, w) && HavenInterior.TheStringersChairAt(Berth, t) is not null
                && HavenInterior.ResolveRegulars(Berth, t).Count(r => r.State == PatronState.AtBar) >= 2)
            {
                return w;
            }
        }

        throw new InvalidOperationException("premise: Selene Gate has a watch of hers with two regulars in");
    }

    private static async Task<DeskBench> DockedOn(long watch)
    {
        DeskBench b = await DeskBench.BootAsync($"/map?dock={Berth}&simhours={(watch * 4) + 1}");
        Assert.Equal(watch, PatronRota.WatchIndex((double)b.Peek("_dockVisitSimTime")!));
        return b;
    }

    private static double Now(DeskBench b) => (double)b.Field("SimTime")!;

    private static List<Map.Quest> Quests(DeskBench b) => (List<Map.Quest>)b.Peek("_quests")!;

    private static Map.Quest Hers(DeskBench b) => Assert.Single(Quests(b), q => q.Kind == Map.QuestKind.CarryThePress);

    private static CarryThePress.Passage PassageOf(DeskBench b) => CarryThePress.Passage.Read(Hers(b).Pin);

    private static void Rewrite(DeskBench b, CarryThePress.Passage p) => b.Call("RewritePassage", Hers(b), p);

    /// <summary>Her contract, paid <paramref name="ago"/> seconds back with a spike against it, then the window
    /// run — the record play would have left.</summary>
    private static void Plant(DeskBench b, double ago, SpikeIt.Pages pages)
    {
        string site = FieldNotes.PlaceLabel("Luna", LandingSites.At("luna", 0).Name);
        var offer = new Map.Quest("press-seat", Map.QuestKind.CarryThePress, CarryThePress.Giver, "",
            site, CarryThePress.CardTitle, CarryThePress.Offer(site), 1250, DestBodyId: "luna", SourceBodyId: Berth,
            Pin: new CarryThePress.Passage(0).Write());
        Quests(b).Add(offer);
        var home = (Map.Quest)b.Call("RewritePassage", offer, CarryThePress.Passage.Read(offer.Pin) with
        {
            Landed = true, Walked = true, Tin = true, TurnedIn = Now(b) - ago, Spike = true, Pages = pages,
        })!;
        b.Call("AdvanceMission", home, Map.QuestState.TurnedIn, null);
        b.Call("ThePressRunsHerStory");
    }

    /// <summary>The room as the page would weld it on this watch.</summary>
    private static DeckPlan OnWatch(DeskBench b, long watch)
    {
        b.Poke("_dockVisitSimTime", (watch * PatronRota.WatchSeconds) + 1);
        b.Call("RebuildDockedDeck");
        return (DeckPlan)b.Peek("_deckPlan")!;
    }

    private static bool HerChairIsTaken(DeckPlan plan) =>
        plan.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label == HerLabel);

    private static bool Elsewhere(DeskBench b) => (bool)b.Call("TheStringerIsElsewhere")!;

    private static void ClearThePulse(DeskBench b)
    {
        object slot = b.Peek("_pulse")!;
        b.Poke("_pulse", slot.GetType().GetField("Empty", CastawayBench.Hidden)?.GetValue(null)
                         ?? slot.GetType().GetProperty("Empty", CastawayBench.Hidden)!.GetValue(null));
    }

    private static void PressAt(DeskBench b, DeckPlan.ConsoleSpot c)
    {
        b.Poke("_avatarX", (double)c.X);
        b.Poke("_avatarY", (double)c.Y);
        b.CallOnTheDispatcher("TalkToStranger");
    }

    // ── HER ABSENCE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HER SEAT IS EMPTY FOR EXACTLY THE SEEDED WATCHES, AND SHE IS BACK AFTER. A SPIKED window this watch writes
    /// the watch she is back (the window's watch plus her contract's seeded span); on every watch before it she
    /// is elsewhere and, on the watches that are hers, the room has no console of hers; from it on she is not
    /// elsewhere, and on her next watch after it her chair is taken again. <b>RED</b> by dropping
    /// <c>TheSpikeKeepsHerAway</c> from <c>TheStringerIsElsewhere</c> (her console at the bar the watch of the
    /// window), and by <c>IsAway</c> reading <c>watch &lt;= b</c> (away one watch too many).
    /// </summary>
    [Fact]
    public async Task HerSeatIsEmptyForExactlyTheSeededWatches()
    {
        long w0 = HerWatch();
        DeskBench b = await DockedOn(w0);
        Plant(b, CarryThePress.StoryAfterSeconds + 60, SpikeIt.Pages.Taken);
        CarryThePress.Passage p = PassageOf(b);
        Assert.Equal(SpikeIt.Outcome.Spiked, p.Outcome);
        Assert.Equal(w0, PatronRota.WatchIndex(CarryThePress.StoryAt(p)!.Value));
        long back = Assert.NotNull(p.Back);
        Assert.Equal(w0 + SpikeIt.AwayFor(Hers(b).Id), back);
        Assert.InRange(back - w0, SpikeIt.AwayAtLeastWatches, SpikeIt.AwayAtMostWatches);

        int herWatchesAway = 0;
        long? herNextWatch = null;
        for (long u = w0; u < back + 12; u++)
        {
            DeckPlan plan = OnWatch(b, u);
            Assert.Equal(u < back, Elsewhere(b));
            if (!CarryThePress.AtTheTable(Berth, u) || HavenInterior.TheStringersChairAt(Berth, (u * PatronRota.WatchSeconds) + 1) is null)
            {
                continue;
            }

            if (u < back)
            {
                herWatchesAway++;
                Assert.False(HerChairIsTaken(plan), $"watch {u}: her console is at the bar while she is away");
            }
            else
            {
                herNextWatch ??= u;
                Assert.True(HerChairIsTaken(plan), $"watch {u}: she is back and her chair is empty");
            }
        }

        Assert.True(herWatchesAway >= 1, "premise: the absence covers one of her watches");
        Assert.NotNull(herNextWatch);
    }

    /// <summary>
    /// ONLY A SPIKED WINDOW EMPTIES HER SEAT: after ALTERED or LATE no watch is written and she is at her chair on
    /// her watch; nothing about the absence is on her line. <b>RED</b> by writing <c>Back</c> for every decided
    /// window.
    /// </summary>
    [Theory]
    [InlineData(SpikeIt.Pages.Swapped)]
    [InlineData(SpikeIt.Pages.OnHerTable)]
    public async Task AStoryThatRanLeavesHerSeatAsItWas(SpikeIt.Pages pages)
    {
        long w0 = HerWatch();
        DeskBench b = await DockedOn(w0);
        Plant(b, CarryThePress.StoryAfterSeconds + 60, pages);
        Assert.NotEqual(SpikeIt.Outcome.Spiked, PassageOf(b).Outcome);
        Assert.Null(PassageOf(b).Back);
        Assert.DoesNotContain("back=", Hers(b).Pin, StringComparison.Ordinal);
        Assert.False(Elsewhere(b));
        Assert.True(HerChairIsTaken(OnWatch(b, w0)));
    }

    // ── THE REGULAR, ASKED ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ASKED ABOUT HER EMPTY SEAT, THE REGULAR BESIDE IT GIVES BEAT 4's SENTENCE — ONCE. On a watch of hers while
    /// she is away, [E] at the regular sitting nearest her chair says <see cref="CareerCost.ColleagueLine"/>,
    /// verbatim, and nothing else; the contract remembers it was asked. Any other regular is their ordinary self
    /// and says nothing of it; the same regular a second time is their ordinary table. <b>RED</b> by dropping the
    /// <c>Asked</c> clause (said on every press), and by dropping the <c>WhoIsAsked</c> comparison (every regular
    /// said it).
    /// </summary>
    [Fact]
    public async Task TheRegularBesideHerEmptySeatAnswersOnce()
    {
        long w0 = HerWatch();
        DeskBench b = await DockedOn(w0);
        Plant(b, CarryThePress.StoryAfterSeconds + 60, SpikeIt.Pages.Taken);
        DeckPlan plan = OnWatch(b, w0);
        Assert.False(HerChairIsTaken(plan));

        double t = (w0 * PatronRota.WatchSeconds) + 1;
        (double cx, double cy) = HavenInterior.TheStringersChairAt(Berth, t)!.Value;
        var seated = HavenInterior.ResolveRegulars(Berth, t).Where(r => r.State == PatronState.AtBar).ToList();
        HavenInterior.SeatedRegular near = seated
            .OrderBy(r => ((r.X - cx) * (r.X - cx)) + ((r.Y - cy) * (r.Y - cy)))
            .ThenBy(r => r.Label, StringComparer.Ordinal).First();
        HavenInterior.SeatedRegular other = seated.First(r => r.Label != near.Label);
        DeckPlan.ConsoleSpot Console(HavenInterior.SeatedRegular r) =>
            plan.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label == r.Label);

        ClearThePulse(b);
        PressAt(b, Console(other));
        Assert.DoesNotContain(CareerCost.ColleagueLine, b.Pulse, StringComparison.Ordinal);
        Assert.False(PassageOf(b).Asked);
        b.Call("ClosePatronTable");
        b.Poke("_pendingOffer", null);

        ClearThePulse(b);
        PressAt(b, Console(near));
        Assert.Contains($"Message = {CareerCost.ColleagueLine},", b.Pulse, StringComparison.Ordinal);
        Assert.Null(b.Peek("_pendingOffer"));
        Assert.True(PassageOf(b).Asked);

        ClearThePulse(b);
        PressAt(b, Console(near));
        Assert.DoesNotContain(CareerCost.ColleagueLine, b.Pulse, StringComparison.Ordinal);
    }

    // ── HER RETURN ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HER RETURN IS ONE LINE, ONCE, AT HER CHAIR AFTER THE ABSENCE. A SPIKED window ten watches gone: her chair is
    /// taken on her watch; [E] there says <see cref="SpikeIt.ReturnLine"/> and slides no card; the next [E] is
    /// her card as ever, with no second telling. <b>RED</b> by dropping the <c>HerReturnIsTold</c> call from
    /// <c>TalkToStranger</c> (her card at once), and by never writing <c>Home</c> (told on every press).
    /// </summary>
    [Fact]
    public async Task HerReturnIsOneLineOnceAtHerChair()
    {
        long w = HerWatch(20);
        DeskBench b = await DockedOn(w);
        Plant(b, CarryThePress.StoryAfterSeconds + (10 * PatronRota.WatchSeconds), SpikeIt.Pages.Taken);
        Assert.True(PassageOf(b).Back <= w);
        DeckPlan plan = OnWatch(b, w);
        DeckPlan.ConsoleSpot chair = plan.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label == HerLabel);

        ClearThePulse(b);
        PressAt(b, chair);
        Assert.Contains($"Message = {SpikeIt.ReturnLine},", b.Pulse, StringComparison.Ordinal);
        Assert.Null(b.Peek("_pendingOffer"));
        Assert.True(PassageOf(b).Home);

        ClearThePulse(b);
        PressAt(b, chair);
        Assert.DoesNotContain(SpikeIt.ReturnLine, b.Pulse, StringComparison.Ordinal);
        Assert.Equal(Map.QuestKind.CarryThePress, ((Map.Quest?)b.Peek("_pendingOffer"))?.Kind);
    }

    /// <summary>
    /// NO ABSENCE, NO RETURN LINE: after a LATE window her chair is hers on her watch and [E] slides her card at
    /// once — nothing said. <b>RED</b> by dropping the outcome clause from <c>HerReturnIsDue</c> with a watch
    /// written for every window.
    /// </summary>
    [Fact]
    public async Task NoAbsenceNoReturnLine()
    {
        long w = HerWatch(20);
        DeskBench b = await DockedOn(w);
        Plant(b, CarryThePress.StoryAfterSeconds + (10 * PatronRota.WatchSeconds), SpikeIt.Pages.OnHerTable);
        DeckPlan plan = OnWatch(b, w);
        DeckPlan.ConsoleSpot chair = plan.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label == HerLabel);

        ClearThePulse(b);
        PressAt(b, chair);
        Assert.DoesNotContain(SpikeIt.ReturnLine, b.Pulse, StringComparison.Ordinal);
        Assert.Equal(Map.QuestKind.CarryThePress, ((Map.Quest?)b.Peek("_pendingOffer"))?.Kind);
        Assert.False(PassageOf(b).Home);
    }
}
