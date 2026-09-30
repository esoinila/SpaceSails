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
/// #1202 (owner ruling 2026-09-29 evening) · <b>RAUHA LIND HAS A SEAT OF HER OWN.</b> <i>"Own seat sounds like a
/// known regular."</i> On her watch (<see cref="CarryThePress.AtTheTable"/>, one in three per berth) she is at
/// one of the bar's numbered chairs with a <c>◈ RAUHA LIND</c> console and a figure plated <c>Lind</c>; on any
/// other watch, and while she is elsewhere, the chair is empty. Her card comes from her chair and never from
/// the stranger's table.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SheHasHerOwnSeatTests
{
    private const string Berth = "selene-gate";
    private static readonly string HerLabel = $"◈ {CarryThePress.Giver}";

    private static readonly string[] Bars =
    [
        "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "the-tilt", "the-deep", "red-eye",
    ];

    private static IEnumerable<DeckPlan.ConsoleSpot> Hers(DeckPlan plan) =>
        plan.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && c.Label == HerLabel);

    private static long HerWatch(string berth, bool hers)
    {
        for (long w = 0; w < 200; w++)
        {
            if (CarryThePress.AtTheTable(berth, w) == hers)
            {
                return w;
            }
        }

        throw new InvalidOperationException($"premise: {berth} has a watch where she is {(hers ? "in" : "out")}");
    }

    // ── THE ROOM ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE IS AT HER CHAIR ON HER WATCH AND NOWHERE ON ANY OTHER, at every bar over fifty watches: one console
    /// of hers exactly when <see cref="CarryThePress.AtTheTable"/> says so; her chair is one of the pool's, no
    /// present regular is in it, and it is never on the free list a regular walking in is allotted from; her
    /// figure is drawn at it (slot 11) and parked off-frame otherwise. And both answers occur.
    ///
    /// <para><b>RED</b> with the console line removed from the build: no console on any of her watches (this and
    /// the churn guard below both went red).</para>
    /// </summary>
    [Fact]
    public void SheIsAtHerOwnChairOnHerWatchAndAbsentOtherwise()
    {
        int present = 0, absent = 0;
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 50; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                DeckPlan plan = HavenInterior.DockedDeck(bar, null, t)!;
                var buffer = new DeckPlan.Droid[plan.DroidCount];
                plan.FillDroids(t, buffer);
                bool hers = CarryThePress.AtTheTable(bar, w);
                List<DeckPlan.ConsoleSpot> consoles = Hers(plan).ToList();
                if (!hers)
                {
                    absent++;
                    Assert.Empty(consoles);
                    Assert.Null(HavenInterior.TheStringersSeat(bar, t));
                    Assert.True(buffer[11].X < -9000, $"{bar} watch {w}: her figure is drawn on a watch not hers");
                    continue;
                }

                present++;
                DeckPlan.ConsoleSpot c = Assert.Single(consoles);
                int chair = Enumerable.Range(0, HavenInterior.PatronSeatCount)
                    .Single(i => HavenInterior.PatronSeatAt(i) is { } p
                                 && Math.Abs(p.X - c.X) < 1e-3 && Math.Abs(p.Y - c.Y) < 1e-3);
                foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(bar, t).Where(r => r.Present))
                {
                    Assert.False(Math.Abs(r.X - c.X) < 1e-3 && Math.Abs(r.Y - c.Y) < 1e-3,
                        $"{bar} watch {w}: {r.ShortName} is in her chair");
                }

                Assert.DoesNotContain(chair, HavenInterior.FreePatronSeats(bar, t));
                Assert.Equal(CarryThePress.Plate, buffer[11].Name);
                Assert.True(Math.Abs(buffer[11].X - c.X) < 1 && Math.Abs(buffer[11].Y - c.Y) < 1,
                    $"{bar} watch {w}: her figure is not at her chair");
            }
        }

        Assert.True(present > 20 && absent > 20, $"premise: both answers occur ({present} in, {absent} out)");
    }

    /// <summary>
    /// SHE IS NEVER IN TWO ROOMS. With the churn naming her (the page's way of saying she is aboard or at her
    /// pages), her watch has no console of hers and her figure is parked.
    /// </summary>
    [Fact]
    public void WhenTheChurnSaysSheIsElsewhereHerChairIsEmpty()
    {
        long w = HerWatch(Berth, hers: true);
        double t = (w * PatronRota.WatchSeconds) + 1;
        var away = new HavenInterior.RoomChurn(
            new HashSet<string>(StringComparer.Ordinal) { CarryThePress.Giver }, new Dictionary<string, int>());
        DeckPlan plan = HavenInterior.DockedDeck(Berth, null, t, churn: away)!;
        Assert.Empty(Hers(plan));
        var buffer = new DeckPlan.Droid[plan.DroidCount];
        plan.FillDroids(t, buffer);
        Assert.True(buffer[11].X < -9000);
        Assert.Single(Hers(HavenInterior.DockedDeck(Berth, null, t)!));
    }

    // ── THE PAGE ────────────────────────────────────────────────────────────────────────────────────────

    private static async Task<DeskBench> DockedOn(long watch)
    {
        DeskBench b = await DeskBench.BootAsync($"/map?dock={Berth}&simhours={(watch * 4) + 1}");
        Assert.Equal(watch, PatronRota.WatchIndex((double)b.Peek("_dockVisitSimTime")!));
        return b;
    }

    private static List<Map.Quest> Quests(DeskBench b) => (List<Map.Quest>)b.Peek("_quests")!;

    private static DeckPlan Deck(DeskBench b) => (DeckPlan)b.Peek("_deckPlan")!;

    private static void StandAt(DeskBench b, DeckPlan.ConsoleSpot c)
    {
        b.Poke("_avatarX", (double)c.X);
        b.Poke("_avatarY", (double)c.Y);
    }

    /// <summary>
    /// THE OFFER COMES FROM HER CHAIR. Docked on her watch: [E] at her console slides her card across — the card's
    /// words as they were (<see cref="CarryThePress.Offer"/>) — and taking it empties her chair on the page's own
    /// deck. On a watch not hers: no console of hers on the page's deck.
    ///
    /// <para><b>RED</b> with her console line removed from the build (no chair of hers to walk up to), and with
    /// <c>TheStringerIsElsewhere</c> kept out of the churn: her console was still in the bar after she stowed her
    /// bag.</para>
    /// </summary>
    [Fact]
    public async Task TheOfferComesFromHerChairAndTakingItEmptiesIt()
    {
        DeskBench b = await DockedOn(HerWatch(Berth, hers: true));
        DeckPlan.ConsoleSpot chair = Assert.Single(Hers(Deck(b)));
        StandAt(b, chair);
        b.CallOnTheDispatcher("TalkToStranger");

        var offer = (Map.Quest?)b.Peek("_pendingOffer");
        Assert.NotNull(offer);
        Assert.Equal(Map.QuestKind.CarryThePress, offer!.Kind);
        Assert.Equal(CarryThePress.Giver, offer.Giver);
        Assert.Equal(CarryThePress.Offer(offer.TargetCallsign), offer.Blurb);

        // Taken: she is aboard, and her chair is empty.
        b.Poke("_pendingOffer", null);
        Quests(b).Add(offer);
        b.CallOnTheDispatcher("SheStowsOneBag");
        Assert.Empty(Hers(Deck(b)));

        DeskBench other = await DockedOn(HerWatch(Berth, hers: false));
        Assert.Empty(Hers(Deck(other)));
    }

    /// <summary>
    /// THE STRANGER NO LONGER OFFERS IT. On her watch, every other face at the bar — the regulars, and whoever
    /// the generic arm answers for — is asked for their standing offer: never her card.
    ///
    /// <para><b>RED</b> by restoring <c>MakePressOffer() ?? MakeHuntOffer(giver)</c> in the default arm: One-Eye
    /// Silas slid her card across.</para>
    /// </summary>
    [Fact]
    public async Task TheStrangerNoLongerOffersHerPassage()
    {
        DeskBench b = await DockedOn(HerWatch(Berth, hers: true));
        foreach (string giver in PatronRota.Roster.Append("A STRANGER"))
        {
            var offer = (Map.Quest?)b.Call("MakeContactOffer", giver);
            Assert.False(offer is { Kind: Map.QuestKind.CarryThePress }, $"{giver} offered her passage");
        }

        var hers = (Map.Quest?)b.Call("MakeContactOffer", CarryThePress.Giver);
        Assert.Equal(Map.QuestKind.CarryThePress, hers?.Kind);
    }
}
