using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #719 · <b>THE STAIR IS AN ESCAPE ROUTE, NOT AN ENTRANCE</b> — the half of the second way out that had to
/// be got right before it could ship at all.
///
/// <para>#1074 beat 1's law is that a sealed working stays sealed, and #715's is that a gate past the meter's
/// middle rung wants a face as well as the paper. A stair cut carelessly would be a second road past both of
/// them and past the SEALED row (#590) besides — and §13.5's one earned thing, DEPTH, would become something
/// a captain could buy by walking down some steps.</para>
///
/// <para>Two things make that impossible and both are proved here:</para>
/// <list type="number">
/// <item><b>The pocket.</b> On a ground the Authority has closed, the seal is a leaf at the back of a recess
/// cut into the spine's upper face — the very face the stair's own pocket hangs off. <c>StairShaftAt</c>
/// refuses that end outright, so the stair can never be cut through the one door in the building that is
/// supposed to stay shut.</item>
/// <item><b>The band.</b> Every floor a panel would refuse belongs to a band the building does not admit to,
/// and no floor of such a band carries a stair door at all.</item>
/// </list>
///
/// <para>The third thing is not geometry and cannot be asserted here: the shaft has a console on every listed
/// floor and none inside itself, so nothing can be ridden or walked DOWN it. That is the client's, and
/// <c>TheStairIsAWayHomeTests</c> presses it.</para>
///
/// <para><b>Its own class, and a serialised one</b>, because it installs the stop register —
/// <c>TheProcessWideWritersAreSerialisedTests</c> is the law that says so, and #1108 is what it cost the day
/// four classes had drifted outside it. The rest of #719's guards stay parallel next door.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheStairNeverOpensIntoASealedWorkingTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>How many generated rocks the sweep walks to find grounds a stop order can be posted on. The
    /// same family shape <c>TheStopOrderAtTheDigTests</c> uses and a family of ids this file owns alone, so
    /// no other suite's register can move the ground under it.</summary>
    private const int Probes = 3000;

    private static List<string> Grounds()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"stair-stop-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 20,
            $"only {found.Count} of {Probes} generated grounds could carry a stop order — this proves "
            + "little, and a population of nothing passes every negative law in this file for the wrong "
            + "reason (the fifth named bug class).");
        return found;
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() => StopOrder.Install([]);
    }

    private static IDisposable Stopped(IEnumerable<string> bodies)
    {
        StopOrder.Install([.. bodies]);
        return new Restore();
    }

    /// <summary>
    /// THE STAIR NEVER CUTS THROUGH THE SEAL. Both pockets are cut into the same face of the same corridor at
    /// one of its two blind ends, so this is a real collision and not a hypothetical one: they are placed by
    /// two different rules — the seal takes the WIDEST end, the stair takes the end nothing else is in — and
    /// two rules that happen to agree today is exactly the arrangement this project keeps paying for.
    ///
    /// <para><b>Proven RED</b> by having <c>StairShaftAt</c> hand back the pocket's own spot
    /// (<c>SpecimenRecessAt</c>) — which is exactly the arrangement the finder's exclusion refuses, and NOT
    /// something deleting that one clause can produce today, because the goods car happens to take the same
    /// end. A break that the code can shrug off proves nothing; this one moves the stair:</para>
    /// <code>
    /// 51 of 51 case(s) break the law: the stair never cuts the seal
    ///   stair-stop-ground-41 B7: the seal stands at x=-136.2 and the stair is cut at x=-136.2 — the
    ///   escape route runs through the one door in this building that is supposed to stay shut.
    /// </code>
    /// </summary>
    [Fact]
    public void TheStairIsNeverCutThroughAStopOrdersSeal()
    {
        List<string> grounds = Grounds();
        var bad = new List<string>();
        int seals = 0;

        using (Stopped(grounds))
        {
            foreach (string body in grounds)
            {
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    if (UndergroundComplex.StopSealOn(body, level, Field) is not { } seal)
                    {
                        continue;
                    }
                    seals++;

                    if (UndergroundComplex.StairShaftAt(Field) is not { } at)
                    {
                        continue;
                    }

                    double sealX = (seal.X1 + seal.X2) / 2.0;
                    if (Math.Abs(sealX - at.X) < (2 * UndergroundComplex.DoorHalf) + UndergroundComplex.ShaftClearDu)
                    {
                        bad.Add($"  {body} B{-level}: the seal stands at x={sealX:F1} and the stair is cut "
                            + $"at x={at.X:F1} — the escape route runs through the one door in this "
                            + "building that is supposed to stay shut.");
                    }

                    // …and the stair is not on that floor at all where the order closed the working, which
                    // is a stronger statement than "not through the leaf": the seal is posted on the LISTED
                    // BOTTOM, a floor the building does admit to, so the stair IS there — and it is there at
                    // the other end of the corridor, which is the whole arrangement.
                    if (!UndergroundComplex.HasStairOn(body, level))
                    {
                        bad.Add($"  {body} B{-level}: the working is closed and this floor has no second way "
                            + "out — a stop order is supposed to take the deep working away, not the exit.");
                    }
                }
            }
        }

        Assert.True(seals > 20,
            $"only {seals} sealed floor(s) were walked — the half of this law about the seal was asked of "
            + "nothing.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {seals} case(s) break the law: the stair never cuts the seal");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    /// <summary>
    /// AND THE BAND BELOW A GATE NEVER CARRIES ONE. Whatever the panel refuses — a SEALED row wanting an
    /// authority nobody has issued, an ID CHECK wanting a face, or one of #592's two silences — it refuses on
    /// behalf of a band the building does not admit to. No floor of such a band has a stair door, so there is
    /// nothing in the shaft to arrive at even if a captain could go down it.
    ///
    /// <para><b>Proven RED</b> by relaxing <c>HasStairOn</c> to <c>level &lt; 0</c>:</para>
    /// <code>
    /// 351 of 351 case(s) break the law: the stair never lands in a band a gate is standing in front of
    ///   stair-stop-ground-41 B5: the band below (B9) is one the panel will not offer, and it carries a
    ///   stair door.
    ///   stair-stop-ground-41 B9: the band below (B17) is one the panel will not offer, and it carries a
    ///   stair door.
    /// </code>
    /// </summary>
    [Fact]
    public void TheStairNeverLandsInABandAGateStandsInFrontOf()
    {
        List<string> grounds = Grounds();
        var bad = new List<string>();
        int gates = 0;

        using (Stopped(grounds))
        {
            foreach (string body in grounds)
            {
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    if (UndergroundComplex.NextShaftBelow(body, level) is not { } next)
                    {
                        continue;
                    }

                    int below = UndergroundComplex.BandTop(next);
                    bool refused = UndergroundComplex.IsUnlisted(body, below)
                        || UndergroundComplex.IsFound(body, below)
                        || UndergroundComplex.StopSealsTheGateTo(body, next);
                    if (!refused)
                    {
                        continue;
                    }
                    gates++;

                    if (UndergroundComplex.HasStairOn(body, below))
                    {
                        bad.Add($"  {body} B{-level}: the band below (B{-below}) is one the panel will not "
                            + "offer, and it carries a stair door.");
                    }
                }
            }
        }

        Assert.True(gates > 20, $"only {gates} gated band(s) were walked — this proved little.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"{bad.Count} of {gates} case(s) break the law: the stair never lands in a band a gate is "
                + "standing in front of");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }
}
