namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 49, THE PAINT JOB (albedo / Yarkovsky deflection). The fifth candidate on the owner's playbook
/// and the only one the issue itself doubts out loud: <i>"probably too slow to be a gig — the lab exists to
/// prove WHY it's not (an honest negative result the barflies can cite)."</i> Paint one face of a rock and let
/// sunlight do the pushing: a body that absorbs light in the morning and re-radiates it in the afternoon
/// throws its heat off-noon, and the recoil of those infrared photons is a real, measured thrust — the
/// Yarkovsky effect. It is the only deflection here that has been <b>observed on real asteroids</b> rather
/// than argued from first principles, which is exactly why it is worth computing honestly.
///
/// <para>Pure, deterministic Core math (repo law §9). The diurnal term is the standard linearised
/// plane-parallel theory (Vokrouhlický 1998/1999; Bottke et al., <i>The Yarkovsky and YORP Effects</i>, 2006):
/// a body of radius R and bulk density ρ at heliocentric distance r feels a radiation factor
/// Φ = 3F/(4ρRc), and the thermal lag turns a fraction of it sideways —
/// <c>f_T = (4/9)·α·Φ·G(Θ)·cos γ</c>, with α = 1−A the absorbed fraction, γ the obliquity and
/// <c>G(Θ) = Θ/(1+2Θ+2Θ²)</c> the lag function of the thermal parameter Θ. Because the semi-major axis obeys
/// <c>da/dt = 2·f_T/n</c> for a near-circular orbit, the same transverse acceleration feeds straight into the
/// along-track leverage the rest of the playbook already uses:
/// <see cref="GravityTractor.Miss"/> (miss = 1.5·a·T²).</para>
///
/// <para><b>The one-sided lever.</b> The only thing paint can change is A, and α·G(Θ(α)) is strictly
/// INCREASING in α (the derivative reduces to (Θ/D²)·(0.25 + 2Θ + 3.5Θ²) &gt; 0 for every Θ, with
/// D = 1+2Θ+2Θ²). So a brighter rock always drifts slower, never faster, and the biggest change a paint job
/// can ever buy is <b>turning the natural drift off</b>. Real near-Earth rocks are already dark
/// (α ≈ 0.9–0.98), so darkening them further is worth a few per cent and whitening them is worth, at the
/// absolute best, one unit of the natural drift. <see cref="CeilingTransverseAccel"/> is that bound, and it
/// is independent of spin, thermal inertia and paint chemistry — it cannot be tuned away.</para>
///
/// <para>NOT a shipped gig, and this class exists to say why with numbers rather than with a shrug. The
/// calibration that earns the right to say it: run <see cref="SemiMajorDriftMetersPerYear"/> on
/// (101955) Bennu's published parameters and the model must land on the measured −284 m/yr.</para>
/// </summary>
public static class YarkovskyPaint
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE UNIVERSE'S OWN CONSTANTS — no game tuning in this block. Everything below is CODATA/IAU.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The solar constant (W/m²) — the flux at 1 AU.</summary>
    public const double SolarConstantWattsPerM2 = 1361.0;

    /// <summary>One astronomical unit (m), IAU 2012 definition.</summary>
    public const double AstronomicalUnitMeters = 1.495978707e11;

    /// <summary>The speed of light (m/s).</summary>
    public const double SpeedOfLightMetersPerSecond = 2.99792458e8;

    /// <summary>The Stefan–Boltzmann constant (W·m⁻²·K⁻⁴).</summary>
    public const double StefanBoltzmann = 5.670374419e-8;

    /// <summary>The Sun's gravitational parameter (m³/s²) — the same value <c>scenarios/sol.json</c> flies.</summary>
    public const double SunGravitationalParameter = 1.32712440018e20;

    /// <summary>Seconds in a Julian year — the unit every drift in this class is quoted in, because the
    /// literature quotes Yarkovsky drifts in metres per year (or AU per Myr).</summary>
    public const double SecondsPerYear = 365.25 * 86400.0;

    /// <summary>Earth's mean radius (m) — the "miss distance needed" yardstick the real planetary-defence
    /// literature uses, and a far smaller bar than the game's own <see cref="DeflectionGig.SafeMissMeters"/>.</summary>
    public const double EarthRadiusMeters = 6.371e6;

    /// <summary>Thermal infrared emissivity of asteroid regolith — 0.9 is the standard working value across
    /// the thermal-modelling literature. Sets both the sub-solar temperature and the lag scale.</summary>
    public const double Emissivity = 0.90;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE ROCK — Bond albedo and thermal inertia per the C/S/M table the series already uses (owner ruling
    //  2026-07-20: C/S/M only, no rubble piles). Bulk density comes from KineticImpactor so the whole
    //  playbook weighs the same rock; only the SURFACE properties are new here.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The BOND albedo (the fraction of incident sunlight actually reflected away, which is the one
    /// that matters for a heat budget — not the geometric albedo the telescopes quote). Carbonaceous bodies
    /// are among the darkest surfaces in the solar system: Bennu's measured Bond albedo is 0.017. Stony and
    /// metallic surfaces are brighter but still dark by everyday standards. Lab-representative.</summary>
    public static double BondAlbedo(RockComposition c) => c switch
    {
        RockComposition.CType => 0.02, // carbonaceous — darker than charcoal (Bennu: 0.017 measured)
        RockComposition.MType => 0.10, // nickel-iron, space-weathered
        _ => 0.09,                     // S-type stony
    };

    /// <summary>Thermal inertia Γ (J·m⁻²·K⁻¹·s^−1/2) — how stubbornly the surface holds the morning's heat
    /// into the afternoon, and therefore how far off noon the recoil points. Fine regolith is low, bare rock
    /// and metal are high. Anchored on measured values: Bennu ≈ 310, Ryugu ≈ 300, Itokawa ≈ 700.
    /// OWNER-TUNABLE / lab-representative.</summary>
    public static double ThermalInertia(RockComposition c) => c switch
    {
        RockComposition.CType => 250.0,  // porous carbonaceous regolith
        RockComposition.MType => 1000.0, // exposed metal conducts
        _ => 350.0,                      // S-type stony
    };

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE PAINT — what a coat can be, and what a coat weighs. Both figures are derived, not asserted.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The Bond albedo of a good white coat. Titanium-dioxide-pigmented spacecraft white sits around
    /// 0.8; this is the optimistic end on purpose — the lab's job is to give the technique its best case and
    /// still measure the answer. OWNER-TUNABLE.</summary>
    public const double WhitePaintBondAlbedo = 0.80;

    /// <summary>The Bond albedo of a black coat — carbon black, about as dark as a manufactured surface gets.
    /// Included because "darken it instead" is the obvious follow-up question, and the answer is a few
    /// per cent (real rocks are already this dark). OWNER-TUNABLE.</summary>
    public const double BlackPaintBondAlbedo = 0.04;

    /// <summary>Dry-film thickness (m) of an opaque coat — 100 μm is the thickness at which a pigmented white
    /// coating stops showing the substrate. The first half of the paint-mass derivation. OWNER-TUNABLE.</summary>
    public const double PaintFilmThicknessMeters = 1.0e-4;

    /// <summary>Density (kg/m³) of a cured pigment-loaded film — the second half of the derivation.</summary>
    public const double PaintFilmDensityKgPerM3 = 1400.0;

    /// <summary>The published back-of-envelope cross-check (kg/m²): the 2012 MIT "paintball" entry to the
    /// Move-an-Asteroid competition budgeted about 5 t of pigment pellets per round for Apophis
    /// (≈340 m across, ≈3.6e5 m² of surface) — ≈0.014 kg/m², which is a film roughly 10 μm thick. An order of
    /// magnitude thinner than <see cref="ArealDensityKgPerM2"/>, and the lab prints both so the reader can see
    /// how little the answer cares.</summary>
    public const double PaintballArealDensityKgPerM2 = 0.014;

    /// <summary>The derived areal density of an opaque coat (kg/m²): thickness × film density = 0.14 kg/m².</summary>
    public static double ArealDensityKgPerM2 => PaintFilmThicknessMeters * PaintFilmDensityKgPerM3;

    /// <summary>The paint bill (kg) to cover a <paramref name="coverage"/> fraction of a spherical rock of
    /// <paramref name="radiusMeters"/> at <paramref name="arealDensityKgPerM2"/>: 4πR²·f·σ. Pure.</summary>
    public static double PaintMassKg(double radiusMeters, double coverage, double arealDensityKgPerM2) =>
        4.0 * System.Math.PI * radiusMeters * radiusMeters
        * System.Math.Clamp(coverage, 0.0, 1.0) * System.Math.Max(0.0, arealDensityKgPerM2);

    /// <summary>The surface-averaged Bond albedo after painting a <paramref name="coverage"/> fraction of a
    /// rock of <paramref name="naturalAlbedo"/> with paint of <paramref name="paintAlbedo"/>. This — not "one
    /// side" — is the honest model: the diurnal effect is an average over one rotation, so a painted patch
    /// spends the day passing through every hour angle and only its share of the MEAN albedo survives. Pure.</summary>
    public static double EffectiveBondAlbedo(double naturalAlbedo, double paintAlbedo, double coverage)
    {
        double f = System.Math.Clamp(coverage, 0.0, 1.0);
        return (1.0 - f) * naturalAlbedo + f * paintAlbedo;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE STANDARD DIURNAL YARKOVSKY — linearised, plane-parallel (valid when the rock is much bigger than
    //  its thermal skin depth, which every body here is by four orders of magnitude).
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Solar flux (W/m²) at <paramref name="auDistance"/>: F = S/r². Pure.</summary>
    public static double SolarFlux(double auDistance) =>
        auDistance <= 0.0 ? 0.0 : SolarConstantWattsPerM2 / (auDistance * auDistance);

    /// <summary>The radiation factor Φ (m/s²) — the acceleration the rock would feel if it absorbed every
    /// photon and threw none back: Φ = πR²F/(mc) = 3F/(4ρRc). The 1/R is the whole size story of this lab:
    /// a small rock is a lot of sunlit area per kilogram. Pure.</summary>
    public static double RadiationFactor(double fluxWattsPerM2, double bulkDensityKgPerM3, double radiusMeters) =>
        bulkDensityKgPerM3 <= 0.0 || radiusMeters <= 0.0
            ? 0.0
            : 3.0 * System.Math.Max(0.0, fluxWattsPerM2)
              / (4.0 * bulkDensityKgPerM3 * radiusMeters * SpeedOfLightMetersPerSecond);

    /// <summary>The sub-solar temperature (K) an absorbing, emitting surface settles at:
    /// εσT⁴ = αF ⇒ T* = (αF/εσ)^¼. Pure.</summary>
    public static double SubsolarTemperature(double absorbedFraction, double fluxWattsPerM2) =>
        System.Math.Pow(System.Math.Max(0.0, absorbedFraction) * System.Math.Max(0.0, fluxWattsPerM2)
                        / (Emissivity * StefanBoltzmann), 0.25);

    /// <summary>Angular spin rate (rad/s) from a rotation period in seconds. Pure.</summary>
    public static double SpinRate(double spinPeriodSeconds) =>
        spinPeriodSeconds <= 0.0 ? 0.0 : System.Math.Tau / spinPeriodSeconds;

    /// <summary>The thermal parameter Θ = Γ√ω / (εσT*³) — the ratio of how much heat the surface can store to
    /// how fast it radiates, which is exactly "how far past noon is the hot spot". Pure.</summary>
    public static double ThermalParameter(double thermalInertia, double spinRateRadPerSecond, double subsolarTemperature)
    {
        double denom = Emissivity * StefanBoltzmann * subsolarTemperature * subsolarTemperature * subsolarTemperature;
        return denom <= 0.0
            ? 0.0
            : System.Math.Max(0.0, thermalInertia) * System.Math.Sqrt(System.Math.Max(0.0, spinRateRadPerSecond)) / denom;
    }

    /// <summary>The lag function G(Θ) = Θ/(1+2Θ+2Θ²) — the fraction of Φ that ends up pointing ALONG the
    /// track. Zero at both ends (a surface with no memory throws its heat straight back at the Sun; a surface
    /// with perfect memory is uniformly warm and throws it everywhere), with a maximum in between. Pure.</summary>
    public static double ThermalLag(double theta) =>
        theta <= 0.0 ? 0.0 : theta / (1.0 + 2.0 * theta + 2.0 * theta * theta);

    /// <summary>The thermal parameter at which <see cref="ThermalLag"/> peaks: Θ = 1/√2 (set the derivative
    /// 1−2Θ² to zero). A rock spinning at exactly this rate is the best Yarkovsky engine there is.</summary>
    public static double OptimalThermalParameter => 1.0 / System.Math.Sqrt(2.0);

    /// <summary>The peak value of <see cref="ThermalLag"/>, G(1/√2) = 1/(2+2√2) ≈ 0.2071. Nothing about a
    /// surface — its spin, its conductivity, its heat capacity — can make the diurnal lag exceed this, which
    /// is what makes <see cref="CeilingTransverseAccel"/> an honest ceiling rather than a tuned figure.</summary>
    public static double MaxThermalLag => ThermalLag(OptimalThermalParameter);

    /// <summary>The DIURNAL Yarkovsky transverse acceleration (m/s²): f_T = (4/9)·α·Φ·G(Θ)·cos γ. Positive
    /// (outward drift) for a prograde rotator, negative for a retrograde one — which is why Bennu, spinning
    /// backwards, falls sunward. Pure.</summary>
    public static double DiurnalTransverseAccel(
        double bondAlbedo, double thermalInertia, double spinPeriodSeconds,
        double bulkDensityKgPerM3, double radiusMeters, double auDistance, double obliquityRadians)
    {
        double flux = SolarFlux(auDistance);
        double alpha = 1.0 - System.Math.Clamp(bondAlbedo, 0.0, 1.0);
        double phi = RadiationFactor(flux, bulkDensityKgPerM3, radiusMeters);
        double theta = ThermalParameter(thermalInertia, SpinRate(spinPeriodSeconds), SubsolarTemperature(alpha, flux));
        return (4.0 / 9.0) * alpha * phi * ThermalLag(theta) * System.Math.Cos(obliquityRadians);
    }

    /// <summary>The SEASONAL Yarkovsky transverse acceleration (m/s²) — the same physics run on the ORBITAL
    /// period instead of the rotation period, driven by the rock's own axial tilt as it goes round the Sun:
    /// f_T = −(2/9)·α·Φ·G(Θ_n)·sin²γ. Always negative (inward), maximal at γ = 90°, and — because the thermal
    /// wave at the orbital frequency reaches metres down while the diurnal one reaches centimetres — normally
    /// far weaker than the diurnal term for the bodies this lab considers. Computed and printed so the
    /// omission is a measurement rather than an assumption. Pure.</summary>
    public static double SeasonalTransverseAccel(
        double bondAlbedo, double thermalInertia, double bulkDensityKgPerM3,
        double radiusMeters, double auDistance, double obliquityRadians)
    {
        double flux = SolarFlux(auDistance);
        double alpha = 1.0 - System.Math.Clamp(bondAlbedo, 0.0, 1.0);
        double phi = RadiationFactor(flux, bulkDensityKgPerM3, radiusMeters);
        double theta = ThermalParameter(thermalInertia, MeanMotion(auDistance), SubsolarTemperature(alpha, flux));
        double s = System.Math.Sin(obliquityRadians);
        return -(2.0 / 9.0) * alpha * phi * ThermalLag(theta) * s * s;
    }

    /// <summary>Heliocentric mean motion (rad/s) at <paramref name="auDistance"/>: n = √(μ☉/a³). Pure.</summary>
    public static double MeanMotion(double auDistance)
    {
        double a = auDistance * AstronomicalUnitMeters;
        return a <= 0.0 ? 0.0 : System.Math.Sqrt(SunGravitationalParameter / (a * a * a));
    }

    /// <summary>The semi-major-axis drift (m/yr) a transverse acceleration produces on a near-circular orbit:
    /// da/dt = 2·f_T/n (Gauss). This is the quantity the radar astronomers actually measure, which is why the
    /// calibration guard lives on it and not on the acceleration. Pure.</summary>
    public static double SemiMajorDriftMetersPerYear(double transverseAccel, double auDistance)
    {
        double n = MeanMotion(auDistance);
        return n <= 0.0 ? 0.0 : 2.0 * transverseAccel / n * SecondsPerYear;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE PAINT JOB ITSELF — the CHANGE a coat buys, and the ceiling on that change.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The change in transverse acceleration (m/s², signed, measured against the un-painted path) a
    /// coat of <paramref name="paintAlbedo"/> over a <paramref name="coverage"/> fraction buys on a rock of
    /// <paramref name="type"/>. This — not the rock's natural drift — is the deflection, because the nominal
    /// trajectory the observers fit already contains the natural Yarkovsky. Pure.</summary>
    public static double PaintDeltaAccel(
        RockType type, double radiusMeters, double auDistance,
        double spinPeriodSeconds, double obliquityRadians, double paintAlbedo, double coverage)
    {
        double rho = KineticImpactor.BulkDensity(type.Composition);
        double inertia = ThermalInertia(type.Composition);
        double natural = DiurnalTransverseAccel(
            BondAlbedo(type.Composition), inertia, spinPeriodSeconds, rho, radiusMeters, auDistance, obliquityRadians);
        double painted = DiurnalTransverseAccel(
            EffectiveBondAlbedo(BondAlbedo(type.Composition), paintAlbedo, coverage),
            inertia, spinPeriodSeconds, rho, radiusMeters, auDistance, obliquityRadians);
        return natural - painted;
    }

    /// <summary>THE CEILING (m/s²). The largest transverse acceleration any paint job could ever change, for
    /// this rock at this distance: (4/9)·α·Φ·G_max, i.e. a surface tuned to the perfect spin, pole-on to the
    /// track, painted from its natural darkness all the way to a perfect mirror. It does not depend on spin,
    /// on thermal inertia, or on the chemistry of the paint, so it cannot be argued upward — which is what
    /// makes the lab's negative result a bound rather than a guess. Pure.</summary>
    public static double CeilingTransverseAccel(RockType type, double radiusMeters, double auDistance)
    {
        double flux = SolarFlux(auDistance);
        double alpha = 1.0 - BondAlbedo(type.Composition);
        double phi = RadiationFactor(flux, KineticImpactor.BulkDensity(type.Composition), radiusMeters);
        return (4.0 / 9.0) * alpha * phi * MaxThermalLag;
    }

    /// <summary>The along-track miss (m) a steady paint-induced acceleration opens over
    /// <paramref name="leadSeconds"/> — the playbook's shared continuous-tow leverage,
    /// <see cref="GravityTractor.Miss"/> (1.5·a·T²), not a second copy of it. Pure.</summary>
    public static double Miss(double deltaAccel, double leadSeconds) =>
        GravityTractor.Miss(System.Math.Abs(deltaAccel), leadSeconds);

    /// <summary>The warning time (s) a paint job of <paramref name="deltaAccel"/> needs to open
    /// <paramref name="missMeters"/> — <see cref="GravityTractor.RequiredLeadSeconds"/>, shared with the
    /// tractor and the long knife so the playbook's five techniques are compared on one axis. Pure.</summary>
    public static double RequiredWarningSeconds(double deltaAccel, double missMeters) =>
        GravityTractor.RequiredLeadSeconds(System.Math.Abs(deltaAccel), missMeters);

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE OTHER HALF OF AN ALBEDO CHANGE — direct radiation pressure. Painting a rock white makes it reflect
    //  more, which pushes it harder straight away from the Sun. That force is RADIAL, and a radial force does
    //  not secularly change the semi-major axis at all (Gauss: da/dt = 2f_T/n has no f_R term). What it does
    //  change is the mean-motion rate, giving an along-track drift LINEAR in time rather than quadratic — so
    //  it loses to the Yarkovsky term over any warning worth having. Computed, not waved away.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The change in RADIAL radiation-pressure acceleration (m/s²) from an albedo change of
    /// <paramref name="deltaAlbedo"/>: Δa_R = (4/9)·ΔA·Φ, the diffuse-sphere reflection term of the standard
    /// C_R = 1 + 4A/9 solar-pressure coefficient. Pure.</summary>
    public static double RadialPressureDelta(double deltaAlbedo, double radiationFactor) =>
        (4.0 / 9.0) * deltaAlbedo * radiationFactor;

    /// <summary>The along-track displacement (m) a constant RADIAL acceleration opens over
    /// <paramref name="leadSeconds"/>: 2·f_R·T/n (Gauss's mean-anomaly equation on a circular orbit). Linear
    /// in T — the honest reason the radiation-pressure half of a paint job never catches up. Pure.</summary>
    public static double RadialAlongTrackDrift(double radialAccel, double leadSeconds, double meanMotion) =>
        meanMotion <= 0.0 ? 0.0 : 2.0 * radialAccel * System.Math.Max(0.0, leadSeconds) / meanMotion;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE CALIBRATION ANCHORS — measured Yarkovsky drifts on real asteroids. The model has to reproduce
    //  these or nothing else it prints is worth reading. Bennu is the tight anchor because OSIRIS-REx went
    //  there and weighed it; Golevka is the loose one because every input it needs is a model fit.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One measured Yarkovsky anchor: the published parameters and the measured semi-major-axis
    /// drift (m/yr) of a real asteroid.</summary>
    /// <param name="Name">The body, as the literature names it.</param>
    /// <param name="RadiusMeters">Mean radius (m).</param>
    /// <param name="BulkDensityKgPerM3">Bulk density (kg/m³).</param>
    /// <param name="BondAlbedo">Bond albedo.</param>
    /// <param name="ThermalInertia">Thermal inertia Γ (J·m⁻²·K⁻¹·s^−1/2).</param>
    /// <param name="SpinPeriodSeconds">Rotation period (s).</param>
    /// <param name="ObliquityDegrees">Obliquity of the spin axis to the orbit normal (°).</param>
    /// <param name="SemiMajorAxisAu">Orbital semi-major axis (AU).</param>
    /// <param name="MeasuredDriftMetersPerYear">The measured da/dt (m/yr) — the number to reproduce.</param>
    public readonly record struct DriftAnchor(
        string Name, double RadiusMeters, double BulkDensityKgPerM3, double BondAlbedo, double ThermalInertia,
        double SpinPeriodSeconds, double ObliquityDegrees, double SemiMajorAxisAu, double MeasuredDriftMetersPerYear);

    /// <summary>(101955) Bennu — the best-characterised Yarkovsky detection there is. OSIRIS-REx measured its
    /// mass, shape, spin and thermal inertia in situ, and radar+optical astrometry measured
    /// da/dt = −284.6 m/yr (Chesley et al. 2014: −19.0×10⁻⁴ AU/Myr). Retrograde (γ ≈ 177.6°), so it falls
    /// sunward.</summary>
    public static DriftAnchor Bennu => new(
        "(101955) Bennu", 246.0, 1190.0, 0.017, 310.0, 4.296 * 3600.0, 177.6, 1.126, -284.6);

    /// <summary>(6489) Golevka — the FIRST direct Yarkovsky detection (Chesley et al., Science 2003:
    /// −6.39×10⁻⁴ AU/Myr ≈ −95.6 m/yr, from a 15 km range anomaly built up over twelve years). Nobody has
    /// been there: its density, thermal inertia and pole are all model fits, which is exactly why it is the
    /// loose anchor and Bennu is the tight one.</summary>
    public static DriftAnchor Golevka => new(
        "(6489) Golevka", 265.0, 2700.0, 0.06, 100.0, 6.026 * 3600.0, 137.0, 2.50, -95.6);

    /// <summary>Run the model on an anchor and return the predicted semi-major-axis drift (m/yr) — the
    /// calibration call. Pure.</summary>
    public static double PredictedDrift(DriftAnchor anchor)
    {
        double f = DiurnalTransverseAccel(
            anchor.BondAlbedo, anchor.ThermalInertia, anchor.SpinPeriodSeconds, anchor.BulkDensityKgPerM3,
            anchor.RadiusMeters, anchor.SemiMajorAxisAu, anchor.ObliquityDegrees * System.Math.PI / 180.0);
        return SemiMajorDriftMetersPerYear(f, anchor.SemiMajorAxisAu);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE GAME'S OWN GEOMETRY — where the deflection gig actually happens. Ringside Exchange rides Saturn
    //  (scenarios/sol.json: saturn.orbitRadiusM = 1.43353e12 m), and sunlight there is 92× thinner than at
    //  Earth. The paint job's worst enemy in this game is not the rock; it is the address.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ringside Exchange's heliocentric distance in AU — Saturn's orbit radius from
    /// <c>scenarios/sol.json</c> (1.43353e12 m) over one AU: 9.58 AU.</summary>
    public static double RingsideHeliocentricAu => 1.43353e12 / AstronomicalUnitMeters;

    /// <summary>A representative rotation period (s) for a small asteroid — 4 hours, in the middle of the
    /// observed spin distribution for bodies of this size. Swept in the probe, so no result rests on it.</summary>
    public const double ReferenceSpinPeriodSeconds = 4.0 * 3600.0;
}
