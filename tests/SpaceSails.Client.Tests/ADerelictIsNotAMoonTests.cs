using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #637 · OCCURRENCES 6 AND 7 OF A MOON CONSTANT GOVERNING A SHIP — and the guard that closes the pattern
/// instead of the two call sites.
///
/// <para><c>MoonSurface.IsSafeAboard</c> asks whether the captain is above the regolith's top rim at
/// y = −20. A derelict's whole deck runs <see cref="WreckLayout.TopY"/> = −9 … <see cref="WreckLayout.BottomY"/>
/// = +9, so <b>every point of every hull satisfies the moon's rule</b>. Anything gated on it aboard a wreck is
/// always-on or always-off, and never wrong enough to notice:</para>
///
/// <list type="number">
/// <item><b>The nerve.</b> <c>onRegolith</c> was false everywhere aboard, so the ambient pressure the whole
/// dread economy runs on never applied inside a hull — <c>?wreck=infested&amp;land=1</c> included. Walk the
/// spine of a haunted ship, in the dark, in vacuum, and the gauge scored it as standing in the shuttle bay.</item>
/// <item><b>The comms.</b> <c>CommsOnsetBias</c> returned its at-the-ship 0.5 at every point of every wreck,
/// so the deep-in-a-dead-hull drop — arguably the best place in the game for one — could never fire.</item>
/// </list>
///
/// <para>The five before them are on record: the regolith tide aboard, the moon barrier clamping the pack
/// outside the hull, the moon spawn point, <c>CaptainBeyondReach</c> (#574) and the suit's air (#621). The
/// cure for a bug that keeps coming back is not a sixth careful call site; it is one function, plus a test
/// that fails when somebody writes a seventh — which is why the last case here reads the live source.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ADerelictIsNotAMoonTests
{
    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius, the value the page passes

    // ── 6 · the nerve: a hull is exposed ground ───────────────────────────────────────────────────────

    [Fact]
    public void EveryMetreOfADerelictShortOfTheLock_IsAwayFromSafety()
    {
        // Walked as a GRID, not sampled: the bug's signature is a predicate satisfied EVERYWHERE, so one
        // probe in the right place proves nothing.
        var bad = new List<string>();

        for (double x = WreckLayout.AftX; x <= WreckLayout.ShuttleLockX - AvatarRadius - 0.5; x += 0.5)
        {
            for (double y = WreckLayout.TopY; y <= WreckLayout.BottomY; y += 0.5)
            {
                if (AwayTeamSide.BackAtTheShuttle(onWreck: true, x, y, AvatarRadius))
                {
                    bad.Add($"  ({x:0.0}, {y:0.0}) — scored as back at the shuttle, so the nerve gauge treats "
                        + "standing here as standing in the bay.");
                }
            }
        }

        Report(bad, "points inside a derelict the nerve would charge nothing for");
    }

    [Fact]
    public void TheAwayTeamsOwnSideOfTheLock_IsStillSafe()
    {
        // Down is not up: a fix that made the WHOLE wreck exposed would tax the captain for standing in
        // their own shuttle, and the give-back beat would never run on a boarding.
        for (double y = WreckLayout.TopY; y <= WreckLayout.BottomY; y += 0.5)
        {
            Assert.True(AwayTeamSide.BackAtTheShuttle(onWreck: true, WreckLayout.BowX - 1, y, AvatarRadius));
        }
    }

    // ── 7 · the comms: the link degrades deeper into a hull ───────────────────────────────────────────

    [Fact]
    public void TheDownlinkDegradesTheDeeperIntoAHullYouWalk()
    {
        double atTheLock = AwayTeamSide.CommsOnsetBias(
            onWreck: true, WreckLayout.ShuttleLockX - AvatarRadius - 0.5, 0, AvatarRadius);
        double amidships = AwayTeamSide.CommsOnsetBias(onWreck: true, 0, 0, AvatarRadius);
        double rightAft = AwayTeamSide.CommsOnsetBias(
            onWreck: true, WreckLayout.AftX, WreckLayout.CauseStation(Derelict.WreckCause.ReactorCascade).Y,
            AvatarRadius);

        // The whole finding in one line: with the moon's rule these were all 0.5.
        Assert.True(atTheLock < amidships,
            $"amidships ({amidships:0.00}×) must be worse than the lock mouth ({atTheLock:0.00}×)");
        Assert.True(amidships < rightAft,
            $"the transom ({rightAft:0.00}×) must be worse than amidships ({amidships:0.00}×)");
        Assert.True(rightAft > 1.9, $"deep aft should approach 2×, not {rightAft:0.00}×");

        // And back through the lock the link is solid again — the 0.5 the surface has always had.
        Assert.Equal(0.5, AwayTeamSide.CommsOnsetBias(onWreck: true, WreckLayout.BowX - 1, 0, AvatarRadius));
    }

    [Fact]
    public void TheMoonsOwnLadderIsUntouched()
    {
        // The surface half of the same function, pinned to the numbers it shipped with: 0.5 up the tube,
        // 1× at the rim, ~2× down by the monolith. A one-function merge must not have moved the moon's line.
        Assert.Equal(0.5,
            AwayTeamSide.CommsOnsetBias(onWreck: false, 0, MoonSurface.SurfaceTopY + 1, AvatarRadius));

        double atTheRim = AwayTeamSide.CommsOnsetBias(
            onWreck: false, 0, MoonSurface.SurfaceTopY - 0.5, AvatarRadius);
        Assert.InRange(atTheRim, 1.0, 1.01);

        double byTheMonolith = AwayTeamSide.CommsOnsetBias(
            onWreck: false, MoonSurface.MonolithX, MoonSurface.MonolithY, AvatarRadius);
        Assert.Equal(2.0, byTheMonolith, 6);
    }

    [Fact]
    public void TheMoonsRimIsWhyBothOfThemHid()
    {
        // Not a fix — the REASON, pinned so nobody re-derives the moon's rule aboard a hull.
        Assert.True(MoonSurface.IsSafeAboard(WreckLayout.TopY));
        Assert.True(MoonSurface.IsSafeAboard(WreckLayout.BottomY));
    }

    // ── The pattern itself ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A RULE ENFORCED ON A FUNCTION A CALLER IS FREE NOT TO USE IS NOT ENFORCED. Both of tonight's bugs
    /// were written by someone reaching past <see cref="AwayTeamSide"/> for the moon's predicate directly, so
    /// the guard has to be on the reaching. Reading live source in a test is an idiom this repo already has
    /// (<c>CssZBandSyncTests</c>, <c>TheDeathCardReadsTheNarrationSeamTests</c>) and for exactly this reason.
    /// </summary>
    [Fact]
    public void NoClientFileOutsideAwayTeamSide_AsksTheMoonWhetherYouAreAboard()
    {
        var offenders = new List<string>();

        foreach (string path in Directory.EnumerateFiles(ClientSourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(path);
            if (name is "AwayTeamSide.cs" or "MoonSurface.cs")
            {
                continue;   // the one place allowed to ask, and the place that answers
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                // A comment EXPLAINING the rule is not the rule being broken — every one of these call sites
                // has a paragraph above it saying why it must not be written this way.
                string code = Regex.Replace(lines[i], @"//.*$", string.Empty);
                if (code.Contains("MoonSurface.IsSafeAboard", StringComparison.Ordinal))
                {
                    offenders.Add($"  {name}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Report(offenders,
            "client call sites asking the MOON whether the captain is aboard. Aboard a derelict the answer "
            + "is YES at every point of every hull. Use AwayTeamSide.BackAtTheShuttle(OnWreck, x, y, r)");
    }

    /// <summary>The client's source tree, found by walking up from the test binary — the same idiom
    /// <c>TheDeathCardReadsTheNarrationSeamTests</c> uses.</summary>
    private static string ClientSourceRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                string candidate = Path.Combine(dir, "src", "SpaceSails.Client");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("src/SpaceSails.Client not found above " + AppContext.BaseDirectory);
        }
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
