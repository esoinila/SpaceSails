using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 · <b>THE OBSERVATION WALK, AS A BENCH.</b> A live page clamped on at Selene Gate, past last call,
/// with the person of interest already on his feet and walking the route — put on the floor through
/// <c>SendThemOutOntoTheWalk</c>, which is the shipping road onto it and not a body placed by a test.
///
/// <para>One bench and not two, for the reason the house keeps <c>CastawayBench</c> and
/// <c>TestTree.RepoRoot</c>: the notice band (<see cref="TheNoticeIsABandTests"/>) and the newspaper
/// (<see cref="TheNewspaperWithEyeHolesTests"/>) are two laws about one man crossing one room, and two
/// copies of the driving would be two chances to drive him differently.</para>
///
/// <para>Everything below is read or driven through the page's own members. Nothing types a coordinate: the
/// rail, the throat, the mouth and the two tables are all asked of <c>HavenInterior</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal sealed class ObservationWalkBench
{
    private readonly SpaceSails.Client.Pages.Map _map;
    private readonly IReadOnlyList<SurfaceCollision.Segment> _walls;

    internal ObservationWalkBench(string canvasId)
    {
        _map = Boot(canvasId);

        var sky = (ICelestialEphemeris)Read(_map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(_map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(_map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(_map, "_dockedHavenId"));
        Assert.True(HavenInterior.HasObservationWalk(Port), $"{Port} has no observation walk to tail onto.");

        object bar = HavenInterior.BarBand(Port)
            ?? throw new InvalidOperationException($"{Port} draws no bar band.");
        Bar = bar;
        _walls = ((DeckPlan)Read(_map, "_deckPlan")!).CollisionField;

        // Past last call, which is the room's own statement of when a regular gets up (#731).
        Set(_map, "_dockVisitSimTime", 0.0);
        Set(_map, "SimTime", PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));

        Person = TheTail.ThePersonOfInterest(Port);
        Invoke(_map, "SendThemOutOntoTheWalk", Bar, Person);
        Assert.True(HeIsOnTheFloor, "the room refused to put him on the floor, so there is no tail to read.");
    }

    internal object Bar { get; }

    internal string Person { get; }

    internal IReadOnlyList<SurfaceCollision.Segment> Walls => _walls;

    private IReadOnlyList<SpaceSails.Client.Pages.Map.Walker> Afoot =>
        (IReadOnlyList<SpaceSails.Client.Pages.Map.Walker>)Read(_map, "_barAfoot")!;

    internal SpaceSails.Client.Pages.Map.Walker? Him => Afoot.Count > 0 ? Afoot[0] : null;

    internal bool HeIsOnTheFloor => Him is not null;

    internal bool HeIsHolding => Him is { } w && w.For.ToString() == "LettingYouPass";

    internal bool HeHasTurnedBack => (bool)Read(_map, "_walkTurnedBack")!;

    internal double GoneSince => (double)Read(_map, "_walkGoneSince")!;

    internal double AtTheRailSince => (double)Read(_map, "_walkAtTheRailSince")!;

    internal bool TheBeatIsSpent => Read(_map, "_observationWalkSpentOn") is not null;

    internal bool ACardWasRaised { get; private set; }

    internal bool TheGalleryIsEmpty =>
        !Afoot.Any(w => HavenInterior.InTheGallery(Port, w.Walk.X, w.Walk.Y));

    internal bool HeIsAtTheRail
    {
        get
        {
            if (Him is not { } w)
            {
                return false;
            }

            (double x, double y) = TheRail;
            return !w.Walk.Afoot
                && ((w.Walk.X - x) * (w.Walk.X - x)) + ((w.Walk.Y - y) * (w.Walk.Y - y))
                    <= DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        }
    }

    internal static (double X, double Y) TheRail =>
        HavenInterior.TheRailAt(Port) is { } r ? (r.X, r.Y) : throw new InvalidOperationException("no rail.");

    internal static IReadOnlyList<DeckReachability.Point> TheTables => HavenInterior.GalleryTops(Port);

    /// <summary>How many times the book has filed a given line — the one funnel, counted rather than
    /// trusted, so a note filed twice is a failure and not a shrug.</summary>
    internal int TimesFiled(string line) =>
        ((IEnumerable<FieldNote>)Read(_map, "_fieldNotes")!)
        .Count(n => string.Equals(n.Text, line, StringComparison.Ordinal));

    /// <summary>Put the captain that many deck units behind him, along the walk's own axis — never a
    /// coordinate typed here.</summary>
    internal void StandTheCaptain(double behindHimDu)
    {
        SpaceSails.Client.Pages.Map.Walker him = Him
            ?? throw new InvalidOperationException("he is not on the floor.");
        Set(_map, "_avatarX", him.Walk.X + behindHimDu);   // the walk runs WEST, so behind is +x
        Set(_map, "_avatarY", him.Walk.Y);
    }

    internal void StandTheCaptainAt(double x, double y)
    {
        Set(_map, "_avatarX", x);
        Set(_map, "_avatarY", y);
    }

    internal void StandTheCaptainAtTheRail()
    {
        (double x, double y) = TheRail;
        StandTheCaptainAt(x, y);
    }

    internal void StandTheCaptainAtTheMouth()
    {
        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(Port)
            ?? throw new InvalidOperationException("no mouth on the walk.");
        StandTheCaptainAt(mouth.X, mouth.Y);
    }

    /// <summary>#1199 · <b>SIT THE CAPTAIN DOWN AT ONE OF THE HAT'S TABLES</b>, through the seat system's own
    /// verb. He is stood on the top's square and <c>TryTakeBarTop</c> is pressed — the eighth site, the one
    /// <c>TableTalk</c>, the same snap onto the chair — so what the walk reads is the seat's own seated
    /// state and nothing a test wrote into a field.</summary>
    internal void SitTheCaptainAt(int table)
    {
        DeckReachability.Point top = TheTables[table];
        StandTheCaptainAt(top.X, top.Y);
        Assert.True(
            (bool)Invoke(_map, "TryTakeBarTop")!,
            $"the gallery table at {top.X:0.0},{top.Y:0.0} refused [E].");
        Assert.NotNull(Read(_map, "SeatedTable"));
    }

    /// <summary>#1199 · …and the OTHER way a captain's eyes leave the room: a card standing in front of it.
    /// Raised through the page's own story-card road, so what the walk reads is #1052's census and not a
    /// bool a test invented.</summary>
    internal void PutACardInFrontOfHim()
    {
        Invoke(
            _map, "RaiseStoryBeat",
            StoryBeats.Beat.TheWalksBinoculars, GalleryFixtures.Look.Out.ToString(), null);
        Assert.True((bool)Read(_map, "AScrimIsUp")!, "the card did not put a scrim in front of the world.");
    }

    /// <summary>Force the notice latch — the roll is #436's and no law here is about the roll.</summary>
    internal void Notice() => Set(_map, "_walkNoticed", true);

    internal void OneFrame() =>
        Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 60.0, Bar, _walls, 0);

    /// <summary>Advance the sim past one whole look, so the gallery's own question is asked again.</summary>
    internal void LetOneLookPass() =>
        Set(_map, "SimTime",
            (double)Read(_map, "SimTime")! + ReeverObservation.LookIntervalSeconds + 0.01);

    internal void LetTheWaitPass() =>
        Set(_map, "SimTime", (double)Read(_map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1.0);

    /// <summary>Walk him the whole route with the captain held at a fixed distance behind him, one shipping
    /// frame at a time, until he is in the hat or off the floor.</summary>
    internal void WalkHimIntoTheHat(double captainBehindDu)
    {
        for (int frame = 0; frame < 20_000 && HeIsOnTheFloor; frame++)
        {
            StandTheCaptain(captainBehindDu);
            Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 30.0, Bar, _walls, 0);
            if (HeIsOnTheFloor && HavenInterior.InTheGallery(Port, Him!.Walk.X, Him!.Walk.Y))
            {
                return;
            }
        }
    }

    /// <summary>…and the same walk with the captain standing wherever the test put him. Runs until he is off
    /// the floor, stops at the rail, or the frames run out; the sim clock advances with the frames, so the
    /// look clock ticks the way it does in play.</summary>
    internal void RunTheWalk(double seconds, Func<bool>? until = null)
    {
        const double dt = 1.0 / 30.0;
        for (double t = 0; t < seconds && HeIsOnTheFloor; t += dt)
        {
            Set(_map, "SimTime", (double)Read(_map, "SimTime")! + dt);
            Invoke(_map, "StepThePersonOfInterest", Him!, dt, Bar, _walls, 0);
            if (until is not null && until())
            {
                return;
            }
        }
    }

    /// <summary>…and the same walk with the captain held a fixed distance behind him every frame — a tail
    /// that never once takes its eyes off him, which is the one ending in which nothing happens.</summary>
    internal void RunTheWalkWithTheCaptainOnHisHeels(double seconds, double behindDu, Func<bool>? until = null)
    {
        const double dt = 1.0 / 30.0;
        for (double t = 0; t < seconds && HeIsOnTheFloor; t += dt)
        {
            StandTheCaptain(behindDu);
            Set(_map, "SimTime", (double)Read(_map, "SimTime")! + dt);
            Invoke(_map, "StepThePersonOfInterest", Him!, dt, Bar, _walls, 0);
            if (until is not null && until())
            {
                return;
            }
        }
    }

    internal bool HeIsInTheTube =>
        Him is { } w
        && HavenInterior.InTheObservationWalk(Port, w.Walk.X, w.Walk.Y)
        && !HavenInterior.InTheGallery(Port, w.Walk.X, w.Walk.Y);

    /// <summary>Walk him off the concourse and into the TUBE with the captain following at twice the
    /// legibility band — far enough back that the room’s own en-route hold does not fire, which is the
    /// point: this is the approach, not the beat. The test then stands the captain wherever the law is
    /// about.</summary>
    internal void WalkHimToTheTube()
    {
        const double dt = 1.0 / 30.0;
        for (int frame = 0; frame < 20_000 && HeIsOnTheFloor && !HeIsInTheTube; frame++)
        {
            StandTheCaptain(FootTail.LegibleDu * 2);
            Set(_map, "SimTime", (double)Read(_map, "SimTime")! + dt);
            Invoke(_map, "StepThePersonOfInterest", Him!, dt, Bar, _walls, 0);
        }
    }

    /// <summary>…and out again, on the return leg, with the captain standing still where he was.</summary>
    internal void WalkHimOut()
    {
        for (int frame = 0; frame < 20_000 && HeIsOnTheFloor; frame++)
        {
            Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 30.0, Bar, _walls, 0);
        }
    }

    /// <summary>Ask the page for the card through its own <c>TheWalkIsEmpty</c>. Whether a card went up is
    /// read off the SPEND rather than off a chrome field, because the spend is written in the same breath as
    /// the card and is the fact the reload law is built on.</summary>
    internal void AskForTheCard()
    {
        bool before = TheBeatIsSpent;
        Invoke(_map, "TheWalkIsEmpty", Bar, Person);
        ACardWasRaised = !before && TheBeatIsSpent;
    }
}
