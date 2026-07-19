using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>Which neighbourhood band a candidate destination falls into, relative to where the
/// captain is standing when the offer is made. The offer mix leans hard on <see cref="Local"/>.</summary>
public enum MissionBand
{
    /// <summary>Same planet system as the berth — its own moons and stations (Saturn → Titan).
    /// A short hop the shuttle or a quick insertion can cross; the everyday work.</summary>
    Local,

    /// <summary>A neighbouring planet system, adjacent by heliocentric orbit radius (Saturn → Uranus,
    /// or Saturn → Jupiter). A real trip, but the next well over — the occasional stretch.</summary>
    Neighbor,

    /// <summary>Everything farther — two or more planet-systems away (Saturn → Mars). The rare saga,
    /// the "10 year flight" the owner wants kept exceptional, priced like it by #357's HaulReward.</summary>
    CrossSystem,
}

/// <summary>
/// THE NEIGHBOURHOOD LAW (owner ruling 2026-07-19, Sunday-morning-wind §6): "We should adjust the
/// missions to prefer staying in relatively nearby places. Having 10 year flights should be an
/// exception in mid mission, not anything casual :-D".
///
/// <para>A pure, deterministic destination-weighting law layered on top of the offer generators
/// (<c>MakeCargoRunOffer</c>, <c>MakeHuntOffer</c>, <c>MakeFavorDeliveryOffer</c>, <c>MakeFetchOffer</c>).
/// Those generators used to roll a flat index over every candidate — treating Luna and Neptune alike,
/// so a Saturn barfly was as likely to send you a decade across the system as to the moon next door.
/// This classifies each candidate into a <see cref="MissionBand"/> and rolls a seeded WEIGHTED pick,
/// so the offer mix is heavily local and the cross-system saga is the exception it should be.</para>
///
/// <para>It does NOT touch the purse. #357's <see cref="HaulReward"/> already prices the rare long
/// haul like the exception it is (pay-scale and offer-frequency are the two halves of one law: long =
/// rare + rich). This law is only the FREQUENCY half — it decides how often a destination is even
/// offered, never what it pays.</para>
///
/// <para>Classification is by ORBITAL-RADIUS ADJACENCY, not absolute distance, so it holds in any
/// scenario: a candidate in the same planet system as the berth is <see cref="MissionBand.Local"/>;
/// one in a planet system within <see cref="NeighborRankSpan"/> ranks (by heliocentric orbit radius)
/// is <see cref="MissionBand.Neighbor"/>; anything farther is <see cref="MissionBand.CrossSystem"/>.
/// The band WEIGHTS below are the owner's tuning dials — exposed as constants and pinned by tests.</para>
///
/// <para>Determinism is law in Core: the pick is a pure function of the seed the caller folds from sim
/// state (never <see cref="System.Random"/> or the clock), so the same booth surfaces the same offer
/// mix, and client and any replay agree on every roll.</para>
/// </summary>
public static class MissionRange
{
    /// <summary>OWNER TUNING — the share of offers that should land in the berth's OWN planet system
    /// (its moons/stations). The everyday work; kept dominant so most jobs are a short hop.</summary>
    public const double LocalWeight = 0.70;

    /// <summary>OWNER TUNING — the share that should reach a NEIGHBOURING planet system (one well over
    /// by orbit radius). The occasional real trip.</summary>
    public const double NeighborWeight = 0.25;

    /// <summary>OWNER TUNING — the share that should be a CROSS-SYSTEM saga (two-plus systems away).
    /// Deliberately small: the "10 year flight" is an exception, not a casual offer.</summary>
    public const double CrossSystemWeight = 0.05;

    /// <summary>How many ranks apart (by heliocentric orbit radius) two planet systems may sit and
    /// still count as neighbours. 1 = strictly adjacent (Saturn ↔ Jupiter, Saturn ↔ Uranus). Raise it
    /// to widen the "neighbourhood" the everyday work can reach.</summary>
    public const int NeighborRankSpan = 1;

    /// <summary>The tuning weight for a band — the target share of the offer mix it should claim when
    /// candidates from every band are on the table. Empty bands redistribute their mass to the present
    /// ones (see <see cref="PickIndex"/>), so a berth with no cross-system candidate loses no offers.</summary>
    public static double Weight(MissionBand band) => band switch
    {
        MissionBand.Local => LocalWeight,
        MissionBand.Neighbor => NeighborWeight,
        _ => CrossSystemWeight,
    };

    /// <summary>
    /// Classify a candidate destination relative to the berth. <paramref name="originSystemId"/> and
    /// <paramref name="candidateSystemId"/> are the PLANET-LEVEL ancestors' ids (the sun-orbiting planet
    /// each rides around) — equal ids mean same system, i.e. <see cref="MissionBand.Local"/>. Otherwise
    /// the two systems are ranked by heliocentric orbit radius against
    /// <paramref name="planetHelioRadiiMeters"/> (every planet's orbit radius in the scenario); within
    /// <paramref name="neighborRankSpan"/> ranks is <see cref="MissionBand.Neighbor"/>, farther is
    /// <see cref="MissionBand.CrossSystem"/>. Pure — no clock, no state.
    /// </summary>
    public static MissionBand Classify(
        string originSystemId,
        string candidateSystemId,
        double originHelioRadiusMeters,
        double candidateHelioRadiusMeters,
        IReadOnlyList<double> planetHelioRadiiMeters,
        int neighborRankSpan = NeighborRankSpan)
    {
        if (string.Equals(originSystemId, candidateSystemId, StringComparison.Ordinal))
        {
            return MissionBand.Local;
        }

        int rankGap = Math.Abs(
            RadiusRank(originHelioRadiusMeters, planetHelioRadiiMeters)
            - RadiusRank(candidateHelioRadiusMeters, planetHelioRadiiMeters));
        return rankGap <= neighborRankSpan ? MissionBand.Neighbor : MissionBand.CrossSystem;
    }

    /// <summary>The orbital RANK of a heliocentric radius: how many planet systems orbit strictly
    /// inside it (0 = innermost). So Mercury ranks 0, Saturn 5, Uranus 6 in the Sol set — and Saturn
    /// and Uranus are one rank apart, hence neighbours. A radius that is not itself a planet (a
    /// heliocentric station drifting between two planets) still ranks cleanly by where it sits.</summary>
    public static int RadiusRank(double radiusMeters, IReadOnlyList<double> planetHelioRadiiMeters)
    {
        int rank = 0;
        foreach (double pr in planetHelioRadiiMeters)
        {
            if (pr < radiusMeters)
            {
                rank++;
            }
        }

        return rank;
    }

    /// <summary>
    /// Roll a WEIGHTED index into a candidate list whose i-th element sits in band <c>bands[i]</c>.
    /// Each band's total probability is proportional to its <see cref="Weight"/>, split evenly across
    /// the candidates in it — so adding three more local havens never dilutes the local SHARE, it only
    /// spreads the local mass across them. Bands with no candidates contribute nothing, so their mass
    /// redistributes across the present bands (a berth with only local + neighbour work still fills
    /// every offer). Deterministic in <paramref name="seed"/> — the one determinism law.
    /// </summary>
    public static int PickIndex(ulong seed, IReadOnlyList<MissionBand> bands)
    {
        if (bands.Count == 0)
        {
            return 0;
        }

        // Per-band candidate counts, so each band's mass is shared evenly within it.
        int local = 0, neighbor = 0, cross = 0;
        foreach (MissionBand b in bands)
        {
            switch (b)
            {
                case MissionBand.Local: local++; break;
                case MissionBand.Neighbor: neighbor++; break;
                default: cross++; break;
            }
        }

        double total = 0.0;
        double[] weights = new double[bands.Count];
        for (int i = 0; i < bands.Count; i++)
        {
            int count = bands[i] switch
            {
                MissionBand.Local => local,
                MissionBand.Neighbor => neighbor,
                _ => cross,
            };
            weights[i] = count > 0 ? Weight(bands[i]) / count : 0.0;
            total += weights[i];
        }

        if (total <= 0.0)
        {
            return 0; // every weight zero (shouldn't happen) — fall back to the first candidate.
        }

        double roll = new DeterministicRandom(seed).NextDouble() * total;
        double cumulative = 0.0;
        for (int i = 0; i < bands.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
            {
                return i;
            }
        }

        return bands.Count - 1; // floating-point tail — the last candidate carries it.
    }

    /// <summary>The renormalized share each band actually claims for a given candidate mix — the offer
    /// distribution the weighted pick converges to. Present bands split the whole (empty bands drop out
    /// and their mass redistributes); the returned shares sum to 1 (or all-zero for an empty mix). For
    /// tuning read-outs and the distribution tests — the live pick uses <see cref="PickIndex"/>.</summary>
    public static (double Local, double Neighbor, double CrossSystem) BandShares(IReadOnlyList<MissionBand> bands)
    {
        bool hasLocal = false, hasNeighbor = false, hasCross = false;
        foreach (MissionBand b in bands)
        {
            switch (b)
            {
                case MissionBand.Local: hasLocal = true; break;
                case MissionBand.Neighbor: hasNeighbor = true; break;
                default: hasCross = true; break;
            }
        }

        double l = hasLocal ? LocalWeight : 0.0;
        double n = hasNeighbor ? NeighborWeight : 0.0;
        double c = hasCross ? CrossSystemWeight : 0.0;
        double total = l + n + c;
        return total <= 0.0 ? (0.0, 0.0, 0.0) : (l / total, n / total, c / total);
    }
}
