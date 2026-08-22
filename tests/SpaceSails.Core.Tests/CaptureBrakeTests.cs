namespace SpaceSails.Core.Tests;

/// <summary>
/// #957 — <b>THE AUTOPILOT LAYS A STEP INSTEAD OF COMPLAINING.</b> The owner, having flown right up to
/// The Rusty Roadstead and pressed Dock: <i>"It should just add the necessary braking step on the plot
/// path and not complain. It is annoying — nobody will ever play the 'let's fly next to it really quiet so
/// autopilot will agree'. That takes forever and ruins the fun of play."</i>
///
/// <para>The gate below is the whole feature in one shape: on a geometry where the plain arm-time
/// rehearsal REFUSES, <see cref="CaptureBrake.Solve"/> must find a single burn that turns the refusal into
/// a promise — and the promise must be the rehearsal's own, flown WITH the burn, inside the same
/// tank-minus-reserve budget. Both halves are asserted in the same test on purpose: that is the guard's
/// proof that it can fail. Take the correction away (or aim it only along −v_rel, which cannot move a
/// flyby's miss distance) and the first assertion still passes while the second goes red.</para>
///
/// <para>And the refusal is kept honest at the other end: a geometry no candidate on the ladder fixes
/// returns null rather than a made-up capture, and an empty budget buys nothing at all — the reserve is
/// never spent to make a click feel good.</para>
/// </summary>
public class CaptureBrakeTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public CaptureBrakeTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const int Tank = 500;
    private const double SearchHorizon = 30 * 86400.0;

    private static (Simulator Sim, ICelestialEphemeris Eph) Sol()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        return (new Simulator(eph, timeStepSeconds: 60), eph);
    }

    private static Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    /// <summary>The owner's #957 read, reconstructed: inbound on a station, a few million km out and
    /// several km/s hot in its frame, on a line that goes BY it rather than to it.</summary>
    private static ShipState InboundPast(ICelestialEphemeris eph, string station, double missMeters, double extraSpeed)
    {
        Vector2d bp = eph.Position(station, 0);
        Vector2d bv = BodyVel(eph, station, 0);
        Vector2d along = bv / bv.Length;
        var across = new Vector2d(-along.Y, along.X);
        return new ShipState(bp + across * missMeters - along * 2.0e10, bv + along * extraSpeed, 0);
    }

    private static int Budget() => Tank - AutopilotRehearsal.ReservePulses(Tank);

    [Fact]
    public void WhereThePlainArmRefuses_OneBurnMakesItAPromise()
    {
        // THE #957 CASE. The Rusty Roadstead (a μ=0 sun-parented berth), ~3.5 M km off the track at
        // 12.4 km/s in its frame. The plain rehearsal coasts the whole horizon and never reaches the
        // dock envelope — "can't verify a capture from here", the useless refusal the owner got.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", missMeters: 3.5e9, extraSpeed: 12_400);
        int budget = Budget();

        AutopilotRehearsal.RehearsalResult plain =
            AutopilotRehearsal.Rehearse(ship, eph, sim, "derelict-roadster", budget, maxHorizonSeconds: SearchHorizon);
        _out.WriteLine($"PLAIN: deliverable={plain.Deliverable} captured={plain.Captured} " +
            $"horizon={plain.HorizonReached} budgetExceeded={plain.BudgetExceeded} charged={plain.PulsesCharged}");

        // Half one: the old behaviour really does refuse here. If this ever stops being true the test
        // below is proving nothing, and it will say so.
        Assert.False(plain.Deliverable,
            "The premise of #957 is a geometry the plain arm refuses; this one no longer does, so the guard is blind.");

        // Half two: and the search turns that refusal into a flown promise.
        CaptureBrake.Solution? solved =
            CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", budget, maxHorizonSeconds: SearchHorizon);
        Assert.NotNull(solved);
        CaptureBrake.Solution s = solved.Value;
        _out.WriteLine($"FIX: aim={s.Aim} dv={s.DeltaVMetersPerSecond:F0} m/s raw={s.RawPulses} charged={s.ChargedPulses}");

        Assert.True(s.Rehearsal.Deliverable, "The returned solution must carry the rehearsal that proves it.");
        Assert.True(s.Rehearsal.Captured);
        Assert.False(s.Rehearsal.HorizonReached);
    }

    [Fact]
    public void TheBurnItLays_IsTheBurnTheShipWillFly()
    {
        // The schedule handed back is the one the live armed loop executes (ApplyTransferBurn fires
        // exactly these epochs), so it must be a single impulse at the stated time with the stated Δv —
        // no second, unquoted burn hiding in it.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", 3.5e9, 12_400);
        CaptureBrake.Solution s =
            CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", Budget(), maxHorizonSeconds: SearchHorizon)!.Value;

        Assert.Single(s.Schedule.Burns);
        Assert.Equal(s.SimTime, s.Schedule.Burns[0].SimTime);
        Assert.Equal(s.DeltaV, s.Schedule.Burns[0].DeltaV);
        Assert.Equal(s.DeltaVMetersPerSecond, s.DeltaV.Length, 6);
        Assert.Equal(ship.SimTime, s.SimTime); // "now" is the epoch with the most leverage
    }

    [Fact]
    public void ItIsPricedWithTheSameKernelTheFlightSpends_AndQuotedAtTheTenth()
    {
        // The named bug class again: the number the click quotes must be the number the tank loses.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", 3.5e9, 12_400);
        int budget = Budget();
        CaptureBrake.Solution s =
            CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", budget, maxHorizonSeconds: SearchHorizon)!.Value;

        Assert.Equal(OrbitRule.PulsesFor(s.DeltaV.Length, ship.Velocity.Length), s.RawPulses);
        Assert.Equal(s.Rehearsal.PulsesCharged, s.ChargedPulses);
        Assert.True(s.ChargedPulses <= budget,
            $"A laid burn must fit the budget the plain arm rehearses against; {s.ChargedPulses} > {budget}.");
        Assert.True(AutopilotRehearsal.Charged(s.RawPulses) <= budget);
    }

    [Fact]
    public void WhenNothingOnTheLadderFlies_ItRefusesInsteadOfPretending()
    {
        // Far off the track and barely moving in the station's frame: no single impulse on the ladder
        // reaches it inside the search horizon. The honest answer is null — the captain then gets a
        // refusal WITH NUMBERS (ArrivalStepRule.RefusalText), not a promise that was never flown.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "the-space-bar", missMeters: 8.0e9, extraSpeed: 4_000);

        AutopilotRehearsal.RehearsalResult plain =
            AutopilotRehearsal.Rehearse(ship, eph, sim, "the-space-bar", Budget(), maxHorizonSeconds: SearchHorizon);
        Assert.False(plain.Deliverable);

        Assert.Null(CaptureBrake.Solve(ship, eph, sim, "the-space-bar", Budget(), maxHorizonSeconds: SearchHorizon));
    }

    [Fact]
    public void AnEmptyBudget_BuysNothing_SoTheReserveIsNeverSpentToMakeAClickFeelGood()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", 3.5e9, 12_400);

        Assert.Null(CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", budgetPulses: 0, maxHorizonSeconds: SearchHorizon));
        Assert.Null(CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", budgetPulses: -5, maxHorizonSeconds: SearchHorizon));
    }

    [Fact]
    public void AParentlessBody_IsNotAJourney()
    {
        // The sun has no parent; the rehearsal refuses to treat it as an arrival, and so does the search.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", 3.5e9, 12_400);
        Assert.Null(CaptureBrake.Solve(ship, eph, sim, "sun", Budget(), maxHorizonSeconds: SearchHorizon));
        Assert.Null(CaptureBrake.Solve(ship, eph, sim, "no-such-body", Budget(), maxHorizonSeconds: SearchHorizon));
    }

    [Fact]
    public void TheLadderIsSmallestFirst_AndBounded()
    {
        // Cheapest-first is what makes "the first candidate that flies" also "the cheapest that flies";
        // the candidate cap is the wall-time guard for a WASM button press.
        for (int i = 1; i < CaptureBrake.ShedFractions.Length; i++)
        {
            Assert.True(CaptureBrake.ShedFractions[i] > CaptureBrake.ShedFractions[i - 1]);
        }
        Assert.Equal(CaptureBrake.Aim.Brake, CaptureBrake.Aims[0]); // the owner's word gets first refusal
        Assert.Equal(3, CaptureBrake.Aims.Length);
        Assert.True(CaptureBrake.MaxCandidates >= CaptureBrake.Aims.Length);
    }

    [Fact]
    public void TheWordsSayWhatWasLaid_AndWhatItCosts()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = InboundPast(eph, "derelict-roadster", 3.5e9, 12_400);
        CaptureBrake.Solution s =
            CaptureBrake.Solve(ship, eph, sim, "derelict-roadster", Budget(), maxHorizonSeconds: SearchHorizon)!.Value;

        string step = CaptureBrake.StepLine(s, "The Rusty Roadstead");
        Assert.Contains(CaptureBrake.AimWord(s.Aim), step, StringComparison.Ordinal);
        Assert.Contains("The Rusty Roadstead", step, StringComparison.Ordinal);
        Assert.Contains($"≈{s.ChargedPulses} p", step, StringComparison.Ordinal);

        string added = CaptureBrake.AddedText(s, "The Rusty Roadstead");
        Assert.Contains("Not declining", added, StringComparison.Ordinal);
        Assert.Contains(ArrivalStepRule.FormatSpeed(s.DeltaVMetersPerSecond), added, StringComparison.Ordinal);
    }
}
