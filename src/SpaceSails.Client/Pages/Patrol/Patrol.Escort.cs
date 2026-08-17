using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #833's walked escort: the route to the car he plans himself, the shoulder the captain is walked at, and the one jump-cut left, which admits it is one.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        // ── #833 · THE WALKED ESCORT ──────────────────────────────────────────────────────────────────────
        //
        // Owner, evening playtest 2026-08-11, escorted four times: "how did I jump to elevator there?" … "So the
        // guard walk me back to the car" … "ohhh ... they should definitely show on the motion tracker".
        //
        // What shipped was StandCaptainAt with EscortLine's prose over it: an instant placement narrated as a
        // walk, with the guard left standing wherever he was. Everything below exists to make that sentence
        // literally true, and the guards on it are about the sentence rather than about the geometry.

        /// <summary>
        /// #833 · He plans the route to the car himself and the walk begins. If the ground will not give him one
        /// — which §13.1's audit says cannot happen on a floor this generator builds — the old placement is kept,
        /// with a caption that ADMITS it is a cut. The sentence may never claim a walk the sim did not take.
        /// </summary>
        private void BeginTheWalkBack(Guard g, IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            (double sx, double sy) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());

            // #835 · WHICH FLOOR THIS WALK ENDS ON, decided ONCE and here — above the route, so that even the
            // pathological cut below still ends where the card said it would. It is asked of the escorts BEFORE
            // this one, which is the same number and the same predicate the card in front of the captain was
            // composed from a moment ago (PatrolBeat.TheGuardHasYou): the man who said he was not pressing the
            // button for your floor is the man who does not press it. Two answers to one question, worked out in
            // two places, is the sentence-vs-sim bug class this feature has already paid for twice.
            KickOutDue = PatrolBeat.BookedTooOften(EscortsThisWatch);
            EscortsThisWatch++;

            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(sx, sy),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"), new PatrolBeat.Stop(sx, sy, "the car"),
                    MoonSurface.ExpeditionField()));

            if (planned.Route is null)
            {
                TheCutToTheLift(sx, sy);
                return;
            }

            // The captain's own hands come off the controls for this stretch and only this stretch — including
            // any route he had clicked, which would otherwise walk him out from under the escort.
            _host.CancelAutoWalk(false);

            g.HeStartsWalkingYouOut(planned.Route);
            Escort = g;
            EscortCar = (sx, sy);
            EscortSeconds = 0;
            EscortSaidPumps = false;
        }

        /// <summary>
        /// #833 · One frame of the walk back. He spends his route through the same stepper his round does, and
        /// the captain is WALKED at his shoulder — through <c>DeckPlan.Move</c>, the one primitive the captain's
        /// body is ever stepped by, so the escort obeys the same walls his own legs do and never once places him.
        ///
        /// <para><b>The tether is what makes them arrive together.</b> A guard who out-walked the man he was
        /// escorting would not be escorting anybody, so he waits when the captain falls behind
        /// (<see cref="PatrolBeat.TetherDu"/>) and the captain's legs are worked a little brisker than his
        /// (<see cref="PatrolBeat.CatchUpFactor"/>) until the gap closes.</para>
        ///
        /// <para><b>The last pace is the captain's.</b> Once the guard is standing at the car the captain keeps
        /// walking, to the car's own mouth — which is the exact square the old placement used, arrived at rather
        /// than assigned. There is no <c>StandCaptainAt</c> on this road at all.</para>
        /// </summary>
        private void WalkTheEscort(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            EscortSeconds += dt;
            g.HeCountsDownToARePlan(dt);
            (double sx, double sy) = EscortCar;

            double hx = sx - g.X, hy = sy - g.Y;
            bool heIsThere = (hx * hx) + (hy * hy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;

            double lx = _host.AvatarX - g.X, ly = _host.AvatarY - g.Y;
            bool waitingForYou = (lx * lx) + (ly * ly) > PatrolBeat.TetherDu * PatrolBeat.TetherDu;

            if (heIsThere || waitingForYou)
            {
                g.Vx = 0;
                g.Vy = 0;
                if (heIsThere)
                {
                    g.Facing = System.Math.Atan2(ly, lx);   // at the doors, half turned back to you
                }
            }
            else
            {
                if (g.Route is not { Active: true } || g.RePlanIn <= 0)
                {
                    g.HeWillRePlanInAWhile();
                    AutoWalk.Attempt again = AutoWalk.Plan(
                        true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(sx, sy),
                        walls, DeckPlan.AvatarRadius,
                        PatrolBeat.LatticeFor(
                            new PatrolBeat.Stop(g.X, g.Y, "here"), new PatrolBeat.Stop(sx, sy, "the car"),
                            MoonSurface.ExpeditionField()));
                    if (again.Route is null)
                    {
                        EndTheEscort(g);
                        TheCutToTheLift(sx, sy);
                        return;
                    }
                    g.HeTakesTheRoute(again.Route);
                }

                SpendTheStride(g, dt, walls);
            }

            // …and the captain, walked. His target is the guard's shoulder while the guard is moving, and the
            // car's own mouth once the guard is standing at it.
            (double tx, double ty) = heIsThere ? (sx, sy) : ShoulderOf(g);
            double cdx = tx - _host.AvatarX, cdy = ty - _host.AvatarY;
            double want = System.Math.Sqrt((cdx * cdx) + (cdy * cdy));
            if (want > 1e-6)
            {
                double pace = System.Math.Min(want, PatrolBeat.WalkSpeed * PatrolBeat.CatchUpFactor * dt);
                (_host.AvatarX, _host.AvatarY) = _host.DeckPlan.Move(_host.AvatarX, _host.AvatarY, cdx / want * pace, cdy / want * pace);
                _host.AvatarHeading = System.Math.Atan2(cdy, cdx);
                _host.RefreshAshore();
            }

            // The small talk, once, on the walk — the punishment's whole texture is a man this unbothered.
            if (!EscortSaidPumps && EscortSeconds >= PatrolBeat.PumpsAfterSeconds)
            {
                EscortSaidPumps = true;
                _host.ShowPulseMessage(PatrolBeat.PumpsLine);
                _host.LogAutopilotEvent(PatrolBeat.PumpsLine);
            }

            double adx = sx - _host.AvatarX, ady = sy - _host.AvatarY;
            if (heIsThere && (adx * adx) + (ady * ady) <= PatrolBeat.AtTheCarDu * PatrolBeat.AtTheCarDu)
            {
                bool up = KickOutDue;
                EndTheEscort(g);

                // #835 · THE WALK THAT KEEPS GOING. Owner: "If we get kicked out then maybe we end up back to the
                // surface :-D" — and the picture is all existing geography, so this is one longer walk and no new
                // machinery. He does not go back to his round from here; he gets in with you.
                if (up)
                {
                    KickOutRideDue = true;
                    return;
                }

                _host.ShowPulseMessage(PatrolBeat.EscortDoneLine, PulseRank.Beat);
                _host.LogAutopilotEvent(PatrolBeat.EscortDoneLine);
                return;
            }

            // The bound. A walk that has taken a minute and a half is a walk something is wrong with, and the one
            // honest way out of it is to say so rather than to keep narrating it.
            if (EscortSeconds > PatrolBeat.EscortSecondsCap)
            {
                EndTheEscort(g);
                TheCutToTheLift(sx, sy);
            }
        }

        /// <summary>
        /// #833 · A pace back and a hand's width to his left — where you walk beside somebody who is showing you
        /// out. Taken off his FACING, so the captain swings round the corners with him instead of being dragged
        /// through them.
        ///
        /// <para><b>Mostly IN HIS WAKE, and that is measured rather than styled.</b> A first cut put the captain
        /// half a pace to the side, and a doorway is not half a pace wider than a man: the target kept landing in
        /// stone, the captain slid along the jamb, the tether stretched and the guard stood waiting — an escort
        /// that stuttered its way down the corridor and was moving a THIRD of the time (titan B6: 34%, and it ran
        /// out the whole ninety-second bound without ever reaching the car). Walking where he walked is walkable
        /// by construction, because he has just walked it: the same sweep now measures 99% moving on all 22
        /// floors. Both numbers are <c>TheEscortIsAWalkTests</c>'s own.</para>
        /// </summary>
        private static (double X, double Y) ShoulderOf(Guard g)
        {
            double back = PatrolBeat.ShoulderDu, side = PatrolBeat.ShoulderDu * 0.25;
            return (g.X - (System.Math.Cos(g.Facing) * back) - (System.Math.Sin(g.Facing) * side),
                    g.Y - (System.Math.Sin(g.Facing) * back) + (System.Math.Cos(g.Facing) * side));
        }

        /// <summary>#833 · The controls come back and he goes back to the round — from the car, which is where
        /// the round starts anyway, with the cooldown running so the doors are not a place you get asked twice.</summary>
        private void EndTheEscort(Guard g)
        {
            Escort = null;
            EscortSeconds = 0;
            EscortSaidPumps = false;
            g.Vx = 0;
            g.Vy = 0;
            g.HeIsDoneWalkingYouOut();
        }

        /// <summary>
        /// #833 · THE ONE HONEST JUMP-CUT. Kept for the pathological case only — a floor that will not give a
        /// guard a route to its own car — and it SAYS it is a cut. The old code did this every single time and
        /// narrated it as a walk, which is the sentence-vs-sim bug class the owner caught twice in one evening.
        /// </summary>
        private void TheCutToTheLift(double sx, double sy)
        {
            // Through the one door the sim ever puts the captain through (#681), so a cut can never end inside a
            // wall either.
            _host.StandCaptainAt(sx, sy, "the guard walks you back to the lift");
            _host.ShowPulseMessage(PatrolBeat.EscortCutLine, PulseRank.Beat);
            _host.LogAutopilotEvent(PatrolBeat.EscortCutLine);

            // #835 · …and a cut may shorten the walk but it may never change where it ends. If the card said he
            // was riding up with you, he rides up with you: a jump-cut that quietly downgraded a kick-out to an
            // escort would be the sentence and the sim disagreeing about the one consequence that costs anything.
            if (KickOutDue)
            {
                KickOutRideDue = true;
            }
        }
    }
}
