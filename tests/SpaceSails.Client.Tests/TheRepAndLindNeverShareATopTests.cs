using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1341 c · <b>HARLAN FESS'S ROUNDS AND RAUHA LIND'S CHAIR NEVER CONTEND FOR ONE TOP.</b> QA 2026-09-30 saw the rep
/// at a bar top in two post-#1335 boots (unverified by re-boot). #1335 moved which chair the regulars' rota leaves
/// free — Lind takes the first one — so this is the law, swept at every haven with a bar over two hundred watches:
/// on each of her watches, no spot his round can walk to (the first free place at the counter, and the side of
/// each present regular's chair the room allows — <c>HavenInterior.BesideATop</c>, the walker's own sounding)
/// puts his body over hers, and her chair is never a present regular's.
///
/// <para>Measured on the room, not on the constants: the walls are the built concourse deck's collision field,
/// the chairs are the rota's and her own published seat. <b>Proven able to fail</b> by seating her in the first
/// chair the rota TOOK rather than the first it left free: <i>the-space-bar watch 3: her chair is Gilt-Eye's</i>, and <i>the rep beside Gilt-Eye stands 1.40 du from her chair</i> —
/// his spot beside a regular lands on her.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRepAndLindNeverShareATopTests
{
    private const int Watches = 200;

    /// <summary>Two bodies closer than this are one body in two places.</summary>
    private const double Apart = 2 * DeckPlan.AvatarRadius;

    [Fact]
    public void HisRoundsNeverStandWhereSheSits()
    {
        int herWatches = 0;
        var clashes = new List<string>();
        foreach (string bar in HavenInterior.InteriorBodyIds.Where(b => Barkeeps.For(b) is not null))
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = HavenInterior.DockedDeck(bar)!.CollisionField;
            HavenInterior.BarFloor floor = HavenInterior.BarBand(bar)!.Value;
            DeckReachability.Point? counter = floor.Fixtures
                .Where(p => !SurfaceCollision.Blocked(p.X, p.Y, DeckPlan.AvatarRadius, walls))
                .Select(p => (DeckReachability.Point?)p)
                .FirstOrDefault();

            for (int w = 0; w < Watches; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                if (HavenInterior.TheStringersChairAt(bar, t) is not { } hers)
                {
                    continue;
                }

                herWatches++;
                var stops = new List<(string What, double X, double Y)>();
                if (counter is { } c)
                {
                    stops.Add(("the counter", c.X, c.Y));
                }

                foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(bar, t).Where(r => r.Present))
                {
                    if (Math.Abs(r.X - hers.X) < 1e-6 && Math.Abs(r.Y - hers.Y) < 1e-6)
                    {
                        clashes.Add($"{bar} watch {w}: her chair is {r.ShortName}'s");
                    }

                    if (HavenInterior.BesideATop(new DeckReachability.Point(r.X, r.Y), DeckPlan.AvatarRadius, walls)
                        is { } beside)
                    {
                        stops.Add(($"beside {r.ShortName}", beside.X, beside.Y));
                    }
                }

                foreach ((string what, double x, double y) in stops)
                {
                    double d = Math.Sqrt(((x - hers.X) * (x - hers.X)) + ((y - hers.Y) * (y - hers.Y)));
                    if (d < Apart)
                    {
                        clashes.Add($"{bar} watch {w}: the rep {what} stands {d:F2} du from her chair");
                    }
                }
            }
        }

        Assert.True(herWatches > 50, $"only {herWatches} of her watches were swept — the sweep asks nothing.");
        Assert.True(clashes.Count == 0, string.Join("\n", clashes.Take(20)));
    }
}
