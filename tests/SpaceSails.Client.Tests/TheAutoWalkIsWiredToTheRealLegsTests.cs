using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #729 · THE OTHER HALF OF THE GUARD — the wiring.
///
/// <para><see cref="ClickToWalkIsStillWalkingTests"/> proves the ROUTE is honest: it never asks for a move
/// the collision refuses, it costs the same air, it dies on a cancel and it plans nothing at all when a
/// caller says not now. Every one of those laws is worth nothing if the client walks the captain some OTHER way — a
/// position assignment, a second stepper, a cancel that happens a frame late. That is this project's third
/// named bug class in one sentence: <b>the sim doing one thing while a sentence or a drawn shape reports
/// another</b>, and the whole feature is one <c>_avatarX = …</c> away from it.</para>
///
/// <para>So this reads the shipping source and pins the three joins that cannot be pinned any other way,
/// in the same idiom <c>EveryStoryBeatHasACallerTests</c> uses for a beat with no caller. Each one goes RED
/// if the join is removed, which is the only property that makes a guard like this worth its lines.</para>
/// </summary>
public sealed class TheAutoWalkIsWiredToTheRealLegsTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string MapDeck() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Deck.cs"));

    private static string MapSim() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Sim.cs"));

    [Fact]
    public void AMovementKeyCancelsTheWalkOnThePressItself()
    {
        // Not "eventually", not "next frame": the very case that records a held movement key must drop the
        // route before it records it. Cancelling any later would let the walk finish the leg it was on, and
        // "it kept going after I grabbed the controls" is the one feel this must never have.
        string src = MapDeck();
        int arrowRight = src.IndexOf("case \"d\" or \"D\" or \"ArrowRight\":", StringComparison.Ordinal);
        Assert.True(arrowRight > 0, "the movement-key case has moved — this guard cannot see what it guards.");

        int add = src.IndexOf("_deckKeys.Add(", arrowRight, StringComparison.Ordinal);
        int cancel = src.IndexOf("CancelAutoWalk(", arrowRight, StringComparison.Ordinal);
        Assert.True(cancel > 0 && cancel < add,
            "a movement key does not cancel the auto-walk before it is recorded — the keys no longer win.");
    }

    [Fact]
    public void TheWalkIsSpentThroughTheAvatarsOwnStepperAndNothingElse()
    {
        // The route hands out a DELTA, never a destination. If any of it ever reached the avatar's position
        // by assignment, every law in the sister file would still be green and the captain would be sliding
        // through walls in the shipping game.
        string src = MapDeck();
        int block = src.IndexOf("if (_autoWalk is { Active: true } route", StringComparison.Ordinal);
        Assert.True(block > 0, "the auto-walk drive block has moved — this guard cannot see what it guards.");

        int end = src.IndexOf("double dx = 0, dy = 0;", block, StringComparison.Ordinal);
        Assert.True(end > block, "could not find the end of the drive block.");
        string drive = src[block..end];

        Assert.Contains("_deckPlan.Move(_avatarX, _avatarY, sdx, sdy)", drive, StringComparison.Ordinal);
        Assert.Contains("CurrentWalkSpeed * dt", drive, StringComparison.Ordinal);

        // The only writes to the avatar's position in the whole block are the ones the stepper answered.
        var writes = new Regex(@"_avatarX\s*=|\(_avatarX,\s*_avatarY\)\s*=", RegexOptions.Compiled);
        foreach (Match m in writes.Matches(drive))
        {
            int lineStart = drive.LastIndexOf('\n', m.Index) + 1;
            int lineEnd = drive.IndexOf('\n', m.Index);
            string line = drive[lineStart..(lineEnd < 0 ? drive.Length : lineEnd)];
            Assert.Contains("_deckPlan.Move", line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BothPointerJoinsAskTheViewQuestionAndTheViewQuestionOnly()
    {
        // #875 · Both pointer joins go through the same VIEW question, and it is about the canvas and
        // nothing else. This used to read AutoWalkAvailable, which mixed four facts into one property — a
        // dev flag, the view, the surface and the escort. The flag retired with the owner's always-on
        // ruling; the rest split in two, and the split is the point: a pointer handler that asked the WALK
        // question would swallow a held click in a branch that has no way to say why.
        string src = MapSim();
        foreach (string handler in new[] { "OnPointerDown", "OnPointerUp" })
        {
            int at = src.IndexOf($"private void {handler}(PointerEventArgs e)", StringComparison.Ordinal);
            Assert.True(at > 0, $"{handler} has moved — this guard cannot see what it guards.");
            int gate = src.IndexOf("ADeckClickIsAPlaceOnTheFloor", at, StringComparison.Ordinal);
            Assert.True(gate > at && gate - at < 2000,
                $"{handler} no longer routes the deck click through the ADeckClickIsAPlaceOnTheFloor gate.");
        }

        // And the two questions are still two questions, each asking exactly what it is named for.
        Assert.Matches(
            new Regex(@"ADeckClickIsAPlaceOnTheFloor\s*=>\s*_deckMode\s*&&\s*!_fpMode\s*;"), MapDeck());
        Assert.Matches(
            new Regex(@"TheCaptainsLegsAreTheirOwn\s*=>\s*_deckMode\s*&&\s*!CaptainIsUnderEscort\s*;"),
            MapDeck());
    }
}
