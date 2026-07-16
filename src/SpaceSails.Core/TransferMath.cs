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

    // ---- Co-orbital rendezvous: the phasing maneuver (Curtis ch. 6.5, Lab 24 "The last mile") ----
    //
    // Lambert cannot price the last mile: a phasing loop returns to its own starting point after a
    // whole revolution, which is exactly the 2π geometry the single-rev universal-variable solver
    // refuses (and should). The closed form doesn't need it: change your PERIOD, not your path —
    // coast k laps on an ellipse whose period differs from the target's by Δθ/k per lap, and the
    // gap closes at your own doorstep. Two small burns; the well does the chasing.

    /// <summary>One row of the phasing trade table: coast <see cref="Revolutions"/> laps on an
    /// ellipse of period <see cref="PhasingPeriod"/> to close the phase gap, paying
    /// <see cref="EnterDeltaV"/> to leave the circular lane and the same again
    /// (<see cref="ExitDeltaV"/>) to re-match on return. More laps = cheaper burns, longer
    /// <see cref="WaitSeconds"/> — catch this bus or the next one. <see cref="Periapsis"/> /
    /// <see cref="Apoapsis"/> are the ellipse's extremes for the caller's clearance checks
    /// (never thread the planet; never graze the tide-stripped outer Hill).</summary>
    public readonly record struct PhasingPlan(
        int Revolutions,
        double PhasingPeriod,
        double SemiMajorAxis,
        double EnterDeltaV,
        double ExitDeltaV,
        double WaitSeconds,
        double Periapsis,
        double Apoapsis)
    {
        public double TotalDeltaV => EnterDeltaV + ExitDeltaV;
    }

    /// <summary>Signed phase angle by which <paramref name="target"/> LEADS <paramref name="ship"/>
    /// around their common parent, in (−π, π], counter-clockwise positive (the rails' direction).
    /// Positions are parent-relative. Feed the result to <see cref="PhasingOrbit"/>, which
    /// normalizes the sign into its two catch-up families.</summary>
    public static double PhaseGap(Vector2d ship, Vector2d target)
    {
        double cross = ship.X * target.Y - ship.Y * target.X;
        double dot = ship.Dot(target);
        return Math.Atan2(cross, dot);
    }

    /// <summary>
    /// The co-orbital phasing orbit (Curtis ch. 6.5): from a circular lane of radius
    /// <paramref name="radius"/> about a body of parameter <paramref name="mu"/>, close a phase gap
    /// of <paramref name="phaseGapRadians"/> (the angle the TARGET leads, CCW positive — see
    /// <see cref="PhaseGap"/>) in exactly <paramref name="revolutions"/> laps.
    ///
    /// <para><paramref name="dipInside"/> selects the family. True: shorten the period
    /// (T·(1 − g/(2πk)), burn retrograde, dip toward the body) so the ship catches a LEADING
    /// target — the burn point becomes the ellipse's APOAPSIS. False: lengthen the period
    /// (T·(1 + (2π − g)/(2πk)), burn prograde, swell outward) so the target laps the ship instead
    /// — the burn point becomes the PERIAPSIS. Both close any gap; they differ in Δv, wait, and
    /// which clearance (surface vs outer Hill) they stress. The planner asks for both and prices.</para>
    ///
    /// <para>Null when the requested family/lap count is geometrically impossible (the shortened
    /// period would need a non-positive semi-major axis) or the inputs are degenerate. The Δv is
    /// the exact two-body figure for an ideally circular lane; the honest verdict on a real rail
    /// stays where it always is — the rehearsal flies the schedule through the N-body integrator.</para>
    /// </summary>
    public static PhasingPlan? PhasingOrbit(
        double radius, double phaseGapRadians, double mu, int revolutions, bool dipInside)
    {
        if (!(radius > 0) || !(mu > 0) || revolutions < 1)
        {
            return null;
        }

        // Normalize the signed lead into "how far ahead along the lane", g ∈ [0, 2π).
        double g = phaseGapRadians % Math.Tau;
        if (g < 0)
        {
            g += Math.Tau;
        }

        double period = LocalCircularPeriod(radius, mu);
        double phasingPeriod = dipInside
            ? period * (1 - g / (Math.Tau * revolutions))
            : period * (1 + (Math.Tau - g) / (Math.Tau * revolutions));
        if (!(phasingPeriod > 0))
        {
            return null;
        }

        // Kepler III backwards: the period names the ellipse, the burn point is one of its apsides
        // (velocity is changed along-track only, so the radius at the burn stays an extremum).
        double semiMajor = Math.Cbrt(mu * Math.Pow(phasingPeriod / Math.Tau, 2));
        double otherApsis = 2 * semiMajor - radius;
        if (otherApsis <= 0)
        {
            return null; // the requested catch-up is too violent for a bound ellipse this way round
        }

        double vCircular = Math.Sqrt(mu / radius);
        double vPhasing = Math.Sqrt(mu * (2 / radius - 1 / semiMajor));
        double deltaV = Math.Abs(vPhasing - vCircular);

        return new PhasingPlan(
            Revolutions: revolutions,
            PhasingPeriod: phasingPeriod,
            SemiMajorAxis: semiMajor,
            EnterDeltaV: deltaV,
            ExitDeltaV: deltaV,
            WaitSeconds: revolutions * phasingPeriod,
            Periapsis: Math.Min(radius, otherApsis),
            Apoapsis: Math.Max(radius, otherApsis));
    }

    /// <summary>Circular-orbit period at <paramref name="radius"/> about <paramref name="mu"/> —
    /// local copy of the Kepler III line so this file stays free of OrbitRule (Core layering:
    /// TransferMath is pure two-body algebra; OrbitRule is ship policy).</summary>
    private static double LocalCircularPeriod(double radius, double mu) =>
        Math.Tau * Math.Sqrt(radius * radius * radius / mu);

    /// <summary>The two-body state produced by <see cref="PropagateKepler"/>: where a ballistic
    /// body is, and how fast, after coasting for the requested time. Both relative to the same
    /// attractor the input state was.</summary>
    public readonly record struct KeplerState(Vector2d Position, Vector2d Velocity);

    /// <summary>
    /// Analytic two-body propagation (Curtis Algorithm 3.4, universal variables): coast a body from
    /// state (<paramref name="position"/>, <paramref name="velocity"/>) relative to an attractor of
    /// parameter <paramref name="mu"/> for <paramref name="dtSeconds"/> seconds, on a pure Kepler
    /// conic — no thrust, no perturbations. This is the RAIL a mass-driver pod (worldbuilding §1,
    /// zero maneuver budget) rides: its position is a closed-form function of time, so a timetable
    /// can name where a pod is at any instant without stepping an integrator. Works for every conic
    /// (ellipse, parabola, hyperbola) via the same Stumpff series the Lambert solver uses.
    ///
    /// <para>Deterministic: fixed Newton budget, seeded analytically, no wall clock — the same
    /// inputs give the same conic point on a platform, exactly as the circular rails do. Negative
    /// <paramref name="dtSeconds"/> propagates backward (where did this pod come from). Returns null
    /// only for degenerate input (non-positive μ or radius); a valid state always has a conic.</para>
    /// </summary>
    public static KeplerState? PropagateKepler(Vector2d position, Vector2d velocity, double dtSeconds, double mu)
    {
        if (!(mu > 0))
        {
            return null;
        }

        double r0 = position.Length;
        if (!(r0 > 0))
        {
            return null;
        }

        if (dtSeconds == 0)
        {
            return new KeplerState(position, velocity);
        }

        double sqrtMu = Math.Sqrt(mu);
        double vr0 = position.Dot(velocity) / r0;             // radial speed at epoch
        double alpha = 2.0 / r0 - velocity.LengthSquared / mu; // 1/a; <0 hyperbolic, 0 parabolic, >0 elliptic

        // Universal anomaly χ. Curtis's analytic seed (χ ≈ √μ·|α|·Δt for an ellipse) converges in a
        // handful of Newton steps for every conic the game flings a pod onto.
        double chi = sqrtMu * Math.Abs(alpha) * dtSeconds;
        for (int i = 0; i < MaxKeplerIterations; i++)
        {
            double chi2 = chi * chi;
            double z = alpha * chi2;
            double c = StumpffC(z);
            double s = StumpffS(z);
            double f = r0 * vr0 / sqrtMu * chi2 * c
                       + (1 - alpha * r0) * chi2 * chi * s
                       + r0 * chi
                       - sqrtMu * dtSeconds;
            double fPrime = r0 * vr0 / sqrtMu * chi * (1 - alpha * chi2 * s)
                            + (1 - alpha * r0) * chi2 * c
                            + r0;
            double delta = f / fPrime;
            chi -= delta;
            if (Math.Abs(delta) < KeplerChiTolerance)
            {
                break;
            }
        }

        double zFinal = alpha * chi * chi;
        double cFinal = StumpffC(zFinal);
        double sFinal = StumpffS(zFinal);

        // Lagrange f and g in universal variables give the new position from the old state directly.
        double fLagrange = 1 - chi * chi / r0 * cFinal;
        double gLagrange = dtSeconds - 1 / sqrtMu * chi * chi * chi * sFinal;
        Vector2d newPosition = position * fLagrange + velocity * gLagrange;

        double r = newPosition.Length;
        double fDot = sqrtMu / (r * r0) * (alpha * chi * chi * chi * sFinal - chi);
        double gDot = 1 - chi * chi / r * cFinal;
        Vector2d newVelocity = position * fDot + velocity * gDot;

        return new KeplerState(newPosition, newVelocity);
    }

    // The Newton correction on χ has units of √m; converge it to a hair (sub-mm on the conic) well
    // inside the shared iteration cap. The cap only bounds the pathological near-parabolic seed.
    private const int MaxKeplerIterations = 24;
    private const double KeplerChiTolerance = 1e-7;

    private static double StumpffC(double z) => z > 1e-8 ? (1 - Math.Cos(Math.Sqrt(z))) / z
        : z < -1e-8 ? (Math.Cosh(Math.Sqrt(-z)) - 1) / -z
        : 0.5 - z / 24 + z * z / 720;

    private static double StumpffS(double z) => z > 1e-8 ? (Math.Sqrt(z) - Math.Sin(Math.Sqrt(z))) / Math.Pow(z, 1.5)
        : z < -1e-8 ? (Math.Sinh(Math.Sqrt(-z)) - Math.Sqrt(-z)) / Math.Pow(-z, 1.5)
        : 1.0 / 6 - z / 120 + z * z / 5040;
}
