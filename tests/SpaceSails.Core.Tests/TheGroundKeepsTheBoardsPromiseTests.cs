using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// U05-F01 (#1419) · THE GROUND KEEPS THE BOARD'S PROMISE — Fable's ruling 2026-11-07: each landing-site kind
/// stamps ONE signature on its seeded ground, off the landing band; QuietBasin and WildPlain stamp nothing; and
/// site 0 stays byte for byte the ground it was.
///
/// <para>The site-0 pins below were measured on <c>origin/main</c> BEFORE the stamps were written, so they pin
/// the old ground rather than the new one. A pin taken after the change would only be the change agreeing with
/// itself.</para>
/// </summary>
public class TheGroundKeepsTheBoardsPromiseTests
{
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    public static TheoryData<string> Stamped => new()
    {
        LandingSiteKind.DepotApron.ToString(), LandingSiteKind.CraterShelf.ToString(),
        LandingSiteKind.IceFissure.ToString(), LandingSiteKind.DerelictPad.ToString(),
        LandingSiteKind.ShadowedRille.ToString(), LandingSiteKind.RidgeCamp.ToString(),
    };

    // WallHash of SurfaceLayout.For(body, DefaultField, "") measured on origin/main before this change.
    private static readonly Dictionary<string, long> SiteZeroOnMain = new()
    {
        ["miranda"] = 8664054885477065855,
        ["luna"] = 8664052914685501755,
        ["phobos"] = 8664052263292740435,
        ["europa"] = 8664062498844184336,
        ["titan"] = 8664059836831175442,
        ["ganymede"] = 8664074281776192753,
        ["callisto"] = 8664067879053189661,
        ["enceladus"] = 8664053663779536104,
        ["triton"] = 8664061009319019814,
    };

    [Fact]
    public void SiteZeroOfEveryBodyIsByteIdenticalToMain()
    {
        foreach (string body in Bodies)
        {
            Assert.Equal(SiteZeroOnMain[body], SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, "")));
            Assert.Equal(SiteZeroOnMain[body], SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, null)));
            Assert.Equal(SiteZeroOnMain[body], SurfaceLayout.WallHash(SurfaceLayout.For(body, Env)));
            // Site 0 is the WildPlain on the canon ground whichever way the kind is spelled.
            Assert.Equal(SiteZeroOnMain[body],
                SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, "", LandingSiteKind.WildPlain)));
            Assert.Empty(SurfaceLayout.StampFor(body, Env, "").Walls);
        }
    }

    [Fact]
    public void QuietBasinAndWildPlainGetNoStamp()
    {
        foreach (string body in Bodies)
        {
            Assert.Empty(SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.QuietBasin)).Walls);
            Assert.Empty(SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.WildPlain)).Walls);
            Assert.Empty(SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.QuietBasin)).Marks);

            // The Quiet Basin's whole ground is what the unstamped seeded generator lays on its key.
            Assert.Equal(
                SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, "QuietBasin", LandingSiteKind.WildPlain)),
                SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, "QuietBasin")));
        }
    }

    [Theory]
    [MemberData(nameof(Stamped))]
    public void EveryStampedKindLaysItsWallsOnEveryBody(string salt)
    {
        foreach (string body in Bodies)
        {
            SurfaceLayout.Stamp stamp = SurfaceLayout.StampFor(body, Env, salt);
            SurfaceLayout.Plan plan = SurfaceLayout.For(body, Env, salt);

            Assert.True(stamp.Walls.Count > 0, $"{body}/{salt}: the stamp laid no walls at all.");
            // The stamp IS in the ground the game builds, not merely in a side computation.
            HashSet<SurfaceLayout.Wall> ground = [.. plan.Walls];
            foreach (SurfaceLayout.Wall w in stamp.Walls)
            {
                Assert.True(ground.Contains(w), $"{body}/{salt}: stamp wall {w} is missing from the ground.");
            }
            // …and the unstamped ground of the same key is a different ground.
            Assert.NotEqual(
                SurfaceLayout.WallHash(SurfaceLayout.For(body, Env, salt, LandingSiteKind.WildPlain)),
                SurfaceLayout.WallHash(plan));

            AssertKindShape(body, salt, stamp);
        }
    }

    private static void AssertKindShape(string body, string salt, SurfaceLayout.Stamp stamp)
    {
        IReadOnlyList<SurfaceLayout.Wall> walls = stamp.Walls;
        string at = $"{body}/{salt}";
        double Len(SurfaceLayout.Wall w) => Math.Sqrt(Math.Pow(w.X2 - w.X1, 2) + Math.Pow(w.Y2 - w.Y1, 2));

        switch (Enum.Parse<LandingSiteKind>(salt))
        {
            case LandingSiteKind.DepotApron:
                // Four 1.5 du post boxes at the apron's corners (a box is four walls), at least two of them
                // surviving the ledger, and the apron itself centred 27 du below the band (#1478: clear of the landing square).
                Assert.True(walls.Count >= 8 && walls.Count % 4 == 0, $"{at}: {walls.Count} post walls");
                Assert.All(walls, w => { Assert.Equal(1.5, Len(w), 3); Assert.True(w.IsHull); });
                Assert.Equal(4, stamp.Marks.Count);
                Assert.Equal(Env.LandingBandY - 27, stamp.Marks.Average(m => (m.Y1 + m.Y2) / 2), 3);
                break;

            case LandingSiteKind.CraterShelf:
                // One long hull wall along one line near the bottom edge, with at least two 4 du gaps.
                Assert.All(walls, w => { Assert.True(w.IsHull); Assert.Equal(w.Y1, w.Y2, 6); });
                // #1478 · the lip steps up around what the ledger will not yield, so it is ONE line only where it can be:
                // most of it still lies on the modal line.
                Assert.True(walls.GroupBy(w => Math.Round(w.Y1, 3)).Max(g => g.Sum(Len)) > 0.4 * walls.Sum(Len), $"{at}: the lip is not one line");
                Assert.True(walls.All(w => w.Y1 < Env.BottomY + 31), $"{at}: the lip is not on the bottom edge");
                var runs = walls.OrderBy(w => w.X1).ToList();
                int gaps = 0;
                for (int i = 1; i < runs.Count; i++)
                {
                    if (runs[i].X1 - runs[i - 1].X2 >= 3.99)
                    {
                        gaps++;
                    }
                }
                Assert.True(gaps >= 2, $"{at}: {gaps} gaps in the lip");
                Assert.True(runs.Count >= 10, $"{at}: only {runs.Count} runs of the lip");
                Assert.True(runs.Sum(Len) > 100, $"{at}: the lip is not long");
                break;

            case LandingSiteKind.IceFissure:
                // A jagged Rille polyline across the field, and a collidable (non-hull) slot of walls along it.
                Assert.True(stamp.Marks.Count >= 10);
                Assert.All(stamp.Marks, m => Assert.Equal(SurfaceScenery.Kind.Rille, m.Of));
                Assert.True(stamp.Marks.Max(m => m.X2) - stamp.Marks.Min(m => m.X1) > 200, $"{at}: not across the field");
                Assert.True(stamp.Marks.Select(m => Math.Round(m.Y1, 1)).Distinct().Count() > 3, $"{at}: not jagged");
                // Two banks x 11 segments is 22 when nothing is in the way; the ledger may refuse a piece or two.
                Assert.True(walls.Count >= 16, $"{at}: {walls.Count} slot walls");
                Assert.All(walls, w => Assert.False(w.IsHull));
                break;

            case LandingSiteKind.DerelictPad:
                // An apron and a 10 x 4 U of hull walls: two 4 du sides and one 10 du back, open end to the band.
                Assert.Equal(3, walls.Count);
                Assert.All(walls, w => Assert.True(w.IsHull));
                SurfaceLayout.Wall back = walls.Single(w => Math.Abs(Len(w) - 10) < 1e-6);
                Assert.Equal(2, walls.Count(w => Math.Abs(Len(w) - 4) < 1e-6));
                Assert.True(walls.Where(w => w != back).All(w => w.Y1 > back.Y1 || w.Y2 > back.Y1),
                    $"{at}: the U does not open toward the band");
                Assert.Equal(4, stamp.Marks.Count);
                break;

            case LandingSiteKind.ShadowedRille:
                // Two parallel walls 10 du apart, running down from the band, open at both ends (no cross wall).
                Assert.Equal(12, walls.Count);   // six 10 du segments x two walls: the pieces no longer refuse each other
                Assert.All(walls, w => Assert.Equal(w.X1, w.X2, 6));
                double[] xs = [.. walls.Select(w => Math.Round(w.X1, 3)).Distinct().OrderBy(x => x)];
                Assert.Equal(2, xs.Length);
                Assert.Equal(10.0, xs[1] - xs[0], 3);
                Assert.True(walls.Max(w => Math.Max(w.Y1, w.Y2)) < Env.LandingBandY);
                break;

            case LandingSiteKind.RidgeCamp:
                // Two U huts, a three-wall lean-to and a 1.5 du mast: 3 + 3 + 3 + 4 walls.
                Assert.Equal(13, walls.Count);
                Assert.Equal(4, walls.Count(w => Math.Abs(Len(w) - 1.5) < 1e-6));
                break;
        }
    }

    /// <summary>A stamp is laid through the generator's own ledger, so it overlaps none of what is already
    /// standing — the landing square, the shelters, the lab chambers, the monolith's keep-out, the anchor's
    /// ceremony ground. Walked along every wall at half-du steps against the raw (un-elbowed) claim boxes.</summary>
    [Theory]
    [MemberData(nameof(Stamped))]
    public void NoStampOverlapsTheLandingBandTheShedTheLabTheMonolithOrAShelter(string salt)
    {
        foreach (string body in Bodies)
        {
            var claims = SurfaceLayout.StandingClaims(body, salt, Env);
            claims.Add((Env.AnchorX, Env.AnchorY, SurfaceLayout.AnchorReserveRadius));
            foreach (SurfaceLayout.Wall w in SurfaceLayout.StampFor(body, Env, salt).Walls)
            {
                double len = Math.Max(1, Math.Sqrt(Math.Pow(w.X2 - w.X1, 2) + Math.Pow(w.Y2 - w.Y1, 2)));
                for (double t = 0; t <= len; t += 0.5)
                {
                    double x = w.X1 + ((w.X2 - w.X1) * t / len), y = w.Y1 + ((w.Y2 - w.Y1) * t / len);
                    foreach ((double cx, double cy, double r) in claims)
                    {
                        Assert.False(Math.Abs(x - cx) < r && Math.Abs(y - cy) < r,
                            $"{body}/{salt}: stamp wall point ({x:F1}, {y:F1}) stands inside the claim ({cx:F0}, {cy:F0}, r{r:F0}).");
                    }
                    Assert.True(y < Env.LandingBandY - 6, $"{body}/{salt}: ({x:F1}, {y:F1}) is on the landing band.");
                }
            }
        }
    }

    /// <summary>A narrow structural check ONLY: no connected group of the stamp's own walls closes a loop wider
    /// than a 1.5 du post. It cannot see a room made by a stamp wall together with someone else's wall; the
    /// real fire-code guard is the sealed-pocket flood in SurfaceReachabilityTests.</summary>
    [Theory]
    [MemberData(nameof(Stamped))]
    public void NoStampWallsLoopIntoARoom(string salt)
    {
        foreach (string body in Bodies)
        {
            IReadOnlyList<SurfaceLayout.Wall> walls = SurfaceLayout.StampFor(body, Env, salt).Walls;
            var left = walls.ToList();
            while (left.Count > 0)
            {
                var piece = new List<SurfaceLayout.Wall> { left[0] };
                left.RemoveAt(0);
                bool grew = true;
                while (grew)
                {
                    grew = false;
                    for (int i = left.Count - 1; i >= 0; i--)
                    {
                        if (piece.Any(p => Touches(p, left[i])))
                        {
                            piece.Add(left[i]);
                            left.RemoveAt(i);
                            grew = true;
                        }
                    }
                }
                if (piece.Count >= 4 && piece.All(w => Degree(piece, w.X1, w.Y1) >= 2 && Degree(piece, w.X2, w.Y2) >= 2))
                {
                    double width = piece.Max(w => Math.Max(w.X1, w.X2)) - piece.Min(w => Math.Min(w.X1, w.X2));
                    Assert.True(width <= 1.5 + 1e-6, $"{body}/{salt}: a closed room {width:F1} du wide.");
                }
            }
        }
    }

    private static bool Touches(SurfaceLayout.Wall a, SurfaceLayout.Wall b) =>
        Same(a.X1, a.Y1, b.X1, b.Y1) || Same(a.X1, a.Y1, b.X2, b.Y2)
        || Same(a.X2, a.Y2, b.X1, b.Y1) || Same(a.X2, a.Y2, b.X2, b.Y2);

    private static bool Same(double x1, double y1, double x2, double y2) =>
        Math.Abs(x1 - x2) < 1e-6 && Math.Abs(y1 - y2) < 1e-6;

    private static int Degree(List<SurfaceLayout.Wall> piece, double x, double y) =>
        piece.Count(w => Same(w.X1, w.Y1, x, y) || Same(w.X2, w.Y2, x, y));
}
