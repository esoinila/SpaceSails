using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 · A PAINTING NOBODY SWITCHED ON.
///
/// <para><c>art/treasure-miranda.jpg</c> was finished and sitting in <c>wwwroot/art</c> while the client's
/// allow-list named miranda, in its own comment, as the example of a body with <i>no</i> art. Nothing could
/// tell: the card's image is a CSS <c>url()</c> layered over a deterministic per-body gradient, so an asset
/// that is missing — or present but never listed — degrades silently to a tint. Exactly the failure mode the
/// <c>onerror</c>-hide law has elsewhere, and exactly why art needs a guard that can go red.</para>
///
/// <para>This one checks BOTH directions, because the bug went the unusual way round: not a name pointing at
/// a file that does not exist, but a file that exists with no name pointing at it.</para>
///
/// <para><b>Proven RED:</b> drop "miranda" from <see cref="TreasureMapArt.PaintedBodies"/> and
/// <see cref="EveryPaintedBodyIsListed"/> fails naming the orphan; add a body with no JPG and
/// <see cref="EveryListedBodyHasItsPainting"/> fails naming the file.</para>
/// </summary>
public class TreasureMapArtIsWiredTests
{
    /// <summary>Where the build drops the treasure art — see the csproj's artsource ItemGroup.</summary>
    private static string ArtDir => Path.Combine(AppContext.BaseDirectory, "artsource");

    [Fact]
    public void EveryListedBodyHasItsPainting()
    {
        Assert.NotEmpty(TreasureMapArt.PaintedBodies);

        foreach (string body in TreasureMapArt.PaintedBodies)
        {
            string art = TreasureMapArt.ArtFile(body);
            Assert.Equal($"art/treasure-{body.ToLowerInvariant()}.jpg", art);
            Assert.True(
                File.Exists(Path.Combine(ArtDir, Path.GetFileName(art))),
                $"{body} is listed as painted but {art} is not in wwwroot/art. The gradient underneath means " +
                "the card still renders, so nothing in the game would ever have said so.");
        }
    }

    [Fact]
    public void EveryPaintedBodyIsListed()
    {
        // The direction that caught miranda: a finished painting with nothing pointing at it.
        foreach (string file in Directory.GetFiles(ArtDir, "treasure-*.jpg"))
        {
            string body = Path.GetFileNameWithoutExtension(file)["treasure-".Length..];
            Assert.True(
                TreasureMapArt.PaintedBodies.Contains(body),
                $"{Path.GetFileName(file)} is painted and shipped, and no body is pointed at it — {body} " +
                "still draws the plain gradient. A painting nobody switched on.");
        }
    }

    [Fact]
    public void AnUnpaintedBodyAsksForNothing()
    {
        // The gradient-only path: an empty answer, never a url() at a file that is not there.
        Assert.Equal(string.Empty, TreasureMapArt.ArtFile("no-such-moon"));
    }

    [Fact]
    public void TheLookupIsCaseBlindLikeTheBodyIdsAre()
    {
        // Body ids arrive from scenarios and from query params; the old client set was OrdinalIgnoreCase
        // and this must stay so, or a "Phobos" in one file and a "phobos" in another silently unpaints it.
        Assert.Equal("art/treasure-phobos.jpg", TreasureMapArt.ArtFile("Phobos"));
        Assert.Equal("art/treasure-phobos.jpg", TreasureMapArt.ArtFile("phobos"));
    }
}
