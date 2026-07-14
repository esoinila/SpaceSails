namespace SpaceSails.Core;

/// <summary>
/// An optional exponential atmosphere shell on a celestial body (the flight-assists "skim/skip"
/// ingredient, PR-H). Density falls off exponentially with altitude above the body's surface and
/// is cut to exactly zero at and above <see cref="TopAltitude"/>, so the shell is a bounded,
/// well-defined region the integrator only ever touches deliberately.
///
/// <para><b>Deterministic and additive.</b> A body with no atmosphere carries <c>null</c> here and
/// feels zero drag on the exact byte-identical path it always flew. Nothing about this model reads
/// a wall clock or randomness — <see cref="DensityAt"/> is a pure function of altitude.</para>
///
/// <para><b>What it models and what it ignores.</b> An isothermal exponential atmosphere,
/// co-rotating with the body only in the sense of translation (the shell moves with the body's
/// orbital velocity); it deliberately ignores the body's spin, lift, and any heating physics —
/// the game charges "too deep" as hull damage, priced off <see cref="Simulator.DragReport"/>'s
/// peak deceleration, not a thermal model. All units SI: kg/m^3, meters.</para>
/// </summary>
public sealed record Atmosphere(double RefDensity, double ScaleHeight, double TopAltitude)
{
    /// <summary>
    /// Local mass density (kg/m^3) at an <paramref name="altitude"/> (meters) above the body's
    /// surface: <c>RefDensity · exp(−altitude / ScaleHeight)</c> inside the shell, exactly zero at
    /// or below the surface and at or above <see cref="TopAltitude"/>.
    /// </summary>
    public double DensityAt(double altitude)
    {
        if (altitude <= 0.0 || altitude >= TopAltitude)
        {
            return 0.0;
        }

        return RefDensity * Math.Exp(-altitude / ScaleHeight);
    }
}
