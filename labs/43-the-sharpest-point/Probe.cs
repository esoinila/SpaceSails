// Lab 43 — The sharpest point: what a hull discharge actually looks like, costs and tells.
//
// Teaching voice: the owner asked for a beautiful effect and put a condition on it.
//
//   "the charge is being equaluzed could have plasma ball like beautifull effect if physics supports it"
//
// IF PHYSICS SUPPORTS IT. So before anybody animates anything, this lab asks the physics five questions —
// and the answers changed three things about the feature, which is the whole reason labs exist here.
//
//   A — WHERE IT STARTS. A discharge does not leave a hull evenly; it leaves from wherever the field is
//       strongest, and field strength is potential over radius of curvature. Printed for the hull, an edge,
//       a mast tip and an antenna whip. If the ratio is large, "plasma ball around the ship" is a lie and
//       "plume off the sharpest thing she has" is the truth.
//   B — WHAT IT COSTS. Hull capacitance is geometry (C = 4πε₀R), stored energy is ½CV². The number decides
//       whether the effect should read as a spark or an explosion — and the game must not draw an explosion
//       for a joule.
//   C — HOW LONG IT LASTS. Charge over emission current. If the honest duration is milliseconds, then a
//       long on-screen effect is a stylisation and should be sold as the CONTACTOR RUNNING rather than as
//       one very slow spark.
//   D — CAN ANYBODY SEE IT? The game already says a charged hull is seen three times as far
//       (SensorModel.ChargeGlowFactor). This probe checks that claim against the hull's own reflected
//       sunlight, which is the thing an observer is actually looking at.
//   E — WHAT THE CONTACTOR IS UP AGAINST. The ambient electron current arriving on her skin, versus what a
//       hollow cathode emits. Decides whether the automatic in #523 can win at all, and in what conditions.
//
// Bands and thresholds come from Core (HullCharge) so the lab and the board cannot drift.

using SpaceSails.Core;

namespace SpaceSails.Labs.Lab43;

internal static class Probe
{
    // ── Physical constants (SI) ───────────────────────────────────────────────────────────────────────
    private const double Eps0 = 8.8541878128e-12;   // F/m
    private const double ElectronCharge = 1.602176634e-19;   // C
    private const double ElectronMass = 9.1093837015e-31;    // kg
    private const double SolarConstant1Au = 1361.0;          // W/m² at 1 AU

    // ── The ship, in metres. Her deck plan is ~54 × 20 deck units; a deck unit reads as about a metre of
    //    real hull, so a 10 m characteristic radius is the honest order of magnitude for a small freighter.
    private const double HullRadius = 10.0;

    /// <summary>What the game's charge scale means in volts at full scale. Spacecraft in a GEO substorm reach
    /// −20 kV; that is the number this lab calibrates the 0…1 scale against.</summary>
    private const double FullScaleVolts = 20_000.0;

    private static void Main()
    {
        Console.WriteLine("Lab 43 — THE SHARPEST POINT");
        Console.WriteLine("what a hull discharge looks like, costs, lasts and tells");
        Console.WriteLine(new string('=', 78));
        Console.WriteLine();

        WhereItStarts();
        WhatItCosts();
        HowLongItLasts();
        CanAnybodySeeIt();
        WhatTheContactorIsUpAgainst();

        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine("FINDINGS — see labs/43-the-sharpest-point/README.md");
    }

    // ══ A ═════════════════════════════════════════════════════════════════════════════════════════════
    private static void WhereItStarts()
    {
        Console.WriteLine("A — WHERE THE DISCHARGE STARTS (field = potential / radius of curvature)");
        Console.WriteLine();
        Console.WriteLine("  at 20 kV on the hull:");
        Console.WriteLine("  feature                     radius       surface field      vs the hull");

        (string Name, double Radius)[] features =
        [
            ("the hull herself", HullRadius),
            ("a hull plate edge", 0.05),
            ("a handrail", 0.02),
            ("the mast tip", 0.005),
            ("an antenna whip", 0.0005),
        ];

        double hullField = FullScaleVolts / HullRadius;
        foreach ((string name, double radius) in features)
        {
            double field = FullScaleVolts / radius;
            Console.WriteLine($"  {name,-26}  {radius,7:0.####} m   {field / 1e6,9:0.000} MV/m   " +
                              $"×{field / hullField,8:0}");
        }

        // Dry air breaks down at ~3 MV/m; a vacuum gap is far tougher, but FIELD EMISSION from a sharp metal
        // point sets in around 1e9 V/m locally, and surface flashover on a dielectric long before that.
        Console.WriteLine();
        Console.WriteLine($"  field-emission onset at a metal point ≈ 1000 MV/m");
        Console.WriteLine($"  → the whip is {(FullScaleVolts / 0.0005) / 1e6 / 1000.0 * 100:0.0}% of the way there " +
                          $"while the hull is at {(hullField / 1e6) / 1000.0 * 100:0.000}%");
        Console.WriteLine();
    }

    // ══ B ═════════════════════════════════════════════════════════════════════════════════════════════
    private static void WhatItCosts()
    {
        Console.WriteLine("B — WHAT ONE DISCHARGE IS WORTH (C = 4πε₀R, E = ½CV²)");
        Console.WriteLine();

        double c = 4 * Math.PI * Eps0 * HullRadius;
        Console.WriteLine($"  hull capacitance, R = {HullRadius:0} m: {c * 1e9:0.00} nF");
        Console.WriteLine();
        Console.WriteLine("  band       charge   potential      stored       comparison");

        foreach (double charge in new[] { 0.18, 0.2, 0.55, 0.9, 1.0 })
        {
            double v = charge * FullScaleVolts;
            double energy = 0.5 * c * v * v;
            HullCharge.Band band = HullCharge.BandOf(charge);

            string comparison = energy switch
            {
                < 0.05 => "less than a dropped coin",
                < 0.3 => "a static shock off a door handle",
                < 1.0 => "a firm static shock",
                _ => "a small firecracker",
            };

            Console.WriteLine($"  {HullCharge.BandWord(band),-9} {charge,6:0.00}   {v / 1000,6:0.0} kV   " +
                              $"{energy,7:0.000} J   {comparison}");
        }

        Console.WriteLine();
        Console.WriteLine("  (a household static shock is ~1–10 mJ; a camera flash ~10 J)");
        Console.WriteLine();
    }

    // ══ C ═════════════════════════════════════════════════════════════════════════════════════════════
    private static void HowLongItLasts()
    {
        Console.WriteLine("C — HOW LONG IT LASTS (t = Q/I, Q = CV)");
        Console.WriteLine();

        double c = 4 * Math.PI * Eps0 * HullRadius;
        double q = c * FullScaleVolts;
        Console.WriteLine($"  charge on a fully wound hull: {q * 1e6:0.0} µC");
        Console.WriteLine();
        Console.WriteLine("  emitter                       current      time to shed it");

        (string Name, double Amps)[] emitters =
        [
            ("a leaky dielectric path", 1e-6),
            ("a passive grounding strap", 1e-4),
            ("a hollow-cathode contactor", 1e-2),
            ("a hard arc to space", 1.0),
        ];

        foreach ((string name, double amps) in emitters)
        {
            double seconds = q / amps;
            string human = seconds >= 1
                ? $"{seconds,8:0.0} s"
                : seconds >= 1e-3 ? $"{seconds * 1e3,8:0.0} ms" : $"{seconds * 1e6,8:0.0} µs";
            Console.WriteLine($"  {name,-28}  {amps * 1000,7:0.###} mA   {human}");
        }

        Console.WriteLine();
        Console.WriteLine($"  the game's plate stays up for {StoryBeats.PlateSeconds:0} s");
        Console.WriteLine();
    }

    // ══ D ═════════════════════════════════════════════════════════════════════════════════════════════
    private static void CanAnybodySeeIt()
    {
        Console.WriteLine("D — CAN ANYBODY SEE HER GLOW? (the game says a charged hull is seen farther)");
        Console.WriteLine();

        double c = 4 * Math.PI * Eps0 * HullRadius;
        double energy = 0.5 * c * FullScaleVolts * FullScaleVolts;
        double arcSeconds = (c * FullScaleVolts) / 1e-2;          // through a contactor
        double opticalFraction = 0.01;                            // generous: 1% of it as visible light
        double flashWatts = energy * opticalFraction / arcSeconds;

        // What an observer is ALREADY looking at: her own reflected sunlight.
        double crossSection = Math.PI * HullRadius * HullRadius;
        double albedo = 0.2;
        double reflectedWatts = SolarConstant1Au * albedo * crossSection;

        Console.WriteLine($"  discharge, as visible light (1% of {energy:0.00} J over {arcSeconds * 1e3:0.0} ms): " +
                          $"{flashWatts:0.0} W");
        Console.WriteLine($"  her hull reflecting sunlight at 1 AU:                        {reflectedWatts / 1000:0} kW");
        Console.WriteLine($"  → the flash is {reflectedWatts / flashWatts:0} × DIMMER than simply being lit by the sun");
        Console.WriteLine();

        Console.WriteLine("  and what the game currently promises:");
        foreach (double charge in new[] { 0.0, 0.18, 0.55, 1.0 })
        {
            Console.WriteLine($"    charge {charge:0.00} → seen {HullCharge.SeenFartherFactor(charge):0.0}× farther " +
                              $"({HullCharge.BandWord(HullCharge.BandOf(charge))})");
        }
        Console.WriteLine();
    }

    // ══ E ═════════════════════════════════════════════════════════════════════════════════════════════
    private static void WhatTheContactorIsUpAgainst()
    {
        Console.WriteLine("E — WHAT THE AUTOMATIC IS UP AGAINST (ambient electron current on her skin)");
        Console.WriteLine();
        Console.WriteLine("  environment                  density      temp     arriving current");

        (string Name, double Density, double ElectronVolts)[] plasmas =
        [
            ("cold outer dark", 1e4, 10),
            ("middling space", 1e5, 100),
            ("inner-system halo", 1e6, 1_000),
            ("inside a plasma stream", 1e7, 5_000),
        ];

        double area = 4 * Math.PI * HullRadius * HullRadius;

        foreach ((string name, double density, double eV) in plasmas)
        {
            double kT = eV * ElectronCharge;
            double thermalSpeed = Math.Sqrt(kT / (2 * Math.PI * ElectronMass));
            double flux = density * thermalSpeed;                  // electrons / m² / s
            double amps = ElectronCharge * flux * area;

            Console.WriteLine($"  {name,-26}  {density,8:0.0e+0} /m³  {eV,5:0} eV   {amps * 1000,8:0.000} mA");
        }

        Console.WriteLine();
        Console.WriteLine($"  a hollow-cathode contactor emits ~10 mA (and can be pushed far past that)");
        Console.WriteLine($"  → it out-argues every one of those environments, which is why the automatic can");
        Console.WriteLine($"    hold her at {HullCharge.ContactorHoldsAt:0.00} ({HullCharge.BandWord(HullCharge.BandOf(HullCharge.ContactorHoldsAt))}) " +
                          $"for {HullCharge.ContactorPulsesPerMinute:0.0} pulses a minute");
        Console.WriteLine();
    }
}
