using System;

namespace SpaceSails.Core;

/// <summary>
/// #349 — THE HONEST CONTRACT PURSE. The owner's live pain (2026-07-18 playtest): a stranger's parcel
/// from The Tilt (Uranus) to Ringside Exchange (Saturn) paid a flat 300 cr — "ridiculously little for
/// such a long trip ... Coming to Uranus costs a LOT of fuel ... it took like 10 years from Mars."
///
/// <para>The old law (<c>300 + dest.OrbitRadius / 1e10</c>) was doubly blind: it read the destination
/// station's LOCAL orbit radius around its own planet (≈1e9 m for any berth), not the heliocentric
/// distance, so it collapsed to the 300 floor for EVERY delivery — Luna or Neptune; and it never looked
/// at where the job was TAKEN, so a cross-system crawl paid the same as a hop next door.</para>
///
/// <para>This is the pure replacement, expressed in the two heliocentric orbit radii the haul actually
/// spans (metres in, credits out). Two forces set the price, both of which the captain feels directly:
/// <list type="bullet">
///   <item><b>the GAP</b> — <c>|rTo − rFrom|</c>, the Δv proxy: a Hohmann transfer's burn grows with the
///   difference between the two orbits, so reaching OUT from Mars to Uranus (or back IN) costs fuel in
///   proportion to how far the orbits are apart.</item>
///   <item><b>the REACH</b> — <c>max(rTo, rFrom)</c>, the time/logistics proxy: the deep outer dark is a
///   long, empty coast however you enter it, so a run that ENDS at Uranus is paid for the years whether it
///   set out from Saturn or from Mars ("outer-system runs must pay like it", owner).</item>
/// </list>
/// Both terms are linear per-AU so the reward bands read plainly and are pinned by tests; a base keeps a
/// truly local hop near its old 300-ish floor. A ratio scale (rTo/rFrom) was rejected: it explodes for the
/// inner planets (Mercury→Earth would out-pay Saturn→Neptune) and reads nothing like real fuel or time.</para>
///
/// <para>The law is ORDER-FREE: <c>|gap|</c> and <c>max()</c> read the same both ways, so an inbound
/// Uranus→Saturn run pays exactly like the outbound Saturn→Uranus one — the void between them is the same
/// void. A parentless / zero radius at one end (a heliocentric station, or the Sun itself) simply
/// contributes nothing to that end, collapsing the formula toward the base for a genuinely local job.</para>
/// </summary>
public static class HaulReward
{
    /// <summary>One astronomical unit in metres — the scale outer-system distances are reckoned at.
    /// Shared with <see cref="LongHaul.AstronomicalUnitMeters"/> so both speak the same AU.</summary>
    public const double AstronomicalUnitMeters = LongHaul.AstronomicalUnitMeters;

    /// <summary>The floor every haul reward starts from — a local hop that spans almost no void still
    /// pays this, keeping short work near its familiar ~300 cr and never insulting.</summary>
    public const int BaseReward = 300;

    /// <summary>Credits per AU of orbital GAP between origin and destination — the Δv/fuel term. Tuned so
    /// a full cross-system reach (Mars 1.5 AU → Uranus 19 AU, ≈17.7 AU gap) contributes ~3,900 cr.</summary>
    public const double PerGapAuCredits = 220.0;

    /// <summary>Credits per AU of REACH (the farther of the two orbits) — the travel-time/logistics term.
    /// Tuned so ending a haul out at Uranus (19.2 AU) contributes ~3,460 cr no matter where it began.</summary>
    public const double PerReachAuCredits = 180.0;

    /// <summary>The distance PREMIUM (credits above the base) for a haul between two heliocentric orbit
    /// radii (metres). This is the whole scaling signal, separable so a fixed-fee "signature" job can add
    /// the same premium onto its own floor (<see cref="WithFloor"/>). Negative radii are clamped to zero;
    /// the result is rounded to a whole credit.</summary>
    public static int Premium(double fromRadiusMeters, double toRadiusMeters)
    {
        double fromAu = Math.Max(0.0, fromRadiusMeters) / AstronomicalUnitMeters;
        double toAu = Math.Max(0.0, toRadiusMeters) / AstronomicalUnitMeters;
        double gapAu = Math.Abs(toAu - fromAu);
        double reachAu = Math.Max(toAu, fromAu);
        double premium = PerGapAuCredits * gapAu + PerReachAuCredits * reachAu;
        return (int)Math.Round(premium, MidpointRounding.AwayFromZero);
    }

    /// <summary>The full scaled reward (credits) for a delivery/haul between two heliocentric orbit radii
    /// (metres): the base plus the distance premium. This is what a cargo run pays. Order-free and rounded
    /// to a whole credit — see the class remarks for the rationale and worked examples.</summary>
    public static int ForHaul(double fromRadiusMeters, double toRadiusMeters) =>
        BaseReward + Premium(fromRadiusMeters, toRadiusMeters);

    /// <summary>A fixed-fee "signature" job (a bounty, a favor) that carries its own authored floor, with
    /// the same haul premium added on top — so even a flat-price job grows with the distance it drags the
    /// captain across. Never pays below <paramref name="floorCredits"/>.</summary>
    public static int WithFloor(int floorCredits, double fromRadiusMeters, double toRadiusMeters) =>
        Math.Max(floorCredits, floorCredits + Premium(fromRadiusMeters, toRadiusMeters));
}
