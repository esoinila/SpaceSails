using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #833's walked escort: the route to the car he plans himself, the pace ahead of him the captain is walked at (#804), and the one jump-cut left, which admits it is one.
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
        /// the captain is WALKED — through <c>DeckPlan.Move</c>, the one primitive the captain's body is ever
        /// stepped by, so the escort obeys the same walls his own legs do and never once places him.
        ///
        /// <para>#804 · <b>AND HE IS WALKED IN FRONT.</b> The canon pass put the arrangement in the guard's own
        /// mouth — <i>"you walk ahead of me to the lift"</i> — so the target is a pace along his own planned
        /// route rather than a pace back into his wake (<see cref="PatrolBeat.AheadOnHisRoute"/>). The route is
        /// what keeps the old guarantee: every leg of it was cleared by the A* at the avatar's radius, so
        /// walking a pace into it is as walkable as his next step and turns the corners before he does.</para>
        ///
        /// <para><b>The tether is what makes them arrive together, and it points the other way now.</b> #833
        /// had the guard wait for a captain who lagged, which is the right rule for a man walked in somebody's
        /// wake. A man walked out in FRONT cannot lag; what he can do is get a corridor ahead, and that is not
        /// compliance, it is leaving. So past <see cref="PatrolBeat.TetherDu"/> it is the CAPTAIN who waits
        /// while the man setting the pace closes up, and <see cref="PatrolBeat.CatchUpFactor"/> is what puts
        /// him out in front in the first place rather than what closes a gap.</para>
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

            // #804 · THE TETHER, THE OTHER WAY ROUND. He is behind you now, so the thing it has to catch is a
            // captain getting out in front — and the one who waits for it is the captain, below.
            bool youAreTooFarAhead = (lx * lx) + (ly * ly) > PatrolBeat.TetherDu * PatrolBeat.TetherDu;

            if (heIsThere)
            {
                g.Vx = 0;
                g.Vy = 0;
                g.Facing = System.Math.Atan2(ly, lx);   // at the doors, half turned back to you
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

            // …and the captain, walked. His target is a pace AHEAD of the guard along the guard's own route
            // while the guard is moving, and the car's own mouth once the guard is standing at it. He does not
            // take the step at all when he is already out past the tether: the man behind him sets the pace.
            (double tx, double ty) = heIsThere ? (sx, sy) : AheadOf(g);
            double cdx = tx - _host.AvatarX, cdy = ty - _host.AvatarY;
            double want = System.Math.Sqrt((cdx * cdx) + (cdy * cdy));
            if (want > 1e-6 && (heIsThere || !youAreTooFarAhead))
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
        /// #804 · A pace out in front of him, on his own route — where you walk when somebody has said
        /// <i>"you walk ahead of me to the lift"</i>.
        ///
        /// <para><b>The arithmetic is Core's</b> (<see cref="PatrolBeat.AheadOnHisRoute"/>) and this is the
        /// hand-off of the one thing Core cannot reach: the waypoints of the route this guard is walking right
        /// now. #833's shipped version did the trigonometry here, and a second author on an escort's geometry is
        /// how a replica and a page come to disagree about where a body is.</para>
        ///
        /// <para><b>Why a ROUTE point and not a ray off his facing</b> is #833's own measurement, read
        /// backwards: half a pace to the SIDE put the target in stone at every doorway and ran the escort at
        /// 34% moving (titan B6) without ever reaching the car, and what fixed it was ground somebody had
        /// already proved walkable. In front of a man there is no such ground — except the A* he is about to
        /// walk, which is exactly what this hands over.</para>
        /// </summary>
        private static (double X, double Y) AheadOf(Guard g) =>
            PatrolBeat.AheadOnHisRoute(g.X, g.Y, g.Facing, g.Route?.Route, PatrolBeat.AheadDu);

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
