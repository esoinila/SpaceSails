using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #821's hide: the round heard going past through a partition, and the one man who watched the catch turn standing outside the door.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
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
        public void RememberWhoWatchedTheCatchGoOver(IReadOnlyList<SurfaceCollision.Segment> sight)
        {
            foreach (Guard g in Guards)
            {
                if (PatrolBeat.Notices(g.X, g.Y, _host.AvatarX, _host.AvatarY, sight))
                {
                    g.HeSeesYouShutTheDoor();
                }
            }
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
            if (WalkedPastSaid)
            {
                return;
            }

            foreach (Guard g in Guards)
            {
                if (g.SawYouShutIt)
                {
                    continue;
                }

                double dx = g.X - _host.AvatarX, dy = g.Y - _host.AvatarY;
                if ((dx * dx) + (dy * dy) > PatrolBeat.NoticeDu * PatrolBeat.NoticeDu)
                {
                    continue;
                }

                WalkedPastSaid = true;
                _host.ShowPulseMessage(CubicleLock.WalkedPastLine);
                _host.LogAutopilotEvent(CubicleLock.WalkedPastLine);
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
            g.HeStopsWalkingUp();
            g.HeCountsDownToARePlan(dt);

            double dx = cell.StepX - g.X, dy = cell.StepY - g.Y;
            if ((dx * dx) + (dy * dy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
            {
                g.Vx = 0;
                g.Vy = 0;
                g.HeDropsHisRoute();
                g.Facing = System.Math.Atan2(cell.DoorY - g.Y, cell.DoorX - g.X);

                if (!g.Knocking)
                {
                    g.HeStandsAtTheDoor();
                }
                if (!g.Knocked)
                {
                    g.HeKnocksOnce();
                    _host.ShowPulseMessage(CubicleLock.KnockLine, PulseRank.Beat);
                    _host.LogAutopilotEvent(CubicleLock.KnockLine);
                    _host.ShowPulseMessage(CubicleLock.BoughtTimeLine);
                    RendererInterop.PlayCue("blip");
                }
                return;
            }

            if (g.Route is not { Active: true } || g.RePlanIn <= 0)
            {
                g.HeWillRePlanInAWhile();
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
                    g.HeForgetsTheCatch();
                    g.HeDropsHisRoute();
                    return;
                }
                g.HeTakesTheRoute(planned.Route);
            }

            g.HeIsStillWalkingToTheDoor();
            SpendTheStride(g, dt, walls);
        }
    }
}
