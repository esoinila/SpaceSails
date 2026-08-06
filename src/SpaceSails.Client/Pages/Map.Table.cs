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

            ShowPulseMessage(
                "🧪 DEV ?tablescene=1: the upper canteen. Walk to a table with somebody at it and press E.");
            StandCaptainAt(a.X, a.Y, "you step into the canteen");
            return;
        }
    }

    /// <summary>One conversation, with everything the panel needs to draw itself.</summary>
    private sealed class TableTalk
    {
        /// <summary>"watch:floor:tableIndex" — what every watch-scoped fact about this table is keyed on.</summary>
        public required string Key { get; init; }

        /// <summary>Which of the three is in the chair.</summary>
        public required CanteenTable.Who Who { get; init; }

        /// <summary>Their plate, exactly as it is drawn over them on the deck.</summary>
        public required string Plate { get; init; }

        /// <summary>The scene, straight off the content file.</summary>
        public required Encounter.Scene Scene { get; init; }

        /// <summary>How many the top seats, and how many chairs are still empty — the fact that let you
        /// ask to join in the first place, kept so the panel can say it.</summary>
        public required int Seats { get; init; }

        /// <summary>Chairs nobody is in.</summary>
        public required int Free { get; init; }

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

                CanteenTable.Who who = CanteenTable.WhoIs(top.Plate);
                if (who == CanteenTable.Who.None || top.Free <= 0 || top.Plate is not { } plate)
                {
                    return false;   // somebody who is not a scene, or a top with nowhere left to sit.
                }

                _table = new TableTalk
                {
                    Key = TableKey(ex, top.Index),
                    Who = who,
                    Plate = plate,
                    Scene = CanteenTable.SceneFor(who, ex.TableTempOverheard),
                    Seats = top.Seats,
                    Free = top.Free,
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
        return Encounter.Available(move, _credits, _satchel, made);
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
            CanteenTable.Answer bye = CanteenTable.TookTheirLeave();
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
                TableAnswered(ex, t, moveId,
                    CanteenTable.MadeSmallTalk(t.Who, moveId == CanteenTable.SmallTalkAgain));
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

            case CanteenTable.DodgeScaffold:
                // THE DODGE IS FREE. No coin, no pip, no hardening, no flag — the answer Core hands back has
                // every field at its default and this branch adds nothing to it. The owner smoke-tests this
                // by hand; a dodge that quietly cost something would pass every test in the repo.
                TableAnswered(ex, t, moveId, CanteenTable.ScaffoldDodged());
                return;

            case CanteenTable.Work:
                TableAsksAboutWork(ex, t, move);
                return;

            default:
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

        CanteenTable.Answer said = CanteenTable.PutOnTheTable(item, t.Who);
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
