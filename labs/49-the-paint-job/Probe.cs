// Lab 49 — The paint job (albedo / Yarkovsky deflection)
//
// Teaching voice: this is the one deflection on the owner's playbook (#395, candidate 5) that nature already
// runs on its own, unattended, on every rock in the sky. A body absorbs sunlight in its morning and throws the
// heat back in its afternoon; the infrared photons carry momentum; the recoil is a real thrust that has been
// MEASURED on real asteroids to the metre per year. The gig idea is to steal the dial: paint the rock and
// change how much light it absorbs. The issue itself doubts it — "probably too slow to be a gig — the lab
// exists to prove WHY it's not" — so this probe is written to give the technique every advantage it can
// honestly be given, and then to print what is left.
//
//   A — CALIBRATION. Bennu's measured −284.6 m/yr and Golevka's −95.6 m/yr, against the model. If the model
//       cannot reproduce the two drifts humanity has actually measured, nothing below it counts.
//   B — the natural drift as a function of SIZE, TYPE, SPIN and DISTANCE — where the effect is strong.
//   C — the LEVER: what a coat changes, and the algebra that makes the lever ONE-SIDED (you can turn the
//       drift off; you can never turn it up).
//   D — the LOGISTICS: how much paint, derived from film thickness, cross-checked against the published
//       "paintball" back-of-envelope.
//   E — the CROSSOVER: warning time needed, per size, against one Earth radius and against the gig's own
//       SafeMiss — at 1 AU where the real threats live, and at 9.58 AU where Ringside does.
//   F — the SAME AXIS as the rest of the playbook: cannonball, tractor, rock driver, long knife, paint.
//
// NOT a shipped gig. IRONCLAD RULE: every number in the README came from running this probe.

using System.Globalization;
using SpaceSails.Core;
using SpaceSails.Labs.Lab49;

static string F(double v, int d) => v.ToString("F" + d.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
static string Sci(double v) => v.ToString("E2", CultureInfo.InvariantCulture);

const double Year = YarkovskyPaint.SecondsPerYear;
static string Years(double s)
{
    if (double.IsInfinity(s)) { return "never"; }
    double y = s / YarkovskyPaint.SecondsPerYear;
    return y >= 1e5 ? y.ToString("E2", CultureInfo.InvariantCulture)
         : y >= 10.0 ? y.ToString("N0", CultureInfo.InvariantCulture)
         : y.ToString("F2", CultureInfo.InvariantCulture);
}

const double SafeMiss = DeflectionGig.SafeMissMeters;          // 3e7 m — clear Ringside on the map
const double EarthR = YarkovskyPaint.EarthRadiusMeters;        // 6.371e6 m — the real-world yardstick
double ringsideAu = YarkovskyPaint.RingsideHeliocentricAu;

RockType cRock = new(RockComposition.CType);
RockType sRock = new(RockComposition.SType);
RockType mRock = new(RockComposition.MType);
RockType[] types = [cRock, sRock, mRock];
double[] radii = KineticImpactor.RealisticRadiiMeters;          // 50 / 140 / 370 / 1000 m

Console.WriteLine("=== Lab 49 — The paint job (albedo / Yarkovsky deflection) ===");
Console.WriteLine("Diurnal Yarkovsky, linearised plane-parallel: f_T = (4/9)·α·Φ·G(Θ)·cos γ, Φ = 3F/(4ρRc),");
Console.WriteLine($"G(Θ) = Θ/(1+2Θ+2Θ²) with Θ = Γ√ω/(εσT*³), ε = {F(YarkovskyPaint.Emissivity, 2)}. da/dt = 2·f_T/n (Gauss).");
Console.WriteLine($"Along-track leverage shared with labs 37/39: miss = {F(GravityTractor.ContinuousTowLeverage, 1)}·a·T².");
Console.WriteLine($"Miss bars: one Earth radius = {Sci(EarthR)} m; the gig's SafeMiss = {Sci(SafeMiss)} m (graze = {Sci(DeflectionGig.GrazeMissMeters)} m).");
Console.WriteLine($"Ringside Exchange rides Saturn: {F(ringsideAu, 2)} AU, where sunlight is {F(YarkovskyPaint.SolarFlux(1.0) / YarkovskyPaint.SolarFlux(ringsideAu), 0)}× thinner than at Earth.");
Console.WriteLine();

// ---- Section A: CALIBRATION against the measured drifts ---------------------------------------------
Console.WriteLine("=== Section A: CALIBRATION — the two Yarkovsky drifts humanity has actually measured ===");
Console.WriteLine("A model of a deflection nobody has flown is only worth its agreement with the effect nature already runs.");
Console.WriteLine();
Console.WriteLine($"{"anchor",-18}{"R (m)",8}{"a (AU)",8}{"P_rot (h)",11}{"γ (°)",8}{"Γ",7}{"measured m/yr",15}{"model m/yr",13}{"ratio",8}");
foreach (YarkovskyPaint.DriftAnchor anchor in new[] { YarkovskyPaint.Bennu, YarkovskyPaint.Golevka })
{
    double predicted = YarkovskyPaint.PredictedDrift(anchor);
    Console.WriteLine($"{anchor.Name,-18}{F(anchor.RadiusMeters, 0),8}{F(anchor.SemiMajorAxisAu, 3),8}" +
                      $"{F(anchor.SpinPeriodSeconds / 3600.0, 2),11}{F(anchor.ObliquityDegrees, 1),8}{F(anchor.ThermalInertia, 0),7}" +
                      $"{F(anchor.MeasuredDriftMetersPerYear, 1),15}{F(predicted, 1),13}" +
                      $"{F(predicted / anchor.MeasuredDriftMetersPerYear, 2),8}");
}
Console.WriteLine();
Console.WriteLine("Golevka's thermal inertia is a model fit, not a measurement — sweep it and watch the answer move:");
Console.WriteLine($"{"Γ assumed",12}{"model m/yr",13}{"ratio to measured",19}");
foreach (double gamma in new[] { 50.0, 100.0, 200.0, 300.0 })
{
    YarkovskyPaint.DriftAnchor g = YarkovskyPaint.Golevka with { ThermalInertia = gamma };
    double predicted = YarkovskyPaint.PredictedDrift(g);
    Console.WriteLine($"{F(gamma, 0),12}{F(predicted, 1),13}{F(predicted / g.MeasuredDriftMetersPerYear, 2),19}");
}
Console.WriteLine();

// ---- Section B: the natural drift ------------------------------------------------------------------
Console.WriteLine("=== Section B: the NATURAL drift — size, type, spin, distance ===");
Console.WriteLine($"At 1 AU, prograde (cos γ = 1), spin {F(YarkovskyPaint.ReferenceSpinPeriodSeconds / 3600.0, 0)} h, the C/S/M albedo + thermal-inertia table.");
Console.WriteLine();
Console.WriteLine($"{"type",-22}{"A_bond",8}{"Γ",7}{"radius",9}{"Θ",8}{"G(Θ)",9}{"f_T (m/s²)",14}{"da/dt (m/yr)",14}");
foreach (RockType t in types)
{
    double albedo = YarkovskyPaint.BondAlbedo(t.Composition);
    double inertia = YarkovskyPaint.ThermalInertia(t.Composition);
    double rho = KineticImpactor.BulkDensity(t.Composition);
    double theta = YarkovskyPaint.ThermalParameter(
        inertia, YarkovskyPaint.SpinRate(YarkovskyPaint.ReferenceSpinPeriodSeconds),
        YarkovskyPaint.SubsolarTemperature(1.0 - albedo, YarkovskyPaint.SolarFlux(1.0)));
    foreach (double r in new[] { 50.0, 140.0, 1000.0 })
    {
        double f = YarkovskyPaint.DiurnalTransverseAccel(
            albedo, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, r, 1.0, 0.0);
        Console.WriteLine($"{t.Label,-22}{F(albedo, 3),8}{F(inertia, 0),7}{F(r, 0) + " m",9}{F(theta, 2),8}" +
                          $"{F(YarkovskyPaint.ThermalLag(theta), 4),9}{Sci(f),14}{F(YarkovskyPaint.SemiMajorDriftMetersPerYear(f, 1.0), 1),14}");
    }
}
Console.WriteLine();
Console.WriteLine("SPIN has an optimum, and it is not 'as fast as possible' — G(Θ) is zero at both ends.");
Console.WriteLine($"(140 m C-type at 1 AU. Peak lag G = {F(YarkovskyPaint.MaxThermalLag, 4)} at Θ = {F(YarkovskyPaint.OptimalThermalParameter, 4)}.)");
Console.WriteLine();
Console.WriteLine($"{"P_rot",12}{"Θ",9}{"G(Θ)",9}{"da/dt (m/yr)",14}");
foreach (double hours in new[] { 0.5, 2.0, 4.0, 10.0, 20.0, 50.0, 200.0, 1000.0 })
{
    double p = hours * 3600.0;
    double albedo = YarkovskyPaint.BondAlbedo(RockComposition.CType);
    double theta = YarkovskyPaint.ThermalParameter(
        YarkovskyPaint.ThermalInertia(RockComposition.CType), YarkovskyPaint.SpinRate(p),
        YarkovskyPaint.SubsolarTemperature(1.0 - albedo, YarkovskyPaint.SolarFlux(1.0)));
    double f = YarkovskyPaint.DiurnalTransverseAccel(
        albedo, YarkovskyPaint.ThermalInertia(RockComposition.CType), p,
        KineticImpactor.BulkDensity(RockComposition.CType), 140.0, 1.0, 0.0);
    Console.WriteLine($"{F(hours, 1) + " h",12}{F(theta, 3),9}{F(YarkovskyPaint.ThermalLag(theta), 4),9}{F(YarkovskyPaint.SemiMajorDriftMetersPerYear(f, 1.0), 1),14}");
}
Console.WriteLine();
Console.WriteLine("DISTANCE, on the same 140 m C-type at its reference spin — and this is where the game lives:");
Console.WriteLine();
Console.WriteLine($"{"heliocentric",14}{"flux (W/m²)",13}{"Θ",8}{"f_T (m/s²)",14}{"da/dt (m/yr)",14}");
foreach (double au in new[] { 1.0, 1.5, 2.5, 5.2, ringsideAu })
{
    double albedo = YarkovskyPaint.BondAlbedo(RockComposition.CType);
    double theta = YarkovskyPaint.ThermalParameter(
        YarkovskyPaint.ThermalInertia(RockComposition.CType),
        YarkovskyPaint.SpinRate(YarkovskyPaint.ReferenceSpinPeriodSeconds),
        YarkovskyPaint.SubsolarTemperature(1.0 - albedo, YarkovskyPaint.SolarFlux(au)));
    double f = YarkovskyPaint.DiurnalTransverseAccel(
        albedo, YarkovskyPaint.ThermalInertia(RockComposition.CType), YarkovskyPaint.ReferenceSpinPeriodSeconds,
        KineticImpactor.BulkDensity(RockComposition.CType), 140.0, au, 0.0);
    Console.WriteLine($"{F(au, 2) + " AU",14}{F(YarkovskyPaint.SolarFlux(au), 1),13}{F(theta, 2),8}{Sci(f),14}{F(YarkovskyPaint.SemiMajorDriftMetersPerYear(f, au), 2),14}");
}
Console.WriteLine();
Console.WriteLine("SEASONAL term (the axial-tilt half, run on the orbital period instead of the rotation period),");
Console.WriteLine("worst case γ = 90° where sin²γ = 1, on the same 140 m C-type — printed so the omission is measured:");
Console.WriteLine();
Console.WriteLine($"{"heliocentric",14}{"diurnal f_T",14}{"seasonal f_T",14}{"seasonal share",16}");
foreach (double au in new[] { 1.0, 2.5, ringsideAu })
{
    double albedo = YarkovskyPaint.BondAlbedo(RockComposition.CType);
    double rho = KineticImpactor.BulkDensity(RockComposition.CType);
    double inertia = YarkovskyPaint.ThermalInertia(RockComposition.CType);
    double diurnal = YarkovskyPaint.DiurnalTransverseAccel(
        albedo, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, 140.0, au, 0.0);
    double seasonal = YarkovskyPaint.SeasonalTransverseAccel(albedo, inertia, rho, 140.0, au, Math.PI / 2.0);
    Console.WriteLine($"{F(au, 2) + " AU",14}{Sci(diurnal),14}{Sci(seasonal),14}{F(100.0 * Math.Abs(seasonal / diurnal), 2) + " %",16}");
}
Console.WriteLine();

// ---- Section C: the lever --------------------------------------------------------------------------
Console.WriteLine("=== Section C: the LEVER — what a coat actually changes, and why it only turns ONE way ===");
Console.WriteLine("α·G(Θ(α)) is strictly increasing in α: d/dα = (Θ/D²)·(0.25 + 2Θ + 3.5Θ²) > 0 for every Θ, D = 1+2Θ+2Θ².");
Console.WriteLine("So brighter is ALWAYS slower. The best a paint job can do is switch the natural drift off.");
Console.WriteLine();
Console.WriteLine($"(140 m C-type at 1 AU, {F(YarkovskyPaint.ReferenceSpinPeriodSeconds / 3600.0, 0)} h spin, full coverage. Natural Bond albedo {F(YarkovskyPaint.BondAlbedo(RockComposition.CType), 3)}.)");
Console.WriteLine();
Console.WriteLine($"{"coat A_bond",13}{"α",8}{"Θ",8}{"G(Θ)",9}{"f_T (m/s²)",14}{"Δ from natural",16}{"|Δ|/natural",13}");
{
    double rho = KineticImpactor.BulkDensity(RockComposition.CType);
    double inertia = YarkovskyPaint.ThermalInertia(RockComposition.CType);
    double natural = YarkovskyPaint.DiurnalTransverseAccel(
        YarkovskyPaint.BondAlbedo(RockComposition.CType), inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds,
        rho, 140.0, 1.0, 0.0);
    foreach (double coat in new[] { YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.BlackPaintBondAlbedo, 0.20, 0.50, YarkovskyPaint.WhitePaintBondAlbedo, 0.95 })
    {
        double alpha = 1.0 - coat;
        double theta = YarkovskyPaint.ThermalParameter(
            inertia, YarkovskyPaint.SpinRate(YarkovskyPaint.ReferenceSpinPeriodSeconds),
            YarkovskyPaint.SubsolarTemperature(alpha, YarkovskyPaint.SolarFlux(1.0)));
        double f = YarkovskyPaint.DiurnalTransverseAccel(
            coat, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, 140.0, 1.0, 0.0);
        Console.WriteLine($"{F(coat, 3),13}{F(alpha, 3),8}{F(theta, 2),8}{F(YarkovskyPaint.ThermalLag(theta), 4),9}" +
                          $"{Sci(f),14}{Sci(natural - f),16}{F(Math.Abs(natural - f) / Math.Abs(natural), 3),13}");
    }
}
Console.WriteLine();
Console.WriteLine("COVERAGE is linear in the surface-averaged albedo, which is the correction the issue's phrasing needs:");
Console.WriteLine("\"paint one side\" does nothing extra — over a 4-hour spin, one side is every side, and only the MEAN survives.");
Console.WriteLine();
Console.WriteLine($"{"coverage",10}{"A_eff",9}{"Δf_T (m/s²)",15}{"share of full coat",20}");
{
    double full = Math.Abs(YarkovskyPaint.PaintDeltaAccel(cRock, 140.0, 1.0, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0));
    foreach (double cov in new[] { 0.10, 0.25, 0.50, 0.75, 1.00 })
    {
        double d = Math.Abs(YarkovskyPaint.PaintDeltaAccel(cRock, 140.0, 1.0, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, cov));
        Console.WriteLine($"{F(100.0 * cov, 0) + " %",10}{F(YarkovskyPaint.EffectiveBondAlbedo(YarkovskyPaint.BondAlbedo(RockComposition.CType), YarkovskyPaint.WhitePaintBondAlbedo, cov), 3),9}{Sci(d),15}{F(100.0 * d / full, 1) + " %",20}");
    }
}
Console.WriteLine();
Console.WriteLine("And the OTHER half of an albedo change — direct radiation pressure. It is bigger per second and it loses anyway,");
Console.WriteLine("because a radial push does not change the semi-major axis: its along-track drift is LINEAR in time, not quadratic.");
Console.WriteLine();
{
    double phi = YarkovskyPaint.RadiationFactor(YarkovskyPaint.SolarFlux(1.0), KineticImpactor.BulkDensity(RockComposition.CType), 140.0);
    double dA = YarkovskyPaint.WhitePaintBondAlbedo - YarkovskyPaint.BondAlbedo(RockComposition.CType);
    double radial = YarkovskyPaint.RadialPressureDelta(dA, phi);
    double yark = Math.Abs(YarkovskyPaint.PaintDeltaAccel(cRock, 140.0, 1.0, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0));
    Console.WriteLine($"radial Δa_R = {Sci(radial)} m/s² vs along-track Δf_T = {Sci(yark)} m/s² — the radial one is {F(radial / yark, 1)}× larger.");
    Console.WriteLine($"{"warning",10}{"radial drift (m)",18}{"Yarkovsky miss (m)",20}{"winner",10}");
    foreach (double yr in new[] { 1.0, 10.0, 30.0, 100.0 })
    {
        double t = yr * Year;
        double rd = YarkovskyPaint.RadialAlongTrackDrift(radial, t, YarkovskyPaint.MeanMotion(1.0));
        double ym = YarkovskyPaint.Miss(yark, t);
        Console.WriteLine($"{F(yr, 0) + " yr",10}{Sci(rd),18}{Sci(ym),20}{(ym >= rd ? "Yarkovsky" : "radial"),10}");
    }
}
Console.WriteLine();

// ---- Section D: the logistics ----------------------------------------------------------------------
Console.WriteLine("=== Section D: the LOGISTICS — how much paint, derived ===");
Console.WriteLine($"An opaque dry film {Sci(YarkovskyPaint.PaintFilmThicknessMeters)} m thick at {F(YarkovskyPaint.PaintFilmDensityKgPerM3, 0)} kg/m³ is {F(YarkovskyPaint.ArealDensityKgPerM2, 3)} kg/m². Mass = 4πR²·f·σ.");
Console.WriteLine($"Cross-check: the 2012 MIT \"paintball\" back-of-envelope budgeted ≈5 t per round for Apophis (≈{F(YarkovskyPaint.PaintballArealDensityKgPerM2, 3)} kg/m², a ~10 μm film).");
Console.WriteLine();
Console.WriteLine($"{"radius",9}{"surface (m²)",15}{"opaque coat (t)",18}{"paintball coat (t)",20}{"vs a 20 t cargo pod",21}");
foreach (double r in radii)
{
    double area = 4.0 * Math.PI * r * r;
    double heavy = YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.ArealDensityKgPerM2);
    double light = YarkovskyPaint.PaintMassKg(r, 1.0, YarkovskyPaint.PaintballArealDensityKgPerM2);
    Console.WriteLine($"{F(r, 0) + " m",9}{Sci(area),15}{F(heavy / 1000.0, 1),18}{F(light / 1000.0, 1),20}{F(heavy / KineticImpactor.CargoPodMassKg, 1) + "× / " + F(light / KineticImpactor.CargoPodMassKg, 2) + "×",21}");
}
Console.WriteLine();
Console.WriteLine("The paint bill is NOT the problem — a 140 m rock is a few tonnes to a few tens of tonnes, the same order as");
Console.WriteLine($"the cannonball's {F(KineticImpactor.CargoPodMassKg / 1000.0, 0)} t cargo pod and a fraction of its {F(KineticImpactor.OldHullMassKg / 1000.0, 0)} t old hull. The bill is in DECADES, not in tonnes.");
Console.WriteLine();

// ---- Section E: the crossover ----------------------------------------------------------------------
Console.WriteLine("=== Section E: the CROSSOVER — how much warning, honestly, for the best coat there is ===");
Console.WriteLine("Two models per row. REALISTIC: the C/S/M table at a 4 h spin, prograde, fully whitewashed.");
Console.WriteLine("CEILING: (4/9)·α·Φ·G_max — the best a paint job could EVER do on this rock at this distance, whatever");
Console.WriteLine("its spin, its regolith or the chemistry of the coat. The ceiling cannot be argued upward.");
Console.WriteLine();
foreach (double au in new[] { 1.0, ringsideAu })
{
    Console.WriteLine($"--- at {F(au, 2)} AU (flux {F(YarkovskyPaint.SolarFlux(au), 1)} W/m²) ---");
    Console.WriteLine($"{"type",-22}{"radius",9}{"Δf_T real",13}{"Δf_T ceiling",14}{"yr → 1 R⊕",12}{"yr → SafeMiss",15}{"ceiling yr → SafeMiss",22}");
    foreach (RockType t in types)
    {
        foreach (double r in radii)
        {
            double real = Math.Abs(YarkovskyPaint.PaintDeltaAccel(
                t, r, au, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0));
            double ceiling = YarkovskyPaint.CeilingTransverseAccel(t, r, au);
            Console.WriteLine($"{t.Label,-22}{F(r, 0) + " m",9}{Sci(real),13}{Sci(ceiling),14}" +
                              $"{Years(YarkovskyPaint.RequiredWarningSeconds(real, EarthR)),12}" +
                              $"{Years(YarkovskyPaint.RequiredWarningSeconds(real, SafeMiss)),15}" +
                              $"{Years(YarkovskyPaint.RequiredWarningSeconds(ceiling, SafeMiss)),22}");
        }
    }
    Console.WriteLine();
}

// ---- Section F: the same axis as the rest of the playbook -------------------------------------------
Console.WriteLine("=== Section F: the whole playbook on ONE axis — warning needed to open SafeMiss on a 140 m S-type ===");
Console.WriteLine("Every constant below is the sibling lab's own, read from Core, not retyped: lab 36 (200 t hull at 6 km/s),");
Console.WriteLine($"lab 37 ({F(GravityTractor.ReferenceShipMassKg / 1000.0, 0)} t tug at {F(GravityTractor.StandoffFactor, 1)}R), lab 38 ({F(RockMassDriver.ReferenceThroughputKgPerSecond, 0)} kg/s rig), lab 39 ({F(LaserAblation.ReferencePlatformPowerWatts / 1e6, 0)} MW), and this lab.");
Console.WriteLine();
{
    const double R = 140.0;
    double hull = KineticImpactor.RequiredLeadSeconds(sRock, R, KineticImpactor.OldHullMassKg, SafeMiss, KineticImpactor.ReferenceClosingSpeed);
    double pod = KineticImpactor.RequiredLeadSeconds(sRock, R, KineticImpactor.CargoPodMassKg, SafeMiss, KineticImpactor.ReferenceClosingSpeed);
    double tractor = GravityTractor.RequiredLeadSeconds(sRock, R, GravityTractor.ReferenceShipMassKg, SafeMiss);
    double laser = LaserAblation.RequiredBurnSeconds(sRock, R, LaserAblation.ReferencePlatformPowerWatts, SafeMiss);
    double paint1 = YarkovskyPaint.RequiredWarningSeconds(
        YarkovskyPaint.PaintDeltaAccel(sRock, R, 1.0, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0), SafeMiss);
    double paintCeil1 = YarkovskyPaint.RequiredWarningSeconds(YarkovskyPaint.CeilingTransverseAccel(sRock, R, 1.0), SafeMiss);
    double paintRing = YarkovskyPaint.RequiredWarningSeconds(
        YarkovskyPaint.PaintDeltaAccel(sRock, R, ringsideAu, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0), SafeMiss);

    // Lab 38's rig quotes a RUN time for a given lead, so its "warning needed" is the lead at which the run
    // just fits inside it — bisected on the lab's own function rather than on a formula retyped here.
    double driver;
    {
        double loT = 3600.0, hiT = 100.0 * Year;
        for (int i = 0; i < 200; i++)
        {
            double mid = 0.5 * (loT + hiT);
            double run = RockMassDriver.RunSecondsToDeflect(
                sRock, R, SafeMiss, mid, RockMassDriver.ReferenceThroughputKgPerSecond, RockMassDriver.ExhaustVelocityMetersPerSecond);
            if (run > mid) { loT = mid; } else { hiT = mid; }
        }
        driver = 0.5 * (loT + hiT);
    }

    (string Name, double Seconds, string Cost)[] rows =
    [
        ("mass driver ON the rock (38)", driver, "a landed rig + 62 MW"),
        ("cannonball, 200 t hull (36)", hull, "200 t flown at 6 km/s"),
        ("long knife, 1 MW laser (39)", laser, "1 MW, held continuously"),
        ("cannonball, 20 t pod (36)", pod, "20 t flown at 6 km/s"),
        ("gravity tractor, 100 t (37)", tractor, "a tug parked for the duration"),
        ("PAINT JOB at 1 AU (49)", paint1, "34.5 t of paint"),
        ("PAINT JOB ceiling, 1 AU (49)", paintCeil1, "…and a perfect spin"),
        ("PAINT JOB at Ringside (49)", paintRing, "34.5 t of paint, 9.58 AU"),
    ];
    Console.WriteLine($"{"technique",-32}{"warning needed",16}{"× the tractor",15}  {"what it costs",-28}");
    foreach ((string name, double seconds, string cost) in rows.OrderBy(r => r.Seconds))
    {
        Console.WriteLine($"{name,-32}{Years(seconds) + " yr",16}{F(seconds / tractor, 2) + "×",15}  {cost,-28}");
    }
    Console.WriteLine();
    Console.WriteLine($"VERDICT: the slowest technique already shipped in a lab is the gravity tractor at {Years(tractor)} yr. The paint job");
    Console.WriteLine($"at 1 AU is {F(paint1 / tractor, 0)}× slower than that, and at Ringside — where the gig actually happens — {F(paintRing / tractor, 0)}× slower.");
    Console.WriteLine();
    Console.WriteLine("The one escape hatch, closed: at Saturn distance the DIURNAL lag has collapsed (Θ = 46.7, G = 0.011) and the");
    Console.WriteLine("SEASONAL term is the bigger of the two. Grant the rock the tilt that maximises it (γ = 90°, sin²γ = 1) and");
    Console.WriteLine("paint that away too — the best case the seasonal term can offer at Ringside:");
    {
        double rho = KineticImpactor.BulkDensity(RockComposition.CType);
        double inertia = YarkovskyPaint.ThermalInertia(RockComposition.CType);
        double bare = YarkovskyPaint.SeasonalTransverseAccel(
            YarkovskyPaint.BondAlbedo(RockComposition.CType), inertia, rho, R, ringsideAu, Math.PI / 2.0);
        double white = YarkovskyPaint.SeasonalTransverseAccel(
            YarkovskyPaint.WhitePaintBondAlbedo, inertia, rho, R, ringsideAu, Math.PI / 2.0);
        double delta = Math.Abs(bare - white);
        Console.WriteLine($"  seasonal Δf_T = {Sci(delta)} m/s² → {Years(YarkovskyPaint.RequiredWarningSeconds(delta, SafeMiss))} yr to SafeMiss, " +
                          $"{Years(YarkovskyPaint.RequiredWarningSeconds(delta, EarthR))} yr to one Earth radius.");
    }
}
Console.WriteLine();

// ---- the pictures -----------------------------------------------------------------------------------
if (Array.IndexOf(args, "--svg") >= 0)
{
    string dir = Array.IndexOf(args, "--svg") + 1 < args.Length && !args[Array.IndexOf(args, "--svg") + 1].StartsWith("--", StringComparison.Ordinal)
        ? args[Array.IndexOf(args, "--svg") + 1]
        : ".";
    Directory.CreateDirectory(dir);

    string crossover = Path.Combine(dir, "warning-crossover.svg");
    File.WriteAllText(crossover, PaintSvg.Crossover(radii, ringsideAu));
    Console.WriteLine($"[svg] wrote {Path.GetFullPath(crossover)}");

    string lever = Path.Combine(dir, "the-one-sided-lever.svg");
    File.WriteAllText(lever, PaintSvg.Lever());
    Console.WriteLine($"[svg] wrote {Path.GetFullPath(lever)}");
}
