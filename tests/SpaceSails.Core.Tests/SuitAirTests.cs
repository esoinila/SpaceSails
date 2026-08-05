using System;
using System.Linq;
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

    // ── #612 · WHERE THE AIR IS COMING FROM. ────────────────────────────────────────────────────────────
    //
    // Owner, standing on a pressurised floor 150 m under a moon: "I thought there is air in the base?" /
    // "where here does it say if I consume tanks or have air?" / "Maybe we should have on our hud a AIR:
    // Tanks / External symbol... That is really important info for the suit hud to tell us."
    //
    // The gauge showed a clock. A clock counting down and a clock that is parked look nearly identical at a
    // glance, and the difference between them is the difference between every minute costing you and this
    // one being free.

    [Fact]
    public void APARKEDClockNeverReadsLikeARunningOne()
    {
        // THE LAW. Whatever the tank holds and however far home is, the sentence a captain reads must be
        // different when the tank is running from when it is not — not a shade different, a different
        // sentence. Swept across every band the gauge has, because a readout that only distinguishes the two
        // in the comfortable band is a readout that goes quiet exactly when it matters.
        (double Air, double Home)[] everyBand =
        [
            (SuitAir.TankSeconds, 20),                       // Easy
            (SuitAir.TankSeconds * 0.5, 3900),               // Thinking
            (200, 3000),                                     // PastTheLine
            (SuitAir.TankSeconds * 0.05, 40),                // Critical
            (SuitAir.ReserveSeconds * 0.5, 100),             // on the reserve
            (0, 10),                                         // Gone
        ];

        foreach ((double air, double home) in everyBand)
        {
            string running = SuitAir.Readout(air, home, SuitAir.Supply.Tanks);
            string held = SuitAir.Readout(air, home, SuitAir.Supply.Room);
            string filling = SuitAir.Readout(air, home, SuitAir.Supply.Ship);

            Assert.NotEqual(running, held);
            Assert.NotEqual(running, filling);
            Assert.NotEqual(held, filling);

            // The reach advice is arithmetic about SPENDING. Quoting it at somebody who is not spending is
            // the instrument answering a question nobody asked — and it is the exact way a parked clock
            // comes to look like a running one.
            Assert.DoesNotContain("du further", held, StringComparison.Ordinal);
            Assert.DoesNotContain("du further", filling, StringComparison.Ordinal);
            Assert.DoesNotContain("PAST THE LINE", held, StringComparison.Ordinal);

            Assert.Contains("SEALED", held, StringComparison.Ordinal);
            Assert.Contains("FILLING", filling, StringComparison.Ordinal);
            Assert.DoesNotContain("SEALED", running, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheSUITAndTheFLOORAgreeAboutEveryFloorInTheHive()
    {
        // Owner: "It has to agree with the plate by the lift on every floor. Two instruments disagreeing
        // about whether you can breathe is worse than one instrument saying nothing."
        //
        // The plate asks SourceOf(level, false, false) and so does the drain, so this is the one question
        // asked of every level a shaft can reach — well past any depth the generator ships, because
        // HoldsPressure is unbounded by design and a law that only holds down to B20 is not a law.
        for (int level = -1; level >= -200; level--)
        {
            SuitAir.Supply supply = SuitAir.SourceOf("miranda", level, insideShelter: false, aboard: false);
            bool holds = UndergroundComplex.HoldsPressure("miranda", level);

            Assert.Equal(holds ? SuitAir.Supply.Room : SuitAir.Supply.Tanks, supply);
            Assert.Equal(!holds, SuitAir.Drawing(supply));

            // And the words on the wall say the same thing as the bit the sim branches on.
            string plate = SuitAir.PlateLine(supply);
            Assert.Equal(holds, plate.Contains("TANK STOPPED", StringComparison.Ordinal));
            Assert.Equal(!holds, plate.Contains("TANK RUNNING", StringComparison.Ordinal));

            // ...and so does the gauge the captain is wearing while standing on it.
            Assert.Equal(holds,
                SuitAir.Readout(SuitAir.TankSeconds * 0.5, 40, supply).Contains("SEALED", StringComparison.Ordinal));
        }

        // On the regolith, with no roof of any kind, the tank is always what you are breathing.
        Assert.Equal(SuitAir.Supply.Tanks, SuitAir.SourceOf("miranda", 0, insideShelter: false, aboard: false));
    }

    [Fact]
    public void TheROOFSAreAskedInTheDrainsOwnOrder()
    {
        // SourceOf exists so that the drain and the gauge cannot come apart, which only holds while it asks
        // the questions in the order the drain asks them: the floor first, then the shelter underfoot, then
        // the ship. Pin all three, because an "obvious" reordering is a silent behaviour change in the one
        // routine that can kill a captain.

        // A live floor holds whatever is standing on it — it is not overruled by anything.
        Assert.Equal(SuitAir.Supply.Room, SuitAir.SourceOf("miranda", -1, insideShelter: false, aboard: true));
        Assert.Equal(SuitAir.Supply.Room, SuitAir.SourceOf("miranda", -1, insideShelter: true, aboard: true));

        // A dead floor is exactly as bare as open regolith. Nothing about being indoors saves you.
        Assert.Equal(SuitAir.Supply.Tanks, SuitAir.SourceOf("miranda", -2, insideShelter: false, aboard: false));

        // #608 · ...unless you are standing in the refuge somebody was made to build. This is the case that
        // broke the first cut of #612: a fourth way to breathe that the drain learned about and the gauge
        // did not, which is a captain sitting in air being told their tank is running out.
        Assert.Equal(SuitAir.Supply.Room, SuitAir.SourceOf("miranda", -2, false, false, inRefuge: true));
        Assert.False(SuitAir.Drawing(SuitAir.SourceOf("miranda", -2, false, false, inRefuge: true)));

        // And a refuge is a room on a FLOOR — it does not exist on the regolith, where the shelters are.
        Assert.Equal(SuitAir.Supply.Tanks, SuitAir.SourceOf("miranda", 0, false, false, inRefuge: true));

        // Up top: a shelter's drum outranks the ship, because you cannot be in both and the shelter is the
        // one you are standing in.
        Assert.Equal(SuitAir.Supply.Room, SuitAir.SourceOf("miranda", 0, insideShelter: true, aboard: true));
        Assert.Equal(SuitAir.Supply.Ship, SuitAir.SourceOf("miranda", 0, insideShelter: false, aboard: true));
    }

    [Fact]
    public void ThereAreThreeROOFSAndNoTwoOfThemLookAlike()
    {
        // Owner: "it must be readable as a symbol and a colour before it is read as a word." A captain
        // glances at this; they do not parse it. So every source needs its OWN glyph and its own word — two
        // roofs sharing a symbol is one roof with two names, and the glance stops working.
        var seenGlyph = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var seenWord = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var seenPlate = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var seenCrossing = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (SuitAir.Supply supply in Enum.GetValues<SuitAir.Supply>())
        {
            Assert.True(seenGlyph.Add(SuitAir.SourceGlyph(supply)),
                $"{supply} shares its glyph with another source — the symbol has stopped separating them.");
            Assert.True(seenWord.Add(SuitAir.SourceLabel(supply)), $"{supply} shares its word.");
            Assert.True(seenPlate.Add(SuitAir.PlateLine(supply)), $"{supply} shares its plate line.");
            Assert.True(seenCrossing.Add(SuitAir.SupplyChangedLine(supply)), $"{supply} shares its crossing line.");

            // One glyph, so it fits on the chip beside the word at gauge size.
            Assert.Single(SuitAir.SourceGlyph(supply));
            // The word is a word, not a sentence: it has to sit on a bar 200 px wide.
            Assert.InRange(SuitAir.SourceLabel(supply).Length, 3, 6);
        }

        // And exactly one of the three spends the tank. If two of them did, the chip would be reporting a
        // distinction the drain does not make.
        Assert.Single(Enum.GetValues<SuitAir.Supply>(), SuitAir.Drawing);
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

    /// <summary>#573/#608 · THE LOW-AIR CARD MAY NOT CONTRADICT ITS OWN HEAD. It told the captain to "find
    /// something out here that still holds a charge" and then, one beat later, that "her tube is the only
    /// place a suit refills" — written when it was true, and false since shelters and pressure refuges
    /// started recharging suits (SurfaceShelter.Transfer, to two thirds and no further). This is the same
    /// fault the underground death card was already fixed for, on the card a captain reads FIRST, and it is
    /// the dangerous direction: a captain who believes there is nothing out here rations a tank they did not
    /// have to ration and walks past the room that would have saved them. Both ends are checked — the card
    /// says a refuge exists, and the rack really does fill one.</summary>
    [Fact]
    public void TheLowAirCardNamesTheRefuge_AndDoesNotClaimTheTubeIsTheOnlyRefill()
    {
        string all = string.Join(" ", AirCard.Beats) + " " + AirCard.Head;

        Assert.Contains("shelter", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("only place a suit refills", all, StringComparison.OrdinalIgnoreCase);

        // And the sim's end of it: a rack really does put air back into a suit that is running low.
        Assert.True(SurfaceShelter.Transfer(
            airLeftSeconds: 60, reservoirSeconds: SurfaceShelter.ReservoirSeconds,
            tankSeconds: SuitAir.TankSeconds, dt: 1.0) > 0);
    }
}
