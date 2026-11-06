using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Client.Pages.Stations;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Round 2 · test user U04 (Rook) · #1418. The page cannot be stood up in a unit test for these beats, so the
/// page-bound facts read the SHIPPING source for the call that matters, and the pure ones call the helper.
/// </summary>
public sealed class U04RoundTwoClientTests
{
    private static string Pages(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", .. parts]));

    /// <summary>The source of one method in a file, from its signature to its matching brace.</summary>
    private static string Body(string source, string method)
    {
        Match sig = Regex.Match(source, @"\bvoid\s+" + Regex.Escape(method) + @"\s*\(");
        Assert.True(sig.Success, method + " not found");
        int open = source.IndexOf('{', sig.Index);
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') { depth++; }
            else if (source[i] == '}' && --depth == 0) { return source[open..(i + 1)]; }
        }

        throw new InvalidOperationException("unbalanced");
    }

    /// <summary>
    /// U04-F05 · The OnceEver plate rises where a round LEAVES the barrel, never at the lock a scrub can undo.
    /// <b>Proven RED</b> by putting the call back in <c>ArmFire</c> (and removing it from <c>CheckLockedFire</c>).
    /// </summary>
    [Fact]
    public void TheFirstRoundPlateRisesAtRoundAwayNotAtBarrelLocked()
    {
        string fire = Pages("Map.Combat.FireControl.Fire.cs");
        const string Raise = "StoryBeats.Beat.FirstShotFired";

        Assert.DoesNotContain(Raise, Body(fire, "ArmFire"), StringComparison.Ordinal);
        Assert.DoesNotContain(Raise, Body(fire, "CancelFiringSolution"), StringComparison.Ordinal);
        string away = Body(fire, "CheckLockedFire");
        Assert.Contains(Raise, away, StringComparison.Ordinal);
        Assert.True(
            away.IndexOf("ROUND AWAY", StringComparison.Ordinal) < away.IndexOf(Raise, StringComparison.Ordinal),
            "the plate must follow the round leaving the barrel");
    }

    /// <summary>U04-F17 · The headline's verb is the sign of the range rate. <b>Proven RED</b> by hard-coding "closing".</summary>
    [Theory]
    [InlineData(4340.0, "closing")]
    [InlineData(0.0, "closing")]
    [InlineData(-4340.0, "opening")]
    public void TheHeadlineReadsTheSignOfTheRangeRate(double closingMps, string verb) =>
        Assert.Equal(verb, WarRoom.HunterHeadlineVerb(closingMps));

    /// <summary>U04-F17 · ...and both headlines on the desk are wired to it, not to the word "closing".</summary>
    [Fact]
    public void BothHeadlinesAskTheHelper()
    {
        string razor = Pages("Stations", "WarRoom.razor");
        Assert.Contains("@nhCompact.Callsign @HunterHeadlineVerb(nhCompact.ClosingSpeed)", razor, StringComparison.Ordinal);
        Assert.Contains("@nh.Callsign @HunterHeadlineVerb(nh.ClosingSpeed)", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("Callsign closing —", razor, StringComparison.Ordinal);
    }

    /// <summary>U04-F19 · A time already gone reads "0 min", never "-0 min" or "-1 min".</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(-20.0)]
    [InlineData(-90.0)]
    [InlineData(-5000.0)]
    public void ATimeThatHasPassedReadsZero(double seconds)
    {
        string said = WarRoom.FormatDuration(seconds);
        Assert.DoesNotContain("-", said, StringComparison.Ordinal);
        Assert.Equal("0 min", said);
    }

    /// <summary>U04-F19 · A round fired at a hunter is listed by her callsign: the name lookup asks the hunter
    /// roster before it gives up and prints the raw id. <b>Proven RED</b> by dropping the roster clause.</summary>
    [Fact]
    public void ARoundAtAHunterIsListedByHerCallsign()
    {
        string npc = Pages("Map.Npc.cs");
        Match name = Regex.Match(npc, @"string NpcName\(string id\)[^;]*;", RegexOptions.Singleline);
        Assert.True(name.Success);
        Assert.Contains("_hunters", name.Value, StringComparison.Ordinal);
    }

    /// <summary>U04-F16 · The desk gives the collector no gender (her warrant says "she").</summary>
    [Fact]
    public void TheDeskDoesNotGiveTheCollectorAGender()
    {
        string razor = Pages("Stations", "WarRoom.razor");
        Assert.DoesNotContain("his catch envelope", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("he misses this pass", razor, StringComparison.Ordinal);
    }

    /// <summary>U04-F22 · A greyed move says why on the card itself, not only in its title.</summary>
    [Fact]
    public void AGreyedMoveSaysWhyOnTheCard()
    {
        string card = Pages("Map", "ViewObjectCard.razor");
        Assert.Matches(@"<div class=""reveal-outcome"">@refused\.Label — @TheStopMoveRefusal\(refused\)</div>", card);
    }
}
