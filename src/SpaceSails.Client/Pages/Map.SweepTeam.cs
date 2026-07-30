using SpaceSails.Client.Rendering;
using SpaceSails.Core;

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

        foreach (Sweeper s in _sweepers)
        {
            s.StateSeconds += dt;

            InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);
            bool seesCaptain = !CaptainBeyondReach && InspectionTeam.Sees(member, _avatarX, _avatarY, sight);

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
    }

    /// <summary>Move to a fresh state and reset its clock. One place, so a state change can never keep an old
    /// clock and time out early.</summary>
    private static void EnterState(Sweeper s, InspectionTeam.Awareness next)
    {
        s.State = next;
        s.StateSeconds = 0;
        s.Announced = false;
    }

    /// <summary>Work the patrol. On arriving at a waypoint, take the next one — and say so occasionally, because
    /// the waiting IS the scene and it needs a heartbeat.</summary>
    private void WalkTheRoute(Sweeper s, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_sweepRoute.Count == 0)
        {
            return;
        }

        WalkToward(s, s.GoalX, s.GoalY, InspectionTeam.SweepSpeed, dt, walls);

        double dx = s.GoalX - s.X, dy = s.GoalY - s.Y;
        if ((dx * dx) + (dy * dy) > 1.5 * 1.5)
        {
            return;
        }

        s.RouteLeg = (s.RouteLeg + 1) % _sweepRoute.Count;
        AimAtRouteLeg(s);

        // One line per lap, from one of them: enough to know they are still working, not enough to be chatter.
        if (s.RouteLeg == 0)
        {
            LogAutopilotEvent(InspectionTeam.SweepingLine(s.Callsign));
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
        (double nx, double ny) = SurfaceCollision.Slide(
            s.X, s.Y, dx / distance * step, dy / distance * step, DeckPlan.AvatarRadius, walls);

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
        TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false, forcedCause: DeathCause.Inspected);
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

            buffer[slot] = i < _sweepers.Count
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
    }
}
