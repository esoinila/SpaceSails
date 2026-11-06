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

    // ── #480 follow-up: the caption must name the meter that actually ran out. ──

    [Fact]
    public void AMaulingDoesNotGetBlamedOnTheNerve()
    {
        // The five blows landed on a captain whose hands were fine. Before #469 was fixed nerve was
        // effectively the only way to die out there, so the card hardcoded the nerve line — and kept
        // printing it after the condition marker started deciding.
        string mauled = DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: false);

        Assert.DoesNotContain("nerve", mauled, System.StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(mauled));
    }

    [Fact]
    public void AnOverdrawStillNamesTheNerve()
    {
        string overdrawn = DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: true);

        Assert.Contains("nerve", overdrawn, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheTwoSurfaceDeathsReadDifferently()
    {
        Assert.NotEqual(
            DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: true),
            DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JoinedKeepsItsOwnLine_WhicheverMeterRanOut(bool nerveRanOut)
    {
        // Walking into the crowd is its own story and outranks the meter that emptied.
        Assert.Contains("footprints", DeathNarration.SurfaceCaption(DeathCause.Joined, nerveRanOut));
    }

    /// <summary>#1401 R1-F25 · "the chest is still out there" is said only to a captain who put a chest down.</summary>
    [Fact]
    public void AnEmptySlingNeverHearsAboutTheChest_ButABuriedChestStillCan()
    {
        bool chestLineSeen = false;
        for (ulong seed = 0; seed < 60; seed++)
        {
            ulong fit = DeathNarration.SeedFor(DeathCause.Reevers, DeathPlace.LandingParty, seed, chestOut: false);
            string empty = DeathNarration.Line(DeathCause.Reevers, DeathPlace.LandingParty, fit, "Miranda");
            Assert.DoesNotContain("chest", empty, StringComparison.OrdinalIgnoreCase);

            ulong kept = DeathNarration.SeedFor(DeathCause.Reevers, DeathPlace.LandingParty, seed, chestOut: true);
            Assert.Equal(seed, kept);
            chestLineSeen |= DeathNarration.Line(DeathCause.Reevers, DeathPlace.LandingParty, kept, "Miranda")
                .Contains("chest", StringComparison.OrdinalIgnoreCase);
        }
        Assert.True(chestLineSeen, "the chest line must stay reachable for a captain who left one.");
    }

    /// <summary>U08-F06 · the Hive suffocation line names the depth of the floor the captain died on (the DEAD AIR card's
    /// own MetresDown), never a constant that contradicted it. Swept over the seeds so the one line that names the
    /// shaft is among them.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-5)]
    public void TheHiveSuffocationShaftIsTheFloorsOwnDepth(int level)
    {
        string depth = UndergroundComplex.MetresDown(level).ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
        int named = 0;
        for (ulong seed = 0; seed < 12; seed++)
        {
            string line = DeathNarration.Line(
                DeathCause.Suffocated, DeathPlace.Underground, seed, "Ganymede", undergroundLevel: level);
            Assert.DoesNotContain("{depth}", line);
            Assert.DoesNotContain("three hundred", line);
            if (line.Contains("of shaft above you", StringComparison.Ordinal))
            {
                Assert.Contains($"{depth} metres of shaft above you", line);
                named++;
            }
        }
        Assert.True(named > 0, "no seed drew the shaft line — the sweep proves nothing.");
    }
}
