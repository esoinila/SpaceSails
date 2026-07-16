namespace SpaceSails.Core.Tests;

public class EphemerisTests
{
    [Fact]
    public void SolScenario_LoadsAllBodies()
    {
        var scenario = SimulatorTests.LoadSol();

        Assert.Equal("Sol", scenario.Name);
        // Sun + 8 planets + Luna (M6) + outer moons, stations and havens (PR-3, vision par. 8):
        // Mercury Compute Farms, Highport Satellite Works, Europa/Ganymede/Callisto, Titan,
        // Enceladus, Ringside Exchange, plus the inner grey-market docks Cinder Roost (Venus),
        // The Space Bar (Mars) and The Tilt (Uranus) — the derelict roadster (go-ashore fetch job),
        // and Selene Gate, the Luna-vicinity fuel port that closes Lab 28's stranded-at-Luna gap (#157).
        Assert.Equal(23, scenario.Bodies.Count);
        Assert.Contains(scenario.Bodies, b => b.Id == "saturn");
        Assert.Contains(scenario.Bodies, b => b.Id == "luna" && b.ParentId == "earth");
        Assert.Contains(scenario.Bodies, b => b.Id == "selene-gate" && b.ParentId == "earth" && b.Haven);
        Assert.Contains(scenario.Bodies, b => b.Id == "titan" && b.ParentId == "saturn" && b.Kind == "moon");
        Assert.Contains(scenario.Bodies, b => b.Id == "mercury-compute" && b.Kind == "station");
        Assert.Contains(scenario.Bodies, b => b.Id == "enceladus" && b.Haven);
    }

    [Fact]
    public void Earth_ReturnsToStart_AfterOnePeriod()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        double period = ephemeris.Bodies.Single(b => b.Id == "earth").OrbitPeriod;

        Vector2d start = ephemeris.Position("earth", 0);
        Vector2d afterOneYear = ephemeris.Position("earth", period);

        Assert.True((afterOneYear - start).Length < 1.0, "Earth must return to its epoch position after one period.");
    }

    [Fact]
    public void ParentChaining_OffsetsChildByParentPosition()
    {
        // A moon on a rotating parent — the mechanism the Wheel of the World scenario is built on.
        var ephemeris = new CircularOrbitEphemeris([
            new CelestialBody("center", "Center", null, 0, 0, 0, 0, 0),
            new CelestialBody("planet", "Planet", "center", 0, 0, OrbitRadius: 1000, OrbitPeriod: 400, InitialPhase: 0),
            new CelestialBody("moon", "Moon", "planet", 0, 0, OrbitRadius: 10, OrbitPeriod: 40, InitialPhase: 0),
        ]);

        // At t = 100 (quarter parent orbit) the planet is at (0, 1000); the moon has done
        // 2.5 of its own orbits, putting it at planet + (-10, 0).
        Vector2d moon = ephemeris.Position("moon", 100);

        Assert.Equal(-10, moon.X, precision: 6);
        Assert.Equal(1000, moon.Y, precision: 6);
    }

    [Fact]
    public void UnknownParent_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CircularOrbitEphemeris([
            new CelestialBody("orphan", "Orphan", "ghost", 0, 0, 1, 1, 0),
        ]));
    }

    // ---- Kepler rails (PR-B) -----------------------------------------------------------------

    // THE REGRESSION GATE (the atmosphere precedent, RegressionGate_NoAtmosphere_ByteIdenticalToVacuum):
    // with eccentricity 0 (the default for every shipped body), Position must be bit-identical to the
    // legacy circular formula — not approximately, exactly. This is what lets Kepler rails land without
    // perturbing a single existing scenario. Checked across the whole real Sol system and many times.
    [Fact]
    public void Eccentricity0_ByteIdenticalToLegacyCircularFormula()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        double[] times = [0, 1, 12345.678, 3.7e5, 2.5e6, 4.4e7, 9.3e8, -5e6];

        foreach (double t in times)
        {
            foreach (CelestialBody body in ephemeris.Bodies)
            {
                Vector2d actual = ephemeris.Position(body.Id, t);
                Vector2d expected = LegacyCircularPosition(ephemeris, body.Id, t);
                // Vector2d is a record struct: this is bitwise value equality, not a tolerance compare.
                Assert.Equal(expected, actual);
            }
        }
    }

    // The verbatim pre-Kepler formula, recursed through parents — the oracle for the byte-identical gate.
    private static Vector2d LegacyCircularPosition(CircularOrbitEphemeris eph, string id, double t)
    {
        CelestialBody body = eph.Bodies.Single(b => b.Id == id);
        Vector2d parent = body.ParentId is null ? Vector2d.Zero : LegacyCircularPosition(eph, body.ParentId, t);
        if (body.OrbitPeriod == 0)
        {
            return parent;
        }

        double angle = body.InitialPhase + Math.Tau * t / body.OrbitPeriod;
        return parent + new Vector2d(body.OrbitRadius * Math.Cos(angle), body.OrbitRadius * Math.Sin(angle));
    }

    // A body with e=0 declared explicitly is identical to one that left it defaulted — the branch keys
    // on the value, not on whether the field was written.
    [Fact]
    public void ExplicitEccentricity0_EqualsDefaulted()
    {
        var defaulted = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("p", "P", "sun", 3.986e14, 6.37e6, 1.496e11, 6.283e7, 1.1),
        ]);
        var explicitZero = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("p", "P", "sun", 3.986e14, 6.37e6, 1.496e11, 6.283e7, 1.1,
                Eccentricity: 0.0, ArgPeriapsis: 2.5),
        ]);

        foreach (double t in new double[] { 0, 3e6, 1.9e7 })
        {
            Assert.Equal(defaulted.Position("p", t), explicitZero.Position("p", t));
        }
    }

    // The Kepler solver satisfies its own equation: E - e*sin(E) == M (mod 2*PI) for a spread of
    // eccentricities and mean anomalies, including negative and multi-revolution M.
    [Theory]
    [InlineData(0.1)]
    [InlineData(0.3)]
    [InlineData(0.6)]
    [InlineData(0.9)]
    [InlineData(0.95)]
    public void KeplerSolver_SatisfiesKeplersEquation(double e)
    {
        double[] means = [0, 0.3, 1.0, Math.PI, 4.0, 6.0, 12.5, -2.2, -9.0];
        foreach (double m in means)
        {
            double bigE = CircularOrbitEphemeris.SolveEccentricAnomaly(m, e);
            double residual = bigE - e * Math.Sin(bigE) - m;
            // Reduce the residual into (-PI, PI] — M and the solved E may differ by whole revolutions.
            residual = Math.IEEERemainder(residual, Math.Tau);
            Assert.True(Math.Abs(residual) < 1e-10, $"e={e}, M={m}: residual {residual}");
        }
    }

    // At M=0 the body sits at periapsis: distance a(1-e) from the parent, along the periapsis
    // direction (argPeriapsis). At M=PI it sits at apoapsis: a(1+e), opposite the periapsis.
    [Fact]
    public void EllipticalOrbit_HitsPeriapsisAndApoapsis()
    {
        const double a = 1.5e11, e = 0.5, w = 0.9;
        var eph = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("comet", "Comet", "sun", 1e12, 1e6, a, OrbitPeriod: 1000, InitialPhase: 0,
                Eccentricity: e, ArgPeriapsis: w),
        ]);

        Vector2d peri = eph.Position("comet", 0);            // M = 0
        Assert.Equal(a * (1 - e), peri.Length, a * 1e-9);
        Assert.Equal(a * (1 - e) * Math.Cos(w), peri.X, a * 1e-9);
        Assert.Equal(a * (1 - e) * Math.Sin(w), peri.Y, a * 1e-9);

        Vector2d apo = eph.Position("comet", 500);           // half period -> M = PI
        Assert.Equal(a * (1 + e), apo.Length, a * 1e-9);
        Assert.Equal(-a * (1 + e) * Math.Cos(w), apo.X, a * 1e-9);
        Assert.Equal(-a * (1 + e) * Math.Sin(w), apo.Y, a * 1e-9);

        // Instantaneous radius agrees with the position magnitude at both apsides.
        Assert.Equal(a * (1 - e), eph.InstantaneousOrbitRadius("comet", 0), a * 1e-9);
        Assert.Equal(a * (1 + e), eph.InstantaneousOrbitRadius("comet", 500), a * 1e-9);
    }

    // Period closure: an elliptical body returns to its epoch position after exactly one period, and
    // the whole ellipse stays bounded between periapsis and apoapsis over a full sweep.
    [Fact]
    public void EllipticalOrbit_ClosesAfterOnePeriod()
    {
        const double a = 2e11, e = 0.7, period = 4.32e7;
        var eph = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("comet", "Comet", "sun", 1e12, 1e6, a, period, InitialPhase: 1.2,
                Eccentricity: e, ArgPeriapsis: 0.4),
        ]);

        Vector2d start = eph.Position("comet", 0);
        Vector2d afterOne = eph.Position("comet", period);
        Assert.True((afterOne - start).Length < a * 1e-9, "must return to epoch position after one period");

        double rPeri = a * (1 - e), rApo = a * (1 + e);
        for (int i = 0; i <= 360; i++)
        {
            double t = period * i / 360.0;
            double r = eph.Position("comet", t).Length;
            Assert.True(r >= rPeri - a * 1e-6 && r <= rApo + a * 1e-6, $"r {r} out of [peri,apo] at t={t}");
        }
    }

    // An eccentric moon chained onto a moving parent is just the parent's position plus the moon's own
    // elliptical offset — parent chaining is orthogonal to the conic.
    [Fact]
    public void EllipticalMoon_OffsetsFromParentByItsOwnEllipse()
    {
        var eph = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("planet", "Planet", "sun", 3.986e14, 6.37e6, 1.5e11, 3.15e7, 0.5),
            new CelestialBody("moon", "Moon", "planet", 1e12, 1e6, 4e8, 2.36e6, InitialPhase: 0,
                Eccentricity: 0.4, ArgPeriapsis: 1.1, Kind: BodyKind.Moon),
        ]);

        double t = 7.7e6;
        Vector2d planet = eph.Position("planet", t);
        Vector2d moon = eph.Position("moon", t);
        double sep = (moon - planet).Length;
        // Moon-planet separation must lie within the moon's own apsides at all times.
        Assert.True(sep >= 4e8 * (1 - 0.4) - 1 && sep <= 4e8 * (1 + 0.4) + 1, $"sep {sep} out of range");
        // And the instantaneous-radius helper reports that same parent separation.
        Assert.Equal(sep, eph.InstantaneousOrbitRadius("moon", t), 4e8 * 1e-9);
    }
}
