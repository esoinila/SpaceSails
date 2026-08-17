using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — the floor: who is on it, whether the round has eyes on the captain, the one frame that walks them all, and the ORDER that decides the one thing each man is doing.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        /// <summary>#870 lane 6′a · IS ANYBODY ON THE ROTA WATCHING THE CAPTAIN RIGHT NOW? Asked by
        /// <c>Map.Bin.cs</c>, which needs the top rung of <see cref="RipAndBin.WhoSaw"/>'s ladder: tearing
        /// something up in front of a round is not the same as tearing it up alone.
        ///
        /// <para>It is the challenge's own predicate (<see cref="PatrolBeat.Notices"/>), over the caller's own
        /// sight blockers, INCLUDING the grace off the car (<see cref="PatrolBeat.CanBeNoticed"/>) — a guard who
        /// has not registered you yet has not seen you do anything. Both halves live here so that a second
        /// caller cannot take one of them and miss the other.</para></summary>
        public bool TheRoundHasEyesOnYou(IReadOnlyList<SurfaceCollision.Segment> sight)
        {
            bool seen = false;
            if (PatrolBeat.CanBeNoticed(FloorSeconds))
            {
                foreach (Guard g in Guards)
                {
                    seen |= PatrolBeat.Notices(g.X, g.Y, _host.AvatarX, _host.AvatarY, sight);
                }
            }
            return seen;
        }

        /// <summary>How long between mentions of the boots. Long enough that it is an event; short enough that
        /// a captain who has walked away and come back is told again.</summary>
        private const double HeardAgainSeconds = 20.0;
        // ── PUTTING THEM ON THE FLOOR ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Build the round for the floor the captain has just arrived on, or clear it if this floor has none.
        /// Called from the lift ride — the one place a floor changes — and never from the deck rebuild, which
        /// happens every time a room is searched and would restart the round under a captain who was timing it.
        /// </summary>
        public void SpawnPatrolFor(SurfaceExcursion ex)
        {
            Guards.Clear();
            Beat.Clear();
            Readables.Clear();
            FloorSeconds = 0;
            HeardAgo = HeardAgainSeconds;

            // #833 · …and the walk back dies with the floor it was being walked on. The car IS the destination,
            // so a captain who has ridden it is a captain the escort is over for — and an escort holding a guard
            // off a list that has just been cleared would hold the controls forever.
            Escort = null;
            EscortDue = null;
            EscortSeconds = 0;
            EscortSaidPumps = false;

            // #835 · …and so does the run and the ride it was owed. A captain who got into the car mid-run has
            // ESCAPED — rung five, and the honest one — so the man who was coming is left standing on a floor
            // this page is no longer simulating. The ride owed is cleared for the same reason it is cleared at
            // the top of TheKickOut: it has either just happened or it never will.
            KickOutDue = false;
            KickOutRideDue = false;

            // #836 · …and so does the paper in your hand. A hand goes to a pocket for a man who is walking over,
            // and the man is a floor above now. The BOOK is not cleared here — that is the durable half, and a
            // captain who has ridden one floor has not forgotten which name worked downstairs.
            WalletFanOpen = false;
            PaperInHand = null;

            // #821 · …and so does the hide. A floor change is a new set of doors and a new set of men, and a
            // "he walked past" line kept from the floor above would be a beat about a room nobody is in.
            //
            // The CATCHES go with it, and the reason is in the field's own doc: a catch is a thing a hand is
            // holding shut, and the hand has just ridden the lift. Today it cannot happen — the only way out of
            // a shut cubicle is to turn the catch back — but a door left OCCUPIED on a floor nobody is standing
            // on would be a room the building had sealed against itself, forever, with nothing to say why. The
            // set is the excursion's rather than the vault's for the same reason (see SurfaceExcursion).
            WalkedPastSaid = false;
            ex.CubiclesShut.Clear();

            string bodyId = ex.Stop.Body.Id;
            int level = ex.Floor;

            // #835 · THE WATCH'S OWN MEMORY, turned over with the shift and with nothing else. Asked before the
            // patrolled-floor gate below, because a captain who has been thrown out and has come back down to a
            // floor with nobody on it is still the same evening.
            if (Watch != ex.CanteenWatch)
            {
                Watch = ex.CanteenWatch;
                EscortsThisWatch = 0;
                WalkedAwayThisWatch = 0;
            }

            if (!PatrolBeat.IsPatrolled(bodyId, level))
            {
                return;
            }

            SurfaceLayout.Field field = MoonSurface.ExpeditionField();
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);

            Beat.AddRange(PatrolBeat.BeatFor(bodyId, level, ex.CanteenWatch, floor, field));
            if (Beat.Count < 2)
            {
                // A floor whose plan yields nothing to walk gets nobody. It is not a failure worth a sentence —
                // an empty corridor is what this building is mostly made of — but it must not put a guard on a
                // round with one stop in it, standing still forever at the car.
                Beat.Clear();
                return;
            }

            // #831 · …and everything on this floor's walls a held man could be reading. Off Core, once, with the
            // round it belongs to.
            Readables.AddRange(PatrolBeat.ReadablesOn(floor, field));

            int heads = RoundsCheat is { } forced
                ? System.Math.Clamp(forced, 0, PatrolBeat.MostOnAFloor)
                : PatrolBeat.GuardsOn(bodyId, level, ex.CanteenWatch);

            for (int i = 0; i < heads; i++)
            {
                int leg = PatrolBeat.StartLeg(Beat.Count, i, System.Math.Max(1, heads));
                PatrolBeat.Stop at = Beat[leg];
                Guards.Add(new Guard
                {
                    DeckName = PatrolBeat.DeckName(i),
                    Plate = PatrolBeat.PlateOf(bodyId, level, ex.CanteenWatch, i),
                    X = at.X,
                    Y = at.Y,
                    Leg = (leg + 1) % Beat.Count,
                    Standing = PatrolBeat.StandSeconds,

                    // #831 · A man is put on the floor already signing the station he is standing at, looking at
                    // it. The first thing a captain stepping off the car sees is somebody doing something.
                    Facing = at.Point is { } start ? start.Facing : 0,
                    SignedPoint = at.Point is { } signed ? signed.Number : 0,
                });
            }
        }

        // ── THE LOOP ──────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Walk them, decide what each side can know about the other, and let a sighting raise the
        /// card. Called once a frame from <c>StepSurface</c>.</summary>
        public void AdvancePatrol(double dtRealSeconds)
        {
            // #835 · …and the one clause that runs where nothing else here does. The KICKED OUT plate is painted
            // on the SURFACE, which is the one place this file has no guards, no beat and no floor — so its clock
            // is above the gate rather than behind it.
            FadeTheKickedOutPlate(dtRealSeconds);

            if (Guards.Count == 0 || _host.Surface is not { } ex || ex.Floor >= 0)
            {
                return;
            }

            double dt = System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
            FloorSeconds += dt;
            HeardAgo += dt;

            IReadOnlyList<SurfaceCollision.Segment> walls = _host.DeckPlan.CollisionField;
            IReadOnlyList<SurfaceCollision.Segment> sight = _host.SightBlockers();

            // #833 · THE CARD HAS COME DOWN, SO THE WALK BEGINS. Asked here rather than wired into
            // CloseViewObject because this is the one place that runs every frame of every floor: whichever road
            // out of the card the captain took — Esc, Enter, E, the backdrop, Close — the walk starts on the
            // first frame after it, and there is no fifth road that could miss it.
            if (EscortDue is { } due && _host.ViewObject is null)
            {
                EscortDue = null;
                BeginTheWalkBack(due, walls);
            }

            // #835 · …and the same clause for the rung above it. The ride up is armed at the car and taken here,
            // on a frame of its own: the ride empties the list of guards this method is about to walk, and a loop
            // that cleared its own collection mid-iteration is a bug looking for a rare afternoon.
            if (KickOutRideDue && _host.ViewObject is null)
            {
                KickOutRideDue = false;
                TheKickOut(ex);
                return;
            }

            // #793 · DOES ANYBODY HAVE TO STOP BECAUSE THE CAPTAIN DID? Owner, on the bench: "it is a good
            // gumshoe move to see if anyone is following us by foot, as they would need to stop moving also."
            // The question is asked of Core once a frame, of every mover, and the answer today is always no —
            // a round is a published route (PatrolBeat.OnTheRound) and a published route is not a tail. This is
            // the SEAM: the hold lives on the stepper every mover already goes through, so nothing has to be
            // rebuilt the day something on this floor is actually following somebody.
            bool sitting = _host.SeatedOnABenchInTheOpen;

            // ── #821 · IS THE CAPTAIN SHUT INTO A CUBICLE? ─────────────────────────────────────────────────
            //
            // Asked ONCE a frame and handed to everything below, so the door that is drawn shut, the door the
            // round cannot see through and the door the exposure ladder calls private are one door.
            //
            // THE SENTRY'S OWN LAW LIVES IN THE TWO BRANCHES IT OPENS, and in nothing else: a guard who watched
            // the catch go over stands outside it (WaitAtTheDoor); a guard who did not walks his beat past an
            // OCCUPIED plate without breaking stride. IT BUYS TIME, NOT SAFETY — there is no branch here in
            // which a locked door ends a challenge, and CubicleLock.OpensALockedCubicle is a constant false
            // rather than a rule, so no future edit can quietly make one.
            (RingOffice.Stall Cell, string Key)? hide = _host.TheCubicleTheCaptainIsShutIn(ex);

            bool anythingHeard = false;
            for (int i = 0; i < Guards.Count; i++)
            {
                Guard g = Guards[i];
                g.SinceStop += dt;

                // #831 · One answer per frame about whether he is performing a cover act, written below by the
                // hold and by nothing else — a man on his round is not covering for anything.
                g.CoverPoint = 0;

                FootTail.Mover afoot = PatrolBeat.OnTheRound(i, g.X, g.Y);
                g.Held = FootTail.MustHold(sitting, _host.AvatarX, _host.AvatarY, in afoot, sight);
                if (!g.Held)
                {
                    // #831 · The hold is over, so whatever he had decided to read is over with it. Cleared HERE,
                    // off the law's own answer, rather than in each of the branches below that walk him away.
                    g.CoverAt = null;
                    g.CoverFor = 0;
                }
                TheOneThingHeIsDoingThisFrame(ex, g, i, dt, walls, sight, hide);

                // WHAT THE CAPTAIN MAY KNOW. One call, one answer, used by the marker and by nothing else — so
                // a guard behind a wall is off the deck by construction rather than by a renderer's opinion.
                // #832 · …and it is now a THREE-rung answer, because the eye's edge is not a cliff: the far
                // fifth of the reach is a distant figure with no round number on it, and only past the whole
                // reach — or behind a wall — is there nothing at all.
                g.Seen = PatrolBeat.SightingFor(_host.AvatarX, _host.AvatarY, g.X, g.Y, sight);
                anythingHeard |= PatrolBeat.Heard(_host.AvatarX, _host.AvatarY, g.X, g.Y, sight);
            }

            // …and the ear, once, cooled. It is deliberately said only for somebody the captain CANNOT see: a
            // line about boots over a marker you are looking at is the picture and the sentence disagreeing.
            if (anythingHeard && HeardAgo >= HeardAgainSeconds)
            {
                HeardAgo = 0;
                _host.ShowPulseMessage(PatrolBeat.HeardLine);
                _host.LogAutopilotEvent(PatrolBeat.HeardLine);
            }

            // #821 · A SHUT DOOR IS NOT A DISGUISE, IT IS A WALL. Nobody new registers the captain while it is
            // over — not because the door hides them, but because there is a partition between the two of them
            // and PatrolBeat.Notices is a sightline question. Asked here, once, rather than inside the loop: a
            // hail raised on the frame the catch turned would be a man challenging a door.
            if (hide is null)
            {
                StopTheRoundIfAnybodySeesYou(sight);
            }
            else
            {
                TheRoundWalkedPast();
            }
        }

        // ── #870 · THE ONE THING HE IS DOING, AND THE ORDER THAT DECIDES IT ───────────────────────────────

        /// <summary>
        /// #870 · WHAT THIS MAN IS DOING THIS FRAME — exactly one of seven things, and the ORDER IS THE FEATURE.
        ///
        /// <para>Every arm below was written into one <c>if / else if</c> chain by the six issues that built this
        /// file, and the chain's order carried five separate rulings that were named nowhere. It is a list now,
        /// read top to bottom, and the first arm that will have him takes him. Each arm's own docblock says what
        /// it is; this is the priority, and WHY it is this priority:</para>
        ///
        /// <list type="number">
        /// <item><b>He is walking you out</b> (#833) — an escort in progress is not a thing a bench can stop and
        /// not a thing a sighting can interrupt. He is not on a round at all, so nothing below applies to him.</item>
        /// <item><b>He is waiting outside the door he saw you shut</b> (#821) — ABOVE the run, and that is the
        /// whole of what a locked door is worth: a man coming at a run cannot run through a partition.</item>
        /// <item><b>He is coming after you</b> (#835) — above everything a round does, because a man who has said
        /// your floor into a radio has stopped doing his rounds.</item>
        /// <item><b>He has lost you to a door</b> (#821/#835) — only ever reached by a man who did NOT watch the
        /// catch turn, because the man who did is two arms up, knocking.</item>
        /// <item><b>He is made and covering for it</b> (#793/#831) — the hold, which is asked once a frame of
        /// every mover in <see cref="AdvancePatrol"/> and spent here.</item>
        /// <item><b>He is crossing the floor to you</b> (#833) — a hail is a detour from the round, so it sits
        /// under everything that is not the round and over the round itself.</item>
        /// <item>…and if nobody wants anything of him, <b>he walks his round</b>. The default, the ordinary case,
        /// and the one the whole floor is mostly made of.</item>
        /// </list>
        ///
        /// <para><b>Nothing here decides anything.</b> The list above is <see cref="Guard.Doing"/>'s own member
        /// order and the tests that pick between them are <see cref="Guard.DoingThisFrame"/> — the same six
        /// predicates 7d carried across, in the same order, in one expression on the man they are about. This
        /// method is the dispatch and nothing else: one arm per state, no clause of its own, and no way for the
        /// state a guard reports and the arm he is walked through to be two different answers.</para>
        ///
        /// <para>The #870 lane that made it a list pinned every frame of twelve rounds first
        /// (<c>EveryRoundFingerprintsTheSameTests</c>) and reproduced every hash after — including a RED run with
        /// two of these lines swapped, which is the proof that the order is load-bearing rather than decorative.
        /// 6′d moved the order onto the type and reproduced all thirteen again.</para>
        /// </summary>
        private void TheOneThingHeIsDoingThisFrame(
            SurfaceExcursion ex, Guard g, int index, double dt,
            IReadOnlyList<SurfaceCollision.Segment> walls,
            IReadOnlyList<SurfaceCollision.Segment> sight,
            (RingOffice.Stall Cell, string Key)? hide)
        {
            switch (g.DoingThisFrame(ReferenceEquals(g, Escort), hide is not null))
            {
                case Guard.Doing.Escorting: HeIsWalkingYouOut(g, dt, walls); break;
                case Guard.Doing.AtTheDoor: HeIsWaitingOutsideTheDoorHeSawYouShut(g, dt, walls, hide!.Value.Cell); break;
                case Guard.Doing.AfterYou: HeIsComingAfterYou(ex, g, dt, walls); break;
                case Guard.Doing.LostToADoor: HeHasLostYouToADoor(g, index); break;
                case Guard.Doing.Covering: HeIsMadeAndCoveringForIt(g, dt, walls, sight); break;
                case Guard.Doing.WalkingUp: HeIsCrossingTheFloorToYou(ex, g, index, dt, walls); break;
                default: WalkTheRound(g, dt, walls); break;
            }
        }

        /// <summary>#833 · THE ESCORT. The one guard on the floor who is not walking a round, walking the captain
        /// off it at his shoulder.</summary>
        private void HeIsWalkingYouOut(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            // #833 · The one guard who is not walking a round at all. He is ahead of everything else in
            // this loop because an escort in progress is not a thing a bench can stop and not a thing a
            // sighting can interrupt: the captain is at his shoulder, and there is nothing left to see.
            g.Held = false;
            WalkTheEscort(g, dt, walls);
        }

        /// <summary>#821 · THE KNOCK. He watched the catch go over, so he is standing outside it — and standing
        /// outside it is the whole of what he does.</summary>
        private void HeIsWaitingOutsideTheDoorHeSawYouShut(
            Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, in RingOffice.Stall cell)
        {
            // #821 · He watched the catch go over. He does not open it — nothing in this game does
            // (CubicleLock.OpensALockedCubicle) — he walks over, knocks once, and waits.
            //
            // ABOVE #835's RUN, and that is the whole of what the door is worth. A man coming at a run
            // cannot run through a partition: he arrives, and then he is a man standing outside a door.
            // What he is NOT is finished — he keeps AfterYou and he keeps his reason, so opening the
            // door gives the captain back the exact rung of the ladder they ducked out of rather than a
            // softer one. IT BUYS TIME, NOT SAFETY, and this branch is where that sentence is spent.
            g.Held = false;
            WaitOutsideTheCubicle(g, dt, walls, in cell);
        }

        /// <summary>#835 · THE RUN. He has called it in and he is coming — the earned branch, and never the
        /// ambient one.</summary>
        private void HeIsComingAfterYou(
            SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            // #835 · The other man who is not walking a round. He is held off the bench law for the
            // escort's own reason and one of his own: #793's hold is a law about a TAIL — something
            // following you covertly, which has to stop when you stop or the trick is up. A man who has
            // said your floor and your direction into a radio is not being covert about anything.
            //
            // #821 · A run that did NOT see the catch turn carries on to where you were, which is what
            // losing somebody looks like, and #835's own cap ends it. A locked door is not a smoke bomb.
            g.Held = false;
            RunAfterTheCaptain(ex, g, dt, walls);
        }

        /// <summary>#821 · THE EMPTY WASHROOM. He was crossing the floor to somebody who is behind a door now,
        /// and that is the ground ending his hail rather than the captain doing it.</summary>
        private void HeHasLostYouToADoor(Guard g, int index)
        {
            // …and a man who was crossing the floor to somebody who is now behind a door has lost them.
            //
            // #835 · NOT BOOKED AS WALKING OFF, and the distinction is exact rather than generous. This
            // branch is only ever reached by a guard who did NOT see the catch turn — the man who did is
            // two branches up, knocking — so what happened is that somebody came round a corner into an
            // empty washroom. That is the GROUND ending it, and #835's own rule is that a refusal by the
            // ground may never be booked against the man standing in front of it. The captain who ducks
            // in where he can see them does not get this branch at all; they get the knock, which is the
            // whole of what the door was ever worth.
            GiveUpTheHail(g, index, walkedAway: false);
            g.Held = false;
        }

        /// <summary>#793/#831 · THE COVER ACT. A made tail that has been stopped by a captain sitting down, doing
        /// something plausible about it.</summary>
        private void HeIsMadeAndCoveringForIt(
            Guard g, double dt,
            IReadOnlyList<SurfaceCollision.Segment> walls,
            IReadOnlyList<SurfaceCollision.Segment> sight)
        {
            // A tail that has been made cannot walk on past you. It stops where it stopped, and it drops
            // off the motion fan honestly while it does — the same clause the stand at a stop keeps.
            //
            // #831 · …and it stops AT SOMETHING. The rule is untouched; the picture is not a statue.
            TheCoverAct(g, dt, walls, sight);
        }

        /// <summary>#833 · THE APPROACH. He has said hold on, and he is walking over to say the rest of it to
        /// your face.</summary>
        private void HeIsCrossingTheFloorToYou(
            SurfaceExcursion ex, Guard g, int index, double dt,
            IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            WalkUpToTheCaptain(ex, g, index, dt, walls);
        }
    }
}
