namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 38, THE ROCK THAT MINES ITSELF (mass driver ON the rock). The fourth deflection technique on
/// the owner's playbook (#395): instead of ramming the rock (<see cref="KineticImpactor"/>, lab 36), towing it
/// (<see cref="GravityTractor"/>, lab 37) or drilling a charge (<see cref="DeflectionGig"/>, lab 35), you LAND
/// a mass-driver rig and throw the asteroid's OWN mass overboard as reaction propellant. The rock becomes both
/// the ship and the fuel — the Luna mass-driver canon (<see cref="MassDriverSchedule"/>, lab 30) turned on the
/// threat itself: the away-mission rig hauls up a bucket-thrower and a reactor, and every tonne of regolith it
/// flings off the far side kicks the rest a little farther from Ringside.
///
/// <para>Pure, deterministic Core math (repo law §9). The heart is the ROCKET EQUATION with the rock as its own
/// tankage — Δv = v_ex·ln(M0/M_final) — so the mass you must fling to give the REMAINING rock a required Δv is
/// M0·(1 − e^(−Δv/v_ex)). The required Δv is set the same along-track way the cannonball's is (miss = 3·Δv·t,
/// reusing <see cref="KineticImpactor.RequiredDeltaV"/>): the rig's net shove is delivered roughly along-track
/// and the WARNING TIME multiplies it. The honest catch the lab prints: the FRACTION flung is tiny (hundredths
/// of a percent), but the ABSOLUTE mass is kilotonnes-to-megatonnes, and at any believable rig throughput that
/// is days-to-years of continuous throwing — a slow, power-hungry, multi-visit engineering gig, not an
/// emergency tool. It rewards early warning exactly as the tractor does.</para>
///
/// <para>NOT a shipped gig — this certifies the physics and prints what a "land a driver and eat the rock"
/// engineering-contract variant would read: how much of the rock you fling, how long the reactor runs, and why
/// big rocks on short warning are simply out of its reach.</para>
/// </summary>
public static class RockMassDriver
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE RIG — a bucket-thrower and its reactor, landed on the rock. The exhaust velocity is the speed it
    //  flings the rock's own regolith; the throughput is how fast it can feed the driver. Both LAB-REPRESENTATIVE
    //  / OWNER-TUNABLE — a rock rig is a more modest cousin of Luna's 3.2 km/s pod driver (lab 30).
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The driver muzzle speed (m/s) — the velocity the rig flings the rock's own mass off the far
    /// side, i.e. the effective exhaust velocity of the rocket equation. A modest rock rig; Luna's compute-pod
    /// driver (lab 30) throws at ~3.2 km/s. OWNER-TUNABLE.</summary>
    public const double ExhaustVelocityMetersPerSecond = 2500.0;

    /// <summary>A reference rig throughput (kg/s) — how much regolith the driver flings per second at full feed.
    /// ~20 kg/s is a serious self-mining rig (a bucket every fraction of a second). OWNER-TUNABLE.</summary>
    public const double ReferenceThroughputKgPerSecond = 20.0;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE ROCKET EQUATION — with the rock as both ship and fuel. Δv = v_ex·ln(M0/M_final). To give the
    //  remaining rock a Δv you must throw off the fraction f = 1 − e^(−Δv/v_ex) of its mass. Because the Δv a
    //  deflection needs is tiny (mm/s over years of lead), f is tiny too — but M0 is enormous.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The velocity change (m/s) the REMAINING rock gains when a rig of exhaust velocity
    /// <paramref name="exhaustVelocity"/> throws a rock of <paramref name="initialMassKg"/> down to
    /// <paramref name="finalMassKg"/>: the Tsiolkovsky rocket equation Δv = v_ex·ln(M0/M_final). Pure.</summary>
    public static double DeltaV(double exhaustVelocity, double initialMassKg, double finalMassKg) =>
        finalMassKg <= 0.0 || initialMassKg <= 0.0
            ? 0.0
            : exhaustVelocity * System.Math.Log(initialMassKg / finalMassKg);

    /// <summary>The mass FRACTION of the rock that must be flung to open a burn of <paramref name="deltaV"/> at
    /// exhaust velocity <paramref name="exhaustVelocity"/>: f = 1 − e^(−Δv/v_ex). Pure. Clamped non-negative.</summary>
    public static double MassFractionFlung(double deltaV, double exhaustVelocity) =>
        exhaustVelocity <= 0.0 ? 0.0 : 1.0 - System.Math.Exp(-System.Math.Max(0.0, deltaV) / exhaustVelocity);

    /// <summary>The mass (kg) the rig must fling off a rock of <paramref name="initialMassKg"/> to give the
    /// remaining rock a burn of <paramref name="deltaV"/> at <paramref name="exhaustVelocity"/>:
    /// M0·<see cref="MassFractionFlung"/>. Pure.</summary>
    public static double MassToFling(double initialMassKg, double deltaV, double exhaustVelocity) =>
        initialMassKg * MassFractionFlung(deltaV, exhaustVelocity);

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE ENGINEERING COST — the fling is not free. It takes TIME (throughput) and POWER (the kinetic energy
    //  poured into every tonne thrown). These are what make it a multi-visit gig with a reactor, not a punch.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The continuous run-time (s) to fling <paramref name="massKg"/> at
    /// <paramref name="throughputKgPerSecond"/>: m / throughput. Pure.</summary>
    public static double RunSeconds(double massKg, double throughputKgPerSecond) =>
        throughputKgPerSecond <= 0.0 ? double.PositiveInfinity : System.Math.Max(0.0, massKg) / throughputKgPerSecond;

    /// <summary>The kinetic power (W) the rig's reactor must pour into the driver to fling regolith at
    /// <paramref name="exhaustVelocity"/> at a rate of <paramref name="throughputKgPerSecond"/>:
    /// P = ṁ·½·v_ex² (the jet's kinetic-energy rate). This is the reactor bill — tens of MW. Pure.</summary>
    public static double DriverPowerWatts(double throughputKgPerSecond, double exhaustVelocity) =>
        0.5 * System.Math.Max(0.0, throughputKgPerSecond) * exhaustVelocity * exhaustVelocity;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  THE HEADLINE — how much of a given rock you fling, and how long the rig runs, to clear Ringside. The
    //  Δv is set along-track (miss = 3·Δv·t, the cannonball's leverage) so warning time is everything: the
    //  same rig that clears a rock with years of lead can't touch it with weeks.
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The mass (kg) the rig must fling off a rock of <paramref name="type"/>/<paramref name="radiusMeters"/>
    /// to open a miss of <paramref name="missMeters"/> given <paramref name="leadSeconds"/> of warning, at
    /// <paramref name="exhaustVelocity"/>. The Δv is the along-track requirement (miss = 3·Δv·t,
    /// <see cref="KineticImpactor.RequiredDeltaV"/>); the mass follows from the rocket equation. Pure.</summary>
    public static double MassToDeflect(
        RockType type, double radiusMeters, double missMeters, double leadSeconds, double exhaustVelocity)
    {
        double m0 = KineticImpactor.AsteroidMass(type, radiusMeters);
        double dv = KineticImpactor.RequiredDeltaV(missMeters, leadSeconds);
        return MassToFling(m0, dv, exhaustVelocity);
    }

    /// <summary>The continuous run-time (s) to deflect a rock of <paramref name="type"/>/<paramref name="radiusMeters"/>
    /// past a miss of <paramref name="missMeters"/> with <paramref name="leadSeconds"/> of warning, at
    /// <paramref name="throughputKgPerSecond"/> and <paramref name="exhaustVelocity"/>. If this exceeds the
    /// lead time itself, the rig cannot finish before impact — the honest negative the lab prints. Pure.</summary>
    public static double RunSecondsToDeflect(
        RockType type, double radiusMeters, double missMeters, double leadSeconds,
        double throughputKgPerSecond, double exhaustVelocity)
    {
        double mass = MassToDeflect(type, radiusMeters, missMeters, leadSeconds, exhaustVelocity);
        return RunSeconds(mass, throughputKgPerSecond);
    }
}
