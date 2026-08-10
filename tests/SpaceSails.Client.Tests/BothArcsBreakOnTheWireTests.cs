using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #663 · THE WIRE IS WHERE AN ARC STOPS BEING YOUR PRIVATE BUSINESS.
///
/// <para><c>docs/QAHandoff-StoryTelling.md</c> §5, quoted by the issue: <i>"an arc beat landing on the news
/// wire is the exact storytelling device the KAAMOS (#411) and Nebula (#422) passes are missing. Both arcs
/// currently advance by finding a fragment and reading a card; neither ever tells the player that the world
/// noticed."</i> #668 gave arc 1 its break — the ice-moon berth taking a filing — and stopped there, so half
/// the finding stayed open and arc 2 still happened entirely inside the captain's reading.</para>
///
/// <para><b>Why this is a scanner and not an assertion about a rendered page.</b> The client test project has
/// no renderer; the shipped idiom for "this seam is wired" here is to read the source the way
/// <c>EveryStoryBeatHasACallerTests</c> does. So the claim is split in two and both halves have to hold: the
/// SOURCE must make the two calls on each arc's own edge (this file's first test), and the WORLD must be able
/// to cross that edge with the fragments the game actually ships (this file's third). A wire nobody can reach
/// is the same nothing as a wire nobody wrote.</para>
/// </summary>
public sealed class BothArcsBreakOnTheWireTests
{
    /// <summary>The arc partials that own a one-time revelation, and the edge each one breaks on. Adding an
    /// arc 3 here is how its author finds out that the wire is part of the idiom.</summary>
    private static readonly (string File, string Edge)[] Arcs =
    [
        ("Map.Kaamos.cs", "the berth-code resolving — a dead berth takes a filing"),
        ("Map.Nebula.cs", "the policy's true terms resolving — the terms are re-lodged"),
    ];

    private static string ClientDir()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the client above {AppContext.BaseDirectory}");
    }

    private static string ArcSource(string file) =>
        File.ReadAllText(Path.Combine(ClientDir(), "Pages", file));

    /// <summary>The guard this lane exists for: BOTH halves of the break, on BOTH arcs. The beat alone would
    /// be a card with nothing behind it; the news event alone would be a line in a ticker nobody looks at.</summary>
    [Fact]
    public void EveryArcWithARevelationBreaksItOnTheWireAndRaisesTheBeat()
    {
        var missing = new List<string>();

        foreach ((string file, string edge) in Arcs)
        {
            string src = ArcSource(file);

            if (!src.Contains("PushNewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks", StringComparison.Ordinal))
            {
                missing.Add($"{file} never files an ArcBeatBreaks event on {edge}");
            }

            if (!src.Contains("RaiseStoryBeat(StoryBeats.Beat.ArcNewsBreaks", StringComparison.Ordinal))
            {
                missing.Add($"{file} never raises StoryBeats.Beat.ArcNewsBreaks on {edge}");
            }
        }

        Assert.True(missing.Count == 0,
            "an arc that ends in a revelation and never reaches the wire is an arc the world never noticed " +
            $"(#663): {string.Join("; ", missing)}");
    }

    /// <summary>The fifth-bug-class companion. A scan that read the wrong files, or empty ones, would pass
    /// every claim above; pin that these files are the real partials and that the scanner can say NO.</summary>
    [Fact]
    public void TheScanIsReadingTheRealArcPartials()
    {
        foreach ((string file, string _) in Arcs)
        {
            string src = ArcSource(file);
            Assert.True(src.Length > 2_000, $"{file} is too small to be the arc partial — the scan is lost");
            Assert.Contains("public partial class Map", src, StringComparison.Ordinal);
        }

        // …and the same scanner, asked about something that is definitely not there, says no. Without this a
        // Contains() that always matched would make the test above green on any tree at all.
        Assert.DoesNotContain("PushNewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks",
            ArcSource("Map.StoryCards.cs"), StringComparison.Ordinal);
    }

    /// <summary>
    /// AND THE WORLD CAN ACTUALLY GET THERE. The edge arc 2's break rides on is
    /// <c>NebulaProgress.KnowsTheTruth</c> flipping, so this walks the shipped fragment pool the way the game
    /// does and pins that (a) the flip happens, (b) it happens exactly once, and (c) it needs a real
    /// collection rather than a single shard. A guard against a state nothing can enter is the vacuous world.
    /// </summary>
    [Fact]
    public void TheEdgeArcTwoBreaksOnIsReachableByAssemblingTheShippedShards()
    {
        var progress = new NebulaProgress();
        Assert.False(progress.KnowsTheTruth);   // a fresh universe knows nothing

        int flips = 0;
        foreach (NebulaFragment f in NebulaLore.Fragments)
        {
            bool before = progress.KnowsTheTruth;
            Assert.True(progress.Assemble(f.Id), $"the shipped pool refused its own fragment {f.Id}");
            if (!before && progress.KnowsTheTruth)
            {
                flips++;
            }
        }

        Assert.True(progress.KnowsTheTruth, "assembling every shipped NEBULA shard still does not resolve the truth");
        Assert.Equal(1, flips);   // one edge, one break — the caller cannot fire twice per universe

        // And it is a COLLECTION, not a single find: one shard must not be enough, or the "arc" is a pickup.
        var oneShard = new NebulaProgress();
        oneShard.Assemble(NebulaLore.Fragments[0].Id);
        Assert.False(oneShard.KnowsTheTruth);
    }

    /// <summary>
    /// THE REGISTER IS THE WHOLE TRICK. Both headlines are clerical notes filed by somebody with no idea what
    /// they are filing — never about the captain, never explaining a plot. A wire that says "you" has stopped
    /// being a wire and started being a quest log, and the beat's caption ("one figure walks away from the
    /// screen instead of toward it, because it is not news to them") stops working the moment it does.
    /// </summary>
    [Theory]
    [InlineData(nameof(CyclerWindow.BerthListedHeadline))]
    [InlineData(nameof(NebulaLore.TermsRefiledHeadline))]
    public void TheWireFilesTheStoryAndNeverNarratesIt(string which)
    {
        string headline = which == nameof(CyclerWindow.BerthListedHeadline)
            ? CyclerWindow.BerthListedHeadline
            : NebulaLore.TermsRefiledHeadline;

        Assert.False(string.IsNullOrWhiteSpace(headline));
        Assert.True(headline.Length > 80, "a filed note is a sentence, not a stub");

        foreach (Match m in Regex.Matches(headline, @"\b(you|your|yours|captain)\b", RegexOptions.IgnoreCase))
        {
            Assert.Fail($"{which} addresses the captain (\"{m.Value}\") — the wire never knows whose hull it is");
        }

        // The headline IS the subject on this event kind, so the wire must not wrap it in anything.
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks, 0.0, headline);
        Assert.Equal(headline, NewsWire.Headline(evt));
    }

    /// <summary>The card that goes up beside the wire item reads whole with arc 2's subject in it. The beat's
    /// caption was written for exactly this moment and has been on the shelf since #528.</summary>
    [Fact]
    public void ArcTwosSubjectReadsWholeInTheBeatsOwnCaption()
    {
        string caption = StoryBeats.Caption(StoryBeats.Beat.ArcNewsBreaks, "the policy");

        Assert.Contains("the policy is not news to them", caption, StringComparison.Ordinal);
        Assert.DoesNotContain(" her is not news", caption, StringComparison.Ordinal);   // the un-subjected default
    }
}
