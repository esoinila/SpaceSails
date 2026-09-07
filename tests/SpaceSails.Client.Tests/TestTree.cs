using System;
using System.IO;
using System.Reflection;
using System.Threading;
using SpaceSails.Contracts;
using SpaceSails.Core;

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

    /// <summary>
    /// #251 · THE SHIPPING SKY, PARSED ONCE PER ASSEMBLY.
    ///
    /// <para>The same measurement that found seventy-one copies of <see cref="RepoRoot"/> found the second
    /// half of the same habit: thirty classes in this project each held their own
    /// <c>Lazy&lt;ScenarioDefinition&gt;</c> over <c>scenarios/sol.json</c>, and each one read and parsed
    /// that file from disk. Nothing in it changes between them — it is the file the game ships — so every
    /// parse after the first bought exactly the object the previous one had already built.</para>
    ///
    /// <para><b>Why one shared object is safe.</b> Sharing a parse across classes xUnit runs in PARALLEL is
    /// only a refactor if the thing shared cannot be written to, and this one cannot:
    /// <see cref="ScenarioDefinition"/> and every type reachable from it —
    /// <see cref="TrafficDefinition"/>, <see cref="RouteDefinition"/>,
    /// <see cref="PodLauncherDefinition"/>, <see cref="StreamDefinition"/>,
    /// <see cref="BodyDefinition"/>, <see cref="AtmosphereDefinition"/> — is a <c>sealed record</c> whose
    /// every property is <c>init</c>-only and whose every collection is exposed as
    /// <see cref="System.Collections.Generic.IReadOnlyList{T}"/>. Neither suite nor <c>src/</c> holds a cast
    /// or a reflective write that would reach past that; the client's own cheat bodies are appended with a
    /// <c>with</c> expression over a fresh list (<c>Map.Sim.World.Build.AppendTheBodiesTheCheatsAskFor</c>),
    /// which is a copy and leaves this instance untouched. The full audit is in the lane's pull request.</para>
    ///
    /// <para><see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> is named rather than left to the
    /// default because that is the whole point: one parse, whichever class gets here first, and every other
    /// class blocked until it is done rather than racing it.</para>
    ///
    /// <para>The path is the CHECKOUT's copy, via <see cref="RepoRoot"/>, because that is what all thirty
    /// copies read: this project — unlike the Core suite next door — does not copy <c>scenarios/</c> beside
    /// its test binary, so there is no other copy to read.</para>
    /// </summary>
    private static readonly Lazy<ScenarioDefinition> TheShippingSky = new(
        () => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary><c>scenarios/sol.json</c>, the sky the game ships, parsed once for the whole assembly.</summary>
    internal static ScenarioDefinition Sol => TheShippingSky.Value;

    // ── #251 · THE THREE REACHES, NAMED ONCE ──────────────────────────────────────────────────────────
    //
    // WHAT THE MEASUREMENT FOUND. This project reaches into the page's private state constantly — that is
    // what a guard over a Blazor component has to do — and on 2026-09-07 it did so through ONE HUNDRED
    // `const BindingFlags Hidden` declarations, spread over three distinct values and four spellings
    // (`private const`, bare `const`, `internal const`, and one `private static readonly`), plus three more
    // holding one of the same three values under another name (`Members` twice, `Anything` once). A hundred
    // and three spellings of three ideas is the law-transcribed-at-its-call-sites shape RepoRoot() had.
    //
    // WHAT WAS DONE, AND WHAT DELIBERATELY WAS NOT. Only the VALUE moved. Every file keeps its own
    // `Hidden` — the name all 833 of its mentions in this project spell — and that name is now an alias for
    // one of the three constants below, so a reader who wants to know what `Hidden` reaches has one place to
    // look, and the day a fourth reach is wanted there is one place to add it. Four constants are LEFT
    // ALONE, and they are left alone for the same reason the sweep happened: they are not copies of these.
    // `DeskBench.Shared` and `HerChainOfOwnersTests.Constants` are Static|NonPublic|Public — a reach nothing
    // else in the tree asks for, twice, under two names; and the five `const BindingFlags Public` in
    // Core.Tests carry DeclaredOnly, which asks a different question (what this type declares, not what it
    // has) and belongs to the other assembly.
    //
    // What is NOT lifted is the reflection HELPERS these flags are passed to: `Get`, `Set`, `Field`, `Read`
    // and `Invoke` are 273 declarations over 108 distinct bodies (the largest single agreement is 26), so a
    // sweep of them would leave one shared helper plus a hundred private ones — the half-sweep #1165 warned
    // against. The full measurement is in this lane's pull request.
    //
    // The names say the REACH rather than the flags, because that is the question a reader arrives with.

    /// <summary>Private instance members only — the narrowest of the three, and the right one for a guard
    /// that means to read a component's own <c>_field</c> and would rather miss than silently pick up a
    /// public property of the same name. 27 declarations in 25 files.</summary>
    internal const BindingFlags PrivateOnAnInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>Any instance member, public or not. The house default: most guards here want the field
    /// whatever its accessibility, because whether a member is private is the page's business and not the
    /// law's. 48 declarations.</summary>
    internal const BindingFlags AnythingOnAnInstance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Any member at all — instance or static, public or not. What a guard needs when the thing it
    /// is reaching for is a nested type, a static table or a helper the page keeps beside the state. 28
    /// declarations, three of which spelled it <c>Members</c> or <c>Anything</c>.</summary>
    internal const BindingFlags AnythingAtAll =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
}
