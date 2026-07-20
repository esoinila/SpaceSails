namespace SpaceSails.Core.Tests;

/// <summary>
/// The seeded WHERE for the NEBULA fragment that needs a deterministic "is it here?" — the rare Nebula Mutual
/// adjuster at a bar (<c>adjuster-tell</c>), issue #422's delivery lane. Pins that presence is DETERMINISTIC
/// (a bar/watch always answers the same), that a patient bar-crawl can actually turn the adjuster up (so the
/// tell is reachable, not a needle no seed ever plants), and that the adjuster is salted DISTINCTLY from the
/// KAAMOS holder so the two roving contacts don't share every watch by coincidence. Mirrors KaamosFindTests.
/// </summary>
public class NebulaFindTests
{
    [Fact]
    public void AdjusterAtBar_IsDeterministicPerBarAndWatch()
    {
        for (int day = 0; day < 40; day++)
        {
            bool a = NebulaFind.AdjusterAtBar("the-space-bar", day);
            bool b = NebulaFind.AdjusterAtBar("the-space-bar", day);
            Assert.Equal(a, b); // no RNG — the same bar/watch always answers the same
        }
    }

    [Fact]
    public void AdjusterAtBar_ShowsUpSometimes_ButNotEveryWatch()
    {
        int present = 0;
        const int watches = 60;
        for (int day = 0; day < watches; day++)
        {
            if (NebulaFind.AdjusterAtBar("ringside-exchange", day))
            {
                present++;
            }
        }

        Assert.True(present > 0, "the adjuster never appears at this bar — the tell would be unreachable");
        Assert.True(present < watches, "the adjuster is ALWAYS in — that's not a rare roving contact");
    }

    [Fact]
    public void AdjusterAtBar_EmptyBar_IsNeverAnAdjusterWatch()
    {
        Assert.False(NebulaFind.AdjusterAtBar("", 3));
    }

    [Fact]
    public void AdjusterAtBar_IsNotAlwaysTheSameWatchAsTheKaamosHolder()
    {
        // The two roving contacts are salted apart: over a long run they must NOT be present on exactly the
        // same set of watches (that would betray a shared seed and let one give away the other's schedule).
        int agree = 0;
        const int watches = 120;
        for (int day = 0; day < watches; day++)
        {
            if (NebulaFind.AdjusterAtBar("the-space-bar", day) == KaamosFind.HolderAtBar("the-space-bar", day))
            {
                agree++;
            }
        }

        Assert.True(agree < watches, "the adjuster and the KAAMOS holder share EVERY watch — the salts collide");
    }
}
