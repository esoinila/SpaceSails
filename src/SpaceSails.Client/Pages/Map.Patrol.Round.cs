using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 split; the header note lives in Map.Patrol.cs) — one leg of the round: the A* between two published stops, #831's lane and its sign-in, #858's plan made while he stands there anyway, the one stepper every mover on this floor spends a frame through, and #831's cover act for a tail that has been made.
public sealed partial class Map
{
    /// <summary>
    /// One leg of the round. The route is planned with A* between two published stops and spent through the
    /// captain's own collision at a person's gait, so a guard finds doorways and slides off walls exactly as
    /// a person does. Arriving starts the STAND — the gap the whole feature is about.
    ///
    /// <para>#831 · <b>AND THE STAND IS NOW A SIGN-IN.</b> Owner: <i>"they actually in real life like have
    /// these check points they electronically sign on rounds to prove they did their round."</i> The stop he
    /// arrives at IS the square a watchclock station is signed from (Core snapped it there), so arriving
    /// turns him to face the plate and the five seconds are the act. Nothing about the timing moved.</para>
    ///
    /// <para>#831 · <b>AND THE LEG KEEPS RIGHT.</b> Owner: <i>"they should respect right side traffic, and
    /// not walk in the middle of the corridor."</i> The A* is untouched — the same search over the same
    /// field — and its WAYPOINT LINE is put on the walker's own side of the corridor
    /// (<see cref="PatrolBeat.KeepRight"/>) before he spends a stride of it. Once per leg, at plan time: Lab
    /// 45's frame budget never sees it.</para>
    /// </summary>
    private void WalkTheRound(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (g.Standing > 0)
        {
            g.Standing -= dt;
            g.Vx = 0;   // a stopped mover drops off a motion fan, honestly
            g.Vy = 0;
            PlanTheNextLegWhileHeStands(g, walls);
            return;
        }

        PatrolBeat.Stop target = _patrolBeat[g.Leg % _patrolBeat.Count];

        if (g.Route is not { Active: true })
        {
            var from = new DeckReachability.Point(g.X, g.Y);
            var to = new DeckReachability.Point(target.X, target.Y);

            // #858 · …and most of the time this costs nothing, because the stand he has just finished spent
            // its three hundred frames doing it (PlanTheNextLegWhileHeStands). Finish() completes whatever
            // is left — everything, when there was no stand, or when what he stood planning was a walk he is
            // no longer making — so the man always leaves the stop with a route in his hand.
            AutoWalk.Planner plan = g.Planning is { } ahead && ahead.PlannedFor(from, to)
                ? ahead
                : PlanTheLeg(g, target, walls);
            g.Planning = null;
            AutoWalk.Attempt planned = plan.Finish();

            if (planned.Route is null)
            {
                // Nothing connects. The audit says this cannot happen on a floor this generator builds
                // (§13.1) and a guard is not the place to find out otherwise — so the round simply drops
                // the stop and carries on rather than standing in a corridor forever.
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
                return;
            }

            // #831 · THE LANE, laid on the line he is about to walk. The route the A* proved is the route he
            // takes; what this decides is where along the width of the corridor it runs — and it hands the
            // offset back at every corner, doorway and rib mouth, because a lane is a preference and a
            // preference that could wedge a man against a jamb would be a wall.
            //
            // …AND ON A RE-PLAN HE TAKES THE MIDDLE. `Retries` is above zero only because the last line he
            // was handed did not walk, and the first thing a preference does when it stops working is stop
            // being applied. KeepRight now proves the ground under every hop before it offsets one, so this
            // is the belt under the braces rather than the fix — but it is the clause that means no future
            // lane can ever be the reason a man stops getting anywhere, which is the difference between a
            // preference and a wall.
            g.Route = AutoWalk.Along(g.Retries == 0
                ? PatrolBeat.KeepRight(planned.Route.Route, walls, DeckPlan.AvatarRadius)
                : planned.Route.Route);
        }

        SpendTheStride(g, dt, walls);

        double gx = target.X - g.X, gy = target.Y - g.Y;
        bool there = (gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;
        if (there)
        {
            g.Route = null;
            g.Retries = 0;
            g.Standing = PatrolBeat.StandSeconds;   // THE GAP the whole feature is about

            // #831 · …and the gap has a body now. He turns to the plate on the wall and signs it. The
            // facing comes off Core's own fixture rather than being worked out here, so the man and the
            // thing he is looking at can never be two different answers.
            if (target.Point is { } station)
            {
                // From where he ACTUALLY ended up, not from the square Core measured the fixture off:
                // AtTheStopDu is a tolerance and a man a stride short of the plate is still looking at it.
                g.Facing = System.Math.Atan2(station.Y - g.Y, station.X - g.X);
                g.SignedPoint = station.Number;
            }

            g.Leg = (g.Leg + 1) % _patrolBeat.Count;
            return;
        }

        // #832 · A SNAG IS NOT AN ARRIVAL. The old clause charged a full stand and skipped the stop the
        // moment the route went inactive for any reason, which meant a body grazing a jamb halfway down a
        // corridor stood for five seconds and then walked somewhere else entirely — a round that was mostly
        // standing, in places that are not stops, and an unlearnable one for a captain trying to time it.
        // Standing is earned by ARRIVING; a refused step only costs the plan, so the leg is taken again from
        // wherever the body actually ended up. Bounded, because a stop that genuinely cannot be reached must
        // not be ground at forever (§13.1's audit says a floor this generator builds has no such stop, and a
        // guard is not the place to find out otherwise).
        if (g.Route is not { Active: true })
        {
            g.Route = null;
            if (++g.Retries > PatrolBeat.RePlansPerLeg)
            {
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
            }
        }
    }

    /// <summary>
    /// #858 · THE PLAN, MADE WHILE HE IS STANDING THERE ANYWAY.
    ///
    /// <para>Lab 45's only frame-eating measurement: the A* this round asks for costs a median 1.6–2.2 ms
    /// and a worst <b>6.4 ms — 38.6% of a 60 fps frame</b>, spent whole on the frame a guard leaves a stop,
    /// about twice a minute per guard, and that is native on a desk machine while this game ships to WASM.
    /// It lands on the frame the player is most likely to be looking straight at him.</para>
    ///
    /// <para>He stands at a stop for <see cref="PatrolBeat.StandSeconds"/> — five seconds, ~300 frames in
    /// which he does not move and already knows which stop is next. So the same search runs then, a slice at
    /// a time (<see cref="PatrolBeat.PlanCellsAFrame"/>), and the departure frame usually finds it done.</para>
    ///
    /// <para><b>NOTHING HE DOES CHANGES.</b> The route is the same route — <c>AutoWalk.Planner</c> is
    /// <c>AutoWalk.Plan</c>'s own search, paused, from the same spot to the same stop over the same walls,
    /// and he cannot move while he is standing — and the stand is the same five seconds, because the slicing
    /// is not allowed to end it or extend it. If the errand changes under him, the plan is simply not the
    /// plan for the walk he makes, and the departure frame pays the old bill exactly as it always did.</para>
    /// </summary>
    private void PlanTheNextLegWhileHeStands(Guard g, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_patrolBeat.Count == 0)
        {
            return;
        }

        PatrolBeat.Stop next = _patrolBeat[g.Leg % _patrolBeat.Count];
        if (g.Planning is not { } ahead
            || !ahead.PlannedFor(
                new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(next.X, next.Y)))
        {
            ahead = PlanTheLeg(g, next, walls);
            g.Planning = ahead;
        }
        ahead.Advance(PatrolBeat.PlanCellsAFrame);
    }

    /// <summary>The A* for one leg of the round, opened but not walked — the ONE place the round says what
    /// it is asking for, so the plan made during a stand and the plan made on the frame a man leaves one are
    /// the same question by construction.
    ///
    /// <para>The lattice is the LEG's box and not the floor's — Core's own answer
    /// (<see cref="PatrolBeat.LatticeFor"/>), so the audit that proves every leg walkable proves the route
    /// this actually plans. See that method for why the difference is not a micro-optimisation: a
    /// floor-sized lattice on every arrival would hitch the frame in WASM.</para></summary>
    private static AutoWalk.Planner PlanTheLeg(
        Guard g, in PatrolBeat.Stop target, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        AutoWalk.Planner.Begin(
            new DeckReachability.Point(g.X, g.Y),
            new DeckReachability.Point(target.X, target.Y),
            walls, DeckPlan.AvatarRadius,
            PatrolBeat.LatticeFor(
                new PatrolBeat.Stop(g.X, g.Y, "here"), target, MoonSurface.ExpeditionField()));

    /// <summary>
    /// #833 · ONE FRAME OF WALKING, FOR EVERY REASON ANYBODY ON THIS FLOOR WALKS. The round spends its leg
    /// through here, the walk-up crosses the corridor through here and the escort walks the captain out
    /// through here — one loop, so a man is the same man whichever errand he is on and there is exactly one
    /// place his gait can be got wrong.
    ///
    /// <para>#832 · THE EPSILON, AND WHY THE FAN WAS SILENT ALL EVENING. This loop is the captain's own
    /// sub-stepper (<c>Map.Deck.cs</c>), copied — and the copy dropped one character of it: the captain
    /// spends his budget while <c>budget &gt; 1e-9</c>, this spent it while <c>budget &gt; 0</c>. A frame's
    /// budget is never consumed to exactly zero in binary, so on nearly every frame the loop took ONE MORE
    /// sub-step of about 1e-17 du, the slide moved the body by less than the snag threshold, and the route
    /// was declared refused by the ground. The round's arrival clause then read that refusal as an ARRIVAL:
    /// five seconds of standing, and the stop skipped. Measured over a simulated minute of luna B2, a guard
    /// was moving 4% of the time and covered 7.7 du — which is why the owner's whole session read <i>"no
    /// movement — for now"</i> with a man plainly walking the corridor. The wiring was never the fault; the
    /// guard genuinely was not moving. (§13 lesson, again: a copied stepper is a copy of its bugs and of
    /// nothing else — which is why #833 has THREE callers here and no second copy.)</para>
    /// </summary>
    /// <returns>Whether the body actually moved this frame — what the fan is about to be told.</returns>
    private bool SpendTheStride(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double budget = PatrolBeat.WalkSpeed * dt;
        double startX = g.X, startY = g.Y;

        for (int step = 0; step < AutoWalkSubStepsPerFrame && budget > 1e-9; step++)
        {
            if (g.Route is not { Active: true } route
                || !route.TryStep(g.X, g.Y, budget, out double dx, out double dy))
            {
                break;
            }

            (double nx, double ny) = SurfaceCollision.Slide(
                g.X, g.Y, dx, dy, DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

            double mx = nx - g.X, my = ny - g.Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                route.Snag();
                break;
            }

            budget -= System.Math.Sqrt((mx * mx) + (my * my));
            g.X = nx;
            g.Y = ny;
        }

        double tx = g.X - startX, ty = g.Y - startY;
        g.Vx = dt > 0 ? tx / dt : 0;
        g.Vy = dt > 0 ? ty / dt : 0;
        if ((tx * tx) + (ty * ty) > 1e-8)
        {
            // Face the way they are actually travelling, not the way they wanted to — truer, and it is what
            // makes a round readable from the far end of a corridor.
            g.Facing = System.Math.Atan2(ty, tx);
            return true;
        }
        return false;
    }

    // ── #831 · THE COVER ACT ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #831 · A MADE TAIL DOES NOT FREEZE BARE — he finds something on a wall and reads it.
    ///
    /// <para>Owner, on the man he watched stand in the middle of a corridor: <i>"why would it just stand
    /// there if there is no inspection point etc"</i> — and the rule that generalises it: <i>"A MADE tail
    /// performs a COVER ACT instead of freezing bare: turns to the nearest wall fixture, checks a plate,
    /// reads a docket — same hold, same honest Vx=0, but the picture says 'man with business' not
    /// 'statue'."</i></para>
    ///
    /// <para><b><see cref="FootTail.MustHold"/> is untouched and stays untouched.</b> He still stops exactly
    /// where the law says he stops, and he still drops off the motion fan honestly when he is standing —
    /// #830's blob is what a standing man returns and this changes none of it. What changed is what a player
    /// SEES: a station, a shut door's sign, a poster, and a man doing the most ordinary thing in a facility
    /// in front of it.</para>
    ///
    /// <para><b>The drift.</b> Owner: <i>"Mid-corridor with nothing near: he does not hold there — he drifts
    /// the few du to the nearest fixture first, then holds."</i> A few du and no more
    /// (<see cref="PatrolBeat.CoverDriftDu"/>) and on a straight slide rather than an A*: a man crossing a
    /// corridor to a plate does not plan a route, and a plan per held frame is Lab 45's bill for nothing.
    /// With nothing at all within reach the hold is bare and HONEST — the audit counts those, because a
    /// floor that could not offer a man anything to look at is a finding rather than a fudge.</para>
    ///
    /// <para><b>The tell rides for free.</b> The nearest thing to a man who has just left a stop is the
    /// station he just signed, so a made tail signs it again — the same act at the same plate twice in one
    /// round, which is the gumshoe's confirmation. Nothing says so: no card, no line, no marker. It is
    /// watched (<see cref="PatrolBeat.DoubleSignIn"/> is the one place the sim names it, for the audit).</para>
    /// </summary>
    private void TheCoverAct(
        Guard g, double dt,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        g.Route = null;
        g.CoverFor += dt;

        // HE PICKS ONCE, on the frame the hold starts, and then it is what he is doing.
        g.CoverAt ??= PatrolBeat.CoverFor(g.X, g.Y, _patrolReadables, sight, walls);

        if (g.CoverAt is not { } thing)
        {
            // Not so much as a WALL within a few du of where the law stopped him — which on these floors is
            // nowhere, and is left standing rather than papered over precisely so a floor plan that grew such
            // a place would show up as a finding instead of as a statue.
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        // HE IS LOOKING AT IT the whole time, walking to it or standing at it. A man crossing two du to a
        // plate has already decided which plate.
        g.Facing = System.Math.Atan2(thing.Y - g.Y, thing.X - g.X);
        g.CoverPoint = thing.Point;

        if (PatrolBeat.AtTheCover(g.X, g.Y, in thing) || g.CoverFor > PatrolBeat.CoverDriftSeconds)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        // THE DRIFT, on the captain's own collision at the round's own gait — and honestly on the fan while
        // it lasts, because he is moving and a motion tracker hears movers.
        double dx = thing.X - g.X, dy = thing.Y - g.Y;
        double gap = System.Math.Sqrt((dx * dx) + (dy * dy));
        double take = System.Math.Min(PatrolBeat.WalkSpeed * dt, System.Math.Max(0, gap - PatrolBeat.CoverStandDu));

        // #832's epsilon, one stepper along: a budget is never spent to exactly zero in binary, and a slide
        // of 1e-17 du is a man who has arrived being reported to the fan as a man who is moving.
        if (take < 1e-9)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        (double nx, double ny) = SurfaceCollision.Slide(
            g.X, g.Y, dx / gap * take, dy / gap * take,
            DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

        double mx = nx - g.X, my = ny - g.Y;
        if ((mx * mx) + (my * my) < 1e-8)
        {
            // The ground will not let him any nearer — a rail, a bench, the corner of a pier. He reads it
            // from where he is, which is what a person does, and he is STILL: a man scraping along a wall by
            // a millionth of a deck unit is a body the motion fan would honestly call a mover.
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        g.X = nx;
        g.Y = ny;
        g.Vx = dt > 0 ? mx / dt : 0;
        g.Vy = dt > 0 ? my / dt : 0;
    }
}
