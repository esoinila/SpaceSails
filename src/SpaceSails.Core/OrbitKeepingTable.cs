namespace SpaceSails.Core;

/// <summary>
/// The per-body station-keeping table — the machine-readable artifact Lab 25 "The tide that takes
/// it back" measured and this game consumes (Friday §0). Each row is a moon's trim BUDGET, flown
/// honestly in the real N-body sim: the Δv/day the parent's tide forces on a held 0.33-Hill park and
/// how many trims a day the tolerance triggers, at <see cref="OrbitKeeping.TrimCadenceFraction"/>
/// cadence and <see cref="OrbitKeeping.TrimEccentricity"/> tolerance. The pulses/day the arm-time
/// contract quotes is derived from these at the parked ship's world speed by
/// <see cref="OrbitKeeping.TrimPulsesPerDay"/>.
///
/// <para><b>GENERATED — do not hand-edit.</b> The rows below are pasted verbatim from the Section C
/// printout of <c>dotnet run --project labs/25-the-tide-that-takes-it-back -c Release</c>
/// (2026-07-17 run). If the physics or the keeping constants change, rerun the lab and re-paste —
/// never edit a number by hand (repo law: every number came from a probe that printed it).</para>
///
/// <para>The lab's headline finding, kept in view: Enceladus's Hill sphere is only 3.8 body radii,
/// so its 0.33-Hill park sits at 1.24 R — there is NO ballistically-stable circular park there; the
/// tide forces e≈0.3 and crashes an unkept orbit within half a day (the owner's stranded ship,
/// #180). Keeping HOLDS it (27 p/day) but Enceladus is genuinely the expensive park; Luna (2 p/day)
/// and Titan (3 p/day) are cheap. The quote tells the captain the truth at arm time.</para>
/// </summary>
public static class OrbitKeepingTable
{
    /// <summary>Measured keeping profiles by body id (Lab 25 Section C, 2026-07-17).</summary>
    public static readonly IReadOnlyDictionary<string, OrbitKeeping.KeepProfile> ByBody =
        new Dictionary<string, OrbitKeeping.KeepProfile>
        {
            ["enceladus"] = new("enceladus", 0.33, 541.6471584806166, 26.546964494357702),
            ["luna"] = new("luna", 0.33, 29.818009522951677, 1.3257574051641803),
            ["titan"] = new("titan", 0.33, 77.13127658990058, 2.2827870216160395),
        };

    /// <summary>The keeping profile for a body — the measured row if Lab 25 flew it, otherwise the
    /// tide-acceleration estimate (<see cref="OrbitKeeping.EstimateProfile"/>) so the arm-time quote
    /// is never silent for a moon the lab never measured. Needs the body's Hill radius, its parent's
    /// gravitational parameter, and its distance from that parent.</summary>
    public static OrbitKeeping.KeepProfile For(CelestialBody body, double hill, double parentMu, double parentDistance) =>
        ByBody.TryGetValue(body.Id, out OrbitKeeping.KeepProfile p)
            ? p
            : OrbitKeeping.EstimateProfile(body, hill, parentMu, parentDistance);

    /// <summary>The trim budget in mass pulses per day for a parked ship, priced at its world
    /// (heliocentric) speed — the number the arm-time contract quotes ("trim ≈N p/day"). Measured
    /// where Lab 25 flew the body; estimated otherwise.</summary>
    public static int TrimPulsesPerDay(CelestialBody body, double hill, double parentMu, double parentDistance, double worldSpeed) =>
        OrbitKeeping.TrimPulsesPerDay(For(body, hill, parentMu, parentDistance), worldSpeed);
}
