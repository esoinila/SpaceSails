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
/// <c>Map.Seated.cs</c>'s short-rest banner and has exactly one caller, the wait beat. The short rest
/// itself stayed on the page — it spends the nerve and condition systems — and the seat asks it for a
/// sentence (<see cref="ISeatHost.RestOneSeatedBeat"/>); this is only the composer that puts that sentence
/// after the room's own answer.</para>
///
/// <para><b>#251 · THE SCENE IS FIVE FILES NOW</b>, cut on the banners it had already written for itself,
/// and this one keeps the first of them. <c>Seating.Table.cs</c> — OPENING THE SCENE: the two watch-scoped
/// keys, joining a top, taking one she led you to, and the teardown every stand-up goes through.
/// <c>Seating.Table.Moves.cs</c> — the situation the dice see, what the panel asks, and making a move.
/// <c>Seating.Table.Wait.cs</c> — #757's wait beat and who comes of it. <c>Seating.Table.Cabinet.cs</c> —
/// #758's curtain and the door, and the contact who asks you to follow her through one.
/// <c>Seating.Table.Answered.cs</c> — #680's one place an outcome becomes words, which is a law rather than
/// a section and so has a file to itself.</para>
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
                    CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn))
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
                    CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn))
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
        /// <c>THE_DIG … the table is gone before the abandon line has a strip to land on</c>.</para>
        ///
        /// <para><b>#784 · STANDING UP WITH A WRITE-UP HALF DUG ABANDONS IT</b>, out loud and with nothing
        /// filed — the same promise every other interruption of #696's hold makes, spoken in the seated
        /// register (<c>Processing.Interruption.StoodUp</c>). Done BEFORE the table goes, so the line still has
        /// the strip to land on.</para>
        ///
        /// <para>#1016 · <b>…AND IT ASKS THE HOLD RATHER THAN A GROUND.</b> This read <c>_host.Surface is
        /// { Processing.Work: Write }</c>, which made "was the captain digging" a question about a MOON: at the
        /// eighth seat (#973 L5b, a top in a docked bar) there is no excursion, so standing up out of a
        /// half-dug sheet ended the dig silently and left a clock running over a sitting that no longer
        /// existed. The work this seam ends is named in the ask now, and the page answers it.</para></summary>
        public void CloseTable()
        {
            // #784/#1016 · A SPREAD IS A SPREAD ON A TABLE, wherever the table is. See the summary.
            _host.AbandonProcessing(Core.Processing.Work.Write, Core.Processing.Interruption.StoodUp);

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
    }
}
