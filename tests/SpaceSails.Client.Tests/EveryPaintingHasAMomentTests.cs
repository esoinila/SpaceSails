using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #528 · THE OTHER DIRECTION — A PAINTING WITH NOBODY TO SHOW IT.
///
/// <para>The 08-02 audit went looking for moments with no picture and found the reverse as well, and said so
/// in as many words: <i>"It also found PICTURES WITH NO MOMENT — four of them — and they are invisible for
/// precisely the reason the missing files are: the fallbacks work. An <c>onerror</c>-hide over a caption, a
/// <c>url()</c> over a gradient. Nothing throws, nothing logs, nothing draws a broken frame."</i></para>
///
/// <para>Both halves of that were then guarded on the moment side and neither on the folder side.
/// <c>StoryArtPresentTests</c> sweeps beat → file → manifest; <c>RevealPlatesArePaintedTests</c> sweeps every
/// Core plate the same way; <c>EveryStoryBeatHasACallerTests</c> asks who raises a beat. Every one of them
/// starts from the CODE. Nothing in the build has ever started from the FOLDER — so a canvas that arrives in
/// <c>wwwroot/art</c> and is never named by anything is exactly as silent as <c>death-suffocated.jpg</c> was
/// for a year, in the opposite direction.</para>
///
/// <para>And it had already happened again. <c>art/castaway.jpg</c> was delivered by #915 — whose own title
/// says so, <i>"the eight posters of the eternally promising, the castaway, and the last unnamed death"</i> —
/// for the one ending in the game that is supposed to be a beginning, and the card went on rendering three
/// lines of text under a stamp. The paperwork agreed with the markup and not with the folder:
/// <c>docs/art-manifest-wrecks.md</c> still filed it under <b>"Still unpainted in this family"</b>. The
/// existing paperwork guard could not see any of that, because it reads one manifest for one phrasing
/// (<c>## … TO PAINT</c>) and asks the question code-first.</para>
///
/// <para>So this test starts from the folder and asks the folder's own question: <b>is there anything in the
/// game that can put this picture on the screen?</b> It is a ratchet, in the shape #663 established — an
/// unreachable canvas that is not on <see cref="KnownOrphans"/> fails, and an entry on
/// <see cref="KnownOrphans"/> that has since been wired fails too, so the excuse list cannot outlive the
/// excuse.</para>
/// </summary>
public sealed class EveryPaintingHasAMomentTests
{
    /// <summary>
    /// Paintings on disk that nothing can show, on purpose, with the reason. Each entry is a debt rather than
    /// a decision; removing one is the fix and adding one wants an issue.
    /// </summary>
    private static readonly Dictionary<string, string> KnownOrphans = new(StringComparer.OrdinalIgnoreCase)
    {
        // The 3D interior renovation (#90) dressed three starboard berths: cabin-tidy, cabin-messy-a and
        // cabin-messy-b. The owner turned CABIN 3 into the MED BAY on 2026-07-18 and the room took
        // ship-med-bay.jpg with it (DeckPlan.cs, the `backdrops` table), which left the second messy cabin a
        // texture for a room that no longer exists. It is NOT a story card and there is no third berth to
        // hang it in, so wiring it would mean inventing a room to justify a file — the opposite of the point.
        ["cabin-messy-b.jpg"] = "#90's third berth became the MED BAY (owner 2026-07-18); the room is gone, the texture is not",
    };

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client", "wwwroot", "art")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            $"could not find src/SpaceSails.Client/wwwroot/art above {AppContext.BaseDirectory}");
    }

    private static string ArtDirectory() =>
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "wwwroot", "art");

    /// <summary>Every source file that could name a painting — the client's markup and code, the shared CSS
    /// and JS, and Core, where #634's law puts the words and the file name together.</summary>
    private static IEnumerable<string> SourceFiles()
    {
        string src = Path.Combine(RepoRoot(), "src");
        return Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f) is ".cs" or ".razor" or ".css" or ".js" or ".html")
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    /// <summary>
    /// What the source can name, in the two shapes the shipped code actually uses.
    ///
    /// <para><b>Whole names</b> — every <c>foo.jpg</c> token anywhere in the source. Deliberately the bare
    /// file name and not <c>art/foo.jpg</c>, because <c>DeathNarration.ArtFile</c> returns
    /// <c>"death-derelict.jpg"</c> and the card prepends the folder; a sweep that insisted on the prefix
    /// would have called ten shipped death frames orphans.</para>
    ///
    /// <para><b>Composed heads</b> — the literal part of an interpolated path, up to the hole:
    /// <c>$"art/treasure-{bodyId}.jpg"</c> gives <c>art/treasure-</c>, and the poster pool and the captain
    /// portraits give the other two. A head is only accepted when it ends in a separator, so a bare
    /// <c>art/</c> can never match the whole folder and turn this guard into one that asserts nothing.</para>
    /// </summary>
    private static (HashSet<string> Whole, List<string> Heads) WhatTheSourceCanName()
    {
        var whole = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var heads = new List<string>();

        var wholeName = new Regex(@"[A-Za-z0-9._-]+\.(?:jpg|jpeg|png|webp|gif|svg)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var composed = new Regex(@"art/([A-Za-z0-9._-]*[-_])(?=[{@$])", RegexOptions.Compiled);

        foreach (string file in SourceFiles())
        {
            string text = File.ReadAllText(file);

            foreach (Match m in wholeName.Matches(text))
            {
                whole.Add(m.Value);
            }

            foreach (Match m in composed.Matches(text))
            {
                heads.Add("art/" + m.Groups[1].Value);
            }
        }

        return (whole, [.. heads.Distinct(StringComparer.OrdinalIgnoreCase)]);
    }

    private static bool IsReachable(string fileName, HashSet<string> whole, List<string> heads) =>
        whole.Contains(fileName)
        || heads.Exists(h => ("art/" + fileName).StartsWith(h, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A sweep that can only say YES proves nothing — this house's fifth bug class, named on 2026-08-02 after
    /// three guards in one afternoon were handed a world that could not tell pass from fail. So: the scan
    /// sees a real folder and a real corpus, it says YES to canvases that are definitely wired in each of the
    /// two shapes it understands, and it says NO to a name nobody has ever painted.
    /// </summary>
    [Fact]
    public void TheSweepCanTellReachedFromUnreached()
    {
        string[] paintings = Directory.GetFiles(ArtDirectory());
        Assert.True(paintings.Length > 100, $"only {paintings.Length} files under wwwroot/art — wrong folder");

        (HashSet<string> whole, List<string> heads) = WhatTheSourceCanName();
        Assert.True(whole.Count > 100, $"the scan found only {whole.Count} art names in src — wrong corpus");
        Assert.NotEmpty(heads);

        // named outright, in Core, beside its own words
        Assert.True(IsReachable("vented-nest-dead.jpg", whole, heads));
        // named without the folder, by DeathNarration.ArtFile, and prefixed by the card
        Assert.True(IsReachable("death-derelict.jpg", whole, heads));
        // reached only through a composed head — $"art/treasure-{bodyId}.jpg"
        Assert.True(IsReachable("treasure-europa.jpg", whole, heads));
        Assert.DoesNotContain("treasure-europa.jpg", whole, StringComparer.OrdinalIgnoreCase);

        // and the half that matters: it can still say no
        Assert.False(IsReachable("no-such-painting-9f3a2b.jpg", whole, heads));
    }

    /// <summary>The ratchet, forward: nothing sits in the folder unreachable unless it is on the list.</summary>
    [Fact]
    public void EveryPaintingIsEitherReachableOrAKnownOrphan()
    {
        (HashSet<string> whole, List<string> heads) = WhatTheSourceCanName();

        List<string> orphans = [.. Directory.GetFiles(ArtDirectory())
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Select(f => f!)
            .Where(f => !IsReachable(f, whole, heads) && !KnownOrphans.ContainsKey(f))
            .Order(StringComparer.OrdinalIgnoreCase)];

        Assert.True(orphans.Count == 0,
            "paintings in wwwroot/art that nothing in src can put on the screen. The onerror-hide law makes "
            + "this silent in both directions, which is why it is asserted rather than noticed: wire each one "
            + "to the moment it was painted for, or file it in KnownOrphans with the reason. "
            + string.Join(", ", orphans));
    }

    /// <summary>The ratchet, backward: an excuse cannot outlive the thing it excused.</summary>
    [Fact]
    public void TheOrphanListShrinksAndNeverRots()
    {
        (HashSet<string> whole, List<string> heads) = WhatTheSourceCanName();

        List<string> stale = [.. KnownOrphans.Keys.Where(f => IsReachable(f, whole, heads))
                                                  .Order(StringComparer.OrdinalIgnoreCase)];

        Assert.True(stale.Count == 0,
            "these paintings are wired now and are still filed as orphans — take them out of KnownOrphans, "
            + $"because a list that keeps a fixed thing on it is how a TODO loses its owner: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// And the one this lane exists for. The castaway ending's painting arrived with #915 and the card that
    /// was supposed to show it went on being three lines of text; the manifest, meanwhile, filed it under
    /// "Still unpainted in this family". Both halves are asserted here, in one place, because either alone
    /// would pass on a lie: the file is on disk, and something in the game names it.
    /// </summary>
    [Fact]
    public void TheCastawayEndingShowsTheBoatThatWasPaintedForIt()
    {
        Assert.True(File.Exists(Path.Combine(ArtDirectory(), "castaway.jpg")));
        Assert.DoesNotContain("castaway.jpg", KnownOrphans.Keys, StringComparer.OrdinalIgnoreCase);

        (HashSet<string> whole, List<string> heads) = WhatTheSourceCanName();
        Assert.True(IsReachable("castaway.jpg", whole, heads));

        // …and it is named where the card's own words are named, not typed into the markup (#634).
        Assert.Equal("art/castaway.jpg", SpaceSails.Core.ShipScuttle.CastawayArt);
    }

    /// <summary>
    /// The paperwork half, done the way the folder forced: <c>StoryArtPresentTests</c> reads one manifest for
    /// one phrasing and could not see that a second manifest was calling a shipped painting unpainted. Every
    /// manifest is read here, and a painted canvas may not be sitting under a heading that says otherwise.
    /// </summary>
    [Fact]
    public void NoManifestFilesAShippedPaintingAsUnpainted()
    {
        string art = ArtDirectory();
        var stale = new List<string>();

        foreach (string manifest in Directory.EnumerateFiles(Path.Combine(RepoRoot(), "docs"), "art-manifest-*.md")
                                             .Order(StringComparer.Ordinal))
        {
            bool unpaintedSection = false;
            foreach (string line in File.ReadAllLines(manifest))
            {
                if (line.StartsWith('#'))
                {
                    unpaintedSection = line.Contains("unpainted", StringComparison.OrdinalIgnoreCase)
                                    || line.Contains("TO PAINT", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!unpaintedSection && !line.Contains("TO PAINT", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (Match m in Regex.Matches(line, @"art/([A-Za-z0-9._-]+\.jpg)"))
                {
                    if (File.Exists(Path.Combine(art, m.Groups[1].Value)))
                    {
                        stale.Add($"{Path.GetFileName(manifest)} → {m.Groups[1].Value}");
                    }
                }
            }
        }

        Assert.True(stale.Count == 0,
            "these paintings are on disk and their manifest still files them as unpainted — the drift that "
            + $"kept the castaway's boat off its own card: {string.Join(", ", stale)}");
    }
}
