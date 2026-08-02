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
/// <para>That is not hypothetical. <c>DeathNarration.ArtFile</c> answered <c>death-suffocated.jpg</c> for a
/// year and <b>the game never shipped that file</b> (#621 found it, #636 fixed it). The name was a promise
/// nothing had to keep, and nothing in the build could tell. So the art has to be checked somewhere that can
/// fail — this is that place, for every art constant the story lane owns.</para>
///
/// <para>The csproj copies the story art beside this assembly on every build (the <c>CssZBandSyncTests</c>
/// idiom), so the test reads the LIVE art directory rather than a snapshot.</para>
///
/// <para><b>Proven RED:</b> point <c>KaamosLore.PlateFor("cold-pod")</c> at <c>art/kaamos-cold-pod-nope.jpg</c>
/// and <see cref="EveryPlateNamesAPaintingThatExists"/> fails with the missing basename; rename a plate's key
/// to <c>"cold-pods"</c> and <see cref="EveryPlateIsKeyedToARealFragment"/> fails. Both were watched go red
/// before this shipped, and the death sweep goes red on the pre-#636 mapping.</para>
/// </summary>
public class RevealPlatesArePaintedTests
{
    /// <summary>Where the build drops the story art — see the csproj's artsource ItemGroup.</summary>
    private static string ArtDir => Path.Combine(AppContext.BaseDirectory, "artsource");

    /// <summary>Both arcs' plates in one sweep, tagged with the arc that owns them so a failure names it.</summary>
    private static IEnumerable<(string Arc, string Id, RevealPlate Plate)> AllPlates()
    {
        foreach ((string id, RevealPlate plate) in KaamosLore.AllPlates)
        {
            yield return ("KAAMOS", id, plate);
        }

        foreach ((string id, RevealPlate plate) in NebulaLore.AllPlates)
        {
            yield return ("NEBULA", id, plate);
        }
    }

    /// <summary>Assert one <c>art/…</c> constant names a file that is actually on disk.</summary>
    private static void AssertPainted(string what, string artFile)
    {
        Assert.False(string.IsNullOrWhiteSpace(artFile), $"{what} names no art at all.");
        string file = Path.Combine(ArtDir, Path.GetFileName(artFile));
        Assert.True(
            File.Exists(file),
            $"{what} names {artFile}, which is not in wwwroot/art. The onerror-hide law means a missing " +
            "painting NEVER shows up in the browser — it just leaves a hole in the card.");
    }

    [Fact]
    public void TheArcsDeclarePlatesAtAll()
    {
        // A guard that passes on an empty collection is a green test that asserts nothing (the house's
        // fifth bug class). Pin the counts so deleting the plates cannot quietly satisfy every test below.
        Assert.Equal(3, KaamosLore.AllPlates.Count());
        Assert.Equal(2, NebulaLore.AllPlates.Count());
    }

    [Fact]
    public void EveryPlateIsKeyedToARealFragment()
    {
        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            bool known = arc == "KAAMOS" ? KaamosLore.ById(id) is not null : NebulaLore.ById(id) is not null;
            Assert.True(
                known,
                $"{arc} plate \"{plate.Title}\" is keyed to \"{id}\", which is not a fragment in that arc's " +
                "pool. A plate nobody can reach is a painting nobody sees.");
        }
    }

    [Fact]
    public void EveryPlateNamesAPaintingThatExists()
    {
        Assert.True(Directory.Exists(ArtDir), $"No copied art beside the test assembly at {ArtDir} — check the csproj artsource ItemGroup.");

        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            Assert.StartsWith("art/", plate.ArtFile, StringComparison.Ordinal);
            AssertPainted($"{arc} plate \"{id}\"", plate.ArtFile);
        }
    }

    [Fact]
    public void TheConvergenceIsPainted()
    {
        // The biggest reveal in the game. It was a text div while a routine collector shakedown had a
        // painted portrait; if the plate is ever unwired or renamed, this is the thing that notices.
        AssertPainted("The convergence", ArcConvergence.ArtFile);
    }

    [Fact]
    public void EveryDeathCardIsPainted()
    {
        // The sweep that #621's bug would have failed: every cause × every place, because the PLACE decides
        // the picture and the pre-#636 mapping only broke for one pairing nobody could reach until they
        // could. Enumerating both enums means a new cause or a new place cannot ship art-less.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            AssertPainted($"Death card {cause} (no place)", "art/" + DeathNarration.ArtFile(cause));

            foreach (DeathPlace place in Enum.GetValues<DeathPlace>())
            {
                AssertPainted($"Death card {cause} on {place}", "art/" + DeathNarration.ArtFile(cause, place));
            }
        }
    }

    [Fact]
    public void EveryPlateSaysSomethingAndSaysItOnce()
    {
        var seenArt = new HashSet<string>(StringComparer.Ordinal);
        var seenTitles = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string arc, string id, RevealPlate plate) in AllPlates())
        {
            Assert.False(string.IsNullOrWhiteSpace(plate.Title), $"{arc} plate \"{id}\" has no title.");
            Assert.True(plate.Caption.Length > 80, $"{arc} plate \"{id}\" has a caption too short to be evidence.");
            Assert.True(seenArt.Add(plate.ArtFile), $"Two plates share the painting {plate.ArtFile}.");
            Assert.True(seenTitles.Add(plate.Title), $"Two plates share the title \"{plate.Title}\".");
        }
    }

    [Fact]
    public void TheBeatsThatAreTheRightSizeAsProseGetNoPlate()
    {
        // Over-carding cheapens the big ones (#528's own discipline). The KAAMOS three are a line on a
        // plaque, a log found in a drawer, and a coordinate bought over a counter — each already arrives
        // with its own scene around it, and none of them is a turning.
        Assert.Null(KaamosLore.PlateFor("listed-berth"));
        Assert.Null(KaamosLore.PlateFor("vantar-log"));
        Assert.Null(KaamosLore.PlateFor("bought-coordinate"));

        // The NEBULA four arrive INSIDE a host card that already carries a picture — the BUSTED modal and
        // the poster. A second frame there would stack a card on a card.
        Assert.Null(NebulaLore.PlateFor("rebirth-glitch"));
        Assert.Null(NebulaLore.PlateFor("fine-print"));
        Assert.Null(NebulaLore.PlateFor("collector-writ"));
        Assert.Null(NebulaLore.PlateFor("clinic-ledger"));
    }

    [Fact]
    public void AnUnknownIdAsksForNothing()
    {
        // The client asks PlateFor at the single assemble seam, with whatever id the caller passed. A typo
        // must be a quiet no-card, never a throw in the middle of a find.
        Assert.Null(KaamosLore.PlateFor("no-such-shard"));
        Assert.Null(NebulaLore.PlateFor("no-such-shard"));
    }

    [Fact]
    public void TheCapstonePlatesAreTheCapstones()
    {
        // The loudest plate in each arc belongs to the beat the whole thread is for — the berth answering,
        // and the policy's true terms resolving. If either key fragment is renamed, this catches the plate
        // left behind on the old id.
        Assert.NotNull(KaamosLore.PlateFor(KaamosLore.KeyFragment.Id));
        Assert.NotNull(NebulaLore.PlateFor(NebulaLore.KeyFragment.Id));
    }
}
