using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · HOW A SWEEPER GETS ANYWHERE — the patrol route and its legs, the slide off a wall that is
/// #324's one wall law, and the step that puts the body where the walk has got to and points it the
/// way it actually moved.
///
/// <para>The one line in this family that claims <c>SurfaceCollision.Gait.Person</c> lives here — a
/// boarding trooper is a person on somebody's payroll, and <c>AJambIsNotASealedDoorTests</c> counts
/// that claim across the whole tree.</para>
///
/// <para>Split out of <c>Map.SweepTeam.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered, and not one field moved.</para>
/// </summary>
public partial class Map
{
    /// <summary>Work the patrol. On arriving at a waypoint, take the next one — and say so occasionally, because
    /// the waiting IS the scene and it needs a heartbeat.</summary>
    private void WalkTheRoute(Sweeper s, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_sweepRoute.Count == 0)
        {
            return;
        }

        // ── #731 v2 · AND THE ROUTE IS WALKED ON THE LATTICE, WHICH IS WHAT THIS FILE ALWAYS CLAIMED ──
        //
        // The comment on WalkToward has said since #724 that these are the other side of the reever line —
        // "a thing that moves sensibly down here is somebody on a payroll… SO THE BLACK-OPS TEAM FINDS
        // DOORWAYS" — and underneath it the round was a STRAIGHT LINE sliding off whatever it hit. A sentence
        // and a sim disagreeing about the same body, which is this repository's third named bug class, and it
        // was invisible for as long as nothing depended on the route ever FINISHING.
        //
        // #731 v2 depends on it: they go home when they have seen the whole hull. Two of the three never got
        // there — they ground against a bulkhead a compartment short and stayed there for fifty minutes of
        // ship time, which the guard printed as `SWEEP-3 Sweeping at (-1.5,-6.0) leg 6/6 laps 0`.
        //
        // So the round is planned now, by the same NpcWalk the canteen's people are walked with, at the
        // sweep's own pace. The hunt and the search keep the straight line on purpose: a professional walking
        // at a noise they just heard goes AT it, and the honest failure of that is a body that gets stuck on
        // a corner for the twelve seconds it searches and then rejoins its round.
        if (s.Walk is null || s.Walk.For.X != s.GoalX || s.Walk.For.Y != s.GoalY)
        {
            s.Walk = OnFoot(
                s.Callsign, new NpcWalk.Bound("", s.GoalX, s.GoalY),
                new DeckReachability.Point(s.X, s.Y), walls, NpcWalk.PersonalSpaceInRadii,
                InspectionTeam.SweepSpeed);
        }

        if (s.Walk is { } leg)
        {
            leg.Step(dt, walls, _avatarX, _avatarY);
            StepTheBodyTo(s, leg, dt);
            if (leg.Afoot)
            {
                return;
            }
            s.Walk = null;
        }
        else
        {
            // No way through at all — the floor's own verdict. Fall back to the straight line rather than
            // standing still, so a hull the lattice cannot cross is still swept badly instead of not at all.
            WalkToward(s, s.GoalX, s.GoalY, InspectionTeam.SweepSpeed, dt, walls);
            double dx = s.GoalX - s.X, dy = s.GoalY - s.Y;
            if ((dx * dx) + (dy * dy) > 1.5 * 1.5)
            {
                return;
            }
        }

        s.RouteLeg = (s.RouteLeg + 1) % _sweepRoute.Count;
        AimAtRouteLeg(s);

        // One line per lap, from one of them: enough to know they are still working, not enough to be chatter.
        if (s.RouteLeg == 0)
        {
            LogAutopilotEvent(InspectionTeam.SweepingLine(s.Callsign));
        }

        // ── #731 v2 · AND WHEN THEY HAVE SEEN ALL OF IT, THEY GO ────────────────────────────────────────
        //
        // Back where they began is a LAP, and it is the only spelling that means the same thing for all
        // three: they are staggered a third of the way round the hull from each other, so leg zero is a
        // different fraction of a sweep for each of them. Nothing is said about the change — a team that
        // announced its own departure would be telling the captain the one thing this whole beat is for.
        if (s.RouteLeg == s.StartLeg && ++s.Laps >= InspectionTeam.LapsBeforeTheyGo)
        {
            EnterState(s, InspectionTeam.Awareness.Leaving);
            // …and the way home is planned on the frame they DECIDE, not on the frame after it. A body that
            // is leaving and has no route for one frame is a body the deck could draw taking a step it never
            // planned, and it is the shape a despawn hides in.
            _ = TheyFileOutThroughTheLock(s, 0, walls);
        }
    }

    private void AimAtRouteLeg(Sweeper s)
    {
        if (_sweepRoute.Count == 0)
        {
            return;
        }
        (double gx, double gy) = _sweepRoute[s.RouteLeg % _sweepRoute.Count];
        s.GoalX = gx;
        s.GoalY = gy;
    }

    /// <summary>Walk, sliding off walls exactly as the captain does (#324's one wall law), and look where you are
    /// going — the facing IS the threat, so it must never be decorative.</summary>
    private static void WalkToward(Sweeper s, double gx, double gy, double speed, double dt,
                                  IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double dx = gx - s.X, dy = gy - s.Y;
        double distance = System.Math.Sqrt((dx * dx) + (dy * dy));
        if (distance < 0.05)
        {
            s.Vx = 0;
            s.Vy = 0;
            return;
        }

        double step = speed * dt;
        // #724 · A SWEEPER IS A PERSON. Owner's ruling was about the Old Ones — "lets not help reevers
        // move in any easier" — and the same session's #729 refinement says why these are the other side of
        // that line: "maybe the guards can use A* also so they do not come off as reevers or kind of crazy
        // scary." A thing that moves sensibly down here is somebody on a payroll, and the player is meant to
        // read that off the motion before any card says so. So the black-ops team finds doorways.
        (double nx, double ny) = SurfaceCollision.Slide(
            s.X, s.Y, dx / distance * step, dy / distance * step, DeckPlan.AvatarRadius, walls,
            SurfaceCollision.Gait.Person);

        s.Vx = dt > 0 ? (nx - s.X) / dt : 0;
        s.Vy = dt > 0 ? (ny - s.Y) / dt : 0;

        // Face the way they are actually travelling, not the way they wanted to: a sweeper sliding along a
        // bulkhead looks down the bulkhead, which is both truer and kinder.
        double mx = nx - s.X, my = ny - s.Y;
        if ((mx * mx) + (my * my) > 1e-8)
        {
            s.Facing = System.Math.Atan2(my, mx);
        }

        s.X = nx;
        s.Y = ny;
    }

    /// <summary>#731 v2 · Put the body where its walk has got to, and point it the way it actually moved.
    /// The motion fan reads <c>Vx/Vy</c> and the lamp reads <c>Facing</c>, so a walked leg has to leave both
    /// of them saying the same thing a slid one does — one place, so the two movers cannot come to two
    /// different accounts of one stride.</summary>
    private static void StepTheBodyTo(Sweeper s, NpcWalk walk, double dt)
    {
        double mx = walk.X - s.X, my = walk.Y - s.Y;
        s.Vx = dt > 0 ? mx / dt : 0;
        s.Vy = dt > 0 ? my / dt : 0;
        if ((mx * mx) + (my * my) > 1e-8)
        {
            s.Facing = System.Math.Atan2(my, mx);
        }
        s.X = walk.X;
        s.Y = walk.Y;
    }

    private static void FaceToward(Sweeper s, double x, double y) =>
        s.Facing = System.Math.Atan2(y - s.Y, x - s.X);
}
