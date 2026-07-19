using System.Collections.Generic;

namespace SpaceSails.Core.Tests;

/// <summary>
/// THE NEIGHBOURHOOD LAW (owner 2026-07-19, Sunday-morning-wind §6): missions prefer nearby places —
/// "having 10 year flights should be an exception ... not anything casual :-D". These pin the band
/// classification (from a Saturn berth: Titan is LOCAL, a Uranus berth NEIGHBOR, Mars CROSS-SYSTEM),
/// the seeded weighted pick's determinism, and the ~70/25/5 offer-mix shape the weights converge to.
/// </summary>
public class MissionRangeTests
{
    // The Sol heliocentric orbit radii (metres), inner → outer, straight off scenarios/sol.json — the
    // ranking set the law orders the planet systems by.
    private const double MercuryR = 5.791e10;
    private const double VenusR = 1.082e11;
    private const double EarthR = 1.496e11;
    private const double MarsR = 2.279e11;
    private const double JupiterR = 7.786e11;
    private const double SaturnR = 1.434e12;
    private const double UranusR = 2.872e12;
    private const double NeptuneR = 4.495e12;

    private static readonly IReadOnlyList<double> SolPlanets =
        [MercuryR, VenusR, EarthR, MarsR, JupiterR, SaturnR, UranusR, NeptuneR];

    // ── Band classification: the worked example the owner's spec names ────────────────────────────────

    [Fact]
    public void FromSaturn_Titan_IsLocal()
    {
        // Titan rides Saturn — same planet system as the Ringside Exchange berth.
        MissionBand band = MissionRange.Classify(
            "saturn", "saturn", SaturnR, SaturnR, SolPlanets);
        Assert.Equal(MissionBand.Local, band);
    }

    [Fact]
    public void FromSaturn_UranusBerth_IsNeighbor()
    {
        // The Tilt orbits Uranus — one rank out from Saturn (rank 5 → rank 6), so a neighbour hop.
        MissionBand band = MissionRange.Classify(
            "saturn", "uranus", SaturnR, UranusR, SolPlanets);
        Assert.Equal(MissionBand.Neighbor, band);
    }

    [Fact]
    public void FromSaturn_Jupiter_IsAlsoNeighbor()
    {
        // The neighbourhood reaches BOTH ways — Jupiter is one rank in from Saturn (rank 4 → rank 5).
        MissionBand band = MissionRange.Classify(
            "saturn", "jupiter", SaturnR, JupiterR, SolPlanets);
        Assert.Equal(MissionBand.Neighbor, band);
    }

    [Fact]
    public void FromSaturn_Mars_IsCrossSystem()
    {
        // Mars (rank 3) is three ranks in from Saturn (rank 5) — the rare cross-system saga.
        MissionBand band = MissionRange.Classify(
            "saturn", "mars", SaturnR, MarsR, SolPlanets);
        Assert.Equal(MissionBand.CrossSystem, band);
    }

    [Fact]
    public void FromSaturn_Neptune_IsCrossSystem()
    {
        // Neptune (rank 7) is two ranks out — past the adjacent Uranus, so still a saga.
        MissionBand band = MissionRange.Classify(
            "saturn", "neptune", SaturnR, NeptuneR, SolPlanets);
        Assert.Equal(MissionBand.CrossSystem, band);
    }

    [Fact]
    public void RadiusRank_OrdersSolBySunwardCount()
    {
        Assert.Equal(0, MissionRange.RadiusRank(MercuryR, SolPlanets));
        Assert.Equal(3, MissionRange.RadiusRank(MarsR, SolPlanets));
        Assert.Equal(5, MissionRange.RadiusRank(SaturnR, SolPlanets));
        Assert.Equal(6, MissionRange.RadiusRank(UranusR, SolPlanets));
        Assert.Equal(7, MissionRange.RadiusRank(NeptuneR, SolPlanets));
    }

    // ── Weighted pick: determinism ────────────────────────────────────────────────────────────────────

    [Fact]
    public void PickIndex_IsDeterministic_SameSeedSameIndex()
    {
        MissionBand[] bands = [MissionBand.Local, MissionBand.Neighbor, MissionBand.CrossSystem];
        for (ulong seed = 1; seed < 500; seed++)
        {
            Assert.Equal(MissionRange.PickIndex(seed, bands), MissionRange.PickIndex(seed, bands));
        }
    }

    [Fact]
    public void PickIndex_SingleCandidate_AlwaysZero()
    {
        MissionBand[] bands = [MissionBand.CrossSystem];
        Assert.Equal(0, MissionRange.PickIndex(12345, bands));
        Assert.Equal(0, MissionRange.PickIndex(0, []));
    }

    // ── Weighted pick: the ~70/25/5 mix shape over a sampled distribution ───────────────────────────────

    [Fact]
    public void PickIndex_MixConvergesToSeventyTwentyfiveFive()
    {
        // A candidate set with all three bands populated (and MORE than one local, to prove the band
        // SHARE is what's weighted, not the raw candidate count). Indices: 0-2 local, 3-4 neighbour, 5 cross.
        MissionBand[] bands =
        [
            MissionBand.Local, MissionBand.Local, MissionBand.Local,
            MissionBand.Neighbor, MissionBand.Neighbor,
            MissionBand.CrossSystem,
        ];

        int local = 0, neighbor = 0, cross = 0;
        const int samples = 40000;
        for (ulong s = 0; s < samples; s++)
        {
            // Fold each sample through the shared DiceRule seed, as the live booth does.
            ulong seed = DiceRule.Seed("mix-test", (long)s);
            switch (bands[MissionRange.PickIndex(seed, bands)])
            {
                case MissionBand.Local: local++; break;
                case MissionBand.Neighbor: neighbor++; break;
                default: cross++; break;
            }
        }

        double localFrac = (double)local / samples;
        double neighborFrac = (double)neighbor / samples;
        double crossFrac = (double)cross / samples;

        // Within 2 percentage points of the tuned shape — heavily local, a slice neighbour, a sliver saga.
        Assert.InRange(localFrac, MissionRange.LocalWeight - 0.02, MissionRange.LocalWeight + 0.02);
        Assert.InRange(neighborFrac, MissionRange.NeighborWeight - 0.02, MissionRange.NeighborWeight + 0.02);
        Assert.InRange(crossFrac, MissionRange.CrossSystemWeight - 0.02, MissionRange.CrossSystemWeight + 0.02);

        // The saga really is the exception the owner asked for: local dwarfs cross-system by an order+.
        Assert.True(local > cross * 10, $"local {local} should dwarf cross {cross}");
    }

    [Fact]
    public void PickIndex_EmptyBandRedistributes_NoOffersLost()
    {
        // Only local + neighbour candidates on the table — the missing cross-system 5% must redistribute
        // across the present bands, not vanish (a berth with no far work still fills every offer).
        MissionBand[] bands = [MissionBand.Local, MissionBand.Neighbor];

        int local = 0, neighbor = 0;
        const int samples = 40000;
        for (ulong s = 0; s < samples; s++)
        {
            ulong seed = DiceRule.Seed("redist-test", (long)s);
            if (bands[MissionRange.PickIndex(seed, bands)] == MissionBand.Local) local++;
            else neighbor++;
        }

        // Every draw lands somewhere.
        Assert.Equal(samples, local + neighbor);

        // The shares renormalize over the present two bands: 0.70/0.95 and 0.25/0.95.
        (double sl, double sn, double sc) = MissionRange.BandShares(bands);
        Assert.Equal(0.0, sc);
        Assert.InRange((double)local / samples, sl - 0.02, sl + 0.02);
        Assert.InRange((double)neighbor / samples, sn - 0.02, sn + 0.02);
    }

    [Fact]
    public void BandShares_AllBandsPresent_SumToTunedWeights()
    {
        MissionBand[] bands = [MissionBand.Local, MissionBand.Neighbor, MissionBand.CrossSystem];
        (double l, double n, double c) = MissionRange.BandShares(bands);
        Assert.Equal(MissionRange.LocalWeight, l, 6);
        Assert.Equal(MissionRange.NeighborWeight, n, 6);
        Assert.Equal(MissionRange.CrossSystemWeight, c, 6);
        Assert.Equal(1.0, l + n + c, 6);
    }
}
