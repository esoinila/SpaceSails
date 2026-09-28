using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #251 · Split from Patrol.Challenge.cs, moved verbatim (the header note lives in Map.Patrol.cs): #836's
// wallet fanned during the approach, the round stopping at you, what he finds and the name you gave.
// The round's state stays in Patrol.cs.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
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
        /// He stops in front of you, and the scene opens.
        ///
        /// <para><b>#746 · IT IS AN ENCOUNTER NOW, AND THAT IS THE WHOLE OF WHAT THIS METHOD LOST.</b> It
        /// used to read the wallet on the very frame he arrived and tell you the answer in one breath, which
        /// was #684's ruling honoured to the letter and one move short of the machine #748 built for exactly
        /// this day. What it raises instead is the same card — same label, same painting, same body — with
        /// the scene's own opening in the amber row and its four moves under it
        /// (<see cref="Patrol.Stop"/>). <b>The read is still automatic and there is still no TRY verb</b>:
        /// SHOW THE PASS hands over the paper #836's fan already put in your hand, and the ladder that judges
        /// it is the shipped one, called once, in <see cref="TheWalletIsRead"/>.</para>
        ///
        /// <para>Everything that happens to a captain still happens in <see cref="TheStopIsAnswered"/>, and
        /// every road there leads through a seam that was already in the game.</para>
        /// </summary>
        private void TheRoundStopsAtYou(
            SurfaceExcursion ex, Guard g, ContactLedger book, double simTime)
        {
            g.HeStandsAtYou();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(_host.AvatarY - g.Y, _host.AvatarX - g.X);

            // #836 · THE PAPER IS IN YOUR HAND BY NOW. The fan comes down here whether or not the captain ever
            // touched it — his hand is out, and the whole ruling is that there is no swapping in front of him.
            // WHICH paper it is is asked at the move (ThePaperHandedOver), because a pass can be taken off you
            // between the hail and the answer and the hand must hold what the satchel holds.
            WalletFanOpen = false;

            // ── #711 · THE PARCEL IS WHAT IS FOUND, AND IT IS FOUND FIRST ────────────────────────────────
            //
            // Canon (head coder, 2026-08-09): "Layer 1's whole job is to be the floor the search stops at."
            // A man with his hand out for your papers is a man who has already seen the box, and a search
            // that found the parcel AFTER reading the wallet would be a search that did not stop at it.
            //
            // On the fine, this method is OVER: no paper is handed over, no ladder is walked, nothing is
            // filed about a name you never gave. He is already looking at the next hull, which is the only
            // sentence on the card and the literal truth about the sim.
            UnlistedParcel.Found? parcel = null;
            if (UnlistedParcel.Held(_host.Satchel))
            {
                parcel = TheParcelIsWhatHeFinds(ex, g, book, simTime);
                if (!parcel.Value.TheReadGoesOn)
                {
                    return;
                }
            }

            // #746 · THE SCENE OPENS. The counterpart is the man, the setting is the stop, and the opening is
            // his one word — built off the plate, so the scene is CONTENT (GuardStop.SceneFor) and this
            // method has no opinion about what a captain may do at a checkpoint.
            PatrolBeat.Read opening = new(
                false, GuardStop.Opening, PatrolBeat.ChallengeLabel, PatrolBeat.ChallengeCard(g.Plate));

            // #711 · …AND HE KEPT LOOKING. The one arm where a parcel does not end the afternoon: the tell
            // goes on the front of the card the captain was always going to get, and everything under it —
            // the moves, the ladder, the consequence, the pip, the escort — happens exactly as it happens on
            // any other afternoon. Nothing about this stop is softened by it, which is the point of it.
            if (parcel is not null)
            {
                opening = UnlistedParcel.TheReadGoesOnAfterIt(opening);
            }

            // #684's idiom, unchanged one lane along: the stop is TOLD on a card, with the man's own words in
            // the card's amber row (#736) rather than pulsed under a backdrop nobody can see through — and
            // now with the four things you may do about it under them, INSIDE the card's own subtree, which
            // is #680's law and the reason the table scene's panel looks the way it does.
            _host.ViewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_host.AvatarX, (float)_host.AvatarY,
                opening.Label, PatrolBeat.ChallengeArtUrl, opening.Card, opening.Told);
            RendererInterop.PlayCue("reveal");

            _host.LogAutopilotEvent($"{opening.Label} — {opening.Told}");

            StopUnderway = new Stop { Man = g, Scene = GuardStop.SceneFor(g.Plate) };
            _host.RequestVaultSave();
        }

        /// <summary>
        /// #711 slice 1 · <b>HE FINDS THE PARCEL.</b> The whole of what a box aboard costs and buys, in the
        /// one place a man on a rota ever looks at a captain.
        ///
        /// <para><b>The judgement is Core's and only Core's.</b>
        /// <see cref="UnlistedParcel.TheParcelIsWhatIsFound"/> decides whether the fine book comes out,
        /// quotes the fine off <c>BustedRule.BribeDemand</c>, banks the crossing and closes the folder — all
        /// in one call, because the ORDER of those is load-bearing and a client that did them separately
        /// would be the place it came apart. This method does the three things Core cannot: coin out of the
        /// purse, the box out of the pocket, and the card up.</para>
        ///
        /// <para><b>The parcel goes either way.</b> A man who found a box does not hand it back, and the arm
        /// where he writes nothing down is the arm where the captain has lost the box AND has nothing to
        /// show for it. Nothing anywhere explains that (§13.8); the line on the card is the whole of the
        /// telling.</para>
        ///
        /// <para>The card is raised HERE only on the fine, because that is the arm where the read is over.
        /// On the other one the read goes on, and one card carries both halves — two cards on one screen
        /// being the stacked-card mistake #777 named.</para>
        /// </summary>
        private UnlistedParcel.Found TheParcelIsWhatHeFinds(
            SurfaceExcursion ex, Guard g, ContactLedger book, double simTime)
        {
            string bodyId = ex.Stop.Body.Id;

            // Seeded off the SITE and the FROZEN watch, never a live clock and never the frame — the
            // discipline the inspection roster keeps one file along. A fine is a number a man wrote down
            // once; it may not be a different number because the captain read the card a second later.
            UnlistedParcel.Found found = UnlistedParcel.TheParcelIsWhatIsFound(
                book, bodyId,
                DiceRule.Seed($"parcel:fine:{bodyId}", ex.CanteenWatch, ex.Floor),
                simTime, _host.WorldSeed);

            _host.PayTheFine(found.Fine);

            // #711 slice 2 · WHICH BOX IT WAS, read BEFORE the pocket is emptied — the length of the quiet
            // that follows is seeded off the parcel that was lost, and a second later there is no parcel to
            // ask. Both arms: the box is carried off whether or not a fine was written, so the work dries up
            // whether or not a fine was written. Nothing is said about it on either.
            string? carriedOff = ParcelDrop.TheParcelIn(_host.Satchel)?.Id;

            _host.Satchel = [.. UnlistedParcel.Confiscated(_host.Satchel)];

            if (carriedOff is not null)
            {
                _host.TheDeskHasNothingForAWhile(carriedOff);
            }

            _host.FileNote(
                found.Fined ? UnlistedParcel.FineNote : UnlistedParcel.TellNote, UnlistedParcel.Glyph);

            if (found.Fined)
            {
                // #684's idiom, and deliberately the round's OWN card: the same label, the same painting of
                // the same man, because it is the same thirty seconds. A card shape of its own for this beat
                // would be the game flagging that something unusual just happened to a captain who has been
                // told, in as many words, that he is ordinary.
                PatrolBeat.Read told = UnlistedParcel.TheFineIsTold(g.Plate);
                _host.ViewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_host.AvatarX, (float)_host.AvatarY,
                    told.Label, PatrolBeat.ChallengeArtUrl, told.Card, told.Told);
                RendererInterop.PlayCue("reveal");
                _host.LogAutopilotEvent($"{told.Label} — {told.Told}");
            }

            _host.RequestVaultSave();
            return found;
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
        /// <param name="how">#1149 · The outcome the CARD was composed off, handed in rather than asked a
        /// second time. It used to re-ask <see cref="WalletChoice.WhatHappens"/> here, which was harmless
        /// while the ladder was a pure function of the paper and the site — and is not harmless now that one
        /// rung depends on the floor and the watch. One read, one answer, one line in the book.</param>
        private void FileTheNameYouGave(
            SurfaceExcursion ex, Satchel.Item? handed, WalletChoice.Outcome how)
        {
            string bodyId = ex.Stop.Body.Id;
            int level = ex.Floor;
            string name = _host.NameOnYourOwnPapers;

            // #605/#741 · WHAT THE ENTRY IS ABOUT, and it is declared HERE because here is where it is known:
            // the read that just happened, and whether cover survived it. A blow is filed under the PLACE —
            // this building — so the THREADS page stacks every time somebody stopped the captain in it under
            // one heading, in the order it happened, drawing no conclusion between them (#741's law: the book
            // keeps no opinion, the red pen is the captain's).
            //
            // A read that WORKED joins no thread, deliberately. Cover is a state and silence is the reward:
            // a heading that filled up with the evenings nothing happened on would be the game telling the
            // captain their disguise is working, which is the one thing this feature may never say (§13.8).
            string subjects = WalletChoice.CoverBlew(how)
                ? PatrolBeat.BlowSubjects(FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name))
                : "";

            if (handed is { } paper)
            {
                IReadOnlyList<WalletChoice.Shown> filed = WalletChoice.Remember(
                    ShownBook, new WalletChoice.Shown(paper.Id, bodyId, level, how));
                ShownBook.Clear();
                ShownBook.AddRange(filed);

                _host.FileNote(
                    WalletChoice.ShownNote(paper, bodyId, level, how, name),
                    WalletChoice.GlyphOf(paper),
                    subjects);
                return;
            }

            // Nothing was handed over, so there is no paper to remember — but there is still an evening, and it
            // still gets a line. Filed against no id, which is why it never colours a chooser row.
            _host.FileNote(
                WalletChoice.ShownNote(null, bodyId, level, how, name), PatrolBeat.BadgeGlyph, subjects);
        }
    }
}
