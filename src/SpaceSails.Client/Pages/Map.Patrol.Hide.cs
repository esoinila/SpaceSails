using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 split; the header note lives in Map.Patrol.cs) — #821's hide: which cubicle the catch is over, the round heard going past through a partition, and the one man who watched it turn standing outside it.
public sealed partial class Map
{
    // ── #821 · THE HIDE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>#821 · Which cubicle the captain is shut into, or null. One question, off the one set of
    /// shut cells the deck itself is rebuilt from — a second opinion about which doors are over is a second
    /// answer to whether the captain is hidden at all.</summary>
    private (RingOffice.Stall Cell, string Key)? TheCubicleTheCaptainIsShutIn(SurfaceExcursion ex) =>
        CubicleAround(ex) is { Cell: { } cell, Key: { } key } && ex.CubiclesShut.Contains(key)
            ? (cell, key)
            : null;

    /// <summary>#821 · Whether the round has already been heard going past this hide. One line per shut
    /// door: it is the reward for having got in unseen, and a sentence repeated every time a man crosses the
    /// room would be a narrator rather than a beat.</summary>
    private bool _walkedPastSaid;

    /// <summary>#870 lane 6′a · THE NEXT HIDE GETS ITS OWN LINE. Told by the door coming off the catch
    /// (<c>Map.Cubicle.cs</c>'s <c>OpenTheCubicle</c>), which is the one moment a hide is over: one shut
    /// door is one sentence, and a captain who goes back in has earned hearing it again.
    ///
    /// <para>A verb rather than a setter the caller could spell the other way round. ARMING it — saying the
    /// line has already been said, for a hide that has not started — would quietly cost a player the one
    /// beat that tells them the plate did nothing for them, and nothing would report it.</para></summary>
    private void TheNextHideGetsItsOwnLine() => _walkedPastSaid = false;

    /// <summary>
    /// #870 lane 6′a · WHO WAS LOOKING WHEN THE CATCH WENT OVER — the one line of #821 that decides how the
    /// whole feature plays, asked for by <c>Map.Cubicle.cs</c>'s <c>ShutTheCubicle</c> and answered here
    /// because the bit it writes is a guard's.
    ///
    /// <para>It is <see cref="PatrolBeat.Notices"/>, the same predicate the challenge itself is gated on,
    /// over the caller's own sight blockers, on the caller's own frame. The caller asks BEFORE it rebuilds
    /// the deck, because the rebuild is what puts the partition across the opening — asking afterwards would
    /// have every guard in the building answer "no" through the door they just watched you shut.</para>
    /// </summary>
    private void RememberWhoWatchedTheCatchGoOver(IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        foreach (Guard g in _guards)
        {
            if (PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight))
            {
                g.SawYouShutIt = true;
            }
        }
    }

    /// <summary>
    /// #870 lane 6′a · …AND EVERYBODY FORGETS IT AGAIN, which is the other half of the same ruling and
    /// belongs beside it. Asked for by <c>Map.Cubicle.cs</c>'s <c>OpenTheCubicle</c>; hands back the FIRST
    /// man who was standing there knocking, or null.
    ///
    /// <para>Both halves matter and both are why this is one member rather than two. The forgetting is swept
    /// over the whole list rather than stopped at the man who knocked, because a second guard who also
    /// watched the catch go over would otherwise keep the bit and be standing outside the NEXT cubicle the
    /// captain shut, having seen nothing at all — a hide that stopped working for reasons the player cannot
    /// read. And only ONE of them is handed back, because two men doing one job is #777's stacked card.</para>
    ///
    /// <para>What the caller does with him is the caller's: the door is what raises the challenge, and there
    /// is no road to a card in this file.</para>
    /// </summary>
    private Guard? EverybodyForgetsTheCatch()
    {
        Guard? waiting = null;
        foreach (Guard g in _guards)
        {
            waiting ??= g.Knocking ? g : null;
            g.Knocking = false;
            g.Knocked = false;
            g.SawYouShutIt = false;
        }
        return waiting;
    }

    /// <summary>
    /// #821 · A ROUND THAT NEVER SAW YOU, HEARD THROUGH A PARTITION.
    ///
    /// <para>Said once, and only for somebody who has actually come into the room — inside
    /// <see cref="PatrolBeat.NoticeDu"/>, which is the reach at which he WOULD have registered you had the
    /// door been open. That is the whole point of the sentence: it is the moment the lock paid, and a captain
    /// who never hears it has not learned that the plate did nothing for them.</para>
    /// </summary>
    private void TheRoundWalkedPast()
    {
        if (_walkedPastSaid)
        {
            return;
        }

        foreach (Guard g in _guards)
        {
            if (g.SawYouShutIt)
            {
                continue;
            }

            double dx = g.X - _avatarX, dy = g.Y - _avatarY;
            if ((dx * dx) + (dy * dy) > PatrolBeat.NoticeDu * PatrolBeat.NoticeDu)
            {
                continue;
            }

            _walkedPastSaid = true;
            ShowPulseMessage(CubicleLock.WalkedPastLine);
            LogAutopilotEvent(CubicleLock.WalkedPastLine);
            return;
        }
    }

    /// <summary>
    /// #821 · HE WALKS OVER, KNOCKS ONCE, AND WAITS.
    ///
    /// <para>Owner's law, word for word: <i>"A guard who SAW you duck in knocks, then waits, then the escort
    /// line is waiting when you open the door."</i> So this method has no branch that opens anything, no
    /// timer that gives up, and no road to a card — the challenge is raised by the door being OPENED
    /// (<c>Map.Cubicle.OpenTheCubicle</c>), through #833's own walk-up, face to face at arm's length.</para>
    ///
    /// <para>He walks to Core's published STEP square (<see cref="RingOffice.Stall.StepX"/>), which is a
    /// door's clearance outside the leaf — a coordinate the placer chose, never one measured here — on the
    /// same A* and the same gait his round uses, because it is the same man doing the same walk.</para>
    /// </summary>
    private void WaitOutsideTheCubicle(
        Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, in RingOffice.Stall cell)
    {
        g.WalkingUp = false;
        g.RePlanIn -= dt;

        double dx = cell.StepX - g.X, dy = cell.StepY - g.Y;
        if ((dx * dx) + (dy * dy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
        {
            g.Vx = 0;
            g.Vy = 0;
            g.Route = null;
            g.Facing = System.Math.Atan2(cell.DoorY - g.Y, cell.DoorX - g.X);

            if (!g.Knocking)
            {
                g.Knocking = true;
                g.Standing = 0;
            }
            if (!g.Knocked)
            {
                g.Knocked = true;
                ShowPulseMessage(CubicleLock.KnockLine, PulseRank.Beat);
                LogAutopilotEvent(CubicleLock.KnockLine);
                ShowPulseMessage(CubicleLock.BoughtTimeLine);
                RendererInterop.PlayCue("blip");
            }
            return;
        }

        if (g.Route is not { Active: true } || g.RePlanIn <= 0)
        {
            g.RePlanIn = PatrolBeat.RePlanEverySeconds;
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y),
                new DeckReachability.Point(cell.StepX, cell.StepY),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"),
                    new PatrolBeat.Stop(cell.StepX, cell.StepY, "the cubicle door"),
                    MoonSurface.ExpeditionField()));

            if (planned.Route is null)
            {
                // The ground will not give him a route to a door he watched shut. He is not left crossing a
                // washroom forever: he forgets he saw anything and goes back on the round, which is the
                // mildest honest outcome and the only one this file has ever had.
                g.SawYouShutIt = false;
                g.Knocking = false;
                g.Knocked = false;
                g.Route = null;
                return;
            }
            g.Route = planned.Route;
        }

        g.Knocking = false;
        SpendTheStride(g, dt, walls);
    }
}
