using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #831's cover act — what a made tail does instead of freezing bare (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #831 · THE COVER ACT: WHAT A MADE TAIL DOES INSTEAD OF FREEZING BARE ──────────────────────────
    //
    // Owner's law, generalised from the same sighting: "A MADE tail performs a COVER ACT instead of freezing
    // bare: turns to the nearest wall fixture, checks a plate, reads a docket — same hold, same honest Vx=0,
    // but the picture says 'man with business' not 'statue'."
    //
    // FootTail.MustHold is untouched and stays untouched: a made tail still stops dead where the law says he
    // stops, and he still drops off the motion fan honestly while he does it. What changes is the PICTURE —
    // he turns to the nearest station and reads it, and if there is nothing near he takes the few du to one
    // first. Nothing is said, no card goes up, and the tell is a thing the player watches rather than a thing
    // the game announces: a man signing a plate he signed ninety seconds ago is the gumshoe's confirmation,
    // and it costs this file no prose at all.

    /// <summary>#831 · SOMETHING ON A WALL A MAN CAN PLAUSIBLY BE READING. A watchclock station, the
    /// department painted on a door that never opens, a framed poster nobody has taken down since
    /// 1970-something. Published as one flat list so the cover act asks ONE question of the floor rather than
    /// growing an opinion per fixture kind.</summary>
    /// <param name="X">Where the thing is, on the wall it is bolted to.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Point">The checkpoint's own number when it is one, 0 for anything else somebody bolted
    /// there, and <see cref="TheStoneItself"/> for a bare face of wall — which is what makes the DOUBLE
    /// SIGN-IN readable at all, and what lets an audit count how much of a round runs past nothing.</param>
    public readonly record struct WallThing(double X, double Y, int Point);

    /// <summary>#831 · What <see cref="WallThing.Point"/> carries when the thing is not a thing at all but a
    /// bare face of wall (<see cref="TheNearestFace"/>). Negative on purpose: every test that asks "is this a
    /// station" asks <c>&gt; 0</c>, so the last resort can never be counted as a sign-in.</summary>
    public const int TheStoneItself = -1;

    /// <summary>#831 · How close he gets to the thing he has decided to read. Arm's length and a step, the
    /// same distance <see cref="CardReachDu"/> says two people talk at.</summary>
    public const double CoverStandDu = 2.0;

    /// <summary>#831 · …and how far he will DRIFT to find one. Owner: <i>"Mid-corridor with nothing near: he
    /// does not hold there — he drifts the few du to the nearest fixture first, then holds."</i> A few du and
    /// no more: a man who walked half a corridor to find a plate would be walking on past you, which is the
    /// one thing the hold exists to stop.</summary>
    public static double CoverDriftDu => MarkerSightDu / 2.0;

    /// <summary>#831 · The longest he will spend getting to it — exactly the time the drift's own distance
    /// takes to walk, so the bound is the same fact as the reach rather than a second number that could drift
    /// from it. Past it he reads whatever he chose from where he has got to, which is what a person does when
    /// a bench or a rail turns out to be in the way: he does not keep shuffling at it forever, and a body
    /// still shuffling is a body the motion fan would honestly call a mover.</summary>
    public static double CoverDriftSeconds => CoverDriftDu / WalkSpeed;

    /// <summary>#831 · Everything on this floor's walls a held man could be reading — the stations, the shut
    /// doors' own signs, the posters, the plated fittings in every room and the bins somebody has to empty.
    /// One call, in the renderer's own idiom: it is a fact about the floor rather than a search a stepper
    /// does per frame.
    ///
    /// <para>It is deliberately WIDE. The owner's clause is <i>"turns to the nearest wall fixture, checks a
    /// plate, reads a docket"</i> — the point is that a facility is covered in things a man with a clipboard
    /// has business with, and a list that only knew about the round's own stations would leave a held tail
    /// standing bare halfway down every leg, which is the exact picture this issue exists to end.</para></summary>
    public static IReadOnlyList<WallThing> ReadablesOn(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var things = new List<WallThing>();
        foreach (Checkpoint p in CheckpointsOn(in floor, in field))
        {
            things.Add(new WallThing(p.X, p.Y, p.Number));
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            things.Add(new WallThing((l.X1 + l.X2) / 2.0, (l.Y1 + l.Y2) / 2.0, 0));
        }
        foreach (LabPosters.Poster poster in floor.TheWalls)
        {
            things.Add(new WallThing(poster.X, poster.Y, 0));
        }
        foreach (SurfaceLayout.Doorway d in floor.Doorways ?? [])
        {
            things.Add(new WallThing((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0, 0));
        }
        foreach (SurfaceLayout.Landmark m in floor.Labels ?? [])
        {
            things.Add(new WallThing(m.X, m.Y, 0));
        }
        foreach (RipAndBin.Bin bin in floor.TheBins)
        {
            things.Add(new WallThing(bin.X, bin.Y, 0));
        }
        foreach (UndergroundComplex.Room room in floor.TheRooms)
        {
            foreach (RingOffice.Fixture fitting in room.Furniture)
            {
                if (fitting.Plate.Length > 0)
                {
                    things.Add(new WallThing(fitting.X, fitting.Y, 0));
                }
            }
        }
        return things;
    }

    /// <summary>#831 · Which of them a held man makes his business — and, when the building has nothing
    /// published within <see cref="CoverDriftDu"/> of him, THE WALL ITSELF (<see cref="TheNearestFace"/>).
    /// Null only where there is no stone within reach either, which on these floors is nowhere.
    ///
    /// <para><b>He has to be able to SEE it.</b> A plate through a wall is not a thing a man was about to
    /// read, and a cover act that set off toward one would be a body sliding along shotcrete for ten seconds.
    /// The look is <see cref="SurfaceCollision.HasLineOfSight"/> — the game's one wall law — taken to the
    /// square he would end up standing on rather than to the plate itself, because the plate is bolted to a
    /// wall and a sightline that ends inside one answers nothing.</para>
    ///
    /// <para><b>The published list is tried FIRST and the wall is the floor under it</b>, never the other way
    /// round: a man with a plate in reach reads the plate, and it is only the bare stretch of spine between
    /// two rib mouths — a fifth of this round, measured — that gets the wall. Cheap in the order that
    /// matters: the fallback is only ever computed for a man the fixtures could not answer.</para></summary>
    public static WallThing? CoverFor(
        double x, double y,
        IReadOnlyList<WallThing>? things,
        IReadOnlyList<SurfaceCollision.Segment>? sight,
        IReadOnlyList<SurfaceCollision.Segment>? stone)
    {
        WallThing? found = TheNearestFixture(x, y, things, sight);
        return found ?? TheNearestFace(x, y, stone, sight);
    }

    /// <summary>#831 · The nearest thing somebody BOLTED to a wall that he can see and reach.</summary>
    private static WallThing? TheNearestFixture(
        double x, double y,
        IReadOnlyList<WallThing>? things,
        IReadOnlyList<SurfaceCollision.Segment>? sight)
    {
        if (things is null)
        {
            return null;
        }

        WallThing? best = null;
        double bestGap = CoverDriftDu;
        foreach (WallThing t in things)
        {
            double dx = t.X - x, dy = t.Y - y;
            double gap = Math.Sqrt((dx * dx) + (dy * dy));
            if (gap >= bestGap || gap < 1e-9)
            {
                continue;
            }

            double reach = Math.Max(0, gap - CoverStandDu);
            if (!SurfaceCollision.HasLineOfSight(
                    x, y, x + (dx / gap * reach), y + (dy / gap * reach), sight))
            {
                continue;
            }

            bestGap = gap;
            best = t;
        }
        return best;
    }

    /// <summary>
    /// #831 · …AND WHEN THERE IS NOTHING ON THE WALL, THE WALL.
    ///
    /// <para><b>This is the owner's actual complaint, answered where it happens.</b> He watched a man
    /// <i>"in the middle of the corridor"</i> and asked <i>"why would it just stand there"</i>. Measured over
    /// every leg of every round on every patrolled floor of all four sites, a FIFTH of the distance walked is
    /// bare spine: the nearest thing anybody has bolted to a wall is a median 19 du off and as much as 31,
    /// and the drift is a few du by design. Leaving those holds bare would have left the exact picture this
    /// issue was filed about standing in a fifth of the building.</para>
    ///
    /// <para>So the last resort is the stone. He steps out of the middle to a pace off the nearest face and
    /// turns to it — which is the two owner sentences of this issue meeting: <i>"they should respect right
    /// side traffic, and not walk in the middle of the corridor"</i> is what a person does when they stop in
    /// a passage, and a man a pace off a wall with his back to you reads as a man looking at something rather
    /// than a man looking at YOU. It carries <c>Point = 0</c>, so it is never a sign-in and can never be
    /// mistaken for one by <see cref="DoubleSignIn"/>.</para>
    ///
    /// <para>Measured against the field a body COLLIDES with rather than the drawn wall list, because the
    /// thing he ends up standing a pace off has to be the thing that stopped him.</para>
    /// </summary>
    public static WallThing? TheNearestFace(
        double x, double y,
        IReadOnlyList<SurfaceCollision.Segment>? stone,
        IReadOnlyList<SurfaceCollision.Segment>? sight)
    {
        if (stone is null)
        {
            return null;
        }

        WallThing? best = null;
        double bestGap = CoverDriftDu;
        foreach (SurfaceCollision.Segment s in stone)
        {
            (double px, double py) = NearestOnSegment(x, y, s.X1, s.Y1, s.X2, s.Y2);
            double dx = px - x, dy = py - y;
            double gap = Math.Sqrt((dx * dx) + (dy * dy));

            // A face he is already inside is not a face he can turn to: the heading would be undefined and
            // the picture would be a man with his nose in the shotcrete.
            if (gap >= bestGap || gap < SignInClearDu)
            {
                continue;
            }

            double reach = Math.Max(0, gap - CoverStandDu);
            if (!SurfaceCollision.HasLineOfSight(
                    x, y, x + (dx / gap * reach), y + (dy / gap * reach), sight))
            {
                continue;
            }

            bestGap = gap;
            best = new WallThing(px, py, TheStoneItself);
        }
        return best;
    }

    /// <summary>#831 · Is he there yet, or is he still taking the few du to it?</summary>
    public static bool AtTheCover(double x, double y, in WallThing thing)
    {
        double dx = thing.X - x, dy = thing.Y - y;
        return (dx * dx) + (dy * dy) <= CoverStandDu * CoverStandDu;
    }

    /// <summary>#831 · THE TELL. Is this man signing a station he already signed on this round? The gumshoe's
    /// confirmation that the tail is made, stated once here so the sim and any audit of it read one sentence
    /// — and deliberately not said out loud anywhere: the double sign-in is watched, never narrated.</summary>
    public static bool DoubleSignIn(int signingNow, int alreadySigned) =>
        signingNow > 0 && signingNow == alreadySigned;
}
