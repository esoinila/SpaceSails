namespace SpaceSails.Core;

/// <summary>
/// #395 — LAB 35, THE PUSH THAT ISN'T A PUSH (the arc-flash extension). The shipped <see cref="DeflectionGig"/>
/// already carries the ablation deflection; this is the game's canonical IMPACT-FLASH model, and it is
/// deliberately Electric-Universe (owner 2026-07-20): SpaceSails already runs EU rules (hull-charge vent #369),
/// so when the charge fires — or a slug strikes — bodies at different electric potential ARC, and the flash
/// exceeds bare kinetics. Owner's own arc-melter reference: 500 A across a 22 kV start spark (Ti on
/// water-cooled copper, in vacuum) ⇒ ~11 MW of arc power.
///
/// <para><b>Flagged non-mainstream:</b> mainstream physics attributes the impact flash to vaporisation
/// alone; this model lets the flash scale with the rock's CONDUCTIVITY, tying straight to Zubrin's type
/// table — M-type metallic is the conductor, so it arcs hardest and flashes brightest (and, in canon, takes
/// the biggest deflection kick); C/S arc weaker. It is the game's licence to be electric, computed and
/// LABELLED as such — never presented as textbook.</para>
/// </summary>
public static class ImpactArcFlash
{
    /// <summary>Owner's arc-melter reference current (amps) — the 500 A working arc.</summary>
    public const double ReferenceArcAmps = 500.0;

    /// <summary>Owner's arc-melter reference start-spark potential (volts) — the 22 kV strike.</summary>
    public const double ReferenceArcVolts = 22_000.0;

    /// <summary>The reference arc power (watts) = V·I ≈ 11 MW — the scale the flash model is pinned to.</summary>
    public const double ReferenceArcPowerWatts = ReferenceArcAmps * ReferenceArcVolts;

    /// <summary>Relative electrical conductivity CLASS by composition (dimensionless, ordinal): metal conducts,
    /// stone resists, carbonaceous is the poorest. Drives the arc strength.</summary>
    public static double ConductivityClass(RockComposition c) => c switch
    {
        RockComposition.MType => 1.0,  // nickel-iron: the conductor
        RockComposition.SType => 0.35, // silicate stone: a poor conductor
        _ => 0.15,                     // carbonaceous: poorest
    };

    /// <summary>The impact-flash brightness MULTIPLIER over a pure-kinetic (vaporisation-only) flash: 1.0 is
    /// the mainstream baseline every rock gets, plus an arc term proportional to conductivity. An M-type
    /// (conductive) flashes hardest; C/S add little. In canon the same conductivity that brightens the flash
    /// also lends the biggest arc-assisted deflection kick — logged here, not (yet) wired into the gig.</summary>
    public static double ArcFlashMultiplier(RockComposition c) => 1.0 + ArcContribution * ConductivityClass(c);

    /// <summary>How much a fully-conductive rock adds to the baseline flash (the M-type arc ceiling above the
    /// pure-kinetic 1.0). Game-canon tuning, non-mainstream. OWNER-TUNABLE.</summary>
    public const double ArcContribution = 1.5;
}
