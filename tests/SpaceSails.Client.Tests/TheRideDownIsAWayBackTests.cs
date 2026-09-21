using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1253 · <b>THE RIDE, ON A LIVE PAGE.</b> The geometry next door
/// (<see cref="TheLevelUnderTheConcourseTests"/>) proves the floor is a floor; this proves the game can put
/// a captain on it and get him back off, through the page's own verbs and nothing a test wrote into a field.
///
/// <para>Everything here is driven through <c>RideTheHavenLiftTo</c>, <c>PressLiftButton</c>,
/// <c>AdvanceBarWalkers</c> and <c>PullAvatarAboard</c> — the four doors a player actually goes through.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRideDownIsAWayBackTests
{
    private static string Berth => HavenInterior.TheHavenWithFloors!;

    /// <summary>A live page clamped on at the one station with a floor under it, ashore in the bar — the
    /// posture a captain is in when he first walks up to a car.</summary>
    private static Pages.Map Ashore(string canvasId)
    {
        Pages.Map map = Boot(canvasId);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Berth);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Berth, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Berth, (string?)Read(map, "_dockedHavenId"));
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!, "the ashore boot refused this berth.");
        Assert.Equal(HavenLevels.Concourse, Floor(map));
        return map;
    }

    private static int Floor(Pages.Map map) => (int)Read(map, "_havenFloor")!;

    private static (double X, double Y) Where(Pages.Map map) =>
        ((double)Read(map, "_avatarX")!, (double)Read(map, "_avatarY")!);

    private static bool Ride(Pages.Map map, int level, int cage) =>
        (bool)Invoke(map, "RideTheHavenLiftTo", level, cage)!;

    private static IList<Pages.Map.Walker> Afoot(Pages.Map map) =>
        (IList<Pages.Map.Walker>)Read(map, "_barAfoot")!;

    /// <summary>One field off the boot's own <c>BootQuery</c> — a private nested struct, so it is read the
    /// way every other private thing in these benches is.</summary>
    private static object? OnTheQuery(object query, string field) =>
        query.GetType().GetField(field, Hidden)!.GetValue(query);

    // ── THE RIDE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>YOU COME OUT WHERE THE CAR YOU RODE OPENS, AND NOWHERE ELSE.</b> Down on one car and back up on a
    /// DIFFERENT one, and the captain is standing at each one's own landing — which is the whole of what the
    /// owner asked for: <i>"The main hall could have multiple elevators… good for tailing."</i> If every car
    /// put you in the same place there would be nothing to guess and nothing to lose.
    ///
    /// <para>The landings are asked of the room (<see cref="HavenInterior.TheCageLandingAt"/>) rather than
    /// typed, and the tolerance is a body's own radius because the placement is nudged clear of stone on the
    /// way out of the car — the net #602 put under every placement in this game after a captain came up
    /// inside a wall.</para>
    ///
    /// <para><b>Proven RED</b> by standing the captain at cage 0's landing whatever cage he rode:
    /// <c>rode car 1 on the concourse and came out 24.0 du from its doors.</c></para>
    /// </summary>
    [Fact]
    public void TheDoorsOpenAtTheCarYouRodeOnEitherFloor()
    {
        Pages.Map map = Ashore("ride-lands-where-you-rode");
        int cars = HavenInterior.TheCagesAt(Berth).Count;
        Assert.Equal(HavenLevels.Cages, cars);

        for (int down = 0; down < cars; down++)
        {
            Assert.True(Ride(map, HavenLevels.ServiceLevel, down), $"car {down} refused to go down.");
            Assert.Equal(HavenLevels.ServiceLevel, Floor(map));
            AssertStandingAtTheCar(map, down, "below");

            int up = (down + 1) % cars;   // …and back up on a DIFFERENT one, which is the whole mechanic.
            Assert.True(Ride(map, HavenLevels.Concourse, up), $"car {up} refused to go up.");
            Assert.Equal(HavenLevels.Concourse, Floor(map));
            AssertStandingAtTheCar(map, up, "on the concourse");
        }
    }

    private static void AssertStandingAtTheCar(Pages.Map map, int cage, string where)
    {
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, cage)!.Value;
        (double x, double y) = Where(map);
        double dx = x - landing.X, dy = y - landing.Y;
        Assert.True(
            Math.Sqrt((dx * dx) + (dy * dy)) <= 2 * DeckPlan.AvatarRadius,
            $"rode car {cage} {where} and came out {Math.Sqrt((dx * dx) + (dy * dy)):F1} du from its doors.");
    }

    /// <summary>
    /// <b>THE PANEL IS THE ROAD, AND IT IS THE PANEL THE PLAYER PRESSES.</b> The press goes through the page's
    /// own <c>PressLiftButton</c> — the same <c>Action&lt;LiftStop&gt;</c> <c>LiftPanel.razor</c> binds — so a
    /// ride that worked only when a test called the ride directly would fail here.
    ///
    /// <para>And the row it is handed is the one the surface would have drawn: <c>LiftStops()</c>, asked of
    /// the page in the posture the panel is open in. That is what makes this a test of the WIRE rather than
    /// of two methods that happen to agree.</para>
    /// </summary>
    [Fact]
    public void PressingTheButtonOnThePanelRidesTheCar()
    {
        Pages.Map map = Ashore("ride-through-the-panel");

        // Stand at a car and press [E] on it, which is how the panel comes up at all.
        DeckReachability.Point car = HavenInterior.TheCagesAt(Berth)[1];
        Set(map, "_avatarX", car.X);
        Set(map, "_avatarY", car.Y);
        Invoke(map, "HavenLiftInteract");
        Assert.True((bool)Read(map, "_showLiftPanel")!, "[E] at a car opened no panel.");

        var stops = (IReadOnlyList<UndergroundComplex.LiftStop>)Invoke(map, "LiftStops")!;
        Assert.Equal(HavenLevels.Levels.Count, stops.Count);
        Assert.Equal(HavenLevels.NameOf(HavenLevels.Concourse), (string)Invoke(map, "LiftPanelDepth")!);

        UndergroundComplex.LiftStop below = stops.Single(s => s.Level == HavenLevels.ServiceLevel);
        Invoke(map, "PressLiftButton", below);

        Assert.False((bool)Read(map, "_showLiftPanel")!, "the panel stayed open after a ride.");
        Assert.Equal(HavenLevels.ServiceLevel, Floor(map));
        AssertStandingAtTheCar(map, 1, "below");
        Assert.Equal(HavenLevels.NameOf(HavenLevels.ServiceLevel), (string)Invoke(map, "LiftPanelDepth")!);
    }

    /// <summary>
    /// <b>CASTING OFF FROM DOWN THERE PUTS THE CAPTAIN ABOARD, WHATEVER HIS Y READS.</b> The audit's own
    /// second prediction: <i>"nothing is z-aware, so <c>RefreshAshore</c> / <c>PullAvatarAboard</c> stay
    /// permanently true — cast off from below and you never get pulled aboard."</i>
    ///
    /// <para>The captain is put on the SOUTHERN half of the service level for this case on purpose, which is
    /// ground whose y reads as ABOARD on the continuous deck the whole complex is welded into. With the floor
    /// test taken out he stays exactly where he is standing, in a station that is no longer welded on, with
    /// no ship under him.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the bare <c>_avatarY &gt; ShipDeckTopY</c>: the captain finishes
    /// the cast-off on the service level's own floor, aboard nothing.</para>
    /// </summary>
    [Fact]
    public void CastingOffFromTheServiceLevelPutsHimBackAboard()
    {
        Pages.Map map = Ashore("cast-off-from-below");
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 1));

        // Somewhere down there whose y is inside the ship's own band — the deck is one coordinate space.
        Set(map, "_avatarX", 2.5);
        Set(map, "_avatarY", 12.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "a square of the service level did not read as ashore.");

        Invoke(map, "PullAvatarAboard");

        Assert.Equal(HavenLevels.Concourse, Floor(map));
        Assert.False((bool)Read(map, "_ashore")!);
        (double _, double y) = Where(map);
        Assert.True(y <= 14, $"the cast-off left the captain at y {y:F1}, which is not aboard.");
    }

    // ── THE PEOPLE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOBODY IS ON TWO FLOORS AT ONCE — AND THE EVENING SURVIVES THE RIDE.</b> Two halves of one rule,
    /// and they pull in opposite directions, which is why they are asserted together.
    ///
    /// <para>The FEET are the floor's: a body crossing the concourse is not in the corridor under it, and a
    /// list carried down a shaft is the audit's named failure (<i>"two rooms, one people-list: bodies are in
    /// two places at once"</i>). The EVENING is the station's: who finished and went, and who came out of the
    /// back and sat down, is what this visit has done to the room — and a captain who rides down for two
    /// minutes must not come back up to find the chairs re-seated and the schedule re-dealt.</para>
    ///
    /// <para><b>Proven RED</b> both ways: keying the feet on the berth alone leaves a walker on the floor
    /// through a ride; clearing the churn on a floor change empties <c>_barLeft</c> under the returning
    /// captain.</para>
    /// </summary>
    [Fact]
    public void TheFeetGoWithTheFloorAndTheEveningDoesNot()
    {
        Pages.Map map = Ashore("feet-and-evening");

        // Put the room's own hours in motion: past last call, somebody is dealt out through a leaf.
        Set(map, "_dockVisitSimTime", 0.0);
        Set(map, "SimTime", PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));
        object bar = HavenInterior.BarBand(Berth)!;
        string person = TheTail.ThePersonOfInterest(Berth);
        Invoke(map, "SendThemOutOntoTheWalk", bar, person, null);

        Assert.NotEmpty(Afoot(map));
        var left = (IReadOnlySet<string>)Read(map, "_barLeft")!;
        Assert.Contains(person, left);

        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));

        Assert.Empty(Afoot(map));
        Assert.Contains(person, (IReadOnlySet<string>)Read(map, "_barLeft")!);

        Assert.True(Ride(map, HavenLevels.Concourse, 0));
        Assert.Empty(Afoot(map));
        Assert.Contains(person, (IReadOnlySet<string>)Read(map, "_barLeft")!);
    }

    /// <summary>
    /// <b>THE ROOM'S BEATS ARE THE CONCOURSE'S, AND THE TAIL IS NOT DEALT DOWN A FLOOR.</b> A whole visit's
    /// worth of frames run on the service level, past last call, at the one station with a walk on it: nobody
    /// is sent anywhere and nothing is dealt. The tail's post and notice logic never targets the lower level
    /// in this slice — stated here rather than in a comment, because a beat that quietly began firing in a
    /// corridor is a beat nobody would find until it was played.
    ///
    /// <para>And the same frames run on the CONCOURSE do deal it, so this case cannot pass by the room being
    /// asleep.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>OnTheConcourse</c> guard out of <c>AdvanceBarWalkers</c>:
    /// <c>the walk was dealt on the service level.</c></para>
    /// </summary>
    [Fact]
    public void NothingIsDealtOnTheServiceLevelAndEverythingIsOnTheConcourse()
    {
        Pages.Map map = Ashore("nothing-is-dealt-below");
        Set(map, "_dockVisitSimTime", 0.0);
        Set(map, "SimTime", PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));

        for (int frame = 0; frame < 600; frame++)
        {
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
        }

        Assert.False((bool)Read(map, "_walkDealt")!, "the walk was dealt on the service level.");
        Assert.Empty(Afoot(map));
        Assert.Empty((IReadOnlySet<string>)Read(map, "_barLeft")!);

        // …and the same frames upstairs DO deal it, so the case is not green because nothing works.
        Assert.True(Ride(map, HavenLevels.Concourse, 0));
        for (int frame = 0; frame < 600; frame++)
        {
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
        }

        Assert.True((bool)Read(map, "_walkDealt")!, "the walk is not dealt on the concourse either.");
    }

    // ── WHAT THE PLACE IS CALLED ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BOOK SAYS WHICH FLOOR, AND ONLY WHEN IT IS NOT THE ONE THE BERTH ALREADY IMPLIES.</b> A note
    /// filed in the bar reads exactly as it always did; a note filed on the service level is filed under a
    /// different drawer, because a captain going back through his own book for where he was standing is
    /// asking about a PLACE and two floors of one station are two places.
    ///
    /// <para><b>Proven RED</b> by having the suffix answer the level's plate at the concourse too: every note
    /// ever filed in a haven bar changes its drawer.</para>
    /// </summary>
    [Fact]
    public void TheFieldBookNamesTheFloorOnlyWhenItIsNotTheConcourse()
    {
        Pages.Map map = Ashore("the-book-names-the-floor");
        var inTheBar = (string)Invoke(map, "TheBooksNameForHere")!;
        Assert.Contains(HavenInterior.BarNameOf(Berth)!, inTheBar, StringComparison.Ordinal);

        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));
        var below = (string)Invoke(map, "TheBooksNameForHere")!;

        Assert.NotEqual(inTheBar, below);
        Assert.Contains(HavenLevels.NameOf(HavenLevels.ServiceLevel), below, StringComparison.Ordinal);
        Assert.DoesNotContain(HavenInterior.BarNameOf(Berth)!, below, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>THE AMBIENT POOL SAYS NOTHING DOWN THERE.</b> The lower concourse gets its own share of the
    /// shudder pool and the share is EMPTY — the walk's precedent (#1261), for the walk's reason: every line
    /// in that pool is a roomful of people deciding together that it was nothing, and there is nobody in a
    /// service corridor to do the deciding.
    ///
    /// <para>The room the page answers is asked through the page, so this is a law about what a captain
    /// standing down there actually hears rather than about an enum member.</para>
    ///
    /// <para><b>Proven RED</b> by giving the level the concourse's share: a shudder on the service level
    /// says <i>"a shudder walks through the concourse and every conversation stops mid-word"</i> in a
    /// corridor with five shut doors on it.</para>
    /// </summary>
    [Fact]
    public void TheShudderHasNothingToSayOnTheServiceLevel()
    {
        Pages.Map map = Ashore("no-line-down-below");
        Assert.True(Ride(map, HavenLevels.ServiceLevel, 0));

        var room = (HullShudder.HavenRoom)Read(map, "TheRoomOfTheHavenHeIsIn")!;
        Assert.Equal(HullShudder.HavenRoom.LowerConcourse, room);
        Assert.Empty(HullShudder.LinesFor(HullShudder.Setting.Haven, room));
        Assert.Null(HullShudder.Line(HullShudder.Setting.Haven, room, seed: 11, shudderIndex: 0));

        // …and the concourse's own line is still there for the floor it was written about.
        Assert.NotEmpty(
            HullShudder.LinesFor(HullShudder.Setting.Haven, HullShudder.HavenRoom.Concourse));
    }

    // ── THE DOOR ON THE FRONT OF IT ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SCENE IS ONE URL AWAY.</b> A scene nobody can reach on demand is a scene that ships broken, and
    /// this one sits behind a walk an MCP-driven tab cannot make and then a press at the far side of a hall.
    /// The row is in the catalogue, its URL is the one the parser understands, and reading that URL puts the
    /// captain on the floor.
    ///
    /// <para>The cheat is read through <c>ReadEveryQueryKey</c> — the boot's own parse — so a row whose URL
    /// nobody could parse is a button that goes nowhere.</para>
    /// </summary>
    [Fact]
    public void TheDevStartRowReadsAsAFloorCheat()
    {
        const string Url = "/map?dock=selene-gate&ashore=1&havenfloor=-1";
        Assert.Contains(DevStarts.All, e => string.Equals(e.Url, Url, StringComparison.Ordinal));

        Pages.Map map = Boot("the-dev-start-reads");
        object q = Invoke(map, "ReadEveryQueryKey", new Uri("https://localhost" + Url))!;
        Invoke(map, "DefaultABerthForTheCheatsThatNeedOne", q);
        Assert.Equal(HavenLevels.ServiceLevel, (int?)OnTheQuery(q, "HavenFloorCheat"));
        Assert.True((bool)OnTheQuery(q, "AshoreCheat")!, "?havenfloor= must imply the ashore walk.");
        Assert.Equal(Berth, (string?)OnTheQuery(q, "DockCheat"));

        // …and a level this game does not have is simply not read: a typo is a concourse, never a building
        // at level −4.
        object bad = Invoke(map, "ReadEveryQueryKey", new Uri("https://localhost/map?havenfloor=-4"))!;
        Assert.Null((int?)OnTheQuery(bad, "HavenFloorCheat"));
    }
}
