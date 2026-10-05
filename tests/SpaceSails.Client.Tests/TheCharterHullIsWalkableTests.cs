using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1074 beat 5 · THE CHARTER HULL ON THE REAL GROUND — parked beside the fence on every body and landing
/// site the game ships, with A*.
///
/// <para>It drives <see cref="MoonSurface.SurfaceDeck"/> and <see cref="MoonSurface.SurveyHullOn"/> and not a
/// hand-built field, for #587's reason: the shed is seeded and every other wall on a landing site is seeded
/// too, so an audit over geometry the test laid itself audits a world the game does not ship. The lift head is
/// forced on every site (<c>hasSecretSite: true</c>), which is strictly harder than the seeded truth.</para>
///
/// <para>What it pins: she is FOUND a place on every ground; the three fixtures aboard can each be WALKED to
/// from the tube with her own walls standing; and asking where she stands changes nothing about the preserved
/// site's own deck — the beat leaves the site byte-identical.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheCharterHullIsWalkableTests
{
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;
    private const double AvatarRadius = 0.7;
    private const double Step = 1.0;

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    private static DeckReachability.Point Spawn => new(Env.HomeX, Env.TopY - 2.0);

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds =>
        (Env.LeftX - 2, Env.BottomY - 2, Env.RightX + 2, Env.TopY);

    private static DeckPlan Deck(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name,
            monolithEpoch: 0, hasSecretSite: true);

    private static ReturningShuttle.Hull? Hull(string body, LandingSite site) =>
        MoonSurface.SurveyHullOn(body, body, [], site.LayoutSalt, site.Name, 0, hasSecretSite: true);

    /// <summary>
    /// SHE IS PARKED ON EVERY GROUND, AND EVERY FIXTURE ABOARD CAN BE WALKED TO — through her gap, from the
    /// tube, with her walls added to the ground's.
    /// </summary>
    [Fact]
    public void EveryFixtureAboardCanBeWalkedToFromTheTube()
    {
        PreservationZone.Install(Bodies);
        try
        {
            var wrong = new List<string>();
            int parked = 0, outposts = 0;
            foreach (string body in Bodies)
            {
                foreach (LandingSite site in LandingSites.For(body))
                {
                    if (Hull(body, site) is not { } hull)
                    {
                        wrong.Add($"  {body}/{site.Name}: nowhere to park her");
                        continue;
                    }
                    parked++;

                    DeckPlan deck = Deck(body, site);
                    // The deck APPENDS two more things on this ground, both seeded: the secret lab's chamber (once
                    // forced) and the home tile's outpost hut. The strictest reading has them standing.
                    SecretLab.Placement lab = SecretLab.OnThisSite(body, site.LayoutSalt, Env, forcePresent: true);
                    SecretLab.Region chamber = SecretLab.Build(body, Env, lab.DoorX, lab.DoorY);
                    var later = new List<SurfaceLayout.Wall>(chamber.Walls);
                    var footprints = new List<(double X0, double Y0, double X1, double Y1)>
                    {
                        (chamber.MinX, chamber.MinY, chamber.MaxX, chamber.MaxY),
                    };
                    SurfaceOutpost.Placement hut = SurfaceOutpost.ForTile(body, site.LayoutSalt, SurfaceTiles.Home);
                    if (hut.HasOutpost)
                    {
                        SurfaceOutpost.Region room = SurfaceOutpost.Build(body, site.LayoutSalt, hut);
                        later.AddRange(room.Walls);
                        footprints.Add((room.MinX, room.MinY, room.MaxX, room.MaxY));
                        outposts++;
                    }

                    double hx0 = hull.CentreX - (ReturningShuttle.HullLength / 2), hx1 = hull.CentreX + (ReturningShuttle.HullLength / 2);
                    double hy0 = hull.CentreY - (ReturningShuttle.HullBreadth / 2), hy1 = hull.CentreY + (ReturningShuttle.HullBreadth / 2);
                    foreach ((double X0, double Y0, double X1, double Y1) f in footprints)
                    {
                        if (hx0 <= f.X1 && hx1 >= f.X0 && hy0 <= f.Y1 && hy1 >= f.Y0)
                        {
                            wrong.Add($"  {body}/{site.Name}: she is parked across an appended footprint");
                        }
                    }

                    var segments = deck.Walls
                        .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))
                        .Concat(later.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)))
                        .Concat(hull.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)))
                        .ToArray();
                    SurfaceCollision.WallIndex walls = SurfaceCollision.WallIndex.Build(segments);

                    foreach ((string what, (double X, double Y) at) in new[]
                    {
                        ("airlock", hull.Airlock), ("rack", hull.Rack), ("log desk", hull.Log),
                    })
                    {
                        var where = new DeckReachability.Point(at.X, at.Y);
                        if (!DeckReachability.Standable(where.X, where.Y, AvatarRadius, walls))
                        {
                            wrong.Add($"  {body}/{site.Name}: the {what} is not a square a captain can stand on");
                        }
                        else if (!DeckReachability.Path(Spawn, where, walls, AvatarRadius, Bounds, Step).Reached)
                        {
                            wrong.Add($"  {body}/{site.Name}: the {what} cannot be walked to from the tube");
                        }
                    }
                }
            }

            Assert.True(parked >= 9, $"only {parked} ground(s) had her parked — this proves little.");
            Assert.True(outposts >= 1, "no ground in the sweep carries an outpost hut — the hut half of this audit proves nothing.");
            Assert.True(wrong.Count == 0, "the hull is not boardable:\n" + string.Join("\n", wrong));
        }
        finally
        {
            PreservationZone.Install([]);
        }
    }

    /// <summary>
    /// THE SITE IS BYTE-IDENTICAL: the preserved ground's own deck — walls, fixtures, labels — is the same
    /// sequence of values before she is parked as after, and she is parked on a ground that is not fenced
    /// nowhere at all.
    /// </summary>
    [Fact]
    public void ParkingHerLeavesThePreservedSitesOwnDeckUntouched()
    {
        PreservationZone.Install(Bodies);
        try
        {
            foreach (string body in Bodies)
            {
                LandingSite site = LandingSites.For(body)[0];
                DeckPlan before = Deck(body, site);
                string[] wallsBefore = [.. before.Walls.Select(w => w.ToString())];
                string[] consolesBefore = [.. before.Consoles.Select(c => c.ToString())];
                string[] labelsBefore = [.. before.RoomLabels.Select(l => l.ToString())];

                Assert.NotNull(Hull(body, site));

                DeckPlan after = Deck(body, site);
                Assert.Equal(wallsBefore, after.Walls.Select(w => w.ToString()).ToArray());
                Assert.Equal(consolesBefore, after.Consoles.Select(c => c.ToString()).ToArray());
                Assert.Equal(labelsBefore, after.RoomLabels.Select(l => l.ToString()).ToArray());
            }
        }
        finally
        {
            PreservationZone.Install([]);
        }

        // …and a ground the office has not taken into care has no hull parked on it, whatever else is true.
        PreservationZone.Install([]);
        foreach (string body in Bodies)
        {
            Assert.Null(Hull(body, LandingSites.For(body)[0]));
        }
    }
}
