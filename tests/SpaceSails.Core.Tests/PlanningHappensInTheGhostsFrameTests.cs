namespace SpaceSails.Core.Tests;

/// <summary>
/// #838 · THE GHOST'S FRAME, NOT THE SHIP'S NOSE. The four quick selects the maneuver-node planner now
/// offers — forward, back, up, down — and the one law that makes them worth pressing.
///
/// <para>Owner (2026-08-11, end of a playtest): <i>"I almost always use those four directions in respect
/// to our trajectory... the intermediate angles are the exception."</i> Ruling A (2026-08-17): the vector
/// view is the only planning surface at the scrub, and the four directions are computed against the
/// trajectory AT THE SCRUBBED NODE — the ghost's frame — never the ship's live heading.</para>
///
/// <para>That last clause is the whole issue, and it is the one a green test can miss: read "forward" off
/// the live ship and every assertion about quarter turns still passes, because the frame is internally
/// consistent — it is just the WRONG frame. So the world these guards fly is chosen so the two frames
/// disagree loudly: a circular orbit whose period is 400 h, with the node a tenth of an orbit (40 h) ahead.
/// The ship's prograde and the ghost's prograde are 36° apart there, and each guard asserts that gap
/// exists before it asserts anything else — a frame law proved in a world where both frames give the same
/// answer is a green test that asserts nothing.</para>
/// </summary>
public class PlanningHappensInTheGhostsFrameTests
{
    private const double Hour = 3600.0;

    /// <summary>An Earth-massed primary at the origin.</summary>
    private const double PrimaryMu = 3.986e14;

    /// <summary>The orbit radius whose circular period is 400 h — chosen so a node 40 h out sits exactly
    /// a tenth of a turn (36°) around the course from the ship. r = ∛(μ·(T/2π)²).</summary>
    private static readonly double OrbitRadius =
        Math.Cbrt(PrimaryMu * Math.Pow(400 * Hour / Math.Tau, 2));

    private static readonly Vector2d Primary = Vector2d.Zero;

    private static CircularOrbitEphemeris World() =>
        new([new CelestialBody("primary", "Primary", null, PrimaryMu, 6.37e6, 0, 0, 0)]);

    /// <summary>The ship on a counter-clockwise circular orbit: out along +X, moving +Y.</summary>
    private static ShipState CircularShip(bool counterClockwise = true)
    {
        double speed = Math.Sqrt(PrimaryMu / OrbitRadius);
        return new ShipState(new Vector2d(OrbitRadius, 0), new Vector2d(0, counterClockwise ? speed : -speed), 0);
    }

    /// <summary>The ribbon the plotting panel draws and the quick selects read — the REAL projection, not a
    /// hand-built list, so these guards fly the same samples the panel does.</summary>
    private static IReadOnlyList<TrajectorySample> Ribbon(ShipState ship)
    {
        var simulator = new Simulator(World(), timeStepSeconds: 10);
        return simulator.ProjectAdaptive(ship, ManeuverPlan.Empty, horizonSeconds: 60 * Hour,
                                         maxTimeStep: 60, maxSamples: 8000);
    }

    private static double Separation(double a, double b)
    {
        double d = Math.Abs(a - b) % 360;
        return d > 180 ? 360 - d : d;
    }

    // ---- GUARD (a): the aim is solved at the NODE'S epoch ---------------------------------------

    /// <summary>
    /// LAW ONE — FORWARD at a node 40 h out is the ghost's prograde THERE, and it is not the ship's.
    ///
    /// <para>Anti-vacuity first: the guard proves the two frames disagree by ~36° in this world before it
    /// checks which one the quick select returned. Read the direction off the live ship and the second
    /// assertion fails by that whole 36°.</para>
    /// </summary>
    [Fact]
    public void Forward_AtANodeFortyHoursOut_IsTheGhostsPrograde_NotTheShips()
    {
        ShipState ship = CircularShip();
        IReadOnlyList<TrajectorySample> ribbon = Ribbon(ship);
        double nodeTime = 40 * Hour;

        double liveForward = NodeFrame.Prograde(ship.Velocity);
        double nodeForward = NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, nodeTime, Primary,
                                                 ship.Position, ship.Velocity);

        // The world must be able to tell the two frames apart, or nothing below means anything.
        Assert.True(Separation(nodeForward, liveForward) > 30,
            $"the test world cannot distinguish the frames: ship {liveForward:0.##}°, node {nodeForward:0.##}°");

        // A tenth of a counter-clockwise turn past a prograde of 90° is 126°, and that is the answer.
        Assert.Equal(126.0, nodeForward, 0);
    }

    /// <summary>The same law from the other end: the ghost's own state at the node, sampled off the ribbon,
    /// is what the quick select is built from — position and velocity both.</summary>
    [Fact]
    public void TheGhostSampledAtTheNode_IsWhereTheShipWillBe_NotWhereItIs()
    {
        ShipState ship = CircularShip();
        IReadOnlyList<TrajectorySample> ribbon = Ribbon(ship);

        Vector2d ghost = NodeFrame.PositionAt(ribbon, 40 * Hour, ship.Position);
        Vector2d ghostVelocity = NodeFrame.VelocityAt(ribbon, 40 * Hour, ship.Velocity);

        // A tenth of a turn around: the ghost has swung 36° off the +X axis it started on.
        double swept = Math.Atan2(ghost.Y, ghost.X) * 180 / Math.PI;
        Assert.Equal(36.0, swept, 0);
        Assert.True(Math.Abs(ghost.Length - OrbitRadius) < OrbitRadius * 1e-3);              // still circular
        Assert.True(Math.Abs(ghostVelocity.Length - ship.Velocity.Length) < ship.Velocity.Length * 1e-2); // same speed, new direction
        Assert.True(Separation(NodeFrame.Prograde(ghostVelocity), NodeFrame.Prograde(ship.Velocity)) > 30);
    }

    // ---- GUARD (b): four directions, four quarter turns, right sense ----------------------------

    /// <summary>
    /// LAW TWO — the four are exactly a half turn and two quarter turns apart, at any epoch.
    /// </summary>
    [Fact]
    public void TheFourDirections_AreQuarterTurnsApart()
    {
        ShipState ship = CircularShip();
        IReadOnlyList<TrajectorySample> ribbon = Ribbon(ship);
        double nodeTime = 40 * Hour;

        double forward = NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);
        double back = NodeFrame.HeadingAt(NodeDirection.Back, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);
        double up = NodeFrame.HeadingAt(NodeDirection.Up, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);
        double down = NodeFrame.HeadingAt(NodeDirection.Down, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);

        Assert.Equal(180.0, Separation(forward, back), 6);
        Assert.Equal(90.0, Separation(forward, up), 6);
        Assert.Equal(90.0, Separation(forward, down), 6);
        Assert.Equal(180.0, Separation(up, down), 6);
    }

    /// <summary>
    /// LAW THREE — and UP is the quarter turn that climbs AWAY from the body, which is the assertion an
    /// inverted sign fails. On a counter-clockwise circular orbit radial-out is prograde MINUS 90; flip the
    /// orbit and it must flip with it. Both directions are flown here on purpose: a hard-coded +90 or −90
    /// passes one of them and fails the other.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Up_PointsAwayFromThePrimary_WhicheverWayTheOrbitRuns(bool counterClockwise)
    {
        ShipState ship = CircularShip(counterClockwise);
        IReadOnlyList<TrajectorySample> ribbon = Ribbon(ship);
        double nodeTime = 40 * Hour;

        Vector2d ghost = NodeFrame.PositionAt(ribbon, nodeTime, ship.Position);
        double up = NodeFrame.HeadingAt(NodeDirection.Up, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);
        double down = NodeFrame.HeadingAt(NodeDirection.Down, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);

        double rad = up * Math.PI / 180;
        var outward = new Vector2d(Math.Cos(rad), Math.Sin(rad));
        Vector2d awayFromPrimary = (ghost - Primary).Normalized();

        // Away from the body, and on a circular orbit that is the outward radial itself.
        Assert.True(outward.Dot(awayFromPrimary) > 0.99,
            $"UP does not climb away from the primary (dot {outward.Dot(awayFromPrimary):0.###})");

        double downRad = down * Math.PI / 180;
        Assert.True(new Vector2d(Math.Cos(downRad), Math.Sin(downRad)).Dot(awayFromPrimary) < -0.99,
            "DOWN does not drop toward the primary");

        // And the sense follows the orbit's handedness rather than a hard-coded sign.
        double forward = NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, nodeTime, Primary, ship.Position, ship.Velocity);
        double expected = counterClockwise ? forward - 90 : forward + 90;
        Assert.Equal(0.0, Separation(up, (expected % 360 + 360) % 360), 6);
    }

    // ---- GUARD (d): re-time the node and the answer is re-solved --------------------------------

    /// <summary>
    /// LAW FOUR — pressing FORWARD again after scrubbing the node to another epoch gives another heading.
    /// The frame is a function of the node's time and nothing else; a cached answer would return the first
    /// one forever, and the burn would fly off a course the ship left hours ago.
    /// </summary>
    [Fact]
    public void ReTimingTheNode_ReSolvesTheDirection()
    {
        ShipState ship = CircularShip();
        IReadOnlyList<TrajectorySample> ribbon = Ribbon(ship);

        double atTen = NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, 10 * Hour, Primary, ship.Position, ship.Velocity);
        double atForty = NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, 40 * Hour, Primary, ship.Position, ship.Velocity);

        // 30 h later on a 400 h orbit is 27° further around — a different answer, and the right one.
        Assert.Equal(27.0, Separation(atTen, atForty), 0);

        // Pure: the same epoch always answers the same, so "re-solved" never means "jittered".
        Assert.Equal(atForty, NodeFrame.HeadingAt(NodeDirection.Forward, ribbon, 40 * Hour, Primary, ship.Position, ship.Velocity), 9);
    }

    // ---- The conversion that lets the planner drop ± without changing a single flown course -----

    /// <summary>
    /// The planner converts a legacy ± node to the Vector burn aimed FORWARD (or BACK) when its editor
    /// opens. That is only honest if the conversion is lossless — so fly both through the real integrator
    /// and demand the same velocity out. This is what makes "the ± idiom leaves the planner" a change of
    /// language rather than a change of flight.
    /// </summary>
    [Theory]
    [InlineData(ManeuverAction.Accelerate, NodeDirection.Forward)]
    [InlineData(ManeuverAction.Decelerate, NodeDirection.Back)]
    public void ConvertingAFactorBurnToItsQuickSelect_FliesTheSameCourse(ManeuverAction action, NodeDirection direction)
    {
        var simulator = new Simulator(World(), timeStepSeconds: 10);
        ShipState ship = CircularShip();

        double heading = NodeFrame.Heading(direction, ship.Velocity, ship.Position, Primary);
        var factor = new ManeuverPlan([new ManeuverNode(0, action, Pulses: 3, Percent: 7.5)]);
        var vector = new ManeuverPlan([new ManeuverNode(0, action, Pulses: 3, Percent: 7.5,
                                                        Mode: BurnMode.Vector, HeadingDegrees: heading)]);

        ShipState flownFactor = simulator.Step(ship, factor);
        ShipState flownVector = simulator.Step(ship, vector);

        Assert.Equal(flownFactor.Velocity.X, flownVector.Velocity.X, precision: 3);
        Assert.Equal(flownFactor.Velocity.Y, flownVector.Velocity.Y, precision: 3);
    }

    // ---- The words on the buttons ---------------------------------------------------------------

    /// <summary>The four labels are four distinct plain words, and not one of them wears a ± — that idiom
    /// went back to reflex flying with the ruling, and the panel must never speak it again.</summary>
    [Fact]
    public void TheFourLabels_AreDistinctPlainWords_WithNoPlusOrMinus()
    {
        Assert.Equal(4, NodeFrame.QuickSelects.Count);
        var labels = NodeFrame.QuickSelects.Select(NodeFrame.Label).ToList();
        Assert.Equal(4, labels.Distinct().Count());
        foreach (string label in labels)
        {
            Assert.DoesNotContain("±", label);
            Assert.DoesNotContain("+", label);
            Assert.DoesNotContain("−", label);
        }

        Assert.Contains("FORWARD", NodeFrame.Label(NodeDirection.Forward));
        Assert.Contains("BACK", NodeFrame.Label(NodeDirection.Back));
        Assert.Contains("UP", NodeFrame.Label(NodeDirection.Up));
        Assert.Contains("DOWN", NodeFrame.Label(NodeDirection.Down));

        // Up and down are ambiguous until the body is named — so the hints name it, and so does the note.
        Assert.Contains("Titan", NodeFrame.Hint(NodeDirection.Up, "Titan"));
        Assert.Contains("Titan", NodeFrame.Hint(NodeDirection.Down, "Titan"));
        Assert.Contains("Titan", NodeFrame.FrameNote("Titan"));
        Assert.Contains("this node", NodeFrame.Hint(NodeDirection.Forward, "Titan"));
    }

    /// <summary>A dead-stop ghost has no forward. The frame degenerates to the world axes rather than
    /// throwing or returning NaN — and the quarter-turn law still holds, so the panel never offers two
    /// buttons that mean the same thing.</summary>
    [Fact]
    public void ADeadStopGhost_StillYieldsFourDistinctQuarterTurns()
    {
        var position = new Vector2d(OrbitRadius, 0);
        double[] headings = [.. NodeFrame.QuickSelects.Select(d => NodeFrame.Heading(d, Vector2d.Zero, position, Primary))];

        Assert.All(headings, h => Assert.False(double.IsNaN(h)));
        Assert.Equal(4, headings.Distinct().Count());
        Assert.Equal(180.0, Separation(headings[0], headings[1]), 6);
        Assert.Equal(90.0, Separation(headings[0], headings[2]), 6);
    }
}
