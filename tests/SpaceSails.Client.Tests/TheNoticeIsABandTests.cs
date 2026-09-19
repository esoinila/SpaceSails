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
/// #1199 · <b>THE VANISH HAPPENS AT THE THROAT, AND IT IS A BAND.</b>
///
/// <para><b>What was played</b> (inspector, live on Selene Gate, 2026-09-18, twice): GILT-EYE walked the tube
/// and then <i>held at the far end for as long as the captain was anywhere behind him</i>. After #1237 gave
/// the walk its crossbar that meant anywhere in a 24 × 8 glass gallery. The vanish never happened, so the
/// wait never started, so the card was unreachable by standing anywhere a person following somebody would
/// stand.</para>
///
/// <h3>THE GUARD THAT WOULD HAVE BEEN GREEN, AND WHY IT IS NOT IN THIS FILE</h3>
///
/// <para>The obvious law — <i>in line at 2× the legibility band he walks on, at half of it he holds</i> — is
/// <b>green on the shipped tree</b>. The EN-ROUTE hold has read the band since slice 1 (<c>3859d3e2</c>,
/// #1201): <c>_walkNoticed &amp;&amp; clearLine &amp;&amp; rangeDu &lt;= FootTail.LegibleDu</c>. Writing it
/// would have been this repository's own named hazard — a guard that cannot fail — so it is not written.
/// <see cref="TheEnRouteHoldWasALREADYABandAndStillIs"/> below pins that as the standing fact it is, and
/// says so in its own name.</para>
///
/// <para>The branch that was <b>line-of-sight ONLY, with no band at all</b>, is the one that decides whether
/// he comes off the floor — <c>if (clearLine) { LookTowards; return false; }</c> at the end of
/// <c>StepThePersonOfInterest</c>. That is the rule this lane changes and the one every law below can
/// redden.</para>
///
/// <h3>AND THE BAND AT THE THROAT IS NOT THE LEGIBILITY ONE — MEASURED</h3>
///
/// <para>The stem is <see cref="ObservationWalk.LengthDu"/> = <b>24 du</b> and <c>FootTail.LegibleDu</c> is
/// <b>30</b>. <b>A captain at the MOUTH is 24 du from the throat, which is inside the 30 du band</b> — so a
/// throat gated on legibility holds for a captain standing anywhere in the tube, and the reported bug ships
/// again in a new costume. The throat therefore borrows the number the ROOM is built from,
/// <see cref="ObservationWalk.TooCloseToGoInDu"/> = <see cref="ObservationWalk.GalleryDepthDu"/> =
/// <c>UndergroundComplex.FireCodeSmallRoomDu</c>, the game's one statement of <i>a space you can cross in
/// two paces</i>. <see cref="TheMouthIsOutsideTheThroatsBandAndInsideTheLegibilityOne"/> is that measurement,
/// written down so the next lane cannot undo it by accident.</para>
///
/// <para><b>Proven RED on the shipped rule</b> — restoring <c>if (clearLine)</c> in place of the band
/// reddens the vanish laws and leaves the en-route one green, which is the shape of the bug. Verbatim in the
/// PR body.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNoticeIsABandTests
{
    private const string CanvasId = "notice-band-canvas";

    // ── THE MEASUREMENT THAT FORCED THE SECOND BAND ─────────────────────────────────────────────────────

    [Fact]
    public void TheMouthIsOutsideTheThroatsBandAndInsideTheLegibilityOne()
    {
        // The whole argument for TooCloseToGoInDu, as arithmetic rather than as prose. If somebody ever makes
        // the stem longer than the legibility band, or the throat's band wider than the stem, this says so
        // before a player has to.
        Assert.True(ObservationWalk.LengthDu <= FootTail.LegibleDu,
            "the stem is now LONGER than the legibility band, so a captain at the mouth is no longer inside "
            + "it — the argument on ObservationWalk.TooCloseToGoInDu has to be re-made before it is trusted.");

        Assert.True(ObservationWalk.TooCloseToGoInDu < ObservationWalk.LengthDu,
            "a captain at the MOUTH must be outside the throat's band, or he can never get the vanish from "
            + "anywhere inside the tube — which is the bug this lane exists to fix.");

        // …and it is a DERIVATION, not a literal: the room's own depth, which is the fire code's own number.
        Assert.Equal(ObservationWalk.GalleryDepthDu, ObservationWalk.TooCloseToGoInDu);
        Assert.Equal(UndergroundComplex.FireCodeSmallRoomDu, ObservationWalk.TooCloseToGoInDu);
    }

    [Fact]
    public void TheEnRouteHoldWasALREADYABandAndStillIs()
    {
        // Named as the standing fact it is. Slice 1 shipped this; the lane did not add it, and a law claiming
        // otherwise would be taking credit for green.
        Walk walk = AWalkWithHimOnIt();

        walk.StandTheCaptain(behindHimDu: FootTail.LegibleDu * 2);
        walk.OneFrame();
        Assert.False(walk.HeIsHolding, "in plain view at twice the legibility band he must walk his errand.");

        walk.StandTheCaptain(behindHimDu: FootTail.LegibleDu / 2);
        walk.Notice();
        walk.OneFrame();
        Assert.True(walk.HeIsHolding, "on his heels in plain view he must stop.");
    }

    // ── 1 · THE VANISH, AT THE THROAT ───────────────────────────────────────────────────────────────────

    [Fact]
    public void FromTheMouthHeIsGoneTheMomentHeReachesTheThroat()
    {
        // The inspector's own case: far down the tube or at its mouth, the captain sees a figure turn into
        // the hat. He arrives, and the gallery is empty.
        Walk walk = AWalkWithHimOnIt();
        walk.WalkHimToTheThroat(captainBehindDu: ObservationWalk.LengthDu);

        Assert.False(walk.HeIsOnTheFloor, "he held at the throat with the captain a whole tube-length away.");
        Assert.True(walk.TheGalleryIsEmpty, "somebody is still standing in the gallery.");
        Assert.False(double.IsNaN(walk.GoneSince), "the wait never started, so the card is unreachable.");
    }

    [Fact]
    public void AndTheCardComesAfterTheWaitExactlyAsShipped()
    {
        Walk walk = AWalkWithHimOnIt();
        walk.WalkHimToTheThroat(captainBehindDu: ObservationWalk.LengthDu);

        walk.StandTheCaptainAtTheRail();
        walk.LetTheWaitPass();
        walk.AskForTheCard();

        Assert.True(walk.TheBeatIsSpent, "the walk was empty and the beat was never spent.");
        Assert.True(walk.ACardWasRaised, "nobody was told the walk was empty.");
    }

    // ── 2 · TAILED TOO CLOSE: HE HOLDS, THEN LEAVES, AND THERE IS NO CARD ───────────────────────────────

    [Fact]
    public void OnHisHeelsAtTheThroatHeWillNotGoIn()
    {
        Walk walk = AWalkWithHimOnIt();
        walk.WalkHimToTheThroat(captainBehindDu: ObservationWalk.TooCloseToGoInDu / 2);

        Assert.True(walk.HeIsOnTheFloor, "he vanished with the captain two paces behind him.");
        Assert.True(walk.HeIsHolding, "he walked into a blind room with somebody on his heels.");
        Assert.True(double.IsNaN(walk.GoneSince), "the wait started for a man who is standing right there.");
    }

    [Fact]
    public void AndAfterTheWaitHeTurnsRoundAndWalksBackOutPastYouWithNoCard()
    {
        Walk walk = AWalkWithHimOnIt();
        walk.WalkHimToTheThroat(captainBehindDu: ObservationWalk.TooCloseToGoInDu / 2);

        walk.LetTheWaitPass();
        walk.OneFrame();
        Assert.True(walk.HeHasTurnedBack, "he never gave up; he is still standing in the doorway.");

        walk.WalkHimOut();

        Assert.False(walk.HeIsOnTheFloor, "he never got out of his own tube.");
        Assert.True(walk.TheBeatIsSpent, "the beat has to be SPENT — tailing too close costs you the scene.");
        Assert.False(walk.ACardWasRaised,
            "a card explained the scene the captain was just denied. There is nothing to show him.");
        Assert.True(double.IsNaN(walk.GoneSince),
            "the wait clock started for a man who walked out past the captain in plain sight.");
    }

    [Fact]
    public void ButBACKINGOFFWhileHeHoldsGivesYouTheVanishOnTheNextFrame()
    {
        // The craft, and the anti-vacuous half of the pair above: the hold is not a dead end the captain is
        // punished into, it is a thing he can undo by giving the man room.
        Walk walk = AWalkWithHimOnIt();
        walk.WalkHimToTheThroat(captainBehindDu: ObservationWalk.TooCloseToGoInDu / 2);
        Assert.True(walk.HeIsOnTheFloor);

        walk.StandTheCaptain(behindHimDu: ObservationWalk.LengthDu);
        walk.OneFrame();

        Assert.False(walk.HeIsOnTheFloor, "he stayed in the doorway after the captain gave him the room.");
        Assert.False(walk.TheBeatIsSpent, "backing off spent the beat — that is the punishment, not the craft.");
    }

    // ── The walk, as a bench ────────────────────────────────────────────────────────────────────────────

    private static Walk AWalkWithHimOnIt() => new(CanvasId);

    /// <summary>A live page clamped on at Selene Gate with the person of interest already on his feet and
    /// walking the route — through <c>SendThemOutOntoTheWalk</c>, the shipping road onto the floor.</summary>
    private sealed class Walk
    {
        private readonly SpaceSails.Client.Pages.Map _map;
        private readonly IReadOnlyList<SurfaceCollision.Segment> _walls;

        internal Walk(string canvasId)
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

        private IReadOnlyList<SpaceSails.Client.Pages.Map.Walker> Afoot =>
            (IReadOnlyList<SpaceSails.Client.Pages.Map.Walker>)Read(_map, "_barAfoot")!;

        private SpaceSails.Client.Pages.Map.Walker? Him => Afoot.Count > 0 ? Afoot[0] : null;

        internal bool HeIsOnTheFloor => Him is not null;

        internal bool HeIsHolding => Him is { } w && w.For.ToString() == "LettingYouPass";

        internal bool HeHasTurnedBack => (bool)Read(_map, "_walkTurnedBack")!;

        internal double GoneSince => (double)Read(_map, "_walkGoneSince")!;

        internal bool TheBeatIsSpent => Read(_map, "_observationWalkSpentOn") is not null;

        internal bool ACardWasRaised => CardRaised;

        private bool CardRaised { get; set; }

        internal bool TheGalleryIsEmpty =>
            !Afoot.Any(w => HavenInterior.InTheGallery(Port, w.Walk.X, w.Walk.Y));

        /// <summary>Put the captain that many deck units behind him, along the walk's own axis — never a
        /// coordinate typed here.</summary>
        internal void StandTheCaptain(double behindHimDu)
        {
            SpaceSails.Client.Pages.Map.Walker him = Him
                ?? throw new InvalidOperationException("he is not on the floor.");
            Set(_map, "_avatarX", him.Walk.X + behindHimDu);   // the walk runs WEST, so behind is +x
            Set(_map, "_avatarY", him.Walk.Y);
        }

        internal void StandTheCaptainAtTheRail()
        {
            (double x, double y) = HavenInterior.TheRailAt(Port)
                ?? throw new InvalidOperationException("no rail at the far end.");
            Set(_map, "_avatarX", x);
            Set(_map, "_avatarY", y);
        }

        /// <summary>Force the notice latch — the roll is #436's and this file is not about the roll.</summary>
        internal void Notice() => Set(_map, "_walkNoticed", true);

        internal void OneFrame() =>
            Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 60.0, Bar, _walls, 0);

        /// <summary>Walk him the whole route with the captain held at a fixed distance behind him, one
        /// shipping frame at a time, until he is through the throat or off the floor.</summary>
        internal void WalkHimToTheThroat(double captainBehindDu)
        {
            for (int frame = 0; frame < 20_000 && HeIsOnTheFloor; frame++)
            {
                StandTheCaptain(captainBehindDu);
                Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 30.0, Bar, _walls, 0);
                if (HeIsOnTheFloor && HavenInterior.InTheGallery(Port, Him!.Walk.X, Him!.Walk.Y))
                {
                    StandTheCaptain(captainBehindDu);
                    Invoke(_map, "StepThePersonOfInterest", Him!, 1.0 / 30.0, Bar, _walls, 0);
                    return;
                }
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

        internal void LetTheWaitPass() =>
            Set(_map, "SimTime", (double)Read(_map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1.0);

        /// <summary>Ask the page for the card through its own <c>TheWalkIsEmpty</c>. Whether a card went up
        /// is read off the SPEND rather than off a chrome field, because the spend is written in the same
        /// breath as the card and is the fact the reload law is built on.</summary>
        internal void AskForTheCard()
        {
            bool before = TheBeatIsSpent;
            Invoke(_map, "TheWalkIsEmpty", Bar, Person);
            CardRaised = !before && TheBeatIsSpent;
        }
    }
}
