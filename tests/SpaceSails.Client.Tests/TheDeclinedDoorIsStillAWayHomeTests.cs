using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1068 · <b>THE WORLD MAY DECLINE, BUT IT MAY NEVER TRAP.</b> Channel one of the watchers' manifestations
/// (#672) shuts one leaf on the concourse of a ground the captain opened. Core proves it is one leaf and that
/// the room could spare it; <b>this walks the deck the captain's boots actually collide with</b> and proves
/// the two things a geometry argument cannot: that the shut leaf really is shut, and that the lift is still a
/// way HOME from everywhere on the floor.
///
/// <para>#600 is why the second half is stated that way round. The A* audit had proved for months that a
/// captain can REACH the lift and never once that the lift is a way home — the car only went down, and every
/// build was green the whole time. So the flood here runs BOTH ways: from the car to every place the floor
/// offers, and from every one of those places back to the car.</para>
///
/// <para>The pair matters as much as either half. A guard that only checked "everything is still reachable"
/// would pass perfectly on a decline that did nothing at all, which is the fifth named bug class on this
/// ground — a world that cannot tell pass from fail. So the first guard proves the leaf is a wall and the
/// second proves the wall costs nobody their way out.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 30 s over 3 test(s), measured 2026-09-02; see TheSlowGateRosterTests.
public sealed class TheDeclinedDoorIsStillAWayHomeTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The one ground in the game guaranteed to have halls to open, and therefore the one guaranteed
    /// to be able to decline. Without it every sweep in this file would audit a universe where nothing has
    /// ever been opened and pass for the wrong reason.</summary>
    private static string Ground => UndergroundComplex.FoundBandCheatSiteId;

    private static void WithDeclined(long window, Action body)
    {
        IReadOnlyList<PoliteDecline.Decline> had = PoliteDecline.Declined;
        try
        {
            PoliteDecline.Install([new PoliteDecline.Decline(Ground, window)]);
            body();
        }
        finally
        {
            PoliteDecline.Install(had);
        }
    }

    /// <summary>Which floor of the site the world declines on — the concourse, asked of the building rather
    /// than assumed, and asserted to be exactly one floor.</summary>
    private static int TheConcourse()
    {
        int? found = null;
        WithDeclined(0, () =>
        {
            foreach (int level in UndergroundComplex.FloorsOf(Ground))
            {
                if (UndergroundComplex.DeclinesOn(Ground, level))
                {
                    Assert.True(found is null, $"{Ground} declines on two floors — one door, one floor.");
                    found = level;
                }
            }
        });
        Assert.True(found is not null, $"{Ground} declines on no floor at all — this suite proves nothing.");
        return found!.Value;
    }

    /// <summary>EVERY WINDOW IN THE FIRST DOZEN THE WORLD TOOK ANYTHING IN, and WHICH leaves it took — both
    /// derived and never typed, by building the concourse with the register installed and without it and
    /// reading the difference off the two doorway lists.
    ///
    /// <para>A dozen rather than one, because which leaf the seed picks is a fact about the window: a walk
    /// run against a single window is a walk against one of two dozen candidate doors, and the other
    /// twenty-three would go unexamined.</para>
    ///
    /// <para><b>It counts what was taken rather than assuming one.</b> "Exactly one leaf" is a LAW here and
    /// not a helper's precondition — a helper that skipped any window where two doors went would have
    /// quietly excused the very bug the law is about, which is the fifth named bug class wearing a
    /// convenience.</para></summary>
    private static List<(long Window, List<SurfaceLayout.Doorway> Leaves)> WhatTheWorldTook(int level)
    {
        IReadOnlyList<SurfaceLayout.Doorway> open =
            UndergroundComplex.Build(Ground, level, Field).Doorways;
        var taken = new List<(long, List<SurfaceLayout.Doorway>)>();

        for (long window = 0; window < 12; window++)
        {
            IReadOnlyList<SurfaceLayout.Doorway> shut = open;
            WithDeclined(window, () => shut = UndergroundComplex.Build(Ground, level, Field).Doorways);
            List<SurfaceLayout.Doorway> gone = open.Where(d => !shut.Any(x => Same(x, d))).ToList();
            if (gone.Count > 0)
            {
                taken.Add((window, gone));
            }
        }

        Assert.True(taken.Count > 6,
            $"only {taken.Count} of 12 windows took a door on {Ground} B{-level} — this proves little.");
        return taken;
    }

    private static bool Same(in SurfaceLayout.Doorway a, in SurfaceLayout.Doorway b) =>
        Math.Abs(a.X1 - b.X1) < 1e-9 && Math.Abs(a.Y1 - b.Y1) < 1e-9
        && Math.Abs(a.X2 - b.X2) < 1e-9 && Math.Abs(a.Y2 - b.Y2) < 1e-9;

    private static DeckPlan DeckFor(int level) =>
        HiveInterior.FloorDeck(Ground, level, Field, 0, (_, _) => { }, []);

    /// <summary>Two points, one either side of a leaf, and the little box that contains both — the whole
    /// question "is this door a way through" in three numbers. <b>The box is bounded to the jamb's own
    /// neighbourhood on purpose</b>: a whole-field flood would find the way round through the corridor and
    /// report success about a door that is bricked up, which is #600's lesson said about a doorway
    /// (<c>TheCirculationIsWalkableTests</c> uses the identical box for the identical reason).</summary>
    private static (DeckReachability.Point A, DeckReachability.Point B,
        (double, double, double, double) Bounds) AcrossTheJamb(double x1, double y1, double x2, double y2)
    {
        const double step = 2.0;
        double cx = (x1 + x2) / 2.0, cy = (y1 + y2) / 2.0;
        bool horizontal = Math.Abs(y1 - y2) < 0.05;
        double ox = horizontal ? 0 : step, oy = horizontal ? step : 0;
        return (
            new DeckReachability.Point(cx - ox, cy - oy),
            new DeckReachability.Point(cx + ox, cy + oy),
            (cx - ox - 2.0, cy - oy - 2.0, cx + ox + 2.0, cy + oy + 2.0));
    }

    /// <summary>
    /// THE LEAF THAT OPENED YESTERDAY DOES NOT OPEN — on the deck, not on the plan. It is drawn as a locked
    /// door, there is a wall behind it, and a body standing on one side of it cannot get to the other side
    /// without leaving the jamb's own neighbourhood. And <b>the same jamb, in the same site, on the same
    /// floor, IS a way through in a world where nobody has been past a seam</b> — which is the half that
    /// makes this a test rather than a description.
    ///
    /// <para><b>Proved RED</b> by deleting the <c>locked.Add(...)</c> line in
    /// <c>UndergroundComplex.DeclineOneDoor</c>: <i>"the declined leaf is not drawn as a locked door on the
    /// deck"</i> — the doorway simply vanished and the wall never arrived.</para>
    /// </summary>
    [Fact]
    public void TheDeclinedLeafIsAWallOnTheDeck()
    {
        int level = TheConcourse();
        DeckPlan before = DeckFor(level);

        foreach ((long window, List<SurfaceLayout.Doorway> leaves) in WhatTheWorldTook(level))
        {
            SurfaceLayout.Doorway leaf = leaves[0];
            (DeckReachability.Point a, DeckReachability.Point b, (double, double, double, double) box) =
                AcrossTheJamb(leaf.X1, leaf.Y1, leaf.X2, leaf.Y2);

            // BEFORE: the same jamb, in a world nobody has opened anything in, is a way through. Without
            // this half the assertion below would pass just as happily on a leaf that was never a door.
            Assert.True(
                DeckReachability.CanReach(a, b, before.CollisionField, DeckPlan.AvatarRadius, box),
                $"{Ground} B{-level}: the leaf at ({leaf.X1:F0},{leaf.Y1:F0}) was not a way through even "
                + "before the world declined — this guard would prove nothing.");

            WithDeclined(window, () =>
            {
                DeckPlan after = DeckFor(level);

                Assert.True(
                    after.Doors.Any(d => d.Locked
                        && Math.Abs(d.X1 - leaf.X1) < 0.01 && Math.Abs(d.Y1 - leaf.Y1) < 0.01
                        && Math.Abs(d.X2 - leaf.X2) < 0.01 && Math.Abs(d.Y2 - leaf.Y2) < 0.01),
                    $"window {window}: the declined leaf at ({leaf.X1:F0},{leaf.Y1:F0}) is not drawn as a "
                    + "locked door on the deck.");

                Assert.False(
                    DeckReachability.CanReach(a, b, after.CollisionField, DeckPlan.AvatarRadius, box),
                    $"window {window}: the declined leaf at ({leaf.X1:F0},{leaf.Y1:F0}) still opens.");
            });
        }
    }

    /// <summary>
    /// EXACTLY ONE LEAF STOPS OPENING, AND NOT A FLOOR OF THEM. Every other doorway that was a way through
    /// before the world declined is still a way through after it — measured on the deck, both ways across
    /// each jamb, inside each jamb's own box so no route round can answer for it.
    ///
    /// <para><b>Proved RED</b> by taking every candidate leaf of the picked room instead of one:
    /// <i>"window 0: the world took 4 leaves, not one"</i>.</para>
    /// </summary>
    [Fact]
    public void EveryOtherLeafOnTheFloorStillOpens()
    {
        int level = TheConcourse();
        DeckPlan before = DeckFor(level);
        int stillOpen = 0;

        foreach ((long window, List<SurfaceLayout.Doorway> leaves) in WhatTheWorldTook(level))
        {
            // ONE LEAF. Stated here rather than assumed by the helper that found them: the whole feature is
            // that the world takes ONE door off a floor that is otherwise exactly the one he walked out of.
            Assert.True(leaves.Count == 1,
                $"window {window}: the world took {leaves.Count} leaves, not one.");
        }

        (long first, List<SurfaceLayout.Doorway> took) = WhatTheWorldTook(level)[0];
        WithDeclined(first, () =>
        {
            DeckPlan after = DeckFor(level);
            foreach (SurfaceLayout.Doorway d in UndergroundComplex.Build(Ground, level, Field).Doorways)
            {
                if (took.Any(t => Same(t, d)))
                {
                    continue;
                }
                (DeckReachability.Point p, DeckReachability.Point q, (double, double, double, double) w) =
                    AcrossTheJamb(d.X1, d.Y1, d.X2, d.Y2);
                if (!DeckReachability.CanReach(p, q, before.CollisionField, DeckPlan.AvatarRadius, w))
                {
                    continue;   // it was never a way through; the decline is not on trial for it
                }
                Assert.True(
                    DeckReachability.CanReach(q, p, after.CollisionField, DeckPlan.AvatarRadius, w),
                    $"{Ground} B{-level}: a second leaf at ({d.X1:F0},{d.Y1:F0}) stopped opening too.");
                stillOpen++;
            }
        });

        Assert.True(stillOpen > 8, $"only {stillOpen} other leaf/leaves were checked — this proves little.");
    }

    /// <summary>
    /// AND THE LIFT IS STILL A WAY HOME FROM EVERYWHERE ON THAT FLOOR. Every place the concourse offers — the
    /// searchable rooms, the amenities, the refuges, the car itself — can be walked to from the lift <b>and
    /// walked back to it</b>, with the declined leaf a wall.
    ///
    /// <para>Both directions, because #600: the audit that had proved for months that a captain can REACH
    /// the lift never once proved that the lift is a way HOME, and the whole feature it was guarding was
    /// broken the entire time. A declined door is exactly the shape of bug that would do it again.</para>
    ///
    /// <para><b>Proved RED</b> by having the decline wall the picked room's remaining ways as well as its
    /// door — one line, and the room is sealed with the floor's own offer inside it:
    /// <i>"window 0, leaf (-69,-157): 1 of 20 places cannot be reached from the lift: (-82, -183)"</i>. That is exactly the failure this walk exists to
    /// catch, and no argument about geometry would have caught it.</para>
    /// </summary>
    [Fact]
    public void EveryPlaceOnTheDeclinedFloorIsStillAWalkThereAndBack()
    {
        int level = TheConcourse();

        foreach ((long window, List<SurfaceLayout.Doorway> leaves) in WhatTheWorldTook(level))
        {
            SurfaceLayout.Doorway leaf = leaves[0];
            WithDeclined(window, () =>
            {
                DeckPlan deck = DeckFor(level);
            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            Assert.True(DeckReachability.Standable(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField),
                "the walk starts inside a wall — this floor's verdict would mean nothing.");

            List<DeckReachability.Point> targets = deck.Consoles
                .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                    or DeckPlan.ConsoleKind.HiveAmenity or DeckPlan.ConsoleKind.HiveRefuge)
                .Select(c => new DeckReachability.Point(c.X, c.Y))
                .ToList();

            Assert.True(targets.Count > 8,
                $"only {targets.Count} place(s) on the declined floor to walk to — this proves little.");

            var stranded = new List<string>();
            var marooned = new List<string>();
            foreach (DeckReachability.Point t in targets)
            {
                if (!DeckReachability.CanReach(spawn, t, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    stranded.Add($"({t.X:F0}, {t.Y:F0})");
                }
                else if (!DeckReachability.CanReach(
                    t, spawn, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    marooned.Add($"({t.X:F0}, {t.Y:F0})");
                }
            }

                Assert.True(stranded.Count == 0,
                    $"window {window}, leaf ({leaf.X1:F0},{leaf.Y1:F0}): {stranded.Count} of "
                    + $"{targets.Count} places cannot be reached from the lift: "
                    + string.Join(", ", stranded.Take(4)));
                Assert.True(marooned.Count == 0,
                    $"window {window}, leaf ({leaf.X1:F0},{leaf.Y1:F0}): {marooned.Count} of "
                    + $"{targets.Count} places are not a walk BACK to the lift: "
                    + string.Join(", ", marooned.Take(4)));
            });
        }
    }
}
