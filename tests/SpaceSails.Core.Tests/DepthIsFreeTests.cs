using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #585 · DEPTH IS FREE, SHAFTS ARE NOT. The owner worked the architecture out himself, mid-build:
///
/// <para><i>"since every secret lab can have a depth of it's own we do not need to worry about running out of
/// space down there, since down there is unlimited amount of floors as far as we are concerned. So let's
/// architect it to keep this in mind from the start. Well ok the lift shafts are the limiting factor, but
/// besides those we have space."</i></para>
///
/// <para>Both halves matter. A floor costs no coordinate space — every one reuses the surface's own envelope
/// — so depth must never be a constant. And the SHAFT is what rations it, which turns "how deep does this
/// go" from a number in a config into something a captain finds out by exploring.</para>
///
/// <para>These are written now rather than later precisely because he said "architect it to keep this in mind
/// from the start": a hard-coded bottom is cheap to remove today and expensive to remove once three other
/// things read it.</para>
/// </summary>
public sealed class DepthIsFreeTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    [Fact]
    public void EverySiteHasItsOWNDepth()
    {
        // If every facility were the same height, depth would be scenery. The whole point of it being free is
        // that it can vary wildly at no cost at all.
        var depths = new HashSet<int>();
        foreach (string body in Bodies)
        {
            depths.Add(UndergroundComplex.DepthOf(body));
        }
        Assert.True(depths.Count >= 4, "every clandestine site in the system is the same depth.");
    }

    [Fact]
    public void ADepthIsAlwaysWorthDescendingAndNeverAbsurd()
    {
        foreach (string body in Bodies)
        {
            int depth = UndergroundComplex.DepthOf(body);
            Assert.True(depth <= -3, $"{body}: {-depth} floors is the two-door apartment again.");
            Assert.True(depth >= UndergroundComplex.DeepestPossibleFloor,
                $"{body}: {-depth} floors is past the performance guard.");
        }
    }

    [Fact]
    public void ASiteIsTheSameDepthEveryVisit()
    {
        foreach (string body in Bodies)
        {
            Assert.Equal(UndergroundComplex.DepthOf(body), UndergroundComplex.DepthOf(body));
        }
    }

    [Fact]
    public void ONELiftNeverServesTheWholeBuilding()
    {
        // The owner's own limiting factor, as the mechanic. If a single car reached the bottom of a twenty
        // floor facility, unlimited depth would just be an unlimited corridor with a lift at one end.
        foreach (string body in Bodies)
        {
            int bottom = UndergroundComplex.DepthOf(body);
            int firstCarReaches = UndergroundComplex.BandFloor(body, 0);

            Assert.True(firstCarReaches >= -UndergroundComplex.FloorsPerShaft,
                $"{body}: the surface car reaches B{-firstCarReaches} — that is a building, not a band.");

            if (bottom < firstCarReaches)
            {
                Assert.True(UndergroundComplex.IsBandBottom(body, firstCarReaches),
                    $"{body}: the car stops at B{-firstCarReaches} and nothing marks it as the end of the line.");
            }
        }
    }

    [Fact]
    public void TheBottomOfTheSITEIsNotAnEndOfTheLine()
    {
        // At the true bottom there is no next shaft to look for, and the game must not send a captain hunting
        // through a dead floor for a lift that was never dug.
        foreach (string body in Bodies)
        {
            // #592: the site's bottom is its TRUE bottom. On a rare site the LISTED bottom is a band bottom
            // — the car stops and there is another shaft — and that is the feature, not a violation.
            Assert.False(UndergroundComplex.IsBandBottom(body, UndergroundComplex.TrueDepthOf(body)),
                $"{body}: the deepest floor claims another shaft goes further down.");
        }
    }

    [Fact]
    public void BandsCoverEveryFloorWithoutAGap()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int band = UndergroundComplex.BandOf(level);
                Assert.InRange(band, 0, 12);
                Assert.True(UndergroundComplex.BandFloor(body, band) <= level,
                    $"{body}: B{-level} sits below the band that is supposed to contain it.");
            }
        }
    }

    [Fact]
    public void ADeepFloorIsStillAFloorAndStillFitsTheEnvelope()
    {
        // The architecture only holds if floor twenty is as well-formed as floor one. That is what "depth is
        // free" has to actually MEAN, rather than "the number is allowed to be bigger".
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;
        foreach (int level in new[] { -9, -14, -20, UndergroundComplex.DeepestPossibleFloor })
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("miranda", level, field);

            Assert.True(floor.Walls.Count > 60, $"B{-level} is thin.");
            Assert.True(floor.RoomCentres.Count >= 4, $"B{-level} has nothing to enter.");
            Assert.False(string.IsNullOrWhiteSpace(floor.Name));

            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                Assert.InRange(w.X1, field.LeftX, field.RightX);
                Assert.InRange(w.Y1, field.BottomY, field.LandingBandY);
            }
        }
    }

    [Fact]
    public void EveryFloorHasAName()
    {
        for (int level = -1; level >= UndergroundComplex.DeepestPossibleFloor; level--)
        {
            string name = UndergroundComplex.NameOf("miranda", level);
            Assert.StartsWith($"B{-level}", name, StringComparison.Ordinal);
            Assert.Contains("·", name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheEndOfTheLineNeverTeases()
    {
        // A panel that hints it might go further turns scale into a puzzle. It simply has no button, which is
        // the honest reason a facility this size has more than one lift.
        string line = UndergroundComplex.EndOfTheLineLine(4);
        Assert.Contains("no button", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unlock", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("perhaps", line, StringComparison.OrdinalIgnoreCase);
    }
}
