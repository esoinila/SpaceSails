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
        //
        // And the shape is one of exactly two. A SCHEDULED departure is one route search and nothing else,
        // so she arrives on a single step and there is nothing to break up. A MID-FLIGHT hauler — the whole
        // reason for this file — is a probe search, a real search, at least one slice of catch-up
        // integration and the step that finishes her: four at the very least, and more the longer she has
        // already been flying. Anything in between means a piece of her planning has been put back into one
        // block.
        foreach (int index in stepsPerShip.Keys)
        {
            Assert.Equal(1, finishedOn.GetValueOrDefault(index));
            Assert.True(stepsPerShip[index] == 1 || stepsPerShip[index] >= 4,
                $"ship {index + 1} came over in {stepsPerShip[index]} steps — a hauler is either one scheduled "
                + "route search or a mid-flight plan broken into at least four pieces, never anything between.");
        }

        // …and this wave genuinely contains the expensive kind: `count * 6 / 10` of the eight are mid-flight
        // by construction, so a run where they all arrived in one step would be this law reading a wave that
        // has nothing in it to break up.
        Assert.True(stepsPerShip.Values.Count(n => n >= 4) >= BootCount * 6 / 10,
            "fewer mid-flight haulers than the planner builds — this law is reading the wrong wave.");
    }

    [Fact]
    public void TakingOneStepIsCheaperThanTakingOneShip()
    {
        // THE ONLY ONE OF THE THREE THAT CAN TELL A REAL HANDOVER FROM A COSMETIC ONE. An iterator that did
        // all of a ship's work and then yielded three times would satisfy the shape law above and the
        // same-sky law beside it, and would hand the browser the identical four-second block.
        //
        // #1236 · IT USED TO ASK A STOPWATCH, AND A STOPWATCH IS NOT A PROPERTY OF THE CODE. It compared
        // `oneStepMs * 2 < oneShipMs` and went red on CI on a DOC-ONLY PR (#1235: "one STEP of the first
        // hauler cost 323 ms against 577 ms"), because the first step of the first hauler pays the JIT and
        // tiering warm-up for the whole route planner. Green here, green on its own lane's CI, red on a
        // shared runner on a commit that changed a markdown file: a guard whose verdict is about the host
        // rather than about the handover.
        //
        // So the same question is asked by COUNTING THE WORK instead. Every piece of planning in here —
        // both route searches and every step of the catch-up integration — has to ask the ephemeris where
        // a body is, so a counting ephemeris measures the work in units the code actually performs, with
        // no clock in it at all. It is the stronger instrument as well as the stable one: a cosmetic
        // handover does not merely fail the ratio, it lands on EXACT EQUALITY, because the first step and
        // the first ship would have done the identical work.
        var sol = new CountingEphemeris(Sol());

        long beforeStep = sol.Queries;
        TrafficSchedule.TrafficStep firstStep = TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount).First();
        long oneStepQueries = sol.Queries - beforeStep;

        long beforeShip = sol.Queries;
        NpcShip firstShip = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount).First();
        long oneShipQueries = sol.Queries - beforeShip;

        // The timings stay — as a DIAGNOSTIC, printed, never asserted. They are the thing #161 actually
        // cares about and they are worth having in the log; they are not worth failing a build over.
        var clock = System.Diagnostics.Stopwatch.StartNew();
        _ = TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount).First();
        long oneStepMs = clock.ElapsedMilliseconds;
        clock.Restart();
        _ = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount).First();
        long oneShipMs = clock.ElapsedMilliseconds;
        Console.WriteLine(
            $"[#161 diagnostic] first step {oneStepQueries} ephemeris queries / {oneStepMs} ms · "
            + $"first ship {oneShipQueries} queries / {oneShipMs} ms");

        Assert.NotNull(firstShip.Id);
        Assert.Null(firstStep.Ship); // the first step of a mid-flight hauler finishes nothing

        // The premise, out loud: this wave really does open on a mid-flight hauler, so there is something
        // here to break up. A wave that opened on a scheduled departure would make the law below trivially
        // true about a ship that is one step by design.
        Assert.True(oneShipQueries > 0 && oneStepQueries > 0,
            "neither the first step nor the first ship asked the ephemeris anything — this law is counting "
            + "a wave that does no planning at all.");

        Assert.True(oneStepQueries * 2 < oneShipQueries,
            $"one STEP of the first hauler did {oneStepQueries} units of planning work against "
            + $"{oneShipQueries} for the whole of her — the step handover is not breaking her planning up, "
            + "so the boot still owes the browser one block per ship. (Work is counted as ephemeris "
            + "queries, never milliseconds: see #1236.)");
    }

    [Fact]
    public void NoSingleStepCarriesMoreThanItsShareOfAHauler()
    {
        // #1236 · THE OTHER HALF OF THE SAME QUESTION, AND THE HALF THE OLD RATIO COULD NOT ASK AT ALL: not
        // "is the FIRST step small" but "is EVERY step small". A handover that broke the probe search out
        // and left the real search and the whole catch-up in one block would pass the law above — the first
        // step really would be cheap — and would still hand the browser the block #161 exists to kill.
        //
        // Counted, per step, for every hauler in the wave: the most expensive single step of a mid-flight
        // hauler may not be half of her. The catch-up is sliced at CatchUpStepsPerSlice, so in a real
        // handover her worst step is one slice out of several and this is not close; collapse the slicing
        // and her catch-up becomes one step worth most of the ship, which is exactly the red.
        var sol = new CountingEphemeris(Sol());

        var costs = new Dictionary<int, List<long>>();
        long last = sol.Queries;
        foreach (TrafficSchedule.TrafficStep step in TrafficSchedule.GenerateStepByStep(sol, BootSeed, BootCount))
        {
            long now = sol.Queries;
            costs.TryAdd(step.ShipIndex, []);
            costs[step.ShipIndex].Add(now - last);
            last = now;
        }

        Assert.Equal(BootCount, costs.Count);

        // A mid-flight hauler is the one that arrives over several steps (NoShipIsHandedOverInASinglePiece
        // pins that shape); a scheduled departure is one step and has nothing to divide.
        var haulers = costs.Where(c => c.Value.Count > 1).ToList();
        Assert.True(haulers.Count >= BootCount * 6 / 10,
            $"only {haulers.Count} of {BootCount} ships arrived over more than one step — this law is "
            + "reading a wave with no mid-flight hauler in it to break up.");

        foreach ((int index, List<long> perStep) in haulers)
        {
            long whole = perStep.Sum();
            long worst = perStep.Max();
            Assert.True(whole > 0,
                $"hauler {index + 1} did no planning work at all — nothing here is being measured.");
            Assert.True(worst * 2 <= whole,
                $"hauler {index + 1} came over in {perStep.Count} steps, but her worst single step did "
                + $"{worst} of her {whole} units of planning work — more than half of her in one block, "
                + "which is the block #161 exists to break up. (Work is counted as ephemeris queries, "
                + "never milliseconds: see #1236.)");
        }
    }

    /// <summary>
    /// #1236 · WORK, COUNTED — the instrument that replaced the stopwatch in this file.
    ///
    /// <para>Every piece of planning the traffic schedule does — a probe route search, a real route search,
    /// each step of a catch-up integration — has to ask the ephemeris where a body is. Counting those asks
    /// measures the work in units the code performs, so a law written on it is a statement about the
    /// handover rather than about how busy the runner was. It delegates everything and changes no number:
    /// the wave that comes out through this wrapper is the wave that comes out without it, which
    /// <see cref="TheCountingEphemerisIsTheSameSky"/> holds.</para>
    /// </summary>
    private sealed class CountingEphemeris(ICelestialEphemeris inner) : ICelestialEphemeris
    {
        public long Queries { get; private set; }

        public IReadOnlyList<CelestialBody> Bodies => inner.Bodies;

        public SpaceSails.Contracts.TrafficDefinition? Traffic => inner.Traffic;

        public Vector2d Position(string bodyId, double simTime)
        {
            Queries++;
            return inner.Position(bodyId, simTime);
        }

        public double InstantaneousOrbitRadius(string bodyId, double simTime)
            => inner.InstantaneousOrbitRadius(bodyId, simTime);
    }

    [Fact]
    public void TheCountingEphemerisIsTheSameSky()
    {
        // The instrument's own premise: a wrapper that changed the wave would make every law written on it
        // a law about something else. Same ids, same places, to the metre.
        IReadOnlyList<NpcShip> plain = TrafficSchedule.Generate(Sol(), BootSeed, BootCount);
        IReadOnlyList<NpcShip> counted = TrafficSchedule.Generate(new CountingEphemeris(Sol()), BootSeed, BootCount);

        AssertTheSameWave(plain, counted);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(128)]
    [InlineData(1_000)]
    [InlineData(100_000)]
    public void TheSameRunHoweverItIsSliced(int stepsPerSlice)
    {
        // THE THIRD PIECE OF A MID-FLIGHT HAULER — the catch-up integration over the 20–70 days she has
        // already been flying — is the longest single block left once the two route searches are yielded
        // around, so it is sliced too (Simulator.RunSliceBySlice). Slicing an integration is the kind of
        // change that looks free and is not: a run cut into two shorter Runs computes its second end time
        // off an ACCUMULATED SimTime, and a last-bit difference there decides whether the loop takes one
        // more step — two hours of a hauler's flight, and a different sky.
        //
        // Red proof: replace RunSliceBySlice's body with two half-duration Run calls and this reddens on
        // every slice size, on SimTime first.
        //
        // THE DURATION IS NOT A ROUND ONE, AND THAT IS THE WHOLE TEST. The first draft of this law used a
        // flat forty days — 480 steps of 7,200 s, exactly — and the two-half-Runs red PASSED it, because
        // two halves of an exact multiple are themselves exact multiples and the step count comes out the
        // same. A law that cannot fail on the wrong implementation is not a law, so the duration now has an
        // awkward tail on it, which is what a real catch-up (`baseSimTime - virtualDeparture`, an arbitrary
        // double) always has: the halves then land mid-step, the second run's end time is computed off an
        // accumulated clock, and the run takes 482 steps where it should take 481.
        CircularOrbitEphemeris sol = Sol();
        NpcShip hauler = TrafficSchedule.Generate(sol, BootSeed, BootCount)[0];
        var sim = new Simulator(sol, timeStepSeconds: 7200);
        double duration = (40 * 86400) + 1234.5;

        foreach (ManeuverPlan? plan in new[] { hauler.Plan, null })
        {
            ShipState inOneRun = sim.Run(hauler.InitialState, duration, plan);
            List<ShipState> sliced = [.. sim.RunSliceBySlice(hauler.InitialState, duration, plan, stepsPerSlice)];
            ShipState answer = sliced[^1];
            string where = $"slice {stepsPerSlice}, plan {(plan is null ? "none" : $"{plan.Nodes.Count} nodes")}";

            Assert.True(inOneRun.SimTime == answer.SimTime, $"{where}: her clock reads differently");
            Assert.True(inOneRun.Position == answer.Position, $"{where}: she is somewhere else");
            Assert.True(inOneRun.Velocity == answer.Velocity, $"{where}: she is going somewhere else");
            Assert.True(inOneRun.Charge == answer.Charge, $"{where}: she is carrying a different charge");

            // …and it really did hand the caller something to breathe on. A slice size under the run's own
            // step count must produce more than the one final state, or "sliced" is a word and not a fact.
            if (stepsPerSlice <= 100)
            {
                Assert.True(sliced.Count > 1,
                    $"{where}: the whole run came back as one element — nothing was handed back mid-run.");
            }
        }
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
        // So the measurement is a ratio: the whole wave against the ask PLUS the first ship out of it.
        // Lazily, that is one ship of eight and by far the cheapest way to get one; eagerly the two are the
        // same number.
        //
        // #1236 · AND IT IS COUNTED, NOT TIMED. This was a Stopwatch ratio too, the twin of the one that
        // went red on a doc-only PR next door, and it would have gone the same way on a busy enough runner.
        // Work is ephemeris queries now — the asks the planner really makes — so the verdict is a property
        // of the code. It is the sharper instrument as well: an EAGER wave does not merely lose the 2x
        // margin, it makes the ASK cost the whole wave and the walk cost nothing, which the first assertion
        // below catches outright.
        var sol = new CountingEphemeris(Sol());

        long beforeAsk = sol.Queries;
        IEnumerable<NpcShip> unwalked = TrafficSchedule.GenerateShipByShip(sol, BootSeed, BootCount);
        long askQueries = sol.Queries - beforeAsk;

        Assert.Equal(0, askQueries);   // the ask itself plans NOTHING — the whole point of the handover

        long beforeFirst = sol.Queries;
        NpcShip firstShip = unwalked.First();
        long oneShipQueries = sol.Queries - beforeFirst;

        long beforeWave = sol.Queries;
        _ = TrafficSchedule.Generate(sol, BootSeed, BootCount);
        long wholeWaveQueries = sol.Queries - beforeWave;

        Assert.NotNull(firstShip.Id);
        Assert.True(oneShipQueries * 2 < wholeWaveQueries,
            $"asking for the wave and taking ONE ship out of it did {oneShipQueries} units of planning work "
            + $"against {wholeWaveQueries} for all {BootCount} — the planning is happening at the ASK, so it "
            + "is still one block and the boot still freezes on it. (Work is counted as ephemeris queries, "
            + "never milliseconds: see #1236.)");
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
