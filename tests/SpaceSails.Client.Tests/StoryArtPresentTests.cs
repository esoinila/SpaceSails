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
///
/// <para>#664 · <b>And it sweeps <see cref="StoryBeats.Canvases"/> now, not <c>ArtFile(beat)</c>.</b> That was
/// the whole truth while every beat had exactly one painting, and it stopped being the truth the moment the
/// two arcs arrived: a KAAMOS or NEBULA shard's canvas is chosen by the fragment the captain just assembled,
/// so <c>ArtFile(beat)</c> with no subject answers the empty string and a sweep built on it would have
/// silently stopped guarding the eight pictures those two beats can actually put on the screen. The manifest
/// half is widened the same way — the arcs' compositions are specified in <c>art-manifest-kaamos.md</c> and
/// <c>art-manifest-nebula.md</c>, and eight of the eleven moments adopted from the deleted reveal card are
/// specified in the wrecks, bars, surface and hive manifests, because that is where the feature that owns
/// each picture writes its specs down. A beat with a canvas in NO manifest is still a canvas nobody can
/// reproduce, which is the claim; which file it is in was never the claim.</para>
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

    /// <summary>The repo root, above the client's art folder.</summary>
    private static string RepoRoot() =>
        Directory.GetParent(Path.GetDirectoryName(ArtDirectory())!)!.Parent!.Parent!.FullName;

    /// <summary>Every composition spec the project keeps, as one blob. Nine manifests today; the sweep reads
    /// the folder rather than a list, so a tenth is covered the day somebody writes it.</summary>
    private static string AllManifests() =>
        string.Concat(Directory.EnumerateFiles(Path.Combine(RepoRoot(), "docs"), "art-manifest-*.md")
                               .Order(StringComparer.Ordinal)
                               .Select(File.ReadAllText));

    /// <summary>
    /// #664 · A SWEEP THAT ASKS FOR NOTHING PROVES NOTHING. <c>Canvases</c> is where the two claims below get
    /// their list, so this pins that the list is real before anything is asserted about it: every beat names
    /// at least one canvas and none of them names the empty string — which is exactly what a subject-keyed
    /// beat would answer if somebody wired one and forgot to give <c>Canvases</c> an arm for it.
    /// </summary>
    [Fact]
    public void EveryBeatNamesAtLeastOneRealCanvas()
    {
        List<string> silent = [.. Enum.GetValues<StoryBeats.Beat>()
            .Where(b => !StoryBeats.Canvases(b).Any(c => !string.IsNullOrWhiteSpace(c)))
            .Select(b => b.ToString())];

        Assert.True(silent.Count == 0,
                    "beats whose Canvases() names no painting at all — a beat keyed by its subject needs an " +
                    "arm there, or the two sweeps below pass by asking it nothing: " + string.Join(", ", silent));
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
            foreach (string named in StoryBeats.Canvases(beat))   // e.g. "art/first-shot.jpg"
            {
                string file = Path.Combine(art, Path.GetFileName(named));
                if (!File.Exists(file))
                {
                    missing.Add($"{beat} → {named}");
                }
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
        string moments = Path.Combine(RepoRoot(), "docs", "art-manifest-moments.md");
        Assert.True(File.Exists(moments), $"no manifest at {moments}");

        string text = AllManifests();
        Assert.Contains("art-manifest", moments, StringComparison.Ordinal);
        Assert.True(text.Length > 10_000, "the manifest sweep read almost nothing — it is looking in the " +
                                          "wrong folder, and an empty blob specifies every beat trivially.");

        List<string> unspecified = [];

        foreach (StoryBeats.Beat beat in Enum.GetValues<StoryBeats.Beat>())
        {
            foreach (string named in StoryBeats.Canvases(beat))
            {
                if (!text.Contains(named, StringComparison.Ordinal))
                {
                    unspecified.Add($"{beat} → {named}");
                }
            }
        }

        Assert.True(unspecified.Count == 0,
                    $"beats with no composition spec in any manifest: {string.Join(", ", unspecified)}");
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

            foreach (string named in Enum.GetValues<StoryBeats.Beat>().SelectMany(StoryBeats.Canvases))
            {
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
