using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 · THE PLATES ARE REAL — the guard behind the reveal-card lane.
///
/// <para>A reveal plate is a title, a painting and a caption. The house degradation law says the code ships
/// first and the JPG drops in behind it, and the <c>onerror</c>-hide is what makes that safe — which is
/// exactly why a plate pointed at a file nobody ever painted is INVISIBLE in the browser. It does not throw,
/// it does not log, it does not draw a broken frame. It simply shows a card with a hole in it, forever.</para>
///
/// <para>So the art has to be checked somewhere that can fail. This is that place: every plate the arc
/// declares must be keyed to a fragment that actually exists in the pool, and must name a JPG that is
/// actually on disk. The csproj copies <c>wwwroot/art/kaamos-*.jpg</c> beside this assembly on every build
/// (the CssZBandSyncTests idiom), so the test reads the LIVE art directory rather than a snapshot.</para>
///
/// <para><b>Proven RED:</b> point <c>KaamosLore.PlateFor("cold-pod")!.ArtFile</c> at
/// <c>art/kaamos-cold-pod-nope.jpg</c> and <see cref="EveryPlateNamesAPaintingThatExists"/> fails with the
/// missing basename; rename a plate's key to <c>"cold-pods"</c> and
/// <see cref="EveryPlateIsKeyedToARealFragment"/> fails. Both were watched go red before this shipped.</para>
/// </summary>
public class KaamosPlatesArePaintedTests
{
    /// <summary>Where the build drops the arc's paintings — see the csproj's artsource ItemGroup.</summary>
    private static string ArtDir => Path.Combine(AppContext.BaseDirectory, "artsource");

    [Fact]
    public void TheArcDeclaresPlatesAtAll()
    {
        // A guard that passes on an empty collection is a green test that asserts nothing (the house's
        // fifth bug class). Pin the count so deleting the plates cannot quietly satisfy every test below.
        Assert.Equal(3, KaamosLore.AllPlates.Count());
    }

    [Fact]
    public void EveryPlateIsKeyedToARealFragment()
    {
        foreach ((string id, KaamosPlate plate) in KaamosLore.AllPlates)
        {
            Assert.True(
                KaamosLore.ById(id) is not null,
                $"Plate \"{plate.Title}\" is keyed to \"{id}\", which is not a fragment in KaamosLore.Fragments. " +
                "A plate nobody can reach is a painting nobody sees.");
        }
    }

    [Fact]
    public void EveryPlateNamesAPaintingThatExists()
    {
        Assert.True(Directory.Exists(ArtDir), $"No copied art beside the test assembly at {ArtDir} — check the csproj artsource ItemGroup.");

        foreach ((string id, KaamosPlate plate) in KaamosLore.AllPlates)
        {
            Assert.StartsWith("art/", plate.ArtFile, StringComparison.Ordinal);
            string file = Path.Combine(ArtDir, Path.GetFileName(plate.ArtFile));
            Assert.True(
                File.Exists(file),
                $"Plate \"{id}\" names {plate.ArtFile}, which is not in wwwroot/art. The onerror-hide law means " +
                "a missing painting NEVER shows up in the browser — it just leaves a hole in the card.");
        }
    }

    [Fact]
    public void EveryPlateSaysSomethingAndSaysItOnce()
    {
        var seenArt = new HashSet<string>(StringComparer.Ordinal);
        var seenTitles = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string id, KaamosPlate plate) in KaamosLore.AllPlates)
        {
            Assert.False(string.IsNullOrWhiteSpace(plate.Title), $"Plate \"{id}\" has no title.");
            Assert.True(plate.Caption.Length > 80, $"Plate \"{id}\" has a caption too short to be evidence.");
            Assert.True(seenArt.Add(plate.ArtFile), $"Two plates share the painting {plate.ArtFile}.");
            Assert.True(seenTitles.Add(plate.Title), $"Two plates share the title \"{plate.Title}\".");
        }
    }

    [Fact]
    public void TheBeatsThatAreTheRightSizeAsProseGetNoPlate()
    {
        // Over-carding cheapens the big ones (#528's own discipline). These three shards are a line on a
        // plaque, a log found in a drawer, and a coordinate bought over a counter — each already arrives
        // with its own scene around it, and none of them is a turning.
        Assert.Null(KaamosLore.PlateFor("listed-berth"));
        Assert.Null(KaamosLore.PlateFor("vantar-log"));
        Assert.Null(KaamosLore.PlateFor("bought-coordinate"));
    }

    [Fact]
    public void AnUnknownIdAsksForNothing()
    {
        // The client asks PlateFor at the single assemble seam, with whatever id the caller passed. A
        // typo must be a quiet no-card, never a throw in the middle of a find.
        Assert.Null(KaamosLore.PlateFor("no-such-shard"));
    }

    [Fact]
    public void TheCapstonePlateIsTheCapstones()
    {
        // The loudest plate in the arc belongs to the beat the whole thread is for — the berth answering.
        // If the key fragment is ever renamed, this catches the plate left behind on the old id.
        Assert.NotNull(KaamosLore.PlateFor(KaamosLore.KeyFragment.Id));
    }
}
