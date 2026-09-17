using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1213 · <b>?simhours=N BUILDS THE WORLD OF A CAPTAIN WHO REALLY DOCKED AT THAT HOUR.</b>
///
/// <para><b>The law.</b> <c>?dock=X&amp;simhours=N</c> must hand over the world a captain reaches by tying up
/// at X when the station clock reads N hours: the same frozen watch, the same seated rota, the same schedule
/// of who finishes and goes. A cheat that produced any other world would be the sim doing one thing while
/// the URL that summoned it claims another — this repository's third named bug class, wearing a bar.</para>
///
/// <para><b>What it was doing.</b> The jump was the last line of <c>SeedTheApproachesAndThePurse</c>, four
/// stages after <c>ApplyTheStartPoint</c> — and <c>?dock=</c> clamps on in <c>ApplyTheStartPoint</c>, where
/// <c>SetDeckForDock</c> freezes <c>_dockVisitSimTime</c>, the one clock the whole bar is resolved at (#410).
/// So <c>_dockVisitSimTime</c> stayed 0 whatever <c>simhours</c> said: <c>BarWatch</c> was watch 0 and
/// <c>IntoTheBarsWatch</c> was the whole of N, past every departure <see cref="Egress"/> schedules inside the
/// first <see cref="Egress.LastCallFraction"/> of a watch. The room emptied its whole evening of leavers on
/// frame one, and #1199's observation walk was then dealt to a chair its own person had just been walked out
/// of — the unchurned rota still saying he was there, the room saying he was not, the beat spent in silence
/// on nobody. Played headless at the documented link, GILT-EYE was out of his chair 1.5 s after boot and
/// 105 s of warp produced no walker, no card and no note.</para>
///
/// <para><b>Where this is proved.</b> Past the browser gate (<see cref="PastTheGateBench"/>), because both the
/// clamp and the clock live behind it and <see cref="TheBootBuildsTheSameWorldTests"/>'s fingerprint horizon
/// is four stages in front.</para>
/// </summary>
public sealed class TheClockMovesBeforeSheDocksTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The berth with the observation walk in it, and the person the rota puts at its counter —
    /// both off Core's own answer, never a name typed here.</summary>
    private static readonly string Berth = ObservationWalk.HavenId;
    private static readonly string Person = TheTail.ThePersonOfInterest(Berth);

    /// <summary>#1213 · The hour <c>docs/testing-links-2026-09-17.md</c> §1 now sends a tester to, and the
    /// reason it moved off 4. Four sim-hours is one whole watch — with the clock fixed, <c>?simhours=4</c>
    /// lands a captain at the very START of watch 1, where nothing is past last call and the walk is
    /// correctly refused. 7.5 is the first hour that is BOTH past
    /// <see cref="Egress.LastCallFraction"/> of its watch and on a watch the rota seats the person of
    /// interest on and the schedule does not walk him out of.</summary>
    private const double TheDocumentedHour = 7.5;

    private static string Url(double simHours, bool ashore = true) =>
        $"/map?dock={Berth}{(ashore ? "&ashore=1" : "")}&simhours="
        + simHours.ToString(CultureInfo.InvariantCulture);

    // ── THE LAW ──────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.5)]
    [InlineData(4.0)]
    [InlineData(TheDocumentedHour)]
    [InlineData(19.5)]
    public async Task TheBerthFreezesTheWatchTheCLOCKSaysAndNotWatchZero(double simHours)
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(Url(simHours));

        Assert.Null(booted.Threw);
        Assert.Equal(simHours * 3600, booted.Read<double>("SimTime"), 6);
        Assert.Equal(simHours * 3600, booted.Read<double>("_dockVisitSimTime"), 6);
    }

    [Fact]
    public async Task TheCheatsRoomIsTheROOMOfThatHour()
    {
        // The law said the only way it can honestly be said: the world the cheat builds is compared against
        // the world reached the other way round — the clock pushed forward on a page that is ALREADY docked,
        // and the berth then taken again, which is a captain arriving at that hour. Same seats, same faces,
        // same evening's schedule.
        PastTheGateBench.Boot cheated = await PastTheGateBench.BootAsync(Url(TheDocumentedHour));
        Assert.Null(cheated.Threw);

        PastTheGateBench.Boot arrived = await PastTheGateBench.BootAsync(Url(TheDocumentedHour, ashore: false));
        Assert.Null(arrived.Threw);
        SetClock(arrived.Page, 0);                       // …undo the jump and tie up at the epoch instead
        Dock(arrived.Page);
        Assert.Equal(0, arrived.Read<double>("_dockVisitSimTime"), 6);   // a different evening, on purpose
        SetClock(arrived.Page, TheDocumentedHour * 3600);
        Dock(arrived.Page);                              // …and NOW arrive, at the hour the cheat claims

        Assert.Equal(
            arrived.Read<double>("_dockVisitSimTime"), cheated.Read<double>("_dockVisitSimTime"), 6);
        Assert.Equal(TheRoomOf(arrived), TheRoomOf(cheated));
        Assert.Equal(TheEveningsSchedule(arrived), TheEveningsSchedule(cheated));
    }

    [Fact]
    public async Task ADifferentHourIsADifferentEvening()
    {
        // …and the anti-vacuous half of the comparison above: if every hour produced one room, the equality
        // would be a tautology and the whole guard would be about nothing.
        PastTheGateBench.Boot one = await PastTheGateBench.BootAsync(Url(TheDocumentedHour));
        PastTheGateBench.Boot another = await PastTheGateBench.BootAsync(Url(TheDocumentedHour + 4));

        Assert.NotEqual(TheRoomOf(one), TheRoomOf(another));
    }

    // ── THE BEAT THE LINK EXISTS FOR ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheDocumentedLinkMeetsEveryPreconditionOfTheWalk()
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(Url(TheDocumentedHour));
        Assert.Null(booted.Threw);

        double frozen = booted.Read<double>("_dockVisitSimTime");
        double into = booted.Read<double>("SimTime") - (PatronRota.WatchIndex(frozen) * PatronRota.WatchSeconds);

        // He is in the room this watch…
        Assert.Equal(PatronState.AtBar, PatronRota.Resolve(Person, Berth, frozen));

        // …the evening is past last call, which is the only point at which the walk may be dealt (#731's
        // hours bind the tail too)…
        Assert.True(into > PatronRota.WatchSeconds * Egress.LastCallFraction,
            $"{into:F0} s into the watch is not past last call at "
            + $"{PatronRota.WatchSeconds * Egress.LastCallFraction:F0} s.");

        // …and there is more watch left than the wait the beat costs, so the room does not turn over under a
        // captain who is standing at the mouth of the tube counting.
        Assert.True(PatronRota.WatchSeconds - into >= ObservationWalk.TheWaitSeconds,
            $"only {PatronRota.WatchSeconds - into:F0} s of watch left for a {ObservationWalk.TheWaitSeconds:F0} s wait.");

        // …and the room's own schedule has not already walked him out of it, which is what made the beat
        // unreachable: dealt once, to a chair nobody was in, and never said.
        Assert.DoesNotContain(Person, booted.Read<HashSet<string>>("_barLeft"));
    }

    [Fact]
    public async Task AndTheWALKIsActuallyDealtWithHimInTheRoom()
    {
        // The preconditions above are arithmetic; this is the beat. A few frames of the SHIPPING metabolism
        // (AdvanceBarWalkers, the same call the walked frame makes) and the person of interest must be on his
        // feet on the route that ends at the rail.
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(Url(TheDocumentedHour));
        Assert.Null(booted.Threw);

        PastTheGateBench.StepTheBar(booted.Page, frames: 5);

        Assert.Contains(PastTheGateBench.Afoot(booted.Page),
            w => w.For == Map.Errand.WalkingTheRoute && string.Equals(w.Who, Person, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AndAtAnHourThatIsNotPastLastCallItIsCorrectlyNOTDealt()
    {
        // The other half, and the reason the doc row had to move: ?simhours=4 is the START of watch 1. With
        // the clock honest, that is a room whose evening has not finished, and the refusal is right. A guard
        // that could not tell those two hours apart would go green on the broken build, where every hour
        // looked past last call.
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(Url(4));
        Assert.Null(booted.Threw);

        PastTheGateBench.StepTheBar(booted.Page, frames: 5);

        Assert.DoesNotContain(PastTheGateBench.Afoot(booted.Page),
            w => w.For == Map.Errand.WalkingTheRoute && string.Equals(w.Who, Person, StringComparison.Ordinal));
        Assert.False(booted.Read<bool>("_walkDealt"));
    }

    // ── How this file reads a booted page ────────────────────────────────────────────────────────────────

    /// <summary>The room as the page holds it: who the rota seated at the frozen watch, and where. Rendered
    /// rather than compared field by field so a failure names the two evenings.</summary>
    private static string TheRoomOf(PastTheGateBench.Boot booted)
    {
        double frozen = booted.Read<double>("_dockVisitSimTime");
        return string.Join("\n", HavenInterior.ResolveRegulars(Berth, frozen)
            .Select(r => $"{r.Id} {r.State} present={r.Present} at ({r.X:F2},{r.Y:F2})"));
    }

    /// <summary>…and the evening that room is going to have: who the shift has finishing and going, when,
    /// and through which leaf.</summary>
    private static string TheEveningsSchedule(PastTheGateBench.Boot booted)
    {
        double frozen = booted.Read<double>("_dockVisitSimTime");
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;
        IReadOnlyList<HavenInterior.SeatedRegular> rota = HavenInterior.ResolveRegulars(Berth, frozen);
        var seated = new List<Egress.Occupant>();
        for (int i = 0; i < rota.Count; i++)
        {
            if (rota[i].Present)
            {
                seated.Add(new Egress.Occupant(i, rota[i].Id));
            }
        }

        return string.Join("\n", Egress
            .Departures(Berth, 0, PatronRota.WatchIndex(frozen), seated, bar.Doors)
            .Select(m => $"{m.Plate} @{m.AtSecondsIntoWatch:F1} door {m.Door}"));
    }

    /// <summary>Move the world's clock the way the page itself moves it — the hull's own <c>SimTime</c> and
    /// the page's together, because they are one fact.</summary>
    private static void SetClock(Map page, double simTime)
    {
        typeof(Map).GetField("SimTime", Hidden)!.SetValue(page, simTime);
        FieldInfo hull = typeof(Map).GetField("_ship", Hidden)!;
        hull.SetValue(page, (ShipState)hull.GetValue(page)! with { SimTime = simTime });
    }

    /// <summary>Tie up at the berth through the page's own one clamp, at whatever the clock now reads.</summary>
    private static void Dock(Map page) =>
        typeof(Map).GetMethod("StartDockedAtHaven", Hidden)!.Invoke(page, [Berth]);
}
