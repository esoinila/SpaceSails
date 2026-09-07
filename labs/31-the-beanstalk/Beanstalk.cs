// Lab 31 — The beanstalk. The arithmetic, in one place, so exactly one copy of it exists.
//
// This file is COMPILED TWICE ON PURPOSE: once into the lab's probe (labs/31-the-beanstalk), which
// prints the tables and the verdicts, and once into tests/SpaceSails.Core.Tests, which links it
// (<Compile Include=... Link=...>) and holds the verdicts to their own numbers. That is the whole
// anti-drift design: the sentence the README publishes and the number the guard checks come out of
// the same function, so a lab cannot quietly stop meaning what it printed.
//
// The physics is the standard uniform-stress (constant-stress) tether relation. A cable that is
// everywhere at the same stress sigma, hanging in an effective potential Phi (gravity plus the
// centrifugal term of the frame it co-rotates with), must taper as
//
//     A(r) / A(anchor) = exp( rho * [Phi(r_max) - Phi(r)] / sigma )
//
// so the whole cable's TAPER RATIO — widest cross-section over narrowest — is
//
//     taper = exp( dPhi / (sigma/rho) )
//
// where dPhi is the effective-potential climb from the surface anchor to the point of maximum
// tension, and sigma/rho is the material's SPECIFIC STRENGTH in J/kg. Everything below is that one
// exponential, evaluated honestly per body. Sources are cited in README.md.
//
// IRONCLAD RULE (labs/README.md): every number in labs/31-the-beanstalk/README.md came from running
// Probe.cs. Change this file and the README goes stale — rerun and re-paste, never hand-edit a table.

namespace SpaceSails.Labs.Lab31;

/// <summary>How honestly you can buy a material, which is a different question from whether it exists.</summary>
public enum MaterialTier
{
    /// <summary>Sold by the kilometre today, by more than one supplier, with a datasheet.</summary>
    Commercial,

    /// <summary>The best specimen anyone has published. Real, measured, and not a supply chain.</summary>
    LaboratoryRecord,

    /// <summary>A calculated ceiling for a perfect single tube. Not a material; a limit.</summary>
    Theoretical,
}

/// <summary>Where the cable's far end is tied, which decides which potential you climb.</summary>
public enum AnchorKind
{
    /// <summary>The body spins fast enough to have a synchronous orbit above its surface: the cable
    /// stands up on its own rotation, and maximum tension is at the synchronous radius.</summary>
    Synchronous,

    /// <summary>A tidally locked satellite has no synchronous orbit inside its Hill sphere — its
    /// "day" is its year. The cable is hung from the primary instead, through the interior Lagrange
    /// point, where maximum tension sits.</summary>
    ThroughL1,
}

/// <summary>A tether material. <see cref="SpecificStrength"/> is the only property the cable cares about.</summary>
/// <param name="Name">Display name, as printed.</param>
/// <param name="ShortName">Column-header name, twelve characters or fewer.</param>
/// <param name="TensileGPa">Tensile strength, GPa.</param>
/// <param name="DensityKgM3">Density, kg/m^3.</param>
/// <param name="Tier">How buyable it is.</param>
public sealed record TetherMaterial(string Name, string ShortName, double TensileGPa, double DensityKgM3, MaterialTier Tier)
{
    /// <summary>sigma/rho in J/kg — strength per unit mass, the number the exponential eats.</summary>
    public double SpecificStrength => TensileGPa * 1e9 / DensityKgM3;

    /// <summary>Real means measured on something someone actually made.</summary>
    public bool IsReal => Tier != MaterialTier.Theoretical;
}

/// <summary>A body a cable could be tied to, with the constants the climb is computed from.</summary>
/// <param name="Name">Display name, as printed.</param>
/// <param name="Anchor">Which potential the cable climbs.</param>
/// <param name="Mu">Body gravitational parameter, m^3/s^2.</param>
/// <param name="Radius">Body radius, m.</param>
/// <param name="SpinRate">Sidereal spin rate, rad/s. For a tidally locked satellite this is the
/// orbital mean motion, and is recomputed from the primary rather than trusted.</param>
/// <param name="PrimaryMu">Primary's gravitational parameter, m^3/s^2. Zero for a synchronous anchor.</param>
/// <param name="PrimaryDistance">Orbit radius about the primary, m. Zero for a synchronous anchor.</param>
/// <param name="Source">Where the constants came from — the lab's honesty column.</param>
public sealed record TetherBody(
    string Name,
    AnchorKind Anchor,
    double Mu,
    double Radius,
    double SpinRate,
    double PrimaryMu,
    double PrimaryDistance,
    string Source)
{
    /// <summary>Surface gravity mu/R^2, m/s^2 — used only to express the climb as a length.</summary>
    public double SurfaceGravity => Mu / (Radius * Radius);
}

/// <summary>Everything the arithmetic says about one body's cable.</summary>
/// <param name="Body">The body measured.</param>
/// <param name="AnchorRadius">Radius of the maximum-tension point from the body's centre, m.</param>
/// <param name="AnchorAltitude">That point's altitude above the surface, m.</param>
/// <param name="DeltaPotential">Effective-potential climb surface -> maximum tension, J/kg.</param>
/// <param name="CharacteristicLength">dPhi divided by the body's own surface gravity, m.</param>
/// <param name="LaunchDeltaV">Ideal propulsive cost of putting a parcel where the cable puts it free, m/s.</param>
/// <param name="PropellantPerTonneKg">…expressed as kg of propellant for a 1 t parcel at the lab's Isp.</param>
public sealed record Climb(
    TetherBody Body,
    double AnchorRadius,
    double AnchorAltitude,
    double DeltaPotential,
    double CharacteristicLength,
    double LaunchDeltaV,
    double PropellantPerTonneKg);

/// <summary>The lab's arithmetic. Pure functions over constants; no I/O, so the guard can link it.</summary>
public static class Beanstalk
{
    // ---------------------------------------------------------------------------------------------
    // The thresholds. Stated BEFORE the data, so a threshold is a threshold and not a description
    // (lab 46's rule). A taper ratio of 10 means the cable is ten times fatter at its waist than at
    // its anchor; past that the mass of the thing runs away and nobody costs it seriously.
    // ---------------------------------------------------------------------------------------------

    /// <summary>Taper ratio at or below which a cable is considered practical to build.</summary>
    public const double PracticalTaper = 10.0;

    /// <summary>Taper ratio at or below which the material has stopped mattering at all: a cable
    /// this close to uniform is a rope, and you would size it for handling, not for stress.</summary>
    public const double RoundingErrorTaper = 1.1;

    /// <summary>Specific impulse of the chemical stage the cable is being compared against, s.</summary>
    public const double ComparisonIsp = 320.0;

    /// <summary>Standard gravity, m/s^2 — the rocket-equation constant, not a body's gravity.</summary>
    public const double G0 = 9.80665;

    /// <summary>Parcel mass the propellant column is quoted for, kg.</summary>
    public const double ParcelMassKg = 1000.0;

    /// <summary>A working-stress safety factor, applied only in the sensitivity section.</summary>
    public const double SafetyFactor = 2.0;

    // ---------------------------------------------------------------------------------------------
    // Verdict labels. These are the lab's published sentences; the guard pins each body to one.
    // ---------------------------------------------------------------------------------------------

    /// <summary>Nothing anyone has ever made gets the taper under <see cref="PracticalTaper"/>.</summary>
    public const string VerdictBeyond = "BEYOND ANY REAL MATERIAL";

    /// <summary>Only the best fibre ever measured clears the threshold. Not a supply chain.</summary>
    public const string VerdictOnlyBest = "ONLY WITH THE BEST FIBRE EVER SPUN";

    /// <summary>A fibre with a datasheet and a price clears it.</summary>
    public const string VerdictFibreToday = "BUILDABLE WITH FIBRE YOU CAN BUY TODAY";

    /// <summary>Steel wire clears it. Steel wire.</summary>
    public const string VerdictSteel = "BUILDABLE WITH STEEL WIRE";

    /// <summary>Even steel barely tapers: the cable is a rope and the counterweight is a rock.</summary>
    public const string VerdictRope = "A LONG ROPE WITH A ROCK ON THE END";

    /// <summary>The material table, ordered from most mundane to least. The order is load-bearing:
    /// the verdict names the FIRST material down this list that clears the threshold.</summary>
    public static IReadOnlyList<TetherMaterial> Materials { get; } =
    [
        // Representative published values for each fibre class — a class, not one lot's certificate.
        new("steel wire", "steel wire", 2.0, 7900, MaterialTier.Commercial),
        new("Kevlar 49", "Kevlar 49", 3.6, 1440, MaterialTier.Commercial),
        new("Zylon PBO", "Zylon PBO", 5.8, 1560, MaterialTier.Commercial),
        new("CNT fibre, best spun", "CNT fibre", 6.0, 1300, MaterialTier.LaboratoryRecord),
        new("CNT single tube, theory", "CNT theory", 130.0, 1300, MaterialTier.Theoretical),
    ];

    /// <summary>The bodies. Constants marked "sol.json" are the game's OWN ephemeris, verbatim, and
    /// <c>Lab31BeanstalkTests</c> reads the scenario file to prove this copy has not drifted from it.
    /// Bodies the game does not carry (Ceres, Deimos) are cited to the usual references instead.</summary>
    public static IReadOnlyList<TetherBody> Bodies { get; } =
    [
        // Earth: sidereal day 86164.0905 s.
        new("Earth", AnchorKind.Synchronous, 3.986004418e14, 6.371e6, 2 * Math.PI / 86164.0905, 0, 0,
            "sol.json + sidereal day"),

        // Mars: sidereal day 88642.663 s (24h 37m 22.66s).
        new("Mars", AnchorKind.Synchronous, 4.282837e13, 3.3895e6, 2 * Math.PI / 88642.663, 0, 0,
            "sol.json + sidereal day"),

        // Luna: tidally locked, so its "synchronous" radius is outside its own Hill sphere. Hung
        // from Earth through the Earth-Moon L1 instead.
        new("Luna", AnchorKind.ThroughL1, 4.9048695e12, 1.7374e6, 0, 3.986004418e14, 3.844e8, "sol.json"),

        // Ceres: JPL SBDB GM = 62.6284 km^3/s^2, mean radius 469.7 km, rotation 9.074170 h.
        new("Ceres", AnchorKind.Synchronous, 6.26284e10, 4.697e5, 2 * Math.PI / 32667.0, 0, 0, "JPL SBDB"),

        // Phobos and Deimos: tidally locked, and their Hill spheres barely clear their own surfaces,
        // which is the finding rather than an inconvenience.
        new("Phobos", AnchorKind.ThroughL1, 7.1e5, 1.1e4, 0, 4.282837e13, 9.377e6, "sol.json"),
        new("Deimos", AnchorKind.ThroughL1, 9.615e4, 6.2e3, 0, 4.282837e13, 2.3463e7, "JPL Mars fact sheet"),
    ];

    /// <summary>The one exponential this whole lab is about.</summary>
    public static double Taper(double deltaPotential, TetherMaterial material) =>
        Math.Exp(deltaPotential / material.SpecificStrength);

    /// <summary>Specific strength a material would need to bring this climb in at the threshold.</summary>
    public static double SpecificStrengthNeeded(double deltaPotential) =>
        deltaPotential / Math.Log(PracticalTaper);

    /// <summary>Synchronous radius: where a circular orbit's period equals the body's day.</summary>
    public static double SynchronousRadius(TetherBody body) =>
        Math.Cbrt(body.Mu / (body.SpinRate * body.SpinRate));

    /// <summary>Mean motion of a locked satellite about its primary, rad/s — recomputed from the two
    /// masses and the separation rather than read off a stated period, so the frame the potential is
    /// written in and the gravity in it are the same physics.</summary>
    public static double MeanMotion(TetherBody body) =>
        Math.Sqrt((body.PrimaryMu + body.Mu) / (body.PrimaryDistance * body.PrimaryDistance * body.PrimaryDistance));

    /// <summary>Distance from the satellite's centre to the interior Lagrange point L1, m. Found by
    /// bisection on the rotating-frame gradient rather than by the cube-root approximation, because
    /// at Phobos the approximation and the answer differ by more than the answer clears the ground.</summary>
    public static double L1Distance(TetherBody body)
    {
        double omega = MeanMotion(body);
        double omega2 = omega * omega;
        double a = body.PrimaryDistance;
        double xSat = a * body.PrimaryMu / (body.PrimaryMu + body.Mu);   // satellite, barycentric

        // Net outward acceleration at distance d sunward... primary-ward of the satellite. Negative
        // near the surface (the satellite still wins), positive far out (the primary and the
        // centrifugal term win); the crossing is L1.
        double Gradient(double d)
        {
            double x = xSat - d;
            double rp = x + a * body.Mu / (body.PrimaryMu + body.Mu);    // distance to the primary
            return body.PrimaryMu / (rp * rp) - body.Mu / (d * d) - omega2 * x;
        }

        double lo = body.Radius, hi = 0.5 * a;
        for (int i = 0; i < 200; i++)
        {
            double mid = 0.5 * (lo + hi);
            if (Gradient(mid) < 0) lo = mid; else hi = mid;
        }

        return 0.5 * (lo + hi);
    }

    /// <summary>Hill radius of a satellite about its primary, m — the ceiling any tether tied to it
    /// must fit under.</summary>
    public static double HillRadius(TetherBody body) =>
        body.PrimaryDistance * Math.Cbrt(body.Mu / (3 * body.PrimaryMu));

    /// <summary>Measure one body's cable: where the tension peaks, how far up the potential that is,
    /// and what the propulsive alternative costs.</summary>
    public static Climb Measure(TetherBody body) => body.Anchor switch
    {
        AnchorKind.Synchronous => MeasureSynchronous(body),
        AnchorKind.ThroughL1 => MeasureThroughL1(body),
        _ => throw new ArgumentOutOfRangeException(nameof(body)),
    };

    /// <summary>Every body, measured, in table order.</summary>
    public static IReadOnlyList<Climb> MeasureAll() => [.. Bodies.Select(Measure)];

    private static Climb MeasureSynchronous(TetherBody body)
    {
        double omega2 = body.SpinRate * body.SpinRate;
        double rSync = SynchronousRadius(body);

        // Effective potential in the co-rotating frame: gravity plus the centrifugal term.
        double Phi(double r) => -body.Mu / r - 0.5 * omega2 * r * r;
        double dPhi = Phi(rSync) - Phi(body.Radius);

        // The propulsive alternative: reach a grazing circular orbit (helped by the ground's own
        // rotation), then Hohmann out to the synchronous radius and circularise — which is exactly
        // the orbit a climber is handed when it lets go at the top. Ideal: no gravity or drag losses.
        double vCirc = Math.Sqrt(body.Mu / body.Radius);
        double dv1 = vCirc - body.SpinRate * body.Radius;
        double aT = 0.5 * (body.Radius + rSync);
        double vp = Math.Sqrt(body.Mu * (2 / body.Radius - 1 / aT));
        double va = Math.Sqrt(body.Mu * (2 / rSync - 1 / aT));
        double dv = dv1 + (vp - vCirc) + (Math.Sqrt(body.Mu / rSync) - va);

        return new Climb(body, rSync, rSync - body.Radius, dPhi, dPhi / body.SurfaceGravity, dv,
            PropellantPerTonne(dv));
    }

    private static Climb MeasureThroughL1(TetherBody body)
    {
        double omega = MeanMotion(body);
        double omega2 = omega * omega;
        double a = body.PrimaryDistance;
        double xSat = a * body.PrimaryMu / (body.PrimaryMu + body.Mu);
        double xPrimary = -a * body.Mu / (body.PrimaryMu + body.Mu);
        double dL1 = L1Distance(body);

        // Rotating-frame effective potential of the circular restricted three-body problem, along
        // the line joining the two bodies. Written as three separated differences rather than as a
        // difference of two sums: at Phobos the climb is ~2 kJ/kg out of a 7 MJ/kg potential, and
        // subtracting two nearly equal seven-digit numbers is how a lab loses its own finding.
        double xSurface = xSat - body.Radius;                     // the sub-primary point on the ground
        double xL1 = xSat - dL1;
        double rpSurface = xSurface - xPrimary;
        double rpL1 = xL1 - xPrimary;

        double dPhi = body.PrimaryMu * (1 / rpSurface - 1 / rpL1)
                      + body.Mu * (1 / body.Radius - 1 / dL1)
                      - 0.5 * omega2 * (xL1 * xL1 - xSurface * xSurface);

        // The propulsive alternative: off the ground into a grazing circular orbit, Hohmann out to
        // L1's distance, then match the speed L1 itself carries (it is a fixed point of the ROTATING
        // frame, so in the satellite's inertial frame it sweeps a circle at the mean motion).
        // Satellite-centred two-body throughout: the primary's tide — which is the whole reason L1
        // exists — is deliberately left out of the ROCKET's bill, so the comparison never flatters
        // the cable.
        double vCirc = Math.Sqrt(body.Mu / body.Radius);
        double dv1 = vCirc - omega * body.Radius;
        double aT = 0.5 * (body.Radius + dL1);
        double vp = Math.Sqrt(body.Mu * (2 / body.Radius - 1 / aT));
        double va = Math.Sqrt(body.Mu * (2 / dL1 - 1 / aT));
        double dv = dv1 + (vp - vCirc) + Math.Abs(omega * dL1 - va);

        return new Climb(body, dL1, dL1 - body.Radius, dPhi, dPhi / body.SurfaceGravity, dv,
            PropellantPerTonne(dv));
    }

    /// <summary>Propellant a chemical stage burns to do by rocket what the climber does on mains
    /// power, for a <see cref="ParcelMassKg"/> parcel — the rocket equation, kg.</summary>
    public static double PropellantPerTonne(double deltaV) =>
        ParcelMassKg * (Math.Exp(deltaV / (ComparisonIsp * G0)) - 1);

    /// <summary>The cheapest material on the list that brings this climb in under the threshold, or
    /// null if nothing does — including the theoretical ceiling, which is on the list precisely so
    /// that "nothing" can be said with a number behind it.</summary>
    public static TetherMaterial? CheapestThatWorks(double deltaPotential) =>
        Materials.FirstOrDefault(m => Taper(deltaPotential, m) <= PracticalTaper);

    /// <summary>The best REAL material on the list — the honest ceiling of what has been made.</summary>
    public static TetherMaterial BestReal { get; } = Materials.Where(m => m.IsReal).MaxBy(m => m.SpecificStrength)!;

    /// <summary>THE VERDICT. Graded off the taper numbers and nothing else.</summary>
    public static string Verdict(double deltaPotential)
    {
        TetherMaterial steel = Materials[0];
        double steelTaper = Taper(deltaPotential, steel);
        if (steelTaper <= RoundingErrorTaper) return VerdictRope;
        if (steelTaper <= PracticalTaper) return VerdictSteel;

        TetherMaterial? cheapest = CheapestThatWorks(deltaPotential);
        return cheapest?.Tier switch
        {
            MaterialTier.Commercial => VerdictFibreToday,
            MaterialTier.LaboratoryRecord => VerdictOnlyBest,
            _ => VerdictBeyond,     // only the theory clears it, or nothing does: same answer
        };
    }

    /// <summary>The line the probe prints and the README carries, verbatim — verdict plus the two
    /// numbers it was read off, so the sentence can never be quoted without its arithmetic.</summary>
    public static string VerdictLine(Climb climb)
    {
        double best = Taper(climb.DeltaPotential, BestReal);
        double needed = SpecificStrengthNeeded(climb.DeltaPotential);
        return $"{climb.Body.Name}: {Verdict(climb.DeltaPotential)} — best real material ({BestReal.Name}) " +
               $"tapers {Format(best)}; taper {PracticalTaper:F0} needs {needed / 1e6:G3} MJ/kg " +
               $"and the best ever made is {BestReal.SpecificStrength / 1e6:F2}.";
    }

    /// <summary>A body is a candidate flagship if the cheapest material that works is one you can
    /// order by the kilometre — a laboratory record is a measurement, not a cable.</summary>
    public static bool BuildableWithStock(double deltaPotential) =>
        CheapestThatWorks(deltaPotential)?.Tier == MaterialTier.Commercial;

    /// <summary>THE FLAGSHIP: among the bodies you could actually build one on with stock material,
    /// the one where doing so saves the most propellant per tonne. Chosen by the numbers, not by
    /// taste — the rule is stated here and the guard applies it independently.</summary>
    public static Climb Flagship() =>
        MeasureAll().Where(c => BuildableWithStock(c.DeltaPotential))
                    .MaxBy(c => c.PropellantPerTonneKg)!;

    /// <summary>Taper ratios span eighty orders of magnitude in this table, so the number has to
    /// change shape or the column stops being readable.</summary>
    public static string Format(double taper) => taper switch
    {
        < 100 => taper.ToString("F2"),
        < 1e6 => taper.ToString("F0"),
        _ => taper.ToString("0.0e+00"),
    };
}
