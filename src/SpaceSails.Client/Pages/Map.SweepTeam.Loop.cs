using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE ONCE-A-FRAME ADVANCE — walk them, look for the captain, resolve what they find; and the
/// #731 v2 hatch, which is the one place a sweeper can leave the deck the way he came onto it.
///
/// <para>Split out of <c>Map.SweepTeam.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered, and not one field moved.</para>
/// </summary>
public partial class Map
{
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

        // #731 (airlock egress) · The sum is Core's now — `Egress.PlaceInTheFile` — because the repo crew
        // filing home to their own boat queues at a hatch the same way, and two copies of "a body-width
        // behind the one in front" is the mirrored-constant bug with somebody standing in it. The numbers
        // are unchanged: the head one standoff off the lock, everybody else a spacing further back down
        // the spine (the file runs in -x, which is the way they came).
        (double gx, _) = Egress.PlaceInTheFile(
            WreckLayout.ShuttleLockX, 0, -1, 0, rank, InspectionTeam.FileSpacingDu);
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
}
