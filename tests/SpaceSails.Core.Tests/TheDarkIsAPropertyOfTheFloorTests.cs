using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #708 · DARKNESS IS A FACT A FLOOR STATES ABOUT ITSELF, AND THE HEADLIGHTS ARE THE SWEEPERS' LAMP.
///
/// <para>Two laws, and both of them are about there being one of something. The floor's darkness has one
/// source (<see cref="UndergroundComplex.IsDark"/>) and the boot cheat is an ARGUMENT to it rather than a
/// second answer OR-ed in beside it; the cone has no numbers of its own at all, it is
/// <see cref="InspectionTeam"/>'s lamp said once (§13.18).</para>
/// </summary>
public sealed class TheDarkIsAPropertyOfTheFloorTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",

        // #677 · …and the one rock in the system with GALLERIES under it. Without it every sweep in this
        // file audits a universe where the found band does not exist and passes for the wrong reason —
        // which is the fifth named bug class (a guard handed a world that cannot tell pass from fail).
        UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<(string Body, int Level)> EveryFloor() =>
        Bodies.SelectMany(b => UndergroundComplex.FloorsOf(b).Select(l => (b, l)));

    // ── The floor's own fact ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ONLY FLOORS THAT GO DARK ON THEIR OWN ARE THE ONES NOBODY DUG (#677). Every floor the FACILITY
    /// built — listed or not — keeps its failing light and the instrument-lit look it has always had; every
    /// gallery past the seam has never had a fixture in it.
    ///
    /// <para>This test used to read "nothing the game ships is dark", which was true for exactly as long as
    /// #708's customer had not arrived. It is written as an EQUIVALENCE rather than as a count, because the
    /// two ways this dies are opposite and both silent: the halls quietly losing their darkness, and darkness
    /// quietly spreading onto floors that have lamps. Either shows up here naming the floors.</para>
    /// </summary>
    [Fact]
    public void THEONLYFloorsDarkOfTheirOwnAccordAreTheOnesNobodyDug()
    {
        var wrong = EveryFloor()
            .Where(f => UndergroundComplex.IsDark(f.Body, f.Level)
                != UndergroundComplex.IsFound(f.Body, f.Level))
            .Select(f => $"{f.Body} B{-f.Level} "
                + (UndergroundComplex.IsFound(f.Body, f.Level) ? "(a gallery, and it is LIT)" : "(went dark)"))
            .ToList();

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) disagree with the seam: {string.Join(", ", wrong.Take(12))}");

        // And the sweep has to have SEEN some halls, or it proved only that ordinary floors are lit.
        int galleries = EveryFloor().Count(f => UndergroundComplex.IsFound(f.Body, f.Level));
        Assert.True(galleries > 0, "no site in this sweep has halls under it — this proved half a law.");
    }

    /// <summary>
    /// THE CHEAT PUTS THE FIXTURES OUT AND DOES NOTHING ELSE. Every floor under the ground goes dark;
    /// nothing above it does, because a surface has a sun and darkness is a property of somewhere with a roof
    /// on it.
    ///
    /// <para>This is the guard against the mistake anybody would make: writing the cheat as
    /// <c>_lampsOut || UndergroundComplex.IsDark(…)</c> at the call site. That reads fine, it is one honest
    /// line, and it blacks out the regolith — the captain lands on a moon at noon and cannot see the shuttle
    /// they are standing next to. Transcribed as the sum of the two facts rather than as an argument to the
    /// one ask, it opens with <c>4 above-ground level(s) blacked out by the cheat: 0, 1, 3, 12</c>.</para>
    /// </summary>
    [Fact]
    public void TheCheatIsAnARGUMENTToTheOneAskAndNeverASecondAnswerBesideIt()
    {
        var litUnderground = EveryFloor()
            .Where(f => !UndergroundComplex.IsDark(f.Body, f.Level, lampsOut: true))
            .Select(f => $"{f.Body} B{-f.Level}")
            .ToList();

        Assert.True(litUnderground.Count == 0,
            $"{litUnderground.Count} floor(s) kept their lights through ?dark=1: "
            + string.Join(", ", litUnderground.Take(12)));

        var blackedOutSky = new[] { 0, 1, 3, 12 }
            .Where(level => Bodies.Any(b => UndergroundComplex.IsDark(b, level, lampsOut: true)))
            .ToList();

        Assert.True(blackedOutSky.Count == 0,
            $"{blackedOutSky.Count} above-ground level(s) blacked out by the cheat: "
            + string.Join(", ", blackedOutSky));
    }

    /// <summary>
    /// DARKNESS IS NOT ANY OTHER FACT ABOUT THIS BUILDING. Three rules somebody could reasonably reach for
    /// instead — a floor that cannot be breathed, a floor deep enough to feel like a cave, a floor of the band
    /// nobody listed — are each transcribed here and each proved to be a DIFFERENT law over real floors.
    ///
    /// <para>The day one of them is quietly adopted as the answer, its disagreement count falls to zero and
    /// this goes red naming it. That is the whole job: the found halls must declare darkness because they
    /// were never ours, not because their air ran out or because they are far down.</para>
    /// </summary>
    [Fact]
    public void AndItIsNotDeadAirNorDepthNorTheBandNobodyListed()
    {
        (string Name, Func<string, int, bool> Rule)[] impostors =
        [
            ("a floor that cannot be breathed", (b, l) => !UndergroundComplex.HoldsPressure(b, l)),
            ("a floor deep enough to feel like a cave", (_, l) => l <= -8),
            ("the band nobody listed", UndergroundComplex.IsUnlisted),
        ];

        var sb = new StringBuilder();
        foreach ((string name, Func<string, int, bool> rule) in impostors)
        {
            int disagreements = EveryFloor()
                .Count(f => rule(f.Body, f.Level) != UndergroundComplex.IsDark(f.Body, f.Level, lampsOut: true));

            if (disagreements == 0)
            {
                sb.AppendLine($"  darkness has become indistinguishable from \"{name}\"");
            }
        }

        Assert.True(sb.Length == 0,
            $"a second source of truth has moved in:{Environment.NewLine}{sb}");
    }

    // ── The cone ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SUIT'S HEADLIGHTS ARE THE SWEEPERS' LAMP — not a copy of its numbers, the same kit. Asserted
    /// BEHAVIOURALLY over ten thousand points of open ground rather than by comparing two constants, because
    /// comparing two constants is a test that would still pass if somebody typed <c>20.0</c> into
    /// <see cref="SuitLamp"/> by hand.
    ///
    /// <para>The sweeper is placed in open ground with no walls, which is the only condition under which the
    /// two are supposed to agree: their difference is that a sweeper's cone stops at a bulkhead and the
    /// captain's does not (filed, §13.18).</para>
    ///
    /// <para>Proved able to fail by typing <c>22.0</c> into <see cref="SuitLamp.RangeDu"/> — two du, the sort
    /// of number a tuning pass produces: <c>facing 0.00: (20.9,-14.7) — sweeper cannot see, suit lights</c>.</para>
    /// </summary>
    [Fact]
    public void TheHeadlightsAreTheSAMELampTheSweepersCarryAndNotACopyOfItsNumbers()
    {
        var disagreements = new List<string>();

        foreach (double heading in new[] { 0.0, 1.1, 2.7, -2.0, Math.PI })
        {
            var sweeper = new InspectionTeam.Member(
                "SWEEP-1", 4.0, -3.0, heading, InspectionTeam.Awareness.Sweeping, 0);

            for (int ix = -25; ix <= 25; ix++)
            {
                for (int iy = -20; iy <= 20; iy++)
                {
                    double x = 4.0 + (ix * 1.3), y = -3.0 + (iy * 1.3);
                    bool sees = InspectionTeam.Sees(in sweeper, x, y, walls: null);
                    bool lights = SuitLamp.Lights(4.0, -3.0, heading, x, y, touchRingDu: 0);
                    if (sees != lights && disagreements.Count < 8)
                    {
                        disagreements.Add(
                            $"facing {heading:F2}: ({x:F1},{y:F1}) — sweeper {(sees ? "sees" : "cannot see")}, "
                            + $"suit {(lights ? "lights" : "does not light")}");
                    }
                }
            }
        }

        Assert.True(disagreements.Count == 0,
            $"the two lamps in this game have come apart:{Environment.NewLine}  "
            + string.Join(Environment.NewLine + "  ", disagreements));
    }

    /// <summary>
    /// TURNING YOUR BODY IS AN ACT OF LOOKING. A fixed point at a fixed distance is lit for exactly the
    /// facings that put it inside the cone, and dark for every other — so the arc of headings that shows you
    /// a given wall is the cone's own width and not a degree more.
    /// </summary>
    [Fact]
    public void APointAtArmsLengthPastTheRingIsLitByExactlyTheCONESWorthOfFacings()
    {
        const double atX = 12.0, atY = 0.0;   // dead ahead of a captain at the origin facing 0, well past the ring
        int litHeadings = 0;
        const int samples = 3600;

        for (int i = 0; i < samples; i++)
        {
            double heading = ((double)i / samples * 2 * Math.PI) - Math.PI;
            if (SuitLamp.Lights(0, 0, heading, atX, atY, touchRingDu: 3.0))
            {
                litHeadings++;
            }
        }

        double degreesThatShowIt = litHeadings * 360.0 / samples;
        Assert.True(Math.Abs(degreesThatShowIt - (2 * SuitLamp.ConeHalfAngleDegrees)) < 1.0,
            $"a wall 12 du ahead was visible from {degreesThatShowIt:F1}° of facings; "
            + $"the cone is {2 * SuitLamp.ConeHalfAngleDegrees:F1}° wide");
    }

    /// <summary>
    /// …AND THE RING DOES NOT CARE WHICH WAY YOU FACE. Inside arm's reach the light is the spill off your own
    /// suit, so it is a circle: something touching you is visible from every heading, and a step past the ring
    /// with your back turned is not visible from any of them.
    /// </summary>
    [Fact]
    public void TheRingIsACircleAndTheFirstStepPastItIsGone()
    {
        const double ring = 3.0;

        for (int i = 0; i < 360; i++)
        {
            double bearing = i * Math.PI / 180.0;
            double behind = bearing + Math.PI;      // facing exactly away from it

            (double nx, double ny) = (Math.Cos(bearing) * (ring * 0.95), Math.Sin(bearing) * (ring * 0.95));
            (double fx, double fy) = (Math.Cos(bearing) * (ring * 1.05), Math.Sin(bearing) * (ring * 1.05));

            Assert.True(SuitLamp.Lights(0, 0, behind, nx, ny, ring),
                $"bearing {i}°: inside arm's reach and unlit");
            Assert.False(SuitLamp.Lights(0, 0, behind, fx, fy, ring),
                $"bearing {i}°: a step past arm's reach, back turned, and still lit");
        }
    }
}
