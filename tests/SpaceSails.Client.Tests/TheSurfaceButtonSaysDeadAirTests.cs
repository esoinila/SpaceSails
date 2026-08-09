using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #802 · THE CLIENT HALF OF "THE SURFACE IS VACUUM". Core now answers the SURFACE row off the one pressure
/// fact (<see cref="UndergroundComplex.HoldsPressure"/>); this is the guard that the panel on screen is still
/// only DRAWING that answer.
///
/// <para>The fix would be worth nothing if <c>Map.razor</c> ever decided for itself — a
/// <c>stop.Level == 0 ? "air"</c> in the loop would put the lie back exactly where it was, with a green Core
/// suite underneath it. So the block is read and asked two things: that the air word turns on
/// <c>stop.Pressurised</c> and on nothing else, and that no row in it is special-cased by level or by
/// name.</para>
///
/// <para>Owner, 2026-08-09: <i>"the surface should be vacuum / unbreathable… being unbreathable makes the
/// breathing so much more scary."</i></para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSurfaceButtonSaysDeadAirTests
{
    private static string RepoRoot()
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

    private static string PanelBlock()
    {
        string razor = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));
        int start = razor.IndexOf("@if (_showLiftPanel", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the lift panel block this guard knows how to find.");
        int end = razor.IndexOf("_lockedDoor is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's lift panel block no longer ends where this guard expects.");
        return razor[start..end];
    }

    [Fact]
    public void TheAirWordOnAButtonTurnsOnCoresAnswerAndNothingElse()
    {
        string panel = PanelBlock();

        Assert.True(panel.Contains("else if (stop.Pressurised)", StringComparison.Ordinal),
            "the panel no longer branches its air word on stop.Pressurised — whatever it branches on now " +
            "is a second answer to a question Core owns (#802).");

        // Every mention of air in the block has to be inside that branch's two arms. If the word appears
        // anywhere a level or a name could reach it, the row can lie again.
        Assert.False(panel.Contains("stop.Level == 0", StringComparison.Ordinal),
            "the panel special-cases the SURFACE row by level — that is where `Pressurised: true` lived " +
            "(#802), and a client literal is the same bug with a different postcode.");
        Assert.False(panel.Contains("\"SURFACE\"", StringComparison.Ordinal),
            "the panel names the SURFACE row itself; the rows and their air come from " +
            "UndergroundComplex.LiftPanel and the client only draws them.");
    }

    [Fact]
    public void TheSURFACERowDrawsAsADEADStopOnEveryBody()
    {
        // The class the CSS greys, and the title the cursor rests on, both hang off the same bit — so the
        // one assertion Core can make (Pressurised is false up top) is the one the screen shows.
        string panel = PanelBlock();
        Assert.True(panel.Contains("(stop.Pressurised ? \"\" : \" dead\")", StringComparison.Ordinal),
            "the airless class no longer comes off stop.Pressurised (#802).");
        Assert.True(
            panel.Contains("stop.Pressurised ? \"holds pressure\" : \"dead air — the tank runs\"",
                StringComparison.Ordinal),
            "the button's title no longer comes off stop.Pressurised (#802).");

        foreach (string body in new[]
                 {
                     "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda",
                     "triton", "the-clinker", "secret-lab-site", "secret-lab-site-unlisted",
                     UndergroundComplex.FoundBandCheatSiteId,
                 })
        {
            UndergroundComplex.LiftStop surface =
                Assert.Single(UndergroundComplex.LiftPanel(body, -1, []), s => s.Level == 0);
            Assert.False(surface.Pressurised,
                $"{body}: the SURFACE button would draw '🫁 air' and title itself 'holds pressure'.");
        }
    }
}
