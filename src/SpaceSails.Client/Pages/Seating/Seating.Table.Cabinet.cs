using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #758 · <b>THE CURTAIN AND THE DOOR</b> — part of the table scene (<see cref="Seating"/>); the file's
/// own class summary lives on <c>Seating.Table.cs</c>.
///
/// <para>Owner, 2026-08-06: <i>"the cabinets could have some kind of privacy curtain effect ... something
/// that prevents the sound or sight catching what happens there too easily but is not a real door."</i>
/// The leaf's stage and its label and hint, drawing or dogging it, what a sensitive beat behind it costs,
/// and the bark that comes back LATER in another part of the room when it leaked — plus #731 v2's
/// <c>SheMightLeadYouIn</c>, the contact who stands up and asks you to follow her through one.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        // ── #758 · THE CURTAIN AND THE DOOR ───────────────────────────────────────────────────────────────
        //
        // Owner, 2026-08-06: "the cabinets could have some kind of privacy curtain effect ... something that
        // prevents the sound or sight catching what happens there too easily but is not a real door."
        //
        // The law is Core's (CabinetPrivacy) and the state is the excursion's set of dogged leaves. What is
        // here is the seat's own three lines: which stage the room the captain is IN stands at, what the one
        // strip button says, and what pressing it does.

        /// <summary>#758 · Which stage this sitting's cabinet stands at — <c>Curtain</c> anywhere that is not
        /// a cabinet at all, because a hall table has no leaf to dog and the default is the honest answer.
        ///
        /// <para>Asked of the one set the DECK is drawn from (<c>ex.CabinetsDogged</c>) and never of a flag
        /// captured when the captain sat down, exactly as the cubicle's catch is (#821): the leaf can be
        /// worked while you are already sitting behind it, and the glyph on the plan and the clause on the
        /// strip have to change on the same frame.</para></summary>
        public CabinetPrivacy.Stage CabinetStage =>
            _host.Surface is { } ex && Table is { Cabinet: > 0 } t
            && ex.CabinetsDogged.Contains(CabinetPrivacy.Key(ex.Floor, t.Cabinet))
                ? CabinetPrivacy.Stage.Door
                : CabinetPrivacy.Stage.Curtain;

        /// <summary>#758 · Is there a leaf to work from this seat? A cabinet is the only seat in the game with
        /// a door of its own, so the strip's button exists nowhere else.</summary>
        public bool ACabinetLeafToWork => Table is { Cabinet: > 0 };

        /// <summary>What the one button says, which is always the OTHER stage — the room never has to
        /// announce the state it is already in.</summary>
        public string CabinetLeafLabel => CabinetPrivacy.LabelFor(CabinetStage);

        /// <summary>…and what it says on the way past, which is the cost.</summary>
        public string CabinetLeafHint => CabinetPrivacy.HintFor(CabinetStage);

        /// <summary>
        /// #758 · WORK THE LEAF — the whole of the captain's half of the mechanic, in one press.
        ///
        /// <para>Dogging is public and the counter remembers: the who-was-inside line goes into the book
        /// ONCE per cabinet per excursion, on the way shut only, and never for the cloth. Letting the leaf
        /// back open writes nothing, because the thing worth remembering happened when it closed and a book
        /// does not un-write.</para>
        ///
        /// <para>It does not rebuild the deck itself — the plan's glyph is refreshed by the page wrapper the
        /// strip actually calls (<c>WorkTheCabinetLeaf</c> on <see cref="Map"/>), for the reason every other
        /// verb in this family reaches the world through <see cref="_host"/>: a seat that rebuilt decks would
        /// need a twenty-ninth thing from the page.</para>
        /// </summary>
        public void DrawOrDogTheCabinet()
        {
            if (_host.Surface is not { } ex || Table is not { Cabinet: > 0 } t)
            {
                return;
            }

            string key = CabinetPrivacy.Key(ex.Floor, t.Cabinet);
            CabinetPrivacy.Stage from = CabinetStage;
            CabinetPrivacy.Stage to = from == CabinetPrivacy.Stage.Door
                ? CabinetPrivacy.Stage.Curtain
                : CabinetPrivacy.Stage.Door;

            if (to == CabinetPrivacy.Stage.Door)
            {
                ex.CabinetsDogged.Add(key);
            }
            else
            {
                ex.CabinetsDogged.Remove(key);
            }

            // The counter's long memory. Core says WHICH transition is memorable; the set says whether this
            // room has already been written down. Two guards read this line: it fires once, and never for a
            // curtain.
            //
            // #917 · …and the FROZEN WATCH goes with it, because the keep only tends this bar on the living
            // ones and the sentence has to be able to say so. The fact is filed on both; the words fork.
            //
            // #770 · …and WHICH LEAF it was is a fact the sentence has to carry honestly. A booked negotiation
            // room borrows this whole mechanic (a room with a door is a room with a door) on an ordinal of its
            // own, and a book that called suite 5 on the garden "cabinet 405" would be the note reporting a
            // different building than the one the captain is sitting in.
            if (CabinetPrivacy.TheCounterWritesItDown(from, to) && ex.CabinetsWitnessed.Add(key))
            {
                _host.FileNote(
                    Core.Interior.RoomBooking.IsABookedLeaf(t.Cabinet)
                        ? Core.Interior.RoomBooking.DoggedTheDoorNote(
                            Core.Interior.RoomBooking.RoomOfLeaf(t.Cabinet), ex.CanteenWatch)
                        : CabinetPrivacy.WhoWasInsideNote(t.Cabinet, ex.CanteenWatch),
                    CanteenRegulars.Glyph);
            }

            // Said on the strip, in the one layer the room stays lit behind (#865).
            t.Outcome = CabinetPrivacy.SaidOn(to);
            _host.RequestVaultSave();
            _host.StateHasChanged();
        }

        /// <summary>
        /// #758 · ONE SENSITIVE BEAT BEHIND THE CURTAIN — rolled, never announced.
        ///
        /// <para><b>WHICH BEAT, and how this came to be the right one.</b> It shipped on
        /// <see cref="TableShow"/> — putting a paper on the table — which reads exactly like the issue's own
        /// <i>"paper on a table in a room with a door"</i> and was <b>unreachable in a cabinet</b>: the SHOW
        /// move only exists on the named cast's scenes (<c>CanteenTable.SceneFor</c>), a cabinet top is built
        /// with no plate so <see cref="TryTakeTable"/> hands it <c>SittingAlone.TheTable</c> (wait and stand
        /// and nothing else), and <c>SomebodyComes</c> refuses to bring anybody to a quiet top. So the whole
        /// of stage one was dead code, and every guard on it was green: Core exercised
        /// <see cref="CabinetPrivacy.Leaks"/> directly and the one client guard planted the leak by
        /// reflection. <b>A guard that cannot tell pass from fail</b> — this repository's fifth named bug
        /// class, found by the cloud ultrareview on #918 and not by any test in here.</para>
        ///
        /// <para>The beat is THE WRITE-UP now, at the [I] spread — the one thing a captain actually does at a
        /// cabinet table, and still literally paper on a table in a room with a door. It fires from
        /// <c>Map.TheWriteUpLands</c>, at the far end of the dig, after the entry has been accepted: one
        /// completed dig is one beat, so a re-press on a sheet already in the book is not a second roll.</para>
        ///
        /// <para><b>Nothing is said either way.</b> A true does not change the write-up's outcome, raise a
        /// card or touch the field book; it waits on the excursion until somebody who has no way of knowing
        /// says the cabinet's number out loud (<see cref="TheBarkAtThisTop"/>). A dogged leaf never gets as
        /// far as the die — <see cref="CabinetPrivacy.Leaks"/> answers the stage first.</para>
        /// </summary>
        public void ASensitiveBeatBehindTheCurtain()
        {
            if (_host.Surface is not { } ex || Table is not { Cabinet: > 0 } t)
            {
                return;
            }

            int beat = ex.CabinetBeats++;
            if (CabinetPrivacy.Leaks(ex.Stop.Body.Id, t.Cabinet, ex.CanteenWatch, beat, CabinetStage))
            {
                ex.CabinetLeaked = t.Cabinet;
            }
        }

        /// <summary>
        /// #758 · WHAT THE PERSON AT THIS TOP SAYS — and, exactly once, the thing said by somebody who has no
        /// way of knowing it.
        ///
        /// <para>A leak behind the curtain is never announced on the beat it happens. It comes back HERE,
        /// later, in another part of the room, as a stranger naming a cabinet number and then declining to go
        /// on — #715's per-entity memory arriving as a sentence rather than as a meter, which is the same
        /// seam <see cref="RipAndBin.SeenNote"/> uses. Spent by being said, so it is one moment and not a
        /// tone the rest of the evening takes on.</para>
        ///
        /// <para>Never in a cabinet, and never from one of the ten named regulars: the whole point is that it
        /// is somebody nobody would think to watch.</para>
        /// </summary>
        private static string? TheBarkAtThisTop(SurfaceExcursion ex, CanteenRegulars.TableSeat top)
        {
            if (top.Stranger && top.Cabinet == 0 && ex.CabinetLeaked is { } cabinet)
            {
                ex.CabinetLeaked = null;
                // #770 · …and it names what it actually leaked out of. A booked suite on the garden is not
                // cabinet three off the back of the hall, and the one sentence in this whole mechanic that
                // reaches the player must not be the one that gets the building wrong.
                return Core.Interior.RoomBooking.IsABookedLeaf(cabinet)
                    ? Core.Interior.RoomBooking.BarkThatKnows(
                        Core.Interior.RoomBooking.RoomOfLeaf(cabinet))
                    : CabinetPrivacy.BarkThatKnows(cabinet);
            }

            return top.Line;
        }
        /// <summary>
        /// #731 v2 · <b>FOLLOW ME.</b>
        ///
        /// <para><b>Owner, 2026-08-06, on #751's cabinets:</b> <i>"Also it is dramatic telling when our
        /// contact wants us to follow them into kabinetti :-D"</i></para>
        ///
        /// <para><b>WHEN.</b> The moment the thing she crossed the room to say becomes sayable and has not
        /// been said — asked of the SCENE'S OWN STATE and of nothing else. <see cref="Core.Interior.Escort.TheDealMoveIn"/>
        /// finds the move the field book would keep (#757 put that mark on the move for a different reason
        /// which turns out to be this one), <see cref="TableMoveAvailable"/> says whether it is on offer this
        /// second, and <c>t.Said</c> says whether it has already been spent. A counterpart with nothing worth
        /// writing down never does this, because there would be nothing to take you anywhere FOR.</para>
        ///
        /// <para><b>WHO.</b> <see cref="Core.Interior.Escort.LeadsYouIn"/>, seeded on the shift and the top, so one evening
        /// always goes the same way and most of the time she simply tells you where she is sitting. A contact
        /// who ALWAYS walks you into a booth is a corridor with a cutscene in it.</para>
        ///
        /// <para><b>WHAT HAPPENS TO THE PANEL.</b> Exactly what happens when anybody stops sitting opposite
        /// you: the card comes down to the strip and the table is your own again. <b>The outcome line she
        /// left is not touched</b> — the last thing she said is the last thing that happened, and wiping it
        /// would be the state change talking over the scene (<see cref="BackToYourOwnTable"/>'s own rule).
        /// Not one word is added anywhere. She is simply on her feet, and there is a doorway across the hall
        /// with somebody standing in it looking at you.</para>
        /// </summary>
        private void SheMightLeadYouIn(SurfaceExcursion ex, TableTalk t)
        {
            if (!t.TheyCameToYou
                || Core.Interior.Escort.TheDealMoveIn(t.Scene) is not { } deal
                || t.Said.Contains(deal.Id)
                || !TableMoveAvailable(ex, t, deal))
            {
                return;
            }

            // The conversation is handed over BEFORE the table is reset, because the reset clears the very set
            // that has to survive the walk.
            if (!_host.WalkTheVisitorIntoACabinet(ex, t.Index, t.Scene, t.Said))
            {
                return;
            }

            t.Solo = true;
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
    }
}
