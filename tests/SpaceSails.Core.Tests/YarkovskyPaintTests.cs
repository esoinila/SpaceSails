namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 49, THE PAINT JOB. The guards for the albedo/Yarkovsky deflection, in four groups.
///
/// <para><b>CALIBRATION.</b> Unlike every other technique in the playbook, this one is already running, on
/// every rock in the sky, and two of its drifts have been MEASURED: (101955) Bennu at −284.6 m/yr and
/// (6489) Golevka at −95.6 m/yr. So the model does not get to be plausible — it has to reproduce them.
/// Bennu is held to a factor of two (the model lands at 0.77×); Golevka only to a factor of five, and the
/// test says why out loud: nobody has been to Golevka, so its density, its thermal inertia and its pole are
/// all model fits, and the guard sweeps the thermal inertia rather than pretending one value is known.</para>
///
/// <para><b>ANTI-VACUITY.</b> A factor-of-two window is wide enough that a model which ignored the thermal
/// lag entirely and always used its maximum would slip through it (0.2071 vs the 0.144 Bennu actually earns
/// is only 1.4× out). So the calibration is fenced by a second guard that the answer MOVES when the spin and
/// the thermal inertia move — a constant-lag model is flat in both and fails it.</para>
///
/// <para><b>MONOTONICITY LAWS.</b> Smaller rock ⇒ more drift (exactly 1/R). Longer warning ⇒ more miss
/// (exactly T², via <see cref="GravityTractor.Miss"/>, not a second copy of the leverage). Brighter ⇒ always
/// slower, at every thermal parameter — the algebraic heart of the negative result, since it means the
/// biggest thing paint can ever buy is switching off the drift the rock already had.</para>
///
/// <para><b>THE VERDICT, PINNED.</b> The crossover the README quotes, and the boundary: at Ringside — where
/// the gig actually happens, 9.58 AU out — not one rock on the C/S/M × size grid comes within five hundred
/// years of the gig's own SafeMiss, even at the physical ceiling.</para>
/// </summary>
public class YarkovskyPaintTests
{
    private const double SafeMiss = DeflectionGig.SafeMissMeters;
    private const double EarthR = YarkovskyPaint.EarthRadiusMeters;
    private const double Year = YarkovskyPaint.SecondsPerYear;
    private const double Spin = YarkovskyPaint.ReferenceSpinPeriodSeconds;
    private const double White = YarkovskyPaint.WhitePaintBondAlbedo;

    private static readonly RockType C = new(RockComposition.CType);
    private static readonly RockType S = new(RockComposition.SType);
    private static readonly RockType M = new(RockComposition.MType);
    private static readonly RockType[] AllTypes = [C, S, M];

    private static double PaintYears(RockType type, double radius, double au, double miss) =>
        YarkovskyPaint.RequiredWarningSeconds(
            YarkovskyPaint.PaintDeltaAccel(type, radius, au, Spin, 0.0, White, 1.0), miss) / Year;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  CALIBRATION — the two drifts humanity has actually measured.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE ANCHOR. OSIRIS-REx weighed Bennu, mapped its thermal inertia and photographed its spin;
    /// radar and optical astrometry measured its semi-major axis falling 284.6 m every year. The linearised
    /// plane-parallel model, handed those published parameters, must land on it.
    ///
    /// <para>Tolerance: a factor of two, and the model spends 0.77 of it. That is not generous — the model
    /// is linear, plane-parallel, single-Θ and circular-orbit, run against a rubble-shaped body on an
    /// e = 0.20 orbit. Every deliberate break tried on it (raw Θ for the lag, the 4/9 dropped, the obliquity
    /// ignored) lands outside.</para></summary>
    [Fact]
    public void Bennu_ReproducesTheMeasuredDrift_WithinAFactorOfTwo()
    {
        YarkovskyPaint.DriftAnchor bennu = YarkovskyPaint.Bennu;
        double predicted = YarkovskyPaint.PredictedDrift(bennu);
        double ratio = predicted / bennu.MeasuredDriftMetersPerYear;

        Assert.True(predicted < 0.0,
            $"Bennu is a retrograde rotator (γ = {bennu.ObliquityDegrees}°) and must drift SUNWARD; got {predicted:F1} m/yr");
        Assert.InRange(ratio, 0.5, 2.0);
        Assert.Equal(0.77, ratio, 0.02);        // the README's printed number
        Assert.Equal(-218.7, predicted, 0.5);   // …and the drift it came from
    }

    /// <summary>The fence around the tolerance above. A model that always used the MAXIMUM thermal lag would
    /// pass a factor-of-two gate on Bennu by accident, and it would be flat in both spin and thermal inertia.
    /// This one is not: halve the rotation period or double the conductivity and the answer has to move.</summary>
    [Fact]
    public void Bennu_TheAnswerActuallyDependsOnSpinAndOnThermalInertia()
    {
        YarkovskyPaint.DriftAnchor bennu = YarkovskyPaint.Bennu;
        double baseline = YarkovskyPaint.PredictedDrift(bennu);

        double faster = YarkovskyPaint.PredictedDrift(bennu with { SpinPeriodSeconds = bennu.SpinPeriodSeconds / 2.0 });
        double stickier = YarkovskyPaint.PredictedDrift(bennu with { ThermalInertia = bennu.ThermalInertia * 2.0 });

        Assert.True(Math.Abs(faster / baseline - 1.0) > 0.10,
            $"halving Bennu's rotation period moved da/dt by only {100.0 * Math.Abs(faster / baseline - 1.0):F1}% — " +
            "the model is not reading the spin, so its agreement with the measurement is a coincidence");
        Assert.True(Math.Abs(stickier / baseline - 1.0) > 0.10,
            $"doubling Bennu's thermal inertia moved da/dt by only {100.0 * Math.Abs(stickier / baseline - 1.0):F1}% — " +
            "the model is not reading the surface either");

        // Bennu sits past the lag peak (Θ = 2.25 > 1/√2), so on that side MORE inertia and FASTER spin both
        // reduce the drift. Directions, not just magnitudes.
        Assert.True(Math.Abs(faster) < Math.Abs(baseline));
        Assert.True(Math.Abs(stickier) < Math.Abs(baseline));
    }

    /// <summary>The LOOSE anchor, and the guard is loose on purpose. Golevka was the first direct Yarkovsky
    /// detection (a 15 km range anomaly accumulated over twelve years), but nobody has visited it: its
    /// density, its thermal inertia and its pole are all fits to lightcurves and radar. So the claim is only
    /// that the model reproduces the ORDER OF MAGNITUDE across the whole plausible thermal-inertia range —
    /// a factor of five, not two — and that the best row in that sweep gets inside a factor of two.</summary>
    [Fact]
    public void Golevka_TheLooseAnchor_LandsWithinAFactorOfFiveAcrossItsUnknownThermalInertia()
    {
        YarkovskyPaint.DriftAnchor golevka = YarkovskyPaint.Golevka;
        double best = 0.0;

        foreach (double inertia in new[] { 50.0, 100.0, 200.0, 300.0 })
        {
            YarkovskyPaint.DriftAnchor tried = golevka with { ThermalInertia = inertia };
            double ratio = YarkovskyPaint.PredictedDrift(tried) / golevka.MeasuredDriftMetersPerYear;
            Assert.True(ratio > 0.0, $"Golevka must drift sunward at Γ = {inertia}");
            Assert.InRange(ratio, 0.2, 5.0);
            best = Math.Max(best, Math.Min(ratio, 1.0 / ratio));
        }

        Assert.True(best >= 0.5,
            $"the best row of the Γ sweep is {1.0 / best:F1}× off the measurement — a model that cannot get " +
            "within a factor of two of Golevka ANYWHERE in its plausible parameter range is not calibrated");
        Assert.Equal(0.60, YarkovskyPaint.PredictedDrift(golevka with { ThermalInertia = 50.0 }) / golevka.MeasuredDriftMetersPerYear, 0.02);
    }

    /// <summary>The lag function's shape, from its own algebra: G(Θ) = Θ/(1+2Θ+2Θ²) peaks where 1 − 2Θ² = 0,
    /// i.e. Θ = 1/√2, at G = 1/(2+2√2) ≈ 0.2071 — and is zero at both ends. This is the lid that makes
    /// <see cref="YarkovskyPaint.CeilingTransverseAccel"/> a bound rather than a guess.</summary>
    [Fact]
    public void ThermalLag_PeaksAtOneOverRootTwo_AndIsZeroAtBothEnds()
    {
        Assert.Equal(1.0 / Math.Sqrt(2.0), YarkovskyPaint.OptimalThermalParameter, 1e-12);
        Assert.Equal(0.20711, YarkovskyPaint.MaxThermalLag, 1e-5);
        Assert.Equal(0.0, YarkovskyPaint.ThermalLag(0.0));

        for (double theta = 0.01; theta < 200.0; theta *= 1.15)
        {
            Assert.True(YarkovskyPaint.ThermalLag(theta) <= YarkovskyPaint.MaxThermalLag + 1e-15,
                $"G({theta:F3}) = {YarkovskyPaint.ThermalLag(theta):F6} exceeds the lid {YarkovskyPaint.MaxThermalLag:F6}");
        }
        Assert.True(YarkovskyPaint.ThermalLag(1e-4) < 1e-3);
        Assert.True(YarkovskyPaint.ThermalLag(1e4) < 1e-3);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  MONOTONICITY LAWS.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>SMALLER ROCK ⇒ MORE DRIFT, and exactly as 1/R: Φ = 3F/(4ρRc) is the only place the radius
    /// enters, because in the plane-parallel limit Θ does not know how big the body is.</summary>
    [Fact]
    public void SmallerRockDriftsFaster_ExactlyAsOneOverRadius()
    {
        foreach (RockType type in AllTypes)
        {
            double previous = double.PositiveInfinity;
            foreach (double r in KineticImpactor.RealisticRadiiMeters)
            {
                double accel = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                    YarkovskyPaint.BondAlbedo(type.Composition), YarkovskyPaint.ThermalInertia(type.Composition),
                    Spin, KineticImpactor.BulkDensity(type.Composition), r, 1.0, 0.0));
                Assert.True(accel < previous, $"{type.Label}: a {r} m rock must drift slower than the one before it");
                previous = accel;
            }

            double small = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                YarkovskyPaint.BondAlbedo(type.Composition), YarkovskyPaint.ThermalInertia(type.Composition),
                Spin, KineticImpactor.BulkDensity(type.Composition), 100.0, 1.0, 0.0));
            double big = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                YarkovskyPaint.BondAlbedo(type.Composition), YarkovskyPaint.ThermalInertia(type.Composition),
                Spin, KineticImpactor.BulkDensity(type.Composition), 700.0, 1.0, 0.0));
            Assert.Equal(7.0, small / big, 1e-9);
        }
    }

    /// <summary>LONGER WARNING ⇒ MORE MISS, quadratically — and through the playbook's SHARED leverage
    /// (<see cref="GravityTractor.Miss"/>, 1.5·a·T²), not a private copy of it. If lab 37 ever revises the
    /// leverage, this lab moves with it instead of drifting quietly out of agreement.</summary>
    [Fact]
    public void LongerWarningOpensMoreMiss_Quadratically_OnTheSharedLeverage()
    {
        double accel = Math.Abs(YarkovskyPaint.PaintDeltaAccel(C, 140.0, 1.0, Spin, 0.0, White, 1.0));

        double previous = -1.0;
        for (double years = 1.0; years <= 200.0; years *= 1.5)
        {
            double miss = YarkovskyPaint.Miss(accel, years * Year);
            Assert.True(miss > previous, $"{years:F1} yr opened {miss:E2} m, less than the shorter warning before it");
            previous = miss;
        }

        Assert.Equal(GravityTractor.Miss(accel, 30.0 * Year), YarkovskyPaint.Miss(accel, 30.0 * Year), 1e-12);
        Assert.Equal(4.0, YarkovskyPaint.Miss(accel, 40.0 * Year) / YarkovskyPaint.Miss(accel, 20.0 * Year), 1e-9);
        Assert.Equal(GravityTractor.ContinuousTowLeverage * accel * Year * Year, YarkovskyPaint.Miss(accel, Year), 1e-24);

        // …and the warning-time query is its exact inverse.
        double needed = YarkovskyPaint.RequiredWarningSeconds(accel, SafeMiss);
        Assert.Equal(SafeMiss, YarkovskyPaint.Miss(accel, needed), 1e-6 * SafeMiss);
    }

    /// <summary>BRIGHTER IS ALWAYS SLOWER — the algebraic heart of this lab. α·G(Θ(α)) is strictly increasing
    /// in α (its derivative reduces to (Θ/D²)·(0.25 + 2Θ + 3.5Θ²) &gt; 0), so no combination of albedo, spin
    /// and regolith lets a coat make a rock drift FASTER. Swept across four decades of spin period so the
    /// claim is tested on both sides of the lag peak, where the naive intuition ("whitening raises Θ toward
    /// the optimum, so it could help") would otherwise bite.</summary>
    [Fact]
    public void BrighterIsAlwaysSlower_OnBothSidesOfTheLagPeak()
    {
        double rho = KineticImpactor.BulkDensity(RockComposition.CType);

        foreach (double hours in new[] { 0.25, 1.0, 4.0, 20.0, 120.0, 1000.0 })
        {
            foreach (double inertia in new[] { 20.0, 250.0, 2500.0 })
            {
                double previous = double.PositiveInfinity;
                for (double albedo = 0.0; albedo <= 0.96; albedo += 0.02)
                {
                    double f = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                        albedo, inertia, hours * 3600.0, rho, 140.0, 1.0, 0.0));
                    Assert.True(f < previous,
                        $"at P = {hours} h, Γ = {inertia}: albedo {albedo:F2} drifts {f:E3} m/s², which is NOT less " +
                        "than the darker step before it — the lever has turned both ways and the whole negative " +
                        "result is void");
                    previous = f;
                }
            }
        }
    }

    /// <summary>THE CEILING LAW. A coat changes the drift by turning the absorbed fraction down, so the change
    /// can never exceed what the rock already had — and it can never exceed the spin-independent physical
    /// ceiling (4/9)·α·Φ·G_max either. Swept over the whole C/S/M × size × distance grid.</summary>
    [Fact]
    public void PaintCanNeverBuyMoreThanTheRockAlreadyHad()
    {
        foreach (RockType type in AllTypes)
        {
            foreach (double r in KineticImpactor.RealisticRadiiMeters)
            {
                foreach (double au in new[] { 0.7, 1.0, 2.5, 5.2, YarkovskyPaint.RingsideHeliocentricAu })
                {
                    double natural = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                        YarkovskyPaint.BondAlbedo(type.Composition), YarkovskyPaint.ThermalInertia(type.Composition),
                        Spin, KineticImpactor.BulkDensity(type.Composition), r, au, 0.0));
                    double delta = Math.Abs(YarkovskyPaint.PaintDeltaAccel(type, r, au, Spin, 0.0, White, 1.0));
                    double ceiling = YarkovskyPaint.CeilingTransverseAccel(type, r, au);

                    Assert.True(delta < natural,
                        $"{type.Label} {r} m at {au:F2} AU: a whitewash changed the drift by {delta:E3} m/s², which is " +
                        $"MORE than the {natural:E3} m/s² the bare rock had — paint would be adding thrust, not removing it");
                    Assert.True(natural <= ceiling * (1.0 + 1e-9),
                        $"{type.Label} {r} m at {au:F2} AU: the natural drift {natural:E3} exceeds the ceiling {ceiling:E3}");
                    Assert.True(delta <= ceiling * (1.0 + 1e-9));
                }
            }
        }

        // Darkening a rock that is already darker than the paint is worth almost nothing — the other half of
        // "one-sided". A black coat on a C-type moves it by under 5%.
        double blackDelta = Math.Abs(YarkovskyPaint.PaintDeltaAccel(C, 140.0, 1.0, Spin, 0.0, YarkovskyPaint.BlackPaintBondAlbedo, 1.0));
        double bare = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
            YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.ThermalInertia(RockComposition.CType),
            Spin, KineticImpactor.BulkDensity(RockComposition.CType), 140.0, 1.0, 0.0));
        Assert.True(blackDelta / bare < 0.05, $"a black coat on a C-type moved it {100.0 * blackDelta / bare:F1}%");
    }

    /// <summary>FARTHER FROM THE SUN ⇒ ALWAYS SLOWER, and faster than inverse-square: the flux falls as 1/r²
    /// AND the colder surface pushes Θ up past the lag peak, so the lag collapses on top of it. This is the
    /// law that decides the game's verdict, because Ringside rides Saturn.</summary>
    [Fact]
    public void FartherFromTheSunIsAlwaysSlower_AndFasterThanInverseSquare()
    {
        double previous = double.PositiveInfinity;
        foreach (double au in new[] { 0.7, 1.0, 1.5, 2.5, 3.5, 5.2, 7.0, YarkovskyPaint.RingsideHeliocentricAu })
        {
            double f = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.ThermalInertia(RockComposition.CType),
                Spin, KineticImpactor.BulkDensity(RockComposition.CType), 140.0, au, 0.0));
            Assert.True(f < previous, $"{au:F2} AU drifts {f:E3} m/s², not less than the closer step before it");
            previous = f;
        }

        double one = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
            YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.ThermalInertia(RockComposition.CType),
            Spin, KineticImpactor.BulkDensity(RockComposition.CType), 140.0, 1.0, 0.0));
        double ringside = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
            YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.ThermalInertia(RockComposition.CType),
            Spin, KineticImpactor.BulkDensity(RockComposition.CType), 140.0, YarkovskyPaint.RingsideHeliocentricAu, 0.0));
        double inverseSquare = YarkovskyPaint.RingsideHeliocentricAu * YarkovskyPaint.RingsideHeliocentricAu;
        Assert.True(one / ringside > inverseSquare,
            $"the Saturn-distance drift fell only {one / ringside:F0}×, which is no worse than the {inverseSquare:F0}× " +
            "the flux alone explains — the thermal-lag collapse is missing from the model");
    }

    /// <summary>MORE COVERAGE ⇒ MORE CHANGE, and linearly in the surface-averaged albedo: the diurnal effect is
    /// an average over one rotation, so "paint one side" is only ever a coverage fraction. The correction the
    /// issue's own phrasing needs, held as a law.</summary>
    [Fact]
    public void MoreCoverageIsMoreChange_AndOneSideIsJustAFraction()
    {
        Assert.Equal(0.02, YarkovskyPaint.EffectiveBondAlbedo(0.02, White, 0.0), 1e-12);
        Assert.Equal(White, YarkovskyPaint.EffectiveBondAlbedo(0.02, White, 1.0), 1e-12);
        Assert.Equal(0.41, YarkovskyPaint.EffectiveBondAlbedo(0.02, White, 0.5), 1e-12);
        Assert.Equal(White, YarkovskyPaint.EffectiveBondAlbedo(0.02, White, 4.7), 1e-12);   // coverage clamps

        double previous = -1.0;
        foreach (double coverage in new[] { 0.0, 0.1, 0.25, 0.5, 0.75, 0.9, 1.0 })
        {
            double delta = Math.Abs(YarkovskyPaint.PaintDeltaAccel(C, 140.0, 1.0, Spin, 0.0, White, coverage));
            Assert.True(delta > previous, $"{100 * coverage:F0}% coverage bought {delta:E3} m/s², not more than the step before");
            previous = delta;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  UNITS SANITY — every formula, checked against the definition it came from.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Φ is a force per unit mass. Written as 3F/(4ρRc) for speed, it has to equal πR²F/(mc) computed
    /// from the playbook's own <see cref="KineticImpactor.AsteroidMass"/> — the same rock, weighed the same way.</summary>
    [Fact]
    public void RadiationFactor_IsSunlightForcePerKilogram_ByTwoRoutes()
    {
        foreach (RockType type in AllTypes)
        {
            foreach (double r in KineticImpactor.RealisticRadiiMeters)
            {
                double flux = YarkovskyPaint.SolarFlux(1.0);
                double byDensity = YarkovskyPaint.RadiationFactor(flux, KineticImpactor.BulkDensity(type.Composition), r);
                double byMass = Math.PI * r * r * flux
                                / (KineticImpactor.AsteroidMass(type, r) * YarkovskyPaint.SpeedOfLightMetersPerSecond);
                Assert.Equal(byMass, byDensity, 1e-12 * byMass);
            }
        }

        Assert.Equal(0.0, YarkovskyPaint.RadiationFactor(1361.0, 0.0, 140.0));
        Assert.Equal(0.0, YarkovskyPaint.RadiationFactor(1361.0, 2700.0, 0.0));
        Assert.Equal(YarkovskyPaint.SolarConstantWattsPerM2, YarkovskyPaint.SolarFlux(1.0), 1e-9);
        Assert.Equal(YarkovskyPaint.SolarConstantWattsPerM2 / 4.0, YarkovskyPaint.SolarFlux(2.0), 1e-9);
    }

    /// <summary>The sub-solar temperature is whatever balances the heat budget: εσT⁴ = αF, no more and no
    /// less. Checked by putting the answer back into the equation it solves.</summary>
    [Fact]
    public void SubsolarTemperature_BalancesTheHeatBudgetItSolves()
    {
        foreach (double au in new[] { 0.7, 1.0, 2.5, YarkovskyPaint.RingsideHeliocentricAu })
        {
            foreach (double alpha in new[] { 0.2, 0.5, 0.98 })
            {
                double flux = YarkovskyPaint.SolarFlux(au);
                double t = YarkovskyPaint.SubsolarTemperature(alpha, flux);
                double emitted = YarkovskyPaint.Emissivity * YarkovskyPaint.StefanBoltzmann * Math.Pow(t, 4.0);
                Assert.Equal(alpha * flux, emitted, 1e-9 * alpha * flux);
            }
        }

        // A dark rock at 1 AU runs near 400 K at the sub-solar point, and Saturn-country is ~130 K.
        Assert.Equal(402.1, YarkovskyPaint.SubsolarTemperature(0.98, YarkovskyPaint.SolarFlux(1.0)), 0.5);
        Assert.InRange(YarkovskyPaint.SubsolarTemperature(0.98, YarkovskyPaint.SolarFlux(YarkovskyPaint.RingsideHeliocentricAu)), 120.0, 140.0);
    }

    /// <summary>The Gauss conversion, both ways. A near-circular orbit's semi-major axis moves at 2·f_T/n, and
    /// the mean motion at 1 AU has to give a one-year period — the cheapest possible check that the AU, the
    /// solar μ and the seconds-per-year constant are all in the same unit system.</summary>
    [Fact]
    public void GaussConversion_AndAnAstronomicalUnitIsAYear()
    {
        Assert.Equal(1.0, Math.Tau / YarkovskyPaint.MeanMotion(1.0) / Year, 0.002);
        Assert.Equal(Math.Pow(9.0, 1.5), Math.Tau / YarkovskyPaint.MeanMotion(9.0) / Year, 0.01 * Math.Pow(9.0, 1.5));

        double accel = 1.0e-12;
        Assert.Equal(2.0 * accel / YarkovskyPaint.MeanMotion(1.0) * Year,
            YarkovskyPaint.SemiMajorDriftMetersPerYear(accel, 1.0), 1e-9);
        Assert.Equal(0.0, YarkovskyPaint.SemiMajorDriftMetersPerYear(accel, 0.0));

        // And the sign survives the trip: an outward push raises the orbit.
        Assert.True(YarkovskyPaint.SemiMajorDriftMetersPerYear(accel, 1.0) > 0.0);
        Assert.True(YarkovskyPaint.SemiMajorDriftMetersPerYear(-accel, 1.0) < 0.0);
    }

    /// <summary>The paint bill: 4πR²·f·σ, with σ derived from a film thickness and a film density rather than
    /// asserted — and cross-checked against the published back-of-envelope, which implies a coat an order of
    /// magnitude thinner. Both are printed in the README so the reader can pick.</summary>
    [Fact]
    public void PaintMass_IsAreaTimesArealDensity_AndTheApophisCrossCheckLands()
    {
        Assert.Equal(0.14, YarkovskyPaint.ArealDensityKgPerM2, 1e-12);

        double r = 140.0;
        double area = 4.0 * Math.PI * r * r;
        Assert.Equal(area * YarkovskyPaint.ArealDensityKgPerM2,
            YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.ArealDensityKgPerM2), 1e-9);
        Assert.Equal(34.5, YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.ArealDensityKgPerM2) / 1000.0, 0.1);
        Assert.Equal(0.5, YarkovskyPaint.PaintMassKg(r, 0.5, YarkovskyPaint.ArealDensityKgPerM2)
                          / YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.ArealDensityKgPerM2), 1e-12);
        Assert.Equal(0.0, YarkovskyPaint.PaintMassKg(r, -1.0, YarkovskyPaint.ArealDensityKgPerM2));

        // The cross-check: the published ≈5 t per round for a ≈340 m body, back out as an areal density.
        double apophisArea = 4.0 * Math.PI * 170.0 * 170.0;
        Assert.Equal(YarkovskyPaint.PaintballArealDensityKgPerM2, 5000.0 / apophisArea, 0.005);

        // And the punchline the README leans on: a 140 m rock's coat is the same order as one cargo pod.
        Assert.InRange(YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.ArealDensityKgPerM2)
                       / KineticImpactor.CargoPodMassKg, 1.0, 3.0);
    }

    /// <summary>Radiation pressure is the other half of an albedo change, and it is the honest trap: per
    /// second it is five times the Yarkovsky term. It still loses, because it pushes RADIALLY — the
    /// semi-major axis does not move, so its along-track drift is linear in time while Yarkovsky's is
    /// quadratic. Pinned at the year where they cross.</summary>
    [Fact]
    public void RadialPressureWinsTheFirstYearAndLosesEveryDecadeAfter()
    {
        double phi = YarkovskyPaint.RadiationFactor(
            YarkovskyPaint.SolarFlux(1.0), KineticImpactor.BulkDensity(RockComposition.CType), 140.0);
        double radial = YarkovskyPaint.RadialPressureDelta(White - YarkovskyPaint.BondAlbedo(RockComposition.CType), phi);
        double yark = Math.Abs(YarkovskyPaint.PaintDeltaAccel(C, 140.0, 1.0, Spin, 0.0, White, 1.0));
        double n = YarkovskyPaint.MeanMotion(1.0);

        Assert.True(radial > yark, "the radial term should be the larger acceleration — that is what makes it a trap");
        Assert.True(YarkovskyPaint.RadialAlongTrackDrift(radial, Year, n) > YarkovskyPaint.Miss(yark, Year));
        Assert.True(YarkovskyPaint.RadialAlongTrackDrift(radial, 10.0 * Year, n) < YarkovskyPaint.Miss(yark, 10.0 * Year));

        // Linear, not quadratic: ten times the warning is ten times the drift, not a hundred.
        Assert.Equal(10.0,
            YarkovskyPaint.RadialAlongTrackDrift(radial, 100.0 * Year, n)
            / YarkovskyPaint.RadialAlongTrackDrift(radial, 10.0 * Year, n), 1e-9);
        Assert.Equal(0.0, YarkovskyPaint.RadialAlongTrackDrift(radial, Year, 0.0));
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE VERDICT, AND ITS BOUNDARY.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The crossover the README prints, held to 2%. Tight on purpose: these numbers come out of the
    /// same code the README was pasted from, so the only thing a loose tolerance would buy is the freedom to
    /// change the model silently. The boundary the lab actually ships: at 1 AU a whitewashed 50 m C-type opens
    /// one Earth radius in ~36 years — so the technique is not impossible, it is just not a career.</summary>
    [Fact]
    public void TheCrossoverIsWhereTheReadmeSaysItIs()
    {
        Assert.Equal(36.0, PaintYears(C, 50.0, 1.0, EarthR), 0.8);
        Assert.Equal(78.0, PaintYears(C, 50.0, 1.0, SafeMiss), 1.6);
        Assert.Equal(130.0, PaintYears(C, 140.0, 1.0, SafeMiss), 2.6);
        Assert.Equal(208.0, PaintYears(S, 140.0, 1.0, SafeMiss), 4.2);
        Assert.Equal(8743.0, PaintYears(S, 140.0, YarkovskyPaint.RingsideHeliocentricAu, SafeMiss), 175.0);

        // The best case anywhere on the grid, and it is still half a working life.
        double best = double.PositiveInfinity;
        foreach (RockType type in AllTypes)
        {
            foreach (double r in KineticImpactor.RealisticRadiiMeters)
            {
                best = Math.Min(best, PaintYears(type, r, 1.0, EarthR));
            }
        }
        Assert.Equal(36.0, best, 0.8);
        Assert.True(best > 25.0,
            "if a paint job ever opens one Earth radius in under twenty-five years the verdict in the README " +
            $"has to be rewritten — the grid's best is now {best:F1} yr");
    }

    /// <summary>THE VERDICT. At Ringside Exchange — 9.58 AU out, where the #394 gig actually happens — not one
    /// rock on the C/S/M × size grid comes within five hundred years of the gig's own SafeMiss, and that is at
    /// the PHYSICAL CEILING: perfect spin, perfect coat, full coverage, pole-on. The gig is refused on physics,
    /// not on taste.</summary>
    [Fact]
    public void AtRingsideThePaintJobIsNeverAGig_EvenAtTheCeiling()
    {
        double bestCeilingYears = double.PositiveInfinity;

        foreach (RockType type in AllTypes)
        {
            foreach (double r in KineticImpactor.RealisticRadiiMeters)
            {
                double ceiling = YarkovskyPaint.CeilingTransverseAccel(type, r, YarkovskyPaint.RingsideHeliocentricAu);
                bestCeilingYears = Math.Min(bestCeilingYears, YarkovskyPaint.RequiredWarningSeconds(ceiling, SafeMiss) / Year);
            }
        }

        Assert.True(bestCeilingYears > 500.0,
            $"at Ringside the best a paint job could EVER do now clears SafeMiss in {bestCeilingYears:F0} years — " +
            "under five hundred and the lab's negative verdict is no longer honest, so rewrite it");
        Assert.Equal(648.0, bestCeilingYears, 15.0);

        // The seasonal term is the bigger of the two at Saturn distance, so grant the rock the tilt that
        // maximises it and paint that away as well — it is still a millennium.
        double rho = KineticImpactor.BulkDensity(RockComposition.CType);
        double inertia = YarkovskyPaint.ThermalInertia(RockComposition.CType);
        double seasonalDelta = Math.Abs(
            YarkovskyPaint.SeasonalTransverseAccel(YarkovskyPaint.BondAlbedo(RockComposition.CType), inertia, rho, 140.0, YarkovskyPaint.RingsideHeliocentricAu, Math.PI / 2.0)
            - YarkovskyPaint.SeasonalTransverseAccel(White, inertia, rho, 140.0, YarkovskyPaint.RingsideHeliocentricAu, Math.PI / 2.0));
        Assert.True(YarkovskyPaint.RequiredWarningSeconds(seasonalDelta, SafeMiss) / Year > 1000.0);
    }

    /// <summary>…and the comparison that makes the verdict a RANKING rather than an opinion: on one rock, one
    /// miss and one axis, against the four techniques the sibling labs already priced, using each of their own
    /// Core constants. The paint job must come last, by an order of magnitude.</summary>
    [Fact]
    public void ThePaintJobIsTheSlowestTechniqueInThePlaybook()
    {
        const double R = 140.0;
        double hull = KineticImpactor.RequiredLeadSeconds(S, R, KineticImpactor.OldHullMassKg, SafeMiss, KineticImpactor.ReferenceClosingSpeed);
        double pod = KineticImpactor.RequiredLeadSeconds(S, R, KineticImpactor.CargoPodMassKg, SafeMiss, KineticImpactor.ReferenceClosingSpeed);
        double tractor = GravityTractor.RequiredLeadSeconds(S, R, GravityTractor.ReferenceShipMassKg, SafeMiss);
        double laser = LaserAblation.RequiredBurnSeconds(S, R, LaserAblation.ReferencePlatformPowerWatts, SafeMiss);
        double paint = YarkovskyPaint.RequiredWarningSeconds(
            YarkovskyPaint.PaintDeltaAccel(S, R, 1.0, Spin, 0.0, White, 1.0), SafeMiss);

        foreach ((string name, double seconds) in new[] { ("cannonball/hull", hull), ("cannonball/pod", pod), ("tractor", tractor), ("long knife", laser) })
        {
            Assert.True(paint > seconds, $"the paint job ({paint / Year:F0} yr) is no longer slower than the {name} ({seconds / Year:F1} yr)");
        }

        Assert.True(paint / tractor > 10.0,
            $"the paint job is only {paint / tractor:F1}× the gravity tractor — the README calls it an order of " +
            "magnitude slower than the slowest technique already certified, so that sentence needs rewriting");
        Assert.Equal(18.0, paint / tractor, 0.5);

        // Same mass, thrown instead of spread: a 20 t cargo pod as a cannonball beats 34.5 t of paint outright.
        Assert.True(pod < paint, "a 20 t slug flown at the rock must still beat 34.5 t of paint spread over it");
    }
}
