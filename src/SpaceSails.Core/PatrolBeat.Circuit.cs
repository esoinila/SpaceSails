using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE CIRCUIT AND THE BEAT — the circuit a round walks, the wanted circuit and the furthest room,
/// the beat for a floor, its lattice and start leg, and the walking constants.
///
/// <para>Split out of <c>PatrolBeat.Lane.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class PatrolBeat
{
    /// <summary>
    /// THE CIRCUIT, TAKEN OFF THE FLOOR PLAN AND NEVER OFF A CONSTANT.
    ///
    /// <para>The lift, then every rib mouth in ASCENDING X, and after each mouth the room on that rib that
    /// stands furthest from the spine. Three things about that are load-bearing:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Every stop is published geometry.</b> <see cref="UndergroundComplex.ShaftAt"/>,
    /// <c>FloorPlan.Ribs</c> (published by #587 precisely so nothing outside the generator has to do
    /// arithmetic that copies the placement) and <c>FloorPlan.RoomCentres</c>. This file computes no
    /// position of its own, which is §13.15 — a seam that re-derived a fact about a building it does not own
    /// would be the mirrored-constant bug this ground keeps paying for.</item>
    /// <item><b>Sorted before it is walked.</b> #587's own lesson, one floor along: a list built by
    /// appending is not a list in order, and a round that jumped back and forth along the spine would be
    /// unlearnable — which is the whole feature.</item>
    /// <item><b>Every stop is a place the A* audit already walks.</b> Room centres are what
    /// <c>HiveInterior</c> hangs its SEARCH THE ROOM consoles on, and §13.1's sweep proves every one of them
    /// is reachable from the car on every floor of every site. A beat built out of them is walkable by
    /// construction rather than by hope — and there is a guard that walks it anyway.</item>
    /// </list>
    ///
    /// <h3>#831 · AND EVERY STOP SNAPS TO ITS CHECKPOINT</h3>
    ///
    /// <para>Owner: <i>"why would it just stand there if there is no inspection point etc"</i> → <i>"they
    /// actually in real life like have these check points they electronically sign on rounds to prove they
    /// did their round."</i> So the coordinates above are where the round WANTS to be, and the coordinates it
    /// walks to are the square the nearest watchclock station is signed from (<see cref="PointFor"/>) — a
    /// pace and a half off a wall, facing a plate. A stop that finds no wall within
    /// <see cref="CheckpointReachDu"/> keeps its own place and says so by carrying a null
    /// <see cref="Stop.Point"/>; the audit counts those, because a stop nobody can explain is the thing this
    /// whole issue is about.</para>
    /// </summary>
    /// <param name="floor">The floor, as Core built it.</param>
    /// <param name="field">The site's envelope — the shaft's position comes from it.</param>
    public static IReadOnlyList<Stop> Circuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        IReadOnlyList<Stop> wanted = WantedCircuit(floor, field);
        List<SurfaceCollision.Segment> blockers = Blockers(in floor);

        var snapped = new List<Stop>(wanted.Count);
        for (int i = 0; i < wanted.Count; i++)
        {
            Stop stop = wanted[i];
            snapped.Add(PointFor(in stop, i + 1, in floor, blockers) is { } at
                ? stop with { X = at.StandX, Y = at.StandY, Point = at }
                : stop);
        }
        return snapped;
    }

    /// <summary>#831 · The circuit as the floor plan alone decides it, BEFORE the stations move it. Split out
    /// so the numbering of the stations is a fact about the PLACE — station 3 is station 3 on every watch —
    /// and so the snap has exactly one caller.</summary>
    private static IReadOnlyList<Stop> WantedCircuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var stops = new List<Stop>();

        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);

        // The car. It is the first stop on every round because it is the first thing anybody checks and the
        // one place on the floor a captain has to come back to.
        stops.Add(new Stop(shaftX, shaftY + 1.0, "the car"));

        // The ribs, in x order — SORTED, not as the plan happens to list them.
        var ribs = new List<UndergroundComplex.Rib>(floor.Ribs ?? []);
        ribs.Sort((a, b) => a.X.CompareTo(b.X));

        var taken = new HashSet<int>();
        foreach (UndergroundComplex.Rib rib in ribs)
        {
            // The mouth: where the cross corridor opens off the spine. This is the square a captain watches
            // from, so it has to be on the round.
            stops.Add(new Stop(rib.X, shaftY, $"the mouth at x{rib.X:F0}"));

            // …and the far room down it. Which room "belongs" to this rib is decided by nearness in x and
            // by which side of the spine the rib runs — both facts the plan publishes.
            (int Index, double X, double Y)? room = FurthestRoomOn(floor, rib, shaftY, taken);
            if (room is { } far)
            {
                taken.Add(far.Index);
                stops.Add(new Stop(far.X, far.Y, $"the far room off x{rib.X:F0}"));
            }
        }

        return stops;
    }

    /// <summary>The room furthest down one rib: nearest to the rib in x, on the rib's own side of the spine,
    /// and the deepest of those. The ordinal comes back with it so the caller can CLAIM it — two ribs
    /// sharing a stop would be a round that doubles back on a room it has just left.</summary>
    private static (int Index, double X, double Y)? FurthestRoomOn(
        in UndergroundComplex.FloorPlan floor, UndergroundComplex.Rib rib, double spineY, HashSet<int> taken)
    {
        (int Index, double X, double Y)? best = null;
        double bestDepth = -1;

        IReadOnlyList<(double X, double Y)> rooms = floor.RoomCentres ?? [];
        for (int i = 0; i < rooms.Count; i++)
        {
            if (taken.Contains(i))
            {
                continue;
            }

            (double rx, double ry) = rooms[i];

            // The rib's own side of the spine. Down means toward the deep field, away from the landing band,
            // which is the flag the plan carries for exactly this question.
            bool below = ry < spineY;
            if (below != rib.Down)
            {
                continue;
            }

            // Rooms hang off the rib they were placed along, so a room more than a room's width away in x is
            // somebody else's. RoomWidthDu is the generator's own number, read rather than guessed.
            if (Math.Abs(rx - rib.X) > UndergroundComplex.RoomWidthDu)
            {
                continue;
            }

            double depth = Math.Abs(ry - spineY);
            if (depth > bestDepth)
            {
                bestDepth = depth;
                best = (i, rx, ry);
            }
        }

        return best;
    }

    /// <summary>
    /// THE ROUND AS IT IS ACTUALLY WALKED THIS WATCH — the circuit, turned over by the shift.
    ///
    /// <para>Two things rotate, and both are seeded on (site, floor, watch): which DIRECTION the round runs,
    /// and which stop it starts from. That is the whole of "rotating guards": the same floor, the same
    /// stops, a different round — so a captain who learned one shift's round has learned the floor and not
    /// the answer, and has to watch again.</para>
    ///
    /// <para>Every guard on the floor shares this ONE list and differs only by which leg they are on
    /// (<see cref="StartLeg"/>) — the sweep team's own idiom, and for its reason: <i>being hidden from has
    /// to be legible.</i> Two rounds with two different routes would be two things to learn at once.</para>
    /// </summary>
    public static IReadOnlyList<Stop> BeatFor(
        string bodyId, int level, long watch,
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<Stop> circuit = Circuit(floor, field);
        if (circuit.Count == 0)
        {
            return circuit;
        }

        var walked = new List<Stop>(circuit);
        if (DiceRule.Roll(DiceRule.Seed($"hive:patrol:dir:{bodyId}:{level}", watch), 2).Face == 1)
        {
            walked.Reverse();
        }

        int offset = DiceRule.Roll(
            DiceRule.Seed($"hive:patrol:from:{bodyId}:{level}", watch), walked.Count).Face - 1;
        if (offset == 0)
        {
            return walked;
        }

        var rotated = new List<Stop>(walked.Count);
        for (int i = 0; i < walked.Count; i++)
        {
            rotated.Add(walked[(offset + i) % walked.Count]);
        }
        return rotated;
    }

    /// <summary>
    /// #804 · HOW BIG A LATTICE ONE LEG IS WALKED OVER, and why it is not the whole floor.
    ///
    /// <para><see cref="AutoWalk.BoundsFor"/> spans every wall it is handed, which is right for a captain
    /// clicking an arbitrary spot and wrong for a guard walking forty du down a rib: a floor-sized lattice
    /// at <see cref="DeckReachability.DefaultStep"/> is a third of a million cells, and this repo runs in
    /// WASM where a Debug build is a hundred times slower than the machine this is written on. A round that
    /// planned one of those every time it reached a stop would hitch the frame it arrived on.</para>
    ///
    /// <para><b>The margin is the whole of the safety.</b> A leg may bulge around a room to reach the next
    /// stop, so the box is the two stops plus a room's own depth on every side — derived from
    /// <c>RoomHeightDu</c> and the corridor's width rather than typed, so a generator that grows its rooms
    /// grows this with them. Clamped to the field, because nothing is built outside it.</para>
    ///
    /// <para><b>And it lives here so the audit and the game ask ONE question.</b> If the client shrank the
    /// box and the sweep walked the whole floor, the sweep would be proving a route the game never plans —
    /// two pathfinders agreeing only by luck, which is the drift <c>DeckReachability</c>'s own lattice was
    /// consolidated to stop.</para>
    /// </summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) LatticeFor(
        in Stop from, in Stop to, in SurfaceLayout.Field field)
    {
        double margin = UndergroundComplex.RoomHeightDu + (UndergroundComplex.CorridorHalf * 2);
        double minX = Math.Max(field.LeftX, Math.Min(from.X, to.X) - margin);
        double maxX = Math.Min(field.RightX, Math.Max(from.X, to.X) + margin);
        double minY = Math.Max(field.BottomY, Math.Min(from.Y, to.Y) - margin);
        double maxY = Math.Min(field.LandingBandY, Math.Max(from.Y, to.Y) + margin);
        return (minX, minY, maxX, maxY);
    }

    /// <summary>Where the <paramref name="index"/>th guard starts on a beat of <paramref name="legs"/> stops
    /// — spread evenly around it, so two of them cover the floor instead of walking in a queue. The sweep
    /// team's arithmetic, because it is the same problem.</summary>
    public static int StartLeg(int legs, int index, int count) =>
        legs <= 0 ? 0 : index * legs / Math.Max(1, count) % legs;

    /// <summary>
    /// How fast a round walks, deck units per second. SLOWER than the captain's walk on purpose: a captain
    /// can always out-walk a guard and it never helps, because the thing they are bad at is not speed.
    /// FLAGGED for the owner's tuning — this and <see cref="StandSeconds"/> are the whole timing window.
    /// </summary>
    public const double WalkSpeed = 3.2;

    /// <summary>How long they stand at a stop before walking on. THE NUMBER THE FEATURE IS ABOUT: it is the
    /// gap a captain watches for and steps into. Long enough to see from the far end of a corridor, short
    /// enough that waiting is a decision rather than a wait. FLAGGED.</summary>
    public const double StandSeconds = 5.0;

    /// <summary>Close enough to a stop to be standing at it. The A* leg lands the guard adjacent to the
    /// stop rather than on it (a room centre has a console on it), so this is the same "arrived" tolerance
    /// the sweep team's route uses.</summary>
    public const double AtTheStopDu = 1.5;

    /// <summary>#832 · How many times a leg may be re-planned after the ground refuses a step before the
    /// round gives up on that stop and walks to the next one. A snag is not an arrival and must not buy a
    /// stand — but a stop nothing can reach must not be ground at forever either, and an A* plan per frame
    /// is not free in WASM. Small on purpose: the audit (§13.1) says every leg of every round on every floor
    /// this generator builds connects, so this is the belt on top of the braces.</summary>
    public const int RePlansPerLeg = 6;

    /// <summary>#858 · How much of the NEXT leg's A* a standing guard walks per frame
    /// (<see cref="AutoWalk.Planner"/>), in lattice cells.
    ///
    /// <para>Lab 45 measured the plan the round asks for at a median 1.6–2.2 ms and a worst 6.4 ms — 38.6%
    /// of a 60 fps frame — spent whole on the frame he leaves a stop, which is the one number in that lab
    /// that can miss a frame. He is standing for <see cref="StandSeconds"/> either way; this is the rate
    /// that gets the same work done during it.</para>
    ///
    /// <para><b>Derived, not tuned.</b> The stand is 5 s ≈ 300 frames, and the biggest lattice
    /// <see cref="LatticeFor"/> poses on the floors this generator builds is 29,002 cells (Lab 45 §C). At
    /// 128 a frame the whole of that lattice is walked in ~227 frames — inside the stand with a quarter of
    /// it to spare — while one frame's share of that worst 6.4 ms plan is about a fiftieth of it. Being a
    /// CELL budget rather than a millisecond one is the point: it means the same thing in WASM, where the
    /// clock this was measured on does not.</para></summary>
    public const int PlanCellsAFrame = 128;
}
