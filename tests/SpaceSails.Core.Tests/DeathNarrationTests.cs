namespace SpaceSails.Core.Tests;

/// <summary>#380 item 1 · The place-dependent death classification and its seeded line pools. The
/// resurrection card names WHAT killed the captain (owner cruise ruling, 2026-07-19); this pins the pure
/// spine — the "joined them" trigger, the per-cause art map, and the deterministic narration.</summary>
public class DeathNarrationTests
{
    [Fact]
    public void SurfaceEnd_SteadyNerve_IsAlwaysReevers_NeverJoined()
    {
        // Above the sliver you ran the right way — no seed can make you join them.
        for (ulong seed = 0; seed < 50; seed++)
        {
            Assert.Equal(DeathCause.Reevers,
                DeathNarration.SurfaceEnd(DeathNarration.JoinedNerveSliver + 0.1, seed));
        }
    }

    [Fact]
    public void SurfaceEnd_SliverNerve_JoinsThem_OnlyOnTheSeededMinority()
    {
        // At/under the sliver, a seed divisible by the chance-in-N joins; the rest are still taken.
        Assert.Equal(DeathCause.Joined, DeathNarration.SurfaceEnd(DeathNarration.JoinedNerveSliver, seed: 0));
        Assert.Equal(DeathCause.Joined,
            DeathNarration.SurfaceEnd(2.0, seed: (ulong)DeathNarration.JoinedChanceInN));
        Assert.Equal(DeathCause.Reevers, DeathNarration.SurfaceEnd(2.0, seed: 1));
    }

    [Fact]
    public void SurfaceEnd_JoinedStaysRare_AtTheSliver()
    {
        int joined = 0;
        const int n = 300;
        for (ulong seed = 0; seed < n; seed++)
        {
            if (DeathNarration.SurfaceEnd(0.0, seed) == DeathCause.Joined)
            {
                joined++;
            }
        }

        // ~1 in JoinedChanceInN — a minority, never the default reading.
        Assert.True(joined > 0, "the eerie variant must be reachable");
        Assert.True(joined < n / 2, "joining them must stay the rare reading, not the default");
    }

    [Theory]
    [InlineData(DeathCause.Collector, "busted-freeze-frame.jpg")]
    [InlineData(DeathCause.Impact, "busted-ship-explosion.jpg")]
    [InlineData(DeathCause.Reevers, "death-reevers.jpg")]
    [InlineData(DeathCause.Joined, "death-joined.jpg")]
    [InlineData(DeathCause.Void, "death-void.jpg")]
    public void ArtFile_MapsEveryCause_ToItsImage(DeathCause cause, string expected)
    {
        Assert.Equal(expected, DeathNarration.ArtFile(cause));
    }

    [Fact]
    public void Line_IsDeterministic_ForACauseSeedAndBody()
    {
        string a = DeathNarration.Line(DeathCause.Impact, seed: 7, "Ganymede");
        string b = DeathNarration.Line(DeathCause.Impact, seed: 7, "Ganymede");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Line_FillsTheBody_ForPlaceDependentCauses()
    {
        for (ulong seed = 0; seed < 6; seed++)
        {
            Assert.Contains("Ganymede", DeathNarration.Line(DeathCause.Impact, seed, "Ganymede"));
            Assert.Contains("Callisto", DeathNarration.Line(DeathCause.Reevers, seed, "Callisto"));
            Assert.Contains("Europa", DeathNarration.Line(DeathCause.Joined, seed, "Europa"));
        }
    }

    [Fact]
    public void Line_HasNoUnfilledPlaceholders_EvenWithoutABody()
    {
        foreach (DeathCause cause in System.Enum.GetValues<DeathCause>())
        {
            for (ulong seed = 0; seed < 6; seed++)
            {
                string withBody = DeathNarration.Line(cause, seed, "Titan");
                string noBody = DeathNarration.Line(cause, seed, null);
                Assert.DoesNotContain("{body}", withBody);
                Assert.DoesNotContain("{where}", withBody);
                Assert.DoesNotContain("{body}", noBody);
                Assert.DoesNotContain("{where}", noBody);
                Assert.False(string.IsNullOrWhiteSpace(noBody));
            }
        }
    }

    [Fact]
    public void Headline_IsPresent_ForEveryCause()
    {
        foreach (DeathCause cause in System.Enum.GetValues<DeathCause>())
        {
            Assert.False(string.IsNullOrWhiteSpace(DeathNarration.Headline(cause)));
        }
    }

    [Fact]
    public void Line_VariesAcrossSeeds_SoTheReadingIsNotFixed()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        for (ulong seed = 0; seed < 12; seed++)
        {
            seen.Add(DeathNarration.Line(DeathCause.Collector, seed, null));
        }

        Assert.True(seen.Count > 1, "the pool must offer more than one reading across seeds");
    }
}
