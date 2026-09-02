using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #538 slice 3 · SOMEBODY ELSE'S TEAM, ON THE DECK. The brain is <see cref="InspectionTeam"/> in Core and every
/// number below comes from it; this file is the walking, the looking and the telling.
///
/// <para><b>Where they land.</b> On the INSURANCE JOB, whose own fiction already says
/// <i>"she was LOST ON PURPOSE… the most valuable thing aboard is the evidence"</i>. That is the perfect host and
/// it needed nothing invented: they are here to remove exactly what the captain came to take, which is the whole of
/// the owner's <i>"they want to keep their secrets, but the rewards could be big also"</i> in one existing cause.
/// Also reachable on any hull with <c>?sweep=N</c>, the way <c>?reevers=N</c> works.</para>
///
/// <para><b>What they do that the pack cannot.</b> They walk a route, they look where they are going, and they
/// hear better than anything else aboard. So the captain's counter-play is not the maze and not speed — it is
/// standing still off their axis and not making a sound, which is the one thing the wreck lane has never asked
/// for.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>One sweeper, walking. Mutable and client-side for the same reason the Reevers are: the rules are
    /// pure in Core and the list is the client's business.</summary>
    private sealed class Sweeper
    {
        public required string Callsign { get; init; }

        public double X;
        public double Y;
        public double Facing;

        /// <summary>How fast they are travelling this frame. A MOTION tracker hears travel and nothing else, so
        /// without this the fan would report an empty hull while three people walked it — the panel disagreeing
        /// with the sim, which is the one thing this codebase does not allow.</summary>
        public double Vx;
        public double Vy;

        public InspectionTeam.Awareness State = InspectionTeam.Awareness.Sweeping;
        public double StateSeconds;

        /// <summary>Where they are walking to — a route waypoint, or the place a noise came from.</summary>
        public double GoalX;
        public double GoalY;

        /// <summary>Which leg of the patrol they are on. They resume it after a search, which is what makes a
        /// diversion a diversion rather than a permanent change of plan.</summary>
        public int RouteLeg;

        /// <summary>Where they last laid eyes on the captain — the only thing they know once sight is broken.
        /// Same idiom as the pack: a PLACE, never a target.</summary>
        public double LastSeenX;
        public double LastSeenY;

        /// <summary>Whether they have said their line for the state they are in. Keeps a challenge from
        /// re-announcing itself sixty times a second.</summary>
        public bool Announced;

        /// <summary>#731 v2 · Which leg of the route they STARTED on. The three of them are staggered a third
        /// of the way round the hull from each other, so "back where I began" is the only spelling of
        /// <i>a lap</i> that means the same thing for all three.</summary>
        public int StartLeg;

        /// <summary>#731 v2 · How many times round the hull they have been. At
        /// <see cref="InspectionTeam.LapsBeforeTheyGo"/> they go home.</summary>
        public int Laps;

        /// <summary>#731 v2 · Their walk to the lock, planned over the captain's own lattice by the same
        /// <c>NpcWalk</c> the canteen's people are walked with (#731). Null on every other state — a body
        /// sweeping a hull is following a checklist, and a body going home is following a route.</summary>
        public NpcWalk? Walk;

        /// <summary>#731 v2 · How long they have been standing at the head of the file working the hatch.
        /// At <see cref="InspectionTeam.ThroughTheLockSeconds"/> they are through it and gone.</summary>
        public double AtTheLock;
    }

    private readonly List<Sweeper> _sweepers = [];

    /// <summary>The patrol: compartment centres, aft to forward. Built once at spawn so all three share one route
    /// and a captain can learn it — being hidden from has to be legible.</summary>
    private readonly List<(double X, double Y)> _sweepRoute = [];

    /// <summary>Dev cheat: <c>?sweep=N</c> puts N sweepers aboard whatever hull you board.</summary>
    private int _sweepTeamCheat;

    private bool SweepersAboard => _sweepers.Count > 0;

    /// <summary>Whether any of them is actively after the captain — read by the story seam so a card cannot land
    /// over a challenge, and by the HUD.</summary>
    private bool AnySweeperOnTheCaptain =>
        _sweepers.Exists(s => s.State is InspectionTeam.Awareness.Challenging or InspectionTeam.Awareness.Hunting);

    /// <summary>The worst state anybody aboard is in, for the one HUD line they get.</summary>
    private InspectionTeam.Awareness WorstSweeperState
    {
        get
        {
            InspectionTeam.Awareness worst = InspectionTeam.Awareness.Sweeping;
            foreach (Sweeper s in _sweepers)
            {
                if (InspectionTeam.NervePerSecond(s.State) > InspectionTeam.NervePerSecond(worst))
                {
                    worst = s.State;
                }
            }
            return worst;
        }
    }

    // ── Putting them aboard ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Their boat mates on and they start work. Placed at the AFT end and routed forward, so they come up the
    /// spine toward the lock — which puts them between the captain and the way out by the time they matter, the
    /// same geometry that makes the pack frightening.
    /// </summary>
    private void SpawnSweepTeam(int count)
    {
        _sweepers.Clear();
        _sweepRoute.Clear();

        // The route: every compartment centre, aft to forward, then the spine at the lock. They work the ship the
        // way anybody would who has done it before — from the far end back to their own boat.
        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments.OrderBy(c => c.Item2))
        {
            _ = x1;
            _ = name;
            _sweepRoute.Add(RoomCentre(name));
        }
        _sweepRoute.Add((WreckLayout.ShuttleLockX, 0));

        for (int i = 0; i < count && i < InspectionTeam.Callsigns.Count; i++)
        {
            // Staggered along the aft spine, each starting a third of the way round the route, so the three of
            // them cover the hull instead of walking in a queue.
            int leg = _sweepRoute.Count > 0 ? i * _sweepRoute.Count / System.Math.Max(1, count) : 0;
            _sweepers.Add(new Sweeper
            {
                Callsign = InspectionTeam.Callsigns[i],
                X = -30 + (i * 3),
                Y = 0,
                Facing = 0,
                RouteLeg = leg,
                StartLeg = leg,
                GoalX = _sweepRoute.Count > 0 ? _sweepRoute[leg].X : 0,
                GoalY = _sweepRoute.Count > 0 ? _sweepRoute[leg].Y : 0,
            });
        }

        ShowPulseMessage(InspectionTeam.TheyArriveLine);
        LogAutopilotEvent(InspectionTeam.TheyArriveLine);
        RendererInterop.PlayCue("alarm");

        // Said once, on the way in, because a captain who has not silenced their own kit has already lost and
        // deserves to be told why rather than discovering it when the tube gun opens up.
        if (!_weaponsTight)
        {
            LogAutopilotEvent(InspectionTeam.YourGunsWillGiveYouAwayLine);
            ShowPulseMessage(InspectionTeam.YourGunsWillGiveYouAwayLine);
        }

        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit, "somebody else is aboard");
        _wreckTrackerLive = true;   // an ear does not un-hear: three moving contacts light the fan
    }

    // ── The loop ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Walk them, look for the captain, and resolve what they find. Called once a frame from the sim.</summary>
    private void AdvanceSweepTeam(double dtRealSeconds)
    {
        if (_sweepers.Count == 0 || _surface is null || !OnWreck)
        {
            return;
        }

        double dt = System.Math.Min(dtRealSeconds, 0.1);   // same frame clamp the pack uses
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;

        List<Sweeper>? gone = null;

        for (int i = _sweepers.Count - 1; i >= 0; i--)
        {
            Sweeper s = _sweepers[i];
            s.StateSeconds += dt;

            InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);

            // #537 slice 3 · WHAT THIS SWEEPER MAKES OF THE CAPTAIN. On a hull with no void this is exactly
            // the sighting test it replaced — one call to InspectionTeam.Sees and nothing else. With a void
            // cut into her it also asks the two questions a plate raises: did this one watch it close, and
            // is the cut round it still bright. Being SEEN outranks both, so no arrangement of hiding state
            // can make a visible captain invisible (HullStowage.WhatGivesYouAway asks it first).
            HullStowage.Tell tell = WhatGivesTheStowawayAway(member, sight);
            bool seesCaptain = !CaptainBeyondReach && HullStowage.Caught(tell);

            // THE PACK OUTRANKS THE CAPTAIN whenever it is actually there. Owner: "It might be sweet if they
            // fought off some reevers etc while the pirates hide." A sweeper busy with an Old One is a sweeper
            // not looking at you, which is the best thing that can happen in this scene.
            (bool packSeen, double packRange) = NearestPackInCone(s, sight);
            if (InspectionTeam.WhoTheyDealWith(seesCaptain, packSeen, packRange) == InspectionTeam.Priority.ThePack)
            {
                if (s.State != InspectionTeam.Awareness.Hunting || !s.Announced)
                {
                    ShowPulseMessage(InspectionTeam.ThreeBodyLine(s.Callsign));
                    LogAutopilotEvent(InspectionTeam.ThreeBodyLine(s.Callsign));
                    s.Announced = true;
                }
                HoldAndFightThePack(s, dt);
                continue;
            }

            switch (s.State)
            {
                case InspectionTeam.Awareness.Challenging when seesCaptain:
                    if (InspectionTeam.ChallengeExpired(member))
                    {
                        ChallengeRunsOut();
                        return;   // the captain is being dealt with; nothing else this frame matters
                    }
                    FaceToward(s, _avatarX, _avatarY);   // the lamp does not wander during a challenge
                    break;

                case InspectionTeam.Awareness.Challenging:
                    // Sight broken inside the beat. THE CAPTAIN SPENT IT WELL — and it still cost them: they
                    // are hunted now, and being hunted outlives the escape.
                    EnterState(s, InspectionTeam.Awareness.Hunting);
                    ShowPulseMessage(InspectionTeam.BrokeContactLine);
                    LogAutopilotEvent(InspectionTeam.BrokeContactLine);
                    break;

                case not InspectionTeam.Awareness.Challenging when seesCaptain:
                    s.LastSeenX = _avatarX;
                    s.LastSeenY = _avatarY;
                    EnterState(s, InspectionTeam.Awareness.Challenging);
                    // #537 slice 3 · WHAT THEY SAW, before what they say. A captain taken out of a hole he
                    // thought was safe is owed the reason, and the reason is a fact about the world — a lamp
                    // that was on the plate as it closed, or a cut still warm enough to read.
                    ShowPulseMessage(HullStowage.TellLine(tell, s.Callsign));
                    LogAutopilotEvent(HullStowage.TellLine(tell, s.Callsign));
                    ShowPulseMessage(InspectionTeam.ChallengeLine(s.Callsign));
                    LogAutopilotEvent(InspectionTeam.ChallengeLine(s.Callsign));
                    RendererInterop.PlayCue("alarm");
                    break;

                case InspectionTeam.Awareness.Hunting:
                    // They walk to where they last saw somebody and clear outward. Giving up takes a while,
                    // because a professional does not shrug and go back to the checklist.
                    WalkToward(s, s.LastSeenX, s.LastSeenY, InspectionTeam.HuntSpeed, dt, walls);
                    if (s.StateSeconds >= InspectionTeam.HuntPersistenceSeconds)
                    {
                        EnterState(s, InspectionTeam.Awareness.Sweeping);
                    }
                    break;

                case InspectionTeam.Awareness.Investigating:
                    WalkToward(s, s.GoalX, s.GoalY, InspectionTeam.SweepSpeed, dt, walls);
                    if (s.StateSeconds >= InspectionTeam.SearchSeconds)
                    {
                        EnterState(s, InspectionTeam.Awareness.Sweeping);
                        AimAtRouteLeg(s);
                    }
                    break;

                case InspectionTeam.Awareness.Leaving:
                    if (TheyFileOutThroughTheLock(s, dt, walls))
                    {
                        (gone ??= []).Add(s);
                    }
                    break;

                default:
                    WalkTheRoute(s, dt, walls);
                    break;
            }

            // Fear is priced by what they are doing, not by how close they are — a lamp on your face is the
            // worst thing in this scene short of being shot (#480's seam).
            double pips = InspectionTeam.NervePerSecond(s.State) * dt;
            if (pips > 0)
            {
                ApplyNerveShock(pips, s.State == InspectionTeam.Awareness.Challenging
                    ? $"{s.Callsign} has the lamp on you"
                    : "somebody professional is looking for you");
            }
        }

        if (gone is not null)
        {
            foreach (Sweeper who in gone)
            {
                _sweepers.Remove(who);
            }
        }

        // -- #731 v2 - AND THE DOOR IS THEIRS WHILE THEY ARE IN IT --------------------------------------
        //
        // The lock has always been a CREW-ONLY hatch. WreckLayout.HeldAtLock is the rule the pack has been
        // held by since the wreck shipped, and it lives in Core precisely so the promise is pinned by a test
        // rather than by a comment. The away team on the far side of it are crew; while they are filing
        // through it, the captain is not.
        //
        // NOT ONE WORD IS SAID. Three professionals queueing at your way home, one at a time, unhurried, is
        // the most legible sentence this scene has, and a line explaining it would be the game saying out
        // loud the thing it has just spent a minute showing.
        if (TheyAreHoldingTheLock && WreckLayout.PastTheLock(_avatarX, DeckPlan.AvatarRadius))
        {
            _avatarX = WreckLayout.HeldAtLock(_avatarX, DeckPlan.AvatarRadius);
        }
    }

    /// <summary>#731 v2 - Is anybody working the hatch right now? Read by the frame above and by nothing
    /// else: this is not a HUD flag, and there is no line hanging off it.</summary>
    private bool TheyAreHoldingTheLock =>
        _sweepers.Exists(s => s.State == InspectionTeam.Awareness.Leaving);

    /// <summary>
    /// #731 v2 - <b>THE SWEEP TEAM WALKS OUT THROUGH THE AIRLOCK: SINGLE FILE, UNHURRIED, AND THEN GONE.</b>
    ///
    /// <para>#731's third first customer, and the one the walker's own file has been promising since v1
    /// (<c>Map.Walkers.cs</c>: <i>"the day a sweep team walks out of an airlock (#731 v2) it will mean
    /// something else again"</i>). Until now they did not leave abstractly - <b>they did not leave at all.</b>
    /// The team was spawned, walked a route forever, and stopped existing only because the NEXT boarding
    /// cleared the list; a captain who hid well enough never found out what happened.</para>
    ///
    /// <h3>Why it is the walker and not the sweep loop</h3>
    ///
    /// <para>The sweep loop is a straight-line <see cref="SurfaceCollision.Slide"/> at a goal - right for a
    /// checklist walked between compartments that all hang off one spine, and wrong for a body that has to
    /// get from wherever it happens to be standing to one particular doorway without grinding along a
    /// bulkhead on the way. So the exit is planned by <see cref="NpcWalk"/>, over the captain's own lattice,
    /// through the one <c>OnFoot</c> every walk on this side goes through - no second planner and no second
    /// gait claim. It is unhurried by construction: <see cref="NpcWalk.PaceDu"/> is slower than
    /// <see cref="InspectionTeam.SweepSpeed"/>, which is the point. They are not in a hurry; they are
    /// finished.</para>
    ///
    /// <h3>Single file</h3>
    ///
    /// <para>Their goal is a place in a QUEUE rather than a place at the door: one standoff off the lock for
    /// whoever is at the head of it, and <see cref="InspectionTeam.FileSpacingDu"/> further back for every
    /// body ahead of them. When the head goes through, everybody's rank drops and the file steps forward - on
    /// the floor, on the lattice, re-planned rather than nudged. The rank is read off the list's own order,
    /// so it is the same every frame and on every machine.</para>
    ///
    /// <h3>And the wreck's door is a coordinate, not a plate</h3>
    ///
    /// <para>On a Hive floor the door somebody leaves through is an <c>UndergroundComplex.LockedDoor</c> with
    /// a sign painted on it, and #731 v1's guard matches the walk's plate back to the building's locked list.
    /// <b>A wreck has neither type nor plate.</b> What it has is <see cref="WreckLayout.ShuttleLockX"/> and
    /// the crew-only rule stated as two functions in Core. So this walk carries no sign at all - this lane
    /// paints nothing on a bulkhead nobody has ever labelled - and the guard asks the LAW instead: the file
    /// stands on the lock's own standoff line, and the captain is held at it while they do.</para>
    /// </summary>
    /// <returns>True when this body is through the hatch and should come off the deck.</returns>
    private bool TheyFileOutThroughTheLock(
        Sweeper s, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        int rank = 0;
        foreach (Sweeper other in _sweepers)
        {
            if (ReferenceEquals(other, s))
            {
                break;
            }
            if (other.State == InspectionTeam.Awareness.Leaving)
            {
                rank++;
            }
        }

        double gx = WreckLayout.ShuttleLockX - Egress.DoorStandoffDu - (rank * InspectionTeam.FileSpacingDu);
        if (s.Walk is null || System.Math.Abs(s.Walk.For.X - gx) > 1e-9)
        {
            s.Walk = OnFoot(
                s.Callsign, new NpcWalk.Bound("", gx, 0),
                new DeckReachability.Point(s.X, s.Y), walls);
            s.AtTheLock = 0;
        }

        if (s.Walk is { } walk)
        {
            walk.Step(dt, walls, _avatarX, _avatarY);
            StepTheBodyTo(s, walk, dt);
            if (walk.Afoot)
            {
                return false;
            }
        }

        // At the head of the file, working the hatch. Anybody behind them stands and waits their turn, which
        // is what a queue is.
        if (rank > 0)
        {
            return false;
        }
        s.AtTheLock += dt;
        return s.AtTheLock >= InspectionTeam.ThroughTheLockSeconds;
    }

    /// <summary>Move to a fresh state and reset its clock. One place, so a state change can never keep an old
    /// clock and time out early.</summary>
    private static void EnterState(Sweeper s, InspectionTeam.Awareness next)
    {
        s.State = next;
        s.StateSeconds = 0;
        s.Announced = false;
        // #731 v2 · …and the route dies with the state. A walk planned for the round is not a walk the hunt
        // may spend, and a stale route is the shape a body teleporting across a compartment hides in.
        s.Walk = null;
    }

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

    /// <summary>The nearest Old One inside this sweeper's cone, and how far off it is. They only fight what they
    /// can actually see — the same lamp, the same walls, no special senses.</summary>
    private (bool Seen, double Range) NearestPackInCone(
        Sweeper s, IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);
        double best = double.MaxValue;

        foreach (Reever r in _reevers)
        {
            if (r.Dormant || !InspectionTeam.Sees(member, r.X, r.Y, sight))
            {
                continue;
            }
            double dx = r.X - s.X, dy = r.Y - s.Y;
            best = System.Math.Min(best, System.Math.Sqrt((dx * dx) + (dy * dy)));
        }

        return (best < double.MaxValue, best);
    }

    /// <summary>
    /// They stop and deal with it. Deliberately abstract: the pack is ground down at the sentries' own rate, they
    /// do not move while doing it, and the captain is free to leave, help, or watch. The one thing this must NOT do
    /// is resolve into a scripted outcome — the owner's line was <i>"You were not offered this and you are not
    /// required to help either one."</i>
    /// </summary>
    private void HoldAndFightThePack(Sweeper s, double dt)
    {
        s.Facing = NearestPackFacing(s) ?? s.Facing;
        s.Vx = 0;   // they stop to shoot, and a stopped mover drops off a motion fan — honestly
        s.Vy = 0;
        _sweepFireSeconds += dt;

        if (_sweepFireSeconds < SentryBot.FireIntervalSeconds)
        {
            return;
        }
        _sweepFireSeconds = 0;

        // Their fire is loud, and it is loud for everybody: it wakes the hull the same way the captain's own
        // guns do. A firefight aft is the captain's best cover and the hull's worst news at once.
        MakeNoiseAboard(s.X, s.Y, LoudEarshot);

        Reever? target = NearestVisiblePackMember(s);
        if (target is null)
        {
            return;
        }

        target.HitsTaken += SentryBot.RoundsPerReever;   // professionals put it down in one burst, not seven
        if (target.HitsTaken >= SentryBot.RoundsPerReever * 2)
        {
            _reevers.Remove(target);
            _surface?.Husks.Add((target.X, target.Y));
        }
    }

    private double _sweepFireSeconds;

    private Reever? NearestVisiblePackMember(Sweeper s)
    {
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);
        Reever? best = null;
        double bestRange = double.MaxValue;

        foreach (Reever r in _reevers)
        {
            if (r.Dormant || !InspectionTeam.Sees(member, r.X, r.Y, sight))
            {
                continue;
            }
            double dx = r.X - s.X, dy = r.Y - s.Y;
            double range = (dx * dx) + (dy * dy);
            if (range < bestRange)
            {
                bestRange = range;
                best = r;
            }
        }

        return best;
    }

    private double? NearestPackFacing(Sweeper s)
    {
        Reever? r = NearestVisiblePackMember(s);
        return r is null ? null : System.Math.Atan2(r.Y - s.Y, r.X - s.X);
    }

    // ── What noise costs ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A racket reaches them, through walls and regardless of where the lamp is pointed — which is what makes
    /// noise discipline the verb of this scene. They get a PLACE and walk to it; if the captain has moved on, three
    /// professionals search an empty compartment for twelve seconds, which is a real tactic and a real cost.
    ///
    /// <para>No cap here, unlike the pack's <c>NoiseRousesAtMost</c>: the whole team shares a channel, so if one
    /// hears it they all hear about it. That is the difference between animals and colleagues.</para>
    /// </summary>
    private void AlertSweepersToNoise(double x, double y)
    {
        foreach (Sweeper s in _sweepers)
        {
            if (s.State is InspectionTeam.Awareness.Challenging or InspectionTeam.Awareness.Hunting)
            {
                continue;   // already on something more interesting than a noise
            }

            InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);
            if (!InspectionTeam.Hears(member, x, y))
            {
                continue;
            }

            bool wasSweeping = s.State == InspectionTeam.Awareness.Sweeping;
            EnterState(s, InspectionTeam.Awareness.Investigating);
            s.GoalX = x;
            s.GoalY = y;

            if (wasSweeping)
            {
                ShowPulseMessage(InspectionTeam.InvestigatingLine(s.Callsign));
                LogAutopilotEvent(InspectionTeam.InvestigatingLine(s.Callsign));
            }
        }
    }

    /// <summary>The challenge ran out. Routed into the same staged death the overdraw and the fifth blow use, so
    /// the piracy insurance issues a new captain and the run continues — Fail Forward, and never a special case.</summary>
    private void ChallengeRunsOut()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        ShowPulseMessage(InspectionTeam.ChallengeUnansweredLine);
        LogAutopilotEvent(InspectionTeam.ChallengeUnansweredLine);
        RendererInterop.PlayCue("alarm");
        // The cause is KNOWN here, so it is passed rather than rolled: being shot by a professional is not
        // being run down by the pack, and the card used to say it was.
        TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false, known: DeathCause.Inspected);
    }

    /// <summary>Fill the sweepers into the droid buffer. Named by callsign, which is how the renderer knows to
    /// draw them cold rather than red — and how a captain reading the deck knows which one is which.</summary>
    private void FillSweeperDroids(DeckPlan.Droid[] buffer, int firstSlot)
    {
        for (int i = 0; i < InspectionTeam.TeamSize; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }

            // #804 · …and NEVER on a floor of the Hive. This band used to fall off the end of the Hive
            // deck's droid count, which parked the sweepers by accident; the count is the whole buffer now
            // (the rounds are the last band), so the accident has to become a rule. A black-ops team hired
            // to strip a derelict is not walking a clandestine basement, and drawing them on one would be
            // the picture claiming something the sim never said.
            buffer[slot] = i < _sweepers.Count && _surface is not { Floor: < 0 }
                ? new DeckPlan.Droid(_sweepers[i].X, _sweepers[i].Y, _sweepers[i].Facing, _sweepers[i].Callsign)
                : new DeckPlan.Droid(-9999, -9999, 0, InspectionTeam.Callsigns[i]);
        }
    }

    /// <summary>
    /// EVERYTHING ABOARD THAT MOVES, for the motion fan. Added because the first playtest of this scene showed
    /// the fan reading "no movement — for now" while three professionals walked the length of her: the tracker
    /// read the pack list directly, and the pack was not who was aboard. One accessor now, so the ear and the
    /// hull can never disagree again about who is walking about in it.
    /// </summary>
    private IEnumerable<MotionTracker.Entity> EverythingThatMoves()
    {
        foreach (Reever r in _reevers)
        {
            yield return new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy);
        }
        foreach (Sweeper s in _sweepers)
        {
            yield return new MotionTracker.Entity(s.X, s.Y, s.Vx, s.Vy);
        }

        // #583 / #633 · AND THE REPO CREW, who were on the other branch's fan and would otherwise have
        // fallen off this one. They WALK, and a motion-only ear hears walking louder than anything else on
        // this ground — which is the whole warning the player gets that the boat that came down is now
        // spread out and coming. Same instrument, no special case: they are contacts. This accessor exists
        // precisely so the ear and the hull can never disagree about who is walking about, so every kind of
        // figure that moves has to be listed HERE and nowhere else.
        foreach (Collector c in _collectors)
        {
            yield return new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy);
        }

        // #804 · AND THE ROUNDS, WHICH ARE THE FIRST THING THE FAN HAS EVER HAD TO HEAR UNDERGROUND. Owner:
        // "We need our motion detector to warn us or that we hear a noise they make before they spot us."
        //
        // Listed HERE and nowhere else, exactly as the comment above demands — and note what it buys for
        // free: the fan hears them through poured wall at #591's degraded reach, the smudge path already
        // draws a wall-blocked return as a smear, and none of that needed a line of new instrument code. A
        // guard walks, so a guard is a contact. There is no special case anywhere.
        // #830 · …AND WITH THEIR REGISTER ON THEM, which is the other half of what a contact is. A guard
        // walking is a mover like anything else; a guard at a beat stop is a LIVING body that has stopped
        // travelling, and the fan owes the captain the unsure blob for it rather than clean silence. The
        // register is Core's own answer (PatrolBeat.FanRegister) and not a flag typed here — the day a
        // second kind of figure walks these floors it declares its own, in Core, beside its own rules.
        // #870 lane 6′a · TheRoundOnFoot is the patrol family's own name for the men walking this floor. The
        // CONTACT is still declared here, exactly as the comment above demands — every kind of figure that
        // moves is listed in this one method — and only the reaching-in is gone.
        foreach (Guard g in TheRoundOnFoot)
        {
            yield return new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister);
        }
    }
}
