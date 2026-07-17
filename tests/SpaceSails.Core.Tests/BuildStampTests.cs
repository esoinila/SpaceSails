namespace SpaceSails.Core.Tests;

/// <summary>
/// The version stamp (#254): the MSBuild target injects a git short SHA + build timestamp into
/// <see cref="BuildStamp"/> at compile time, and the app shows it to kill build-ghosting at a glance.
/// These guard the seam — the constants are populated and the one-line display parses to the promised
/// shape (<c>build &lt;sha&gt; · &lt;yyyy-MM-dd HH:mm&gt;</c>) — without pinning the volatile values.
/// </summary>
public class BuildStampTests
{
    [Fact]
    public void CommitSha_IsNonEmpty()
    {
        // Either a real short SHA, or the "dev" fallback when git is unavailable — never blank.
        Assert.False(string.IsNullOrWhiteSpace(BuildStamp.CommitSha));
    }

    [Fact]
    public void BuildTimeUtc_ParsesAsAMinutePrecisionTimestamp()
    {
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$", BuildStamp.BuildTimeUtc);
    }

    [Fact]
    public void Display_CarriesBothPartsInTheAdvertisedShape()
    {
        // e.g. "build d957fb4 · 2026-07-17 21:40"
        Assert.Matches(@"^build \S+ · \d{4}-\d{2}-\d{2} \d{2}:\d{2}$", BuildStamp.Display);
        Assert.Contains(BuildStamp.CommitSha, BuildStamp.Display);
        Assert.Contains(BuildStamp.BuildTimeUtc, BuildStamp.Display);
    }
}
