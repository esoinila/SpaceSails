using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #251 · Split from Map.Table.cs, moved verbatim: the TableTalk conversation (one sitting, drawn by the panel),
// #743/#746's mess-chit beat, and #870 lane 6c's forwarders. The scene's fields stay in Map.Table.cs.
public partial class Map
{
    /// <summary>One conversation, with everything the panel needs to draw itself.</summary>
    public sealed class TableTalk
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
        /// of the verb. It is the one fact the panel needs that is not on the scene.
        ///
        /// <para>#865 · IT IS STILL THE OCCUPANCY FACT AND IT IS NO LONGER THE FRAME FACT. It is what
        /// <c>SeatedAlone</c> reads for the privacy ladder — a top with the weighbridge clerk's tray on it is
        /// not a seat you lay evidence out on, whoever chose to sit there — but which FRAME the sitting wears
        /// is <see cref="TheyCameToYou"/> now, because the owner ruled that co-seating is a strip state.</para></summary>
        public bool Solo { get; set; }

        /// <summary>
        /// #865 · DID THIS PERSON COME TO YOU? — the one fact the seated FRAME forks on.
        ///
        /// <para>Owner's ruling, live at a weighbridge clerk's table: <i>"what if I just sit and eat here…
        /// now I am kind of blinded of the surrounding here because somebody else sits in the same
        /// table"</i>, then <i>"It should somehow UI wise be same style as the sitting alone case"</i>, and
        /// sealing it: <i>"Just the social functions as additional options."</i></para>
        ///
        /// <para><b>Posture changes are a strip; people who come to you are a card.</b> A seat you CHOSE —
        /// an empty top, a bench, an office chair, a cubicle, and now the clerk's own top joined through
        /// [E] — is a posture change, and it presents in the docked strip with the room still lit behind it.
        /// Somebody crossing the hall and taking the chair opposite (#757's approach, #731's walker when she
        /// arrives) is an ENCOUNTER: her face is the point, and the card is what a face is for.</para>
        ///
        /// <para>This is deliberately NOT <see cref="Solo"/>, which is what it used to be. Solo asks <i>is
        /// the chair opposite empty</i>, and the two questions came apart the moment the owner ruled that
        /// joining a stranger keeps the room visible: at a top you joined, Solo is false and the frame is
        /// still the strip. A frame law read off an occupancy flag is one fact answering two questions, which
        /// is how the hall came to go black behind one small card.</para>
        /// </summary>
        public bool TheyCameToYou { get; set; }

        /// <summary>#783 · Whether this sitting READS AS RELAXED — boots up on the spare chair, which is a
        /// different sentence, a different goodbye and a different picture.
        /// <see cref="SittingAlone.SitReadsAsRelaxed"/> decides it; this carries the answer, so the prose and
        /// the art cannot come to two different views of the same minute.
        ///
        /// <para>NOT the same question as #784's <see cref="CaptainIsRestingAtATable"/>, and deliberately
        /// named apart from it: every solo sit is a short REST for the body (that is #784's mechanic), while
        /// this is whether the sit reads relaxed in WORDS AND PICTURES. A back-to-the-wall watch still gives
        /// your breath back; it is simply not the sentence about boots.</para></summary>
        public bool Relaxed { get; set; }

        /// <summary>#783 · …and whether there is a bought pour in it, which adds the drink's own line.</summary>
        public bool DrinkInHand { get; set; }

        /// <summary>#783 · The picture the panel wears, or null for a table that is somebody else's. Owner,
        /// live at a taken table: <i>"the pop up could have Gen AI here."</i> Two states, two images, off the
        /// one <see cref="Relaxed"/> answer above.</summary>
        /// <para>#793 · A BENCH WEARS NEITHER. Both pictures are photographs of a canteen table — an empty
        /// chair pulled out opposite, or boots up on that same chair — and a park bench has no chair
        /// opposite and no table to put anything on. A panel drawing one of them over a sit on gravel would
        /// be the picture and the sentence disagreeing, which is the fault this very field was added to
        /// avoid. A bench is always the DOCKED strip anyway (it is never a conversation), and the strip
        /// draws no art at all.</para>
        /// <para>#1040 · A STOOL WEARS NEITHER, for the bench's reason exactly: both pictures are
        /// photographs of a table with a chair pulled out opposite, and a counter has no chair opposite and
        /// no table. It is always the docked strip anyway (nobody ever comes aboard), and the strip draws no
        /// art — but a field that would answer wrongly if it were ever asked is a lie waiting for a
        /// caller.</para>
        /// <para>#1199 (2026-09-18) · <b>…UNLESS THE SEAT HAS A WINDOW IN FRONT OF IT</b>, in which case that
        /// is what the captain is looking at. The stool's own rule one room over (<c>Interior.TheStools</c>,
        /// #756/#759 — owner: <i>"I do not see the park through the bar windows?"</i>): standing at a fixture
        /// you are looking AT the fixture, and once you have sat down you are looking out of whatever is in
        /// front of it. A table in the observation walk's gallery has the rail, the glass and the Earth in
        /// front of it, and the room already owns that canvas. Carried ON THE SEAT rather than decided here,
        /// because which room a chair is in is the room's answer and never a panel's.</para>
        public string? Window { get; init; }

        public string? ArtUrl =>
            Window
            ?? (!Bench && !Stool && Who == CanteenTable.Who.None ? SittingAlone.ArtFor(Relaxed) : null);

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

        /// <summary>#758 · WHICH cabinet, as the plate beside its door reads — 0 anywhere else.
        ///
        /// <para><see cref="Quiet"/> is the room CLASS and this is the ROOM. They were one bool while a
        /// cabinet's only question was whether the counter has eyes in here, and they came apart the moment a
        /// cabinet acquired a state of its own: a curtain is drawn per LEAF, the keep writes down which one
        /// you were in, and the bark that knows too much says the number. Core's own
        /// (<see cref="CanteenRegulars.TableSeat.Cabinet"/>), carried rather than re-derived, so the top the
        /// captain is sitting at and the leaf they are dogging are one room.</para></summary>
        public int Cabinet { get; init; }

        /// <summary>
        /// #793 · Whether this seat is a PARK BENCH rather than a canteen top.
        ///
        /// <para>The same scene machinery holds a bench — one sitting, one panel, one WAIT beat, one short
        /// rest — because it IS the same posture, and a second seated panel beside this one would be a second
        /// answer to "is the captain sitting down". What the flag buys is the three places a bench is
        /// genuinely a different seat: which rung of the exposure ladder it is
        /// (<c>Map.Seated.SeatedIn</c>), what the room says when nobody comes (a park is not a hall), and
        /// what somebody arriving DOES — on a bench they sit down beside you and say nothing, which is not a
        /// conversation and must not raise one.</para>
        /// </summary>
        public bool Bench { get; init; }

        /// <summary>#793 · Which bench, as the park's own ordinal — for the deck and for the identity of the
        /// seat. <see cref="Index"/> carries the APPROACH ordinal instead, which is deliberately a different
        /// number (see <see cref="ParkBenches.ApproachOrdinal"/>).</summary>
        public int BenchIndex { get; init; }

        /// <summary>
        /// #817 · Whether this seat is a CHAIR AT A DESK in one of the park-view suites.
        ///
        /// <para>The same machinery again, and for the owner's own stated reason — an office table is the
        /// restaurant's table <i>"just more rectangular"</i> with <i>"the functionality … about the same
        /// otherwise"</i>. What the flag buys is the two places an office genuinely differs: NOBODY EVER
        /// COMES OVER (the staff of this building are somewhere else, and the canteen's recruiter walking
        /// across an empty office would be the scene and the room disagreeing), and the silence is described
        /// in an office's own words rather than a hall's.</para>
        /// </summary>
        public bool Office { get; init; }

        /// <summary>
        /// #1016 · Whether this seat is on the captain's OWN SHIP — a top in her cantina, or the desk in
        /// CABIN 1.
        ///
        /// <para>Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin
        /// either?"</i>, <i>"I expect to have a bar table like this in this ships galley also.... feature
        /// complete."</i> The same machinery a third time, and for the third time the flag buys exactly the
        /// places a boat genuinely differs from a hall — <see cref="Office"/>'s own two, in fact. NOBODY EVER
        /// COMES OVER: her crew is three droids on a fixed patrol, and a haulier crossing the captain's own
        /// cantina to ask about her brother would be the scene and the ship disagreeing. And the silence is
        /// described in the BOAT's words (<see cref="SittingAlone.NobodyCameAboard"/>) rather than in an
        /// eighty-seat canteen's.</para>
        ///
        /// <para>It is deliberately not <see cref="Office"/> reused: an office is a room in somebody else's
        /// building on a shift that no longer runs, and its silence says so. Two rooms that agree about one
        /// mechanic and disagree about every word are two flags.</para>
        /// </summary>
        public bool Aboard { get; init; }

        /// <summary>
        /// #1040 · Whether this seat is a STOOL AT A COUNTER — today, the row bolted along the ship's own
        /// cantina counter.
        ///
        /// <para>Owner, on 7 Deck: <i>"Our on ship bar can be upgraded to match the other bars... the UI
        /// represents code long time ago."</i> The same machinery a fourth time, and the flag buys exactly
        /// the one place a stool genuinely differs from a top: <b>the rung</b>. <c>SeatedIn</c> reads it and
        /// answers <see cref="SeatedHud.Seat.BarStool"/>, which is where the gumshoe rule lives — the case
        /// does not come out at a bar, and it does not come out at your own bar either
        /// (<see cref="SeatedSpread.NotAtTheBarLine"/>, said out loud, never silently).</para>
        ///
        /// <para>It is deliberately NOT the counter's own <c>StoolSeat</c> (<c>Seating.Stool.cs</c>) reused:
        /// that seat is a fact about a Hive canteen's frozen watch, its floor and its neighbour, and every
        /// one of those is a question a boat cannot be asked. What the two share is the RUNG, and the rung
        /// is the thing this flag reaches.</para>
        /// </summary>
        public bool Stool { get; init; }

        /// <summary>
        /// #1016 · HOW MANY BEATS HAVE BEEN WAITED OUT AT THIS SEAT — <b>aboard only</b>, and null anywhere
        /// else in the sense that nothing reads it there.
        ///
        /// <para>Ashore the beat counter is the ROOM's (<c>ex.TableWaits</c>) and that is a law rather than a
        /// convenience: the approach is seeded on (site, floor, top, watch, beat), so a captain who stood up
        /// and sat down again to reroll the dice simply carries on from the beat they were on. <b>There is no
        /// such dice aboard</b> — nobody ever comes to either of the ship's seats — so there is nothing to
        /// reroll and nothing to abuse, and the counter's only job is to stop the two silence lines looping
        /// on the first one. It lives on the sitting because the ship has no excursion to keep a ledger on,
        /// and inventing one for a number that decides which of two sentences you read would be a second
        /// ledger for a fact nothing else asks about.</para>
        /// </summary>
        public int Waits { get; set; }

        /// <summary>
        /// #1016 · WHICH SHIFT THIS SITTING IS ON, when there is no excursion to ask.
        ///
        /// <para>Ashore the watch is the room's and is frozen on the excursion (<c>ex.CanteenWatch</c>,
        /// #709), and this stays 0 there and is never read — a second copy of a fact the room already holds
        /// is one source consumed in the wrong order waiting to happen. It is written only by the sittings
        /// that HAVE no excursion behind them (a top in a docked station's bar, and the ship's own two), for
        /// the one question the silence has to ask a clock: which of the hall's two pools a fruitless wait
        /// comes out of.</para>
        /// </summary>
        public long Watch { get; init; }

        /// <summary>
        /// #820 · WHERE STANDING UP PUTS THE BODY — the square this seat is stepped off onto.
        ///
        /// <para>Worked out from published geometry at the moment the captain sat down and carried here, so
        /// that standing up does not have to go and look the furniture up a second time (and cannot come to
        /// a different answer if the watch has turned over in between). It is the seat's own square wherever
        /// a seat is a place you could have been standing anyway — a ring-office chair
        /// (<see cref="RingOffice.Chair.StandAt"/>), a chair round a canteen top — and the WALK SIDE of the
        /// plank at a park bench, which is solid and would otherwise close over the dot the moment the
        /// sitting ended.</para>
        ///
        /// <para>Null at a sitting that never moved the body, which is no sitting that ships today; the
        /// standing simply leaves the captain where they are rather than inventing a square for them.</para>
        /// </summary>
        public (double X, double Y)? StepOff { get; init; }

        /// <summary>
        /// #821 · WHICH WC CUBICLE THIS SEAT IS IN, by <c>HiveInterior.CubicleKey</c>, or null anywhere else.
        ///
        /// <para>An office chair and a cubicle's pan are the same POSTURE and the same panel — the seam
        /// <see cref="Office"/> already opened — and what the cubicle adds is one question the ladder has to
        /// be able to ask: <b>is the catch over right now?</b> The key is carried rather than the answer,
        /// because the answer changes while you are sitting on it: a captain can sit down in an open cubicle,
        /// reach back, turn the catch, and the spread has to become allowed on that very frame.</para>
        /// </summary>
        public string? CubicleKey { get; init; }

        /// <summary>
        /// #793 · SOMEBODY IS ON THE OTHER END OF THIS BENCH.
        ///
        /// <para>Deliberately NOT <see cref="Solo"/>, and the distinction is the whole of the bench rung.
        /// <c>Solo</c> means <i>this is not a conversation</i> — it is what decides whether the frame docks
        /// or the full card comes up, and whether the wait beat and the short rest run at all. A stranger who
        /// sits down at the far end of a plank has started no conversation with anybody; you are still
        /// sitting, still resting, still findable. What you have lost is PRIVACY, and this is the flag that
        /// says so — the one <c>SeatedAlone</c> reads for the spread.</para>
        ///
        /// <para>Settable, because one sitting can change occupancy in both directions exactly as a table's
        /// chair opposite can.</para>
        /// </summary>
        public bool SharedSeat { get; set; }

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
        /// that was made before you left the table, because the man made it to somebody who then stood up.</para>
        ///
        /// <para>#731 v2 · <c>init</c>, and only init: a sitting may be OPENED with things already said in it
        /// — the conversation she stood up in the middle of, carried across the hall and resumed in a booth —
        /// and that has to happen in the initializer, because the one construction method is the whole of
        /// #870 lane 6d's law. It is still never reassigned after that; the set is added to and cleared in
        /// place, and this accessor cannot be reached again.</para></summary>
        public HashSet<string> Said { get; init; } = [];
    }

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

    // -- #870 lane 6c · THE FORWARDERS, AND EVERY ONE OF THEM HAS A CALLER OUTSIDE THIS FAMILY --------
    //
    // The scene lives on Seating now (Seating.Table.cs). These nine are the spellings the rest of the page
    // still asks for BY NAME, measured rather than assumed: five presses and three question-askers in
    // Map.razor, the [E] arm in Map.Deck.Interact.cs, the Esc chain in Map.Sim.Cancel.cs, and the ?spread=1
    // row above, which sits the captain down through the very handler a captain's own press reaches.
    //
    // Everything else the scene does -- the wait beat, the dice, the arrival, the one place an outcome
    // becomes words -- is reached from inside the seat and kept no forwarder at all. Do not add one for a
    // NEW question: add the question to Seating and ask it through _seating.

    /// <inheritdoc cref="Seating.TryOpenTable"/>
    private bool TryOpenTable() => _seating.TryOpenTable();

    /// <inheritdoc cref="Seating.TryTakeTable"/>
    private bool TryTakeTable() => _seating.TryTakeTable();

    /// <inheritdoc cref="Seating.CloseTable"/>
    private void CloseTable() => _seating.CloseTable();

    // #794 · …except FEEL UNDER THE LIP, which moves things the seat does not own and is answered by the
    // page first (Map.ChalkMark's TheLipIsFelt). #1202 slice 2 · …and TAKE THE PAGES / LEAVE YOUR PAGE, for
    // the same reason (Map.SpikeIt's ThePagesAreTouched).
    /// <inheritdoc cref="Seating.TableMoveClicked"/>
    private Task TableMoveClicked(string moveId) =>
        ThePagesAreTouched(moveId) ? Task.CompletedTask
        : TheLipIsFelt(moveId) ? Task.CompletedTask : _seating.TableMoveClicked(moveId);

    /// <inheritdoc cref="Seating.TableMoveOnOffer"/>
    private bool TableMoveOnOffer(Encounter.Move move) => _seating.TableMoveOnOffer(move);

    /// <inheritdoc cref="Seating.TableMoveRefusal"/>
    private string TableMoveRefusal(Encounter.Move move) => _seating.TableMoveRefusal(move);

    /// <inheritdoc cref="Seating.TableMoveIsUrged"/>
    private bool TableMoveIsUrged(Encounter.Move move) => _seating.TableMoveIsUrged(move);

    /// <inheritdoc cref="Seating.TableShow"/>
    private void TableShow(Core.Satchel.Item item) => _seating.TableShow(item);

    /// <inheritdoc cref="Seating.TableShowables"/>
    private IReadOnlyList<Core.Satchel.Item> TableShowables() => _seating.TableShowables();

    // ── #758 · THE CURTAIN AND THE DOOR, FROM THE STRIP ───────────────────────────────────────────────
    //
    // Three of these are the ordinary forwarders the rule above allows for a press and the two things its
    // button has to draw. The fourth is the one that could not live in the seat: working a leaf changes what
    // the DECK says about that leaf, and rebuilding a deck is a page's job — a seat that could do it would
    // need a twenty-ninth member on ISeatHost, and that interface may only shrink.

    /// <inheritdoc cref="Seating.ACabinetLeafToWork"/>
    private bool ACabinetLeafToWork => _seating.ACabinetLeafToWork;

    /// <inheritdoc cref="Seating.CabinetLeafLabel"/>
    private string CabinetLeafLabel => _seating.CabinetLeafLabel;

    /// <inheritdoc cref="Seating.CabinetLeafHint"/>
    private string CabinetLeafHint => _seating.CabinetLeafHint;

    /// <summary>#758 · Draw the curtain, or dog the door. The seat decides and remembers
    /// (<see cref="Seating.DrawOrDogTheCabinet"/>); this puts the answer back on the plan, because the
    /// cabinet's glyph is drawn from the set that press just changed and a plan still showing cloth over a
    /// dogged leaf is the picture and the sim disagreeing about a room the captain is sitting in.</summary>
    private void WorkTheCabinetLeaf()
    {
        if (!ACabinetLeafToWork)
        {
            return;
        }
        _seating.DrawOrDogTheCabinet();
        RebuildSurfaceDeck();
    }
}
