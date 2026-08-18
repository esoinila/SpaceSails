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
            SurfaceExcursion ex, Guard g, int index, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
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
                    TheRoundStopsAtYou(ex, g);
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
        private void StopTheRoundIfAnybodySeesYou(IReadOnlyList<SurfaceCollision.Segment> sight)
        {
            // …and never while somebody is already on their way over, or walking you out. An approach is the
            // stop, in progress; a second one behind it would be two men doing one job.
            if (_host.ViewObject is not null || !PatrolBeat.CanBeNoticed(FloorSeconds)
                || Escort is not null || EscortDue is not null)
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

        // ── #836 · THE WALLET, FANNED DURING THE APPROACH ─────────────────────────────────────────────────
        //
        // Owner: "I think I should be able to pick the badge I show the guard... like Fletch ... suppose we have
        // 4 different ID's ... one of them real ... but not authorized for access to all places we roam."
        //
        // WHERE IT LIVES IS THE WHOLE RULING. The 2026-08-08 call — no TRY verb, the read is automatic and told
        // on a card — is untouched, because the choice happens one beat EARLIER: he has said hold on and is
        // crossing the floor, and the fan is up over the walk-up while he does it. By the time he is at arm's
        // length your hand is already in your pocket, and what is in it is what he reads.
        //
        // The controls are never taken for it. A captain may ignore the fan, or walk away from the whole thing
        // (#833/#835) — the default is seeded at the hail precisely so ignoring it is a real option rather than
        // an accident.

        /// <summary>#836 · The papers a guard's palm is for, in the fan's own stable order. One list, read by the
        /// dialog and by the default alike.</summary>
        public IReadOnlyList<Satchel.Item> TheWalletFan =>
            _host.Surface is { } ex ? WalletChoice.Fan(ex.Stop.Body.Id, _host.Satchel) : [];

        /// <summary>#836 · Is the fan actually in front of the captain? Asked by the dialog that draws it AND by
        /// the cancel key that shuts it, so Esc can never swallow a keystroke for a dialog nobody can see — a
        /// wallet that lost a paper between the hail and the frame is a wallet with no choice left in it.</summary>
        public bool WalletFanIsUp => WalletFanOpen && TheWalletFan.Count > 1;

        /// <summary>
        /// #836 · THE HAIL PUTS A PAPER IN YOUR HAND, and — only when there is a choice to make — opens the fan.
        ///
        /// <para>With one paper in the wallet nothing happens that did not happen before this feature existed: no
        /// dialog, no friction, and the same paper goes into the same hand. That is the promise the
        /// <see cref="WalletChoice.Fans"/> gate keeps, and it is asked of Core so the dialog and the read cannot
        /// come to different conclusions about whether the captain had a decision.</para>
        /// </summary>
        private void FanTheWallet()
        {
            if (_host.Surface is not { } ex)
            {
                return;
            }

            string bodyId = ex.Stop.Body.Id;
            PaperInHand = WalletChoice.DefaultFor(bodyId, _host.Satchel, ShownBook);
            WalletFanOpen = WalletChoice.Fans(bodyId, _host.Satchel);
        }

        /// <summary>#836 · ONE CHOICE, and picking is the whole of it. There is no confirm step — the row IS the
        /// decision, the same way the bin's row is (#798) — and the fan comes down with it, because a modal left
        /// standing over the approach would hide the man who is walking at you. Owner's own framing: <i>one
        /// choice, under time pressure, made BEFORE the read.</i></summary>
        public void ChooseThePaper(Satchel.Item paper)
        {
            PaperInHand = paper;
            WalletFanOpen = false;
            RendererInterop.PlayCue("blip");
        }

        /// <summary>
        /// #836 · WHAT ACTUALLY GOES INTO HIS HAND, asked at the read and not before.
        ///
        /// <para>The chosen paper, if it is still in the wallet — a pass can be taken off you between the hail and
        /// the arrival (<see cref="PatrolBeat.PassRevokedLine"/>), and a hand holding a paper the satchel no longer
        /// has would be the sim and the pocket disagreeing. Otherwise the default, which is what a captain who
        /// never opened the fan is holding anyway.</para>
        /// </summary>
        private Satchel.Item? ThePaperHandedOver(string bodyId) =>
            PaperInHand is { } chosen && WalletChoice.StillHeld(_host.Satchel, chosen)
                ? chosen
                : WalletChoice.DefaultFor(bodyId, _host.Satchel, ShownBook);

        /// <summary>
        /// He stops, reads what is in your wallet, and tells you the answer. The judgement is Core's and only
        /// Core's; this raises the card, spends the nerve and — when it comes to that — walks you back.
        /// </summary>
        private void TheRoundStopsAtYou(SurfaceExcursion ex, Guard g)
        {
            g.HeStandsAtYou();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);

            // #836 · THE PAPER, AND THEN THE READ OF IT. The fan comes down here whether or not the captain ever
            // touched it — his hand is out, and the whole ruling is that there is no swapping in front of him.
            string bodyId = ex.Stop.Body.Id;
            WalletFanOpen = false;
            Satchel.Item? handed = ThePaperHandedOver(bodyId);

            PatrolBeat.Read read = PatrolBeat.TheGuardReads(bodyId, g.Plate, handed);

            // #684's idiom, one building along: the read is TOLD on a card, with the outcome in the card's own
            // amber row (#736) rather than pulsed under a backdrop nobody can see through. #804 shipped it
            // caption-only under the house's degradation law and the painting has now dropped in behind it —
            // Core's own constant, the same plate whichever way the wallet reads, because the man in it has not
            // read it yet either.
            _host.ViewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_host.AvatarX, (float)_host.AvatarY,
                read.Label, PatrolBeat.ChallengeArtUrl, read.Card, read.Told);
            RendererInterop.PlayCue("reveal");

            _host.LogAutopilotEvent($"{read.Label} — {read.Told}");

            // #836 · THE ROUND LOG REMEMBERS THE NAME. Owner: "every challenge writes down which identity you
            // showed." This is the captain's own half of that ledger, filed on BOTH arms — and the clean arm is
            // the one that had to change, because a paper that worked here is exactly the thing the next chooser
            // row has to be able to say. It is the escort note's idiom (a fact, never a mechanic), and it is the
            // ONLY thing the hint on a row is ever derived from.
            FileTheNameYouGave(bodyId, ex.Floor, handed);

            // A PASS THAT WORKS COSTS NOTHING. Encounter.NervePipsFor's own arithmetic — the band that lands is
            // free and the two that hurt cost a pip — and it has to be, or the badge is worth nothing: a captain
            // who paid the same either way would have earned a longer sentence and no mechanic.
            if (read.Satisfied)
            {
                _host.RequestVaultSave();
                return;
            }

            _host.ApplyNerveShock(NervePips.SightingPips * NervePips.PipUnit, "you were asked and could not answer");
            _host.FileNote(PatrolBeat.EscortNote, "👮");

            // The mildest honest consequence, and the whole of it: back to the car — WALKED (#833). It is only
            // ARMED here, because the card telling the captain about it is standing in front of him at this exact
            // moment; the walk starts on the first frame after the card comes down, which is the frame he can
            // actually watch it happen on.
            EscortDue = g;
            _host.RequestVaultSave();
        }

        /// <summary>
        /// #836 · THE BOOK KEEPS WHICH NAME YOU GAVE HIM — one line in the field book, and one row in the
        /// captain's own paper trail, composed from the SAME outcome the card was composed from
        /// (<see cref="WalletChoice.WhatHappens"/>).
        ///
        /// <para>That single source is the point. The sentence the captain reads on the amber row and the line
        /// their book keeps about the same thirty seconds are two readings of one fact, so no future edit can make
        /// the book remember a challenge differently from the way it was told — which on this ground is the third
        /// named bug class, in the one system whose entire register is procedure.</para>
        ///
        /// <para>An empty hand is filed too. <i>Nothing came out of the wallet</i> is a thing that happened to
        /// you, and a book that only kept the interesting nights would be a book that flattered its owner.</para>
        /// </summary>
        private void FileTheNameYouGave(string bodyId, int level, Satchel.Item? handed)
        {
            WalletChoice.Outcome how = WalletChoice.WhatHappens(bodyId, handed);
            string name = _host.NameOnYourOwnPapers;

            if (handed is { } paper)
            {
                IReadOnlyList<WalletChoice.Shown> filed = WalletChoice.Remember(
                    ShownBook, new WalletChoice.Shown(paper.Id, bodyId, level, how));
                ShownBook.Clear();
                ShownBook.AddRange(filed);

                _host.FileNote(
                    WalletChoice.ShownNote(paper, bodyId, level, how, name), WalletChoice.GlyphOf(paper));
                return;
            }

            // Nothing was handed over, so there is no paper to remember — but there is still an evening, and it
            // still gets a line. Filed against no id, which is why it never colours a chooser row.
            _host.FileNote(WalletChoice.ShownNote(null, bodyId, level, how, name), PatrolBeat.BadgeGlyph);
        }
    }
}
