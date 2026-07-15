using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// Analytic circular orbits with parent chaining (a body may orbit a body that orbits another —
/// the Wheel of the World scenario relies on this). Deterministic and allocation-free per query.
/// </summary>
public sealed class CircularOrbitEphemeris : ICelestialEphemeris
{
    private readonly Dictionary<string, CelestialBody> _byId;
    private readonly List<CelestialBody> _bodies;
    private readonly TrafficDefinition? _traffic;

    public CircularOrbitEphemeris(IEnumerable<CelestialBody> bodies, TrafficDefinition? traffic = null)
    {
        _bodies = [.. bodies];
        _byId = _bodies.ToDictionary(b => b.Id);
        _traffic = traffic;

        foreach (CelestialBody body in _bodies)
        {
            if (body.ParentId is not null && !_byId.ContainsKey(body.ParentId))
            {
                throw new ArgumentException($"Body '{body.Id}' orbits unknown parent '{body.ParentId}'.");
            }
        }
    }

    public static CircularOrbitEphemeris FromScenario(ScenarioDefinition scenario) =>
        new(scenario.Bodies.Select(b => new CelestialBody(
            b.Id, b.Name, b.ParentId, b.Mu, b.BodyRadiusM, b.OrbitRadiusM, b.OrbitPeriodS, b.InitialPhaseRad,
            ParseKind(b.Kind), b.Haven, ParseAtmosphere(b.Atmosphere), b.Eccentricity, b.ArgPeriapsisRad)),
            scenario.Traffic);

    private static Atmosphere? ParseAtmosphere(AtmosphereDefinition? atm) =>
        atm is null ? null : new Atmosphere(atm.RefDensity, atm.ScaleHeightM, atm.TopAltitudeM);

    private static BodyKind ParseKind(string kind) => kind switch
    {
        "moon" => BodyKind.Moon,
        "station" => BodyKind.Station,
        _ => BodyKind.Planet,
    };

    public IReadOnlyList<CelestialBody> Bodies => _bodies;

    public TrafficDefinition? Traffic => _traffic;

    public Vector2d Position(string bodyId, double simTime)
    {
        CelestialBody body = _byId[bodyId];
        Vector2d parentPosition = body.ParentId is null ? Vector2d.Zero : Position(body.ParentId, simTime);

        if (body.OrbitPeriod == 0)
        {
            return parentPosition;
        }

        // e == 0 is the legacy circular path, kept EXACTLY as it was (same operations, same order) so
        // every existing scenario is byte-for-byte identical to before Kepler rails existed — the
        // regression gate (EphemerisTests) asserts this against a whole circular system.
        if (body.Eccentricity == 0.0)
        {
            double angle = body.InitialPhase + Math.Tau * simTime / body.OrbitPeriod;
            return parentPosition + new Vector2d(body.OrbitRadius * Math.Cos(angle), body.OrbitRadius * Math.Sin(angle));
        }

        return parentPosition + EllipticalOffset(body, simTime);
    }

    public double InstantaneousOrbitRadius(string bodyId, double simTime)
    {
        CelestialBody body = _byId[bodyId];
        if (body.ParentId is null || body.OrbitPeriod == 0)
        {
            return 0.0;
        }

        // Circle: the radius is constant, so skip the offset math and the Kepler solve entirely.
        return body.Eccentricity == 0.0 ? body.OrbitRadius : EllipticalOffset(body, simTime).Length;
    }

    // The parent-relative offset of a body on its ellipse at simTime. Semi-major axis a = OrbitRadius,
    // eccentricity e, argument of periapsis ω = ArgPeriapsis, and InitialPhase reinterpreted as the
    // mean anomaly at epoch. In the perifocal frame periapsis is +x: x = a(cosE − e), y = a√(1−e²)sinE;
    // that frame is then rotated by ω into world axes. At e = 0 this reduces algebraically to the
    // circular formula, but the circular path above is what callers actually take, so this stays the
    // sole eccentric branch.
    private static Vector2d EllipticalOffset(CelestialBody body, double simTime)
    {
        double e = body.Eccentricity;
        double meanAnomaly = body.InitialPhase + Math.Tau * simTime / body.OrbitPeriod;
        double eccentricAnomaly = SolveEccentricAnomaly(meanAnomaly, e);

        double cosE = Math.Cos(eccentricAnomaly);
        double sinE = Math.Sin(eccentricAnomaly);
        double a = body.OrbitRadius;
        double px = a * (cosE - e);                        // perifocal x (toward periapsis)
        double py = a * Math.Sqrt(1.0 - e * e) * sinE;     // perifocal y (toward +90° from periapsis)

        double cosW = Math.Cos(body.ArgPeriapsis);
        double sinW = Math.Sin(body.ArgPeriapsis);
        return new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
    }

    /// <summary>
    /// Solve Kepler's equation M = E − e·sinE for the eccentric anomaly E by deterministic Newton
    /// iteration. Fixed iteration budget with an early convergence break, seeded analytically
    /// (E₀ = M + e·sinM) after reducing M to [−π, π] — from that seed Newton converges quadratically,
    /// reaching double precision in ~3–5 steps for the eccentricities the game uses (cyclers ~0.3–0.6,
    /// comets up to ~0.9). No per-frame state, so <see cref="Position"/> stays a pure function of time
    /// (essential for out-of-order plot/scrub queries). Deterministic: the same M and e always yield
    /// the same E on a given platform, exactly as the existing circular sin/cos rails already rely on.
    /// </summary>
    public static double SolveEccentricAnomaly(double meanAnomaly, double eccentricity)
    {
        // Reduce to [−π, π] for the fastest, most stable convergence (IEEERemainder is deterministic).
        double m = Math.IEEERemainder(meanAnomaly, Math.Tau);
        double e = eccentricity;

        double bigE = m + e * Math.Sin(m);
        for (int i = 0; i < MaxKeplerIterations; i++)
        {
            double f = bigE - e * Math.Sin(bigE) - m;
            double fPrime = 1.0 - e * Math.Cos(bigE);
            double delta = f / fPrime;
            bigE -= delta;
            if (Math.Abs(delta) < KeplerTolerance)
            {
                break;
            }
        }

        return bigE;
    }

    // Newton budget: quadratic convergence from the analytic seed reaches machine epsilon well inside
    // this cap for every e < 1; the cap only bounds the pathological near-parabolic case (loader
    // forbids e >= 1 anyway). The tolerance is on the E-correction, in radians.
    private const int MaxKeplerIterations = 12;
    private const double KeplerTolerance = 1e-12;
}
