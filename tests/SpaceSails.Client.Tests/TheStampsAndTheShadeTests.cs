using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U05-F01 (#1419) + R1-F78 (#1411) · Fable's ground rulings 2026-11-07, on the REAL deck the game builds.
///
/// <para><b>The shade.</b> The monolith's shadow was nine near-parallel <c>Rille</c> strokes, which read as a
/// channel with banks. It is two FILLED <c>Shade</c> quads now (umbra, penumbra) — scenery only, so the movement
/// code never sees them. (This lives with the client's tests because the shadow is laid by
/// <c>MoonSurface</c>, which Core cannot reach.)</para>
///
/// <para><b>The stamps.</b> Every console on every stamped kind x body is still walkable from the landing
/// square — the stamp must never wall a console in.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheStampsAndTheShadeTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan Deck(string body, string salt, bool lab = false) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, siteSalt: salt, siteName: salt, hasSecretSite: lab);

    [Fact]
    public void TheMonolithsShadowIsTwoFilledQuadsAndNotNineStrokes()
    {
        DeckPlan deck = Deck(Monolith.BodyId, "");
        SurfaceScenery.Mark[] shade = [.. deck.Scenery.Where(m => m.Of == SurfaceScenery.Kind.Shade)];

        Assert.Equal(2, shade.Length);
        // Umbra then penumbra: the umbra is the darker, and the penumbra picks up where it ends.
        Assert.True(shade[0].Alpha > shade[1].Alpha && shade[1].Alpha > 0);
        Assert.Equal(shade[0].Y2, shade[1].Y1, 6);
        Assert.Equal(Field.AnchorY + Monolith.HalfHeight, shade[0].Y1, 6);
        Assert.Equal(Monolith.HalfWidth, shade[0].HalfNear, 6);
        Assert.True(shade[1].HalfFar > shade[0].HalfNear, "a low sun's penumbra spreads");

        // The old nine strokes are gone: no Rille stroke starts at the slab's face inside the lane.
        double faceY = Field.AnchorY + Monolith.HalfHeight;
        Assert.DoesNotContain(deck.Scenery, m =>
            m.Of == SurfaceScenery.Kind.Rille && Math.Abs(m.Y1 - faceY) < 1e-6
            && Math.Abs(m.X1 - Field.AnchorX) <= Monolith.HalfWidth + 1e-6);
        // …and not one stroke of it remains: shade is the whole of the shadow's marks (no Rille runs along the lane's edge).
        Assert.DoesNotContain(deck.Scenery, m =>
            m.Of == SurfaceScenery.Kind.Rille && Math.Abs(m.X1 - (Field.AnchorX + Monolith.HalfWidth)) < 1e-6
            && Math.Abs(m.Y1 - faceY) < 1e-6);
    }

    [Fact]
    public void ShadeIsSceneryAndIsFoundOnlyOnTheMonolithsGround()
    {
        foreach ((string body, string salt) in new[] { ("luna", ""), ("miranda", ""), ("phobos", "RidgeCamp"), ("europa", "DepotApron") })
        {
            Assert.DoesNotContain(Deck(body, salt).Scenery, m => m.Of == SurfaceScenery.Kind.Shade);
        }
        // Scenery cannot collide: Shade lives in DeckPlan.Scenery, which the movement code never reads.
        DeckPlan ground = Deck(Monolith.BodyId, "");
        Assert.Contains(ground.Scenery, m => m.Of == SurfaceScenery.Kind.Shade);
    }

    [Fact]
    public void EveryConsoleOnEveryStampedKindOnEveryBodyIsStillWalkableFromTheLandingSquare()
    {
        string[] bodies = ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];
        (double dropX, double dropY, _) = SurfaceLayout.LandingApproach(Field);
        var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.TopY);
        var bad = new List<string>();
        int checkedConsoles = 0;

        foreach (string body in bodies)
        {
            foreach (LandingSiteKind kind in Enum.GetValues<LandingSiteKind>()
                .Where(k => k is not (LandingSiteKind.WildPlain or LandingSiteKind.QuietBasin)))
            {
                foreach (bool lab in new[] { false, true })
                {
                    DeckPlan deck = Deck(body, kind.ToString(), lab);
                    if (!DeckReachability.Standable(dropX, dropY, DeckPlan.AvatarRadius, deck.CollisionField))
                    {
                        bad.Add($"{body}/{kind}{(lab ? "+lab" : "")}: the landing square is not standable");
                        continue;
                    }
                    // ONE flood from the landing square (A* per console is minutes at this size): a console is
                    // reached when any flooded cell stands within 2 du of it.
                    const double Step = 1.0;
                    int cols = (int)((bounds.Item3 - bounds.Item1) / Step) + 1, rows = (int)((bounds.Item4 - bounds.Item2) / Step) + 1;
                    var seen = new bool[cols, rows];
                    var queue = new Queue<(int C, int R)>();
                    (int c0, int r0) = ((int)((dropX - bounds.Item1) / Step), (int)((dropY - bounds.Item2) / Step));
                    seen[c0, r0] = true;
                    queue.Enqueue((c0, r0));
                    while (queue.Count > 0)
                    {
                        (int c, int r) = queue.Dequeue();
                        foreach ((int dc, int dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int nc = c + dc, nr = r + dr;
                            if (nc < 0 || nr < 0 || nc >= cols || nr >= rows || seen[nc, nr]) { continue; }
                            if (!DeckReachability.Standable(bounds.Item1 + (nc * Step), bounds.Item2 + (nr * Step), DeckPlan.AvatarRadius, deck.CollisionField)) { continue; }
                            seen[nc, nr] = true;
                            queue.Enqueue((nc, nr));
                        }
                    }
                    foreach (DeckPlan.ConsoleSpot con in deck.Consoles)
                    {
                        if (con.Y >= Field.TopY) { continue; }   // the ship above the rim is a different place
                        if (!DeckReachability.Standable(con.X, con.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            bad.Add($"{body}/{kind}{(lab ? "+lab" : "")}: {con.Kind} '{con.Label}' at ({con.X:F0}, {con.Y:F0}) is not standable");
                            continue;
                        }
                        checkedConsoles++;
                        bool reached = false;
                        for (int dc = -2; dc <= 2 && !reached; dc++)
                        {
                            for (int dr = -2; dr <= 2 && !reached; dr++)
                            {
                                int cc = (int)Math.Round((con.X - bounds.Item1) / Step) + dc, rr = (int)Math.Round((con.Y - bounds.Item2) / Step) + dr;
                                reached = cc >= 0 && rr >= 0 && cc < cols && rr < rows && seen[cc, rr];
                            }
                        }
                        if (!reached)
                        {
                            bad.Add($"{body}/{kind}: cannot walk to {con.Kind} '{con.Label}' at ({con.X:F0}, {con.Y:F0})");
                        }
                    }
                }
            }
        }
        Assert.True(checkedConsoles > bodies.Length * 6, $"the sweep looked at only {checkedConsoles} consoles");
        Assert.True(bad.Count == 0, "A stamp walled a console in:\n  " + string.Join("\n  ", bad));
    }
}
