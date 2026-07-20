namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 37, THE SLOW HAND. The gravity-tractor deflection: no charge, no collision — the ship simply
/// STATION-KEEPS a fixed standoff off the rock for days, weeks or years and lets its own feeble gravity tow
/// the orbit over. Pure, deterministic Core math (repo law §9). The tug's gravity pulls the rock at
/// a = G·m_ship/d²; held continuously over a lead time T it accumulates a miss of ≈1.5·a·T² (a continuous
/// ramp is half the impulsive 3·Δv·T leverage). The number that falls out is the whole lesson: the pull is
/// so tiny that the tractor only works with YEARS of warning — early detection is not a nicety, it is the
/// entire technique.
///
/// <para>Ties to Lab 25 (orbit-keeping): to hover the tug must continuously thrust AWAY from the rock, or
/// it falls in — the same station-keeping discipline the autopilot spends at a moon, here spent to hold a
/// standoff instead of a park. <see cref="HoverThrust"/> prices that hold. NOT a shipped gig; this certifies
/// the physics and prints how early you must arrive for a gravity tractor to save Ringside — the
/// early-detection-pays lesson, quantified.</para>
/// </summary>
public static class GravityTractor
{
    /// <summary>Newton's gravitational constant (m³·kg⁻¹·s⁻²).</summary>
    public const double G = 6.674e-11;

    /// <summary>A lab-reference tug mass (kg) — ~100 t, a heavy hauler pressed into towing. (Core carries no
    /// ship-mass constant; this is the lab's stated assumption, swept for sensitivity in the probe.)</summary>
    public const double ReferenceShipMassKg = 1.0e5;

    /// <summary>The standoff distance as a multiple of the rock's radius — far enough that the hover
    /// thrusters' plume clears the surface, close enough that the pull is worth having. Lab-representative.</summary>
    public const double StandoffFactor = 1.5;

    /// <summary>The hover standoff distance (m, rock-centre to ship) for a rock of
    /// <paramref name="radiusMeters"/>: <see cref="StandoffFactor"/>·r.</summary>
    public static double Standoff(double radiusMeters) => StandoffFactor * radiusMeters;

    /// <summary>The acceleration (m/s²) the tug's gravity imparts to the rock: a = G·m_ship/d². Pure.</summary>
    public static double TugAcceleration(double shipMassKg, double standoffMeters) =>
        standoffMeters <= 0.0 ? 0.0 : G * shipMassKg / (standoffMeters * standoffMeters);

    /// <summary>The continuous-tow leverage: a constant tug acceleration held over lead time T opens a miss
    /// of 1.5·a·T² (integrating the along-track 3·Δv·(T−t) leverage over a linearly-growing Δv). Pinned.</summary>
    public const double ContinuousTowLeverage = 1.5;

    /// <summary>The miss distance (m) a steady tug acceleration <paramref name="accel"/> opens over
    /// <paramref name="leadSeconds"/> of towing: <see cref="ContinuousTowLeverage"/>·a·T². Pure.</summary>
    public static double Miss(double accel, double leadSeconds) =>
        ContinuousTowLeverage * accel * leadSeconds * leadSeconds;

    /// <summary>The warning time (s) a tug of <paramref name="accel"/> needs to open a miss of
    /// <paramref name="missMeters"/>: solve miss = 1.5·a·T² for T. The headline — how early you must arrive.</summary>
    public static double RequiredLeadSeconds(double accel, double missMeters) =>
        accel <= 0.0 ? double.PositiveInfinity : System.Math.Sqrt(missMeters / (ContinuousTowLeverage * accel));

    /// <summary>The warning time (s) to deflect a rock of <paramref name="type"/>/<paramref name="radiusMeters"/>
    /// by <paramref name="missMeters"/> with a tug of <paramref name="shipMassKg"/> hovering at the standard
    /// standoff. Convenience over <see cref="TugAcceleration"/> + <see cref="RequiredLeadSeconds"/>. Pure.</summary>
    public static double RequiredLeadSeconds(RockType type, double radiusMeters, double shipMassKg, double missMeters)
    {
        double a = TugAcceleration(shipMassKg, Standoff(radiusMeters));
        return RequiredLeadSeconds(a, missMeters);
    }

    /// <summary>The thrust (N) the tug must sustain to HOVER — it must cancel the rock's pull on itself,
    /// F = m_ship·G·M_rock/d², or it falls onto the rock. This is the Lab-25 station-keeping bill of the
    /// slow hand: not Δv but DURATION. Pure.</summary>
    public static double HoverThrust(double shipMassKg, double asteroidMassKg, double standoffMeters) =>
        standoffMeters <= 0.0 ? 0.0 : shipMassKg * G * asteroidMassKg / (standoffMeters * standoffMeters);
}
