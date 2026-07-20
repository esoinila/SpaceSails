namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 36, THE CANNONBALL. The kinetic-impactor deflection, honestly costed. Where the shipped
/// <see cref="DeflectionGig"/> ablates a rock with a drilled charge (a periapsis raise measured at the
/// impact instant), the cannonball is the OTHER classic technique: hurl a mass at the rock and let raw
/// momentum — amplified by the ejecta it blows off — nudge its orbit, then let the WARNING TIME do the
/// rest. This is pure, deterministic Core math (repo law §9): asteroid mass from Zubrin type + size, the
/// momentum transfer Δv = β·m·u/M, the ejecta enhancement β (DART/Dimorphos measured β≈3.6 on a real
/// S-type — the ejecta plume added ~2.6× beyond bare momentum), and the along-track LEVERAGE that turns a
/// millimetre-per-second nudge into a Ringside-clearing miss when you have years of lead.
///
/// <para>NOT a shipped gig — the labs are the menu the owner picks from. This certifies the physics and
/// prints the numbers a future "sacrifice a cargo pod / an old hull as the slug" gig variant would read:
/// how much WARNING plus how much MASS deflects a given rock past the Ringside Exchange.</para>
///
/// <para>Honesty note (owner 2026-07-20, reconciling the DART reality with the #394 type table): a loose,
/// volatile-rich body can OVER-deliver, not under — the looser surface throws a bigger ejecta plume, so
/// β climbs. C-type carbonaceous ⇒ biggest β; M-type metallic (dense, little ejecta, near-inelastic) ⇒
/// smallest. The old "rubble absorbs the push" intuition is backwards; the lab says so.</para>
/// </summary>
public static class KineticImpactor
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE TARGET — mass from Zubrin composition (bulk density) and radius. The game's deflection rock
    //  is a visibility abstraction (4000 km radius, so the shuttle board can land on it); the honest
    //  kinetic physics is computed at REAL city-killer sizes that could actually threaten a station.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bulk density (kg/m³) by composition — carbonaceous porous-and-light, stony the firm
    /// middle, metallic dense. Bulk (not grain) figures: the porosity is part of why C throws more ejecta.</summary>
    public static double BulkDensity(RockComposition c) => c switch
    {
        RockComposition.CType => 1400.0, // carbonaceous, porous
        RockComposition.MType => 5300.0, // nickel-iron, dense
        _ => 2700.0,                     // S-type stony
    };

    /// <summary>Asteroid mass (kg) for a spherical rock of <paramref name="radiusMeters"/> and the given
    /// type: ρ·(4/3)πr³. Pure.</summary>
    public static double AsteroidMass(RockType type, double radiusMeters) =>
        BulkDensity(type.Composition) * (4.0 / 3.0) * System.Math.PI * radiusMeters * radiusMeters * radiusMeters;

    /// <summary>Representative REAL asteroid radii (m) a Ringside-threatening rock might carry — from a
    /// 50 m tunguska-class up to a 1 km harbour-killer. (The gig's on-map rock is 4000 km for the camera;
    /// these are the sizes the deflection physics is honest at.)</summary>
    public static readonly double[] RealisticRadiiMeters = [50.0, 140.0, 370.0, 1000.0];

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE MOMENTUM TRANSFER — Δv = β·(m·u)/M. β is the momentum ENHANCEMENT factor: 1.0 would be bare
    //  inelastic capture, but the impact blows off ejecta whose recoil adds thrust, so β>1. DART measured
    //  β≈3.6 on Dimorphos (a real S-type) — the plume did most of the work.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>DART's measured impact velocity (m/s) — the real intercept the β figure is anchored to.</summary>
    public const double DartImpactVelocity = 6100.0;

    /// <summary>A representative in-system intercept closing speed (m/s) for a Ringside-inbound rock —
    /// DART-scale. OWNER-TUNABLE / lab-representative.</summary>
    public const double ReferenceClosingSpeed = 6000.0;

    /// <summary>The ejecta momentum-ENHANCEMENT factor β by composition (owner's DART reconciliation): a
    /// loose C-type throws the biggest plume (β highest); a dense M-type barely spalls (β lowest). The
    /// S-type value is DART/Dimorphos measured (β≈3.6). β=1 would be no ejecta at all.</summary>
    public static double Beta(RockComposition c) => c switch
    {
        RockComposition.CType => 4.5, // volatile-rich, biggest ejecta plume — OVER-delivers
        RockComposition.MType => 1.5, // dense metal, little ejecta, near-inelastic
        _ => 3.6,                     // S-type — DART/Dimorphos measured
    };

    /// <summary>The velocity change (m/s) an impactor of <paramref name="impactorMassKg"/> striking at
    /// <paramref name="closingSpeed"/> imparts to a rock of <paramref name="asteroidMassKg"/>, enhanced by
    /// <paramref name="beta"/>: Δv = β·m·u/M. Pure.</summary>
    public static double DeltaV(double impactorMassKg, double closingSpeed, double asteroidMassKg, double beta) =>
        asteroidMassKg <= 0.0 ? 0.0 : beta * impactorMassKg * closingSpeed / asteroidMassKg;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE LEVERAGE — a kinetic nudge is applied ALONG-TRACK (the efficient direction), and an along-track
    //  Δv changes the orbital period, so the miss GROWS with the warning time: miss ≈ 3·Δv·t_lead (the
    //  standard along-track secular factor — a period change accumulates 3× the raw displacement). This is
    //  the whole reason early detection pays, and why the cannonball rewards WARNING over brute mass.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The along-track secular leverage factor: an along-track Δv accumulates a downrange miss of
    /// 3·Δv·t (a change in period drifts 3× the bare displacement). Pinned by tests.</summary>
    public const double AlongTrackLeverage = 3.0;

    /// <summary>The miss distance (m) an along-track <paramref name="deltaV"/> (m/s) opens up over
    /// <paramref name="leadSeconds"/> of warning: <see cref="AlongTrackLeverage"/>·Δv·t. Pure.</summary>
    public static double AlongTrackMiss(double deltaV, double leadSeconds) =>
        AlongTrackLeverage * deltaV * System.Math.Max(0.0, leadSeconds);

    /// <summary>The Δv (m/s) needed to open a miss of <paramref name="missMeters"/> given
    /// <paramref name="leadSeconds"/> of warning — the inverse of <see cref="AlongTrackMiss"/>.</summary>
    public static double RequiredDeltaV(double missMeters, double leadSeconds) =>
        leadSeconds <= 0.0 ? double.PositiveInfinity : missMeters / (AlongTrackLeverage * leadSeconds);

    /// <summary>The impactor mass (kg) needed to deflect a rock of <paramref name="type"/> and
    /// <paramref name="radiusMeters"/> by <paramref name="missMeters"/>, given <paramref name="leadSeconds"/>
    /// of warning and a <paramref name="closingSpeed"/> intercept: m = Δv_req·M/(β·u). Pure.</summary>
    public static double RequiredImpactorMass(
        RockType type, double radiusMeters, double missMeters, double leadSeconds, double closingSpeed)
    {
        double m = AsteroidMass(type, radiusMeters);
        double dvReq = RequiredDeltaV(missMeters, leadSeconds);
        double beta = Beta(type.Composition);
        return dvReq * m / (beta * closingSpeed);
    }

    /// <summary>The warning time (s) an impactor of <paramref name="impactorMassKg"/> needs to deflect a
    /// rock of <paramref name="type"/>/<paramref name="radiusMeters"/> by <paramref name="missMeters"/> at
    /// <paramref name="closingSpeed"/>: solve miss = 3·Δv·t for t. The headline the "sacrifice a pod" gig
    /// would quote — how early you must throw a given slug. Pure.</summary>
    public static double RequiredLeadSeconds(
        RockType type, double radiusMeters, double impactorMassKg, double missMeters, double closingSpeed)
    {
        double dv = DeltaV(impactorMassKg, closingSpeed, AsteroidMass(type, radiusMeters), Beta(type.Composition));
        return dv <= 0.0 ? double.PositiveInfinity : missMeters / (AlongTrackLeverage * dv);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE SACRIFICE — game-plausible slugs the crew could throw. A cargo pod is cheap and light; an old
    //  hull is the heavy hammer. These are the masses a future gig variant would offer as the ammunition.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A jettisoned cargo pod as an impactor (kg) — the cheap, light slug (~20 t).</summary>
    public const double CargoPodMassKg = 2.0e4;

    /// <summary>A decommissioned hull flown into the rock (kg) — the heavy hammer (~200 t).</summary>
    public const double OldHullMassKg = 2.0e5;
}
