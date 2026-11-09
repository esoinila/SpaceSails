using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// U06-F05 (#1420) · ONE NUMBER ON SCREEN AT A TIME, and only a live instrument may carry it.
///
/// <para>One tank, three air figures: the card said 1h09, the strip said RESERVE 13m and the big counter
/// said O2 0:31 — all off the same <c>AirSeconds</c>, in two units and at two moments (the card's face is
/// frozen when it is raised, the gauge keeps counting). Fable's ruling: while the #573 real-seconds counter
/// hangs over the captain the gauge drops its figure, and cards and one-shot pulses never quote one.</para>
/// </summary>
public sealed class OneFigureOnTheGlassTests
{
    private static readonly Regex Digit = new(@"\d", RegexOptions.Compiled);

    /// <summary>Every digit-bearing air string the glass can carry at one instant on a floor with no
    /// breathable air: the strip's Readout and, when it hangs over the captain, the big counter — which the
    /// client formats as <c>O2 m:ss</c> off real seconds (Map.Surface.Hud.cs, Map.Station.cs).</summary>
    private static List<string> LiveAirStrings(double air, double distanceHome)
    {
        var live = new List<string>();
        string readout = SuitAir.Readout(air, distanceHome, SuitAir.Supply.Tanks);
        if (Digit.IsMatch(readout.Replace("▼", "")))
        {
            live.Add(readout);
        }
        if (SuitAir.CountdownShows(air, distanceHome, SuitAir.Supply.Tanks))
        {
            live.Add($"O2 {(int)(air / 60)}:{(int)(air % 60):00}");
        }
        return live;
    }

    [Fact]
    public void AtMostOneDigitBearingAirStringIsLiveInAnyFrameOfTheWalkDown()
    {
        var bad = new List<string>();
        int framesWithCounter = 0;

        // 420 play-seconds down to nothing, from near the tube and from far out, a quarter-second a frame.
        foreach (double distanceHome in new[] { 0.0, 60.0, 400.0 })
        {
            for (double air = 420; air >= 0; air -= 0.25)
            {
                List<string> live = LiveAirStrings(air, distanceHome);
                if (live.Count > 1)
                {
                    bad.Add($"  air {air:F2}s, {distanceHome:F0} du home: {string.Join("  |  ", live)}");
                }
                if (live.Any(l => l.StartsWith("O2 ", StringComparison.Ordinal)))
                {
                    framesWithCounter++;
                }
            }
        }

        // THE WORLD CAN TELL PASS FROM FAIL: the sweep must actually enter the counter's territory, or
        // "at most one" holds vacuously.
        Assert.True(framesWithCounter > 100, "the walk never reached the big counter — the guard proves nothing.");
        Assert.True(bad.Count == 0,
            "two air figures on screen at once:\n" + string.Join("\n", bad.Take(12)));
    }

    [Fact]
    public void TheCountdownStateNamesTheTroubleAndCarriesNoFigure()
    {
        double reserve = SuitAir.ReserveSeconds * 0.5;
        Assert.Equal("AIR ▼ RESERVE — the count is on you.",
            SuitAir.Readout(reserve, 0, SuitAir.Supply.Tanks));
        Assert.Equal("AIR ▼ PAST THE LINE — the count is on you.",
            SuitAir.Readout(300, SuitAir.WalkSpeedDu * 600, SuitAir.Supply.Tanks));
        // RunningLow but not past the line: a short walk against a low tank.
        double air = SuitAir.TankSeconds * SuitAir.LowAirFraction * 0.9;
        double dist = SuitAir.WalkSpeedDu * air * 0.6;
        Assert.True(SuitAir.RunningLow(air, dist) && !SuitAir.PastPointOfNoReturn(air, dist));
        Assert.Equal("AIR ▼ LOW — the count is on you.", SuitAir.Readout(air, dist, SuitAir.Supply.Tanks));
    }

    [Fact]
    public void TheOneShotLinesQuoteNoNumberOfAirLeft()
    {
        Assert.DoesNotMatch(@"\d", SuitAir.ReserveEngagedLine);
        double air = SuitAir.TankSeconds * SuitAir.LowAirFraction * 0.9;
        Assert.StartsWith("🫁 AIR LOW — the last third of the tank. ", SuitAir.LowAirWarning(air, 50));
        Assert.DoesNotMatch(@"\d+h\d\d", SuitAir.LowAirWarning(air, 50));
    }

    [Fact]
    public void NoCallerOfTheFictionClockRemainsOutsideReadout()
    {
        string src = Path.Combine(TestTree.RepoRoot(), "src");
        var call = new Regex(@"(?<![A-Za-z0-9_])(?:SuitAir\.)?(?:SuitClock|Clock)\(", RegexOptions.Compiled);
        var offenders = new List<string>();

        foreach (string file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                              && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            string name = Path.GetFileName(file);
            bool inSuitAir = name.StartsWith("SuitAir", StringComparison.Ordinal);
            string[] lines = File.ReadAllLines(file);
            int readoutStart = Array.FindIndex(lines, l => l.Contains("public static string Readout(", StringComparison.Ordinal));
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("///")
                    || line.Contains("public static string"))
                {
                    continue;
                }
                foreach (Match m in call.Matches(line))
                {
                    bool qualified = m.Value.StartsWith("SuitAir.", StringComparison.Ordinal);
                    if (!qualified && !inSuitAir)
                    {
                        continue; // some other class's own Clock — not the suit's
                    }
                    bool inReadout = name == "SuitAir.Supply.cs" && readoutStart >= 0 && i > readoutStart;
                    bool clockInsideClock = name == "SuitAir.cs" && m.Value.StartsWith("SuitClock", StringComparison.Ordinal);
                    if (!inReadout && !clockInsideClock)
                    {
                        offenders.Add($"{name}:{i + 1}  {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "something other than SuitAir.Readout prints the fiction clock:\n" + string.Join("\n", offenders));
    }
}
