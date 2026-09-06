using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · <b>THE TABLE'S MOVES</b> — part of the table scene (<see cref="Seating"/>); the file's own
/// class summary lives on <c>Seating.Table.cs</c>.
///
/// <para>Three banners' worth of one subject: THE SITUATION THE DICE SEE (the last ten minutes as facts —
/// every one of them something the player did and could narrate back), WHAT THE PANEL ASKS (three
/// one-liners so the razor never holds a <c>SurfaceExcursion</c> or a <c>TableTalk</c> in a local), and
/// MAKING A MOVE itself. Nothing here decides what a move COSTS or what anybody says — that is
/// <c>Encounter</c> and <c>CanteenTable</c>, as it has always been — and every branch ends at
/// <c>TableAnswered</c>, which is next door in <c>Seating.Table.Answered.cs</c>.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        // ── THE SITUATION THE DICE SEE ────────────────────────────────────────────────────────────────────

        /// <summary>The last ten minutes, as facts. Every one of them is something the player did and could
        /// narrate back — which is the difference between this and a character sheet.</summary>
        private Encounter.Situation TableSituation(SurfaceExcursion ex, TableTalk t) => new(
            RoundBought: ex.TableRounds.Contains(t.Key),
            PaperShown: ex.TableMoves.Contains(MoveKey(t, CanteenTable.Show + ":relevant")),
            HouseWaysLearned: ex.TableHouseWays,
            NerveMarked: Encounter.NerveReadsAcrossATable(_host.Nerve),
            Fumbled: ex.TableHardened.Contains(t.Key));

        /// <summary>Is this move on offer right now? Core's own requirement check, plus the one fact that is
        /// about the ROOM rather than about the move: a LOUD file shuts ask-about-work at this table for the
        /// watch, and a shut ask is disabled with a reason rather than quietly missing.</summary>
        private bool TableMoveAvailable(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
        {
            if (move.Id == CanteenTable.Work && ex.TableAskShut.Contains(t.Key))
            {
                return false;
            }
            var made = new List<string>();
            foreach (Encounter.Move m in t.Scene.Moves)
            {
                if (ex.TableMoves.Contains(MoveKey(t, m.Id)))
                {
                    made.Add(m.Id);
                }
            }
            // #749 · Both sets, and they are not the same set. The watch's is what the room remembers; the
            // sitting's is what has actually been SAID in front of you, which is the only thing an answer can be
            // an answer to.
            return Encounter.Available(move, _host.Credits, _host.Satchel, made, t.Said);
        }

        /// <summary>Why a move is not on offer. #603's founding law one layer up: a control that does nothing
        /// and says nothing is indistinguishable from a bug.</summary>
        private string TableMoveWhyNot(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
        {
            if (move.Id == CanteenTable.Work && ex.TableAskShut.Contains(t.Key))
            {
                return "Not after what you just put on the table. Not at this table, not this shift.";
            }
            return move.Needs switch
            {
                Encounter.Requirement.Credits => $"You are short of the {move.Credits} cr.",
                Encounter.Requirement.SatchelItem => "Your pockets are empty.",
                Encounter.Requirement.PriorMoveThisWatch when move.Id == CanteenTable.SmallTalkAgain
                    => "They have not got that far with you yet.",
                _ => "Not yet.",
            };
        }

        // ── WHAT THE PANEL ASKS ───────────────────────────────────────────────────────────────────────────
        //
        // Three one-liners so the razor never has to hold a SurfaceExcursion or a TableTalk in a local. Same
        // discipline the rest of this page uses: the markup asks questions, it does not compute answers.

        /// <summary>
        /// Is this move on offer?
        ///
        /// <para><b>#1016 · AND A SEAT WITH NO BUILDING UNDER IT ANSWERS PROPERLY NOW.</b> This used to open
        /// on <c>_host.Surface</c>, which meant every button on a sitting that has no excursion behind it
        /// rendered DISABLED: the eighth seat (#973 L5b's top in a docked station's bar) shipped with a strip
        /// whose SIT A WHILE and Stand up were both greyed out, and the ship's own two would have inherited
        /// it. A control that is drawn and does nothing is #603's founding complaint, and the seat was not
        /// the thing that was wrong — the gate was.</para>
        ///
        /// <para>What such a seat's scene actually offers is Core's answer, unchanged: the two moves of
        /// <see cref="SittingAlone.TheTable"/>, neither of which has a requirement to check. The room's own
        /// clauses (a LOUD file shutting the ask, a move already made this watch) are checked where there IS
        /// a room, because they are facts about one.</para>
        /// </summary>
        public bool TableMoveOnOffer(Encounter.Move move) =>
            Table is { } t
            && (_host.Surface is { } ex
                ? TableMoveAvailable(ex, t, move)
                : Encounter.Available(move, _host.Credits, _host.Satchel, [], t.Said));

        /// <summary>Why not, said out loud on the disabled control.</summary>
        public string TableMoveRefusal(Encounter.Move move) =>
            _host.Surface is { } ex && Table is { } t ? TableMoveWhyNot(ex, t, move) : "Not yet.";

        /// <summary>#746 · Is the game NUDGING you at this move? Exactly one does: the fitter's ask, after the
        /// hand has waved you toward it. That is the NO-AND's "another door opens in the conversation" made
        /// visible — the scene moved, and the panel should look like it moved.</summary>
        public bool TableMoveIsUrged(Encounter.Move move) =>
            _host.Surface is { } ex && Table is { Who: CanteenTable.Who.Fitter } t
            && ex.TableFitterOpen
            && move.Id == CanteenTable.Work
            && !ex.TableMoves.Contains(MoveKey(t, CanteenTable.Work));

        // ── MAKING A MOVE ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #746 · One move at the table.
        ///
        /// <para>Every branch ends at <see cref="TableAnswered"/>, which is the one place an outcome becomes
        /// words on the screen — so there is exactly one method that can put #680's law back in the pulse.</para>
        /// </summary>
        private void TableMove(string moveId)
        {
            if (Table is not { } t)
            {
                return;
            }

            // #973 L5b/#1016 · THE EXCURSION IS NULLABLE FROM HERE DOWN, and that is the whole of what a seat
            // OFF a landing needed. Two of this scene's moves are about the CHAIR — stand up, and wait to see
            // who comes — and neither has ever consulted a building: the eighth seat (a top in a docked
            // station's bar) and the ship's own two (a cantina top, the desk in CABIN 1) are sittings with no
            // `ex` behind them at all, and a guard that returned on the first line left them with a panel of
            // dead buttons. Everything BELOW the wait is genuinely the hall's business — a round bought on a
            // tab, a paper put on a table, an ask that hardens a top for the watch — and every one of those
            // is keyed on the excursion's own ledgers, so they stay behind it.
            SurfaceExcursion? ex = _host.Surface;

            // Leaving is free and never penalised. First, so nothing below can ever grow a price on it.
            if (moveId == CanteenTable.Leave)
            {
                // #757 · …and the SCENE says what leaving it looks like. Standing up from a table you took
                // alone is not the same sentence as standing up from somebody else's — one is a courtesy, the
                // other is just a chair going back under a table — and which it is belongs to the content file
                // rather than to a constant this method reached for.
                CanteenTable.Answer bye = TheLeaveMove(t) is { } goodbye
                    ? CanteenTable.SaidPlainly(goodbye)
                    : CanteenTable.TookTheirLeave();
                CloseTable();
                // The ONE pulse in this scene, and it is correct precisely because the panel has just gone:
                // there is no dialog subtree left to say it in. #680 is about which surface the player is
                // looking at, never about pulses being wrong.
                _host.ShowPulseMessage(bye.Line);
                return;
            }

            // Asking for the chair. No roll, no cost — and the wave-in is the ANSWER to it, said inside the
            // panel like every other answer at this table (#680). Nothing durable changed, so nothing is saved:
            // a captain who reloads is standing at the table again, which is where they were.
            if (moveId == CanteenTable.Join)
            {
                t.Joined = true;
                t.Outcome = t.Scene.Opening;
                return;
            }

            if (moveId == CanteenTable.Show)
            {
                t.Showing = !t.Showing;
                t.Math = null;
                return;
            }

            // #757 · WAIT — the passive verb, and the only one a table you took alone has. Its own branch and
            // not a fixed outcome, because what it says is decided by the ROOM at the moment it is pressed.
            if (moveId == SittingAlone.Wait)
            {
                TableWaited(ex, t);
                return;
            }

            // …and the rest of the scene IS the hall. Nothing below this line is a question a chair can
            // answer on a boat.
            if (ex is null)
            {
                return;
            }

            Encounter.Move move = default;
            bool found = false;
            foreach (Encounter.Move m in t.Scene.Moves)
            {
                if (string.Equals(m.Id, moveId, StringComparison.Ordinal))
                {
                    (move, found) = (m, true);
                    break;
                }
            }
            if (!found || !TableMoveAvailable(ex, t, move))
            {
                return;
            }

            switch (moveId)
            {
                case CanteenTable.SmallTalk:
                case CanteenTable.SmallTalkAgain:
                    // #751 · A stranger says the one thing they were dealt this watch. Core drew it (per
                    // patron, per watch); this only hands it back.
                    TableAnswered(ex, t, moveId,
                        t.Who == CanteenTable.Who.Stranger
                            ? CanteenTable.StrangerSaid(t.Bark ?? "")
                            : CanteenTable.MadeSmallTalk(t.Who, moveId == CanteenTable.SmallTalkAgain));
                    return;

                case CanteenTable.Round:
                    // Bought BEFORE the answer, because the answer is the glasses arriving. The +1 it buys is
                    // read off ex.TableRounds by every ask afterwards at THIS table — the drink walks over from
                    // the counter fixture Core already put in this room, not from a menu in the abstract.
                    _host.Credits -= move.Credits;
                    ex.TableRounds.Add(t.Key);
                    TableAnswered(ex, t, moveId, CanteenTable.BoughtTheRound());
                    return;

                case CanteenTable.TakeScaffold:
                    TableAnswered(ex, t, moveId, CanteenTable.ScaffoldTaken());
                    return;

                case CanteenTable.Work:
                    TableAsksAboutWork(ex, t, move);
                    return;

                default:
                    // #749/#680 · A FIXED OUTCOME IS STILL AN ANSWER, and it speaks because the move CARRIES a
                    // line — never because somebody wrote its id a case down here.
                    //
                    // This is the path the dodge falls down, and it is the whole of the fix: the switch above
                    // used to enumerate ids and drop everything else off the end in silence, so
                    // Encounter.Move.Says — the framework's own "the outcome is FIXED" field, the one a guard
                    // stop's content will be written on — reached the screen for exactly the moves a client
                    // author had remembered. THE DODGE IS STILL FREE: the answer built here has every field but
                    // the line at its default, which is the nothing the owner smoke-tests by hand.
                    if (move.Says is { Length: > 0 })
                    {
                        TableAnswered(ex, t, moveId, CanteenTable.SaidPlainly(move));
                    }

                    // #757 · …and one of those fixed outcomes ENDS A VISIT rather than the sitting. Waving
                    // somebody off is free (it is a refusal, and refusals are free here), it says what it says
                    // through the one ending above like everything else, and then the table is yours again —
                    // the panel never blinks, because it was one occupation of one table all along.
                    if (moveId == SittingAlone.WaveOff)
                    {
                        BackToYourOwnTable(ex, t);
                    }
                    return;
            }
        }

        /// <summary>
        /// #746 · The ask. The Fitter's is offered plainly; the Hand's is the one rolled move at this table.
        /// </summary>
        private void TableAsksAboutWork(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
        {
            if (!move.Rolled)
            {
                TableAnswered(ex, t, move.Id, CanteenTable.FitterAsksAboutWork());
                return;
            }

            // The deep card on the table settles it without a roll: fear, not friendship. Core decides that this
            // is what that card does — the client only notices it is down.
            Encounter.Situation situation = TableSituation(ex, t);
            if (situation.PaperShown)
            {
                TableAnswered(ex, t, move.Id, CanteenTable.HandAsksAboutWork(Encounter.Band.YesBut));
                return;
            }

            // ATTEMPT INDEX, so a second ask at a different table this watch is a different roll and pressing the
            // same button twice is not. The counter is the count of asks already made this watch, which is what
            // the fumble set already remembers.
            int attempt = ex.TableHardened.Contains(t.Key) ? 1 : 0;
            DiceRoll roll = Encounter.Roll(
                ex.Stop.Body.Id, ex.Floor, t.Who.ToString(), move.Id, attempt, situation);
            Encounter.Band band = Encounter.Settle(roll, _host.RollCheat);

            t.Math = roll.Describe();
            TableAnswered(ex, t, move.Id, CanteenTable.HandAsksAboutWork(band));
        }
    }
}
