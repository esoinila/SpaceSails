using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #488 / #528 · THE NEST'S TWO PLATES — before and after, and both painted.
///
/// <para>The after-card is the house's type specimen for a reveal. The before-card did not exist until
/// #528, though <c>art/vented-nest-intact.jpg</c> had shipped in <c>wwwroot/art</c> with nothing pointing
/// at it — so the setup for this hull's best payoff was missing while the picture of it sat on disk.</para>
///
/// <para><b>Proven RED:</b> point either plate at a file that is not there and
/// <see cref="BothPlatesArePainted"/> fails naming it; let the two plates share one painting and
/// <see cref="BeforeAndAfterAreDifferentPictures"/> fails, which is the whole point of the pair.</para>
/// </summary>
public class NestPlatesTests
{
    private static string ArtDir => Path.Combine(AppContext.BaseDirectory, "artsource");

    [Fact]
    public void BothPlatesArePainted()
    {
        foreach ((string which, RevealPlate plate) in new[] { ("live", NestPlates.Live), ("dead", NestPlates.Dead) })
        {
            Assert.StartsWith("art/", plate.ArtFile, StringComparison.Ordinal);
            Assert.True(
                File.Exists(Path.Combine(ArtDir, Path.GetFileName(plate.ArtFile))),
                $"The {which} nest plate names {plate.ArtFile}, which is not in wwwroot/art.");
        }
    }

    [Fact]
    public void BeforeAndAfterAreDifferentPictures()
    {
        // The pair only works as a pair. Two cards of the same room showing the same image would say the
        // vacuum changed nothing, which is the opposite of what the mechanic is for.
        Assert.NotEqual(NestPlates.Live.ArtFile, NestPlates.Dead.ArtFile);
        Assert.NotEqual(NestPlates.Live.Caption, NestPlates.Dead.Caption);
    }

    [Fact]
    public void TheAfterCardsTitleNamesTheRoom()
    {
        // The room is the subject of that sentence, and the two halves used to live in two files.
        string title = NestPlates.DeadTitle("DEEP HOLD");
        Assert.Contains("DEEP HOLD", title, StringComparison.Ordinal);
        Assert.EndsWith(NestPlates.Dead.Title, title, StringComparison.Ordinal);
    }

    [Fact]
    public void NeitherPlateExplainsAnything()
    {
        // The house rule for this hull and the reason the canon grep passes: the room is evidence. It may
        // never name what made it. (TheHiveTests.NothingDownHereEXPLAINSAnything is the same law, downstairs.)
        foreach (RevealPlate plate in new[] { NestPlates.Live, NestPlates.Dead })
        {
            string text = plate.Title + " " + plate.Caption;
            foreach (string forbidden in new[] { "Reever", "Old One", "alien", "creature", "hive", "queen" })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
