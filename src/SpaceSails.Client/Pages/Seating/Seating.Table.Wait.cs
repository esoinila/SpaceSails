using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #757 · <b>WAITING, AND WHO COMES OF IT</b> — part of the table scene (<see cref="Seating"/>); the
/// file's own class summary lives on <c>Seating.Table.cs</c>.
///
/// <para>Owner: <i>"Suppose I just want to sit down and wait to be disturbed?"</i> — so this is not a
/// filler button. Sitting at a table on your own is a choice to be FINDABLE, and the wait is the game
/// asking the room whether it has anything for you. On the right watch it does; on the wrong one it does
/// not, and being told so in an eighty-seat hall that used to be loud IS the event. So: the leave move,
/// the wait beat and its body footnote, the visitor crossing the room, somebody taking the chair, going
/// back to your own table — and putting something on it, with the satchel the panel offers from.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        // ── #757 · WAITING, AND WHO COMES OF IT ───────────────────────────────────────────────────────────
        //
        // Owner: "Suppose I just want to sit down and wait to be disturbed?" — so this is not a filler button.
        // Sitting at a table on your own is a choice to be FINDABLE, and the wait is the game asking the room
        // whether it has anything for you. On the right watch it does. On the wrong one it does not, and being
        // told so in an eighty-seat hall that used to be loud IS the event.

        /// <summary>The scene's own leave move, if it has one. <see cref="Encounter.CanAlwaysLeave"/>'s law says
        /// every scene must, so this is a lookup rather than a doubt.</summary>
        private static Encounter.Move? TheLeaveMove(TableTalk t)
        {
            foreach (Encounter.Move m in t.Scene.Moves ?? [])
            {
                if (string.Equals(m.Id, Encounter.Leave, StringComparison.Ordinal))
                {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// #757 · One beat of holding a table. Core decides whether anybody crosses the room; this counts the
        /// beats and applies the answer.
        ///
        /// <para>THE BEAT COUNTER IS THE ROOM'S, not the sitting's (<c>ex.TableWaits</c>), and that is the whole
        /// anti-abuse law: the approach is seeded on (site, floor, top, watch, beat), so a captain who stood up
        /// and sat down again to get a different answer would simply carry on from the beat they were on. There
        /// is no way to re-press your way into company, which is the same rule every other roll in this game
        /// keeps.</para>
        ///
        /// <para>AND THE WATCH IS NOT TOUCHED. A wait is a beat inside the frozen shift (#709), never a nudge to
        /// the clock — a wait that re-dated the room would make the drawn room and the pressed room two rooms,
        /// which is this project's third named bug class.</para>
        ///
        /// <para>#1016 · <b>…except at the seats that have no room.</b> A top in a docked station's bar and
        /// the ship's own two have no <c>SurfaceExcursion</c> behind them, so there is no room's ledger to
        /// count in — the counter for those lives on the sitting (<c>TableTalk.Waits</c>). It is safe there
        /// for the very reason the ledger exists: the number is what the APPROACH is seeded on, and nobody
        /// ever crosses a floor to a seat aboard your own boat, so there is no roll to re-press your way
        /// into. All the number does there is stop two silence lines saying the same one twice.</para>
        /// </summary>
        private void TableWaited(SurfaceExcursion? ex, TableTalk t)
        {
            int beat;
            if (ex is not null)
            {
                ex.TableWaits.TryGetValue(t.Key, out beat);
                ex.TableWaits[t.Key] = beat + 1;
            }
            else
            {
                // #1016 · ABOARD, THE COUNTER IS THE SITTING'S. The room's ledger is an excursion's, and a
                // boat has none. It is safe to keep it here for exactly one reason, and it is the reason the
                // ledger existed: the beat number is what the APPROACH is seeded on, so a captain who stood
                // up to reroll it would have got a free re-press. Nobody ever comes to a seat on your own
                // ship, so there is no roll to reroll — all the number does aboard is stop the two silence
                // lines saying the same one twice.
                beat = t.Waits;
                t.Waits = beat + 1;
            }

            // #784 · THE BEAT IS ALSO A SHORT REST. Owner: "Sitting down relaxes and heals" / "it is like short
            // rest in TTRPG." The wait already IS the seated watch-beat, so the recovery hangs off it rather
            // than off a second clock — Map.Seated.cs owns the arithmetic and the ceiling.
            //
            // #1016 · AND ABOARD IT IS SKIPPED, STATED RATHER THAN HIDDEN. The short rest's pips are counted
            // in `ex.RestPipsEased`, per watch, on the excursion — the ship has no excursion, so a sit in her
            // cantina gives the body nothing back today. That is a real gap and it is named here: the honest
            // v1 is that the seat works, the wait says its line, and the ledger is not written, because the
            // alternative is a SECOND rest ledger on the page and two answers to "how much has this watch
            // given back" is the fault this file has already had once with a drink in it.
            string? rested = ex is null ? null : _host.RestOneSeatedBeat(ex, beat);

            // #793 · …AND ON A BENCH THE BEAT IS ALSO A LOOK. Owner: "it is a good gumshoe move to see if anyone
            // is following us by foot, as they would need to stop moving also." Sitting still is what makes the
            // reading possible, so the reading is taken on the beat you spend sitting still — never on the press
            // that sat you down, which would be an answer to a question nobody had asked yet.
            string? seen = t.Bench ? _host.TheTailReading() : null;

            // One approach per top per watch. She came over, and whichever way that went, it went.
            //
            // #793 · …and a bench with somebody already on the far end has nowhere to put a third person. Core's
            // own arithmetic (two ends, one of them yours), asked rather than assumed — a wait that dealt an
            // arrival onto a full plank would be the panel claiming an occupancy the room does not have.
            //
            // #817 · …and NOBODY COMES INTO AN OFFICE. The staff of this building are somewhere else on a shift
            // this facility no longer runs; a stranger crossing a private suite to offer the captain work would
            // be the canteen's own scene played in a room whose whole tell is that it is empty.
            //
            // #1016 · …AND NOBODY COMES ABOARD YOUR OWN SHIP, for the office's reason with a hull round it.
            // Her crew is three droids on a fixed patrol; a haulier crossing the captain's own cantina to
            // ask about her brother would be the canteen's scene played in the one room in the game where
            // the player knows exactly who is aboard. It is asked FIRST because it is also the answer where
            // there is no excursion to ask anything else of.
            bool comes = !t.Aboard
                && ex is not null
                && !t.Office
                && (!t.Bench || ParkBenches.TheOtherEndIsFree(t.SharedSeat))
                && !ex.TableApproached.Contains(t.Key)
                && (_host.ApproachCheat
                    ?? SittingAlone.SomebodyComes(
                        ex.Stop.Body.Id, ex.Floor, t.Index, ex.CanteenWatch, beat, t.Quiet));

            // …and the second clause is the compiler being told what the first one already guarantees: a
            // room that could send somebody over is a room, and a room is an excursion. Written out rather
            // than asserted, because a nullable that is "obviously" not null is how this repository has been
            // surprised before.
            if (!comes || ex is null)
            {
                // #680/#736 · NOTHING HAPPENING IS AN ANSWER, and it is said on the panel the captain pressed,
                // through the one ending every other answer at this table uses. A wait that produced silence
                // and no words would be indistinguishable from a control that is broken (#603).
                // #784 · …with the body's footnote after it, when the beat gave something back. The silence is
                // the EVENT and it keeps the first sentence; the rest is one clause added to it, and never a
                // second panel line competing with the room's own answer.
                //
                // Composed INSIDE the call rather than hoisted into a local, deliberately: #778's own guard
                // reads the ordering here to prove the nobody-came line goes through the one ending, and a local
                // computed above it flips that reading while changing nothing about the behaviour. The guard is
                // right about the law, so this stays shaped the way the law is checked.
                //
                // #793 · …and a PARK is not a hall. The room's answer comes from the room you are sitting in:
                // trays and eighty chairs read on gravel under grow-lamps would be #740's fault with a bench
                // under it. The tail reading rides in front of the body's footnote because it is what the beat
                // was SPENT on — you sat still to look, and what you saw is the answer.
                TableAnswered(ex, t, SittingAlone.Wait,
                    new CanteenTable.Answer(WithTheBodysFootnote(
                        WithTheBodysFootnote(
                            // #821 · …and a CUBICLE is not an office either. A chair creaking down the row and
                            // lamps over a garden, read from inside a locked WC, would be the room's answer
                            // describing a room the captain is not in — #740's fault with a partition round it.
                            // #1016 · …and A BOAT is not a building at all. Owner, on 7 Deck: "Why no table
                            // here to sit at?" / "Why no table in cabin either?" A hall's eighty chairs and
                            // an office's turned-off shift are both somebody else's room; what a captain
                            // sitting in his own cantina hears is his own boat, and the cabin hears it
                            // through a door. Quiet is which of the two, exactly as it is one line down.
                            // #1040 · …and a STOOL is not a table, even aboard. The cantina's own silence
                            // ends on "the chair opposite stays yours", and a counter has no chair opposite;
                            // a sentence naming furniture the picture does not have is the fastest lie this
                            // game has ever been caught telling.
                            t.Aboard
                                ? t.Stool
                                    ? SittingAlone.NobodyCameAtYourOwnCounter(beat)
                                    : SittingAlone.NobodyCameAboard(t.Quiet, beat)
                                : t.CubicleKey is { Length: > 0 }
                                ? CubicleLock.NothingHappens(beat)
                                : t.Office
                                ? RingOffice.NobodyCame(beat)
                                : t.Bench
                                    ? ParkBenches.NobodyCame(beat)
                                    // …and the shift is the ROOM's where there is a room, and the sitting's
                                    // own frozen one at the seats that have no excursion behind them.
                                    : SittingAlone.NobodyCame(
                                        ex?.CanteenWatch ?? t.Watch, beat, t.Quiet),
                            seen),
                        rested)));
                return;
            }

            ex.TableApproached.Add(t.Key);

            // #793 · …and WHAT ARRIVING MEANS depends on the furniture. A chair opposite is a conversation; the
            // far end of a plank is a stranger with a cup who says nothing. One branch, because they are two
            // different events and collapsing them would raise a full card over a park.
            if (t.Bench)
            {
                SomebodyTakesTheOtherEnd(ex, t);
                return;
            }

            // #731 · AND SHE WALKS. Owner: "Now it is possible to have NPC ask to sit down at our table and
            // offer a quest! This is the classic TTRPG event." The beat has decided somebody comes; WHERE FROM
            // and HOW is the walker's, and it is a door that does not open for the captain and a route across
            // the real floor. The card is raised when she gets here (TheVisitorHasArrived) — the same card off
            // the same flag. The walk is the ceremony, not a second event.
            //
            // A floor with no such door, or no way through, has no provenance to offer, and she arrives the
            // way she always has. That is the honest fallback and never a body placed at the far end of a walk
            // nobody could walk.
            if (_host.WalkSomebodyToYourTable(ex, t.Index))
            {
                return;
            }

            SomebodyTakesTheChair(ex, t);
        }

        /// <summary>#731 · She has crossed the room and is standing at the table. The strip's rule is #865's
        /// and is untouched: somebody came to you, therefore the card.
        ///
        /// <para>The sitting is CHECKED rather than assumed. A captain who stood up, took their leave, or
        /// walked to another top while she was on her feet has ended the scene she was walking into, and the
        /// right answer is that nothing happens — never a card raised over a chair nobody is in.</para>
        /// </summary>
        public void TheVisitorHasArrived(SurfaceExcursion ex, int tableIndex)
        {
            if (Table is not { Bench: false, TheyCameToYou: false } t || t.Index != tableIndex)
            {
                return;
            }
            SomebodyTakesTheChair(ex, t);
        }

        /// <summary>
        /// #757 · SOMEBODY CROSSES THE ROOM — and the roles are the other way round.
        ///
        /// <para>Owner: <i>"a stranger may approach me and 1. ask to sit down, 2. maybe offer to buy me a drink,
        /// 3. tell me what they have in mind… think Gandalf knocking on Bilbo's door."</i> #746's table is the
        /// captain talking their way into somebody else's business; this is somebody walking across a hall to
        /// recruit the captain into theirs, and the ladder is Core's, on the same machine.</para>
        ///
        /// <para>THE SAME PANEL, not a second one. One table, one continuous sitting — swapping the scene keeps
        /// the outcome the captain is mid-way through reading on the screen, where closing and re-opening would
        /// blink it (#680).</para>
        /// </summary>
        private void SomebodyTakesTheChair(SurfaceExcursion ex, TableTalk t)
        {
            // #784 · THE PAPERS GO AWAY WHEN SOMEBODY SITS DOWN. Owner: "your papers are OUT when somebody walks
            // up… putting things away is a beat, not an instant." The privacy predicate that licensed the spread
            // reads Solo, and Solo is about to become false — so the hold ends HERE, before the flag flips, and
            // it ends the way privacy ending should end it: sleeve shut, book blank, nothing filed.
            _host.AbandonProcessing(Core.Processing.Work.Write, Core.Processing.Interruption.CompanyArrived);

            t.Solo = false;

            // #865 · …AND THIS IS THE ONE SITTING THAT BECOMES A CARD. She walked across the hall to you; her
            // face is the point, which is the owner's own line between the two frames — posture changes are a
            // strip, people who come to you are a card. It is set HERE and nowhere else, so the only way into
            // the modal frame is somebody arriving at a captain who was already sitting down.
            t.TheyCameToYou = true;

            t.Plate = SittingAlone.VisitorPlate;
            t.Scene = SittingAlone.TheVisitor();
            t.Said.Clear();     // #749 · a new conversation: nothing has been said to HER yet.
            t.Showing = false;
            t.Math = null;
            t.Free = Math.Max(0, t.Free - 1);

            // #761 · Told, clearly, on the surface the captain is looking at. She is standing there; the game
            // says so in words and does not leave the arrival to be inferred from a changed button row.
            t.Outcome = t.Scene.Opening;
            RendererInterop.PlayCue("reveal");
            _host.RequestVaultSave();
            _host.StateHasChanged();
        }

        /// <summary>#757 · She goes, and the table is yours again. The outcome line stays exactly as it was —
        /// what she said on the way out is the last thing that happened, and it must not be wiped by the state
        /// change that follows it.
        ///
        /// <para>#783 · The register is asked AGAIN rather than remembered: a glass goes warm while somebody is
        /// standing over you, and a table that stayed "resting" because it was resting ten minutes ago would be
        /// a picture of boots up on a chair she has just got out of.</para></summary>
        private void BackToYourOwnTable(SurfaceExcursion ex, TableTalk t)
        {
            // #731 · AND SHE GOES — on her own legs, back through the door she came out of. Owner: "If they go
            // behind a door that is locked to us, we use that as 'I guess that concludes the conversation'
            // point in the plot / situation." This is the TRIGGERED departure the issue proposed, and it is
            // the same walker the shift uses for ambience; the trigger is simply that the scene is over.
            //
            // FIRST, while she is still the person at this table: the walk starts from the chair she crossed
            // the room to, and the lines below are what happens to the PANEL after she has stood up. Nothing
            // is said about any of it — the room delivers the full stop by parting for her a second time.
            _host.WalkTheVisitorOut(ex, t.Index);

            t.Solo = true;
            // #865 · …and the card goes with her. The frame comes back down to the strip the captain was sitting
            // in before she arrived, which is the same one occupation of one table it always was.
            t.TheyCameToYou = false;
            t.DrinkInHand = _host.APourInFrontOfYou;
            t.Relaxed = SittingAlone.SitReadsAsRelaxed(t.DrinkInHand, ex.CanteenWatch);
            t.Plate = SittingAlone.OwnTablePlate;
            t.Scene = SittingAlone.TheTable(t.Relaxed, t.DrinkInHand);
            t.Said.Clear();
            t.Showing = false;
            t.Free = Math.Min(t.Seats, t.Free + 1);
            _host.StateHasChanged();
        }

        /// <summary>
        /// #746 · Put something on the table. The satchel as a conversational move: papers make hands nervous,
        /// an authority card makes a table quiet, a file on somebody is LOUD.
        /// </summary>
        public void TableShow(Core.Satchel.Item item)
        {
            if (_host.Surface is not { } ex || Table is not { } t)
            {
                return;
            }

            // #751 · …and WHERE. The LOUD closure is a fact about the room, not about the paper: in a cabinet
            // the counter has no eyes, so nothing closes. Core decides that; this hands it the one bit.
            CanteenTable.Answer said = CanteenTable.PutOnTheTable(item, t.Who, t.Quiet);
            t.Showing = false;

            // The one thing on this table that counts as a MODIFIER gets remembered, keyed like every other
            // watch fact. It is the flag TableSituation reads for the +1 and the auto-resolve alike, so the
            // paper the Hand cannot look away from can never be two different facts.
            if (CanteenTable.CountsAsPaperOnTheTable(item, t.Who))
            {
                ex.TableMoves.Add(MoveKey(t, CanteenTable.Show + ":relevant"));
            }

            TableAnswered(ex, t, CanteenTable.Show, said);
        }
        // ── THE SATCHEL, AS SEEN FROM A TABLE ─────────────────────────────────────────────────────────────

        /// <summary>What the captain could put on the table. Everything they are carrying — the gesture is
        /// "put something down", and a pocket that hid the wrong answers would be hinting at the right one.</summary>
        public IReadOnlyList<Core.Satchel.Item> TableShowables() => _host.Satchel;

        /// <summary>#784 · The room's answer, with the body's footnote after it when the beat gave something
        /// back. One sentence and then one clause: the silence at your table is the EVENT (#757) and it keeps
        /// the lead, because a rest that pushed in front of the room's own answer would be the mechanic talking
        /// over the scene.</summary>
        private static string WithTheBodysFootnote(string saidByTheRoom, string? saidByTheBody) =>
            saidByTheBody is { Length: > 0 } ? $"{saidByTheRoom} {saidByTheBody}" : saidByTheRoom;
    }
}
