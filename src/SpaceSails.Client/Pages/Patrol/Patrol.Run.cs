using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — where the site's own pass comes from, #835's earned run and the hand on your arm, the kick-out back to the sky and the plate over the shed — and the figures the captain may actually see.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        // ── WHERE THE PASS COMES FROM ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #804 · The site puts the captain on its books. Called from the ARRIVAL the day-labour chit opened
        /// (#752) — the moment the gig is not a promise any more — and once per site, because a wallet with two
        /// identical passes in it would be the pocket saying something the sim did not.
        /// </summary>
        public void IssueTheSitePass(SurfaceExcursion ex)
        {
            string bodyId = ex.Stop.Body.Id;
            if (PatrolBeat.BadgeHeld(bodyId, _host.Satchel))
            {
                return;
            }

            Satchel.Item pass = PatrolBeat.Badge(bodyId);
            if (!Satchel.CanTake(_host.Satchel, pass))
            {
                return;   // the wallet never fills, so this cannot happen — and a silent grant that did not land
            }             // is exactly the third named bug class, so it is asked rather than assumed (§13.9).

            _host.Satchel = [.. Satchel.Add(_host.Satchel, pass)];
            _host.ShowPulseMessage(PatrolBeat.BadgeIssuedLine);
            _host.LogAutopilotEvent(PatrolBeat.BadgeIssuedLine);
            _host.FileNote(PatrolBeat.BadgeGist, PatrolBeat.BadgeGlyph);
        }

        // ── #835 · THE OTHER BRANCH ───────────────────────────────────────────────────────────────────────
        //
        // Owner, evening playtest 2026-08-11: "they need to catch us .... like reevers :-D we could use that code
        // :-D" … "just no damage by default :-D" … "If we get kicked out then maybe we end up back to the surface
        // :-D".
        //
        // THREE THINGS ABOUT EVERYTHING BELOW, and all three are what keep it from being a stealth level:
        //
        //   1. IT IS EARNED. The only callers of TheRadioCall are the three provocations, and every one of them
        //      is a thing the captain chose to do. A round that merely sees somebody still hails (#833).
        //   2. HE IS PROCEDURAL, NOT FERAL. The radio comes first and he stands still to say it, which is the
        //      warning; then he runs on the Old Ones' own homing step (the owner named that code) at a PERSON's
        //      gait, so he finds a doorway rather than grinding his shoulder on the jamb.
        //   3. NOTHING HERE TOUCHES THE BODY. There is no HitsTaken in this file, no swing, no roll. Being caught
        //      costs one pip of nerve and the rest of your evening; the horror stays with the Old Ones.
        //
        // AND HE CANNOT OPEN ANYTHING YOU CANNOT. The run is spent through SurfaceCollision.Slide against
        // _host.DeckPlan.CollisionField — the captain's own walls, the same list his own legs are stepped against — so
        // a door that is shut for the captain is a wall for the man behind him, by construction rather than by a
        // clause. That is the whole of rung five's promise about a locked room: whatever the door's state is, it
        // is one state, and both of them are asking it.

        /// <summary>
        /// #835 · HE SAYS IT INTO THE RADIO, AND THEN HE COMES. The one road into a run, so a fourth trigger
        /// invented tomorrow has to come through here and be a <see cref="PatrolBeat.Provocation"/> to do it.
        ///
        /// <para>The round is suspended while it lasts and resumes from wherever he ends up, exactly as a walk-up
        /// does — a run is a longer detour, never a second state machine.</para>
        /// </summary>
        /// <param name="index">His place in the list, which fixes the hand he takes a wall on. Two men rounding a
        /// slab from opposite ends is <c>ReeverChase</c>'s own idiom and its own reason.</param>
        private void TheRadioCall(Guard g, PatrolBeat.Provocation why, int index)
        {
            if (!PatrolBeat.EarnsIt(why))
            {
                return;   // the gate, asked rather than assumed: nothing may run on Provocation.None
            }

            // #836 · …and the fan comes down here too. A man who has said your floor into a radio is not going to
            // look at a piece of paper, and a chooser left up over a run would be a decision with nothing on the
            // other end of it.
            WalletFanOpen = false;

            g.HeCallsItIn(why, index);
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);

            _host.ShowPulseMessage(PatrolBeat.CallsItInLine, PulseRank.Beat);
            _host.LogAutopilotEvent(PatrolBeat.CallsItInLine);
            RendererInterop.PlayCue("blip");
        }

        /// <summary>
        /// #835 · ONE FRAME OF BEING COME AFTER. <c>ReeverChase.Step</c>, which is the code the owner pointed at,
        /// with the one thing a uniform changes about it: the legs are <c>Gait.Person</c>, so he goes through the
        /// doorway a shambler would grind against.
        ///
        /// <para><b>He is not planned and that is deliberate.</b> The round and the walk-up are A*, because a man
        /// doing his job takes a route; a man running after somebody does not plan, he comes at you and grazes
        /// the walls. The handrail is #324's own crude try-perpendicular and it is the whole of his cleverness —
        /// which is why a corner is worth taking and a shut door is worth being behind.</para>
        ///
        /// <para>Three ways out, and none of them removes him from the floor: he has you
        /// (<see cref="HeHasYou"/>), he loses you (<see cref="HeLosesYou"/>), or the captain rides the car and
        /// the whole floor stops being simulated — which is the escape rung five is about.</para>
        /// </summary>
        private void RunAfterTheCaptain(
            SurfaceExcursion ex, Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
        {
            g.HeIsOneFrameFurtherIntoTheRun(dt);

            // THE RADIO FIRST, AND HE STANDS STILL FOR IT. This beat is the warning, and a man who talked while
            // he ran would have spent it. He is off the fan for it too, honestly — he is not travelling.
            if (g.CallingIn > 0)
            {
                g.HeSpendsAFrameOnTheRadio(dt);
                g.Vx = 0;
                g.Vy = 0;
                g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);
                return;
            }

            if (PatrolBeat.HasYou(g.X, g.Y, _host.AvatarX, _host.AvatarY))
            {
                HeHasYou(ex, g);
                return;
            }

            if (!PatrolBeat.StillAfterYou(g.AfterYouFor, g.X, g.Y, _host.AvatarX, _host.AvatarY))
            {
                HeLosesYou(g);
                return;
            }

            double startX = g.X, startY = g.Y;
            (g.X, g.Y) = ReeverChase.Step(
                g.X, g.Y, _host.AvatarX, _host.AvatarY, PatrolBeat.AfterYouSpeed * dt,
                // No barrier. The Old Ones are penned on their side of a crew-only door; a man on the payroll is
                // already inside the building and there is nothing down here he is not allowed past. What stops
                // him is the walls, and the walls are the captain's own.
                barrierY: double.PositiveInfinity,
                walls, DeckPlan.AvatarRadius, g.WallSide, SurfaceCollision.Gait.Person);

            // …and the fan is told what actually happened, not what was asked for — #832's whole lesson. A man
            // flat against stone is a man standing still, and the instrument may say so.
            double mx = g.X - startX, my = g.Y - startY;
            g.Vx = dt > 0 ? mx / dt : 0;
            g.Vy = dt > 0 ? my / dt : 0;
            if ((mx * mx) + (my * my) > 1e-8)
            {
                g.Facing = System.Math.Atan2(my, mx);
            }
        }

        /// <summary>
        /// #835 · A HAND ON YOUR ARM — and that is the entire physical event.
        ///
        /// <para><b>Zero damage, by the owner's own ruling.</b> Nothing in this method reads or writes
        /// <c>HitsTaken</c>, swings anything or rolls anything, and the whole file is grep-able for that. One pip
        /// of nerve moves, because being run down in a corridor is frightening, and it is the TOUCH pip — the
        /// lump the model already keeps for a hand laid on you — rather than a new cause nobody ruled on.</para>
        ///
        /// <para>Then it is the ladder, and the ladder is the escort you already know: the card goes up, and the
        /// walk it names starts on the first frame after it comes down (<see cref="Patrol.EscortDue"/>), which is the
        /// frame the captain can watch it happen on.</para>
        /// </summary>
        private void HeHasYou(SurfaceExcursion ex, Guard g)
        {
            // …unless something else is in front of the captain. He stands there with your arm until it comes
            // down: a catch behind a backdrop is a catch nobody read (#777).
            if (_host.ViewObject is not null)
            {
                g.Vx = 0;
                g.Vy = 0;
                return;
            }

            PatrolBeat.Provocation why = g.Why;
            EndTheRun(g);
            g.HeStandsAtYou();
            g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);

            // The same two-armed card the wallet read is told on, with the guard's own plate behind it — one
            // picture for the whole feature, because the man in it has not decided anything yet either (#804).
            PatrolBeat.Read held = PatrolBeat.TheGuardHasYou(g.Plate, why, EscortsThisWatch);
            _host.ViewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_host.AvatarX, (float)_host.AvatarY,
                held.Label, PatrolBeat.ChallengeArtUrl, held.Card, held.Told);
            RendererInterop.PlayCue("reveal");
            _host.LogAutopilotEvent($"{held.Label} — {held.Told}");

            _host.ApplyNerveShock(NervePips.TouchPips * NervePips.PipUnit, "a hand closed on your arm in a corridor");
            _host.FileNote(PatrolBeat.EscortNote, "👮");

            EscortDue = g;
            _host.RequestVaultSave();
        }

        /// <summary>#835 · HE HAS LOST YOU, AND HE IS STILL THERE. The other end of the run, and the one the
        /// captain's own legs earn: he stops, he says one more thing into the radio, and he goes back to the
        /// round from wherever he has ended up — with the cooldown running, so the floor does not simply start
        /// again on the next frame. Nothing is removed from the list; a despawn would be the building rubbing out
        /// a man who is standing in a corridor you can walk back down.</summary>
        private void HeLosesYou(Guard g)
        {
            EndTheRun(g);
            g.HeStandsAtYou();
            _host.ShowPulseMessage(PatrolBeat.LostYouLine, PulseRank.Beat);
            _host.LogAutopilotEvent(PatrolBeat.LostYouLine);
        }

        /// <summary>#835 · He stops running. One place, so the two ends of a run cannot leave different amounts
        /// of it behind on the man.</summary>
        private static void EndTheRun(Guard g)
        {
            g.HeStopsRunning();
            g.Vx = 0;
            g.Vy = 0;
        }

        /// <summary>
        /// #835 · SOMEBODY SAW YOU DO THAT — the seam a crime comes in through, and the only one.
        ///
        /// <para>ONE crime is wired today and it is wired honestly: the gun (#803's DESIGNATE, the hasp coming
        /// off a door). It is the only thing a captain can do on these floors that this game already calls a
        /// crime — the door-forcing channels are all surface ground, and turning over a room is the floor's
        /// ordinary verb rather than something anybody has ruled on. The day a door-force lands down here it
        /// hangs off this same call and needs nothing else.</para>
        ///
        /// <para><b>SEEN, not heard — and #618 did not change that, it built the OTHER half beside it.</b> A man
        /// who came RUNNING at a bang three corridors away would still be the floor-wide hunt this feature does
        /// not have, and there is still no road from a noise to a radio call: <see cref="TheRoundHearsAShot"/>
        /// walks somebody over to look, and <see cref="PatrolBeat.EarnsIt"/> refuses its provocation at this
        /// method's own gate. This one asks the question <see cref="PatrolBeat.Notices"/> answers — his eye, his
        /// own short reach, over the same walls everything else on this floor sees through — and it is what buys
        /// the run.</para>
        ///
        /// <para>The car's four-second grace is deliberately not asked. That grace exists so that stepping out of
        /// a lift into somebody's face is a beat rather than an instant; a gun going off is not somebody standing
        /// there being looked at, and a man who ignored it because his shift had just started would be the
        /// building refusing to notice its own doors coming apart.</para>
        /// </summary>
        public void SomebodySawThat(PatrolBeat.Provocation why)
        {
            if (!PatrolBeat.EarnsIt(why) || Guards.Count == 0 || _host.Surface is not { Floor: < 0 })
            {
                return;
            }

            // Not while somebody is already walking you out, and not while somebody is already coming: an
            // escalation on top of an escalation is two men doing one job.
            if (Escort is not null || EscortDue is not null || KickOutRideDue)
            {
                return;
            }

            IReadOnlyList<SurfaceCollision.Segment> sight = _host.SightBlockers();
            for (int i = 0; i < Guards.Count; i++)
            {
                if (Guards[i].AfterYou)
                {
                    return;
                }
            }

            for (int i = 0; i < Guards.Count; i++)
            {
                Guard g = Guards[i];
                if (!PatrolBeat.Notices(g.X, g.Y, _host.AvatarX, _host.AvatarY, sight))
                {
                    continue;
                }
                TheRadioCall(g, why, i);
                return;
            }
        }

        // ── #618 · …AND THE ONE THAT COMES IN THROUGH THE EAR ─────────────────────────────────────────────
        //
        // Owner's ruling, 2026-08-05, and the last thread of #618 left hanging when the rest of it landed as
        // #804/#833/#835/#836/#715: "they come if we make a big noise like start to use the special ammo to open
        // a locked door. I guess we can flee but they will follow."
        //
        // FOUR THINGS ABOUT IT, and they are what keep it a beat rather than an alarm:
        //
        //   1. HE DOES NOT KNOW IT WAS YOU. He heard a bang and he is walking to the place it came from. There
        //      is no radio call on this road (PatrolBeat.EarnsIt refuses the provocation at TheRadioCall's own
        //      gate), no run, and no card. If he finds out who fired it, he finds out by SEEING you, on the
        //      identical ladder as every other man on every other floor — TheNoiseTurnsIntoAPerson hands him
        //      straight to #833's hail and nothing about the approach after that is any different.
        //   2. NOTHING IS SAID. No banner, no "SECURITY ALERTED", not one line of pulse or log on either end of
        //      it. The guard simply comes; that is #603's inference horror and it is the whole of the register.
        //      The one sentence a captain ever gets about the noise is the one they already got, from their own
        //      gun, the first time they fired it indoors (GunfireHeard.WhatItCostLine — "Nothing has come yet.
        //      That is not the same as nothing having heard it.") — and this lane is what finally makes it true.
        //   3. NOBODY HEARS IT AND NOTHING HAPPENS. An empty floor, the FOUND band, or a shot out past the
        //      range: no walk, and no heat. Not a special case — there is nobody on the rota to be the register.
        //   4. THE RANGE IS THE ONE THAT WAS ALREADY THERE. GunfireHeard.EarshotDu is
        //      ReeverHearing.RangeOf(Noise.Gunfire), and its WithinEarshot has carried a doc comment since #803
        //      saying it is "the question #804 will ask of every patrol on the floor". This is that question,
        //      asked. No second acoustics, and no wall term: the ear on this ground has never had one.

        /// <summary>
        /// #618 · <b>A GUN GOES OFF, AND SOMEBODY ON THE ROTA HEARD IT.</b>
        ///
        /// <para>Asked once a frame off the excursion's own append-only ledger — the client does not publish
        /// anything new and <c>Map.Combat.Remote.cs</c> did not gain a line: the shot was already filed there
        /// (<c>GunfireHeard.File</c>) the moment the trigger was pressed, and the round picks it up on the next
        /// frame the way it picks up everything else.</para>
        ///
        /// <para><b>The cursor moves first, and unconditionally.</b> Every road out of this method has already
        /// spent the shot, so a bang is answered at most once whatever the floor happens to be doing — which is
        /// the property the heat guard is about, and it is arithmetic here rather than a clause somewhere
        /// downstream.</para>
        ///
        /// <para><b>Hearing it and going to look at it are two different questions.</b> The outfit's memory is
        /// owed the moment one of their men hears a gun on their floor, whether or not he is free to walk over —
        /// a man who is at that second walking a captain to the car heard it just as well. So the crossing is
        /// banked on the ear, and the errand is handed out only if there is somebody to hand it to.</para>
        /// </summary>
        private void TheRoundHearsAShot(SurfaceExcursion ex, ContactLedger book, double simTime)
        {
            if (GunfireHeard.SinceLastHeard(ex.ShotsHeard, ShotsAnswered) is not { } shot)
            {
                return;
            }
            ShotsAnswered = GunfireHeard.Count(ex.ShotsHeard);

            var ears = new List<(double X, double Y)>(Guards.Count);
            foreach (Guard man in Guards)
            {
                ears.Add((man.X, man.Y));
            }

            int who = GunfireHeard.NearestEar(shot, ears);
            if (who < 0)
            {
                return;   // nobody was close enough. No register covers it, so nothing is owed and nobody comes.
            }

            // …AND IT IS OWED TO WHOEVER RUNS THIS GROUND. Through IllegalHeat.Bank like every other crossing in
            // the game, off the body the captain is standing on, so the operator is looked up in the one place
            // that knows it (#715) and never re-derived here.
            IllegalHeat.Bank(
                book, IllegalHeat.Charge(ex.Stop.Body.Id, IllegalHeat.Crossing.ShotOnTheirFloor), simTime);
            _host.RequestVaultSave();

            // Not while somebody is already walking you out, already coming, or already crossing the floor to
            // you: two men doing one job is #777's stacked card with legs, and a man who is being hailed does
            // not need the round to also be investigating him.
            if (Escort is not null || EscortDue is not null || KickOutRideDue || LookingIntoIt is not null)
            {
                return;
            }
            foreach (Guard man in Guards)
            {
                if (man.AfterYou || man.WalkingUp)
                {
                    return;
                }
            }

            Guard g = Guards[who];
            LookingIntoIt = g;
            TheNoise = (shot.X, shot.Y);

            // #833's own transition, unchanged and unwrapped: a walk to a bang is a walk-up with a place at the
            // end of it. There is no new posture on the man and nothing new for the pen to draw — the round is
            // suspended and resumes from wherever the walk leaves him, which is what a detour is.
            g.HeStartsWalkingUp();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(shot.Y - g.Y, shot.X - g.X);
        }

        /// <summary>
        /// #618 · <b>HE COMES ROUND THE CORNER AND THERE YOU ARE.</b> The one road from a noise to a person, and
        /// it is <see cref="PatrolBeat.Notices"/> — his own eye, his own short reach, the identical predicate the
        /// hail has always been gated on, including the grace off the car.
        ///
        /// <para>What he does about it is <see cref="TheHail"/> and nothing else: he says the one short line and
        /// keeps walking, and from that frame he is crossing the floor to a captain rather than to a door. The
        /// whole of #833's ladder is then in front of the player exactly as it would have been if he had simply
        /// looked up — which is the point. A captain who fires a gun and then stands in the corridor is caught by
        /// the same rule as a captain who stands in a corridor.</para>
        ///
        /// <para>It also collects the errand when the walk-up ends any other way. Every road out of a walk-up
        /// (the card, the give-up, the door, the radio) leaves the man not walking up, and a reference kept past
        /// that would be an errand pointing at somebody who is signing a watchclock station.</para>
        /// </summary>
        private void TheNoiseTurnsIntoAPerson(IReadOnlyList<SurfaceCollision.Segment> sight)
        {
            if (LookingIntoIt is not { } g)
            {
                return;
            }

            if (!g.WalkingUp)
            {
                LookingIntoIt = null;   // whatever ended his walk-up ended the errand with it
                return;
            }

            if (!PatrolBeat.CanBeNoticed(FloorSeconds)
                || !PatrolBeat.Notices(g.X, g.Y, _host.AvatarX, _host.AvatarY, sight))
            {
                return;
            }

            LookingIntoIt = null;
            TheHail(g);
        }

        /// <summary>#618 · <b>IT WAS NOTHING, AND HE SAYS NOTHING ABOUT IT.</b> The three ends of a walk to a
        /// bang — he got there, the clock ran out, or the floor would not give him a route — and all three leave
        /// the same man behind: back on his round from where he stands, with the cooldown running.
        ///
        /// <para>Deliberately NOT <see cref="GiveUpTheHail"/>, and the difference is one sentence. That method
        /// pulses <see cref="PatrolBeat.WalkedAwayLine"/> — a man watching somebody go down a corridor — which is
        /// a description of a thing that did not happen here, and the walk-away counter it can spend belongs to
        /// hails the captain walked off. Nobody walked off anything. He looked, and there was a door with a hole
        /// in it and nobody standing by it, and men on rotas do not narrate that.</para></summary>
        private void TheNoiseWasNothing(Guard g)
        {
            LookingIntoIt = null;
            g.HeGivesUp();
            g.Vx = 0;
            g.Vy = 0;
        }

        // ── #835 · THE TOP RUNG: BACK TO THE SKY ──────────────────────────────────────────────────────────

        /// <summary>
        /// #835 · THE KICK-OUT. The escort has reached the car and he gets in with you.
        ///
        /// <para><b>No new machinery, one longer walk.</b> The ride is <c>_host.RideTheLiftTo(ex, 0)</c> — the ONE
        /// transition this game has ever had between a floor and the regolith, the same one the panel's SURFACE
        /// row presses — so the captain comes out of the cage inside the shed, a pace in from its door, through
        /// the one net every placement in the excursion goes through (#681). There is no
        /// <c>StandCaptainAt</c> on this road: the walk to the car was walked (#833) and the ride is a ride.</para>
        ///
        /// <para><b>The pass goes first, and it is SAID.</b> A possession that leaves the satchel in silence is
        /// the sim doing something the prose never mentioned — the bug class this feature has paid for twice. It
        /// is only said when there was one to take: a captain who never had a pass is thrown out of a site he was
        /// never on the books of, and nothing about that needs a sentence.</para>
        ///
        /// <para><b>And the way back in is left exactly where the building already keeps it.</b> The shaft's own
        /// gate reads the wallet (#752), so a captain with nothing in it is refused in words by machinery that
        /// was already there. Nothing here has to invent a re-entry rule, and #836's wallet of names can grow one
        /// later without this method changing.</para>
        /// </summary>
        private void TheKickOut(SurfaceExcursion ex)
        {
            string bodyId = ex.Stop.Body.Id;
            bool hadOne = PatrolBeat.BadgeHeld(bodyId, _host.Satchel);
            if (hadOne)
            {
                _host.Satchel = [.. Satchel.Remove(_host.Satchel, Satchel.Kind.Badge, PatrolBeat.BadgeId(bodyId))];
            }

            // The plate is armed BEFORE the ride, because the ride rebuilds the deck the plate is painted on.
            KickedOutPlateFor = PatrolBeat.KickedOutPlateSeconds;
            _host.RideTheLiftTo(ex, 0);

            // ONE REGION, NOT TWO PULSES — #774's law, and this moment is exactly what it is for. The ejection
            // has two things to say in one breath (the pass, and the doors) and the slot holds one line: said as
            // two calls they are two writes to it and the captain reads only the second, which would have made
            // "the removal is spoken" a sentence that was technically emitted and never seen. The quiet line goes
            // LAST because it is the closer, and it is the owner's copy verbatim.
            var said = new List<string>();
            if (hadOne)
            {
                said.Add(PatrolBeat.PassRevokedLine);
                _host.FileNote(PatrolBeat.PassRevokedNote, PatrolBeat.BadgeGlyph);
            }
            said.Add(PatrolBeat.DoorsCloseLine);

            string closing = string.Join("\n\n", said);
            _host.ShowPulseMessage(closing, PulseRank.Beat);
            _host.LogAutopilotEvent(closing);
            _host.FileNote(PatrolBeat.KickOutNote, "👮");
            _host.RequestVaultSave();
        }

        /// <summary>
        /// #835 · THE BIG TEXT, as the tube doors part — and it is the DESCENT PLATE, not a new instrument.
        ///
        /// <para>The stack is the one <c>HiveInterior</c> paints over every car mouth in the building, in the
        /// same three sizes and the same stencil ink: the big line, the floor's name, and whether you can breathe
        /// on it. The bottom two are read off the same two functions every other plate in the game reads them off
        /// (<c>UndergroundComplex.DepthPaint</c> and <c>SuitAir.PlateLine</c>), so the sign over the shed and the
        /// gauge on the suit are physically incapable of disagreeing — and what they say is exactly what the
        /// owner's copy says they say: SURFACE, and a tank running.</para>
        ///
        /// <para>It hangs over the shed's roof, off the hut's own envelope rather than off a number typed here,
        /// and it comes down after <see cref="PatrolBeat.KickedOutPlateSeconds"/> because a sign that stayed
        /// would be #694's facility name on all thirteen floors: a thing you stop reading.</para>
        /// </summary>
        public (float X, float Y, string Text, float Px, int Tone)[]? TheKickedOutPlate(SurfaceExcursion ex)
        {
            if (KickedOutPlateFor <= 0)
            {
                return null;
            }

            MoonSurface.LiftHeadBox shed = MoonSurface.LiftHead(
                ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
            double x = shed.CentreX, top = shed.CentreY + shed.HalfH;

            SuitAir.Supply air = SuitAir.SourceOf(ex.Stop.Body.Id, 0, insideShelter: false, aboard: false);
            return
            [
                ((float)x, (float)(top + 8.6), PatrolBeat.KickedOutBigText, 44f, 0),
                ((float)x, (float)(top + 5.8), UndergroundComplex.DepthPaint(0), 19f, 0),
                ((float)x, (float)(top + 3.4), SuitAir.PlateLine(air), 17f, SuitAir.Drawing(air) ? 2 : 1),
            ];
        }

        /// <summary>#835 · The plate's own clock, ticked where nothing else in this file runs — the surface. When
        /// it runs out ONE rebuild takes the sign down; the guard clause above it is what keeps that rebuild from
        /// being a per-frame cost on every excursion this game has.</summary>
        private void FadeTheKickedOutPlate(double dtRealSeconds)
        {
            if (KickedOutPlateFor <= 0)
            {
                return;
            }

            KickedOutPlateFor -= System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
            if (KickedOutPlateFor <= 0)
            {
                KickedOutPlateFor = 0;
                _host.RebuildSurfaceDeck();
            }
        }

        // ── DRAWING THEM, AND HEARING THEM ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fill the guards into the droid buffer — and ONLY the ones the captain can actually see. An unseen
        /// round is parked off-map at the same coordinates an empty slot uses, the #371 idiom the Old Ones
        /// already take, so a guard behind a wall is not on the deck at all rather than drawn dim.
        /// </summary>
        public void FillPatrolDroids(DeckPlan.Droid[] buffer, int firstSlot)
        {
            // …and never anywhere but a floor of the Hive. The list is cleared on the way up, but the buffer is
            // shared with the ship, the havens and the derelicts, and a filler that trusted a list to have been
            // emptied is one lifted shuttle away from drawing a contract guard on the bridge.
            bool underground = _host.Surface is { Floor: < 0 };

            for (int i = 0; i < PatrolBand; i++)
            {
                int slot = firstSlot + i;
                if (slot >= buffer.Length)
                {
                    return;
                }

                // #793 · …and whether they are HELD rides along, off the same one answer the step wrote. A
                // figure that has stopped because you sat down is drawn stopped (#795's warm seated ink), and
                // the fact goes down from the sim rather than being worked out by the pen.
                // #832 · …as does whether this is a figure or a MARKER. Out at the far end of the eye's reach
                // the pen gets a silhouette to draw and no round number to write over it — the sim decides which
                // rung (PatrolBeat.SightingFor), the renderer only draws what it is handed.
                buffer[slot] = underground && i < Guards.Count
                               && Guards[i].Seen != PatrolBeat.Sighting.None
                    ? new DeckPlan.Droid(
                        Guards[i].X, Guards[i].Y, Guards[i].Facing, Guards[i].DeckName, Guards[i].Held,
                        Guards[i].Seen == PatrolBeat.Sighting.Smear)
                    : new DeckPlan.Droid(-9999, -9999, 0, PatrolBeat.DeckName(i));
            }
        }
    }
}
