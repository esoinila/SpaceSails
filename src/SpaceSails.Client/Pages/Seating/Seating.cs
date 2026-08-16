using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6b - THE SEAT'S STATE IS ONE OBJECT.
///
/// <para>The seat family is seven partials of <see cref="Map"/> - <c>Map.Seated.cs</c>, <c>Map.Stool.cs</c>,
/// <c>Map.Table.cs</c>, <c>Map.Bench.cs</c>, <c>Map.OfficeChair.cs</c>, <c>Map.Cubicle.cs</c>,
/// <c>Map.SitStandDesk.cs</c> - and between them they held FIVE fields loose on the page. 6a made the
/// outside world stop reading them raw; this lane gathers them, and every question that is a pure function
/// of them, into <see cref="Seating"/>. <b>The page now has one seat field where it had five</b>, and a
/// reader who wants to know what sitting down IS has one file to open instead of seven.</para>
///
/// <h3>What is in here and what deliberately is not</h3>
///
/// <para>IN: the five, and every read whose whole answer comes off them - no excursion, no avatar, no
/// renderer, no clock. Those reads moved with their docblocks unchanged and their bodies changed by ONE
/// substitution each (the receiver), which is what makes this lane provable rather than reviewed.</para>
///
/// <para>NOT IN: every question that also consults the world. <see cref="SeatedIn"/> asks
/// <see cref="CubicleIsShut"/>, and a shut door is a fact about the excursion's own set - so it stays, and
/// so does everything derived from it (<see cref="CaptainIsSeatedAnywhere"/>,
/// <see cref="CanSpreadTheCaseHere"/>, <see cref="SpreadRefusal"/>). So do <see cref="SeatedAlone"/>,
/// <see cref="StandingUpCostsARest"/>, <see cref="SeatedCustomerLine"/>, and the table's and stool's own
/// offer/refusal pairs, which read the purse and the sleeve.</para>
///
/// <para>And NOT the VERBS. Taking a seat, getting off one, arming and spending the sit beat all still live
/// in the family partials, and now write <c>_seating.Table</c> where they wrote a loose field. #870's 6c is
/// the lane that moves those, behind an explicit host surface.</para>
///
/// <h3>Why it is nested</h3>
///
/// <para>Because <see cref="TableTalk"/> and <see cref="StoolSeat"/> - the two records this state is MADE
/// of - are private nested types of <see cref="Map"/>. A top-level class would have had to widen both to
/// <c>internal</c>, publishing the seat's own furniture to the whole client assembly on the very lane whose
/// point is narrowing the seat's surface; and it would have broken five <c>see cref</c>s in docblocks that
/// were meant to travel untouched. Nested, the moved text needs nothing it did not already have in scope.
/// It is still ONE object with ONE surface, and 6c's host interface is unaffected by where it is
/// declared.</para>
/// </summary>
public partial class Map
{
    /// <summary>#870 lane 6b - THE SEAT, as one thing. The page's only seat field: everything that used to
    /// be five loose fields on <see cref="Map"/> is reached through here, and
    /// <c>TheSeatKeepsItsOwnStateTests</c> is what keeps it that way.
    ///
    /// <para><c>readonly</c>, and never re-assigned: standing up EMPTIES the seat, it does not swap in a
    /// different one. A captain who stood up and a captain who never sat are the same captain, and a second
    /// <see cref="Seating"/> would be a second answer to "where am I sitting" - this repo's first named bug
    /// class, aimed at a posture.</para>
    ///
    /// <para>#870 lane 6c - it is BUILT IN THE CONSTRUCTOR now rather than at its declaration, and that is a
    /// language rule rather than a design change: the seat is handed the page it is a seat on
    /// (<see cref="ISeatHost"/>), and an instance field initialiser may not name <c>this</c>. Still one seat,
    /// still assigned exactly once.</para>
    ///
    /// <para>#870 lane 6′c - and the ASSIGNMENT moved out of this file, into <c>Map.Collaborators.cs</c>. The
    /// round is handed the page the same way now, and one constructor cannot live in two families' files; a
    /// page field named in a seat file is also exactly what the seat's own sweep is there to catch, and an
    /// exemption for it would have been a guard weakened to make room for a second collaborator.</para></summary>
    private readonly Seating _seating;

    /// <summary>
    /// #870 lane 6b - WHAT IT IS TO BE SITTING DOWN: the whole of the state, and every question whose answer
    /// is a pure function of it.
    ///
    /// <para>Five fields. A table (or a bench, or a cabinet, or an office chair, or a cubicle - they are all
    /// one sitting with a flag on it), a stool at the counter, whether the stand-up confirm is on the screen,
    /// how much of the sit beat is still owed, and which floor the cubicle cache was read off.</para>
    ///
    /// <para>It knows nothing about the world it sits in. Nothing in here reads an excursion, a deck plan, a
    /// clock or a renderer - a question that needs the world is a question <see cref="Map"/> answers, and the
    /// list of those is in this file's own summary above.</para>
    ///
    /// <para>#870 lane 6c - AND NOW IT HAS THE VERBS TOO, in the partials beside this file
    /// (<c>Seating.Seated.cs</c>, <c>Seating.Table.cs</c>, <c>Seating.Stool.cs</c>, <c>Seating.Bench.cs</c>,
    /// <c>Seating.OfficeChair.cs</c>). What it still needs from the page it is drawn on is
    /// <see cref="ISeatHost"/> and NOTHING else - twenty-eight members, written down, and a guard that says
    /// the number may only come down. The paragraph above is still true where it is aimed: the reads in THIS
    /// file are pure functions of the five, and the verbs next door reach the world through
    /// <see cref="_host"/> or not at all.</para>
    /// </summary>
    private sealed partial class Seating
    {
        /// <summary>#870 lane 6c - THE PAGE, and the only way to it. Every reach out of this object goes
        /// through here, which is what makes <see cref="ISeatHost"/> the true and complete list of what a
        /// chair needs from the page it is bolted to.</summary>
        private readonly ISeatHost _host;

        /// <summary>A seat is a seat ON something. There is no parameterless way to make one, deliberately:
        /// a <see cref="Seating"/> with no host would be a chair floating in the dark, and every verb on it
        /// would have to check.</summary>
        public Seating(ISeatHost host) => _host = host;

        // -- THE FIVE, IN ONE PLACE --------------------------------------------------------------------

        /// <summary>The open table, or null. One at a time: you are sitting at it.</summary>
        public TableTalk? Table { get; set; }

        /// <summary>The stool you are on, or null while you are standing at the counter. One at a time — there
        /// is only one of you.</summary>
        public StoolSeat? Stool { get; set; }

        /// <summary>#784 · Whether the little stand-up confirm is up. Its own flag and not a mode on the table:
        /// it is a question ABOUT the table, and it has to be able to go away without the table going with
        /// it.</summary>
        public bool StandUpAsk { get; set; }

        /// <summary>#865 · How much of the sit beat is still owed, in real seconds. Armed by the ONE placement
        /// that sits a captain down (<c>SitCaptainOn</c>) and spent by the surface tick, so every seat kind in
        /// the game gets it without a single verb having to remember to ask.</summary>
        public double SitBeatOwedSeconds { get; set; }

        /// <summary>#821 · The floor <see cref="_floorCubicles"/> was read off, or null before anything has been
        /// read. See <see cref="CubiclesOn"/> for why there is a cache here at all.</summary>
        public string? CubicleFloorKey { get; set; }

        // ── IS THE CAPTAIN SITTING DOWN? ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// #784 · THE ONE ANSWER. The table panel being open IS the captain being in the chair — #757 built it
        /// that way (the seat is the spot you walked to; nothing teleports anybody onto furniture) and this does
        /// not invent a parallel flag beside it, because a second answer to "are you sitting down" would be this
        /// repo's first named bug class aimed at a posture.
        ///
        /// <para>#847 · IT IS THE ONE ANSWER ABOUT A TABLE, and the reader has to know that much: it is what the
        /// DRAWN posture and the stand-up confirm are wired to, and a counter stool has never been in it. The
        /// question <i>am I on a seat of any kind</i> — which is the one every MOVEMENT path now asks — is
        /// <see cref="CaptainIsSeatedAnywhere"/> one screen down.</para>
        /// </summary>
        public bool CaptainIsSeated => Table is not null;

        /// <summary>#870 lane 6a · THE SITTING ITSELF, for the one reader outside this family that needs the
        /// whole of it rather than a question about it: the seated frame in <c>Map.razor</c>, whose strip and
        /// card draw the plate, the art, the setting, the free chairs, the outcome and every move off this
        /// object.
        ///
        /// <para>It exists so that <c>Table</c> is the seat family's field and nobody else's — the markup
        /// renames a receiver and stops reaching into a private field of a partial it does not own (the guard is
        /// <c>TheSeatKeepsItsOwnStateTests</c>, and lane 6b is what it is protecting). Anything that wants a
        /// FACT rather than the furniture asks for the fact: <see cref="CaptainIsSeated"/>,
        /// <see cref="SeatedIsDocked"/>, <see cref="SeatedWithCompany"/>, <see cref="SeatedIn"/>,
        /// <see cref="SeatedAlone"/>. Do not add a second raw reader through here; add the question you
        /// mean.</para></summary>
        public TableTalk? SeatedTable => Table;

        /// <summary>
        /// #784/#783 · IS THE CAPTAIN RESTING — the question a panel asks when it is choosing which picture to
        /// draw. A table you took ALONE is a rest; a table with somebody talking across it is a conversation,
        /// whatever your legs are doing.
        /// </summary>
        public bool CaptainIsRestingAtATable => Table is { Solo: true };

        /// <summary>#793 · Is the captain sitting still, outdoors, where the walk runs clear both ways? The one
        /// question <see cref="FootTail"/> is asked with, named here so the law reads as a sentence at both call
        /// sites. A bench only exists in the park, so being on one IS being in the open.</summary>
        public bool SeatedOnABenchInTheOpen => Table is { Bench: true };

        // ── PHASE TWO · THE FRAME DOCKS, IT DOES NOT DIM ──────────────────────────────────────────────────
        //
        // Owner, live at a taken table with the panel up (his own screenshot read: the card centred, the backdrop
        // dimming the entire hall AND the new park to near-black behind the glass): "the seated frame docks, it
        // does not dim… the hall stays lit, the A* walkers stay visible, the park stays green. The full card
        // returns only for conversations." And, classifying the whole family: "Like the sit a while… kind of same
        // issue as operating with the service spot and menu — the card is for CONVERSATIONS; the room is for
        // everything else."
        //
        // THE STATE MACHINE, in one place so nothing else has to derive it:
        //
        //   seated-idle   (Table is { TheyCameToYou: false }) → the DOCKED STRIP. No backdrop element at all.
        //   conversation  (Table is { TheyCameToYou: true })  → the full card, backdrop and art (#778/#787).
        //   standing      (Table is null)                     → neither.
        //
        // #865 · THE FORK MOVED OFF SOLO, and it moved because the owner drew the line somewhere else. It USED
        // to read Solo — the chair opposite being empty — and that was the same fact answering two questions.
        // Solo is OCCUPANCY: it is what the privacy ladder reads, and a top you are sharing is not a top you lay
        // a case out on, whoever chose to sit there. The FRAME is a different question, and the owner answered it
        // live at a weighbridge clerk's table with the whole hall dimmed to black behind one small card:
        //
        //   "what if I just sit and eat here... now I am kind of blinded of the surrounding here because
        //    somebody else sits in the same table"
        //   "It should somehow UI wise be same style as the sitting alone case."
        //   "Just the social functions as additional options."
        //
        // So: POSTURE CHANGES ARE A STRIP, PEOPLE WHO COME TO YOU ARE A CARD. Every seat the captain CHOSE — an
        // empty top, a bench, an office chair, a cubicle, and now the clerk's own top joined through [E] — is a
        // posture change and docks. Somebody crossing the hall and taking the chair opposite (#757's approach,
        // and #731's walker when she walks it for real) is an ENCOUNTER: her face is the point, and a card is
        // what a face is for. TableTalk.TheyCameToYou carries that and nothing else does.
        //
        // The COUNTER is already docked and stays that way: #780/#789 took the bar card out from under the scrim,
        // so a stool has never dimmed the room. There is nothing to change there and the guard says so in the
        // affirmative rather than this file growing a second frame for it.

        /// <summary>#784/#865 · Is the seated frame in its DOCKED state right now — the HUD strip, with the hall
        /// lit and running behind it? The one question the razor asks, so the frame law lives here and not in
        /// markup.</summary>
        public bool SeatedIsDocked => Table is { TheyCameToYou: false };

        /// <summary>#784/#865 · …and the other half, named for the reader rather than derived at each use: a
        /// table somebody CAME OVER TO is a conversation and gets the card it has always had.</summary>
        public bool SeatedIsAConversation => Table is { TheyCameToYou: true };

        /// <summary>#865 · Is the captain sharing this top with somebody — the fact the strip's company lines and
        /// the customer line's own seat clause hang off. It is <c>Solo</c> read the plain way round, named here
        /// so the razor asks a question instead of negating a flag.</summary>
        public bool SeatedWithCompany => Table is { Solo: false };

        /// <summary>
        /// #865 · WHO IS SHARING THE TOP — the strip's company line, or null at a seat you have to yourself.
        ///
        /// <para>Owner: <i>"It should somehow UI wise be same style as the sitting alone case."</i> So this is
        /// not a second panel and not a second component: it is one more line on the strip the captain already
        /// had, built by <see cref="SeatedHud.CompanyLine"/> out of the plate the deck draws over them and the
        /// room's own chair arithmetic — every figure of it the same figure the card was printing.</para>
        /// </summary>
        public string? SeatedCompanyLine() =>
            Table is { Solo: false } t
                ? SeatedHud.CompanyLine(t.Plate, t.Scene.Setting, t.Seats, t.Free)
                : null;

        /// <summary>
        /// #865 · THEIR ONE BREATH, OVERHEARD — the neighbour's bark on the strip's own line, or null.
        ///
        /// <para>Shown only until something has actually been SAID at this table: the strip has one outcome slot
        /// (#680's law travelled onto the surface with it) and a bark left standing under the answer it becomes
        /// when Small talk is pressed would print the same sentence twice. Before that press it is the whole of
        /// what sitting down at somebody else's top gets you, which is #751's own design read out loud.</para>
        /// </summary>
        public string? SeatedOverheardLine() =>
            Table is { Solo: false, Bark: { Length: > 0 } bark, Outcome: null or "" }
                ? SeatedHud.OverheardLine(bark)
                : null;

        // ── #870 lane 6a · WHAT THE COUNTER CARD ASKS ─────────────────────────────────────────────────────
        //
        // The bar card is drawn in Map.razor and it is ONE CARD IN TWO POSTURES (#756): standing at the counter
        // it is the counter's, up on a stool it is the stool's. The markup used to ask that by reading Stool
        // straight out of Map.Stool.cs three times over. These two members are those three reads named, and
        // nothing else changed about what gets drawn.
        //
        // Note that SeatedIn == SeatedHud.Seat.BarStool is NOT the same question and must not be substituted for
        // it: SeatedIn asks the table FIRST, so a captain who is somehow at a top would answer "not on a stool"
        // while the stool state was still set. The card wants the posture it is drawing, not the ladder rung.

        /// <summary>#870 lane 6a · Is the captain up on a counter stool right now? Asked by the bar card twice —
        /// once to pick the window-wall picture over the desk art, once to choose between the take-a-stool verb
        /// and the seated row.</summary>
        public bool CaptainIsOnAStool => Stool is not null;

        /// <summary>#870 lane 6a · What the bar card's plate says when the captain is sitting on one — the
        /// neighbour's plate if she has turned to you, otherwise this stool's own number — and null on your feet,
        /// where the card falls back to the keep's own name. The pick used to live in the markup with the field
        /// under it; the sentence is Core's either way (<see cref="Core.Interior.TheStools"/>).</summary>
        public string? SeatedStoolPlate =>
            Stool is { } s
                ? s.WithNeighbour
                    ? Core.Interior.TheStools.NeighbourPlate
                    : Core.Interior.TheStools.StoolPlate(s.Index)
                : null;

        // -- WHAT IS ON THE PANEL ----------------------------------------------------------------------
        //
        // #870 lane 6b - Both of these are one line of Core's own call over the sitting's own memory, and
        // both are asked by the markup. Their siblings - the offer and refusal pairs - stayed on Map,
        // because those read the purse and the excursion and are therefore questions about the world.

        /// <summary>#749 · The moves that are ON THE TABLE — the ones that exist to be pressed at all. Core's
        /// own call, so the day a checkpoint renders through this same block it cannot have a different idea of
        /// when an answer exists. Everything the captain has simply not earned is still HERE and still refused
        /// out loud (#603); what is missing is only what nobody has said yet.</summary>
        public IReadOnlyList<Encounter.Move> TableMovesOnTheTable() =>
            Table is { } t ? Encounter.OnTheTable(t.Scene, t.Said) : [];

        /// <summary>#749 · What is on the stool's panel right now. Core's own call, so a reply to a sentence
        /// nobody has spoken is not drawn at all.</summary>
        public IReadOnlyList<Encounter.Move> StoolMovesOnTheTable() =>
            Stool is { } s ? Encounter.OnTheTable(s.Scene, s.Said) : [];

        // -- THE TWO SMALL CLOCKS ----------------------------------------------------------------------
        //
        // #870 lane 6b - The confirm and the sit beat, asked. Both are ARMED and SPENT by verbs that stayed
        // on Map (AskWhetherToStandUp / KeepYourSeat, OweTheSitBeat / SpendTheSitBeat); what is here is only
        // the reading of them, which is all anybody outside the family ever wanted.

        /// <summary>#870 lane 6a · Is that confirm on the screen right now? Asked from outside the family in two
        /// places, and they are the two ends of one question: the card itself in <c>Map.razor</c>, and the cancel
        /// / confirm chains in <c>Map.Sim.Cancel.cs</c>, which must reach it before they peel the panel
        /// underneath it (Esc keeps the seat; Enter is the one question that key is allowed to answer).</summary>
        public bool TheStandUpConfirmIsUp => StandUpAsk;

        /// <summary>#865 · Is the captain still in the middle of taking the chair? Asked by the first-entry polls
        /// and by the story-card seam, and by nothing that decides what a room CONTAINS — this holds a
        /// PRESENTATION back for a beat and never changes what the world did.</summary>
        public bool TheSitBeatIsSettling => SitBeatOwedSeconds > 0.0;
    }
}
