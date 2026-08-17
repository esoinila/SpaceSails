namespace SpaceSails.Core.Tests;

/// <summary>
/// #928 · THE AUTOPILOT FLIES AT A TENTH.
///
/// <para>The owner, playing 2026-08-17: <i>"The autopilot declines too easily … we should fix it to be
/// less picky / expensive … Like we should increase the limit of accepting by 10 X at least without
/// extra cost (or have that effect)."</i> and <i>"Now the autopilot often refuses when it calculates the
/// cost to be too big. If we divide the cost by 10 that we calculate, it should fix it nicely."</i></para>
///
/// <para>The rule that came out of it: an autopilot-flown approach costs ONE TENTH — the rehearsal's
/// pulse count is divided by <see cref="AutopilotRehearsal.EconomyFactor"/> BEFORE the budget verdict,
/// and the flown burns charge the tank at the same tenth. Both ends, or the refusal line's promise
/// ("It won't strand you") would be a sentence about a different ship: dividing only the estimate arms
/// trips the tank cannot finish.</para>
///
/// <h3>The four facts</h3>
/// <list type="number">
/// <item><b>(a) THE ACCEPTED SET IS A STRICT SUPERSET.</b> Over a representative sweep of approaches —
/// the owner's own #146 Titan inbound, moon arrivals at Luna/Enceladus/Europa/Triton/Miranda, the
/// four-Hill-radii Earth capture, and two that can never be flown — nothing that armed before is refused
/// now, at least one journey that was REFUSED is now ACCEPTED (named: the Titan ellipse, 587 raw p → 59
/// charged), and at least one is STILL refused (Phobos, which bleeds without ever capturing). Anti-vacuous
/// in both directions: a factor that accepted everything would fail the second half.</item>
/// <item><b>(b) EVERY ACCEPTED APPROACH IS REALLY FLYABLE.</b> Each accepted case is FLOWN to arrival
/// through the sim with the live loop's own decision logic and the client's charging discipline: it
/// inserts, it never stands down for fuel, the tank never reaches zero, and the flown charge lands inside
/// the quoted estimate plus the reserve. RED by dividing only the estimate — flying the same trips while
/// charging the raw Δv strands them (measured: the Titan run reaches the reserve floor and hands back).</item>
/// <item><b>(e) THE TENTH ACCUMULATES HONESTLY.</b> Ten one-pulse autopilot burns cost ONE pulse, not ten
/// and not zero; five cost one, not zero forever. The flown total is always exactly ⌈raw/10⌉ — the same
/// formula the estimate quotes — which is why (b) can be asserted at all. RED by truncating per burn.</item>
/// <item><b>The constant is the constant.</b> Ten, ceiling, in Core, one place.</item>
/// </list>
///
/// <para>The captain's own pulses are NOT economized — that guard lives with the client strings in
/// <c>TheTenthIsQuotedAndOnlyTheAutopilotsTests</c> (Client.Tests), because the manual sites are client
/// code.</para>
/// </summary>
public class TheAutopilotFliesAtATenthTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheAutopilotFliesAtATenthTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    /// <summary>The starter tank (#262 seeds 500; the rehearsal tests fly the 250 the player used to
    /// carry, which is the harsher budget of the two).</summary>
    private const int Tank = 250;

    private static (Simulator Sim, ICelestialEphemeris Eph) Sol()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        return (new Simulator(eph, timeStepSeconds: 60), eph);
    }

    private static Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    /// <summary>One approach the autopilot could be armed for: a name, the state at the instant of
    /// arming, and the target.</summary>
    private readonly record struct Approach(string Name, ShipState Ship, string Target);

    /// <summary>A ship on a fast parent-centred ellipse, <paramref name="outMeters"/> outward of the
    /// target and NOT co-moving with it — the geometry the owner keeps arming from (the #146 "big Saturn
    /// ellipse"): in range of the target's capture floor, far too fast to be cheap.</summary>
    private static ShipState Ellipse(ICelestialEphemeris eph, string parentId, string targetId, double outMeters, double tangentialMps)
    {
        Vector2d parentPos = eph.Position(parentId, 0);
        Vector2d parentVel = BodyVel(eph, parentId, 0);
        Vector2d targetPos = eph.Position(targetId, 0);
        Vector2d outward = (targetPos - parentPos).Normalized();
        Vector2d tangential = new(-outward.Y, outward.X);
        return new ShipState(targetPos + outward * outMeters, parentVel + tangential * tangentialMps, 0);
    }

    /// <summary>A ship on the target's doorstep, co-moving with it — the cheap arrival a transfer sets up.</summary>
    private static ShipState Alongside(ICelestialEphemeris eph, string parentId, string targetId, double outMeters)
    {
        Vector2d parentPos = eph.Position(parentId, 0);
        Vector2d targetPos = eph.Position(targetId, 0);
        Vector2d outward = (targetPos - parentPos).Normalized();
        return new ShipState(targetPos + outward * outMeters, BodyVel(eph, targetId, 0), 0);
    }

    /// <summary>4 Hill radii out from Earth at 12 km/s across — the OrbitAutopilotTests e2e capture.</summary>
    private static ShipState EarthFromFourHillRadii(ICelestialEphemeris eph)
    {
        CelestialBody earth = eph.Bodies.First(b => b.Id == "earth");
        CelestialBody sun = eph.Bodies.First(b => b.Id == "sun");
        double hill = OrbitRule.HillRadius(earth, sun.Mu);
        return new ShipState(
            eph.Position("earth", 0) + new Vector2d(4 * hill, 0),
            BodyVel(eph, "earth", 0) + new Vector2d(-2000, 12000), 0);
    }

    /// <summary>The sweep: eleven approaches across five wells, from the trivially cheap to the
    /// un-flyable. Fixed and named, so the flip list in the PR body is a measurement and not a claim.</summary>
    private static Approach[] Sweep(ICelestialEphemeris eph) =>
    [
        // The owner's own #146 arm: 2.4 M km out from Titan on a fast Saturn ellipse.
        new("titan · Saturn ellipse 2.4 Mkm @ 8 km/s", Ellipse(eph, "saturn", "titan", 2.4e9, 8000), "titan"),
        new("europa · Jupiter ellipse 1 Mkm @ 10 km/s", Ellipse(eph, "jupiter", "europa", 1e9, 10000), "europa"),
        new("triton · Neptune ellipse 1 Mkm @ 6 km/s", Ellipse(eph, "neptune", "triton", 1e9, 6000), "triton"),
        new("miranda · Uranus ellipse 300 kkm @ 5 km/s", Ellipse(eph, "uranus", "miranda", 3e8, 5000), "miranda"),
        // Cheap arrivals — accepted before the tenth and still accepted after (the superset's floor).
        new("luna · alongside 5000 km", Alongside(eph, "earth", "luna", 5e6), "luna"),
        new("luna · Earth ellipse 200 kkm @ 4 km/s", Ellipse(eph, "earth", "luna", 2e8, 4000), "luna"),
        new("enceladus · alongside 5000 km", Alongside(eph, "saturn", "enceladus", 5e6), "enceladus"),
        new("enceladus · Saturn ellipse 100 kkm @ 15 km/s", Ellipse(eph, "saturn", "enceladus", 1e8, 15000), "enceladus"),
        new("earth · from 4 Hill radii @ 12 km/s", EarthFromFourHillRadii(eph), "earth"),
        // …and two the tenth must NOT wave through: Phobos bleeds without ever capturing, and a Europa
        // arm from 3 M km at 25 km/s never comes back into range inside the horizon.
        new("phobos · alongside 2000 km", Alongside(eph, "mars", "phobos", 2e6), "phobos"),
        new("europa · Jupiter ellipse 3 Mkm @ 25 km/s", Ellipse(eph, "jupiter", "europa", 3e9, 25000), "europa"),
    ];

    /// <summary>The verdict the autopilot gave BEFORE #928, read off a #928 rehearsal: the journey was
    /// promisable iff it captured inside the horizon for a RAW cost within the budget. Sound because the
    /// only thing the tenth changed inside <c>Rehearse</c> is WHEN the budget bail trips: a run that the
    /// old code bailed on has raw &gt; budget here too (it bails ten times later, or captures having spent
    /// more than the budget), and a run neither bails on is bit-identical.</summary>
    private static bool AcceptedBeforeTheTenth(AutopilotRehearsal.RehearsalResult r, int budget) =>
        r.Captured && !r.HorizonReached && r.Pulses <= budget;

    // ---- (a) the accepted set is a strict superset -------------------------------------------------

    [Fact]
    public void TheAcceptedSetIsAStrictSuperset_TitanFlips_PhobosStillRefused()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        int reserve = AutopilotRehearsal.ReservePulses(Tank);
        int budget = Tank - reserve;

        var flipped = new List<string>();
        var stillRefused = new List<string>();
        foreach (Approach a in Sweep(eph))
        {
            AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(a.Ship, eph, sim, a.Target, budget);
            bool before = AcceptedBeforeTheTenth(r, budget);
            bool now = r.Deliverable;
            _out.WriteLine($"{a.Name,-42} raw={r.Pulses,5} charged={r.PulsesCharged,4} " +
                $"captured={r.Captured,-5} exceeded={r.BudgetExceeded,-5} horizon={r.HorizonReached,-5} " +
                $"BEFORE={(before ? "ACCEPT" : "refuse")} NOW={(now ? "ACCEPT" : "refuse")} " +
                $"days={r.SimDurationSeconds / 86400:F2}");

            Assert.True(!before || now,
                $"#928 may only ADD acceptances: '{a.Name}' armed before the tenth and is refused now " +
                $"(raw={r.Pulses}, charged={r.PulsesCharged}, budget={budget}).");
            if (!before && now) flipped.Add(a.Name);
            if (!now) stillRefused.Add(a.Name);
        }

        // Anti-vacuous, both ways: something real flipped, and the gate did not become "always yes".
        Assert.Contains(flipped, n => n.StartsWith("titan ·", StringComparison.Ordinal));
        Assert.Contains(stillRefused, n => n.StartsWith("phobos ·", StringComparison.Ordinal));
        _out.WriteLine($"FLIPPED refused→accepted ({flipped.Count}): {string.Join(" | ", flipped)}");
        _out.WriteLine($"STILL refused ({stillRefused.Count}): {string.Join(" | ", stillRefused)}");
    }

    [Fact]
    public void ThePhobosRefusalIsNotAHairOverTheLine_ItIsStillRefusedAtDoubleTheBudget()
    {
        // The refusals bail the instant the CHARGED cost crosses the budget, so "charged = budget + 1" is
        // the shape of every bail and proves nothing about margin. Give the same arm twice the budget:
        // Phobos still cannot be promised — it bleeds without ever capturing, which is a refusal about
        // the geometry, not about a threshold the tenth happens to sit beside.
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        ShipState ship = Alongside(eph, "mars", "phobos", 2e6);
        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);

        AutopilotRehearsal.RehearsalResult tight = AutopilotRehearsal.Rehearse(ship, eph, sim, "phobos", budget);
        AutopilotRehearsal.RehearsalResult loose = AutopilotRehearsal.Rehearse(ship, eph, sim, "phobos", 2 * budget);
        _out.WriteLine($"PHOBOS at budget {budget}: charged={tight.PulsesCharged} deliverable={tight.Deliverable}; " +
            $"at {2 * budget}: raw={loose.Pulses} charged={loose.PulsesCharged} deliverable={loose.Deliverable} " +
            $"days={loose.SimDurationSeconds / 86400:F2}");

        Assert.False(tight.Deliverable);
        Assert.False(loose.Deliverable, "Phobos is refused for the bleed, not for a hair over the budget.");
    }

    // ---- (b) every accepted approach is really flyable ----------------------------------------------

    /// <summary>What one flown journey cost: the tank's lowest point, what it charged, the raw Δv price,
    /// whether it parked, and whether it ever stood down.</summary>
    private readonly record struct Flight(int Charged, int Raw, int TankLow, bool Inserted, string? StoodDownReason);

    /// <summary>
    /// Fly the armed approach the way <c>Map.CheckArmedInsertion</c> does — the same
    /// <see cref="OrbitRule.AutopilotDecision"/> dispatch, the same parent-chord obstacle, the same
    /// reserve floor, and the same #928 charging: every burn asks
    /// <see cref="AutopilotRehearsal.ChargeForBurn"/> what the tank loses and the RAW cost is what the
    /// accumulator ledger carries. This is the client's arithmetic, executed against the real sim.
    /// </summary>
    private static Flight Fly(Simulator sim, ICelestialEphemeris eph, ShipState ship, string targetId, int maxDays = 40)
    {
        CelestialBody body = eph.Bodies.First(b => b.Id == targetId);
        CelestialBody parent = eph.Bodies.First(b => b.Id == body.ParentId);
        double hill = OrbitRule.HillRadius(body, parent.Mu);
        int reserveFloor = AutopilotRehearsal.ReservePulses(Tank);
        int tank = Tank, tankLow = Tank, rawSpent = 0, charged = 0;
        double end = ship.SimTime + maxDays * 86400.0;

        while (ship.SimTime < end)
        {
            Vector2d bodyPos = eph.Position(body.Id, ship.SimTime);
            Vector2d bodyVel = BodyVel(eph, body.Id, ship.SimTime);
            double keptRadiusCap = OrbitRule.MaxKeptRadiusUnderParent(
                eph.InstantaneousOrbitRadius(body.Id, ship.SimTime), parent);
            OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
                ? null
                : new OrbitRule.ApproachObstacle(
                    eph.Position(parent.Id, ship.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, body, hill, keptRadiusCap))
            {
                case OrbitRule.AutopilotAction.Approach:
                {
                    int cost = OrbitRule.ApproachPulseCost(ship, bodyPos, bodyVel, body, obstacle, hill);
                    int charge = AutopilotRehearsal.ChargeForBurn(rawSpent, cost);
                    if (tank - charge < reserveFloor)
                    {
                        return new Flight(charged, rawSpent, tankLow,
                            false, $"reserve floor reached ({tank} p left, floor {reserveFloor}, next burn {charge} p)");
                    }
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel, body, obstacle, hill);
                    tank -= charge;
                    rawSpent += cost;
                    charged += charge;
                    tankLow = Math.Min(tankLow, tank);
                    break;
                }

                case OrbitRule.AutopilotAction.Insert:
                {
                    int cost = OrbitRule.PulseCost(ship, bodyPos, bodyVel, body);
                    int charge = AutopilotRehearsal.ChargeForBurn(rawSpent, cost);
                    if (charge > tank)
                    {
                        return new Flight(charged, rawSpent, tankLow,
                            false, $"insertion needs {charge} p, only {tank} left");
                    }
                    ship = OrbitRule.Insert(ship, bodyPos, bodyVel, body);
                    tank -= charge;
                    rawSpent += cost;
                    charged += charge;
                    tankLow = Math.Min(tankLow, tank);
                    return new Flight(charged, rawSpent, tankLow, true, null);
                }
            }

            ship = sim.Step(ship);
        }

        return new Flight(charged, rawSpent, tankLow, false, $"never captured within {maxDays} days");
    }

    [Fact]
    public void EveryAcceptedApproachIsFlownToAPark_AndTheTankNeverRunsDry()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        int reserve = AutopilotRehearsal.ReservePulses(Tank);
        int budget = Tank - reserve;
        int flown = 0;

        foreach (Approach a in Sweep(eph))
        {
            AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(a.Ship, eph, sim, a.Target, budget);
            if (!r.Deliverable)
            {
                continue; // a refusal is never armed — there is nothing to fly
            }

            Flight f = Fly(sim, eph, a.Ship, a.Target);
            flown++;
            _out.WriteLine($"{a.Name,-42} quoted={r.PulsesCharged,4} flownCharged={f.Charged,4} flownRaw={f.Raw,5} " +
                $"tankLow={f.TankLow,4} parked={f.Inserted} {f.StoodDownReason}");

            Assert.True(f.Inserted,
                $"'{a.Name}' was ARMED (quoted ≈{r.PulsesCharged} p) but the flight never parked: {f.StoodDownReason}");
            Assert.Null(f.StoodDownReason);
            Assert.True(f.TankLow > 0,
                $"'{a.Name}': the tank hit {f.TankLow} p mid-flight — an armed trip must never strand the captain.");
            Assert.True(f.Charged <= r.PulsesCharged + reserve,
                $"'{a.Name}': flown charge {f.Charged} p overran the quoted {r.PulsesCharged} p plus the " +
                $"{reserve} p reserve. The estimate and the burn must be the same arithmetic (#928).");
            // …and the flown charge really is the tenth of the flown Δv, not a coincidence.
            Assert.Equal(AutopilotRehearsal.Charged(f.Raw), f.Charged);
        }

        Assert.True(flown >= 8, $"the sweep should have flown most of its approaches; flew {flown}.");
    }

    // ---- (e) the tenth accumulates honestly ---------------------------------------------------------

    [Fact]
    public void TheConstantIsTenAndTheChargeIsACeiling()
    {
        Assert.Equal(10, AutopilotRehearsal.EconomyFactor);
        Assert.Equal(0, AutopilotRehearsal.Charged(0));
        Assert.Equal(1, AutopilotRehearsal.Charged(1));    // a burn that happens is never free
        Assert.Equal(1, AutopilotRehearsal.Charged(10));
        Assert.Equal(2, AutopilotRehearsal.Charged(11));
        Assert.Equal(59, AutopilotRehearsal.Charged(587)); // the owner's Titan inbound
    }

    [Fact]
    public void TenAutopilotPulsesCostOne_FiveCostOne_AndNeverZeroForever()
    {
        // Ten one-pulse autopilot burns in a row cost the tank ONE pulse — not ten (charging ⌈cost/10⌉
        // per burn) and not zero (truncating per burn, the "free forever" bug the accumulator exists to
        // prevent). And the running total is charged the instant the first burn fires, so five burns have
        // cost one, not nothing.
        int raw = 0, charged = 0;
        var after = new int[11];
        for (int i = 1; i <= 10; i++)
        {
            charged += AutopilotRehearsal.ChargeForBurn(raw, 1);
            raw += 1;
            after[i] = charged;
        }

        Assert.Equal(1, after[1]);
        Assert.Equal(1, after[5]);   // five pulses have cost ONE, not zero forever
        Assert.Equal(1, after[10]);  // ten pulses cost ONE, not ten
        Assert.Equal(10, raw);

        // An eleventh raw pulse is the second charged one — ⌈11/10⌉.
        charged += AutopilotRehearsal.ChargeForBurn(raw, 1);
        Assert.Equal(2, charged);
    }

    [Fact]
    public void TheFlownChargeIsAlwaysTheCeilingOfTheFlownRaw_WhateverTheBurnSizes()
    {
        // The invariant the whole promise rests on: however the journey's Δv is chopped into burns, the
        // accumulated charge equals ⌈raw/10⌉ — the very formula RehearsalResult.PulsesCharged quotes. So
        // the estimate on the panel and the tank after arrival can never tell two different stories.
        int[][ ] shapes =
        [
            [1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
            [7, 3],
            [37, 4, 91, 2, 1],
            [250],
            [9, 1, 9, 1],
            [3, 3, 3, 3, 3, 3, 3],
        ];

        foreach (int[] burns in shapes)
        {
            int raw = 0, charged = 0;
            foreach (int b in burns)
            {
                charged += AutopilotRehearsal.ChargeForBurn(raw, b);
                raw += b;
            }
            Assert.Equal(AutopilotRehearsal.Charged(raw), charged);
        }
    }
}
