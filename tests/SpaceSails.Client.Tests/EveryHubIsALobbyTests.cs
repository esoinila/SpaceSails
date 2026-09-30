using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1332 A · <b>EVERY HUB IS A LOBBY</b> (= #1253 slice 3), on a live page. Owner, 2026-09-29: <i>"The big round
/// immigration points already look like elevator lobbies, so we might as well have those hubs have elevators
/// that take to apartment-hotel-like, usually locked, spaces down below."</i>
///
/// <para>The floor's own laws (cars, corridor, fire code, way home, doors, one label) run over every haven in
/// <see cref="TheLevelUnderTheConcourseTests"/> and <see cref="TheRideDownIsAWayBackTests"/>. This file holds
/// what the slice ADDED on top of Selene Gate's shape: the one told line, the dev starts, and the fence that
/// keeps GILT-EYE's night at the one station it was written for.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EveryHubIsALobbyTests
{
    public static TheoryData<string> Lobbies => TheLevelUnderTheConcourseTests.Lobbies;

    /// <summary>Every lobby but the one with the walk — the six stations this slice gave a floor.</summary>
    public static TheoryData<string> NewLobbies
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (string id in HavenInterior.InteriorBodyIds
                         .Where(HavenInterior.HasLowerLevel)
                         .Where(id => !string.Equals(id, ObservationWalk.HavenId, StringComparison.Ordinal)))
            {
                rows.Add(id);
            }

            return rows;
        }
    }

    private static string? InTheSlot(Pages.Map map) => ((PulseSlot)Read(map, "_pulse")!).Message;

    private static bool Ride(Pages.Map map, int level, int cage) =>
        (bool)Invoke(map, "RideTheHavenLiftTo", level, cage)!;

    private static void Frames(Pages.Map map, int n, bool freeTheSlot)
    {
        for (int frame = 0; frame < n; frame++)
        {
            if (freeTheSlot)
            {
                Set(map, "_pulse", PulseSlot.Empty);
            }

            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
        }
    }

    /// <summary>
    /// <b>THE FIRST RIDE DOWN IS TOLD ONCE AT EACH HAVEN — ON A FREE SLOT, AND FILED NOWHERE.</b> Fable canon,
    /// verbatim: <i>"The car stops where the public map does not go. Somebody lives here, and it is not
    /// you."</i>
    ///
    /// <list type="number">
    /// <item>On the concourse, frame after frame with the slot free: never.</item>
    /// <item>Down on the first ride, with a line still in the slot: it waits.</item>
    /// <item>The slot comes free: it is told, and the book has no entry for it.</item>
    /// <item>Up, down again on another car, slot free every frame: never a second time at this haven.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by switching the told block off (the call in <c>AdvanceBarWalkers</c>'
    /// lower-floor branch removed): the line was never in the slot. <b>RED</b> by dropping its
    /// <c>_pulse.Message is not null</c> clause: the line wrote over a line still being read. <b>RED</b> by
    /// dropping the <c>_firstRideToldAt</c> clause: told again on the second ride.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Lobbies))]
    public void TheFirstRideDownIsToldOnceOnAFreeSlotAndFiledNowhere(string berth)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore($"first-ride-{berth}", berth);

        // 1 · the concourse says nothing of it.
        var said = new List<string?>();
        for (int frame = 0; frame < 30; frame++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
            said.Add(InTheSlot(map));
        }

        Assert.DoesNotContain(HavenLevels.FirstRideLine, said);

        // 2 · down, with somebody else's line still being read: it waits its turn.
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ShowPulseMessage", "a line still being read", PulseRank.Status);
        Frames(map, 5, freeTheSlot: false);
        Assert.Equal("a line still being read", InTheSlot(map));

        // 3 · the slot comes free: told, and filed nowhere.
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1, freeTheSlot: false);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));
        Assert.DoesNotContain(
            (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!,
            n => n.Text.Contains(HavenLevels.FirstRideLine, StringComparison.Ordinal));

        // 4 · up, and down again on a different car: never a second time at this haven.
        Assert.True(Ride(map, HavenLevels.Concourse, 1));
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 2));
        for (int frame = 0; frame < 30; frame++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
            Assert.NotEqual(HavenLevels.FirstRideLine, InTheSlot(map));
        }
    }

    /// <summary>
    /// <b>ONCE AT EACH HAVEN, NOT ONCE IN THE GAME.</b> The same page tells the line at one station, casts
    /// off, docks at another and rides down: it is told again there — the memory is per station.
    ///
    /// <para><b>Proven RED</b> by keying the memory on nothing (any station told stops every other):
    /// the second station never hears it.</para>
    /// </summary>
    [Fact]
    public void TheLineIsOwedAtEveryHavenNotOnceInTheGame()
    {
        string[] two = [.. HavenInterior.InteriorBodyIds.Where(HavenInterior.HasLowerLevel).Take(2)];
        Assert.Equal(2, two.Length);

        Pages.Map map = TheRideDownIsAWayBackTests.Ashore("first-ride-twice", two[0]);
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));
        Frames(map, 1, freeTheSlot: true);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));

        Invoke(map, "PullAvatarAboard");
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody next = sky.Bodies.First(b => b.Id == two[1]);
        Invoke(map, "ClampOntoHaven", next, sky.Position(two[1], (double)Read(map, "SimTime")!), null);
        Assert.Equal(two[1], (string?)Read(map, "_dockedHavenId"));
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!);
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1, freeTheSlot: false);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));
    }

    /// <summary>
    /// <b>GILT-EYE'S NIGHT STAYS AT SELENE GATE.</b> Every haven has cars and cabins now, and the night's own
    /// fork (<c>BeginHisNight</c>) reads only "does this berth have a floor under it" — which is true
    /// everywhere. What keeps him at one station is the claim before it (<c>TheManTheWalkHasClaimed</c>: this
    /// berth has the walk). So a whole visit's frames past last call, on both floors of each of the six new
    /// lobbies: nobody is claimed, the walk is never dealt, nobody is sent down a car, and no leg opens.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>HasObservationWalk</c> clause out of
    /// <c>TheManTheWalkHasClaimed</c>: at the-space-bar the night was dealt — a man went down a car to a
    /// cabin at a station with no walk for him to vanish from.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(NewLobbies))]
    public void HisNightIsSeleneGatesAndNobodyElses(string berth)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore($"no-night-{berth}", berth);
        Set(map, "_dockVisitSimTime", 0.0);
        Set(map, "SimTime", PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));

        Assert.Null(Invoke(map, "TheManTheWalkHasClaimed", berth));
        Assert.False(ObservationWalk.WouldSpend(berth, null));

        Frames(map, 600, freeTheSlot: true);
        Assert.False((bool)Read(map, "_walkDealt")!, $"{berth}: the night was dealt on the concourse.");

        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));
        Frames(map, 600, freeTheSlot: true);
        Assert.False((bool)Read(map, "_walkDealt")!, $"{berth}: the night was dealt on the floor below.");
        Assert.DoesNotContain(
            (IList<Pages.Map.Walker>)Read(map, "_barAfoot")!,
            w => string.Equals(w.Who, TheTail.ThePersonOfInterest(berth), StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>EVERY NEW FLOOR IS ONE BUTTON AWAY.</b> One dev start per lobby, each the floor cheat pointed at its
    /// own berth, and each read by the boot's own parse into that berth and level −1 — so a tester can stand
    /// on any of the seven floors without the walk and the press an MCP-driven tab cannot make.
    ///
    /// <para><b>Proven RED</b> by leaving the six rows out of <c>DevStarts.All</c>: <c>no dev start stands on
    /// the-space-bar's lower floor.</c></para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Lobbies))]
    public void EveryLowerFloorHasADevStart(string berth)
    {
        string url = $"/map?dock={berth}&ashore=1&havenfloor=-1";
        Assert.True(
            DevStarts.All.Any(e => string.Equals(e.Url, url, StringComparison.Ordinal)),
            $"no dev start stands on {berth}'s lower floor.");

        Pages.Map map = Boot($"dev-start-{berth}");
        object q = Invoke(map, "ReadEveryQueryKey", new Uri("https://localhost" + url))!;
        Invoke(map, "DefaultABerthForTheCheatsThatNeedOne", q);
        Assert.Equal(HavenLevels.ServiceLevel, (int?)q.GetType().GetField("HavenFloorCheat", Hidden)!.GetValue(q));
        Assert.Equal(berth, (string?)q.GetType().GetField("DockCheat", Hidden)!.GetValue(q));
    }
}
