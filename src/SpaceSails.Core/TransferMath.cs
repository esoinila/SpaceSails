namespace SpaceSails.Core;

/// <summary>
/// Pure two-body transfer kernels (#146, Lab 23 "The moon run"). Everything here is classical
/// textbook machinery — Curtis ch. 5-6 — promoted from the lab probes (14, 17, 19) into Core so
/// the autopilot's in-well planner and the lessons compute with literally the same code. Nothing
/// in this file integrates: these are the PROPOSING half of the house pattern (Lambert proposes,
/// the integrator disposes). SI units throughout; μ in m³/s²; 2-D ecliptic vectors.
///
/// <para>Deterministic by construction: fixed iteration caps, no randomness, no wall clock. A
/// solver that cannot certify its answer returns <c>null</c> — callers skip the cell, they never
/// receive a guess.</para>
/// </summary>
public static class TransferMath
{
    /// <summary>A certified single-revolution Lambert solution: the velocity the ship must have
    /// at r1 to coast (two-body) to r2 in exactly the requested time, and the velocity it arrives
    /// with. Both in the same frame the position vectors were given in (deltas are frame-offset
    /// free, so a parent-frame Δv is a world-frame Δv).</summary>
    public readonly record struct LambertSolution(Vector2d V1, Vector2d V2, int Iterations);

    /// <summary>Upper end of the universal-variable bracket: z = (2π)² is a full revolution,
    /// where the time of flight blows up. Single-rev only — multi-rev arcs are out of scope on
    /// purpose (same ruling as lab 14).</summary>
    private const double ZFullRevolution = Math.Tau * Math.Tau;

    /// <summary>Deepest hyperbolic bracket the expansion will reach. At z = −65536 every
    /// intermediate (cosh(256), (−z)^1.5) is still finite in a double, so the sentinel logic
    /// below never has to reason about NaN from overflow. No sane game/porkchop cell roots
    /// anywhere near this deep.</summary>
    private const double ZDeepestHyperbolic = -65536.0;

    /// <summary>
    /// Lambert's problem (Curtis Algorithm 5.2, universal variables): given two position vectors
    /// relative to the attracting body and a time of flight, find the connecting two-body arc.
    /// This is lab 14's solver hardened for Core duty:
    /// (1) the lower bracket EXPANDS (doubling from −1 down to <see cref="ZDeepestHyperbolic"/>)
    /// instead of the lab's fixed −100, so fast hyperbolic cells in a porkchop scan don't falsely
    /// read "no solution";
    /// (2) degenerate geometry — transfer angle at 0° or 180°, where the plane/f-and-g math is
    /// singular — returns null instead of dividing by ~0 (scans step OVER the 180° blind spot);
    /// (3) the achieved time of flight is re-checked against the request and the Lagrange g is
    /// checked non-degenerate before any velocity is built — a bisection always returns a z, and
    /// a z is not a solution until it is verified.
    /// </summary>
    /// <param name="r1">Departure position relative to the attractor (m).</param>
    /// <param name="r2">Arrival position relative to the attractor (m).</param>
    /// <param name="tofSeconds">Required time of flight (s), &gt; 0.</param>
    /// <param name="mu">Gravitational parameter of the attractor (m³/s²).</param>
    /// <param name="prograde">Choose the counter-clockwise (true, the rails' direction) or
    /// clockwise sweep from r1 to r2. The same two points are reachable both ways; the other way
    /// is usually absurdly expensive, but the caller decides.</param>
    /// <returns>The certified solution, or null when no honest single-rev arc exists for this
    /// geometry and clock (degenerate angle, unreachable time of flight, or verification
    /// failure). Null means SKIP — never substitute a guess.</returns>
    public static LambertSolution? Lambert(
        Vector2d r1, Vector2d r2, double tofSeconds, double mu, bool prograde = true)
    {
        if (!(tofSeconds > 0) || !(mu > 0))
        {
            return null;
        }

        double len1 = r1.Length, len2 = r2.Length;
        if (len1 <= 0 || len2 <= 0)
        {
            return null;
        }

        // Transfer angle in [0, 2π), swept in the requested direction (2-D: the cross product's
        // sign IS the sweep direction, no plane ambiguity).
        double cross = r1.X * r2.Y - r1.Y * r2.X;
        double dTheta = Math.Acos(Math.Clamp(r1.Dot(r2) / (len1 * len2), -1.0, 1.0));
        bool counterClockwise = cross >= 0;
        if (counterClockwise != prograde)
        {
            dTheta = Math.Tau - dTheta;
        }

        // The two singular geometries. 0°: departure and arrival rays coincide — the chord is
        // radial and A's denominator vanishes. 180°: sin Δθ = 0 makes A = 0, and the f-and-g
        // construction divides by g = A·√(y/μ) — Lambert's classic blind spot. Both are measure-
        // zero cells a scan steps over; a caller that genuinely needs 180° nudges the clock.
        double sinDTheta = Math.Sin(dTheta);
        double oneMinusCos = 1 - Math.Cos(dTheta);
        if (Math.Abs(sinDTheta) < 1e-9 || oneMinusCos < 1e-12)
        {
            return null;
        }

        double a = sinDTheta * Math.Sqrt(len1 * len2 / oneMinusCos);
        double sqrtMu = Math.Sqrt(mu);

        double YOf(double z) => len1 + len2 + a * (z * StumpffS(z) - 1) / Math.Sqrt(StumpffC(z));

        // Signed time-of-flight error at z. Below the root the arc flies too fast (< 0), above
        // too slow (> 0); y < 0 marks geometry unreachable that far down (the "too fast" side).
        // Non-finite intermediates (only possible pressed against the full-rev boundary, where
        // the true time of flight is enormous) read as the slow side.
        double TofError(double z)
        {
            double y = YOf(z);
            if (y < 0)
            {
                return double.NegativeInfinity;
            }

            double error = Math.Pow(y / StumpffC(z), 1.5) * StumpffS(z) + a * Math.Sqrt(y) - sqrtMu * tofSeconds;
            return double.IsNaN(error) ? double.PositiveInfinity : error;
        }

        // Bracket the root. The elliptic ceiling is fixed just under one full revolution; the
        // floor expands into the hyperbolic domain until the fast side is actually in hand. A
        // long-way arc (Δθ > π) cannot be flown hyperbolically at all, so for an impossible
        // clock the expansion runs out — that is a refusal, not an error.
        double zHi = ZFullRevolution - 1e-9;
        double zLo = -1.0;
        int iterations = 0;
        while (TofError(zLo) >= 0)
        {
            zLo *= 2;
            iterations++;
            if (zLo < ZDeepestHyperbolic)
            {
                return null;
            }
        }

        while (zHi - zLo > 1e-11 * Math.Max(1.0, Math.Abs(zLo)) && iterations < 400)
        {
            double zMid = 0.5 * (zLo + zHi);
            if (TofError(zMid) > 0)
            {
                zHi = zMid;
            }
            else
            {
                zLo = zMid;
            }
            iterations++;
        }

        // Certification. Bisection ALWAYS produces a z; it is only a solution if the equation is
        // actually satisfied there (a sign-change bracket can lie when the requested arc has no
        // single-rev solution) and the Lagrange g is far enough from zero to divide by.
        double zSol = 0.5 * (zLo + zHi);
        double ySol = YOf(zSol);
        if (!(ySol > 0))
        {
            return null;
        }

        // RELATIVE tolerance on purpose: an absolute floor (lab 14 accepted anything within 1 s)
        // would happily certify a wildly wrong arc against an absurdly short request.
        double residualSeconds = TofError(zSol) / sqrtMu;
        if (!double.IsFinite(residualSeconds) || Math.Abs(residualSeconds) > 1e-6 * tofSeconds)
        {
            return null;
        }

        double g = a * Math.Sqrt(ySol / mu);
        if (Math.Abs(g) < 1e-6)
        {
            return null;
        }

        double f = 1 - ySol / len1;
        double gDot = 1 - ySol / len2;
        return new LambertSolution((r2 - r1 * f) / g, (r2 * gDot - r1) / g, iterations);
    }

    /// <summary>The classical circular-to-circular transfer (Curtis ch. 6): both burns, the
    /// coast time, and the transfer ellipse's semi-major axis. Exact only for coplanar circular
    /// rails and apsis-to-apsis timing — the teacher and the scan seed, not the engine.</summary>
    public readonly record struct HohmannPlan(
        double DepartDeltaV, double ArriveDeltaV, double TransferSeconds, double SemiMajorAxis)
    {
        public double TotalDeltaV => DepartDeltaV + ArriveDeltaV;
    }

    /// <summary>Hohmann transfer between circular orbits of radius <paramref name="r1"/> and
    /// <paramref name="r2"/> (either direction) about a body of parameter <paramref name="mu"/>.</summary>
    public static HohmannPlan Hohmann(double r1, double r2, double mu)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(r1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(r2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mu);

        double aTransfer = (r1 + r2) / 2;
        double depart = Math.Abs(Math.Sqrt(mu * (2 / r1 - 1 / aTransfer)) - Math.Sqrt(mu / r1));
        double arrive = Math.Abs(Math.Sqrt(mu / r2) - Math.Sqrt(mu * (2 / r2 - 1 / aTransfer)));
        double tof = Math.PI * Math.Sqrt(aTransfer * aTransfer * aTransfer / mu);
        return new HohmannPlan(depart, arrive, tof, aTransfer);
    }

    /// <summary>How often the same relative geometry between two circular orbits repeats:
    /// 1/|1/T₁ − 1/T₂|. Equal periods never re-align — that returns +∞.</summary>
    public static double SynodicPeriod(double period1, double period2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period2);
        double rate = Math.Abs(1 / period1 - 1 / period2);
        return rate <= 0 ? double.PositiveInfinity : 1 / rate;
    }

    /// <summary>The target's lead angle over the ship at a Hohmann departure: the transfer covers
    /// π radians while the target covers n_target·TOF, so the window opens when the target leads
    /// by α = π − n_target·TOF. Normalized to (−π, π]; negative means the target should TRAIL at
    /// departure (inward hops, r2 &lt; r1). The bus timetable in one number.</summary>
    public static double HohmannLeadAngle(double r1, double r2, double mu)
    {
        HohmannPlan plan = Hohmann(r1, r2, mu);
        double targetMeanMotion = Math.Sqrt(mu / (r2 * r2 * r2));
        double lead = Math.IEEERemainder(Math.PI - targetMeanMotion * plan.TransferSeconds, Math.Tau);
        // IEEERemainder lands in [−π, π]; fold the −π edge onto +π so the contract is (−π, π].
        return lead <= -Math.PI ? lead + Math.Tau : lead;
    }

    /// <summary>The standardized rails velocity: central difference over ±1 s, exactly the
    /// private helper the rehearsal and the lab probes carry — promoted so every consumer reads
    /// the same number. (Sub-mm/s error on any rail in the game; an analytic Kepler velocity is
    /// not worth a second code path that could disagree with <see cref="ICelestialEphemeris.Position"/>.)</summary>
    public static Vector2d BodyVelocity(ICelestialEphemeris ephemeris, string bodyId, double simTime) =>
        (ephemeris.Position(bodyId, simTime + 1.0) - ephemeris.Position(bodyId, simTime - 1.0)) / 2.0;

    private static double StumpffC(double z) => z > 1e-8 ? (1 - Math.Cos(Math.Sqrt(z))) / z
        : z < -1e-8 ? (Math.Cosh(Math.Sqrt(-z)) - 1) / -z
        : 0.5 - z / 24 + z * z / 720;

    private static double StumpffS(double z) => z > 1e-8 ? (Math.Sqrt(z) - Math.Sin(Math.Sqrt(z))) / Math.Pow(z, 1.5)
        : z < -1e-8 ? (Math.Sinh(Math.Sqrt(-z)) - Math.Sqrt(-z)) / Math.Pow(-z, 1.5)
        : 1.0 / 6 - z / 120 + z * z / 5040;
}
