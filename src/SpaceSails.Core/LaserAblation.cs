namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 39, THE LONG KNIFE (laser / standoff ablation). The sixth deflection technique on the owner's
/// playbook (#395), and the only one that never touches the rock: a big laser holds a bright spot on the
/// asteroid from a STANDOFF distance and boils it. The ablated jet of vaporised rock carries momentum away, so
/// the rock feels a continuous low thrust — no landing, no drilling, no thrown slug. It is the gentlest active
/// deflection after the tractor, and (like the tractor) its cost is TIME and, uniquely, POWER: it is the
/// upgrade-gated late-game variant, the one that waits on a serious power plant.
///
/// <para>Pure, deterministic Core math (repo law §9). Two honest physical limits are computed. First, the
/// STANDOFF budget: a diffraction-limited beam spreads to a spot of radius ≈ d·λ/D at range d, so the spot
/// intensity falls as 1/d² (inverse-square) and the minimum power to keep the spot above the ablation flux
/// threshold rises as d² — beyond a range set by aperture and power, you simply can't boil the rock. Second,
/// the DEFLECTION rate: the ablation jet's thrust is F = C_m·P (the standard laser-ablation momentum-coupling
/// coefficient, N per watt delivered), so the acceleration a = F/M and — run continuously over the warning
/// time — the miss grows as 1.5·a·T² (the same continuous-tow leverage as <see cref="GravityTractor"/>, lab 37,
/// reused here). The headline the lab prints: how long, and at what power, the long knife clears a 140 m rock.</para>
///
/// <para>NOT a shipped gig — this certifies the physics and prints what a late-game "standoff laser" deflection
/// variant would read: MW-class power buys years, a 100 MW upgrade buys months, and the whole technique is
/// gated on the reactor you can bring, not on getting a rig onto the rock.</para>
/// </summary>
public static class LaserAblation
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE BEAM — a big near-IR laser through a focusing aperture. Diffraction sets the spot; the spot sets
    //  the intensity; the intensity must clear the ablation threshold to boil rock at all. LAB-REPRESENTATIVE /
    //  OWNER-TUNABLE — a 10 m aperture, 1-micron laser is a serious but not fantastical late-game instrument.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The laser wavelength (m) — a 1-micron near-IR fibre/Nd laser, the workhorse band. OWNER-TUNABLE.</summary>
    public const double WavelengthMeters = 1.06e-6;

    /// <summary>The focusing aperture diameter (m) — a large space mirror. Bigger aperture ⇒ tighter spot ⇒
    /// longer reach (spot radius ∝ 1/D). OWNER-TUNABLE.</summary>
    public const double ApertureMeters = 10.0;

    /// <summary>The sustained flux (W/m²) needed to keep silicate rock ablating — the intensity floor the spot
    /// must clear. ~10 MW/m² is a representative continuous-vaporisation threshold. OWNER-TUNABLE.</summary>
    public const double AblationThresholdWattsPerM2 = 1.0e7;

    /// <summary>The momentum-coupling coefficient C_m (newtons of ablation thrust per watt delivered) — the
    /// standard laser-ablation figure (~50 μN/W for rock/metal targets). Turns delivered power straight into
    /// thrust: F = C_m·P. OWNER-TUNABLE.</summary>
    public const double MomentumCouplingNewtonsPerWatt = 5.0e-5;

    /// <summary>A reference platform power (W) — the MW-class baseline a serious ship can field. The late-game
    /// upgrade lifts this toward 100 MW. OWNER-TUNABLE.</summary>
    public const double ReferencePlatformPowerWatts = 1.0e6;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE STANDOFF BUDGET — diffraction. Spot radius ≈ d·λ/D, so spot area ∝ d² and, at fixed power, the spot
    //  intensity falls as 1/d² (inverse-square). To keep boiling you need power ∝ d²; past a range the platform
    //  simply can't reach the threshold. This is the honest limit on "how far can the long knife stand off".
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The diffraction-limited spot RADIUS (m) at standoff <paramref name="standoffMeters"/>:
    /// r ≈ d·λ/D. Pure.</summary>
    public static double SpotRadius(double standoffMeters) =>
        ApertureMeters <= 0.0 ? double.PositiveInfinity : System.Math.Max(0.0, standoffMeters) * WavelengthMeters / ApertureMeters;

    /// <summary>The spot AREA (m²) at standoff <paramref name="standoffMeters"/>: π·r². Pure.</summary>
    public static double SpotArea(double standoffMeters)
    {
        double r = SpotRadius(standoffMeters);
        return System.Math.PI * r * r;
    }

    /// <summary>The spot INTENSITY (W/m²) when <paramref name="powerWatts"/> is delivered to the spot at
    /// standoff <paramref name="standoffMeters"/>: I = P/A ∝ P/d² (inverse-square in range). Pure.</summary>
    public static double SpotIntensity(double powerWatts, double standoffMeters)
    {
        double a = SpotArea(standoffMeters);
        return a <= 0.0 ? double.PositiveInfinity : System.Math.Max(0.0, powerWatts) / a;
    }

    /// <summary>The minimum power (W) to hold the spot at the ablation threshold at standoff
    /// <paramref name="standoffMeters"/>: I_ablate·A(d) ∝ d². Below this the rock will not boil. Pure.</summary>
    public static double MinPowerToAblate(double standoffMeters) =>
        AblationThresholdWattsPerM2 * SpotArea(standoffMeters);

    /// <summary>The maximum standoff (m) at which a platform of <paramref name="powerWatts"/> can still reach
    /// the ablation threshold: solve P = I_ablate·π·(d·λ/D)² for d. Beyond it the long knife goes cold. Pure.</summary>
    public static double MaxStandoff(double powerWatts) =>
        (ApertureMeters / WavelengthMeters)
        * System.Math.Sqrt(System.Math.Max(0.0, powerWatts) / (AblationThresholdWattsPerM2 * System.Math.PI));

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE DEFLECTION RATE — thrust. The ablation jet couples momentum at C_m newtons per watt, so F = C_m·P,
    //  independent of how the power is spread (once the spot clears threshold). The rock's acceleration is F/M,
    //  and held continuously over the warning time the miss opens as 1.5·a·T² — the tractor's continuous-tow
    //  leverage (lab 37), reused. Power buys thrust; warning buys leverage.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The ablation THRUST (N) from <paramref name="powerWatts"/> of delivered laser power:
    /// F = C_m·P. Pure.</summary>
    public static double Thrust(double powerWatts) =>
        MomentumCouplingNewtonsPerWatt * System.Math.Max(0.0, powerWatts);

    /// <summary>The continuous ablation ACCELERATION (m/s²) a laser of <paramref name="powerWatts"/> imparts to
    /// a rock of <paramref name="asteroidMassKg"/>: a = F/M. Pure.</summary>
    public static double Acceleration(double powerWatts, double asteroidMassKg) =>
        asteroidMassKg <= 0.0 ? 0.0 : Thrust(powerWatts) / asteroidMassKg;

    /// <summary>The cumulative Δv (m/s) a continuous ablation burn opens over <paramref name="burnSeconds"/> of
    /// running at <paramref name="powerWatts"/> on a rock of <paramref name="asteroidMassKg"/>: a·t (the ablated
    /// mass is a negligible fraction, so M is taken constant). Pure.</summary>
    public static double CumulativeDeltaV(double powerWatts, double asteroidMassKg, double burnSeconds) =>
        Acceleration(powerWatts, asteroidMassKg) * System.Math.Max(0.0, burnSeconds);

    /// <summary>The continuous burn/warning time (s) a laser of <paramref name="powerWatts"/> needs to open a
    /// miss of <paramref name="missMeters"/> on a rock of <paramref name="type"/>/<paramref name="radiusMeters"/>,
    /// held over the whole run: solve miss = 1.5·a·T² for T (the continuous-tow leverage,
    /// <see cref="GravityTractor.RequiredLeadSeconds"/>). The headline — how long the long knife must cut. Pure.</summary>
    public static double RequiredBurnSeconds(RockType type, double radiusMeters, double powerWatts, double missMeters)
    {
        double a = Acceleration(powerWatts, KineticImpactor.AsteroidMass(type, radiusMeters));
        return GravityTractor.RequiredLeadSeconds(a, missMeters);
    }
}
