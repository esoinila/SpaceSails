using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT THEY DO ABOUT THE OLD ONES, AND WHAT THE NOISE COSTS — the cone they see down, the
/// stand-and-fire, the husk that is filed where a body falls, and the alert a gunshot sends through
/// the whole team.
///
/// <para><c>ChallengeRunsOut</c> is here because it is the other end of the same beat: the challenge
/// that was not answered goes into the SAME staged death the overdraw and the fifth blow use.</para>
///
/// <para>Split out of <c>Map.SweepTeam.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered. <c>_sweepFireSeconds</c>, the one field this fight reads, stayed in the opening file
/// with the rest of the family's state.</para>
/// </summary>
public partial class Map
{
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
            if (_surface is { } ex)
            {
                // #316 · Through the one writer, exactly as the captain's own sentries go — a professional's
                // kill leaves the same evidence in the regolith as an amateur's.
                AHuskFallsAt(ex, target.X, target.Y);
            }
        }
    }

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
}
