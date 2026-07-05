namespace SpaceSails.Core;

/// <summary>
/// M28 · Fire control (Sunday Plan F1). The firing solution is a boundary value problem —
/// "leave the muzzle NOW, be at that point at time t_hit" — solved by the SHOOTING METHOD:
/// Newton iteration over the launch bearing AND ejection speed, where every residual
/// evaluation is one run of the real deterministic <see cref="Simulator"/>. Two unknowns for
/// two constraints (the aim point's x and y): with a fixed muzzle speed exact hits would be
/// measure-zero luck; a mass driver with an adjustable charge makes the problem square.
/// Exactly the Gravity Lab's method (labs/05, labs/12), weaponized; the iteration trace is
/// returned so the gun deck can show the solution converging. Deterministic: no clocks,
/// no randomness.
/// </summary>
public static class FireControl
{
    /// <summary>One Newton step of the solve, for the "CALCULATING FIRING SOLUTION" display.</summary>
    public readonly record struct IterationStep(int Iteration, double BearingRad, double MuzzleSpeed, double MissMeters);

    /// <summary>
    /// A firing solution. <see cref="ExpectedMissMeters"/> is the solver residual — how far
    /// from the aim point the slug passes if the TARGET is exactly where predicted; the
    /// honest dispersion on top of that is the target track's cone half-width at t_hit
    /// (<see cref="PredictedPath.HalfWidthAt"/>) — fire-control quality IS track quality.
    /// <see cref="ValiditySeconds"/> is how long the shooter may delay the shot before the
    /// solution's own miss (same bearing, same charge, later departure) goes stale.
    /// </summary>
    public readonly record struct Solution(
        Vector2d LaunchDirection,
        double BearingRad,
        double MuzzleSpeed,
        double TimeOfFlightSeconds,
        double ExpectedMissMeters,
        double ValiditySeconds,
        bool Converged,
        IReadOnlyList<IterationStep> Trace);

    /// <summary>A solution counts as converged under this residual.</summary>
    public const double ConvergedMissMeters = 1e5;

    /// <summary>A delayed shot is still "valid" while its miss stays under this.</summary>
    public const double StaleMissMeters = 1e6;

    /// <summary>The driver won't eject slower than this fraction of its maximum.</summary>
    public const double MinMuzzleFraction = 0.02;

    /// <summary>The delays probed for the validity window, seconds.</summary>
    private static readonly double[] ValidityProbes = [60, 120, 300, 600];

    /// <summary>
    /// Solve for the launch bearing and ejection speed that put a ballistic slug at
    /// <paramref name="targetPosition"/> at <paramref name="tHit"/>, firing from
    /// <paramref name="shooter"/> now with at most <paramref name="maxMuzzleSpeed"/> relative
    /// to the ship. Damped 2×2 Newton with finite-difference sensitivities; each evaluation
    /// flies the slug through the given <paramref name="simulator"/>.
    /// </summary>
    public static Solution Solve(
        Simulator simulator,
        ShipState shooter,
        double maxMuzzleSpeed,
        Vector2d targetPosition,
        double tHit,
        int maxIterations = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMuzzleSpeed);
        double timeOfFlight = tHit - shooter.SimTime;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeOfFlight);

        // Initial guess: the straight-line requirement — gravity bends it from there.
        Vector2d requiredRel = (targetPosition - shooter.Position) / timeOfFlight - shooter.Velocity;
        double bearing = Math.Atan2(requiredRel.Y, requiredRel.X);
        double speed = Math.Clamp(requiredRel.Length, MinMuzzleFraction * maxMuzzleSpeed, maxMuzzleSpeed);

        var trace = new List<IterationStep>(maxIterations);
        double bestBearing = bearing, bestSpeed = speed;
        double bestMiss = double.MaxValue;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Vector2d residual = MissVector(simulator, shooter, speed, bearing, targetPosition, timeOfFlight);
            double miss = residual.Length;
            trace.Add(new IterationStep(iteration, bearing, speed, miss));
            if (miss < bestMiss)
            {
                (bestMiss, bestBearing, bestSpeed) = (miss, bearing, speed);
            }

            if (miss <= ConvergedMissMeters)
            {
                break;
            }

            // 2×2 Jacobian by forward differences: three simulator flights per iteration.
            const double hBearing = 1e-5;
            double hSpeed = Math.Max(1e-3, speed * 1e-5);
            Vector2d dB = (MissVector(simulator, shooter, speed, bearing + hBearing, targetPosition, timeOfFlight) - residual) / hBearing;
            Vector2d dV = (MissVector(simulator, shooter, speed + hSpeed, bearing, targetPosition, timeOfFlight) - residual) / hSpeed;

            double det = dB.X * dV.Y - dB.Y * dV.X;
            double stepBearing, stepSpeed;
            if (Math.Abs(det) > 1e-12)
            {
                // Newton: J · (dθ, dv) = −r.
                stepBearing = (-residual.X * dV.Y + residual.Y * dV.X) / det;
                stepSpeed = (-dB.X * residual.Y + dB.Y * residual.X) / det;
            }
            else
            {
                // Degenerate Jacobian (e.g. speed pinned at a bound): fall back to
                // Gauss-Newton on the bearing alone.
                double jtj = dB.LengthSquared;
                if (jtj <= 0)
                {
                    break;
                }

                stepBearing = -residual.Dot(dB) / jtj;
                stepSpeed = 0;
            }

            // Trust region: past these the linearization is junk.
            bearing += Math.Clamp(stepBearing, -0.5, 0.5);
            speed = Math.Clamp(speed + Math.Clamp(stepSpeed, -0.25 * maxMuzzleSpeed, 0.25 * maxMuzzleSpeed),
                MinMuzzleFraction * maxMuzzleSpeed, maxMuzzleSpeed);
        }

        bool converged = bestMiss <= ConvergedMissMeters;
        double validity = converged
            ? ValidityWindow(simulator, shooter, bestSpeed, bestBearing, targetPosition, tHit)
            : 0;

        return new Solution(
            new Vector2d(Math.Cos(bestBearing), Math.Sin(bestBearing)),
            bestBearing, bestSpeed, timeOfFlight, bestMiss, validity, converged, trace);
    }

    /// <summary>Where a slug launched NOW on this bearing/charge ends up at t_hit, minus the aim point.</summary>
    private static Vector2d MissVector(
        Simulator simulator, ShipState shooter, double muzzleSpeed, double bearing,
        Vector2d targetPosition, double timeOfFlight)
    {
        var launchDir = new Vector2d(Math.Cos(bearing), Math.Sin(bearing));
        var slug = new ShipState(shooter.Position, shooter.Velocity + launchDir * muzzleSpeed, shooter.SimTime);
        ShipState arrived = simulator.RunAdaptive(slug, timeOfFlight);
        return arrived.Position - targetPosition;
    }

    /// <summary>
    /// How long the shot keeps: re-fly the SAME bearing and charge from progressively later
    /// departures (the shooter having coasted in the meantime), same t_hit. The window is
    /// the largest probed delay whose miss stays under <see cref="StaleMissMeters"/>.
    /// </summary>
    private static double ValidityWindow(
        Simulator simulator, ShipState shooter, double muzzleSpeed, double bearing,
        Vector2d targetPosition, double tHit)
    {
        double window = 0;
        foreach (double delay in ValidityProbes)
        {
            double timeOfFlight = tHit - shooter.SimTime - delay;
            if (timeOfFlight <= 0)
            {
                break;
            }

            ShipState coasted = simulator.RunAdaptive(shooter, delay);
            Vector2d miss = MissVector(simulator, coasted, muzzleSpeed, bearing, targetPosition, timeOfFlight);
            if (miss.Length > StaleMissMeters)
            {
                break;
            }

            window = delay;
        }

        return window;
    }
}
