using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #251 · Split from Patrol.Run.cs, moved verbatim (the header note lives in Map.Patrol.cs): #618's shot
// within earshot and #602's keypad calling its own security, the kick-out back to the sky and its plate,
// and drawing and hearing the figures. The round's state stays in Patrol.cs.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
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
        /// #602 · <b>THE PAD CALLED SECURITY, AND SOMEBODY ON THE ROTA IS SENT TO IT.</b>
        ///
        /// <para>Owner's ruling: three wrong entries inside the window and <i>"the security patrol comes"</i>.
        /// This is the whole of the summons, and the load-bearing fact about it is <b>how little of it is
        /// new</b>. There is no new kind of security, no sweep team, no alert state, no floor-wide hunt and
        /// no <see cref="PatrolBeat.Provocation"/> member — #618 still owes the owner the ruling on what a
        /// second security body would even be, and this lane leaves that owing rather than spending it.</para>
        ///
        /// <para><b>It is #618's own walk to a place, pointed at a keypad</b> — which is why it lives in this
        /// file, beside <see cref="TheRoundHearsAShot"/> and inside the round the source-shape guards
        /// concatenate. A partial of its own would have been a piece of the patrol sitting outside every law
        /// those guards hold over it.</para>
        ///
        /// <para> A man leaves his round, crosses
        /// the floor to the spot, and what happens when he gets there is decided by the identical rule that
        /// decides it for a gunshot: <see cref="TheNoiseTurnsIntoAPerson"/> asks his own eye
        /// (<see cref="PatrolBeat.Notices"/>), and a captain still standing at the pad is hailed and read
        /// exactly as a captain standing anywhere else is — the GENERAL HANDS challenge (#804/#833/#836),
        /// a pass that works and a pass that does not, and the challenge's own outcomes. A captain who
        /// walked away gets <see cref="TheNoiseWasNothing"/>: a man looks at a keypad, and men on rotas do
        /// not narrate that.</para>
        ///
        /// <para><b>NO EARSHOT AND NO EYE ON THE WAY IN,</b> and that is the one thing that differs from a
        /// bang. A gunshot is heard, so the nearest EAR answers and only if it is close enough
        /// (<c>GunfireHeard.NearestEar</c>). A pad does not make a noise — it makes a CALL, on the
        /// building's own wiring, to whoever is on the floor. So the nearest man answers it whatever the
        /// distance, because he was told rather than because he heard. If nobody is on the rota down here
        /// nobody comes, and nothing anywhere says so: that is #618's rule three, unchanged, and it is the
        /// register (§13.8) — no line explains the patrol, on either end of it.</para>
        ///
        /// <para><b>NOTHING IS SAID AND NOTHING IS BANKED.</b> No pulse, no banner, no heat crossing. The
        /// captain has already been told, once, by the sticker on the wall before the first press, and the
        /// pad has already said <c>SECURITY CALLED</c>. A second sentence here would be the building
        /// explaining its own consequence to the person it is happening to.</para>
        /// </summary>
        /// <param name="x">Where the pad is — the console the captain is standing at.</param>
        /// <param name="y">The same.</param>
        /// <returns>Whether anybody was actually sent. False is an empty rota or a floor already busy with
        /// one of these, and the caller does not narrate either.</returns>
        public bool SecurityWasCalledTo(double x, double y)
        {
            if (Guards.Count == 0 || _host.Surface is not { Floor: < 0 })
            {
                return false;
            }

            // Not while somebody is already walking you out, already coming, or already crossing the floor:
            // two men doing one job is #777's stacked card with legs, and it is the identical gate
            // TheRoundHearsAShot keeps.
            if (Escort is not null || EscortDue is not null || KickOutRideDue || LookingIntoIt is not null)
            {
                return false;
            }
            foreach (Guard man in Guards)
            {
                if (man.AfterYou || man.WalkingUp)
                {
                    return false;
                }
            }

            int who = -1;
            double nearest = double.MaxValue;
            for (int i = 0; i < Guards.Count; i++)
            {
                double dx = Guards[i].X - x;
                double dy = Guards[i].Y - y;
                double d2 = (dx * dx) + (dy * dy);
                if (d2 < nearest)
                {
                    nearest = d2;
                    who = i;
                }
            }

            Guard g = Guards[who];
            LookingIntoIt = g;
            TheNoise = (x, y);

            // #833's own transition, unchanged and unwrapped — the same one a bang gets. The round is
            // suspended and resumes from wherever the walk leaves him, which is what a detour is.
            g.HeStartsWalkingUp();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(y - g.Y, x - g.X);
            return true;
        }

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
            // #605 · EVERY pass of this site, and that is a correction rather than a widening. The test was
            // BadgeHeld — any tier — and the removal named ONE id, so a captain walked out carrying a found
            // department pass would have been told, in PassRevokedLine's own words, that the paper went into
            // a man's breast pocket, while the sim left it in his wallet. The sentence-vs-sim bug class, in
            // the one feature whose whole register is procedure. One question now, asked and answered by
            // PatrolBeat: what this building issued, it takes back.
            bool hadOne = PatrolBeat.BadgeHeld(bodyId, _host.Satchel);
            if (hadOne)
            {
                _host.Satchel = [.. PatrolBeat.TakeTheSitePasses(_host.Satchel, bodyId)];
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
