using System;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #336 · <b>THE LINK IS RANGE AND CLOSING SPEED. THE PURE HALF.</b>
///
/// <para>Owner ruling (2026-07-18), verbatim: <i>"the shuttle ship link should not break even if the ship
/// undocks, because the docking is not requisite for the ship to stay in vicinity. As long as the ship is in
/// shuttle range (shown on map) and is not moving too fast away, we should be able to fly back to it from a
/// landing site."</i></para>
///
/// <para>This file holds the law itself; <c>SpaceSails.Client.Tests.TheShuttleCanCatchHerTests</c> stands a
/// captain on Miranda and asks the shipping page the same questions.</para>
/// </summary>
public sealed class TheShuttleLinkIsRangeNotDockageTests
{
    private const double Hop = ShuttleRange.RangeMeters;
    private const double Catch = ShuttleRange.CatchSpeedMps;

    // ── THE CATCH LAW ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SheCanBeCaughtInsideTheLegsAtAWalkingRelativeSpeed()
    {
        Assert.True(ShuttleRange.CanCatch(0, 0));
        Assert.True(ShuttleRange.CanCatch(Hop * 0.5, Catch * 0.5));
        Assert.True(ShuttleRange.CanCatch(Hop, Catch * 0.999));   // the rim itself is still a hop
    }

    [Fact]
    public void AndSheCannotBeCaughtPastTheLegsOrAboveTheCatchSpeed()
    {
        // The anti-vacuous half of the two rows above: a law that answered TRUE for everything would pass
        // them and would mean the boat flies to Neptune.
        Assert.False(ShuttleRange.CanCatch(Hop * 1.0001, 0));
        Assert.False(ShuttleRange.CanCatch(Hop * 200, 0));
        Assert.False(ShuttleRange.CanCatch(0, Catch));            // she cannot close what she cannot out-fly
        Assert.False(ShuttleRange.CanCatch(Hop * 0.1, Catch * 3));
    }

    [Fact]
    public void TheCatchSpeedIsTheBoatsOwnCruiseAndNotASecondNumber() =>
        // If somebody ever gives the catch its own literal, this is where the two models start to drift.
        Assert.Equal(ShuttleRange.CruiseSpeedMps, ShuttleRange.CatchSpeedMps);

    // ── THE REFUSAL, WITH ITS REASON ────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheBoatFliesWhateverIsHappeningToTheOrbitOrTheClamp()
    {
        // #336's whole point, and the reason this method takes only two arguments: there is no third one
        // for dock state, orbit hold, tank or keeper, so no future edit can quietly reintroduce one without
        // changing the signature this test names.
        Assert.Equal(2, typeof(ShuttleLink).GetMethod(nameof(ShuttleLink.AskTheBoat))!.GetParameters().Length);
        Assert.Equal(ShuttleLink.Refusal.None, ShuttleLink.AskTheBoat(Hop * 0.4, Catch * 0.1));
    }

    [Theory]
    [InlineData(1.5, 0.0, ShuttleLink.Refusal.BeyondRange)]
    [InlineData(40.0, 0.0, ShuttleLink.Refusal.BeyondRange)]
    [InlineData(0.4, 1.0, ShuttleLink.Refusal.TooFastToCatch)]
    [InlineData(0.01, 9.0, ShuttleLink.Refusal.TooFastToCatch)]
    public void AndWhenSheCannotBeCaughtTheBoatSaysWHICH(double hops, double catches, ShuttleLink.Refusal expected)
    {
        ShuttleLink.Refusal refusal = ShuttleLink.AskTheBoat(Hop * hops, Catch * catches);

        Assert.Equal(expected, refusal);
        Assert.NotEqual("", ShuttleLink.RefusalLine(refusal));

        // …and it is the BOAT's voice, never the ladder's. A captain who read "that is a maroon" as the
        // answer to pressing a button would think the run was over while he was still standing on a rock he
        // can be fetched off in twenty minutes.
        Assert.DoesNotContain("maroon", ShuttleLink.RefusalLine(refusal), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RangeIsAskedBEFORESpeed()
    {
        // Both wrong at once has to read as the one the captain can do nothing about. A ship four hops out
        // is not a "slow down" problem, and telling him it is would send him waiting for a brake that would
        // not help.
        Assert.Equal(ShuttleLink.Refusal.BeyondRange, ShuttleLink.AskTheBoat(Hop * 4, Catch * 4));
    }

    // ── THE LADDER: THREE RUNGS, AND THE ONE STATUS THAT IS NOT ONE ─────────────────────────────────────

    [Theory]
    [InlineData(0.0, ShuttleLink.Stage.Calm)]
    [InlineData(0.5, ShuttleLink.Stage.Calm)]
    [InlineData(0.7499, ShuttleLink.Stage.Calm)]
    [InlineData(0.75, ShuttleLink.Stage.Amber)]     // the band opens exactly at the stated fraction
    [InlineData(0.99, ShuttleLink.Stage.Amber)]
    [InlineData(1.0, ShuttleLink.Stage.Amber)]      // the rim itself is still a ride home
    [InlineData(1.0001, ShuttleLink.Stage.Lost)]
    [InlineData(12.0, ShuttleLink.Stage.Lost)]
    public void TheRungFollowsTheRANGEBand(double hops, ShuttleLink.Stage expected) =>
        Assert.Equal(expected, ShuttleLink.StageFor(Hop * hops, WindowStatus.Holding));

    [Fact]
    public void TheAmberBandIsAFractionOfTheONEReach() =>
        // Not a radius of its own. The day somebody gives amber a metre literal is the day there are two
        // numbers to keep in step, which is this repository's oldest bug class.
        Assert.Equal(Hop * ShuttleLink.AmberFraction, ShuttleLink.AmberFraction * ShuttleRange.RangeMeters);

    [Fact]
    public void AndTheWINDOWDecidesOnlyWhetherItIsAMaroonOrAWait()
    {
        // #955 NAV-2's own corner case: clamped at a giant's haven the moon windows are PERIODIC, so the gap
        // opens past a hop and closes again every synodic period with nobody in any danger. Calling that a
        // maroon would be the third named bug class — a sentence saying "that is a maroon, captain" over a
        // sim that hands the boat back in twenty minutes.
        Assert.Null(ShuttleLink.StageFor(Hop * 4, WindowStatus.Closed));

        // …and a CLOSED window swallows the in-range rungs too, because a window nobody can use is not a
        // rung whatever the gap reads: this is the status saying "she is out of reach right now".
        Assert.Null(ShuttleLink.StageFor(Hop * 0.2, WindowStatus.Closed));

        // Every other status leaves the band in charge — which is the anti-vacuous half: a mapping that
        // answered null for everything would pass the two rows above and would silence the whole ladder.
        Assert.All(Enum.GetValues<WindowStatus>(), s =>
            Assert.True(s == WindowStatus.Closed
                ? ShuttleLink.StageFor(Hop * 4, s) is null
                : ShuttleLink.StageFor(Hop * 4, s) == ShuttleLink.Stage.Lost,
                $"{s} has no considered answer in ShuttleLink.StageFor."));
    }

    // ── THE THREE LINES, VERBATIM ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLadderSaysExactlyWhatItWasAuthoredToSay()
    {
        // Pinned to the character. These are the owner-approved lines of #336 and the file that holds them
        // is the only place in the game they are spelled — a second copy anywhere is a line that will be
        // edited in one place and not the other.
        Assert.Equal("The ship drifts — still in shuttle range.", ShuttleLink.Line(ShuttleLink.Stage.Calm));
        Assert.Equal("The ship is nearing the edge of shuttle range.", ShuttleLink.Line(ShuttleLink.Stage.Amber));
        Assert.Equal("She is beyond the shuttle's legs. That is a maroon, captain.", ShuttleLink.Line(ShuttleLink.Stage.Lost));
    }

    [Fact]
    public void AndNothingIsEverSplICEDIntoThem()
    {
        // A canon line that is composed with is a canon line that has been edited. Line() is a lookup and
        // must stay one: no number, no name, no tail.
        Assert.All(Enum.GetValues<ShuttleLink.Stage>(), stage =>
        {
            string line = ShuttleLink.Line(stage);
            Assert.DoesNotContain('{', line);
            Assert.Equal(line, ShuttleLink.Line(stage));
        });

        // Three rungs, three distinct lines — a ladder whose rungs all said the same thing would pass every
        // "the stage is right" assertion in this file and would tell the captain nothing.
        Assert.Equal(3, Enum.GetValues<ShuttleLink.Stage>().Select(ShuttleLink.Line).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheMAROONIsANNOUNCEDAndReadsAsRed()
    {
        // The maroon canon: survivable, and never silent. The lost rung carries a spoken line and the top
        // severity, which is what the surface HUD's #324 visibility law paints red.
        Assert.Contains("maroon", ShuttleLink.Line(ShuttleLink.Stage.Lost), StringComparison.Ordinal);
        Assert.Equal(2, ShuttleLink.Severity(ShuttleLink.Stage.Lost));
        Assert.Equal(1, ShuttleLink.Severity(ShuttleLink.Stage.Amber));
        Assert.Equal(0, ShuttleLink.Severity(ShuttleLink.Stage.Calm));
    }
}
