using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #833's approach — the hail a notice buys, the walk across the floor, and walking away from it — then the challenge itself and #836's wallet: what goes into his hand, what he reads, and what the captain's own book remembers about it.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        // ── #833 · THE APPROACH ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// He has said <i>hold on</i>, and now he crosses the floor to say the rest of it.
        ///
        /// <para>The same A* and the same gait as a leg of his round — it is the same man doing the same walk,
        /// with a moving destination — and the captain's controls are never touched. Three ways out, and only one
        /// of them raises a card: he ARRIVES at <see cref="PatrolBeat.CardReachDu"/>, or the captain walks out
        /// past <see cref="PatrolBeat.GivesUpBeyondDu"/>, or the floor refuses him a route to where you are
        /// standing. The last two put him back on his round with the cooldown running.</para>
        ///
        /// <para><b>#618 · …AND SOMETIMES HE IS NOT WALKING TO A PERSON AT ALL.</b> A man who heard a gun go off
        /// is walking to the PLACE it came from (<see cref="Patrol.LookingIntoIt"/>), and that is the only
        /// difference: the same transition, the same A*, the same stride, the same clock, the same one arm of the
        /// conductor. Three lines below read a destination instead of the avatar, and each of the three ways out
        /// asks which walk this is — because arriving at a door is not a read, a place cannot walk away from you,
        /// and a man who has looked at a hole in a hasp has nothing to say about it
        /// (<see cref="TheNoiseWasNothing"/>). A second walker would have been a second set of edge cases in the
        /// one method on this floor that has already been worth three issues of them.</para>
        /// </summary>
        private void WalkUpToTheCaptain(
            SurfaceExcursion ex, Guard g, int index, double dt, IReadOnlyList<SurfaceCollision.Segment> walls,
            ContactLedger book, double simTime)
        {
            g.HeIsOneFrameFurtherIntoTheWalkUp(dt);
            g.HeCountsDownToARePlan(dt);

            // #618 · WHERE HE IS WALKING TO, asked once and spent three times below.
            bool toTheNoise = ReferenceEquals(g, LookingIntoIt);
            double toX = toTheNoise ? TheNoise.X : _host.AvatarX;
            double toY = toTheNoise ? TheNoise.Y : _host.AvatarY;

            // THE READ HAPPENS HERE AND NOWHERE ELSE. Face to face, at card distance — which is the whole of
            // #833's first half, and the reason this clause is above everything else in the method.
            if (PatrolBeat.AtCardReach(g.X, g.Y, toX, toY))
            {
                g.Vx = 0;
                g.Vy = 0;
                g.HeDropsHisRoute();
                g.Facing = System.Math.Atan2(toY - g.Y, toX - g.X);

                // #618 · He is standing where the gun went off. There is a plate with a hole in it and nobody
                // beside it — a captain still standing there was seen on the way over and this is a hail by now
                // — so he looks at it and goes back to work, and nothing anywhere says a word about it.
                if (toTheNoise)
                {
                    TheNoiseWasNothing(g);
                    return;
                }

                // …unless something else is already in front of the captain. He simply stands there at arm's
                // length until it comes down: a challenge behind a backdrop is a challenge nobody read (#777).
                if (_host.ViewObject is null)
                {
                    g.HeStopsWalkingUp();
                    TheRoundStopsAtYou(ex, g, book, simTime);
                }
                return;
            }

            // WALKING AWAY IS ALLOWED — and #835 did not take that away. Owner's own note on the approach: it is
            // its own tell. The FIRST one in a watch still ends exactly as it always has, with a man stopping
            // where he is and writing something short. It is doing it twice that <see cref="GiveUpTheHail"/> now
            // has an answer for, and that answer is a whole rung further up the ladder.
            //
            // #618 · A PLACE DOES NOT WALK OFF, so the walk to a bang is bounded by its clock and by nothing
            // else. StillComing would have given up on the first frame of every one of them: a shot carries
            // thirty-four deck units and a hail is abandoned past thirteen.
            if (toTheNoise
                ? !PatrolBeat.StillLookingIntoIt(g.WalkUpFor)
                : !PatrolBeat.StillComing(g.WalkUpFor, g.X, g.Y, toX, toY))
            {
                if (toTheNoise)
                {
                    TheNoiseWasNothing(g);
                    return;
                }
                GiveUpTheHail(g, index, walkedAway: true);
                return;
            }

            if (g.Route is not { Active: true } || g.RePlanIn <= 0)
            {
                g.HeWillRePlanInAWhile();
                AutoWalk.Attempt planned = AutoWalk.Plan(
                    true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(toX, toY),
                    walls, DeckPlan.AvatarRadius,
                    PatrolBeat.LatticeFor(
                        new PatrolBeat.Stop(g.X, g.Y, "here"),
                        new PatrolBeat.Stop(toX, toY, toTheNoise ? "the noise" : "you"),
                        MoonSurface.ExpeditionField()));

                if (planned.Route is null)
                {
                    // He can see you and cannot walk to you — a window, a gallery, the far side of a rail. That
                    // is not a challenge, it is a man deciding it is not worth the detour. #835 · And it is NOT
                    // walking away: the captain did nothing, so it may never be counted as the second time he
                    // did it. The ground refused him, and the ground is not the captain's fault.
                    //
                    // #618 · …and a bang behind a door the floor will not route him through is the same refusal
                    // by the same ground, minus the sentence: nobody hailed anybody, so there is nothing to say.
                    if (toTheNoise)
                    {
                        TheNoiseWasNothing(g);
                        return;
                    }
                    GiveUpTheHail(g, index, walkedAway: false);
                    return;
                }
                g.HeTakesTheRoute(planned.Route);
            }

            SpendTheStride(g, dt, walls);
        }

        /// <summary>
        /// #833 · He thinks better of it and goes back to work — from wherever the walk-up left him, with the
        /// cooldown running so the floor does not simply hail you again on the next frame.
        ///
        /// <para>#835 · …unless this is the second time tonight you have done it to him. The first is free and
        /// stays free (<see cref="PatrolBeat.HailsYouMayWalkAwayFrom"/>) — a man who followed you the first time
        /// would make #833's whole approach a trap rather than a decision. The second is one of the three things
        /// that earn a run, and the count is the WATCH's, not this man's: walking off on two different guards is
        /// walking off twice.</para>
        /// </summary>
        /// <param name="walkedAway">Whether the CAPTAIN ended it. False when the floor did — no route, a rail, a
        /// gallery — and a refusal by the ground may never be booked against the man standing in front of it.</param>
        private void GiveUpTheHail(Guard g, int index, bool walkedAway)
        {
            // #836 · Nobody is coming, so the fan comes down. A wallet open in front of a captain with no man
            // crossing the floor is a dialog with no clock on it — and the paper stays in the hand, because
            // deciding who you are is not undone by somebody thinking better of asking.
            WalletFanOpen = false;

            g.HeGivesUp();
            g.Vx = 0;
            g.Vy = 0;

            if (walkedAway && PatrolBeat.WalkingOffEarnsIt(++WalkedAwayThisWatch))
            {
                TheRadioCall(g, PatrolBeat.Provocation.WalkedAwayTwice, index);
                return;
            }

            _host.ShowPulseMessage(PatrolBeat.WalkedAwayLine);
            _host.LogAutopilotEvent(PatrolBeat.WalkedAwayLine);
        }

        // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The first guard who registers the captain HAILS him. One at a time, and never while a card is already
        /// up: two of these on one screen would be the stacked-card mistake #777 named, and a challenge behind a
        /// backdrop is a challenge nobody read.
        ///
        /// <para>#833 · This used to raise the card itself, which is what made the inspection telepathy: a notice
        /// is registered at up to <see cref="PatrolBeat.NoticeDu"/>, and a wallet is read at arm's length. So a
        /// notice now buys the HAIL and nothing else, and the card is the walk-up's business
        /// (<see cref="WalkUpToTheCaptain"/>).</para>
        /// </summary>
        private void StopTheRoundIfAnybodySeesYou(
            IReadOnlyList<SurfaceCollision.Segment> sight, ContactLedger book)
        {
            // …and never while somebody is already on their way over, or walking you out. An approach is the
            // stop, in progress; a second one behind it would be two men doing one job.
            if (_host.ViewObject is not null || !PatrolBeat.CanBeNoticed(FloorSeconds)
                || Escort is not null || EscortDue is not null)
            {
                return;
            }

            // ── #711 · …AND NEVER WHERE THE ANSWER IS ALREADY ON FILE ────────────────────────────────────
            //
            // Canon (head coder, 2026-08-09): "an entity that has already caught you once has an answer for
            // you, and answers are never re-questioned without new cause." This is that sentence, and it is
            // the whole payoff of the fine the captain paid here.
            //
            // It is a SILENCE and not a sentence, which is the only shape §13.8 allows it. A pass-without-a-
            // read told on a card would be the building explaining to the captain that his cover is working,
            // in the feature whose entire premise is that nobody ever says so. What the player gets is that
            // the rounds on this ground stop stopping — and the day they start again, the meter moved a whole
            // band and nothing anywhere will say that either.
            //
            // Asked HERE, at the sighting, rather than inside the read: a man who has an answer for you does
            // not walk over and then decline to ask. He looks at you and carries on doing his corridors.
            if (_host.Surface is { } settled && UnlistedParcel.TheFolderIsClosed(book, settled.Stop.Body.Id))
            {
                return;
            }

            foreach (Guard g in Guards)
            {
                if (g.WalkingUp || g.AfterYou)
                {
                    return;
                }
            }

            for (int i = 0; i < Guards.Count; i++)
            {
                Guard g = Guards[i];
                if (g.SinceStop < PatrolBeat.AfterTheStopSeconds
                    || !PatrolBeat.Notices(g.X, g.Y, _host.AvatarX, _host.AvatarY, sight))
                {
                    continue;
                }

                // #835 · THE ONE PLACE A SIGHTING CAN BUY ANYTHING BUT A HAIL, and it is not the sighting that
                // buys it — it is the four lines already on the clipboard. Owner: the fiction strains when the
                // same guard books the same face four times and just keeps walking. Everything else about this
                // loop is #833's, unchanged: notice, hail, walk over, read.
                if (PatrolBeat.BookedTooOften(EscortsThisWatch))
                {
                    TheRadioCall(g, PatrolBeat.Provocation.BookedTooManyTimes, i);
                    return;
                }

                TheHail(g);
                return;
            }
        }

        /// <summary>
        /// #833 · THE HAIL. He turns, says the one short line, and starts walking — and that is the whole of what
        /// a notice buys. It is the second of warning that makes the approach a beat rather than an ambush: a
        /// captain with a badge gets it out, and a captain mid-mischief has a corridor's length to decide.
        ///
        /// <para>The cooldown clock starts HERE rather than at the card, so a hail that is walked away from costs
        /// the same silence as one that ends in a read — the floor does not get to keep asking.</para>
        /// </summary>
        public void TheHail(Guard g)
        {
            FanTheWallet();
            g.HeStartsWalkingUp();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);

            _host.ShowPulseMessage(PatrolBeat.HailLine, PulseRank.Beat);
            _host.LogAutopilotEvent(PatrolBeat.HailLine);
            RendererInterop.PlayCue("blip");
        }
    }
}
