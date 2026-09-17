using System.Globalization;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab49;

/// <summary>
/// #395 · The two pictures this lab needs. The first is the whole verdict in one frame: warning time needed,
/// per rock size, for every technique on the playbook — on a log axis, because the paint job is not a little
/// slower than the others, it is three to five decades of seconds slower. The second is the physics that makes
/// it so: the albedo lever only turns one way, and there is a ceiling nothing can lift.
///
/// <para>Hand-rolled SVG rather than LabViz, for lab 45's and lab 48's reason: <c>SpaceSails.LabViz</c> is a
/// heliocentric trajectory viewer — bodies on rails, paths in metres, an epoch — and it has no idea what a
/// warning year is. Same dark panel and light ink as labs 45/47/48, so it reads the same in a light and a dark
/// README, and the whole project runs <c>InvariantGlobalization</c> so the file is byte-identical wherever it
/// is generated.</para>
/// </summary>
internal static class PaintSvg
{
    private const string Ink = "#e6edf3";
    private const string Dim = "#8b949e";
    private const string Grid = "#21262d";
    private const string Axis = "#484f58";
    private const string Panel = "#0d1117";

    private static string N(double v, int d) => v.ToString("F" + d.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    // ── Picture 1: the crossover, on one log axis ────────────────────────────────────────────────

    /// <summary>Warning time needed to open the gig's own SafeMiss, per rock radius, for the five techniques.
    /// A career line at 40 years says where a gig stops being a gig.</summary>
    internal static string Crossover(IReadOnlyList<double> radii, double ringsideAu)
    {
        ArgumentNullException.ThrowIfNull(radii);

        const int W = 900, H = 520, L = 82, R = 292, T = 62, B = 62;
        const double SafeMiss = DeflectionGig.SafeMissMeters;
        double year = YarkovskyPaint.SecondsPerYear;
        var s = new RockType(RockComposition.SType);

        double Warn(Func<double, double> f, double r) => f(r) / year;
        static string Yr(double y) => y >= 10.0 ? y.ToString("N0", CultureInfo.InvariantCulture) : y.ToString("F2", CultureInfo.InvariantCulture);

        (string Label, string Colour, Func<double, double> Years)[] series =
        [
            ("cannonball · 200 t hull (36)", "#f0883e",
                r => Warn(x => KineticImpactor.RequiredLeadSeconds(s, x, KineticImpactor.OldHullMassKg, SafeMiss, KineticImpactor.ReferenceClosingSpeed), r)),
            ("long knife · 1 MW laser (39)", "#a371f7",
                r => Warn(x => LaserAblation.RequiredBurnSeconds(s, x, LaserAblation.ReferencePlatformPowerWatts, SafeMiss), r)),
            ("gravity tractor · 100 t (37)", "#58a6ff",
                r => Warn(x => GravityTractor.RequiredLeadSeconds(s, x, GravityTractor.ReferenceShipMassKg, SafeMiss), r)),
            ("PAINT JOB · whitewash, 1 AU (49)", "#3fb950",
                r => Warn(x => YarkovskyPaint.RequiredWarningSeconds(
                    YarkovskyPaint.PaintDeltaAccel(s, x, 1.0, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0), SafeMiss), r)),
            ("PAINT JOB · at Ringside, 9.58 AU", "#f778ba",
                r => Warn(x => YarkovskyPaint.RequiredWarningSeconds(
                    YarkovskyPaint.PaintDeltaAccel(s, x, ringsideAu, YarkovskyPaint.ReferenceSpinPeriodSeconds, 0.0, YarkovskyPaint.WhitePaintBondAlbedo, 1.0), SafeMiss), r)),
        ];

        double minR = radii[0], maxR = radii[^1];
        const double LoDecade = -2, HiDecade = 5;   // 0.01 yr → 100,000 yr

        double Px(double r) => L + (Math.Log10(r) - Math.Log10(minR)) / (Math.Log10(maxR) - Math.Log10(minR)) * (W - L - R);
        double Py(double yrs) => H - B - (Math.Log10(Math.Max(yrs, 1e-3)) - LoDecade) / (HiDecade - LoDecade) * (H - T - B);

        var sb = new StringBuilder();
        void A(string line) => sb.Append(line).Append('\n');

        A($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\" font-family=\"ui-monospace, SFMono-Regular, Menlo, monospace\">");
        A($"  <rect x=\"0\" y=\"0\" width=\"{W}\" height=\"{H}\" fill=\"{Panel}\"/>");
        A($"  <text x=\"{L}\" y=\"28\" fill=\"{Ink}\" font-size=\"15\">Lab 49 · how much WARNING each deflection needs to open the gig's own SafeMiss</text>");
        A($"  <text x=\"{L}\" y=\"46\" fill=\"{Dim}\" font-size=\"11\">S-type rock · miss = {N(SafeMiss / 1e6, 0)} Mm · every constant read from the sibling lab's own Core class</text>");

        for (int d = (int)LoDecade; d <= (int)HiDecade; d++)
        {
            double y = Py(Math.Pow(10, d));
            A($"  <line x1=\"{N(L, 1)}\" y1=\"{N(y, 1)}\" x2=\"{W - R}\" y2=\"{N(y, 1)}\" stroke=\"{Grid}\" stroke-width=\"1\"/>");
            string label = d < 0 ? N(Math.Pow(10, d), -d) : d <= 3 ? N(Math.Pow(10, d), 0) : "1e" + d.ToString(CultureInfo.InvariantCulture);
            A($"  <text x=\"{L - 8}\" y=\"{N(y + 4, 1)}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"end\">{label}</text>");
        }
        A($"  <line x1=\"{L}\" y1=\"{T}\" x2=\"{L}\" y2=\"{H - B}\" stroke=\"{Axis}\" stroke-width=\"1\"/>");
        A($"  <text x=\"22\" y=\"{(T + H - B) / 2}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\" transform=\"rotate(-90 22 {(T + H - B) / 2})\">warning needed (years, log)</text>");

        // A human career: the line above which "deflection" stops being something a captain does.
        double career = Py(40);
        A($"  <line x1=\"{L}\" y1=\"{N(career, 1)}\" x2=\"{W - R}\" y2=\"{N(career, 1)}\" stroke=\"#f85149\" stroke-width=\"1\" stroke-dasharray=\"5 4\"/>");
        A($"  <text x=\"{L + 8}\" y=\"{N(career - 6, 1)}\" fill=\"#f85149\" font-size=\"10\">40 years — one working career</text>");

        int legend = 0;
        foreach ((string label, string colour, Func<double, double> years) in series)
        {
            var pts = new StringBuilder();
            foreach (double r in radii)
            {
                pts.Append(N(Px(r), 1)).Append(',').Append(N(Py(years(r)), 1)).Append(' ');
            }
            A($"  <polyline fill=\"none\" stroke=\"{colour}\" stroke-width=\"2\" points=\"{pts}\"/>");
            foreach (double r in radii)
            {
                A($"  <circle cx=\"{N(Px(r), 1)}\" cy=\"{N(Py(years(r)), 1)}\" r=\"3.5\" fill=\"{colour}\"/>");
            }
            double ly = T + 10 + legend * 20;
            A($"  <line x1=\"{W - R + 6}\" y1=\"{N(ly - 4, 1)}\" x2=\"{W - R + 28}\" y2=\"{N(ly - 4, 1)}\" stroke=\"{colour}\" stroke-width=\"2\"/>");
            A($"  <text x=\"{W - R + 34}\" y=\"{N(ly, 1)}\" fill=\"{colour}\" font-size=\"10.5\">{label}</text>");
            A($"  <text x=\"{W - R + 34}\" y=\"{N(ly + 12, 1)}\" fill=\"{Dim}\" font-size=\"9.5\">{Yr(years(radii[0]))} yr at {N(radii[0], 0)} m → {Yr(years(radii[^1]))} yr at {N(radii[^1], 0)} m</text>");
            legend += 2;
        }

        foreach (double r in radii)
        {
            A($"  <text x=\"{N(Px(r), 1)}\" y=\"{H - B + 18}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"middle\">{N(r, 0)}</text>");
        }
        A($"  <text x=\"{(L + W - R) / 2}\" y=\"{H - 20}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\">rock radius (m, log) — 50 m Tunguska-class to a 1 km harbour-killer</text>");
        A("</svg>");
        return sb.ToString();
    }

    // ── Picture 2: the one-sided lever, and the ceiling over it ──────────────────────────────────

    /// <summary>Two panels. LEFT: the drift against coat albedo — strictly downhill, so the biggest change
    /// paint can buy is the natural drift itself. RIGHT: the lag function against spin period, with the
    /// ceiling G_max marked — the best any surface can do, which is what makes the verdict a bound.</summary>
    internal static string Lever()
    {
        const int W = 900, H = 410, T = 66, B = 66;
        const int L1 = 78, R1 = 470;          // left panel plot box
        const int L2 = 556, R2 = 32;          // right panel

        double rho = KineticImpactor.BulkDensity(RockComposition.CType);
        double inertia = YarkovskyPaint.ThermalInertia(RockComposition.CType);
        double natural = YarkovskyPaint.BondAlbedo(RockComposition.CType);
        double naturalF = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
            natural, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, 140.0, 1.0, 0.0));

        var sb = new StringBuilder();
        void A(string line) => sb.Append(line).Append('\n');

        A($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\" font-family=\"ui-monospace, SFMono-Regular, Menlo, monospace\">");
        A($"  <rect x=\"0\" y=\"0\" width=\"{W}\" height=\"{H}\" fill=\"{Panel}\"/>");
        A($"  <text x=\"{L1}\" y=\"28\" fill=\"{Ink}\" font-size=\"15\">Lab 49 · the lever only turns one way, and there is a lid on it</text>");
        A($"  <text x=\"{L1}\" y=\"46\" fill=\"{Dim}\" font-size=\"11\">140 m C-type at 1 AU · Bond albedo {N(natural, 3)} · Γ = {N(inertia, 0)} · prograde</text>");

        // ---- LEFT: |f_T| vs coat albedo -----------------------------------------------------------
        double PxA(double albedo) => L1 + albedo * (R1 - L1);
        double PyA(double f) => H - B - f / (naturalF * 1.08) * (H - T - B);

        for (int g = 0; g <= 4; g++)
        {
            double f = naturalF * 1.08 * g / 4.0;
            double y = PyA(f);
            A($"  <line x1=\"{L1}\" y1=\"{N(y, 1)}\" x2=\"{R1}\" y2=\"{N(y, 1)}\" stroke=\"{Grid}\" stroke-width=\"1\"/>");
            A($"  <text x=\"{L1 - 8}\" y=\"{N(y + 4, 1)}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"end\">{f.ToString("E1", CultureInfo.InvariantCulture)}</text>");
        }
        A($"  <line x1=\"{L1}\" y1=\"{T}\" x2=\"{L1}\" y2=\"{H - B}\" stroke=\"{Axis}\" stroke-width=\"1\"/>");
        A($"  <line x1=\"{L1}\" y1=\"{H - B}\" x2=\"{R1}\" y2=\"{H - B}\" stroke=\"{Axis}\" stroke-width=\"1\"/>");

        var curve = new StringBuilder();
        for (int i = 0; i <= 96; i++)
        {
            double albedo = i / 96.0 * 0.96;
            double f = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
                albedo, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, 140.0, 1.0, 0.0));
            curve.Append(N(PxA(albedo), 1)).Append(',').Append(N(PyA(f), 1)).Append(' ');
        }
        A($"  <polyline fill=\"none\" stroke=\"#3fb950\" stroke-width=\"2\" points=\"{curve}\"/>");

        // The rock starts here, and a full whitewash moves it to there. The gap is the entire deflection.
        double whiteF = Math.Abs(YarkovskyPaint.DiurnalTransverseAccel(
            YarkovskyPaint.WhitePaintBondAlbedo, inertia, YarkovskyPaint.ReferenceSpinPeriodSeconds, rho, 140.0, 1.0, 0.0));
        A($"  <line x1=\"{N(PxA(natural), 1)}\" y1=\"{N(PyA(naturalF), 1)}\" x2=\"{N(PxA(natural), 1)}\" y2=\"{N(PyA(whiteF), 1)}\" stroke=\"#f0883e\" stroke-width=\"1.5\" stroke-dasharray=\"4 3\"/>");
        A($"  <line x1=\"{N(PxA(natural), 1)}\" y1=\"{N(PyA(whiteF), 1)}\" x2=\"{N(PxA(YarkovskyPaint.WhitePaintBondAlbedo), 1)}\" y2=\"{N(PyA(whiteF), 1)}\" stroke=\"#f0883e\" stroke-width=\"1.5\" stroke-dasharray=\"4 3\"/>");
        A($"  <circle cx=\"{N(PxA(natural), 1)}\" cy=\"{N(PyA(naturalF), 1)}\" r=\"4.5\" fill=\"#3fb950\"/>");
        A($"  <circle cx=\"{N(PxA(YarkovskyPaint.WhitePaintBondAlbedo), 1)}\" cy=\"{N(PyA(whiteF), 1)}\" r=\"4.5\" fill=\"#f0883e\"/>");
        A($"  <text x=\"{N(PxA(natural) + 10, 1)}\" y=\"{N(PyA(naturalF) + 4, 1)}\" fill=\"#3fb950\" font-size=\"10.5\">bare rock — every rock starts at the top</text>");
        A($"  <text x=\"{N(PxA(YarkovskyPaint.WhitePaintBondAlbedo), 1)}\" y=\"{N(PyA(whiteF) - 10, 1)}\" fill=\"#f0883e\" font-size=\"10.5\" text-anchor=\"end\">whitewashed — the drift is switched OFF, never UP</text>");
        foreach (double tick in new[] { 0.0, 0.2, 0.4, 0.6, 0.8, 0.95 })
        {
            A($"  <text x=\"{N(PxA(tick), 1)}\" y=\"{H - B + 16}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"middle\">{N(tick, 2)}</text>");
        }
        A($"  <text x=\"{(L1 + R1) / 2}\" y=\"{H - B + 34}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\">coat Bond albedo →</text>");
        A($"  <text x=\"22\" y=\"{(T + H - B) / 2}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\" transform=\"rotate(-90 22 {(T + H - B) / 2})\">|f_T| (m/s²)</text>");

        // ---- RIGHT: G(Θ) against spin period, with the lid ----------------------------------------
        double lo = Math.Log10(0.25), hi = Math.Log10(2000.0);
        double PxB(double hours) => L2 + (Math.Log10(hours) - lo) / (hi - lo) * (W - L2 - R2);
        double PyB(double g) => H - B - g / (YarkovskyPaint.MaxThermalLag * 1.18) * (H - T - B);

        double GAt(double hours)
        {
            double theta = YarkovskyPaint.ThermalParameter(
                inertia, YarkovskyPaint.SpinRate(hours * 3600.0),
                YarkovskyPaint.SubsolarTemperature(1.0 - natural, YarkovskyPaint.SolarFlux(1.0)));
            return YarkovskyPaint.ThermalLag(theta);
        }

        for (int g = 0; g <= 4; g++)
        {
            double v = YarkovskyPaint.MaxThermalLag * 1.18 * g / 4.0;
            double y = PyB(v);
            A($"  <line x1=\"{L2}\" y1=\"{N(y, 1)}\" x2=\"{W - R2}\" y2=\"{N(y, 1)}\" stroke=\"{Grid}\" stroke-width=\"1\"/>");
            A($"  <text x=\"{L2 - 8}\" y=\"{N(y + 4, 1)}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"end\">{N(v, 3)}</text>");
        }
        A($"  <line x1=\"{L2}\" y1=\"{T}\" x2=\"{L2}\" y2=\"{H - B}\" stroke=\"{Axis}\" stroke-width=\"1\"/>");
        A($"  <line x1=\"{L2}\" y1=\"{H - B}\" x2=\"{W - R2}\" y2=\"{H - B}\" stroke=\"{Axis}\" stroke-width=\"1\"/>");

        double lid = PyB(YarkovskyPaint.MaxThermalLag);
        A($"  <line x1=\"{L2}\" y1=\"{N(lid, 1)}\" x2=\"{W - R2}\" y2=\"{N(lid, 1)}\" stroke=\"#f85149\" stroke-width=\"1\" stroke-dasharray=\"5 4\"/>");
        A($"  <text x=\"{L2 + 8}\" y=\"{N(lid - 6, 1)}\" fill=\"#f85149\" font-size=\"10\">G_max = {N(YarkovskyPaint.MaxThermalLag, 4)} — no surface beats this</text>");

        var lag = new StringBuilder();
        for (int i = 0; i <= 120; i++)
        {
            double hours = Math.Pow(10, lo + (hi - lo) * i / 120.0);
            lag.Append(N(PxB(hours), 1)).Append(',').Append(N(PyB(GAt(hours)), 1)).Append(' ');
        }
        A($"  <polyline fill=\"none\" stroke=\"#58a6ff\" stroke-width=\"2\" points=\"{lag}\"/>");

        foreach (double hours in new[] { 0.5, 4.0, 20.0, 200.0, 1000.0 })
        {
            A($"  <circle cx=\"{N(PxB(hours), 1)}\" cy=\"{N(PyB(GAt(hours)), 1)}\" r=\"3.5\" fill=\"#58a6ff\"/>");
            A($"  <text x=\"{N(PxB(hours), 1)}\" y=\"{H - B + 16}\" fill=\"{Dim}\" font-size=\"10\" text-anchor=\"middle\">{(hours < 1 ? N(hours, 1) : N(hours, 0))}</text>");
        }
        A($"  <text x=\"{(L2 + W - R2) / 2}\" y=\"{H - B + 34}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\">rotation period (hours, log)</text>");
        A($"  <text x=\"{L2 - 46}\" y=\"{(T + H - B) / 2}\" fill=\"{Dim}\" font-size=\"11\" text-anchor=\"middle\" transform=\"rotate(-90 {L2 - 46} {(T + H - B) / 2})\">thermal lag G(Θ)</text>");
        A("</svg>");
        return sb.ToString();
    }
}
