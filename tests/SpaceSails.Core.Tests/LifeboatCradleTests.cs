namespace SpaceSails.Core.Tests;

/// <summary>
/// THE CRADLES ON THE WALL. Owner, on walking into the compartment named for them and finding an empty
/// box: <i>"Are the lifeboats there or not … we should somehow see this like slots that are filled or
/// empty … on the wall."</i> How many are gone is a fact about the room, countable from the doorway.
/// </summary>
public class LifeboatCradleTests
{
    [Fact]
    public void EveryCradleSitsInsideTheLifeboatCompartment()
    {
        // Drawn on the wall of the room they belong to — not floating in the spine, not through a bulkhead.
        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == WreckLayout.LifeboatCompartment);

        Assert.False(top, "the cradles' compartment moved rows — the wall they are drawn on is the wrong one");

        foreach ((float x, float y) in WreckLayout.CradleSpots())
        {
            Assert.InRange(x, x0, x1);
            Assert.InRange(y, WreckLayout.SpineHalfHeight, WreckLayout.BottomY);
        }
    }

    [Fact]
    public void ThereAreAsManySpotsAsCradles()
    {
        Assert.Equal(WreckLayout.CradleCount, WreckLayout.CradleSpots().Count());
    }

    [Fact]
    public void TheCradlesDoNotOverlapEachOther()
    {
        var xs = WreckLayout.CradleSpots().Select(p => p.X).OrderBy(x => x).ToList();
        for (int i = 1; i < xs.Count; i++)
        {
            Assert.True(xs[i] - xs[i - 1] > 1.0f, "two cradles are drawn on top of each other");
        }
    }

    // ── What the count says about her ─────────────────────────────────────────────────────────────────

    [Fact]
    public void NobodyGotOffTheHullsWhereNobodyCouldHave()
    {
        // The loudest fact a wreck has. On these three, every boat is still clamped in — which means
        // everybody aboard is still aboard, and the away team is walking through the reason.
        foreach (Derelict.WreckCause cause in new[]
                 {
                     Derelict.WreckCause.Infested,
                     Derelict.WreckCause.VentedByOneOfTheirOwn,
                     Derelict.WreckCause.LifeSupportFailure,
                 })
        {
            Assert.Equal(0, Derelict.LifeboatsLaunched("any-hull", cause, WreckLayout.CradleCount));
        }
    }

    [Fact]
    public void AStagedLossLeavesEveryCradleEmpty()
    {
        // Already what Derelict.Evidence says about her in words: the cradles are empty and the clamps were
        // stowed rather than blown. The wall now says it too, before anybody reads anything.
        Assert.Equal(
            WreckLayout.CradleCount,
            Derelict.LifeboatsLaunched("any-hull", Derelict.WreckCause.InsuranceJob, WreckLayout.CradleCount));
    }

    [Fact]
    public void AMutinyLeavesSomeAndTakesSome()
    {
        int gone = Derelict.LifeboatsLaunched("any-hull", Derelict.WreckCause.Mutiny, WreckLayout.CradleCount);

        Assert.True(gone > 0, "one side left");
        Assert.True(gone < WreckLayout.CradleCount, "the other side did not");
    }

    [Fact]
    public void TheOpenCausesAreSeeded_SoWhetherAnyoneMadeItIsNotPredictableFromTheCauseAlone()
    {
        var seen = new HashSet<int>();
        for (int i = 0; i < 200; i++)
        {
            seen.Add(Derelict.LifeboatsLaunched($"hull-{i}", Derelict.WreckCause.DriveFailure, WreckLayout.CradleCount));
        }

        Assert.True(seen.Count > 1, "every drive failure reads the same from the doorway");
        Assert.All(seen, n => Assert.InRange(n, 0, WreckLayout.CradleCount));
    }

    [Fact]
    public void TheSameHullAlwaysReadsTheSame()
    {
        foreach (string id in new[] { "hull-3", "hull-77", "hull-401" })
        {
            Assert.Equal(
                Derelict.LifeboatsLaunched(id, Derelict.WreckCause.Piracy, WreckLayout.CradleCount),
                Derelict.LifeboatsLaunched(id, Derelict.WreckCause.Piracy, WreckLayout.CradleCount));
        }
    }

    // ── The words ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheReadingCountsAndNeverConcludes()
    {
        // Same sentence structure for both extremes on purpose. The captain decides which is worse.
        string none = Derelict.CradleReading(0, 6);
        string all = Derelict.CradleReading(6, 6);
        string some = Derelict.CradleReading(2, 6);

        Assert.Contains("Not one boat left this ship", none);
        Assert.Contains("without hurrying", all);
        Assert.Contains("Somebody left. Somebody did not.", some);

        foreach (string s in new[] { none, all, some })
        {
            foreach (string verdict in new[] { "fraud", "murder", "staged", "insurance" })
            {
                Assert.DoesNotContain(verdict, s, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheSingularReadsLikeEnglish()
    {
        Assert.Contains("1 boat is still clamped", Derelict.CradleReading(5, 6));
        Assert.Contains("boats are still clamped", Derelict.CradleReading(4, 6));
    }
}
