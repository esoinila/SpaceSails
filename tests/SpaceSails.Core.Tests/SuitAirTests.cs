using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #564 · The suit tank. These are written directly against the owner's acceptance test:
///
/// <para><i>"I want to test walking in some direction until I get warning that my point of no return to walk
/// back is soon... then I just continue the same distance more and suffocate (or find by luck a supply of
/// refill there). Then I can walk back and everything is logically there."</i></para>
/// </summary>
public class SuitAirTests
{
    [Fact]
    public void TheTutorialStopsBeingALie()
    {
        // GroundLesson.Laws[0] has told every new captain "The walk back is half the tank" since #440,
        // about a resource that did not exist. The claim is now checkable, so check it: standing at the
        // reach of a full tank, the walk home really is about half of what you started with.
        double reach = SuitAir.RemainingReachDu(SuitAir.TankSeconds, 0);
        double home = SuitAir.WalkHomeSeconds(reach);

        Assert.InRange(home / SuitAir.TankSeconds, 0.40, 0.50);
    }

    [Fact]
    public void TheOwnersTest_WalkOutToTheWarning_ThenTheSameAgain_Suffocates()
    {
        // Walk out one deck-unit at a time from the tube on a full tank, stopping the moment the suit says
        // the line is behind you. Then walk exactly that distance again — which is what he described — and
        // require the tank to be empty before it is finished.
        double air = SuitAir.TankSeconds;
        double outDu = 0;
        const double stepDu = 1.0;
        double stepSeconds = stepDu / SuitAir.WalkSpeedDu;

        while (!SuitAir.PastPointOfNoReturn(air, outDu) && air > 0)
        {
            outDu += stepDu;
            air = SuitAir.Drain(air, stepSeconds);
        }

        Assert.True(outDu > 0, "the warning fired before the captain had taken a step.");
        Assert.True(air > 0, "the captain suffocated before ever being warned — that is the silent timer.");

        double warnedAt = outDu;
        double target = outDu * 2;
        while (outDu < target && air > 0)
        {
            outDu += stepDu;
            air = SuitAir.Drain(air, stepSeconds);
        }

        // DOOMED, not necessarily dead on the spot — and the distinction is the design, not a fudge.
        //
        // The warning fires slightly BEFORE the true point of no return (ReserveFactor), which is what
        // makes obeying it survivable; a warning you cannot act on is decoration. So a captain who ignores
        // it and walks the same distance again arrives at the far end still breathing, on a margin that
        // cannot possibly carry them home. They suffocate on the walk back, which is exactly the owner's
        // "continue the same distance more and suffocate" — and it is a better scene than dropping on the
        // spot, because they get the whole walk to know.
        Assert.True(air > 0, "dead exactly at the far point — no walk back, no dawning realisation.");
        Assert.True(SuitAir.PastPointOfNoReturn(air, outDu),
            $"went twice the warned distance ({warnedAt:F0} du) and can still get home — the line means nothing.");
        Assert.True(air < SuitAir.WalkHomeSeconds(outDu),
            "the tank could still cover the bare walk home; ignoring the warning has to be fatal.");

        // Prove it: turn round now and walk, and the air is gone before the tube is.
        double onTheWayBack = SuitAir.Drain(air, SuitAir.WalkHomeSeconds(outDu));
        Assert.Equal(0.0, onTheWayBack);
    }

    [Fact]
    public void TurningRoundAtTheWarning_GetsYouHomeWithAirToSpare()
    {
        // The other half of the promise, and the one that makes the warning worth obeying. If heeding it
        // still killed you, it would be decoration.
        double air = SuitAir.TankSeconds;
        double outDu = 0;
        const double stepDu = 1.0;
        double stepSeconds = stepDu / SuitAir.WalkSpeedDu;

        while (!SuitAir.PastPointOfNoReturn(air, outDu))
        {
            outDu += stepDu;
            air = SuitAir.Drain(air, stepSeconds);
        }

        // Turn round here and walk all the way back.
        air = SuitAir.Drain(air, SuitAir.WalkHomeSeconds(outDu));
        Assert.True(air > 0, "obeying the warning still suffocated you — the reserve is a lie.");
    }

    [Fact]
    public void AShortExcursionIsNeverHurried()
    {
        // Air prices DISTANCE, not busyness. A dig-and-bury run inside the landing area — a few dozen deck
        // units of walking and a couple of minutes of standing still — must not come close to the line.
        const double aroundTheLandingArea = 40;
        double air = SuitAir.Drain(SuitAir.TankSeconds, 120);   // two minutes of digging

        Assert.False(SuitAir.PastPointOfNoReturn(air, aroundTheLandingArea));
        Assert.Equal(SuitAir.Band.Easy, SuitAir.BandFor(air, aroundTheLandingArea));

        // ...and the suit says nothing at all about it, because home is right there. Owner: air must not be
        // "an adversary scarier than the old ones", and a warning that fires when nothing is wrong is how
        // that happens.
        Assert.False(SuitAir.RunningLow(SuitAir.TankSeconds * 0.2, aroundTheLandingArea));
    }

    [Fact]
    public void TheReadoutSaysHowMuchFURTHER_NotJustHowMuchLEFT()
    {
        // A bare countdown is the silent timer wearing a gauge. The number that makes it actionable is how
        // much further you may go and still come home.
        string easy = SuitAir.Readout(SuitAir.TankSeconds, 20);
        Assert.Contains("du further", easy, StringComparison.Ordinal);

        // ...and once the line is behind you it stops offering a distance and says so plainly. Chosen well
        // ABOVE the critical band and the reserve: at 30 s the suit has more urgent things to say, and
        // rightly — "past the line" is a routing problem, "almost gone" is an emergency.
        string past = SuitAir.Readout(200, 3000);
        Assert.Contains("PAST THE LINE", past, StringComparison.Ordinal);
    }

    [Fact]
    public void ReachFallsToZeroExactlyWhenTheLineIsCrossed()
    {
        // The gauge and the warning must agree. Two numbers that disagree about the same fact is the exact
        // class of bug this project keeps paying for.
        for (double d = 0; d < 2000; d += 25)
        {
            double air = SuitAir.NeededToGetHome(d);
            Assert.Equal(0.0, SuitAir.RemainingReachDu(air - 0.001, d));
            Assert.True(SuitAir.PastPointOfNoReturn(air - 0.001, d));
            Assert.False(SuitAir.PastPointOfNoReturn(air + 0.001, d));
        }
    }

    [Fact]
    public void TheSuitCaresAboutTHEROUTEHOME_NeverAboutHowDeepYouAre()
    {
        // #453: "Let's not have any don't venture too far set-up by y-coordinate … How deep you dare go is
        // priced by sentries and nerve, not by geometry." Every entry point here takes a DISTANCE, so a
        // captain 400 du sideways and a captain 400 du deep are treated identically — which is the whole
        // difference between pricing a route and grading a map.
        Assert.Equal(SuitAir.NeededToGetHome(400), SuitAir.NeededToGetHome(400));
        Assert.Equal(SuitAir.BandFor(120, 400), SuitAir.BandFor(120, 400));
        Assert.True(SuitAir.NeededToGetHome(800) > SuitAir.NeededToGetHome(400));
    }

    [Fact]
    public void ARefillCanNeverHandYouMoreReachThanTheSuitHolds()
    {
        // A found cache extends a trip; it must not create a captain who can outrun the mechanic entirely.
        // FullSeconds, not TankSeconds — the EMU's secondary pack rides on top of the primary, so a suit
        // brimmed to the stops holds both.
        Assert.Equal(SuitAir.FullSeconds, SuitAir.Refill(SuitAir.FullSeconds, 9999));
        Assert.Equal(SuitAir.FullSeconds, SuitAir.Refill(10, 9999));
        Assert.Equal(60.0, SuitAir.Refill(20, 40));
    }

    [Fact]
    public void TheCrossingWarningIsALineYouCrossed_NotANumberYouMissed()
    {
        // The rule the whole mechanic is built under. The words matter: it has to name the decision, and it
        // has to say turning round still works.
        Assert.Contains("THAT WAS THE LINE", SuitAir.CrossingWarning, StringComparison.Ordinal);
        Assert.Contains("Turn now", SuitAir.CrossingWarning, StringComparison.Ordinal);
        Assert.True(SuitAir.SuffocationLine.Length > 60);
    }

    [Fact]
    public void EmptyReadsEmpty()
    {
        Assert.Equal(SuitAir.Band.Gone, SuitAir.BandFor(0, 10));
        Assert.Contains("EMPTY", SuitAir.Readout(0, 10), StringComparison.Ordinal);
        Assert.Equal(0.0, SuitAir.Drain(0, 5));
    }

    // ── #573 · BREATHING. All of it from the owner's diving: "keep calm so the O2 does not run out". ──

    [Fact]
    public void StandingStillIsCheaperThanWalking_WhichIsCheaperThanRunning()
    {
        // The rule that makes holding your nerve a MOVE rather than a mood. If running were ever cheaper,
        // the correct play would be to sprint everywhere and the whole idea collapses.
        double still = SuitAir.Breathing.Rate(SuitAir.Breathing.Still, 100, 0, 5);
        double walk = SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 100, 0, 5);
        double run = SuitAir.Breathing.Rate(SuitAir.Breathing.Running, 100, 0, 5);

        Assert.True(still < walk);
        Assert.True(walk < run);
    }

    [Fact]
    public void FearAndInjuryBothCostAir_AndTheyCompound()
    {
        // "close encounters with reevers and running from them might consume more air", and the owner's own
        // upset-stomach dive: a body in trouble burns more just existing.
        double calmWhole = SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 100, 0, 5);
        double frightened = SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 5, 0, 5);
        double hurt = SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 100, 4, 5);
        double both = SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 5, 4, 5);

        Assert.True(frightened > calmWhole);
        Assert.True(hurt > calmWhole);
        Assert.True(both > frightened && both > hurt);
    }

    [Fact]
    public void KEEPINGCALMBEATSRUNNING_WhichIsTheWholePoint()
    {
        // The owner's shark. A captain who holds still with their nerve intact spends far less than one who
        // panics and runs — so "keep calm in the face of danger so you don't choke" is literally the
        // cheapest option, not merely the brave-sounding one.
        double calmAndStill = SuitAir.Breathing.Rate(SuitAir.Breathing.Still, 90, 0, 5);
        double panickedFlight = SuitAir.Breathing.Rate(SuitAir.Breathing.Running, 10, 3, 5);

        Assert.True(panickedFlight > calmAndStill * 2.5,
            $"panic costs only {panickedFlight / calmAndStill:F1}x a calm halt — not enough to be a decision.");
    }

    [Fact]
    public void TheWorstCaseIsBounded_SoAPlayerCanPlanAgainstIt()
    {
        // An unbounded distress spiral is a death nobody can see coming. There has to be a floor under how
        // bad it gets.
        double worst = SuitAir.Breathing.Rate(SuitAir.Breathing.HeavyLabour, 0, 5, 5);
        Assert.True(worst <= SuitAir.Breathing.HeavyLabour * SuitAir.Breathing.MaxDistress + 0.001);
    }

    [Fact]
    public void PhysicalWorkCostsMorePerMinuteThanEasyWork()
    {
        // "physical chores like digging a hole could cost more even though not taking as long." Effort is
        // not measured in minutes, and a short hard job can outweigh a long easy one.
        double diggingTenMinutes = SuitAir.CostOfTask(10, SuitAir.Breathing.HeavyLabour);
        double loiteringTwenty = SuitAir.CostOfTask(20, SuitAir.Breathing.Still);

        Assert.True(diggingTenMinutes > loiteringTwenty,
            "ten minutes of digging is cheaper than twenty of standing about — effort is not counting.");
    }

    [Fact]
    public void ATaskCanBeCheckedBeforeItStrandsYou()
    {
        // A time-skip that leaves you unable to get home is a trap, not a decision. The question has to be
        // askable BEFORE the clock jumps.
        // A HALF again over the bare walk home, not 5% — at a 5% cushion even a one-minute job strands you,
        // which is the arithmetic being right and the test being unrealistic. A captain with a genuine
        // margin can afford a short task and not a long one, which is the decision this exists to support.
        double airForAShortWalkHome = SuitAir.NeededToGetHome(300) * 1.5;
        Assert.False(SuitAir.TaskWouldStrandYou(airForAShortWalkHome, 1, 300));
        Assert.True(SuitAir.TaskWouldStrandYou(airForAShortWalkHome, 240, 300));
    }
}
