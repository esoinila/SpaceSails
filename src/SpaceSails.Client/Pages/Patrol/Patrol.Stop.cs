using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #746's guard stop as
// an ENCOUNTER: the scene the card is carrying, which moves are on offer, the dice a rolled one casts, and
// the one place a move becomes an answer and then reaches an existing seam.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        // ── #746 · THE STOP IS AN ENCOUNTER ───────────────────────────────────────────────────────────────
        //
        // Owner, 2026-08-06: "guard stops (show ID, explain yourself) should use the same choose-your-action
        // mechanic." #748 built the machine and said, in the file's own words, that the guard stop is not a
        // second system but an encounter whose setting walked up to you. This is that claim cashed.
        //
        // WHAT THIS FILE DOES AND DOES NOT DO. It opens no card of its own, invents no sentence, decides no
        // band and owns no punishment. Which moves exist and what each band answers is GuardStop; what a man
        // makes of a paper is PatrolBeat.TheGuardReads exactly as it has been since #804; what a refusal
        // costs is the escort, the pip and the line in a book, reached through the very fields #833 and #715
        // already arm. The one card is the one the round has always raised (#684's ruling, #736's amber row):
        // the moves are drawn INSIDE it, and pressing one replaces the row they sat in.

        /// <summary>#746 · The stop standing in front of the captain, or null. One at a time by
        /// construction — a card is up for the whole of it, and <c>StopTheRoundIfAnybodySeesYou</c> has
        /// refused to hail anybody while a card is up since #777.</summary>
        public sealed class Stop
        {
            /// <summary>The man. Held by reference like the escort's own guard, and for the same reason: he
            /// is the one who walks you out if this goes badly, and a copy would be a second man.</summary>
            public required Guard Man { get; init; }

            /// <summary>The scene, straight off the content file — counterpart, setting, opening, moves.</summary>
            public required Encounter.Scene Scene { get; init; }
        }

        /// <inheritdoc cref="Stop"/>
        public Stop? StopUnderway { get; set; }

        /// <summary>#746 · Which men have already been asked the way this watch, by their own plate — which
        /// carries the site, the floor, the watch and which man he is, so this is <i>once per guard per
        /// watch</i> without a second key being invented for it. Cleared at the turn of the watch beside the
        /// two counters that turn over with it.</summary>
        public HashSet<string> AskedTheWay { get; } = [];

        /// <summary>#746 · …and which men have watched an ask of yours come apart. It is the −1 in
        /// <see cref="Encounter.Modifiers"/>'s own stack (<c>you already asked once</c>), scoped to the man
        /// rather than to the floor, because the modifier is a fact about a face.</summary>
        public HashSet<string> FumbledAtTheStop { get; } = [];

        /// <summary>#746 QA · <c>?roll=hi|lo</c>, for the round. It is a field of the ROUND rather than a
        /// twenty-second member of <see cref="IPatrolHost"/> — that interface may only shrink — and it is
        /// set beside <see cref="RoundsCheat"/> and <see cref="BadgeCheat"/> from the same cheat parse the
        /// table's own reads. It overrides the BAND and never the roll (<see cref="Encounter.Settle"/>).</summary>
        public Encounter.Band? RollCheat { get; set; }

        /// <summary>#746 · Whether the walk that is armed is the HELPFUL one. The escort machinery is a man
        /// walking you to the car; #804's version of it is a refusal, and this lane is the first thing in the
        /// game to want the same legs with none of the bill. False on every road that existed before this
        /// lane, so a refused read costs exactly what it has always cost.</summary>
        public bool EscortIsFree { get; set; }

        /// <summary>#746 · A YES-BUT has been answered and one rung of #715's heat is owed. Armed rather than
        /// banked, the same way <see cref="EscortDue"/> and <see cref="KickOutRideDue"/> are and for the same
        /// reason: the ledger and the clock belong to the frame loop, and a move is pressed on a UI event.
        /// Nothing is ever said about it.</summary>
        public bool NameInTheBookDue { get; set; }

        // ── WHAT THE PANEL ASKS ───────────────────────────────────────────────────────────────────────────
        //
        // Four one-liners so the markup never holds a SurfaceExcursion or a Stop in a local — the seat's own
        // discipline one family along: the markup asks questions, it does not compute answers.

        /// <summary>Is a stop waiting on a move right now? What the card's move row is drawn on.</summary>
        public bool TheStopIsWaitingOnAMove => StopUnderway is not null;

        /// <summary>The moves on the table, in the scene's own order — through <see cref="Encounter.OnTheTable"/>,
        /// so the checkpoint and the canteen cannot end up with two different ideas of when a move exists.</summary>
        public IReadOnlyList<Encounter.Move> TheStopsMoves() =>
            StopUnderway is { } stop ? Encounter.OnTheTable(stop.Scene, []) : [];

        /// <summary>Is this move on offer? Core's own answer (<see cref="GuardStop.OnOffer"/>).</summary>
        public bool TheStopMoveOnOffer(Encounter.Move move) =>
            StopUnderway is { } stop && _host.Surface is { } ex
            && GuardStop.OnOffer(
                move.Id, ex.Stop.Body.Id, ex.Floor, _host.Satchel, AskedTheWay.Contains(stop.Man.Plate));

        /// <summary>Why not, said out loud on the disabled control (#603).</summary>
        public string TheStopMoveRefusal(Encounter.Move move) => GuardStop.WhyNot(move.Id);

        // ── MAKING A MOVE ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #746 · ONE MOVE AT A CHECKPOINT. Every branch ends at <see cref="TheStopIsAnswered"/>, which is
        /// the one place an outcome becomes words and reaches a seam.
        /// </summary>
        /// <param name="moveId">Which move was pressed.</param>
        /// <param name="nerveMarked">Whether the captain's hands read across a corridor — the ANSWER, handed
        /// down by the page rather than the gauge itself. <see cref="IPatrolHost"/> may only shrink, and the
        /// technique that keeps it short is asking for answers: the page reads its own nerve through
        /// <see cref="Encounter.NerveReadsAcrossATable"/>, which is the same rungs the readout draws, so the
        /// −1 and the gauge the player is looking at can never disagree.</param>
        /// <param name="byClosing">Whether this is the ✕ rather than a button. Only
        /// <see cref="GuardStop.Nothing"/> ever arrives that way — closing IS the leave move, the idiom #615's
        /// find card already keeps — and it is the one road where the card is already gone, so the answer is
        /// pulsed instead of drawn. That is the table scene's own rule for its one pulse: #680 is about which
        /// surface the player is looking at, and by this point there is no dialog subtree left to say it in.</param>
        public void TheStopMove(string moveId, bool nerveMarked, bool byClosing = false)
        {
            if (StopUnderway is not { } stop || _host.Surface is not { } ex)
            {
                return;
            }

            Encounter.Move move = default;
            bool found = false;
            foreach (Encounter.Move m in stop.Scene.Moves)
            {
                if (string.Equals(m.Id, moveId, System.StringComparison.Ordinal))
                {
                    (move, found) = (m, true);
                    break;
                }
            }
            if (!found || (!byClosing && !TheStopMoveOnOffer(move)))
            {
                return;
            }

            string bodyId = ex.Stop.Body.Id;
            StopUnderway = null;

            switch (moveId)
            {
                case GuardStop.Show:
                    TheWalletIsRead(ex, stop, byClosing);
                    return;

                case GuardStop.Nothing:
                {
                    // The ladder's own empty-hand rung, asked of the SIM with nothing in it rather than
                    // composed here — so the sentence a captain who says nothing reads is the sentence a
                    // captain with an empty wallet has read since #804, to the byte. And it is FILED like
                    // one, in the order the shipped read files it: the card, the captain's own paper trail,
                    // and only then what it cost.
                    GuardStop.Answer said = GuardStop.NothingIsSaid(PatrolBeat.TheGuardReads(
                        bodyId, ex.Floor, ex.CanteenWatch, stop.Man.Plate, null, ex.InspectionRunning));
                    TheStopSaysIt(said.Read, byClosing);
                    FileTheNameYouGave(ex, null, WalletChoice.Outcome.NothingShown);
                    TheStopEndsWith(ex, stop, said);
                    return;
                }

                case GuardStop.Mess:
                {
                    // Once per man per watch, marked BEFORE the dice: the ask has been made whichever way it
                    // goes, and a band deciding whether a question was asked would be the sim forgetting a
                    // conversation it just had.
                    AskedTheWay.Add(stop.Man.Plate);
                    GuardStop.Answer asked = GuardStop.TheWayToTheMess(
                        TheStopRolls(ex, stop, moveId, nerveMarked), stop.Man.Plate);
                    TheStopSaysIt(asked.Read, byClosing);
                    TheStopEndsWith(ex, stop, asked);
                    return;
                }

                default:
                {
                    GuardStop.Answer talked = GuardStop.TheWork(
                        TheStopRolls(ex, stop, moveId, nerveMarked), stop.Man.Plate);
                    TheStopSaysIt(talked.Read, byClosing);
                    TheStopEndsWith(ex, stop, talked);
                    return;
                }
            }
        }

        /// <summary>
        /// #746 · THE DICE, at a checkpoint. <see cref="Encounter.Roll"/>'s own seed — (site, floor,
        /// counterpart, move, attempt) — so pressing the same button twice is the same answer and a test
        /// walks attempt indices rather than mocking a die.
        ///
        /// <para>The ATTEMPT is whether this man has already watched an ask of yours come apart, which is the
        /// same fact the −1 is read off. One source, two uses: a second ask to a man you fumbled in front of
        /// is a different roll AND a worse one.</para>
        ///
        /// <para>Three of the five modifiers apply and the stack is built in Core
        /// (<see cref="GuardStop.SituationAt"/>) — a corridor is not a canteen, and the two that could never
        /// be true here are left out rather than passed as false into a receipt.</para>
        /// </summary>
        private Encounter.Band TheStopRolls(
            SurfaceExcursion ex, Stop stop, string moveId, bool nerveMarked)
        {
            bool fumbled = FumbledAtTheStop.Contains(stop.Man.Plate);
            Encounter.Situation situation = GuardStop.SituationAt(
                GuardStop.ARelevantPaper(ex.Stop.Body.Id, ex.Floor, _host.Satchel),
                nerveMarked,
                fumbled);

            DiceRoll roll = Encounter.Roll(
                ex.Stop.Body.Id, ex.Floor, stop.Man.Plate, moveId, fumbled ? 1 : 0, situation);
            _host.LogAutopilotEvent($"🎲 {roll.Describe()}");
            return Encounter.Settle(roll, RollCheat);
        }

        /// <summary>
        /// #746 · <b>MOVE 1 — and the shipped read is the whole of the verdict.</b>
        ///
        /// <para>Everything that used to happen the instant a man arrived happens here instead, in the same
        /// order and with the same calls: the paper that is actually in the hand (#836), the one ladder
        /// (<see cref="WalletChoice.WhatHappens"/>), the card the read is told on, the inspection flag and
        /// the line the captain's own book keeps. What moved is WHEN, and the 2026-08-08 ruling survives it
        /// the way it survived #836: there is still no TRY verb, and the read is still as automatic as it
        /// ever was — the captain chose to put the paper in his hand, and he reads what he is given.</para>
        /// </summary>
        private void TheWalletIsRead(SurfaceExcursion ex, Stop stop, bool byClosing)
        {
            string bodyId = ex.Stop.Body.Id;
            Satchel.Item? handed = ThePaperHandedOver(bodyId);

            WalletChoice.Outcome how = WalletChoice.WhatHappens(bodyId, ex.Floor, ex.CanteenWatch, handed);
            PatrolBeat.Read read = PatrolBeat.TheGuardReads(
                bodyId, ex.Floor, ex.CanteenWatch, stop.Man.Plate, handed, ex.InspectionRunning);

            // #1149 · THE INSPECTION IS ON, from this read until the shuttle lifts — set BEFORE the card goes
            // up, and that ordering is load-bearing in one direction only: the sentence was composed off the
            // flag's OLD value, so the authored line is said exactly once and the gates open from here.
            if (how == WalletChoice.Outcome.Inspection)
            {
                ex.InspectionRunning = true;
            }

            // The same three beats in the same order the shipped read has always run them in: the card, the
            // captain's own paper trail, and only then what it cost.
            GuardStop.Answer answer = GuardStop.ThePaperGoesIntoHisHand(read);
            TheStopSaysIt(answer.Read, byClosing);
            FileTheNameYouGave(ex, handed, how);
            TheStopEndsWith(ex, stop, answer);
        }

        /// <summary>
        /// #746 · <b>THE ONE PLACE A MOVE REACHES A SEAM.</b>
        ///
        /// <para>The card is already up (<see cref="TheStopSaysIt"/>) and the book is already written; this
        /// is what HAPPENS.</para>
        ///
        /// <para>Three endings and not one of them is new machinery. He walks on (nothing). He walks you out
        /// — the pip, the escort note, the maintenance break and <see cref="EscortDue"/>, which is what a
        /// refused read has armed since #804 and where #715's crossing is banked one file along. Or he walks
        /// you to the halls, which is the same legs with <see cref="EscortIsFree"/> set, so the floor loop
        /// does the walk and skips the bill.</para>
        ///
        /// <para>The YES-BUT's rung is ARMED rather than banked, beside the escort and for its reason: the
        /// ledger and the clock are the frame loop's, and nothing is said about it either way.</para>
        /// </summary>
        private void TheStopEndsWith(SurfaceExcursion ex, Stop stop, GuardStop.Answer answer)
        {
            if (answer.NameGoesInTheBook)
            {
                NameInTheBookDue = true;
            }

            switch (answer.How)
            {
                case GuardStop.Ending.HeWalksOn:
                    _host.RequestVaultSave();
                    return;

                case GuardStop.Ending.HeWalksYouToTheHalls:
                    EscortIsFree = true;
                    EscortDue = stop.Man;
                    _host.RequestVaultSave();
                    return;

                default:
                    // …and this is #804's own road, unchanged and in its own order: the pip and the note are
                    // what a refusal has always cost, the radio call goes after them because this lane may
                    // not quietly reprice either, and the walk is ARMED because the card saying so is
                    // standing in front of the captain at this exact moment.
                    FumbledAtTheStop.Add(stop.Man.Plate);
                    _host.ApplyNerveShock(
                        NervePips.SightingPips * NervePips.PipUnit, "you were asked and could not answer");
                    _host.FileNote(PatrolBeat.EscortNote, "👮");
                    TheCarIsStoppedForMaintenance(ex);
                    EscortIsFree = false;
                    EscortDue = stop.Man;
                    _host.RequestVaultSave();
                    return;
            }
        }

        /// <summary>
        /// #746 · The answer, said where the captain is looking.
        ///
        /// <para>Pressed from a button, the card is still up and the answer REPLACES the move row inside it —
        /// #680's law, which this scene's sibling learned the expensive way: a conversation rendered behind
        /// frosted glass is a conversation nobody read. Closing IS the leave move, and by then there is no
        /// dialog subtree left, so that one road pulses — which is exactly the exception the table scene
        /// already carves for its own goodbye.</para>
        /// </summary>
        private void TheStopSaysIt(PatrolBeat.Read read, bool byClosing)
        {
            if (byClosing)
            {
                _host.ShowPulseMessage(read.Told, PulseRank.Beat);
                _host.LogAutopilotEvent($"{read.Label} — {read.Told}");
                return;
            }

            _host.ViewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_host.AvatarX, (float)_host.AvatarY,
                read.Label, PatrolBeat.ChallengeArtUrl, read.Card, read.Told);
            RendererInterop.PlayCue("reveal");
            _host.LogAutopilotEvent($"{read.Label} — {read.Told}");
        }

    }
}
