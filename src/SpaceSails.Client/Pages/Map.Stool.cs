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

    // -- #870 lane 6c · THE FORWARDERS, AND EVERY ONE OF THEM HAS A CALLER OUTSIDE THIS FAMILY --------
    //
    // The counter's seat lives on Seating now (Seating.Stool.cs). These six are the spellings the rest of
    // the page still asks for BY NAME, measured rather than assumed: four of them are the bar card in
    // Map.razor drawing its own two postures, one is the ?stool=1 row in Map.Surface.Cheats.cs, and one is
    // the card SHUTTING in Map.Quests.Bar.cs -- which is not a getting-down at all, and says so where it is
    // declared. Getting down, the moves, the beat and the turn kept no forwarder: nothing outside the seat
    // has ever called them.

    /// <inheritdoc cref="Seating.CounterHasStools"/>
    private bool CounterHasStools() => _seating.CounterHasStools();

    /// <inheritdoc cref="Seating.TakeAStool"/>
    private void TakeAStool() => _seating.TakeAStool();

    /// <inheritdoc cref="Seating.LeaveTheStoolBehind"/>
    private void LeaveTheStoolBehind() => _seating.LeaveTheStoolBehind();

    /// <inheritdoc cref="Seating.StoolMoveOnOffer"/>
    private bool StoolMoveOnOffer(Encounter.Move move) => _seating.StoolMoveOnOffer(move);

    /// <inheritdoc cref="Seating.StoolMoveRefusal"/>
    private string StoolMoveRefusal(Encounter.Move move) => _seating.StoolMoveRefusal(move);

    /// <inheritdoc cref="Seating.StoolMoveClicked"/>
    private Task StoolMoveClicked(string moveId) => _seating.StoolMoveClicked(moveId);
}
