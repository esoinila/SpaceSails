using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #602 · WHERE THE CAR PUTS YOU BACK. Owner, stepping out of the lift onto the regolith: <i>"Oh I emerged
/// into the wall on the surface... I cannot move :-D"</i>
///
/// <para>The shed is drawn at <see cref="SecretLab.HeadSpot"/>, which takes the raw seeded door spot and
/// then MOVES it clear of the shelters and huts already standing on that ground. The lift's return path
/// asked <see cref="SecretLab.For"/> instead — the spot before the nudge — so on any site where the nudge
/// did something, the car delivered the captain inside the exact structure the nudge exists to avoid.</para>
///
/// <para>The same shape as every expensive bug on this ground: two sources for one fact. The comment above
/// the offending function even asserted the property the code did not have — <i>"one seeded fact, two uses,
/// nothing to keep in sync"</i>.</para>
///
/// <para><b>Why it needed a CLIENT test.</b> Core knows where the shed is; only the client knows what else
/// is standing on that ground, because the surface deck is where the shelters, huts and geography are
/// actually assembled. A Core guard could compare two coordinates; only this one can ask whether a captain
/// can stand there.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLiftPutsYouSomewhereYouCanSTANDTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    [Fact]
    public void TheCarNeverDeliversTheCaptainIntoAWall()
    {
        // The bug, stated as the law. Coming up out of a facility must land the captain on ground they can
        // walk off — on every body, and on every site of it, because the shed is nudged by what that
        // particular site happens to have built near it.
        var stuck = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                DeckPlan deck = MoonSurface.SurfaceDeck(
                    body, body, [], 0, (_, _) => { },
                    siteSalt: site.LayoutSalt, siteName: site.Name, hasSecretSite: true);

                // Asked of the shed itself — a test that retyped the offset would be the same bug it is
                // guarding against.
                (double x, double y) = MoonSurface.LiftHead(body, site.LayoutSalt, Field).CarFloor;

                if (!DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    stuck.Add($"  {body} · {site.Name}: the car opens at ({x:F1}, {y:F1}) and that is solid.");
                }
            }
        }

        Assert.True(stuck.Count == 0,
            "the lift returns the captain INSIDE something on these sites:\n" + string.Join("\n", stuck));
    }

    [Fact]
    public void AndTheCaptainCanWALKAwayFromThereToTheWayHome()
    {
        // Standing is not enough — a captain set down in a sealed pocket can stand and still be finished.
        // The way home is the tube, and it must be walkable from where the car leaves them.
        var stranded = new List<string>();
        // The box has to contain BOTH endpoints. Underground audits stop at the landing band because a
        // floor does; the surface does not — the tube mouth sits above the band, and capping there made
        // every site fail for a goal that was simply outside the search area rather than walled off.
        var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.TopY);

        foreach (string body in new[] { "luna", "miranda", "secret-lab-site", "secret-lab-site-unlisted" })
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                DeckPlan deck = MoonSurface.SurfaceDeck(
                    body, body, [], 0, (_, _) => { },
                    siteSalt: site.LayoutSalt, siteName: site.Name, hasSecretSite: true);

                (double cx, double cy) = MoonSurface.LiftHead(body, site.LayoutSalt, Field).CarFloor;
                var from = new DeckReachability.Point(cx, cy);
                // The way home is the TUBE MOUTH — the spot a landing actually puts the captain on,
                // so it is standable on every site by construction. The first cut of this test aimed at
                // a point I picked off the landing band instead, and it sat inside a structure on The
                // Crater Shelf: the guard went red for a destination that was solid rather than for a
                // captain who was trapped. A reachability test is only as honest as both its endpoints.
                var home = new DeckReachability.Point(MoonSurface.SpawnX, MoonSurface.SpawnY);

                if (!DeckReachability.CanReach(from, home, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    stranded.Add($"  {body} · {site.Name}: cannot walk from the lift head to the way home.");
                }
            }
        }

        Assert.True(stranded.Count == 0,
            "the car leaves the captain somewhere they cannot walk out of:\n" + string.Join("\n", stranded));
    }

    [Fact]
    public void TheShedAndTheCarAgreeAboutWhereTheShedIS()
    {
        // The root cause, pinned directly so it cannot come back by a different route: there is ONE answer to
        // "where is the lift head", and everything that needs it asks the same function. A future caller that
        // reaches for SecretLab.For(...).DoorX again will fail here rather than in a playtest.
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                (double nudgedX, double nudgedY) = SecretLab.HeadSpot(body, site.LayoutSalt, Field);
                SecretLab.Placement raw = SecretLab.For(body, Field, forcePresent: true);

                // They are ALLOWED to differ — that is what the nudge is for. What is not allowed is for
                // anything that places the captain to use the raw one, which the tests above enforce.
                // This one simply records that the difference is real, so nobody "simplifies" the nudge away
                // believing the two were always the same.
                bool moved = Math.Abs(nudgedX - raw.DoorX) > 0.01 || Math.Abs(nudgedY - raw.DoorY) > 0.01;
                if (moved)
                {
                    return;   // proven: the two spots genuinely diverge somewhere in the shipped set
                }
            }
        }

        Assert.Fail(
            "the shed's nudge never moved it on ANY site — either the keep-out logic has stopped working, " +
            "or these tests are no longer covering the case that broke (#602).");
    }


    [Fact]
    public void TheCarOpensINSIDETheShedItWentDownFrom()
    {
        // Owner's own expectation, and the fiction's: "I would expect to spawn into the elevator box where we
        // went down with." RideTheLiftTo already says it out loud — "the car climbs for a long time and lets
        // you out into somebody's idea of a maintenance shed" — so the captain must actually be in the shed,
        // not merely near it.
        //
        // With clearance for their own width, so "inside" means standing in the room rather than embedded in
        // its wall, and on the DOOR side of centre, because that is the way out.
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                MoonSurface.LiftHeadBox box = MoonSurface.LiftHead(body, site.LayoutSalt, Field);
                (double x, double y) = box.CarFloor;

                Assert.True(box.Contains(x, y, DeckPlan.AvatarRadius),
                    $"{body} · {site.Name}: the car sets the captain down outside its own shed.");

                // #606 · Stated as a DISTANCE now, not as "y is smaller". The shed became an ordinary hut
                // with a seeded angle, and an axis-aligned assertion about a rotated building is right on one
                // site in a hundred by accident — which is the flavour of guard that passes while the ground
                // is broken.
                double toDoor = Math.Sqrt(((x - box.DoorX) * (x - box.DoorX)) + ((y - box.DoorY) * (y - box.DoorY)));
                double centreToDoor = Math.Sqrt(
                    ((box.CentreX - box.DoorX) * (box.CentreX - box.DoorX))
                    + ((box.CentreY - box.DoorY) * (box.CentreY - box.DoorY)));
                Assert.True(toDoor < centreToDoor,
                    $"{body} · {site.Name}: the car opens on the far side of the shed from its door.");
            }
        }
    }
}
