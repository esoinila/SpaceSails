using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #605 · WHICH FLOOR IS THIS? Owner, riding between floors cut from identical bones:
///
/// <para><i>"Let's like change the wall colors on different floors... now they look too same"</i> · <i>"We
/// could use something like star trek og or Babylon 5 colors for different purposes ... command, medical,
/// so fourth"</i> · <i>"It is the where-am-I question answer when you come with the elevator so it is like
/// the most important thing to see"</i> · <i>"sometimes people get off the elevator at wrong floor so there
/// is this instinct to always check that the floor is correct"</i></para>
///
/// <para>That last one is the design in a sentence: the plate exists to answer a reflex people already have.
/// These pin the two halves of the answer — the LIVERY (which department) and the PLATE (which depth).</para>
/// </summary>
public sealed class TheFloorTellsYouWhereYouAreTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    [Fact]
    public void TheLiveryBelongsToTheDEPARTMENTAndNotToTheFloorNumber()
    {
        // The call that makes this a language rather than decoration (spec §11). A colour per floor would be
        // pretty and tell you nothing; a colour per department means two ADMINISTRATION floors nine levels
        // apart look alike BECAUSE THEY ARE ALIKE, and the captain learns the building.
        foreach (string body in Bodies)
        {
            var byDepartment = new Dictionary<string, BodyPalette.Ink>(StringComparer.Ordinal);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.LiveryFor(body, level) is not { } ink)
                {
                    continue;   // an unlisted floor is bare on purpose — see below
                }
                string dept = UndergroundComplex.DepartmentOf(body, level);
                if (byDepartment.TryGetValue(dept, out BodyPalette.Ink seen))
                {
                    Assert.Equal(seen, ink);
                }
                else
                {
                    byDepartment[dept] = ink;
                }
            }
        }
    }

    [Fact]
    public void NoTwoDepartmentsShareAColour()
    {
        // If two departments read alike the language has a homonym in it, and the whole point was telling
        // floors apart at a glance.
        var seen = new Dictionary<BodyPalette.Ink, string>();
        for (int level = -1; level >= -UndergroundComplex.Departments.Length; level--)
        {
            BodyPalette.Ink ink = UndergroundComplex.LiveryFor("luna", level)!.Value;
            string dept = UndergroundComplex.DepartmentOf("luna", level);
            Assert.False(seen.TryGetValue(ink, out string? other),
                $"{dept} and {other} are painted the same colour.");
            seen[ink] = dept;
        }
        Assert.Equal(UndergroundComplex.Departments.Length, seen.Count);
    }

    [Fact]
    public void ConsecutiveFloorsNeverLookAlike()
    {
        // The complaint that started this: "now they look too same". Two floors you ride between must not
        // read as the same place, so neighbours must differ — which the department cycle gives for free, and
        // this is the guard that notices if the cycle length ever collides with the band length.
        foreach (string body in Bodies)
        {
            var floors = UndergroundComplex.FloorsOf(body).ToList();
            for (int i = 1; i < floors.Count; i++)
            {
                BodyPalette.Ink? a = UndergroundComplex.LiveryFor(body, floors[i - 1]);
                BodyPalette.Ink? b = UndergroundComplex.LiveryFor(body, floors[i]);
                if (a is null || b is null)
                {
                    continue;   // the bare band is its own signal
                }
                Assert.NotEqual(a, b);
            }
        }
    }

    [Fact]
    public void TheFloorNobodyListedIsLeftBARE()
    {
        // #592 · A livery is something a department paints on its own corridor. Those floors have no
        // department and no plate, so the concrete is unpainted — and the ABSENCE is the tell, which costs
        // not one word of narration and cannot leak the secret early because you have to be standing on it.
        foreach (string body in Bodies.Where(UndergroundComplex.HasUnlistedBand))
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool unlisted = UndergroundComplex.IsUnlisted(body, level);
                Assert.Equal(unlisted, UndergroundComplex.LiveryFor(body, level) is null);
            }
        }
    }

    [Fact]
    public void TheBuildingSaysItsNameOnlyWhereYouENTERIt()
    {
        // #694 · Owner, on B11 of a deep site: "every floor has the text 'The Clinic' on it. Some kind of
        // artifact?" It was not an artifact — the facility plate simply drew on every floor — and his
        // reaction is the finding: a name repeated identically twenty floors deep reads as a rendering bug,
        // adds nothing after the lobby, and spends the one place where it would have been a story.
        //
        // The law: the plate falls on the floors you ENTER the building by. B1, where you arrive from the
        // surface; and the unlisted band's own shaft head, where it names a DIFFERENT Kind from the twelve
        // floors above it. Nowhere else — including B5 and B9, which are shaft heads and not doorways.
        var wrong = new List<string>();
        foreach (string body in Sweep())
        {
            int unlistedLobby = UndergroundComplex.HasUnlistedBand(body)
                ? UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body))
                : int.MinValue;

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool want = level == -1 || level == unlistedLobby;
                bool got = UndergroundComplex.ShowsFacilityPlate(body, level);
                if (want != got)
                {
                    wrong.Add(
                        $"  {body} B{-level}: plate {(got ? "DRAWN" : "absent")}, wanted " +
                        $"{(want ? "DRAWN" : "absent")} — {UndergroundComplex.TitleOf(UndergroundComplex.KindOn(body, level))}");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) disagree with the lobby law:\n{string.Join("\n", wrong.Take(24))}");

        // And nothing above ground: the surface is not a floor of the facility.
        foreach (string body in Sweep())
        {
            foreach (int above in new[] { 0, 1, 5 })
            {
                Assert.False(UndergroundComplex.ShowsFacilityPlate(body, above),
                    $"{body}: the facility plate reached level {above}, which is outdoors.");
            }
        }
    }

    [Fact]
    public void TheUnlistedLOBBYIsTheOnlyPlateThatContradictsTheOneAbove()
    {
        // Why the second floor is worth the exception at all. Everywhere the plate draws twice, the two
        // plates must say DIFFERENT things — otherwise it is the same wallpaper problem two floors apart.
        // "▣ THE CLINIC" first seen under twelve floors of RETENTION 40 YR is #592's whole arithmetic
        // delivered by one sign, and KindOn already guarantees the words differ; this pins that the plate
        // draws exactly where that guarantee pays.
        foreach (string body in Sweep().Where(UndergroundComplex.HasUnlistedBand))
        {
            var plated = UndergroundComplex.FloorsOf(body)
                .Where(l => UndergroundComplex.ShowsFacilityPlate(body, l))
                .ToList();

            Assert.Equal(2, plated.Count);
            Assert.NotEqual(
                UndergroundComplex.TitleOf(UndergroundComplex.KindOn(body, plated[0])),
                UndergroundComplex.TitleOf(UndergroundComplex.KindOn(body, plated[1])));
            Assert.True(UndergroundComplex.IsUnlisted(body, plated[1]));
        }
    }

    [Fact]
    public void TheHeadOfficeNamesItselfONCEAndTakesNoException()
    {
        // #411 · Twenty-four listed floors and, by HasUnlistedBand, nothing under them — so HQ falls out of
        // the same law with exactly one plate, in its own lobby, and needs no special case in the renderer
        // or here. It is also better in character: the head office does not have to keep telling you where
        // you are.
        string hq = KaamosLore.IceMoonBodyId;
        Assert.Equal(UndergroundComplex.Kind.HeadOffice, UndergroundComplex.KindFor(hq));
        Assert.False(UndergroundComplex.HasUnlistedBand(hq));

        var plated = UndergroundComplex.FloorsOf(hq)
            .Where(l => UndergroundComplex.ShowsFacilityPlate(hq, l))
            .ToList();
        Assert.Equal([-1], plated);
        Assert.Equal("▣ THE HEAD OFFICE", UndergroundComplex.TitleOf(UndergroundComplex.KindOn(hq, -1)));
    }

    /// <summary>The scenario's own sites plus enough generated ones that both shapes — with an unlisted band
    /// and without — are certainly in the sweep, and the shallow sites are too.</summary>
    private static IEnumerable<string> Sweep()
    {
        var all = Bodies.Concat(Enumerable.Range(0, 120).Select(i => $"probe-moon-{i}")).ToList();
        Assert.Contains(all, UndergroundComplex.HasUnlistedBand);
        Assert.Contains(all, b => !UndergroundComplex.HasUnlistedBand(b));
        return all;
    }

    [Fact]
    public void NothingAboveGroundIsPainted()
    {
        // The surface is not a floor of the facility and must not inherit its livery.
        foreach (int level in new[] { 0, 1, 5 })
        {
            Assert.Null(UndergroundComplex.LiveryFor("luna", level));
        }
    }

    [Fact]
    public void TheNameAndTheLiveryReadTheSameDepartment()
    {
        // ONE list. The departments were a `string[]` local inside NameOf until the colour started depending
        // on them too — at which point two copies would have been two answers to one question, which is the
        // failure this ground's spec opens with a table of.
        foreach (int level in Enumerable.Range(1, 40).Select(n => -n))
        {
            string dept = UndergroundComplex.DepartmentOf("luna", level);
            Assert.Contains(dept, UndergroundComplex.NameOf("luna", level), StringComparison.Ordinal);
            Assert.Contains(dept, UndergroundComplex.Departments);
        }
    }

    [Fact]
    public void TheDepthIsTheNumberThatMakesTheWalkBackMeanSomething()
    {
        // Owner: "also we could make it deeper like 150 meters :-D" — 40 m was a car park. The overburden is
        // the whole ride down before the first door opens, and the descent card has always described a shaft
        // long enough to lose count in.
        Assert.Equal(150.0, UndergroundComplex.MetresDown(-1));
        Assert.Equal("SURFACE", UndergroundComplex.DepthPaint(0));
        Assert.Contains("150", UndergroundComplex.DepthPaint(-1), StringComparison.Ordinal);

        // Strictly deeper, all the way to the performance guard, and a real hole at the bottom.
        double prev = UndergroundComplex.MetresDown(-1);
        for (int level = -2; level >= UndergroundComplex.DeepestPossibleFloor; level--)
        {
            double here = UndergroundComplex.MetresDown(level);
            Assert.True(here > prev);
            prev = here;
        }
        Assert.True(UndergroundComplex.MetresDown(UndergroundComplex.DeepestPossibleFloor) > 400,
            "the deepest site in the game is under 400 m — that is not a hole worth telling people about.");
    }
}
