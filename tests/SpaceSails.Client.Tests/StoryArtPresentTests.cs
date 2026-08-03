using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #528 · THE FOLDER AND THE CODE AGREE. Every story beat names an art file (<see cref="StoryBeats.ArtFile"/>),
/// and by design a beat whose JPG has not been painted still fires — the title and caption carry it and the
/// <c>&lt;img&gt;</c> hides itself. That graceful degradation is good, and it is exactly why nobody notices when a
/// canvas is missing: <b>nothing breaks, it just quietly stops being illustrated.</b>
///
/// <para>Which is what happened here. Two canvases were painted and shipped and the manifest still said ⬜ TO
/// PAINT, and one beat (<c>FireAboard</c>) had a file in the folder and no manifest entry at all. Nothing was
/// wrong at runtime; the paperwork had simply drifted from the folder, and the only way to find out was to list
/// both by hand.</para>
///
/// <para>So this is a paperwork law rather than a crash guard, and it lives in the CLIENT test project because
/// this is the only one that can see <c>wwwroot</c> — the same reason the console-crowding audit lives here.
/// Adding a ninth beat now means painting it or being told.</para>
/// </summary>
public sealed class StoryArtPresentTests
{
    /// <summary>Walk up from the test binary until the client's art folder appears. Cheaper and more robust than
    /// threading a repo path through MSBuild, and it fails with a legible message rather than a null.</summary>
    private static string ArtDirectory()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client", "wwwroot", "art");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            $"could not find src/SpaceSails.Client/wwwroot/art above {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// EVERY BEAT HAS A CANVAS. The story-card seam degrades silently by design, so the only thing standing
    /// between a beat and being permanently unillustrated is somebody noticing. This is that somebody.
    /// </summary>
    [Fact]
    public void EveryStoryBeatsArtFileIsActuallyInTheFolder()
    {
        string art = ArtDirectory();
        List<string> missing = [];

        foreach (StoryBeats.Beat beat in Enum.GetValues<StoryBeats.Beat>())
        {
            string named = StoryBeats.ArtFile(beat);           // e.g. "art/first-shot.jpg"
            string file = Path.Combine(art, Path.GetFileName(named));
            if (!File.Exists(file))
            {
                missing.Add($"{beat} → {named}");
            }
        }

        Assert.True(missing.Count == 0,
                    "story beats naming art that is not in wwwroot/art (paint it, or the card fires without a " +
                    $"picture and nobody notices): {string.Join(", ", missing)}");
    }

    /// <summary>
    /// …AND THE MANIFEST KNOWS ABOUT IT. The composition specs in <c>docs/art-manifest-moments.md</c> are what a
    /// regeneration is done FROM — including the space-not-sea negative list that two canvases had to be redone
    /// for. A beat with a file and no spec is a canvas nobody can reproduce.
    /// </summary>
    [Fact]
    public void EveryStoryBeatsArtFileIsSpecifiedInTheManifest()
    {
        string repo = Directory.GetParent(Path.GetDirectoryName(ArtDirectory())!)!.Parent!.Parent!.FullName;
        string manifest = Path.Combine(repo, "docs", "art-manifest-moments.md");
        Assert.True(File.Exists(manifest), $"no manifest at {manifest}");

        string text = File.ReadAllText(manifest);
        List<string> unspecified = [];

        foreach (StoryBeats.Beat beat in Enum.GetValues<StoryBeats.Beat>())
        {
            if (!text.Contains(StoryBeats.ArtFile(beat), StringComparison.Ordinal))
            {
                unspecified.Add($"{beat} → {StoryBeats.ArtFile(beat)}");
            }
        }

        Assert.True(unspecified.Count == 0,
                    $"beats with no composition spec in the manifest: {string.Join(", ", unspecified)}");
    }

    /// <summary>And nothing in the folder claims to be painted while the manifest still says ⬜ TO PAINT — the
    /// exact drift this file was written after finding.</summary>
    [Fact]
    public void TheManifestDoesNotCallAPaintedCanvasUnpainted()
    {
        string art = ArtDirectory();
        string repo = Directory.GetParent(Path.GetDirectoryName(art)!)!.Parent!.Parent!.FullName;
        string[] lines = File.ReadAllLines(Path.Combine(repo, "docs", "art-manifest-moments.md"));
        List<string> stale = [];

        foreach (string line in lines)
        {
            if (!line.StartsWith("## ", StringComparison.Ordinal) || !line.Contains("TO PAINT", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (StoryBeats.Beat beat in Enum.GetValues<StoryBeats.Beat>())
            {
                string named = StoryBeats.ArtFile(beat);
                if (line.Contains(named, StringComparison.Ordinal) &&
                    File.Exists(Path.Combine(art, Path.GetFileName(named))))
                {
                    stale.Add(named);
                }
            }
        }

        Assert.True(stale.Count == 0,
                    $"painted and still marked TO PAINT in the manifest: {string.Join(", ", stale)}");
    }
}
