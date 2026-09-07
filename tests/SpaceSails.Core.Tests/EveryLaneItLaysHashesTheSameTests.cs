using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #870 lane 7e · THE SNAPSHOT UNDER <see cref="PatrolBeat.KeepRight"/>.
///
/// <para>#831's lane is not a value, it is a SEQUENCE: a straightness test, an ease ramp, an offset, and a
/// ground check that steps the offset back down a notch at a time. Which of those runs before which is the
/// whole content of the method — ease the shift before you decide which waypoints are on a straight run and
/// you get a different line down the same corridor, one that still passes every existing lane guard because
/// every existing lane guard asks a QUESTION of the answer (is it shifted? is it inside a wall? is the
/// corner untouched?) rather than pinning the answer itself.</para>
///
/// <para>So this file pins the answer itself. Every case is serialised as its point list — both coordinates
/// at round-trip ("R") precision, exactly as the method returns them, because <c>KeepRight</c> rounds
/// nothing and a guard that rounded would be choosing what it is willing to notice — and sha256'd. The
/// digests below were taken <b>on the old code</b>, on the 119-line single-method <c>KeepRight</c>, and
/// committed before a line of it moved.</para>
///
/// <para><b>Every branch, both sides.</b> Real legs off real generated floors (luna B1, a middle floor, the
/// deepest floor it patrols; europa B4, the floor whose corner clipping #831's crew found; phobos B1) walked
/// with the same A* and the same collision list Core measures its own checkpoints against — and beside each
/// one the CENTRE line the same legs walk when the caller declines the lane (a re-plan: <c>Retries &gt; 0</c>
/// in <c>Map.Patrol.Round</c> hands <c>planned.Route.Route</c> straight to <c>AutoWalk.Along</c>), so both
/// sides of that branch are written down. Then the hand-built ones, one per clause: all four headings, a leg
/// shorter than the ease ramp, a corner, a rib mouth's two turns, a repeated waypoint, an offset the ground
/// refuses, piers on the lane's own line, a segment refused with both ends already on the centre, and the
/// two routes too short to lane at all.</para>
/// </summary>
[SlowGate] // #251 · 18 s over 3 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class EveryLaneItLaysHashesTheSameTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>A body's own half-width. <c>DeckPlan.AvatarRadius</c> is the client's constant and this is
    /// Core; the existing Core lane guards write the same 0.7 for the same reason.</summary>
    private const double BodyRadius = 0.7;

    /// <summary>The lane each case lays, as "<c>points sha256</c>" — TAKEN ON THE OLD CODE.</summary>
    private static readonly Dictionary<string, string> Pinned = new(StringComparer.Ordinal)
    {
        ["luna B1 · the lane"] = "1084 3fa7ab0dec6c289368411abb587fbee0f0c870d674ca1298207ad4314f4a5d81",
        ["luna B1 · the centre line a re-plan walks"] = "1084 53602bffc3fa2538bee80d5dbfb09f1ca8a10dede9ab8cd444363da162e12638",
        ["luna · a middle floor · the lane"] = "1268 05359e75ef584442ea0a4a61043ce788de2083a88e3c99cdbedf8fc5539b9840",
        ["luna · a middle floor · the centre line a re-plan walks"] = "1268 2be5386228abbe4d53008d384a7409645edeb3fea8fbe951dc008cd2ec948c14",
        ["luna · the deepest floor it patrols · the lane"] = "1471 aecea75323f28236f09f1490f4a31a4775e3da36bc5302443e0accef4dff727b",
        ["luna · the deepest floor it patrols · the centre line a re-plan walks"] = "1471 35d5f9b35722d0100e43a3640ae38280c2ee9740221cb6d770d0bb763fe359de",
        ["europa B4 · the lane"] = "1115 5edf32a839ae4c5464259f411b16a757ba093038e5ea2b3e6cd476aa0bf3b499",
        ["europa B4 · the centre line a re-plan walks"] = "1115 d12b6c3d346e545b7f3b3bf7d277d89219f714d64fa7e979895c3a4aa2a5b483",
        ["phobos B1 · the lane"] = "1098 904c7a91b93bb6dbeca065cae65ce3b42fc7d06deb793b3ac78ec334ae304027",
        ["phobos B1 · the centre line a re-plan walks"] = "1098 982cc4139fbb5e1ada15bff44a9a268d32ee15803c24d763896bd9950f68b485",
        ["a straight leg east · the right hand is south"] = "121 93f54dcc82c28da799a8c0dffd40e95eb9c4bd2d8cead2ced833cc14aa68496e",
        ["a straight leg west · the right hand is north"] = "121 3d62226f7be8c6df25f5e13d8f5080d411abefed271bc70d1cab87df1a2bcf97",
        ["a straight leg north · the right hand is east"] = "121 092396cb5112906ea12b67bd7ce878fd0454d429cae8c1df6e44ef571bd258d9",
        ["a straight leg south · the right hand is west"] = "121 39676069f93d23a0186bf4e29562f385dc1239bcb80e6a99f4158831ccce8202",
        ["a leg shorter than the ease ramp"] = "4 4c9f347678ed8cedd9f7511227de984841076d755bc5a847764bee8409f62121",
        ["a corner · the lane is given up"] = "41 ffcfe2898bcba93e1375acc44eb932a004abed737d95556a4b939a6f0aae2912",
        ["a rib mouth · two turns and the straight between them"] = "47 c2680a21838d891b836aec32f9f3dc3aef601bea8d5181b16ee7de9ba2284a2d",
        ["a repeated waypoint · no heading to take a normal off"] = "42 41ddbeed79db4426fe07b629b68259f595361a6a73fc2b56f0cca403f1780e0b",
        ["the offset would stand a body in stone · stepped down"] = "41 91a55c6ab069a97356ccb685615c9a2ae94e63763a3167c529419bfee6cfc4d5",
        ["piers on the lane's own line · the hop is refused"] = "121 9953c9d5bf1c00990f12c1078120ab4c1a23190493c631f0f4fff3da5c063747",
        ["both ends on the centre and still refused · the A*'s own segment"] = "41 93ed9df9a3fe3b6a13888486217c9b19e6b1d9ab87d4cc0750c73c4dbaeb37f4",
        ["two points · too short to lane"] = "2 a7620f74c519e64e5258ad490663eb7fc96442a7be2881ba156db48949463add",
        ["no route at all"] = "0 e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    };

    /// <summary>
    /// EVERY LANE IT LAYS HASHES THE SAME AS IT DID BEFORE THE SPLIT.
    ///
    /// <para>A red run names every case that moved and prints both digests, so the failure is a list of
    /// places rather than one number that differs.</para>
    /// </summary>
    [Fact]
    public void EveryCaseLaysTheLaneItLaidOnTheOldCode()
    {
        var moved = new List<string>();
        var missing = new List<string>();

        foreach ((string name, IReadOnlyList<DeckReachability.Point> line) in EveryCase())
        {
            string now = $"{line.Count} {Sha256Of(line)}";
            if (!Pinned.TryGetValue(name, out string? was) || was.Length == 0)
            {
                missing.Add($"  [\"{name}\"] = \"{now}\",");
                continue;
            }
            if (!string.Equals(was, now, StringComparison.Ordinal))
            {
                moved.Add($"  {name}\n      now    {now}\n      pinned {was}");
            }
        }

        if (missing.Count > 0)
        {
            Assert.Fail(
                $"{missing.Count} case(s) carry no pin. A snapshot is taken on the OLD code and never on " +
                "the new; if these are being filled in from a run of a CHANGED KeepRight, stop.\n" +
                string.Join("\n", missing));
        }

        if (moved.Count > 0)
        {
            Assert.Fail(
                $"{moved.Count} case(s) lay a different lane than the one pinned on the old code:\n" +
                string.Join("\n", moved));
        }
    }

    /// <summary>THE LANE IS NOT THE CENTRE LINE. Ten of the cases above come in pairs, and a pair whose two
    /// digests agreed would be a snapshot of a lane that never moved anybody — the green that asserts
    /// nothing. This says out loud that every real floor is actually laned, and by how many waypoints.</summary>
    [Fact]
    public void EveryRealFloorIsActuallyLaned()
    {
        int floors = 0, shifted = 0;

        foreach ((string body, int level, string label) in RealFloors())
        {
            (IReadOnlyList<DeckReachability.Point> laned, IReadOnlyList<DeckReachability.Point> centre) =
                TheRoundOn(body, level);
            Assert.Equal(centre.Count, laned.Count);
            Assert.True(centre.Count > 100, $"{label} planned only {centre.Count} waypoints.");

            int here = 0;
            for (int i = 0; i < laned.Count; i++)
            {
                if (Math.Abs(laned[i].X - centre[i].X) + Math.Abs(laned[i].Y - centre[i].Y) > 1e-9)
                {
                    here++;
                }
            }

            Assert.True(here > 0, $"{label}: not one waypoint of {laned.Count} was moved off the centre.");
            floors++;
            shifted += here;
        }

        Assert.Equal(5, floors);
        Assert.True(shifted > 500,
            $"only {shifted} waypoints over {floors} floors were moved off the centre line.");
    }

    /// <summary>THE LANE IS A FUNCTION AND NOT A STATE. Asked twice for the same route it answers the same
    /// list — the clause that catches a step that kept something between calls.</summary>
    [Fact]
    public void AskedTwiceItAnswersTheSame()
    {
        foreach ((string name, IReadOnlyList<DeckReachability.Point> line) in EveryCase())
        {
            Assert.Equal(Sha256Of(line), Sha256Of(line));
        }

        double half = UndergroundComplex.CorridorHalf;
        SurfaceCollision.Segment[] walls = [new(-40, -half, 40, -half), new(-40, +half, 40, +half)];
        IReadOnlyList<DeckReachability.Point> route = Straight(-30, 0, +30, 0);

        Assert.Equal(
            Sha256Of(PatrolBeat.KeepRight(route, walls, BodyRadius)),
            Sha256Of(PatrolBeat.KeepRight(route, walls, BodyRadius)));
    }

    // ── THE CASES ─────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<(string Name, IReadOnlyList<DeckReachability.Point> Line)> EveryCase()
    {
        foreach ((string body, int level, string label) in RealFloors())
        {
            (IReadOnlyList<DeckReachability.Point> laned, IReadOnlyList<DeckReachability.Point> centre) =
                TheRoundOn(body, level);
            yield return ($"{label} · the lane", laned);
            yield return ($"{label} · the centre line a re-plan walks", centre);
        }

        // A bare corridor, one corridor-width across, and nothing else in the world. All four headings,
        // because the sign of the normal is (ay, −ax) and a sign is exactly the thing a reorder flips.
        double half = UndergroundComplex.CorridorHalf;
        SurfaceCollision.Segment[] eastWest = [new(-40, -half, 40, -half), new(-40, +half, 40, +half)];
        SurfaceCollision.Segment[] northSouth = [new(-half, -40, -half, 40), new(+half, -40, +half, 40)];

        yield return ("a straight leg east · the right hand is south",
            PatrolBeat.KeepRight(Straight(-30, 0, +30, 0), eastWest, BodyRadius));
        yield return ("a straight leg west · the right hand is north",
            PatrolBeat.KeepRight(Straight(+30, 0, -30, 0), eastWest, BodyRadius));
        yield return ("a straight leg north · the right hand is east",
            PatrolBeat.KeepRight(Straight(0, -30, 0, +30), northSouth, BodyRadius));
        yield return ("a straight leg south · the right hand is west",
            PatrolBeat.KeepRight(Straight(0, +30, 0, -30), northSouth, BodyRadius));

        // Shorter than LaneEaseCells, so the ramp never reaches 1 and every waypoint is a fraction of the
        // offset — the case that tells an eased lane from a lane applied whole.
        yield return ("a leg shorter than the ease ramp",
            PatrolBeat.KeepRight(Straight(-0.75, 0, +0.75, 0), eastWest, BodyRadius));

        // The turn. Both legs are straight runs; the waypoint between them is not, and neither is anything
        // within the ramp of it.
        var corner = new List<DeckReachability.Point>();
        for (double x = 0; x <= 10; x += 0.5)
        {
            corner.Add(new DeckReachability.Point(x, 0));
        }
        for (double y = 0.5; y <= 10; y += 0.5)
        {
            corner.Add(new DeckReachability.Point(10, y));
        }
        yield return ("a corner · the lane is given up", PatrolBeat.KeepRight(corner, [], BodyRadius));

        // A rib mouth: out of the spine, across, and back onto it — two turns close enough together that
        // the ramps between them overlap, which is where the min() of the two runs earns its keep.
        var mouth = new List<DeckReachability.Point>();
        for (double x = -10; x <= 0; x += 0.5)
        {
            mouth.Add(new DeckReachability.Point(x, 0));
        }
        for (double y = 0.5; y <= 3; y += 0.5)
        {
            mouth.Add(new DeckReachability.Point(0, y));
        }
        for (double x = 0.5; x <= 10; x += 0.5)
        {
            mouth.Add(new DeckReachability.Point(x, 3));
        }
        yield return ("a rib mouth · two turns and the straight between them",
            PatrolBeat.KeepRight(mouth, [], BodyRadius));

        // A waypoint on top of its neighbour: no heading, so no normal, so no lane — the degenerate leg
        // that a search on a lattice can hand back at a stop it is already standing on.
        var repeated = new List<DeckReachability.Point>(Straight(-10, 0, +10, 0));
        repeated.Insert(repeated.Count / 2, repeated[repeated.Count / 2]);
        yield return ("a repeated waypoint · no heading to take a normal off",
            PatrolBeat.KeepRight(repeated, eastWest, BodyRadius));

        // The right hand of an eastbound walk, blocked solid: the offset is stepped back down a notch at a
        // time until a body fits, rather than dropped to the middle and back.
        yield return ("the offset would stand a body in stone · stepped down",
            PatrolBeat.KeepRight(Straight(-10, 2, 10, 2), [new(-40, -0.2, 40, -0.2)], BodyRadius));

        // Piers standing on the lane's own line, spaced so a waypoint can sit BETWEEN two of them: the
        // clause that asks about the hop and not only about the waypoint.
        var piers = new List<SurfaceCollision.Segment>
        {
            new(-40, -half, 40, -half),
            new(-40, +half, 40, +half),
        };
        double lane = -PatrolBeat.LaneOffsetDu;
        for (double x = -20; x <= 20; x += 3.1)
        {
            piers.Add(new SurfaceCollision.Segment(x, lane - 0.4, x, lane + 0.4));
        }
        yield return ("piers on the lane's own line · the hop is refused",
            PatrolBeat.KeepRight(Straight(-30, 0, 30, 0), piers, BodyRadius));

        // A wall across the route itself. Both ends come back to the centre line and it is STILL refused —
        // the A*'s own segment, which a lane is not allowed to have an opinion about.
        yield return ("both ends on the centre and still refused · the A*'s own segment",
            PatrolBeat.KeepRight(Straight(-10, 0, 10, 0), [new(0, -5, 0, 5)], BodyRadius));

        // Too short to have a middle at all.
        yield return ("two points · too short to lane", PatrolBeat.KeepRight(
            [new DeckReachability.Point(0, 0), new DeckReachability.Point(1, 0)], eastWest, BodyRadius));
        yield return ("no route at all", PatrolBeat.KeepRight(null, eastWest, BodyRadius));
    }

    /// <summary>The floors the lane is taken on. luna's middle and deepest are read off the generator rather
    /// than typed, so a floor list that changes shape moves a digest instead of quietly dropping a case.
    /// </summary>
    private static IEnumerable<(string Body, int Level, string Label)> RealFloors()
    {
        yield return ("luna", -1, "luna B1");

        var patrolled = new List<int>();
        foreach (int level in UndergroundComplex.FloorsOf("luna"))
        {
            if (PatrolBeat.IsPatrolled("luna", level))
            {
                patrolled.Add(level);
            }
        }
        Assert.True(patrolled.Count >= 4, $"luna patrols only {patrolled.Count} floors.");

        yield return ("luna", patrolled[patrolled.Count / 2], "luna · a middle floor");
        yield return ("luna", patrolled[^1], "luna · the deepest floor it patrols");
        yield return ("europa", -4, "europa B4");
        yield return ("phobos", -1, "phobos B1");
    }

    /// <summary>Every leg of one floor's round, planned with the same A* the game plans it with, laid end to
    /// end — both as the lane leaves it and as the centre line a re-plan walks instead.</summary>
    private static (IReadOnlyList<DeckReachability.Point> Laned, IReadOnlyList<DeckReachability.Point> Centre)
        TheRoundOn(string body, int level)
    {
        Assert.True(PatrolBeat.IsPatrolled(body, level), $"{body} B{-level} is not patrolled.");

        UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
        List<SurfaceCollision.Segment> walls = Blockers(in plan);
        IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor(body, level, 0, plan, Field);
        Assert.True(beat.Count >= 2, $"{body} B{-level} has a round of {beat.Count} stops.");

        var laned = new List<DeckReachability.Point>();
        var centre = new List<DeckReachability.Point>();

        for (int i = 0; i < beat.Count; i++)
        {
            PatrolBeat.Stop here = beat[i], next = beat[(i + 1) % beat.Count];
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true,
                new DeckReachability.Point(here.X, here.Y),
                new DeckReachability.Point(next.X, next.Y),
                walls, BodyRadius, PatrolBeat.LatticeFor(here, next, Field));
            if (planned.Route is null)
            {
                continue;
            }

            centre.AddRange(planned.Route.Route);
            laned.AddRange(PatrolBeat.KeepRight(planned.Route.Route, walls, BodyRadius));
        }

        return (laned, centre);
    }

    /// <summary>Everything on this floor a body collides with — the same three published lists Core's own
    /// checkpoint measurement concatenates (walls, #759's glazing, the doors that never open).</summary>
    private static List<SurfaceCollision.Segment> Blockers(in UndergroundComplex.FloorPlan floor)
    {
        var segs = new List<SurfaceCollision.Segment>();
        foreach (SurfaceLayout.Wall w in floor.Walls ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }
        foreach (SurfaceLayout.Wall w in floor.Windows ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            segs.Add(new SurfaceCollision.Segment(l.X1, l.Y1, l.X2, l.Y2));
        }
        return segs;
    }

    /// <summary>A dense straight line of waypoints, the shape the lattice A* hands back.</summary>
    private static IReadOnlyList<DeckReachability.Point> Straight(
        double x0, double y0, double x1, double y1)
    {
        var pts = new List<DeckReachability.Point>();
        double dx = x1 - x0, dy = y1 - y0;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        int steps = (int)Math.Round(len / DeckReachability.DefaultStep);
        for (int i = 0; i <= steps; i++)
        {
            pts.Add(new DeckReachability.Point(x0 + (dx * i / steps), y0 + (dy * i / steps)));
        }
        return pts;
    }

    /// <summary>The point list, both coordinates at round-trip precision because <c>KeepRight</c> rounds
    /// nothing, one waypoint a line.</summary>
    private static string Sha256Of(IReadOnlyList<DeckReachability.Point> line)
    {
        var sb = new StringBuilder();
        foreach (DeckReachability.Point p in line)
        {
            sb.Append(p.X.ToString("R", CultureInfo.InvariantCulture))
              .Append(' ')
              .Append(p.Y.ToString("R", CultureInfo.InvariantCulture))
              .Append('\n');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))
            .ToLowerInvariant();
    }
}
