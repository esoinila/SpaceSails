namespace SpaceSails.Core.Tests;

public class ScanDiscoveryTests
{
    private const ulong Seed = 42;

    [Fact]
    public void FindAt_NeverComesBackEmpty()
    {
        // A spread of arbitrary patches: whatever the cells hold, the scan resolves something.
        for (int i = 0; i < 20; i++)
        {
            var center = new Vector2d(1e11 + i * 7.3e10, -5e10 + i * 3.1e10);
            IReadOnlyList<Discovery> found = ScanDiscoveries.FindAt(Seed, center, 5e9, simTime: i * 13_000);

            Assert.NotEmpty(found);
        }
    }

    [Fact]
    public void FindAt_IsDeterministic()
    {
        var center = new Vector2d(2.5e11, 1e11);

        IReadOnlyList<Discovery> first = ScanDiscoveries.FindAt(Seed, center, 3e10, 5 * 86400.0);
        IReadOnlyList<Discovery> second = ScanDiscoveries.FindAt(Seed, center, 3e10, 5 * 86400.0);

        Assert.Equal(first, second);
    }

    [Fact]
    public void FindAt_SameDaySameCells_DifferentTimeOfDay_AgreesExactly()
    {
        var center = new Vector2d(2.5e11, 1e11);

        IReadOnlyList<Discovery> morning = ScanDiscoveries.FindAt(Seed, center, 3e10, 5 * 86400.0 + 3600);
        IReadOnlyList<Discovery> evening = ScanDiscoveries.FindAt(Seed, center, 3e10, 5 * 86400.0 + 80_000);

        Assert.Equal(morning, evening);
    }

    [Fact]
    public void FindAt_TheSkyTurnsAPage_OnANewDay()
    {
        var center = new Vector2d(2.5e11, 1e11);

        IReadOnlyList<Discovery> today = ScanDiscoveries.FindAt(Seed, center, 3e10, 5 * 86400.0);
        IReadOnlyList<Discovery> tomorrow = ScanDiscoveries.FindAt(Seed, center, 3e10, 6 * 86400.0);

        Assert.NotEqual(today.Select(d => d.Id), tomorrow.Select(d => d.Id));
    }

    [Fact]
    public void FindAt_EverythingResolved_LiesInsideTheScannedDisc()
    {
        var center = new Vector2d(2.5e11, 1e11);
        double radius = 2.5e10;

        IReadOnlyList<Discovery> found = ScanDiscoveries.FindAt(Seed, center, radius, 86400.0);

        Assert.All(found, d => Assert.True(
            (d.Position - center).Length <= radius,
            $"{d.Id} resolved outside the scanned disc"));
    }

    [Fact]
    public void FindAt_DifferentSeeds_PopulateDifferentSkies()
    {
        var center = new Vector2d(2.5e11, 1e11);

        IReadOnlyList<Discovery> skyA = ScanDiscoveries.FindAt(1, center, 3e10, 86400.0);
        IReadOnlyList<Discovery> skyB = ScanDiscoveries.FindAt(2, center, 3e10, 86400.0);

        Assert.NotEqual(skyA, skyB);
    }
}
