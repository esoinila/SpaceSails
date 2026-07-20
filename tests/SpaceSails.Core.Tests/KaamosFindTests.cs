namespace SpaceSails.Core.Tests;

/// <summary>
/// The seeded WHERE for the two randomly-placed KAAMOS fragments (#411 delivery lane): the cold supply pod
/// in an outer moon's regolith, and the rare berth-holder at a bar. Pins that both are DETERMINISTIC (a
/// square/watch always answers the same), that the pod only surfaces on the icy outer moons (never on a hot
/// inner rock or the open dust), and that a methodical sweep and a patient bar-crawl can actually turn each
/// up — so the fragment is reachable in real play, not a needle no seed ever plants.
/// </summary>
public class KaamosFindTests
{
    // ── The cold supply pod (cold-pod). ──

    [Fact]
    public void ColdPodBodies_AreTheOuterIcyMoons_IncludingTheIceMoon()
    {
        Assert.Contains(KaamosLore.IceMoonBodyId, KaamosFind.ColdPodBodies); // enceladus is on the run
        Assert.Contains("europa", KaamosFind.ColdPodBodies);
        Assert.Contains("titan", KaamosFind.ColdPodBodies);
        // Never a hot inner rock or a home world.
        Assert.DoesNotContain("earth", KaamosFind.ColdPodBodies);
        Assert.DoesNotContain("mercury", KaamosFind.ColdPodBodies);
    }

    [Fact]
    public void IsColdPodSquare_IsDeterministic()
    {
        for (int i = 0; i < 50; i++)
        {
            bool a = KaamosFind.IsColdPodSquare("titan", i, i * 2);
            bool b = KaamosFind.IsColdPodSquare("titan", i, i * 2);
            Assert.Equal(a, b); // no RNG — the same square always answers the same
        }
    }

    [Fact]
    public void IsColdPodSquare_NeverTrueOffAColdPodBody()
    {
        foreach (string body in new[] { "earth", "mars", "luna", "sun", "mercury", "", "not-a-body" })
        {
            for (int x = -20; x <= 20; x++)
            {
                for (int y = -20; y <= 20; y++)
                {
                    Assert.False(KaamosFind.IsColdPodSquare(body, x, y), $"{body} should hide no pod");
                }
            }
        }
    }

    [Fact]
    public void IsColdPodSquare_PlantsAtLeastOnePod_OnEachColdPodBody()
    {
        // A captain sweeping a field of squares must be able to find it — every cold-pod body seeds at least
        // one pod square within a modest sweep (and not EVERY square, or it wouldn't be a rare find).
        foreach (string body in KaamosFind.ColdPodBodies)
        {
            int hits = 0, total = 0;
            for (int x = -25; x <= 25; x++)
            {
                for (int y = -25; y <= 25; y++)
                {
                    total++;
                    if (KaamosFind.IsColdPodSquare(body, x, y))
                    {
                        hits++;
                    }
                }
            }

            Assert.True(hits > 0, $"{body} seeds no cold pod anywhere in the sweep");
            Assert.True(hits < total, $"{body} seeds a pod on EVERY square — not a rare find");
        }
    }

    // ── The rare berth-holder at a bar (holders-tell). ──

    [Fact]
    public void HolderAtBar_IsDeterministicPerBarAndWatch()
    {
        for (int day = 0; day < 40; day++)
        {
            bool a = KaamosFind.HolderAtBar("the-space-bar", day);
            bool b = KaamosFind.HolderAtBar("the-space-bar", day);
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void HolderAtBar_ShowsUpSometimes_ButNotEveryWatch()
    {
        int present = 0;
        const int watches = 60;
        for (int day = 0; day < watches; day++)
        {
            if (KaamosFind.HolderAtBar("ringside-exchange", day))
            {
                present++;
            }
        }

        Assert.True(present > 0, "the holder never appears at this bar — the tell would be unreachable");
        Assert.True(present < watches, "the holder is ALWAYS in — that's not a rare contact");
    }

    [Fact]
    public void HolderAtBar_EmptyBar_IsNeverAHolderWatch()
    {
        Assert.False(KaamosFind.HolderAtBar("", 3));
    }
}
