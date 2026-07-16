namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 30 — The mass-driver timetable (worldbuilding notes §1: Luna's drivers lob
/// ballistic compute-core pods). See labs/30-the-mass-driver-timetable/README.md and Probe.cs.
/// Every gate exercises the SAME Core code the probe and the game spend with:
/// <see cref="TransferMath.PropagateKepler"/> (the analytic rail) and <see cref="MassDriverSchedule"/>
/// (launch state, timetable, cadence). The theme: a pod has zero maneuver budget, so its whole
/// future is a closed-form conic — the rail must match the integrator, the timetable must be a
/// deterministic clockwork, and the pods must spawn and expire honestly.
/// </summary>
public class Lab30MassDriverTests
{
    private const double Day = 86400.0;
    private const double SunMu = 1.32712440018e20;
    private const double EarthMu = 3.986004418e14;
    private const double AU = 1.495978707e11;

    // Inner-system slice of sol.json (verbatim), enough for a Luna launch and its heliocentric conic.
    private static CircularOrbitEphemeris InnerField()
    {
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("mercury", "mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
            new CelestialBody("venus", "venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
            new CelestialBody("earth", "earth", "sun", EarthMu, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
            new CelestialBody("luna", "luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon),
            new CelestialBody("mars", "mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
        };
        return new CircularOrbitEphemeris(bodies);
    }

    // The heliocentric conic shape a launch state rides (sun at origin => launch position is the
    // sun-relative radius). Same arithmetic the probe reads the launch family off.
    private static (double Perihelion, double Aphelion, bool Bound) Conic(ShipState s)
    {
        double r = s.Position.Length;
        double energy = s.Velocity.LengthSquared / 2 - SunMu / r;
        double h = s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X;
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (SunMu * SunMu)));
        double a = -SunMu / (2 * energy);
        return (a * (1 - e), energy < 0 ? a * (1 + e) : double.PositiveInfinity, energy < 0);
    }

    [Fact]
    public void G1_PropagateKepler_MatchesTheIntegrator_OnABallisticConic()
    {
        // The rail must BE the ballistic trajectory: in a sun-only world (pure two-body, the regime a
        // pod flies once clear of the launch well) the analytic conic and the N-body integrator must
        // agree to a hair over a long coast — that is what makes the timetable trustworthy.
        var eph = new CircularOrbitEphemeris([new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0)]);
        var sim = new Simulator(eph, timeStepSeconds: 60);

        // A bound heliocentric ellipse: 1 AU, 90% of circular speed, tilted off-tangential so the
        // conic has real radial motion (a lazy purely-tangential case would hide integration error).
        double vc = Math.Sqrt(SunMu / AU);
        var launch = new ShipState(new Vector2d(AU, 0), new Vector2d(0.1 * vc, 0.9 * vc), 0);

        for (double days = 10; days <= 120; days += 10)
        {
            ShipState integrated = sim.RunAdaptive(launch, days * Day);
            ShipState rail = MassDriverSchedule.PodRailState(launch, days * Day, SunMu)!.Value;

            double posErr = (integrated.Position - rail.Position).Length;
            double velErr = (integrated.Velocity - rail.Velocity).Length;
            // The rail is the EXACT conic; the residual here is the N-body integrator's own drift over
            // a months-long coast (~0.1% at 100 d), not rail error. A quarter-percent envelope covers it.
            Assert.True(posErr < 2.5e-3 * rail.Position.Length,
                $"rail position must track the integrator (<0.25%) at {days:F0} d; off by {posErr / rail.Position.Length:E2}");
            Assert.True(velErr < 2.5e-3 * rail.Velocity.Length,
                $"rail velocity must track the integrator (<0.25%) at {days:F0} d; off by {velErr / rail.Velocity.Length:E2}");
        }
    }

    [Fact]
    public void G2_PropagateKepler_RoundTripsAndPreservesACircularOrbit()
    {
        // Propagating forward then back by the same span returns the start (time-reversibility of a
        // conic), and a circular state stays exactly on its circle (radius and speed invariant).
        double vc = Math.Sqrt(SunMu / AU);
        var start = new ShipState(new Vector2d(AU, 0), new Vector2d(0, vc), 0);

        TransferMath.KeplerState forward = TransferMath.PropagateKepler(start.Position, start.Velocity, 37 * Day, SunMu)!.Value;
        TransferMath.KeplerState back = TransferMath.PropagateKepler(forward.Position, forward.Velocity, -37 * Day, SunMu)!.Value;
        Assert.True((back.Position - start.Position).Length < 1.0, "forward-then-back must return the start (<1 m)");

        Assert.Equal(AU, forward.Position.Length, precision: 0);        // still on the circle
        Assert.Equal(vc, forward.Velocity.Length, precision: 3);        // still at circular speed
    }

    [Fact]
    public void G3_LaunchFamily_RetrogradeReachesVenusThenMercury_ProgradeClimbsOutward()
    {
        // The useful family, read off the perihelion column: a retrograde lob dives inward (Venus,
        // then Mercury as the driver pushes harder); a prograde lob climbs outward (toward Mars).
        var eph = InnerField();
        double venusR = eph.Bodies.First(b => b.Id == "venus").OrbitRadius;
        double mercuryR = eph.Bodies.First(b => b.Id == "mercury").OrbitRadius;
        double marsR = eph.Bodies.First(b => b.Id == "mars").OrbitRadius;

        var venus = Conic(MassDriverSchedule.LaunchState(eph, "luna", 2.6e3, Math.PI, 0));
        var mercury = Conic(MassDriverSchedule.LaunchState(eph, "luna", 7.6e3, Math.PI, 0));
        var mars = Conic(MassDriverSchedule.LaunchState(eph, "luna", 5.0e3, 0.0, 0));

        Assert.True(venus.Bound && venus.Perihelion <= venusR * 1.05 && venus.Perihelion > mercuryR,
            $"~2.6 km/s retrograde must reach Venus's lane (peri {venus.Perihelion / AU:F3} AU)");
        Assert.True(mercury.Bound && mercury.Perihelion <= mercuryR * 1.05,
            $"~7.6 km/s retrograde must thread the Mercury yards (peri {mercury.Perihelion / AU:F3} AU)");
        Assert.True(mars.Aphelion >= marsR * 0.9,
            $"~5 km/s prograde must climb toward Mars (apo {mars.Aphelion / AU:F3} AU)");

        // Perihelion is monotone in retrograde driver speed — harder lob, deeper dive.
        double slow = Conic(MassDriverSchedule.LaunchState(eph, "luna", 3.0e3, Math.PI, 0)).Perihelion;
        double fast = Conic(MassDriverSchedule.LaunchState(eph, "luna", 6.0e3, Math.PI, 0)).Perihelion;
        Assert.True(fast < slow, "a faster retrograde lob must reach a lower perihelion");
    }

    [Fact]
    public void G4_SurfaceEscapeSpeed_IsTheLunarNumber()
    {
        CelestialBody luna = InnerField().Bodies.First(b => b.Id == "luna");
        double vEsc = MassDriverSchedule.SurfaceEscapeSpeed(luna);
        Assert.Equal(2376.2, vEsc, precision: 0);   // sqrt(2*mu_luna/R_luna)
    }

    [Fact]
    public void G5_Timetable_IsADeterministicClockworkCadence()
    {
        var eph = InnerField();
        var run = MassDriverSchedule.MassDriverRun.LunaMilkRun();
        const int count = 8;

        var a = MassDriverSchedule.Timetable(eph, run, baseSimTime: 0, count);
        var b = MassDriverSchedule.Timetable(eph, run, baseSimTime: 0, count);

        Assert.Equal(count, a.Count);
        for (int i = 0; i < count; i++)
        {
            // Deterministic: byte-identical launch time and launch state on a repeat call.
            Assert.Equal(a[i].LaunchTime, b[i].LaunchTime, precision: 9);
            Assert.Equal(a[i].Launch.Position.X, b[i].Launch.Position.X, precision: 6);
            Assert.Equal(a[i].Launch.Velocity.Y, b[i].Launch.Velocity.Y, precision: 6);

            // Exact cadence spacing, and expiry is launch + lifespan (the pod's rail lifespan).
            if (i > 0)
            {
                Assert.Equal(run.CadenceSeconds, a[i].LaunchTime - a[i - 1].LaunchTime, precision: 6);
            }

            Assert.Equal(a[i].LaunchTime + run.LifespanSeconds, a[i].ExpiryTime, precision: 6);
        }

        // Centred on the base time: about half already fired (in flight), about half still to come.
        Assert.Equal(count / 2, a.Count(e => e.LaunchTime < 0));
    }

    [Fact]
    public void G6_Pods_SpawnAndExpireOnTheRails()
    {
        var eph = InnerField();
        var run = MassDriverSchedule.MassDriverRun.LunaMilkRun();
        IReadOnlyList<NpcShip> pods = MassDriverSchedule.GenerateCadence(eph, run, baseSimTime: 0, count: 4);

        Assert.Equal(4, pods.Count);
        Assert.Equal(4, pods.Select(p => p.Id).Distinct().Count());   // unique ids
        foreach (NpcShip pod in pods)
        {
            Assert.True(pod.IsPod);
            Assert.Equal(0, pod.ManeuverBudget);                       // zero maneuver budget — no engine
            Assert.Empty(pod.Plan.Nodes);                             // the driver's kick is the whole plan
            Assert.Equal("Compute cores", pod.CargoClass);
            Assert.Equal("luna", pod.OriginId);
            // Lifespan: the pod is declared arrived (swept off the rails) one lifespan after launch.
            Assert.Equal(pod.DepartureTime + run.LifespanSeconds, pod.EstimatedArrivalTime, precision: 3);
        }

        // A pod fired in the past is live NOW (in flight, visible); one still to fire activates later.
        var timetable = MassDriverSchedule.Timetable(eph, run, baseSimTime: 0, count: 4);
        MassDriverSchedule.LaunchEntry inFlight = timetable.First(e => e.LaunchTime < 0);
        MassDriverSchedule.LaunchEntry scheduled = timetable.First(e => e.LaunchTime >= 0);
        Assert.False(inFlight.IsLive(inFlight.LaunchTime - 1));        // before launch: not on the rails
        Assert.True(inFlight.IsLive(0));                              // now: in flight
        Assert.True(scheduled.IsLive(scheduled.LaunchTime + Day));     // after its launch: live
        Assert.False(scheduled.IsLive(scheduled.ExpiryTime + 1));      // past lifespan: expired
    }
}
