using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #756 · SITTING AT THE COUNTER — the stool, and the one on the next stool.
///
/// <para>Owner, live playtest 2026-08-08: <i>"Also there should be high chairs so sitting at the bar desk is
/// also possible."</i> And, filing the design: <i>"the #757 seat verb on the counter fixture (one E,
/// pick-or-default a free stool), the keep serves you seated (same #756 menu seam), and the neighbor may
/// open the approach ladder from the NEXT STOOL without asking to sit — they are already seated, proximity
/// IS the invitation."</i></para>
///
/// <h3>THE GRAMMAR, AND WHY IT IS THIS ONE</h3>
///
/// <para>#757's law is ONE [E] PER FIXTURE. The counter already owns its press — E there opens the service
/// card — and a row of eight stool consoles bolted along the same five deck-units would have put nine
/// press-targets inside one arm's length of each other, in the one room where the tops are already dotted
/// with consoles. A captain aiming for a drink would have hit a chair.</para>
///
/// <para>So the stool is not a second fixture. <b>It is a POSTURE of the one that is already there.</b> E at
/// the counter opens the card; the card carries <i>Take a stool</i>; taking one changes what the card IS
/// without changing which card it is. That is what makes <i>"the keep serves you seated"</i> true rather than
/// arranged: the menu does not have to be re-opened, re-plumbed or re-priced from a stool, because it never
/// went anywhere. The same six items, the same <c>BuyDrink</c>, the same purse, the same receipt slot
/// (<c>_barNotice</c>) that #736 was fought over — one card, two things you can be doing at it.</para>
///
/// <para>The alternative — a stool that opens the TABLE panel — was rejected for the reason the owner's own
/// sentence rules out: the menu would then live on the card you are NOT on, and ordering a drink from a bar
/// stool would mean shutting the bar. Two cards flip-flopping is the shape #680 and #736 were both filed
/// against.</para>
///
/// <h3>What is Core's and what is here</h3>
///
/// <para>Everything that decides anything is <see cref="TheStools"/> and <see cref="Encounter"/>: which
/// seats are taken, which is free, whether anybody is beside you, whether they speak this beat, what they
/// say, what a rung costs and which rung the field book keeps. This file counts beats, spends coin, and puts
/// the answer in the slot the card already draws. It decides nothing.</para>
///
/// <para>#680/#736 · Not one outcome here pulses. The card is up for the whole sitting, and a pulse raised
/// under it plays behind its own blur.</para>
/// </summary>
public partial class Map
{
    /// <summary>The stool you are on, or null while you are standing at the counter. One at a time — there
    /// is only one of you.</summary>
    private StoolSeat? _stool;

    /// <summary>#756 QA · <c>?stool=1</c> — walk to the counter, open the card and TAKE A STOOL, so the
    /// seated posture is one URL away. Set in Map.Sim's cheat parse; read by the landing's last leg.</summary>
    private bool _stoolCheat;

    /// <summary>#756 QA · <c>?neighbour=1|0</c> — force the answer to WAITING on a stool.
    ///
    /// <para>1 turns the one beside you on the very next wait; 0 means nobody ever does, which is the OTHER
    /// half of the feature and just as much a scene — a counter where the seat next to you stays quiet is
    /// the thing the room is saying. Without it the beat is a seeded roll behind a seeded occupancy on one
    /// shift, and "sit down and press wait until the dice agree" is not a demo (#693's rule).</para>
    ///
    /// <para>It forces WHETHER, never WHO or WHAT: the ladder, her lines and what she wants are the ones a
    /// captain would get.</para></summary>
    private bool? _neighbourCheat;

    /// <summary>One sitting on one stool. The scene is swapped in place when the neighbour speaks, exactly
    /// as the table's is (#757) — one continuous occupation of one seat, so the sentence the captain is
    /// mid-way through reading is never blinked away by a card closing and re-opening.</summary>
    private sealed class StoolSeat
    {
        /// <summary>Which seat, 0-based — Core's own ordinal, never a pair of doubles.</summary>
        public int Index { get; init; }

        /// <summary>The watch-scoped key this seat's beats and its one approach are remembered under.</summary>
        public string Key { get; init; } = "";

        /// <summary>What is on offer right now: your own stool, or the neighbour's ladder.</summary>
        public Encounter.Scene Scene { get; set; }

        /// <summary>#749 · What has been SAID in this sitting — the only thing a reply can be a reply to.</summary>
        public List<string> Said { get; } = [];

        /// <summary>Whether the neighbour is talking to you. False on your own stool.</summary>
        public bool WithNeighbour { get; set; }
    }

    /// <summary>The key this stool's beats and its one approach are filed under. Watch-scoped like every
    /// other fact about this room, so a new shift is a new evening at the bar.</summary>
    private static string StoolKey(SurfaceExcursion ex, int stool) =>
        $"stool:{ex.Floor}:{ex.CanteenWatch}:{stool}";

    // ── TAKING ONE, AND GETTING OFF ───────────────────────────────────────────────────────────────────

    /// <summary>Is there a stool to take right now? The card asks this to decide whether to draw the verb —
    /// and it draws it either way when the counter has stools at all, because #212's law is that an
    /// affordance never hides: a full row REFUSES OUT LOUD (<see cref="TheStools.RowIsFullLine"/>) rather
    /// than vanishing and leaving a captain to wonder whether stools exist.</summary>
    private bool CounterHasStools() =>
        _surface is { Floor: < 0 } ex
        && _barMenu is { } keep
        && CounterService.For(ex.Stop.Body.Id, UndergroundComplex.Comfort.UpperCanteen) is { } serves
        && serves.BodyId == keep.BodyId;

    /// <summary>
    /// #756 · TAKE A STOOL — one press, pick-or-default.
    ///
    /// <para>Which seat is free is Core's answer off the frozen watch (<see cref="TheStools.FirstFreeStool"/>),
    /// so the row a captain lands on is the row the room has. A full row is an answer with words on it and
    /// never a control that did nothing.</para>
    /// </summary>
    private void TakeAStool()
    {
        if (_surface is not { } ex || ex.Floor >= 0 || _stool is not null)
        {
            return;
        }

        if (TheStools.FirstFreeStool(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch) is not { } seat)
        {
            _barNotice = TheStools.RowIsFullLine;
            return;
        }

        _stool = new StoolSeat
        {
            Index = seat,
            Key = StoolKey(ex, seat),
            Scene = TheStools.TheStool(seat),
        };
        _barNotice = TheStools.TookAStoolLine;
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Get down. Free, always — and it leaves you STANDING at the counter rather than shutting the
    /// card, because getting off a stool is not leaving a bar.</summary>
    private void GetDownFromStool()
    {
        _stool = null;
        _barNotice = TheStools.GotDownLine;
        StateHasChanged();
    }

    // ── THE MOVES ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#749 · What is on the stool's panel right now. Core's own call, so a reply to a sentence
    /// nobody has spoken is not drawn at all.</summary>
    private IReadOnlyList<Encounter.Move> StoolMovesOnTheTable() =>
        _stool is { } s ? Encounter.OnTheTable(s.Scene, s.Said) : [];

    /// <summary>Can this move be made? Core's requirement check, plus the one fact Core's
    /// <see cref="Encounter.Available"/> cannot fold into a REPLY: a rung that costs coin needs the coin.
    /// Drawn and REFUSED (#212/#603), never silently missing.</summary>
    private bool StoolMoveOnOffer(Encounter.Move move) =>
        _stool is { } s
        && Encounter.Available(move, _credits, _satchel, s.Said, s.Said)
        && _credits >= move.Credits;

    /// <summary>Why a drawn move is refused, in words. The only thing that can refuse one here is money.</summary>
    private string StoolMoveRefusal(Encounter.Move move) =>
        $"{move.Credits} cr, and you have {_credits}.";

    /// <summary>A move, pressed with the MOUSE — and the way home when it shuts the panel. The seam
    /// <c>Dismiss</c> documents: a click leaves focus on a button that has just stopped existing, and the
    /// deck goes deaf.</summary>
    private async Task StoolMoveClicked(string moveId)
    {
        StoolMove(moveId);
        if (_stool is null && _barMenu is null)
        {
            await RefocusMap();
        }
    }

    /// <summary>
    /// #756 · One press on the stool's panel.
    ///
    /// <para>WAIT is its own branch, because what it says is decided by the room at the moment it is pressed.
    /// Everything else is a FIXED OUTCOME that speaks because the move CARRIES a line (#749) — never because
    /// somebody wrote its id into a switch down here, which is the bug #750 was filed for.</para>
    /// </summary>
    private void StoolMove(string moveId)
    {
        if (_surface is not { } ex || _stool is not { } s)
        {
            return;
        }

        if (moveId == TheStools.Wait)
        {
            StoolWaited(ex, s);
            return;
        }

        Encounter.Move move = default;
        bool found = false;
        foreach (Encounter.Move m in s.Scene.Moves)
        {
            if (m.Id == moveId)
            {
                (move, found) = (m, true);
                break;
            }
        }
        if (!found || !StoolMoveOnOffer(move))
        {
            return;
        }

        if (moveId == TheStools.GetDown)
        {
            GetDownFromStool();
            return;
        }

        // Bought BEFORE the answer, because the answer IS the glass arriving. One number: the move's own,
        // which is CounterService.HouseRate, which is what the card under the glass charges for that pour.
        if (move.Credits > 0)
        {
            _credits -= move.Credits;
            RequestVaultSave();
        }

        s.Said.Add(moveId);
        if (move.Says is { Length: > 0 } said)
        {
            _barNotice = said;
        }

        // …and the ONE rung the field book keeps. The note rides the move, so no author here has to remember
        // which sentence was worth writing down.
        if (move.Note is { Length: > 0 } note)
        {
            FileNote(note, TheStools.Glyph);
            RequestVaultSave();
        }

        StateHasChanged();
    }

    // ── WAITING, AND WHETHER SHE TURNS ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #756 · One beat of sitting there. Core decides whether the one beside you speaks; this counts the
    /// beats and applies the answer.
    ///
    /// <para>THE BEAT COUNTER IS THE ROOM'S (<c>ex.TableWaits</c>, the same dictionary the tops use, under a
    /// stool key), and that is the anti-abuse law #757 already wrote: the roll is seeded on the beat, so a
    /// captain who got down and back up again simply carries on from the beat they were on. There is no
    /// re-pressing your way into company at a bar either.</para>
    ///
    /// <para>ONE APPROACH PER STOOL PER WATCH. She turned to you, and whichever way that went, it went.</para>
    ///
    /// <para>AND THE WATCH IS NOT TOUCHED — a wait is a beat inside the frozen shift (#709), never a nudge to
    /// the clock. A wait that re-dated the room would make the drawn room and the pressed room two rooms.</para>
    /// </summary>
    private void StoolWaited(SurfaceExcursion ex, StoolSeat s)
    {
        ex.TableWaits.TryGetValue(s.Key, out int beat);
        ex.TableWaits[s.Key] = beat + 1;

        bool turns = !ex.TableApproached.Contains(s.Key)
            && (_neighbourCheat
                ?? TheStools.SomebodyTurns(ex.Stop.Body.Id, ex.Floor, s.Index, ex.CanteenWatch, beat));

        if (!turns)
        {
            // NOTHING HAPPENING IS AN ANSWER, and it is said on the card the captain pressed. Which silence
            // it is depends on whether anybody is actually beside you — Core's own question, asked of Core,
            // and the difference between "nobody could have spoken" and "somebody could, and did not".
            _barNotice = TheStools.NobodySpoke(
                ex.CanteenWatch, beat,
                TheStools.HasNeighbour(ex.Stop.Body.Id, ex.Floor, s.Index, ex.CanteenWatch));
            StateHasChanged();
            return;
        }

        ex.TableApproached.Add(s.Key);
        SheTurnsToYou(s);
    }

    /// <summary>
    /// #756 · SHE TURNS — and she does not ask to sit, because she is sitting.
    ///
    /// <para>THE SAME CARD, not a second one. Swapping the scene in place keeps the outcome the captain is
    /// mid-way through reading on the screen, and keeps the menu under it exactly where it was — which is
    /// what "the keep serves you seated" costs in code, and it is nothing.</para>
    /// </summary>
    private void SheTurnsToYou(StoolSeat s)
    {
        s.WithNeighbour = true;
        s.Scene = TheStools.TheNeighbour();
        s.Said.Clear();     // #749 · a new conversation: nothing has been said to HER yet.

        // #761 · Told clearly, on the surface the captain is looking at. She spoke; the game prints what she
        // said and does not leave her arrival to be inferred from a changed button row.
        _barNotice = s.Scene.Opening;
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }
}
