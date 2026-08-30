using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #957 — <b>A BERTH NO SHIP CAN RIDE ALONGSIDE IS A BERTH NO AUTOPILOT CAN DOCK AT.</b>
///
/// <para>The owner, flying up to The Rusty Roadstead and pressing Dock: <i>"Here the autopilot refuses
/// to Dock to Rustys… why… it should not. … I mean I go right next to it here… the criteria somehow
/// horribly wrong that it refuses here. … nobody will ever play the 'let's fly next to it really quiet
/// so autopilot will agree'."</i> Two minutes later, on the same course thirteen sim-days on, the
/// identical press was ACCEPTED. Nothing about the plan had changed.</para>
///
/// <para><b>What was wrong was not the autopilot. It was the rail under the bar.</b> The Rusty Roadstead
/// (<c>the-space-bar</c>) sat 12,000 km off Mars on a <c>7200</c>-second rail. Kepler's period for that
/// radius about Mars is <b>39,910 s</b>. The literal therefore whipped the berth round its parent at
/// 2πr/T = <b>10,472 m/s</b> where a ship in that orbit flies at √(μ/r) = <b>1,889 m/s</b> — nearly four
/// times the local escape speed, which is to say the bar was not in orbit at all. And
/// <see cref="DockRule.MatchSpeed"/> is 8,000 m/s: a ship that had matched <i>Mars itself</i>, perfectly,
/// with the engine cold, still read 10.5 km/s at the berth and was over the clamp limit. Flying quiet
/// made the number WORSE, not better. The only way under 8 km/s was to be hot in Mars' frame <i>and</i>
/// pointed whichever way the berth happened to be whipping at that instant — which is exactly why the
/// same course was refused once and accepted thirteen days later. Phase luck, precisely as reported.</para>
///
/// <para>The owner's own panel had been printing the contradiction the whole time and nobody read it:
/// <c>rel 12.4 km/s</c> to the berth on one line and <c>Nearest: Mars … 2.6 km/s rel</c> on the next.
/// For a berth on a gravity rail those two can differ by at most its orbital speed — 1.9 km/s. They
/// differed by ten. <c>cinder-roost</c> (8,000 s where Kepler says 20,252) and <c>the-tilt</c> (14,000 s
/// where Kepler says 59,065) carried the same wrong literal; every other body in every shipped scenario
/// agrees with Kepler to under one per cent.</para>
///
/// <para><b>The law.</b> A rail is a claim about gravity, so it must be one: for every body in the
/// Sol-family scenarios that circles a parent with mass, the speed its rail carries it at must be the
/// speed Newton gives at that radius. That is the only condition under which a ship can hold station
/// beside it — and holding station beside it is the whole of docking. The consequence is asserted
/// separately and in the owner's own words: at every dockable haven, a ship flying the berth's own orbit
/// must be inside the clamp's match speed of the berth.</para>
///
/// <para><b>Proving it can fail</b> (house law): put <c>7200</c> back on <c>the-space-bar</c> in
/// <c>scenarios/sol.json</c> and <see cref="EveryRailInTheSolFamilyIsGravitys"/> goes red at
/// <c>the-space-bar</c> (10,472 vs 1,889 m/s), <see cref="FlyingQuietAlongsideAHaven_IsInsideTheClamp"/>
/// goes red (8,583 m/s of rel speed for a ship that is doing everything right), and
/// <see cref="AtTheOwnersRead_TheAutopilotTakesTheDock"/> goes red with the refusal he photographed.
/// Verified red on 8,000 / 7,200 / 14,000 before the literals were corrected.</para>
///
/// <para><b>Why <c>wheel.json</c> is not held to it</b>, and why that is not a loophole: the Wheel of the
/// World says of itself that <i>"the spoke is not gravity's work — something older holds it"</i>, so its
/// planets are supposed to ride a rail no orbit explains. The exemption is bounded by
/// <see cref="TheOneScenarioAllowedANonGravityRail_BerthsNobodyOnIt"/>: it may bend the rail only
/// because there is no berth on it to dock at.</para>
/// </summary>
public class EveryBerthRidesAGravityRailTests
{
    /// <summary>The scenarios that claim to be gravity — sol.json says of itself "circular coplanar
    /// orbits at true radii and periods", and the other two are cuts of the same system.</summary>
    public static TheoryData<string> GravityScenarios() => ["sol.json", "sol-eu.json", "oops.json"];

    /// <summary>How far a hand-entered period may sit from Kepler's. The shipped rails use real-world
    /// sidereal periods rather than radius-derived ones, so Luna (0.5%) and Saturn (0.7%) are honestly
    /// a little off; 2% keeps them while leaving no room at all for the 5.5×, 2.5× and 4.2× errors this
    /// guard was written for.</summary>
    private const double Tolerance = 0.02;

    private static ScenarioDefinition Load(string file) =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", file));

    /// <summary>The speed the rail carries the body at: one circumference per period. Retrograde rails
    /// carry a negative period (Triton really does go backwards); the direction is not the law's
    /// business, only the speed.</summary>
    private static double RailSpeed(BodyDefinition b) => Math.Abs(Math.Tau * b.OrbitRadiusM / b.OrbitPeriodS);

    /// <summary>The speed Newton gives for a circular orbit of that radius about that parent — and so
    /// the speed a ship that wants to ride alongside must fly.</summary>
    private static double CircularSpeed(double parentMu, double radius) => Math.Sqrt(parentMu / radius);

    [Theory]
    [MemberData(nameof(GravityScenarios))]
    public void EveryRailInTheSolFamilyIsGravitys(string file)
    {
        ScenarioDefinition scenario = Load(file);
        Dictionary<string, double> mu = scenario.Bodies.ToDictionary(b => b.Id, b => b.Mu);
        var wrong = new List<string>();
        int checkedRails = 0;

        foreach (BodyDefinition body in scenario.Bodies)
        {
            if (body.ParentId is null || body.OrbitPeriodS == 0 || body.OrbitRadiusM <= 0
                || !mu.TryGetValue(body.ParentId, out double parentMu) || parentMu <= 0)
            {
                continue; // the sun, and anything pinned to a mass-less parent: no gravity to obey
            }

            checkedRails++;
            // Kepler's third law on the semi-major axis: true of a circle and of an ellipse alike, so a
            // future cycler on an eccentric rail is judged by the same line as the shipped circles.
            double kepler = Math.Tau * Math.Sqrt(body.OrbitRadiusM * body.OrbitRadiusM * body.OrbitRadiusM / parentMu);
            if (Math.Abs(Math.Abs(body.OrbitPeriodS) - kepler) > Tolerance * kepler)
            {
                wrong.Add($"{body.Id}: period {Math.Abs(body.OrbitPeriodS):F0} s but Kepler says {kepler:F0} s at "
                    + $"{body.OrbitRadiusM:E3} m — the rail carries it at {RailSpeed(body):F0} m/s where Newton "
                    + $"gives {CircularSpeed(parentMu, body.OrbitRadiusM):F0} m/s");
            }
        }

        Assert.True(checkedRails > 0, $"{file} has no gravity rails to judge — the guard would be blind.");
        Assert.True(wrong.Count == 0,
            $"#957: a rail is a claim about gravity. In {file} these do not obey it, so no ship can ride "
            + $"alongside and no autopilot can dock:\n  " + string.Join("\n  ", wrong));
    }

    [Theory]
    [MemberData(nameof(GravityScenarios))]
    public void FlyingQuietAlongsideAHaven_IsInsideTheClamp(string file)
    {
        // The owner's sentence as arithmetic. A ship doing the one correct thing — flying the berth's
        // own circular orbit, engine cold — must be matched to the berth. On a gravity rail that number
        // is zero; on the rail the Roadstead used to ride it was 8,583 m/s, over DockRule.MatchSpeed,
        // and no amount of careful flying could have brought it down.
        ScenarioDefinition scenario = Load(file);
        Dictionary<string, double> mu = scenario.Bodies.ToDictionary(b => b.Id, b => b.Mu);
        var unreachable = new List<string>();
        int havens = 0;

        foreach (BodyDefinition body in scenario.Bodies)
        {
            if (!body.Haven || body.ParentId is null || body.OrbitPeriodS == 0 || body.Eccentricity != 0
                || !mu.TryGetValue(body.ParentId, out double parentMu) || parentMu <= 0)
            {
                continue; // "flying its orbit" is one speed only on a circle; an ellipse wants its own law
            }

            havens++;
            double shortfall = Math.Abs(RailSpeed(body) - CircularSpeed(parentMu, body.OrbitRadiusM));
            if (shortfall > DockRule.MatchSpeed)
            {
                unreachable.Add($"{body.Name} ({body.Id}): a ship flying its orbit still reads "
                    + $"{shortfall:F0} m/s at the berth, and the clamp shears above {DockRule.MatchSpeed:F0}");
            }
        }

        Assert.True(havens > 0, $"{file} has no orbiting haven to judge — the guard would be blind.");
        Assert.True(unreachable.Count == 0,
            "#957 — \"nobody will ever play the 'let's fly next to it really quiet so autopilot will "
            + $"agree'\": in {file} flying quiet does not get you there.\n  " + string.Join("\n  ", unreachable));
    }

    [Fact]
    public void TheOneScenarioAllowedANonGravityRail_BerthsNobodyOnIt()
    {
        // The Wheel of the World holds Venus, Earth and Mars on a rigid spoke around Saturn — deliberate,
        // declared in its own description, and the reason it is out of the law above. The exemption is
        // only safe while nothing on that spoke is a place you dock: a berth there would be exactly the
        // #957 trap again, and this is what would notice.
        ScenarioDefinition wheel = Load("wheel.json");
        Assert.Contains("not gravity's work", wheel.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(wheel.Bodies, b => b.Haven);
        Assert.DoesNotContain(wheel.Bodies, b => string.Equals(b.Kind, "station", StringComparison.Ordinal));
    }

    [Fact]
    public void AtTheOwnersRead_TheAutopilotTakesTheDock()
    {
        // THE SCENE FROM THE ISSUE, flown. Sim time 178d 10h 48m; the ship trails Mars by 3.60 M km and
        // is closing at 2.6 km/s in its frame — the two numbers off the owner's own panel in the
        // screenshot where the press was refused with "can't verify a capture from here". The plain
        // arm-time rehearsal must now promise the dock outright: no correction burn, no phase luck.
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var sim = new Simulator(eph, timeStepSeconds: 60);
        double t = (178 * 86400.0) + (10 * 3600.0) + (48 * 60.0);

        Vector2d marsPos = eph.Position("mars", t);
        Vector2d marsVel = (eph.Position("mars", t + 1.0) - eph.Position("mars", t - 1.0)) / 2.0;
        Vector2d along = marsVel / marsVel.Length;
        var ship = new ShipState(marsPos - (along * 3.60e9), marsVel + (along * 2600.0), t);

        Vector2d berthVel = (eph.Position("the-space-bar", t + 1.0) - eph.Position("the-space-bar", t - 1.0)) / 2.0;
        double relToBerth = (ship.Velocity - berthVel).Length;
        Assert.True(relToBerth <= DockRule.MatchSpeed,
            $"The owner was 2.6 km/s off Mars; the berth beside it should not read {relToBerth:F0} m/s.");

        int budget = 500 - AutopilotRehearsal.ReservePulses(500);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "the-space-bar", budget, maxHorizonSeconds: 40 * 86400.0);
        Assert.True(r.Deliverable,
            "#957: from the owner's own read the autopilot must take the dock, not decline it.");
        Assert.True(r.Captured);
        Assert.True(r.PulsesCharged <= budget);
    }
}
