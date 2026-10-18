using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The wiring half of "the second line of one breath is said beside the first": the pages cannot be
/// instantiated by a test, so the call sites are pinned in source. Each names the revert watched go red.
///
/// <para><b>RED</b> by turning any of the <c>ShowPulseAlso</c> calls named here back into
/// <c>ShowPulseMessage</c>, by moving the escape line before the liftoff lines, or by dropping the
/// <c>IsPumpRunningLine</c> clause from the pump refresh.</para>
/// </summary>
public sealed class TwoSayingsOneBreathWiringTests
{
    private static string Code(params string[] parts)
    {
        string text = File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", .. parts]));
        text = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(text, "//[^\n]*", " ");
    }

    [Theory]
    [InlineData("Map.SpikeIt.cs", "ShowPulseAlso(p.Outcome == SpikeIt.Outcome.Late")]
    [InlineData("Map.GeocacheSale.cs", "ShowPulseAlso(GeocacheSale.LiftedFirstLine)")]
    [InlineData("Map.Combat.Hunters.cs", "ShowPulseAlso($\"Word's out")]
    [InlineData("Map.Combat.cs", "ShowPulseAlso(compliance == ComplianceState.Stubborn")]
    public void TheFollowingLineIsSaidBesideTheOneBeforeIt(string file, string call) =>
        Assert.Contains(call, Code(file), StringComparison.Ordinal);

    [Fact]
    public void TheEscapeIsSaidAfterTheLiftoffLineAndBesideIt()
    {
        string code = Code("Map.Surface.Liftoff.cs");
        int liftoff = code.IndexOf("Back aboard from", StringComparison.Ordinal);
        int escape = code.IndexOf("ShowPulseAlso(CollectorLanding.EscapedLine)", StringComparison.Ordinal);
        Assert.True(liftoff > 0 && escape > liftoff, "the escape line must come after the liftoff pulse and join it.");
        Assert.DoesNotContain("ShowPulseMessage(CollectorLanding.EscapedLine)", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePumpWarningIsNotWipedByTheCountdown() =>
        Assert.Contains("HullVenting.IsPumpRunningLine(_ventMessage)", Code("Map.Venting.Pumps.Run.cs"), StringComparison.Ordinal);
}
