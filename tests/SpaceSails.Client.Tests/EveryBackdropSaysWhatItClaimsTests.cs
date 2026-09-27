using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #442 · THE THIRD DIRECTION — ROOM BACKDROPS, ART BY ART. The owner's ruling: <i>"the visible and physics
/// wall ALWAYS 1 to 1 the same."</i> #1099 made the pen the fifth reader of the one wall list and #1154 gave
/// the Reevers door sight. What was left was the art laid under the grid: a picture of a counter over floor
/// that a captain walks straight across.
///
/// <para><b>The audit (posted on #442, 2026-09-27) found one fact that decides the whole direction.</b> Every
/// backdrop in the game is an EYE-LEVEL PERSPECTIVE photograph laid over a plan rectangle. None is a top-down
/// plan, so a painted barrier has no plan footprint that could be copied into the wall list. Where a fixture's
/// plan position HAS been decided — #1040's cantina counter, the haven counters placed off each bar's own art
/// (<c>BarDesks</c>), #780's counter art painted at the desk, the gallery's fixtures, the park's solid beds — the
/// wall is already there. The concourse arts are the one family whose painted desks have no wall under the
/// picture, and that is a re-framing call for the owner rather than geometry a lane may invent.</para>
///
/// <para><b>So the guard is a classification, and it is not a barrier list.</b> It records a JUDGEMENT about
/// each picture (what it claims), never a single coordinate. Every backdrop drawn over a walkable plan must be
/// either:</para>
/// <list type="bullet">
/// <item><b>a portrait</b>: ambience that claims no plan edge; the room's own walls are the room; or</item>
/// <item><b>at-fixture art</b>: painted AT a carved fixture, which must stand on collidable wall along its
/// long axis and have no console standing inside it. That is the one form of "the picture says barrier" a
/// plan can be held to.</item>
/// </list>
/// <para>A new picture that is neither goes red here. So the owner's question (is this painted counter a
/// wall?) is asked the day the art is wired in, not the day somebody walks through it.</para>
/// </summary>
public sealed class EveryBackdropSaysWhatItClaimsTests
{
    private enum Claim
    {
        /// <summary>An eye-level picture of the room, stretched over the room. Claims no plan edge.</summary>
        Portrait,

        /// <summary>A picture of ONE fixture, laid over that fixture. Must stand on wall along its length.</summary>
        AtFixture,
    }

    /// <summary>
    /// Every backdrop, judged once. The reasons are the audit's (#442, 2026-09-27); each was read off the image
    /// itself.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Claim> Judged = new Dictionary<string, Claim>(StringComparer.Ordinal)
    {
        // The ship. The cantina's counter is #1040's wall; the berths are one bunk-width wide, and their
        // bunk/locker/cot/toilet stand where each room's own [E] console is.
        ["art/the-space-bar.jpg"] = Claim.Portrait,
        ["art/cabin-tidy.jpg"] = Claim.Portrait,
        ["art/cabin-messy-a.jpg"] = Claim.Portrait,
        ["art/ship-med-bay.jpg"] = Claim.Portrait,
        ["art/space-head.jpg"] = Claim.Portrait,

        // The havens' bars: the counter is placed off each art's own desk (BarDesks, pinned in BarkeepTests).
        ["art/the-roadstead-bar.jpg"] = Claim.Portrait,
        ["art/cinder-roost-bar.jpg"] = Claim.Portrait,
        ["art/ringside-bar.jpg"] = Claim.Portrait,
        ["art/selene-gate-bar.jpg"] = Claim.Portrait,
        ["art/red-eye-bar.jpg"] = Claim.Portrait,
        ["art/the-deep-bar.jpg"] = Claim.Portrait,
        ["art/the-tilt-bar.jpg"] = Claim.Portrait,

        // The havens' concourses. Their painted booths have NO wall under the picture; the plan's immigration
        // counters stand just below its bottom edge. Left for the owner as "re-frame", not invented here.
        ["art/the-rusty-roadstead-lobby.jpg"] = Claim.Portrait,
        ["art/cinder-roost-hall.jpg"] = Claim.Portrait,
        ["art/ringside-hall.jpg"] = Claim.Portrait,
        ["art/selene-gate-hall.jpg"] = Claim.Portrait,
        ["art/red-eye-hall.jpg"] = Claim.Portrait,
        ["art/the-deep-hall.jpg"] = Claim.Portrait,
        ["art/the-tilt-hall.jpg"] = Claim.Portrait,

        // Selene Gate's walk, gallery and service level.
        ["art/observation-walk.jpg"] = Claim.Portrait,
        ["art/gallery-cafeteria-tall.jpg"] = Claim.Portrait,
        ["art/selene-service-level.jpg"] = Claim.Portrait,

        // The Hive: the hall and the park are the room; the counter art is #780's "picture of the thing, at
        // the thing", laid over the desk the carve poured.
        [UndergroundComplex.CantinaHallArtUrl] = Claim.Portrait,
        [UndergroundComplex.ParkArtUrl] = Claim.Portrait,
        [UndergroundComplex.CounterArtUrl] = Claim.AtFixture,
    };

    /// <summary>How much of an at-fixture picture's long axis must stand on wall.</summary>
    private const double FixtureCover = 0.5;

    private static readonly string[] Havens =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "red-eye", "the-deep", "the-tilt"];

    private static readonly string[] HiveBodies = ["luna", "europa", "titan", "secret-lab-site"];

    /// <summary>Every plan in the game that wears a backdrop, built by its real builder.</summary>
    private static IEnumerable<(string Where, DeckPlan Plan)> EveryPlanWithArt()
    {
        yield return ("the ship", DeckPlan.Ship);
        foreach (string haven in Havens)
        {
            yield return (haven, HavenInterior.DockedDeck(haven)
                ?? throw new Xunit.Sdk.XunitException($"{haven} builds no deck."));
        }
        yield return ("selene-gate service level",
            HavenInterior.DockedDeck("selene-gate", level: HavenLevels.ServiceLevel)
                ?? throw new Xunit.Sdk.XunitException("selene-gate builds no service level."));

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        foreach (string body in HiveBodies)
        {
            int level = UndergroundComplex.TopPressurisedFloor(body)
                ?? throw new Xunit.Sdk.XunitException($"{body} has no pressurised floor.");
            yield return ($"{body} B{-level}", HiveInterior.FloorDeck(body, level, field, 0, (_, _) => { }, []));
        }
    }

    /// <summary>
    /// EVERY BACKDROP IS JUDGED, AND EVERY JUDGEMENT IS OF A BACKDROP THAT IS STILL DRAWN. A picture wired in
    /// without a claim is red, and so is a judgement about a picture nothing draws any more, so the table
    /// cannot rot into a list of names.
    /// </summary>
    [Fact]
    public void EveryBackdropOverAWalkablePlanIsJudged()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unjudged = new List<string>();
        foreach ((string where, DeckPlan plan) in EveryPlanWithArt())
        {
            foreach (DeckPlan.Backdrop b in plan.Backdrops)
            {
                seen.Add(b.Url);
                if (!Judged.ContainsKey(b.Url))
                {
                    unjudged.Add($"{where}: {b.Url}");
                }
            }
        }

        Assert.True(seen.Count >= 20, $"only {seen.Count} backdrops were found — the sweep has shrunk.");
        Assert.True(unjudged.Count == 0,
            "#442 · a backdrop is drawn over a walkable plan and nobody has said what it claims. Look at it: "
            + "if it is an eye-level picture of the room, it is a Portrait; if it is a picture of ONE fixture "
            + "laid over that fixture, it is AtFixture and must stand on wall. If its painted counter is a "
            + "barrier with no wall under it, that is the owner's call (#442), not a line in this table:\n  "
            + string.Join("\n  ", unjudged.Distinct()));
        Assert.Empty(Judged.Keys.Where(k => !seen.Contains(k)).Select(k => $"judged, never drawn: {k}"));
    }

    /// <summary>
    /// AT-FIXTURE ART STANDS ON WALL — the picture of a counter is laid over a counter a captain cannot walk
    /// through, along at least half its length. Asked of the plan's own collision segments, the list the boot
    /// and the pen both read (#1099).
    /// </summary>
    [Fact]
    public void AtFixtureArtStandsOnWallAlongItsLength()
    {
        var wrong = new List<string>();
        int judged = 0;
        foreach ((string where, DeckPlan plan) in EveryPlanWithArt())
        {
            foreach (DeckPlan.Backdrop b in plan.Backdrops)
            {
                if (!Judged.TryGetValue(b.Url, out Claim claim) || claim != Claim.AtFixture)
                {
                    continue;
                }
                judged++;
                double cover = WallUnder(b, plan.CollisionSegments);
                if (cover < FixtureCover)
                {
                    wrong.Add($"{where}: {b.Url} at ({b.X:F1}, {b.Y:F1}) {b.W:F1}×{b.H:F1} — only "
                        + $"{cover:P0} of its length stands on wall");
                }

                // …and it is a FIXTURE, not a room: nothing a captain presses [E] at stands inside it. Without
                // this, a room-sized picture passes on the room's own walls (watched: the cantina's art judged
                // at-fixture went green on its four bulkheads).
                int standing = plan.Consoles.Count(c =>
                    c.X > b.X && c.X < b.X + b.W && c.Y < b.Y && c.Y > b.Y - b.H);
                if (standing > 0)
                {
                    wrong.Add($"{where}: {b.Url} — {standing} console(s) stand inside it; it is a room, not a fixture");
                }
            }
        }

        Assert.True(judged >= HiveBodies.Length,
            $"only {judged} at-fixture pictures were checked — this proves little.");
        Assert.True(wrong.Count == 0,
            "#442 · the picture of a fixture is laid where the plan has no fixture — a counter you walk through:\n  "
            + string.Join("\n  ", wrong));
    }

    /// <summary>The fraction of a backdrop's LONG axis covered by collision segments lying inside it,
    /// measured as the union of their projections, so two walls over one stretch are not counted twice.</summary>
    private static double WallUnder(DeckPlan.Backdrop b, IReadOnlyList<SurfaceCollision.Segment> segments)
    {
        double x0 = b.X, x1 = b.X + b.W, y1 = b.Y, y0 = b.Y - b.H;
        bool alongX = b.W >= b.H;
        const double eps = 1e-6;
        bool In(double x, double y) => x >= x0 - eps && x <= x1 + eps && y >= y0 - eps && y <= y1 + eps;

        var spans = new List<(double Lo, double Hi)>();
        foreach (SurfaceCollision.Segment s in segments)
        {
            if (!In(s.X1, s.Y1) || !In(s.X2, s.Y2))
            {
                continue;
            }
            (double a, double c) = alongX ? (s.X1, s.X2) : (s.Y1, s.Y2);
            spans.Add((Math.Min(a, c), Math.Max(a, c)));
        }

        spans.Sort((p, q) => p.Lo.CompareTo(q.Lo));
        double covered = 0, reach = double.NegativeInfinity;
        foreach ((double lo, double hi) in spans)
        {
            double from = Math.Max(lo, reach);
            if (hi > from)
            {
                covered += hi - from;
            }
            reach = Math.Max(reach, hi);
        }
        double length = alongX ? b.W : b.H;
        return length <= 0 ? 0 : covered / length;
    }
}
