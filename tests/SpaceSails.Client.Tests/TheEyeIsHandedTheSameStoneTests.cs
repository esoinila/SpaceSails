using System;
using System.Collections.Generic;
using System.IO;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #858 · THE EYE AND THE LEGS ARE LOOKING AT THE SAME STONE — and now they are handed the same OBJECT.
///
/// <para>Lab 45 (#852) found the client keeping two views of one wall list and giving them to different
/// callers: everything that WALKS got <c>DeckPlan.CollisionField</c>, #448's grid, and everything that
/// LOOKS got a plain <c>List&lt;Segment&gt;</c> refilled every frame. The sweep is strictly O(walls) at
/// ~18–25 ns a segment and the indexed answer is flat, so the eye was paying 29× at 436 segments for
/// nothing. <c>SightBlockers()</c> now files its list into a <see cref="SurfaceCollision.WallIndex"/> and
/// keeps it.</para>
///
/// <para><b>What this file is for.</b> A faster road to a DIFFERENT answer is not an optimisation, it is a
/// shot through a wall — and #442's own war story is that the first suspect for "see it fire through wall
/// now" was this very index. So the sight set is swept, on real decks with real doors, both ways: the
/// index's answer against the honest hand sweep of the identical segments. And because a guard that cannot
/// tell pass from fail is the fifth named bug class in this repo, the same sweep is run against a
/// DESYNCED index — the plausible wrong fix, handing the eye the legs' own <c>CollisionField</c>, which
/// carries the walls and not the shut doors — and it must be caught.</para>
///
/// <para>It lives in the client tests rather than Core's for <see cref="TheRoundIsWalkableTests"/>'s
/// reason: Core owns the geometry, but only the client has a DOOR, and half of what the eye is stopped by
/// on a hive floor is a door that is shut.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheEyeIsHandedTheSameStoneTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The heavy world and the light one — B1 carries the #834 frontage (~465 segments, the floor
    /// Lab 45 measured the 29× on) and the lower floors carry the rounds.</summary>
    private static readonly (string Body, int Level)[] Floors =
        [("luna", -1), ("luna", -2), ("luna", -3), ("miranda", -1), ("miranda", -2), ("titan", -2)];

    /// <summary>How far the eye reaches — <see cref="PatrolBeat.MarkerSightDu"/>'s own order, so the pairs
    /// swept are the pairs the game actually asks about rather than lines across the whole field.</summary>
    private const double EyeReachDu = 30.0;

    /// <summary>#465's rule, replicated: what blocks a look is the walls plus whatever doors are shut, and a
    /// door is shut when the captain is not standing at it. The captain is parked far away here, which is
    /// the ordinary case for a guard's sightline and the case in which every auto-door is opaque.</summary>
    private static List<SurfaceCollision.Segment> SightBlockersOf(DeckPlan deck, double captainX, double captainY)
    {
        var sight = new List<SurfaceCollision.Segment>(deck.CollisionSegments);
        foreach (DeckPlan.Door d in deck.Doors)
        {
            if (!IsDoorShut(deck, d, captainX, captainY))
            {
                continue;
            }
            sight.Add(new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2));
        }
        return sight;
    }

    private static bool IsDoorShut(DeckPlan deck, DeckPlan.Door d, double captainX, double captainY)
    {
        if (d.Locked)
        {
            return true;
        }
        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
        double toDoor = Math.Sqrt(((captainX - mx) * (captainX - mx)) + ((captainY - my) * (captainY - my)));

        double nearestPartner = double.PositiveInfinity;
        if (d.Interlock != 0)
        {
            foreach (DeckPlan.Door other in deck.Doors)
            {
                if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                {
                    continue;
                }
                double ox = (other.X1 + other.X2) / 2.0, oy = (other.Y1 + other.Y2) / 2.0;
                nearestPartner = Math.Min(nearestPartner,
                    Math.Sqrt(((captainX - ox) * (captainX - ox)) + ((captainY - oy) * (captainY - oy))));
            }
        }
        return !Airlock.MayOpen(toDoor, nearestPartner, DeckPlan.DoorOpenRadius);
    }

    /// <summary>
    /// THE INDEXED SIGHT SET ANSWERS EXACTLY WHAT THE HAND SWEEP ANSWERS, on every real floor, over every
    /// look the eye is capable of. One source of truth: the index is built FROM the list, so this is the
    /// #448 law re-asked of the one caller that had never been given the index.
    /// </summary>
    [Fact]
    public void TheEyesIndexAnswersExactlyWhatTheHandSweepAnswers()
    {
        int checks = 0, blocked = 0, clear = 0;
        var bad = new List<string>();

        foreach ((string body, int level) in Floors)
        {
            DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
            (double cx, double cy) = HiveInterior.SpawnOn(Field);
            List<SurfaceCollision.Segment> plain = SightBlockersOf(deck, cx, cy);
            SurfaceCollision.WallIndex indexed = SurfaceCollision.WallIndex.Build(plain);

            // The index IS the list — same segments, same order — or the two are already two authorities.
            Assert.Equal(plain.Count, indexed.Count);
            for (int i = 0; i < plain.Count; i++)
            {
                Assert.Equal(plain[i], indexed[i]);
            }

            var rng = new Random(20260812 + level);
            for (int i = 0; i < 3000; i++)
            {
                double ax = Field.LeftX + (rng.NextDouble() * (Field.RightX - Field.LeftX));
                double ay = Field.BottomY + (rng.NextDouble() * (Field.LandingBandY - Field.BottomY));
                double angle = rng.NextDouble() * Math.PI * 2;
                double reach = rng.NextDouble() * EyeReachDu;
                double bx = ax + (Math.Cos(angle) * reach), by = ay + (Math.Sin(angle) * reach);

                bool swept = SurfaceCollision.HasLineOfSight(ax, ay, bx, by, plain);
                bool filed = SurfaceCollision.HasLineOfSight(ax, ay, bx, by, indexed);
                checks++;
                if (swept)
                {
                    clear++;
                }
                else
                {
                    blocked++;
                }
                if (swept != filed)
                {
                    bad.Add($"  {body} B{-level}: ({ax:F2},{ay:F2})→({bx:F2},{by:F2}) — " +
                            $"swept says {swept}, the index says {filed}.");
                }
            }
        }

        Assert.True(bad.Count == 0,
            $"{bad.Count} sightline(s) answer differently indexed:\n" +
            string.Join("\n", bad.GetRange(0, Math.Min(6, bad.Count))));

        // A sweep that never met a wall would prove nothing at all (§13's green test that asserts nothing),
        // and neither would one that never got a clear look. Pin BOTH halves.
        Assert.True(checks > 15_000, $"only {checks} sightlines were asked — the sweep is too small.");
        Assert.True(blocked > 1_000, $"only {blocked} of {checks} looks were STOPPED by something.");
        Assert.True(clear > 1_000, $"only {clear} of {checks} looks were CLEAR.");
    }

    /// <summary>
    /// …AND THE SWEEP ABOVE CAN TELL PASS FROM FAIL. The plausible wrong fix for #858 is to hand the eye
    /// <c>_deckPlan.CollisionField</c> — the index that already exists — which carries the walls and NOT the
    /// doors, and would put the tube's built-in gun back to shooting through a closed airlock (#465, owner:
    /// <i>"a reever was shot through a closed door"</i>). Desync the index that way and the sweep must go
    /// red, or it was measuring nothing. It went red 155 times over the six floors below on the day this was
    /// written; the bar is set well under that, because the count is a fact about the generator and the
    /// LAW is that it cannot be zero.
    /// </summary>
    [Fact]
    public void AnIndexThatQuietlyDroppedTheShutDoorsWouldBeCaught()
    {
        int doorsProved = 0;

        foreach ((string body, int level) in Floors)
        {
            DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
            (double cx, double cy) = HiveInterior.SpawnOn(Field);
            List<SurfaceCollision.Segment> plain = SightBlockersOf(deck, cx, cy);

            // THE DESYNC: the legs' own field — the same stone, minus every shut door.
            SurfaceCollision.WallIndex desynced = deck.CollisionField;

            foreach (DeckPlan.Door d in deck.Doors)
            {
                if (!IsDoorShut(deck, d, cx, cy))
                {
                    continue;
                }

                // A short look straight through the leaf: perpendicular to the door, a unit either side of
                // its middle. Nothing else can be in the way at that range but the door itself.
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 1e-9)
                {
                    continue;
                }
                double px = -dy / len, py = dx / len;
                double ax = mx + (px * 1.0), ay = my + (py * 1.0);
                double bx = mx - (px * 1.0), by = my - (py * 1.0);

                // The look the eye must get wrong if the doors are dropped: stopped by the honest sight set,
                // clear through the walls-only index. (A look that some jamb also blocks proves nothing
                // either way, and is simply not counted.)
                if (!SurfaceCollision.HasLineOfSight(ax, ay, bx, by, plain)
                    && SurfaceCollision.HasLineOfSight(ax, ay, bx, by, desynced))
                {
                    doorsProved++;
                }
            }
        }

        Assert.True(doorsProved > 10,
            $"only {doorsProved} shut doors were caught by the desync — this test can no longer tell a " +
            "walls-only index from the honest sight set, so the sweep above is proving nothing.");
    }

    // ── THE PAGE ITSELF ───────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    /// <summary>
    /// The one thing the sweep above CANNOT see: whether the game actually hands the eye the index. Both
    /// answers are identical by law, so a page that quietly went back to returning the plain list would be
    /// perfectly green and 29× slower — which is the whole of what #858 fixes. Pinned in the page, in
    /// <see cref="TheRoundIsNotStandingStillTests"/>'s idiom.
    /// </summary>
    [Fact]
    public void ThePageFilesItsSightBlockersIntoTheIndexAndKeepsThem()
    {
        string sight = Between(
            Pages("Map.Surface.Reevers.cs"),
            "private IReadOnlyList<SurfaceCollision.Segment> SightBlockers()",
            "private bool IsDoorShut(");

        // Built FROM the list this method used to return — one source of truth, never a second wall list.
        Assert.Contains("SurfaceCollision.WallIndex.Build(_sightBlockers)", sight, StringComparison.Ordinal);
        Assert.Contains("return _sightIndex", sight, StringComparison.Ordinal);

        // …and kept: the old shape refilled it on every call, whether or not anybody was looking.
        Assert.DoesNotContain("return _sightBlockers;", sight, StringComparison.Ordinal);
    }
}
