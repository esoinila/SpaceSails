using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · WHERE THE REPOSITORY IS, SAID ONCE.
///
/// <para><b>Why this exists.</b> A great many guards in this suite read a file the game ships — a scenario,
/// a <c>.razor</c>, a stylesheet, a painting, a page of the manual — and every one of them first has to
/// answer the same question: where is the repository, from a test binary buried in
/// <c>bin/Release/net10.0/</c>? On 2026-09-06 that question had <b>seventy-one byte-identical answers</b> in
/// this project alone, one per file, each a thirteen-line private method walking up from
/// <see cref="AppContext.BaseDirectory"/> looking for <c>src/SpaceSails.Client</c>. Seventy-one copies of one
/// sentence is precisely the law-transcribed-at-its-call-sites shape this repository has paid for four times:
/// the day the layout moves, seventy-one files have to agree about it, and nothing in the compiler will say
/// so if seventy of them do.</para>
///
/// <para><b>What it is NOT.</b> It is not a fallback and it is not clever. It walks up and it throws if it
/// runs out of parents, because a guard that silently got the wrong root would read no files, find no
/// offenders and pass forever — this repo's fifth named bug class, arriving through the back door of a path
/// helper.</para>
///
/// <para><b>The anchor is <c>src/SpaceSails.Client</c></b>, which is what all seventy-one copies used. It is
/// a directory that exists in a checkout and in no build output, so the walk cannot stop early inside
/// <c>bin/</c> or <c>obj/</c>. The Core suite keeps its own twin next door — the two assemblies cannot see
/// each other — anchored on <c>src/SpaceSails.Core</c> for the same reason.</para>
///
/// <para><b>There are still other answers in the tree, and they are written down rather than swept.</b> The
/// 2026-09-06 measurement found twenty-eight distinct implementations of this one idea across the test
/// projects, anchored variously on <c>src/SpaceSails.Client</c>, <c>src/SpaceSails.Core</c>,
/// <c>scenarios/</c>, <c>docs/</c>, <c>SpaceSails.slnx</c> and <c>*.sln</c>. Only the byte-identical ones
/// were moved here: a helper that looks for a DIFFERENT landmark is a different helper, however similar the
/// name, and folding it in would be a behaviour change wearing a refactor's clothes. The rest are listed in
/// this lane's pull request as the measured backlog.</para>
/// </summary>
internal static class TestTree
{
    /// <summary>The repository root, found by walking up from the test assembly until
    /// <c>src/SpaceSails.Client</c> is under foot.</summary>
    /// <exception cref="DirectoryNotFoundException">There is no checkout above the test binary.</exception>
    internal static string RepoRoot()
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
}
