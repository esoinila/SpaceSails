namespace SpaceSails.Core;

/// <summary>
/// The compile-time build stamp: the git short SHA and the UTC timestamp of the build, injected by
/// an MSBuild target (see SpaceSails.Core.csproj → <c>GenerateBuildStamp</c>) into the generated
/// partial in <c>obj/…/BuildStamp.g.cs</c>. It exists to kill "am I even looking at the new build?"
/// ghosting — one glance at the footer tells you which commit is live.
///
/// When git is unavailable (a source drop, a checkout with no .git, a machine without git on PATH)
/// the SHA falls back to <c>"dev"</c> so the build never fails on account of the stamp.
/// </summary>
public static partial class BuildStamp
{
    /// <summary>
    /// The one-line stamp for display, e.g. <c>build d957fb4 · 2026-07-17 21:40</c>.
    /// </summary>
    public static string Display => $"build {CommitSha} · {BuildTimeUtc}";
}
