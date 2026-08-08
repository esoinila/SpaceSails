using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #746 · THE TABLE SCENE — sitting down with somebody, and the moves you have once you have.
///
/// <para>Owner, 2026-08-06 (work-break brief): <i>"bar interaction is the next big lane. Asking to sit is
/// missing... sit-down and drink-offer — in both directions — are how contact with new people begins."</i>
/// And the proto, in his own words: <i>"with all the charm we can muster, get the job that takes us
/// downstairs — and politely dodge the jobs that don't."</i></para>
///
/// <h3>What lives here and what deliberately does not</h3>
///
/// <para>This file opens a panel, presses buttons and applies the answers Core hands back. It decides
/// NOTHING. Which moves exist, what they cost, which are rolled, what a band grants, what anybody says —
/// all of it is <see cref="Encounter"/> and <see cref="CanteenTable"/>, because the day the guard stop
/// arrives it has to be a content file rather than a second copy of this method.</para>
///
/// <h3>#680's law, applied from birth</h3>
///
/// <para>Every outcome is stored on the open scene and rendered INSIDE the panel's own subtree. Not one of
/// them is pulsed: the pulse HUD renders under the modal backdrop and its blur, which is where #680 was
/// filed from, and this panel is up for the whole conversation. The field book still gets its notes through
/// <c>FileNote</c> — the #686 half — because the book must remember what the pulse never said.</para>
/// </summary>
public partial class Map
{
    /// <summary>The open table, or null. One at a time: you are sitting at it.</summary>
    private TableTalk? _table;

    /// <summary>#746 QA · <c>?roll=hi|lo</c> — force every band this session. It overrides the BAND and
    /// never the roll, so the dice still cast and the on-screen math still reads truthfully; what a tester
    /// watches play out is the scene a captain would get.</summary>
    private Encounter.Band? _rollCheat;

    /// <summary>#746 QA · <c>?tablescene=1</c> — the whole route, booted. Set in Map.Sim's cheat parse; read
    /// once, by the landing, to walk the last leg into the canteen.</summary>
    private bool _tableSceneCheat;

    /// <summary>#757 QA · <c>?tablescene=free</c> — the same route, but the last leg lands you at a top with
    /// NOBODY at it, which is the table this whole issue is named after.</summary>
    private bool _freeTableCheat;

    /// <summary>#751 QA · <c>?watch=N</c> — pin which shift the hall is on, so a tester can walk into the
    /// heaving one and the empty one without waiting four sim-hours between looks. Null off the cheat, in
    /// which case the watch is <see cref="PatronRota.WatchIndex"/> of the sim clock exactly as before.</summary>
    private long? _watchCheat;

    /// <summary>#757 QA · <c>?approach=1|0</c> — force the answer to WAITING at a table you took alone.
    ///
    /// <para>1 brings somebody over on the very next wait; 0 means nobody ever comes, which is the OTHER
    /// half of the feature and just as much a scene: an empty room on the wrong watch is the event. Without
    /// this the approach is a seeded roll at one top on one shift, and "sit down and press wait until the
    /// dice agree" is not a demo. #693's own rule, written in the file that then could not follow it: <i>a
    /// scene nobody can reach on demand is a scene that ships broken.</i></para>
    ///
    /// <para>It forces WHETHER, never WHO or WHAT — the ladder, her lines and what she wants are the ones a
    /// captain would get, because a cheat that showed a different scene is worse than no cheat.</para></summary>
    private bool? _approachCheat;

    /// <summary>
    /// #746 QA · Stand the captain IN the upper canteen when <c>?tablescene=1</c> asked for it.
    ///
    /// <para>ASKED OF THE BUILDING, not retyped. The room's centre is the amenity Core carved (#707) — the
    /// same coordinate the fixture console sits on, which is by construction clear of the counter it laid
    /// against the back wall. A cheat that typed its own spot in a room it does not own is §13.15's second
    /// cause, and this project has been set down inside a wall by exactly that mistake twice.</para>
    /// </summary>
    private void StandInTheCanteenIfAsked(SurfaceExcursion ex)
    {
        if (!_tableSceneCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (a.Use != UndergroundComplex.Comfort.UpperCanteen)
            {
                continue;
            }

            // #757 · …or AT A FREE TOP, for ?tablescene=free. Which top that is comes off the very same
            // call the deck was drawn with, off the same frozen watch — the cheat picks one of the room's
            // own empty tables, and never a coordinate of its own (§13.15: the two times this project set
            // the captain down inside a wall, it was a caller typing geometry about a room it did not own).
            if (_freeTableCheat && FirstFreeTop(ex, a) is { } free)
            {
                ShowPulseMessage(
                    "🧪 DEV ?tablescene=free: a table with nobody at it. Press E to take it, then Wait.");
                StandCaptainAt(free.X, free.Y, "you step into the canteen");
                return;
            }

            ShowPulseMessage(
                "🧪 DEV ?tablescene=1: the upper canteen. Walk to a table with somebody at it and press E.");
            StandCaptainAt(a.X, a.Y, "you step into the canteen");
            return;
        }
    }

    /// <summary>#757 QA · The first top in this room that nobody is at, this watch — Core's own list, so the
    /// cheat cannot disagree with the room about which tables are free.</summary>
    private static CanteenRegulars.TableSeat? FirstFreeTop(SurfaceExcursion ex, UndergroundComplex.Amenity a)
    {
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
        {
            if (!top.Taken)
            {
                return top;
            }
        }
        return null;
    }

    /// <summary>One conversation, with everything the panel needs to draw itself.</summary>
    private sealed class TableTalk
    {
        /// <summary>"watch:floor:tableIndex" — what every watch-scoped fact about this table is keyed on.</summary>
        public required string Key { get; init; }

        /// <summary>#757 · Which top this is, as Core's own ordinal. The key already carries it, and this
        /// carries it as a NUMBER because the approach roll is seeded on it — parsing an ordinal back out of
        /// a string we built ourselves is a second answer to a question we already had.</summary>
        public required int Index { get; init; }

        /// <summary>Which of the three is in the chair.</summary>
        public required CanteenTable.Who Who { get; init; }

        /// <summary>#757 · Their plate, exactly as it is drawn over them on the deck — or, at a table you
        /// took alone, whose table it is. Settable because ONE SITTING CAN CHANGE COUNTERPART: you sit down
        /// on your own, you wait, and somebody crosses the room and takes the chair opposite. That is one
        /// continuous occupation of one table, not two panels, and the alternative — closing this one and
        /// opening another — would blink the answer the player is reading off the screen (#680).</summary>
        public required string Plate { get; set; }

        /// <summary>The scene, straight off the content file. Settable for the same reason
        /// <see cref="Plate"/> is: waiting is a scene that can turn into a different scene at the same
        /// table.</summary>
        public required Encounter.Scene Scene { get; set; }

        /// <summary>#757 · Whether this is a table you took ALONE — nobody opposite, and WAIT is the whole
        /// of the verb. It is the one fact the panel needs that is not on the scene.</summary>
        public bool Solo { get; set; }

        /// <summary>How many the top seats, and how many chairs are still empty — the fact that let you
        /// ask to join in the first place, kept so the panel can say it.</summary>
        public required int Seats { get; init; }

        /// <summary>Chairs nobody is in. #757 · Settable, because the captain sitting down is one of them
        /// and somebody joining them is another — occupancy the player can count.</summary>
        public required int Free { get; set; }

        /// <summary>#751 · Their bark, for a background patron — drawn by Core per patron per watch, and
        /// carried here because it is the only thing a stranger has to say.</summary>
        public string? Bark { get; init; }

        /// <summary>#751 · Whether this table is in a CABINET, which is the one fact the quiet rule reads.
        /// Core's own (<see cref="CanteenRegulars.TableSeat.Quiet"/>) — a client flag would be a second
        /// answer to a question about a room the client does not own.</summary>
        public bool Quiet { get; init; }

        /// <summary>#680 · What the last move answered, said HERE and nowhere else.</summary>
        public string? Outcome { get; set; }

        /// <summary>The dice, spelled out, when a roll decided it — §5.0's whole homage is that the player
        /// watches the numbers add up.</summary>
        public string? Math { get; set; }

        /// <summary>Whether the satchel sub-list is fanned open under "put something on the table".</summary>
        public bool Showing { get; set; }

        /// <summary>#746 · Whether the captain has actually ASKED yet.
        ///
        /// <para>The press puts you at the table; asking to join is its own beat, because that is the beat
        /// the owner said was missing (<i>"asking to sit is missing"</i>). No roll — sitting is cheap in bar
        /// culture and most working people wave you in — but it is a thing you DO, and the wave-in is the
        /// answer to it rather than a caption that was always there. A panel that opened straight into the
        /// moves would have skipped the only part of this scene the issue is named after.</para></summary>
        public bool Joined { get; set; }

        /// <summary>#749 · What has been said in THIS sitting, by move id.
        ///
        /// <para>Deliberately beside <c>ex.TableMoves</c> rather than instead of it, because they are two
        /// different facts and the bug was reading one for the other. The excursion's set is what the ROOM
        /// remembers for the watch — a round stood, an ask fumbled, a file put down — and it must outlive
        /// standing up. This one is the CONVERSATION, and it dies with the panel: you cannot answer an offer
        /// that was made before you left the table, because the man made it to somebody who then stood up.</para></summary>
        public HashSet<string> Said { get; } = [];
    }

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
    private bool TryOpenTable()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting there. The press is CONSUMED and nothing happens — re-opening would wipe the
        // outcome line the captain is in the middle of reading, and E is not how you stand up: "Take your
        // leave" is, because leaving a table right is a thing this scene has an opinion about.
        if (_table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveRegular } spot)
        {
            return false;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                // #751 · WHICH TIER, off Core's own list. A background patron is a Stranger and gets the
                // thin scene; one of the ten named regulars is matched by their plate exactly as before.
                CanteenTable.Who who = top.Stranger
                    ? CanteenTable.Who.Stranger
                    : CanteenTable.WhoIs(top.Plate);
                if (who == CanteenTable.Who.None || top.Free <= 0 || top.Plate is not { } plate)
                {
                    return false;   // somebody who is not a scene, or a top with nowhere left to sit.
                }

                _table = new TableTalk
                {
                    Key = TableKey(ex, top.Index),
                    Index = top.Index,
                    Who = who,
                    Plate = plate,
                    Scene = who == CanteenTable.Who.Stranger
                        ? CanteenTable.StrangerScene(plate)
                        : CanteenTable.SceneFor(who, ex.TableTempOverheard),
                    Seats = top.Seats,
                    Free = top.Free,
                    Bark = top.Line,
                    Quiet = top.Quiet,
                };
                RendererInterop.PlayCue("reveal");
                StateHasChanged();
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
    /// <para>SAME POSTURE, SAME GEOMETRY, and deliberately no new ones. The seat is the spot you walked to in
    /// order to press the key, exactly as it is at an occupied table — this file has never moved the captain
    /// to sit down, and a solo table that teleported them onto the furniture would be §13.15's second cause
    /// in the one room where the tops are drawn but do not collide. Which table it is comes off Core's own
    /// list (<see cref="CanteenRegulars.Tables"/>), off the frozen watch, matched to the console the press
    /// landed on — the same lookup, and still a lookup rather than a decision.</para>
    /// </summary>
    private bool TryTakeTable()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting. The press is CONSUMED — E is not how you stand up, "take your leave" is.
        if (_table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveTable } spot)
        {
            return false;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                if (top.Taken)
                {
                    return false;   // somebody is there after all: that is #746's press, not this one.
                }

                _table = new TableTalk
                {
                    Key = TableKey(ex, top.Index),
                    Index = top.Index,
                    Who = CanteenTable.Who.None,
                    Plate = SittingAlone.OwnTablePlate,
                    Scene = SittingAlone.TheTable(),
                    Seats = top.Seats,
                    // One of them is yours now. The room can see you sitting alone, which is the whole
                    // premise (#757: "sitting alone is STATE"), and the panel says it in chairs.
                    Free = Math.Max(0, top.Seats - 1),
                    Quiet = top.Quiet,
                    Solo = true,
                    // Nobody to ask. #746's ask-to-join beat is the answer to a person, and there is not one
                    // here — the table is simply taken, and the taking is the scene's opening line.
                    Joined = true,
                    Outcome = SittingAlone.TookTheTableLine,
                };
                RendererInterop.PlayCue("reveal");
                StateHasChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>Stand up. Free, always, and it is the only way the panel shuts — the backdrop click and the
    /// Close button both come through here, so leaving a table is one act however you do it.</summary>
    private void CloseTable() => _table = null;

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
    private async Task TableMoveClicked(string moveId)
    {
        TableMove(moveId);
        if (_table is null)
        {
            await RefocusMap();
        }
    }

    // ── THE SITUATION THE DICE SEE ────────────────────────────────────────────────────────────────────

    /// <summary>The last ten minutes, as facts. Every one of them is something the player did and could
    /// narrate back — which is the difference between this and a character sheet.</summary>
    private Encounter.Situation TableSituation(SurfaceExcursion ex, TableTalk t) => new(
        RoundBought: ex.TableRounds.Contains(t.Key),
        PaperShown: ex.TableMoves.Contains(MoveKey(t, CanteenTable.Show + ":relevant")),
        HouseWaysLearned: ex.TableHouseWays,
        NerveMarked: Encounter.NerveReadsAcrossATable(_nerve),
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
        return Encounter.Available(move, _credits, _satchel, made, t.Said);
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

    /// <summary>#749 · The moves that are ON THE TABLE — the ones that exist to be pressed at all. Core's
    /// own call, so the day a checkpoint renders through this same block it cannot have a different idea of
    /// when an answer exists. Everything the captain has simply not earned is still HERE and still refused
    /// out loud (#603); what is missing is only what nobody has said yet.</summary>
    private IReadOnlyList<Encounter.Move> TableMovesOnTheTable() =>
        _table is { } t ? Encounter.OnTheTable(t.Scene, t.Said) : [];

    /// <summary>Is this move on offer?</summary>
    private bool TableMoveOnOffer(Encounter.Move move) =>
        _surface is { } ex && _table is { } t && TableMoveAvailable(ex, t, move);

    /// <summary>Why not, said out loud on the disabled control.</summary>
    private string TableMoveRefusal(Encounter.Move move) =>
        _surface is { } ex && _table is { } t ? TableMoveWhyNot(ex, t, move) : "Not yet.";

    /// <summary>#746 · Is the game NUDGING you at this move? Exactly one does: the fitter's ask, after the
    /// hand has waved you toward it. That is the NO-AND's "another door opens in the conversation" made
    /// visible — the scene moved, and the panel should look like it moved.</summary>
    private bool TableMoveIsUrged(Encounter.Move move) =>
        _surface is { } ex && _table is { Who: CanteenTable.Who.Fitter } t
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
        if (_surface is not { } ex || _table is not { } t)
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
            ShowPulseMessage(bye.Line);
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
                _credits -= move.Credits;
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
                    BackToYourOwnTable(t);
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
        Encounter.Band band = Encounter.Settle(roll, _rollCheat);

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

        // One approach per top per watch. She came over, and whichever way that went, it went.
        bool comes = !ex.TableApproached.Contains(t.Key)
            && (_approachCheat
                ?? SittingAlone.SomebodyComes(
                    ex.Stop.Body.Id, ex.Floor, t.Index, ex.CanteenWatch, beat, t.Quiet));

        if (!comes)
        {
            // #680/#736 · NOTHING HAPPENING IS AN ANSWER, and it is said on the panel the captain pressed,
            // through the one ending every other answer at this table uses. A wait that produced silence
            // and no words would be indistinguishable from a control that is broken (#603).
            TableAnswered(ex, t, SittingAlone.Wait,
                new CanteenTable.Answer(SittingAlone.NobodyCame(ex.CanteenWatch, beat, t.Quiet)));
            return;
        }

        ex.TableApproached.Add(t.Key);
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
        t.Solo = false;
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
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>#757 · She goes, and the table is yours again. The outcome line stays exactly as it was —
    /// what she said on the way out is the last thing that happened, and it must not be wiped by the state
    /// change that follows it.</summary>
    private void BackToYourOwnTable(TableTalk t)
    {
        t.Solo = true;
        t.Plate = SittingAlone.OwnTablePlate;
        t.Scene = SittingAlone.TheTable();
        t.Said.Clear();
        t.Showing = false;
        t.Free = Math.Min(t.Seats, t.Free + 1);
        StateHasChanged();
    }

    /// <summary>
    /// #746 · Put something on the table. The satchel as a conversational move: papers make hands nervous,
    /// an authority card makes a table quiet, a file on somebody is LOUD.
    /// </summary>
    private void TableShow(Core.Satchel.Item item)
    {
        if (_surface is not { } ex || _table is not { } t)
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
            _satchel = [.. Core.Satchel.Add(_satchel, CanteenTable.Chit(said.UnderAnotherName))];
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
            ApplyNerveShock(said.NervePips * NervePips.PipUnit, "a table you cannot read");
        }
        if (said.Note is { Length: > 0 } note)
        {
            // FileNote and NOT ShowAndFile: the saying happens in the panel, and a pulse under an open
            // modal's blur is the exact bug #680 was filed on. The book still remembers (#686).
            FileNote(note, CanteenRegulars.Glyph);
        }

        // #680 · Said HERE, in the one layer the backdrop cannot blur.
        t.Outcome = said.Line;
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE SATCHEL, AS SEEN FROM A TABLE ─────────────────────────────────────────────────────────────

    /// <summary>What the captain could put on the table. Everything they are carrying — the gesture is
    /// "put something down", and a pocket that hid the wrong answers would be hinting at the right one.</summary>
    private IReadOnlyList<Core.Satchel.Item> TableShowables() => _satchel;

    // ── #743/#746 · THE MESS, WITH THE CHIT IN YOUR HAND ──────────────────────────────────────────────
    //
    // The chit is a wallet card, and a wallet card that only ever satisfied a future guard would be a token
    // in a ledger nobody sees. So it has ONE visible payoff now, in the room the pass exists for: B17's staff
    // mess (#743), which says PASS TO BE SHOWN on the door and has nobody behind it to show anything to.
    //
    // ADDITIVE, never a replacement: #743's own room card still fires first on first entry, because the find
    // is the room and this is what you do in it. Once per excursion, in the DEAD AIR family.

    /// <summary>Raise the chit beat if the captain is carrying cover and standing in the mess. Called by the
    /// staff-mess poll, after that room's own card has had its turn.</summary>
    private void ShowMessChitBeat(SurfaceExcursion ex)
    {
        if (ex.MessChitBeatShown || !CanteenTable.Cover.Held(_satchel))
        {
            return;
        }

        ex.MessChitBeatShown = true;
        ShowAndFile(CanteenTable.MessBeatLine, CanteenTable.ChitGlyph);
        ApplyNerveRelief(CanteenTable.MessBeatPips * NervePips.PipUnit);
        RequestVaultSave();
    }
}
