namespace SpaceSails.Core.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using SpaceSails.Core;

/// <summary>
/// #637 · THE NERVE LEDGER'S WORDS, NOW THAT THEY ARE READ ON A STEEL DECK.
///
/// <para>Until #637 the gauge could only ever run on a moon: a derelict's whole deck satisfied
/// <c>MoonSurface.IsSafeAboard</c>, so nothing inside a hull cost a pip and no <see cref="NervePips.Cause"/>
/// label ever reached a player standing in one. Fixing that made every one of these lines reachable aboard
/// a wreck — where there is no regolith, no tube, and no sky.</para>
///
/// <para><c>Cause.Cornered</c> read <i>"cornered — no lane to the tube"</i>, which is the away team's
/// geometry told to a captain whose way home is the shuttle's own lock. Caught the same afternoon the fix
/// landed, which is the point: a fix that makes dead prose reachable inherits responsibility for it.</para>
/// </summary>
public class TheNerveGaugeNowRunsInTwoWorldsTests
{
    /// <summary>Fixtures that exist in exactly ONE of the two places the gauge now runs. A label naming any
    /// of them is correct in one world and nonsense in the other.</summary>
    private static readonly string[] OneWorldOnly =
    [
        "tube",       // the ship's tube down to a moon; a wreck has a shuttle lock
        "regolith",   // there is no dust on a steel deck
        "the ground", // nor any ground
        "the sky",
        "the spine",  // and no spine corridor on a moon
        "the hull",
    ];

    [Fact]
    public void NoNerveLabelNamesAFixtureOnlyOneWorldHas()
    {
        var wrong = new List<string>();

        foreach (NervePips.Cause cause in Enum.GetValues<NervePips.Cause>())
        {
            string label = NervePips.Name(cause);
            if (label.Length == 0)
            {
                continue;
            }

            foreach (string fixture in OneWorldOnly)
            {
                if (label.Contains(fixture, StringComparison.OrdinalIgnoreCase))
                {
                    wrong.Add($"  {cause} — \"{label}\" names \"{fixture}\", which only one of the two "
                        + "places the nerve gauge runs actually has. Since #637 this line is reachable "
                        + "aboard a derelict as well as on a moon.");
                }
            }
        }

        Report(wrong, "nerve-ledger labels that are true in one world and nonsense in the other");
    }

    [Fact]
    public void EveryCauseThatCanFireStillSaysSomething()
    {
        // The replacement must not have emptied one. The ledger's whole deliverable (#480) is that every
        // pip that moved says why.
        foreach (NervePips.Cause cause in Enum.GetValues<NervePips.Cause>())
        {
            Assert.False(string.IsNullOrWhiteSpace(NervePips.Name(cause)), $"{cause} has no words");
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
        foreach (string line in bad)
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
