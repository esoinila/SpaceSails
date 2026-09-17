namespace SpaceSails.Core.Tests;

/// <summary>
/// #161 · THE WAVE IS THE SAME WAVE WHETHER IT ARRIVES IN ONE BREATH OR IN EIGHT.
///
/// <para><b>Why this file exists.</b> The boot's own measurement said that planning the eight founding
/// freighters is a single synchronous block of about fourteen seconds on the interpreted WASM payload —
/// ninety-five percent of the whole boot, and by itself the browser's "page unresponsive" dialog. The fix
/// is not to plan them differently; it is to hand them over ONE AT A TIME, so the page can give the frame
/// back to the browser between ships. <see cref="TrafficSchedule.GenerateShipByShip"/> is that handover,
/// and <see cref="TrafficSchedule.Generate"/> is now nothing but it, drained in one go.</para>
///
/// <para><b>What could have gone wrong, and what this holds.</b> The loop's iterations share one
/// <see cref="DeterministicRandom"/>, so the whole world's traffic rides on the draws happening in one
/// order, uninterrupted. An iterator suspends and resumes on the same rng in the same state, so it does —
/// but "so it does" is exactly the kind of claim that is true until somebody moves a line. These tests
/// walk the wave BOTH ways and compare it ship by ship, field by field: same ids, same callsigns, same
/// routes, same personalities, same departure and activation times, same initial state to the metre and
/// the metre per second, same plans node for node.</para>
///
/// <para><b>Red proof.</b> Give the iterator its own rng per ship — a plausible-looking "make each ship
/// independent" edit — and <see cref="TheSameEightShipsEitherWay"/> reddens on ship 1 of 8. Drain the
/// enumerable twice into one list (the classic double-enumeration slip) and the count law reddens at
/// sixteen. Neither is caught by any other test in this suite: nothing else asks the traffic planner the
/// same question twice.</para>
///
/// <h3>#161, second pass · ONE SHIP WAS NOT SMALL ENOUGH</h3>
///
/// <para>Ship-by-ship took the boot's worst block from fourteen seconds to four and a quarter, and then the
/// boot's own clock said why it stopped there: the wave is <c>2,743 + 3,081 + 3,137 + 4,234 + 83 + 98 +
/// 435 + 277 ms</c>, which is not eight equal ships but four mid-flight haulers costing SECONDS each and
/// four scheduled departures costing tenths. A four-second block is still a block a browser will put a
/// "page unresponsive" dialog over, so <see cref="TrafficSchedule.GenerateStepByStep"/> hands the same wave
/// over one PIECE OF WORK at a time — a mid-flight hauler is a probe route search, a real route search and
/// a catch-up integration — and <see cref="TrafficSchedule.GenerateShipByShip"/> is now nothing but that
/// walk with the part-way steps filtered out.</para>
///
/// <para><b>What the second pass holds, and its reds.</b> That the steps build the SAME SHIPS
/// (<see cref="TheSameEightShipsHoweverFinelyTheWaveIsWalked"/>); that no ship is planned in one block
/// (<see cref="NoShipIsHandedOverInASinglePiece"/> — collapse the three yields of a mid-flight hauler back
/// into one and it reddens with every ship at one step); and that taking one STEP is materially cheaper
/// than taking one SHIP (<see cref="TakingOneStepIsCheaperThanTakingOneShip"/>), which is the only one of
/// the three that can tell a real handover from a cosmetic one — an iterator that did all three pieces of
/// work and then yielded three times would pass both of the others.</para>
/// </summary>
public class TheSkyIsTheSameSkyOneShipAtATimeTests
{
    /// <summary>The boot's own call, to the seed and the count — so what is pinned here is the wave the
    /// game actually flies, not a look-alike with friendlier numbers.</summary>
    private const ulong BootSeed = 42;
    private const int BootCount = 8;

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(TestTree.Sol);

    [Fact]
    public void TheSameEightShipsEitherWay()
    {
        CircularOrbitEphemeris sol = Sol();

        IReadOnlyList<NpcShip> inOneBreath = TrafficSchedule.Generate(sol, BootSeed, BootCount);

        // …and the way the boot now walks it: pulled one ship at a time, with (in the page) a frame handed
        // back to the browser between each. The frame is what this cannot simulate; the SUSPENSION is, and
        // that is the part that could have moved the rng.
        var oneAtATime = new List<NpcShip>();
        foreach (NpcShip ship in TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount))
        {
            oneAtATime.Add(ship);
        }

        AssertTheSameWave(inOneBreath, oneAtATime);
    }

    /// <summary>The wave, ship by ship and field by field. Shared by the ship-at-a-time law above and the
    /// step-at-a-time law below, so the two finenesses are held to ONE definition of "the same sky" — a
    /// second copy of this comparison is how one of them ends up quietly weaker than the other.</summary>
    private static void AssertTheSameWave(IReadOnlyList<NpcShip> inOneBreath, IReadOnlyList<NpcShip> walked)
    {
        Assert.Equal(inOneBreath.Count, walked.Count);
        for (int i = 0; i < inOneBreath.Count; i++)
        {
            NpcShip a = inOneBreath[i];
            NpcShip b = walked[i];
            string where = $"ship {i + 1} of {inOneBreath.Count}";

            Assert.Equal(a.Id, b.Id);
            Assert.Equal(a.Callsign, b.Callsign);
            Assert.Equal(a.CargoClass, b.CargoClass);
            Assert.Equal(a.OriginId, b.OriginId);
            Assert.Equal(a.DestinationId, b.DestinationId);
            Assert.Equal(a.Personality, b.Personality);
            Assert.Equal(a.CargoUnits, b.CargoUnits);
            Assert.Equal(a.IsPod, b.IsPod);
            Assert.True(a.DepartureTime == b.DepartureTime, $"{where}: departure time moved");
            Assert.True(a.ActivationTime == b.ActivationTime, $"{where}: activation time moved");
            Assert.True(a.EstimatedArrivalTime == b.EstimatedArrivalTime, $"{where}: arrival estimate moved");

            // The state is the ship: where she is and how fast, at the instant the sim adopts her.
            Assert.True(a.InitialState.Position == b.InitialState.Position, $"{where}: she is somewhere else");
            Assert.True(a.InitialState.Velocity == b.InitialState.Velocity, $"{where}: she is going somewhere else");
            Assert.True(a.InitialState.SimTime == b.InitialState.SimTime, $"{where}: her clock reads differently");

            // …and the plan she is flying, node for node. A wave that agreed on the spawn and disagreed on
            // the burns would look identical at t=0 and be a different sky an hour in.
            Assert.Equal(a.Plan.Nodes.Count, b.Plan.Nodes.Count);
            for (int n = 0; n < a.Plan.Nodes.Count; n++)
            {
                Assert.Equal(a.Plan.Nodes[n], b.Plan.Nodes[n]);
            }
        }
    }

    [Fact]
    public void TheSameEightShipsHoweverFinelyTheWaveIsWalked()
    {
        // …and the same question one fineness down. The step walk yields part-way steps carrying no ship at
        // all; the ships that DO come out of it must be the wave, in order, to the metre.
        CircularOrbitEphemeris sol = Sol();

        IReadOnlyList<NpcShip> inOneBreath = TrafficSchedule.Generate(sol, BootSeed, BootCount);

        var stepByStep = new List<NpcShip>();
        foreach (TrafficSchedule.TrafficStep step in TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount))
        {
            if (step.Ship is { } finished)
            {
                stepByStep.Add(finished);
            }
        }

        AssertTheSameWave(inOneBreath, stepByStep);
    }

    [Fact]
    public void NoShipIsHandedOverInASinglePiece()
    {
        // THE SHAPE OF THE HANDOVER. A ship finishes on exactly one step, so a wave handed over ship by ship
        // has exactly `count` steps — which is what this lane set out to stop being true. The mid-flight
        // haulers (the expensive ones; the whole reason for this file) must each arrive over THREE steps.
        CircularOrbitEphemeris sol = Sol();

        var stepsPerShip = new Dictionary<int, int>();
        var finishedOn = new Dictionary<int, int>();
        int total = 0;
        foreach (TrafficSchedule.TrafficStep step in TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount))
        {
            total++;
            stepsPerShip[step.ShipIndex] = stepsPerShip.GetValueOrDefault(step.ShipIndex) + 1;
            if (step.Ship is not null)
            {
                finishedOn[step.ShipIndex] = finishedOn.GetValueOrDefault(step.ShipIndex) + 1;
            }

            Assert.Equal(BootCount, step.ShipCount);
        }

        Assert.Equal(BootCount, stepsPerShip.Count);
        Assert.True(total > BootCount,
            $"the wave came over in {total} steps for {BootCount} ships — that is one step per ship, which is "
            + "the block this lane exists to break up.");

        // Exactly one step per ship CARRIES that ship: a wave that yielded a ship twice would be a doubled
        // sky that every other law in this file would still call identical.
        foreach (int index in stepsPerShip.Keys)
        {
            Assert.Equal(1, finishedOn.GetValueOrDefault(index));
            Assert.Contains(stepsPerShip[index], new[] { 1, 3 });
        }

        Assert.Contains(stepsPerShip.Values, n => n == 3);
    }

    [Fact]
    public void TakingOneStepIsCheaperThanTakingOneShip()
    {
        // THE ONLY ONE OF THE THREE THAT CAN TELL A REAL HANDOVER FROM A COSMETIC ONE. An iterator that did
        // all of a ship's work and then yielded three times would satisfy the shape law above and the same-
        // sky law beside it, and would hand the browser the identical four-second block. So the measurement
        // is a ratio, in the idiom TheWaveIsLazyUntilItIsWalked already uses: the FIRST STEP against the
        // FIRST SHIP, out of the same wave. The first ship is mid-flight — a probe search, a real search and
        // a catch-up — so one step of her is about a third; the 2x margin is the honest half of that.
        CircularOrbitEphemeris sol = Sol();
        _ = TrafficSchedule.Generate(sol, BootSeed, 1); // warm the planner's own first-call costs

        var clock = System.Diagnostics.Stopwatch.StartNew();
        NpcShip firstShip = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount).First();
        long oneShipMs = clock.ElapsedMilliseconds;

        clock.Restart();
        TrafficSchedule.TrafficStep firstStep = TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount).First();
        long oneStepMs = clock.ElapsedMilliseconds;

        Assert.NotNull(firstShip.Id);
        Assert.Null(firstStep.Ship); // the first step of a mid-flight hauler finishes nothing
        Assert.True(oneStepMs * 2 < oneShipMs,
            $"one STEP of the first hauler cost {oneStepMs} ms against {oneShipMs} ms for the whole of her — "
            + "the step handover is not breaking her planning up, so the boot still owes the browser one "
            + "block per ship.");
    }

    [Fact]
    public void ANonsenseCountIsRefusedAtTheASKOfTheSteps()
    {
        // The same eager-check law as the ship walk's, one fineness down: GenerateStepByStep validates and
        // then RETURNS the iterator, so a bad count is refused at the call and not inside somebody's foreach.
        CircularOrbitEphemeris sol = Sol();

        Assert.Throws<ArgumentOutOfRangeException>(() => TrafficSchedule.GenerateStepByStep(sol, BootSeed, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TrafficSchedule.GenerateStepByStep(sol, BootSeed, -3));
    }

    [Fact]
    public void EnumeratingItTwiceIsTwoIdenticalWavesNotOneDoubledOne()
    {
        // The iterator is re-runnable and each run is the wave from the top — which is what lets
        // TrafficSchedule.Generate be nothing but a drain of it, and what would break loudly if somebody
        // ever cached the enumerator instead of the enumerable.
        CircularOrbitEphemeris sol = Sol();
        IEnumerable<NpcShip> wave = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount);

        List<NpcShip> first = [.. wave];
        List<NpcShip> second = [.. wave];

        Assert.Equal(BootCount, first.Count);
        Assert.Equal(BootCount, second.Count);
        Assert.Equal(first.Select(s => s.Id).ToList(), second.Select(s => s.Id).ToList());
        for (int i = 0; i < first.Count; i++)
        {
            Assert.True(first[i].InitialState.Position == second[i].InitialState.Position,
                $"ship {i + 1}: a second walk of the same wave put her somewhere else");
        }
    }

    [Fact]
    public void TheWaveIsLazyUntilItIsWalked()
    {
        // THE WHOLE POINT OF THE HANDOVER: asking for the wave must plan nothing. If GenerateShipByShip
        // planned eagerly and merely handed back a finished list, the page would still owe the browser one
        // fourteen-second block and this lane would have changed nothing at all — while every other test in
        // this file went on passing, because an eager wave IS the same wave.
        //
        // So the measurement is a ratio, not a clock reading: the whole wave against the ask PLUS the first
        // ship out of it. Lazily, that is one ship of eight and by far the cheapest way to get one; eagerly
        // the two are the same number. The margin is enormous (the first ship is under a third of the wave
        // on every payload this has been run on), which is why a coarse 2× is a law and not a flake.
        CircularOrbitEphemeris sol = Sol();
        _ = TrafficSchedule.Generate(sol, BootSeed, 1); // warm the planner's own first-call costs

        var clock = System.Diagnostics.Stopwatch.StartNew();
        _ = TrafficSchedule.Generate(sol, BootSeed, BootCount);
        long wholeWaveMs = clock.ElapsedMilliseconds;

        clock.Restart();
        NpcShip firstShip = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount).First();
        long askAndOneShipMs = clock.ElapsedMilliseconds;

        Assert.NotNull(firstShip.Id);
        Assert.True(askAndOneShipMs * 2 < wholeWaveMs,
            $"asking for the wave and taking ONE ship out of it cost {askAndOneShipMs} ms against "
            + $"{wholeWaveMs} ms for all {BootCount} — the planning is happening at the ASK, so it is still "
            + "one block and the boot still freezes on it.");
    }

    [Fact]
    public void ANonsenseCountIsRefusedAtTheASK()
    {
        // …and the other half of laziness: the argument check must NOT be lazy. An iterator method defers
        // its whole body, throw included, so a `count: 0` would sail past the call site and blow up later
        // inside somebody's foreach. GenerateShipByShip is deliberately a plain method that validates and
        // then RETURNS the iterator, which is the standard shape and the one this pins.
        CircularOrbitEphemeris sol = Sol();

        Assert.Throws<ArgumentOutOfRangeException>(() => TrafficSchedule.GenerateShipByShip(sol, BootSeed, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TrafficSchedule.GenerateShipByShip(sol, BootSeed, -3));
    }
}
