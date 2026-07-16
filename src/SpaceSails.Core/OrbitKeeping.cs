namespace SpaceSails.Core;

/// <summary>
/// Station-keeping — the autopilot's promise once it has PARKED (Friday §0, owner ruling: "armed
/// auto-orbit ends in a KEPT orbit, not an achieved one"). A parked orbit at a small moon is not
/// static: the parent's tide pumps the two-body eccentricity, and left alone the orbit drifts out of
/// the tide-stable band and strips over hours (the owner's stranded Enceladus ship, #180). Keeping
/// holds the park with periodic TRIM burns — re-circularizing at the current radius the moment the
/// tide has pumped the eccentricity past a tight tolerance — priced in pulses like every assisted
/// burn. The tolerance and the per-body trim BUDGET come from Lab 25 "The tide that takes it back",
/// measured honestly in the real N-body sim, not guessed.
///
/// <para>The trim burn is exactly <see cref="OrbitRule.Insert"/> — the same circularization the
/// insertion flew — so keeping spends with the SAME pulse kernel as everything else; there is no
/// second pricing source to drift.</para>
/// </summary>
public static class OrbitKeeping
{
    /// <summary>Re-circularize once the parent's tide has pumped the two-body eccentricity past this.
    /// Lab 25's drift sweep: at Enceladus/Luna/Titan a 0.02 band holds the whole orbit inside the
    /// tide-stable zone over a 30-day propagation while keeping trims rare (a few per day at most).
    /// Tighter wastes pulses on noise; looser lets the apoapsis reach the chaotic band between trims.</summary>
    public const double TrimEccentricity = 0.02;

    /// <summary>How often keeping CONSIDERS a trim, as a fraction of the local park period. The
    /// parent's tide forces a bounded eccentricity that reverses every orbit; correcting it on every
    /// tick pays for motion the tide would undo for free (Lab 25's treadmill). Checking once every
    /// ~quarter park period lets the reversible oscillation reverse itself, so the ship pays for the
    /// SECULAR drift alone — dramatically cheaper, and still far finer than the drift timescale.</summary>
    public const double TrimCadenceFraction = 0.25;

    /// <summary>Two-body elements of the ship about a body — the same conic reduction
    /// <see cref="OrbitRule.ParkStability"/> uses, exposed so keeping reads eccentricity directly.</summary>
    /// <param name="SemiMajorAxis">a (m); meaningful only when <paramref name="Bound"/>.</param>
    /// <param name="Eccentricity">e (dimensionless).</param>
    /// <param name="Radius">Current body-relative distance (m).</param>
    /// <param name="Bound">Negative two-body energy — a real closed orbit.</param>
    public readonly record struct Elements(double SemiMajorAxis, double Eccentricity, double Radius, bool Bound);

    /// <summary>Reduce the ship's body-relative state to its two-body conic elements. Energy and
    /// specific angular momentum give a and e (same algebra as the transfer/insert math).</summary>
    public static Elements OrbitElements(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body)
    {
        Vector2d r = ship.Position - bodyPos;
        double radius = r.Length;
        double mu = body.Mu;
        if (!(radius > 0) || !(mu > 0))
        {
            return new Elements(0, 0, radius, false);
        }

        Vector2d v = ship.Velocity - bodyVel;
        double energy = v.LengthSquared / 2 - mu / radius;
        if (energy >= 0)
        {
            return new Elements(0, 0, radius, false);
        }

        double h = r.X * v.Y - r.Y * v.X;
        double a = -mu / (2 * energy);
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (mu * mu)));
        return new Elements(a, e, radius, true);
    }

    /// <summary>True when the parked orbit has drifted enough that a trim is due — the two-body
    /// eccentricity has been pumped past <paramref name="eccTol"/>. An unbound state never needs a
    /// trim (keeping is over; the backstop degradation alert owns that failure).</summary>
    public static bool NeedsTrim(
        ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body, double eccTol = TrimEccentricity)
    {
        Elements el = OrbitElements(ship, bodyPos, bodyVel, body);
        return el.Bound && el.Eccentricity > eccTol;
    }

    /// <summary>The velocity a trim burn sets — a purely tangential burn (prograde, keeping the
    /// ship's current swing sense) whose SPEED pins the two-body semi-major axis to
    /// <paramref name="parkRadius"/> via vis-viva: v = √(μ(2/r − 1/park)). This is the crucial
    /// difference from a naive re-circularize-at-current-radius: pinning the energy to the PARK
    /// orbit stops the semi-major axis random-walking into the surface when trims fire off-radius —
    /// the deep-well failure Lab 25 measured (at Enceladus, re-circularizing at the current radius
    /// crashed the ship; pinning a=park holds it). At the park radius it is exactly the circular
    /// insert; off it, the current radius becomes an apsis of a park-sized orbit that oscillates
    /// back, so the residual eccentricity stays bounded by the radial offset |r−park|/park.</summary>
    public static Vector2d TrimVelocity(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body, double parkRadius)
    {
        Vector2d radial = ship.Position - bodyPos;
        double r = radial.Length;
        if (!(r > 0) || !(body.Mu > 0))
        {
            return ship.Velocity;
        }
        double spd2 = body.Mu * (2.0 / r - 1.0 / parkRadius);
        double spd = spd2 > 0 ? Math.Sqrt(spd2) : Math.Sqrt(body.Mu / r); // guard: fall back to circular
        Vector2d tangent = new Vector2d(-radial.Y, radial.X) / r;
        Vector2d relVel = ship.Velocity - bodyVel;
        if (radial.X * relVel.Y - radial.Y * relVel.X < 0)
        {
            tangent = -tangent;
        }
        return bodyVel + tangent * spd;
    }

    /// <summary>The trim burn's Δv from the current state — the cost to pin the orbit back to the
    /// park radius (see <see cref="TrimVelocity"/>).</summary>
    public static double TrimDeltaV(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body, double parkRadius) =>
        (TrimVelocity(ship, bodyPos, bodyVel, body, parkRadius) - ship.Velocity).Length;

    /// <summary>Mass-pulse cost of one trim burn from the current state (at least 1) — priced with the
    /// SAME <see cref="OrbitRule.PulsesFor"/> kernel every other assisted burn spends with.</summary>
    public static int TrimPulseCost(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body, double parkRadius) =>
        OrbitRule.PulsesFor(TrimDeltaV(ship, bodyPos, bodyVel, body, parkRadius), ship.Velocity.Length);

    /// <summary>Apply the trim: pin the orbit's energy back to the park radius (<see cref="TrimVelocity"/>).
    /// Impulsive, like every pulse in the game.</summary>
    public static ShipState Trim(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body, double parkRadius) =>
        ship with { Velocity = TrimVelocity(ship, bodyPos, bodyVel, body, parkRadius) };

    /// <summary>The measured station-keeping demand of one body's park (Lab 25). Frame-independent
    /// physics: the tide's Δv/day and how many trims a day it forces. Pulses/day is derived from these
    /// at the ship's real world speed by <see cref="TrimPulsesPerDay"/>, because a pulse buys Δv as a
    /// fraction of heliocentric speed and a park deep in a giant's well rides fast.</summary>
    /// <param name="BodyId">The body this profile was measured for.</param>
    /// <param name="ParkHillFraction">Where the autopilot parks, as a fraction of the Hill sphere
    /// (informational — <see cref="OrbitRule.ParkStableHillFraction"/>).</param>
    /// <param name="TrimDvPerDay">Total trim Δv the tide forces per day at the park (m/s/day).</param>
    /// <param name="TrimsPerDay">How many discrete trim burns per day the tolerance triggers.</param>
    public readonly record struct KeepProfile(string BodyId, double ParkHillFraction, double TrimDvPerDay, double TrimsPerDay);

    /// <summary>The trim budget quoted at arm time, in mass pulses per day, at the parked ship's world
    /// (heliocentric) speed. Each trim's Δv is <see cref="KeepProfile.TrimDvPerDay"/> spread over the
    /// day's trims; each is priced with <see cref="OrbitRule.PulsesFor"/> and summed — so a deep, fast
    /// park (Enceladus rides ~10 km/s of Saturn) prices cheaper per m/s than a slow one.</summary>
    public static int TrimPulsesPerDay(KeepProfile profile, double worldSpeed)
    {
        if (profile.TrimsPerDay <= 0 || profile.TrimDvPerDay <= 0)
        {
            return 0;
        }
        double dvPerTrim = profile.TrimDvPerDay / profile.TrimsPerDay;
        int pulsesPerTrim = OrbitRule.PulsesFor(dvPerTrim, worldSpeed);
        return Math.Max(1, (int)Math.Ceiling(pulsesPerTrim * profile.TrimsPerDay));
    }

    /// <summary>The differential tidal acceleration the parent raises across the park orbit — the
    /// physics that drives the whole keeping bill (m/s²): a_tide ≈ 2·μ_parent·r / D³, the gradient of
    /// the parent's pull over the park radius <paramref name="parkRadius"/> at the moon's distance
    /// <paramref name="parentDistance"/> from its parent of gravitational parameter
    /// <paramref name="parentMu"/>. Used by <see cref="EstimateProfile"/> to price bodies Lab 25 never
    /// measured, and printed in the lab as the calibration axis.</summary>
    public static double TideAcceleration(double parkRadius, double parentMu, double parentDistance) =>
        parentDistance > 0 ? 2 * parentMu * parkRadius / (parentDistance * parentDistance * parentDistance) : 0;

    /// <summary>Dimensionless calibration constant fitting <see cref="EstimateProfile"/> to Lab 25's
    /// three measured moons: TrimDvPerDay ≈ Ktide · a_tide · 86400. Set to the mean Ktide the lab
    /// printed (Section C, 2026-07-17 run: 2.004). Only the fallback for un-measured bodies leans on
    /// it; the moons that ship today (Enceladus/Luna/Titan) quote from the measured table.</summary>
    public const double TideBudgetConstant = 2.0; // mean Ktide, Lab 25 — see OrbitKeepingTable

    /// <summary>A physics estimate of a body's keeping demand for bodies Lab 25 never measured — the
    /// honest fallback so the arm-time quote is never silent. Δv/day scales with the tide the parent
    /// raises across the park (<see cref="TideAcceleration"/>), calibrated by
    /// <see cref="TideBudgetConstant"/>; trims/day is derived from the tolerance and the park period.</summary>
    public static KeepProfile EstimateProfile(CelestialBody body, double hill, double parentMu, double parentDistance)
    {
        double park = OrbitRule.ParkingRadius(body, hill);
        double aTide = TideAcceleration(park, parentMu, parentDistance);
        double dvPerDay = TideBudgetConstant * aTide * 86400.0;
        // Trims/day: the tide pumps e to the tolerance over roughly the park orbital period times a
        // tolerance-scaled number of laps; one trim empties it. A floor of one keeps a quote non-zero.
        double period = OrbitRule.LocalOrbitPeriod(park, body.Mu);
        double trimsPerDay = period > 0 ? Math.Max(1.0, 86400.0 / period) : 1.0;
        return new KeepProfile(body.Id, OrbitRule.ParkStableHillFraction, dvPerDay, trimsPerDay);
    }
}
