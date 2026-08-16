using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #831's checkpoints — where a stand finally has a body, and the clearances a station is sited by (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #831 · THE CHECKPOINTS, AND WHY A STAND FINALLY HAS A BODY ────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, watching a man motionless in a corridor: "why would it just stand
    // there if there is no inspection point etc" — and then, completing it himself with the real-world
    // anchor: "they actually in real life like have these check points they electronically sign on rounds to
    // prove they did their round."
    //
    // Guard-tour watchclock stations. That one sentence turns the five seconds of StandSeconds from a timing
    // constant into a THING THE MAN IS DOING: he walks up to a plate on a wall, faces it, signs in, walks on.
    // "Why is he standing there" is answered forever, and it is answered by the building rather than by a
    // caption.
    //
    // THE FIXTURE IS PUBLISHED, and the stop SNAPS TO IT. A checkpoint is a point on a wall the generator
    // already built, clear of every opening the generator already cut, with the square a body signs it from
    // published beside it — the #862 measured-wall idiom, one scale down. The renderer bolts a plate on it
    // and measures nothing. Nothing here computes a wall of its own (§13.15).
    //
    // WHAT IS DELIBERATELY NOT HERE. The round LOG — the clipboard at the station, the ledger behind the
    // desk, the paper a gumshoe steals to read the beat's timing off it — is the #828 satchel economy's
    // work and is filed as the release after. So is the forged log: a guard who skips a leg has a lever on
    // him, and levers are not this file's business.

    /// <summary>#831 · What a checkpoint wears. A small round contact plate, which is what a watchclock
    /// station has looked like since somebody first had to prove they walked a building.</summary>
    public const string CheckpointGlyph = "🔘";

    /// <summary>#831 · What is stencilled on one. §13.8's register exactly: it names the FIXTURE and says
    /// nothing whatever about what the facility is for. The number is the station's own, fixed to the place
    /// rather than to the order a shift happens to walk them in — a plate that renumbered itself every watch
    /// would be a building that cannot count its own doors.</summary>
    public static string CheckpointPlate(int number) => $"{CheckpointGlyph} ROUND POINT {number}";

    /// <summary>#831 · Is this plate one of ours? The renderer's and the audit's one answer, so a fourth kind
    /// of stencil cannot be told apart by a string literal typed twice.</summary>
    public static bool IsCheckpointPlate(string? plate) =>
        plate is not null && plate.StartsWith(CheckpointGlyph, StringComparison.Ordinal);

    /// <summary>
    /// #831 · HOW FAR OFF A WALL A ROUND WALKS AND STANDS — the ONE number the lane and the sign-in share.
    ///
    /// <para>Owner, in the same breath as the checkpoints: <i>"they should respect right side traffic, and
    /// not walk in the middle of the corridor."</i> A man trained in a facility walks a du or two off the
    /// wall on his own side, and the man signing a plate stands at exactly that distance from it — because
    /// it is the same distance, and two numbers for it would be a lane that does not end at the station it
    /// is walking to.</para>
    ///
    /// <para>Comfortably outside a body's own width (<see cref="SignInClearDu"/>) and comfortably inside the
    /// corridor's half-width, so the lane is a lane rather than a scrape along the shotcrete. FLAGGED for
    /// the owner's tuning.</para>
    /// </summary>
    public const double LaneFromWallDu = 1.5;

    /// <summary>#831 · How far off the CENTRE LINE of a corridor the lane runs — the corridor's own half
    /// width less the distance from the wall, read off <see cref="UndergroundComplex.CorridorHalf"/> rather
    /// than typed, so a generator that widens its passages widens this with them. Two guards on opposite
    /// legs are twice this apart, which is the whole of "they pass on opposite sides".</summary>
    public static double LaneOffsetDu => UndergroundComplex.CorridorHalf - LaneFromWallDu;

    /// <summary>#831 · How straight a stretch has to be before the lane is worth keeping on it, as the cosine
    /// between the step into a waypoint and the step out of it. The lane is a PREFERENCE and never a wall:
    /// at a corner, a doorway or a rib mouth the offset is simply dropped and the round takes the middle,
    /// which is what a person does and what keeps a laned route walking through a 6.4 du leaf.</summary>
    public const double LaneStraightEnough = 0.995;

    /// <summary>#831 · How far from its stop a checkpoint may be bolted. A room's own width, so a stop in the
    /// open middle of a chamber still finds a wall — and short enough that a "checkpoint" is never a plate in
    /// the next corridor with a guard standing at it for no visible reason.</summary>
    public const double CheckpointReachDu = UndergroundComplex.RoomWidthDu;

    /// <summary>#831 · How much clear floor the square a man signs from needs. A body's own width and no
    /// more — <see cref="RipAndBin.StandClearDu"/>'s own reasoning about a bin, said again about a plate:
    /// somebody stands at one, they do not park a vehicle at one.</summary>
    public const double SignInClearDu = RipAndBin.StandClearDu;

    /// <summary>#831 · How far a checkpoint keeps from a published opening. <see cref="RingOffice.DoorClearDu"/>
    /// — the building's own answer to "how much of a wall does a doorway claim", read rather than re-derived,
    /// so a station can never end up on the jamb of a door somebody has to walk through.</summary>
    public static double ClearOfOpeningDu => RingOffice.DoorClearDu;

    /// <summary>
    /// #831 · ONE WATCHCLOCK STATION: the plate on the wall, and the square it is signed from.
    /// </summary>
    /// <param name="Number">Which station this is on the floor, 1-based and fixed to the PLACE. It is what is
    /// stencilled on it and it does not turn over with the shift.</param>
    /// <param name="X">Where the plate is bolted, on a wall the generator built.</param>
    /// <param name="Y">The same.</param>
    /// <param name="StandX">Where a body stands to sign it — <see cref="LaneFromWallDu"/> off the wall, on
    /// the side the stop was on. THIS is what the beat stop snaps to.</param>
    /// <param name="StandY">The same.</param>
    /// <param name="What">The stop it belongs to, for a red audit's printout. Never shown to a player.</param>
    public readonly record struct Checkpoint(
        int Number, double X, double Y, double StandX, double StandY, string What)
    {
        /// <summary>What is stencilled on it.</summary>
        public string Plate => CheckpointPlate(Number);

        /// <summary>Which way a man signing it is looking: AT THE FIXTURE. The whole of the owner's law in
        /// one expression — a guard standing at a stop is looking at the thing he is standing at.</summary>
        public double Facing => Math.Atan2(Y - StandY, X - StandX);

        /// <summary>Is a body at this station? The same <see cref="AtTheStopDu"/> tolerance the round's own
        /// arrival uses, because arriving at the stop and arriving at the plate are the same arrival.</summary>
        public bool Signing(double x, double y)
        {
            double dx = x - StandX, dy = y - StandY;
            return (dx * dx) + (dy * dy) <= AtTheStopDu * AtTheStopDu;
        }

        /// <summary>How far a body is from the square this is signed from.</summary>
        public double GapFrom(double x, double y) =>
            Math.Sqrt(((x - StandX) * (x - StandX)) + ((y - StandY) * (y - StandY)));
    }

    /// <summary>#831 · Every station on this floor, in the circuit's own order. The renderer's one call — it
    /// bolts a plate on each and measures nothing.</summary>
    public static IReadOnlyList<Checkpoint> CheckpointsOn(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var found = new List<Checkpoint>();
        foreach (Stop stop in Circuit(floor, field))
        {
            if (stop.Point is { } at)
            {
                found.Add(at);
            }
        }
        return found;
    }

    /// <summary>#831 · Everything on this floor a body collides with, as the client will build it — the
    /// walls, the glazing (#759 keeps it out of <c>Walls</c> so nothing paints it as concrete, and it stops a
    /// body all the same) and the doors that never open. A station whose standing square were measured
    /// against a shorter list than the one the game collides with would be the drawn-versus-simulated split
    /// this house has a name for.</summary>
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

    /// <summary>
    /// #831 · WHERE THIS STOP'S STATION IS BOLTED — the nearest wall the building offers, measured.
    ///
    /// <para>Three tests and a station has to pass all three, which is <see cref="RipAndBin"/>'s own idiom
    /// one fixture along: the plate is ON a published wall, it is <see cref="ClearOfOpeningDu"/> from every
    /// published hole a body walks through, and there is somewhere to STAND at it that a body fits on. A
    /// candidate that fails any of them is skipped and the next wall round is tried.</para>
    ///
    /// <para>It is the NEAREST passing wall and not a chosen one, and that is deliberate: a facility bolts a
    /// watchclock station to whatever is at hand where the round has to stop — a corridor face, a room's own
    /// wall, the end of a bench somebody installed in 40-something. What it never does is put one across a
    /// doorway.</para>
    /// </summary>
    private static Checkpoint? PointFor(
        in Stop stop, int number,
        in UndergroundComplex.FloorPlan floor,
        List<SurfaceCollision.Segment> blockers)
    {
        Checkpoint? best = null;
        double bestGap = double.MaxValue;

        foreach (SurfaceLayout.Wall w in floor.Walls ?? [])
        {
            (double px, double py) = NearestOnSegment(stop.X, stop.Y, w.X1, w.Y1, w.X2, w.Y2);
            double dx = stop.X - px, dy = stop.Y - py;
            double gap = Math.Sqrt((dx * dx) + (dy * dy));

            // Cheap tests first, and the incumbent's gap is one of them: the expensive two below then run
            // only for a wall that would actually win, which is what keeps this off the frame budget.
            if (gap < 1e-6 || gap > CheckpointReachDu || gap >= bestGap)
            {
                continue;
            }
            if (!ClearOfEveryOpening(px, py, in floor))
            {
                continue;
            }

            double sx = px + (dx / gap * LaneFromWallDu);
            double sy = py + (dy / gap * LaneFromWallDu);
            if (SurfaceCollision.Blocked(sx, sy, SignInClearDu, blockers))
            {
                continue;
            }

            bestGap = gap;
            best = new Checkpoint(number, px, py, sx, sy, stop.What);
        }

        return best;
    }

    /// <summary>#831 · Is this spot on the wall far enough from every hole in it? Doorways and the doors that
    /// never open alike — a locked leaf already carries a sign, and two stencils on one door is a wall
    /// nobody can read.</summary>
    private static bool ClearOfEveryOpening(
        double px, double py, in UndergroundComplex.FloorPlan floor)
    {
        foreach (SurfaceLayout.Doorway d in floor.Doorways ?? [])
        {
            if (SurfaceCollision.DistanceToSegment(px, py, d.X1, d.Y1, d.X2, d.Y2) < ClearOfOpeningDu)
            {
                return false;
            }
        }
        foreach (UndergroundComplex.LockedDoor l in floor.Locked ?? [])
        {
            if (SurfaceCollision.DistanceToSegment(px, py, l.X1, l.Y1, l.X2, l.Y2) < ClearOfOpeningDu)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The point on a segment nearest a spot. The one bit of arithmetic a placer is allowed to do,
    /// because it is about a line somebody else drew.</summary>
    private static (double X, double Y) NearestOnSegment(
        double px, double py, double x1, double y1, double x2, double y2)
    {
        double vx = x2 - x1, vy = y2 - y1;
        double len2 = (vx * vx) + (vy * vy);
        if (len2 < 1e-12)
        {
            return (x1, y1);
        }
        double t = Math.Clamp((((px - x1) * vx) + ((py - y1) * vy)) / len2, 0.0, 1.0);
        return (x1 + (t * vx), y1 + (t * vy));
    }
}
