using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #488 / #528 · THE NEST'S THREE PLATES — before, after, and the door coming off its dogs.
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
        foreach ((string which, RevealPlate plate) in AllThree())
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

    /// <summary>The three plates this room owns — before, after, and the door coming off its dogs. A list
    /// rather than three names, so adding a fourth cannot skip the canon grep below.</summary>
    private static (string Which, RevealPlate Plate)[] AllThree() =>
    [
        ("live", NestPlates.Live),
        ("dead", NestPlates.Dead),
        ("released", NestPlates.Released),
    ];

    [Fact]
    public void TheRoomHasThreePlatesAndNoTwoAreTheSameCard()
    {
        // #528 round two: the sealed door is the whole decision the vacuum mechanic exists to make
        // interesting and it was a pulse line, beside a before-card and an after-card that had had
        // paintings for weeks. Pinned at three so a deletion cannot quietly satisfy the sweeps.
        (string Which, RevealPlate Plate)[] all = AllThree();
        Assert.Equal(3, all.Length);
        Assert.Equal(3, all.Select(p => p.Plate.ArtFile).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, all.Select(p => p.Plate.Title).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, all.Select(p => p.Plate.Caption).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheReleasedCardsTitleNamesTheRoomToo()
    {
        // Same reason as the after-card: it is a door in a NAMED compartment, and the two halves of that
        // sentence must not live in two files.
        string title = NestPlates.ReleasedTitle("DEEP HOLD");
        Assert.Contains("DEEP HOLD", title, StringComparison.Ordinal);
        Assert.EndsWith(NestPlates.Released.Title, title, StringComparison.Ordinal);
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
        foreach ((string _, RevealPlate plate) in AllThree())
        {
            string text = plate.Title + " " + plate.Caption;
            foreach (string forbidden in new[] { "Reever", "Old One", "alien", "creature", "hive", "queen" })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
