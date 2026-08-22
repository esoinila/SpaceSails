using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c · THE TABLE SCENE, as the seat's own verbs — moved off <c>Map.Table.cs</c>, byte for byte
/// except for the receiver.
///
/// <para>#746's design, #680's law about where an outcome is said, and the reason this file decides NOTHING
/// (which moves exist, what they cost, what anybody says — all of it is <c>Encounter</c> and
/// <c>CanteenTable</c>) are documented where they have always been documented: <c>Map.Table.cs</c>'s own class
/// summary, which stayed with the <c>TableTalk</c> record, the dev rows and the mess chit. What is here is the
/// SCENE — opening a top, the moves on it, the wait beat, somebody crossing the room, and the one teardown
/// every stand-up in the game goes through — and what it needs from the page it is drawn on is
/// <see cref="ISeatHost"/> and nothing else.</para>
///
/// <para>One member arrived from a different partial: <c>WithTheBodysFootnote</c>, which was declared under
/// <c>Map.Seated.cs</c>'s short-rest banner and has exactly one caller, the wait beat below. The short rest
/// itself stayed on the page — it spends the nerve and condition systems — and the seat asks it for a
/// sentence (<see cref="ISeatHost.RestOneSeatedBeat"/>); this is only the composer that puts that sentence
/// after the room's own answer.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>What a table's watch-scoped state is keyed on. An ORDINAL and never a position: two doubles
        /// compared with a tolerance is a guess, and Core hands the ordinal over for free.</summary>
        private static string TableKey(SurfaceExcursion ex, int tableIndex) =>
            $"{ex.CanteenWatch}:{ex.Floor}:{tableIndex}";

        /// <summary>Whether a move has already been made at this table this watch.</summary>
        private static string MoveKey(TableTalk t, string moveId) => $"{t.Key}:{t.Who}:{moveId}";

        // ── OPENING THE SCENE ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #746 · Stand at a table with a free seat and ask to join.
        ///
        /// <para>WHICH TABLE AND WHO IS AT IT COME FROM CORE — <see cref="CanteenRegulars.Tables"/>, the same
        /// call the renderer drew the room with, off the same frozen watch. Matching by position against the
        /// console the press landed on is the only geometry here, and it is a lookup rather than a decision.</para>
        ///
        /// <para>Only the three wired regulars are a scene. The rest of #709's cast keep their one breath: they
        /// are the room being a room, and a canteen where every stranger has a conversation tree is a corridor
        /// with quest-givers in it.</para>
        /// </summary>
        public bool TryOpenTable()
        {
            if (_host.Surface is not { } ex || ex.Floor >= 0)
            {
                return false;
            }

            // Already sitting there. The press is CONSUMED and nothing happens — re-opening would wipe the
            // outcome line the captain is in the middle of reading, and E is not how you stand up: "Take your
            // leave" is, because leaving a table right is a thing this scene has an opinion about.
            if (Table is not null)
            {
                return true;
            }

            if (_host.DeckPlan.NearestConsoleSpot(_host.AvatarX, _host.AvatarY) is not
                { Kind: DeckPlan.ConsoleKind.HiveRegular } spot)
            {
                return false;
            }

            UndergroundComplex.FloorPlan floor =
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                foreach (CanteenRegulars.TableSeat top in
                    CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp))
                {
                    if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                    {
                        continue;
                    }

                    // #842 · A FULL TOP REFUSES OUT LOUD — and it answers FIRST, before who is at it is even
                    // asked, because "there is nowhere to sit" is true of every table with no chair left whoever
                    // is in them. Core's own arithmetic (#840's honest Heads), never a count taken here.
                    //
                    // THE PRESS IS CONSUMED (true), which is the whole of the fix: falling through returned
                    // false, and Map.Deck's arm then raised the patron's one-breath card — so [E] at a table
                    // you cannot join quietly did a different thing instead of saying no. #603's law is that a
                    // refusal is SAID, and this is the sentence.
                    //
                    // AND NOTHING ELSE HAPPENS, EVER, however many times it is pressed. The card is not behind a
                    // second press: what is being said at a full top is something you overhear by SITTING
                    // NEARBY, which the neighbour machinery already owns, and a press that eventually gave in
                    // would teach that leaning on strangers works.
                    if (top.Free <= 0)
                    {
                        _host.ShowPulseMessage(CanteenTable.TableIsFullLine);
                        return true;
                    }

                    // #751 · WHICH TIER, off Core's own list. A background patron is a Stranger and gets the
                    // thin scene; one of the ten named regulars is matched by their plate exactly as before.
                    CanteenTable.Who who = top.Stranger
                        ? CanteenTable.Who.Stranger
                        : CanteenTable.WhoIs(top.Plate);
                    if (who == CanteenTable.Who.None || top.Plate is not { } plate)
                    {
                        return false;   // somebody who is not a scene: #709's one breath, and nothing else.
                    }

                    // #820 · WHICH CHAIR, off Core's own ring, read before the body moves. The nearest one the
                    // party is not already in — a captain waved into a seat that had somebody in it would be
                    // the drawn room and the pressed room disagreeing about a lap (#823's own complaint).
                    (double X, double Y)? chair = top.ChairYouTake(_host.AvatarX, _host.AvatarY);
                    if (chair is { } sit)
                    {
                        _host.SitCaptainOn(sit.X, sit.Y);
                    }

                    TakeThisSeat(new TableTalk
                    {
                        Key = TableKey(ex, top.Index),
                        Index = top.Index,
                        // …and standing up leaves the captain on the chair's own square. A canteen top is drawn
                        // and does not collide, so the seat is floor and nothing has to be stepped off — the
                        // square is carried all the same, because it is StandCaptainAt that gets the nudge's
                        // opinion on whether the room agrees.
                        StepOff = chair,
                        Who = who,
                        Plate = plate,
                        Scene = who == CanteenTable.Who.Stranger
                            ? CanteenTable.StrangerScene(plate)
                            : CanteenTable.SceneFor(who, ex.TableTempOverheard),
                        Seats = top.Seats,
                        Free = top.Free,
                        Bark = TheBarkAtThisTop(ex, top),
                        Quiet = top.Quiet,
                        Cabinet = top.Cabinet,
                        // #865 · SOLO IS FALSE AND THE FRAME IS STILL THE STRIP, and the two lines that are not
                        // here are the whole of the ruling. Solo stays false because somebody IS in the chair
                        // opposite — that is the occupancy the privacy ladder reads, and a top you are sharing is
                        // not a top you spread a case out on. TheyCameToYou stays false because NOBODY CAME:
                        // the captain crossed the room and asked for the chair, which is a posture change and
                        // presents in the docked strip with the hall lit behind it. Owner, at the clerk's table:
                        // "what if I just sit and eat here… now I am kind of blinded of the surrounding here
                        // because somebody else sits in the same table."
                    });
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// #757 · TAKE A FREE TABLE — the normal way to operate in a bar, and the one the room refused.
        ///
        /// <para>Owner, live in the hall: <i>"I have empty table but I cannot sit down."</i> #746's press needs a
        /// counterpart because its whole verb is <b>ask to join</b>; an empty top had no console over it at all,
        /// so [E] there answered nothing — an absence rather than a refusal, which is the one kind of "no" a
        /// player cannot read.</para>
        ///
        /// <para>SAME POSTURE, SAME GEOMETRY, and deliberately no new ones. Which table it is comes off Core's
        /// own list (<see cref="CanteenRegulars.Tables"/>), off the frozen watch, matched to the console the
        /// press landed on — a lookup rather than a decision — and #820's snap puts the captain in one of that
        /// top's own published chairs, exactly as it does at an occupied table. Not one coordinate below was
        /// measured here, which is §13.15's whole point: this project has set a captain down inside a wall twice
        /// by letting a caller do arithmetic about a room it did not carve.</para>
        /// </summary>
        public bool TryTakeTable()
        {
            if (_host.Surface is not { } ex || ex.Floor >= 0)
            {
                return false;
            }

            // Already sitting. The press is CONSUMED — E is not how you stand up, "take your leave" is.
            if (Table is not null)
            {
                return true;
            }

            if (_host.DeckPlan.NearestConsoleSpot(_host.AvatarX, _host.AvatarY) is not
                { Kind: DeckPlan.ConsoleKind.HiveTable } spot)
            {
                return false;
            }

            UndergroundComplex.FloorPlan floor =
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                foreach (CanteenRegulars.TableSeat top in
                    CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp))
                {
                    if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                    {
                        continue;
                    }

                    if (top.Taken)
                    {
                        return false;   // somebody is there after all: that is #746's press, not this one.
                    }

                    // #731 v2 · …UNLESS SOMEBODY IS HOLDING THIS DOOR OPEN FOR YOU, in which case sitting down
                    // is not taking a free table, it is FOLLOWING HER IN, and the conversation she stood up in
                    // the middle of picks up where it stopped. One branch, and everything that forks below it
                    // is a VALUE on the one record — #870 lane 6d's rule, and TakeThisSeat is still the only
                    // construction site there is.
                    if (SheLedYouHere(ex, top))
                    {
                        return true;
                    }

                    // #783 · WHICH REGISTER THIS SIT IS IN, decided once, by Core, off the room and the glass.
                    // Owner: "with a bought drink in hand, or on a quiet watch, the sit becomes the other thing."
                    // #783/#784 · ONE reading of the counter's pour, and it is #784's — Map.Seated.cs owns the
                    // window, excludes a drunk captain and is the same fact the short rest doubles its rate on.
                    // A second window here would let the panel say "cold glass" on a beat the rest engine had
                    // already decided there was no pour, which is the fault canon review caught in this scene.
                    bool drink = _host.APourInFrontOfYou;
                    bool relaxed = SittingAlone.SitReadsAsRelaxed(drink, ex.CanteenWatch);
                    Encounter.Scene sat = SittingAlone.TheTable(relaxed, drink);

                    // #820 · …and the same snap as at an occupied top, which is the point of it being one law:
                    // an empty table has every chair free, so this is simply the one the captain walked up to.
                    (double X, double Y)? chair = top.ChairYouTake(_host.AvatarX, _host.AvatarY);
                    if (chair is { } sit)
                    {
                        _host.SitCaptainOn(sit.X, sit.Y);
                    }

                    TakeThisSeat(new TableTalk
                    {
                        Key = TableKey(ex, top.Index),
                        Index = top.Index,
                        StepOff = chair,
                        Who = CanteenTable.Who.None,
                        Plate = SittingAlone.OwnTablePlate,
                        Scene = sat,
                        Seats = top.Seats,
                        // One of them is yours now. The room can see you sitting alone, which is the whole
                        // premise (#757: "sitting alone is STATE"), and the panel says it in chairs.
                        Free = Math.Max(0, top.Seats - 1),
                        Quiet = top.Quiet,
                        Cabinet = top.Cabinet,
                        Solo = true,
                        Relaxed = relaxed,
                        DrinkInHand = drink,
                        // Nobody to ask. #746's ask-to-join beat is the answer to a person, and there is not one
                        // here — the table is simply taken, and the taking is the scene's opening line.
                        Joined = true,
                        // #783 · …and it is the SCENE's opening, never a constant this method reached for. The
                        // owner's first-line law ("the panel's FIRST line must confirm the state change") is
                        // kept by the content file, and a client that pinned one of the two registers here
                        // would print the wary line over a picture of somebody's boots.
                        Outcome = sat.Opening,
                    });
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// #731 v2 · <b>YOU FOLLOWED HER IN, AND THE DOOR SHUTS BEHIND YOU.</b>
        ///
        /// <para><b>Owner, 2026-08-06:</b> <i>"Also it is dramatic telling when our contact wants us to follow
        /// them into kabinetti :-D"</i> Nobody said "follow me", nothing pulsed, no card came up and no arrow
        /// pointed at anything. She stood up in the middle of a sentence, crossed the hall, and stood in a
        /// doorway looking at you; you walked over and sat down. <b>That is the whole beat, and this method is
        /// the only thing in the game that knows it happened.</b></para>
        ///
        /// <h3>The same conversation, in a room with no ears in it</h3>
        ///
        /// <para>It is not a new sitting with a new stranger. Her plate, her scene, and — the point of the
        /// whole walk — <b>what has already been said to her</b> come back off the excursion, so the deal move
        /// she got up before making is on offer at this table and is the SAME move: #757's third rung, said
        /// where #751 has already ruled the counter has no eyes. She is the one who would not say it in a hall
        /// with eighty people in it, and now she does not have to.</para>
        ///
        /// <h3>The door is HERS</h3>
        ///
        /// <para><see cref="CabinetPrivacy.EscortsStage"/> shipped ahead of this lane saying exactly this —
        /// <i>"#731's walkers are the ones who will call this"</i> — and it is seeded on WHO she is and
        /// nothing else, so the frightened one dogs the leaf every single time and the casual one leaves the
        /// weave. A captain who meets both learns which of them is afraid without either of them saying a
        /// word, and it costs one line of code and no dialogue at all. #758's default is the curtain, and
        /// dogging it is the choice.</para>
        ///
        /// <para><b>And the panel's first line is the room's, not this lane's.</b> The opening sentence is
        /// <see cref="CabinetPrivacy.SaidOn"/> — #758's own already-shipped description of a curtain being
        /// pulled or a leaf coming out of a wall. Not one word is authored here, and nothing anywhere says
        /// why you are in a cabinet. The room says what it did; the game says nothing.</para>
        /// </summary>
        /// <returns>False — meaning "this is an ordinary free top" — whenever nobody is holding this door: no
        /// escort, a different booth, or a walk still crossing the hall, which is a captain who got there
        /// first and is simply early.</returns>
        private bool SheLedYouHere(SurfaceExcursion ex, in CanteenRegulars.TableSeat top)
        {
            if (ex.EscortCabinetTop != top.Index || top.Cabinet <= 0)
            {
                return false;
            }

            // She has to BE there. A route still being walked is not an open door, and a scene resumed over a
            // woman who is halfway across the hall would be the panel and the floor disagreeing about where
            // somebody is standing — this repository's third named bug class, in a booth.
            Walker? her = null;
            foreach (Walker w in ex.Walkers)
            {
                if (w.For == Errand.LeadingYouIn && w.Table == top.Index
                    && w.Walk.State == Core.Interior.NpcWalk.Doing.Arrived)
                {
                    her = w;
                    break;
                }
            }
            if (her is null)
            {
                return false;
            }

            string who = ex.EscortWho;
            List<string> alreadySaid = [.. ex.EscortSaid];

            // The leaf, or the weave — her call, and the hall watches whichever it is.
            CabinetPrivacy.Stage stage = CabinetPrivacy.EscortsStage(who);
            string leaf = CabinetPrivacy.Key(ex.Floor, top.Cabinet);
            if (stage == CabinetPrivacy.Stage.Door)
            {
                ex.CabinetsDogged.Add(leaf);
            }
            else
            {
                ex.CabinetsDogged.Remove(leaf);
            }

            ex.Walkers.Remove(her);
            ForgetTheEscort(ex);

            // #820 · the same snap every seat in the game sits a captain with. Done AFTER the leaf is set, so
            // the deck it rebuilds draws the door the way she left it.
            (double X, double Y)? chair = top.ChairYouTake(_host.AvatarX, _host.AvatarY);
            if (chair is { } sit)
            {
                _host.SitCaptainOn(sit.X, sit.Y);
            }

            bool drink = _host.APourInFrontOfYou;

            // #870 lane 6d · THE SEVENTH CONSTRUCTION SITE, AND IT GOES THROUGH THE ONE METHOD LIKE THE OTHER
            // SIX. The reveal cue and the draw live in `TakeThisSeat` and nowhere else, so the record is built
            // in the argument — a sitting assembled into a local and handed over afterwards is exactly what
            // `ThereIsOnePlaceASittingIsOpened` exists to refuse. What was already said to her rides in on the
            // initializer too — `TableTalk.Said` is init-settable for exactly this — so the conversation is
            // whole the first time anything reads the seat, cue and draw included.
            TakeThisSeat(new TableTalk
            {
                Key = TableKey(ex, top.Index),
                Index = top.Index,
                StepOff = chair,
                Who = CanteenTable.Who.None,
                Plate = who,
                Scene = SittingAlone.TheVisitor(),
                Seats = top.Seats,
                // Two chairs are spoken for now: yours and hers.
                Free = Math.Max(0, top.Seats - 2),
                Quiet = top.Quiet,
                Cabinet = top.Cabinet,
                Solo = false,
                // #865 · SHE CAME TO YOU. The walk moved the table and it did not move who approached whom —
                // a captain who follows somebody into a room they were led to is still the one who was asked.
                TheyCameToYou = true,
                Relaxed = SittingAlone.SitReadsAsRelaxed(drink, ex.CanteenWatch),
                DrinkInHand = drink,
                Joined = true,
                Outcome = CabinetPrivacy.SaidOn(stage),
                Said = [.. alreadySaid],
            });
            return true;
        }

        /// <summary>Stand up. Free, always, and it is the only way the panel shuts — the backdrop click and the
        /// Close button both come through here, so leaving a table is one act however you do it.
        ///
        /// <para>#820 · …and it also STEPS THE CAPTAIN OFF THE SEAT: Core's own published square, carried on the
        /// sitting (<see cref="TableTalk.StepOff"/>) rather than worked out here, and gone to through
        /// <c>StandCaptainAt</c> so the nudge has its say. That is the whole reason a solid seat — a park bench
        /// is a segment in the collision field — cannot close over the dot when the sitting ends.</para>
        ///
        /// <para>THE ORDER OF THE THREE STATEMENTS BELOW IS THE WHOLE OF THIS COMMENT. The abandon line needs
        /// the strip to land on, so the table may not go first; <c>StandCaptainAt</c> rebuilds the deck and can
        /// put a line of its own on the screen, so it may not run while the strip is still up. Watched go red as
        /// <c>THE_DIG … the table is gone before the abandon line has a strip to land on</c>.</para></summary>
        public void CloseTable()
        {
            // #784 · A SPREAD IS A SPREAD ON A TABLE. Standing up with a write-up half dug abandons it, out loud
            // and with nothing filed — the same promise every other interruption of #696's hold makes, spoken in
            // the seated register (Processing.Interruption.StoodUp). Done BEFORE the table goes, so the line
            // still has the strip to land on.
            if (_host.Surface is { Processing.Work: Core.Processing.Work.Write } ex)
            {
                _host.AbandonProcessing(ex, Core.Processing.Interruption.StoodUp);
            }

            // #820 · Read, then the table goes, then the body moves. See the summary.
            (double X, double Y)? step = Table?.StepOff;

            Table = null;

            if (step is { } spot)
            {
                _host.StandCaptainAt(spot.X, spot.Y, "you push the seat back and stand up");
            }
        }

        /// <summary>
        /// #746 · A move, pressed with the MOUSE — and the way home when it shuts the panel.
        ///
        /// <para>The seam <c>Dismiss</c> documents one file over: <i>"only the mouse needs the way home."</i> A
        /// keyboard path already owns focus; a click leaves it on the button that has just stopped existing, and
        /// the deck goes deaf — the captain presses W and nothing walks. Found by playing it: after "Take your
        /// leave" the map took no keys at all until it was clicked.</para>
        ///
        /// <para>Only when the panel actually went away. A move that keeps it up must NOT steal focus back, or
        /// tabbing through the moves would fight the map for every press.</para>
        /// </summary>
        public async Task TableMoveClicked(string moveId)
        {
            TableMove(moveId);
            if (Table is null)
            {
                await _host.RefocusMap();
            }
        }

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

        /// <summary>Is this move on offer?</summary>
        public bool TableMoveOnOffer(Encounter.Move move) =>
            _host.Surface is { } ex && Table is { } t && TableMoveAvailable(ex, t, move);

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
            if (_host.Surface is not { } ex || Table is not { } t)
            {
                return;
            }

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
        /// </summary>
        private void TableWaited(SurfaceExcursion ex, TableTalk t)
        {
            ex.TableWaits.TryGetValue(t.Key, out int beat);
            ex.TableWaits[t.Key] = beat + 1;

            // #784 · THE BEAT IS ALSO A SHORT REST. Owner: "Sitting down relaxes and heals" / "it is like short
            // rest in TTRPG." The wait already IS the seated watch-beat, so the recovery hangs off it rather
            // than off a second clock — Map.Seated.cs owns the arithmetic and the ceiling.
            string? rested = _host.RestOneSeatedBeat(ex, beat);

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
            bool comes = !t.Office
                && (!t.Bench || ParkBenches.TheOtherEndIsFree(t.SharedSeat))
                && !ex.TableApproached.Contains(t.Key)
                && (_host.ApproachCheat
                    ?? SittingAlone.SomebodyComes(
                        ex.Stop.Body.Id, ex.Floor, t.Index, ex.CanteenWatch, beat, t.Quiet));

            if (!comes)
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
                            t.CubicleKey is { Length: > 0 }
                                ? CubicleLock.NothingHappens(beat)
                                : t.Office
                                ? RingOffice.NobodyCame(beat)
                                : t.Bench
                                    ? ParkBenches.NobodyCame(beat)
                                    : SittingAlone.NobodyCame(ex.CanteenWatch, beat, t.Quiet),
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
            if (ex.Processing is { Work: Core.Processing.Work.Write })
            {
                _host.AbandonProcessing(ex, Core.Processing.Interruption.CompanyArrived);
            }

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
        /// #746/#680 · THE ONE PLACE AN OUTCOME BECOMES WORDS — and it puts them INSIDE the panel.
        ///
        /// <para>Applies everything the answer carries: the chit into the wallet, the name in somebody's book,
        /// the ask shutting, the table hardening, the fitter opening, the temp overhearing, the pips. Nothing
        /// about WHAT a band grants is decided here; a guard forces a band and pins the state that appears.</para>
        /// </summary>
        private void TableAnswered(SurfaceExcursion ex, TableTalk t, string moveId, CanteenTable.Answer said)
        {
            ex.TableMoves.Add(MoveKey(t, moveId));
            // #749 · …and the conversation's own memory, which is what an ANSWER is allowed to answer. The room
            // keeps the first for the watch; this one stands up when you do.
            t.Said.Add(moveId);

            if (said.GrantsChit)
            {
                _host.Satchel = [.. Core.Satchel.Add(_host.Satchel, CanteenTable.Chit(said.UnderAnotherName))];
            }
            if (said.ClosesTheAsk)
            {
                ex.TableAskShut.Add(t.Key);
            }
            if (said.HardensTable)
            {
                ex.TableHardened.Add(t.Key);
            }
            if (said.OpensFitter)
            {
                ex.TableFitterOpen = true;
            }
            if (said.ArmsTheTemp)
            {
                ex.TableTempOverheard = true;
            }
            if (said.TeachesTheHouse)
            {
                ex.TableHouseWays = true;
            }
            if (said.NervePips > 0)
            {
                // Through the ordinary nerve system and nothing else — the same gauge the ground bleeds, which
                // is the sanity system's own lineage saying a scene going wrong down here is frightening.
                _host.ApplyNerveShock(said.NervePips * NervePips.PipUnit, "a table you cannot read");
            }
            if (said.Note is { Length: > 0 } note)
            {
                // FileNote and NOT ShowAndFile: the saying happens in the panel, and a pulse under an open
                // modal's blur is the exact bug #680 was filed on. The book still remembers (#686).
                _host.FileNote(note, CanteenRegulars.Glyph);
            }

            // #680 · Said HERE, in the one layer the backdrop cannot blur.
            t.Outcome = said.Line;
            _host.RequestVaultSave();
            _host.StateHasChanged();

            // #731 v2 · …AND THIS IS THE MOMENT SHE MIGHT STAND UP. Last, deliberately: the line she has just
            // said stays on the panel while she crosses the hall, which is exactly what somebody leaving in
            // the middle of a conversation leaves behind them.
            SheMightLeadYouIn(ex, t);
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
